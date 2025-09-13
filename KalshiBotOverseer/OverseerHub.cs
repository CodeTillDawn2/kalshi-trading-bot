/// <summary>
/// SignalR hub that manages real-time communication between the Kalshi trading bot overseer
/// and connected brain instances. This hub handles client connections, authentication via
/// handshakes, periodic status check-ins from brain instances, and broadcasting of trading
/// data and updates. It serves as the central communication point for the overseer system,
/// enabling real-time monitoring and control of trading bot operations.
/// </summary>
using Microsoft.AspNetCore.SignalR;
using KalshiBotData.Data.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using BacklashDTOs;
using BacklashDTOs.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Concurrent;
using KalshiBotOverseer.Models;
using System.Threading;
using System.Diagnostics;

namespace KalshiBotOverseer
{
    /// <summary>
    /// SignalR hub for managing real-time communication with brain instances in the
    /// Kalshi trading bot overseer system. Handles authentication, status updates,
    /// and data broadcasting between the overseer and distributed brain components.
    /// </summary>
    public class OverseerHub : Hub
    {
        private readonly ILogger<OverseerHub> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ExecutionConfig _executionConfig;
        private static readonly ConcurrentDictionary<string, BrainPersistence> _brainStateCache = new ConcurrentDictionary<string, BrainPersistence>();

        // Performance metrics
        private readonly Stopwatch _uptimeStopwatch = Stopwatch.StartNew();
        private long _totalMessagesProcessed;
        private long _totalConnections;
        private long _activeConnections;
        private readonly ConcurrentDictionary<string, DateTime> _lastActivity = new();

        // Rate limiting
        private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _handshakeRateLimit = new();
        private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _checkInRateLimit = new();

        // Message batching
        private readonly ConcurrentQueue<(string Method, object Data, string? ClientFilter)> _messageBatch = new();
        private readonly Timer _messageBatchTimer;
        private readonly SemaphoreSlim _messageBatchSemaphore = new(1, 1);

        // Connection health monitoring
        private readonly Timer _healthCheckTimer;
        private readonly ConcurrentDictionary<string, DateTime> _connectionHealth = new();

        // Session management
        private readonly Timer _cleanupTimer;

        /// <summary>
        /// Initializes a new instance of the OverseerHub with required dependencies.
        /// </summary>
        /// <param name="logger">Logger instance for recording hub operations and errors.</param>
        /// <param name="scopeFactory">Factory for creating service scopes to access database context.</param>
        /// <param name="executionConfig">Execution configuration options.</param>
        public OverseerHub(ILogger<OverseerHub> logger, IServiceScopeFactory scopeFactory, IOptions<ExecutionConfig> executionConfig)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _executionConfig = executionConfig.Value;

            // Initialize timers
            _messageBatchTimer = new Timer(ProcessMessageBatch, null,
                _executionConfig.MessageBatchIntervalMs, _executionConfig.MessageBatchIntervalMs);

            _healthCheckTimer = new Timer(PerformHealthChecks, null,
                _executionConfig.ConnectionHealthCheckIntervalSeconds * 1000,
                _executionConfig.ConnectionHealthCheckIntervalSeconds * 1000);

            _cleanupTimer = new Timer(CleanupStaleConnections, null,
                _executionConfig.StaleConnectionCleanupIntervalMinutes * 60 * 1000,
                _executionConfig.StaleConnectionCleanupIntervalMinutes * 60 * 1000);
        }

        /// <summary>
        /// Handles client connection events, performing authentication and connection setup.
        /// Validates client credentials from query parameters and establishes the connection
        /// if authentication succeeds. Aborts the connection for invalid or missing credentials.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task OnConnectedAsync()
        {
            Interlocked.Increment(ref _totalConnections);
            Interlocked.Increment(ref _activeConnections);

            var httpContext = Context.GetHttpContext();
            var clientId = httpContext?.Request.Query["clientId"].ToString();
            var authToken = httpContext?.Request.Query["authToken"].ToString();
            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(authToken))
            {
                // Check rate limiting for connections
                if (IsRateLimitExceeded(_handshakeRateLimit, ipAddress, _executionConfig.HandshakeRateLimitPerMinute))
                {
                    LogAuditEvent("ConnectionRateLimited", clientId, ipAddress, false, "Connection rate limit exceeded");
                    _logger.LogWarning("Connection rate limit exceeded for IP: {IPAddress}", ipAddress);
                    Context.Abort();
                    return;
                }

                if (await AuthenticateClient(clientId, authToken))
                {
                    await UpdateClientConnectionId(clientId, Context.ConnectionId);
                    _connectionHealth[Context.ConnectionId] = DateTime.UtcNow;
                    _lastActivity[Context.ConnectionId] = DateTime.UtcNow;
                    LogAuditEvent("ConnectionAuthenticated", clientId, ipAddress, true);
                    _logger.LogInformation("Authenticated client connected: {ClientId}", clientId);
                }
                else
                {
                    LogAuditEvent("ConnectionAuthenticationFailed", clientId, ipAddress, false);
                    _logger.LogWarning("Failed authentication for client: {ClientId}", clientId);
                    Context.Abort();
                    return;
                }
            }
            else
            {
                LogAuditEvent("ConnectionMissingCredentials", clientId ?? "unknown", ipAddress, false);
                _logger.LogWarning("Missing authentication parameters for connection: {ConnectionId}", Context.ConnectionId);
                Context.Abort();
                return;
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Handles client disconnection events, logging the disconnection for monitoring purposes.
        /// </summary>
        /// <param name="exception">The exception that caused the disconnection, if any.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Interlocked.Decrement(ref _activeConnections);
            _connectionHealth.TryRemove(Context.ConnectionId, out _);
            _lastActivity.TryRemove(Context.ConnectionId, out _);

            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Performs initial handshake with a client, validating and storing client information,
        /// generating an authentication token, and logging the client to the database.
        /// This establishes the client's identity and prepares it for authenticated operations.
        /// </summary>
        /// <param name="clientId">Unique identifier for the client.</param>
        /// <param name="clientName">Name of the client (typically the brain instance name).</param>
        /// <param name="clientType">Type of client connecting (e.g., brain, dashboard).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task Handshake(string clientId, string clientName, string clientType)
        {
            var httpContext = Context.GetHttpContext();
            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // Check rate limiting
            if (IsRateLimitExceeded(_handshakeRateLimit, ipAddress, _executionConfig.HandshakeRateLimitPerMinute))
            {
                LogAuditEvent("HandshakeRateLimited", clientId, ipAddress, false, "Handshake rate limit exceeded");
                _logger.LogWarning("Handshake rate limit exceeded for IP: {IPAddress}", ipAddress);
                await Clients.Caller.SendAsync("HandshakeResponse", new
                {
                    Success = false,
                    Message = "Rate limit exceeded. Please try again later."
                });
                return;
            }

            _logger.LogInformation("Handshake request from client: {ClientId}, Name: {ClientName}, Type: {ClientType}",
                clientId, clientName, clientType);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

                // Check if client already exists
                var existingClient = await context.GetSignalRClient(clientId);
                if (existingClient == null)
                {
                    // Register new client
                    var newClient = new BacklashDTOs.SignalRClient
                    {
                        ClientId = clientId,
                        ClientName = clientName,
                        IPAddress = ipAddress,
                        ClientType = clientType,
                        AuthToken = GenerateAuthToken(clientId, clientName),
                        IsActive = true,
                        ConnectionId = Context.ConnectionId,
                        LastSeen = DateTime.UtcNow
                    };

                    await context.AddOrUpdateSignalRClient(newClient);
                    LogAuditEvent("HandshakeNewClient", clientId, ipAddress, true, $"Client type: {clientType}");
                    _logger.LogInformation("New client registered: {ClientId}", clientId);
                }
                else
                {
                    // Update existing client
                    existingClient.ConnectionId = Context.ConnectionId;
                    existingClient.LastSeen = DateTime.UtcNow;
                    existingClient.IsActive = true;
                    await context.AddOrUpdateSignalRClient(existingClient);
                    LogAuditEvent("HandshakeExistingClient", clientId, ipAddress, true, $"Client type: {clientType}");
                    _logger.LogInformation("Existing client updated: {ClientId}", clientId);
                }

                // Update activity tracking
                _lastActivity[Context.ConnectionId] = DateTime.UtcNow;

                // Send handshake response with auth token
                var response = new
                {
                    Success = true,
                    AuthToken = existingClient?.AuthToken ?? GenerateAuthToken(clientId, clientName),
                    Message = "Handshake successful"
                };

                await Clients.Caller.SendAsync("HandshakeResponse", response);
            }
            catch (Exception ex)
            {
                LogAuditEvent("HandshakeError", clientId, ipAddress, false, ex.Message);
                _logger.LogError(ex, "Error during handshake for client: {ClientId}", clientId);
                await Clients.Caller.SendAsync("HandshakeResponse", new
                {
                    Success = false,
                    Message = $"Handshake failed: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Processes periodic check-in data from a brain instance, updating its state in memory
        /// and broadcasting the updated status to all connected clients. This method handles
        /// comprehensive brain status updates including market data, performance metrics,
        /// and operational state information.
        /// </summary>
        /// <param name="checkInData">The check-in data containing brain status and metrics.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task CheckIn(CheckInData checkInData)
        {
            // Check rate limiting
            if (IsRateLimitExceeded(_checkInRateLimit, Context.ConnectionId, _executionConfig.CheckInRateLimitPerMinute))
            {
                _logger.LogWarning("CheckIn rate limit exceeded for client: {ConnectionId}", Context.ConnectionId);
                await Clients.Caller.SendAsync("CheckInReceived", new
                {
                    Success = false,
                    Message = "Rate limit exceeded. Please try again later."
                });
                return;
            }

            _logger.LogInformation("Received CheckIn from client: {ConnectionId}", Context.ConnectionId);
            Interlocked.Increment(ref _totalMessagesProcessed);

            // Update activity tracking
            _lastActivity[Context.ConnectionId] = DateTime.UtcNow;

            try
            {
                // Update in-memory BrainPersistence
                var brainInstanceName = checkInData.BrainInstanceName;
                var brainPersistence = _brainStateCache.GetOrAdd(brainInstanceName, _ => new BrainPersistence
                {
                    BrainInstanceName = brainInstanceName,
                    LastSeen = DateTime.UtcNow
                });

                // Update all properties
                brainPersistence.CurrentMarketTickers = new List<string>(checkInData.Markets ?? new List<string>());
                brainPersistence.ErrorCount = checkInData.ErrorCount;
                brainPersistence.LastSnapshot = checkInData.LastSnapshot;
                brainPersistence.IsStartingUp = checkInData.IsStartingUp;
                brainPersistence.IsShuttingDown = checkInData.IsShuttingDown;
                brainPersistence.WatchPositions = checkInData.WatchPositions;
                brainPersistence.WatchOrders = checkInData.WatchOrders;
                brainPersistence.ManagedWatchList = checkInData.ManagedWatchList;
                brainPersistence.CaptureSnapshots = checkInData.CaptureSnapshots;
                brainPersistence.TargetWatches = checkInData.TargetWatches;
                brainPersistence.MinimumInterest = checkInData.MinimumInterest;
                brainPersistence.UsageMin = checkInData.UsageMin;
                brainPersistence.UsageMax = checkInData.UsageMax;
                brainPersistence.IsWebSocketConnected = checkInData.IsWebSocketConnected;

                // Update metric histories with deduplication based on LastPerformanceSampleDate or current time
                var timestamp = checkInData.LastPerformanceSampleDate ?? DateTime.UtcNow;

                // Helper method to add metric if not duplicate
                void AddMetric(List<MetricHistory> history, double value)
                {
                    if (!history.Any(m => m.Timestamp == timestamp))
                    {
                        history.Add(new MetricHistory { Timestamp = timestamp, Value = value });
                    }
                }

                // Add queue and CPU metrics
                AddMetric(brainPersistence.CpuUsageHistory, checkInData.CurrentCpuUsage);
                AddMetric(brainPersistence.EventQueueHistory, checkInData.EventQueueAvg);
                AddMetric(brainPersistence.TickerQueueHistory, checkInData.TickerQueueAvg);
                AddMetric(brainPersistence.NotificationQueueHistory, checkInData.NotificationQueueAvg);
                AddMetric(brainPersistence.OrderbookQueueHistory, checkInData.OrderbookQueueAvg);

                // Add market count and error count to histories
                AddMetric(brainPersistence.MarketCountHistory, checkInData.Markets?.Count ?? 0);
                AddMetric(brainPersistence.ErrorHistory, checkInData.ErrorCount);

                // Add refresh metrics
                AddMetric(brainPersistence.RefreshCycleSecondsHistory, checkInData.LastRefreshCycleSeconds);
                AddMetric(brainPersistence.RefreshCycleIntervalHistory, checkInData.LastRefreshCycleInterval);
                AddMetric(brainPersistence.RefreshMarketCountHistory, checkInData.LastRefreshMarketCount);
                AddMetric(brainPersistence.RefreshUsagePercentageHistory, checkInData.LastRefreshUsagePercentage);
                AddMetric(brainPersistence.PerformanceSampleDateHistory, checkInData.LastPerformanceSampleDate?.Ticks ?? DateTime.UtcNow.Ticks);

                brainPersistence.LastRefreshTimeAcceptable = checkInData.LastRefreshTimeAcceptable;

                // Send response to caller
                await Clients.Caller.SendAsync("CheckInReceived", new
                {
                    Success = true,
                    Timestamp = DateTime.UtcNow,
                    Message = "CheckIn processed successfully"
                });

                // Queue CheckInUpdate for batched broadcasting
                var checkInUpdate = new
                {
                    BrainInstanceName = brainInstanceName,
                    MarketCount = checkInData.Markets?.Count ?? 0,
                    ErrorCount = checkInData.ErrorCount,
                    LastSnapshot = checkInData.LastSnapshot,
                    LastCheckIn = DateTime.UtcNow,
                    IsStartingUp = checkInData.IsStartingUp,
                    IsShuttingDown = checkInData.IsShuttingDown,
                    WatchPositions = checkInData.WatchPositions,
                    WatchOrders = checkInData.WatchOrders,
                    ManagedWatchList = checkInData.ManagedWatchList,
                    CaptureSnapshots = checkInData.CaptureSnapshots,
                    TargetWatches = checkInData.TargetWatches,
                    MinimumInterest = checkInData.MinimumInterest,
                    UsageMin = checkInData.UsageMin,
                    UsageMax = checkInData.UsageMax,
                    CurrentCpuUsage = checkInData.CurrentCpuUsage,
                    EventQueueAvg = checkInData.EventQueueAvg,
                    TickerQueueAvg = checkInData.TickerQueueAvg,
                    NotificationQueueAvg = checkInData.NotificationQueueAvg,
                    OrderbookQueueAvg = checkInData.OrderbookQueueAvg,
                    IsWebSocketConnected = checkInData.IsWebSocketConnected,
                    LastRefreshCycleSeconds = checkInData.LastRefreshCycleSeconds,
                    LastRefreshCycleInterval = checkInData.LastRefreshCycleInterval,
                    LastRefreshMarketCount = checkInData.LastRefreshMarketCount,
                    LastRefreshUsagePercentage = checkInData.LastRefreshUsagePercentage,
                    LastRefreshTimeAcceptable = checkInData.LastRefreshTimeAcceptable,
                    LastPerformanceSampleDate = checkInData.LastPerformanceSampleDate
                };

                QueueMessage("CheckInUpdate", checkInUpdate);

                _logger.LogInformation("Processed CheckIn for bot {BrainInstanceName} with {MarketCount} markets", brainInstanceName, checkInData.Markets?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CheckIn from client: {ConnectionId}", Context.ConnectionId);
                await Clients.Caller.SendAsync("CheckInReceived", new
                {
                    Success = false,
                    Message = $"CheckIn processing failed: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Authenticates a client by validating their credentials against the database
        /// and verifying the provided authentication token. Updates the client's last
        /// seen timestamp upon successful authentication.
        /// </summary>
        /// <param name="clientId">The unique identifier of the client to authenticate.</param>
        /// <param name="authToken">The authentication token provided by the client.</param>
        /// <returns>True if authentication succeeds, false otherwise.</returns>
        private async Task<bool> AuthenticateClient(string clientId, string authToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

                var client = await context.GetSignalRClient(clientId);
                if (client == null || !client.IsActive)
                    return false;

                // Validate auth token
                var expectedToken = GenerateAuthToken(client.ClientId, client.ClientName);
                if (authToken != expectedToken)
                    return false;

                // Update last seen
                await context.UpdateSignalRClientLastSeen(clientId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating client credentials for {ClientId}", clientId);
                return false;
            }
        }

        /// <summary>
        /// Updates the connection ID for a client in the database to maintain
        /// the mapping between client identity and SignalR connection.
        /// </summary>
        /// <param name="clientId">The unique identifier of the client.</param>
        /// <param name="connectionId">The new SignalR connection ID for the client.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task UpdateClientConnectionId(string clientId, string connectionId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
                await context.UpdateSignalRClientConnection(clientId, connectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating client connection for {ClientId}", clientId);
            }
        }

        /// <summary>
        /// Generates a simple authentication token based on client ID, name, and validity period.
        /// This provides basic authentication for client connections and should be replaced
        /// with more secure token generation in production environments.
        /// </summary>
        /// <param name="clientId">The client's unique identifier.</param>
        /// <param name="clientName">The client's name.</param>
        /// <returns>A base64-encoded hash string serving as the auth token.</returns>
        private string GenerateAuthToken(string clientId, string clientName)
        {
            using var sha256 = SHA256.Create();
            var validityDate = DateTime.UtcNow.AddHours(_executionConfig.AuthTokenValidityHours).Date;
            var input = $"{clientId}:{clientName}:{validityDate:yyyy-MM-dd}";
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Processes batched messages for efficient broadcasting.
        /// </summary>
        /// <param name="state">Timer state (unused).</param>
        private async void ProcessMessageBatch(object? state)
        {
            if (_messageBatch.IsEmpty)
                return;

            await _messageBatchSemaphore.WaitAsync();
            try
            {
                var batch = new List<(string Method, object Data, string? ClientFilter)>();
                while (_messageBatch.TryDequeue(out var message) && batch.Count < _executionConfig.MessageBatchSize)
                {
                    batch.Add(message);
                }

                if (batch.Count > 0)
                {
                    // Group messages by method and client filter for efficient sending
                    var groupedMessages = batch.GroupBy(m => (m.Method, m.ClientFilter));

                    foreach (var group in groupedMessages)
                    {
                        var method = group.Key.Method;
                        var clientFilter = group.Key.ClientFilter;

                        if (string.IsNullOrEmpty(clientFilter))
                        {
                            // Broadcast to all clients
                            await Clients.All.SendAsync(method, group.Select(g => g.Data).ToArray());
                        }
                        else
                        {
                            // Send to specific client
                            await Clients.Client(clientFilter).SendAsync(method, group.Select(g => g.Data).ToArray());
                        }
                    }

                    Interlocked.Add(ref _totalMessagesProcessed, batch.Count);
                }
            }
            finally
            {
                _messageBatchSemaphore.Release();
            }
        }

        /// <summary>
        /// Performs health checks on active connections.
        /// </summary>
        /// <param name="state">Timer state (unused).</param>
        private async void PerformHealthChecks(object? state)
        {
            var now = DateTime.UtcNow;
            var timeout = TimeSpan.FromSeconds(_executionConfig.ConnectionHealthTimeoutSeconds);

            foreach (var connection in _connectionHealth)
            {
                if (now - connection.Value > timeout)
                {
                    _logger.LogWarning("Connection {ConnectionId} failed health check", connection.Key);
                    // Could implement connection termination or notification here
                }
            }

            // Update connection health metrics
            _connectionHealth[Context.ConnectionId] = now;
        }

        /// <summary>
        /// Cleans up stale connections and sessions.
        /// </summary>
        /// <param name="state">Timer state (unused).</param>
        private async void CleanupStaleConnections(object? state)
        {
            var now = DateTime.UtcNow;
            var maxAge = TimeSpan.FromMinutes(_executionConfig.MaxConnectionAgeMinutes);

            // Clean up stale activity records
            var staleKeys = _lastActivity.Where(kvp => now - kvp.Value > maxAge).Select(kvp => kvp.Key).ToList();
            foreach (var key in staleKeys)
            {
                _lastActivity.TryRemove(key, out _);
            }

            // Clean up stale rate limit records
            var staleRateLimitKeys = _handshakeRateLimit.Keys.Where(key =>
                _handshakeRateLimit[key].IsEmpty || now - _handshakeRateLimit[key].Last() > TimeSpan.FromMinutes(1)).ToList();
            foreach (var key in staleRateLimitKeys)
            {
                _handshakeRateLimit.TryRemove(key, out _);
            }

            var staleCheckInKeys = _checkInRateLimit.Keys.Where(key =>
                _checkInRateLimit[key].IsEmpty || now - _checkInRateLimit[key].Last() > TimeSpan.FromMinutes(1)).ToList();
            foreach (var key in staleCheckInKeys)
            {
                _checkInRateLimit.TryRemove(key, out _);
            }

            _logger.LogInformation("Cleaned up {StaleConnections} stale connections", staleKeys.Count);
        }

        /// <summary>
        /// Checks if the rate limit has been exceeded for a given operation.
        /// </summary>
        /// <param name="rateLimitDict">The rate limit dictionary to check.</param>
        /// <param name="key">The key to check (IP or client ID).</param>
        /// <param name="limit">The rate limit per minute.</param>
        /// <returns>True if rate limit exceeded, false otherwise.</returns>
        private bool IsRateLimitExceeded(ConcurrentDictionary<string, ConcurrentQueue<DateTime>> rateLimitDict, string key, int limit)
        {
            var now = DateTime.UtcNow;
            var queue = rateLimitDict.GetOrAdd(key, _ => new ConcurrentQueue<DateTime>());

            // Remove old entries outside the 1-minute window
            while (queue.TryPeek(out var oldest) && now - oldest > TimeSpan.FromMinutes(1))
            {
                queue.TryDequeue(out _);
            }

            if (queue.Count >= limit)
            {
                return true;
            }

            queue.Enqueue(now);
            return false;
        }

        /// <summary>
        /// Queues a message for batched sending.
        /// </summary>
        /// <param name="method">The SignalR method to call.</param>
        /// <param name="data">The data to send.</param>
        /// <param name="clientFilter">Optional client ID filter.</param>
        private void QueueMessage(string method, object data, string? clientFilter = null)
        {
            _messageBatch.Enqueue((method, data, clientFilter));
        }

        /// <summary>
        /// Logs an audit event for authentication operations.
        /// </summary>
        /// <param name="eventType">The type of authentication event.</param>
        /// <param name="clientId">The client ID involved.</param>
        /// <param name="ipAddress">The IP address of the client.</param>
        /// <param name="success">Whether the operation was successful.</param>
        /// <param name="details">Additional details about the event.</param>
        private void LogAuditEvent(string eventType, string clientId, string ipAddress, bool success, string details = "")
        {
            _logger.LogInformation("AUDIT: {EventType} - Client: {ClientId}, IP: {IPAddress}, Success: {Success}, Details: {Details}",
                eventType, clientId, ipAddress, success, details);
        }

        /// <summary>
        /// Gets current hub performance metrics.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task GetHubMetrics()
        {
            var metrics = new
            {
                Uptime = _uptimeStopwatch.Elapsed,
                TotalConnections = _totalConnections,
                ActiveConnections = _activeConnections,
                TotalMessagesProcessed = _totalMessagesProcessed,
                MessagesPerSecond = _totalMessagesProcessed / Math.Max(1, _uptimeStopwatch.Elapsed.TotalSeconds),
                ConnectionHealthCount = _connectionHealth.Count,
                MessageBatchQueueSize = _messageBatch.Count,
                HandshakeRateLimitCount = _handshakeRateLimit.Count,
                CheckInRateLimitCount = _checkInRateLimit.Count
            };

            await Clients.Caller.SendAsync("HubMetrics", metrics);
        }

        /// <summary>
        /// Broadcasts a message to all clients or filtered clients based on permissions.
        /// </summary>
        /// <param name="method">The SignalR method to call.</param>
        /// <param name="data">The data to broadcast.</param>
        /// <param name="clientFilter">Optional client type filter (e.g., "brain", "dashboard").</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task BroadcastMessage(string method, object data, string? clientFilter = null)
        {
            // Basic client filtering - in a real implementation, this would check permissions
            if (string.IsNullOrEmpty(clientFilter))
            {
                QueueMessage(method, data);
            }
            else
            {
                // For filtered broadcasts, we'd need to look up clients by type
                // This is a simplified implementation
                QueueMessage(method, data);
            }
        }
    }
}