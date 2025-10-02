using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using BacklashDTOs;
using BacklashDTOs.Exceptions;
using BacklashInterfaces.Enums;
using BacklashInterfaces.PerformanceMetrics;
using KalshiBotAPI.Configuration;
using KalshiBotAPI.WebSockets.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace KalshiBotAPI.Websockets
{
    /// <summary>
    /// Processes incoming WebSocket messages from Kalshi's trading platform, routing them to appropriate handlers
    /// based on message type. Features configurable message batching for high-volume scenarios, performance
    /// metrics collection, sophisticated locking mechanisms, and enhanced message deduplication with warnings.
    /// Manages event counting, order book message queuing, and integrates with data persistence and API services
    /// for comprehensive market data processing.
    /// </summary>
    public class MessageProcessor : IMessageProcessor
    {
        private readonly ILogger<MessageProcessor> _logger;
        private readonly IWebSocketConnectionManager _connectionManager;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly IStatusTrackerService _statusTrackerService;
        private readonly ISqlDataService _sqlDataService;
        private readonly IKalshiAPIService _kalshiAPIService;
        private readonly MessageProcessorConfig _config;
        private readonly KalshiAPIServiceConfig _apiConfig;
        private bool _isDataPersistenceEnabled;
        private readonly bool _enablePerformanceMonitoring;
        private readonly IPerformanceMonitor _performanceMonitor;
        private readonly ConcurrentDictionary<string, long> _messageTypeCounts;
        private readonly PriorityQueue<(JsonElement Data, string OfferType, long Seq, Guid EventId), long> _orderBookUpdateQueue;
        private readonly ReaderWriterLockSlim? _orderBookQueueLock;
        private readonly object _orderBookQueueSynchronizationLock = new object(); // Keep for backward compatibility
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, SubscriptionState>> _marketChannelSubscriptionStates;
        private readonly Dictionary<string, (int Sid, HashSet<string> Markets)> _channelSubscriptions;
        private readonly object _sequenceNumberSynchronizationLock = new object();
        private long _latestProcessedSequenceNumber = 0;
        private readonly ConcurrentDictionary<string, HashSet<long>> _processedSequenceNumbersByChannel = new ConcurrentDictionary<string, HashSet<long>>();
        /// <summary>
        /// Total count of duplicate messages detected and skipped.
        /// </summary>
        private int _duplicateMessageCount = 0;

        /// <summary>
        /// Timestamp of the last duplicate message warning log.
        /// </summary>
        private DateTime _lastDuplicateWarningTime = DateTime.UtcNow;

        /// <summary>
        /// Count of duplicate messages detected within the current time window.
        /// </summary>
        private int _duplicateMessagesInWindow = 0;

        /// <summary>
        /// Total count of "Already subscribed" error messages detected.
        /// </summary>
        private int _alreadySubscribedErrorCount = 0;

        /// <summary>
        /// Timestamp of the last "Already subscribed" error warning log.
        /// </summary>
        private DateTime _lastAlreadySubscribedWarningTime = DateTime.UtcNow;

        /// <summary>
        /// Count of "Already subscribed" errors detected within the current time window.
        /// </summary>
        private int _alreadySubscribedErrorsInWindow = 0;

        // Message batching fields
        /// <summary>
        /// Queue for batching messages when message batching is enabled for high-volume scenarios.
        /// </summary>
        private readonly ConcurrentQueue<string> _messageBatchQueue = new();

        /// <summary>
        /// Semaphore to ensure thread-safe batch processing operations.
        /// </summary>
        private readonly SemaphoreSlim _batchProcessingSemaphore = new(1, 1);

        /// <summary>
        /// Background task that processes message batches at configured intervals.
        /// </summary>
        private Task _batchProcessingTask = null!;

        /// <summary>
        /// Cancellation token source for controlling the batch processing task lifecycle.
        /// </summary>
        private readonly CancellationTokenSource _batchCancellationSource = new();


        // Performance metrics fields
        /// <summary>
        /// Stopwatch for measuring message processing performance.
        /// </summary>
        private readonly Stopwatch _processingStopwatch = new();

        /// <summary>
        /// Total number of messages processed since last metrics reset.
        /// </summary>
        private long _totalMessagesProcessed = 0;

        /// <summary>
        /// Total processing time in milliseconds since last metrics reset.
        /// </summary>
        private long _totalProcessingTimeMs = 0;

        /// <summary>
        /// Timestamp of the last performance metrics log.
        /// </summary>
        private DateTime _lastMetricsLogTime = DateTime.UtcNow;

        /// <summary>
        /// Lock object for thread-safe performance metrics operations.
        /// </summary>
        private readonly object _metricsLock = new object();

        // private int _orderBookSubscriptionId = 0; // Currently unused, reserved for future subscription ID tracking
        private Task _messageReceivingTask = null!;
        private CancellationToken _processingCancellationToken;
        private DateTime _lastMessageTimestamp = DateTime.UtcNow;

        // Rate limiter for lifecycle events
        private readonly object _lifecycleRateLimiterLock = new object();
        private DateTime _lifecycleRateLimiterLastReset = DateTime.UtcNow;
        private int _lifecycleRateLimiterCounter = 0;

        // Queue for delayed API calls
        private readonly ConcurrentQueue<(string MarketTicker, TaskCompletionSource<bool> Tcs, DateTime QueuedTime)> _delayedApiCallQueue = new();
        private readonly SemaphoreSlim _delayedProcessingSemaphore = new(1, 1);
        private Task _delayedApiProcessingTask = null!;
        private readonly CancellationTokenSource _delayedProcessingCancellation = new();

        // Delayed API call metrics
        private long _totalDelayedApiCalls = 0;
        private long _totalWaitTimeMs = 0;
        private long _maxWaitTimeMs = 0;
        private DateTime _lastDelayedMetricsTime = DateTime.UtcNow;
        private readonly object _delayedMetricsLock = new object();

        /// <summary>
        /// Occurs when an order book update is received.
        /// </summary>
        public event EventHandler<OrderBookEventArgs>? OrderBookReceived;

        /// <summary>
        /// Occurs when a ticker update is received.
        /// </summary>
        public event EventHandler<TickerEventArgs>? TickerReceived;

        /// <summary>
        /// Occurs when a trade event is received.
        /// </summary>
        public event EventHandler<TradeEventArgs>? TradeReceived;

        /// <summary>
        /// Occurs when a fill event is received.
        /// </summary>
        public event EventHandler<FillEventArgs>? FillReceived;

        /// <summary>
        /// Occurs when a market lifecycle event is received.
        /// </summary>
        public event EventHandler<MarketLifecycleEventArgs>? MarketLifecycleReceived;

        /// <summary>
        /// Occurs when an event lifecycle event is received.
        /// </summary>
        public event EventHandler<EventLifecycleEventArgs>? EventLifecycleReceived;

        /// <summary>
        /// Occurs when any WebSocket message is received, providing the timestamp.
        /// </summary>
        public event EventHandler<DateTime>? MessageReceived;

        /// <summary>
        /// Handles order book processed events from the subscription manager and forwards them as OrderBookReceived events.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The order book event arguments.</param>
        private void OnOrderBookProcessed(object? sender, OrderBookEventArgs e)
        {
            OrderBookReceived?.Invoke(this, e);
        }

        /// <summary>
        /// Initializes a new instance of the MessageProcessor with required dependencies.
        /// Sets up internal data structures for message processing, event counting, and queue management.
        /// </summary>
        /// <param name="logger">Logger for recording processing activities and errors.</param>
        /// <param name="connectionManager">Manages WebSocket connection lifecycle and communication.</param>
        /// <param name="subscriptionManager">Handles market data subscription management and state tracking.</param>
        /// <param name="statusTrackerService">Provides system status and cancellation token management.</param>
        /// <param name="sqlDataService">Handles data persistence to SQL database when enabled.</param>
        /// <param name="kalshiAPIService">Provides access to Kalshi API for market data retrieval and updates.</param>
        /// <param name="config">Configuration settings for WebSocket operations.</param>
        /// <param name="apiConfig">Configuration settings for Kalshi API operations.</param>
        /// <param name="performanceMonitor">Service for recording performance metrics.</param>
        public MessageProcessor(
            ILogger<MessageProcessor> logger,
            IWebSocketConnectionManager connectionManager,
            ISubscriptionManager subscriptionManager,
            IStatusTrackerService statusTrackerService,
            ISqlDataService sqlDataService,
            IKalshiAPIService kalshiAPIService,
            MessageProcessorConfig config,
            IOptions<KalshiAPIServiceConfig> apiConfig,
            IPerformanceMonitor performanceMonitor)
        {
            _logger = logger;
            _connectionManager = connectionManager;
            _subscriptionManager = subscriptionManager;
            _statusTrackerService = statusTrackerService;
            _sqlDataService = sqlDataService;
            _kalshiAPIService = kalshiAPIService;
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _apiConfig = apiConfig?.Value ?? throw new ArgumentNullException(nameof(apiConfig));
            _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
            _isDataPersistenceEnabled = false; // Default to false, will be set by SetDataPersistenceEnabled method
            _enablePerformanceMonitoring = _config.EnablePerformanceMetrics;
            _processingCancellationToken = statusTrackerService.GetCancellationToken();

            // Initialize locking mechanism based on configuration
            _orderBookQueueLock = _config.UseAdvancedLocking
                ? new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion)
                : null;

            // Initialize internal state
            _messageTypeCounts = new ConcurrentDictionary<string, long>();
            _orderBookUpdateQueue = new PriorityQueue<(JsonElement Data, string OfferType, long Seq, Guid EventId), long>();
            _marketChannelSubscriptionStates = new ConcurrentDictionary<string, ConcurrentDictionary<string, SubscriptionState>>();
            _channelSubscriptions = new Dictionary<string, (int Sid, HashSet<string> Markets)>();

            // Initialize message type counts
            _messageTypeCounts.TryAdd("OrderBook", 0);
            _messageTypeCounts.TryAdd("Ticker", 0);
            _messageTypeCounts.TryAdd("Trade", 0);
            _messageTypeCounts.TryAdd("Fill", 0);
            _messageTypeCounts.TryAdd("MarketLifecycle", 0);
            _messageTypeCounts.TryAdd("EventLifecycle", 0);
            _messageTypeCounts.TryAdd("Subscribe", 0);
            _messageTypeCounts.TryAdd("Unsubscribe", 0);
            _messageTypeCounts.TryAdd("Ok", 0);
            _messageTypeCounts.TryAdd("Error", 0);
            _messageTypeCounts.TryAdd("Unknown", 0);

            // Start batch processing task if enabled
            if (_config.EnableMessageBatching)
            {
                _batchProcessingTask = Task.Run(() => ProcessMessageBatchAsync(), _batchCancellationSource.Token);
            }

            // Start delayed API processing task
            _delayedApiProcessingTask = Task.Run(() => ProcessDelayedApiCallsAsync(), _delayedProcessingCancellation.Token);

            _logger.LogInformation("MessageProcessor initialized with configuration: Batching={BatchingEnabled}, AdvancedLocking={AdvancedLockingEnabled}, Metrics={MetricsEnabled}",
                _config.EnableMessageBatching, _config.UseAdvancedLocking, _enablePerformanceMonitoring);

            // Subscribe to order book processed events from subscription manager
            _subscriptionManager.OrderBookProcessed += OnOrderBookProcessed;
        }

        /// <summary>
        /// Starts the background message processing task that continuously receives and processes WebSocket messages.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task StartProcessingAsync()
        {
            _messageReceivingTask = Task.Run(() => ReceiveAsync(), _processingCancellationToken);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Stops the message processing task and waits for it to complete.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task StopProcessingAsync()
        {
            // Cancel batch processing
            _batchCancellationSource.Cancel();

            if (_batchProcessingTask != null && !_batchProcessingTask.IsCompleted)
            {
                try
                {
                    await _batchProcessingTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancelling
                }
            }

            // Cancel delayed API processing
            _delayedProcessingCancellation.Cancel();

            if (_delayedApiProcessingTask != null && !_delayedApiProcessingTask.IsCompleted)
            {
                try
                {
                    await _delayedApiProcessingTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancelling
                }
            }

            if (_messageReceivingTask != null && !_messageReceivingTask.IsCompleted)
            {
                await _messageReceivingTask.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets the current count of order book update messages in the processing queue.
        /// Uses ReaderWriterLockSlim for thread-safe access when advanced locking is enabled,
        /// otherwise falls back to standard lock for backward compatibility.
        /// </summary>
        public int OrderBookMessageQueueCount
        {
            get
            {
                if (_config.UseAdvancedLocking && _orderBookQueueLock != null)
                {
                    _orderBookQueueLock.EnterReadLock();
                    try
                    {
                        return _orderBookUpdateQueue.Count;
                    }
                    finally
                    {
                        _orderBookQueueLock.ExitReadLock();
                    }
                }
                else
                {
                    lock (_orderBookQueueSynchronizationLock)
                    {
                        return _orderBookUpdateQueue.Count;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the count of pending subscription confirmations. Always returns 0 as this is now managed by SubscriptionManager.
        /// </summary>
        public int PendingConfirmsCount => 0;

        /// <summary>
        /// Gets the count of duplicate messages detected and skipped.
        /// </summary>
        public int DuplicateMessageCount => _duplicateMessageCount;

        /// <summary>
        /// Gets the total number of messages processed since last metrics reset.
        /// </summary>
        public long TotalMessagesProcessed => _totalMessagesProcessed;

        /// <summary>
        /// Gets the total processing time in milliseconds since last metrics reset.
        /// </summary>
        public long TotalProcessingTimeMs => _totalProcessingTimeMs;

        /// <summary>
        /// Gets the timestamp of the last performance metrics log.
        /// </summary>
        public DateTime LastMetricsLogTime => _lastMetricsLogTime;

        /// <summary>
        /// Gets the current messages per second rate based on recent processing.
        /// </summary>
        public double MessagesPerSecond
        {
            get
            {
                lock (_metricsLock)
                {
                    var timeSinceLastLog = DateTime.UtcNow - _lastMetricsLogTime;
                    if (timeSinceLastLog.TotalMilliseconds > 0)
                    {
                        return _totalMessagesProcessed / timeSinceLastLog.TotalSeconds;
                    }
                    return 0;
                }
            }
        }

        /// <summary>
        /// Gets the average processing time per message in milliseconds.
        /// </summary>
        public double AverageProcessingTimeMs
        {
            get
            {
                lock (_metricsLock)
                {
                    return _totalMessagesProcessed > 0 ? (double)_totalProcessingTimeMs / _totalMessagesProcessed : 0;
                }
            }
        }

        /// <summary>
        /// Gets the count of messages by type processed since startup.
        /// </summary>
        /// <returns>Dictionary containing message type counts.</returns>
        public IReadOnlyDictionary<string, long> GetMessageTypeCounts()
        {
            return _messageTypeCounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// Continuously receives WebSocket messages and processes them until cancellation is requested.
        /// Handles message fragmentation, connection monitoring, and error recovery.
        /// </summary>
        /// <returns>A task representing the asynchronous receive operation.</returns>
        private async Task ReceiveAsync()
        {
            _logger.LogInformation("WebSocket message receiving task started");
            var buffer = new byte[16384]; // Use configurable buffer size
            var messageBuilder = new StringBuilder();
            try
            {
                while (!_processingCancellationToken.IsCancellationRequested)
                {
                    var webSocket = _connectionManager.GetWebSocket();
                    if (webSocket == null || webSocket.State != WebSocketState.Open)
                        throw new InvalidOperationException("WebSocket connection lost");

                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogError("WebSocket closed by server: Code={Code}, Reason={Reason}", result.CloseStatus, result.CloseStatusDescription);
                        throw new InvalidOperationException($"WebSocket closed: {result.CloseStatusDescription}");
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var messagePart = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        messageBuilder.Append(messagePart);

                        if (result.EndOfMessage)
                        {
                            var fullMessage = messageBuilder.ToString();
                            _lastMessageTimestamp = DateTime.UtcNow;
                            _logger.LogInformation("Received complete WebSocket message: Length={Length}, Timestamp={Timestamp}", fullMessage.Length, _lastMessageTimestamp);

                            // Use batching or direct processing based on configuration
                            if (_config.EnableMessageBatching)
                            {
                                _messageBatchQueue.Enqueue(fullMessage);
                            }
                            else
                            {
                                await ProcessMessageAsync(fullMessage);
                            }

                            messageBuilder.Clear();
                        }
                    }
                    else
                    {
                        _logger.LogError("Unexpected WebSocket message type: {MessageType}", result.MessageType);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("WebSocket message receiving task cancelled at {Timestamp}", DateTime.UtcNow);
            }
            catch (WebSocketException ex) when (ex.Message.Contains("without completing the close handshake"))
            {
                // Handle reconnection - placeholder for future implementation
            }
            catch (Exception ex)
            {
                _logger.LogError("WebSocket receiver encountered error: {Message}. Attempting to reconnect", ex.Message);
                // Handle reconnection - placeholder for future implementation
            }
            finally
            {
                _logger.LogInformation("WebSocket message receiving task completed at {Timestamp}", DateTime.UtcNow);
            }
        }

        /// <summary>
        /// Processes a single WebSocket message by parsing its JSON content and routing it to the appropriate handler
        /// based on the message type. Updates event counts and triggers relevant events for subscribers.
        /// </summary>
        /// <param name="message">The raw JSON message string received from the WebSocket.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        public async Task ProcessMessageAsync(string message)
        {
            var processingStartTime = _processingStopwatch.ElapsedMilliseconds;

            _logger.LogInformation("Processing WebSocket message: {Message}, ProcessingStartTime={ProcessingStartTime}", message, DateTime.UtcNow);
            try
            {
                _processingCancellationToken.ThrowIfCancellationRequested();
                var data = JsonSerializer.Deserialize<JsonElement>(message);
                var msgType = data.GetProperty("type").GetString() ?? "unknown";

                // Check for message deduplication based on sequence number per channel
                long sequenceNumber = 0;
                string channelKey = msgType; // Default to message type as channel identifier
                if (data.TryGetProperty("seq", out var seqProp) && seqProp.TryGetInt64(out sequenceNumber))
                {
                    // For messages with sid, use sid as the channel identifier since seq is per sid
                    if (data.TryGetProperty("sid", out var sidProp) && sidProp.TryGetInt32(out int sid))
                    {
                        channelKey = sid.ToString();
                    }

                    lock (_sequenceNumberSynchronizationLock)
                    {
                        // Get or create sequence number set for this channel
                        var channelSequences = _processedSequenceNumbersByChannel.GetOrAdd(channelKey, _ => new HashSet<long>());

                        if (channelSequences.Contains(sequenceNumber))
                        {
                            if (_enablePerformanceMonitoring)
                            {
                                _duplicateMessageCount++;
                                _duplicateMessagesInWindow++;
                                CheckDuplicateMessageWarnings();
                            }
                            _logger.LogWarning("DUP-Duplicate message detected with sequence number {SequenceNumber} for channel {Channel}. Total duplicates: {_DuplicateMessageCount}. Skipping processing.", sequenceNumber, channelKey, _duplicateMessageCount);
                            return; // Skip processing duplicate messages
                        }
                        channelSequences.Add(sequenceNumber);

                        // Update latest processed sequence number
                        if (sequenceNumber > _latestProcessedSequenceNumber)
                        {
                            _latestProcessedSequenceNumber = sequenceNumber;
                        }

                        // Periodic cleanup of old sequence numbers to prevent memory leaks
                        if (channelSequences.Count > _config.MaxSequenceNumbersToKeep)
                        {
                            var oldestToKeep = _latestProcessedSequenceNumber - (_config.MaxSequenceNumbersToKeep / 2);
                            channelSequences.RemoveWhere(seq => seq < oldestToKeep);
                        }
                    }
                }

                _logger.LogDebug("Received WebSocket message type: {MsgType}, Seq: {SequenceNumber}", msgType, sequenceNumber);

                if (MessageReceived != null)
                    MessageReceived?.Invoke(this, DateTime.UtcNow);

                // Record channel activity for stale detection
                string channelForActivity = msgType switch
                {
                    "orderbook_snapshot" => "orderbook_delta",
                    "orderbook_delta" => "orderbook_delta",
                    "ticker" => "ticker",
                    "trade" => "trade",
                    "fill" => "fill",
                    "market_lifecycle_v2" => "market_lifecycle_v2",
                    "event_lifecycle" => "event_lifecycle",
                    _ => null
                };

                if (channelForActivity != null)
                {
                    _subscriptionManager.RecordChannelActivity(channelForActivity);
                }

                switch (msgType)
                {
                    case "orderbook_snapshot":
                    case "orderbook_delta":
                        await ProcessOrderBookUpdateAsync(data, msgType);
                        break;
                    case "ticker":
                        await ProcessTickerUpdateAsync(data);
                        break;
                    case "trade":
                        await ProcessTradeUpdateAsync(data);
                        break;
                    case "fill":
                        await ProcessFillUpdateAsync(data);
                        break;
                    case "market_lifecycle_v2":
                        await ProcessMarketLifecycleUpdateAsync(data);
                        break;
                    case "event_lifecycle":
                        await ProcessEventLifecycleUpdateAsync(data);
                        break;
                    case "subscribed":
                        await ProcessSubscriptionConfirmationAsync(data);
                        break;
                    case "unsubscribed":
                        await ProcessUnsubscriptionConfirmationAsync(data);
                        break;
                    case "ok":
                        await ProcessOkConfirmationAsync(data);
                        break;
                    case "error":
                        await ProcessErrorMessageAsync(data);
                        break;
                    default:
                        _messageTypeCounts.AddOrUpdate("Unknown", 1, (_, count) => count + 1);
                        _logger.LogWarning("Received unknown message type: {MsgType}, Message: {Message}", msgType, message);
                        break;
                }

                // Update performance metrics
                var processingTime = _processingStopwatch.ElapsedMilliseconds - processingStartTime;
                if (_enablePerformanceMonitoring)
                {
                    lock (_metricsLock)
                    {
                        _totalMessagesProcessed++;
                        _totalProcessingTimeMs += processingTime;
                    }
                }
                LogPerformanceMetrics();
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown when cancellation token is set
                _logger.LogDebug("WebSocket message processing cancelled during shutdown");
            }
            catch (JsonException ex)
            {
                _logger.LogWarning("The input does not contain any JSON tokens. Raw: {RawMessage}. Exception: {Message}, Inner: {Inner}", message, ex.Message, ex.InnerException?.Message ?? "None");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing WebSocket message: {Message}", message);
            }
        }

        /// <summary>
        /// Gets the latest sequence number processed from WebSocket messages.
        /// Used for maintaining message ordering and detecting gaps in message streams.
        /// </summary>
        public long LastSequenceNumber => _latestProcessedSequenceNumber;

        /// <summary>
        /// Resets all message type counters to zero. Used for periodic statistics reset or testing.
        /// </summary>
        public void ResetEventCounts()
        {
            _messageTypeCounts.Clear();
            // Also clear per-channel sequence numbers to prevent stale data
            _processedSequenceNumbersByChannel.Clear();
        }

        /// <summary>
        /// Configures whether processed market data should be persisted to the SQL database.
        /// When enabled, order book, ticker, trade, and lifecycle data will be stored for analysis.
        /// </summary>
        /// <param name="isDataPersistenceEnabled">True to enable SQL persistence, false to disable.</param>
        public void SetWriteToSql(bool isDataPersistenceEnabled)
        {
            _isDataPersistenceEnabled = isDataPersistenceEnabled;
            _logger.LogInformation("Data persistence to SQL set to {IsEnabled}", _isDataPersistenceEnabled);
        }

        /// <summary>
        /// Removes all queued order book update messages for a specific market ticker.
        /// Used when clearing stale data or resetting market state.
        /// </summary>
        /// <param name="marketTicker">The market ticker to clear messages for.</param>
        public void ClearOrderBookQueue(string marketTicker)
        {
            _logger.LogInformation("Clearing order book update queue for market: {MarketTicker}", marketTicker);

            if (_config.UseAdvancedLocking && _orderBookQueueLock != null)
            {
                _orderBookQueueLock.EnterWriteLock();
                try
                {
                    ClearOrderBookQueueInternal(marketTicker);
                }
                finally
                {
                    _orderBookQueueLock.ExitWriteLock();
                }
            }
            else
            {
                lock (_orderBookQueueSynchronizationLock)
                {
                    ClearOrderBookQueueInternal(marketTicker);
                }
            }
        }

        private void ClearOrderBookQueueInternal(string marketTicker)
        {
            var tempQueue = new PriorityQueue<(JsonElement Data, string OfferType, long Seq, Guid EventId), long>();
            while (_orderBookUpdateQueue.TryDequeue(out var message, out var seq))
            {
                if (message.Data.TryGetProperty("msg", out var msg) &&
                    msg.TryGetProperty("market_ticker", out var tickerProp) &&
                    tickerProp.GetString() != marketTicker)
                {
                    tempQueue.Enqueue(message, seq);
                }
            }
            while (tempQueue.TryDequeue(out var message, out var seq))
            {
                _orderBookUpdateQueue.Enqueue(message, seq);
            }
            _logger.LogInformation("Cleared order book messages for {MarketTicker}. Remaining queue count: {Count}", marketTicker, _orderBookUpdateQueue.Count);
        }

        /// <summary>
        /// Waits asynchronously for the order book update queue to be empty for a specific market,
        /// with a configurable timeout. Used to ensure all pending updates are processed before
        /// taking snapshots or performing market analysis.
        /// </summary>
        /// <param name="marketTicker">The market ticker to wait for queue clearance.</param>
        /// <param name="timeout">Maximum time to wait for queue clearance.</param>
        /// <returns>A task representing the asynchronous wait operation.</returns>
        public async Task WaitForEmptyOrderBookQueueAsync(string marketTicker, TimeSpan timeout)
        {
            var startTime = DateTime.UtcNow;
            bool waited = false;
            var effectiveTimeout = timeout;

            while (!_processingCancellationToken.IsCancellationRequested)
            {
                bool hasPendingUpdates;

                if (_config.UseAdvancedLocking && _orderBookQueueLock != null)
                {
                    _orderBookQueueLock.EnterReadLock();
                    try
                    {
                        hasPendingUpdates = CheckForPendingUpdates(marketTicker);
                    }
                    finally
                    {
                        _orderBookQueueLock.ExitReadLock();
                    }
                }
                else
                {
                    lock (_orderBookQueueSynchronizationLock)
                    {
                        hasPendingUpdates = CheckForPendingUpdates(marketTicker);
                    }
                }

                if (!hasPendingUpdates)
                {
                    _logger.LogInformation("Order book queue cleared for market: {MarketTicker}", marketTicker);
                    return;
                }
                {
                    waited = true;
                    _logger.LogInformation("Waiting for order book queue to clear for market: {MarketTicker}", marketTicker);
                }

                if (DateTime.UtcNow - startTime > effectiveTimeout)
                {
                    _logger.LogWarning("Timeout waiting for order book queue to clear for market: {MarketTicker} after {TimeoutSeconds}s", marketTicker, effectiveTimeout.TotalSeconds);
                    return;
                }

                await Task.Delay(100, _processingCancellationToken);
            }

            if (waited)
                _logger.LogInformation("Market {MarketTicker} waited {WaitTime}s before saving snapshot", marketTicker, (DateTime.UtcNow - startTime).TotalSeconds);
        }

        private bool CheckForPendingUpdates(string marketTicker)
        {
            if (_orderBookUpdateQueue.Count == 0)
                return false;

            return _orderBookUpdateQueue.UnorderedItems.Any(item =>
            {
                if (item.Element.Data.TryGetProperty("msg", out var msg) &&
                    msg.TryGetProperty("market_ticker", out var tickerProp))
                {
                    return tickerProp.GetString() == marketTicker;
                }
                return false;
            });
        }

        /// <summary>
        /// Gets the count of different event types processed for a specific market ticker.
        /// Currently returns default values as market-specific event tracking is not yet implemented.
        /// </summary>
        /// <param name="marketTicker">The market ticker to get event counts for.</param>
        /// <returns>A tuple containing counts for orderbook, trade, and ticker events.</returns>
        public (int orderbookEvents, int tradeEvents, int tickerEvents) GetEventCountsByMarket(string marketTicker)
        {
            // TODO: Implement market-specific event counting by maintaining per-market dictionaries
            // This would require tracking events by market ticker in addition to global counts
            return (0, 0, 0);
        }

        /// <summary>
        /// Processes order book snapshot and delta messages, enqueuing them for ordered processing.
        /// Handles both full snapshots and incremental updates to maintain current order book state.
        /// </summary>
        /// <param name="data">The JSON data containing the order book message.</param>
        /// <param name="msgType">The message type ("orderbook_snapshot" or "orderbook_delta").</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        private async Task ProcessOrderBookUpdateAsync(JsonElement data, string msgType)
        {
            _logger.LogDebug("Processing order book update: {MsgType}, DataPersistence: {IsEnabled}", msgType, _isDataPersistenceEnabled);
            _messageTypeCounts.AddOrUpdate("OrderBook", 1, (_, count) => count + 1);

            try
            {
                // Extract sid and seq for enqueuing
                if (data.TryGetProperty("sid", out var sidProp) && data.TryGetProperty("seq", out var seqProp))
                {
                    var sid = sidProp.GetInt32();
                    var seq = seqProp.GetInt64();
                    var offerType = msgType == "orderbook_snapshot" ? "SNP" : "DEL";

                    // Enqueue the message for ordered processing
                    _subscriptionManager.EnqueueOrderBookMessage(sid, data, offerType, seq);

                    if (_isDataPersistenceEnabled)
                    {
                        _logger.LogDebug("Persisting order book data: {MsgType}, offerType: {OfferType}", msgType, offerType);
                        await _sqlDataService.StoreOrderBookAsync(data, offerType);
                    }
                    else
                    {
                        _logger.LogDebug("Skipping data persistence for order book: DataPersistence is disabled");
                    }
                }
                else
                {
                    _logger.LogWarning("Order book message missing sid or seq properties, cannot enqueue for ordered processing");
                }
            }
            catch (Exception ex)
            {
                if (!data.TryGetProperty("msg", out var msg))
                {
                    _logger.LogError(ex, "Error processing order book update message. Could not parse message.");
                }
                if (!msg.TryGetProperty("market_ticker", out var marketTickerProp) || string.IsNullOrEmpty(marketTickerProp.GetString()))
                {
                    _logger.LogError(ex, "Error processing order book update message. Could not parse market ticker.");
                }
                _logger.LogWarning(new OrderbookTransientFailureException(marketTickerProp.GetString(), "", ex), "Problem processing order book update message. Exception: {Message}, Inner: {Inner}", ex.Message, ex.InnerException?.Message ?? "None");
            }
        }

        /// <summary>
        /// Processes ticker messages containing real-time market price and volume data.
        /// Extracts ticker information and triggers events for subscribers while optionally persisting data.
        /// </summary>
        /// <param name="data">The JSON data containing the ticker message.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        private async Task ProcessTickerUpdateAsync(JsonElement data)
        {
            _logger.LogDebug("Processing ticker update, DataPersistence: {IsEnabled}", _isDataPersistenceEnabled);
            _messageTypeCounts.AddOrUpdate("Ticker", 1, (_, count) => count + 1);

            try
            {
                var msg = data.GetProperty("msg");
                var eventArgs = new TickerEventArgs();

                // Deserialize the ticker data from the "msg" property
                if (msg.TryGetProperty("market_id", out var marketIdProp) && marketIdProp.TryGetGuid(out var marketId))
                    eventArgs.market_id = marketId;
                if (msg.TryGetProperty("market_ticker", out var tickerProp))
                    eventArgs.market_ticker = tickerProp.GetString() ?? "";
                if (msg.TryGetProperty("price", out var priceProp))
                    eventArgs.price = priceProp.GetInt32();
                if (msg.TryGetProperty("yes_bid", out var yesBidProp))
                    eventArgs.yes_bid = yesBidProp.GetInt32();
                if (msg.TryGetProperty("yes_ask", out var yesAskProp))
                    eventArgs.yes_ask = yesAskProp.GetInt32();
                if (msg.TryGetProperty("volume", out var volumeProp))
                    eventArgs.volume = volumeProp.GetInt32();
                if (msg.TryGetProperty("open_interest", out var openInterestProp))
                    eventArgs.open_interest = openInterestProp.GetInt32();
                if (msg.TryGetProperty("dollar_volume", out var dollarVolumeProp))
                    eventArgs.dollar_volume = dollarVolumeProp.GetInt32();
                if (msg.TryGetProperty("dollar_open_interest", out var dollarOpenInterestProp))
                    eventArgs.dollar_open_interest = dollarOpenInterestProp.GetInt32();
                if (msg.TryGetProperty("ts", out var tsProp))
                    eventArgs.ts = tsProp.GetInt64();

                eventArgs.LoggedDate = DateTime.UtcNow;

                _logger.LogInformation("Ticker data extracted - Market: {Market}, Bid: {Bid}, Ask: {Ask}, Price: {Price}",
                    eventArgs.market_ticker, eventArgs.yes_bid, eventArgs.yes_ask, eventArgs.price);

                TickerReceived?.Invoke(this, eventArgs);

                if (_isDataPersistenceEnabled)
                {
                    _logger.LogDebug("Persisting ticker data");
                    await _sqlDataService.StoreTickerAsync(data);
                }
                else
                {
                    _logger.LogDebug("Skipping data persistence for ticker: DataPersistence is disabled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ticker update message");
            }
        }

        /// <summary>
        /// Processes trade execution messages containing details of completed market transactions.
        /// Triggers events for subscribers and optionally persists trade data for analysis.
        /// </summary>
        /// <param name="data">The JSON data containing the trade message.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        private async Task ProcessTradeUpdateAsync(JsonElement data)
        {
            _logger.LogDebug("Processing trade update, DataPersistence: {IsEnabled}", _isDataPersistenceEnabled);
            _messageTypeCounts.AddOrUpdate("Trade", 1, (_, count) => count + 1);

            try
            {
                var eventArgs = new TradeEventArgs(data);
                TradeReceived?.Invoke(this, eventArgs);

                if (_isDataPersistenceEnabled)
                {
                    _logger.LogDebug("Persisting trade data");
                    await _sqlDataService.StoreTradeAsync(data);
                }
                else
                {
                    _logger.LogDebug("Skipping data persistence for trade: DataPersistence is disabled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing trade update message");
            }
        }

        /// <summary>
        /// Processes fill messages indicating order execution and position updates.
        /// Triggers events for subscribers and optionally persists fill data for tracking.
        /// </summary>
        /// <param name="data">The JSON data containing the fill message.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        private async Task ProcessFillUpdateAsync(JsonElement data)
        {
            _logger.LogDebug("Processing fill update, DataPersistence: {IsEnabled}", _isDataPersistenceEnabled);
            _messageTypeCounts.AddOrUpdate("Fill", 1, (_, count) => count + 1);

            try
            {
                var eventArgs = new FillEventArgs(data);
                FillReceived?.Invoke(this, eventArgs);

                if (_isDataPersistenceEnabled)
                {
                    _logger.LogDebug("Persisting fill data");
                    await _sqlDataService.StoreFillAsync(data);
                }
                else
                {
                    _logger.LogDebug("Skipping data persistence for fill: DataPersistence is disabled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing fill update message");
            }
        }

        /// <summary>
        /// Processes market lifecycle messages indicating changes in market status or availability.
        /// Triggers events for subscribers, optionally persists data, and refreshes market information from API.
        /// </summary>
        /// <param name="data">The JSON data containing the market lifecycle message.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        private async Task ProcessMarketLifecycleUpdateAsync(JsonElement data)
        {
            _logger.LogDebug("Processing market lifecycle update, DataPersistence: {IsEnabled}", _isDataPersistenceEnabled);
            _messageTypeCounts.AddOrUpdate("MarketLifecycle", 1, (_, count) => count + 1);

            try
            {
                var eventArgs = new MarketLifecycleEventArgs(data);
                MarketLifecycleReceived?.Invoke(this, eventArgs);

                if (_isDataPersistenceEnabled)
                {
                    _logger.LogDebug("Persisting market lifecycle data");
                    await _sqlDataService.StoreMarketLifecycleAsync(data);
                }
                else
                {
                    _logger.LogDebug("Skipping data persistence for market lifecycle: DataPersistence is disabled");
                }

                // Trigger API refresh for the affected market, but throttle to prevent rate limiting
                if (data.TryGetProperty("msg", out var msg) && msg.TryGetProperty("market_ticker", out var tickerProp))
                {
                    var marketTicker = tickerProp.GetString();
                    if (!string.IsNullOrEmpty(marketTicker))
                    {
                        _logger.LogDebug("Triggering API refresh for market {MarketTicker} due to lifecycle event, ApiCallTime={ApiCallTime}",
                            marketTicker, DateTime.UtcNow);
                        await ProcessLifecycleApiCallAsync(marketTicker);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing market lifecycle update message");
            }
        }

        /// <summary>
        /// Processes event lifecycle messages indicating changes in event status or market groupings.
        /// Triggers events for subscribers, optionally persists data, and refreshes event or market information from API.
        /// </summary>
        /// <param name="data">The JSON data containing the event lifecycle message.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        private async Task ProcessEventLifecycleUpdateAsync(JsonElement data)
        {
            _logger.LogDebug("Processing event lifecycle update, DataPersistence: {IsEnabled}", _isDataPersistenceEnabled);
            _messageTypeCounts.AddOrUpdate("EventLifecycle", 1, (_, count) => count + 1);

            try
            {
                var eventArgs = new EventLifecycleEventArgs(data);
                EventLifecycleReceived?.Invoke(this, eventArgs);

                if (_isDataPersistenceEnabled)
                {
                    _logger.LogDebug("Persisting event lifecycle data");
                    await _sqlDataService.StoreEventLifecycleAsync(data);
                }
                else
                {
                    _logger.LogDebug("Skipping data persistence for event lifecycle: DataPersistence is disabled");
                }

                // Trigger API refresh if market_ticker or event_ticker is available, but throttle to prevent rate limiting
                if (data.TryGetProperty("msg", out var msg))
                {
                    // First try market_ticker
                    if (msg.TryGetProperty("market_ticker", out var marketTickerProp))
                    {
                        var marketTicker = marketTickerProp.GetString();
                        if (!string.IsNullOrEmpty(marketTicker))
                        {
                            _logger.LogDebug("Triggering API refresh for market {MarketTicker} due to event lifecycle, ApiCallTime={ApiCallTime}",
                                marketTicker, DateTime.UtcNow);
                            await ProcessLifecycleApiCallAsync(marketTicker);
                        }
                    }
                    // Then try event_ticker
                    else if (msg.TryGetProperty("event_ticker", out var eventTickerProp))
                    {
                        var eventTicker = eventTickerProp.GetString();
                        if (!string.IsNullOrEmpty(eventTicker))
                        {
                            _logger.LogDebug("Triggering API refresh for event {EventTicker} due to event lifecycle, ApiCallTime={ApiCallTime}",
                                eventTicker, DateTime.UtcNow);
                            // For events, we fetch the event data directly (not through the throttled market API)
                            var eventResponse = await _kalshiAPIService.FetchEventAsync(eventTicker);
                            _logger.LogDebug("Fetched API data for event {EventTicker} due to event lifecycle: {Success}",
                                eventTicker, eventResponse != null ? "success" : "failed");
                        }
                    }
                    else
                    {
                        _logger.LogDebug("Event lifecycle message does not contain market_ticker or event_ticker, skipping API refresh");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event lifecycle update message");
            }
        }

        /// <summary>
        /// Processes subscription confirmation messages indicating successful channel subscriptions.
        /// Updates subscription state in the subscription manager and removes from pending confirmations.
        /// </summary>
        /// <param name="data">The JSON data containing the subscription confirmation message.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        private async Task ProcessSubscriptionConfirmationAsync(JsonElement data)
        {
            _logger.LogDebug("Processing subscription confirmation");
            _messageTypeCounts.AddOrUpdate("Subscribe", 1, (_, count) => count + 1);

            try
            {
                var msg = data.GetProperty("msg");
                if (msg.TryGetProperty("sid", out var sidProp))
                {
                    var sid = sidProp.GetInt32();
                    var channel = "";
                    var marketInfo = "";

                    // Extract channel information
                    if (msg.TryGetProperty("channel", out var channelProp))
                    {
                        channel = channelProp.GetString() ?? "";
                    }

                    // Try to extract market information from the message
                    if (msg.TryGetProperty("market_ticker", out var marketProp))
                    {
                        marketInfo = $" for market {marketProp.GetString()}";
                    }
                    else if (msg.TryGetProperty("event_ticker", out var eventProp))
                    {
                        marketInfo = $" for event {eventProp.GetString()}";
                    }

                    _logger.LogInformation("Subscription confirmed with SID: {Sid} for channel '{Channel}'{MarketInfo} | Source: {Source}", sid, channel, marketInfo, "MessageProcessor");

                    // Update subscription state in subscription manager
                    if (!string.IsNullOrEmpty(channel))
                    {
                        await _subscriptionManager.UpdateSubscriptionStateFromConfirmationAsync(sid, channel);

                        // Note: Healthy events are only raised by SubscriptionManager when recovering from unhealthy state
                        // Initial subscription confirmations do not trigger healthy events
                    }
                    else
                    {
                        _logger.LogWarning("Subscription confirmation message missing channel property");
                    }

                    // Remove from pending confirmations if this ID was pending
                    if (data.TryGetProperty("id", out var idProp))
                    {
                        var id = idProp.GetInt32();
                        _subscriptionManager.RemovePendingConfirmation(id);
                    }
                    else
                    {
                        _logger.LogWarning("Subscription confirmation message missing id property");
                    }
                }
                else
                {
                    _logger.LogWarning("Subscription confirmation message missing sid property");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing subscription confirmation message");
            }
        }

        /// <summary>
        /// Processes unsubscription confirmation messages indicating successful channel unsubscriptions.
        /// Logs the confirmation and updates internal state as needed.
        /// </summary>
        /// <param name="data">The JSON data containing the unsubscription confirmation message.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        private async Task ProcessUnsubscriptionConfirmationAsync(JsonElement data)
        {
            _logger.LogDebug("Processing unsubscription confirmation");
            _messageTypeCounts.AddOrUpdate("Unsubscribe", 1, (_, count) => count + 1);

            try
            {
                if (data.TryGetProperty("sid", out var sidProp))
                {
                    var sid = sidProp.GetInt32();
                    var channel = "";
                    var marketInfo = "";

                    // Try to extract channel information from the message
                    if (data.TryGetProperty("msg", out var msg) && msg.TryGetProperty("channel", out var channelProp))
                    {
                        channel = channelProp.GetString() ?? "";
                    }

                    // Try to extract market information
                    if (data.TryGetProperty("msg", out msg))
                    {
                        if (msg.TryGetProperty("market_ticker", out var marketProp))
                        {
                            marketInfo = $" for market {marketProp.GetString()}";
                        }
                        else if (msg.TryGetProperty("event_ticker", out var eventProp))
                        {
                            marketInfo = $" for event {eventProp.GetString()}";
                        }
                    }

                    _logger.LogInformation("Unsubscription confirmed for SID: {Sid} from channel '{Channel}'{MarketInfo} | Source: {Source}", sid, channel, marketInfo, "MessageProcessor");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing unsubscription confirmation message");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Processes general confirmation messages for various operations (updates, subscriptions).
        /// Handles both subscription confirmations with SIDs and general operation confirmations.
        /// Enqueues ok messages with seq for ordered processing.
        /// </summary>
        /// <param name="data">The JSON data containing the confirmation message.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        private async Task ProcessOkConfirmationAsync(JsonElement data)
        {
            _logger.LogDebug("Processing confirmation message");
            _messageTypeCounts.AddOrUpdate("Ok", 1, (_, count) => count + 1);

            try
            {
                // Handle update confirmations and possibly subscribe confirmations
                if (data.TryGetProperty("id", out var idProp))
                {
                    var id = idProp.GetInt32();
                    var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    _logger.LogInformation("[{Timestamp}] RECEIVED confirmation for ID: {Id}", timestamp, id);

                    // Check if this is a subscribe confirmation with sid
                    if (data.TryGetProperty("sid", out var sidProp))
                    {
                        var sid = sidProp.GetInt32();
                        var channel = "";
                        var marketInfo = "";

                        // Get channel from pending confirmation since it's not in the message
                        var pending = _subscriptionManager.GetPendingConfirm(id);
                        if (pending.HasValue)
                        {
                            channel = pending.Value.Channel;

                            // Try to extract market information from the pending confirmation data
                            if (pending.Value.MarketTickers != null && pending.Value.MarketTickers.Length > 0)
                            {
                                var markets = string.Join(", ", pending.Value.MarketTickers);
                                marketInfo = $" for markets: {markets}";
                            }

                            await _subscriptionManager.UpdateSubscriptionStateFromConfirmationAsync(sid, channel);
                        }
                        else
                        {
                            _logger.LogWarning("No pending confirmation found for subscribe confirmation ID: {Id}", id);
                        }

                        _logger.LogInformation("[{Timestamp}] RECEIVED subscribe confirmed with SID: {Sid} for ID: {Id} on channel '{Channel}'{MarketInfo} | Source: {Source}", timestamp, sid, id, channel, marketInfo, "MessageProcessor");

                        // Note: Healthy events are only raised by SubscriptionManager when recovering from unhealthy state
                        // Initial subscription confirmations do not trigger healthy events
                    }
                    else
                    {
                        _logger.LogInformation("Update confirmed for ID: {Id}", id);
                    }

                    // Remove from pending confirmations
                    _subscriptionManager.RemovePendingConfirmation(id);
                }

                // Enqueue ALL ok messages with sid and seq for ordered processing
                if (data.TryGetProperty("sid", out var okSidProp) && data.TryGetProperty("seq", out var okSeqProp))
                {
                    var okSid = okSidProp.GetInt32();
                    var okSeq = okSeqProp.GetInt64();
                    _subscriptionManager.EnqueueOkMessage(okSid, data, okSeq);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing confirmation message");
            }
        }

        /// <summary>
        /// Processes error messages from the WebSocket connection, extracting error codes and messages.
        /// Handles specific error conditions like "Already subscribed" and logs appropriate warnings.
        /// </summary>
        /// <param name="data">The JSON data containing the error message.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        private async Task ProcessErrorMessageAsync(JsonElement data)
        {
            _logger.LogInformation("Processing error message");
            _messageTypeCounts.AddOrUpdate("Error", 1, (_, count) => count + 1);

            try
            {
                if (data.TryGetProperty("msg", out var msgProp))
                {
                    string errorMsg = "Unknown error";
                    int errorCode = -1;

                    // Handle different types of error messages
                    if (msgProp.ValueKind == JsonValueKind.String)
                    {
                        errorMsg = msgProp.GetString() ?? "Unknown error";
                    }
                    else if (msgProp.ValueKind == JsonValueKind.Object)
                    {
                        // Extract error code and message
                        if (msgProp.TryGetProperty("code", out var codeProp))
                        {
                            errorCode = codeProp.GetInt32();
                        }
                        if (msgProp.TryGetProperty("msg", out var msgTextProp))
                        {
                            errorMsg = msgTextProp.GetString() ?? "Unknown error";
                        }
                        else
                        {
                            errorMsg = $"Error object: {msgProp.ToString()}";
                        }
                    }
                    else
                    {
                        // For other types, convert to string representation
                        errorMsg = $"Error value: {msgProp.ToString()}";
                    }

                    _logger.LogWarning("WebSocket error received: Code={Code}, Message={ErrorMessage}", errorCode, errorMsg);

                    // Handle specific error codes
                    if (errorCode == 6 && errorMsg.Contains("Already subscribed"))
                    {
                        // Rate limit "Already subscribed" error logging
                        _alreadySubscribedErrorCount++;
                        _alreadySubscribedErrorsInWindow++;
                        CheckAlreadySubscribedWarnings();

                        // If we get "Already subscribed", treat it as successful subscription
                        // Try to extract the ID and update subscription state
                        if (data.TryGetProperty("id", out var idProp))
                        {
                            var id = idProp.GetInt32();
                            var channel = "";
                            var marketInfo = "";

                            // Try to get channel information from pending confirmation
                            var pending = _subscriptionManager.GetPendingConfirm(id);
                            if (pending.HasValue)
                            {
                                channel = pending.Value.Channel;
                                if (pending.Value.MarketTickers != null && pending.Value.MarketTickers.Length > 0)
                                {
                                    var markets = string.Join(", ", pending.Value.MarketTickers);
                                    marketInfo = $" for markets: {markets}";
                                }

                                // Get the current SID for this channel
                                var currentSid = _subscriptionManager.GetChannelSid(channel);
                                if (currentSid > 0)
                                {
                                    // Update subscription state to include the pending markets
                                    await _subscriptionManager.UpdateSubscriptionStateFromConfirmationAsync(currentSid, channel);
                                    _logger.LogDebug("Updated subscription state for 'Already subscribed' on channel '{Channel}' with SID {Sid}{MarketInfo}", channel, currentSid, marketInfo);
                                }
                                else
                                {
                                    _logger.LogWarning("No existing SID found for channel '{Channel}' when handling 'Already subscribed' error", channel);
                                }
                            }

                            _logger.LogDebug("Received 'Already subscribed' for ID {Id} on channel '{Channel}'{MarketInfo}, treating as successful subscription | Source: {Source}", id, channel, marketInfo, "MessageProcessor");

                            // Remove from pending confirmations since it's already subscribed
                            _subscriptionManager.RemovePendingConfirmation(id);
                        }
                    }
                }
                else
                {
                    _logger.LogError("WebSocket error received with no message");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing error message");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Processes messages in batches for high-volume scenarios to reduce event overhead.
        /// Runs continuously when message batching is enabled.
        /// </summary>
        /// <returns>A task representing the asynchronous batch processing operation.</returns>
        private async Task ProcessMessageBatchAsync()
        {
            _logger.LogInformation("Message batch processing task started");

            try
            {
                while (!_batchCancellationSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(_config.BatchProcessingIntervalMs, _batchCancellationSource.Token);

                    if (_messageBatchQueue.IsEmpty)
                        continue;

                    var messagesToProcess = new List<string>();

                    // Collect batch of messages
                    while (messagesToProcess.Count < _config.MaxBatchSize &&
                           _messageBatchQueue.TryDequeue(out var message))
                    {
                        messagesToProcess.Add(message);
                    }

                    if (messagesToProcess.Count > 0)
                    {
                        await _batchProcessingSemaphore.WaitAsync(_batchCancellationSource.Token);

                        try
                        {
                            // Process messages in parallel for better performance
                            var processingTasks = messagesToProcess.Select(msg => ProcessMessageAsync(msg)).ToArray();
                            await Task.WhenAll(processingTasks);

                            _logger.LogDebug("Processed batch of {Count} messages", messagesToProcess.Count);
                        }
                        finally
                        {
                            _batchProcessingSemaphore.Release();
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Message batch processing task cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in message batch processing task");
            }
            finally
            {
                _logger.LogInformation("Message batch processing task completed");
            }
        }

        /// <summary>
        /// Logs performance metrics for message processing rates and queue depths.
        /// Called periodically when performance metrics are enabled.
        /// </summary>
        private void LogPerformanceMetrics()
        {
            lock (_metricsLock)
            {
                var now = DateTime.UtcNow;
                var timeSinceLastLog = now - _lastMetricsLogTime;

                if (timeSinceLastLog.TotalMilliseconds >= _config.PerformanceMetricsLogIntervalMs)
                {
                    var messagesPerSecond = _totalMessagesProcessed / timeSinceLastLog.TotalSeconds;
                    var avgProcessingTime = _totalMessagesProcessed > 0 ? _totalProcessingTimeMs / _totalMessagesProcessed : 0;

                    if (_enablePerformanceMonitoring)
                    {
                        _logger.LogInformation("Performance Metrics - Messages/sec: {MessagesPerSecond:F2}, Avg Processing Time: {AvgProcessingTime}ms, Queue Depth: {QueueDepth}, Total Processed: {TotalProcessed}",
                            messagesPerSecond, avgProcessingTime, _orderBookUpdateQueue.Count, _totalMessagesProcessed);
                    }

                    // Post metrics to central monitoring services
                    if (!_enablePerformanceMonitoring)
                    {
                        _performanceMonitor.RecordDisabledMetric("MessageProcessor", "TotalMessagesProcessed", "Total Messages Processed", "Total number of messages processed", _totalMessagesProcessed, "count", "MessageProcessing");
                        _performanceMonitor.RecordDisabledMetric("MessageProcessor", "AverageProcessingTime", "Average Processing Time", "Average time to process a message", avgProcessingTime, "ms", "MessageProcessing");
                        _performanceMonitor.RecordDisabledMetric("MessageProcessor", "MessagesPerSecond", "Messages Per Second", "Rate of message processing", messagesPerSecond, "msg/s", "MessageProcessing");
                        _performanceMonitor.RecordDisabledMetric("MessageProcessor", "OrderBookQueueDepth", "Order Book Queue Depth", "Current depth of order book update queue", _orderBookUpdateQueue.Count, "count", "MessageProcessing");
                    }
                    else
                    {
                        _performanceMonitor.RecordCounterMetric("MessageProcessor", "TotalMessagesProcessed", "Total Messages Processed", "Total number of messages processed", _totalMessagesProcessed, "count", "MessageProcessing");
                        _performanceMonitor.RecordSpeedDialMetric("MessageProcessor", "AverageProcessingTime", "Average Processing Time", "Average time to process a message", avgProcessingTime, "ms", "MessageProcessing", null, null, null);
                        _performanceMonitor.RecordSpeedDialMetric("MessageProcessor", "MessagesPerSecond", "Messages Per Second", "Rate of message processing", messagesPerSecond, "msg/s", "MessageProcessing", null, null, null);
                        _performanceMonitor.RecordCounterMetric("MessageProcessor", "OrderBookQueueDepth", "Order Book Queue Depth", "Current depth of order book update queue", _orderBookUpdateQueue.Count, "count", "MessageProcessing");
                    }

                    // Post message type distribution metrics
                    var messageTypeCounts = GetMessageTypeCounts();
                    foreach (var kvp in messageTypeCounts)
                    {
                        if (!_enablePerformanceMonitoring)
                        {
                            _performanceMonitor.RecordDisabledMetric("MessageProcessor", $"MessageType_{kvp.Key}", $"Message Type {kvp.Key}", $"Count of {kvp.Key} messages", kvp.Value, "count", "MessageTypes");
                        }
                        else
                        {
                            _performanceMonitor.RecordCounterMetric("MessageProcessor", $"MessageType_{kvp.Key}", $"Message Type {kvp.Key}", $"Count of {kvp.Key} messages", kvp.Value, "count", "MessageTypes");
                        }
                    }

                    // Reset counters for next interval
                    _totalMessagesProcessed = 0;
                    _totalProcessingTimeMs = 0;
                    _lastMetricsLogTime = now;
                }
            }
        }

        /// <summary>
        /// Attempts to process a lifecycle API call immediately or queues it for later processing.
        /// Resets the counter every second and allows up to MaxLifecycleEventsPerSecond events.
        /// </summary>
        /// <param name="marketTicker">The market ticker for the API call.</param>
        /// <returns>A task that completes when the API call is processed (either immediately or after queuing).</returns>
        private Task ProcessLifecycleApiCallAsync(string marketTicker)
        {
            var tcs = new TaskCompletionSource<bool>();
            var queuedItem = (MarketTicker: marketTicker, Tcs: tcs, QueuedTime: DateTime.UtcNow);

            lock (_lifecycleRateLimiterLock)
            {
                var now = DateTime.UtcNow;
                if ((now - _lifecycleRateLimiterLastReset).TotalSeconds >= 1)
                {
                    _lifecycleRateLimiterCounter = 0;
                    _lifecycleRateLimiterLastReset = now;
                }

                if (_lifecycleRateLimiterCounter < _apiConfig.MaxLifecycleEventsPerSecond)
                {
                    _lifecycleRateLimiterCounter++;
                    // Process immediately
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var (processedCount, errorCount) = await _kalshiAPIService.FetchMarketsAsync(tickers: new[] { marketTicker });
                            _logger.LogDebug("Processed immediate API call for market {MarketTicker}: {ProcessedCount} processed, {ErrorCount} errors",
                                marketTicker, processedCount, errorCount);
                            tcs.SetResult(true);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing immediate API call for market {MarketTicker}", marketTicker);
                            tcs.SetException(ex);
                        }
                    });
                }
                else
                {
                    // Queue for later processing
                    _delayedApiCallQueue.Enqueue(queuedItem);
                    _logger.LogDebug("Queued API call for market {MarketTicker} due to rate limiting, QueueDepth={QueueDepth}",
                        marketTicker, _delayedApiCallQueue.Count);
                }
            }

            return tcs.Task;
        }

        /// <summary>
        /// Processes queued API calls when rate limiting allows.
        /// Runs continuously in the background.
        /// </summary>
        private async Task ProcessDelayedApiCallsAsync()
        {
            _logger.LogInformation("Delayed API call processing task started");

            try
            {
                while (!_delayedProcessingCancellation.Token.IsCancellationRequested)
                {
                    await Task.Delay(100, _delayedProcessingCancellation.Token); // Check every 100ms

                    if (_delayedApiCallQueue.IsEmpty)
                        continue;

                    lock (_lifecycleRateLimiterLock)
                    {
                        var now = DateTime.UtcNow;
                        if ((now - _lifecycleRateLimiterLastReset).TotalSeconds >= 1)
                        {
                            _lifecycleRateLimiterCounter = 0;
                            _lifecycleRateLimiterLastReset = now;
                        }

                        if (_lifecycleRateLimiterCounter < _apiConfig.MaxLifecycleEventsPerSecond && _delayedApiCallQueue.TryDequeue(out var item))
                        {
                            _lifecycleRateLimiterCounter++;
                            var queueTime = now - item.QueuedTime;
                            var queueTimeMs = (long)queueTime.TotalMilliseconds;

                            // Track metrics
                            lock (_delayedMetricsLock)
                            {
                                _totalDelayedApiCalls++;
                                _totalWaitTimeMs += queueTimeMs;
                                if (queueTimeMs > _maxWaitTimeMs)
                                {
                                    _maxWaitTimeMs = queueTimeMs;
                                }
                            }

                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    var (processedCount, errorCount) = await _kalshiAPIService.FetchMarketsAsync(tickers: new[] { item.MarketTicker });
                                    _logger.LogDebug("Processed delayed API call for market {MarketTicker} (queued {QueueTimeMs}ms): {ProcessedCount} processed, {ErrorCount} errors",
                                        item.MarketTicker, queueTimeMs, processedCount, errorCount);
                                    item.Tcs.SetResult(true);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error processing delayed API call for market {MarketTicker}", item.MarketTicker);
                                    item.Tcs.SetException(ex);
                                }
                            });

                            // Send metrics periodically (every 30 seconds)
                            if ((now - _lastDelayedMetricsTime).TotalSeconds >= 30)
                            {
                                lock (_delayedMetricsLock)
                                {
                                    var totalCalls = _totalDelayedApiCalls;
                                    var totalWait = _totalWaitTimeMs;
                                    var maxWait = _maxWaitTimeMs;
                                    var avgWait = totalCalls > 0 ? (double)totalWait / totalCalls : 0;

                                    RecordDelayedApiCallMetricsPrivate(
                                        totalCalls,
                                        avgWait,
                                        maxWait,
                                        _delayedApiCallQueue.Count,
                                        _enablePerformanceMonitoring);

                                    // Reset metrics for next period
                                    _totalDelayedApiCalls = 0;
                                    _totalWaitTimeMs = 0;
                                    _maxWaitTimeMs = 0;
                                    _lastDelayedMetricsTime = now;
                                }
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Delayed API call processing task cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in delayed API call processing task");
            }
            finally
            {
                _logger.LogInformation("Delayed API call processing task completed");
            }
        }

        /// <summary>
        /// Records delayed API call performance metrics using the IPerformanceMonitor interface.
        /// </summary>
        /// <param name="totalCalls">Total number of delayed API calls processed</param>
        /// <param name="avgWait">Average wait time in milliseconds</param>
        /// <param name="maxWait">Maximum wait time in milliseconds</param>
        /// <param name="queueCount">Current queue count</param>
        /// <param name="enablePerformanceMonitoring">Whether performance monitoring is enabled</param>
        private void RecordDelayedApiCallMetricsPrivate(long totalCalls, double avgWait, long maxWait, int queueCount, bool enablePerformanceMonitoring)
        {
            string className = "MessageProcessor";
            string category = "DelayedApiCalls";

            if (!enablePerformanceMonitoring)
            {
                // Send disabled metrics
                _performanceMonitor.RecordDisabledMetric(className, "TotalDelayedApiCalls", "Total Delayed API Calls", "Number of delayed API calls processed", totalCalls, "count", category);
                _performanceMonitor.RecordDisabledMetric(className, "AverageWaitTime", "Average Wait Time", "Average wait time for delayed API calls", avgWait, "ms", category);
                _performanceMonitor.RecordDisabledMetric(className, "MaxWaitTime", "Max Wait Time", "Maximum wait time for delayed API calls", maxWait, "ms", category);
                _performanceMonitor.RecordDisabledMetric(className, "QueueCount", "Queue Count", "Current number of queued delayed API calls", queueCount, "count", category);
            }
            else
            {
                // Record actual metrics
                _performanceMonitor.RecordCounterMetric(className, "TotalDelayedApiCalls", "Total Delayed API Calls", "Number of delayed API calls processed", totalCalls, "count", category);
                _performanceMonitor.RecordSpeedDialMetric(className, "AverageWaitTime", "Average Wait Time", "Average wait time for delayed API calls", avgWait, "ms", category, null, null, null);
                _performanceMonitor.RecordSpeedDialMetric(className, "MaxWaitTime", "Max Wait Time", "Maximum wait time for delayed API calls", maxWait, "ms", category, null, null, null);
                _performanceMonitor.RecordCounterMetric(className, "QueueCount", "Queue Count", "Current number of queued delayed API calls", queueCount, "count", category);
            }
        }

        /// <summary>
        /// Checks and logs warnings for duplicate message detection.
        /// Tracks duplicate messages within a time window and warns if threshold is exceeded.
        /// </summary>
        private void CheckDuplicateMessageWarnings()
        {
            var now = DateTime.UtcNow;
            var timeSinceLastWarning = now - _lastDuplicateWarningTime;

            if (timeSinceLastWarning.TotalSeconds >= 10 && _duplicateMessageCount > 0)
            {
                if (_enablePerformanceMonitoring)
                {
                    _logger.LogWarning("DUP-Duplicate messages detected: {DuplicateCount} total duplicates. Any duplicate is unacceptable and should not occur.",
                        _duplicateMessageCount);
                }

                // Post duplicate metrics to central monitoring
                if (!_enablePerformanceMonitoring)
                {
                    _performanceMonitor.RecordDisabledMetric("MessageProcessor", "TotalDuplicateMessages", "Total Duplicate Messages", "Total number of duplicate messages detected", _duplicateMessageCount, "count", "DuplicateMessages");
                    _performanceMonitor.RecordDisabledMetric("MessageProcessor", "DuplicateMessagesInWindow", "Duplicate Messages In Window", "Number of duplicate messages in current time window", _duplicateMessagesInWindow, "count", "DuplicateMessages");
                    _performanceMonitor.RecordDisabledMetric("MessageProcessor", "LastDuplicateWarningTime", "Last Duplicate Warning Time", "Timestamp of last duplicate message warning", _lastDuplicateWarningTime.Ticks, "ticks", "DuplicateMessages");
                }
                else
                {
                    _performanceMonitor.RecordCounterMetric("MessageProcessor", "TotalDuplicateMessages", "Total Duplicate Messages", "Total number of duplicate messages detected", _duplicateMessageCount, "count", "DuplicateMessages");
                    _performanceMonitor.RecordCounterMetric("MessageProcessor", "DuplicateMessagesInWindow", "Duplicate Messages In Window", "Number of duplicate messages in current time window", _duplicateMessagesInWindow, "count", "DuplicateMessages");
                    _performanceMonitor.RecordNumericDisplayMetric("MessageProcessor", "LastDuplicateWarningTime", "Last Duplicate Warning Time", "Timestamp of last duplicate message warning", _lastDuplicateWarningTime.Ticks, "ticks", "DuplicateMessages");
                }

                _lastDuplicateWarningTime = now;
            }
        }

        /// <summary>
        /// Checks and logs warnings for "Already subscribed" error message detection.
        /// Tracks these errors within a time window and warns periodically to reduce log spam.
        /// </summary>
        private void CheckAlreadySubscribedWarnings()
        {
            var now = DateTime.UtcNow;
            var timeSinceLastWarning = now - _lastAlreadySubscribedWarningTime;

            if (timeSinceLastWarning.TotalSeconds >= 30 && _alreadySubscribedErrorCount > 0)
            {
                _logger.LogWarning("Already subscribed errors detected: {ErrorCount} total 'Already subscribed' errors in the last {TimeWindow}s. This indicates redundant subscription attempts.",
                    _alreadySubscribedErrorCount, timeSinceLastWarning.TotalSeconds);

                // Post metrics to central monitoring
                if (!_enablePerformanceMonitoring)
                {
                    _performanceMonitor.RecordDisabledMetric("MessageProcessor", "TotalAlreadySubscribedErrors", "Total Already Subscribed Errors", "Total number of 'Already subscribed' errors detected", _alreadySubscribedErrorCount, "count", "SubscriptionErrors");
                    _performanceMonitor.RecordDisabledMetric("MessageProcessor", "AlreadySubscribedErrorsInWindow", "Already Subscribed Errors In Window", "Number of 'Already subscribed' errors in current time window", _alreadySubscribedErrorsInWindow, "count", "SubscriptionErrors");
                }
                else
                {
                    _performanceMonitor.RecordCounterMetric("MessageProcessor", "TotalAlreadySubscribedErrors", "Total Already Subscribed Errors", "Total number of 'Already subscribed' errors detected", _alreadySubscribedErrorCount, "count", "SubscriptionErrors");
                    _performanceMonitor.RecordCounterMetric("MessageProcessor", "AlreadySubscribedErrorsInWindow", "Already Subscribed Errors In Window", "Number of 'Already subscribed' errors in current time window", _alreadySubscribedErrorsInWindow, "count", "SubscriptionErrors");
                }

                // Reset window counters
                _alreadySubscribedErrorsInWindow = 0;
                _lastAlreadySubscribedWarningTime = now;
            }
        }

    }
}
