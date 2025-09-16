using Microsoft.AspNetCore.SignalR;
using BacklashOverseer.Services;
using KalshiBotAPI.Configuration;
using KalshiBotAPI.KalshiAPI;
using KalshiBotAPI.Websockets;
using KalshiBotAPI.WebSockets.Interfaces;
using KalshiBotData.Data;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using BacklashOverseer.Models;
using System.Threading;
using System.Diagnostics;
using BacklashBotData.Data.Interfaces;

namespace BacklashOverseer
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

        /// <summary>
        /// Gets or sets a value indicating whether performance metrics collection is enabled.
        /// When enabled, all performance metrics including latency tracking, message counting,
        /// connection monitoring, and brain metrics are collected and recorded.
        /// When disabled, only essential operations are performed with minimal overhead.
        /// Default is false for performance reasons.
        /// </summary>
        public bool EnablePerformanceMetrics { get; set; } = false;
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

        // Performance metrics are now handled by PerformanceMetricsService

        // Rate limiting
        private static readonly ConcurrentDictionary<string, ClientRateLimit> _rateLimits = new();
        private static readonly Timer _rateLimitCleanupTimer;

        // Connection health monitoring
        private static readonly Timer _healthCheckTimer;

        private readonly ILogger<OverseerHub> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly BrainPersistenceService _brainService;
        private readonly OverseerHubConfig _config;
        private readonly PerformanceMetricsService _performanceMetrics;

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

        /// <summary>
        /// Initializes a new instance of the OverseerHub class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="scopeFactory">The service scope factory.</param>
        /// <param name="brainService">The brain persistence service.</param>
        /// <param name="config">The hub configuration options.</param>
        /// <param name="performanceMetrics">The performance metrics service.</param>
        public OverseerHub(
            ILogger<OverseerHub> logger,
            IServiceScopeFactory scopeFactory,
            BrainPersistenceService brainService,
            IOptions<OverseerHubConfig> config,
            PerformanceMetricsService performanceMetrics)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _brainService = brainService;
            _config = config.Value;
            _performanceMetrics = performanceMetrics;
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
        public Dictionary<string, object> GetPerformanceMetrics()
        {
            var signalRMetrics = _performanceMetrics.GetSignalRMetrics();
            var now = DateTime.UtcNow;
            var timeSinceReset = now - signalRMetrics.LastReset;
            var minutesSinceReset = timeSinceReset.TotalMinutes;

            return new Dictionary<string, object>
            {
                ["TotalMessagesProcessed"] = signalRMetrics.MessagesProcessed,
                ["TotalHandshakeRequests"] = signalRMetrics.HandshakeRequests,
                ["TotalCheckInRequests"] = signalRMetrics.CheckInRequests,
                ["MessagesPerMinute"] = minutesSinceReset > 0 ? signalRMetrics.MessagesProcessed / minutesSinceReset : 0,
                ["HandshakeRequestsPerMinute"] = minutesSinceReset > 0 ? signalRMetrics.HandshakeRequests / minutesSinceReset : 0,
                ["CheckInRequestsPerMinute"] = minutesSinceReset > 0 ? signalRMetrics.CheckInRequests / minutesSinceReset : 0,
                ["AverageHandshakeLatencyMs"] = _config.EnablePerformanceMetrics ? signalRMetrics.AvgHandshakeLatencyMs : 0,
                ["AverageCheckInLatencyMs"] = _config.EnablePerformanceMetrics ? signalRMetrics.AvgCheckInLatencyMs : 0,
                ["AverageMessageLatencyMs"] = _config.EnablePerformanceMetrics ? signalRMetrics.AvgMessageLatencyMs : 0,
                ["CurrentConnectionCount"] = _connectedClients.Count,
                ["LastMetricsReset"] = signalRMetrics.LastReset
            };
        }

        /// <summary>
        /// Resets the performance metrics counters.
        /// </summary>
        public void ResetPerformanceMetrics()
        {
            _performanceMetrics.ResetSignalRMetrics();
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
            var stopwatch = _config.EnablePerformanceMetrics ? Stopwatch.StartNew() : null;

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

            if (_config.EnablePerformanceMetrics)
            {
                _performanceMetrics.RecordSignalRHandshake();
                _performanceMetrics.RecordSignalRMessage();
            }

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

                // Log to database if available and performance metrics are enabled
                if (_config.EnablePerformanceMetrics)
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();

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
            finally
            {
                if (stopwatch != null)
                {
                    stopwatch.Stop();
                    _performanceMetrics.RecordSignalRHandshakeLatency(stopwatch.Elapsed);
                }
            }
        }

        /// <summary>
        /// Processes a check-in request from a connected brain instance.
        /// </summary>
        /// <param name="checkInData">The check-in data containing brain status information.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ProcessCheckIn(CheckInData checkInData)
        {
            var stopwatch = _config.EnablePerformanceMetrics ? Stopwatch.StartNew() : null;

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

            if (_config.EnablePerformanceMetrics)
            {
                _performanceMetrics.RecordSignalRCheckIn();
                _performanceMetrics.RecordSignalRMessage();
            }

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
                if (_config.EnablePerformanceMetrics && !string.IsNullOrEmpty(clientInfo.ClientName))
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
                if (_config.EnablePerformanceMetrics)
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();

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
            finally
            {
                if (stopwatch != null)
                {
                    stopwatch.Stop();
                    _performanceMetrics.RecordSignalRCheckInLatency(stopwatch.Elapsed);
                }
            }
        }

        /// <summary>
        /// Processes performance metrics from a connected brain instance.
        /// </summary>
        /// <param name="performanceMetrics">The performance metrics data containing detailed system performance information.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ProcessPerformanceMetrics(PerformanceMetricsData performanceMetrics)
        {
            var stopwatch = _config.EnablePerformanceMetrics ? Stopwatch.StartNew() : null;

            _logger.LogInformation("PerformanceMetrics received from connection: {ConnectionId}", Context.ConnectionId);

            try
            {
                if (!_clientInfo.TryGetValue(Context.ConnectionId, out var clientInfo))
                {
                    _logger.LogWarning("PerformanceMetrics received from unregistered client: {ConnectionId}", Context.ConnectionId);
                    await Clients.Caller.SendAsync("PerformanceMetricsResponse", new
                    {
                        Success = false,
                        Message = "Client not registered. Please perform handshake first."
                    });
                    return;
                }

                // Validate PerformanceMetricsData
                if (performanceMetrics == null)
                {
                    _logger.LogWarning("PerformanceMetrics received with null data from client: {ConnectionId}", Context.ConnectionId);
                    await Clients.Caller.SendAsync("PerformanceMetricsResponse", new
                    {
                        Success = false,
                        Message = "PerformanceMetrics data is null."
                    });
                    return;
                }

                if (string.IsNullOrEmpty(performanceMetrics.BrainInstanceName))
                {
                    _logger.LogWarning("PerformanceMetrics received with missing BrainInstanceName from client: {ConnectionId}", Context.ConnectionId);
                    await Clients.Caller.SendAsync("PerformanceMetricsResponse", new
                    {
                        Success = false,
                        Message = "BrainInstanceName is required."
                    });
                    return;
                }

                _logger.LogInformation("PerformanceMetrics received from {ClientId} ({ClientName}): Timestamp={Timestamp}",
                    clientInfo.ClientId, clientInfo.ClientName, performanceMetrics.Timestamp);

                try
                {
                    // Store performance metrics in brain persistence
                    await _brainService.UpdatePerformanceMetricsAsync(clientInfo.ClientName, performanceMetrics);
                    _logger.LogInformation("Performance metrics stored for brain: {BrainName}", clientInfo.ClientName);
                }
                catch (Exception brainEx)
                {
                    _logger.LogWarning(brainEx, "Failed to update performance metrics for {ClientName}", clientInfo.ClientName);
                }
                

                // Update client last seen
                clientInfo.LastSeen = DateTime.UtcNow;

                // Update client last seen in database
                if (_config.EnablePerformanceMetrics)
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();

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
                        _logger.LogWarning(dbEx, "Failed to log PerformanceMetrics to database for client: {ClientId}", clientInfo.ClientId);
                    }
                }

                // Broadcast performance metrics update to all connected clients (including web UI)
                var connections = _connectedClients.Count;
                _logger.LogInformation("Broadcasting PerformanceMetricsUpdate for {Brain} to {Count} connections",
                    performanceMetrics.BrainInstanceName, connections);

                await Clients.All.SendAsync("PerformanceMetricsUpdate", performanceMetrics);

                await Clients.All.SendAsync("BroadcastTrace", new
                {
                    kind = "PerformanceMetricsUpdate",
                    brain = performanceMetrics.BrainInstanceName,
                    timestamp = performanceMetrics.Timestamp,
                    serverUtc = DateTime.UtcNow
                });

                await Clients.Caller.SendAsync("PerformanceMetricsResponse", new
                {
                    Success = true,
                    Message = "Performance metrics processed successfully",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PerformanceMetrics from client: {ConnectionId}", Context.ConnectionId);
                await Clients.Caller.SendAsync("PerformanceMetricsResponse", new
                {
                    Success = false,
                    Message = $"PerformanceMetrics processing failed: {ex.Message}"
                });
            }
            finally
            {
                if (stopwatch != null)
                {
                    stopwatch.Stop();
                    _performanceMetrics.RecordSignalRCheckInLatency(stopwatch.Elapsed);
                }
            }
        }


        /// <summary>
        /// Handles incoming performance metrics from brain instances.
        /// Stores the metrics in the brain persistence service for monitoring and analysis.
        /// </summary>
        /// <param name="performanceMetrics">The comprehensive performance metrics data from a brain instance.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task HandlePerformanceMetrics(object performanceMetrics)
        {
            var stopwatch = _config.EnablePerformanceMetrics ? Stopwatch.StartNew() : null;

            try
            {
                // Store the performance metrics in the brain persistence service
                // The brain instance name and other details are embedded in the metrics object
                await _brainService.UpdatePerformanceMetricsAsync("DefaultBrain", performanceMetrics);

                // Broadcast the performance metrics to all connected clients (including web UI)
                var connections = _connectedClients.Count;
                _logger.LogInformation("Broadcasting performance metrics update to {Count} connections", connections);

                await Clients.All.SendAsync("PerformanceMetricsUpdate", performanceMetrics);

                await Clients.All.SendAsync("BroadcastTrace", new
                {
                    kind = "PerformanceMetricsUpdate",
                    timestamp = DateTime.UtcNow,
                    serverUtc = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing performance metrics");
            }
            finally
            {
                if (stopwatch != null)
                {
                    stopwatch.Stop();
                    _performanceMetrics.RecordSignalRMessageLatency(stopwatch.Elapsed);
                }
            }
        }

        /// <summary>
        /// Handles performance metrics updates from brain instances.
        /// Stores the comprehensive performance data for monitoring and display purposes.
        /// </summary>
        /// <param name="brainInstanceName">The name of the brain instance sending the metrics.</param>
        /// <param name="performanceMetrics">The comprehensive performance metrics data.</param>
        /// <param name="timestamp">The timestamp when the metrics were sent.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task HandlePerformanceMetricsUpdate(string brainInstanceName, object performanceMetrics, DateTime timestamp)
        {
            _logger.LogInformation("Received performance metrics update from brain {BrainInstanceName}", brainInstanceName);

            try
            {
                // Store the performance metrics in brain persistence
                await _brainService.UpdatePerformanceMetricsAsync(brainInstanceName, performanceMetrics);

                // Broadcast the performance metrics update to all connected clients (including web UI)
                await Clients.All.SendAsync("PerformanceMetricsUpdate", new
                {
                    BrainInstanceName = brainInstanceName,
                    PerformanceMetrics = performanceMetrics,
                    Timestamp = timestamp
                });

                _logger.LogInformation("Performance metrics update processed and broadcasted for brain {BrainInstanceName}", brainInstanceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing performance metrics update from brain {BrainInstanceName}", brainInstanceName);
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
            var stopwatch = _config.EnablePerformanceMetrics ? Stopwatch.StartNew() : null;

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
            finally
            {
                if (stopwatch != null)
                {
                    stopwatch.Stop();
                    _performanceMetrics.RecordSignalRMessageLatency(stopwatch.Elapsed);
                }
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

    /// <summary>
    /// Data structure containing comprehensive performance metrics from the CentralPerformanceMonitor.
    /// Used for detailed performance monitoring and analytics, including database operations,
    /// WebSocket metrics, queue depths, and system resource utilization.
    /// </summary>
    public class PerformanceMetricsData
    {
        /// <summary>
        /// Gets or sets the name of the brain instance providing the performance metrics.
        /// </summary>
        public string? BrainInstanceName { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when these performance metrics were collected.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the database performance metrics.
        /// </summary>
        public IReadOnlyDictionary<string, (int SuccessCount, int FailureCount, TimeSpan TotalTime, double AverageTimeMs)>? DatabaseMetrics { get; set; }

        /// <summary>
        /// Gets or sets the OverseerClientService performance metrics.
        /// </summary>
        public IReadOnlyDictionary<string, object>? OverseerClientServiceMetrics { get; set; }

        /// <summary>
        /// Gets or sets the WebSocket processing time metrics in ticks.
        /// </summary>
        public ConcurrentDictionary<string, long>? WebSocketProcessingTimeTicks { get; set; }

        /// <summary>
        /// Gets or sets the WebSocket processing count metrics.
        /// </summary>
        public ConcurrentDictionary<string, int>? WebSocketProcessingCount { get; set; }

        /// <summary>
        /// Gets or sets the WebSocket buffer usage metrics in bytes.
        /// </summary>
        public ConcurrentDictionary<string, long>? WebSocketBufferUsageBytes { get; set; }

        /// <summary>
        /// Gets or sets the WebSocket operation times.
        /// </summary>
        public ConcurrentDictionary<string, TimeSpan>? WebSocketOperationTimes { get; set; }

        /// <summary>
        /// Gets or sets the WebSocket semaphore wait counts.
        /// </summary>
        public ConcurrentDictionary<string, int>? WebSocketSemaphoreWaitCount { get; set; }

        /// <summary>
        /// Gets or sets the SubscriptionManager operation metrics.
        /// </summary>
        public IReadOnlyDictionary<string, (long AverageTicks, long TotalOperations, long SuccessfulOperations)>? SubscriptionManagerOperationMetrics { get; set; }

        /// <summary>
        /// Gets or sets the SubscriptionManager lock contention metrics.
        /// </summary>
        public IReadOnlyDictionary<string, (long AcquisitionCount, long AverageWaitTicks, long ContentionCount)>? SubscriptionManagerLockMetrics { get; set; }

        /// <summary>
        /// Gets or sets the MessageProcessor total messages processed.
        /// </summary>
        public long MessageProcessorTotalMessagesProcessed { get; set; }

        /// <summary>
        /// Gets or sets the MessageProcessor total processing time in milliseconds.
        /// </summary>
        public long MessageProcessorTotalProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the MessageProcessor average processing time in milliseconds.
        /// </summary>
        public double MessageProcessorAverageProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the MessageProcessor messages per second rate.
        /// </summary>
        public double MessageProcessorMessagesPerSecond { get; set; }

        /// <summary>
        /// Gets or sets the MessageProcessor order book queue depth.
        /// </summary>
        public int MessageProcessorOrderBookQueueDepth { get; set; }

        /// <summary>
        /// Gets or sets the MessageProcessor duplicate message count.
        /// </summary>
        public int MessageProcessorDuplicateMessageCount { get; set; }

        /// <summary>
        /// Gets or sets the MessageProcessor duplicates in window.
        /// </summary>
        public int MessageProcessorDuplicatesInWindow { get; set; }

        /// <summary>
        /// Gets or sets the MessageProcessor last duplicate warning time.
        /// </summary>
        public DateTime MessageProcessorLastDuplicateWarningTime { get; set; }

        /// <summary>
        /// Gets or sets the MessageProcessor message type counts.
        /// </summary>
        public IReadOnlyDictionary<string, long>? MessageProcessorMessageTypeCounts { get; set; }

        /// <summary>
        /// Gets or sets the API execution times.
        /// </summary>
        public ConcurrentDictionary<string, List<(DateTime Timestamp, long Milliseconds)>>? ApiExecutionTimes { get; set; }

        /// <summary>
        /// Gets or sets the configurable metrics for GUI consumption.
        /// </summary>
        public IReadOnlyDictionary<string, object>? ConfigurableMetrics { get; set; }
    }
}