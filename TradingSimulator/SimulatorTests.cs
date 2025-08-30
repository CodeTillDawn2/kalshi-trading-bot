// SimulatorTests.cs
// Updated to avoid redundant data loads for markets, remove parallel operations, and maintain separate strategy set methods

using KalshiBotData.Data;
using KalshiBotData.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework.Constraints;
using SmokehouseBot.Configuration;
using SmokehouseBot.Management;
using SmokehouseBot.Management.Interfaces;
using SmokehouseBot.Services;
using SmokehouseBot.Services.Interfaces;
using SmokehouseDTOs;
using SmokehouseDTOs.Data;
using System.Text.Json;
using TradingSimulator.Strategies;
using TradingSimulator.TestObjects;
using TradingStrategies;
using TradingStrategies.Classification;
using TradingStrategies.Classification.Interfaces;
using TradingStrategies.Configuration;
using TradingStrategies.Strategies;
using TradingStrategies.Trading.Helpers;
using static SmokehouseInterfaces.Enums.StrategyEnums;

namespace TradingSimulator.Simulator
{
    public delegate void TradingStrategyFunc<T>(T currentData, T previousData, SnapshotConfig config, TradingContext context);

    public class TradingContext
    {
        public Dictionary<string, double> SharedVariables { get; set; } = new Dictionary<string, double>();
        public TradingDecision Decision { get; set; } = new TradingDecision();
    }

    [TestFixture]
    public class SimulatorTests
    {
        private ITradingSnapshotService _snapshotService;
        private ISnapshotPeriodHelper _snapshotPeriodHelper;
        private IServiceFactory _serviceFactory;
        private TradingOverseer _overseer;
        private Mock<ILogger<ITradingSnapshotService>> _snapshotLoggerMock;
        private Mock<ILogger<IInterestScoreService>> _interestScoreLoggerMock;
        private Mock<ILogger<TradingStrategy<MarketSnapshot>>> _strategyLoggerMock;
        private IOptions<SnapshotConfig> _snapshotOptions;
        private IOptions<TradingConfig> _tradingOptions;
        private IServiceScopeFactory _scopeFactory;
        private IKalshiBotContext _dbContext;
        private MarketAnalysisHelper _marketAnalysisHelper;
        private IOptions<ExecutionConfig> _executionConfig;
        private HashSet<string> _processedMarkets;
        private readonly string _cacheDirectory = Path.Combine("..", "..", "..", "..", "..", "TestingOutput");
        private Mock<ILogger<SqlDataService>> _sqlLoggerMock;
        public event Action<string> OnTestProgress;
        public event Action<string, double> OnProfitLossUpdate;
        public event Action<string> OnMarketProcessed;

        private SqlDataService _sqlDataService;

        // SimulatorTests.cs
        [SetUp]
        public void Setup()
        {
            var basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "SmokehouseBot"));
            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.local.json", optional: false, reloadOnChange: false)
                .Build();

            _snapshotLoggerMock = new Mock<ILogger<ITradingSnapshotService>>();
            _serviceFactory = new Mock<IServiceFactory>().Object;
            _strategyLoggerMock = new Mock<ILogger<TradingStrategy<MarketSnapshot>>>();
            _interestScoreLoggerMock = new Mock<ILogger<IInterestScoreService>>();

            var snapshotConfig = config.GetSection("Snapshots").Get<SnapshotConfig>();
            var tradingConfig = config.GetSection("TradingConfig").Get<TradingConfig>();
            _snapshotOptions = Options.Create(snapshotConfig);
            _tradingOptions = Options.Create(tradingConfig);
            _executionConfig = Options.Create(config.GetSection("Execution").Get<ExecutionConfig>());

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddDbContext<KalshiBotContext>(options => options.UseSqlServer(config.GetConnectionString("DefaultConnection")));
            services.AddScoped<IKalshiBotContext>(sp => sp.GetRequiredService<KalshiBotContext>());
            var serviceProvider = services.BuildServiceProvider();

            _scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

            // FIX: init order — construct helpers AFTER these are ready
            _snapshotPeriodHelper = new SnapshotPeriodHelper();
            _snapshotService = new TradingSnapshotService(_snapshotLoggerMock.Object, _snapshotOptions, _tradingOptions, _scopeFactory);
            _overseer = new TradingOverseer(_scopeFactory, _snapshotService);
            _marketAnalysisHelper = new MarketAnalysisHelper(_scopeFactory, _snapshotPeriodHelper, _snapshotService, _executionConfig);

            _dbContext = serviceProvider.GetRequiredService<IKalshiBotContext>();
            _sqlLoggerMock = new Mock<ILogger<SqlDataService>>();
            _sqlDataService = new SqlDataService(config, _sqlLoggerMock.Object);

            _processedMarkets = new HashSet<string>();
            Directory.CreateDirectory(_cacheDirectory); // ensure output dir exists

            OnTestProgress?.Invoke("Setup completed.");
        }




        public void SetProcessedMarkets(List<string> markets)
        {
            _processedMarkets = new HashSet<string>(markets);
        }

        private static void SimplePriceDropStrategy(MarketSnapshot current, MarketSnapshot previous, SnapshotConfig config, TradingContext context)
        {
            var currentBid = current.BestYesBid;
            var previousBid = previous.BestYesBid;
            var priceChangePercent = ((double)currentBid - previousBid) / previousBid * 100;
            context.SharedVariables["PriceChangePercent"] = priceChangePercent;
            if (priceChangePercent <= -5.0)
                context.Decision.AddSignal("PriceDrop", 1.0);
        }

        private static void SimplePriceRiseStrategy(MarketSnapshot current, MarketSnapshot previous, SnapshotConfig config, TradingContext context)
        {
            var currentBid = current.BestYesBid;
            var previousBid = previous.BestYesBid;
            var priceChangePercent = ((double)currentBid - previousBid) / previousBid * 100;
            context.SharedVariables["PriceChangePercent"] = priceChangePercent;
            if (priceChangePercent >= 5.0)
                context.Decision.AddSignal("PriceRise", 1.0);
        }

        public void EnsureInitialized()
        {
            if (_scopeFactory == null) Setup();
        }

        private static IEnumerable<(string Name, TradingStrategyFunc<MarketSnapshot> Func)> GetTradingStrategies()
        {
            yield return ("SimplePriceDropStrategy", SimplePriceDropStrategy);
            yield return ("SimplePriceRiseStrategy", SimplePriceRiseStrategy);
        }

        // SimulatorTests.cs
        private async Task<List<SnapshotGroupDTO>> GetFilteredSnapshotGroupsAsync(
            IKalshiBotContext context, int? maxGroups, List<string>? marketsToRun)
        {
            var groups = await context.GetSnapshotGroups_cached(maxGroups: maxGroups).ConfigureAwait(false);
            var filtered = new List<SnapshotGroupDTO>();
            foreach (var g in groups)
            {
                var recorded = g.EndTime - g.StartTime;
                if (recorded.TotalHours < 1) continue;              // FIX: TotalHours
                if (marketsToRun != null && !marketsToRun.Contains(g.MarketTicker)) continue;
                filtered.Add(g);
            }
            return filtered;
        }


        private async Task<(double finalPnL, List<PricePoint> bidPoints, List<PricePoint> askPoints,
         List<PricePoint> buyPoints, List<PricePoint> sellPoints, List<PricePoint> exitPoints,
         List<PricePoint> eventPoints, List<PricePoint> intendedLongPoints, List<PricePoint> intendedShortPoints,
         List<PricePoint> discrepancyPoints)>
     ProcessMarketAsync(
         string marketTicker,
         List<MarketSnapshot> marketSnapshots,
         Dictionary<MarketType, List<Strategy>> strategiesDict,
         IServiceScopeFactory scopeFactory,
         string progressPrefix = "",
         bool writeToFile = false,
         SnapshotGroupDTO? group = null,
         bool ignoreProcessedCache = false)
        {
            if (!ignoreProcessedCache && _processedMarkets.Contains(marketTicker))
            {
                OnTestProgress?.Invoke($"{progressPrefix}Skipping cached market: {marketTicker}");
                return (0, null, null, null, null, null, null, null, null, null);
            }
            OnTestProgress?.Invoke($"{progressPrefix}Processing market: {marketTicker}");

            try
            {
                var helper = new MarketTypeHelper();
                foreach (var s in marketSnapshots) s.MarketType = helper.GetMarketType(s).ToString();

                // NEW: Detect discrepancies
                var discrepancyPoints = DetectDiscrepancies(marketSnapshots);
                OnTestProgress?.Invoke($"{progressPrefix}Detected {discrepancyPoints.Count} orderbook discrepancies in {marketTicker}.");

                var scenario = new Scenario(strategiesDict);
                var pathData = await Task.Run(() => _overseer.TestScenario(scenario, marketSnapshots, writeToFile, 100, group));

                if (pathData.Any())
                {
                    var bestPath = pathData.OrderByDescending(p => p.performance.Equity).First();
                    var eventLogs = bestPath.events;
                    var finalPnL = bestPath.performance.PnL;

                    var bidPoints = marketSnapshots.Select(s => new PricePoint(s.Timestamp, s.BestYesBid, " Best Bid")).ToList();
                    var askPoints = marketSnapshots.Select(s => new PricePoint(s.Timestamp, s.BestYesAsk, " Best Ask")).ToList();

                    var buyPoints = new List<PricePoint>();
                    var sellPoints = new List<PricePoint>();
                    var exitPoints = new List<PricePoint>();
                    var intendedLongPoints = new List<PricePoint>();
                    var intendedShortPoints = new List<PricePoint>();

                    int prevPos = 0;
                    foreach (var ev in eventLogs)
                    {
                        string prefix = " ";

                        // trade markers (existing)
                        if (ev.Position != prevPos)
                        {
                            if (ev.Position > prevPos)
                                buyPoints.Add(new PricePoint(ev.Timestamp, ev.BestYesAsk, prefix + ev.Memo));
                            else
                                sellPoints.Add(new PricePoint(ev.Timestamp, ev.BestYesBid, prefix + ev.Memo));
                        }
                        else
                        {
                            if (ev.Action == "Long")
                                intendedLongPoints.Add(new PricePoint(ev.Timestamp, ev.BestYesAsk, prefix + "Intended Long: " + ev.Memo));
                            else if (ev.Action == "Short")
                                intendedShortPoints.Add(new PricePoint(ev.Timestamp, ev.BestYesBid, prefix + "Intended Short: " + ev.Memo));
                        }

                        // NEW: explicit exits (close long at bid; close short at ask)
                        bool isExit = string.Equals(ev.Action, "Exit", StringComparison.OrdinalIgnoreCase)
                                      || (prevPos != 0 && ev.Position == 0);
                        if (isExit)
                        {
                            if (prevPos > 0)
                                exitPoints.Add(new PricePoint(ev.Timestamp, ev.BestYesBid, prefix + "Exit Long: " + ev.Memo));
                            else if (prevPos < 0)
                                exitPoints.Add(new PricePoint(ev.Timestamp, ev.BestYesAsk, prefix + "Exit Short: " + ev.Memo));
                        }

                        prevPos = ev.Position;
                    }

                    var eventPoints = eventLogs
                        .Select(ev => new PricePoint(ev.Timestamp, (ev.BestYesBid + ev.BestYesAsk) / 2.0, " " + ev.Memo))
                        .ToList();

                    return (finalPnL, bidPoints, askPoints, buyPoints, sellPoints, exitPoints, eventPoints, intendedLongPoints, intendedShortPoints, discrepancyPoints);
                }
            }
            finally
            {
                OnTestProgress?.Invoke($"{progressPrefix}Cleared memory for market: {marketTicker}");
            }
            return (0, null, null, null, null, null, null, null, null, null);
        }


        // Returns rolling averages and aggregated depth deltas across the window.
        // Signature now returns windowDy, windowDn for correct 5‑minute window calculations.
        private (double RollObsYes5m, double RollObsNo5m, double RollObsYesWin, double RollObsNoWin,
                double WindowDt, string GapNote, double WindowDy, double WindowDn)
            ComputeRollingObs(MarketSnapshot curr, List<MarketSnapshot> fullSnapshots,
                              double averagingWindowMin, double gapThresholdMin)
        {
            int currIndex = fullSnapshots.IndexOf(curr);
            if (currIndex < 1) return (0, 0, 0, 0, 0, "Insufficient data", 0, 0);

            DateTime windowStart = curr.Timestamp.AddMinutes(-averagingWindowMin);
            double windowDt = 0.0;
            double windowDy = 0.0;
            double windowDn = 0.0;
            string gapNote = string.Empty;

            int j = currIndex;
            while (j > 0 && fullSnapshots[j - 1].Timestamp >= windowStart)
            {
                var prev = fullSnapshots[j - 1];
                double pairDt = (fullSnapshots[j].Timestamp - prev.Timestamp).TotalMinutes;
                if (pairDt > gapThresholdMin)
                {
                    gapNote = $"Gap of {pairDt:0.##} min detected; change rolling off from {prev.Timestamp:yyyy-MM-ddTHH:mm:ss.fffffffK}.";
                    break;
                }
                if (prev.Timestamp < windowStart) break;

                double dy = fullSnapshots[j].TotalOrderbookDepth_Yes - prev.TotalOrderbookDepth_Yes;
                double dn = fullSnapshots[j].TotalOrderbookDepth_No  - prev.TotalOrderbookDepth_No;

                windowDy += dy;
                windowDn += dn;
                windowDt += pairDt;
                j--;
            }

            double rollObsYes5m = (windowDt > 0) ? (windowDy / 100.0) / windowDt : 0.0;
            double rollObsNo5m = (windowDt > 0) ? (windowDn  / 100.0) / windowDt : 0.0;
            double rollObsYesWin = rollObsYes5m;
            double rollObsNoWin = rollObsNo5m;

            return (rollObsYes5m, rollObsNo5m, rollObsYesWin, rollObsNoWin, windowDt, gapNote, windowDy, windowDn);
        }


        // Detect discrepancies: destructure the new return values and pass the correct windowDy/windowDn to GenerateMemo.
        private List<PricePoint> DetectDiscrepancies(
            List<MarketSnapshot> s,
            double relativeSlack = 1.5,
            double averagingWindowMin = 5.0,
            int minAbsChangeToFlag = 500,
            int minAbsChangeOnZeroVelocity = 100,
            double shortIntervalExponent = 0.5,
            double gapThresholdMin = 1.5,
            double leakageFactor = 0.05)
        {
            var outPts = new List<PricePoint>();
            if (s == null || s.Count <= 1) return outPts;

            for (int i = 1; i < s.Count; i++)
            {
                var curr = s[i];
                var prev = s[i - 1];
                if (!curr.ChangeMetricsMature) continue;

                double dtMin = (curr.Timestamp - prev.Timestamp).TotalMinutes;
                if (dtMin <= 0) continue;

                double dy = curr.TotalOrderbookDepth_Yes - prev.TotalOrderbookDepth_Yes;
                double dn = curr.TotalOrderbookDepth_No  - prev.TotalOrderbookDepth_No;

                double vYes = curr.VelocityPerMinute_Top_Yes_Bid + curr.VelocityPerMinute_Bottom_Yes_Bid;
                double vNo = curr.VelocityPerMinute_Top_No_Bid  + curr.VelocityPerMinute_Bottom_No_Bid;

                // Compute rolling with aggregated windowDy/windowDn
                var (rollObsYes5m, rollObsNo5m, rollObsYesWin, rollObsNoWin,
                     windowDt, gapNote, windowDy, windowDn) =
                    ComputeRollingObs(curr, s, averagingWindowMin, gapThresholdMin);

                double rollExpYes = vYes;
                double rollExpNo = vNo;

                double scale = 1.0;
                if (windowDt > 0 && windowDt < averagingWindowMin)
                    scale = Math.Pow(averagingWindowMin / windowDt, shortIntervalExponent);

                double toleranceYes = 0.0;
                double toleranceNo = 0.0;
                int oldestIncludedIndex = FindOldestIncludedIndex(i, s, averagingWindowMin, gapThresholdMin);
                if (oldestIncludedIndex > 1)
                {
                    var preSnap = s[oldestIncludedIndex - 1];
                    var prePrevSnap = s[oldestIncludedIndex - 2];
                    double preDt = (preSnap.Timestamp - prePrevSnap.Timestamp).TotalMinutes;
                    if (preDt > 0 && preDt <= gapThresholdMin)
                    {
                        double preDy = preSnap.TotalOrderbookDepth_Yes - prePrevSnap.TotalOrderbookDepth_Yes;
                        double preDn = preSnap.TotalOrderbookDepth_No  - prePrevSnap.TotalOrderbookDepth_No;
                        double preVelYes = preDy / (100.0 * preDt);
                        double preVelNo = preDn / (100.0 * preDt);
                        toleranceYes = Math.Abs(preVelYes) * leakageFactor;
                        toleranceNo  = Math.Abs(preVelNo)  * leakageFactor;
                    }
                }

                double baseThrYes = Math.Abs(rollExpYes) * relativeSlack * scale;
                double thrYes = Math.Max(baseThrYes + toleranceYes, (double)minAbsChangeToFlag / (100.0 * averagingWindowMin));
                double baseThrNo = Math.Abs(rollExpNo)  * relativeSlack * scale;
                double thrNo = Math.Max(baseThrNo  + toleranceNo, (double)minAbsChangeToFlag / (100.0 * averagingWindowMin));

                bool discYes = Math.Abs(rollObsYes5m - rollExpYes) > thrYes;
                bool discNo = Math.Abs(rollObsNo5m  - rollExpNo)  > thrNo;

                bool zeroVelYesDisc = (vYes == 0 && Math.Abs(dy) >= minAbsChangeOnZeroVelocity);
                bool zeroVelNoDisc = (vNo  == 0 && Math.Abs(dn) >= minAbsChangeOnZeroVelocity);
                if (zeroVelYesDisc) discYes = true;
                if (zeroVelNoDisc) discNo  = true;

                if (discYes || discNo)
                {
                    var lastSnapshots = s.Skip(Math.Max(0, i - 6)).Take(7).ToList();
                    string memo = GenerateMemo(
                        curr, lastSnapshots, s,
                        averagingWindowMin, gapThresholdMin,
                        windowDt, windowDy, windowDn,
                        rollExpYes, rollExpNo,
                        rollObsYes5m, rollObsNo5m,
                        rollObsYesWin, rollObsNoWin,
                        gapNote, scale, shortIntervalExponent,
                        zeroVelYesDisc, zeroVelNoDisc,
                        toleranceYes, toleranceNo, leakageFactor);

                    outPts.Add(new PricePoint(curr.Timestamp, (curr.BestYesBid + curr.BestYesAsk) / 2.0, memo));
                    AppendDiscrepancyLog(curr.MarketTicker ?? "UnknownMarket", memo);
                }
            }

            return outPts;
        }


        private void AppendDiscrepancyLog(string marketTicker, string memo)
        {
            string logPath = Path.Combine(_cacheDirectory, "discrepancies.log");  // Or a per-market file if preferred
            File.AppendAllText(logPath, $"Market: {marketTicker}\n{memo}\n\n");
        }

        // Updated helper signature to include gapThresholdMin
        private int FindOldestIncludedIndex(int currIndex, List<MarketSnapshot> s, double averagingWindowMin, double gapThresholdMin)
        {
            DateTime windowStart = s[currIndex].Timestamp.AddMinutes(-averagingWindowMin);
            int j = currIndex - 1;
            while (j > 0 && s[j].Timestamp >= windowStart)
            {
                var priorPrev = s[j - 1];
                double priorDt = (s[j].Timestamp - priorPrev.Timestamp).TotalMinutes;
                if (priorDt > gapThresholdMin) break;
                if (priorPrev.Timestamp < windowStart) break;
                j--;
            }
            return j + 1;  // Oldest included is the current j after loop (adjusted for decrement)
        }
        // Updated report generation: new columns for your rolling averages and the orderbook-derived instantaneous flow.
        private string GenerateMemo(
            MarketSnapshot curr,
            List<MarketSnapshot> lastSnapshots,
            List<MarketSnapshot> fullSnapshots,
            double averagingWindowMin,
            double gapThresholdMin,
            double windowDt,
            double windowDy,
            double windowDn,
            double rollExpYes,
            double rollExpNo,
            double rollObsYes5m,
            double rollObsNo5m,
            double rollObsYesWin,
            double rollObsNoWin,
            string gapNote,
            double scale,
            double shortIntervalExponent,
            bool zeroVelYesDisc,
            bool zeroVelNoDisc,
            double toleranceYes,
            double toleranceNo,
            double leakageFactor)
        {
            var sb = new System.Text.StringBuilder();

            string discType = zeroVelYesDisc ? "[YES ZERO-VELOCITY]" : zeroVelNoDisc ? "[NO ZERO-VELOCITY]" : "[DISCREPANCY]";
            sb.AppendLine($"{discType} {curr.Timestamp:yyyy-MM-ddTHH:mm:ss.fffffffK}");

            int steps = lastSnapshots.Count - 1;
            sb.AppendLine($"Window: size={steps} steps, duration={windowDt:0.##} min");
            if (!string.IsNullOrEmpty(gapNote))
                sb.AppendLine(gapNote);

            sb.AppendLine("Last 7 snapshots (oldest first):");
            string headerFormat = "{0,-35} {1,8} {2,28} {3,32} {4,12} {5,28} {6,32} {7,12}";
            string header = string.Format(headerFormat,
                "Timestamp (UTC)",
                "WinMin",
                "Your Rolling Yes ($/min)",
                "Orderbook Flow Yes (inst $/min)",
                "YesDepth(¢)",
                "Your Rolling No ($/min)",
                "Orderbook Flow No (inst $/min)",
                "NoDepth(¢)");
            sb.AppendLine("    " + header);

            string rowFormat = "{0,-35} {1,8:0.##} {2,28:0.##} {3,32:0.##} {4,12:0.##} {5,28:0.##} {6,32:0.##} {7,12:0.##}";
            foreach (var snap in lastSnapshots)
            {
                // Rolling velocities from the snapshot (user’s data)
                double yourRollingYes = snap.VelocityPerMinute_Top_Yes_Bid + snap.VelocityPerMinute_Bottom_Yes_Bid;
                double yourRollingNo = snap.VelocityPerMinute_Top_No_Bid  + snap.VelocityPerMinute_Bottom_No_Bid;

                // Instantaneous orderbook-derived flow between consecutive snapshots
                double instFlowYes = 0.0, instFlowNo = 0.0;
                int idx = fullSnapshots.IndexOf(snap);
                if (idx > 0)
                {
                    var prevSnap = fullSnapshots[idx - 1];
                    double dtRow = (snap.Timestamp - prevSnap.Timestamp).TotalMinutes;
                    if (dtRow > 0)
                    {
                        double dyRow = snap.TotalOrderbookDepth_Yes - prevSnap.TotalOrderbookDepth_Yes;
                        double dnRow = snap.TotalOrderbookDepth_No  - prevSnap.TotalOrderbookDepth_No;
                        instFlowYes = (dyRow / 100.0) / dtRow;
                        instFlowNo  = (dnRow  / 100.0) / dtRow;
                    }
                }

                double winMin = (curr.Timestamp - snap.Timestamp).TotalMinutes;
                sb.AppendLine("    " + string.Format(rowFormat,
                    snap.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffffffK"),
                    winMin,
                    yourRollingYes,
                    instFlowYes,
                    snap.TotalOrderbookDepth_Yes,
                    yourRollingNo,
                    instFlowNo,
                    snap.TotalOrderbookDepth_No));
            }

            // Rolling orderbook flow summary for the window
            sb.AppendLine("Rolling math (end snapshot - orderbook flow):");
            sb.AppendLine($"  Window: ΔYes(¢)={windowDy:0.##} ⇒ {rollObsYes5m:0.##} $/min, ΔNo(¢)={windowDn:0.##} ⇒ {rollObsNo5m:0.##} $/min (Σdt={windowDt:0.##} min)");

            // If the HUD velocities are zero but the depth-change indicates a flow, infer an implied expected velocity
            double displayExpYes = (Math.Abs(rollExpYes) < 1e-9 && Math.Abs(windowDy) > 0 && windowDt > 0)
                ? (windowDy / 100.0) / windowDt
                : rollExpYes;
            double displayExpNo = (Math.Abs(rollExpNo)  < 1e-9 && Math.Abs(windowDn)  > 0 && windowDt > 0)
                ? (windowDn / 100.0) / windowDt
                : rollExpNo;
            sb.AppendLine($"  Detection Expected (may use implied): Yes={displayExpYes:0.##} $/min, No={displayExpNo:0.##} $/min (ExpScale={scale:0.##})");

            // Details for debugging
            sb.AppendLine("Computation details:");
            sb.AppendLine($"    Short-Window Scale = (Window Minutes<{averagingWindowMin:0.##}? ( {averagingWindowMin:0.##}/Window Minutes )^{shortIntervalExponent:0.###} : 1) = {scale:0.######}");
            sb.AppendLine($"    Expected ΔDepth (¢) = Sum(Expected Flow×min)*100 = {displayExpYes * windowDt:0.######}*100 = {(displayExpYes * windowDt * 100):0.##} c");
            sb.AppendLine($"    Observed ΔDepth (¢) = windowΔ = {windowDy:0.##}");
            sb.AppendLine($"    Edge Tolerance (Leakage Factor={leakageFactor:0.###}): Yes={toleranceYes:0.##} $/min, No={toleranceNo:0.##} $/min");
            sb.AppendLine($"MID={(curr.BestYesBid + curr.BestYesAsk) / 2.0:0.##}");

            if (Math.Abs(rollObsYes5m - displayExpYes) < 1e-6 && Math.Abs(rollObsNo5m - displayExpNo) < 1e-6)
                sb.AppendLine("Resolution hint: Implied==Observed; likely timing skew cancel with adjacent snapshot.");

            return sb.ToString();
        }

        private List<PricePoint> CoalesceDiscrepancies(
    List<PricePoint> pts,
    int maxPairGapMinutes,
    double maxVelocityDiff)
        {
            if (pts == null || pts.Count < 2) return pts;

            var keep = new bool[pts.Count];
            Array.Fill(keep, true);

            // Extract compact metrics from memo lines
            bool TryParseMetrics(string memo, out double obsYes, out double expYes, out double obsNo, out double expNo)
            {
                obsYes = expYes = obsNo = expNo = 0;
                // Parse "Window: ΔYes(¢)=X ⇒ Y $/min, ΔNo(¢)=Z ⇒ W $/min" and "Expected (HUD-scale): Yes=A $/min, No=B $/min"
                // Safe parsing; failures just return false -> no coalescing.
                try
                {
                    var lines = memo.Split('\n');
                    var roll = lines.FirstOrDefault(l => l.StartsWith("  Window:", StringComparison.Ordinal));
                    var exp = lines.FirstOrDefault(l => l.StartsWith("  Expected (HUD-scale):", StringComparison.Ordinal));
                    if (roll == null || exp == null) return false;

                    // Y and W
                    int yi = roll.IndexOf("⇒", StringComparison.Ordinal);
                    int yiEnd = roll.IndexOf("$", yi + 1, StringComparison.Ordinal);
                    int wi = roll.LastIndexOf("⇒", StringComparison.Ordinal);
                    int wiEnd = roll.LastIndexOf("$", StringComparison.Ordinal);
                    if (yi < 0 || yiEnd < 0 || wi < 0 || wiEnd < 0) return false;
                    var yStr = roll.Substring(yi + 1, yiEnd - (yi + 1)).Replace("$/min", "").Trim();
                    var wStr = roll.Substring(wi + 1, wiEnd - (wi + 1)).Replace("$/min", "").Trim();
                    if (!double.TryParse(yStr, out obsYes)) return false;
                    if (!double.TryParse(wStr, out obsNo)) return false;

                    // A and B
                    var parts = exp.Split(new[] { "Yes=", "No=", "$/min" }, StringSplitOptions.RemoveEmptyEntries);
                    // parts like: ["  Expected (HUD-scale): ", "A ", " , ", "B ", " (ExpScale=..."]
                    var nums = parts.Where(p => double.TryParse(p.Trim(), out _))
                                    .Select(p => double.Parse(p.Trim()))
                                    .ToList();
                    if (nums.Count < 2) return false;
                    expYes = nums[0];
                    expNo  = nums[1];
                    return true;
                }
                catch { return false; }
            }

            for (int i = 1; i < pts.Count; i++)
            {
                if (!keep[i]) continue;
                for (int j = i - 1; j >= 0; j--)
                {
                    if (!keep[j]) continue;
                    var dt = (pts[i].Date - pts[j].Date).TotalMinutes;
                    if (dt > maxPairGapMinutes) break;

                    if (TryParseMetrics(pts[i].Memo, out var oYi, out var eYi, out var oNi, out var eNi) &&
                        TryParseMetrics(pts[j].Memo, out var oYj, out var eYj, out var oNj, out var eNj))
                    {
                        // Compare residuals (Observed-Expected) for Yes and No
                        double riY = oYi - eYi, rjY = oYj - eYj;
                        double riN = oNi - eNi, rjN = oNj - eNj;

                        bool cancelsY = Math.Abs(riY + rjY) <= maxVelocityDiff;
                        bool cancelsN = Math.Abs(riN + rjN) <= maxVelocityDiff;

                        if (cancelsY || cancelsN)
                        {
                            keep[i] = false;
                            keep[j] = false;
                            break;
                        }
                    }
                }
            }

            var result = new List<PricePoint>(pts.Count);
            for (int k = 0; k < pts.Count; k++)
                if (keep[k]) result.Add(pts[k]);
            return result;
        }



        private void SaveMarketDataToFile(
            string marketTicker, double finalPnL,
            List<PricePoint> bidPoints, List<PricePoint> askPoints,
            List<PricePoint> buyPoints, List<PricePoint> sellPoints, List<PricePoint> exitPoints,
            List<PricePoint> eventPoints, List<PricePoint> intendedLongPoints, List<PricePoint> intendedShortPoints,
            List<PricePoint> discrepancyPoints,
            string fileNameSuffix = "")
        {
            var cachedData = new CachedMarketData
            {
                Market = marketTicker,
                PnL = finalPnL,
                BidPoints = bidPoints,
                AskPoints = askPoints,
                BuyPoints = buyPoints,
                SellPoints = sellPoints,
                ExitPoints = exitPoints,
                EventPoints = eventPoints,
                IntendedLongPoints = intendedLongPoints,
                IntendedShortPoints = intendedShortPoints,
                DiscrepancyPoints = discrepancyPoints
            };

            var json = JsonSerializer.Serialize(cachedData);
            Directory.CreateDirectory(_cacheDirectory);
            var filePath = Path.Combine(_cacheDirectory, $"{marketTicker}{fileNameSuffix}.json");
            File.WriteAllText(filePath, json);
        }


        public async Task RunSelectedSetForGuiAsync(
            string setKey,
            string weightName,
            bool writeToFile,
            List<string>? marketsToRun = null,
            int? maxGroups = null)
        {
            // map provided setKey -> family
            var family = MapFamilyFromSetKey(setKey);

            // resolve strategies + param sets for that family
            var (strategiesList, paramSets, label) = ResolveFamily(family);

            // find the requested weight set by name (exact, case-insensitive)
            var idx = paramSets.FindIndex(ps =>
                string.Equals(ps.Name, weightName, StringComparison.OrdinalIgnoreCase));
            if (idx < 0)
                throw new InvalidOperationException($"Weight set '{weightName}' not found in {label}.");

            // prep context once
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

            // create DTO for just this one set
            var (name, parameters) = paramSets[idx];
            var dto = new WeightSetDTO
            {
                StrategyName = name,
                Weights = JsonSerializer.Serialize(parameters),
                LastRun = DateTime.UtcNow,
                WeightSetMarkets = new List<WeightSetMarketDTO>()
            };

            // filter groups/markets
            var filteredGroups = await GetFilteredSnapshotGroupsAsync(context, maxGroups, marketsToRun).ConfigureAwait(false);
            var uniqueMarkets = filteredGroups.Select(g => g.MarketTicker).Distinct().ToList();

            OnTestProgress?.Invoke($"{label}/{dto.StrategyName}: 1 strategy set × {uniqueMarkets.Count} markets");

            // NEW: Running counter for discrepancies
            int totalDiscrepancies = 0;

            // iterate markets
            for (int mIdx = 0; mIdx < uniqueMarkets.Count; mIdx++)
            {
                var market = uniqueMarkets[mIdx];
                var marketGroups = filteredGroups.Where(g => g.MarketTicker == market).ToList();

                var allSnapshotData = new List<SnapshotDTO>();
                foreach (var g in marketGroups)
                {
                    var snaps = await context.GetSnapshots_cached(
                        marketTicker: g.MarketTicker,
                        startDate: g.StartTime,
                        endDate: g.EndTime).ConfigureAwait(false);
                    allSnapshotData.AddRange(snaps);
                }
                allSnapshotData = allSnapshotData.OrderBy(x => x.SnapshotDate).ToList();

                var cache = await _snapshotService.LoadManySnapshots(allSnapshotData).ConfigureAwait(false);
                var marketSnapshots = cache
                    .SelectMany(kvp => kvp.Value)
                    .Where(ms => ms != null && ms.Timestamp > DateTime.MinValue && ms.BestYesBid > 0 && ms.BestYesAsk > 0)
                    .OrderBy(ms => ms.Timestamp)
                    .ToList();

                if (marketSnapshots.Count == 0) continue;

                var groupForId = new SnapshotGroupDTO { MarketTicker = market, JsonPath = $"{market}.json" };

                var strategies = strategiesList[idx]; // only the selected set
                OnTestProgress?.Invoke($"[{label}/{dto.StrategyName}] market {market} ({mIdx + 1}/{uniqueMarkets.Count})");

                var (finalPnL, bid, ask, buy, sell, exit, ev, il, ishort, disc) =
                    await ProcessMarketAsync(
                        market, marketSnapshots, strategies, _scopeFactory,
                        progressPrefix: $"[{label}/{dto.StrategyName}] ",
                        writeToFile: writeToFile, group: groupForId,
                        ignoreProcessedCache: true).ConfigureAwait(false);

                totalDiscrepancies += disc?.Count ?? 0;

                if (bid != null)
                {
                    if (writeToFile)
                        SaveMarketDataToFile(market, finalPnL, bid, ask, buy, sell, exit, ev, il, ishort,
                            disc, fileNameSuffix: $"_{label}_{dto.StrategyName}");

                    dto.WeightSetMarkets.Add(new WeightSetMarketDTO
                    {
                        MarketTicker = market,
                        PnL = (decimal)finalPnL,
                        LastRun = DateTime.UtcNow
                    });

                    OnProfitLossUpdate?.Invoke(market, finalPnL);
                }

                OnMarketProcessed?.Invoke(market);
                marketSnapshots.Clear();
                allSnapshotData.Clear();
                cache.Clear();
            }

            // save the one set
            using var saveScope = _scopeFactory.CreateScope();
            var saveContext = saveScope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
            await saveContext.AddOrUpdateWeightSet(dto).ConfigureAwait(false);

            OnTestProgress?.Invoke($"Saved {label}/{dto.StrategyName} ({dto.WeightSetMarkets.Count}/{uniqueMarkets.Count} markets)");
            OnTestProgress?.Invoke($"Total discrepancies across all markets: {totalDiscrepancies} (widespread if >10% of snapshots).");
            OnTestProgress?.Invoke($"{label}/{dto.StrategyName}: completed");
        }

        private StrategyFamily MapFamilyFromSetKey(string setKey)
        {
            if (string.IsNullOrWhiteSpace(setKey)) throw new ArgumentException("setKey is required.", nameof(setKey));
            var k = setKey.Trim().ToLowerInvariant();
            if (k.Contains("breakout")) return StrategyFamily.Breakout;
            if (k.Contains("bollinger")) return StrategyFamily.Bollinger;
            if (k.Contains("flowmo") || k.Contains("flow")) return StrategyFamily.FlowMo;
            if (k.Contains("nothing")) return StrategyFamily.NothingHappens;
            if (k.Contains("momentum")) return StrategyFamily.Momentum;
            // specific short names
            if (k is "b2" or "breakout2") return StrategyFamily.Breakout;
            if (k is "bb" or "bollingerbreakout") return StrategyFamily.Bollinger;
            throw new ArgumentOutOfRangeException(nameof(setKey), $"Unrecognized setKey: {setKey}");
        }


        public async Task RunMultipleAllStrategiesForGuiAsync(
            bool writeToFile,
            int? maxGroups = null,
            List<string>? marketsToRun = null)
        {
            // run all families automatically
            var families = new[] {
                //StrategyFamily.Bollinger,
                //StrategyFamily.FlowMo,
                //StrategyFamily.Breakout,
                StrategyFamily.Momentum,
                //StrategyFamily.NothingHappens
            };

            foreach (var fam in families)
            {
                await RunMultipleForGuiAsync(
                    family: fam,
                    writeToFile: writeToFile,
                    maxGroups: maxGroups,
                    marketsToRun: marketsToRun).ConfigureAwait(false);
            }
        }

        public async Task<HashSet<string>> GetSnapshotGroupNames()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
            return await context.GetSnapshotGroupNames();
        }


        public enum StrategyFamily
        {
            Bollinger,
            FlowMo,
            Breakout,
            NothingHappens,
            Momentum
        }

        private (List<Dictionary<MarketType, List<Strategy>>> Strategies,
                 List<(string Name, object Parameters)> ParamSets,
                 string Label)
        ResolveFamily(StrategyFamily family)
        {
            var helper = new StrategySelectionHelper();

            switch (family)
            {
                case StrategyFamily.Bollinger:
                    return (
                        helper.GetTrainingMappings("Bollinger"),
                        (StrategySelectionHelper.BollingerParameterSets
                            ?? throw new InvalidOperationException("BollingerParameterSets is null."))
                            .Select(ps => (ps.Name, (object)ps.Parameters)).ToList(),
                        "Bollinger"
                    );

                case StrategyFamily.FlowMo:
                    return (
                        helper.GetTrainingMappings("FlowMo"),
                        (StrategySelectionHelper.FlowMomentumParameterSets
                            ?? throw new InvalidOperationException("FlowMomentumParameterSets is null."))
                            .Select(ps => (ps.Name, (object)ps.Parameters)).ToList(),
                        "FlowMo"
                    );

                case StrategyFamily.Breakout:
                    return (
                        helper.GetBreakoutStrategiesForTraining(),
                        (StrategySelectionHelper.BreakoutParameterSets
                            ?? throw new InvalidOperationException("BreakoutParameterSets is null."))
                            .Select(ps => (ps.Name, (object)ps.Parameters)).ToList(),
                        "Breakout"
                    );

                case StrategyFamily.NothingHappens:
                    return (
                        helper.GetNothingEverHappensStrategiesForTraining(),
                        (StrategySelectionHelper.NothingEverHappensParameterSets
                            ?? throw new InvalidOperationException("NothingEverHappensParameterSets is null."))
                            .Select(ps => (ps.Name, (object)ps.Parameters)).ToList(),
                        "NothingHappens"
                    );
                case StrategyFamily.Momentum:
                    return (
                        helper.GetMomentumTradingStrategiesForTraining(),
                        (StrategySelectionHelper.MomentumTradingParameterSets
                            ?? throw new InvalidOperationException("MomentumTradingParameterSets is null."))
                            .Select(ps => (ps.Name, (object)ps.Parameters)).ToList(),
                        "Momentum"
                    );
                default:
                    throw new ArgumentOutOfRangeException(nameof(family));
            }
        }


        public async Task RunMultipleForGuiAsync(
            StrategyFamily family,
            bool writeToFile,
            int? maxGroups = null,
            List<string>? marketsToRun = null)
        {
            var (strategiesList, paramSets, label) = ResolveFamily(family);

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

            var weightSetDtos = new List<WeightSetDTO>(paramSets.Count);
            for (int i = 0; i < paramSets.Count; i++)
            {
                var (name, parameters) = paramSets[i];
                weightSetDtos.Add(new WeightSetDTO
                {
                    StrategyName = name,
                    Weights = JsonSerializer.Serialize(parameters),
                    LastRun = DateTime.UtcNow,
                    WeightSetMarkets = new List<WeightSetMarketDTO>()
                });
            }

            var filteredGroups = await GetFilteredSnapshotGroupsAsync(context, maxGroups, marketsToRun).ConfigureAwait(false);
            var uniqueMarkets = filteredGroups.Select(g => g.MarketTicker).Distinct().ToList();

            OnTestProgress?.Invoke($"{label}: {strategiesList.Count} strategy sets × {uniqueMarkets.Count} markets");

            // NEW: Running counter for discrepancies
            int totalDiscrepancies = 0;

            for (int mIdx = 0; mIdx < uniqueMarkets.Count; mIdx++)
            {
                var market = uniqueMarkets[mIdx];
                var marketGroups = filteredGroups.Where(g => g.MarketTicker == market).ToList();

                var allSnapshotData = new List<SnapshotDTO>();
                foreach (var g in marketGroups)
                {
                    var snaps = await context.GetSnapshots_cached(
                        marketTicker: g.MarketTicker,
                        startDate: g.StartTime,
                        endDate: g.EndTime).ConfigureAwait(false);
                    allSnapshotData.AddRange(snaps);
                }
                allSnapshotData = allSnapshotData.OrderBy(x => x.SnapshotDate).ToList();

                var cache = await _snapshotService.LoadManySnapshots(allSnapshotData).ConfigureAwait(false);
                var marketSnapshots = cache
                    .SelectMany(kvp => kvp.Value)
                    .Where(ms => ms != null && ms.Timestamp > DateTime.MinValue && ms.BestYesBid > 0 && ms.BestYesAsk > 0)
                    .OrderBy(ms => ms.Timestamp)
                    .ToList();

                if (marketSnapshots.Count == 0) continue;

                var groupForId = new SnapshotGroupDTO { MarketTicker = market, JsonPath = $"{market}.json" };

                for (int setIdx = 0; setIdx < strategiesList.Count; setIdx++)
                {
                    var strategies = strategiesList[setIdx];
                    var dto = weightSetDtos[setIdx];

                    OnTestProgress?.Invoke($"[{label}/{dto.StrategyName}] market {market} ({mIdx + 1}/{uniqueMarkets.Count}) set {setIdx + 1}/{strategiesList.Count}");

                    var (finalPnL, bid, ask, buy, sell, exit, ev, il, ishort, disc) =
                        await ProcessMarketAsync(
                            market, marketSnapshots, strategies, _scopeFactory,
                            progressPrefix: $"[{label}/{dto.StrategyName}] ",
                            writeToFile: writeToFile, group: groupForId,
                            ignoreProcessedCache: true).ConfigureAwait(false);

                    totalDiscrepancies += disc?.Count ?? 0;

                    if (bid != null)
                    {
                        if (writeToFile)
                            SaveMarketDataToFile(market, finalPnL, bid, ask, buy, sell, exit, ev, il, ishort,
                                disc, fileNameSuffix: $"_{label}_{dto.StrategyName}");

                        dto.WeightSetMarkets.Add(new WeightSetMarketDTO
                        {
                            MarketTicker = market,
                            PnL = (decimal)finalPnL,
                            LastRun = DateTime.UtcNow
                        });

                        OnProfitLossUpdate?.Invoke(market, finalPnL);
                    }
                }

                OnMarketProcessed?.Invoke(market);
                marketSnapshots.Clear();
                allSnapshotData.Clear();
                cache.Clear();
            }

            foreach (var dto in weightSetDtos)
            {
                using var saveScope = _scopeFactory.CreateScope();
                var saveContext = saveScope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
                await saveContext.AddOrUpdateWeightSet(dto).ConfigureAwait(false);
                OnTestProgress?.Invoke($"Saved {label}/{dto.StrategyName} ({dto.WeightSetMarkets.Count}/{uniqueMarkets.Count} markets)");
            }

            OnTestProgress?.Invoke($"Total discrepancies across all markets: {totalDiscrepancies} (widespread if >10% of snapshots).");
            OnTestProgress?.Invoke($"{label}: all strategy sets completed");
        }
        [TearDown]
        public void TearDown()
        {
            _dbContext.Dispose();
            _sqlDataService.Dispose();
            OnTestProgress?.Invoke("TearDown completed.");
        }
    }
}