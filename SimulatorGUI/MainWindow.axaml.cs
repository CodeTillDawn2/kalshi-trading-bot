using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using LiveChartsCore;
using LiveChartsCore.Kernel;                                 // Coordinate, ChartPoint<>
using LiveChartsCore.SkiaSharpView;                          // LineSeries, ScatterSeries, Axis
using LiveChartsCore.SkiaSharpView.Avalonia;                 // CartesianChart
using LiveChartsCore.SkiaSharpView.Painting;                 // SolidColorPaint
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;       // CircleGeometry, LabelGeometry
using SkiaSharp;
using TradingSimulator.Simulator;
using TradingSimulator.TestObjects;                          // PricePoint
using TradingStrategies.Trading.Helpers;                     // StrategySelectionHelper

namespace TradingSimulatorGUI
{
    public class MarketResult : INotifyPropertyChanged
    {
        string _market = "";
        double _pnl;
        bool _isSelected;

        public string Market { get => _market; set { _market = value; OnPropertyChanged(nameof(Market)); } }

        public double PnL
        {
            get => _pnl;
            set
            {
                _pnl = Math.Round(value, 2, MidpointRounding.AwayFromZero);
                OnPropertyChanged(nameof(PnL));
                OnPropertyChanged(nameof(PnLDisplay));
            }
        }

        public string PnLDisplay => double.IsNaN(_pnl) ? "" : _pnl.ToString("F2");
        public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public class CachedMarketHeader
    {
        public string? Market { get; set; }
        public double? PnL { get; set; }
        public double? LastPnL { get; set; }
    }

    public class CachedMarketData
    {
        public string Market { get; set; } = "";
        public double? PnL { get; set; }
        public double? LastPnL { get; set; }
        public List<PricePoint> BidPoints { get; set; } = new();
        public List<PricePoint> AskPoints { get; set; } = new();
        public List<PricePoint> BuyPoints { get; set; } = new();
        public List<PricePoint> SellPoints { get; set; } = new();
        public List<PricePoint>? EventPoints { get; set; }
        public List<PricePoint>? IntendedLongPoints { get; set; }
        public List<PricePoint>? IntendedShortPoints { get; set; }
    }

    public partial class MainWindow : Window
    {
        ComboBox _setCombo = default!;
        ComboBox _weightCombo = default!;
        DataGrid _marketGrid = default!;
        TextBlock _profitLossText = default!;
        TextBox _testOutput = default!;
        Grid _listPanel = default!;
        Grid _chartPanel = default!;
        ContentControl _chartHost = default!;
        TextBlock _chartTitle = default!;

        readonly ObservableCollection<MarketResult> _marketResults = new();
        readonly DispatcherTimer _updateTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };

        readonly SimulatorTests _test = new();
        readonly StrategySelectionHelper _selHelper = new();

        CartesianChart _chart = default!;
        MarketResult? _selectedMarket;

        DateTime _currentMinDate = default;

        readonly string _cacheDirectory;
        readonly JsonSerializerOptions _readJson = new()
        {
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals | JsonNumberHandling.AllowReadingFromString
        };
        readonly JsonSerializerOptions _writeJson = new()
        {
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            WriteIndented = false
        };

        readonly Dictionary<string, (ObservableCollection<PricePoint> Bid,
                                     ObservableCollection<PricePoint> Ask,
                                     ObservableCollection<PricePoint> Buy,
                                     ObservableCollection<PricePoint> Sell,
                                     ObservableCollection<PricePoint> Ev,
                                     ObservableCollection<PricePoint> IntendedLong,
                                     ObservableCollection<PricePoint> IntendedShort)> _marketData = new();

        public MainWindow()
        {
            InitializeComponent();

            _cacheDirectory = ResolveCacheDirectory();
            Directory.CreateDirectory(_cacheDirectory);

            LoadCachedHeaders();                 // lazy: headers only (Market+PnL)
            _marketGrid.ItemsSource = _marketResults;

            _updateTimer.Tick += (_, __) => _updateTimer.Stop();

            _test.OnTestProgress += msg => Dispatcher.UIThread.Post(() => SafeAppend(msg));
            _test.OnProfitLossUpdate += (m, total) => Dispatcher.UIThread.Post(() => UpdateMarketPnL(m, total));
            _test.OnMarketProcessed += _ => Dispatcher.UIThread.Post(UpdateProfitLossText);

            InitSetWeightUi();
            _ = LoadAllMarketsAsync();
        }

        void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _setCombo = this.FindControl<ComboBox>("SetCombo") ?? throw new InvalidOperationException("SetCombo not found");
            _weightCombo = this.FindControl<ComboBox>("WeightCombo") ?? throw new InvalidOperationException("WeightCombo not found");
            _marketGrid = this.FindControl<DataGrid>("MarketGrid") ?? throw new InvalidOperationException("MarketGrid not found");
            _profitLossText = this.FindControl<TextBlock>("ProfitLossText") ?? throw new InvalidOperationException("ProfitLossText not found");
            _testOutput = this.FindControl<TextBox>("TestOutput") ?? throw new InvalidOperationException("TestOutput not found");
            _listPanel = this.FindControl<Grid>("ListPanel") ?? throw new InvalidOperationException("ListPanel not found");
            _chartPanel = this.FindControl<Grid>("ChartPanel") ?? throw new InvalidOperationException("ChartPanel not found");
            _chartHost = this.FindControl<ContentControl>("ChartHost") ?? throw new InvalidOperationException("ChartHost not found");
            _chartTitle = this.FindControl<TextBlock>("ChartTitle") ?? throw new InvalidOperationException("ChartTitle not found");
        }

        static string ResolveCacheDirectory()
        {
            var env = Environment.GetEnvironmentVariable("TS_CACHE_DIR");
            if (!string.IsNullOrWhiteSpace(env) && Directory.Exists(env)) return env;

            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var orig = Path.Combine(docs, "GitHub", "TestingOutput");
            if (Directory.Exists(orig)) return orig;

            var local = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TradingSimulatorGUI", "Cache");
            Directory.CreateDirectory(local);
            return local;
        }

        void InitSetWeightUi()
        {
            var sets = _selHelper.GetSetKeys().ToList();
            _setCombo.ItemsSource = sets;
            if (sets.Count > 0) _setCombo.SelectedIndex = 0;
        }

        void SetCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var setKey = _setCombo.SelectedItem as string;
            var weights = string.IsNullOrWhiteSpace(setKey)
                ? new List<string>()
                : _selHelper.GetWeightNames(setKey).ToList();

            _weightCombo.ItemsSource = weights;
            if (weights.Count > 0) _weightCombo.SelectedIndex = 0;
        }

        async void RunSelected_Click(object? sender, RoutedEventArgs e)
        {
            var setKey = (_setCombo.SelectedItem as string) ?? "Bollinger";
            var weightName = (_weightCombo.SelectedItem as string)
                             ?? _selHelper.GetWeightNames(setKey).FirstOrDefault() ?? "Default";
            var sel = _marketResults.Where(r => r.IsSelected).Select(r => r.Market).ToList();

            await ResetUIAsync();
            await Task.Run(async () =>
            {
                _test.Setup();
                await _test.RunSelectedSetForGuiAsync(setKey, weightName, writeToFile: true, marketsToRun: sel);
                _test.TearDown();
            });
            Dispatcher.UIThread.Post(UpdateProfitLossText);
        }

        async void RunTraining_Click(object? sender, RoutedEventArgs e)
        {
            var setKey = (_setCombo.SelectedItem as string) ?? "Bollinger";
            var sel = _marketResults.Where(r => r.IsSelected).Select(r => r.Market).ToList();

            await ResetUIAsync();
            await Task.Run(async () =>
            {
                _test.Setup();
                await _test.RunTrainingForGuiAsync(setKey, writeToFile: false, marketsToRun: sel);
                _test.TearDown();
            });
            Dispatcher.UIThread.Post(UpdateProfitLossText);
        }

        void SelectAllNA_Click(object? sender, RoutedEventArgs e)
        {
            foreach (var r in _marketResults) r.IsSelected = double.IsNaN(r.PnL);
        }

        void SelectAllWithResultsButton_Click(object? sender, RoutedEventArgs e)
        {
            foreach (var r in _marketResults) r.IsSelected = !double.IsNaN(r.PnL);
        }

        void SelectAll_Click(object? sender, RoutedEventArgs e)
        {
            foreach (var r in _marketResults) r.IsSelected = true;
        }

        void MarketGrid_DoubleTapped(object? sender, TappedEventArgs e)
        {
            if (_marketGrid.SelectedItem is not MarketResult mr) return;
            _selectedMarket = mr;
            _chartTitle.Text = $"Market: {mr.Market}";
            DrawChart(mr.Market);
            _listPanel.IsVisible = false;
            _chartPanel.IsVisible = true;
        }

        async void BackButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_selectedMarket != null)
                await SaveMarketToCacheAsync(_selectedMarket.Market);

            _chartPanel.IsVisible = false;
            _listPanel.IsVisible = true;
        }

        async Task LoadAllMarketsAsync()
        {
            try
            {
                _test.Setup();
                var tickers = await _test.GetSnapshotGroupNames();
                foreach (var t in tickers)
                    if (_marketResults.All(r => r.Market != t))
                        _marketResults.Add(new MarketResult { Market = t, PnL = double.NaN });
            }
            catch (Exception ex)
            {
                SafeAppend($"Startup error: {ex.Message}");
            }
        }

        void LoadCachedHeaders()
        {
            var files = Directory.Exists(_cacheDirectory)
                ? Directory.GetFiles(_cacheDirectory, "*.json")
                : Array.Empty<string>();

            foreach (var file in files)
            {
                try
                {
                    var head = JsonSerializer.Deserialize<CachedMarketHeader>(File.ReadAllText(file), _readJson);
                    if (head == null || string.IsNullOrWhiteSpace(head.Market)) continue;

                    var pnlVal = head.PnL ?? head.LastPnL;
                    var existing = _marketResults.FirstOrDefault(x => x.Market == head.Market);
                    if (existing == null)
                    {
                        _marketResults.Add(new MarketResult
                        {
                            Market = head.Market,
                            PnL = pnlVal ?? double.NaN
                        });
                    }
                    else if (pnlVal.HasValue)
                    {
                        existing.PnL = pnlVal.Value;
                    }
                }
                catch (Exception ex)
                {
                    SafeAppend($"Failed to load header {Path.GetFileName(file)}: {ex.Message}");
                }
            }
        }

        async Task SaveMarketToCacheAsync(string market)
        {
            if (!_marketData.TryGetValue(market, out var tuple)) return;

            var model = new CachedMarketData
            {
                Market = market,
                PnL = _marketResults.FirstOrDefault(r => r.Market == market)?.PnL,
                LastPnL = _marketResults.FirstOrDefault(r => r.Market == market)?.PnL,
                BidPoints = Sanitize(tuple.Bid).ToList(),
                AskPoints = Sanitize(tuple.Ask).ToList(),
                BuyPoints = Sanitize(tuple.Buy).ToList(),
                SellPoints = Sanitize(tuple.Sell).ToList(),
                EventPoints = Sanitize(tuple.Ev).ToList(),
                IntendedLongPoints = Sanitize(tuple.IntendedLong).ToList(),
                IntendedShortPoints = Sanitize(tuple.IntendedShort).ToList()
            };

            var path = Path.Combine(_cacheDirectory, $"{market}.json");
            var json = JsonSerializer.Serialize(model, _writeJson);
            await File.WriteAllTextAsync(path, json);
        }

        static IEnumerable<PricePoint> Sanitize(IEnumerable<PricePoint> src)
        {
            foreach (var p in src)
            {
                var price = double.IsFinite(p.Price) ? p.Price : 0.0;
                yield return new PricePoint(p.Date, price, p.Memo);
            }
        }

        (ObservableCollection<PricePoint>, ObservableCollection<PricePoint>,
         ObservableCollection<PricePoint>, ObservableCollection<PricePoint>,
         ObservableCollection<PricePoint>, ObservableCollection<PricePoint>,
         ObservableCollection<PricePoint>) GetOrCreateSeries(string market)
        {
            if (_marketData.TryGetValue(market, out var tuple)) return tuple;

            var path = Path.Combine(_cacheDirectory, $"{market}.json");
            if (File.Exists(path))
            {
                try
                {
                    var cached = JsonSerializer.Deserialize<CachedMarketData>(File.ReadAllText(path), _readJson);
                    if (cached != null)
                    {
                        var loaded = (
                            new ObservableCollection<PricePoint>(Sanitize(cached.BidPoints)),
                            new ObservableCollection<PricePoint>(Sanitize(cached.AskPoints)),
                            new ObservableCollection<PricePoint>(Sanitize(cached.BuyPoints)),
                            new ObservableCollection<PricePoint>(Sanitize(cached.SellPoints)),
                            new ObservableCollection<PricePoint>(Sanitize(cached.EventPoints ?? new List<PricePoint>())),
                            new ObservableCollection<PricePoint>(Sanitize(cached.IntendedLongPoints ?? new List<PricePoint>())),
                            new ObservableCollection<PricePoint>(Sanitize(cached.IntendedShortPoints ?? new List<PricePoint>()))
                        );
                        _marketData[market] = loaded;
                        return loaded;
                    }
                }
                catch (Exception ex)
                {
                    SafeAppend($"Failed to load chart data for {market}: {ex.Message}");
                }
            }

            var empty = (new ObservableCollection<PricePoint>(), new ObservableCollection<PricePoint>(),
                         new ObservableCollection<PricePoint>(), new ObservableCollection<PricePoint>(),
                         new ObservableCollection<PricePoint>(), new ObservableCollection<PricePoint>(),
                         new ObservableCollection<PricePoint>());
            _marketData[market] = empty;
            return empty;
        }

        // --------------------- Charting (zoom/pan + readable time + memo tooltips) ---------------------
        void EnsureChart()
        {
            if (_chart != null) return;

            _chart = new CartesianChart
            {
                Series = Array.Empty<ISeries>(),
                ZoomMode = LiveChartsCore.Measure.ZoomAndPanMode.X,
                ZoomingSpeed = 0.2,
                XAxes = new[]
                {
                    new Axis
                    {
                        Name = "Time",
                        Labeler = v => _currentMinDate == default
                            ? ""
                            : _currentMinDate.AddMinutes(v).ToString("yyyy-MM-dd HH:mm"),
                        TextSize = 12,
                        MinStep = 1
                    }
                },
                YAxes = new[]
                {
                    new Axis { Name = "Price", TextSize = 12 }
                }
            };
            _chartHost.Content = _chart;
        }

        static IReadOnlyDictionary<long, string> BuildMemoMap(IEnumerable<PricePoint> points, DateTime origin)
        {
            var d = new Dictionary<long, string>();
            foreach (var p in points)
            {
                var key = (long)Math.Round((p.Date - origin).TotalSeconds);
                d[key] = p.Memo ?? "";
            }
            return d;
        }

        static string FormatTip(
            IReadOnlyDictionary<long, string> memoMap,
            DateTime origin,
            ChartPoint<PricePoint, CircleGeometry, LabelGeometry> cp)
        {
            var minutes = cp.Coordinate.SecondaryValue;
            var when = origin.AddMinutes(minutes);
            var key = (long)Math.Round(minutes * 60.0);
            memoMap.TryGetValue(key, out var memo);
            return $"{when:yyyy-MM-dd HH:mm:ss}  {memo}  Price: {cp.Coordinate.PrimaryValue:F2}";
        }

        void DrawChart(string market)
        {
            EnsureChart();

            var (bid, ask, buy, sell, ev, il, ishort) = GetOrCreateSeries(market);

            _currentMinDate =
                bid.Select(p => p.Date).Concat(ask.Select(p => p.Date))
                   .Concat(buy.Select(p => p.Date)).Concat(sell.Select(p => p.Date))
                   .Concat(ev.Select(p => p.Date)).Concat(il.Select(p => p.Date)).Concat(ishort.Select(p => p.Date))
                   .DefaultIfEmpty(DateTime.UtcNow).Min();

            var bidMap = BuildMemoMap(bid, _currentMinDate);
            var askMap = BuildMemoMap(ask, _currentMinDate);
            var buyMap = BuildMemoMap(buy, _currentMinDate);
            var sellMap = BuildMemoMap(sell, _currentMinDate);
            var evMap = BuildMemoMap(ev, _currentMinDate);
            var ilMap = BuildMemoMap(il, _currentMinDate);
            var ishortMap = BuildMemoMap(ishort, _currentMinDate);

            var series = new List<ISeries>
            {
                new LineSeries<PricePoint>
                {
                    Values = bid,
                    Mapping = (p, i) => new Coordinate((p.Date - _currentMinDate).TotalMinutes, p.Price),
                    Name = "Bid",
                    GeometrySize = 0,
                    Stroke = new SolidColorPaint(new SKColor(0, 120, 215)) { StrokeThickness = 2 },
                    // SINGLE-ARG delegate per your build:
                    YToolTipLabelFormatter = cp => FormatTip(bidMap, _currentMinDate, cp)
                },
                new LineSeries<PricePoint>
                {
                    Values = ask,
                    Mapping = (p, i) => new Coordinate((p.Date - _currentMinDate).TotalMinutes, p.Price),
                    Name = "Ask",
                    GeometrySize = 0,
                    Stroke = new SolidColorPaint(new SKColor(220, 70, 50)) { StrokeThickness = 2 },
                    YToolTipLabelFormatter = cp => FormatTip(askMap, _currentMinDate, cp)
                }
            };

            if (buy.Any())
                series.Add(new ScatterSeries<PricePoint>
                {
                    Values = buy,
                    Mapping = (p, i) => new Coordinate((p.Date - _currentMinDate).TotalMinutes, p.Price),
                    GeometrySize = 6,
                    Name = "Buys",
                    Fill = new SolidColorPaint(new SKColor(0, 150, 0)),
                    YToolTipLabelFormatter = cp => FormatTip(buyMap, _currentMinDate, cp)
                });

            if (sell.Any())
                series.Add(new ScatterSeries<PricePoint>
                {
                    Values = sell,
                    Mapping = (p, i) => new Coordinate((p.Date - _currentMinDate).TotalMinutes, p.Price),
                    GeometrySize = 6,
                    Name = "Sells",
                    Fill = new SolidColorPaint(new SKColor(200, 0, 200)),
                    YToolTipLabelFormatter = cp => FormatTip(sellMap, _currentMinDate, cp)
                });

            if (ev.Any())
                series.Add(new ScatterSeries<PricePoint>
                {
                    Values = ev,
                    Mapping = (p, i) => new Coordinate((p.Date - _currentMinDate).TotalMinutes, p.Price),
                    GeometrySize = 4,
                    Name = "Events",
                    Fill = new SolidColorPaint(new SKColor(128, 128, 128)),
                    YToolTipLabelFormatter = cp => FormatTip(evMap, _currentMinDate, cp)
                });

            if (il.Any())
                series.Add(new ScatterSeries<PricePoint>
                {
                    Values = il,
                    Mapping = (p, i) => new Coordinate((p.Date - _currentMinDate).TotalMinutes, p.Price),
                    GeometrySize = 4,
                    Name = "Intended Long",
                    Fill = new SolidColorPaint(new SKColor(50, 180, 255)),
                    YToolTipLabelFormatter = cp => FormatTip(ilMap, _currentMinDate, cp)
                });

            if (ishort.Any())
                series.Add(new ScatterSeries<PricePoint>
                {
                    Values = ishort,
                    Mapping = (p, i) => new Coordinate((p.Date - _currentMinDate).TotalMinutes, p.Price),
                    GeometrySize = 4,
                    Name = "Intended Short",
                    Fill = new SolidColorPaint(new SKColor(255, 150, 0)),
                    YToolTipLabelFormatter = cp => FormatTip(ishortMap, _currentMinDate, cp)
                });

            _chart.Series = series;
        }

        // --------------------- Misc ---------------------
        void UpdateMarketPnL(string market, double totalRevenue)
        {
            var item = _marketResults.FirstOrDefault(r => r.Market == market);
            if (item == null)
            {
                item = new MarketResult { Market = market };
                _marketResults.Add(item);
            }
            item.PnL = totalRevenue;
            _updateTimer.Stop(); _updateTimer.Start();
            UpdateProfitLossText();
        }

        void UpdateProfitLossText()
        {
            var valid = _marketResults.Where(r => !double.IsNaN(r.PnL)).ToList();
            var pnl = valid.Sum(r => r.PnL);
            _profitLossText.Text = $"Total P&L: {pnl:F2}    Markets: {valid.Count}/{_marketResults.Count}";
        }

        async Task ResetUIAsync()
        {
            _testOutput.Text = "";
            UpdateProfitLossText();
            await Task.Yield();
        }

        void SafeAppend(string text)
        {
            try
            {
                _testOutput.Text += text + Environment.NewLine;
                _testOutput.CaretIndex = _testOutput.Text?.Length ?? 0;
            }
            catch { }
        }
    }
}
