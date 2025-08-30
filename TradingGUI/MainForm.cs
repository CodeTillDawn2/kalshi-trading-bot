using System.Text.Json;
using TradingSimulator.Simulator;
using TradingSimulator.TestObjects;

namespace SimulatorWinForms
{
    public partial class MainForm : Form
    {
        private readonly SimulatorTests _simulator;
        private readonly string _cacheDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "TestingOutput");
        private List<(double x, double y, string memo)> _tooltipPoints = new();
        private string _lastTooltipMemo = null;
        private HashSet<string> _checkedMarketNames = new();
        private bool _simSetup;
        private Label _tooltipOverlay;
        private ScottPlot.Plottable.VLine _hoverLine;

        private readonly Dictionary<string, double> _bestPnL = new(StringComparer.OrdinalIgnoreCase);
        public MainForm()
        {
            InitializeComponent();

            toolTip1.ShowAlways = true;

            _simulator = new SimulatorTests();
            _simulator.EnsureInitialized();
            _simulator.OnTestProgress += msg => AppendLog(msg);
            _simulator.OnProfitLossUpdate += (m, pnl) => UpdatePnL(m, pnl);
            _simulator.OnMarketProcessed += m => AppendLog($"✔ Processed {m}");
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

            // thin vertical hover indicator (no pattern property in this ScottPlot version)
            _hoverLine = formsPlot1.Plot.AddVerticalLine(0, Color.Gray, 1);
            _hoverLine.IsVisible = false;
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

        private async Task LoadCache()
        {
            dgvMarkets.Columns.Clear();
            dgvMarkets.Rows.Clear();

            dgvMarkets.RowHeadersVisible = false;
            dgvMarkets.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            var checkCol = new DataGridViewCheckBoxColumn { Name = "CheckedCol", HeaderText = "✓", AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells };
            var marketCol = new DataGridViewTextBoxColumn { Name = "Market", HeaderText = "Market", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 75 };
            var pnlCol = new DataGridViewTextBoxColumn { Name = "PnL", HeaderText = "PnL", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells };
            dgvMarkets.Columns.Add(checkCol);
            dgvMarkets.Columns.Add(marketCol);
            dgvMarkets.Columns.Add(pnlCol);

            dgvMarkets.HandleCreated += (_, __) =>
            {
                if (dgvMarkets.Columns["CheckedCol"] is DataGridViewCheckBoxColumn c) c.Width = 32;
            };

            // Always drive entries from snapshot groups
            try
            {
                _simulator.EnsureInitialized();

                var groupNames = await _simulator.GetSnapshotGroupNames();
                var bases = groupNames
                    .Select(g => System.Text.RegularExpressions.Regex.Replace(g ?? "", @"_(\d+)$", ""))
                    .Where(b => !string.IsNullOrWhiteSpace(b))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(b => b, StringComparer.OrdinalIgnoreCase)
                    .ToList();

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
                                if (cd != null) pnlByBase[b] = cd.PnL;
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
                                    if (d != null) sum += d.PnL;
                                }
                                pnlByBase[b] = sum;
                            }
                        }
                        catch (Exception ex)
                        {
                            AppendLog($"PnL enrich failed for {b}: {ex.Message}");
                        }
                    }
                }
                else
                {
                    AppendLog($"Missing cache directory: {_cacheDir} (entries still loaded from snapshots).");
                }

                foreach (var b in bases)
                {
                    string pnlText = pnlByBase.TryGetValue(b, out var pnl) ? pnl.ToString("F2") : "";
                    dgvMarkets.Rows.Add(_checkedMarketNames.Contains(b), b, pnlText);
                }

                AppendLog($"Loaded {bases.Count} markets from snapshot groups (cache used only for PnL if available).");
                RestoreCheckboxes();
            }
            catch (Exception ex)
            {
                AppendLog($"Snapshot group load failed: {ex.Message}");
            }
        }




        private void EnsureSimulatorSetup()
        {
            if (_simSetup) return;
            _simulator.Setup();
            _simSetup = true;
        }

        private List<string> GetCheckedMarkets()
        {
            return _checkedMarketNames.ToList();
        }

        private void dgvMarkets_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dgvMarkets.Columns[e.ColumnIndex].Name != "CheckedCol")
                return;

            var row = dgvMarkets.Rows[e.RowIndex];
            var isChecked = Convert.ToBoolean(row.Cells["CheckedCol"].Value);
            var marketName = row.Cells["Market"].Value?.ToString();

            if (marketName == null) return;

            if (isChecked)
                _checkedMarketNames.Add(marketName);
            else
                _checkedMarketNames.Remove(marketName);
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


        private void AddPoints(List<PricePoint> pts, string fallbackLabel, Color color, int size, bool collectTooltips, bool connectLine = false)
        {
            if (pts == null || pts.Count == 0) return;

            double[] xs = pts.Select(p => p.Date.ToOADate()).ToArray();
            double[] ys = pts.Select(p => (double)p.Price).ToArray();

            formsPlot1.Plot.AddScatter(xs, ys, color, markerSize: size, lineWidth: connectLine ? 1 : 0);

            if (!collectTooltips) return;

            for (int i = 0; i < pts.Count; i++)
            {
                string memo = string.IsNullOrWhiteSpace(pts[i].Memo) ? fallbackLabel : pts[i].Memo.Trim();
                _tooltipPoints.Add((xs[i], ys[i], memo));
            }
        }

        private void FormsPlot1_MouseMove(object sender, MouseEventArgs e)
        {
            if (_tooltipPoints == null || _tooltipPoints.Count == 0)
                return;

            int mxPx = e.X;
            int bestIdx = 0;
            int bestDxPx = int.MaxValue;

            for (int i = 0; i < _tooltipPoints.Count; i++)
            {
                int px = (int)Math.Round(formsPlot1.Plot.GetPixelX(_tooltipPoints[i].x));
                int dx = Math.Abs(px - mxPx);
                if (dx < bestDxPx)
                {
                    bestDxPx = dx;
                    bestIdx = i;
                }
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
            _tooltipOverlay.Visible = false;
            _lastTooltipMemo = null;
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
            if (!_bestPnL.TryGetValue(market, out var best) || pnl > best)
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



        private void DgvMarkets_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvMarkets.SelectedRows.Count == 0) return;
            string market = dgvMarkets.SelectedRows[0].Cells["Market"].Value?.ToString();
            if (!string.IsNullOrWhiteSpace(market))
                LoadChart(market);
        }

        private void LoadChart(string market)
        {
            formsPlot1.Plot.Clear();
            _tooltipPoints.Clear();

            var baseMarket = System.Text.RegularExpressions.Regex.Replace(market ?? "", @"_(\d+)$", "");
            var canonical = Path.Combine(_cacheDir, $"{baseMarket}.json");

            CachedMarketData merged = null;

            if (File.Exists(canonical))
            {
                var json = File.ReadAllText(canonical);
                merged = JsonSerializer.Deserialize<CachedMarketData>(json);
            }
            else
            {
                var parts = Directory.GetFiles(_cacheDir, $"{baseMarket}_*.json");
                if (parts == null || parts.Length == 0)
                {
                    AppendLog($"Missing files for {baseMarket}");
                    return;
                }

                merged = new CachedMarketData
                {
                    Market = baseMarket,
                    PnL = 0,
                    BidPoints = new List<PricePoint>(),
                    AskPoints = new List<PricePoint>(),
                    BuyPoints = new List<PricePoint>(),
                    SellPoints = new List<PricePoint>(),
                    ExitPoints = new List<PricePoint>(),
                    EventPoints = new List<PricePoint>(),
                    IntendedLongPoints = new List<PricePoint>(),
                    IntendedShortPoints = new List<PricePoint>(),
                    DiscrepancyPoints = new List<PricePoint>()  // NEW
                };

                foreach (var fp in parts)
                {
                    var json = File.ReadAllText(fp);
                    var d = JsonSerializer.Deserialize<CachedMarketData>(json);
                    if (d == null) continue;
                    merged.PnL += d.PnL;
                    if (d.BidPoints != null) merged.BidPoints.AddRange(d.BidPoints);
                    if (d.AskPoints != null) merged.AskPoints.AddRange(d.AskPoints);
                    if (d.BuyPoints != null) merged.BuyPoints.AddRange(d.BuyPoints);
                    if (d.SellPoints != null) merged.SellPoints.AddRange(d.SellPoints);
                    if (d.ExitPoints != null) merged.ExitPoints.AddRange(d.ExitPoints);
                    if (d.EventPoints != null) merged.EventPoints.AddRange(d.EventPoints);
                    if (d.IntendedLongPoints != null) merged.IntendedLongPoints.AddRange(d.IntendedLongPoints);
                    if (d.IntendedShortPoints != null) merged.IntendedShortPoints.AddRange(d.IntendedShortPoints);
                    if (d.DiscrepancyPoints != null) merged.DiscrepancyPoints.AddRange(d.DiscrepancyPoints);  // NEW
                }

                merged.BidPoints = merged.BidPoints.OrderBy(p => p.Date).ToList();
                merged.AskPoints = merged.AskPoints.OrderBy(p => p.Date).ToList();
                merged.BuyPoints = merged.BuyPoints.OrderBy(p => p.Date).ToList();
                merged.SellPoints = merged.SellPoints.OrderBy(p => p.Date).ToList();
                merged.ExitPoints = merged.ExitPoints.OrderBy(p => p.Date).ToList();
                merged.EventPoints = merged.EventPoints.OrderBy(p => p.Date).ToList();
                merged.IntendedLongPoints = merged.IntendedLongPoints.OrderBy(p => p.Date).ToList();
                merged.IntendedShortPoints = merged.IntendedShortPoints.OrderBy(p => p.Date).ToList();
                merged.DiscrepancyPoints = merged.DiscrepancyPoints.OrderBy(p => p.Date).ToList();  // NEW
            }

            if (merged == null)
            {
                AppendLog($"No data for {baseMarket}");
                return;
            }

            AddPoints(merged.AskPoints, "Ask", Color.OrangeRed, 4, collectTooltips: false, connectLine: true);
            AddPoints(merged.BidPoints, "Bid", Color.DodgerBlue, 4, collectTooltips: false, connectLine: true);
            AddPoints(merged.BuyPoints, "Buy", Color.Green, 12, collectTooltips: true);
            AddPoints(merged.SellPoints, "Sell", Color.Red, 12, collectTooltips: true);
            AddPoints(merged.ExitPoints, "Exit", Color.Black, 12, collectTooltips: true); // NEW
            AddPoints(merged.EventPoints, "Event", Color.Purple, 0, collectTooltips: true);
            AddPoints(merged.DiscrepancyPoints, "Discrepancy", Color.Magenta, 8, collectTooltips: true);  // NEW

            formsPlot1.Plot.XAxis.TickLabelFormat("yyyy-MM-dd HH:mm", dateTimeFormat: true);
            formsPlot1.Plot.AxisAuto();
            var lim = formsPlot1.Plot.GetAxisLimits();
            double xPad = (lim.XMax - lim.XMin) * 0.05;
            double yPad = (lim.YMax - lim.YMin) * 0.10;
            formsPlot1.Plot.SetAxisLimits(lim.XMin - xPad, lim.XMax + xPad, lim.YMin - yPad, lim.YMax + yPad);

            _hoverLine = formsPlot1.Plot.AddVerticalLine(0, Color.Gray, 1);
            _hoverLine.IsVisible = false;
            formsPlot1.Render();
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
                    setKey: "Momentum",
                    weightName: "MT_J15",
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
