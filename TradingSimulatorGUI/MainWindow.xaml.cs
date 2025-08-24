using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.VisualElements;
using LiveChartsCore.SkiaSharpView.WPF;
using SkiaSharp;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using TradingSimulator.Simulator;
using TradingSimulator.Strategies;
using TradingSimulator.TestObjects;

namespace TradingSimulatorGUI
{
    public class ChartViewModel : INotifyPropertyChanged
    {
        public string Market { get; set; }
        public ObservableCollection<ISeries> Series { get; set; }
        public Axis[] XAxes { get; set; }
        public Axis[] YAxes { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class MarketResult : INotifyPropertyChanged
    {
        private string _market;
        public string Market { get => _market; set { _market = value; OnPropertyChanged(nameof(Market)); } }
        private double _pnL;
        public double PnL { get => _pnL; set { _pnL = value; OnPropertyChanged(nameof(PnL)); } }
        private ChartViewModel _chart;
        public ChartViewModel Chart { get => _chart; set { _chart = value; OnPropertyChanged(nameof(Chart)); } }
        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); } }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class PnLConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
        {
            if (value is double v) return double.IsNaN(v) ? "N/A" : v.ToString("F2", c);
            return value;
        }
        public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }

    public partial class MainWindow : Window
    {
        // keep fields; instantiate lazily on Loaded to avoid ctor-time work
        private SimulatorTests _test;
        private readonly Dictionary<string, (ObservableCollection<PricePoint> BidPoints,
                                            ObservableCollection<PricePoint> AskPoints,
                                            ObservableCollection<PricePoint> BuyPoints,
                                            ObservableCollection<PricePoint> SellPoints,
                                            ObservableCollection<PricePoint> EventPoints,
                                            ObservableCollection<PricePoint> IntendedLongPoints,
                                            ObservableCollection<PricePoint> IntendedShortPoints)> _marketData
            = new();

        private readonly ObservableCollection<MarketResult> _marketResults = new();
        private readonly DispatcherTimer _updateTimer = new() { Interval = TimeSpan.FromSeconds(0.5) };
        private MarketResult _selectedMarketResult;

        private bool IsChartVisible() => ChartPanel.Visibility == Visibility.Visible;

        private readonly string _cacheDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "TestingOutput"));

        private CartesianChart _chartViewer; // created after Loaded

        public ObservableCollection<MarketResult> MarketResults => _marketResults;

        public MainWindow()
        {
            InitializeComponent();

            // keep ctor minimal; no IO, no simulator, no chart, no JSON here
            DataContext = this;

            _updateTimer.Tick += (s, e) => _updateTimer.Stop();

            // Defer everything until after the window is shown
            Loaded += async (_, __) =>
            {
                try
                {
                    Directory.CreateDirectory(_cacheDirectory);
                    LoadCachedMetadata();

                    _test = new SimulatorTests();

                    _test.OnTestProgress += msg => Dispatcher.Invoke(() =>
                    {
                        SafeAppend(msg);
                        SafeScrollToEnd();
                    });

                    _test.OnProfitLossUpdate += (market, totalRevenue) => Dispatcher.Invoke(() =>
                    {
                        UpdateMarketPnL(market, totalRevenue);
                    });

                    _test.OnMarketProcessed += market => Dispatcher.Invoke(() =>
                    {
                        SafeAppend($"Market {market} processing completed, total results: {_marketResults.Count}");
                        UpdateProfitLossText();
                        ShowGridView();
                    });

                    await LoadAllMarketsAsync();
                    ShowGridView();
                }
                catch (Exception ex)
                {
                    // if something explodes early, show it — helps debugging “hangs”
                    MessageBox.Show($"Startup error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    SafeAppend($"Startup error: {ex}");
                    throw;
                }
            };
        }

        private async Task LoadAllMarketsAsync()
        {
            _test.Setup();
            var tickers = await _test.GetSnapshotGroupNames();
            foreach (var t in tickers)
                if (!_marketResults.Any(r => r.Market == t))
                    _marketResults.Add(new MarketResult { Market = t, PnL = double.NaN });
        }

        private void LoadCachedMetadata()
        {
            var files = Directory.GetFiles(_cacheDirectory, "*.json");
            SafeAppend($"Found {files.Length}.");
            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var cached = JsonSerializer.Deserialize<CachedMarketData>(json);
                    var existing = _marketResults.FirstOrDefault(r => r.Market == cached.Market);
                    if (existing != null) existing.PnL = cached.PnL;
                    else _marketResults.Add(new MarketResult { Market = cached.Market, PnL = cached.PnL });
                    SafeAppend($"Loaded metadata for {cached.Market} with PnL {cached.PnL} series.");
                }
                catch (Exception ex)
                {
                    SafeAppend($"Failed to load metadata for {Path.GetFileName(file)}: {ex.Message} series.");
                }
            }
        }

        private (ObservableCollection<PricePoint> BidPoints, ObservableCollection<PricePoint> AskPoints,
                 ObservableCollection<PricePoint> BuyPoints, ObservableCollection<PricePoint> SellPoints,
                 ObservableCollection<PricePoint> EventPoints, ObservableCollection<PricePoint> IntendedLongPoints,
                 ObservableCollection<PricePoint> IntendedShortPoints)
            LoadMarketData(string market)
        {
            var path = Path.Combine(_cacheDirectory, $"{market}.json");
            if (!File.Exists(path))
            {
                SafeAppend($"No cache file found for {market}.");
                return (new(), new(), new(), new(), new(), new(), new());
            }

            try
            {
                SafeAppend($"Loading data for {market} from {path}.");
                var json = File.ReadAllText(path);
                var cached = JsonSerializer.Deserialize<CachedMarketData>(json);

                var bid = new ObservableCollection<PricePoint>(cached.BidPoints);
                var ask = new ObservableCollection<PricePoint>(cached.AskPoints);
                var buy = new ObservableCollection<PricePoint>(cached.BuyPoints);
                var sell = new ObservableCollection<PricePoint>(cached.SellPoints);
                var evs = new ObservableCollection<PricePoint>(cached.EventPoints ?? new List<PricePoint>());
                var il = new ObservableCollection<PricePoint>(cached.IntendedLongPoints ?? new List<PricePoint>());
                var ishort = new ObservableCollection<PricePoint>(cached.IntendedShortPoints ?? new List<PricePoint>());

                SafeAppend($"Loaded {bid.Count} bid points, {ask.Count} ask points, {buy.Count} buy points, {sell.Count} sell points, {evs.Count} event points for {market}.");
                if (bid.Any()) SafeAppend($"Sample bid point: Date={bid.First().Date}, Price={bid.First().Price}");

                return (bid, ask, buy, sell, evs, il, ishort);
            }
            catch (Exception ex)
            {
                SafeAppend($"Failed to load data for {market}: {ex.Message}");
                return (new(), new(), new(), new(), new(), new(), new());
            }
        }

        private void UpdateMarketPnL(string market, double pnL)
        {
            var r = _marketResults.FirstOrDefault(x => x.Market == market);
            if (r != null) r.PnL = pnL; else _marketResults.Add(new MarketResult { Market = market, PnL = pnL });
        }

        private void UpdateProfitLossText()
        {
            var total = _marketResults.Where(r => !double.IsNaN(r.PnL)).Sum(r => r.PnL);
            ProfitLossText.Text = $"Profit/Loss: ${total:F2}";
        }

        private async void RunBollinger_Click(object sender, RoutedEventArgs e)
        {
            var sel = _marketResults.Where(r => r.IsSelected).Select(r => r.Market).ToList();
            await ResetUIAsync();
            await Task.Run(async () =>
            {
                _test.Setup();
                await _test.RunWeightsForGuiAsync(marketsToRun: sel, writeToFile: true);
                _test.TearDown();
            });
            Dispatcher.Invoke(() => { UpdateProfitLossText(); ShowGridView(); });
            SafeScrollToEnd();
        }

        private async void RunBollingerTraining_Click(object sender, RoutedEventArgs e)
        {
            var sel = _marketResults.Where(r => r.IsSelected).Select(r => r.Market).ToList();
            await ResetUIAsync();
            await Task.Run(async () =>
            {
                _test.Setup();
                await _test.RunMultipleAllStrategiesForGuiAsync(marketsToRun: sel, writeToFile: false);
                _test.TearDown();
            });
            Dispatcher.Invoke(() => { UpdateProfitLossText(); ShowGridView(); SafeScrollToEnd(); });
        }


        private void SelectAllNA_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                foreach (var r in _marketResults) r.IsSelected = double.IsNaN(r.PnL);
                SafeAppend("Selected all markets with NaN PnL.");
            });
        }

        private void SelectAllWithResultsButton_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                foreach (var r in _marketResults) r.IsSelected = !double.IsNaN(r.PnL) && r.PnL != 0;
                SafeAppend("Selected all markets with PnL.");
            });
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                foreach (var r in _marketResults) r.IsSelected = true;
                SafeAppend("Selected all markets.");
            });
        }

        private async Task ResetUIAsync()
        {
            _marketData.Clear();
            _marketResults.Clear();
            _updateTimer.Stop();
            TestOutput.Text = "";
            ProfitLossText.Text = "Profit/Loss: $0.00";
            LoadCachedMetadata();
            await LoadAllMarketsAsync();
            ShowGridView();
        }

        private ChartViewModel BuildChartViewModel(
            string market,
            (ObservableCollection<PricePoint> BidPoints,
             ObservableCollection<PricePoint> AskPoints,
             ObservableCollection<PricePoint> BuyPoints,
             ObservableCollection<PricePoint> SellPoints,
             ObservableCollection<PricePoint> EventPoints,
             ObservableCollection<PricePoint> IntendedLongPoints,
             ObservableCollection<PricePoint> IntendedShortPoints) data)
        {
            var allDates = data.BidPoints.Select(p => p.Date)
                .Concat(data.AskPoints.Select(p => p.Date))
                .Concat(data.BuyPoints.Select(p => p.Date))
                .Concat(data.SellPoints.Select(p => p.Date))
                .Concat(data.EventPoints.Select(p => p.Date))
                .Concat(data.IntendedLongPoints.Select(p => p.Date))
                .Concat(data.IntendedShortPoints.Select(p => p.Date))
                .ToList();

            var minDate = allDates.Any() ? allDates.Min() : DateTime.UtcNow;

            // single midpoint line (average of bid/ask)
            var mids = new ObservableCollection<PricePoint>();
            var n = Math.Min(data.BidPoints.Count, data.AskPoints.Count);
            for (int i = 0; i < n; i++)
            {
                var b = data.BidPoints[i];
                var a = data.AskPoints[i];
                var memo = !string.IsNullOrEmpty(b.Memo) ? b.Memo : a.Memo;
                mids.Add(new PricePoint(b.Date, (b.Price + a.Price) / 2.0, memo));
            }

            var series = new ObservableCollection<ISeries>
            {
                new LineSeries<PricePoint>
                {
                    Values = mids,
                    Name = $"{market} Midpoint",
                    Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 2 },
                    Fill = null,
                    Mapping = (pt, _) => new Coordinate((pt.Date - minDate).TotalMinutes, pt.Price),
                    AnimationsSpeed = TimeSpan.Zero,
                    YToolTipLabelFormatter = cp =>
                    {
                        var pt = cp.Model as PricePoint;
                        var time = pt?.Date ?? DateTime.MinValue;
                        string memo = "";
                        if (pt != null && !string.IsNullOrEmpty(pt.Memo))
                        {
                            var formatted = string.Join(Environment.NewLine,
                                pt.Memo.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                       .Select(s => s.Trim())
                                       .Where(s => !string.IsNullOrEmpty(s)));
                            memo = $"{Environment.NewLine}Memo:{Environment.NewLine}{formatted}";
                        }
                        return $"Mid: {cp.Coordinate.SecondaryValue:F2} at {time:yyyy-MM-dd HH:mm}{memo}";
                    }
                }
            };

            // vertical buy lines
            var buys = new ObservableCollection<PricePoint?>();
            foreach (var p in data.BuyPoints) { buys.Add(new PricePoint(p.Date, 0, p.Memo)); buys.Add(new PricePoint(p.Date, 100, p.Memo)); buys.Add(null); }
            series.Add(new LineSeries<PricePoint?>
            {
                Values = buys,
                Name = $"{market} Buys",
                Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 2 },
                Fill = null,
                GeometrySize = 5,
                LineSmoothness = 0,
                Mapping = (pt, _) => pt == null ? Coordinate.Empty : new Coordinate((pt.Date - minDate).TotalMinutes, pt.Price),
                AnimationsSpeed = TimeSpan.Zero,
                YToolTipLabelFormatter = cp =>
                {
                    if (cp.Model is not PricePoint pt) return "N/A";
                    if (!string.IsNullOrEmpty(pt.Memo))
                    {
                        var formatted = string.Join(Environment.NewLine, pt.Memo.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)));
                        return $"Buy Event{Environment.NewLine}Time: {pt.Date:yyyy-MM-dd HH:mm}{Environment.NewLine}Memo:{Environment.NewLine}{formatted}";
                    }
                    return $"Buy Event at {pt.Date:yyyy-MM-dd HH:mm}";
                }
            });

            // vertical sell lines
            var sells = new ObservableCollection<PricePoint?>();
            foreach (var p in data.SellPoints) { sells.Add(new PricePoint(p.Date, 0, p.Memo)); sells.Add(new PricePoint(p.Date, 100, p.Memo)); sells.Add(null); }
            series.Add(new LineSeries<PricePoint?>
            {
                Values = sells,
                Name = $"{market} Sells",
                Stroke = new SolidColorPaint(SKColors.Tan) { StrokeThickness = 2 },
                Fill = null,
                GeometrySize = 5,
                LineSmoothness = 0,
                Mapping = (pt, _) => pt == null ? Coordinate.Empty : new Coordinate((pt.Date - minDate).TotalMinutes, pt.Price),
                AnimationsSpeed = TimeSpan.Zero,
                YToolTipLabelFormatter = cp =>
                {
                    if (cp.Model is not PricePoint pt) return "N/A";
                    if (!string.IsNullOrEmpty(pt.Memo))
                    {
                        var formatted = string.Join(Environment.NewLine, pt.Memo.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)));
                        return $"Sell Event{Environment.NewLine}Time: {pt.Date:yyyy-MM-dd HH:mm}{Environment.NewLine}Memo:{Environment.NewLine}{formatted}";
                    }
                    return $"Sell Event at {pt.Date:yyyy-MM-dd HH:mm}";
                }
            });

            // events
            series.Add(new ScatterSeries<PricePoint>
            {
                Values = data.EventPoints,
                Name = $"{market} Events",
                Stroke = new SolidColorPaint(SKColors.Purple) { StrokeThickness = 2 },
                Fill = new SolidColorPaint(SKColors.Purple),
                GeometrySize = 8,
                Mapping = (pt, _) => new Coordinate((pt.Date - minDate).TotalMinutes, pt.Price),
                AnimationsSpeed = TimeSpan.Zero,
                YToolTipLabelFormatter = cp =>
                {
                    if (cp.Model is not PricePoint pt) return "N/A";
                    var memo = pt.Memo ?? "N/A";
                    var formatted = string.Join(Environment.NewLine, memo.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)));
                    return $"Event{Environment.NewLine}Time: {pt.Date:yyyy-MM-dd HH:mm}{Environment.NewLine}Memo:{Environment.NewLine}{formatted}";
                }
            });

            // intended long
            series.Add(new ScatterSeries<PricePoint>
            {
                Values = data.IntendedLongPoints,
                Name = $"{market} Intended Longs",
                Stroke = new SolidColorPaint(SKColors.LightGreen) { StrokeThickness = 2 },
                Fill = new SolidColorPaint(SKColors.LightGreen),
                GeometrySize = 10,
                Mapping = (pt, _) => new Coordinate((pt.Date - minDate).TotalMinutes, pt.Price),
                AnimationsSpeed = TimeSpan.Zero,
                YToolTipLabelFormatter = cp =>
                {
                    if (cp.Model is not PricePoint pt) return "N/A";
                    var memo = pt.Memo ?? "N/A";
                    var formatted = string.Join(Environment.NewLine, memo.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)));
                    return $"Intended Long{Environment.NewLine}Time: {pt.Date:yyyy-MM-dd HH:mm}{Environment.NewLine}Memo:{Environment.NewLine}{formatted}";
                }
            });

            // intended short
            series.Add(new ScatterSeries<PricePoint>
            {
                Values = data.IntendedShortPoints,
                Name = $"{market} Intended Shorts",
                Stroke = new SolidColorPaint(SKColors.Orange) { StrokeThickness = 2 },
                Fill = new SolidColorPaint(SKColors.Orange),
                GeometrySize = 10,
                Mapping = (pt, _) => new Coordinate((pt.Date - minDate).TotalMinutes, pt.Price),
                AnimationsSpeed = TimeSpan.Zero,
                YToolTipLabelFormatter = cp =>
                {
                    if (cp.Model is not PricePoint pt) return "N/A";
                    var memo = pt.Memo ?? "N/A";
                    var formatted = string.Join(Environment.NewLine, memo.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)));
                    return $"Intended Short{Environment.NewLine}Time: {pt.Date:yyyy-MM-dd HH:mm}{Environment.NewLine}Memo:{Environment.NewLine}{formatted}";
                }
            });

            return new ChartViewModel
            {
                Market = market,
                Series = series,
                XAxes = new[]
                {
                    new Axis { Labeler = v => minDate.AddMinutes(v).ToString("yyyy-MM-dd HH:mm"), UnitWidth = 1, MinStep = 1 }
                },
                YAxes = new[]
                {
                    new Axis { Labeler = v => $"{v:F2}", MinStep = 0.01 }
                }
            };
        }

        private void EnsureChartCreated()
        {
            if (_chartViewer != null) return;
            _chartViewer = new CartesianChart { ZoomMode = ZoomAndPanMode.Both, ZoomingSpeed = 0.5, TooltipTextSize = 10 };
            ChartHost.Content = _chartViewer;
        }

        private void ShowGridView()
        {
            MarketGrid.Visibility = Visibility.Visible;
            ChartPanel.Visibility = Visibility.Collapsed;
            BackButton.Visibility = Visibility.Collapsed;
        }

        private void ShowChartView()
        {
            if (_selectedMarketResult?.Chart == null)
            {
                MessageBox.Show("Chart is null for selected market.");
                SafeAppend($"Chart is null for {_selectedMarketResult?.Market}.");
                return;
            }
            if (_selectedMarketResult.Chart.Series.Count == 0)
            {
                MessageBox.Show("No series in chart for selected market.");
                SafeAppend($"No series in chart for {_selectedMarketResult.Market}.");
                return;
            }

            EnsureChartCreated();

            MarketGrid.Visibility = Visibility.Collapsed;
            ChartPanel.Visibility = Visibility.Visible;
            BackButton.Visibility = Visibility.Visible;

            _chartViewer.DataContext = _selectedMarketResult.Chart;
            _chartViewer.Series = _selectedMarketResult.Chart.Series;
            _chartViewer.XAxes = _selectedMarketResult.Chart.XAxes;
            _chartViewer.YAxes = _selectedMarketResult.Chart.YAxes;
            _chartViewer.TooltipPosition = TooltipPosition.Top;

            _chartViewer.Title = new LabelVisual
            {
                Text = _selectedMarketResult.Chart.Market,
                TextSize = 18,
                Paint = new SolidColorPaint(SKColors.Black)
            };
        }

        private void MarketGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid dg && dg.CurrentColumn?.Header is string h && h == "PnL")
            {
                if (MarketGrid.SelectedItem is MarketResult selected)
                {
                    SafeAppend($"Selected market: {selected.Market}.");
                    _selectedMarketResult = selected;
                    if (_selectedMarketResult.Chart == null)
                    {
                        var data = LoadMarketData(_selectedMarketResult.Market);
                        if (data.BidPoints.Count > 0 || data.AskPoints.Count > 0)
                        {
                            _selectedMarketResult.Chart = BuildChartViewModel(_selectedMarketResult.Market, data);
                            _marketData[_selectedMarketResult.Market] = data;
                        }
                        else SafeAppend($"No data loaded for {selected.Market}.");
                    }
                    else SafeAppend($"Chart already exists for {selected.Market}.");
                    ShowChartView();
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMarketResult?.Chart != null)
            {
                foreach (var s in _selectedMarketResult.Chart.Series)
                {
                    if (s is LineSeries<PricePoint> l1) (l1.Values as ObservableCollection<PricePoint>)?.Clear();
                    else if (s is LineSeries<PricePoint?> l2) (l2.Values as ObservableCollection<PricePoint?>)?.Clear();
                    else if (s is ScatterSeries<PricePoint> sc) (sc.Values as ObservableCollection<PricePoint>)?.Clear();
                }
                _selectedMarketResult.Chart.Series?.Clear();
                _selectedMarketResult.Chart = null;

                if (_marketData.ContainsKey(_selectedMarketResult.Market))
                {
                    var d = _marketData[_selectedMarketResult.Market];
                    d.BidPoints.Clear(); d.AskPoints.Clear(); d.BuyPoints.Clear(); d.SellPoints.Clear();
                    d.EventPoints.Clear(); d.IntendedLongPoints.Clear(); d.IntendedShortPoints.Clear();
                    _marketData.Remove(_selectedMarketResult.Market);
                }
                SafeAppend($"Cleared chart and data for {_selectedMarketResult.Market}.");
            }
            ShowGridView();
        }

        private void SafeScrollToEnd() { try { if (!IsChartVisible()) TestOutput.ScrollToEnd(); } catch { } }
        private void SafeAppend(string text) { try { TestOutput.Text += $"{text}\n"; } catch { } }
    }
}
