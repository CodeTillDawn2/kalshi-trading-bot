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

        public WebSocketConnectionManager(
            IOptions<KalshiConfig> kalshiConfig,
            ILogger<WebSocketConnectionManager> logger)
        {
            _kalshiConfig = kalshiConfig.Value;
            _logger = logger;
            _privateKey = RSA.Create();
            _privateKey.ImportFromPem(File.ReadAllText(_kalshiConfig.KeyFile));
        }

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
                        _logger.LogDebug("Disposed closed or stale WebSocket");
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
                _logger.LogDebug("WebSocket connection established");
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
                    _logger.LogDebug("Released connect semaphore");
                }
            }
        }

        public async Task ResetConnectionAsync()
        {
            if (_isReconnecting)
            {
                _logger.LogDebug("Reconnection already in progress, skipping");
                return;
            }

            _isReconnecting = true;
            _logger.LogDebug("Resetting WebSocket connection");
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
                    _logger.LogDebug("Closed and disposed old WebSocket");
                }

                if (_allowReconnect)
                {
                    _logger.LogInformation("Waiting 5 seconds");
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

        public async Task SendMessageAsync(string message)
        {
            _logger.LogInformation("Sending WebSocket message: {Message}", message);
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
            _logger.LogDebug("Sent WebSocket message successfully");
        }

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

        public void DisableReconnect()
        {
            _logger.LogDebug("Disabling WebSocket reconnection.");
            _allowReconnect = false;
        }

        public void EnableReconnect()
        {
            _logger.LogDebug("Enabling WebSocket reconnection.");
            _allowReconnect = true;
        }

        public async Task ReceiveAsync()
        {
            _logger.LogDebug("ReceiveAsync started");
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
                _logger.LogDebug("ReceiveAsync completed");
            }
        }

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
                    _logger.LogDebug("Closed and disposed WebSocket");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping WebSocketConnectionManager");
            }
        }

        public ClientWebSocket? GetWebSocket()
        {
            lock (_webSocketLock)
            {
                return _webSocket;
            }
        }

        public int ConnectSemaphoreCount => _connectSemaphore.CurrentCount;

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