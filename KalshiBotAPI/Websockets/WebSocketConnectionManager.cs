using BacklashBot.Management.Interfaces;
using BacklashDTOs.Exceptions;
using BacklashInterfaces.PerformanceMetrics;
using KalshiBotAPI.Configuration;
using KalshiBotAPI.WebSockets.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;

namespace KalshiBotAPI.Websockets
{
    /// <summary>
    /// Manages WebSocket connections to Kalshi's trading platform with robust reconnection logic,
    /// authentication, and thread-safe operations. This class handles the low-level WebSocket
    /// communication layer, providing reliable connection management for real-time market data streaming.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The WebSocketConnectionManager is responsible for:
    /// - Establishing authenticated WebSocket connections using RSA-based signatures
    /// - Managing connection lifecycle with automatic reconnection capabilities
    /// - Providing thread-safe message sending and receiving operations
    /// - Handling connection state monitoring and error recovery
    /// - Coordinating with higher-level components for message processing
    /// </para>
    /// <para>
    /// This class implements the IWebSocketConnectionManager interface and is designed to be used
    /// by higher-level orchestration components like KalshiWebSocketClient. It provides the
    /// foundation for reliable real-time communication with Kalshi's trading platform.
    /// </para>
    /// <para>
    /// Key features include:
    /// - Configurable retry logic with customizable delays and maximum attempts
    /// - Performance metrics collection (connection success rates, latency, message throughput)
    /// - Signature caching for reduced computational overhead with hit rate tracking
    /// - Configurable WebSocket buffer sizes with utilization monitoring
    /// - Semaphore-based synchronization to prevent concurrent connection attempts
    /// - Configurable reconnection behavior (can be disabled/enabled)
    /// - Connection failure reason tracking and diagnostics
    /// - Sliding window throughput calculation for accurate real-time metrics
    /// - Comprehensive logging for operational visibility
    /// - Proper resource cleanup and disposal
    /// </para>
    /// </remarks>
    public class WebSocketConnectionManager : IWebSocketConnectionManager
    {
        private readonly ILogger<WebSocketConnectionManager> _logger;
        private readonly KalshiConfig _kalshiConfig;
        private readonly WebSocketConnectionManagerConfig _websocketConfig;
        private readonly RSA _privateKey;
        private readonly IPerformanceMonitor _performanceMonitor;
        private ClientWebSocket? _webSocket = null!;
        private readonly object _webSocketLock = new object();
        private bool _isConnected = false;
        private readonly SemaphoreSlim _connectSemaphore = new SemaphoreSlim(1, 1);
        private bool _allowReconnect = true;

        // Performance metrics
        private int _connectionAttempts = 0;
        private int _connectionSuccesses = 0;
        private int _reconnectionCount = 0;
        private readonly List<long> _connectionLatencies = new List<long>();
        private int _messagesReceived = 0;
        private DateTime _lastMessageTime = DateTime.UtcNow;
        private double _messageThroughput = 0; // messages per second
        private readonly Queue<(DateTime timestamp, int count)> _throughputWindow = new Queue<(DateTime, int)>();
        private int _connectionFailures = 0;
        private readonly Dictionary<string, int> _connectionFailureReasons = new Dictionary<string, int>();
        private int _signatureCacheHits = 0;
        private int _signatureCacheMisses = 0;
        private long _totalBytesReceived = 0;
        private int _bufferUtilizationCount = 0;
        private double _averageBufferUtilization = 0;

        // Additional network-level metrics
        private long _totalConnectedTimeMs = 0;
        private DateTime _lastConnectionStart = DateTime.MinValue;
        private double _bandwidthBps = 0; // bytes per second
        private readonly Queue<long> _bandwidthWindow = new Queue<long>(); // for bandwidth calculation
        private int _messageErrors = 0;

        // Operational metrics
        private double _errorRate = 0; // messages with errors / total messages
        private int _queueDepth = 0; // current queue depth if implemented

        // Performance monitoring
        private DateTime _lastMetricsPost = DateTime.MinValue;
        private readonly TimeSpan _metricsPostInterval = TimeSpan.FromMinutes(1); // Post metrics every minute

        // Configuration options
        private readonly int _maxRetryAttempts;
        private readonly int[] _retryDelays;
        private readonly int _bufferSize;
        private readonly int _resetDelayMs;
        private readonly int _semaphoreTimeoutMs;

        // Signature caching
        private readonly ConcurrentDictionary<string, (string timestamp, string signature, DateTime expiry)> _signatureCache = new ConcurrentDictionary<string, (string, string, DateTime)>();
        private readonly TimeSpan _signatureCacheDuration;

        /// <summary>
        /// Gets or sets whether performance metrics collection is enabled.
        /// When disabled, metric tracking is skipped to reduce overhead.
        /// </summary>
        public bool EnableMetrics
        {
            get => _enableMetrics;
            set
            {
                if (_enableMetrics != value)
                {
                    _enableMetrics = value;
                    (_performanceMonitor as ICentralPerformanceMonitor)?.UpdateWebSocketMetricsRecordingStatus(_enableMetrics);
                    if (_enableMetrics && _performanceMonitor == null)
                    {
                        _logger.LogWarning("Performance metrics enabled but no ICentralPerformanceMonitor was provided. Metrics will be collected locally but not posted to central monitoring.");
                    }
                }
            }
        }
        private bool _enableMetrics = true;

        /// <summary>
        /// Initializes a new instance of the WebSocketConnectionManager class.
        /// </summary>
        /// <param name="kalshiConfig">Configuration options containing API credentials.</param>
        /// <param name="websocketConfig">Configuration options for WebSocket operations.</param>
        /// <param name="logger">Logger instance for recording connection operations and errors.</param>
        /// <param name="performanceMonitor">Optional performance monitor for recording WebSocket metrics.</param>
        /// <remarks>
        /// The constructor sets up the RSA private key for authentication by loading it from the
        /// configured key file. This key is used to generate signatures for WebSocket connection
        /// authentication with Kalshi's servers. It also initializes all configurable parameters
        /// including retry delays, buffer sizes, timeouts, and cache durations for optimal performance.
        /// </remarks>
        public WebSocketConnectionManager(
            IOptions<KalshiConfig> kalshiConfig,
            IOptions<WebSocketConnectionManagerConfig> websocketConfig,
            ILogger<WebSocketConnectionManager> logger,
            IPerformanceMonitor performanceMonitor)
        {
            _kalshiConfig = kalshiConfig.Value;
            _websocketConfig = websocketConfig.Value;
            _logger = logger;
            _performanceMonitor = performanceMonitor;

            _logger.LogInformation("WebSocketConnectionManager: Constructor called");

            _logger.LogInformation("WebSocketConnectionManager: Creating RSA key");
            _privateKey = RSA.Create();

            // Use the resolved key file path from configuration
            var keyFilePath = _kalshiConfig.KeyFile;
            _logger.LogInformation("WebSocketConnectionManager: Loading key file: {KeyFile}", keyFilePath);

            if (!File.Exists(keyFilePath))
            {
                _logger.LogError("WebSocketConnectionManager: Key file not found at {KeyFile}", keyFilePath);
                throw new FileNotFoundException($"Kalshi private key file not found at {keyFilePath}", keyFilePath);
            }

            _privateKey.ImportFromPem(File.ReadAllText(keyFilePath));
            _logger.LogInformation("WebSocketConnectionManager: Key file loaded successfully");

            // Warn if metrics are enabled but no performance monitor is provided
            if (EnableMetrics && _performanceMonitor == null)
            {
                _logger.LogWarning("Performance metrics are enabled but no ICentralPerformanceMonitor was provided. Metrics will be collected locally but not posted to central monitoring.");
            }

            // Initialize configuration values
            _maxRetryAttempts = _websocketConfig.MaxRetryAttempts;
            _retryDelays = _websocketConfig.RetryDelays ?? new int[] { 1000, 2000, 4000, 8000, 16000 };
            _bufferSize = _websocketConfig.BufferSize;
            _resetDelayMs = _websocketConfig.ResetDelayMs;
            _semaphoreTimeoutMs = _websocketConfig.SemaphoreTimeoutMs;
            _signatureCacheDuration = TimeSpan.FromMinutes(_websocketConfig.SignatureCacheDurationMinutes);

            // Initialize metrics configuration (defaults to true if not specified)
            EnableMetrics = _websocketConfig.EnablePerformanceMetrics;

            // Notify performance monitor of initial metrics status
            _performanceMonitor?.UpdateWebSocketMetricsRecordingStatus(EnableMetrics);
        }

        /// <summary>
        /// Establishes a WebSocket connection to Kalshi's trading platform with automatic retry logic.
        /// </summary>
        /// <param name="retryCount">The current retry attempt number (used internally for exponential backoff).</param>
        /// <returns>A task representing the asynchronous connection operation.</returns>
        /// <remarks>
        /// <para>
        /// This method handles the complete WebSocket connection process including:
        /// - Checking reconnection permissions and current connection state
        /// - Acquiring connection semaphore to prevent concurrent connection attempts
        /// - Disposing of stale connections and creating new WebSocket instances
        /// - Setting up authentication headers with RSA signature generation
        /// - Establishing the connection with proper error handling
        /// </para>
        /// <para>
        /// The method implements configurable retry logic for connection failures,
        /// with configurable retry delays and maximum attempts. If all retries fail, it throws an
        /// InvalidOperationException with details about the connection failure.
        /// </para>
        /// <para>
        /// Thread Safety: This method is thread-safe and uses semaphores to prevent
        /// concurrent connection attempts that could cause race conditions.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the WebSocket connection cannot be established after all retry attempts.
        /// </exception>
        public async Task ConnectAsync(int retryCount = 0)
        {
            if (!_allowReconnect && !_isConnected)
            {
                _logger.LogDebug("Reconnection disabled, skipping connect attempt");
                return;
            }

            _logger.LogInformation("Connecting WebSocket, retry attempt: {RetryCount}", retryCount);
            if (_enableMetrics) _connectionAttempts++;
            bool semaphoreAcquired = false;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                semaphoreAcquired = await _connectSemaphore.WaitAsync(_semaphoreTimeoutMs);
                if (!semaphoreAcquired)
                {
                    _logger.LogError("Failed to acquire connect semaphore within {Timeout}ms", _semaphoreTimeoutMs);
                    if (_enableMetrics) TrackConnectionFailure("SemaphoreTimeout");
                    return;
                }

                lock (_webSocketLock)
                {
                    if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                    {
                        _logger.LogDebug("WebSocket already open, skipping connection attempt");
                        return;
                    }
                    if (_webSocket != null)
                    {
                        _webSocket.Dispose();
                        _webSocket = null;
                        _isConnected = false;
                        _logger.LogInformation("Disposed existing WebSocket connection");
                    }
                }

                var uri = new Uri(_kalshiConfig.Environment == "demo"
                    ? "wss://demo-api.kalshi.co/trade-api/ws/v2"
                    : "wss://api.elections.kalshi.com/trade-api/ws/v2");

                var newWebSocket = new ClientWebSocket();
                newWebSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(10);
                var (timestamp, signature) = GenerateAuthHeaders("GET", "/trade-api/ws/v2");
                newWebSocket.Options.SetRequestHeader("KALSHI-ACCESS-KEY", _kalshiConfig.KeyId);
                newWebSocket.Options.SetRequestHeader("KALSHI-ACCESS-SIGNATURE", signature);
                newWebSocket.Options.SetRequestHeader("KALSHI-ACCESS-TIMESTAMP", timestamp);

                await newWebSocket.ConnectAsync(uri, CancellationToken.None);
                stopwatch.Stop();
                if (_enableMetrics)
                {
                    _connectionLatencies.Add(stopwatch.ElapsedMilliseconds);
                    _connectionSuccesses++;
                    if (retryCount > 0) _reconnectionCount++;
                    _lastConnectionStart = DateTime.UtcNow; // Start tracking uptime
                }
                PostPerformanceMetric("Connect", stopwatch.ElapsedMilliseconds);
                _logger.LogInformation("WebSocket connection established to {Uri}", uri);
                _isConnected = true;

                lock (_webSocketLock)
                {
                    _webSocket = newWebSocket;
                }
            }
            catch (WebSocketException ex) when (ex.Message.Contains("Failed to connect WebSocket on retry"))
            {
                _logger.LogWarning(new WebSocketRetryFailedException(ex.Message, ex), "Failed to connect to a websocket. Exception: {Message}, Inner: {Inner}", ex.Message, ex.InnerException?.Message ?? "None");
                if (_enableMetrics)
                {
                    TrackConnectionFailure("WebSocketException");
                    if (_isConnected)
                    {
                        _totalConnectedTimeMs += (long)(DateTime.UtcNow - _lastConnectionStart).TotalMilliseconds;
                        _isConnected = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect WebSocket on retry {RetryCount}", retryCount);
                if (_enableMetrics)
                {
                    TrackConnectionFailure(ex.GetType().Name);
                    if (_isConnected)
                    {
                        _totalConnectedTimeMs += (long)(DateTime.UtcNow - _lastConnectionStart).TotalMilliseconds;
                        _isConnected = false;
                    }
                }
                if (retryCount < _maxRetryAttempts && _allowReconnect)
                {
                    int delay = retryCount < _retryDelays.Length ? _retryDelays[retryCount] : _retryDelays[_retryDelays.Length - 1];
                    _logger.LogInformation("Retrying connection in {Delay}ms", delay);
                    await Task.Delay(delay);
                    await ConnectAsync(retryCount + 1);
                }
                else
                {
                    throw new InvalidOperationException("Failed to establish WebSocket connection after maximum retries", ex);
                }
            }
            finally
            {
                if (semaphoreAcquired)
                {
                    _connectSemaphore.Release();
                }
            }
        }

        /// <summary>
        /// Resets the current WebSocket connection and establishes a new one.
        /// </summary>
        /// <returns>A task representing the asynchronous reset operation.</returns>
        /// <remarks>
        /// <para>
        /// This method performs a clean reset of the WebSocket connection by:
        /// - Checking if a reconnection is already in progress to prevent duplicate operations
        /// - Closing the existing connection gracefully if it exists
        /// - Disposing of the old WebSocket instance
        /// - Establishing a new connection if reconnection is allowed
        /// </para>
        /// <para>
        /// The method includes proper error handling and ensures thread-safe operations
        /// during the connection reset process. If reconnection is disabled, the method
        /// will only clean up the existing connection without establishing a new one.
        /// </para>
        /// </remarks>
        public async Task ResetConnectionAsync()
        {
            if (_isReconnecting)
            {
                _logger.LogDebug("Reconnection already in progress, skipping");
                return;
            }

            _isReconnecting = true;
            _logger.LogInformation("Resetting WebSocket connection");
            try
            {
                ClientWebSocket? oldSocket = null;
                lock (_webSocketLock)
                {
                    if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                    {
                        _logger.LogWarning("Closing existing WebSocket connection");
                        oldSocket = _webSocket;
                    }
                    _webSocket = null;
                    _isConnected = false;
                }

                if (oldSocket != null)
                {
                    await oldSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Resetting socket", CancellationToken.None);
                    oldSocket.Dispose();
                    _logger.LogInformation("Closed and disposed existing WebSocket connection during reset");
                }

                if (_allowReconnect)
                {
                    _logger.LogDebug("Waiting {Delay}ms before reconnecting", _resetDelayMs);
                    await Task.Delay(_resetDelayMs);
                    await ConnectAsync();
                }
                else
                {
                    _logger.LogDebug("Reconnection disabled, skipping connect attempt.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset WebSocket connection");
                _isConnected = false;
            }
            finally
            {
                _isReconnecting = false;
                _logger.LogDebug("Reconnection attempt completed");
            }
        }

        /// <summary>
        /// Sends a text message through the WebSocket connection.
        /// </summary>
        /// <param name="message">The message string to send.</param>
        /// <returns>A task representing the asynchronous send operation.</returns>
        /// <remarks>
        /// <para>
        /// This method ensures thread-safe message sending by:
        /// - Acquiring the WebSocket lock to prevent concurrent access
        /// - Verifying the connection is active before sending
        /// - Converting the message to UTF-8 bytes for transmission
        /// - Sending the message as a complete text frame
        /// </para>
        /// <para>
        /// If the WebSocket is not connected or in an invalid state, the method
        /// will throw an InvalidOperationException with details about the connection status.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when attempting to send a message while the WebSocket is not connected.
        /// </exception>
        public async Task SendMessageAsync(string message)
        {
            _logger.LogDebug("Sending WebSocket message: {Message}", message);
            ClientWebSocket currentSocket;
            lock (_webSocketLock)
            {
                if (_webSocket == null || _webSocket.State != WebSocketState.Open)
                {
                    _logger.LogError("Cannot send message: WebSocket is not connected");
                    throw new InvalidOperationException("Cannot send message: WebSocket is not connected");
                }
                currentSocket = _webSocket;
            }

            var buffer = Encoding.UTF8.GetBytes(message);
            await currentSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
            _logger.LogInformation("Sent WebSocket message successfully");
        }

        /// <summary>
        /// Determines whether the WebSocket connection is currently active and ready for communication.
        /// </summary>
        /// <returns>True if the WebSocket is connected and in an open state, false otherwise.</returns>
        /// <remarks>
        /// <para>
        /// This method performs a comprehensive connection check by evaluating:
        /// - The internal connection flag (_isConnected)
        /// - The existence of a WebSocket instance
        /// - The WebSocket's current state (must be WebSocketState.Open)
        /// </para>
        /// <para>
        /// The method is thread-safe and provides detailed logging of the connection status
        /// for debugging purposes. It should be used to verify connection state before
        /// attempting to send messages or perform connection-dependent operations.
        /// </para>
        /// </remarks>
        public bool IsConnected()
        {
            lock (_webSocketLock)
            {
                bool connected = _isConnected && _webSocket != null && _webSocket.State == WebSocketState.Open;
                _logger.LogDebug("WebSocket connection status: {Connected} (isConnected={IsConnectedFlag}, webSocket={WebSocketState})",
                    connected, _isConnected, _webSocket?.State.ToString() ?? "null");
                return connected;
            }
        }

        /// <summary>
        /// Disables automatic reconnection for the WebSocket connection.
        /// </summary>
        /// <remarks>
        /// When reconnection is disabled, the WebSocket will not attempt to automatically
        /// reconnect after connection failures. This is useful during application shutdown
        /// or when manual connection control is required. The current connection state
        /// remains unchanged until explicitly modified.
        /// </remarks>
        public void DisableReconnect()
        {
            _logger.LogInformation("Disabling WebSocket reconnection.");
            _allowReconnect = false;
        }

        /// <summary>
        /// Enables automatic reconnection for the WebSocket connection.
        /// </summary>
        /// <remarks>
        /// When reconnection is enabled, the WebSocket will automatically attempt to
        /// reconnect after connection failures using configurable retry logic.
        /// This is the default behavior for maintaining reliable connections to Kalshi's platform.
        /// </remarks>
        public void EnableReconnect()
        {
            _logger.LogInformation("Enabling WebSocket reconnection.");
            _allowReconnect = true;
        }

        /// <summary>
        /// Continuously receives messages from the WebSocket connection until the connection is closed or an error occurs.
        /// </summary>
        /// <returns>A task representing the asynchronous receive operation.</returns>
        /// <remarks>
        /// <para>
        /// This method implements a continuous message receiving loop that:
        /// - Monitors the connection state and terminates if the WebSocket is no longer connected
        /// - Receives message fragments and assembles them into complete messages
        /// - Handles WebSocket close messages by throwing an exception to trigger reconnection
        /// - Processes only text messages, logging errors for unexpected message types
        /// - Maintains message ordering by waiting for EndOfMessage before processing
        /// </para>
        /// <para>
        /// The method includes comprehensive error handling for:
        /// - Connection loss during message reception
        /// - Server-initiated connection closures
        /// - Unexpected message types
        /// - General exceptions during the receive process
        /// </para>
        /// <para>
        /// Note: In the current implementation, received messages are logged but not processed.
        /// Message processing is handled by the higher-level MessageProcessor component that
        /// should be called from the main WebSocket client orchestrator.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the WebSocket connection is lost or closed by the server.
        /// </exception>
        public async Task ReceiveAsync()
        {
            _logger.LogInformation("WebSocket message receiving loop started");
            var buffer = new byte[_bufferSize];
            var messageBuilder = new StringBuilder();
            try
            {
                while (_isConnected)
                {
                    ClientWebSocket currentSocket;
                    lock (_webSocketLock)
                    {
                        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
                            throw new InvalidOperationException("WebSocket connection lost");
                        currentSocket = _webSocket;
                    }

                    var result = await currentSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
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
                            if (_enableMetrics)
                            {
                                _messagesReceived++;
                                var now = DateTime.UtcNow;
                                var messageLength = fullMessage.Length;

                                // Track buffer utilization
                                _totalBytesReceived += messageLength;
                                _bufferUtilizationCount++;
                                _averageBufferUtilization = (_averageBufferUtilization * (_bufferUtilizationCount - 1) + (double)messageLength / _bufferSize) / _bufferUtilizationCount;

                                // Update bandwidth
                                if (_lastConnectionStart != DateTime.MinValue)
                                {
                                    double connectedSeconds = (DateTime.UtcNow - _lastConnectionStart).TotalSeconds;
                                    _bandwidthBps = connectedSeconds > 0 ? _totalBytesReceived / connectedSeconds : 0;
                                }

                                // Update throughput with sliding window
                                _throughputWindow.Enqueue((now, 1));
                                while (_throughputWindow.Count > 0 && (now - _throughputWindow.Peek().timestamp).TotalSeconds > 60)
                                {
                                    _throughputWindow.Dequeue();
                                }
                                _messageThroughput = _throughputWindow.Count / 60.0; // messages per second over last 60 seconds

                                _lastMessageTime = now;

                                // Post metrics snapshot periodically
                                if ((now - _lastMetricsPost) >= _metricsPostInterval)
                                {
                                    PostMetricsSnapshot();
                                    _lastMetricsPost = now;
                                }
                            }
                            _logger.LogDebug("Received complete WebSocket message: Length={Length}", fullMessage.Length);
                            // Note: In the refactored design, message processing should be handled by MessageProcessor
                            // This method should be called from the main client which has access to MessageProcessor
                            messageBuilder.Clear();
                        }
                    }
                    else
                    {
                        _logger.LogError("Unexpected WebSocket message type: {MessageType}", result.MessageType);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WebSocket receiver encountered error: {Message}", ex.Message);
                if (_enableMetrics)
                {
                    _messageErrors++;
                    _errorRate = (_messagesReceived + _messageErrors) > 0 ? (double)_messageErrors / (_messagesReceived + _messageErrors) : 0;
                }
                _isConnected = false;
            }
            finally
            {
                _logger.LogInformation("WebSocket message receiving loop completed");
            }
        }

        /// <summary>
        /// Gracefully stops the WebSocket connection and disables reconnection.
        /// </summary>
        /// <returns>A task representing the asynchronous stop operation.</returns>
        /// <remarks>
        /// <para>
        /// This method performs a clean shutdown by:
        /// - Disabling automatic reconnection to prevent restart attempts
        /// - Closing the active WebSocket connection with a normal closure status
        /// - Disposing of the WebSocket instance to free resources
        /// - Updating the connection state to reflect the shutdown
        /// </para>
        /// <para>
        /// The method includes proper error handling to ensure the shutdown process
        /// completes even if individual steps fail. This is typically called during
        /// application shutdown or when manual connection control is required.
        /// </para>
        /// </remarks>
        public async Task StopAsync()
        {
            _allowReconnect = false;
            try
            {
                ClientWebSocket? oldSocket = null;
                lock (_webSocketLock)
                {
                    if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                    {
                        oldSocket = _webSocket;
                        if (_enableMetrics)
                        {
                            _totalConnectedTimeMs += (long)(DateTime.UtcNow - _lastConnectionStart).TotalMilliseconds;
                        }
                        _webSocket = null;
                        _isConnected = false;
                    }
                }
                if (oldSocket != null)
                {
                    await oldSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Stopping client", CancellationToken.None);
                    oldSocket.Dispose();
                    _logger.LogInformation("Closed and disposed WebSocket during shutdown");
                }

                // Post final metrics snapshot on shutdown
                PostMetricsSnapshot();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping WebSocketConnectionManager");
            }
        }

        /// <summary>
        /// Gets the current WebSocket instance for direct access.
        /// </summary>
        /// <returns>The current ClientWebSocket instance, or null if not connected.</returns>
        /// <remarks>
        /// <para>
        /// This method provides direct access to the underlying WebSocket for advanced operations
        /// that require low-level WebSocket manipulation. The returned instance should be used
        /// with caution as it bypasses the connection manager's thread-safety mechanisms.
        /// </para>
        /// <para>
        /// Thread Safety: This method is thread-safe and uses proper locking to ensure
        /// the WebSocket instance is not modified while being accessed.
        /// </para>
        /// </remarks>
        public ClientWebSocket? GetWebSocket()
        {
            lock (_webSocketLock)
            {
                return _webSocket;
            }
        }

        /// <summary>
        /// Gets the current count of the connection semaphore, indicating connection operation status.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The semaphore count indicates whether connection operations are currently in progress:
        /// - 1: No connection operation is active (semaphore is available)
        /// - 0: A connection operation is currently in progress (semaphore is held)
        /// </para>
        /// <para>
        /// This property is useful for monitoring and debugging connection concurrency issues.
        /// A count of 0 indicates that another thread is currently attempting to connect or
        /// reset the WebSocket connection.
        /// </para>
        /// </remarks>
        public int ConnectSemaphoreCount => _connectSemaphore.CurrentCount;

        /// <summary>
        /// Gets the total number of connection attempts made.
        /// </summary>
        public int ConnectionAttempts => _connectionAttempts;

        /// <summary>
        /// Gets the number of successful connections.
        /// </summary>
        public int ConnectionSuccesses => _connectionSuccesses;

        /// <summary>
        /// Gets the connection success rate (0.0 to 1.0).
        /// </summary>
        public double ConnectionSuccessRate => _connectionAttempts > 0 ? (double)_connectionSuccesses / _connectionAttempts : 0;

        /// <summary>
        /// Gets the number of reconnections performed.
        /// </summary>
        public int ReconnectionCount => _reconnectionCount;

        /// <summary>
        /// Gets the average connection latency in milliseconds.
        /// </summary>
        public double AverageConnectionLatency => _connectionLatencies.Count > 0 ? _connectionLatencies.Average() : 0;

        /// <summary>
        /// Gets the total number of messages received.
        /// </summary>
        public int MessagesReceived => _messagesReceived;

        /// <summary>
        /// Gets the current message throughput (messages per second).
        /// </summary>
        /// <remarks>
        /// Calculated using a sliding window approach over the last 60 seconds
        /// for more accurate real-time throughput measurement.
        /// </remarks>
        public double MessageThroughput => _messageThroughput;

        /// <summary>
        /// Gets the total number of connection failures.
        /// </summary>
        /// <remarks>
        /// Tracks all connection failures for monitoring connection reliability
        /// and identifying potential network or configuration issues.
        /// </remarks>
        public int ConnectionFailures => _connectionFailures;

        /// <summary>
        /// Gets the connection failure reasons and their counts.
        /// </summary>
        /// <remarks>
        /// Provides detailed breakdown of failure types (e.g., "WebSocketException", "TimeoutException")
        /// to help diagnose connection issues and optimize retry strategies.
        /// </remarks>
        public IReadOnlyDictionary<string, int> ConnectionFailureReasons => _connectionFailureReasons;

        /// <summary>
        /// Gets the signature cache hit rate (0.0 to 1.0).
        /// </summary>
        /// <remarks>
        /// Indicates the effectiveness of signature caching in reducing computational overhead.
        /// Higher values suggest better cache performance and reduced CPU usage for authentication.
        /// </remarks>
        public double SignatureCacheHitRate => (_signatureCacheHits + _signatureCacheMisses) > 0 ? (double)_signatureCacheHits / (_signatureCacheHits + _signatureCacheMisses) : 0;

        /// <summary>
        /// Gets the total number of signature cache hits.
        /// </summary>
        /// <remarks>
        /// Tracks successful cache lookups that avoided expensive signature generation.
        /// </remarks>
        public int SignatureCacheHits => _signatureCacheHits;

        /// <summary>
        /// Gets the total number of signature cache misses.
        /// </summary>
        /// <remarks>
        /// Tracks cache misses that required new signature generation.
        /// </remarks>
        public int SignatureCacheMisses => _signatureCacheMisses;

        /// <summary>
        /// Gets the total bytes received.
        /// </summary>
        /// <remarks>
        /// Cumulative count of all data received through the WebSocket connection.
        /// Useful for monitoring data transfer volumes and connection efficiency.
        /// </remarks>
        public long TotalBytesReceived => _totalBytesReceived;

        /// <summary>
        /// Gets the average buffer utilization (0.0 to 1.0).
        /// </summary>
        /// <remarks>
        /// Indicates how efficiently the WebSocket buffer is being used.
        /// Values closer to 1.0 suggest the buffer size is appropriate for the message sizes.
        /// Values much lower than 1.0 might indicate the buffer is oversized.
        /// </remarks>
        public double AverageBufferUtilization => _averageBufferUtilization;

        /// <summary>
        /// Gets the round-trip time (RTT) in milliseconds (proxied by average connection latency).
        /// </summary>
        /// <remarks>
        /// Since WebSocket is asynchronous and receive-only, this uses connection latency as a proxy for RTT.
        /// </remarks>
        public double RoundTripTime => AverageConnectionLatency;

        /// <summary>
        /// Gets the current bandwidth utilization in bytes per second.
        /// </summary>
        /// <remarks>
        /// Calculated as total bytes received divided by total connected time.
        /// </remarks>
        public double BandwidthBps => _bandwidthBps;

        /// <summary>
        /// Gets the error rate (0.0 to 1.0) for message processing.
        /// </summary>
        /// <remarks>
        /// Ratio of messages with errors to total messages processed.
        /// </remarks>
        public double ErrorRate => _errorRate;

        /// <summary>
        /// Gets the total connected time in milliseconds.
        /// </summary>
        /// <remarks>
        /// Cumulative time the WebSocket has been connected across all sessions.
        /// </remarks>
        public long TotalConnectedTimeMs => _totalConnectedTimeMs;

        /// <summary>
        /// Gets the current queue depth (number of messages waiting to be processed).
        /// </summary>
        /// <remarks>
        /// Currently returns 0 as messages are processed immediately without queuing.
        /// </remarks>
        public int QueueDepth => _queueDepth;

        /// <summary>
        /// Generates authentication headers required for WebSocket connection to Kalshi's platform.
        /// </summary>
        /// <param name="method">The HTTP method (typically "GET" for WebSocket connections).</param>
        /// <param name="path">The request path for the WebSocket endpoint.</param>
        /// <returns>A tuple containing the timestamp and base64-encoded RSA signature.</returns>
        /// <remarks>
        /// <para>
        /// This method implements Kalshi's authentication protocol by:
        /// - Generating a current timestamp in Unix milliseconds
        /// - Creating a message string combining timestamp, method, and path
        /// - Signing the message using RSA-PSS with SHA-256
        /// - Base64-encoding the signature for transmission
        /// </para>
        /// <para>
        /// The authentication headers are required for establishing secure WebSocket connections
        /// to Kalshi's trading platform. The signature proves the client's identity and prevents
        /// unauthorized access to real-time market data.
        /// </para>
        /// </remarks>
        private (string timestamp, string signature) GenerateAuthHeaders(string method, string path)
        {
            var cacheKey = $"{method}:{path.Split('?')[0]}";
            if (_signatureCache.TryGetValue(cacheKey, out var cached) && cached.expiry > DateTime.UtcNow)
            {
                _logger.LogDebug("Using cached auth headers for {Method} {Path}", method, path);
                if (_enableMetrics) _signatureCacheHits++;
                return (cached.timestamp, cached.signature);
            }
            if (_enableMetrics) _signatureCacheMisses++;

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var message = $"{timestamp}{method}{path.Split('?')[0]}";
            var signature = Convert.ToBase64String(_privateKey.SignData(
                Encoding.UTF8.GetBytes(message),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pss));

            _signatureCache[cacheKey] = (timestamp, signature, DateTime.UtcNow.Add(_signatureCacheDuration));
            return (timestamp, signature);
        }

        private bool _isReconnecting = false;

        /// <summary>
        /// Tracks connection failure reasons for monitoring and diagnostics.
        /// </summary>
        /// <param name="reason">The reason for the connection failure (e.g., exception type name).</param>
        /// <remarks>
        /// This method maintains a dictionary of failure reasons and their occurrence counts,
        /// enabling detailed analysis of connection reliability issues and troubleshooting.
        /// </remarks>
        private void TrackConnectionFailure(string reason)
        {
            if (_enableMetrics)
            {
                _connectionFailures++;
                if (_connectionFailureReasons.ContainsKey(reason))
                {
                    _connectionFailureReasons[reason]++;
                }
                else
                {
                    _connectionFailureReasons[reason] = 1;
                }
            }
        }

        /// <summary>
        /// Posts WebSocket performance metrics to the central performance monitor.
        /// </summary>
        /// <param name="operation">The operation name for the metric (e.g., "WebSocketConnect", "WebSocketReceive").</param>
        /// <param name="milliseconds">The execution time in milliseconds.</param>
        /// <remarks>
        /// This method sends performance data to the central performance monitor if available.
        /// It records execution times for various WebSocket operations to enable performance analysis.
        /// </remarks>
        private void PostPerformanceMetric(string operation, long milliseconds)
        {
            if (_performanceMonitor != null && _enableMetrics)
            {
                _performanceMonitor.RecordSpeedDialMetric("WebSocketConnectionManager", operation, $"WebSocket {operation} Time", $"Time taken for {operation}", (double)milliseconds, "ms", "WebSocket", 0, 10000, 1000, _enableMetrics);
            }
        }

        /// <summary>
        /// Posts current WebSocket metrics snapshot to the performance monitor.
        /// </summary>
        /// <remarks>
        /// This method captures and posts a comprehensive snapshot of current WebSocket performance
        /// metrics including connection stats, throughput, and error rates to the central monitor.
        /// </remarks>
        private void PostMetricsSnapshot()
        {
            if (_performanceMonitor == null || !_enableMetrics) return;

            // Post connection success rate as progress bar
            _performanceMonitor.RecordProgressBarMetric("WebSocketConnectionManager", "ConnectionSuccessRate", "WebSocket Connection Success Rate", "Percentage of successful connections", ConnectionSuccessRate * 100, "%", "WebSocket", 0, 100, 95, _enableMetrics);

            // Post throughput as speed dial
            _performanceMonitor.RecordSpeedDialMetric("WebSocketConnectionManager", "MessageThroughput", "WebSocket Message Throughput", "Messages received per second", MessageThroughput, "msg/sec", "WebSocket", 0, 1000, 100, _enableMetrics);

            // Post error rate as traffic light
            _performanceMonitor.RecordTrafficLightMetric("WebSocketConnectionManager", "ErrorRate", "WebSocket Error Rate", "Percentage of messages with errors", ErrorRate * 100, "%", "WebSocket", 0, 5, 1, _enableMetrics);

            // Post bandwidth as speed dial
            _performanceMonitor.RecordSpeedDialMetric("WebSocketConnectionManager", "Bandwidth", "WebSocket Bandwidth", "Data received per second", BandwidthBps, "bytes/sec", "WebSocket", 0, 1000000, 100000, _enableMetrics);

            _logger.LogDebug("Posted WebSocketConnectionManager metrics snapshot to performance monitor");
        }
    }
}
