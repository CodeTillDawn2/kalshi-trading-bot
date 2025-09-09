using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using BacklashDTOs;
using BacklashDTOs.Data;
using BacklashDTOs.Exceptions;
using BacklashDTOs.Helpers;
using BacklashDTOs.KalshiAPI;
using System.Collections.Concurrent;
using System.Text.Json;

namespace BacklashBot.Services
{
    public class OrderBookService : IOrderBookService
    {
        private readonly ILogger<IOrderBookService> _logger;
        private readonly IScopeManagerService _scopeManagerService;
        private IStatusTrackerService _statusTrackerService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IServiceFactory _serviceFactory;
        private BlockingCollection<(JsonElement Data, string OfferType, long Seq, Guid EventId)> _eventQueue = new();
        private BlockingCollection<string> _tickerQueue = new();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _orderBookUpdateLocks = new();
        private readonly ConcurrentDictionary<string, object> _orderbookLocks = new();
        private Task _eventProcessor;
        private Task _tickerProcessor;
        private BlockingCollection<string> _notificationQueue = new();
        private bool _notificationQueueDisposed = false;
        private Task _notificationProcessor;
        private bool _eventQueueDisposed = false;
        private bool _tickerQueueDisposed = false;
        private readonly object _queueLock = new();
        private OrderBookEventArgs? _currentOrderBookEventArgs;
        private readonly ConcurrentDictionary<string, List<long>> _lockWaitTimes = new();
        private const int MaxWaitTimeSamples = 100;

        public event EventHandler<string> OrderBookUpdated;

        public OrderBookService(
            ILogger<IOrderBookService> logger,
            IServiceScopeFactory scopeFactory,
            IServiceFactory serviceFactory,
            IScopeManagerService scopeManagerService,
            IStatusTrackerService statusTrackerService)
        {
            _logger = logger;
            _scopeManagerService = scopeManagerService;
            _statusTrackerService = statusTrackerService;
            _serviceFactory = serviceFactory;
            _scopeFactory = scopeFactory;

            _logger.LogDebug("OrderBookService constructor initializing processors...");
            StartProcessors();
            _logger.LogDebug("OrderBookService initialized with processors, EventProcessor Status: {EventStatus}, TickerProcessor Status: {TickerStatus}, NotificationProcessor Status: {NotificationStatus}",
                _eventProcessor.Status, _tickerProcessor.Status, _notificationProcessor.Status);
        }

        public Task StartServicesAsync()
        {
            _logger.LogDebug("OrderBookService StartAsync starting...");
            lock (_queueLock)
            {
                _eventQueue = new BlockingCollection<(JsonElement Data, string OfferType, long Seq, Guid EventId)>();
                _tickerQueue = new BlockingCollection<string>();
                _notificationQueue = new BlockingCollection<string>();
                _eventQueueDisposed = false;
                _tickerQueueDisposed = false;
                _notificationQueueDisposed = false;
            }
            _serviceFactory.GetKalshiWebSocketClient().OrderBookReceived -= HandleOrderBookReceived;
            _serviceFactory.GetKalshiWebSocketClient().TradeReceived -= HandleTradeReceived;
            _serviceFactory.GetKalshiWebSocketClient().OrderBookReceived += HandleOrderBookReceived;
            _serviceFactory.GetKalshiWebSocketClient().TradeReceived += HandleTradeReceived;
            StartProcessors();
            _logger.LogDebug("OrderBookService StartAsync completed, processors started with new queues.");
            return Task.CompletedTask;
        }

        public void AssignWebSocketHandlers()
        {
            _serviceFactory.GetKalshiWebSocketClient().OrderBookReceived -= HandleOrderBookReceived;
            _serviceFactory.GetKalshiWebSocketClient().TradeReceived -= HandleTradeReceived;
            _serviceFactory.GetKalshiWebSocketClient().OrderBookReceived += HandleOrderBookReceived;
            _serviceFactory.GetKalshiWebSocketClient().TradeReceived += HandleTradeReceived;
        }

        public List<OrderbookData> GetCurrentOrderBook(string marketTicker)
        {
            if (!_serviceFactory.GetDataCache().Markets.ContainsKey(marketTicker)) return new List<OrderbookData>();
            var orderbook = _serviceFactory.GetDataCache().Markets[marketTicker]?.OrderbookData;
            if (orderbook == null) return new List<OrderbookData>();
            var lockObj = _orderbookLocks.GetOrAdd(marketTicker, _ => new object());
            lock (lockObj) { return orderbook.OrderBy(x => x.Price).ToList(); }
        }

        public async Task SyncOrderBookAsync(string marketTicker)
        {
            if (!_serviceFactory.GetDataCache().Markets.ContainsKey(marketTicker)) return;

            var semaphore = _orderBookUpdateLocks.GetOrAdd(marketTicker, _ => new SemaphoreSlim(1, 1));
            bool lockAcquired = false;
            var startTime = DateTime.UtcNow;
            try
            {
                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();
                var orderbook = _serviceFactory.GetDataCache().Markets[marketTicker]?.OrderbookData;
                if (orderbook != null)
                {
                    lockAcquired = await semaphore.WaitAsync(30000, _statusTrackerService.GetCancellationToken());
                    if (!lockAcquired)
                    {
                        _logger.LogError("Timeout waiting for sync lock for {MarketTicker}", marketTicker);
                        return;
                    }
                }
                else
                {
                    orderbook = new List<OrderbookData>();
                }
                var waitTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _lockWaitTimes.AddOrUpdate(
                    marketTicker,
                    _ => new List<long> { (long)waitTimeMs },
                    (_, list) => { list.Add((long)waitTimeMs); return list.TakeLast(MaxWaitTimeSamples).ToList(); }
                );
                if (_serviceFactory.GetKalshiWebSocketClient().IsConnected() && _serviceFactory.GetDataCache().Markets[marketTicker].OrderbookData.Any())
                {
                    NotifyOrderBookUpdated(marketTicker);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("SyncOrderBookAsync was cancelled for {MarketTicker}", marketTicker);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync order book for {MarketTicker}", marketTicker);
            }
            finally
            {
                if (lockAcquired)
                {
                    semaphore.Release();
                }
            }
        }

        public void ClearQueueForMarketAsync(string marketTicker)
        {
            _logger.LogDebug("Clearing data for market {MarketTicker}", marketTicker);

            try
            {
                ClearMarketFromQueue(_eventQueue, marketTicker, e => e.Data.GetProperty("msg").GetProperty("market_ticker").GetString() == marketTicker);
                ClearMarketFromQueue(_tickerQueue, marketTicker, t => t == marketTicker);
                ClearMarketFromQueue(_notificationQueue, marketTicker, n => n == marketTicker);

                if (_orderBookUpdateLocks.TryRemove(marketTicker, out var semaphore))
                {
                    try
                    {
                        semaphore.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                        _logger.LogDebug("Semaphore for {MarketTicker} already disposed", marketTicker);
                    }
                }
                _lockWaitTimes.TryRemove(marketTicker, out _);

                lock (_serviceFactory.GetDataCache().Markets)
                {
                    _serviceFactory.GetDataCache().Markets.Remove(marketTicker, out _);
                }

                _logger.LogDebug("Successfully cleared data for market {MarketTicker}", marketTicker);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear data for market {MarketTicker}", marketTicker);
            }
        }


        public bool IsEventQueueUnderLimit(int limit)
        {
            bool queuesUnderLimit = _eventQueue.Count < limit;
            return queuesUnderLimit;
        }

        public async Task StopServicesAsync()
        {
            _logger.LogDebug("OrderBookService stopping...");

            _serviceFactory.GetKalshiWebSocketClient().OrderBookReceived -= HandleOrderBookReceived;
            _serviceFactory.GetKalshiWebSocketClient().TradeReceived -= HandleTradeReceived;

            try
            {
                lock (_queueLock)
                {
                    if (!_eventQueueDisposed)
                    {
                        try
                        {
                            _eventQueue.CompleteAdding();
                            _logger.LogDebug("Marked event queue as complete");
                        }
                        catch (ObjectDisposedException)
                        {
                            _logger.LogDebug("Event queue was already disposed");
                        }
                    }

                    if (!_tickerQueueDisposed)
                    {
                        try
                        {
                            _tickerQueue.CompleteAdding();
                            _logger.LogDebug("Marked ticker queue as complete");
                        }
                        catch (ObjectDisposedException)
                        {
                            _logger.LogDebug("Ticker queue was already disposed");
                        }
                    }

                    if (!_notificationQueueDisposed)
                    {
                        try
                        {
                            _notificationQueue.CompleteAdding();
                            _logger.LogDebug("Marked notification queue as complete");
                        }
                        catch (ObjectDisposedException)
                        {
                            _logger.LogDebug("Notification queue was already disposed");
                        }
                    }
                }

                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5), _statusTrackerService.GetCancellationToken());
                var allTasks = Task.WhenAll(_eventProcessor, _tickerProcessor, _notificationProcessor);
                var completedTask = await Task.WhenAny(allTasks, timeoutTask);
                if (completedTask == timeoutTask)
                {
                    _logger.LogWarning("OrderBookService processor tasks did not complete within 5 seconds");
                }
                else
                {
                    await allTasks.ContinueWith(t =>
                    {
                        if (t.IsCanceled)
                            _logger.LogDebug("OrderBookService queue tasks canceled as expected.");
                        else if (t.IsFaulted)
                            _logger.LogError(t.Exception, "Error in OrderBookService queue tasks.");
                        else
                            _logger.LogDebug("OrderBookService queue tasks completed successfully.");
                    }, TaskContinuationOptions.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error stopping OrderBookService.");
            }

            lock (_queueLock)
            {
                foreach (var semaphore in _orderBookUpdateLocks.Values)
                {
                    try
                    {
                        semaphore.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                        _logger.LogDebug("Semaphore already disposed");
                    }
                }
                _orderBookUpdateLocks.Clear();
                _orderbookLocks.Clear();

                if (!_eventQueueDisposed)
                {
                    try
                    {
                        _eventQueue.Dispose();
                        _eventQueueDisposed = true;
                        _logger.LogDebug("Event queue disposed");
                    }
                    catch (ObjectDisposedException)
                    {
                        _logger.LogDebug("Event queue already disposed");
                    }
                }

                if (!_tickerQueueDisposed)
                {
                    try
                    {
                        _tickerQueue.Dispose();
                        _tickerQueueDisposed = true;
                        _logger.LogDebug("Ticker queue disposed");
                    }
                    catch (ObjectDisposedException)
                    {
                        _logger.LogDebug("Ticker queue was already disposed");
                    }
                }

                if (!_notificationQueueDisposed)
                {
                    try
                    {
                        _notificationQueue.Dispose();
                        _notificationQueueDisposed = true;
                        _logger.LogDebug("Notification queue disposed");
                    }
                    catch (ObjectDisposedException)
                    {
                        _logger.LogDebug("Notification queue was already disposed");
                    }
                }
            }

            _logger.LogDebug("OrderBookService stopped.");
        }

        public (int EventQueueCount, int TickerQueueCount, int NotificationQueueCount) GetQueueCounts()
        {
            return (_eventQueue.Count, _tickerQueue.Count, _notificationQueue.Count);
        }

        private void HandleTradeReceived(object sender, TradeEventArgs args)
        {
            _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();
            _logger.LogDebug("HandleTradeReceived called");
            var msg = args.Data.GetProperty("msg");
            var marketTicker = msg.GetProperty("market_ticker").GetString() ?? "Unknown";
            var takerSide = msg.GetProperty("taker_side").GetString();
            var yesPrice = msg.GetProperty("yes_price").GetInt32();
            var noPrice = msg.GetProperty("no_price").GetInt32();
            var count = msg.GetProperty("count").GetInt32();
            var ts = msg.GetProperty("ts").GetInt64();
            var loggedDate = UnixHelper.ConvertFromUnixTimestamp(ts);

            if (_serviceFactory.GetDataCache().Markets.TryGetValue(marketTicker, out var marketData))
            {
                marketData.ChangeTracker.LogTrade(takerSide, yesPrice, noPrice, count, loggedDate);
            }
            else
            {
                _logger.LogWarning("Market {MarketTicker} not found in cache for trade event", marketTicker);
            }
        }

        private void HandleOrderBookReceived(object sender, OrderBookEventArgs args)
        {
            _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();
            var marketTicker = args.Data.GetProperty("msg").GetProperty("market_ticker").GetString() ?? "Unknown";
            _logger.LogDebug("HandleOrderBookReceived called for offer type: {OfferType}, market: {MarketTicker}",
                args.OfferType, marketTicker);
            _currentOrderBookEventArgs = args;
            QueueOrderBookUpdateAsync(args);
        }

        private void QueueOrderBookUpdateAsync(OrderBookEventArgs args)
        {
            _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();
            var marketTicker = args.Data.GetProperty("msg").GetProperty("market_ticker").GetString() ?? "Unknown";
            _logger.LogDebug("QueueOrderBookUpdateAsync called for offer type: {OfferType}, market: {MarketTicker}", args.OfferType, marketTicker);
            var seq = args.Data.TryGetProperty("seq", out var seqProp) ? seqProp.GetInt64() : -1;
            var eventId = Guid.NewGuid();
            var msg = args.Data.GetProperty("msg");
            _eventQueue.Add((args.Data, args.OfferType, seq, eventId), _statusTrackerService.GetCancellationToken());
            _logger.LogDebug("Queued orderbook update for {MarketTicker}, Message: {0}, Seq: {Seq}, EventId: {EventId}", marketTicker, msg, seq, eventId);
        }

        private void StartProcessors()
        {
            _logger.LogDebug("Starting processor");
            _eventProcessor = Task.Run(() => HandleEventQueueAsync(), _statusTrackerService.GetCancellationToken())
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        _logger.LogError(t.Exception, "Event processor task faulted");
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);
            _tickerProcessor = Task.Run(() => HandleTickerQueueAsync(), _statusTrackerService.GetCancellationToken())
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        _logger.LogError(t.Exception, "Ticker processor task faulted");
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);
            _notificationProcessor = Task.Run(() => HandleNotificationQueueAsync(), _statusTrackerService.GetCancellationToken())
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        _logger.LogError(t.Exception, "Notification processor task faulted");
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private async Task HandleNotificationQueueAsync()
        {
            _logger.LogDebug("HandleNotificationQueueAsync started, CancellationRequested: {CancellationRequested}", _statusTrackerService.GetCancellationToken().IsCancellationRequested);
            while (!_statusTrackerService.GetCancellationToken().IsCancellationRequested)
            {
                try
                {
                    string marketTicker;
                    try
                    {
                        marketTicker = _notificationQueue.Take(_statusTrackerService.GetCancellationToken());
                    }
                    catch (InvalidOperationException) when (_notificationQueue.IsCompleted)
                    {
                        _logger.LogDebug("Notification queue completed, stopping notification queue processing");
                        break;
                    }

                    _logger.LogDebug("Processing notification for {MarketTicker}", marketTicker);
                    OrderBookUpdated?.Invoke(this, marketTicker);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogDebug("Notification queue processing stopped due to cancellation");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing notification queue");
                    await Task.Delay(1000, _statusTrackerService.GetCancellationToken());
                }
            }
            _logger.LogDebug("Notification queue processing stopped.");
        }

        private async Task HandleEventQueueAsync()
        {
            _logger.LogDebug("HandleEventQueueAsync started, CancellationRequested: {CancellationRequested}", _statusTrackerService.GetCancellationToken().IsCancellationRequested);
            while (!_statusTrackerService.GetCancellationToken().IsCancellationRequested)
            {
                string marketTicker = "";
                try
                {
                    (JsonElement data, string offerType, long seq, Guid eventId) eventData;
                    try
                    {
                        eventData = _eventQueue.Take(_statusTrackerService.GetCancellationToken());
                    }
                    catch (InvalidOperationException) when (_eventQueue.IsCompleted)
                    {
                        _logger.LogDebug("Event queue completed, stopping event queue processing");
                        break;
                    }

                    var (data, offerType, seq, eventId) = eventData;
                    marketTicker = data.GetProperty("msg").GetProperty("market_ticker").GetString() ?? "Unknown";
                    _logger.LogDebug("Processing orderbook event for {MarketTicker}, Seq: {Seq}, EventId: {EventId}", marketTicker, seq, eventId);

                    var semaphore = _orderBookUpdateLocks.GetOrAdd(marketTicker, _ => new SemaphoreSlim(1, 1));
                    _logger.LogDebug("Acquiring semaphore for {MarketTicker}, Seq: {Seq}, ThreadId: {ThreadId}",
                        marketTicker, seq, Thread.CurrentThread.ManagedThreadId);
                    var startTime = DateTime.UtcNow;
                    bool lockAcquired = await semaphore.WaitAsync(1000, _statusTrackerService.GetCancellationToken());
                    if (!lockAcquired)
                    {
                        _logger.LogWarning(new OrderbookTransientFailureException(marketTicker,
                            $"Timeout waiting for orderbook update lock for {marketTicker}, Seq: {seq}")
                            , "Timeout waiting for orderbook update lock for {MarketTicker}, Seq: {Seq}", marketTicker, seq);
                        continue;
                    }

                    try
                    {
                        _logger.LogDebug("Applying orderbook update for {MarketTicker}, Seq: {Seq}", marketTicker, seq);
                        await ApplyOrderBookUpdateAsync(data, offerType, seq, eventId, marketTicker);
                        _logger.LogDebug("Completed orderbook update for {MarketTicker}, Seq: {Seq}", marketTicker, seq);
                    }
                    finally
                    {
                        _logger.LogDebug("Releasing semaphore for {MarketTicker}, Seq: {Seq}", marketTicker, seq);
                        semaphore.Release();
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogDebug("Event queue processing stopped due to cancellation");
                    break;
                }
                catch (ObjectDisposedException)
                {
                    _logger.LogDebug("Semaphore was disposed when attempting access.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing event queue iteration while processing market {market}", marketTicker);
                    continue;
                }
            }
            _logger.LogDebug("Event queue processing stopped.");
        }

        private void HandleTickerQueueAsync()
        {
            _logger.LogDebug("HandleTickerQueueAsync started, CancellationRequested: {CancellationRequested}", _statusTrackerService.GetCancellationToken().IsCancellationRequested);
            while (!_statusTrackerService.GetCancellationToken().IsCancellationRequested)
            {
                try
                {
                    string marketTicker;
                    try
                    {
                        marketTicker = _tickerQueue.Take(_statusTrackerService.GetCancellationToken());
                    }
                    catch (InvalidOperationException) when (_tickerQueue.IsCompleted)
                    {
                        _logger.LogDebug("Ticker queue completed, stopping ticker queue processing");
                        break;
                    }
                    GenerateTickerFromOrderBook(marketTicker);
                }
                catch (ObjectDisposedException)
                {
                    _logger.LogDebug("Ticker queue processing stopped due to disposal");
                    break;
                }
                catch (OperationCanceledException)
                {
                    _logger.LogDebug("Ticker queue processing stopped due to cancellation");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing ticker queue");
                    continue;
                }
            }
            _logger.LogDebug("Ticker queue processing stopped.");
        }

        private void ClearMarketFromQueue<T>(BlockingCollection<T> queue, string marketTicker, Func<T, bool> predicate)
        {
            try
            {
                var tempQueue = new BlockingCollection<T>();
                while (queue.TryTake(out var item))
                {
                    if (!predicate(item))
                    {
                        tempQueue.Add(item);
                    }
                }
                while (tempQueue.TryTake(out var item))
                {
                    queue.Add(item);
                }
                _logger.LogDebug("Cleared market {MarketTicker} from queue", marketTicker);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error clearing market {MarketTicker} from queue", marketTicker);
            }
        }

        private async Task ApplyOrderBookUpdateAsync(JsonElement data, string offerType, long seq, Guid eventId, string marketTicker)
        {
            _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();
            _logger.LogDebug("ApplyOrderBookUpdateAsync started for {MarketTicker}, OfferType: {OfferType}, Seq: {Seq}, EventId: {EventId}, Data: {data}",
                marketTicker, offerType, seq, eventId, data.ToString());
            var message = new OrderbookMessage(data, offerType);
            List<OrderbookData>? updatedOrderbook = null;

            IMarketData marketData;

            if (!_serviceFactory.GetDataCache().Markets.TryGetValue(marketTicker, out marketData))
            {
                if (!_serviceFactory.GetDataCache().RecentlyRemovedMarkets.Contains(marketTicker))
                {
                    _logger.LogWarning("Market {MarketTicker} not in cache, skipping order book update, and unsubscribing. Seq: {Seq}", marketTicker, seq);
                }
                else
                {
                    _logger.LogInformation("Market {MarketTicker} not in cache, but it was recently unwatched. Seq: {Seq}", marketTicker, seq);
                }
                try
                {
                    foreach (var action in new[] { "orderbook", "ticker", "trade" })
                        await _serviceFactory.GetKalshiWebSocketClient().UpdateSubscriptionAsync("delete_markets", new string[] { marketTicker }, action);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to unsubscribe from market {MarketTicker} for order book update, Seq: {Seq}", marketTicker, seq);
                }
                return;
            }

            marketData.LastOrderbookEventTimestamp = DateTime.UtcNow;

            try
            {
                var lockObj = _orderbookLocks.GetOrAdd(marketTicker, _ => new object());
                bool priceChanged = false;

                if (offerType == "SNP")
                {
                    _logger.LogInformation("Processing snapshot for {MarketTicker}, Seq: {Seq}", marketTicker, seq);
                    updatedOrderbook = ProcessOrderBookSnapshotAsync(message, marketTicker);
                    priceChanged = true;
                }
                else if (offerType == "DEL")
                {
                    _logger.LogDebug("Processing delta for {MarketTicker}, Seq: {Seq}", marketTicker, seq);
                    updatedOrderbook = ProcessOrderBookDeltaAsync(message, marketTicker);

                    lock (lockObj)
                    {
                        // Use TryGetValue to safely access market data
                        if (_serviceFactory.GetDataCache().Markets.TryGetValue(marketTicker, out var updatedMarketData))
                        {
                            int currentYes = updatedMarketData.BestYesBid;
                            int currentNo = updatedMarketData.BestNoBid;
                            var updatedYes = updatedOrderbook.LastOrDefault(x => x.Side == "yes")?.Price;
                            var updatedNo = updatedOrderbook.LastOrDefault(x => x.Side == "no")?.Price;
                            priceChanged = currentYes != updatedYes || currentNo != updatedNo;
                            if (priceChanged)
                            {
                                _logger.LogInformation(
                                    "Delta update changed price for {MarketTicker}. Yes side: {CurrentYesPrice} -> {UpdatedYesPrice}, No side: {CurrentNoPrice} -> {UpdatedNoPrice}, Seq: {Seq}",
                                    marketTicker, currentYes, updatedYes ?? 0, currentNo, updatedNo ?? 0, seq);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Market {MarketTicker} removed from cache during delta processing, skipping price change check. Seq: {Seq}", marketTicker, seq);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Skipping non-SNP/DEL update for {MarketTicker}, Seq: {Seq}", marketTicker, seq);
                    return;
                }

                if (updatedOrderbook != null)
                {
                    lock (lockObj)
                    {
                        _serviceFactory.GetDataCache().Markets[marketTicker].OrderbookData = updatedOrderbook;
                        _logger.LogDebug("DELTA-Cache updated with {Count} entries for {MarketTicker}, Seq: {Seq}", updatedOrderbook?.Count ?? 0, marketTicker, seq);
                    }

                    NotifyOrderBookUpdated(marketTicker);

                    if ((offerType == "SNP" || priceChanged) && !_tickerQueue.Contains(marketTicker))
                    {
                        _logger.LogDebug("Queueing ticker generation for {MarketTicker}, Seq: {Seq}", marketTicker, seq);
                        _tickerQueue.Add(marketTicker, _statusTrackerService.GetCancellationToken());
                    }
                }

                _logger.LogDebug("ApplyOrderBookUpdateAsync completed for {MarketTicker}, Seq: {Seq}", marketTicker, seq);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("ApplyOrderBookUpdateAsync was cancelled for {MarketTicker}", marketTicker);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update orderbook for {MarketTicker}, Seq: {Seq}", marketTicker, seq);
                throw;
            }
            finally
            {
                _currentOrderBookEventArgs = null;
            }
        }

        private List<OrderbookData> ProcessOrderBookSnapshotAsync(OrderbookMessage message, string marketTicker)
        {
            if (!_serviceFactory.GetDataCache().Markets.ContainsKey(marketTicker)) return new List<OrderbookData>();
            _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();
            _logger.LogDebug("ProcessOrderBookSnapshotAsync started for {MarketTicker}", marketTicker);
            var updatedOrderbook = new List<OrderbookData>();

            try
            {
                var lockObj = _orderbookLocks.GetOrAdd(marketTicker, _ => new object());
                lock (lockObj)
                {
                    var orderbook = _serviceFactory.GetDataCache().Markets[marketTicker]?.OrderbookData;
                    if (orderbook == null)
                    {
                        orderbook = new List<OrderbookData>();
                    }
                    orderbook.Clear();

                    var yesOrders = (message.YesOrders ?? new List<PriceLevel>()).Select(o => (o.Price, o.RestingContracts));
                    var noOrders = (message.NoOrders ?? new List<PriceLevel>()).Select(o => (o.Price, o.RestingContracts));

                    _logger.LogDebug("DELTA-Raw Snapshot for {MarketTicker}: YesOrders=[{YesOrders}], NoOrders=[{NoOrders}]",
                        marketTicker,
                        string.Join("; ", yesOrders.Select(o => $"Price={o.Price},Contracts={o.RestingContracts}")),
                        string.Join("; ", noOrders.Select(o => $"Price={o.Price},Contracts={o.RestingContracts}")));

                    if (_serviceFactory.GetDataCache().Markets.TryGetValue(marketTicker, out var marketData))
                    {
                        foreach (PriceLevel order in message.YesOrders ?? new List<PriceLevel>())
                        {
                            updatedOrderbook.Add(new OrderbookData(marketTicker, order.Price, "yes", order.RestingContracts));
                        }
                        foreach (PriceLevel order in message.NoOrders ?? new List<PriceLevel>())
                        {
                            updatedOrderbook.Add(new OrderbookData(marketTicker, order.Price, "no", order.RestingContracts));
                        }
                        List<OrderbookData>? originalOrderbook =
                            new List<OrderbookData>(orderbook);
                        marketData.ChangeTracker.LogOrderbookSnapshot(originalOrderbook, updatedOrderbook);
                        _serviceFactory.GetDataCache().Markets[marketTicker].OrderbookData = updatedOrderbook;
                        marketData.OrderbookData = updatedOrderbook;

                        _logger.LogDebug("Stored OrderbookData for {MarketTicker}: [{Entries}]",
                            marketTicker,
                            string.Join("; ", updatedOrderbook.Select(o => $"Price={o.Price},Side={o.Side},Contracts={o.RestingContracts}")));

                        marketData.ReceivedFirstSnapshot = true;
                    }

                    _logger.LogDebug("DELTA-Orderbook Snapshot processed for {MarketTicker}, {Count} orders added", marketTicker, updatedOrderbook.Count);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("ProcessOrderBookSnapshotAsync was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing orderbook snapshot for {MarketTicker}", marketTicker);
                throw;
            }
            return updatedOrderbook;
        }

        private List<OrderbookData> ProcessOrderBookDeltaAsync(OrderbookMessage message, string marketTicker)
        {
            if (!_serviceFactory.GetDataCache().Markets.ContainsKey(marketTicker)) return new List<OrderbookData>();
            _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();

            var orderbook = _serviceFactory.GetDataCache().Markets[marketTicker]?.OrderbookData;
            if (orderbook == null && _serviceFactory.GetDataCache().WatchedMarkets.Contains(marketTicker))
            {
                _logger.LogWarning("No order book found in cache for {MarketTicker} or unwatched, returning empty list", marketTicker);
                return new List<OrderbookData>();
            }

            orderbook = orderbook.OrderBy(x => x.Price).ToList();

            try
            {
                OrderbookData? orderData;

                var lockObj = _orderbookLocks.GetOrAdd(marketTicker, _ => new object());
                lock (lockObj)
                {
                    _logger.LogDebug("DELTA-Processing delta for {MarketTicker}, Price: {Price}, Side: {Side}, Delta: {Delta}, CurrentOrderBookCount: {Count}, Kalshi SeqID: {0}",
                        marketTicker, message.Price.Value, message.Side, message.Delta.Value, orderbook.Count, message.Seq);

                    orderData = orderbook.FirstOrDefault(o => o.Price == message.Price && o.Side == message.Side);

                    if (orderData != null)
                    {
                        _logger.LogDebug("DELTA-Found existing orderData for {MarketTicker}, Price: {Price}, Side: {Side}, RestingContracts: {RestingContracts}",
                            marketTicker, orderData.Price, orderData.Side, orderData.RestingContracts);
                        orderbook.Remove(orderData);
                        _serviceFactory.GetDataCache().Markets[marketTicker].OrderbookData = orderbook;
                    }
                    else
                    {
                        _logger.LogDebug("DELTA-No existing orderData found for {MarketTicker}, Price: {Price}, Side: {Side}",
                            marketTicker, message.Price.Value, message.Side);
                    }

                    if (_serviceFactory.GetDataCache().Markets.TryGetValue(marketTicker, out var marketData))
                    {
                        _logger.LogDebug("TRADEMON-Logging orderbook delta for {0}... Side={1}, Price={2}, Delta={3}",
                            marketData.MarketTicker, message.Side, message.Price.Value, message.Delta.Value);
                        marketData.ChangeTracker.LogChange(message.Side, message.Price.Value, message.Delta.Value);
                    }
                    else
                    {
                        _logger.LogWarning("Market {MarketTicker} not found in cache for delta update", marketTicker);
                    }

                    if (message.Delta.Value <= 0 && orderData == null)
                    {
                        _logger.LogWarning(new OrderbookTransientFailureException(marketTicker, "DELTA-Received non-positive delta for non-existent level"),
                            "DELTA-Received non-positive delta for non-existent level: {MarketTicker}, Price: {message.Price}, Side: {Side}, Delta: {Delta}, CurrentOrderBookCount: {Count}",
                            marketTicker, message.Price, message.Side, message.Delta.Value, orderbook.Count);
                        return orderbook;
                    }

                    if (message.Delta.Value > 0 || (orderData != null && orderData.RestingContracts + message.Delta.Value > 0))
                    {
                        if (orderData == null)
                        {
                            orderData = new OrderbookData(marketTicker, message.Price.Value, message.Side, message.Delta.Value);
                            _logger.LogDebug("Created new orderData for {MarketTicker}, Price: {Price}, Side: {Side}, RestingContracts: {RestingContracts}",
                                marketTicker, orderData.Price, orderData.Side, orderData.RestingContracts);
                        }
                        else
                        {
                            orderData = new OrderbookData(marketTicker, message.Price.Value, message.Side, orderData.RestingContracts + message.Delta.Value);
                            _logger.LogDebug("Updated existing orderData for {MarketTicker}, Price: {Price}, Side: {Side}, OldRestingContracts: {OldRestingContracts}, Delta: {Delta}, NewRestingContracts: {NewRestingContracts}",
                                marketTicker, orderData.Price, orderData.Side, orderData.RestingContracts - message.Delta.Value, message.Delta.Value, orderData.RestingContracts);
                        }
                        orderbook.Add(orderData);
                        _logger.LogDebug("Added orderData to orderbook for {MarketTicker}, Price: {Price}, Side: {Side}, RestingContracts: {RestingContracts}, NewOrderBookCount: {Count}",
                            marketTicker, orderData.Price, orderData.Side, orderData.RestingContracts, orderbook.Count);
                    }
                    else
                    {
                        _logger.LogDebug("Skipping add for {MarketTicker}, Price: {Price}, Side: {Side}, Delta: {Delta}, ExistingRestingContracts: {ExistingRestingContracts}, WouldBeZeroOrNegative: {WouldBe}",
                            marketTicker, message.Price.Value, message.Side, message.Delta.Value, orderData?.RestingContracts ?? 0, orderData != null ? orderData.RestingContracts + message.Delta.Value : 0);
                    }

                    orderbook = orderbook.OrderBy(x => x.Price).ToList();
                    _serviceFactory.GetDataCache().Markets[marketTicker].OrderbookData = orderbook;

                    _logger.LogDebug("Delta Update for {MarketTicker}: FinalOrderBook=[{OrderBook}]",
                        marketTicker,
                        string.Join("; ", orderbook.Select(o => $"Price={o.Price},Side={o.Side},Contracts={o.RestingContracts}")));
                    var yesBid = orderbook.LastOrDefault(x => x.Side == "yes")?.Price;
                    var noBid = orderbook.LastOrDefault(x => x.Side == "no")?.Price;
                    if (yesBid == null) yesBid = 0;
                    if (noBid == null) noBid = 0;
                    if (yesBid.HasValue && noBid.HasValue && yesBid == 100 - noBid)
                    {
                        _logger.LogWarning("Invalid order book state for {MarketTicker}: YesBid={YesBid}, NoBid={NoBid}", marketTicker, yesBid, noBid);
                    }
                    _logger.LogDebug("Updated cache for {MarketTicker}, FinalOrderBook: {OrderBook}",
                        marketTicker, string.Join("; ", orderbook.Select(o => $"Price={o.Price},Side={o.Side},Contracts={o.RestingContracts}")));
                }



                return orderbook;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("ProcessOrderBookDeltaAsync was cancelled");
                return orderbook;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing orderbook delta for {MarketTicker}", marketTicker);
                throw;
            }
        }

        private void GenerateTickerFromOrderBook(string marketTicker)
        {
            _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();
            _logger.LogDebug("GenerateTickerFromOrderBook started for {MarketTicker}", marketTicker);
            try
            {
                var orderBook = GetCurrentOrderBook(marketTicker);
                if (!_serviceFactory.GetDataCache().Markets.TryGetValue(marketTicker, out var marketData))
                {
                    _logger.LogWarning("Market {MarketTicker} not found for ticker generation.", marketTicker);
                    return;
                }

                var yesOrders = orderBook.Where(o => o.Side == "yes").OrderByDescending(o => o.Price).ToList();
                var noOrders = orderBook.Where(o => o.Side == "no").OrderBy(o => o.Price).ToList();
                int bestYesBid = yesOrders.Any() ? yesOrders[0].Price : 0;
                int bestYesAsk = noOrders.Any() ? (100 - noOrders[noOrders.Count - 1].Price) : 0;

                _logger.LogDebug("Calculated prices for {MarketTicker}: Ask={Ask}, Bid={Bid}, YesOrders={YesCount}, NoOrders={NoCount}",
                    marketTicker, bestYesAsk, bestYesBid, yesOrders.Count, noOrders.Count);

                var ticker = new TickerDTO
                {
                    market_ticker = marketTicker,
                    yes_ask = bestYesAsk,
                    yes_bid = bestYesBid,
                    LoggedDate = DateTime.UtcNow,
                    ProcessedDate = DateTime.UtcNow
                };

                if (ticker.yes_ask == 0)
                {
                    _logger.LogDebug("Applying ticker from orderbook fix for {0}", marketTicker);
                    ticker.price = 100;
                    ticker.yes_ask = 100;
                }

                _logger.LogDebug("Applying orderbook ticker update for {MarketTicker}, Bid={Bid}, Ask={Ask}", marketTicker, ticker.yes_bid, ticker.yes_ask);
                _serviceFactory.GetMarketDataService().ReceiveTicker(
                    marketTicker,
                    marketId: ticker.market_id,
                    price: ticker.price,
                    yesBid: ticker.yes_bid,
                    yesAsk: ticker.yes_ask,
                    volume: ticker.volume,
                    openInterest: ticker.open_interest,
                    dollarVolume: ticker.dollar_volume,
                    dollarOpenInterest: ticker.dollar_open_interest,
                    ts: ticker.ts,
                    loggedDate: ticker.LoggedDate,
                    processedDate: ticker.ProcessedDate
                );
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("GenerateTickerFromOrderBook was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate ticker for {MarketTicker}", marketTicker);
            }
        }

        private void NotifyOrderBookUpdated(string marketTicker)
        {
            _notificationQueue.Add(marketTicker, _statusTrackerService.GetCancellationToken());
            _logger.LogDebug("Enqueued notification for {MarketTicker}", marketTicker);
        }


    }
}
