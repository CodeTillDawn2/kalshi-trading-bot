using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using ScottPlot;
using TradingSimulator.Simulator;
using TradingSimulator.TestObjects;
using System.Threading.Tasks;

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

        public MainForm()
        {
            InitializeComponent();

            // keep tooltip fixed even when focus changes
            toolTip1.ShowAlways = true;

            _simulator = new SimulatorTests();
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

        }


        private void LoadCache()
        {
            dgvMarkets.Columns.Clear();
            dgvMarkets.Rows.Clear();

            // grid sizing defaults
            dgvMarkets.RowHeadersVisible = false;
            dgvMarkets.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            var checkCol = new DataGridViewCheckBoxColumn
            {
                Name = "CheckedCol",
                HeaderText = "✓",
                FalseValue = false,
                TrueValue = true,
                ValueType = typeof(bool),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells
            };
            dgvMarkets.Columns.Add(checkCol);

            var marketCol = new DataGridViewTextBoxColumn
            {
                Name = "Market",
                HeaderText = "Market",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 75
            };
            dgvMarkets.Columns.Add(marketCol);

            var pnlCol = new DataGridViewTextBoxColumn
            {
                Name = "PnL",
                HeaderText = "PnL",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            };
            dgvMarkets.Columns.Add(pnlCol);

            // tighten checkbox column after it's realized
            dgvMarkets.HandleCreated += (_, __) =>
            {
                if (dgvMarkets.Columns["CheckedCol"] is DataGridViewCheckBoxColumn c)
                    dgvMarkets.Columns["CheckedCol"].Width = 32;
            };

            if (!Directory.Exists(_cacheDir))
            {
                AppendLog($"Missing cache directory: {_cacheDir}");
                return;
            }

            foreach (var file in Directory.GetFiles(_cacheDir, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var data = JsonSerializer.Deserialize<CachedMarketData>(json);
                    dgvMarkets.Rows.Add(_checkedMarketNames.Contains(data.Market), data.Market, data.PnL.ToString("F2"));
                }
                catch (Exception ex)
                {
                    AppendLog($"Error loading {file}: {ex.Message}");
                }
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


        private void AddPoints(List<PricePoint> points, string label, Color color)
        {
            if (points == null || points.Count == 0) return;

            var xs = points.Select(p => p.Date.ToOADate()).ToArray();
            var ys = points.Select(p => p.Price).ToArray();
            var memos = points.Select(p => string.IsNullOrWhiteSpace(p.Memo) ? label : p.Memo).ToArray();

            formsPlot1.Plot.AddScatter(xs, ys, color: color, label: label, markerSize: 6, lineWidth: 0);

            for (int i = 0; i < xs.Length; i++)
                _tooltipPoints.Add((xs[i], ys[i], memos[i]));
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
        }



        private void FormsPlot1_MouseLeave(object sender, EventArgs e)
        {
            _tooltipOverlay.Visible = false;
            _lastTooltipMemo = null;
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
            rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
            rtbLog.ScrollToCaret();
        }

        private void UpdatePnL(string market, double pnl)
        {
            foreach (DataGridViewRow row in dgvMarkets.Rows)
            {
                if (row.Cells["Market"].Value?.ToString() == market)
                {
                    row.Cells["PnL"].Value = pnl.ToString("F2");
                    break;
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
            string path = Path.Combine(_cacheDir, $"{market}.json");
            if (!File.Exists(path))
            {
                AppendLog($"Missing file: {path}");
                return;
            }

            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<CachedMarketData>(json);
            formsPlot1.Plot.Clear();

            var mids = data.BidPoints.Zip(data.AskPoints, (b, a) => new
            {
                Time = b.Date.ToOADate(),
                Price = (b.Price + a.Price) / 2
            }).ToList();

            formsPlot1.Plot.AddScatter(
                mids.Select(m => m.Time).ToArray(),
                mids.Select(m => m.Price).ToArray(),
                color: Color.Blue,
                label: "Midpoint");

            _tooltipPoints.Clear();

            AddPoints(data.BuyPoints, "Buy", Color.Green);
            AddPoints(data.SellPoints, "Sell", Color.Red);
            AddPoints(data.EventPoints, "Event", Color.Purple);

            formsPlot1.Plot.XAxis.TickLabelFormat("yyyy-MM-dd HH:mm", dateTimeFormat: true);

            formsPlot1.Plot.Legend();
            formsPlot1.Render();

        }

        private async void btnRun_Click(object sender, EventArgs e)
        {
            var sel = GetCheckedMarkets();
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
            EnsureSimulatorSetup();
            try
            {
                await _simulator.RunSelectedSetForGuiAsync(
                    setKey: "Breakout2",
                    weightName: "B2_MRB5_A10",
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
