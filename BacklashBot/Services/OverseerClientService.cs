using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using BacklashBot.Services.Interfaces;
using BacklashBot.Management.Interfaces;
using BacklashDTOs.Configuration;
using BacklashDTOs;
using BacklashInterfaces.SmokehouseBot.Services;

namespace BacklashBot.Services
{
    /// <summary>
    /// Manages the client-side connection to an Overseer server for monitoring and oversight of the trading bot.
    /// This service establishes and maintains a SignalR connection to an Overseer instance, handles periodic check-ins
    /// with system status information, and automatically discovers and switches to better overseer servers when available.
    /// It provides resilience through automatic reconnection, connection validation, and failover mechanisms.
    /// The service integrates with the broader bot ecosystem to report market data, error counts, and performance metrics.
    /// </summary>
    /// <remarks>
    /// The OverseerClientService is designed to be optional - if no overseer is available, the bot continues operating
    /// without oversight. It discovers available overseer instances from the database only, prioritizing production
    /// instances over those named "DevInstance", and selects the most appropriate one based on recency of heartbeat.
    /// </remarks>
    public class OverseerClientService : IOverseerClientService
    {
        // Response classes for SignalR events
        private class HandshakeResponse
        {
            public bool Success { get; set; }
            public string AuthToken { get; set; } = "";
            public string Message { get; set; } = "";
        }

        private class CheckInResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = "";
            public string[] TargetTickers { get; set; } = Array.Empty<string>();
            public DateTime Timestamp { get; set; }
        }

        private class MessageResponse
        {
            public bool Success { get; set; }
            public string MessageType { get; set; } = "";
            public DateTime Timestamp { get; set; }
            public string Message { get; set; } = "";
        }

        private readonly ILogger<OverseerClientService> _logger;
        private readonly IServiceFactory _serviceFactory;
        private HubConnection? _hubConnection;
        private Timer? _checkInTimer;
        private Timer? _overseerDiscoveryTimer;
        private bool _isConnected = false;
        private string _clientId;
        private string _clientName = "BacklashBot";
        private string _clientType = "TradingBot";
        private BacklashDTOs.Data.OverseerInfo? _currentOverseer;
        private string? _currentOverseerUrl;
        private DateTime _lastOverseerDiscovery = DateTime.MinValue;
        private readonly TimeSpan _overseerDiscoveryInterval = TimeSpan.FromMinutes(3); // Check for better overseer every 3 minutes
        private readonly object _connectionStateLock = new object(); // Lock for connection state changes
        private readonly SemaphoreSlim _connectionOperationSemaphore = new SemaphoreSlim(1, 1); // Semaphore for connection operations

        /// <summary>
        /// Initializes a new instance of the OverseerClientService with required dependencies.
        /// </summary>
        /// <param name="logger">The logger instance for recording service operations and errors.</param>
        /// <param name="serviceFactory">Factory for accessing other bot services like market data and error handlers.</param>
        /// <param name="executionConfig">Configuration options including the brain instance name.</param>
        public OverseerClientService(
            ILogger<OverseerClientService> logger,
            IServiceFactory serviceFactory,
            IOptions<ExecutionConfig> executionConfig)
        {
            _logger = logger;
            _serviceFactory = serviceFactory;
            _clientId = Guid.NewGuid().ToString();

            // Read brain instance name from configuration, fallback to hardcoded if not set
            _clientName = executionConfig.Value.BrainInstance ?? "BacklashBot";
            _logger.LogInformation("OVERSEER- Using brain instance name: {BrainInstanceName}", _clientName);
        }

        /// <summary>
        /// Starts the overseer client service, initializing timers for discovery and check-ins,
        /// and attempting an initial connection to an available overseer.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method sets up background timers for periodic overseer discovery (every 3 minutes)
        /// and check-in operations (every 30 seconds). It also attempts an immediate connection
        /// if an overseer URL can be determined from configuration or database.
        /// </remarks>
        public async Task StartAsync()
        {
            try
            {
                _logger.LogInformation("OVERSEER- Starting OverseerClientService...");

                // Start Overseer Discovery timer first (every 3 minutes) - this ensures we keep trying to find overseer
                _overseerDiscoveryTimer = new Timer(async _ => await PerformOverseerDiscoveryAsync(), null, TimeSpan.Zero, _overseerDiscoveryInterval);
                _logger.LogInformation("OVERSEER- Overseer discovery timer started (every {Interval} minutes)", _overseerDiscoveryInterval.TotalMinutes);

                // Start CheckIn timer (every 30 seconds) - it will only send if connected
                _checkInTimer = new Timer(async _ => await SendCheckInAsync(), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
                _logger.LogInformation("OVERSEER- CheckIn timer started (every 30 seconds, first check in 10 seconds)");

                // Find overseer IP from database or configuration
                var overseerUrl = await GetOverseerUrlAsync();

                if (!string.IsNullOrEmpty(overseerUrl))
                {
                    _logger.LogInformation("OVERSEER- Attempting initial connection to overseer at: {Url}", overseerUrl);
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await AttemptConnectionAsync(overseerUrl, "Initial Startup");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("OVERSEER- Failed to connect to overseer. Exception: {0}", ex.Message);
                        }
                    });
                }
                else
                {
                    _logger.LogInformation("OVERSEER- No overseer URL found initially. Discovery will continue in background every 3 minutes.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("OVERSEER- Failed to initialize overseer client. This is optional and the bot will continue without oversight. Exception: {0}, ST:{1}"
                    , ex.Message, ex.StackTrace);
            }
        }

        /// <summary>
        /// Stops the overseer client service, disposing of timers and cleaning up the connection.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method safely disposes of the discovery and check-in timers, resets connection state,
        /// and ensures the SignalR connection is properly closed and disposed.
        /// </remarks>
        public async Task StopAsync()
        {
            try
            {
                if (_checkInTimer != null)
                {
                    await _checkInTimer.DisposeAsync();
                    _checkInTimer = null;
                }

                if (_overseerDiscoveryTimer != null)
                {
                    await _overseerDiscoveryTimer.DisposeAsync();
                    _overseerDiscoveryTimer = null;
                }

                // Use lock to safely clean up connection
                lock (_connectionStateLock)
                {
                    _isConnected = false;
                    _currentOverseer = null;
                    _currentOverseerUrl = null;
                }

                await CleanupConnectionAsync();

                _logger.LogInformation("OVERSEER- Overseer client stopped");
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error stopping overseer client. Error: {0}", ex.Message);
            }
        }

        private async Task CleanupConnectionAsync()
        {
            _logger.LogDebug("OVERSEER- CleanupConnectionAsync ENTERED - checking if semaphore is already held");

            // Check if we already hold the semaphore (called from AttemptConnectionAsync)
            bool semaphoreHeld = _connectionOperationSemaphore.CurrentCount == 0;
            _logger.LogDebug("OVERSEER- Semaphore already held: {Held}", semaphoreHeld);

            if (!semaphoreHeld)
            {
                _logger.LogDebug("OVERSEER- Acquiring cleanup semaphore");
                await _connectionOperationSemaphore.WaitAsync();
                _logger.LogDebug("OVERSEER- Cleanup semaphore acquired");
            }
            else
            {
                _logger.LogDebug("OVERSEER- Skipping semaphore acquisition (already held)");
            }

            try
            {
                if (_hubConnection == null)
                {
                    _logger.LogDebug("OVERSEER- HubConnection is null, nothing to cleanup");
                    return;
                }

                _logger.LogDebug("OVERSEER- HubConnection exists, state: {State}", _hubConnection.State);

                try
                {
                    var currentState = _hubConnection.State;
                    _logger.LogDebug("OVERSEER- Cleaning up HubConnection in state: {State}", currentState);

                    // Stop the connection if it's not already disconnected
                    if (currentState != HubConnectionState.Disconnected)
                    {
                        _logger.LogDebug("OVERSEER- About to stop connection");
                        try
                        {
                            await _hubConnection.StopAsync();
                            _logger.LogDebug("OVERSEER- Stopped existing connection during cleanup");
                        }
                        catch (Exception stopEx)
                        {
                            _logger.LogWarning("OVERSEER- Error stopping connection during cleanup: {Error}", stopEx.Message);
                        }
                    }
                    else
                    {
                        _logger.LogDebug("OVERSEER- Connection already disconnected, skipping stop");
                    }

                    // Always dispose to ensure clean state
                    _logger.LogDebug("OVERSEER- About to dispose connection");
                    await _hubConnection.DisposeAsync();
                    _hubConnection = null;
                    _logger.LogDebug("OVERSEER- Disposed HubConnection during cleanup");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("OVERSEER- Error during connection cleanup: {Error}", ex.Message);
                    // Force null the connection even if disposal failed
                    _hubConnection = null;
                }
            }
            finally
            {
                if (!semaphoreHeld)
                {
                    _connectionOperationSemaphore.Release();
                    _logger.LogDebug("OVERSEER- Cleanup semaphore released");
                }
                else
                {
                    _logger.LogDebug("OVERSEER- Skipping semaphore release (held by caller)");
                }
            }
        }

        private async Task<string?> GetOverseerUrlAsync()
        {
            try
            {
                // Get overseer from database only
                var overseerUrl = await GetOverseerUrlFromDatabaseAsync();
                if (!string.IsNullOrEmpty(overseerUrl))
                {
                    _logger.LogInformation("OVERSEER- Found overseer URL from database: {Url}", overseerUrl);
                    return overseerUrl;
                }

                // No overseer found
                _logger.LogInformation("OVERSEER- No active overseer found in database. Discovery will continue in background.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "OVERSEER- Failed to discover overseer URL");
                return null;
            }
        }


        private async Task<string?> GetOverseerUrlFromDatabaseAsync()
        {
            try
            {
                using var scope = _serviceFactory.GetScopeManager().CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<KalshiBotData.Data.Interfaces.IKalshiBotContext>();

                // Get active overseers from database, ordered by most recent heartbeat
                var activeOverseers = await context.GetActiveOverseerInfos();

                if (activeOverseers.Any())
                {
                    // Prioritize overseers that are NOT called "DevInstance"
                    var prioritizedOverseers = activeOverseers
                        .Where(o => o.HostName != "DevInstance")
                        .OrderByDescending(o => o.LastHeartbeat)
                        .ToList();

                    // If no non-DevInstance overseers, fall back to all active overseers
                    if (!prioritizedOverseers.Any())
                    {
                        prioritizedOverseers = activeOverseers.OrderByDescending(o => o.LastHeartbeat).ToList();
                        _logger.LogInformation("OVERSEER- No production overseers found, using DevInstance as fallback");
                    }

                    var overseer = prioritizedOverseers.First();
                    var url = $"http://{overseer.IPAddress}:{overseer.Port}/chartHub";

                    // Store current overseer information for validation during check-ins
                    _currentOverseer = overseer;
                    _currentOverseerUrl = url;

                    // Check for multiple active overseers
                    if (activeOverseers.Count > 1)
                    {
                        _logger.LogInformation("OVERSEER- Multiple active overseers detected. Prioritized overseers:");
                        foreach (var activeOverseer in prioritizedOverseers)
                        {
                            var isSelected = activeOverseer.Id == overseer.Id ? " (SELECTED)" : "";
                            _logger.LogInformation("OVERSEER-   - {HostName} at {IPAddress}:{Port} (Heartbeat: {Heartbeat}){Selected}",
                                activeOverseer.HostName, activeOverseer.IPAddress, activeOverseer.Port,
                                activeOverseer.LastHeartbeat?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never",
                                isSelected);
                        }
                    }

                    _logger.LogInformation("OVERSEER- Selected overseer from database: {HostName} at {IPAddress}:{Port}",
                        overseer.HostName, overseer.IPAddress, overseer.Port);
                    return url;
                }

                _logger.LogDebug("OVERSEER- No active overseers found in database");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogDebug("OVERSEER- Failed to get overseer URL from database. Error: {Error}", ex.Message);
                return null;
            }
        }


        private async Task AttemptConnectionAsync(string overseerUrl, string reason)
        {
            _logger.LogDebug("OVERSEER- AttemptConnectionAsync ENTERED for {Url}", overseerUrl);
            _logger.LogDebug("OVERSEER- Current semaphore count: {Count}", _connectionOperationSemaphore.CurrentCount);

            // Add timeout to semaphore to prevent hanging
            using var semaphoreTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await _connectionOperationSemaphore.WaitAsync(semaphoreTimeout.Token);
            _logger.LogDebug("OVERSEER- Semaphore acquired successfully");

            try
            {
                _logger.LogInformation("OVERSEER- Attempting connection to overseer at {Url} (Reason: {Reason})", overseerUrl, reason);

                // Clean up existing connection safely
                _logger.LogDebug("OVERSEER- About to cleanup existing connection");
                await CleanupConnectionAsync();
                _logger.LogDebug("OVERSEER- Cleanup completed");

                // Reset connection state
                lock (_connectionStateLock)
                {
                    _isConnected = false;
                    _currentOverseerUrl = null;
                }
                _logger.LogDebug("OVERSEER- Connection state reset");

                // Create new connection
                _logger.LogDebug("OVERSEER- Creating new HubConnection");
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(overseerUrl)
                    .WithAutomaticReconnect()
                    .Build();
                _logger.LogDebug("OVERSEER- HubConnection created, initial state: {State}", _hubConnection.State);

                // Set up event handlers
                _logger.LogDebug("OVERSEER- Setting up event handlers");
                _hubConnection.On<HandshakeResponse>("HandshakeResponse", HandleHandshakeResponse);
                _hubConnection.On<CheckInResponse>("CheckInResponse", HandleCheckInResponse);
                _hubConnection.On<MessageResponse>("MessageResponse", HandleMessageResponse);

                // Handle connection events
                _hubConnection.Closed += HandleConnectionClosed;
                _hubConnection.Reconnected += HandleReconnected;
                _logger.LogDebug("OVERSEER- Event handlers registered");

                // Verify connection is in correct state before starting
                if (_hubConnection.State != HubConnectionState.Disconnected)
                {
                    _logger.LogWarning("OVERSEER- HubConnection is in {State} state, cannot start. Creating fresh connection.", _hubConnection.State);
                    await CleanupConnectionAsync();

                    _hubConnection = new HubConnectionBuilder()
                        .WithUrl(overseerUrl)
                        .WithAutomaticReconnect()
                        .Build();

                    // Re-setup event handlers
                    _hubConnection.On<HandshakeResponse>("HandshakeResponse", HandleHandshakeResponse);
                    _hubConnection.On<CheckInResponse>("CheckInResponse", HandleCheckInResponse);
                    _hubConnection.On<MessageResponse>("MessageResponse", HandleMessageResponse);
                    _hubConnection.Closed += HandleConnectionClosed;
                    _hubConnection.Reconnected += HandleReconnected;
                    _logger.LogDebug("OVERSEER- Fresh connection created");
                }

                _logger.LogDebug("OVERSEER- About to call StartAsync with 30s timeout");
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await _hubConnection.StartAsync(cts.Token);
                _logger.LogInformation("OVERSEER- StartAsync completed successfully, state: {State}", _hubConnection.State);

                lock (_connectionStateLock)
                {
                    _isConnected = true;
                    _currentOverseerUrl = overseerUrl;
                }
                _logger.LogInformation("OVERSEER- Successfully connected to overseer at {Url}", overseerUrl);

                // Perform handshake
                _logger.LogDebug("OVERSEER- About to perform handshake");
                await PerformHandshakeAsync();
                _logger.LogDebug("OVERSEER- Handshake completed");
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("OVERSEER- Connection attempt timed out after 30 seconds");
            }
            catch (Exception ex)
            {
                _logger.LogWarning("OVERSEER- Exception in AttemptConnectionAsync: {Message}", ex.Message);
                _logger.LogWarning("OVERSEER- Stack trace: {StackTrace}", ex.StackTrace);
            }
            finally
            {
                _connectionOperationSemaphore.Release();
                _logger.LogDebug("OVERSEER- Semaphore released");
            }
        }


        private async Task ValidateCurrentOverseerAsync()
        {
            if (_currentOverseer == null || string.IsNullOrEmpty(_currentOverseerUrl))
            {
                return; // No current overseer to validate
            }

            try
            {
                using var scope = _serviceFactory.GetScopeManager().CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<KalshiBotData.Data.Interfaces.IKalshiBotContext>();

                // Get the most recent active overseer
                var activeOverseers = await context.GetActiveOverseerInfos();

                if (!activeOverseers.Any())
                {
                    _logger.LogDebug("OVERSEER- No active overseers found in database during validation - this is normal in testing mode");
                    return;
                }

                var mostRecentOverseer = activeOverseers.First();

                // Check if we're still connected to the most recent overseer
                if (_currentOverseer.Id != mostRecentOverseer.Id)
                {
                    _logger.LogInformation("OVERSEER- Found newer overseer available. Current: {CurrentHost} ({CurrentHeartbeat}), Available: {RecentHost} ({RecentHeartbeat})",
                        _currentOverseer.HostName,
                        _currentOverseer.LastHeartbeat?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never",
                        mostRecentOverseer.HostName,
                        mostRecentOverseer.LastHeartbeat?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never");

                    // Attempt to switch to the better overseer
                    await SwitchToBetterOverseerAsync(mostRecentOverseer);
                }

                // Check for multiple active overseers (this shouldn't happen)
                if (activeOverseers.Count > 1)
                {
                    _logger.LogInformation("OVERSEER- Multiple active overseers detected during validation:");
                    foreach (var overseer in activeOverseers)
                    {
                        var isCurrent = overseer.Id == _currentOverseer.Id ? " (CURRENT)" : "";
                        _logger.LogInformation("OVERSEER-   - {HostName} at {IPAddress}:{Port} (Heartbeat: {Heartbeat}){CurrentFlag}",
                            overseer.HostName, overseer.IPAddress, overseer.Port,
                            overseer.LastHeartbeat?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never",
                            isCurrent);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("OVERSEER- Failed to validate current overseer connection - this is normal in testing mode. Error: {Error}", ex.Message);
            }
        }

        private async Task PerformOverseerDiscoveryAsync()
        {
            try
            {
                _logger.LogDebug("OVERSEER- Performing overseer discovery cycle...");

                // Skip if we recently performed discovery
                if (DateTime.UtcNow - _lastOverseerDiscovery < _overseerDiscoveryInterval)
                {
                    _logger.LogDebug("OVERSEER- Skipping discovery - too soon since last attempt ({Seconds} seconds ago)",
                        (DateTime.UtcNow - _lastOverseerDiscovery).TotalSeconds);
                    return;
                }

                _lastOverseerDiscovery = DateTime.UtcNow;

                // If we're not connected, try to find any overseer
                if (!_isConnected || _currentOverseer == null)
                {
                    _logger.LogInformation("OVERSEER- No overseer connection detected, attempting to discover and connect to available overseer");
                    await DiscoverAndConnectToBestOverseerAsync();
                    return;
                }

                // If we're connected, check for better overseers
                _logger.LogDebug("OVERSEER- Validating current overseer connection...");
                await ValidateCurrentOverseerAsync();

            }
            catch (Exception ex)
            {
                _logger.LogWarning("OVERSEER- Overseer discovery failed - this is normal and will be retried on next cycle. Error: {Error}", ex.Message);
            }
        }

        private async Task DiscoverAndConnectToBestOverseerAsync()
        {
            try
            {
                _logger.LogDebug("OVERSEER- Starting overseer discovery process...");

                // Try database only
                var databaseUrl = await GetOverseerUrlFromDatabaseAsync();
                if (!string.IsNullOrEmpty(databaseUrl))
                {
                    _logger.LogInformation("OVERSEER- Found overseer in database: {Url}, attempting connection", databaseUrl);
                    await AttemptConnectionAsync(databaseUrl, "Database Discovery");
                    return;
                }

                // No overseer found - this is normal and will be retried on next discovery cycle
                _logger.LogDebug("OVERSEER- Overseer discovery completed - no active overseer found in database, will retry on next cycle");
            }
            catch (Exception ex)
            {
                _logger.LogWarning("OVERSEER- Failed during overseer discovery process. Will retry on next cycle. Error: {Error}", ex.Message);
            }
        }

        private async Task SwitchToBetterOverseerAsync(BacklashDTOs.Data.OverseerInfo newOverseer)
        {
            try
            {
                var newUrl = $"http://{newOverseer.IPAddress}:{newOverseer.Port}/chartHub";

                // Don't switch if we're already connected to this overseer
                if (_currentOverseerUrl == newUrl)
                {
                    return;
                }

                _logger.LogInformation("OVERSEER- Switching to better overseer: {HostName} at {IPAddress}:{Port}",
                    newOverseer.HostName, newOverseer.IPAddress, newOverseer.Port);

                await SwitchToOverseerUrlAsync(newUrl, $"Better Overseer: {newOverseer.HostName}");
            }
            catch (Exception ex)
            {
                _logger.LogDebug("OVERSEER- Failed to switch to better overseer - will retry on next discovery cycle. Error: {Error}", ex.Message);
            }
        }

        private async Task SwitchToOverseerUrlAsync(string newUrl, string reason)
        {
            await AttemptConnectionAsync(newUrl, reason);
        }

        private bool IsConnectionActive()
        {
            return _hubConnection != null &&
                   _isConnected &&
                   _hubConnection.State == HubConnectionState.Connected;
        }

        private async Task PerformHandshakeAsync()
        {
            await PerformHandshakeAsyncInternal();
        }

        private async Task PerformHandshakeAsyncInternal()
        {
            if (_hubConnection == null)
            {
                _logger.LogWarning("OVERSEER- Cannot send handshake: HubConnection is null");
                return;
            }

            _logger.LogDebug("OVERSEER- Checking connection state for handshake: IsConnected={IsConnected}, State={State}",
                _isConnected, _hubConnection.State);

            if (!IsConnectionActive())
            {
                _logger.LogWarning("OVERSEER- Cannot send handshake: Connection not active (IsConnected={IsConnected}, State={State})",
                    _isConnected, _hubConnection.State);
                return;
            }

            try
            {
                _logger.LogInformation("OVERSEER- Sending handshake with ClientId={ClientId}, ClientName={ClientName}, ClientType={ClientType}",
                    _clientId, _clientName, _clientType);
                await _hubConnection.InvokeAsync("Handshake", _clientId, _clientName, _clientType);
                _logger.LogInformation("OVERSEER- Handshake sent successfully to overseer");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("cannot be called if the connection is not active"))
            {
                _logger.LogWarning("OVERSEER- Connection became inactive during handshake attempt. Will mark as disconnected. Error: {Error}", ex.Message);
                lock (_connectionStateLock)
                {
                    _isConnected = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("OVERSEER- Failed to send handshake. Error: {Error}", ex.Message);
            }
        }

        private async Task SendCheckInAsync()
        {
            if (!IsConnectionActive() || _hubConnection == null)
            {
                _logger.LogDebug("OVERSEER- Overseer not connected or connection not active, skipping CheckIn (will retry on next cycle)");
                return;
            }

            try
            {
                _logger.LogDebug("OVERSEER- Preparing CheckIn data...");

                // Validate that we're still connected to the most recent overseer
                await ValidateCurrentOverseerAsync();

                var markets = await _serviceFactory.GetMarketDataService().FetchWatchedMarketsAsync();
                var errorHandler = _serviceFactory.GetBacklashErrorHandler();

                var checkInData = new
                {
                    BrainInstanceName = _clientName, // Add the brain instance name
                    Markets = markets,
                    ErrorCount = errorHandler.ErrorCount,
                    LastSnapshot = errorHandler.LastSuccessfulSnapshot == DateTime.MinValue
                        ? (DateTime?)null
                        : errorHandler.LastSuccessfulSnapshot,
                    LastErrorDate = errorHandler.LastErrorDate == DateTime.MinValue
                        ? (DateTime?)null
                        : errorHandler.LastErrorDate,
                    IsStartingUp = false, // Could be determined from service state
                    IsShuttingDown = false,
                    WatchPositions = true, // From configuration
                    WatchOrders = true,
                    ManagedWatchList = true,
                    CaptureSnapshots = false,
                    TargetWatches = 200, // From configuration
                    MinimumInterest = 5.0,
                    UsageMin = 70.0,
                    UsageMax = 90.0,
                    CurrentCpuUsage = 0.0, // Could get from system
                    EventQueueAvg = 0.0,
                    TickerQueueAvg = 0.0,
                    NotificationQueueAvg = 0.0,
                    OrderbookQueueAvg = 0.0,
                    LastRefreshCycleSeconds = 0.0,
                    LastRefreshCycleInterval = 0,
                    LastRefreshMarketCount = 0,
                    LastRefreshUsagePercentage = 0.0,
                    LastRefreshTimeAcceptable = true,
                    LastPerformanceSampleDate = (DateTime?)null,
                    IsWebSocketConnected = true // Could be determined
                };

                _logger.LogDebug("OVERSEER- Sending CheckIn to overseer: {MarketCount} markets, ErrorCount: {ErrorCount}, LastSnapshot: {LastSnapshot}",
                    markets.Count, errorHandler.ErrorCount, checkInData.LastSnapshot);

                await _hubConnection.InvokeAsync("CheckIn", checkInData);
                _logger.LogInformation("OVERSEER- CheckIn sent successfully to overseer at {Url}", _currentOverseerUrl);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("cannot be called if the connection is not active"))
            {
                _logger.LogWarning("OVERSEER- Connection became inactive during CheckIn attempt. Will mark as disconnected and retry. Error: {Error}", ex.Message);
                lock (_connectionStateLock)
                {
                    _isConnected = false; // Mark as disconnected so discovery can try to reconnect
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("OVERSEER- Failed to send CheckIn to overseer. Connection may have been lost - will retry. Error: {Error}", ex.Message);
                lock (_connectionStateLock)
                {
                    _isConnected = false; // Mark as disconnected so discovery can try to reconnect
                }
            }
        }

        private void HandleHandshakeResponse(HandshakeResponse response)
        {
            _logger.LogInformation("OVERSEER- Handshake response received from overseer: Success={Success}, Message={Message}", response.Success, response.Message);
        }

        private void HandleCheckInResponse(CheckInResponse response)
        {
            _logger.LogInformation("OVERSEER- CheckIn response received from overseer: Success={Success}, Message={Message}, TargetTickers={Count}", response.Success, response.Message, response.TargetTickers.Length);
        }

        private void HandleMessageResponse(MessageResponse response)
        {
            _logger.LogInformation("OVERSEER- Message response received from overseer: Success={Success}, MessageType={Type}, Message={Message}", response.Success, response.MessageType, response.Message);
        }

        private Task HandleConnectionClosed(Exception? exception)
        {
            lock (_connectionStateLock)
            {
                _isConnected = false;
            }

            if (exception != null)
            {
                _logger.LogWarning("OVERSEER- Connection to overseer closed unexpectedly. Automatic reconnection will be attempted. Error: {Error}", exception.Message);
            }
            else
            {
                _logger.LogInformation("OVERSEER- Connection to overseer closed gracefully");
            }

            return Task.CompletedTask;
        }

        private Task HandleReconnected(string? connectionId)
        {
            lock (_connectionStateLock)
            {
                _isConnected = true;
            }

            _logger.LogInformation("OVERSEER- Successfully reconnected to overseer with connection ID: {ConnectionId}", connectionId);

            // Re-perform handshake on reconnection
            _ = Task.Run(async () =>
            {
                try
                {
                    await PerformHandshakeAsync();
                    _logger.LogInformation("OVERSEER- Handshake completed after reconnection");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("OVERSEER- Failed to perform handshake after reconnection. Error: {Error}", ex.Message);
                }
            });

            return Task.CompletedTask;
        }
    }
}