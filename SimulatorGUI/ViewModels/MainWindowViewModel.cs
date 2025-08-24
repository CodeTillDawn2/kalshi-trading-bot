using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using ReactiveUI;
using SimulatorGUI.Models;
using SimulatorGUI.Services;
using SkiaSharp;

namespace SimulatorGUI.ViewModels
{
    public sealed class MainWindowViewModel : ReactiveObject
    {
        private readonly UiRunnerService _runner = new();
        private CancellationTokenSource? _cts;

        // ======= config / diagnostics =======
        // If your cache folder moves, update this in one place
        private string CacheDir => Path.Combine("..", "..", "..", "..", "..", "TestingOutput");

        private string _diagnostics = "";
        public string Diagnostics
        {
            get => _diagnostics;
            set => this.RaiseAndSetIfChanged(ref _diagnostics, value);
        }

        // ======= table =======
        public ObservableCollection<MarketResult> MarketResults { get; } = new();

        private MarketResult? _selectedMarket;
        public MarketResult? SelectedMarket
        {
            get => _selectedMarket;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedMarket, value);
                _ = LoadChartForSelectionAsync();
            }
        }

        // ======= top bar =======
        private string _selectedPreset = "Bollinger";
        public string SelectedPreset
        {
            get => _selectedPreset;
            set => this.RaiseAndSetIfChanged(ref _selectedPreset, value);
        }

        private string _status = "Idle";
        public string Status
        {
            get => _status;
            set => this.RaiseAndSetIfChanged(ref _status, value);
        }

        private string _output = "";
        public string Output
        {
            get => _output;
            set => this.RaiseAndSetIfChanged(ref _output, value);
        }

        public string TotalPnLText => $"Total PnL: {MarketResults.Sum(m => m.PnL):F2}";

        // Toggle: include cached markets not present in runner list
        private bool _includeUnknownCached = true;
        public bool IncludeUnknownCached
        {
            get => _includeUnknownCached;
            set
            {
                if (this.RaiseAndSetIfChanged(ref _includeUnknownCached, value))
                    _ = RebuildMarketListAsync();
            }
        }

        // Track and expose which cached markets are not in runner list
        public ObservableCollection<string> UnknownCachedMarkets { get; } = new();
        private int _unknownCachedCount;
        public int UnknownCachedCount
        {
            get => _unknownCachedCount;
            private set => this.RaiseAndSetIfChanged(ref _unknownCachedCount, value);
        }
        public string UnknownCachedInfo =>
            _unknownCachedCount == 0 ? "" : $"(+{_unknownCachedCount} cache-only markets hidden)";

        // ======= chart state =======
        public ObservableCollection<ISeries> ChartSeries { get; } = new();

        private ICartesianAxis[] _xAxes = new ICartesianAxis[]
        {
            new Axis { Labeler = v => v.ToString(CultureInfo.InvariantCulture), MinStep = 1 }
        };
        public ICartesianAxis[] XAxes
        {
            get => _xAxes;
            private set => this.RaiseAndSetIfChanged(ref _xAxes, value);
        }

        private ICartesianAxis[] _yAxes = new ICartesianAxis[]
        {
            new Axis { Labeler = v => v.ToString("F2", CultureInfo.InvariantCulture), MinStep = 0.01 }
        };
        public ICartesianAxis[] YAxes
        {
            get => _yAxes;
            private set => this.RaiseAndSetIfChanged(ref _yAxes, value);
        }

        private bool _isChartVisible;
        public bool IsChartVisible
        {
            get => _isChartVisible;
            set => this.RaiseAndSetIfChanged(ref _isChartVisible, value);
        }

        // ======= commands =======
        public ReactiveCommand<Unit, Unit> RunBollingerCommand { get; }
        public ReactiveCommand<Unit, Unit> RunTrainingCommand { get; }
        public ReactiveCommand<Unit, Unit> SelectAllCommand { get; }
        public ReactiveCommand<Unit, Unit> SelectNaCommand { get; }
        public ReactiveCommand<Unit, Unit> SelectPnLCommand { get; }
        public ReactiveCommand<Unit, Unit> ShowHiddenCommand { get; }

        public MainWindowViewModel()
        {
            // wire runner events
            _runner.TestProgress += msg => AppendLog(msg);
            _runner.ProfitLossUpdate += (market, pnl) =>
            {
                var row = MarketResults.FirstOrDefault(r => string.Equals(r.Market, market, StringComparison.OrdinalIgnoreCase));
                if (row is null)
                {
                    row = new MarketResult { Market = market, PnL = pnl };
                    MarketResults.Add(row);
                }
                else row.PnL = pnl;

                this.RaisePropertyChanged(nameof(TotalPnLText));
            };
            _runner.MarketProcessed += market => AppendLog($"Processed {market}\n");

            RunBollingerCommand = ReactiveCommand.CreateFromTask(RunBollingerAsync);
            RunTrainingCommand = ReactiveCommand.CreateFromTask(RunTrainingAsync);
            SelectAllCommand = ReactiveCommand.Create(SelectAll);
            SelectNaCommand = ReactiveCommand.Create(SelectNa);
            SelectPnLCommand = ReactiveCommand.Create(SelectPnLNonZero);
            ShowHiddenCommand = ReactiveCommand.Create(LogHiddenMarkets);
        }

        // ======= initialization / rebuild =======
        public async Task InitializeAsync() => await RebuildMarketListAsync();

        private async Task RebuildMarketListAsync()
        {
            Diagnostics = $"CacheDir: {Path.GetFullPath(CacheDir)}";

            var cache = ReadCacheIndex(); // Market -> (pnl, path)
            HashSet<string> names;
            try
            {
                names = await _runner.GetSnapshotGroupNames();
            }
            catch (Exception ex)
            {
                AppendLog($"[Init] Failed to query markets from runner: {ex.Message}\n");
                names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            MarketResults.Clear();
            UnknownCachedMarkets.Clear();

            if (names.Count == 0)
            {
                foreach (var (market, pnl) in cache.Select(kv => (kv.Key, kv.Value.pnl))
                                                   .OrderBy(t => t.Key, StringComparer.OrdinalIgnoreCase))
                    MarketResults.Add(new MarketResult { Market = market, PnL = pnl });

                UnknownCachedCount = 0;
                this.RaisePropertyChanged(nameof(UnknownCachedInfo));
                this.RaisePropertyChanged(nameof(TotalPnLText));
                return;
            }

            // Authoritative list first
            foreach (var name in names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
            {
                cache.TryGetValue(name, out var entry);
                MarketResults.Add(new MarketResult { Market = name, PnL = entry.pnl });
            }

            // Determine which cache markets are not in runner names
            foreach (var kv in cache)
                if (!names.Contains(kv.Key))
                    UnknownCachedMarkets.Add(kv.Key);

            UnknownCachedCount = UnknownCachedMarkets.Count;
            this.RaisePropertyChanged(nameof(UnknownCachedInfo));

            if (IncludeUnknownCached && UnknownCachedCount > 0)
            {
                foreach (var market in UnknownCachedMarkets.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                {
                    var pnl = cache.TryGetValue(market, out var entry) ? entry.pnl : 0;
                    MarketResults.Add(new MarketResult { Market = market, PnL = pnl });
                }
            }

            this.RaisePropertyChanged(nameof(TotalPnLText));
        }

        /// <summary>Loads cache entries: Market -> (PnL, FilePath)</summary>
        private Dictionary<string, (double pnl, string path)> ReadCacheIndex()
        {
            var dict = new Dictionary<string, (double, string)>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!Directory.Exists(CacheDir)) return dict;

                foreach (var file in Directory.EnumerateFiles(CacheDir, "*.json"))
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var cached = JsonSerializer.Deserialize<CachedMarketData>(json);
                        if (cached is null || string.IsNullOrWhiteSpace(cached.Market)) continue;

                        // Normalize names in case of stray whitespace etc.
                        var key = NormalizeName(cached.Market);
                        dict[key] = (cached.PnL, file);
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"[Cache] Failed to parse {Path.GetFileName(file)}: {ex.Message}\n");
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[Cache] {ex.Message}\n");
            }
            return dict;
        }

        /// <summary>Central place to normalize market names so runner+cache match.</summary>
        private static string NormalizeName(string name) =>
            (name ?? string.Empty).Trim();

        // ======= selection helpers =======
        private void SelectAll()
        {
            foreach (var r in MarketResults) r.IsSelected = true;
        }
        private void SelectNa()
        {
            foreach (var r in MarketResults) r.IsSelected = double.IsNaN(r.PnL);
        }
        private void SelectPnLNonZero()
        {
            foreach (var r in MarketResults) r.IsSelected = Math.Abs(r.PnL) > double.Epsilon;
        }

        // ======= runners =======
        private async Task RunBollingerAsync()
        {
            await RunWithSelectionAsync(async selected =>
                await _runner.RunWeightsForGuiAsync(writeToFile: true, marketsToRun: selected));
        }
        private async Task RunTrainingAsync()
        {
            await RunWithSelectionAsync(async selected =>
                await _runner.RunMultipleAllStrategiesForGuiAsync(writeToFile: true, marketsToRun: selected));
        }
        private async Task RunWithSelectionAsync(Func<List<string>, Task> run)
        {
            var selected = MarketResults.Where(m => m.IsSelected).Select(m => m.Market).Distinct().ToList();
            if (selected.Count == 0) { AppendLog("No markets selected.\n"); return; }

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            Status = "Running…";
            try
            {
                await run(selected);
                Status = "Done.";
            }
            catch (OperationCanceledException) { Status = "Canceled."; }
            catch (Exception ex) { AppendLog($"[Run] {ex.Message}\n"); Status = "Error."; }
            finally { this.RaisePropertyChanged(nameof(TotalPnLText)); }
        }

        // ======= chart =======
        private async Task LoadChartForSelectionAsync()
        {
            var sel = SelectedMarket;
            if (sel is null) { IsChartVisible = false; return; }

            var file = Path.Combine(CacheDir, $"{sel.Market}.json");
            if (!File.Exists(file)) { IsChartVisible = false; return; }

            try
            {
                var json = await File.ReadAllTextAsync(file);
                var data = JsonSerializer.Deserialize<CachedMarketData>(json);
                if (data is null) { IsChartVisible = false; return; }

                BuildChart(sel.Market, data);
                IsChartVisible = true;
            }
            catch (Exception ex)
            {
                AppendLog($"[Chart] {ex.Message}\n");
                IsChartVisible = false;
            }
        }

        private void BuildChart(string market, CachedMarketData data)
        {
            ChartSeries.Clear();

            if (data.BidPoints.Count == 0 && data.AskPoints.Count == 0)
            {
                XAxes = new[] { new Axis { Labeler = v => v.ToString(CultureInfo.InvariantCulture), MinStep = 1 } };
                YAxes = new[] { new Axis { Labeler = v => v.ToString("F2", CultureInfo.InvariantCulture), MinStep = 0.01 } };
                return;
            }

            var allTimes = data.BidPoints.Select(p => p.Date).Concat(data.AskPoints.Select(p => p.Date)).ToList();
            var minDate = allTimes.Min();
            double X(DateTime t) => (t - minDate).TotalMinutes;

            var mid = new List<ObservablePoint>();
            var count = Math.Min(data.BidPoints.Count, data.AskPoints.Count);
            for (int i = 0; i < count; i++)
            {
                var b = data.BidPoints[i];
                var a = data.AskPoints[i];
                mid.Add(new ObservablePoint(X(b.Date), (b.Price + a.Price) / 2.0));
            }

            ChartSeries.Add(new LineSeries<ObservablePoint>
            {
                Values = mid,
                Name = $"{market} Mid",
                Stroke = new SolidColorPaint(SKColors.Black) { StrokeThickness = 1.5f },
                GeometryFill = null,
                GeometryStroke = null,
                AnimationsSpeed = TimeSpan.Zero
            });

            if (data.BuyPoints.Any())
            {
                ChartSeries.Add(new ScatterSeries<ObservablePoint>
                {
                    Values = data.BuyPoints.ConvertAll(p => new ObservablePoint(X(p.Date), p.Price)),
                    Name = "Buys",
                    Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 2 },
                    Fill = new SolidColorPaint(SKColors.Green),
                    GeometrySize = 9,
                    AnimationsSpeed = TimeSpan.Zero
                });
            }

            if (data.SellPoints.Any())
            {
                ChartSeries.Add(new ScatterSeries<ObservablePoint>
                {
                    Values = data.SellPoints.ConvertAll(p => new ObservablePoint(X(p.Date), p.Price)),
                    Name = "Sells",
                    Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 2 },
                    Fill = new SolidColorPaint(SKColors.Red),
                    GeometrySize = 9,
                    AnimationsSpeed = TimeSpan.Zero
                });
            }

            if (data.EventPoints.Any())
            {
                ChartSeries.Add(new ScatterSeries<ObservablePoint>
                {
                    Values = data.EventPoints.ConvertAll(p => new ObservablePoint(X(p.Date), p.Price)),
                    Name = "Events",
                    Stroke = new SolidColorPaint(SKColors.Purple) { StrokeThickness = 2 },
                    Fill = new SolidColorPaint(SKColors.Purple),
                    GeometrySize = 7,
                    AnimationsSpeed = TimeSpan.Zero
                });
            }

            if (data.IntendedLongPoints.Any())
            {
                ChartSeries.Add(new ScatterSeries<ObservablePoint>
                {
                    Values = data.IntendedLongPoints.ConvertAll(p => new ObservablePoint(X(p.Date), p.Price)),
                    Name = "Intended Long",
                    Stroke = new SolidColorPaint(SKColors.LightGreen) { StrokeThickness = 2 },
                    Fill = new SolidColorPaint(SKColors.LightGreen),
                    GeometrySize = 10,
                    AnimationsSpeed = TimeSpan.Zero
                });
            }

            if (data.IntendedShortPoints.Any())
            {
                ChartSeries.Add(new ScatterSeries<ObservablePoint>
                {
                    Values = data.IntendedShortPoints.ConvertAll(p => new ObservablePoint(X(p.Date), p.Price)),
                    Name = "Intended Short",
                    Stroke = new SolidColorPaint(SKColors.Orange) { StrokeThickness = 2 },
                    Fill = new SolidColorPaint(SKColors.Orange),
                    GeometrySize = 10,
                    AnimationsSpeed = TimeSpan.Zero
                });
            }

            XAxes = new ICartesianAxis[]
            {
                new Axis
                {
                    Labeler = v => (minDate + TimeSpan.FromMinutes(v)).ToString("yyyy-MM-dd HH:mm"),
                    UnitWidth = 1, MinStep = 1
                }
            };
            YAxes = new ICartesianAxis[]
            {
                new Axis
                {
                    Labeler = v => v.ToString("F2", CultureInfo.InvariantCulture),
                    MinStep = 0.01
                }
            };
        }

        private void LogHiddenMarkets()
        {
            if (UnknownCachedMarkets.Count == 0)
            {
                AppendLog("No hidden cached markets.\n");
                return;
            }

            AppendLog($"Hidden cached markets ({UnknownCachedMarkets.Count}):\n");
            foreach (var m in UnknownCachedMarkets.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                AppendLog($"  • {m}\n");

            AppendLog("\nTips: If these are valid, either enable 'Include unknown cached' to append them,\n" +
                      "or ensure your simulator returns these names from GetSnapshotGroupNames().\n" +
                      "Name matching is case-insensitive and trimmed.\n\n");
        }

        private void AppendLog(string message)
        {
            Dispatcher.UIThread.Post(() => Output += message);
        }
    }
}
