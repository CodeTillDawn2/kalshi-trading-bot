using BacklashBot.Configuration;
using BacklashBot.Management.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using BacklashDTOs;
using BacklashDTOs.Exceptions;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Timers;

namespace BacklashBot.Services
{
    /// <summary>
    /// Tracks and analyzes orderbook changes for a specific market in the Kalshi trading system.
    /// This class is responsible for processing orderbook snapshots, recording individual changes,
    /// matching trades to orderbook changes, calculating various market metrics (velocity, volume, rates),
    /// and maintaining a rolling window of orderbook events for analysis. It implements the
    /// IOrderbookChangeTracker interface and integrates with the broader trading bot ecosystem
    /// to provide real-time market data analysis and metrics calculation.
    /// </summary>
    /// <remarks>
    /// The tracker maintains concurrent queues for orderbook changes and trade events, using
    /// timers for periodic metric recalculation and cleanup of old events. It employs locking
    /// mechanisms to ensure thread safety during matching operations and supports cancellation
    /// tokens for graceful shutdown. Metrics are calculated over configurable time windows and
    /// include velocity per minute, trade rates, average trade sizes, and order volume analysis.
    /// </remarks>
    public class OrderbookChangeTracker : IOrderbookChangeTracker
    {
        private readonly ILogger<IOrderbookChangeTracker> _logger;
        private readonly string _marketTicker;
        private readonly IDataCache _cache;
        private readonly IOptions<OrderbookChangeTrackerConfig> _trackerConfig;
        private CancellationToken _cancellationToken => _statusTrackerService.GetCancellationToken();


        private readonly IScopeManagerService _scopeManagerService;

        private IStatusTrackerService _statusTrackerService;
        private readonly ICentralPerformanceMonitor _centralPerformanceMonitor;

        private readonly bool _enablePerformanceMetrics;

        /// <summary>
        /// Occurs when the market is determined to be invalid during event validation.
        /// </summary>
        public event EventHandler<string> MarketInvalid;

        private readonly ConcurrentQueue<OrderbookChange> _orderbookChanges = new ConcurrentQueue<OrderbookChange>();
        private readonly ConcurrentQueue<TradeEvent> _tradeEvents = new ConcurrentQueue<TradeEvent>();
        private readonly System.Timers.Timer _recalculationTimer;
        private readonly System.Timers.Timer _logOutputTimer;
        private long _lastSequence = 0;
        /// <summary>
        /// Gets or sets the timestamp when the market last opened for trading.
        /// This is used to determine market maturity and calculate elapsed trading time.
        /// </summary>
        /// <value>The UTC timestamp of the last market open, or DateTime.MinValue if never opened</value>
        public DateTime LastMarketOpenTime { get; set; } = DateTime.MinValue;
        private DateTime _lastEventTime = DateTime.MinValue;

        #region Properties
        /// <summary>
        /// Gets the market data object associated with this tracker.
        /// Returns null if the market is not found in the cache or if cancellation is requested.
        /// </summary>
        /// <value>The market data object, or null if unavailable</value>
        public IMarketData Market
        {
            get
            {
                if (_cache == null || !_cache.Markets.ContainsKey(_marketTicker) || _cancellationToken.IsCancellationRequested)
                    return null;
                return _cache.Markets[_marketTicker];
            }
        }

        private bool IsFirstSnapshotProcessed { get; set; } = false;

        /// <summary>
        /// Gets a value indicating whether the market has been open long enough to have mature metrics.
        /// Maturity is determined by comparing the elapsed time since market open to the configured change window duration.
        /// </summary>
        /// <value>true if the market has been open for at least the change window duration, false otherwise</value>
        /// <remarks>
        /// Mature markets have sufficient historical data for reliable metric calculations.
        /// This property is used to determine when metrics are ready for analysis.
        /// </remarks>
        public bool IsMature
        {
            get
            {
                if (LastMarketOpenTime == DateTime.MinValue)
                {
                    return false;
                }
                TimeSpan elapsedSinceOpen = DateTime.UtcNow - LastMarketOpenTime;
                return elapsedSinceOpen >= TimeSpan.FromMinutes(5);
            }
        }

        /// <summary>
        /// Gets the duration of the change window used for metric calculations.
        /// This determines how far back in time to consider orderbook changes and trades.
        /// </summary>
        /// <value>The change window duration from configuration</value>
        public TimeSpan ChangeWindowDuration => TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets the time window used for matching trades to orderbook changes.
        /// This determines how far back and forward to look when correlating trades with orderbook activity.
        /// </summary>
        /// <value>The trade matching window duration from configuration</value>
        public TimeSpan TradeMatchingWindow => TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets the time window used for detecting orderbook change cancellations.
        /// This determines how far back to look when checking if opposing orderbook changes cancel each other out.
        /// </summary>
        /// <value>The orderbook cancel window duration from configuration</value>
        public TimeSpan OrderbookCancelWindow => TimeSpan.FromSeconds(10);
        #endregion

        #region Constructor and Initialization
        private bool MetricsNeedRecalculation = true;

        private readonly object _orderbookMatchingLock = new object();

        // Performance metrics
        private long _totalMatchingOperations = 0;
        private long _successfulMatches = 0;
        private long _totalProcessingTimeMs = 0;
        private long _totalQueueProcessingTimeMs = 0;
        private int _maxQueueDepth = 0;
        private DateTime _lastMetricsReset = DateTime.UtcNow;

        // Event processing latency metrics
        private long _totalEventProcessingTimeMs = 0;
        private int _eventProcessingCount = 0;

        // Timer accuracy metrics
        private DateTime _lastRecalculationTimerElapsed = DateTime.MinValue;
        private DateTime _lastLogTimerElapsed = DateTime.MinValue;
        private long _totalTimerDriftMs = 0;
        private int _timerCallbackCount = 0;
        private long _totalTimerExecutionTimeMs = 0;

        /// <summary>
        /// Initializes a new instance of the OrderbookChangeTracker for the specified market.
        /// Sets up timers for periodic metric recalculation and log output, initializes event queues,
        /// and prepares the tracker for processing orderbook changes and trades.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market to track</param>
        /// <param name="logger">Logger instance for recording operations and errors</param>
        /// <param name="cache">Shared data cache containing market data and system state</param>
        /// <param name="trackerConfig">Orderbook change tracker configuration containing time windows and thresholds</param>
        /// <param name="scopeManagerService">Service for managing dependency injection scopes</param>
        /// <param name="statusTrackerService">Service for tracking system status and cancellation tokens</param>
        /// <param name="centralPerformanceMonitor">Central performance monitor for recording metrics</param>
        /// <exception cref="ArgumentNullException">Thrown when marketTicker, logger, trackerConfig, or statusTrackerService is null</exception>
        public OrderbookChangeTracker(
            string marketTicker,
            ILogger<IOrderbookChangeTracker> logger,
            IDataCache cache,
            IOptions<OrderbookChangeTrackerConfig> trackerConfig,
            IScopeManagerService scopeManagerService,
            IStatusTrackerService statusTrackerService,
            ICentralPerformanceMonitor centralPerformanceMonitor)
        {
            _marketTicker = marketTicker ?? throw new ArgumentNullException(nameof(marketTicker));
            _scopeManagerService = scopeManagerService;
            _cache = cache;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _trackerConfig = trackerConfig ?? throw new ArgumentNullException(nameof(trackerConfig));
            _statusTrackerService = statusTrackerService;
            _centralPerformanceMonitor = centralPerformanceMonitor ?? throw new ArgumentNullException(nameof(centralPerformanceMonitor));

            _enablePerformanceMetrics = _trackerConfig.Value.EnablePerformanceMetrics;

            _recalculationTimer = new System.Timers.Timer(10000); // 10 seconds
            _recalculationTimer.Elapsed += (sender, e) => OnRecalculationTimerElapsed(sender, e);
            _recalculationTimer.AutoReset = true;

            _logOutputTimer = new System.Timers.Timer(300000); // 5 minutes
            _logOutputTimer.Elapsed += (sender, e) => OnLogOutputTimerElapsed(sender, e);
            _logOutputTimer.AutoReset = true;
            _logOutputTimer.Start();

            _logger.LogDebug("OrderbookChangeTracker initialized for market {MarketTicker}", _marketTicker);
            RecalculateMetrics();
            _recalculationTimer.Start();
        }

        /// <summary>
        /// Performs complete shutdown of the OrderbookChangeTracker, stopping timers,
        /// clearing all event queues, resetting state, and disposing resources.
        /// </summary>
        /// <remarks>
        /// This method should be called when the tracker is no longer needed.
        /// It ensures clean shutdown and prevents resource leaks.
        /// </remarks>
        public void Shutdown()
        {
            _logger.LogDebug("Initiating shutdown for OrderbookChangeTracker associated with market {MarketTicker}", _marketTicker);

            // Halt periodic timer operations
            _recalculationTimer.Stop();
            _logOutputTimer.Stop();

            // Clear event queues to release held data
            _orderbookChanges.Clear();
            _tradeEvents.Clear();

            // Reset internal state variables
            _lastSequence = 0;
            LastMarketOpenTime = DateTime.MinValue;
            _lastEventTime = DateTime.MinValue;
            IsFirstSnapshotProcessed = false;
            MetricsNeedRecalculation = false;

            // Dispose of timer resources
            _recalculationTimer.Dispose();
            _logOutputTimer.Dispose();

            _logger.LogDebug("Shutdown completed for OrderbookChangeTracker associated with market {MarketTicker}: Timers stopped and disposed, queues cleared, state reset", _marketTicker);
        }
        private void OnLogOutputTimerElapsed(object sender, ElapsedEventArgs e)
        {
            var timerExecutionStopwatch = _enablePerformanceMetrics ? Stopwatch.StartNew() : null;

            if (_enablePerformanceMetrics)
            {
                if (_lastLogTimerElapsed != DateTime.MinValue)
                {
                    var expectedInterval = TimeSpan.FromMinutes(5);
                    var actualInterval = DateTime.UtcNow - _lastLogTimerElapsed;
                    var drift = actualInterval - expectedInterval;
                    _totalTimerDriftMs += (long)drift.TotalMilliseconds;
                }

                _lastLogTimerElapsed = DateTime.UtcNow;
                _timerCallbackCount++;
            }

            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Log output cancelled for {MarketTicker}", _marketTicker);
                if (timerExecutionStopwatch != null)
                {
                    timerExecutionStopwatch.Stop();
                    _totalTimerExecutionTimeMs += timerExecutionStopwatch.ElapsedMilliseconds;
                }
                return;
            }

            try
            {
                // Removed file logging; no direct replacement needed as per request
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process log output for {MarketTicker}", _marketTicker);
            }

            if (timerExecutionStopwatch != null)
            {
                timerExecutionStopwatch.Stop();
                _totalTimerExecutionTimeMs += timerExecutionStopwatch.ElapsedMilliseconds;
            }
        }
        #endregion

        #region Market Status
        /// <summary>
        /// Updates the market status and handles market opening/closing transitions.
        /// When the market opens, it starts the recalculation timer and sets the open time.
        /// When the market closes, it clears event queues and stops the timer.
        /// </summary>
        /// <param name="isExchangeActive">Whether the exchange is currently active</param>
        /// <param name="isTradingActive">Whether trading is currently active for this market</param>
        /// <remarks>
        /// This method is called when market status changes are detected. It manages
        /// the lifecycle of the tracker's processing based on market availability.
        /// </remarks>
        public void UpdateMarketStatus(bool isExchangeActive, bool isTradingActive)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Market status update cancelled for {MarketTicker}", _marketTicker);
                return;
            }

            if (isTradingActive && isExchangeActive && LastMarketOpenTime == DateTime.MinValue)
            {
                LastMarketOpenTime = DateTime.UtcNow;
                _logger.LogInformation("Market {MarketTicker} opened at {Time}", _marketTicker, LastMarketOpenTime);
                _recalculationTimer.Start();
            }
            else if ((!isTradingActive || !isExchangeActive) && LastMarketOpenTime != DateTime.MinValue)
            {
                LastMarketOpenTime = DateTime.MinValue;
                _orderbookChanges.Clear();
                _tradeEvents.Clear();
                _lastSequence = 0;
                _recalculationTimer.Stop();
                _logger.LogInformation("Market {MarketTicker} closed, queues and timer reset", _marketTicker);
            }
        }
        #endregion

        /// <summary>
        /// Stops the periodic timers used for metric recalculation and log output.
        /// This method halts background processing without clearing data or disposing resources.
        /// </summary>
        /// <remarks>
        /// Use this method to temporarily pause processing. Call Shutdown() for complete cleanup.
        /// </remarks>
        public void Stop()
        {
            _recalculationTimer.Stop();
            _logOutputTimer.Stop();
            _logger.LogDebug("OrderbookChangeTracker stopped for {MarketTicker}: Timers stopped", _marketTicker);
        }

        /// <summary>
        /// Gets the current performance metrics for the orderbook change tracker.
        /// </summary>
        /// <returns>A dictionary containing performance metrics including matching success rates, processing times, and queue depths.</returns>
        public Dictionary<string, object> GetPerformanceMetrics()
        {
            var uptime = DateTime.UtcNow - _lastMetricsReset;
            var matchSuccessRate = _totalMatchingOperations > 0 ? (double)_successfulMatches / _totalMatchingOperations : 0.0;
            var avgProcessingTimeMs = _totalMatchingOperations > 0 ? (double)_totalProcessingTimeMs / _totalMatchingOperations : 0.0;
            var avgQueueProcessingTimeMs = _totalMatchingOperations > 0 ? (double)_totalQueueProcessingTimeMs / _totalMatchingOperations : 0.0;

            return new Dictionary<string, object>
            {
                ["MarketTicker"] = _marketTicker,
                ["TotalMatchingOperations"] = _totalMatchingOperations,
                ["SuccessfulMatches"] = _successfulMatches,
                ["MatchSuccessRate"] = Math.Round(matchSuccessRate * 100, 2),
                ["TotalProcessingTimeMs"] = _totalProcessingTimeMs,
                ["AverageProcessingTimeMs"] = Math.Round(avgProcessingTimeMs, 2),
                ["TotalQueueProcessingTimeMs"] = _totalQueueProcessingTimeMs,
                ["AverageQueueProcessingTimeMs"] = Math.Round(avgQueueProcessingTimeMs, 2),
                ["MaxQueueDepth"] = _maxQueueDepth,
                ["CurrentQueueDepth"] = _orderbookChanges.Count,
                ["CurrentTradeQueueDepth"] = _tradeEvents.Count,
                ["UptimeSeconds"] = uptime.TotalSeconds,
                ["OperationsPerSecond"] = uptime.TotalSeconds > 0 ? Math.Round(_totalMatchingOperations / uptime.TotalSeconds, 2) : 0.0,
                ["TotalEventProcessingTimeMs"] = _totalEventProcessingTimeMs,
                ["EventProcessingCount"] = _eventProcessingCount,
                ["AverageEventProcessingTimeMs"] = _eventProcessingCount > 0 ? Math.Round((double)_totalEventProcessingTimeMs / _eventProcessingCount, 2) : 0.0,
                ["TotalTimerDriftMs"] = _totalTimerDriftMs,
                ["TimerCallbackCount"] = _timerCallbackCount,
                ["AverageTimerDriftMs"] = _timerCallbackCount > 1 ? Math.Round((double)_totalTimerDriftMs / (_timerCallbackCount - 1), 2) : 0.0,
                ["TotalTimerExecutionTimeMs"] = _totalTimerExecutionTimeMs,
                ["AverageTimerExecutionTimeMs"] = _timerCallbackCount > 0 ? Math.Round((double)_totalTimerExecutionTimeMs / _timerCallbackCount, 2) : 0.0,
                ["LastMetricsReset"] = _lastMetricsReset
            };
        }

        /// <summary>
        /// Resets all performance metrics counters to zero.
        /// </summary>
        public void ResetPerformanceMetrics()
        {
            _totalMatchingOperations = 0;
            _successfulMatches = 0;
            _totalProcessingTimeMs = 0;
            _totalQueueProcessingTimeMs = 0;
            _maxQueueDepth = 0;
            _totalEventProcessingTimeMs = 0;
            _eventProcessingCount = 0;
            _totalTimerDriftMs = 0;
            _timerCallbackCount = 0;
            _totalTimerExecutionTimeMs = 0;
            _lastRecalculationTimerElapsed = DateTime.MinValue;
            _lastLogTimerElapsed = DateTime.MinValue;
            _lastMetricsReset = DateTime.UtcNow;
            _logger.LogInformation("Performance metrics reset for {MarketTicker}", _marketTicker);
        }

        #region Event Logging and Matching
        /// <summary>
        /// Processes a complete orderbook snapshot by comparing it with the previous state.
        /// Calculates deltas for each price level, records individual orderbook changes,
        /// and triggers metric recalculation. Handles market reset scenarios when snapshots
        /// are empty or when significant time gaps are detected.
        /// </summary>
        /// <param name="originalOrderbook">The previous orderbook state, or null for initial snapshot</param>
        /// <param name="newOrderbook">The current orderbook state to process</param>
        /// <remarks>
        /// This method is called periodically with full orderbook data. It compares the new
        /// snapshot against the previous one to identify changes, then records each change
        /// individually. If the original orderbook is null or a 2-minute gap is detected,
        /// it resets the event queues and recalculates metrics to ensure data consistency.
        /// </remarks>
        public void ProcessOrderbookSnapshot(List<OrderbookData> originalOrderbook, List<OrderbookData> newOrderbook)
        {
            var eventProcessingStopwatch = _enablePerformanceMetrics ? Stopwatch.StartNew() : null;
            var queueProcessingStopwatch = Stopwatch.StartNew();
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Orderbook snapshot logging cancelled for {MarketTicker}", _marketTicker);
                return;
            }

            _logger.LogDebug("Processing orderbook snapshot for {MarketTicker}", _marketTicker);

            bool shouldResetAndRecalculate = originalOrderbook == null || !originalOrderbook.Any() ||
                (_lastSequence > 0 && DateTime.UtcNow - _lastEventTime >= TimeSpan.FromMinutes(2));

            if (shouldResetAndRecalculate)
            {
                _logger.LogDebug("DELTA-Resetting events and recalculating metrics for {MarketTicker} due to empty original or 2-minute inactivity", _marketTicker);
                ClearEventQueues();
                RecalculateMetrics();
            }
            else
            {
                _lastEventTime = DateTime.UtcNow;
            }

            var originalBySideAndPrice = originalOrderbook?.GroupBy(o => (o.Side, o.Price))
                .ToDictionary(g => g.Key, g => g.First().RestingContracts) ?? new Dictionary<(string, int), int>();
            var newBySideAndPrice = newOrderbook.GroupBy(o => (o.Side, o.Price))
                .ToDictionary(g => g.Key, g => g.First().RestingContracts);


            if (IsFirstSnapshotProcessed)
            {
                var allKeys = originalBySideAndPrice.Keys.Union(newBySideAndPrice.Keys).ToList();

                foreach (var (side, price) in allKeys)
                {
                    int originalContracts = originalBySideAndPrice.GetValueOrDefault((side, price), 0);
                    int newContracts = newBySideAndPrice.GetValueOrDefault((side, price), 0);
                    int deltaContracts = newContracts - originalContracts;

                    if (deltaContracts != 0)
                    {
                        _logger.LogDebug("DELTA-Converted change for {MarketTicker} from snapshot: Side={Side}, Price={Price}, DeltaContracts={DeltaContracts}",
                            _marketTicker, side, price, deltaContracts);
                        RecordOrderbookChange(side, price, deltaContracts);
                    }

                }
            }


            IsFirstSnapshotProcessed = true;

            queueProcessingStopwatch.Stop();
            _totalQueueProcessingTimeMs += queueProcessingStopwatch.ElapsedMilliseconds;

            // Track max queue depth
            int currentQueueDepth = _orderbookChanges.Count;
            if (currentQueueDepth > _maxQueueDepth)
            {
                _maxQueueDepth = currentQueueDepth;
            }

            _logger.LogDebug("Completed orderbook snapshot processing for {MarketTicker} in {ProcessingTime}ms, QueueDepth={QueueDepth}",
                _marketTicker, queueProcessingStopwatch.ElapsedMilliseconds, currentQueueDepth);

            if (eventProcessingStopwatch != null)
            {
                eventProcessingStopwatch.Stop();
                _totalEventProcessingTimeMs += eventProcessingStopwatch.ElapsedMilliseconds;
                _eventProcessingCount++;
            }
        }

        /// <summary>
        /// Records an individual orderbook change event with the specified parameters.
        /// Creates a new OrderbookChange object, attempts to match it with existing trades,
        /// checks for canceling orderbook changes, and enqueues the change for processing.
        /// </summary>
        /// <param name="side">The side of the orderbook ("yes" or "no")</param>
        /// <param name="price">The price level of the change</param>
        /// <param name="deltaContracts">The change in contract count (positive for additions, negative for reductions)</param>
        /// <remarks>
        /// This method is called for each individual orderbook change detected. It creates
        /// a timestamped change record, attempts to correlate it with trade events, and
        /// checks for order cancellations. The change is added to the processing queue
        /// and metrics are marked as needing recalculation.
        /// </remarks>
        public void RecordOrderbookChange(string side, int price, int deltaContracts)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Orderbook change logging cancelled for {MarketTicker}", _marketTicker);
                return;
            }

            // Input validation for data integrity
            if (string.IsNullOrWhiteSpace(side))
            {
                _logger.LogWarning("Invalid orderbook change for {MarketTicker}: side is null or empty", _marketTicker);
                return;
            }

            if (side != "yes" && side != "no")
            {
                _logger.LogWarning("Invalid orderbook change for {MarketTicker}: side '{Side}' is not 'yes' or 'no'", _marketTicker, side);
                return;
            }

            if (price < 0 || price > 100)
            {
                _logger.LogWarning("Invalid orderbook change for {MarketTicker}: price {Price} is outside valid range (0-100)", _marketTicker, price);
                return;
            }

            if (deltaContracts == 0)
            {
                _logger.LogWarning("Invalid orderbook change for {MarketTicker}: deltaContracts is 0 (no change)", _marketTicker);
                return;
            }

            var eventProcessingStopwatch = _enablePerformanceMetrics ? Stopwatch.StartNew() : null;

            if (deltaContracts != 0)
            {
                var recordTime = DateTime.UtcNow;
                _logger.LogInformation("Recording orderbook change for {MarketTicker}: Side={Side}, Price={Price}, Delta={DeltaContracts}, RecordTime={RecordTime}",
                    _marketTicker, side, price, deltaContracts, recordTime);
                var change = new OrderbookChange
                {
                    Sequence = _lastSequence++,
                    Side = side,
                    Price = price,
                    DeltaContracts = deltaContracts,
                    Timestamp = recordTime,
                    IsTradeRelated = false
                };

                _lastEventTime = change.Timestamp;

                lock (_orderbookMatchingLock)
                {
                    var expectedRollOffTime = change.Timestamp + TimeSpan.FromMinutes(5);
                    _logger.LogDebug(
                        "Received orderbook change for {MarketTicker}: ChangeID={ChangeID}, Side={Side}, Price={Price}, DeltaContracts={DeltaContracts}, Timestamp={Timestamp:F1}, ExpectedRollOffTime={ExpectedRollOffTime}",
                        _marketTicker, change.Id, change.Side, change.Price, change.DeltaContracts, change.Timestamp, expectedRollOffTime);

                    if (deltaContracts < 0)
                        FindMatchingTrade(change);

                    _orderbookChanges.Enqueue(change);
                    DetectCancelingOrderbookChange(change);
                }

                _logger.LogDebug("Logged change for {MarketTicker}: ChangeID={ChangeID}, Side={Side}, Price={Price}, Delta={Delta}, Sequence={Sequence}, IsTradeRelated={IsTradeRelated}, IsCanceled={IsCanceled}",
                    _marketTicker, change.Id, side, price, deltaContracts, change.Sequence, change.IsTradeRelated, change.IsCanceled);
                MetricsNeedRecalculation = true;

                if (eventProcessingStopwatch != null)
                {
                    eventProcessingStopwatch.Stop();
                    _totalEventProcessingTimeMs += eventProcessingStopwatch.ElapsedMilliseconds;
                    _eventProcessingCount++;
                }
            }
        }

        /// <summary>
        /// Records a trade event with the specified parameters and attempts to match it
        /// with corresponding orderbook changes. The trade is enqueued for processing
        /// and metrics are marked for recalculation.
        /// </summary>
        /// <param name="takerSide">The side of the taker in the trade ("yes" or "no")</param>
        /// <param name="yesPrice">The price on the yes side of the trade</param>
        /// <param name="noPrice">The price on the no side of the trade</param>
        /// <param name="count">The number of contracts traded</param>
        /// <param name="timestamp">The timestamp when the trade occurred</param>
        /// <remarks>
        /// This method processes incoming trade data, creates a TradeEvent object,
        /// and attempts to correlate it with orderbook changes that may have caused
        /// the trade. Successful matches help distinguish between market-making
        /// activity and actual trading volume.
        /// </remarks>
        public void RecordTrade(string takerSide, int yesPrice, int noPrice, int count, DateTime timestamp)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Trade logging cancelled for {MarketTicker}", _marketTicker);
                return;
            }

            // Input validation for trade data integrity
            if (string.IsNullOrWhiteSpace(takerSide))
            {
                _logger.LogWarning("Invalid trade for {MarketTicker}: takerSide is null or empty", _marketTicker);
                return;
            }

            if (takerSide != "yes" && takerSide != "no")
            {
                _logger.LogWarning("Invalid trade for {MarketTicker}: takerSide '{TakerSide}' is not 'yes' or 'no'", _marketTicker, takerSide);
                return;
            }

            if (yesPrice < 0 || yesPrice > 100)
            {
                _logger.LogWarning("Invalid trade for {MarketTicker}: yesPrice {YesPrice} is outside valid range (0-100)", _marketTicker, yesPrice);
                return;
            }

            if (noPrice < 0 || noPrice > 100)
            {
                _logger.LogWarning("Invalid trade for {MarketTicker}: noPrice {NoPrice} is outside valid range (0-100)", _marketTicker, noPrice);
                return;
            }

            if (count <= 0)
            {
                _logger.LogWarning("Invalid trade for {MarketTicker}: count {Count} must be positive", _marketTicker, count);
                return;
            }

            if (timestamp == default || timestamp > DateTime.UtcNow.AddMinutes(1) || timestamp < DateTime.UtcNow.AddHours(-24))
            {
                _logger.LogWarning("Invalid trade for {MarketTicker}: timestamp {Timestamp} is invalid or outside reasonable range", _marketTicker, timestamp);
                return;
            }

            var eventProcessingStopwatch = _enablePerformanceMetrics ? Stopwatch.StartNew() : null;

            var trade = new TradeEvent
            {
                TakerSide = takerSide,
                YesPrice = yesPrice,
                NoPrice = noPrice,
                Count = count,
                Timestamp = timestamp
            };

            var expectedRollOffTime = trade.Timestamp + TimeSpan.FromMinutes(5);

            _logger.LogDebug(
                "Received trade for {MarketTicker}: TradeID={TradeID}, TakerSide={TakerSide}, YesPrice={YesPrice}, NoPrice={NoPrice}, Count={Count}, Timestamp={Timestamp}, ExpectedRollOffTime={ExpectedRollOffTime}",
                _marketTicker, trade.Id, trade.TakerSide, trade.YesPrice, trade.NoPrice, trade.Count, trade.Timestamp, expectedRollOffTime);

            FindMatchingOrderbookChange(trade);

            if (trade.HasMatchingOrderbookChange)
            {
                _logger.LogDebug(
                    "TRADEMON-Trade matched with orderbook change for {MarketTicker}: TradeID={TradeID}, TakerSide={TakerSide}, YesPrice={YesPrice}, NoPrice={NoPrice}, Count={Count}, Timestamp={Timestamp}",
                    _marketTicker, trade.Id, trade.TakerSide, trade.YesPrice, trade.NoPrice, trade.Count, trade.Timestamp);
            }
            else
            {
                _logger.LogDebug("TRADEMON-Trade {TradeID} not matched to existing change when processed", trade.Id);
            }

            _tradeEvents.Enqueue(trade);

            if (!trade.HasMatchingOrderbookChange)
            {
                _logger.LogDebug("Trade for {MarketTicker} had no matching orderbook change: TakerSide={TakerSide}, YesPrice={YesPrice}, NoPrice={NoPrice}, Count={Count}, Timestamp={Timestamp}, EventId={EventId}",
                    _marketTicker, trade.TakerSide, trade.YesPrice, trade.NoPrice, trade.Count, trade.Timestamp, trade.Id);
            }

            _logger.LogDebug("Logged trade for {MarketTicker}: TakerSide={TakerSide}, YesPrice={YesPrice}, NoPrice={NoPrice}, Count={Count}, Timestamp={Timestamp}, HasMatchingOrderbookChange={HasMatchingOrderbookChange}, EventId={EventId}",
                _marketTicker, takerSide, yesPrice, noPrice, count, timestamp, trade.HasMatchingOrderbookChange, trade.Id);

            MetricsNeedRecalculation = true;

            if (eventProcessingStopwatch != null)
            {
                eventProcessingStopwatch.Stop();
                _totalEventProcessingTimeMs += eventProcessingStopwatch.ElapsedMilliseconds;
                _eventProcessingCount++;
            }
        }

        private bool FindMatchingOrderbookChange(TradeEvent trade)
        {
            var stopwatch = Stopwatch.StartNew();
            lock (_orderbookMatchingLock)
            {
                _totalMatchingOperations++;
                if (_cancellationToken.IsCancellationRequested || trade.HasMatchingOrderbookChange)
                {
                    _logger.LogDebug("Trade already matched or cancelled: TradeID={TradeID}", trade.Id);
                    stopwatch.Stop();
                    _totalProcessingTimeMs += stopwatch.ElapsedMilliseconds;
                    return trade.HasMatchingOrderbookChange;
                }

                var cutoff = trade.Timestamp - TimeSpan.FromSeconds(5);
                var futureCutoff = trade.Timestamp + TimeSpan.FromSeconds(5);
                string expectedOrderBookSide = trade.TakerSide == "yes" ? "no" : "yes";
                int tradePriceToMatch = trade.TakerSide == "yes" ? trade.NoPrice : trade.YesPrice;
                OrderbookChange? bestMatch = null;
                double minTimeDiff = double.MaxValue;

                foreach (var change in _orderbookChanges)
                {
                    if (change.IsTradeRelated || change.DeltaContracts >= 0 || change.Side != expectedOrderBookSide)
                        continue;

                    bool isWithinTimeWindow = change.Timestamp >= cutoff && change.Timestamp <= futureCutoff;
                    if (!isWithinTimeWindow || change.Price != tradePriceToMatch || Math.Abs(change.DeltaContracts) != trade.Count)
                        continue;

                    double timeDiff = Math.Abs((change.Timestamp - trade.Timestamp).TotalSeconds);
                    if (!change.IsCanceled && timeDiff < minTimeDiff)
                    {
                        bestMatch = change;
                        minTimeDiff = timeDiff;
                    }
                }

                if (bestMatch != null)
                {
                    bestMatch.IsTradeRelated = true;
                    bestMatch.MatchedTradeId = trade.Id;
                    trade.HasMatchingOrderbookChange = true;
                    _successfulMatches++;
                    _logger.LogDebug(
                        "TRADEMON-Matched orderbook change to trade in {MarketTicker}: ChangeID={ChangeID}, Side={Side}, Price={Price}, Delta={Delta}, TradeID={TradeID}, TradeTimestamp={TradeTimestamp}, TimeDiff={TimeDiff:F2}s",
                        _marketTicker, bestMatch.Id, bestMatch.Side, bestMatch.Price, bestMatch.DeltaContracts, trade.Id, trade.Timestamp, minTimeDiff);
                    stopwatch.Stop();
                    _totalProcessingTimeMs += stopwatch.ElapsedMilliseconds;
                    return true;
                }

                // Fallback to canceled changes (unchanged logic)
                OrderbookChange? bestCanceledMatch = null;
                foreach (var change in _orderbookChanges)
                {
                    if (change.IsTradeRelated || change.DeltaContracts >= 0 || change.Side != expectedOrderBookSide || !change.IsCanceled || change.MatchedTradeId != null)
                        continue;

                    bool isWithinTimeWindow = change.Timestamp >= cutoff && change.Timestamp <= futureCutoff;
                    if (!isWithinTimeWindow || change.Price != tradePriceToMatch || Math.Abs(change.DeltaContracts) != trade.Count)
                        continue;

                    bestCanceledMatch = change;
                    break;
                }

                if (bestCanceledMatch != null)
                {
                    bestCanceledMatch.IsCanceled = false;
                    bestCanceledMatch.IsTradeRelated = true;
                    bestCanceledMatch.MatchedTradeId = trade.Id;
                    trade.HasMatchingOrderbookChange = true;
                    _successfulMatches++;
                    _logger.LogDebug(
                        "TRADEMON-Reclassified canceled orderbook change for trade in {MarketTicker}: ChangeID={ChangeID}, Side={Side}, Price={Price}, Delta={Delta}, TradeID={TradeID}, TradeTimestamp={TradeTimestamp}",
                        _marketTicker, bestCanceledMatch.Id, bestCanceledMatch.Side, bestCanceledMatch.Price, bestCanceledMatch.DeltaContracts, trade.Id, trade.Timestamp);
                    stopwatch.Stop();
                    _totalProcessingTimeMs += stopwatch.ElapsedMilliseconds;
                    return true;
                }

                _logger.LogDebug(
                    "No matching orderbook change found for trade in {MarketTicker}: TradeID={TradeID}, TakerSide={TakerSide}, Price={Price}, Count={Count}, Timestamp={Timestamp}",
                    _marketTicker, trade.Id, trade.TakerSide, tradePriceToMatch, trade.Count, trade.Timestamp);
                stopwatch.Stop();
                _totalProcessingTimeMs += stopwatch.ElapsedMilliseconds;
                return false;
            }
        }

        private bool FindMatchingTrade(OrderbookChange change)
        {
            lock (_orderbookMatchingLock)
            {
                if (_cancellationToken.IsCancellationRequested || change.DeltaContracts >= 0)
                    return false;

                var cutoff = change.Timestamp - TimeSpan.FromSeconds(5);
                foreach (var trade in _tradeEvents)
                {
                    if (trade.HasMatchingOrderbookChange)
                        continue;

                    string expectedOrderBookSide = trade.TakerSide == "yes" ? "no" : "yes";
                    if (change.Side != expectedOrderBookSide)
                        continue;

                    int tradePriceToMatch = trade.TakerSide == "yes" ? trade.NoPrice : trade.YesPrice;
                    if (change.Price != tradePriceToMatch || Math.Abs(change.DeltaContracts) != trade.Count)
                        continue;

                    bool isWithinTimeWindow = trade.Timestamp >= cutoff;
                    _logger.LogDebug(
                        "TRADEMON-Matched trade to orderbook change in {MarketTicker}: ChangeID={ChangeID}, Side={Side}, Price={Price}, Delta={Delta}, TradeID={TradeID}, TradeTimestamp={TradeTimestamp}, WithinTimeWindow={WithinTimeWindow}",
                        _marketTicker, change.Id, change.Side, change.Price, change.DeltaContracts, trade.Id, trade.Timestamp, isWithinTimeWindow);
                    trade.HasMatchingOrderbookChange = true;
                    change.IsTradeRelated = true;
                    change.MatchedTradeId = trade.Id;
                    return true;
                }

                return false;
            }
        }

        private void DetectCancelingOrderbookChange(OrderbookChange newChange)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Canceling orderbook change check cancelled for {MarketTicker}", _marketTicker);
                return;
            }

            if (newChange.IsCanceled || newChange.IsTradeRelated)
            {
                _logger.LogDebug("Skipping canceling check for already canceled or trade-related change: ChangeID={ChangeID}", newChange.Id);
                return;
            }

            var cutoff = newChange.Timestamp - TimeSpan.FromSeconds(10);
            lock (_orderbookMatchingLock)
            {
                foreach (var existingChange in _orderbookChanges)
                {
                    if (_cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogDebug("Canceling orderbook change loop cancelled for {MarketTicker}", _marketTicker);
                        return;
                    }

                    if (existingChange.IsCanceled || existingChange == newChange || existingChange.IsTradeRelated)
                    {
                        continue;
                    }

                    bool isWithinTimeWindow = existingChange.Timestamp >= cutoff && existingChange.Timestamp <= newChange.Timestamp;
                    if (!isWithinTimeWindow)
                    {
                        continue;
                    }

                    if (existingChange.Side != newChange.Side || existingChange.Price != newChange.Price)
                    {
                        continue;
                    }

                    if (existingChange.DeltaContracts + newChange.DeltaContracts == 0)
                    {
                        existingChange.IsCanceled = true;
                        newChange.IsCanceled = true;
                        _logger.LogDebug(
                            "TRADEMON-Matched canceling orderbook changes in {MarketTicker}: Change1Id={Change1Id}, Change2Id={Change2Id}, Side={Side}, Price={Price}, Delta1={Delta1}, Delta2={Delta2}, Timestamp1={Timestamp1}, Timestamp2={Timestamp2}",
                            _marketTicker, existingChange.Id, newChange.Id, newChange.Side, newChange.Price, existingChange.DeltaContracts, newChange.DeltaContracts, existingChange.Timestamp, newChange.Timestamp);
                        MetricsNeedRecalculation = true;
                    }
                }
            }

            if (newChange.IsCanceled)
            {
                _logger.LogDebug("Change marked as canceled: ChangeID={ChangeID}, Side={Side}, Price={Price}, DeltaContracts={DeltaContracts}",
                    newChange.Id, newChange.Side, newChange.Price, newChange.DeltaContracts);
            }
        }

        #endregion

        #region Metric Recalculation
        private void OnRecalculationTimerElapsed(object sender, ElapsedEventArgs e)
        {
            var timerExecutionStopwatch = _enablePerformanceMetrics ? Stopwatch.StartNew() : null;

            if (_enablePerformanceMetrics)
            {
                if (_lastRecalculationTimerElapsed != DateTime.MinValue)
                {
                    var expectedInterval = TimeSpan.FromSeconds(10);
                    var actualInterval = DateTime.UtcNow - _lastRecalculationTimerElapsed;
                    var drift = actualInterval - expectedInterval;
                    _totalTimerDriftMs += (long)drift.TotalMilliseconds;
                }

                _lastRecalculationTimerElapsed = DateTime.UtcNow;
                _timerCallbackCount++;
            }

            RecalculateAllMetrics();

            if (timerExecutionStopwatch != null)
            {
                timerExecutionStopwatch.Stop();
                _totalTimerExecutionTimeMs += timerExecutionStopwatch.ElapsedMilliseconds;
            }

            // Post metrics to central performance monitor
            if (_enablePerformanceMetrics)
            {
                if (_eventProcessingCount > 0)
                {
                    long avgEventProcessingTime = _totalEventProcessingTimeMs / _eventProcessingCount;
                    RecordExecutionTimePrivate($"OrderbookChangeTracker_{_marketTicker}_AverageEventProcessingTimeMs", avgEventProcessingTime, _enablePerformanceMetrics);
                }

                if (_timerCallbackCount > 1)
                {
                    long avgDrift = _totalTimerDriftMs / (_timerCallbackCount - 1);
                    RecordExecutionTimePrivate($"OrderbookChangeTracker_{_marketTicker}_AverageTimerDriftMs", avgDrift, _enablePerformanceMetrics);
                }
                if (_timerCallbackCount > 0)
                {
                    long avgExecutionTime = _totalTimerExecutionTimeMs / _timerCallbackCount;
                    RecordExecutionTimePrivate($"OrderbookChangeTracker_{_marketTicker}_AverageTimerExecutionTimeMs", avgExecutionTime, _enablePerformanceMetrics);
                }
            }
        }

        /// <summary>
        /// Triggers a complete recalculation of all market metrics including cleanup of old events.
        /// This method performs standard metrics recalculation and updates current snapshot metrics.
        /// </summary>
        /// <remarks>
        /// This method is called periodically by the recalculation timer and also on-demand
        /// when significant changes occur. It ensures all metrics remain current and accurate.
        /// </remarks>
        public void RecalculateAllMetrics()
        {
            // Do not run if cancellation has been requested.
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Recalculation cancelled for {MarketTicker}", _marketTicker);
                return;
            }

            _logger.LogDebug("Recalculation timer triggered for {MarketTicker}", _marketTicker);

            // Perform standard metrics recalculation (includes cleanup of old events/trades).
            RecalculateMetrics();

            // After standard metrics, compute and push snapshot-scoped metrics onto Market.  This call
            // writes directly to the Market's "Current*" fields.  Those fields are expected
            // to exist on the Market type; if they do not, the assignment will fail to compile.
            // It is invoked on the same 3-second cadence as the general metrics so that both
            // sets of measurements remain in sync.
            UpdateCurrentSnapshotMetrics();
        }

        private void RecalculateMetrics()
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Metric recalculation cancelled for {MarketTicker}", _marketTicker);
                return;
            }
            _logger.LogDebug("Recalculating metrics for {MarketTicker}", _marketTicker);
            if (LastMarketOpenTime == DateTime.MinValue)
            {
                _logger.LogDebug("Skipping recalculation for {MarketTicker}: Market is not yet initialized", _marketTicker);
                return;
            }

            CleanupOldEvents();
            CleanupOldTrades();

            if (!MetricsNeedRecalculation) return;
            MetricsNeedRecalculation = false;

            foreach (var trade in _tradeEvents.Where(x => x.HasMatchingOrderbookChange == false))
            {
                if (_cancellationToken.IsCancellationRequested)
                {
                    _logger.LogDebug("Trade matching in recalculation cancelled for {MarketTicker}", _marketTicker);
                    return;
                }
                FindMatchingOrderbookChange(trade);
                if (trade.HasMatchingOrderbookChange)
                {
                    _logger.LogDebug(
                        "TRADEMON-During cleanup, trade matched with orderbook change for {MarketTicker}: TradeID={TradeID}, TakerSide={TakerSide}, YesPrice={YesPrice}, NoPrice={NoPrice}, Count={Count}, Timestamp={Timestamp}",
                        _marketTicker, trade.Id, trade.TakerSide, trade.YesPrice, trade.NoPrice, trade.Count, trade.Timestamp);
                }
            }

            var stopwatch = Stopwatch.StartNew();
            OrderbookChange[] orderbookChanges;
            lock (_orderbookMatchingLock)
            {
                orderbookChanges = _orderbookChanges.ToArray();
            }
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > 100)
            {
                _logger.LogWarning(
                    "Waited {WaitTime} ms for lock in RecalculateMetrics for {MarketTicker}",
                    stopwatch.ElapsedMilliseconds,
                    _marketTicker);
            }

            _logger.LogDebug("After cleanup, processing {OrderbookChangeCount} order book changes and {TradeEventCount} trade events for {MarketTicker}",
                orderbookChanges.Length, _tradeEvents.Count, _marketTicker);

            List<OrderbookChange> validYesChanges = orderbookChanges
                .Where(c => !c.IsCanceled && c.Side == "yes")
                .ToList();
            List<OrderbookChange> validNoChanges = orderbookChanges
                .Where(c => !c.IsCanceled && c.Side == "no")
                .ToList();
            _logger.LogDebug("Processing {YesChangeCount} Yes-side and {NoChangeCount} No-side order book changes for {MarketTicker}",
                validYesChanges.Count, validNoChanges.Count, _marketTicker);

            var (isValid, invalidCount) = ValidateEvents(validYesChanges.Concat(validNoChanges).ToList());
            if (!isValid)
            {
                _logger.LogWarning("Event validation failed for {MarketTicker}: {InvalidCount} invalid events found, skipping metric recalculation", _marketTicker, invalidCount);
                MarketInvalid?.Invoke(this, _marketTicker);
                return;
            }

            if (Market == null)
            {
                _logger.LogWarning("OrderbookChangeTracker survived market {ticker}", _marketTicker);
                return;
            }
            List<OrderbookData> bids = new List<OrderbookData>(Market.GetBids());
            _logger.LogDebug("RecalculateMetrics for {MarketTicker}: 5.0={5.0}", _marketTicker, 5.0);

            double velocitySumYes = validYesChanges.Sum(c => c.Price / 100.0 * c.DeltaContracts);
            double orderVolumeYesBid = validYesChanges.Where(c => !c.IsTradeRelated).Sum(c => c.Price / 100.0 * c.DeltaContracts);
            double tradeVolumeYes = validYesChanges.Where(c => c.IsTradeRelated).Sum(c => c.Price / 100.0 * c.DeltaContracts);

            double velocitySumNo = validNoChanges.Sum(c => c.Price / 100.0 * c.DeltaContracts);
            double orderVolumeNoBid = validNoChanges.Where(c => !c.IsTradeRelated).Sum(c => c.Price / 100.0 * c.DeltaContracts);
            double tradeVolumeNo = validNoChanges.Where(c => c.IsTradeRelated).Sum(c => c.Price / 100.0 * c.DeltaContracts);

            double velocitySum = velocitySumYes + velocitySumNo;
            double rateSum = (orderVolumeYesBid + tradeVolumeYes) + (orderVolumeNoBid + tradeVolumeNo);

            velocitySum = Math.Round(velocitySum, 2);
            velocitySumYes = Math.Round(velocitySumYes, 2);
            velocitySumNo = Math.Round(velocitySumNo, 2);
            orderVolumeYesBid = Math.Round(orderVolumeYesBid, 2);
            tradeVolumeYes = Math.Round(tradeVolumeYes, 2);
            orderVolumeNoBid = Math.Round(orderVolumeNoBid, 2);
            tradeVolumeNo = Math.Round(tradeVolumeNo, 2);
            rateSum = Math.Round(rateSum, 2);

            if (Math.Abs(velocitySum - rateSum) > 0.01)
            {
                _logger.LogWarning("Inconsistent raw totals for {MarketTicker}: VelocitySum={VelocitySum:F2} (Yes={VelocitySumYes:F2}, No={VelocitySumNo:F2}), RateSum={RateSum:F2} (Yes={YesRateSum:F2}, No={NoRateSum:F2}), Diff={Diff:F2}",
                    _marketTicker, velocitySum, velocitySumYes, velocitySumNo, rateSum, orderVolumeYesBid + tradeVolumeYes, orderVolumeNoBid + tradeVolumeNo, velocitySum - rateSum);
            }

            foreach (var change in validYesChanges.Concat(validNoChanges))
            {
                if (_cancellationToken.IsCancellationRequested)
                {
                    _logger.LogDebug("Velocity sum logging cancelled for {MarketTicker}", _marketTicker);
                    return;
                }
                double dollarDelta = change.Price / 100.0 * change.DeltaContracts;
                _logger.LogDebug("VelocitySum contribution for {MarketTicker}: ChangeID={ChangeID}, Side={Side}, Price={Price}, DeltaContracts={DeltaContracts}, DollarDelta={DollarDelta:F2}, IsTradeRelated={IsTradeRelated}",
                    _marketTicker, change.Id, change.Side, change.Price, change.DeltaContracts, dollarDelta, change.IsTradeRelated);
            }

            _logger.LogDebug("VelocitySum total dollar delta for {MarketTicker}: {VelocitySum:F2} (Yes={VelocitySumYes:F2}, No={VelocitySumNo:F2})",
                _marketTicker, velocitySum, velocitySumYes, velocitySumNo);
            _logger.LogDebug("OrderVolume_Yes_Bid total dollar delta for {MarketTicker}: {OrderVolume:F2}, {OrderCount} events",
                _marketTicker, orderVolumeYesBid, validYesChanges.Count(c => !c.IsTradeRelated));
            _logger.LogDebug("TradeVolume_Yes total dollar delta for {MarketTicker}: {TradeVolume:F2}, {TradeCount} events",
                _marketTicker, tradeVolumeYes, validYesChanges.Count(c => c.IsTradeRelated));
            _logger.LogDebug("OrderVolume_No_Bid total dollar delta for {MarketTicker}: {OrderVolume:F2}, {OrderCount} events",
                _marketTicker, orderVolumeNoBid, validNoChanges.Count(c => !c.IsTradeRelated));
            _logger.LogDebug("TradeVolume_No total dollar delta for {MarketTicker}: {TradeVolume:F2}, {TradeCount} events",
                _marketTicker, tradeVolumeNo, validNoChanges.Count(c => c.IsTradeRelated));
            _logger.LogDebug("RateSum total dollar delta for {MarketTicker}: {RateSum:F2}", _marketTicker, rateSum);

            RefreshOrderbookChangeOverTimeMetrics(validYesChanges.Concat(validNoChanges).ToList(), bids);
            RefreshTradeChangeOverTimeMetrics(validYesChanges.Concat(validNoChanges).ToList());
        }

        private void RefreshOrderbookChangeOverTimeMetrics(List<OrderbookChange> orderbookChanges,
    List<OrderbookData> bids)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Orderbook change over time metrics refresh cancelled for {MarketTicker}", _marketTicker);
                return;
            }

            _logger.LogDebug("Starting RefreshOrderbookChangeOverTimeMetrics for {MarketTicker}, BestYesBid={BestYesBid}, BestNoBid={BestNoBid}",
                _marketTicker, Market.BestYesBid, Market.BestNoBid);

            List<OrderbookData> noBids = bids.Where(x => x.Side == "no").ToList();
            List<OrderbookData> yesBids = bids.Where(x => x.Side == "yes").ToList();

            int yesThreshold = yesBids.Any() ? (int)Math.Floor(yesBids.Max(x => x.Price) * 0.9) : 0;
            int noThreshold = noBids.Any() ? (int)Math.Floor(noBids.Max(x => x.Price) * 0.9) : 0;

            var YesTopVelocity = GetTopYesVelocityPerMinute(yesBids, orderbookChanges, yesThreshold);
            var YesBottomVelocity = GetBottomYesVelocityPerMinute(yesBids, orderbookChanges, yesThreshold);
            var NoTopVelocity = GetTopNoVelocityPerMinute(noBids, orderbookChanges, noThreshold);
            var NoBottomVelocity = GetBottomNoVelocityPerMinute(noBids, orderbookChanges, noThreshold);

            Market.LevelCount_Top_Yes_Bid = YesTopVelocity.Levels;
            Market.LevelCount_Bottom_Yes_Bid = YesBottomVelocity.Levels;
            Market.LevelCount_Top_No_Bid = NoTopVelocity.Levels;
            Market.LevelCount_Bottom_No_Bid = NoBottomVelocity.Levels;

            Market.VelocityPerMinute_Bottom_Yes_Bid = Math.Round(YesBottomVelocity.Volume, 2);
            Market.VelocityPerMinute_Top_Yes_Bid = Math.Round(YesTopVelocity.Volume, 2);
            Market.VelocityPerMinute_Bottom_No_Bid = Math.Round(NoBottomVelocity.Volume, 2);
            Market.VelocityPerMinute_Top_No_Bid = Math.Round(NoTopVelocity.Volume, 2);

            _logger.LogDebug(
                "{MarketTicker}: Completed RefreshOrderbookChangeOverTimeMetrics  " +
                "YesBidTop={YesBidTopRate}/min ({YesBidTopLevels} levels), " +
                "YesBidBottom={YesBidBottomRate}/min ({YesBidBottomLevels} levels), " +
                "NoBidTop={NoBidTopRate}/min ({NoBidTopLevels} levels), " +
                "NoBidBottom={NoBidBottomRate}/min ({NoBidBottomLevels} levels)",
                _marketTicker,
                YesTopVelocity.Volume, YesTopVelocity.Levels,
                YesBottomVelocity.Volume, YesBottomVelocity.Levels,
                NoTopVelocity.Volume, NoTopVelocity.Levels,
                NoBottomVelocity.Volume, NoBottomVelocity.Levels);
        }

        private void RefreshTradeChangeOverTimeMetrics(List<OrderbookChange> orderbookChanges)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Trade change over time metrics refresh cancelled for {MarketTicker}", _marketTicker);
                return;
            }

            _logger.LogDebug("Starting RefreshTradeChangeOverTimeMetrics for {MarketTicker}", _marketTicker);

            List<OrderbookChange> yesMakerNonTradeChanges = orderbookChanges
                .Where(c => c.Side == "yes" && !c.IsTradeRelated && !c.IsCanceled)
                .ToList();
            List<OrderbookChange> noMakerNonTradeChanges = orderbookChanges
                .Where(c => c.Side == "no" && !c.IsTradeRelated && !c.IsCanceled)
                .ToList();
            List<OrderbookChange> yesMakerTradeRelatedChanges = orderbookChanges
                .Where(c => c.Side == "yes" && c.IsTradeRelated && !c.IsCanceled)
                .ToList();
            List<OrderbookChange> noMakerTradeRelatedChanges = orderbookChanges
                .Where(c => c.Side == "no" && c.IsTradeRelated && !c.IsCanceled)
                .ToList();

            var yesBidNetOrderRate = GetYesNetOrderVolumePerMinute(yesMakerNonTradeChanges);
            var noBidNetOrderRate = GetNoNetOrderVolumePerMinute(noMakerNonTradeChanges);
            var yesTradeRate = GetTradeRatePerMinute_MakerYes(yesMakerTradeRelatedChanges);
            var noTradeRate = GetTradeRatePerMinute_MakerNo(noMakerTradeRelatedChanges);
            var averageTradeSizeYes = GetAverageTradeSize_MakerYes(yesMakerTradeRelatedChanges);
            var averageTradeSizeNo = GetAverageTradeSize_MakerNo(noMakerTradeRelatedChanges);

            Market.OrderVolumePerMinute_YesBid = Math.Round(yesBidNetOrderRate.Volume, 2);
            Market.OrderVolumePerMinute_NoBid = Math.Round(noBidNetOrderRate.Volume, 2);

            Market.NonTradeRelatedOrderCount_Yes = yesBidNetOrderRate.Count;
            Market.NonTradeRelatedOrderCount_No = noBidNetOrderRate.Count;

            Market.TradeVolumePerMinute_Yes = Math.Round(yesTradeRate.volume, 2);
            Market.TradeVolumePerMinute_No = Math.Round(noTradeRate.volume, 2);
            Market.TradeRatePerMinute_Yes = Math.Round(yesTradeRate.rate, 2);
            Market.TradeRatePerMinute_No = Math.Round(noTradeRate.rate, 2);

            Market.AverageTradeSize_Yes = Math.Round(averageTradeSizeYes, 2);
            Market.AverageTradeSize_No = Math.Round(averageTradeSizeNo, 2);
            Market.TradeCount_Yes = yesMakerTradeRelatedChanges.Count;
            Market.TradeCount_No = noMakerTradeRelatedChanges.Count;

            if (Market.TradeCount_Yes == 0 && Market.TradeVolumePerMinute_Yes != 0)
            {
                _logger.LogWarning("TradeCount_Yes is 0 but TradeVolumePerMinute_Yes is not for {MarketTicker}", _marketTicker);
            }
            if (Market.TradeCount_No == 0 && Market.TradeVolumePerMinute_No != 0)
            {
                _logger.LogWarning("TradeCount_No is 0 but TradeVolumePerMinute_No is not for {MarketTicker}", _marketTicker);
            }

            _logger.LogDebug(
                "{MarketTicker}: Completed RefreshTradeChangeOverTimeMetrics " +
                "YesBidNetOrderRate={YesBidNetOrderRate:F2}/min ({YesOrderCount} events), " +
                "NoBidNetOrderRate={NoBidNetOrderRate:F2}/min ({NoOrderCount} events), " +
                "AverageTradeSize_Yes={AverageTradeSize_Yes:F2}, " +
                "AverageTradeSize_No={AverageTradeSize_No:F2}, " +
                "YesTradeRate={YesTradeRate:F2}/min, " +
                "NoTradeRate={NoTradeRate:F2}/min, " +
                "TradeCount_Yes={TradeCount_Yes}, " +
                "TradeCount_No={TradeCount_No}",
                _marketTicker,
                yesBidNetOrderRate.Volume, yesBidNetOrderRate.Count,
                noBidNetOrderRate.Volume, noBidNetOrderRate.Count,
                averageTradeSizeYes,
                averageTradeSizeNo,
                yesTradeRate.rate,
                noTradeRate.rate,
                yesMakerTradeRelatedChanges.Count,
                noMakerTradeRelatedChanges.Count);
        }
        #endregion

        #region Cleanup and Utility Methods
        private void ClearEventQueues()
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Event reset cancelled for {MarketTicker}", _marketTicker);
                return;
            }

            _lastEventTime = DateTime.MinValue;
            LastMarketOpenTime = DateTime.UtcNow;
            _orderbookChanges.Clear();
            _tradeEvents.Clear();
            _lastSequence = 0;
            MetricsNeedRecalculation = true;
            _logger.LogDebug("TRADEMON-{MarketTicker} Events reset, all queues cleared, sequence reset at {Time}",
                _marketTicker, LastMarketOpenTime);
        }

        private void CleanupOldEvents()
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Old event cleanup cancelled for {MarketTicker}", _marketTicker);
                return;
            }

            var cutoff = DateTime.UtcNow - TimeSpan.FromMinutes(_trackerConfig.Value.EventCalculationPeriod);
            int removedCount = 0;
            int queueSizeBefore = _orderbookChanges.Count;

            // Then, remove events older than the cleanup threshold
            while (_orderbookChanges.TryPeek(out var change) && change.Timestamp < cutoff)
            {
                if (_cancellationToken.IsCancellationRequested)
                {
                    _logger.LogDebug("Old event cleanup loop cancelled for {MarketTicker}", _marketTicker);
                    return;
                }

                if (_orderbookChanges.TryDequeue(out change))
                {
                    removedCount++;
                    _logger.LogDebug(
                        "Orderbook change rolled off for {MarketTicker}: ChangeID={ChangeID}, Side={Side}, Price={Price}, DeltaContracts={DeltaContracts}, Timestamp={Timestamp}, RollOffTime={RollOffTime}, IsCanceled={IsCanceled}, IsTradeRelated={IsTradeRelated}",
                        _marketTicker, change.Id, change.Side, change.Price, change.DeltaContracts, change.Timestamp, DateTime.UtcNow, change.IsCanceled, change.IsTradeRelated);
                }
            }

            if (removedCount > 0)
            {
                MetricsNeedRecalculation = true;
                _logger.LogDebug("Cleaned up {Count} old order book events for {MarketTicker} (queue size: {Before} -> {After})",
                    removedCount, _marketTicker, queueSizeBefore, _orderbookChanges.Count);
            }
        }

        private (bool isValid, int invalidCount) ValidateEvents(List<OrderbookChange> orderbookChanges)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Event validation cancelled for {MarketTicker}", _marketTicker);
                return (false, 0);
            }

            bool isValid = true;
            HashSet<string> changeIds = new HashSet<string>();
            int invalidCount = 0;

            foreach (var change in orderbookChanges)
            {
                if (_cancellationToken.IsCancellationRequested)
                {
                    _logger.LogDebug("Event validation loop cancelled for {MarketTicker}", _marketTicker);
                    return (false, invalidCount);
                }

                if (!changeIds.Add(change.Id))
                {
                    _logger.LogWarning("DUP-Duplicate ChangeID detected for {MarketTicker}: ChangeID={ChangeID}, Side={Side}, Price={Price}, DeltaContracts={DeltaContracts}, Timestamp={Timestamp}",
                        _marketTicker, change.Id, change.Side, change.Price, change.DeltaContracts, change.Timestamp);
                    isValid = false;
                    invalidCount++;
                    continue;
                }

                if (change.Price < 0 || change.Price > 100)
                {
                    _logger.LogWarning("Invalid price for {MarketTicker}: ChangeID={ChangeID}, Price={Price}, DeltaContracts={DeltaContracts}, Timestamp={Timestamp}",
                        _marketTicker, change.Id, change.Price, change.DeltaContracts, change.Timestamp);
                    isValid = false;
                    invalidCount++;
                    continue;
                }

                if (change.DeltaContracts == 0)
                {
                    _logger.LogWarning("Invalid DeltaContracts for {MarketTicker}: ChangeID={ChangeID}, Price={Price}, DeltaContracts={DeltaContracts}, Timestamp={Timestamp}",
                        _marketTicker, change.Id, change.Price, change.DeltaContracts, change.Timestamp);
                    isValid = false;
                    invalidCount++;
                    continue;
                }

                if (change.Timestamp > DateTime.UtcNow.AddMinutes(1) || change.Timestamp < DateTime.UtcNow.AddMinutes(-_trackerConfig.Value.EventCalculationPeriod))
                {
                    string reason;
                    if (change.Timestamp > DateTime.UtcNow.AddMinutes(1))
                    {
                        reason = $"timestamp is more than 1 minute in the future (current UTC: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss})";
                    }
                    else
                    {
                        reason = $"timestamp is more than {_trackerConfig.Value.EventCalculationPeriod} minutes in the past (current UTC: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss})";
                    }
                    _logger.LogWarning("Invalid timestamp for {MarketTicker}: ChangeID={ChangeID}, Timestamp={Timestamp}, Price={Price}, DeltaContracts={DeltaContracts}. Reason: {Reason}",
                        _marketTicker, change.Id, change.Timestamp, change.Price, change.DeltaContracts, reason);
                    isValid = false;
                    invalidCount++;
                }
            }

            if (invalidCount > 0)
            {
                _logger.LogWarning("Found {InvalidCount} invalid order book changes for {MarketTicker}", invalidCount, _marketTicker);
            }
            else
            {
                _logger.LogDebug("All order book changes validated successfully for {MarketTicker}", _marketTicker);
            }

            return (isValid, invalidCount);
        }

        private void CleanupOldTrades()
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var cutoff = DateTime.UtcNow - TimeSpan.FromMinutes(_trackerConfig.Value.EventCalculationPeriod);
            var gracePeriodEnd = LastMarketOpenTime.Add(TimeSpan.FromMinutes(5));
            int removedCount = 0;
            int warningCount = 0;
            int queueSizeBefore = _tradeEvents.Count;

            // remove trades older than the cleanup threshold
            while (_tradeEvents.TryPeek(out var trade) && trade.Timestamp < cutoff)
            {
                if (_cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (_tradeEvents.TryDequeue(out trade))
                {
                    bool isGracePeriodOver = trade.Timestamp > gracePeriodEnd;

                    if (!trade.HasMatchingOrderbookChange)
                    {
                        var matchingChange = FindMatchingOrderbookChangeForTrade(trade);
                        if (matchingChange != null)
                        {
                            trade.HasMatchingOrderbookChange = true;
                            matchingChange.IsTradeRelated = true;
                            matchingChange.MatchedTradeId = trade.Id;
                            MetricsNeedRecalculation = true;  // Trigger recalculation due to new match
                            _logger.LogDebug(
                                "Found matching orderbook change during cleanup for TradeID={TradeID}: ChangeID={ChangeID}, Side={Side}, Price={Price}, Delta={Delta}",
                                trade.Id, matchingChange.Id, matchingChange.Side, matchingChange.Price, matchingChange.DeltaContracts);
                        }
                        else if (isGracePeriodOver)
                        {
                            warningCount++;
                            _logger.LogWarning(
                                "TRADEMON-Trade cleared without matching orderbook change for {MarketTicker}: TradeID={TradeID}, TakerSide={TakerSide}, YesPrice={YesPrice}, NoPrice={NoPrice}, Count={Count}, Timestamp={Timestamp}, EventId={EventId}",
                                _marketTicker, trade.Id, trade.TakerSide, trade.YesPrice, trade.NoPrice, trade.Count, trade.Timestamp, trade.Id);
                            _logger.LogWarning(new TradeMissedException(_marketTicker,
                                $"Trade cleared from queue without matching orderbook change for {_marketTicker}"),
                                "TRADEMON-Trade cleared from queue without matching orderbook change for {marketTicker}", _marketTicker);
                            _logger.LogWarning(
                                "TRADEMON-Grace period status for {MarketTicker}: IsGracePeriodOver={IsGracePeriodOver}, GracePeriodStart={GracePeriodStart}, GracePeriodEnd={GracePeriodEnd}, GracePeriodDuration={GracePeriodDuration}, CurrentTime={CurrentTime}",
                                _marketTicker, isGracePeriodOver, LastMarketOpenTime, gracePeriodEnd, TimeSpan.FromMinutes(5), DateTime.UtcNow);
                        }
                    }
                    MetricsNeedRecalculation = true;
                    removedCount++;
                }
            }

            if (removedCount > 0)
            {
                _logger.LogDebug("TRADEMON-Cleaned up {Count} old trade events for {MarketTicker} (queue size: {Before} -> {After}), {WarningCount} trades without matching orderbook changes",
                    removedCount, _marketTicker, queueSizeBefore, _tradeEvents.Count, warningCount);
            }
        }

        private OrderbookChange FindMatchingOrderbookChangeForTrade(TradeEvent trade)
        {
            lock (_orderbookMatchingLock)
            {
                var cutoff = trade.Timestamp - TimeSpan.FromSeconds(5);
                var futureCutoff = trade.Timestamp + TimeSpan.FromSeconds(5);
                string expectedOrderBookSide = trade.TakerSide == "yes" ? "no" : "yes";
                int tradePriceToMatch = trade.TakerSide == "yes" ? trade.NoPrice : trade.YesPrice;

                foreach (var change in _orderbookChanges)
                {
                    bool isWithinTimeWindow = change.Timestamp >= cutoff && change.Timestamp <= futureCutoff;
                    if (!isWithinTimeWindow || change.DeltaContracts >= 0 || change.Side != expectedOrderBookSide || change.IsCanceled)
                    {
                        continue;
                    }
                    if (change.Price != tradePriceToMatch || Math.Abs(change.DeltaContracts) != trade.Count)
                    {
                        continue;
                    }

                    // Update trade and change
                    trade.HasMatchingOrderbookChange = true;
                    change.IsTradeRelated = true;
                    change.MatchedTradeId = trade.Id;
                    _logger.LogDebug(
                        "Found matching orderbook change during cleanup for TradeID={TradeID}: ChangeID={ChangeID}, Side={Side}, Price={Price}, Delta={Delta}",
                        trade.Id, change.Id, change.Side, change.Price, change.DeltaContracts);
                    return change;
                }
                _logger.LogDebug(
                    "No matching orderbook change found during cleanup for TradeID={TradeID}, TakerSide={TakerSide}, Price={Price}, Count={Count}, TimeWindow={Cutoff} to {FutureCutoff}",
                    trade.Id, trade.TakerSide, tradePriceToMatch, trade.Count, cutoff, futureCutoff);
                return null;
            }
        }

        #endregion

        #region Velocity and Rate Calculations
        /// <summary>
        /// Calculates the velocity per minute for bottom-level "no" side bids.
        /// Velocity is measured as dollar volume of orderbook changes per minute for price levels below the threshold.
        /// </summary>
        /// <param name="noBids">The list of "no" side bid data</param>
        /// <param name="orderbookChanges">The list of orderbook changes to analyze</param>
        /// <param name="threshold">The price threshold separating top and bottom levels</param>
        /// <returns>A tuple containing the volume per minute and the number of levels analyzed</returns>
        public (double Volume, int Levels) GetBottomNoVelocityPerMinute(List<OrderbookData> noBids, List<OrderbookChange> orderbookChanges, int threshold)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Bottom no velocity calculation cancelled for {MarketTicker}", _marketTicker);
                return (0, 0);
            }

            var validChanges = orderbookChanges.Where(c => c.Side == "no" && !c.IsCanceled).ToList();
            if (!validChanges.Any())
            {
                _logger.LogDebug("No valid 'no' side order book changes found for {MarketTicker}, returning (0, 0)", _marketTicker);
                return (0, 0);
            }

            int levels;
            if (noBids.Count == 0)
            {
                _logger.LogDebug("No 'no' side bids for {MarketTicker}, bottom velocity is 0 as all changes are top-level", _marketTicker);
                levels = 0;
            }
            else
            {
                levels = noBids.Where(x => x.Price < threshold).Count(); // Lower prices are bottom
            }

            var bottomChanges = validChanges.Where(c => c.Price < threshold).ToList();
            _logger.LogDebug("Calculating VelocityPerMinute_Bottom_No_Bid for {MarketTicker}, Threshold={Threshold}, Levels={Levels}, Changes={ChangeCount}, NoBidsCount={NoBidsCount}, EffectiveMinutes={EffectiveMinutes:F2}",
                _marketTicker, threshold, levels, bottomChanges.Count, noBids.Count, 5.0);

            foreach (var change in bottomChanges)
            {
                _logger.LogDebug("BottomNoBid change for {MarketTicker}: ChangeID={ChangeID}, Price={Price}, DeltaContracts={DeltaContracts}, IsTradeRelated={IsTradeRelated}, IsCanceled={IsCanceled}",
                    _marketTicker, change.Id, change.Price, change.DeltaContracts, change.IsTradeRelated, change.IsCanceled);
            }

            double volume = 0;
            if (bottomChanges.Any())
            {
                double totalDollarDelta = bottomChanges.Sum(c => c.Price / 100.0 * c.DeltaContracts);
                volume = 5.0 > 0 ? totalDollarDelta / 5.0 : 0;
                _logger.LogDebug("VelocityPerMinute_Bottom_No_Bid for {MarketTicker}: TotalDollarDelta={TotalDollarDelta:F2}, EffectiveMinutes={EffectiveMinutes:F2}, Rate={Rate:F2} dollars/minute, Levels={Levels}",
                    _marketTicker, totalDollarDelta, 5.0, volume, levels);
            }
            else
            {
                _logger.LogDebug("No changes found for VelocityPerMinute_Bottom_No_Bid in {MarketTicker}, Rate=0, Levels={Levels}", _marketTicker, levels);
            }

            return (Math.Round(volume, 2), levels);
        }

        /// <summary>
        /// Gets the count of bid levels that are at or above the specified lower bound price.
        /// Used for determining the number of "top" price levels in the orderbook.
        /// </summary>
        /// <param name="Bids">The list of orderbook bid data to analyze</param>
        /// <param name="lowerBound">The minimum price threshold for counting levels</param>
        /// <returns>The number of bid levels at or above the lower bound price</returns>
        public int GetTopLevels(List<OrderbookData> Bids, int lowerBound)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Top levels calculation cancelled for {MarketTicker}", _marketTicker);
                return 0;
            }
            return Bids.Where(x => x.Price >= lowerBound).Count();
        }

        /// <summary>
        /// Gets the count of bid levels that are below the specified upper bound price.
        /// Used for determining the number of "bottom" price levels in the orderbook.
        /// </summary>
        /// <param name="Bids">The list of orderbook bid data to analyze</param>
        /// <param name="upperBound">The maximum price threshold for counting levels</param>
        /// <returns>The number of bid levels below the upper bound price</returns>
        public int GetBottomLevels(List<OrderbookData> Bids, int upperBound)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Bottom levels calculation cancelled for {MarketTicker}", _marketTicker);
                return 0;
            }
            return Bids.Where(x => x.Price < upperBound).Count();
        }

        /// <summary>
        /// Calculates the velocity per minute for bottom-level "yes" side bids.
        /// Velocity is measured as dollar volume of orderbook changes per minute for price levels below the threshold.
        /// </summary>
        /// <param name="yesBids">The list of "yes" side bid data</param>
        /// <param name="orderbookChanges">The list of orderbook changes to analyze</param>
        /// <param name="threshold">The price threshold separating top and bottom levels</param>
        /// <returns>A tuple containing the volume per minute and the number of levels analyzed</returns>
        public (double Volume, int Levels) GetBottomYesVelocityPerMinute(List<OrderbookData> yesBids, List<OrderbookChange> orderbookChanges, int threshold)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Bottom yes velocity calculation cancelled for {MarketTicker}", _marketTicker);
                return (0, 0);
            }

            var validChanges = orderbookChanges.Where(c => c.Side == "yes" && !c.IsCanceled).ToList();
            if (!validChanges.Any())
            {
                _logger.LogDebug("No valid 'yes' side order book changes found for {MarketTicker}, returning (0, 0)", _marketTicker);
                return (0, 0);
            }

            int levels;
            if (yesBids.Count == 0)
            {
                _logger.LogDebug("No 'yes' side bids for {MarketTicker}, bottom velocity is 0 as all changes are top-level", _marketTicker);
                levels = 0;
            }
            else
            {
                levels = yesBids.Where(x => x.Price < threshold).Count(); // Lower prices are bottom
            }

            var bottomChanges = validChanges.Where(c => c.Price < threshold).ToList();
            _logger.LogDebug("Calculating VelocityPerMinute_Bottom_Yes_Bid for {MarketTicker}, Threshold={Threshold}, Levels={Levels}, Changes={ChangeCount}, YesBidsCount={YesBidsCount}, EffectiveMinutes={EffectiveMinutes:F2}",
                _marketTicker, threshold, levels, bottomChanges.Count, yesBids.Count, 5.0);

            foreach (var change in bottomChanges)
            {
                _logger.LogDebug("BottomYesBid change: ChangeID={ChangeID}, Price={Price}, DeltaContracts={DeltaContracts}, IsTradeRelated={IsTradeRelated}, IsCanceled={IsCanceled}",
                    change.Id, change.Price, change.DeltaContracts, change.IsTradeRelated, change.IsCanceled);
            }

            double volume = 0;
            if (bottomChanges.Any())
            {
                double totalDollarDelta = bottomChanges.Sum(c => c.Price / 100.0 * c.DeltaContracts);
                volume = 5.0 > 0 ? totalDollarDelta / 5.0 : 0;
                _logger.LogDebug("VelocityPerMinute_Bottom_Yes_Bid for {MarketTicker}: TotalDollarDelta={TotalDollarDelta:F2}, EffectiveMinutes={EffectiveMinutes:F2}, Rate={Rate:F2} dollars/minute, Levels={Levels}",
                    _marketTicker, totalDollarDelta, 5.0, volume, levels);
            }
            else
            {
                _logger.LogDebug("No changes found for VelocityPerMinute_Bottom_Yes_Bid in {MarketTicker}, Rate=0, Levels={Levels}", _marketTicker, levels);
            }

            return (Math.Round(volume, 2), levels);
        }

        /// <summary>
        /// Calculates the velocity per minute for top-level "no" side bids.
        /// Velocity is measured as dollar volume of orderbook changes per minute for price levels at or above the threshold.
        /// </summary>
        /// <param name="noBids">The list of "no" side bid data</param>
        /// <param name="orderbookChanges">The list of orderbook changes to analyze</param>
        /// <param name="threshold">The price threshold separating top and bottom levels</param>
        /// <returns>A tuple containing the volume per minute and the number of levels analyzed</returns>
        public (double Volume, int Levels) GetTopNoVelocityPerMinute(List<OrderbookData> noBids, List<OrderbookChange> orderbookChanges, int threshold)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Top no velocity calculation cancelled for {MarketTicker}", _marketTicker);
                return (0, 0);
            }

            var validChanges = orderbookChanges.Where(c => c.Side == "no" && !c.IsCanceled).ToList();
            if (!validChanges.Any())
            {
                _logger.LogDebug("No valid 'no' side order book changes found for {MarketTicker}, returning (0, 0)", _marketTicker);
                return (0, 0);
            }

            int levels;
            if (noBids.Count == 0)
            {
                _logger.LogDebug("No 'no' side bids for {MarketTicker}, treating all valid changes as top-level", _marketTicker);
                levels = 0;
            }
            else
            {
                levels = noBids.Where(x => x.Price >= threshold).Count(); // Higher prices are top
            }

            var topChanges = validChanges.Where(c => c.Price >= threshold).ToList();
            _logger.LogDebug("Calculating VelocityPerMinute_Top_No_Bid for {MarketTicker}, Threshold={Threshold}, Levels={Levels}, Changes={ChangeCount}, NoBidsCount={NoBidsCount}, EffectiveMinutes={EffectiveMinutes:F2}",
                _marketTicker, threshold, levels, topChanges.Count, noBids.Count, 5.0);

            foreach (var change in topChanges)
            {
                _logger.LogDebug("TopNoBid change for {MarketTicker}: ChangeID={ChangeID}, Price={Price}, DeltaContracts={DeltaContracts}, IsTradeRelated={IsTradeRelated}, IsCanceled={IsCanceled}",
                    _marketTicker, change.Id, change.Price, change.DeltaContracts, change.IsTradeRelated, change.IsCanceled);
            }

            double volume = 0;
            if (topChanges.Any())
            {
                double totalDollarDelta = topChanges.Sum(c => c.Price / 100.0 * c.DeltaContracts);
                volume = 5.0 > 0 ? totalDollarDelta / 5.0 : 0;
                _logger.LogDebug("VelocityPerMinute_Top_No_Bid for {MarketTicker}: TotalDollarDelta={TotalDollarDelta:F2}, EffectiveMinutes={EffectiveMinutes:F2}, Rate={Rate:F2} dollars/minute, Levels={Levels}",
                    _marketTicker, totalDollarDelta, 5.0, volume, levels);
            }
            else
            {
                _logger.LogDebug("No changes found for VelocityPerMinute_Top_No_Bid in {MarketTicker}, Rate=0, Levels={Levels}", _marketTicker, levels);
            }

            return (Math.Round(volume, 2), levels);
        }

        /// <summary>
        /// Calculates the velocity per minute for top-level "yes" side bids.
        /// Velocity is measured as dollar volume of orderbook changes per minute for price levels at or above the threshold.
        /// </summary>
        /// <param name="yesBids">The list of "yes" side bid data</param>
        /// <param name="orderbookChanges">The list of orderbook changes to analyze</param>
        /// <param name="threshold">The price threshold separating top and bottom levels</param>
        /// <returns>A tuple containing the volume per minute and the number of levels analyzed</returns>
        public (double Volume, int Levels) GetTopYesVelocityPerMinute(List<OrderbookData> yesBids, List<OrderbookChange> orderbookChanges, int threshold)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Top yes velocity calculation cancelled for {MarketTicker}", _marketTicker);
                return (0, 0);
            }

            var validChanges = orderbookChanges.Where(c => c.Side == "yes" && !c.IsCanceled).ToList();
            if (!validChanges.Any())
            {
                _logger.LogDebug("No valid 'yes' side order book changes found for {MarketTicker}, returning (0, 0)", _marketTicker);
                return (0, 0);
            }

            int levels;
            if (yesBids.Count == 0)
            {
                _logger.LogDebug("No 'yes' side bids for {MarketTicker}, treating all valid changes as top-level", _marketTicker);
                levels = 0;
            }
            else
            {
                levels = yesBids.Where(x => x.Price >= threshold).Count(); // Higher prices are top
            }

            var topChanges = validChanges.Where(c => c.Price >= threshold).ToList();
            _logger.LogDebug("Calculating VelocityPerMinute_Top_Yes_Bid for {MarketTicker}, Threshold={Threshold}, Levels={Levels}, Changes={ChangeCount}, YesBidsCount={YesBidsCount}, EffectiveMinutes={EffectiveMinutes:F2}",
                _marketTicker, threshold, levels, topChanges.Count, yesBids.Count, 5.0);

            foreach (var change in topChanges)
            {
                _logger.LogDebug("TopYesBid change: ChangeID={ChangeID}, Price={Price}, DeltaContracts={DeltaContracts}, IsTradeRelated={IsTradeRelated}, IsCanceled={IsCanceled}",
                    change.Id, change.Price, change.DeltaContracts, change.IsTradeRelated, change.IsCanceled);
            }

            double volume = 0;
            if (topChanges.Any())
            {
                double totalDollarDelta = topChanges.Sum(c => c.Price / 100.0 * c.DeltaContracts);
                volume = 5.0 > 0 ? totalDollarDelta / 5.0 : 0;
                _logger.LogDebug("VelocityPerMinute_Top_Yes_Bid for {MarketTicker}: TotalDollarDelta={TotalDollarDelta:F2}, EffectiveMinutes={EffectiveMinutes:F2}, Rate={Rate:F2} dollars/minute, Levels={Levels}",
                    _marketTicker, totalDollarDelta, 5.0, volume, levels);
            }
            else
            {
                _logger.LogDebug("No changes found for VelocityPerMinute_Top_Yes_Bid in {MarketTicker}, Rate=0, Levels={Levels}", _marketTicker, levels);
            }

            return (Math.Round(volume, 2), levels);
        }

        /// <summary>
        /// Calculates the net order volume per minute for "yes" side bids.
        /// This measures the dollar volume of non-trade-related orderbook changes (market making activity).
        /// </summary>
        /// <param name="yesBidChanges">The list of "yes" side orderbook changes to analyze</param>
        /// <returns>A tuple containing the volume per minute and the count of orders</returns>
        public (double Volume, int Count) GetYesNetOrderVolumePerMinute(List<OrderbookChange> yesBidChanges)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Yes net order volume calculation cancelled for {MarketTicker}", _marketTicker);
                return (0, 0);
            }

            var validChanges = yesBidChanges.Where(c => c.Side == "yes" && !c.IsTradeRelated && !c.IsCanceled).ToList();
            _logger.LogDebug("Calculating OrderRatePerMinute_YesBid for {MarketTicker}, NonTradeRelatedYesChanges={Count}",
                _marketTicker, validChanges.Count);

            int orderCount = validChanges.Count;
            if (!validChanges.Any())
            {
                _logger.LogDebug("No 'yes' side non-trade-related changes found for {MarketTicker}, Rate=0, Count=0", _marketTicker);
                return (0, orderCount);
            }

            double totalDollarDelta = validChanges.Sum(c => c.Price / 100.0 * c.DeltaContracts);
            double volume = 5.0 > 0 ? totalDollarDelta / 5.0 : 0;
            volume = Math.Round(volume, 2);

            _logger.LogDebug("OrderRatePerMinute_YesBid for {MarketTicker}: TotalDollarDelta={TotalDollarDelta:F2}, Minutes={Minutes:F2}, Rate={Rate:F2} dollars/minute, Count={Count}",
                _marketTicker, totalDollarDelta, 5.0, volume, orderCount);
            return (volume, orderCount);
        }

        /// <summary>
        /// Calculates the net order volume per minute for "no" side bids.
        /// This measures the dollar volume of non-trade-related orderbook changes (market making activity).
        /// </summary>
        /// <param name="noBidChanges">The list of "no" side orderbook changes to analyze</param>
        /// <returns>A tuple containing the volume per minute and the count of orders</returns>
        public (double Volume, int Count) GetNoNetOrderVolumePerMinute(List<OrderbookChange> noBidChanges)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("No net order volume calculation cancelled for {MarketTicker}", _marketTicker);
                return (0, 0);
            }

            var validChanges = noBidChanges.Where(c => c.Side == "no" && !c.IsTradeRelated && !c.IsCanceled).ToList();
            _logger.LogDebug("Calculating OrderRatePerMinute_NoBid for {MarketTicker}, NonTradeRelatedNoChanges={Count}",
                _marketTicker, validChanges.Count);

            int orderCount = validChanges.Count;
            if (!validChanges.Any())
            {
                _logger.LogDebug("No 'no' side non-trade-related changes found for {MarketTicker}, Rate=0, Count=0", _marketTicker);
                return (0, orderCount);
            }

            double totalDollarDelta = validChanges.Sum(c => c.Price / 100.0 * c.DeltaContracts);
            double volume = 5.0 > 0 ? totalDollarDelta / 5.0 : 0;
            volume = Math.Round(volume, 2);

            _logger.LogDebug("OrderRatePerMinute_NoBid for {MarketTicker}: TotalDollarDelta={TotalDollarDelta:F2}, Minutes={Minutes:F2}, Rate={Rate:F2} dollars/minute, Count={Count}",
                _marketTicker, totalDollarDelta, 5.0, volume, orderCount);
            return (volume, orderCount);
        }

        /// <summary>
        /// Calculates the trade rate and volume per minute for "yes" side maker trades.
        /// This measures actual trading activity where the maker was on the "yes" side.
        /// </summary>
        /// <param name="yesTradeRelatedChanges">The list of trade-related "yes" side orderbook changes</param>
        /// <returns>A tuple containing the trade rate per minute and volume per minute</returns>
        public (double rate, double volume) GetTradeRatePerMinute_MakerYes(List<OrderbookChange> yesTradeRelatedChanges)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Yes trade rate calculation cancelled for {MarketTicker}", _marketTicker);
                return (0, 0);
            }

            if (yesTradeRelatedChanges.Count == 0)
            {
                _logger.LogDebug("No trade-related 'yes' side changes found for {MarketTicker}, returning (0, 0)", _marketTicker);
                return (0, 0);
            }

            _logger.LogDebug("Calculating tradeVolumePerMinute_Yes for {MarketTicker}, TradeRelatedYesChanges={Count}",
                _marketTicker, yesTradeRelatedChanges.Count);

            double volume;
            double rate = 5.0 > 0 ? yesTradeRelatedChanges.Count / 5.0 : 0;

            if (!yesTradeRelatedChanges.Any())
            {
                _logger.LogDebug("No trade-related 'yes' side changes found for {MarketTicker}, Rate=0, Volume=0", _marketTicker);
                volume = 0;
            }
            else
            {
                double totalTradeDollar = yesTradeRelatedChanges.Sum(c => c.Price / 100.0 * c.DeltaContracts);
                volume = 5.0 > 0 ? totalTradeDollar / 5.0 : 0;
                _logger.LogDebug("tradeVolumePerMinute_Yes for {MarketTicker}: TotalTradeDollar={TotalTradeDollar:F2}, Minutes={Minutes:F2}, Rate={Rate:F2}, Volume={Volume:F2} dollars/minute",
                    _marketTicker, totalTradeDollar, 5.0, rate, volume);
            }

            return (Math.Round(rate, 2), volume);
        }

        /// <summary>
        /// Calculates the trade rate and volume per minute for "no" side maker trades.
        /// This measures actual trading activity where the maker was on the "no" side.
        /// </summary>
        /// <param name="noTradeRelatedChanges">The list of trade-related "no" side orderbook changes</param>
        /// <returns>A tuple containing the trade rate per minute and volume per minute</returns>
        public (double rate, double volume) GetTradeRatePerMinute_MakerNo(List<OrderbookChange> noTradeRelatedChanges)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("No trade rate calculation cancelled for {MarketTicker}", _marketTicker);
                return (0, 0);
            }

            if (noTradeRelatedChanges.Count == 0)
            {
                _logger.LogDebug("No trade-related 'no' side changes found for {MarketTicker}, returning (0, 0)", _marketTicker);
                return (0, 0);
            }

            _logger.LogDebug("Calculating tradeVolumePerMinute_No for {MarketTicker}, TradeRelatedNoChanges={Count}",
                _marketTicker, noTradeRelatedChanges.Count);

            double volume;
            double rate = 5.0 > 0 ? noTradeRelatedChanges.Count / 5.0 : 0;

            if (!noTradeRelatedChanges.Any())
            {
                _logger.LogDebug("No trade-related 'no' side changes found for {MarketTicker}, Rate=0, Volume=0", _marketTicker);
                volume = 0;
            }
            else
            {
                double totalTradeDollar = noTradeRelatedChanges.Sum(c => c.Price / 100.0 * c.DeltaContracts);
                volume = 5.0 > 0 ? totalTradeDollar / 5.0 : 0;
                _logger.LogDebug("tradeVolumePerMinute_No for {MarketTicker}: TotalTradeDollar={TotalTradeDollar:F2}, Minutes={Minutes:F2}, Rate={Rate:F2}, Volume={Volume:F2} dollars/minute",
                    _marketTicker, totalTradeDollar, 5.0, rate, volume);
            }

            return (Math.Round(rate, 2), volume);
        }

        /// <summary>
        /// Calculates the average trade size in dollars for "yes" side maker trades.
        /// This measures the typical dollar value of trades where the maker was on the "yes" side.
        /// </summary>
        /// <param name="tradeEvents">The list of trade-related orderbook changes to analyze</param>
        /// <returns>The average trade size in dollars, rounded to 2 decimal places</returns>
        public double GetAverageTradeSize_MakerYes(List<OrderbookChange> tradeEvents)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Yes average trade size calculation cancelled for {MarketTicker}", _marketTicker);
                return 0;
            }

            tradeEvents = tradeEvents.Where(c => !c.IsCanceled).ToList();
            if (tradeEvents.Count == 0)
            {
                _logger.LogDebug("No 'yes' side trades found for {MarketTicker}", _marketTicker);
                return 0;
            }

            _logger.LogDebug("Calculating AverageTradeSize_Yes for {MarketTicker}, YesTradeCount={Count}",
                _marketTicker, tradeEvents.Count);

            int TradeCount_Yes = tradeEvents.Count;
            double totalDollar = tradeEvents.Sum(t => Math.Abs(t.DeltaContracts) * t.Price / 100.0);
            double averageDollar = TradeCount_Yes > 0 ? totalDollar / TradeCount_Yes : 0;

            _logger.LogDebug("AverageTradeSize_Yes for {MarketTicker}: TotalDollar={TotalDollar:F2}, TradeCount_Yes={TradeCount_Yes}, Average={Average:F2}",
                _marketTicker, totalDollar, TradeCount_Yes, averageDollar);

            return Math.Round(averageDollar, 2);
        }

        /// <summary>
        /// Calculates the average trade size in dollars for "no" side maker trades.
        /// This measures the typical dollar value of trades where the maker was on the "no" side.
        /// </summary>
        /// <param name="tradeEvents">The list of trade-related orderbook changes to analyze</param>
        /// <returns>The average trade size in dollars, rounded to 2 decimal places</returns>
        public double GetAverageTradeSize_MakerNo(List<OrderbookChange> tradeEvents)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("No average trade size calculation cancelled for {MarketTicker}", _marketTicker);
                return 0;
            }

            tradeEvents = tradeEvents.Where(c => !c.IsCanceled).ToList();
            if (tradeEvents.Count == 0)
            {
                _logger.LogDebug("No 'no' side trades found for {MarketTicker}", _marketTicker);
                return 0;
            }

            _logger.LogDebug("Calculating AverageTradeSize_No for {MarketTicker}, NoTradeCount={Count}",
                _marketTicker, tradeEvents.Count);

            int TradeCount_No = tradeEvents.Count;
            double totalDollar = tradeEvents.Sum(t => Math.Abs(t.DeltaContracts) * t.Price / 100.0);
            double averageDollar = TradeCount_No > 0 ? totalDollar / TradeCount_No : 0;

            _logger.LogDebug("AverageTradeSize_No for {MarketTicker}: TotalDollar={TotalDollar:F2}, TradeCount_No={TradeCount_No}, Average={Average:F2}",
                _marketTicker, totalDollar, TradeCount_No, averageDollar);

            return Math.Round(averageDollar, 2);
        }

        /// <summary>
        /// Gets the total count of trades where the maker was on the "yes" side.
        /// This represents trades where the taker was on the "no" side.
        /// </summary>
        /// <returns>The number of trades where the maker was on the "yes" side</returns>
        public int GetTradeCount_MakerYes()
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Yes trade count calculation cancelled for {MarketTicker}", _marketTicker);
                return 0;
            }

            if (_tradeEvents.Count == 0)
            {
                _logger.LogDebug("No 'yes' side trades found for {MarketTicker}", _marketTicker);
                return 0;
            }

            _logger.LogDebug("Calculating GetTradeCount_Yes for {MarketTicker}", _marketTicker);

            var yesTrades = _tradeEvents.Where(t => t.TakerSide == "no").ToList();
            int tradeCount = yesTrades.Count;

            _logger.LogDebug("GetTradeCount_Yes for {MarketTicker}: YesTradesCount={TradeCount}", _marketTicker, tradeCount);

            return tradeCount;
        }

        /// <summary>
        /// Gets the total count of trades where the maker was on the "no" side.
        /// This represents trades where the taker was on the "yes" side.
        /// </summary>
        /// <returns>The number of trades where the maker was on the "no" side</returns>
        public int GetTradeCount_MakerNo()
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("No trade count calculation cancelled for {MarketTicker}", _marketTicker);
                return 0;
            }

            if (_tradeEvents.Count == 0)
            {
                _logger.LogDebug("No 'no' side trades found for {MarketTicker}", _marketTicker);
                return 0;
            }

            _logger.LogDebug("Calculating GetTradeCount_No for {MarketTicker}", _marketTicker);

            var noTrades = _tradeEvents.Where(t => t.TakerSide == "yes").ToList();
            int tradeCount = noTrades.Count;

            _logger.LogDebug("GetTradeCount_No for {MarketTicker}: NoTradesCount={TradeCount}", _marketTicker, tradeCount);

            return tradeCount;
        }
        #endregion

        #region Current snapshot metrics updater
        /// <summary>
        /// Computes snapshot-scoped metrics (raw counts, volumes, and averages) for the
        /// interval strictly after <see cref="IMarketData.LastSnapshotTaken"/> and writes
        /// them into corresponding properties on the Market object.  This method is
        /// invoked from the periodic recalculation timer so that snapshot metrics are
        /// refreshed on the same cadence as the general metrics.  It writes directly to
        /// fields such as CurrentTradeRatePerMinute_Yes/No, CurrentTradeVolumePerMinute_Yes/No,
        /// CurrentTradeCount_Yes/No, CurrentOrderVolumePerMinute_YesBid/NoBid,
        /// CurrentNonTradeRelatedOrderCount_Yes/No, and CurrentAverageTradeSize_Yes/No.  All
        /// rate and volume metrics are normalized per minute over the elapsed time since
        /// the last snapshot, and counts (trade and non-trade) are likewise normalized per
        /// minute.  Average trade size remains a raw (per-trade) value.  Assignments
        /// will fail to compile if the target properties do not yet exist on the Market
        /// class; the user indicated they will add these themselves.
        /// </summary>
        private void UpdateCurrentSnapshotMetrics()
        {
            // Obtain a consistent snapshot of orderbook changes under lock to avoid
            // enumerating the concurrent queue during updates.
            OrderbookChange[] snapshot;
            lock (_orderbookMatchingLock)
            {
                snapshot = _orderbookChanges.ToArray();
            }

            // If there is no market or no snapshot tracking, there is nothing to update.
            var market = Market;
            if (market == null) return;
            DateTime since = market.LastSnapshotTaken;

            // Calculate the minutes elapsed since the last snapshot; avoid division by zero
            double minutes = Math.Max(1e-9, (DateTime.UtcNow - since).TotalMinutes);

            // Filter recent changes (strictly after snapshot) that haven't been canceled.
            var recent = snapshot.Where(c => !c.IsCanceled && c.Timestamp > since).ToArray();

            // Separate trade-related vs non-trade-related changes by side.
            var yesTrades = recent.Where(c => c.IsTradeRelated && c.Side == "yes").ToList();
            var noTrades = recent.Where(c => c.IsTradeRelated && c.Side == "no").ToList();

            var yesOrders = recent.Where(c => !c.IsTradeRelated && c.Side == "yes").ToList();
            var noOrders = recent.Where(c => !c.IsTradeRelated && c.Side == "no").ToList();

            // Raw counts
            int tradeCountYesRaw = yesTrades.Count;
            int tradeCountNoRaw = noTrades.Count;
            int nonTradeYesRaw = yesOrders.Count;
            int nonTradeNoRaw = noOrders.Count;

            // Normalize counts per minute (counts per minute)
            double tradeCountPerMinYes = tradeCountYesRaw / minutes;
            double tradeCountPerMinNo = tradeCountNoRaw  / minutes;
            double nonTradePerMinYes = nonTradeYesRaw   / minutes;
            double nonTradePerMinNo = nonTradeNoRaw    / minutes;

            // Per-minute volumes (dollarized). Use absolute delta for trade-related volumes and signed delta for orders
            double tradeVolPerMinYes = yesTrades.Sum(c => (c.Price / 100.0) * Math.Abs(c.DeltaContracts)) / minutes;
            double tradeVolPerMinNo = noTrades.Sum(c => (c.Price / 100.0) * Math.Abs(c.DeltaContracts)) / minutes;
            double orderVolPerMinYes = yesOrders.Sum(c => (c.Price / 100.0) * c.DeltaContracts) / minutes;
            double orderVolPerMinNo = noOrders.Sum(c => (c.Price / 100.0) * c.DeltaContracts) / minutes;

            // Average trade size (dollarized per trade) remains raw (not normalized per minute)
            double avgTradeSizeYes = 0;
            if (tradeCountYesRaw > 0)
            {
                double totalDollarYes = yesTrades.Sum(t => Math.Abs(t.DeltaContracts) * (t.Price / 100.0));
                avgTradeSizeYes = totalDollarYes / tradeCountYesRaw;
            }

            double avgTradeSizeNo = 0;
            if (tradeCountNoRaw > 0)
            {
                double totalDollarNo = noTrades.Sum(t => Math.Abs(t.DeltaContracts) * (t.Price / 100.0));
                avgTradeSizeNo = totalDollarNo / tradeCountNoRaw;
            }

            // Round values for consistent precision
            tradeCountPerMinYes = Math.Round(tradeCountPerMinYes, 2);
            tradeCountPerMinNo  = Math.Round(tradeCountPerMinNo, 2);
            tradeVolPerMinYes   = Math.Round(tradeVolPerMinYes, 2);
            tradeVolPerMinNo    = Math.Round(tradeVolPerMinNo, 2);
            orderVolPerMinYes   = Math.Round(orderVolPerMinYes, 2);
            orderVolPerMinNo    = Math.Round(orderVolPerMinNo, 2);
            nonTradePerMinYes   = Math.Round(nonTradePerMinYes, 2);
            nonTradePerMinNo    = Math.Round(nonTradePerMinNo, 2);
            avgTradeSizeYes     = Math.Round(avgTradeSizeYes, 2);
            avgTradeSizeNo      = Math.Round(avgTradeSizeNo, 2);

            // Assign normalized metrics directly to Market fields.  Counts are stored as per-minute values.
            market.CurrentTradeRatePerMinute_Yes        = tradeCountPerMinYes;
            market.CurrentTradeRatePerMinute_No         = tradeCountPerMinNo;
            market.CurrentTradeVolumePerMinute_Yes      = tradeVolPerMinYes;
            market.CurrentTradeVolumePerMinute_No       = tradeVolPerMinNo;
            market.CurrentTradeCount_Yes                = tradeCountPerMinYes;
            market.CurrentTradeCount_No                 = tradeCountPerMinNo;
            market.CurrentOrderVolumePerMinute_YesBid   = orderVolPerMinYes;
            market.CurrentOrderVolumePerMinute_NoBid    = orderVolPerMinNo;
            market.CurrentNonTradeRelatedOrderCount_Yes = nonTradePerMinYes;
            market.CurrentNonTradeRelatedOrderCount_No  = nonTradePerMinNo;
            market.CurrentAverageTradeSize_Yes          = avgTradeSizeYes;
            market.CurrentAverageTradeSize_No           = avgTradeSizeNo;
        }

        #endregion

        /// <summary>
        /// Records execution time metrics using the IPerformanceMonitor interface.
        /// </summary>
        /// <param name="operationName">Name of the operation being timed</param>
        /// <param name="executionTimeMs">Execution time in milliseconds</param>
        /// <param name="enableMetrics">Whether performance metrics are enabled</param>
        private void RecordExecutionTimePrivate(string operationName, long executionTimeMs, bool enableMetrics)
        {
            string className = "OrderbookChangeTracker";
            string category = "OrderbookTracking";

            if (!enableMetrics)
            {
                // Send disabled metric
                _centralPerformanceMonitor.RecordDisabledMetricMetric(className, operationName, $"{operationName} Execution Time", $"Execution time for {operationName}", executionTimeMs, "ms", category, false);
            }
            else
            {
                // Record actual metric
                _centralPerformanceMonitor.RecordSpeedDialMetric(className, operationName, $"{operationName} Execution Time", $"Execution time for {operationName}", executionTimeMs, "ms", category, null, null, null, true);
            }
        }

        #region Dispose
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// Stops and disposes of all timers and releases resources used by the tracker.
        /// </summary>
        public void Dispose()
        {
            _recalculationTimer?.Stop();
            _logOutputTimer?.Stop();
            _recalculationTimer?.Dispose();
            _logOutputTimer?.Dispose();
            _logger.LogDebug("OrderbookChangeTracker disposed for {MarketTicker}: Timers stopped and disposed", _marketTicker);
        }
        #endregion
    }
}
