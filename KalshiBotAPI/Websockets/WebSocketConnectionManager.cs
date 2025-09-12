using KalshiBotAPI.Configuration;
using KalshiBotAPI.WebSockets.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BacklashDTOs.Exceptions;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

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
    /// - Exponential backoff retry logic for connection failures
    /// - Semaphore-based synchronization to prevent concurrent connection attempts
    /// - Configurable reconnection behavior (can be disabled/enabled)
    /// - Comprehensive logging for operational visibility
    /// - Proper resource cleanup and disposal
    /// </para>
    /// </remarks>
    public class WebSocketConnectionManager : IWebSocketConnectionManager
    {
        private readonly ILogger<WebSocketConnectionManager> _logger;
        private readonly KalshiConfig _kalshiConfig;
        private readonly RSA _privateKey;
        private ClientWebSocket? _webSocket = null!;
        private readonly object _webSocketLock = new object();
        private bool _isConnected = false;
        private readonly SemaphoreSlim _connectSemaphore = new SemaphoreSlim(1, 1);
        private bool _allowReconnect = true;

        /// <summary>
        /// Initializes a new instance of the WebSocketConnectionManager class.
        /// </summary>
        /// <param name="kalshiConfig">Configuration options containing API credentials and connection settings.</param>
        /// <param name="logger">Logger instance for recording connection operations and errors.</param>
        /// <remarks>
        /// The constructor sets up the RSA private key for authentication by loading it from the
        /// configured key file. This key is used to generate signatures for WebSocket connection
        /// authentication with Kalshi's servers.
        /// </remarks>
        public WebSocketConnectionManager(
            IOptions<KalshiConfig> kalshiConfig,
            ILogger<WebSocketConnectionManager> logger)
        {
            _kalshiConfig = kalshiConfig.Value;
            _logger = logger;
            _privateKey = RSA.Create();
            _privateKey.ImportFromPem(File.ReadAllText(_kalshiConfig.KeyFile));
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
        /// The method implements exponential backoff retry logic for connection failures,
        /// with a maximum of 5 retry attempts. If all retries fail, it throws an
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
            bool semaphoreAcquired = false;
            try
            {
                semaphoreAcquired = await _connectSemaphore.WaitAsync(60000);
                if (!semaphoreAcquired)
                {
                    _logger.LogError("Failed to acquire connect semaphore within 60000ms");
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
                _logger.LogInformation("WebSocket connection established to {Uri}", uri);
                _isConnected = true;

                lock (_webSocketLock)
                {
                    _webSocket = newWebSocket;
                }
            }
            catch (WebSocketException ex) when (ex.Message.Contains("Failed to connect WebSocket on retry"))
            {
                _logger.LogWarning(new WebSocketRetryFailedException(ex.Message, ex), "Failed to connect to a websocket");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect WebSocket on retry {RetryCount}", retryCount);
                if (retryCount < 5 && _allowReconnect)
                {
                    int delay = (int)Math.Pow(2, retryCount) * 1000;
                    _logger.LogInformation("Retrying connection in {Delay}ms", delay);
                    await Task.Delay(delay);
                    await ConnectAsync(retryCount + 1);
                }
                else
                {
                    _isConnected = false;
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
                    _logger.LogDebug("Waiting 5 seconds");
                    await Task.Delay(5000);
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
        /// reconnect after connection failures using exponential backoff retry logic.
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
            var buffer = new byte[1024 * 16];
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
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var message = $"{timestamp}{method}{path.Split('?')[0]}";
            var signature = Convert.ToBase64String(_privateKey.SignData(
                Encoding.UTF8.GetBytes(message),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pss));
            return (timestamp, signature);
        }

        private bool _isReconnecting = false;
    }
}