// MainForm.cs: Full class with restored tooltip/hover (black line), back button fix via BackAction, and axis restore on back
using KalshiBotData.Data;
using KalshiBotData.Models;
using Microsoft.Extensions.Configuration;
using SmokehouseDTOs;
using System.Text.Json;
using System.Text.RegularExpressions;
using TradingSimulator.Simulator;
using TradingSimulator.TestObjects;

namespace SimulatorWinForms
{
    public partial class MainForm : Form
    {
        private readonly SimulatorTests _simulator;
        private readonly KalshiBotContext _context;
        private readonly string _cacheDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "TestingOutput");
        private List<(double x, double y, string memo)> _tooltipPoints = new();
        private string? _lastTooltipMemo = null;
        private HashSet<string> _checkedMarketNames = new();
        private bool _simSetup;
        private Label _tooltipOverlay;
        private ScottPlot.Plottable.VLine _hoverLine;
        private bool _isRightPanning;
        private Point _panStartPx;
        private (double xMin, double xMax, double yMin, double yMax) _panStartLimits;

        private readonly Dictionary<string, double> _bestPnL = new(StringComparer.OrdinalIgnoreCase);

        List<MarketSnapshot> _snapshots = new();

        private (double xMin, double xMax, double yMin, double yMax) _savedChartLimits;

        private SnapshotViewer _snapshotViewer;

        // Original dimensions for scaling reference
        private const int OriginalWidth = 1100;
        private const float MinScale = 0.8f;  // Minimum font scale (80% of original)
        private const float MaxScale = 2.0f;  // Maximum font scale (200% of original)

        public MainForm()
        {
            InitializeComponent();

            toolTip1.ShowAlways = true;

            // Initialize database context
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=KalshiBot;Trusted_Connection=True;"
                })
                .Build();
            _context = new KalshiBotContext(config);

            _simulator = new SimulatorTests();
            _simulator.EnsureInitialized();
            _simulator.OnTestProgress += msg => AppendLog(msg);
            _simulator.OnProfitLossUpdate += (m, pnl) => UpdatePnL(m, pnl);
            _simulator.OnMarketProcessed += m => AppendLog($"✔ Processed {m}");
            // DISABLE ScottPlot's built-in pan/zoom to prevent conflicts
            formsPlot1.Configuration.Pan = false;
            formsPlot1.Configuration.Zoom = false;
            formsPlot1.Configuration.ScrollWheelZoom = false;

            formsPlot1.MouseMove += FormsPlot1_MouseMove;
            formsPlot1.MouseLeave += FormsPlot1_MouseLeave;

            Load += (_, __) => LoadCache();
            dgvMarkets.SelectionChanged += DgvMarkets_SelectionChanged;

            dgvMarkets.CellValueChanged += dgvMarkets_CellValueChanged;
            dgvMarkets.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dgvMarkets.IsCurrentCellDirty)
                    dgvMarkets.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            dgvMarkets.Sorted += (s, e) => RestoreCheckboxes();

            _tooltipOverlay = new Label
            {
                AutoSize = true,
                Visible = false,
                BackColor = Color.Khaki,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(4),
                MaximumSize = new Size(dgvMarkets.Width - 16, 0)
            };
            dgvMarkets.Controls.Add(_tooltipOverlay);
            _tooltipOverlay.Location = new Point(8, 8);
            _tooltipOverlay.BringToFront();

            dgvMarkets.Resize += (_, __) =>
            {
                _tooltipOverlay.MaximumSize = new Size(dgvMarkets.Width - 16, 0);
            };

            // thin vertical hover indicator (black now)
            _hoverLine = formsPlot1.Plot.AddVerticalLine(0, Color.Black, 1);
            _hoverLine.IsVisible = false;

            // wire a MouseDown handler to allow swapping the chart with the dashboard on click
            formsPlot1.MouseDown += FormsPlot1_MouseDown;

            // Add resize handler for dynamic scaling
            this.ResizeEnd += MainForm_ResizeEnd;

            // Add activation handler to reset panning state
            this.Activated += MainForm_Activated;
        }

        private void ResetPnLForMarkets(IEnumerable<string> markets)
        {
            if (dgvMarkets.InvokeRequired)
            {
                dgvMarkets.BeginInvoke(new Action(() => ResetPnLForMarkets(markets)));
                return;
            }

            var set = new HashSet<string>(markets ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            foreach (DataGridViewRow row in dgvMarkets.Rows)
            {
                var marketName = row.Cells["Market"].Value?.ToString();
                if (string.IsNullOrWhiteSpace(marketName)) continue;
                if (!set.Contains(marketName)) continue;

                _bestPnL[marketName] = double.NegativeInfinity; // clear best
                row.Cells["PnL"].Value = "";                    // visually reset
            }
        }

        private async Task LoadCache(List<string>? includeBases = null)
        {
            dgvMarkets.Columns.Clear();
            dgvMarkets.Rows.Clear();

            dgvMarkets.RowHeadersVisible = false;
            dgvMarkets.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            var checkCol = new DataGridViewCheckBoxColumn { Name = "CheckedCol", HeaderText = "✓", AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells };
            var marketCol = new DataGridViewTextBoxColumn { Name = "Market", HeaderText = "Market", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 30 };
            var titleCol = new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "Title", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 40 };
            var pnlCol = new DataGridViewTextBoxColumn { Name = "PnL", HeaderText = "PnL", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells };
            dgvMarkets.Columns.Add(checkCol);
            dgvMarkets.Columns.Add(marketCol);
            dgvMarkets.Columns.Add(titleCol);
            dgvMarkets.Columns.Add(pnlCol);

            dgvMarkets.HandleCreated += (_, __) =>
            {
                if (dgvMarkets.Columns["CheckedCol"] is DataGridViewCheckBoxColumn c) c.Width = 32;
            };

            // Always drive entries from snapshot groups, filtered for validity
            try
            {
                _simulator.EnsureInitialized();

                List<string> basesToUse;
                if (includeBases == null)
                {
                    // Compute all possible bases from all group names
                    var groupNames = await _simulator.GetSnapshotGroupNames();
                    basesToUse = groupNames
                        .Select(g => Regex.Replace(g ?? "", @"_(\d+)$", ""))
                        .Where(b => !string.IsNullOrWhiteSpace(b))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
                else
                {
                    basesToUse = includeBases;
                }

                // Retrieve valid base markets, filtered by basesToUse
                var bases = await _simulator.GetValidBaseMarketsAsync(basesToInclude: basesToUse);

                // Optional enrichment from cache (if present)
                var pnlByBase = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                if (Directory.Exists(_cacheDir))
                {
                    foreach (var b in bases)
                    {
                        try
                        {
                            var canonical = Path.Combine(_cacheDir, $"{b}.json");
                            if (File.Exists(canonical))
                            {
                                var json = File.ReadAllText(canonical);
                                var cd = JsonSerializer.Deserialize<CachedMarketData>(json);
                                if (cd != null)
                                {
                                    pnlByBase[b] = cd.PnL;
                                }
                                continue;
                            }

                            var parts = Directory.GetFiles(_cacheDir, $"{b}_*.json");
                            if (parts.Length > 0)
                            {
                                double sum = 0;
                                foreach (var p in parts)
                                {
                                    var pj = File.ReadAllText(p);
                                    var d = JsonSerializer.Deserialize<CachedMarketData>(pj);
                                    if (d != null)
                                    {
                                        sum += d.PnL;
                                    }
                                }
                                pnlByBase[b] = sum;
                            }
                        }
                        catch (Exception ex)
                        {
                            AppendLog($"Cache enrich failed for {b}: {ex.Message}");
                        }
                    }
                }

                // Query markets from database
                var markets = await _context.GetMarkets();
                foreach (var market in markets.OrderBy(m => m.market_ticker))
                {
                    var pnlStr = pnlByBase.TryGetValue(market.market_ticker, out var pnl) ? pnl.ToString("F2") : "";
                    var row = dgvMarkets.Rows.Add(false, market.market_ticker, market.title, pnlStr);
                    dgvMarkets.Rows[row].Cells["CheckedCol"].Value = _checkedMarketNames.Contains(market.market_ticker);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"LoadCache failed: {ex}");
            }
        }

        private void RestoreCheckboxes()
        {
            foreach (DataGridViewRow row in dgvMarkets.Rows)
            {
                var marketName = row.Cells["Market"].Value?.ToString();
                if (marketName != null)
                    row.Cells["CheckedCol"].Value = _checkedMarketNames.Contains(marketName);
            }
        }

        private void dgvMarkets_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == dgvMarkets.Columns["CheckedCol"].Index)
            {
                var marketName = dgvMarkets.Rows[e.RowIndex].Cells["Market"].Value?.ToString();
                if (marketName == null) return;

                if ((bool)dgvMarkets.Rows[e.RowIndex].Cells["CheckedCol"].Value)
                    _checkedMarketNames.Add(marketName);
                else
                    _checkedMarketNames.Remove(marketName);
            }
        }

        private List<string> GetCheckedMarkets()
        {
            return _checkedMarketNames.ToList();
        }

        private void EnsureSimulatorSetup()
        {
            if (_simSetup) return;
            _simulator.Setup();
            _simSetup = true;
        }

        private void FormsPlot1_MouseMove(object sender, MouseEventArgs e)
        {
            // Right-drag panning - only if both flag is set AND button is actually pressed
            if (_isRightPanning && e.Button == MouseButtons.Right && Control.MouseButtons == MouseButtons.Right)
            {
                // compute data-space deltas from pixel movement
                double xNow = formsPlot1.Plot.GetCoordinateX(e.X);
                double xStart = formsPlot1.Plot.GetCoordinateX(_panStartPx.X);
                double yNow = formsPlot1.Plot.GetCoordinateY(e.Y);
                double yStart = formsPlot1.Plot.GetCoordinateY(_panStartPx.Y);

                double dx = xNow - xStart;
                double dy = yNow - yStart;

                formsPlot1.Plot.SetAxisLimits(
                    _panStartLimits.xMin - dx,
                    _panStartLimits.xMax - dx,
                    _panStartLimits.yMin - dy,
                    _panStartLimits.yMax - dy);

                formsPlot1.Refresh();
                return; // skip tooltip/hover while panning
            }
            else if (_isRightPanning && e.Button != MouseButtons.Right)
            {
                // If we were panning but right button is no longer pressed, reset state
                ResetPanningState();
            }

            // if the right button is no longer held, end pan
            if (_isRightPanning && e.Button != MouseButtons.Right)
            {
                _isRightPanning = false;
                formsPlot1.Cursor = Cursors.Default;
                _panStartPx = Point.Empty;
            }

            // ----- existing tooltip/hover logic (unchanged) -----
            if (_tooltipPoints == null || _tooltipPoints.Count == 0)
                return;

            int mxPx = e.X;
            int bestIdx = 0;
            int bestDxPx = int.MaxValue;

            for (int i = 0; i < _tooltipPoints.Count; i++)
            {
                int px = (int)Math.Round(formsPlot1.Plot.GetPixelX(_tooltipPoints[i].x));
                int dxpx = Math.Abs(px - mxPx);
                if (dxpx < bestDxPx) { bestDxPx = dxpx; bestIdx = i; }
            }

            string memo = _tooltipPoints[bestIdx].memo;
            if (!string.Equals(memo, _lastTooltipMemo))
            {
                _tooltipOverlay.Text = memo;
                _tooltipOverlay.Visible = true;
                _tooltipOverlay.BringToFront();
                _lastTooltipMemo = memo;
            }

            if (_hoverLine != null)
            {
                _hoverLine.X = _tooltipPoints[bestIdx].x; // OADate X
                _hoverLine.IsVisible = true;
            }

            formsPlot1.Render();
        }



        private void FormsPlot1_MouseLeave(object sender, EventArgs e)
        {
            // Reset panning state when mouse leaves the chart
            ResetPanningState();

            _tooltipOverlay.Visible = false;
            _lastTooltipMemo = null;
            if (_hoverLine != null)
                _hoverLine.IsVisible = false;
            formsPlot1.Render();
        }



        private void BtnCheckAll_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgvMarkets.Rows)
            {
                row.Cells["CheckedCol"].Value = true;
                var marketName = row.Cells["Market"].Value?.ToString();
                if (marketName != null)
                    _checkedMarketNames.Add(marketName);
            }
        }

        private void BtnUncheckAll_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgvMarkets.Rows)
            {
                row.Cells["CheckedCol"].Value = false;
                var marketName = row.Cells["Market"].Value?.ToString();
                if (marketName != null)
                    _checkedMarketNames.Remove(marketName);
            }
        }


        private void AppendLog(string msg)
        {
            if (rtbLog.InvokeRequired)
            {
                rtbLog.BeginInvoke(new Action(() =>
                {
                    rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
                    rtbLog.ScrollToCaret();
                }));
                return;
            }

            rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
            rtbLog.ScrollToCaret();
        }

        private void UpdatePnL(string market, double pnl)
        {
            if (dgvMarkets.InvokeRequired)
            {
                dgvMarkets.BeginInvoke(new Action(() => UpdatePnL(market, pnl)));
                return;
            }

            // only show if this beats the session-best for this market
            if (!_bestPnL.TryGetValue(market, out var best) || (pnl > best && pnl != 0))
            {
                _bestPnL[market] = pnl;

                foreach (DataGridViewRow row in dgvMarkets.Rows)
                {
                    if (row.Cells["Market"].Value?.ToString() == market)
                    {
                        row.Cells["PnL"].Value = pnl.ToString("F2");
                        break;
                    }
                }
            }
        }



        private async void DgvMarkets_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvMarkets.SelectedRows.Count == 0) return;
            string market = dgvMarkets.SelectedRows[0].Cells["Market"].Value?.ToString();
            if (!string.IsNullOrWhiteSpace(market))
                await LoadChart(market);
        }
        private async Task LoadChart(string market)
        {
            _lastTooltipMemo = null;

            _tooltipPoints = SimulatorWinForms.Charting.MarketChartRenderer.Render(
                formsPlot1,
                _cacheDir,
                market,
                AppendLog);

            _hoverLine = formsPlot1.Plot.AddVerticalLine(0, Color.Black, 1);
            _hoverLine.IsVisible = false;
            formsPlot1.Refresh();  // Ensure the plot is refreshed after adding the line

            _snapshots = await _simulator.ReturnSnapshotsForMarket(market);
            AppendLog($"Loaded snapshots for {market}");
        }


        private async void btnRun_Click(object sender, EventArgs e)
        {
            var sel = GetCheckedMarkets();

            // reset UI PnL for selected markets before the run
            ResetPnLForMarkets(sel);

            EnsureSimulatorSetup();
            try
            {
                await _simulator.RunMultipleAllStrategiesForGuiAsync(writeToFile: false, marketsToRun: sel);
            }
            finally
            {
                _simulator.TearDown();
                _simSetup = false;
            }
        }

        private void btnReload_Click(object sender, EventArgs e)
        {
            LoadCache();
        }
        private async void btnRunSet_Click(object sender, EventArgs e)
        {
            var sel = GetCheckedMarkets();

            // reset UI PnL for selected markets before the run
            ResetPnLForMarkets(sel);

            EnsureSimulatorSetup();
            try
            {
                await _simulator.RunSelectedSetForGuiAsync(
                    setKey: "TryAgain",
                    weightName: "TryAgain_S091",
                    writeToFile: true,
                    marketsToRun: sel);
            }
            finally
            {
                _simulator.TearDown();
                _simSetup = false;
            }
        }

        /// <summary>
        /// Handles mouse down events on the ScottPlot chart. When the user clicks
        /// anywhere on the plot area, the view is replaced with a dashboard control
        /// that emulates the web-based layout on the full window. The click position's x-coordinate
        /// in OADate form is passed to the dashboard for potential use (not used
        /// currently but reserved for future enhancements).
        /// </summary>
        private void FormsPlot1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                double xVal = formsPlot1.Plot.GetCoordinateX(e.X);
                ShowDashboardAt(xVal);
                return;
            }

            if (e.Button == MouseButtons.Right)
            {
                // prevent ScottPlot’s default right-drag zoom from fighting our pan (if available)
                try { formsPlot1.Configuration.RightClickDragZoom = false; } catch { /* ignore if not supported */ }

                var lim = formsPlot1.Plot.GetAxisLimits();
                _panStartLimits = (lim.XMin, lim.XMax, lim.YMin, lim.YMax);
                _panStartPx = e.Location;
                _isRightPanning = true;
                formsPlot1.Cursor = Cursors.SizeAll;
            }
        }


        /// <summary>
        /// Replaces the entire form content with a new SnapshotViewer.
        /// If one already exists, it is reused. This allows users to view a richer
        /// set of data and controls similar to the provided HTML dashboard when they
        /// click on the chart, filling the whole window.
        /// </summary>
        /// <param name="xOADate">The x-coordinate of the click in OADate time.</param>
        private void ShowDashboardAt(double xOADate)
        {
            _savedChartLimits = (formsPlot1.Plot.GetAxisLimits().XMin, formsPlot1.Plot.GetAxisLimits().XMax, formsPlot1.Plot.GetAxisLimits().YMin, formsPlot1.Plot.GetAxisLimits().YMax);

            if (_snapshotViewer == null)
            {
                _snapshotViewer = new SnapshotViewer();
            }

            _snapshotViewer.CacheDir = _cacheDir;  // Pass the cache directory

            // Find the closest snapshot to the clicked xOADate
            DateTime clickedTime = DateTime.FromOADate(xOADate);
            MarketSnapshot? closest = _snapshots.OrderBy(s => Math.Abs((s.Timestamp - clickedTime).TotalSeconds)).FirstOrDefault();
            if (closest == null) return;

            List<MarketSnapshot> history = _snapshots;  // Already ordered by timestamp

            // Precompute memos per snapshot for alignment during navigation
            var memosPerSnapshot = new List<string>(history.Count);
            double tolerance = 1e-6;  // Adjust if needed; ~0.1 ms for OA dates

            foreach (var snap in history)
            {
                double snapOADate = snap.Timestamp.ToOADate();
                var matchingMemos = _tooltipPoints
                    .Where(p => Math.Abs(p.x - snapOADate) < tolerance)
                    .Select(p => p.memo.Trim())
                    .Distinct()
                    .ToList();

                string joined = matchingMemos.Any() ? string.Join("\n", matchingMemos) : "No events at this time.";
                memosPerSnapshot.Add(joined);
            }

            // Load position, average cost, and resting orders data from cache
            int simulatedPosition = 0;
            double averageCost = 0.0;
            int simulatedRestingOrders = 0;
            List<PricePoint> positionPoints = new List<PricePoint>();
            List<PricePoint> averageCostPoints = new List<PricePoint>();
            List<PricePoint> restingOrdersPoints = new List<PricePoint>();
            List<PricePoint> patternPoints = new List<PricePoint>();
            string marketName = closest.MarketTicker ?? "Unknown";
            try
            {
                // Look for files with suffixes and use the most recent one
                var pattern = $"{marketName}_*.json";
                var matchingFiles = Directory.GetFiles(_cacheDir, pattern)
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .ToList();

                if (matchingFiles.Any())
                {
                    var json = File.ReadAllText(matchingFiles.First());
                    var cd = JsonSerializer.Deserialize<CachedMarketData>(json);
                    if (cd != null)
                    {
                        simulatedPosition = cd.SimulatedPosition;
                        averageCost = cd.AverageCost;
                        positionPoints = cd.PositionPoints ?? new List<PricePoint>();
                        averageCostPoints = cd.AverageCostPoints ?? new List<PricePoint>();
                        restingOrdersPoints = cd.RestingOrdersPoints ?? new List<PricePoint>();
                        patternPoints = cd.PatternPoints ?? new List<PricePoint>();
                        patternPoints = cd.PatternPoints ?? new List<PricePoint>();

                        // Calculate simulated resting orders count for current snapshot
                        if (restingOrdersPoints != null && restingOrdersPoints.Count > 0)
                        {
                            var matchingPoint = restingOrdersPoints.FirstOrDefault(p =>
                                Math.Abs((p.Date - closest.Timestamp).TotalSeconds) < 1);
                            if (matchingPoint != null)
                            {
                                simulatedRestingOrders = (int)matchingPoint.Price;
                            }
                        }
                    }
                }
                else
                {
                    // Fallback: try the canonical filename
                    var canonical = Path.Combine(_cacheDir, $"{marketName}.json");
                    if (File.Exists(canonical))
                    {
                        var json = File.ReadAllText(canonical);
                        var cd = JsonSerializer.Deserialize<CachedMarketData>(json);
                        if (cd != null)
                        {
                            simulatedPosition = cd.SimulatedPosition;
                            averageCost = cd.AverageCost;
                            positionPoints = cd.PositionPoints ?? new List<PricePoint>();
                            averageCostPoints = cd.AverageCostPoints ?? new List<PricePoint>();
                            restingOrdersPoints = cd.RestingOrdersPoints ?? new List<PricePoint>();

                            // Calculate simulated resting orders count for current snapshot
                            if (restingOrdersPoints != null && restingOrdersPoints.Count > 0)
                            {
                                var matchingPoint = restingOrdersPoints.FirstOrDefault(p =>
                                    Math.Abs((p.Date - closest.Timestamp).TotalSeconds) < 1);
                                if (matchingPoint != null)
                                {
                                    simulatedRestingOrders = (int)matchingPoint.Price;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Failed to load position data for {marketName}: {ex.Message}");
            }

            _snapshotViewer.Populate(closest, history, memosPerSnapshot, simulatedPosition, averageCost, simulatedRestingOrders, positionPoints, averageCostPoints, restingOrdersPoints, patternPoints);

            // Set back action to switch back to main layout
            _snapshotViewer.BackAction = HideDashboard;

            // DISABLE MainForm chart control to prevent interference with SnapshotViewer
            formsPlot1.Enabled = false;
            formsPlot1.Visible = false;

            // Replace entire form content with dashboard
            this.SuspendLayout();
            this.Controls.Remove(layout);
            _snapshotViewer.Dock = DockStyle.Fill;
            this.Controls.Add(_snapshotViewer);
            this.ResumeLayout();

            _snapshotViewer.Focus();  // Ensure the UserControl has focus for key events

            AppendLog("Switched to full dashboard view.");
        }

        /// <summary>
        /// Switches the form back to the main layout view and restores the previous axis limits.
        /// </summary>
        private void HideDashboard()
        {
            this.SuspendLayout();
            this.Controls.Remove(_snapshotViewer);
            layout.Dock = DockStyle.Fill;
            this.Controls.Add(layout);
            this.ResumeLayout();

            // RE-ENABLE MainForm chart control now that we're back to main view
            formsPlot1.Enabled = true;
            formsPlot1.Visible = true;

            formsPlot1.Plot.SetAxisLimits(_savedChartLimits.xMin, _savedChartLimits.xMax, _savedChartLimits.yMin, _savedChartLimits.yMax);
            formsPlot1.Refresh();

            AppendLog("Switched back to chart view.");
        }

        private void MainForm_ResizeEnd(object sender, EventArgs e)
        {
            // Calculate scale factor based on width (clamp between min and max)
            float scaleFactor = Math.Clamp((float)this.Width / OriginalWidth, MinScale, MaxScale);

            // Scale fonts for DataGridView (including headers)
            dgvMarkets.Font = new Font(dgvMarkets.Font.FontFamily, 8.25f * scaleFactor);  // Default WinForms font size is ~8.25
            dgvMarkets.ColumnHeadersDefaultCellStyle.Font = new Font(dgvMarkets.Font.FontFamily, 8.25f * scaleFactor, FontStyle.Bold);
            dgvMarkets.AutoResizeColumns();  // Ensure columns adjust

            // Scale RichTextBox font
            rtbLog.Font = new Font("Consolas", 9f * scaleFactor);

            // Scale button fonts
            foreach (Control control in buttonPanel.Controls)
            {
                if (control is Button button)
                {
                    button.Font = new Font(button.Font.FontFamily, 8.25f * scaleFactor);
                }
            }

            // Scale ScottPlot fonts (adjust labels, ticks, etc.)
            var plot = formsPlot1.Plot;
            plot.XAxis.LabelStyle(fontSize: 12f * scaleFactor);
            plot.YAxis.LabelStyle(fontSize: 12f * scaleFactor);
            plot.XAxis.TickLabelStyle(fontSize: 10f * scaleFactor);
            plot.YAxis.TickLabelStyle(fontSize: 10f * scaleFactor);

            // Refresh the plot to apply changes
            formsPlot1.Refresh();

            // Refresh tooltip overlay font if needed
            _tooltipOverlay.Font = new Font(_tooltipOverlay.Font.FontFamily, 9f * scaleFactor);
        }

        private void ResetPanningState()
        {
            _isRightPanning = false;
            formsPlot1.Cursor = Cursors.Default;
            _panStartPx = Point.Empty;
        }

        private void MainForm_Activated(object sender, EventArgs e)
        {
            // Reset panning state when the form becomes active again
            // This handles the case where user returns from snapshot viewer while still holding mouse button
            ResetPanningState();

            // Additional safeguard: use a timer to reset panning state after a short delay
            // This handles edge cases where the mouse button state might not be properly detected
            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 100; // 100ms delay
            timer.Tick += (s, args) =>
            {
                ResetPanningState();
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }

        private async void btnRunML_Click(object sender, EventArgs e)
        {
            var sel = GetCheckedMarkets();

            // Reset UI PnL for selected markets before the run
            ResetPnLForMarkets(sel);
            EnsureSimulatorSetup();
            try
            {
                await _simulator.RunMLTrainingAndSimulationForGuiAsync(
                    writeToFile: true,
                    marketsToRun: sel);
            }
            finally
            {
                _simulator.TearDown();
                _simSetup = false;
            }
        }
    }
}