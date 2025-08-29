// SimulatorTests.cs
// Updated to avoid redundant data loads for markets, remove parallel operations, and maintain separate strategy set methods

using KalshiBotData.Data;
using KalshiBotData.Data.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SmokehouseBot.Configuration;
using SmokehouseBot.Management;
using SmokehouseBot.Management.Interfaces;
using SmokehouseBot.Services;
using SmokehouseBot.Services.Interfaces;
using SmokehouseDTOs;
using SmokehouseDTOs.Data;
using TradingStrategies;
using TradingStrategies.Classification;
using TradingStrategies.Classification.Interfaces;
using TradingStrategies.Configuration;
using Microsoft.EntityFrameworkCore;
using static TradingStrategies.Trading.Overseer.ReportGenerator;
using static SmokehouseInterfaces.Enums.StrategyEnums;
using TradingSimulator.Strategies;
using TradingStrategies.Strategies;
using TradingStrategies.Trading.Helpers;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using TradingSimulator.TestObjects;
using static TradingStrategies.Strategies.Strats.BollingerBreakout;
using System.Threading;

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


        private List<PricePoint> DetectDiscrepancies(
            List<MarketSnapshot> s,
            double relativeSlack = 1.5,
            double averagingWindowMin = 5.0,
            int minAbsChangeToFlag = 500,
            int minAbsChangeOnZeroVelocity = 100,
            double shortIntervalExponent = 0.5,
            double gapThresholdMin = 1.5)  // Threshold for detecting gaps (e.g., >1.5 min for ~1-min snapshots)
        {
            var outPts = new List<PricePoint>();
            if (s == null || s.Count <= 1) return outPts;

            for (int i = 1; i < s.Count; i++)
            {
                var curr = s[i];
                var prev = s[i - 1];

                // Only flag on mature snapshots, but allow immature ones to contribute to rolling calculations
                if (!curr.ChangeMetricsMature) continue;

                double dtMin = (curr.Timestamp - prev.Timestamp).TotalMinutes;
                if (dtMin <= 0) continue;  // Skip invalid timestamps

                // Actual raw deltas (cents)
                double dy = curr.TotalOrderbookDepth_Yes - prev.TotalOrderbookDepth_Yes;
                double dn = curr.TotalOrderbookDepth_No - prev.TotalOrderbookDepth_No;

                // Instantaneous expected from actual velocity fields ($/min)
                double vYes = curr.VelocityPerMinute_Top_Yes_Bid + curr.VelocityPerMinute_Bottom_Yes_Bid;
                double vNo = curr.VelocityPerMinute_Top_No_Bid + curr.VelocityPerMinute_Bottom_No_Bid;
                double expYes = vYes * dtMin;  // $/min * min = $
                double expNo = vNo * dtMin;    // $/min * min = $

                // Instantaneous observed from actual deltas ($/min)
                double obsYes = dy / (100.0 * dtMin);
                double obsNo = dn / (100.0 * dtMin);

                // Rolling window setup: time-bound, using actual data only (no decay)
                DateTime windowStart = curr.Timestamp.AddMinutes(-averagingWindowMin);
                double windowDt = 0.0;
                double windowDy = 0.0;
                double windowDn = 0.0;
                double windowVYesSum = 0.0;  // Aggregate expected ($/min * min = $)
                double windowVNoSum = 0.0;
                string gapNote = string.Empty;

                // Start with current pair's actual data
                bool hasGapInCurrent = dtMin > gapThresholdMin;
                if (hasGapInCurrent)
                {
                    gapNote = $"Gap of {dtMin:0.##} min detected; isolated change rolling off from {prev.Timestamp:yyyy-MM-ddTHH:mm:ss.fffffffK}.";
                }
                else
                {
                    windowDy += dy;
                    windowDn += dn;
                    windowVYesSum += vYes * dtMin;
                    windowVNoSum += vNo * dtMin;
                    windowDt += dtMin;
                }

                // Extend backward using actual prior pairs, but stop at gaps (immature snapshots can contribute)
                int j = i - 1;
                while (j > 0 && s[j].Timestamp >= windowStart)
                {
                    var priorPrev = s[j - 1];
                    double priorDt = (s[j].Timestamp - priorPrev.Timestamp).TotalMinutes;
                    if (priorDt > gapThresholdMin)
                    {
                        gapNote = $"Gap of {priorDt:0.##} min detected; change rolling off from {priorPrev.Timestamp:yyyy-MM-ddTHH:mm:ss.fffffffK}.";
                        break;  // Exclude pre-gap data
                    }

                    double priorVYes = s[j].VelocityPerMinute_Top_Yes_Bid + s[j].VelocityPerMinute_Bottom_Yes_Bid;
                    double priorVNo = s[j].VelocityPerMinute_Top_No_Bid + s[j].VelocityPerMinute_Bottom_No_Bid;

                    double priorDy = s[j].TotalOrderbookDepth_Yes - priorPrev.TotalOrderbookDepth_Yes;
                    double priorDn = s[j].TotalOrderbookDepth_No - priorPrev.TotalOrderbookDepth_No;

                    windowDy += priorDy;
                    windowDn += priorDn;
                    windowVYesSum += priorVYes * priorDt;
                    windowVNoSum += priorVNo * priorDt;
                    windowDt += priorDt;

                    j--;
                }

                // Rolling observed (actual deltas / time, $/min) and expected ($/min, averaged over window)
                double rollObsYes5m = (windowDt > 0) ? (windowDy / 100.0) / windowDt : 0.0;
                double rollObsNo5m = (windowDt > 0) ? (windowDn / 100.0) / windowDt : 0.0;
                double rollExpYes = (windowDt > 0) ? windowVYesSum / windowDt : 0.0;
                double rollExpNo = (windowDt > 0) ? windowVNoSum / windowDt : 0.0;

                // Full window rolling observed ($/min)
                double rollObsYesWin = (windowDt > 0) ? (windowDy / 100.0) / windowDt : 0.0;
                double rollObsNoWin = (windowDt > 0) ? (windowDn / 100.0) / windowDt : 0.0;

                // Scale only for short valid windows
                double scale = 1.0;
                if (windowDt > 0 && windowDt < averagingWindowMin)
                {
                    scale = Math.Pow(averagingWindowMin / windowDt, shortIntervalExponent);
                }

                // Rolling thresholds and discrepancy checks (using cents for deltas)
                double windowExpYesCents = windowVYesSum * 100;
                double windowExpNoCents = windowVNoSum * 100;
                double thrYes = Math.Abs(windowExpYesCents) * relativeSlack * scale;
                double thrNo = Math.Abs(windowExpNoCents) * relativeSlack * scale;
                bool discYes = (windowDt > 0) && Math.Abs(windowDy - windowExpYesCents) > Math.Max(thrYes, minAbsChangeToFlag);
                bool discNo = (windowDt > 0) && Math.Abs(windowDn - windowExpNoCents) > Math.Max(thrNo, minAbsChangeToFlag);

                // Zero-velocity checks (instantaneous, using actual data; only on mature snapshots)
                bool zeroVelYesDisc = (vYes == 0 && Math.Abs(dy) >= minAbsChangeOnZeroVelocity);
                bool zeroVelNoDisc = (vNo == 0 && Math.Abs(dn) >= minAbsChangeOnZeroVelocity);
                if (zeroVelYesDisc) discYes = true;
                if (zeroVelNoDisc) discNo = true;

                // Flag if discrepancy (rolling or zero-velocity)
                if (discYes || discNo)
                {
                    // Collect last 7 snapshots for reporting (or fewer)
                    var lastSnapshots = s.Skip(Math.Max(0, i - 6)).Take(7).ToList();

                    // Generate memo with original-like reporting
                    string memo = GenerateMemo(curr, lastSnapshots, windowDt, windowDy, windowDn, rollExpYes, rollExpNo, rollObsYes5m, rollObsNo5m, rollObsYesWin, rollObsNoWin, gapNote, averagingWindowMin, scale, shortIntervalExponent, zeroVelYesDisc, zeroVelNoDisc);

                    outPts.Add(new PricePoint(curr.Timestamp, (curr.BestYesBid + curr.BestYesAsk) / 2.0, memo));

                    // Separate logging to file (similar to original AppendDiscrepancyLog)
                    AppendDiscrepancyLog(curr.MarketTicker ?? "UnknownMarket", memo);
                }
            }

            return outPts;
        }

        private string GenerateMemo(
            MarketSnapshot curr,
            List<MarketSnapshot> lastSnapshots,
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
            double averagingWindowMin,
            double scale,
            double shortIntervalExponent,
            bool zeroVelYesDisc,
            bool zeroVelNoDisc)
        {
            var sb = new System.Text.StringBuilder();

            // Header with type (similar to original)
            string discType = zeroVelYesDisc ? "[YES ZERO-VELOCITY]" : zeroVelNoDisc ? "[NO ZERO-VELOCITY]" : "[DISCREPANCY]";
            sb.AppendLine($"{discType} {curr.Timestamp:yyyy-MM-ddTHH:mm:ss.fffffffK}");

            // Window summary
            int steps = lastSnapshots.Count - 1;  // Approximate steps in window
            sb.AppendLine($"Window: size={steps} steps, duration={windowDt:0.##} min");

            // Gap note if any
            if (!string.IsNullOrEmpty(gapNote))
            {
                sb.AppendLine(gapNote);
            }

            // Table of last snapshots (with padded columns for alignment)
            sb.AppendLine("Last 7 snapshots (oldest first):");

            // Define column widths (adjust as needed for your data; positive for right-align, negative for left-align)
            string headerFormat = "{0,-35} {1,8} {2,18} {3,20} {4,21} {5,12} {6,17} {7,19} {8,20} {9,12}";
            string header = string.Format(headerFormat,
                "Timestamp (UTC)", "WinMin", "RollExpYes($/min)", "RollObsYes_5m($/min)", "RollObsYes_Win($/min)",
                "YesDepth(¢)", "RollExpNo($/min)", "RollObsNo_5m($/min)", "RollObsNo_Win($/min)", "NoDepth(¢)");
            sb.AppendLine("    " + header);

            string rowFormat = "{0,-35} {1,8:0.##} {2,18:0.##} {3,20:0.##} {4,21:0.##} {5,12:0.##} {6,17:0.##} {7,19:0.##} {8,20:0.##} {9,12:0.##}";
            foreach (var snap in lastSnapshots)
            {
                // For each snapshot, compute its local values for reporting (using actual data)
                double localVYes = snap.VelocityPerMinute_Top_Yes_Bid + snap.VelocityPerMinute_Bottom_Yes_Bid;
                double localVNo = snap.VelocityPerMinute_Top_No_Bid + snap.VelocityPerMinute_Bottom_No_Bid;
                // Note: RollObs_5m and Win use the end-window values for consistency with original; adjust if per-row rolls needed
                double winMin = (curr.Timestamp - snap.Timestamp).TotalMinutes;  // Approximate WinMin

                string row = string.Format(rowFormat,
                    snap.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffffffK"), winMin, localVYes, rollObsYes5m, rollObsYesWin,
                    snap.TotalOrderbookDepth_Yes, localVNo, rollObsNo5m, rollObsNoWin, snap.TotalOrderbookDepth_No);
                sb.AppendLine("    " + row);
            }

            // Rolling math section (using actual data)
            sb.AppendLine("Rolling math (end snapshot):");
            sb.AppendLine($"  5-min: ΔYes(¢)={windowDy:0.##} ⇒ {rollObsYes5m:0.##} $/min, ΔNo(¢)={windowDn:0.##} ⇒ {rollObsNo5m:0.##} $/min");
            sb.AppendLine($"  Window: ΔYes(¢)={windowDy:0.##} ⇒ {rollObsYesWin:0.##} $/min, ΔNo(¢)={windowDn:0.##} ⇒ {rollObsNoWin:0.##} $/min (Σdt={windowDt:0.##} min)");
            sb.AppendLine($"  Expected (HUD-scale): Yes={rollExpYes:0.##} $/min, No={rollExpNo:0.##} $/min (ExpScale={(int)scale})");

            // Computation details
            sb.AppendLine("Computation details:");
            sb.AppendLine($"    Short-Window Scale = (Window Minutes<{averagingWindowMin:0.##}? ( {averagingWindowMin:0.##}/Window Minutes )^{shortIntervalExponent:0.###} : 1) = {scale:0.######}");
            sb.AppendLine($"    Expected Yes ΔDepth (¢) = Sum(Expected Yes Flow×min)*100 = {rollExpYes * windowDt:0.######}*100 = {windowDy:0.##} c");  // Adjusted for example; refine as needed
            sb.AppendLine($"    Observed Yes ΔDepth (¢) = currY-oldY = {windowDy:0.##}");
            sb.AppendLine($"MID={(curr.BestYesBid + curr.BestYesAsk) / 2.0:0.##}");

            return sb.ToString();
        }

        // Separate logging method (reintroduced similar to original)
        private void AppendDiscrepancyLog(string marketTicker, string memo)
        {
            string logPath = Path.Combine(_cacheDirectory, "discrepancies.log");  // Or a per-market file if preferred
            File.AppendAllText(logPath, $"Market: {marketTicker}\n{memo}\n\n");
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