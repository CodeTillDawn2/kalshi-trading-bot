using BacklashDTOs;
using BacklashDTOs.Data;
using TradingStrategies;
using static TradingStrategies.Trading.Overseer.ReportGenerator;
using TradingStrategies.Trading.Overseer;
using TradingStrategies.Trading.Helpers;
using System.Text.Json;
using static BacklashInterfaces.Enums.StrategyEnums;
using static TradingStrategies.Trading.Overseer.ReportGenerator;
using TradingSimulator.Simulator;
using Microsoft.Extensions.DependencyInjection;
using TradingStrategies.Strategies;
using System.Threading;

namespace TradingSimulator
{
    /// <summary>
    /// Processes individual markets in the trading simulator by running trading strategies,
    /// analyzing event logs, and generating visualization data points for market analysis.
    /// This class orchestrates the complete market processing pipeline from simulation to data persistence.
    /// </summary>
    public class MarketProcessor
    {
        /// <summary>
        /// The trading overseer responsible for executing trading scenarios and simulations.
        /// </summary>
        private readonly TradingOverseer _overseer;

        /// <summary>
        /// Factory for creating service scopes to resolve dependencies during processing.
        /// </summary>
        private readonly IServiceScopeFactory _scopeFactory;

        /// <summary>
        /// Set of market tickers that have already been processed to avoid duplicate processing.
        /// </summary>
        private readonly HashSet<string> _processedMarkets;

        /// <summary>
        /// Directory path where processed market data is cached for future use.
        /// </summary>
        private readonly string _cacheDirectory;

        /// <summary>
        /// Reporting service for detecting velocity discrepancies in market snapshots.
        /// </summary>
        private readonly SimulatorReporting _simulatorReporting;

        /// <summary>
        /// Timeout in seconds for processing operations to prevent hanging in high-throughput scenarios.
        /// </summary>
        private readonly int _processingTimeoutSeconds;

        /// <summary>
        /// Maximum number of markets to process concurrently in batch operations.
        /// </summary>
        private readonly int _maxConcurrentMarkets;

        /// <summary>
        /// Size of batches for processing multiple markets to reduce memory pressure.
        /// </summary>
        private readonly int _batchSize;

        /// <summary>
        /// Performance metrics for tracking processing rates and queue depths.
        /// </summary>
        private readonly PerformanceMetrics _performanceMetrics;

        /// <summary>
        /// Event raised to report progress during market processing operations.
        /// </summary>
        public event Action<string> OnTestProgress;

        /// <summary>
        /// Initializes a new instance of the MarketProcessor class with required dependencies.
        /// </summary>
        /// <param name="overseer">The trading overseer for running simulations.</param>
        /// <param name="scopeFactory">Factory for creating service scopes.</param>
        /// <param name="processedMarkets">Set of already processed market tickers.</param>
        /// <param name="cacheDirectory">Directory for caching processed data.</param>
        /// <param name="simulatorReporting">Reporting service for discrepancy detection.</param>
        /// <param name="processingTimeoutSeconds">Timeout in seconds for processing operations.</param>
        /// <param name="maxConcurrentMarkets">Maximum number of markets to process concurrently in batch operations.</param>
        /// <param name="batchSize">Size of batches for processing multiple markets to reduce memory pressure.</param>
        public MarketProcessor(
            TradingOverseer overseer,
            IServiceScopeFactory scopeFactory,
            HashSet<string> processedMarkets,
            string cacheDirectory,
            SimulatorReporting simulatorReporting,
            int processingTimeoutSeconds,
            int maxConcurrentMarkets = 5,
            int batchSize = 10)
        {
            _overseer = overseer;
            _scopeFactory = scopeFactory;
            _processedMarkets = processedMarkets;
            _cacheDirectory = cacheDirectory;
            _simulatorReporting = simulatorReporting;
            _processingTimeoutSeconds = processingTimeoutSeconds;
            _maxConcurrentMarkets = maxConcurrentMarkets;
            _batchSize = batchSize;
            _performanceMetrics = new PerformanceMetrics();
        }

        /// <summary>
        /// Processes a single market by running trading strategies and generating visualization data.
        /// This method orchestrates the complete market processing workflow including simulation,
        /// discrepancy detection, and data point generation for charting and analysis.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market to process.</param>
        /// <param name="marketSnapshots">List of historical market snapshots for the simulation.</param>
        /// <param name="strategiesDict">Dictionary mapping market types to their associated trading strategies.</param>
        /// <param name="progressPrefix">Optional prefix for progress reporting messages.</param>
        /// <param name="writeToFile">Whether to save the processed data to a cache file.</param>
        /// <param name="detectVelocityDiscrepancies">Whether to detect and report orderbook velocity discrepancies.</param>
        /// <param name="group">Optional snapshot group metadata for file naming and organization.</param>
        /// <param name="ignoreProcessedCache">Whether to ignore the processed markets cache and reprocess.</param>
        /// <returns>A tuple containing final P&L, position, average cost, and various lists of price points for visualization.</returns>
        public async Task<(double finalPnL, int finalPosition, double finalAverageCost, List<PricePoint> bidPoints, List<PricePoint> askPoints,
            List<PricePoint> buyPoints, List<PricePoint> sellPoints, List<PricePoint> exitPoints,
            List<PricePoint> eventPoints, List<PricePoint> intendedLongPoints, List<PricePoint> intendedShortPoints,
            List<PricePoint> positionPoints, List<PricePoint> averageCostPoints, List<PricePoint> restingOrdersPoints,
            List<PricePoint> discrepancyPoints, List<PricePoint> patternPoints)>
        ProcessMarketAsync(
            string marketTicker,
            List<MarketSnapshot> marketSnapshots,
            Dictionary<MarketType, List<Strategy>> strategiesDict,
            string progressPrefix = "",
            bool writeToFile = false,
            bool detectVelocityDiscrepancies = false,
            SnapshotGroupDTO? group = null,
            bool ignoreProcessedCache = false)
        {
            if (!ignoreProcessedCache && _processedMarkets.Contains(marketTicker))
            {
                OnTestProgress?.Invoke($"{progressPrefix}Skipping cached market: {marketTicker}");
                return (0, 0, 0.0, new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>());
            }

            OnTestProgress?.Invoke($"{progressPrefix}Processing market: {marketTicker}");

            // Record performance metrics
            var startTime = DateTime.UtcNow;
            _performanceMetrics.StartMarketProcessing(marketTicker, marketSnapshots.Count);

            try
            {
                // Set market types
                var helper = new MarketTypeHelper();
                foreach (var s in marketSnapshots) s.MarketType = helper.GetMarketType(s).ToString();

                // Detect discrepancies
                List<PricePoint> discrepancyPoints = new List<PricePoint>();
                if (detectVelocityDiscrepancies)
                {
                    discrepancyPoints = _simulatorReporting.DetectVelocityDiscrepancies(marketSnapshots, writeToFile);
                    OnTestProgress?.Invoke($"{progressPrefix}Detected {discrepancyPoints.Count} orderbook discrepancies in {marketTicker}.");
                }

                // Run simulation with timeout
                var scenario = new Scenario(strategiesDict);
                var simulationTask = Task.Run(() => _overseer.TestScenario(scenario, marketSnapshots, writeToFile, 100, group));
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(_processingTimeoutSeconds));
                var completedTask = await Task.WhenAny(simulationTask, timeoutTask);

                var pathData = new List<(PathPerformance performance, List<SimulationEventLog> events)>();
                if (completedTask == simulationTask)
                {
                    pathData = await simulationTask;
                }
                else
                {
                    OnTestProgress?.Invoke($"{progressPrefix}Processing timeout exceeded ({_processingTimeoutSeconds}s) for {marketTicker}. Skipping.");
                    _performanceMetrics.CompleteMarketProcessing(marketTicker, DateTime.UtcNow - startTime);
                    return (0, 0, 0.0, new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>());
                }

                if (pathData.Any())
                {
                    var bestPath = pathData.OrderByDescending(p => p.performance.Equity).First();
                    var eventLogs = bestPath.events;

                    var result = ProcessEventLogs(marketSnapshots, eventLogs, marketTicker, writeToFile, progressPrefix, group);
                    _performanceMetrics.CompleteMarketProcessing(marketTicker, DateTime.UtcNow - startTime);
                    return result;
                }
            }
            catch (Exception ex)
            {
                OnTestProgress?.Invoke($"{progressPrefix}Error processing {marketTicker}: {ex.Message}");
            }

            _performanceMetrics.CompleteMarketProcessing(marketTicker, DateTime.UtcNow - startTime);
            return (0, 0, 0.0, new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>(), new List<PricePoint>());
        }

        /// <summary>
        /// Processes the event logs from a trading simulation to generate comprehensive visualization data.
        /// This method extracts trading actions, position changes, and market events from the simulation results
        /// and converts them into various price point collections for charting and analysis.
        /// </summary>
        /// <param name="marketSnapshots">The original market snapshots used in the simulation.</param>
        /// <param name="eventLogs">The event logs generated by the trading simulation.</param>
        /// <param name="marketTicker">The ticker symbol of the market being processed.</param>
        /// <param name="writeToFile">Whether to save the processed data to a cache file.</param>
        /// <param name="progressPrefix">Optional prefix for progress reporting messages.</param>
        /// <param name="group">Optional snapshot group metadata for file naming.</param>
        /// <returns>A tuple containing final P&L, position, average cost, and various lists of price points for visualization.</returns>
        private (double, int, double, List<PricePoint>, List<PricePoint>, List<PricePoint>, List<PricePoint>,
            List<PricePoint>, List<PricePoint>, List<PricePoint>, List<PricePoint>, List<PricePoint>,
            List<PricePoint>, List<PricePoint>, List<PricePoint>, List<PricePoint>)
        ProcessEventLogs(
            List<MarketSnapshot> marketSnapshots,
            List<SimulationEventLog> eventLogs,
            string marketTicker,
            bool writeToFile,
            string progressPrefix,
            SnapshotGroupDTO? group)
        {
            var bestPath = new { performance = new { PnL = 0.0, SimulatedPosition = 0, AverageCost = 0.0 }, events = eventLogs };
            var finalPnL = bestPath.performance.PnL;
            var finalPosition = bestPath.performance.SimulatedPosition;
            var finalAverageCost = bestPath.performance.AverageCost;

            // Create price points
            var (bidPoints, askPoints) = CreateBidAskPoints(marketSnapshots);
            var (buyPoints, sellPoints, exitPoints, intendedLongPoints, intendedShortPoints) = CreateTradePoints(eventLogs);
            var eventPoints = CreateEventPoints(eventLogs);
            var (positionPoints, averageCostPoints) = CreatePositionPoints(eventLogs);
            var restingOrdersPoints = CreateRestingOrdersPoints(eventLogs);
            var patternPoints = CreatePatternPoints(eventLogs);

            // Save to file if requested
            if (writeToFile)
            {
                SaveMarketDataToFile(marketTicker, finalPnL, finalPosition, finalAverageCost,
                    bidPoints, askPoints, buyPoints, sellPoints, exitPoints, eventPoints,
                    intendedLongPoints, intendedShortPoints, positionPoints, averageCostPoints,
                    restingOrdersPoints, new List<PricePoint>(), patternPoints,
                    group != null ? $"_{Path.GetFileNameWithoutExtension(group.JsonPath)}" : "");
            }

            return (finalPnL, finalPosition, finalAverageCost, bidPoints, askPoints, buyPoints, sellPoints,
                exitPoints, eventPoints, intendedLongPoints, intendedShortPoints, positionPoints,
                averageCostPoints, restingOrdersPoints, new List<PricePoint>(), patternPoints);
        }

        /// <summary>
        /// Processes multiple markets in batches for high-volume scenarios to reduce event overhead.
        /// Markets are processed concurrently within each batch, with configurable batch sizes and concurrency limits.
        /// </summary>
        /// <param name="marketTickers">List of market tickers to process.</param>
        /// <param name="marketSnapshots">Dictionary mapping market tickers to their historical snapshots.</param>
        /// <param name="strategiesDict">Dictionary mapping market types to their associated trading strategies.</param>
        /// <param name="progressPrefix">Optional prefix for progress reporting messages.</param>
        /// <param name="writeToFile">Whether to save the processed data to cache files.</param>
        /// <param name="detectVelocityDiscrepancies">Whether to detect and report orderbook velocity discrepancies.</param>
        /// <param name="group">Optional snapshot group metadata for file naming and organization.</param>
        /// <param name="ignoreProcessedCache">Whether to ignore the processed markets cache and reprocess.</param>
        /// <returns>A dictionary mapping market tickers to their processing results.</returns>
        public async Task<Dictionary<string, (double finalPnL, int finalPosition, double finalAverageCost, List<PricePoint> bidPoints, List<PricePoint> askPoints,
            List<PricePoint> buyPoints, List<PricePoint> sellPoints, List<PricePoint> exitPoints,
            List<PricePoint> eventPoints, List<PricePoint> intendedLongPoints, List<PricePoint> intendedShortPoints,
            List<PricePoint> positionPoints, List<PricePoint> averageCostPoints, List<PricePoint> restingOrdersPoints,
            List<PricePoint> discrepancyPoints, List<PricePoint> patternPoints)>> ProcessMarketsBatchAsync(
            List<string> marketTickers,
            Dictionary<string, List<MarketSnapshot>> marketSnapshots,
            Dictionary<MarketType, List<Strategy>> strategiesDict,
            string progressPrefix = "",
            bool writeToFile = false,
            bool detectVelocityDiscrepancies = false,
            SnapshotGroupDTO? group = null,
            bool ignoreProcessedCache = false)
        {
            var results = new Dictionary<string, (double, int, double, List<PricePoint>, List<PricePoint>, List<PricePoint>, List<PricePoint>, List<PricePoint>, List<PricePoint>, List<PricePoint>, List<PricePoint>, List<PricePoint>, List<PricePoint>, List<PricePoint>, List<PricePoint>, List<PricePoint>)>();

            // Filter out already processed markets if not ignoring cache
            var marketsToProcess = ignoreProcessedCache
                ? marketTickers
                : marketTickers.Where(ticker => !_processedMarkets.Contains(ticker)).ToList();

            if (!marketsToProcess.Any())
            {
                OnTestProgress?.Invoke($"{progressPrefix}All markets already processed or cached.");
                return results;
            }

            OnTestProgress?.Invoke($"{progressPrefix}Processing {marketsToProcess.Count} markets in batches (batch size: {_batchSize}, max concurrent: {_maxConcurrentMarkets})");

            // Process markets in batches
            for (int i = 0; i < marketsToProcess.Count; i += _batchSize)
            {
                var batchStartTime = DateTime.UtcNow;
                var batch = marketsToProcess.Skip(i).Take(_batchSize).ToList();
                OnTestProgress?.Invoke($"{progressPrefix}Processing batch {i / _batchSize + 1} with {batch.Count} markets");

                // Process markets in this batch concurrently, but limit concurrency
                var batchTasks = new List<Task>();
                var semaphore = new SemaphoreSlim(_maxConcurrentMarkets);

                foreach (var marketTicker in batch)
                {
                    if (!marketSnapshots.TryGetValue(marketTicker, out var snapshots))
                    {
                        OnTestProgress?.Invoke($"{progressPrefix}Warning: No snapshots found for market {marketTicker}, skipping.");
                        continue;
                    }

                    batchTasks.Add(Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            var result = await ProcessMarketAsync(marketTicker, snapshots, strategiesDict,
                                progressPrefix, writeToFile, detectVelocityDiscrepancies, group, ignoreProcessedCache);
                            lock (results)
                            {
                                results[marketTicker] = result;
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }

                // Wait for all tasks in this batch to complete
                await Task.WhenAll(batchTasks);
                var batchProcessingTime = DateTime.UtcNow - batchStartTime;
                _performanceMetrics.RecordBatchProcessing(batch.Count, batchProcessingTime);
                OnTestProgress?.Invoke($"{progressPrefix}Completed batch {i / _batchSize + 1} in {batchProcessingTime.TotalSeconds:F2}s");
            }

            OnTestProgress?.Invoke($"{progressPrefix}Batch processing completed. Processed {results.Count} markets.");
            return results;
        }

        /// <summary>
        /// Creates price point collections for bid and ask prices from market snapshots.
        /// This method extracts the best bid and ask prices at each snapshot timestamp
        /// to provide the baseline price data for market visualization.
        /// </summary>
        /// <param name="marketSnapshots">The list of market snapshots to extract price data from.</param>
        /// <returns>A tuple containing lists of bid and ask price points for charting.</returns>
        private (List<PricePoint>, List<PricePoint>) CreateBidAskPoints(List<MarketSnapshot> marketSnapshots)
        {
            var bidPoints = marketSnapshots.Select(s => new PricePoint(s.Timestamp, s.BestYesBid, " Best Bid")).ToList();
            var askPoints = marketSnapshots.Select(s => new PricePoint(s.Timestamp, s.BestYesAsk, " Best Ask")).ToList();
            return (bidPoints, askPoints);
        }

        /// <summary>
        /// Creates price point collections for various trading actions from event logs.
        /// This method analyzes the event logs to identify buy/sell actions, intended trades,
        /// and explicit exits, converting them into timestamped price points for visualization.
        /// </summary>
        /// <param name="eventLogs">The event logs from the trading simulation to analyze.</param>
        /// <returns>A tuple containing lists of buy, sell, exit, intended long, and intended short price points.</returns>
        private (List<PricePoint>, List<PricePoint>, List<PricePoint>, List<PricePoint>, List<PricePoint>)
        CreateTradePoints(List<SimulationEventLog> eventLogs)
        {
            var buyPoints = new List<PricePoint>();
            var sellPoints = new List<PricePoint>();
            var exitPoints = new List<PricePoint>();
            var intendedLongPoints = new List<PricePoint>();
            var intendedShortPoints = new List<PricePoint>();

            int prevPos = 0;
            foreach (var ev in eventLogs)
            {
                string prefix = " ";

                // Trade markers
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

                // Explicit exits
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

            return (buyPoints, sellPoints, exitPoints, intendedLongPoints, intendedShortPoints);
        }

        /// <summary>
        /// Creates price point collections for significant market events from event logs.
        /// This method extracts all event memos and associates them with mid-price points
        /// for visualization of important market occurrences during the simulation.
        /// </summary>
        /// <param name="eventLogs">The event logs containing market events and memos.</param>
        /// <returns>A list of price points representing significant market events.</returns>
        private List<PricePoint> CreateEventPoints(List<SimulationEventLog> eventLogs)
        {
            return eventLogs
                .Select(ev => new PricePoint(ev.Timestamp, (ev.BestYesBid + ev.BestYesAsk) / 2.0, " " + ev.Memo))
                .ToList();
        }

        /// <summary>
        /// Creates price point collections for position and average cost tracking from event logs.
        /// This method extracts position sizes and average costs at each event timestamp
        /// to provide visualization of portfolio changes over time.
        /// </summary>
        /// <param name="eventLogs">The event logs containing position and cost information.</param>
        /// <returns>A tuple containing lists of position and average cost price points.</returns>
        private (List<PricePoint>, List<PricePoint>) CreatePositionPoints(List<SimulationEventLog> eventLogs)
        {
            var positionPoints = eventLogs
                .Select(ev => new PricePoint(ev.Timestamp, ev.Position, $"Position: {ev.Position}"))
                .ToList();

            var averageCostPoints = eventLogs
                .Select(ev => new PricePoint(ev.Timestamp, ev.AverageCost, $"Avg Cost: {ev.AverageCost:F2}"))
                .ToList();

            return (positionPoints, averageCostPoints);
        }

        /// <summary>
        /// Creates price point collections for resting orders count from event logs.
        /// This method parses the resting orders data from event logs to track
        /// the number of outstanding orders at each point in time.
        /// </summary>
        /// <param name="eventLogs">The event logs containing resting orders information.</param>
        /// <returns>A list of price points representing resting orders count over time.</returns>
        private List<PricePoint> CreateRestingOrdersPoints(List<SimulationEventLog> eventLogs)
        {
            return eventLogs
                .Select(ev =>
                {
                    int totalResting = 0;

                    // Parse RestingYesBids
                    if (!string.IsNullOrEmpty(ev.RestingYesBids) && ev.RestingYesBids != "N/A")
                    {
                        var yesOrders = ev.RestingYesBids.Split(',');
                        foreach (var order in yesOrders)
                        {
                            var parts = order.Trim().Split(':');
                            if (parts.Length == 2 && int.TryParse(parts[1], out int qty))
                            {
                                totalResting += qty;
                            }
                        }
                    }

                    // Parse RestingNoBids
                    if (!string.IsNullOrEmpty(ev.RestingNoBids) && ev.RestingNoBids != "N/A")
                    {
                        var noOrders = ev.RestingNoBids.Split(',');
                        foreach (var order in noOrders)
                        {
                            var parts = order.Trim().Split(':');
                            if (parts.Length == 2 && int.TryParse(parts[1], out int qty))
                            {
                                totalResting += qty;
                            }
                        }
                    }

                    return new PricePoint(ev.Timestamp, totalResting, $"Resting Orders: {totalResting}");
                })
                .ToList();
        }

        /// <summary>
        /// Creates price point collections for detected candlestick patterns from event logs.
        /// This method extracts pattern detection events and converts them into
        /// visualization points for technical analysis pattern recognition.
        /// </summary>
        /// <param name="eventLogs">The event logs containing pattern detection information.</param>
        /// <returns>A list of price points representing detected patterns.</returns>
        private List<PricePoint> CreatePatternPoints(List<SimulationEventLog> eventLogs)
        {
            var patternPoints = new List<PricePoint>();
            foreach (var ev in eventLogs)
            {
                if (ev.Patterns != null && ev.Patterns.Any())
                {
                    foreach (var pattern in ev.Patterns)
                    {
                        patternPoints.Add(new PricePoint(ev.Timestamp, 0, $"Pattern: {pattern.Name}"));
                    }
                }
            }
            return patternPoints;
        }

        /// <summary>
        /// Saves the processed market data to a JSON cache file for future use.
        /// This method serializes all the processed market data including price points,
        /// trading results, and visualization data into a structured JSON format.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market being saved.</param>
        /// <param name="finalPnL">The final profit and loss from the simulation.</param>
        /// <param name="finalPosition">The final position size at the end of the simulation.</param>
        /// <param name="finalAverageCost">The final average cost of the position.</param>
        /// <param name="bidPoints">List of bid price points for visualization.</param>
        /// <param name="askPoints">List of ask price points for visualization.</param>
        /// <param name="buyPoints">List of buy trade points for visualization.</param>
        /// <param name="sellPoints">List of sell trade points for visualization.</param>
        /// <param name="exitPoints">List of exit trade points for visualization.</param>
        /// <param name="eventPoints">List of event points for visualization.</param>
        /// <param name="intendedLongPoints">List of intended long positions for visualization.</param>
        /// <param name="intendedShortPoints">List of intended short positions for visualization.</param>
        /// <param name="positionPoints">List of position size points for visualization.</param>
        /// <param name="averageCostPoints">List of average cost points for visualization.</param>
        /// <param name="restingOrdersPoints">List of resting orders count points for visualization.</param>
        /// <param name="discrepancyPoints">List of discrepancy detection points for visualization.</param>
        /// <param name="patternPoints">List of pattern detection points for visualization.</param>
        /// <param name="fileNameSuffix">Optional suffix for the cache file name.</param>
        public void SaveMarketDataToFile(
            string marketTicker, double finalPnL, int finalPosition, double finalAverageCost,
            List<PricePoint> bidPoints, List<PricePoint> askPoints,
            List<PricePoint> buyPoints, List<PricePoint> sellPoints, List<PricePoint> exitPoints,
            List<PricePoint> eventPoints, List<PricePoint> intendedLongPoints, List<PricePoint> intendedShortPoints,
            List<PricePoint> positionPoints, List<PricePoint> averageCostPoints, List<PricePoint> restingOrdersPoints,
            List<PricePoint> discrepancyPoints, List<PricePoint> patternPoints,
            string fileNameSuffix = "")
        {
            var cachedData = new CachedMarketData
            {
                Market = marketTicker,
                PnL = finalPnL,
                SimulatedPosition = finalPosition,
                AverageCost = finalAverageCost,
                BidPoints = bidPoints,
                AskPoints = askPoints,
                BuyPoints = buyPoints,
                SellPoints = sellPoints,
                ExitPoints = exitPoints,
                EventPoints = eventPoints,
                IntendedLongPoints = intendedLongPoints,
                IntendedShortPoints = intendedShortPoints,
                PositionPoints = positionPoints,
                AverageCostPoints = averageCostPoints,
                RestingOrdersPoints = restingOrdersPoints,
                DiscrepancyPoints = discrepancyPoints,
                PatternPoints = patternPoints
            };

            var json = JsonSerializer.Serialize(cachedData);
            Directory.CreateDirectory(_cacheDirectory);
            var filePath = Path.Combine(_cacheDirectory, $"{marketTicker}{fileNameSuffix}.json");
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Gets the current performance metrics for monitoring processing efficiency.
        /// </summary>
        public PerformanceMetrics GetPerformanceMetrics() => _performanceMetrics;

        /// <summary>
        /// Resets all performance metrics counters.
        /// </summary>
        public void ResetPerformanceMetrics() => _performanceMetrics.Reset();
    }

    /// <summary>
    /// Performance metrics class for tracking message processing rates and queue depths.
    /// </summary>
    public class PerformanceMetrics
    {
        private readonly object _metricsLock = new object();
        private DateTime _startTime = DateTime.UtcNow;
        private int _totalMarketsProcessed = 0;
        private int _totalSnapshotsProcessed = 0;
        private TimeSpan _totalProcessingTime = TimeSpan.Zero;
        private int _currentQueueDepth = 0;
        private int _maxQueueDepth = 0;
        private int _batchProcessingCount = 0;
        private TimeSpan _totalBatchProcessingTime = TimeSpan.Zero;

        /// <summary>
        /// Records the start of market processing for performance tracking.
        /// </summary>
        /// <param name="marketTicker">The market ticker being processed.</param>
        /// <param name="snapshotCount">Number of snapshots in this market.</param>
        public void StartMarketProcessing(string marketTicker, int snapshotCount)
        {
            lock (_metricsLock)
            {
                _totalSnapshotsProcessed += snapshotCount;
                _currentQueueDepth++;
                if (_currentQueueDepth > _maxQueueDepth)
                    _maxQueueDepth = _currentQueueDepth;
            }
        }

        /// <summary>
        /// Records the completion of market processing and updates timing metrics.
        /// </summary>
        /// <param name="marketTicker">The market ticker that was processed.</param>
        /// <param name="processingTime">Time taken to process this market.</param>
        public void CompleteMarketProcessing(string marketTicker, TimeSpan processingTime)
        {
            lock (_metricsLock)
            {
                _totalMarketsProcessed++;
                _totalProcessingTime += processingTime;
                _currentQueueDepth--;
            }
        }

        /// <summary>
        /// Records batch processing metrics.
        /// </summary>
        /// <param name="batchSize">Number of markets in the batch.</param>
        /// <param name="processingTime">Time taken to process the batch.</param>
        public void RecordBatchProcessing(int batchSize, TimeSpan processingTime)
        {
            lock (_metricsLock)
            {
                _batchProcessingCount++;
                _totalBatchProcessingTime += processingTime;
            }
        }

        /// <summary>
        /// Gets the average processing time per market.
        /// </summary>
        public TimeSpan AverageProcessingTimePerMarket =>
            _totalMarketsProcessed > 0 ? _totalProcessingTime / _totalMarketsProcessed : TimeSpan.Zero;

        /// <summary>
        /// Gets the average processing time per batch.
        /// </summary>
        public TimeSpan AverageBatchProcessingTime =>
            _batchProcessingCount > 0 ? _totalBatchProcessingTime / _batchProcessingCount : TimeSpan.Zero;

        /// <summary>
        /// Gets the processing rate in markets per second.
        /// </summary>
        public double MarketsPerSecond =>
            _totalProcessingTime.TotalSeconds > 0 ? _totalMarketsProcessed / _totalProcessingTime.TotalSeconds : 0;

        /// <summary>
        /// Gets the processing rate in snapshots per second.
        /// </summary>
        public double SnapshotsPerSecond =>
            _totalProcessingTime.TotalSeconds > 0 ? _totalSnapshotsProcessed / _totalProcessingTime.TotalSeconds : 0;

        /// <summary>
        /// Gets the current queue depth.
        /// </summary>
        public int CurrentQueueDepth => _currentQueueDepth;

        /// <summary>
        /// Gets the maximum queue depth recorded.
        /// </summary>
        public int MaxQueueDepth => _maxQueueDepth;

        /// <summary>
        /// Gets the total number of markets processed.
        /// </summary>
        public int TotalMarketsProcessed => _totalMarketsProcessed;

        /// <summary>
        /// Gets the total number of snapshots processed.
        /// </summary>
        public int TotalSnapshotsProcessed => _totalSnapshotsProcessed;

        /// <summary>
        /// Gets the total processing time.
        /// </summary>
        public TimeSpan TotalProcessingTime => _totalProcessingTime;

        /// <summary>
        /// Gets the uptime since metrics were started.
        /// </summary>
        public TimeSpan Uptime => DateTime.UtcNow - _startTime;

        /// <summary>
        /// Resets all performance metrics to their initial state.
        /// </summary>
        public void Reset()
        {
            lock (_metricsLock)
            {
                _startTime = DateTime.UtcNow;
                _totalMarketsProcessed = 0;
                _totalSnapshotsProcessed = 0;
                _totalProcessingTime = TimeSpan.Zero;
                _currentQueueDepth = 0;
                _maxQueueDepth = 0;
                _batchProcessingCount = 0;
                _totalBatchProcessingTime = TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Gets a summary of current performance metrics as a formatted string.
        /// </summary>
        public string GetMetricsSummary()
        {
            lock (_metricsLock)
            {
                return $"Performance Metrics:\n" +
                       $"  Total Markets Processed: {_totalMarketsProcessed}\n" +
                       $"  Total Snapshots Processed: {_totalSnapshotsProcessed}\n" +
                       $"  Total Processing Time: {_totalProcessingTime.TotalSeconds:F2}s\n" +
                       $"  Average Time per Market: {AverageProcessingTimePerMarket.TotalMilliseconds:F2}ms\n" +
                       $"  Markets per Second: {MarketsPerSecond:F2}\n" +
                       $"  Snapshots per Second: {SnapshotsPerSecond:F2}\n" +
                       $"  Current Queue Depth: {_currentQueueDepth}\n" +
                       $"  Max Queue Depth: {_maxQueueDepth}\n" +
                       $"  Uptime: {_startTime.ToString()}";
            }
        }
    }
}