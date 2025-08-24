using Microsoft.Extensions.Options;
using SmokehouseBot.Exceptions;
using SmokehouseBot.Management.Interfaces;
using SmokehouseBot.Services.Interfaces;
using SmokehouseBot.State.Interfaces;
using SmokehouseDTOs;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Timers;
using TradingStrategies.Configuration;

namespace SmokehouseBot.Services
{
    public class OrderbookChangeTracker : IOrderbookChangeTracker
    {
        private readonly ILogger<IOrderbookChangeTracker> _logger;
        private readonly string _marketTicker;
        private readonly IDataCache _cache;
        private readonly IOptions<TradingConfig> _config;
        private CancellationToken _cancellationToken => _statusTrackerService.GetCancellationToken();


        private readonly IScopeManagerService _scopeManagerService;

        private IStatusTrackerService _statusTrackerService;

        private readonly ConcurrentQueue<OrderbookChange> _orderbookChanges = new ConcurrentQueue<OrderbookChange>();
        private readonly ConcurrentQueue<TradeEvent> _tradeEvents = new ConcurrentQueue<TradeEvent>();
        private readonly System.Timers.Timer _recalculationTimer;
        private readonly System.Timers.Timer _logOutputTimer;
        private long _lastSequence = 0;
        public DateTime LastMarketOpenTime { get; set; } = DateTime.MinValue;
        private DateTime _lastEventTime = DateTime.MinValue;

        #region Properties
        public IMarketData Market { 
            get 
            {
                if (_cache == null || !_cache.Markets.ContainsKey(_marketTicker) || _cancellationToken.IsCancellationRequested)
                    return null;
                return _cache.Markets[_marketTicker];
            } 
        }

        private bool FirstSnapshotReceived { get; set; } = false;

        public bool IsMature
        {
            get
            {
                if (LastMarketOpenTime == DateTime.MinValue)
                {
                    return false;
                }
                TimeSpan elapsedSinceOpen = DateTime.UtcNow - LastMarketOpenTime;
                return elapsedSinceOpen >= _config.Value.ChangeWindowDuration;
            }
        }

        public TimeSpan ChangeWindowDuration => _config.Value.ChangeWindowDuration;

        public TimeSpan TradeMatchingWindow => _config.Value.TradeMatchingWindow;

        public TimeSpan OrderbookCancelWindow => _config.Value.OrderbookCancelWindow;
        #endregion

        #region Constructor and Initialization
        private bool CalculationsDirty = true;

        private readonly object _matchingLock = new object();

        public OrderbookChangeTracker(
            string marketTicker,
            ILogger<IOrderbookChangeTracker> logger,
            IDataCache cache,
            IOptions<TradingConfig> config,
            IScopeManagerService scopeManagerService,
            IStatusTrackerService statusTrackerService)
        {
            _marketTicker = marketTicker ?? throw new ArgumentNullException(nameof(marketTicker));
            _scopeManagerService = scopeManagerService;
            _cache = cache;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _statusTrackerService = statusTrackerService;

            _recalculationTimer = new System.Timers.Timer(30000); // 30 seconds
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
            FirstSnapshotReceived = false;
            CalculationsDirty = false;

            // Dispose of timer resources
            _recalculationTimer.Dispose();
            _logOutputTimer.Dispose();

            _logger.LogDebug("Shutdown completed for OrderbookChangeTracker associated with market {MarketTicker}: Timers stopped and disposed, queues cleared, state reset", _marketTicker);
        }
        private void OnLogOutputTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Log output cancelled for {MarketTicker}", _marketTicker);
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
        }
        #endregion

        #region Market Status
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
                _logger.LogDebug("Market {MarketTicker} opened at {Time}", _marketTicker, LastMarketOpenTime);
                _recalculationTimer.Start();
            }
            else if ((!isTradingActive || !isExchangeActive) && LastMarketOpenTime != DateTime.MinValue)
            {
                LastMarketOpenTime = DateTime.MinValue;
                _orderbookChanges.Clear();
                _tradeEvents.Clear();
                _lastSequence = 0;
                _recalculationTimer.Stop();
                _logger.LogDebug("Market {MarketTicker} closed, queues and timer reset", _marketTicker);
            }
        }
        #endregion

        public void Stop()
        {
            _recalculationTimer.Stop();
            _logOutputTimer.Stop();
            _logger.LogDebug("OrderbookChangeTracker stopped for {MarketTicker}: Timers stopped", _marketTicker);
        }

        #region Event Logging and Matching
        public void LogOrderbookSnapshot(List<OrderbookData> originalOrderbook, List<OrderbookData> newOrderbook)
        {
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
                ResetEvents();
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


            if (!FirstSnapshotReceived)
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
                        LogChange(side, price, deltaContracts);
                    }

                }
            }


            FirstSnapshotReceived = true;

            _logger.LogDebug("Completed orderbook snapshot processing for {MarketTicker}", _marketTicker);
        }

        public void LogChange(string side, int price, int deltaContracts)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Orderbook change logging cancelled for {MarketTicker}", _marketTicker);
                return;
            }

            if (deltaContracts != 0)
            {
                var change = new OrderbookChange
                {
                    Sequence = _lastSequence++,
                    Side = side,
                    Price = price,
                    DeltaContracts = deltaContracts,
                    Timestamp = DateTime.UtcNow,
                    IsTradeRelated = false
                };

                _lastEventTime = change.Timestamp;

                lock (_matchingLock)
                {
                    var expectedRollOffTime = change.Timestamp + _config.Value.ChangeWindowDuration;
                    _logger.LogDebug(
                        "Received orderbook change for {MarketTicker}: ChangeID={ChangeID}, Side={Side}, Price={Price}, DeltaContracts={DeltaContracts}, Timestamp={Timestamp:F1}, ExpectedRollOffTime={ExpectedRollOffTime}",
                        _marketTicker, change.Id, change.Side, change.Price, change.DeltaContracts, change.Timestamp, expectedRollOffTime);

                    if (deltaContracts < 0)
                        CheckForMatchingTrade(change);

                    _orderbookChanges.Enqueue(change);
                }

                _logger.LogDebug("Logged change for {MarketTicker}: ChangeID={ChangeID}, Side={Side}, Price={Price}, Delta={Delta}, Sequence={Sequence}, IsTradeRelated={IsTradeRelated}, IsCanceled={IsCanceled}",
                    _marketTicker, change.Id, side, price, deltaContracts, change.Sequence, change.IsTradeRelated, change.IsCanceled);
                CalculationsDirty = true;
            }
        }

        public void LogTrade(string takerSide, int yesPrice, int noPrice, int count, DateTime timestamp)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Trade logging cancelled for {MarketTicker}", _marketTicker);
                return;
            }

            var trade = new TradeEvent
            {
                TakerSide = takerSide,
                YesPrice = yesPrice,
                NoPrice = noPrice,
                Count = count,
                Timestamp = timestamp
            };

            var expectedRollOffTime = trade.Timestamp + _config.Value.ChangeWindowDuration;

            _logger.LogDebug(
                "Received trade for {MarketTicker}: TradeID={TradeID}, TakerSide={TakerSide}, YesPrice={YesPrice}, NoPrice={NoPrice}, Count={Count}, Timestamp={Timestamp}, ExpectedRollOffTime={ExpectedRollOffTime}",
                _marketTicker, trade.Id, trade.TakerSide, trade.YesPrice, trade.NoPrice, trade.Count, trade.Timestamp, expectedRollOffTime);

            CheckForMatchingOrderbookChange(trade);

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

            CalculationsDirty = true;
        }

        private bool CheckForMatchingOrderbookChange(TradeEvent trade)
        {
            lock (_matchingLock)
            {
                if (_cancellationToken.IsCancellationRequested || trade.HasMatchingOrderbookChange)
                {
                    _logger.LogDebug("Trade already matched or cancelled: TradeID={TradeID}", trade.Id);
                    return trade.HasMatchingOrderbookChange;
                }

                var cutoff = trade.Timestamp - _config.Value.TradeMatchingWindow;
                var futureCutoff = trade.Timestamp + _config.Value.TradeMatchingWindow;
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
                    _logger.LogDebug(
                        "TRADEMON-Matched orderbook change to trade in {MarketTicker}: ChangeID={ChangeID}, Side={Side}, Price={Price}, Delta={Delta}, TradeID={TradeID}, TradeTimestamp={TradeTimestamp}, TimeDiff={TimeDiff:F2}s",
                        _marketTicker, bestMatch.Id, bestMatch.Side, bestMatch.Price, bestMatch.DeltaContracts, trade.Id, trade.Timestamp, minTimeDiff);
                    return true;
                }

                // Fallback to canceled changes (unchanged logic)
                OrderbookChange bestCanceledMatch = null;
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
                    _logger.LogDebug(
                        "TRADEMON-Reclassified canceled orderbook change for trade in {MarketTicker}: ChangeID={ChangeID}, Side={Side}, Price={Price}, Delta={Delta}, TradeID={TradeID}, TradeTimestamp={TradeTimestamp}",
                        _marketTicker, bestCanceledMatch.Id, bestCanceledMatch.Side, bestCanceledMatch.Price, bestCanceledMatch.DeltaContracts, trade.Id, trade.Timestamp);
                    return true;
                }

                _logger.LogDebug(
                    "No matching orderbook change found for trade in {MarketTicker}: TradeID={TradeID}, TakerSide={TakerSide}, Price={Price}, Count={Count}, Timestamp={Timestamp}",
                    _marketTicker, trade.Id, trade.TakerSide, tradePriceToMatch, trade.Count, trade.Timestamp);
                return false;
            }
        }

        private bool CheckForMatchingTrade(OrderbookChange change)
        {
            lock (_matchingLock)
            {
                if (_cancellationToken.IsCancellationRequested || change.DeltaContracts >= 0)
                    return false;

                var cutoff = change.Timestamp - _config.Value.TradeMatchingWindow;
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

        private void CheckForCancelingOrderbookChange(OrderbookChange newChange)
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

            var cutoff = newChange.Timestamp - _config.Value.OrderbookCancelWindow;
            List<(OrderbookChange ExistingChange, OrderbookChange NewChange)> canceledPairs = new();
            lock (_matchingLock)
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
                        canceledPairs.Add((existingChange, newChange));
                        _logger.LogDebug(
                            "TRADEMON-Matched canceling orderbook changes in {MarketTicker}: Change1Id={Change1Id}, Change2Id={Change2Id}, Side={Side}, Price={Price}, Delta1={Delta1}, Delta2={Delta2}, Timestamp1={Timestamp1}, Timestamp2={Timestamp2}",
                            _marketTicker, existingChange.Id, newChange.Id, newChange.Side, newChange.Price, existingChange.DeltaContracts, newChange.DeltaContracts, existingChange.Timestamp, newChange.Timestamp);
                        CalculationsDirty = true;
                    }
                }
            }
            newChange.CanceledPairs = canceledPairs;

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
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Recalculation cancelled for {MarketTicker}", _marketTicker);
                return;
            }

            _logger.LogDebug("Recalculation timer triggered for {MarketTicker}", _marketTicker);
            RecalculateMetrics();
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

            if (!CalculationsDirty) return;
            CalculationsDirty = false;

            foreach (var trade in _tradeEvents.Where(x => x.HasMatchingOrderbookChange == false))
            {
                if (_cancellationToken.IsCancellationRequested)
                {
                    _logger.LogDebug("Trade matching in recalculation cancelled for {MarketTicker}", _marketTicker);
                    return;
                }
                CheckForMatchingOrderbookChange(trade);
                if (trade.HasMatchingOrderbookChange)
                {
                    _logger.LogDebug(
                        "TRADEMON-During cleanup, trade matched with orderbook change for {MarketTicker}: TradeID={TradeID}, TakerSide={TakerSide}, YesPrice={YesPrice}, NoPrice={NoPrice}, Count={Count}, Timestamp={Timestamp}",
                        _marketTicker, trade.Id, trade.TakerSide, trade.YesPrice, trade.NoPrice, trade.Count, trade.Timestamp);
                }
            }

            var stopwatch = Stopwatch.StartNew();
            OrderbookChange[] orderbookChanges;
            lock (_matchingLock)
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

            if (!ValidateEvents(validYesChanges.Concat(validNoChanges).ToList()))
            {
                _logger.LogError("Event validation failed for {MarketTicker}, skipping metric recalculation", _marketTicker);
                return;
            }

            if (Market == null)
            {
                _logger.LogWarning("OrderbookChangeTracker survived market {ticker}", _marketTicker);
                return;
            }
            List<OrderbookData> bids = new List<OrderbookData>(Market.GetBids());
            double elapsedMinutes = GetElapsedMinutes();
            _logger.LogDebug("RecalculateMetrics for {MarketTicker}: elapsedMinutes={ElapsedMinutes}", _marketTicker, elapsedMinutes);

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

            RefreshOrderbookChangeOverTimeMetrics(validYesChanges.Concat(validNoChanges).ToList(), bids, elapsedMinutes);
            RefreshTradeChangeOverTimeMetrics(validYesChanges.Concat(validNoChanges).ToList(), elapsedMinutes);
        }

        private void RefreshOrderbookChangeOverTimeMetrics(List<OrderbookChange> orderbookChanges,
            List<OrderbookData> bids, double elapsedMinutes)
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
            int noThreshold = noBids.Any() ? (int)Math.Ceiling(noBids.Min(x => x.Price) + (100 - noBids.Min(x => x.Price)) * 0.1) : 100;

            var YesTopVelocity = GetTopYesVelocityPerMinute(yesBids, orderbookChanges, elapsedMinutes, yesThreshold);
            var YesBottomVelocity = GetBottomYesVelocityPerMinute(yesBids, orderbookChanges, elapsedMinutes, yesThreshold);
            var NoTopVelocity = GetTopNoVelocityPerMinute(noBids, orderbookChanges, elapsedMinutes, noThreshold);
            var NoBottomVelocity = GetBottomNoVelocityPerMinute(noBids, orderbookChanges, elapsedMinutes, noThreshold);

            Market.LevelCount_Top_Yes_Bid = YesTopVelocity.Levels;
            Market.LevelCount_Bottom_Yes_Bid = YesBottomVelocity.Levels;
            Market.LevelCount_Top_No_Bid = NoTopVelocity.Levels;
            Market.LevelCount_Bottom_No_Bid = NoBottomVelocity.Levels;

            Market.VelocityPerMinute_Bottom_Yes_Bid = Math.Round(YesBottomVelocity.Volume, 2);
            Market.VelocityPerMinute_Top_Yes_Bid = Math.Round(YesTopVelocity.Volume, 2);

            //These two are problems
            Market.VelocityPerMinute_Bottom_No_Bid = Math.Round(NoBottomVelocity.Volume, 2);
            Market.VelocityPerMinute_Top_No_Bid = Math.Round(NoTopVelocity.Volume, 2);

            _logger.LogDebug(
                "{MarketTicker}: Completed RefreshOrderbookChangeOverTimeMetrics  " +
                "YesBidTop={YesBidTopRate}/min ({YesBidTopLevels} levels), " +
                "YesBidBottom={YesBidBottomRate}/min ({YesBidBottomLevels} levels), " +
                "NoBidTop={NoBidTopRate}/min ({NoBidTopLevels} levels), " +
                "NoBidBottom={NoBidBottomRate}/min ({NoBidBottomLevels} levels), ",
                _marketTicker,
                YesTopVelocity.Volume, YesTopVelocity.Levels,
                NoTopVelocity.Volume, NoTopVelocity.Levels,
                YesBottomVelocity.Volume, YesBottomVelocity.Levels,
                NoBottomVelocity.Volume, NoBottomVelocity.Levels,
                Market.VelocityPerMinute_Top_No_Bid, Market.LevelCount_Top_No_Bid,
                Market.VelocityPerMinute_Bottom_No_Bid, Market.LevelCount_Bottom_No_Bid);
        }

        private void RefreshTradeChangeOverTimeMetrics(List<OrderbookChange> orderbookChanges, double elapsedMinutes)
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

            var yesBidNetOrderRate = GetYesNetOrderVolumePerMinute(yesMakerNonTradeChanges, elapsedMinutes);
            var noBidNetOrderRate = GetNoNetOrderVolumePerMinute(noMakerNonTradeChanges, elapsedMinutes);
            var yesTradeRate = GetTradeRatePerMinute_MakerYes(yesMakerTradeRelatedChanges, elapsedMinutes);
            var noTradeRate = GetTradeRatePerMinute_MakerNo(noMakerTradeRelatedChanges, elapsedMinutes);
            var averageTradeSizeYes = GetAverageTradeSize_MakerYes(yesMakerTradeRelatedChanges, elapsedMinutes);
            var averageTradeSizeNo = GetAverageTradeSize_MakerNo(noMakerTradeRelatedChanges, elapsedMinutes);

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
        private void ResetEvents()
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
            CalculationsDirty = true;
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

            var cutoff = DateTime.UtcNow - _config.Value.ChangeWindowDuration;
            int removedCount = 0;
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
                CalculationsDirty = true;
                _logger.LogDebug("Cleaned up {Count} old order book events for {MarketTicker}", removedCount, _marketTicker);
            }
        }

        private bool ValidateEvents(List<OrderbookChange> orderbookChanges)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Event validation cancelled for {MarketTicker}", _marketTicker);
                return false;
            }

            bool isValid = true;
            HashSet<string> changeIds = new HashSet<string>();
            int invalidCount = 0;

            foreach (var change in orderbookChanges)
            {
                if (_cancellationToken.IsCancellationRequested)
                {
                    _logger.LogDebug("Event validation loop cancelled for {MarketTicker}", _marketTicker);
                    return false;
                }

                if (!changeIds.Add(change.Id))
                {
                    _logger.LogWarning("Duplicate ChangeID detected for {MarketTicker}: ChangeID={ChangeID}, Side={Side}, Price={Price}, DeltaContracts={DeltaContracts}, Timestamp={Timestamp}",
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

                if (change.Timestamp > DateTime.UtcNow.AddMinutes(1) || change.Timestamp < DateTime.UtcNow.AddMinutes(-10))
                {
                    _logger.LogWarning("Invalid timestamp for {MarketTicker}: ChangeID={ChangeID}, Timestamp={Timestamp}, Price={Price}, DeltaContracts={DeltaContracts}",
                        _marketTicker, change.Id, change.Timestamp, change.Price, change.DeltaContracts);
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

            return isValid;
        }

        private void CleanupOldTrades()
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var cutoff = DateTime.UtcNow - _config.Value.ChangeWindowDuration;
            var gracePeriodEnd = LastMarketOpenTime.Add(_config.Value.ChangeWindowDuration);
            int removedCount = 0;
            int warningCount = 0;
            bool FoundMatch = false;

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
                            FoundMatch = true;
                            break;
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
                                _marketTicker, isGracePeriodOver, LastMarketOpenTime, gracePeriodEnd, _config.Value.ChangeWindowDuration, DateTime.UtcNow);
                        }
                    }
                    CalculationsDirty = true;
                    removedCount++;
                }
            }

            if (removedCount > 0 && warningCount > 0)
            {
                _logger.LogDebug("TRADEMON-Cleaned up {Count} old trade events for {MarketTicker}, {WarningCount} trades without matching orderbook changes",
                    removedCount, _marketTicker, warningCount);
            }
        }

        private OrderbookChange FindMatchingOrderbookChangeForTrade(TradeEvent trade)
        {
            lock (_matchingLock)
            {
                var cutoff = trade.Timestamp - _config.Value.TradeMatchingWindow;
                var futureCutoff = trade.Timestamp + _config.Value.TradeMatchingWindow;
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

        private double GetElapsedMinutes()
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Elapsed minutes calculation cancelled for {MarketTicker}", _marketTicker);
                return 0;
            }

            if (LastMarketOpenTime == DateTime.MinValue)
            {
                return 0;
            }
            TimeSpan elapsed = DateTime.UtcNow - LastMarketOpenTime;
            double minutes = Math.Min(elapsed.TotalMinutes, _config.Value.ChangeWindowDuration.TotalMinutes);
            _logger.LogDebug("Calculated elapsed minutes for {MarketTicker}: {ElapsedMinutes} minutes (capped at ChangeWindowDuration={ChangeWindowDuration} minutes)",
                _marketTicker, minutes, _config.Value.ChangeWindowDuration.TotalMinutes);
            return minutes;
        }


        #endregion

        #region Velocity and Rate Calculations
        public (double Volume, int Levels) GetBottomNoVelocityPerMinute(List<OrderbookData> noBids, List<OrderbookChange> orderbookChanges, double elapsedMinutes, int threshold)
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
                return (0, 0);
            }
            else
            {
                levels = noBids.Where(x => x.Price > threshold).Count(); // Count lower price levels
            }

            var bottomChanges = validChanges.Where(c => c.Price > threshold).ToList();
            _logger.LogDebug("Calculating VelocityPerMinute_Bottom_No_Bid for {MarketTicker}, Threshold={Threshold}, Levels={Levels}, Changes={ChangeCount}, NoBidsCount={NoBidsCount}",
                _marketTicker, threshold, levels, bottomChanges.Count, noBids.Count);
            foreach (var change in bottomChanges)
            {
                _logger.LogDebug("BottomNoBid change for {MarketTicker}: ChangeID={ChangeID}, Price={Price}, DeltaContracts={DeltaContracts}, IsTradeRelated={IsTradeRelated}, IsCanceled={IsCanceled}",
                    _marketTicker, change.Id, change.Price, change.DeltaContracts, change.IsTradeRelated, change.IsCanceled);
            }

            double volume = 0;
            if (bottomChanges.Any())
            {
                double totalDollarDelta = bottomChanges.Sum(c => c.Price / 100.0 * c.DeltaContracts);
                volume = elapsedMinutes > 0 ? totalDollarDelta / elapsedMinutes : 0;
                _logger.LogDebug("BottomNoBidVelocityPerMinute for {MarketTicker}: TotalDollarDelta={TotalDollarDelta:F2}, Minutes={Minutes:F2}, Rate={Rate:F2} dollars/minute, Levels={Levels}",
                    _marketTicker, totalDollarDelta, elapsedMinutes, volume, levels);
            }
            else
            {
                _logger.LogDebug("No changes found for BottomNoBidVelocityPerMinute in {MarketTicker}, Rate=0, Levels={Levels}", _marketTicker, levels);
            }

            return (Math.Round(volume, 2), levels);
        }

        public int GetTopLevels(List<OrderbookData> Bids, int lowerBound)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Top levels calculation cancelled for {MarketTicker}", _marketTicker);
                return 0;
            }
            return Bids.Where(x => x.Price >= lowerBound).Count();
        }

        public int GetBottomLevels(List<OrderbookData> Bids, int upperBound)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Bottom levels calculation cancelled for {MarketTicker}", _marketTicker);
                return 0;
            }
            return Bids.Where(x => x.Price < upperBound).Count();
        }

        public (double Volume, int Levels) GetBottomYesVelocityPerMinute(List<OrderbookData> yesBids, List<OrderbookChange> orderbookChanges, double elapsedMinutes, int threshold)
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

            if (yesBids.Count == 0)
            {
                _logger.LogDebug("No 'yes' side bids for {MarketTicker}, bottom velocity is 0 as all changes are top-level", _marketTicker);
                return (0, 0); // No bids, so bottom velocity is 0
            }

            int levels = GetBottomLevels(yesBids, threshold);
            var changes = validChanges.Where(c => c.Price < threshold).ToList();
            _logger.LogDebug("Calculating BottomYesBidVelocityPerMinute for {MarketTicker}, Threshold={Threshold}, Levels={Levels}, Changes={ChangeCount}, YesBidsCount={YesBidsCount}",
                _marketTicker, threshold, levels, changes.Count, yesBids.Count);
            foreach (var change in changes)
            {
                _logger.LogDebug("BottomYesBid change: ChangeID={ChangeID}, Price={Price}, DeltaContracts={DeltaContracts}, IsTradeRelated={IsTradeRelated}, IsCanceled={IsCanceled}",
                    change.Id, change.Price, change.DeltaContracts, change.IsTradeRelated, change.IsCanceled);
            }

            double volume = 0;
            if (changes.Any())
            {
                double totalDollarDelta = changes.Sum(c => c.Price / 100.0 * c.DeltaContracts);
                volume = elapsedMinutes > 0 ? totalDollarDelta / elapsedMinutes : 0;
                _logger.LogDebug("BottomYesBidVelocityPerMinute for {MarketTicker}: TotalDollarDelta={TotalDollarDelta:F2}, Minutes={Minutes:F2}, Rate={Rate:F2} dollars/minute, Levels={Levels}",
                    _marketTicker, totalDollarDelta, elapsedMinutes, volume, levels);
            }
            else
            {
                _logger.LogDebug("No changes found for BottomYesBidVelocityPerMinute in {MarketTicker}, Rate=0, Levels={Levels}", _marketTicker, levels);
            }

            return (Math.Round(volume, 2), levels);
        }

        public (double Volume, int Levels) GetTopNoVelocityPerMinute(List<OrderbookData> noBids, List<OrderbookChange> orderbookChanges, double elapsedMinutes, int threshold)
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
                levels = noBids.Where(x => x.Price <= threshold).Count(); // Count higher price levels
            }

            var topChanges = validChanges.Where(c => c.Price <= threshold).ToList();
            _logger.LogDebug("Calculating VelocityPerMinute_Top_No_Bid for {MarketTicker}, Threshold={Threshold}, Levels={Levels}, Changes={ChangeCount}, NoBidsCount={NoBidsCount}",
                _marketTicker, threshold, levels, topChanges.Count, noBids.Count);
            foreach (var change in topChanges)
            {
                _logger.LogDebug("TopNoBid change for {MarketTicker}: ChangeID={ChangeID}, Price={Price}, DeltaContracts={DeltaContracts}, IsTradeRelated={IsTradeRelated}, IsCanceled={IsCanceled}",
                    _marketTicker, change.Id, change.Price, change.DeltaContracts, change.IsTradeRelated, change.IsCanceled);
            }

            double volume = 0;
            if (topChanges.Any())
            {
                double totalDollarDelta = topChanges.Sum(c => c.Price / 100.0 * c.DeltaContracts);
                volume = elapsedMinutes > 0 ? totalDollarDelta / elapsedMinutes : 0;
                _logger.LogDebug("VelocityPerMinute_Top_No_Bid for {MarketTicker}: TotalDollarDelta={TotalDollarDelta:F2}, Minutes={Minutes:F2}, Rate={Rate:F2} dollars/minute, Levels={Levels}",
                    _marketTicker, totalDollarDelta, elapsedMinutes, volume, levels);
            }
            else
            {
                _logger.LogDebug("No changes found for VelocityPerMinute_Top_No_Bid in {MarketTicker}, Rate=0, Levels={Levels}", _marketTicker, levels);
            }

            return (Math.Round(volume, 2), levels);
        }

        public (double Volume, int Levels) GetTopYesVelocityPerMinute(List<OrderbookData> yesBids, List<OrderbookChange> orderbookChanges, double elapsedMinutes, int threshold)
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
                levels = 0; // No bids, so no levels
            }
            else
            {
                levels = GetTopLevels(yesBids, threshold);
            }

            var topChanges = validChanges.Where(c => c.Price >= threshold).ToList();
            _logger.LogDebug("Calculating VelocityPerMinute_Top_Yes_Bid for {MarketTicker}, Threshold={Threshold}, Levels={Levels}, Changes={ChangeCount}, YesBidsCount={YesBidsCount}",
                _marketTicker, threshold, levels, topChanges.Count, yesBids.Count);
            foreach (var change in topChanges)
            {
                _logger.LogDebug("TopYesBid change: ChangeID={ChangeID}, Price={Price}, DeltaContracts={DeltaContracts}, IsTradeRelated={IsTradeRelated}, IsCanceled={IsCanceled}",
                    change.Id, change.Price, change.DeltaContracts, change.IsTradeRelated, change.IsCanceled);
            }

            double volume = 0;
            if (topChanges.Any())
            {
                double totalDollarDelta = topChanges.Sum(c => c.Price / 100.0 * c.DeltaContracts);
                volume = elapsedMinutes > 0 ? totalDollarDelta / elapsedMinutes : 0;
                _logger.LogDebug("VelocityPerMinute_Top_Yes_Bid for {MarketTicker}: TotalDollarDelta={TotalDollarDelta:F2}, Minutes={Minutes:F2}, Rate={Rate:F2} dollars/minute, Levels={Levels}",
                    _marketTicker, totalDollarDelta, elapsedMinutes, volume, levels);
            }
            else
            {
                _logger.LogDebug("No changes found for VelocityPerMinute_Top_Yes_Bid in {MarketTicker}, Rate=0, Levels={Levels}", _marketTicker, levels);
            }

            return (Math.Round(volume, 2), levels);
        }

        public (double Volume, int Count) GetYesNetOrderVolumePerMinute(List<OrderbookChange> yesBidChanges, double minutes)
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
            double volume = minutes > 0 ? totalDollarDelta / minutes : 0;
            volume = Math.Round(volume, 2);

            _logger.LogDebug("OrderRatePerMinute_YesBid for {MarketTicker}: TotalDollarDelta={TotalDollarDelta:F2}, Minutes={Minutes:F2}, Rate={Rate:F2} dollars/minute, Count={Count}",
                _marketTicker, totalDollarDelta, minutes, volume, orderCount);
            return (volume, orderCount);
        }

        public (double Volume, int Count) GetNoNetOrderVolumePerMinute(List<OrderbookChange> noBidChanges, double minutes)
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
            double volume = minutes > 0 ? totalDollarDelta / minutes : 0;
            volume = Math.Round(volume, 2);

            _logger.LogDebug("OrderRatePerMinute_NoBid for {MarketTicker}: TotalDollarDelta={TotalDollarDelta:F2}, Minutes={Minutes:F2}, Rate={Rate:F2} dollars/minute, Count={Count}",
                _marketTicker, totalDollarDelta, minutes, volume, orderCount);
            return (volume, orderCount);
        }

        public (double rate, double volume) GetTradeRatePerMinute_MakerYes(List<OrderbookChange> yesTradeRelatedChanges, double elapsedMinutes)
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
            double rate = elapsedMinutes > 0 ? yesTradeRelatedChanges.Count / elapsedMinutes : 0;

            if (!yesTradeRelatedChanges.Any())
            {
                _logger.LogDebug("No trade-related 'yes' side changes found for {MarketTicker}, Rate=0, Volume=0", _marketTicker);
                volume = 0;
            }
            else
            {
                double totalTradeDollar = yesTradeRelatedChanges.Sum(c => c.Price / 100.0 * c.DeltaContracts);
                volume = elapsedMinutes > 0 ? totalTradeDollar / elapsedMinutes : 0;
                _logger.LogDebug("tradeVolumePerMinute_Yes for {MarketTicker}: TotalTradeDollar={TotalTradeDollar:F2}, Minutes={Minutes:F2}, Rate={Rate:F2}, Volume={Volume:F2} dollars/minute",
                    _marketTicker, totalTradeDollar, elapsedMinutes, rate, volume);
            }

            return (Math.Round(rate, 2), volume);
        }

        public (double rate, double volume) GetTradeRatePerMinute_MakerNo(List<OrderbookChange> noTradeRelatedChanges, double elapsedMinutes)
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
            double rate = elapsedMinutes > 0 ? noTradeRelatedChanges.Count / elapsedMinutes : 0;

            if (!noTradeRelatedChanges.Any())
            {
                _logger.LogDebug("No trade-related 'no' side changes found for {MarketTicker}, Rate=0, Volume=0", _marketTicker);
                volume = 0;
            }
            else
            {
                double totalTradeDollar = noTradeRelatedChanges.Sum(c => c.Price / 100.0 * c.DeltaContracts);
                volume = elapsedMinutes > 0 ? totalTradeDollar / elapsedMinutes : 0;
                _logger.LogDebug("tradeVolumePerMinute_No for {MarketTicker}: TotalTradeDollar={TotalTradeDollar:F2}, Minutes={Minutes:F2}, Rate={Rate:F2}, Volume={Volume:F2} dollars/minute",
                    _marketTicker, totalTradeDollar, elapsedMinutes, rate, volume);
            }

            return (Math.Round(rate, 2), volume);
        }

        public double GetAverageTradeSize_MakerYes(List<OrderbookChange> tradeEvents, double elapsedMinutes)
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

        public double GetAverageTradeSize_MakerNo(List<OrderbookChange> tradeEvents, double elapsedMinutes)
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

        #region Dispose
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