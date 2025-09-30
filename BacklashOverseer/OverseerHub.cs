using BacklashBotData.Data.Interfaces;
using BacklashDTOs.Data;
using BacklashInterfaces.PerformanceMetrics;
using BacklashOverseer.Config;
using BacklashOverseer.Models;
using BacklashOverseer.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OverseerBotShared;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace BacklashOverseer
{

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
        private static readonly ConcurrentDictionary<string, double> _brainPortfolioValues = new();

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
        private readonly IPerformanceMonitor _performanceMonitor;

        // Local metric tracking
        private long _handshakeRequests;
        private long _checkInRequests;
        private long _signalrMessages;
        private double _handshakeLatencySum;
        private double _checkInLatencySum;
        private double _messageLatencySum;
        private int _handshakeLatencyCount;
        private int _checkInLatencyCount;
        private int _messageLatencyCount;
        private DateTime _lastMetricsReset = DateTime.UtcNow;

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
        /// <param name="performanceMonitor">The performance monitor interface.</param>
        public OverseerHub(
            ILogger<OverseerHub> logger,
            IServiceScopeFactory scopeFactory,
            BrainPersistenceService brainService,
            IOptions<OverseerHubConfig> config,
            IPerformanceMonitor performanceMonitor)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _brainService = brainService;
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config), "OverseerHubConfig is required");
            _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
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

            var ipAddress = "unknown";
            try
            {
                var httpContext = Context.GetHttpContext();
                if (httpContext != null)
                {
                    ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                }
            }
            catch (NullReferenceException)
            {
                // Handle case in unit tests where HttpContext is not available
            }

            _logger.LogInformation("OVERSEER- Client connected: ConnectionId={ConnectionId}, IP={IPAddress}",
                Context.ConnectionId, ipAddress);

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
        /// Sets client info for testing purposes.
        /// </summary>
        /// <param name="connectionId">The connection ID.</param>
        /// <param name="clientInfo">The client info.</param>
        public static void SetClientInfoForTesting(string connectionId, ClientInfo clientInfo)
        {
            _clientInfo[connectionId] = clientInfo;
        }

        /// <summary>
        /// Gets the current performance metrics for the hub.
        /// </summary>
        /// <returns>A dictionary containing performance metrics.</returns>
        public Dictionary<string, object> GetPerformanceMetrics()
        {
            var now = DateTime.UtcNow;
            var timeSinceReset = now - _lastMetricsReset;
            var minutesSinceReset = timeSinceReset.TotalMinutes;

            return new Dictionary<string, object>
            {
                ["TotalMessagesProcessed"] = _signalrMessages,
                ["TotalHandshakeRequests"] = _handshakeRequests,
                ["TotalCheckInRequests"] = _checkInRequests,
                ["MessagesPerMinute"] = minutesSinceReset > 0 ? _signalrMessages / minutesSinceReset : 0,
                ["HandshakeRequestsPerMinute"] = minutesSinceReset > 0 ? _handshakeRequests / minutesSinceReset : 0,
                ["CheckInRequestsPerMinute"] = minutesSinceReset > 0 ? _checkInRequests / minutesSinceReset : 0,
                ["AverageHandshakeLatencyMs"] = _config.EnablePerformanceMetrics && _handshakeLatencyCount > 0 ? _handshakeLatencySum / _handshakeLatencyCount : 0,
                ["AverageCheckInLatencyMs"] = _config.EnablePerformanceMetrics && _checkInLatencyCount > 0 ? _checkInLatencySum / _checkInLatencyCount : 0,
                ["AverageMessageLatencyMs"] = _config.EnablePerformanceMetrics && _messageLatencyCount > 0 ? _messageLatencySum / _messageLatencyCount : 0,
                ["CurrentConnectionCount"] = _connectedClients.Count,
                ["LastMetricsReset"] = _lastMetricsReset
            };
        }

        /// <summary>
        /// Resets the performance metrics counters.
        /// </summary>
        public void ResetPerformanceMetrics()
        {
            _handshakeRequests = 0;
            _checkInRequests = 0;
            _signalrMessages = 0;
            _handshakeLatencySum = 0;
            _checkInLatencySum = 0;
            _messageLatencySum = 0;
            _handshakeLatencyCount = 0;
            _checkInLatencyCount = 0;
            _messageLatencyCount = 0;
            _lastMetricsReset = DateTime.UtcNow;
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
        /// Processes a check-in request from a connected brain instance.
        /// </summary>
        /// <param name="checkInData">The check-in data containing brain status information.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task CheckIn(CheckInData checkInData)
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

            _checkInRequests++;
            _signalrMessages++;
            if (_config.EnablePerformanceMetrics)
            {
                _performanceMonitor.RecordCounterMetric("OverseerHub", "checkin_requests", "CheckIn Requests", "Total number of check-in requests", _checkInRequests, "count", "SignalR");
                _performanceMonitor.RecordCounterMetric("OverseerHub", "signalr_messages", "SignalR Messages", "Total number of SignalR messages", _signalrMessages, "count", "SignalR");
            }
            else
            {
                _performanceMonitor.RecordDisabledMetric("OverseerHub", "checkin_requests", "CheckIn Requests", "Total number of check-in requests", _checkInRequests, "count", "SignalR");
                _performanceMonitor.RecordDisabledMetric("OverseerHub", "signalr_messages", "SignalR Messages", "Total number of SignalR messages", _signalrMessages, "count", "SignalR");
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
                        _logger.LogWarning(brainEx, "Failed to update brain service for {ClientName}. Exception: {Message}, Inner: {Inner}", clientInfo.ClientName, brainEx.Message, brainEx.InnerException?.Message ?? "None");
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
                        _logger.LogWarning(tickerEx, "Failed to get target tickers for {ClientName}. Exception: {Message}, Inner: {Inner}", clientInfo.ClientName, tickerEx.Message, tickerEx.InnerException?.Message ?? "None");
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
                        _logger.LogWarning(dbEx, "Failed to log CheckIn to database for client: {ClientId}. Exception: {Message}, Inner: {Inner}", clientInfo.ClientId, dbEx.Message, dbEx.InnerException?.Message ?? "None");
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

                // Update BrainPersistence with status values from checkInData
                if (existingBrain != null)
                {
                    existingBrain.IsStartingUp = checkInData.IsStartingUp;
                    existingBrain.IsShuttingDown = checkInData.IsShuttingDown;
                    existingBrain.ErrorCount = checkInData.ErrorCount;
                    existingBrain.LastSnapshot = checkInData.LastSnapshot;
                    existingBrain.IsWebSocketConnected = checkInData.IsWebSocketConnected;
                    // The performance metrics are already updated via UpdateMetricHistoryAsync
                }

                // Get brain instance for TargetWatches
                BrainInstanceDTO? brainInstanceDto = null;
                if (!string.IsNullOrEmpty(clientInfo.ClientName))
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();
                        brainInstanceDto = await context.GetBrainInstanceByName(clientInfo.ClientName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get BrainInstance for {ClientName}", clientInfo.ClientName);
                    }
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
                    IsStartingUp = existingBrain?.IsStartingUp ?? false,
                    IsShuttingDown = existingBrain?.IsShuttingDown ?? false,

                    // Brain configuration - sourced from database, not from client data
                    WatchPositions = brainInstanceDto?.WatchPositions ?? false,
                    WatchOrders = brainInstanceDto?.WatchOrders ?? false,
                    ManagedWatchList = brainInstanceDto?.ManagedWatchList ?? false,
                    CaptureSnapshots = brainInstanceDto?.CaptureSnapshots ?? false,
                    TargetWatches = brainInstanceDto?.TargetWatches ?? 0,
                    MinimumInterest = brainInstanceDto?.MinimumInterest ?? 0.0,
                    UsageMin = brainInstanceDto?.UsageMin ?? 0.0,
                    UsageMax = brainInstanceDto?.UsageMax ?? 0.0,

                    // Performance metrics - sourced from checkin data
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
                    IsWebSocketConnected = existingBrain?.IsWebSocketConnected ?? true,

                    // Market watch data
                    WatchedMarkets = null
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

                // Update and broadcast total portfolio value
                _brainPortfolioValues[clientInfo.ClientName ?? ""] = checkInData.PortfolioValue;
                double totalPortfolio = _brainPortfolioValues.Values.Sum();
                await Clients.All.SendAsync("PortfolioUpdate", new { TotalPortfolio = totalPortfolio, Timestamp = DateTime.UtcNow });

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
                    _checkInLatencySum += stopwatch.Elapsed.TotalMilliseconds;
                    _checkInLatencyCount++;
                    if (_config.EnablePerformanceMetrics)
                    {
                        _performanceMonitor.RecordSpeedDialMetric("OverseerHub", "checkin_latency", "CheckIn Latency", "Latency for check-in processing", stopwatch.Elapsed.TotalMilliseconds, "ms", "SignalR", null, null, null);
                    }
                    else
                    {
                        _performanceMonitor.RecordDisabledMetric("OverseerHub", "checkin_latency", "CheckIn Latency", "Latency for check-in processing", stopwatch.Elapsed.TotalMilliseconds, "ms", "SignalR");
                    }
                }
            }
        }




        /// <summary>
        /// Processes performance metrics from a connected brain instance.
        /// </summary>
        /// <param name="performanceMetrics">The performance metrics data containing detailed system performance information.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SendPerformanceMetrics(PerformanceMetricsData performanceMetrics)
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
                    _logger.LogWarning(brainEx, "Failed to update performance metrics for {ClientName}. Exception: {Message}, Inner: {Inner}", clientInfo.ClientName, brainEx.Message, brainEx.InnerException?.Message ?? "None");
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
                        _logger.LogWarning(dbEx, "Failed to log PerformanceMetrics to database for client: {ClientId}. Exception: {Message}, Inner: {Inner}", clientInfo.ClientId, dbEx.Message, dbEx.InnerException?.Message ?? "None");
                    }
                }

                // Performance metrics stored successfully - no broadcasting needed
                _logger.LogInformation("Performance metrics received and stored for brain: {BrainName}",
                    performanceMetrics.BrainInstanceName);

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
                    _messageLatencySum += stopwatch.Elapsed.TotalMilliseconds;
                    _messageLatencyCount++;
                    if (_config.EnablePerformanceMetrics)
                    {
                        _performanceMonitor.RecordSpeedDialMetric("OverseerHub", "message_latency", "Message Latency", "Latency for message processing", stopwatch.Elapsed.TotalMilliseconds, "ms", "SignalR", null, null, null);
                    }
                    else
                    {
                        _performanceMonitor.RecordDisabledMetric("OverseerHub", "message_latency", "Message Latency", "Latency for message processing", stopwatch.Elapsed.TotalMilliseconds, "ms", "SignalR");
                    }
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
                string brainName = "DefaultBrain";
                if (performanceMetrics is PerformanceMetricsData pmData && !string.IsNullOrEmpty(pmData.BrainInstanceName))
                {
                    brainName = pmData.BrainInstanceName;
                }
                // Store the performance metrics in the brain persistence service
                await _brainService.UpdatePerformanceMetricsAsync(brainName, performanceMetrics);

                _logger.LogInformation("Performance metrics received and stored for brain: {BrainName}", brainName);
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
                    _messageLatencySum += stopwatch.Elapsed.TotalMilliseconds;
                    _messageLatencyCount++;
                    if (_config.EnablePerformanceMetrics)
                    {
                        _performanceMonitor.RecordSpeedDialMetric("OverseerHub", "message_latency", "Message Latency", "Latency for message processing", stopwatch.Elapsed.TotalMilliseconds, "ms", "SignalR", null, null, null);
                    }
                    else
                    {
                        _performanceMonitor.RecordDisabledMetric("OverseerHub", "message_latency", "Message Latency", "Latency for message processing", stopwatch.Elapsed.TotalMilliseconds, "ms", "SignalR");
                    }
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

                _logger.LogInformation("Performance metrics update processed and stored for brain {BrainInstanceName}", brainInstanceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing performance metrics update from brain {BrainInstanceName}", brainInstanceName);
            }
        }

        /// <summary>
        /// Processes initial handshake with a connecting client, validates credentials,
        /// registers the client, and generates an authentication token for subsequent communications.
        /// </summary>
        /// <param name="request">The handshake request containing client identification information.</param>
        /// <returns>A task representing the asynchronous operation that returns a handshake response.</returns>
        public async Task<HandshakeResponse> Handshake(HandshakeRequest request)
        {
            var stopwatch = _config.EnablePerformanceMetrics ? Stopwatch.StartNew() : null;

            // Rate limiting check
            var connectionId = Context.ConnectionId;
            if (IsRateLimited(connectionId, "handshake", _config.MaxHandshakeRequestsPerMinute))
            {
                _logger.LogWarning("Handshake rate limit exceeded for connection: {ConnectionId}", connectionId);
                return new HandshakeResponse
                {
                    Success = false,
                    Message = "Rate limit exceeded. Please try again later."
                };
            }

            _handshakeRequests++;
            _signalrMessages++;
            if (_config.EnablePerformanceMetrics)
            {
                _performanceMonitor.RecordCounterMetric("OverseerHub", "handshake_requests", "Handshake Requests", "Total number of handshake requests", _handshakeRequests, "count", "SignalR");
                _performanceMonitor.RecordCounterMetric("OverseerHub", "signalr_messages", "SignalR Messages", "Total number of SignalR messages", _signalrMessages, "count", "SignalR");
            }
            else
            {
                _performanceMonitor.RecordDisabledMetric("OverseerHub", "handshake_requests", "Handshake Requests", "Total number of handshake requests", _handshakeRequests, "count", "SignalR");
                _performanceMonitor.RecordDisabledMetric("OverseerHub", "signalr_messages", "SignalR Messages", "Total number of SignalR messages", _signalrMessages, "count", "SignalR");
            }

            _logger.LogInformation("Handshake received from connection: {ConnectionId}", connectionId);

            try
            {
                // Validate HandshakeRequest
                if (request == null)
                {
                    _logger.LogWarning("Handshake received with null request from connection: {ConnectionId}", connectionId);
                    return new HandshakeResponse
                    {
                        Success = false,
                        Message = "Handshake request is null."
                    };
                }

                if (string.IsNullOrEmpty(request.ClientId))
                {
                    _logger.LogWarning("Handshake received with missing ClientId from connection: {ConnectionId}", connectionId);
                    return new HandshakeResponse
                    {
                        Success = false,
                        Message = "ClientId is required."
                    };
                }

                if (string.IsNullOrEmpty(request.ClientName))
                {
                    _logger.LogWarning("Handshake received with missing ClientName from connection: {ConnectionId}", connectionId);
                    return new HandshakeResponse
                    {
                        Success = false,
                        Message = "ClientName is required."
                    };
                }

                if (string.IsNullOrEmpty(request.ClientType))
                {
                    _logger.LogWarning("Handshake received with missing ClientType from connection: {ConnectionId}", connectionId);
                    return new HandshakeResponse
                    {
                        Success = false,
                        Message = "ClientType is required."
                    };
                }

                // Get IP address
                var ipAddress = "unknown";
                try
                {
                    var httpContext = Context.GetHttpContext();
                    if (httpContext != null)
                    {
                        ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    }
                }
                catch (NullReferenceException)
                {
                    // Handle case in unit tests where HttpContext is not available
                }

                // Generate authentication token
                var authToken = GenerateAuthToken(request.ClientId, request.ClientName);

                // Store client information
                var clientInfo = new ClientInfo
                {
                    ClientId = request.ClientId,
                    ClientName = request.ClientName,
                    ClientType = request.ClientType,
                    IPAddress = ipAddress,
                    ConnectionId = connectionId,
                    LastSeen = DateTime.UtcNow
                };

                _clientInfo[connectionId] = clientInfo;

                _logger.LogInformation("Handshake successful for client {ClientId} ({ClientName}) from IP {IPAddress}",
                    request.ClientId, request.ClientName, ipAddress);

                // Send handshake response to client
                await Clients.Caller.SendAsync("HandshakeResponse", new HandshakeResponse
                {
                    Success = true,
                    AuthToken = authToken,
                    Message = "Handshake successful. Client registered."
                });

                return new HandshakeResponse
                {
                    Success = true,
                    AuthToken = authToken,
                    Message = "Handshake successful. Client registered."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Handshake from connection: {ConnectionId}", connectionId);
                return new HandshakeResponse
                {
                    Success = false,
                    Message = $"Handshake processing failed: {ex.Message}"
                };
            }
            finally
            {
                if (stopwatch != null)
                {
                    stopwatch.Stop();
                    _handshakeLatencySum += stopwatch.Elapsed.TotalMilliseconds;
                    _handshakeLatencyCount++;
                    if (_config.EnablePerformanceMetrics)
                    {
                        _performanceMonitor.RecordSpeedDialMetric("OverseerHub", "handshake_latency", "Handshake Latency", "Latency for handshake processing", stopwatch.Elapsed.TotalMilliseconds, "ms", "SignalR", null, null, null);
                    }
                    else
                    {
                        _performanceMonitor.RecordDisabledMetric("OverseerHub", "handshake_latency", "Handshake Latency", "Latency for handshake processing", stopwatch.Elapsed.TotalMilliseconds, "ms", "SignalR");
                    }
                }
            }
        }

        /// <summary>
        /// Processes messages from overseer clients, handling different message types
        /// such as data refresh requests.
        /// </summary>
        /// <param name="messageType">The type of message being sent (e.g., "refresh_data").</param>
        /// <param name="message">The message content or payload.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SendOverseerMessage(string messageType, string message)
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
                    _messageLatencySum += stopwatch.Elapsed.TotalMilliseconds;
                    _messageLatencyCount++;
                    if (_config.EnablePerformanceMetrics)
                    {
                        _performanceMonitor.RecordSpeedDialMetric("OverseerHub", "message_latency", "Message Latency", "Latency for message processing", stopwatch.Elapsed.TotalMilliseconds, "ms", "SignalR", null, null, null);
                    }
                    else
                    {
                        _performanceMonitor.RecordDisabledMetric("OverseerHub", "message_latency", "Message Latency", "Latency for message processing", stopwatch.Elapsed.TotalMilliseconds, "ms", "SignalR");
                    }
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

}
