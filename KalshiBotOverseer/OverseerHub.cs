using Microsoft.AspNetCore.SignalR;
using KalshiBotOverseer.Services;
using KalshiBotAPI.Configuration;
using KalshiBotAPI.KalshiAPI;
using KalshiBotAPI.Websockets;
using KalshiBotAPI.WebSockets.Interfaces;
using KalshiBotData.Data;
using KalshiBotData.Data.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using KalshiBotOverseer.Models;
using System.Threading;
using System.Diagnostics;

namespace KalshiBotOverseer
{
    /// <summary>
    /// Configuration options for the OverseerHub SignalR hub.
    /// </summary>
    public class OverseerHubConfig
    {
        /// <summary>
        /// Gets or sets the timeout in seconds for connection health monitoring.
        /// Default is 300 seconds (5 minutes).
        /// </summary>
        public int ConnectionHealthTimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// Gets or sets the interval in seconds between health checks.
        /// Default is 60 seconds (1 minute).
        /// </summary>
        public int HealthCheckIntervalSeconds { get; set; } = 60;

        /// <summary>
        /// Gets or sets the validity duration for authentication tokens in hours.
        /// Default is 24 hours (1 day).
        /// </summary>
        public int AuthTokenValidityHours { get; set; } = 24;

        /// <summary>
        /// Gets or sets the maximum number of handshake requests allowed per minute per IP.
        /// Default is 10.
        /// </summary>
        public int MaxHandshakeRequestsPerMinute { get; set; } = 10;

        /// <summary>
        /// Gets or sets the maximum number of check-in requests allowed per minute per client.
        /// Default is 60.
        /// </summary>
        public int MaxCheckInRequestsPerMinute { get; set; } = 60;
    }

    /// <summary>
    /// SignalR hub that manages real-time communication between the Kalshi trading bot overseer
    /// and connected clients. This hub handles client connections, authentication via handshakes,
    /// periodic status check-ins from brain instances, and broadcasting of trading data and updates.
    /// It serves as the central communication point for the overseer system, enabling real-time
    /// monitoring and control of trading bot operations.
    /// </summary>
    public class OverseerHub : Hub
    {
        private static readonly HashSet<string> _connectedClients = new HashSet<string>();
        private static readonly ConcurrentDictionary<string, ClientInfo> _clientInfo = new();

        // Performance metrics
        private static long _totalMessagesProcessed = 0;
        private static long _totalHandshakeRequests = 0;
        private static long _totalCheckInRequests = 0;
        private static DateTime _lastMetricsReset = DateTime.UtcNow;
        private static readonly object _metricsLock = new object();

        // Rate limiting
        private static readonly ConcurrentDictionary<string, ClientRateLimit> _rateLimits = new();
        private static readonly Timer _rateLimitCleanupTimer;

        // Connection health monitoring
        private static readonly Timer _healthCheckTimer;

        private readonly ILogger<OverseerHub> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly BrainPersistenceService _brainService;
        private readonly OverseerHubConfig _config;

        static OverseerHub()
        {
            // Initialize timers
            _healthCheckTimer = new Timer(PerformHealthChecks, null, 0, 60000); // 60 seconds
            _rateLimitCleanupTimer = new Timer(CleanupRateLimits, null, 0, 300000); // 5 minutes
        }

        /// <summary>
        /// Performs periodic health checks on connected clients.
        /// Removes clients that have exceeded the configured timeout.
        /// </summary>
        private static void PerformHealthChecks(object? state)
        {
            try
            {
                var now = DateTime.UtcNow;
                var timeout = TimeSpan.FromSeconds(300); // Default 5 minutes, should be configurable but static method can't access instance config

                lock (_connectedClients)
                {
                    var clientsToRemove = new List<string>();
                    foreach (var connectionId in _connectedClients)
                    {
                        if (_clientInfo.TryGetValue(connectionId, out var clientInfo))
                        {
                            if (now - clientInfo.LastSeen > timeout)
                            {
                                clientsToRemove.Add(connectionId);
                            }
                        }
                    }

                    foreach (var connectionId in clientsToRemove)
                    {
                        _connectedClients.Remove(connectionId);
                        _clientInfo.TryRemove(connectionId, out _);
                    }
                }
            }
            catch (Exception)
            {
                // Log error - would need logger instance
            }
        }

        /// <summary>
        /// Cleans up expired rate limit entries.
        /// </summary>
        private static void CleanupRateLimits(object? state)
        {
            try
            {
                var now = DateTime.UtcNow;
                var expiredKeys = new List<string>();

                foreach (var kvp in _rateLimits)
                {
                    if (now - kvp.Value.WindowStart > TimeSpan.FromMinutes(1))
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }

                foreach (var key in expiredKeys)
                {
                    _rateLimits.TryRemove(key, out _);
                }
            }
            catch (Exception)
            {
                // Log error
            }
        }

        public OverseerHub(
            ILogger<OverseerHub> logger,
            IServiceScopeFactory scopeFactory,
            BrainPersistenceService brainService,
            IOptions<OverseerHubConfig> config)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _brainService = brainService;
            _config = config.Value;
        }

        /// <summary>
        /// Handles client connection events, tracking connected clients and logging connection details.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task OnConnectedAsync()
        {
            lock (_connectedClients)
            {
                _connectedClients.Add(Context.ConnectionId);
            }

            var httpContext = Context.GetHttpContext();
            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            _logger.LogInformation("Client connected: {ConnectionId} from IP: {IPAddress}. Total clients: {ClientCount}",
                Context.ConnectionId, ipAddress, _connectedClients.Count);

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Handles client disconnection events, cleaning up client tracking and logging disconnection details.
        /// </summary>
        /// <param name="exception">The exception that caused the disconnection, if any.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            string clientId = "";
            lock (_connectedClients)
            {
                _connectedClients.Remove(Context.ConnectionId);
                if (_clientInfo.TryGetValue(Context.ConnectionId, out var info))
                {
                    clientId = info.ClientId;
                    _clientInfo.TryRemove(Context.ConnectionId, out _);
                }
            }

            _logger.LogInformation("Client disconnected: {ConnectionId} (ClientId: {ClientId}). Total clients: {ClientCount}",
                Context.ConnectionId, clientId, _connectedClients.Count);

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Checks if there are any connected clients to the hub.
        /// </summary>
        /// <returns>True if there are connected clients, false otherwise.</returns>
        public static bool HasConnectedClients()
        {
            lock (_connectedClients)
            {
                return _connectedClients.Any();
            }
        }

        /// <summary>
        /// Clears all connected client tracking, useful for testing or reset scenarios.
        /// </summary>
        public static void ClearConnectedClients()
        {
            lock (_connectedClients)
            {
                _connectedClients.Clear();
                _clientInfo.Clear();
            }
        }

        /// <summary>
        /// Gets the current performance metrics for the hub.
        /// </summary>
        /// <returns>A dictionary containing performance metrics.</returns>
        public static Dictionary<string, object> GetPerformanceMetrics()
        {
            lock (_metricsLock)
            {
                var now = DateTime.UtcNow;
                var timeSinceReset = now - _lastMetricsReset;
                var minutesSinceReset = timeSinceReset.TotalMinutes;

                return new Dictionary<string, object>
                {
                    ["TotalMessagesProcessed"] = _totalMessagesProcessed,
                    ["TotalHandshakeRequests"] = _totalHandshakeRequests,
                    ["TotalCheckInRequests"] = _totalCheckInRequests,
                    ["MessagesPerMinute"] = minutesSinceReset > 0 ? _totalMessagesProcessed / minutesSinceReset : 0,
                    ["HandshakeRequestsPerMinute"] = minutesSinceReset > 0 ? _totalHandshakeRequests / minutesSinceReset : 0,
                    ["CheckInRequestsPerMinute"] = minutesSinceReset > 0 ? _totalCheckInRequests / minutesSinceReset : 0,
                    ["CurrentConnectionCount"] = _connectedClients.Count,
                    ["LastMetricsReset"] = _lastMetricsReset
                };
            }
        }

        /// <summary>
        /// Resets the performance metrics counters.
        /// </summary>
        public static void ResetPerformanceMetrics()
        {
            lock (_metricsLock)
            {
                _totalMessagesProcessed = 0;
                _totalHandshakeRequests = 0;
                _totalCheckInRequests = 0;
                _lastMetricsReset = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Checks if a client has exceeded rate limits for a specific operation.
        /// </summary>
        private bool IsRateLimited(string key, string operation, int maxRequestsPerMinute)
        {
            var now = DateTime.UtcNow;
            var rateLimit = _rateLimits.GetOrAdd(key, _ => new ClientRateLimit { Key = key, WindowStart = now });

            // Reset window if it's been more than a minute
            if (now - rateLimit.WindowStart > TimeSpan.FromMinutes(1))
            {
                rateLimit.WindowStart = now;
                rateLimit.HandshakeCount = 0;
                rateLimit.CheckInCount = 0;
            }

            // Check limits
            if (operation == "handshake" && rateLimit.HandshakeCount >= maxRequestsPerMinute)
                return true;
            if (operation == "checkin" && rateLimit.CheckInCount >= maxRequestsPerMinute)
                return true;

            // Increment counter
            if (operation == "handshake") rateLimit.HandshakeCount++;
            else if (operation == "checkin") rateLimit.CheckInCount++;

            return false;
        }

        /// <summary>
        /// Performs initial handshake with a client, validating and storing client information,
        /// generating an authentication token, and logging the client to the database.
        /// Includes rate limiting to prevent abuse.
        /// </summary>
        /// <param name="clientId">Unique identifier for the client.</param>
        /// <param name="clientName">Name of the client (typically the brain instance name).</param>
        /// <param name="clientType">Type of client connecting (e.g., brain, dashboard).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task Handshake(string clientId, string clientName, string clientType)
        {
            var httpContext = Context.GetHttpContext();
            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // Rate limiting check
            if (IsRateLimited(ipAddress, "handshake", _config.MaxHandshakeRequestsPerMinute))
            {
                _logger.LogWarning("Handshake rate limit exceeded for IP: {IPAddress}", ipAddress);
                await Clients.Caller.SendAsync("HandshakeResponse", new
                {
                    Success = false,
                    Message = "Rate limit exceeded. Please try again later."
                });
                return;
            }

            Interlocked.Increment(ref _totalHandshakeRequests);
            Interlocked.Increment(ref _totalMessagesProcessed);

            _logger.LogInformation("Handshake request from client: {ClientId}, Name: {ClientName}, Type: {ClientType}",
                clientId, clientName, clientType);

            try
            {

                // Store client information
                var clientInfo = new ClientInfo
                {
                    ClientId = clientId,
                    ClientName = clientName,
                    ClientType = clientType,
                    IPAddress = ipAddress,
                    ConnectionId = Context.ConnectionId,
                    LastSeen = DateTime.UtcNow
                };

                _clientInfo[Context.ConnectionId] = clientInfo;

                // Log to database if available
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

                    var signalRClient = new BacklashDTOs.SignalRClient
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

                    await context.AddOrUpdateSignalRClient(signalRClient);
                    _logger.LogInformation("Client registered in database: {ClientId} from {IPAddress}", clientId, ipAddress);
                }
                catch (Exception dbEx)
                {
                    _logger.LogWarning(dbEx, "Failed to log client to database: {ClientId}", clientId);
                }

                // Send handshake response
                var response = new
                {
                    Success = true,
                    AuthToken = GenerateAuthToken(clientId, clientName),
                    Message = "Handshake successful"
                };

                await Clients.Caller.SendAsync("HandshakeResponse", response);
                _logger.LogInformation("Handshake completed for client: {ClientId}", clientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during handshake for client: {ClientId}", clientId);
                await Clients.Caller.SendAsync("HandshakeResponse", new
                {
                    Success = false,
                    Message = $"Handshake failed: {ex.Message}"
                });
            }
        }

        public async Task ProcessCheckIn(CheckInData checkInData)
        {
            // Rate limiting check
            var clientId = Context.ConnectionId;
            if (IsRateLimited(clientId, "checkin", _config.MaxCheckInRequestsPerMinute))
            {
                _logger.LogWarning("CheckIn rate limit exceeded for client: {ConnectionId}", clientId);
                await Clients.Caller.SendAsync("CheckInResponse", new
                {
                    Success = false,
                    Message = "Rate limit exceeded. Please try again later."
                });
                return;
            }

            Interlocked.Increment(ref _totalCheckInRequests);
            Interlocked.Increment(ref _totalMessagesProcessed);

            _logger.LogInformation("CheckIn received from connection: {ConnectionId}", Context.ConnectionId);

            try
            {
                if (!_clientInfo.TryGetValue(Context.ConnectionId, out var clientInfo))
                {
                    _logger.LogWarning("CheckIn received from unregistered client: {ConnectionId}", Context.ConnectionId);
                    await Clients.Caller.SendAsync("CheckInResponse", new
                    {
                        Success = false,
                        Message = "Client not registered. Please perform handshake first."
                    });
                    return;
                }

                // Validate CheckInData
                if (checkInData == null)
                {
                    _logger.LogWarning("CheckIn received with null data from client: {ConnectionId}", Context.ConnectionId);
                    await Clients.Caller.SendAsync("CheckInResponse", new
                    {
                        Success = false,
                        Message = "CheckIn data is null."
                    });
                    return;
                }

                if (string.IsNullOrEmpty(checkInData.BrainInstanceName))
                {
                    _logger.LogWarning("CheckIn received with missing BrainInstanceName from client: {ConnectionId}", Context.ConnectionId);
                    await Clients.Caller.SendAsync("CheckInResponse", new
                    {
                        Success = false,
                        Message = "BrainInstanceName is required."
                    });
                    return;
                }

                _logger.LogInformation("CheckIn received from {ClientId} ({ClientName}): {MarketCount} markets, ErrorCount: {ErrorCount}, LastSnapshot: {LastSnapshot}",
                    clientInfo.ClientId, clientInfo.ClientName,
                    checkInData.Markets?.Count ?? 0,
                    checkInData.ErrorCount,
                    checkInData.LastSnapshot);

                // Verify brain name consistency between handshake and CheckIn
                if (clientInfo.ClientName != checkInData.BrainInstanceName)
                {
                    _logger.LogWarning("Brain name mismatch! Handshake: '{HandshakeName}', CheckIn: '{CheckInName}' for client {ClientId}",
                        clientInfo.ClientName, checkInData.BrainInstanceName, clientInfo.ClientId);
                }

                // Log database brain data availability
                var dbBrainExists = _brainService.GetBrain(clientInfo.ClientName ?? "") != null;
                _logger.LogInformation("Database brain data check: ClientName='{ClientName}', ExistsInDB={Exists}",
                    clientInfo.ClientName, dbBrainExists);

                // Update current market tickers in persistence service
                if (!string.IsNullOrEmpty(clientInfo.ClientName))
                {
                    try
                    {
                        await _brainService.UpdateCurrentMarketTickersAsync(clientInfo.ClientName, checkInData.Markets ?? new List<string>());

                        // Update historical metrics
                        await _brainService.UpdateMetricHistoryAsync(clientInfo.ClientName, "CpuUsage", checkInData.CurrentCpuUsage);
                        await _brainService.UpdateMetricHistoryAsync(clientInfo.ClientName, "EventQueue", checkInData.EventQueueAvg);
                        await _brainService.UpdateMetricHistoryAsync(clientInfo.ClientName, "TickerQueue", checkInData.TickerQueueAvg);
                        await _brainService.UpdateMetricHistoryAsync(clientInfo.ClientName, "NotificationQueue", checkInData.NotificationQueueAvg);
                        await _brainService.UpdateMetricHistoryAsync(clientInfo.ClientName, "OrderbookQueue", checkInData.OrderbookQueueAvg);
                        await _brainService.UpdateMetricHistoryAsync(clientInfo.ClientName, "MarketCount", checkInData.Markets?.Count ?? 0);
                        await _brainService.UpdateMetricHistoryAsync(clientInfo.ClientName, "Error", checkInData.ErrorCount);
                    }
                    catch (Exception brainEx)
                    {
                        _logger.LogWarning(brainEx, "Failed to update brain service for {ClientName}", clientInfo.ClientName);
                    }
                }

                // Update client last seen
                clientInfo.LastSeen = DateTime.UtcNow;

                // Get target market tickers for this brain
                var targetTickers = new string[0];
                if (!string.IsNullOrEmpty(clientInfo.ClientName))
                {
                    try
                    {
                        targetTickers = _brainService.GetTargetMarketTickers(clientInfo.ClientName).ToArray();
                    }
                    catch (Exception tickerEx)
                    {
                        _logger.LogWarning(tickerEx, "Failed to get target tickers for {ClientName}", clientInfo.ClientName);
                        targetTickers = new string[0];
                    }
                }

                // Update client last seen in database
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

                    // Update client last seen
                    var signalRClient = await context.GetSignalRClient(clientInfo.ClientId);
                    if (signalRClient != null)
                    {
                        signalRClient.LastSeen = DateTime.UtcNow;
                        await context.AddOrUpdateSignalRClient(signalRClient);
                    }
                }
                catch (Exception dbEx)
                {
                    _logger.LogWarning(dbEx, "Failed to log CheckIn to database for client: {ClientId}", clientInfo.ClientId);
                }

                // Get existing brain data for historical metrics
                BrainPersistence existingBrain;
                if (!string.IsNullOrEmpty(clientInfo.ClientName))
                {
                    existingBrain = _brainService.GetBrain(clientInfo.ClientName);
                    _logger.LogInformation("Retrieved brain persistence data for '{BrainName}': CpuHistory={CpuCount}, EventHistory={EventCount}, ErrorHistory={ErrorCount}",
                        clientInfo.ClientName,
                        existingBrain.CpuUsageHistory?.Count ?? 0,
                        existingBrain.EventQueueHistory?.Count ?? 0,
                        existingBrain.ErrorHistory?.Count ?? 0);
                }
                else
                {
                    _logger.LogWarning("ClientName is null or empty, using default brain data");
                    existingBrain = new BrainPersistence { BrainInstanceName = "" };
                }

                // Create comprehensive brain status data
                var brainStatus = new BrainStatusData
                {
                    BrainInstanceName = checkInData.BrainInstanceName,

                    // Basic market data
                    Markets = checkInData.Markets,
                    ErrorCount = checkInData.ErrorCount,
                    LastSnapshot = checkInData.LastSnapshot,
                    LastCheckIn = DateTime.UtcNow, // Add lastCheckIn timestamp
                    IsStartingUp = checkInData.IsStartingUp,
                    IsShuttingDown = checkInData.IsShuttingDown,

                    // Brain configuration
                    WatchPositions = checkInData.WatchPositions,
                    WatchOrders = checkInData.WatchOrders,
                    ManagedWatchList = checkInData.ManagedWatchList,
                    CaptureSnapshots = checkInData.CaptureSnapshots,
                    TargetWatches = checkInData.TargetWatches,
                    MinimumInterest = checkInData.MinimumInterest,
                    UsageMin = checkInData.UsageMin,
                    UsageMax = checkInData.UsageMax,

                    // Performance metrics
                    CurrentCpuUsage = checkInData.CurrentCpuUsage,
                    EventQueueAvg = checkInData.EventQueueAvg,
                    TickerQueueAvg = checkInData.TickerQueueAvg,
                    NotificationQueueAvg = checkInData.NotificationQueueAvg,
                    OrderbookQueueAvg = checkInData.OrderbookQueueAvg,
                    LastRefreshCycleSeconds = checkInData.LastRefreshCycleSeconds,
                    LastRefreshCycleInterval = checkInData.LastRefreshCycleInterval,
                    LastRefreshMarketCount = checkInData.LastRefreshMarketCount,
                    LastRefreshUsagePercentage = checkInData.LastRefreshUsagePercentage,
                    LastRefreshTimeAcceptable = checkInData.LastRefreshTimeAcceptable,
                    LastPerformanceSampleDate = checkInData.LastPerformanceSampleDate,

                    // Connection status
                    IsWebSocketConnected = checkInData.IsWebSocketConnected,

                    // Market watch data
                    WatchedMarkets = checkInData.WatchedMarkets
                };

                // Broadcast comprehensive brain status to all connected clients (including web UI)
                var connections = _connectedClients.Count;
                _logger.LogInformation("Broadcasting BrainStatusUpdate for {Brain} to {Count} connections",
                    brainStatus.BrainInstanceName, connections);

                await Clients.All.SendAsync("BrainStatusUpdate", brainStatus);

                await Clients.All.SendAsync("BroadcastTrace", new
                {
                    kind = "BrainStatusUpdate",
                    brain = brainStatus.BrainInstanceName,
                    marketCount = brainStatus.Markets?.Count ?? 0,
                    serverUtc = DateTime.UtcNow
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CheckIn from client: {ConnectionId}", Context.ConnectionId);
                await Clients.Caller.SendAsync("CheckInResponse", new
                {
                    Success = false,
                    Message = $"CheckIn processing failed: {ex.Message}"
                });
            }
        }


        /// <summary>
        /// Processes messages from overseer clients, handling different message types
        /// such as data refresh requests.
        /// </summary>
        /// <param name="messageType">The type of message being sent (e.g., "refresh_data").</param>
        /// <param name="message">The message content or payload.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task HandleOverseerMessage(string messageType, string message)
        {
            _logger.LogInformation("Received SendOverseerMessage: {MessageType} - {Message}", messageType, message);

            try
            {
                // Handle different message types
                switch (messageType.ToLower())
                {
                    case "refresh_data":
                        // Broadcast refresh request to all connected clients
                        await Clients.All.SendAsync("DataRefreshRequested", new
                        {
                            MessageType = messageType,
                            Message = message,
                            Timestamp = DateTime.UtcNow,
                            RequestedBy = Context.ConnectionId
                        });
                        break;

                    default:
                        _logger.LogWarning("Unknown message type received: {MessageType}", messageType);
                        break;
                }

                // Send confirmation back to caller
                await Clients.Caller.SendAsync("OverseerMessageReceived", new
                {
                    Success = true,
                    MessageType = messageType,
                    Timestamp = DateTime.UtcNow,
                    Message = "Message processed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SendOverseerMessage: {MessageType}", messageType);
                await Clients.Caller.SendAsync("OverseerMessageReceived", new
                {
                    Success = false,
                    MessageType = messageType,
                    Message = $"Failed to process message: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Generates a configurable authentication token based on client ID, name, and validity period.
        /// This is used for basic client validation during the handshake process.
        /// </summary>
        /// <param name="clientId">The client's unique identifier.</param>
        /// <param name="clientName">The client's name.</param>
        /// <returns>A base64-encoded hash string serving as the auth token.</returns>
        private string GenerateAuthToken(string clientId, string clientName)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var expiry = DateTime.UtcNow.AddHours(_config.AuthTokenValidityHours);
            var input = $"{clientId}:{clientName}:{expiry:yyyy-MM-ddTHH:mm:ssZ}";
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Represents information about a connected client, including identification,
        /// connection details, and last activity timestamp.
        /// </summary>
        public class ClientInfo
        {
            /// <summary>
            /// Gets or sets the unique identifier for the client.
            /// </summary>
            public string ClientId { get; set; } = "";

            /// <summary>
            /// Gets or sets the name of the client (typically the brain instance name).
            /// </summary>
            public string ClientName { get; set; } = "";

            /// <summary>
            /// Gets or sets the type of client (e.g., brain, dashboard).
            /// </summary>
            public string ClientType { get; set; } = "";

            /// <summary>
            /// Gets or sets the IP address of the connecting client.
            /// </summary>
            public string IPAddress { get; set; } = "";

            /// <summary>
            /// Gets or sets the SignalR connection ID for this client.
            /// </summary>
            public string ConnectionId { get; set; } = "";

            /// <summary>
            /// Gets or sets the timestamp of the client's last activity.
            /// </summary>
            public DateTime LastSeen { get; set; }
            }
    
            /// <summary>
            /// Represents rate limiting information for a client.
            /// </summary>
            private class ClientRateLimit
            {
                public string Key { get; set; } = "";
                public int HandshakeCount { get; set; }
                public int CheckInCount { get; set; }
                public DateTime WindowStart { get; set; } = DateTime.UtcNow;
            }
        }

    /// <summary>
    /// Data structure containing comprehensive information about a brain instance's current state,
    /// used during periodic check-in operations to report status to the overseer system.
    /// </summary>
    public class CheckInData
    {
        /// <summary>
        /// Gets or sets the name of the brain instance performing the check-in.
        /// </summary>
        public string? BrainInstanceName { get; set; }

        /// <summary>
        /// Gets or sets the list of market tickers currently being monitored by the brain.
        /// </summary>
        public List<string>? Markets { get; set; }

        /// <summary>
        /// Gets or sets the total count of errors encountered by the brain since startup.
        /// </summary>
        public long ErrorCount { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last snapshot taken by the brain.
        /// </summary>
        public DateTime? LastSnapshot { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the brain is currently starting up.
        /// </summary>
        public bool IsStartingUp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the brain is currently shutting down.
        /// </summary>
        public bool IsShuttingDown { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the brain is monitoring positions.
        /// </summary>
        public bool WatchPositions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the brain is monitoring orders.
        /// </summary>
        public bool WatchOrders { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the brain uses a managed watch list.
        /// </summary>
        public bool ManagedWatchList { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the brain captures snapshots.
        /// </summary>
        public bool CaptureSnapshots { get; set; }

        /// <summary>
        /// Gets or sets the target number of markets to watch.
        /// </summary>
        public int TargetWatches { get; set; }

        /// <summary>
        /// Gets or sets the minimum interest threshold for market selection.
        /// </summary>
        public double MinimumInterest { get; set; }

        /// <summary>
        /// Gets or sets the minimum usage threshold for the brain.
        /// </summary>
        public double UsageMin { get; set; }

        /// <summary>
        /// Gets or sets the maximum usage threshold for the brain.
        /// </summary>
        public double UsageMax { get; set; }

        /// <summary>
        /// Gets or sets the current CPU usage percentage of the brain process.
        /// </summary>
        public double CurrentCpuUsage { get; set; }

        /// <summary>
        /// Gets or sets the average size of the event processing queue.
        /// </summary>
        public double EventQueueAvg { get; set; }

        /// <summary>
        /// Gets or sets the average size of the ticker processing queue.
        /// </summary>
        public double TickerQueueAvg { get; set; }

        /// <summary>
        /// Gets or sets the average size of the notification processing queue.
        /// </summary>
        public double NotificationQueueAvg { get; set; }

        /// <summary>
        /// Gets or sets the average size of the orderbook processing queue.
        /// </summary>
        public double OrderbookQueueAvg { get; set; }

        /// <summary>
        /// Gets or sets the duration in seconds of the last refresh cycle.
        /// </summary>
        public double LastRefreshCycleSeconds { get; set; }

        /// <summary>
        /// Gets or sets the interval between refresh cycles.
        /// </summary>
        public double LastRefreshCycleInterval { get; set; }

        /// <summary>
        /// Gets or sets the number of markets processed in the last refresh cycle.
        /// </summary>
        public double LastRefreshMarketCount { get; set; }

        /// <summary>
        /// Gets or sets the CPU usage percentage during the last refresh cycle.
        /// </summary>
        public double LastRefreshUsagePercentage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the last refresh cycle completed within acceptable time limits.
        /// </summary>
        public bool LastRefreshTimeAcceptable { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last performance sample.
        /// </summary>
        public DateTime? LastPerformanceSampleDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the brain is connected to the WebSocket feed.
        /// </summary>
        public bool IsWebSocketConnected { get; set; }

        /// <summary>
        /// Gets or sets the list of markets currently being watched with detailed information.
        /// </summary>
        public List<MarketWatchData>? WatchedMarkets { get; set; }
    }
}