// MainForm.cs
using BacklashBotData.Data;
using BacklashDTOs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScottPlot.Plottable;
using System.Text.Json;
using System.Text.RegularExpressions;
using TradingSimulator;
using TradingStrategies.Strategies.Strategies.Strats;
using TradingStrategies.Strategies.Strats;
using TradingStrategies.Trading.Helpers;

namespace TradingGUI
{
    /// <summary>
    /// Main form for the Backlash Trading Bot GUI application.
    /// Provides a comprehensive interface for running trading strategy simulations,
    /// visualizing market data through interactive charts, and managing market selections.
    /// Integrates with the TradingSimulatorService for backtesting and the BacklashBotContext
    /// for database operations. Supports real-time chart interaction including panning,
    /// zooming, and tooltips, as well as switching to detailed snapshot views.
    /// </summary>
    public partial class MainForm : Form
    {
        private TradingSimulatorService _simulator;
        private BacklashBotContext _context;
        private IServiceProvider _serviceProvider;
        private ILogger<MainForm> _logger;
        private readonly string _cacheDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "TestingOutput");
        private readonly string _configFilePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        private List<(double x, double y, string memo)> _tooltipPoints = new();
        private string? _lastTooltipMemo = null;
        private HashSet<string> _checkedMarketNames = new();
        private bool _simSetup;
        private Label _tooltipOverlay;
        private VLine _hoverLine;
        private bool _isRightPanning;
        private Point _panStartPx;
        private (double xMin, double xMax, double yMin, double yMax) _panStartLimits;

        private readonly Dictionary<string, double> _bestPnL = new(StringComparer.OrdinalIgnoreCase);

        List<MarketSnapshot> _snapshots = new();

        /// <summary>
        /// Gets whether pattern image generation is currently enabled.
        /// </summary>
        public bool EnablePatternImageGeneration => _enablePatternImagesCheckBox?.Checked ?? true;

        // Strategy selection controls
        private ComboBox? _strategyTypeComboBox;
        private ComboBox? _weightSetComboBox;
        private Button? _refreshWeightSetsButton;

        // Pattern image generation control
        private CheckBox? _enablePatternImagesCheckBox;

        private (double xMin, double xMax, double yMin, double yMax) _savedChartLimits;

        private SnapshotViewer? _snapshotViewer;

        // Progress indicator for long-running operations
        private ProgressBar? _progressBar;

        // Original dimensions for scaling reference
        private const int OriginalWidth = 1100;
        private const float MinScale = 0.8f;  // Minimum font scale (80% of original)
        private const float MaxScale = 2.0f;  // Maximum font scale (200% of original)

        /// <summary>
        /// Initializes a new instance of the MainForm class.
        /// Sets up the trading simulator service, database context, chart controls,
        /// event handlers for user interactions, and typography scaling system.
        /// Configures ScottPlot charts to disable built-in pan/zoom for custom handling.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            toolTip1.ShowAlways = true;
            // DISABLE ScottPlot's built-in pan/zoom to prevent conflicts
            formsPlot1.Configuration.Pan = false;
            formsPlot1.Configuration.Zoom = false;
            formsPlot1.Configuration.ScrollWheelZoom = false;

            formsPlot1.MouseMove += HandleChartMouseMove;
            formsPlot1.MouseLeave += HandleChartMouseLeave;

            Load += async (_, __) => await LoadCache();
            dgvMarkets.SelectionChanged += HandleMarketSelectionChanged;

            dgvMarkets.CellValueChanged += HandleMarketCheckStateChanged;
            dgvMarkets.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dgvMarkets.IsCurrentCellDirty)
                    dgvMarkets.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            dgvMarkets.Sorted += (s, e) => RestoreCheckboxes();
            dgvMarkets.CellFormatting += FormatMarketGridCell;

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
            _hoverLine = formsPlot1.Plot.AddVerticalLine(x: 0, color: Color.Black, width: 1);
            _hoverLine.IsVisible = false;

            // wire a MouseDown handler to allow swapping the chart with the dashboard on click
            formsPlot1.MouseDown += HandleChartMouseDown;

            // Add resize handler for dynamic scaling
            this.ResizeEnd += HandleFormResize;

            // Add activation handler to reset panning state
            this.Activated += HandleFormActivated;

            // Initialize typography system
            ApplyInitialTypography();
        }

        /// <summary>
        /// Initializes a new instance of the MainForm class with dependency injection support.
        /// </summary>
        /// <param name="simulator">The trading simulator service.</param>
        /// <param name="context">The database context.</param>
        /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
        public MainForm(TradingSimulatorService simulator, BacklashBotContext context, IServiceProvider serviceProvider) : this()
        {
            _simulator = simulator;
            _context = context;
            _serviceProvider = serviceProvider;
            _logger = _serviceProvider.GetRequiredService<ILogger<MainForm>>();
            _simulator.EnsureInitialized();

            // Set up event handlers for the simulator
            _simulator.OnTestProgress += msg => AppendLog(msg);
            _simulator.OnProfitLossUpdate += (m, pnl) => UpdatePnL(m, pnl);
            _simulator.OnMarketProcessed += m => AppendLog($"Processed market: {m}");
        }

        /// <summary>
        /// Raises the Load event and initializes strategy controls after the form is fully loaded.
        /// </summary>
        /// <param name="e">An EventArgs that contains the event data.</param>
        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Initialize strategy selection controls after form is fully loaded
            await InitializeStrategyControls();
        }

        /// <summary>
        /// Resets the PnL (Profit and Loss) values for the specified markets in the UI grid.
        /// Clears both the internal tracking dictionary and the visual display.
        /// </summary>
        /// <param name="markets">Collection of market names to reset PnL for.</param>
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

        /// <summary>
        /// Loads market data from the database and cache files, populating the market grid.
        /// Retrieves market information from the database, enriches it with cached PnL data,
        /// and displays it in the DataGridView with appropriate formatting and checkboxes.
        /// Supports filtering by specific market bases for targeted loading.
        /// </summary>
        /// <param name="includeBases">Optional list of market base names to include. If null, loads all available markets.</param>
        private async Task LoadCache(List<string>? includeBases = null)
        {
            dgvMarkets.Columns.Clear();
            dgvMarkets.Rows.Clear();

            dgvMarkets.RowHeadersVisible = false;
            dgvMarkets.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            var checkCol = new DataGridViewCheckBoxColumn { Name = "CheckedCol", HeaderText = "?", AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells };
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
                                var json = await File.ReadAllTextAsync(canonical);
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
                                    var pj = await File.ReadAllTextAsync(p);
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
                var markets = await _context.GetMarketsFiltered(includedMarkets: bases.ToHashSet());

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

        /// <summary>
        /// Restores the checkbox states in the market grid based on the internal tracking set.
        /// Ensures that the UI reflects the current selection state after operations like sorting.
        /// </summary>
        private void RestoreCheckboxes()
        {
            foreach (DataGridViewRow row in dgvMarkets.Rows)
            {
                var marketName = row.Cells["Market"].Value?.ToString();
                if (marketName != null)
                    row.Cells["CheckedCol"].Value = _checkedMarketNames.Contains(marketName);
            }
        }

        /// <summary>
        /// Handles changes to market selection checkboxes in the grid.
        /// Updates the internal tracking set when users check or uncheck market rows.
        /// </summary>
        /// <param name="sender">The DataGridView that triggered the event.</param>
        /// <param name="e">Event arguments containing row and column information.</param>
        private void HandleMarketCheckStateChanged(object? sender, DataGridViewCellEventArgs e)
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

        private void FormatMarketGridCell(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == dgvMarkets.Columns["PnL"].Index && e.Value != null && e.CellStyle != null)
            {
                string? pnlStr = e.Value.ToString();
                if (!string.IsNullOrWhiteSpace(pnlStr) && double.TryParse(pnlStr, out double pnl))
                {
                    if (pnl > 0)
                    {
                        e.CellStyle.ForeColor = Color.Green;
                        e.CellStyle.Font = new Font(e.CellStyle.Font, FontStyle.Bold);
                    }
                    else if (pnl < 0)
                    {
                        e.CellStyle.ForeColor = Color.Red;
                        e.CellStyle.Font = new Font(e.CellStyle.Font, FontStyle.Bold);
                    }
                }
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

        private void HandleChartMouseMove(object? sender, MouseEventArgs e)
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



        private void HandleChartMouseLeave(object? sender, EventArgs e)
        {
            // Reset panning state when mouse leaves the chart
            ResetPanningState();

            _tooltipOverlay.Visible = false;
            _lastTooltipMemo = null;
            if (_hoverLine != null)
                _hoverLine.IsVisible = false;
            formsPlot1.Render();


        }

        private void HandleCheckAllMarkets(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgvMarkets.Rows)
            {
                row.Cells["CheckedCol"].Value = true;
                var marketName = row.Cells["Market"].Value?.ToString();
                if (marketName != null)
                    _checkedMarketNames.Add(marketName);
            }
        }

        private void HandleUncheckAllMarkets(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgvMarkets.Rows)
            {
                row.Cells["CheckedCol"].Value = false;
                var marketName = row.Cells["Market"].Value?.ToString();
                if (marketName != null)
                    _checkedMarketNames.Remove(marketName);
            }
        }


        private void AppendLog(string msg, LogLevel level = LogLevel.Information)
        {
            // Log to the database logger
            _logger.Log(level, msg);

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



        private async void HandleMarketSelectionChanged(object? sender, EventArgs e)
        {
            if (dgvMarkets.SelectedRows.Count == 0) return;
            string? market = dgvMarkets.SelectedRows[0].Cells["Market"].Value?.ToString();
            if (!string.IsNullOrWhiteSpace(market))
                await LoadChart(market);
        }
        /// <summary>
        /// Loads and renders a chart for the specified market using cached simulation data.
        /// Uses MarketChartRenderer to create interactive visualizations with tooltips,
        /// hover lines, and trading signals. Updates the snapshot data for the market
        /// to support dashboard navigation.
        /// </summary>
        /// <param name="market">The market ticker symbol to load chart data for.</param>
        private async Task LoadChart(string market)
        {
            _lastTooltipMemo = null;

            _tooltipPoints = await TradingGUI.Charting.MarketChartRenderer.Render(
                formsPlot1,
                _cacheDir,
                market,
                msg => AppendLog(msg));

            _hoverLine = formsPlot1.Plot.AddVerticalLine(0, Color.Black, 1);
            _hoverLine.IsVisible = false;
            formsPlot1.Refresh();  // Ensure the plot is refreshed after adding the line

            _snapshots = await _simulator.GetSnapshotsForMarket(market);
            AppendLog($"Loaded snapshots for {market}");
        }


        private async void HandleRunSimulation(object sender, EventArgs e)
        {
            var sel = GetCheckedMarkets();

            // reset UI PnL for selected markets before the run
            ResetPnLForMarkets(sel);

            EnsureSimulatorSetup();

            // Show and initialize progress bar
            if (_progressBar != null)
            {
                _progressBar.Visible = true;
                _progressBar.Value = 0;
            }

            try
            {
                string? selectedStrategy = _strategyTypeComboBox?.SelectedItem?.ToString();

                if (string.IsNullOrEmpty(selectedStrategy))
                {
                    AppendLog("Please select a strategy type.");
                    return;
                }

                AppendLog($"Running ALL weight sets for {selectedStrategy} strategy");

                // Get all weight sets for the selected strategy
                var allWeightSets = GetWeightNamesForStrategy(selectedStrategy).ToList();

                if (!allWeightSets.Any())
                {
                    AppendLog($"No weight sets found for {selectedStrategy}");
                    return;
                }

                AppendLog($"Found {allWeightSets.Count} weight sets to run: {string.Join(", ", allWeightSets)}");

                // Set up progress tracking: total combinations of markets and weight sets
                int totalSteps = sel.Count * allWeightSets.Count;
                int currentStep = 0;

                // Run all weight sets for each selected market in sequence
                foreach (var market in sel)
                {
                    AppendLog($"Processing market {market} with all weight sets");

                    await _simulator.RunAllSetsForSingleMarketAsync(
                        setKey: selectedStrategy,
                        weightNames: allWeightSets,
                        market: market,
                        writeToFile: false); // Don't save files when running all weight sets

                    AppendLog($"Completed all weight sets for market {market}");

                    // Update progress: increment by number of weight sets processed for this market
                    currentStep += allWeightSets.Count;
                    if (_progressBar != null)
                    {
                        _progressBar.Value = (int)((double)currentStep / totalSteps * 100);
                    }
                }

                AppendLog($"Completed running all {allWeightSets.Count} weight sets for {selectedStrategy} on {sel.Count} markets");
            }
            finally
            {
                _simulator.TearDown();
                _simSetup = false;

                // Hide progress bar
                if (_progressBar != null)
                {
                    _progressBar.Visible = false;
                }
            }
        }


        private async void HandleReloadMarkets(object sender, EventArgs e)
        {
            await LoadCache();
        }
        private async void HandleRunSpecificSet(object sender, EventArgs e)
        {
            var sel = GetCheckedMarkets();

            // reset UI PnL for selected markets before the run
            ResetPnLForMarkets(sel);

            EnsureSimulatorSetup();

            // Show and initialize progress bar
            if (_progressBar != null)
            {
                _progressBar.Visible = true;
                _progressBar.Value = 0;
            }

            try
            {
                AppendLog("Starting specific set simulation...");

                await _simulator.RunSelectedSetForGuiAsync(
                    setKey: "TryAgain",
                    weightName: "TryAgain_S091",
                    writeToFile: true,
                    marketsToRun: sel);

                AppendLog("Specific set simulation completed.");

                // Update progress to complete
                if (_progressBar != null)
                {
                    _progressBar.Value = 100;
                }
            }
            finally
            {
                _simulator.TearDown();
                _simSetup = false;

                // Hide progress bar after a short delay
                if (_progressBar != null)
                {
                    await Task.Delay(500); // Brief delay to show completion
                    _progressBar.Visible = false;
                }
            }
        }

        /// <summary>
        /// Handles mouse down events on the ScottPlot chart. When the user clicks
        /// anywhere on the plot area, the view is replaced with a dashboard control
        /// that emulates the web-based layout on the full window. The click position's x-coordinate
        /// in OADate form is passed to the dashboard for potential use (not used
        /// currently but reserved for future enhancements).
        /// </summary>
        private async void HandleChartMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                double xVal = formsPlot1.Plot.GetCoordinateX(e.X);
                await ShowDashboardAt(xVal);
                return;
            }

            if (e.Button == MouseButtons.Right)
            {
                // prevent ScottPlot s default right-drag zoom from fighting our pan (if available)
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
        /// <summary>
        /// Switches the main form view to a detailed SnapshotViewer dashboard.
        /// Creates or reuses a SnapshotViewer control, populates it with market data
        /// and snapshots corresponding to the clicked chart position, then replaces
        /// the main form content with the dashboard for detailed analysis.
        /// </summary>
        /// <param name="xOADate">The x-coordinate in OADate format where the user clicked on the chart.</param>
        private async Task ShowDashboardAt(double xOADate)
        {
            _savedChartLimits = (formsPlot1.Plot.GetAxisLimits().XMin, formsPlot1.Plot.GetAxisLimits().XMax, formsPlot1.Plot.GetAxisLimits().YMin, formsPlot1.Plot.GetAxisLimits().YMax);

            if (_snapshotViewer == null)
            {
                _snapshotViewer = _serviceProvider.GetRequiredService<SnapshotViewer>();
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
                    var json = await File.ReadAllTextAsync(matchingFiles.First());
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
                        var json = await File.ReadAllTextAsync(canonical);
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
        /// <summary>
        /// Switches back from the SnapshotViewer dashboard to the main chart view.
        /// Restores the main form layout, re-enables chart controls, and restores
        /// the previously saved chart axis limits to maintain user context.
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

        private void HandleFormResize(object? sender, EventArgs e)
        {
            // Calculate scale factor based on width and DPI (more conservative scaling)
            float widthScale = Math.Clamp((float)this.Width / OriginalWidth, MinScale, MaxScale);
            float dpiScale = TypographyManager.Instance.GetTypographyScale();

            // Use more conservative scaling to prevent layout issues
            float scaleFactor = Math.Min(widthScale, dpiScale);

            // Only apply typography scaling if it's significantly different from 1.0
            if (Math.Abs(scaleFactor - 1.0f) > 0.1f)
            {
                TypographyManager.Instance.ApplyTypography(this, scaleFactor);

                // Special handling for ScottPlot (typography manager skips it)
                var plot = formsPlot1.Plot;
                plot.XAxis.LabelStyle(fontSize: 12f * scaleFactor);
                plot.YAxis.LabelStyle(fontSize: 12f * scaleFactor);
                plot.XAxis.TickLabelStyle(fontSize: 10f * scaleFactor);
                plot.YAxis.TickLabelStyle(fontSize: 10f * scaleFactor);

                // Refresh the plot to apply changes
                formsPlot1.Refresh();

                // Ensure tooltip overlay uses proper typography
                _tooltipOverlay.Font = TypographyManager.Instance.GetScaledFont(FontSize.Medium, scaleFactor);
            }
        }

        private void ResetPanningState()
        {
            _isRightPanning = false;
            formsPlot1.Cursor = Cursors.Default;
            _panStartPx = Point.Empty;
        }

        private void HandleFormActivated(object? sender, EventArgs e)
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

        private void ApplyInitialTypography()
        {
            // Apply typography on initial load with conservative DPI scaling
            float dpiScale = TypographyManager.Instance.GetTypographyScale();

            // Use more conservative scaling for initial load to prevent layout issues
            float scaleFactor = Math.Min(dpiScale, 1.2f);

            if (Math.Abs(scaleFactor - 1.0f) > 0.1f)
            {
                TypographyManager.Instance.ApplyTypography(this, scaleFactor);
            }
        }

        private async Task InitializeStrategyControls()
        {
            // Create progress bar for long-running operations
            _progressBar = new ProgressBar
            {
                Size = new Size(200, 20),
                Visible = false,
                Margin = new Padding(3),
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };

            // Create strategy type ComboBox
            _strategyTypeComboBox = new ComboBox
            {
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(3)
            };

            // Add actual strategy names from centralized configuration
            var strategyNames = TradingStrategies.Trading.Helpers.StrategyConfiguration.GetStrategyNames().ToArray();
            _strategyTypeComboBox.Items.AddRange(strategyNames);
            _strategyTypeComboBox.SelectedIndexChanged += StrategyTypeComboBox_SelectedIndexChanged;

            // Create weight set ComboBox
            _weightSetComboBox = new ComboBox
            {
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = true, // Enabled by default since all strategies use parameter sets
                Margin = new Padding(3)
            };
            _weightSetComboBox.SelectedIndexChanged += WeightSetComboBox_SelectedIndexChanged;

            // Create refresh button for weight sets
            _refreshWeightSetsButton = new Button
            {
                Text = "↻",
                Size = new Size(30, 25),
                Margin = new Padding(3),
                Enabled = true // Enabled by default since all strategies use parameter sets
            };
            _refreshWeightSetsButton.Click += RefreshWeightSetsButton_Click;

            // Create pattern image generation checkbox
            _enablePatternImagesCheckBox = new CheckBox
            {
                Text = "Generate Pattern Images",
                Checked = true,
                AutoSize = true,
                Margin = new Padding(3)
            };
            _enablePatternImagesCheckBox.CheckedChanged += EnablePatternImagesCheckBox_CheckedChanged;

            // Add controls to buttonPanel
            // buttonPanel is declared in the designer file and should be accessible
            buttonPanel.Controls.Add(_progressBar);
            buttonPanel.Controls.Add(_strategyTypeComboBox);
            buttonPanel.Controls.Add(_weightSetComboBox);
            buttonPanel.Controls.Add(_refreshWeightSetsButton);
            buttonPanel.Controls.Add(_enablePatternImagesCheckBox);

            // Move them to the front of the flow (before the buttons)
            buttonPanel.Controls.SetChildIndex(_strategyTypeComboBox, 0);
            buttonPanel.Controls.SetChildIndex(_weightSetComboBox, 1);
            buttonPanel.Controls.SetChildIndex(_refreshWeightSetsButton, 2);
            buttonPanel.Controls.SetChildIndex(_enablePatternImagesCheckBox, 3);

            // Load previous selections from config
            LoadConfig();

            // Populate weight sets asynchronously
            await PopulateWeightSetsAsync();
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var json = File.ReadAllText(_configFilePath);
                    var appSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                    if (appSettings != null && appSettings.TryGetValue("TradingGUI", out var tradingGuiObj))
                    {
                        var tradingGuiJson = JsonSerializer.Serialize(tradingGuiObj);
                        var config = JsonSerializer.Deserialize<TradingGUIConfig>(tradingGuiJson);

                        if (config != null)
                        {
                            // Set strategy selection
                            if (!string.IsNullOrEmpty(config.LastSelectedStrategy) &&
                                _strategyTypeComboBox.Items.Contains(config.LastSelectedStrategy))
                            {
                                _strategyTypeComboBox.SelectedItem = config.LastSelectedStrategy;
                            }
                            else
                            {
                                _strategyTypeComboBox.SelectedIndex = 0; // Default to first strategy
                            }

                            // Set pattern image generation setting
                            if (_enablePatternImagesCheckBox != null)
                            {
                                _enablePatternImagesCheckBox.Checked = config.EnablePatternImageGeneration;
                            }
                        }
                        else
                        {
                            _strategyTypeComboBox.SelectedIndex = 0; // Default to first strategy
                            if (_enablePatternImagesCheckBox != null)
                            {
                                _enablePatternImagesCheckBox.Checked = true; // Default to enabled
                            }
                        }
                    }
                    else
                    {
                        _strategyTypeComboBox.SelectedIndex = 0; // Default to first strategy
                        if (_enablePatternImagesCheckBox != null)
                        {
                            _enablePatternImagesCheckBox.Checked = true; // Default to enabled
                        }
                    }
                }
                else
                {
                    _strategyTypeComboBox.SelectedIndex = 0; // Default to first strategy
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Failed to load config: {ex.Message}");
                _strategyTypeComboBox.SelectedIndex = 0; // Default to first strategy
            }
        }

        private void SaveConfig()
        {
            try
            {
                var config = new TradingGUIConfig
                {
                    LastSelectedStrategy = _strategyTypeComboBox.SelectedItem?.ToString(),
                    LastSelectedWeightSet = _weightSetComboBox.SelectedItem?.ToString(),
                    EnablePatternImageGeneration = _enablePatternImagesCheckBox?.Checked ?? true
                };

                // Read the entire appsettings.json
                var json = File.ReadAllText(_configFilePath);
                var appSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();

                // Update the TradingGUI section
                appSettings["TradingGUI"] = config;

                // Serialize back with indentation
                var updatedJson = JsonSerializer.Serialize(appSettings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configFilePath, updatedJson);
            }
            catch (Exception ex)
            {
                AppendLog($"Failed to save config: {ex.Message}");
            }
        }

        private async void StrategyTypeComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            string? selectedStrategy = _strategyTypeComboBox?.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(selectedStrategy))
            {
                _weightSetComboBox.Enabled = true;
                _refreshWeightSetsButton.Enabled = true;
                await PopulateWeightSetsAsync();
            }
            else
            {
                _weightSetComboBox.Enabled = false;
                _refreshWeightSetsButton.Enabled = false;
                _weightSetComboBox.Items.Clear();
            }

            // Save config when strategy changes
            SaveConfig();
        }

        private void WeightSetComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // Save config when weight set changes
            SaveConfig();
        }

        private async void RefreshWeightSetsButton_Click(object? sender, EventArgs e)
        {
            await PopulateWeightSetsAsync();
        }

        private void EnablePatternImagesCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            // Save config when pattern image generation setting changes
            SaveConfig();
        }

        private IEnumerable<string> GetWeightNamesForStrategy(string strategyName)
        {
            return strategyName switch
            {
                "Bollinger" => StrategySelectionHelper.BollingerParameterSets.Select(x => x.Name),
                "Breakout2" => StrategySelectionHelper.BreakoutParameterSets.Select(x => x.Name),
                "Nothing" => StrategySelectionHelper.NothingEverHappensParameterSets.Select(x => x.Name),
                "FlowMo" => StrategySelectionHelper.FlowMomentumParameterSets.Select(x => x.Name),
                "MLShared" => MLEntrySeekerShared.MLSharedParameterSets.Select(x => x.Name),
                "TryAgain" => TryAgainStrat.TryAgainStratParameterSets.Select(x => x.Name),
                "SloMo" => SlopeMomentumStrat.SlopeMomentumParameterSets.Select(x => x.Name),
                "Momentum" => StrategySelectionHelper.MomentumTradingParameterSets.Select(x => x.Name),
                _ => Enumerable.Empty<string>()
            };
        }

        private async Task PopulateWeightSetsAsync()
        {
            if (_weightSetComboBox == null) return;

            _weightSetComboBox.Items.Clear();
            _weightSetComboBox.Items.Add("Loading...");

            try
            {
                string? selectedStrategy = _strategyTypeComboBox?.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(selectedStrategy))
                {
                    _weightSetComboBox.Items.Clear();
                    _weightSetComboBox.Items.Add("Select a strategy first");
                    return;
                }

                AppendLog($"Loading parameter sets for {selectedStrategy}...");

                // Get weight names from centralized configuration
                var weightNames = TradingStrategies.Trading.Helpers.StrategyConfiguration.GetStrategiesWithParameterSets()
                    .Contains(selectedStrategy) ? GetWeightNamesForStrategy(selectedStrategy) : null;

                _weightSetComboBox.Items.Clear();

                if (weightNames != null && weightNames.Any())
                {
                    foreach (var weightName in weightNames.OrderBy(name => name))
                    {
                        _weightSetComboBox.Items.Add(weightName);
                    }

                    if (_weightSetComboBox.Items.Count > 0)
                    {
                        // Try to restore last selected weight set from config
                        if (File.Exists(_configFilePath))
                        {
                            try
                            {
                                var json = File.ReadAllText(_configFilePath);
                                var appSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                                if (appSettings != null && appSettings.TryGetValue("TradingGUI", out var tradingGuiObj))
                                {
                                    var tradingGuiJson = JsonSerializer.Serialize(tradingGuiObj);
                                    var config = JsonSerializer.Deserialize<TradingGUIConfig>(tradingGuiJson);
                                    if (config != null && !string.IsNullOrEmpty(config.LastSelectedWeightSet) &&
                                        _weightSetComboBox.Items.Contains(config.LastSelectedWeightSet))
                                    {
                                        _weightSetComboBox.SelectedItem = config.LastSelectedWeightSet;
                                    }
                                    else
                                    {
                                        _weightSetComboBox.SelectedIndex = 0;
                                    }
                                }
                                else
                                {
                                    _weightSetComboBox.SelectedIndex = 0;
                                }
                            }
                            catch
                            {
                                _weightSetComboBox.SelectedIndex = 0;
                            }
                        }
                        else
                        {
                            _weightSetComboBox.SelectedIndex = 0;
                        }
                    }

                    AppendLog($"Loaded {weightNames.Count()} parameter sets for {selectedStrategy}.");
                }
                else
                {
                    _weightSetComboBox.Items.Add("No parameter sets found");
                    AppendLog($"No parameter sets found for {selectedStrategy}.");
                }
            }
            catch (Exception ex)
            {
                _weightSetComboBox.Items.Clear();
                _weightSetComboBox.Items.Add("Error loading parameter sets");
                AppendLog($"Failed to load parameter sets: {ex.Message}");
                AppendLog($"Stack trace: {ex.StackTrace}");
            }
        }

        private async void HandleRunMLTraining(object sender, EventArgs e)
        {
            var sel = GetCheckedMarkets();

            // Reset UI PnL for selected markets before the run
            ResetPnLForMarkets(sel);
            EnsureSimulatorSetup();

            // Show and initialize progress bar
            if (_progressBar != null)
            {
                _progressBar.Visible = true;
                _progressBar.Value = 0;
            }

            try
            {
                AppendLog("Starting ML training and simulation...");

                await _simulator.RunMLTrainingAndSimulationForGuiAsync(
                    writeToFile: true,
                    marketsToRun: sel);

                AppendLog("ML training and simulation completed.");

                // Update progress to complete
                if (_progressBar != null)
                {
                    _progressBar.Value = 100;
                }
            }
            finally
            {
                _simulator.TearDown();
                _simSetup = false;

                // Hide progress bar after a short delay
                if (_progressBar != null)
                {
                    await Task.Delay(500); // Brief delay to show completion
                    _progressBar.Visible = false;
                }
            }
        }
    }
}
