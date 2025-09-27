using BacklashBot.Configuration;
using BacklashBot.Management.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashCommon.Configuration;
using BacklashDTOs;
using BacklashInterfaces.SmokehouseBot.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using OverseerBotShared;
using System.Diagnostics;
using System.Net.Http;
using BacklashInterfaces.PerformanceMetrics;

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

        private readonly ILogger<OverseerClientService> _logger;
        private readonly IServiceFactory _serviceFactory;
        private readonly ICentralPerformanceMonitor _centralPerformanceMonitor;
        private HubConnection? _hubConnection;
        private Timer? _checkInTimer;
        private Timer? _overseerDiscoveryTimer;
        private bool _isConnected = false;
        private bool _handshakeCompleted = false;

        /// <summary>
        /// Gets a value indicating whether the service is currently connected to an Overseer.
        /// </summary>
        public bool IsConnected => _isConnected;
        private string _clientId;
        private string _clientName = "BacklashBot";
        private string _clientType = "TradingBot";
        private string? _authToken; // For authentication
        private DateTime _authTokenExpiry = DateTime.MinValue; // Token expiry
        private BacklashDTOs.Data.OverseerInfo? _currentOverseer;
        private string? _currentOverseerUrl;
        private DateTime _lastOverseerDiscovery = DateTime.MinValue;
        private readonly TimeSpan _overseerDiscoveryInterval; // Configurable discovery interval
        private readonly TimeSpan _checkInInterval; // Configurable check-in interval
        private readonly TimeSpan _connectionTimeout; // Configurable connection timeout
        private readonly TimeSpan _semaphoreTimeout; // Configurable semaphore timeout
        private readonly int _circuitBreakerFailureThreshold; // Configurable circuit breaker threshold
        private readonly TimeSpan _circuitBreakerTimeout; // Configurable circuit breaker timeout
        private readonly object _connectionStateLock = new object(); // Lock for connection state changes
        private readonly SemaphoreSlim _connectionOperationSemaphore = new SemaphoreSlim(1, 1); // Semaphore for connection operations
        private int _circuitBreakerFailureCount = 0; // Current failure count for circuit breaker
        private DateTime _circuitBreakerLastFailure = DateTime.MinValue; // Last failure time
        private readonly object _circuitBreakerLock = new object(); // Lock for circuit breaker state
        private readonly bool _enablePerformanceMetrics; // Whether to collect performance metrics

        // Performance metrics
        private int _connectionAttemptCount = 0; // Total connection attempts
        private int _connectionSuccessCount = 0; // Successful connections
        private TimeSpan _totalDiscoveryTime = TimeSpan.Zero; // Total time spent on discovery
        private int _discoveryOperationCount = 0; // Number of discovery operations

        // Latency metrics
        private TimeSpan _totalHandshakeTime = TimeSpan.Zero;
        private int _handshakeCount = 0;
        private TimeSpan _totalCheckInTime = TimeSpan.Zero;
        private int _checkInCount = 0;
        private TimeSpan _totalConnectionAttemptTime = TimeSpan.Zero;
        private int _connectionAttemptTimeCount = 0;

        // Throughput metrics
        private int _messagesSent = 0;
        private int _messagesReceived = 0;
        private DateTime _lastThroughputReset = DateTime.UtcNow;

        // Error granularity
        private int _timeoutFailureCount = 0;
        private int _authFailureCount = 0;
        private int _networkFailureCount = 0;
        private int _otherFailureCount = 0;

        /// <summary>
        /// Initializes a new instance of the OverseerClientService with required dependencies.
        /// </summary>
        /// <param name="logger">The logger instance for recording service operations and errors.</param>
        /// <param name="serviceFactory">Factory for accessing other bot services like market data and error handlers.</param>
        /// <param name="overseerConfig">Configuration options for the overseer.</param>
        /// <param name="instanceNameConfig">Configuration options for general execution settings.</param>
        /// <param name="centralPerformanceMonitor">Central performance monitor for recording metrics.</param>
        public OverseerClientService(
            ILogger<OverseerClientService> logger,
            IServiceFactory serviceFactory,
            IOptions<OverseerClientServiceConfig> overseerConfig,
            IOptions<InstanceNameConfig> instanceNameConfig,
            ICentralPerformanceMonitor centralPerformanceMonitor)
        {
            _logger = logger;
            _serviceFactory = serviceFactory;
            _centralPerformanceMonitor = centralPerformanceMonitor;
            _clientId = Guid.NewGuid().ToString();

            // Read brain instance name from configuration, fallback to hardcoded if not set
            _clientName = instanceNameConfig.Value.Name;
            _logger.LogInformation("OVERSEER- Using brain instance name: {BrainInstanceName}", _clientName);

            // Initialize configurable timeouts and intervals
            _connectionTimeout = TimeSpan.FromSeconds(overseerConfig.Value.OverseerConnectionTimeoutSeconds);
            _semaphoreTimeout = TimeSpan.FromSeconds(overseerConfig.Value.OverseerSemaphoreTimeoutSeconds);
            _overseerDiscoveryInterval = TimeSpan.FromMinutes(overseerConfig.Value.OverseerDiscoveryIntervalMinutes);
            _checkInInterval = TimeSpan.FromSeconds(overseerConfig.Value.OverseerCheckInIntervalSeconds);
            _circuitBreakerFailureThreshold = overseerConfig.Value.OverseerCircuitBreakerFailureThreshold;
            _circuitBreakerTimeout = TimeSpan.FromMinutes(overseerConfig.Value.OverseerCircuitBreakerTimeoutMinutes);
            _enablePerformanceMetrics = overseerConfig.Value.EnablePerformanceMetrics;
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

                // Start Overseer Discovery timer first - this ensures we keep trying to find overseer
                _overseerDiscoveryTimer = new Timer(async _ => await PerformOverseerDiscoveryAsync(), null, TimeSpan.Zero, _overseerDiscoveryInterval);
                _logger.LogInformation("OVERSEER- Overseer discovery timer started (every {Interval} minutes)", _overseerDiscoveryInterval.TotalMinutes);

                // Start CheckIn timer - it will only send if connected
                _checkInTimer = new Timer(async _ => await SendCheckInAsync(), null, TimeSpan.FromSeconds(10), _checkInInterval);
                _logger.LogInformation("OVERSEER- CheckIn timer started (every {Interval} seconds, first check in 10 seconds)", _checkInInterval.TotalSeconds);

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
                            _logger.LogInformation("OVERSEER- Failed to connect to overseer. Exception: {0}", ex.Message);
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
                _logger.LogInformation("OVERSEER- Failed to initialize overseer client. This is optional and the bot will continue without oversight. Exception: {0}"
                    , ex.Message);
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

                // Reset handshake completed flag
                _handshakeCompleted = false;

                _logger.LogInformation("OVERSEER- Overseer client stopped");
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Error stopping overseer client. Error: {0}", ex.Message);
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
                            _logger.LogInformation("OVERSEER- Error stopping connection during cleanup: {Error}", stopEx.Message);
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
                    _logger.LogInformation("OVERSEER- Error during connection cleanup: {Error}", ex.Message);
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
                var context = scope.ServiceProvider.GetRequiredService<BacklashBotData.Data.Interfaces.IBacklashBotContext>();

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

            // Check circuit breaker
            if (IsCircuitBreakerOpen())
            {
                _logger.LogInformation("OVERSEER- Circuit breaker is open, skipping connection attempt to {Url}", overseerUrl);
                return;
            }

            // Add timeout to semaphore to prevent hanging
            using var semaphoreTimeout = new CancellationTokenSource(_semaphoreTimeout);
            await _connectionOperationSemaphore.WaitAsync(semaphoreTimeout.Token);
            _logger.LogDebug("OVERSEER- Semaphore acquired successfully");

            var connectionStopwatch = Stopwatch.StartNew();
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
                    .WithUrl(overseerUrl, options =>
                    {
                        options.HttpMessageHandlerFactory = (handler) =>
                        {
                            if (handler is HttpClientHandler clientHandler)
                            {
                                clientHandler.UseProxy = false;
                            }
                            return handler;
                        };
                    })
                    .AddJsonProtocol(o => { o.PayloadSerializerOptions.PropertyNamingPolicy = null; })
                    .WithAutomaticReconnect()
                    .Build();
                _logger.LogDebug("OVERSEER- HubConnection created, about to call StartAsync");
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
                    _logger.LogInformation("OVERSEER- HubConnection is in {State} state, cannot start. Creating fresh connection.", _hubConnection.State);
                    await CleanupConnectionAsync();

                    _hubConnection = new HubConnectionBuilder()
                        .WithUrl(overseerUrl)
                        .AddJsonProtocol(o => { o.PayloadSerializerOptions.PropertyNamingPolicy = null; })
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

                _logger.LogDebug("OVERSEER- About to call StartAsync with configurable timeout ({Timeout}s)", _connectionTimeout.TotalSeconds);
                using var cts = new CancellationTokenSource(_connectionTimeout);
                try
                {
                    await _hubConnection.StartAsync(cts.Token);
                    _logger.LogInformation("OVERSEER- StartAsync completed successfully, state: {State}", _hubConnection.State);
                }
                catch (Exception startEx)
                {
                    _logger.LogWarning("OVERSEER- StartAsync failed with exception: {Message}", startEx.Message);
                    throw;
                }

                lock (_connectionStateLock)
                {
                    _isConnected = true;
                    _currentOverseerUrl = overseerUrl;
                }
                _logger.LogInformation("OVERSEER- Successfully connected to overseer at {Url}", overseerUrl);

                // Record successful connection
                RecordConnectionAttempt(true);

                // Perform handshake
                _logger.LogDebug("OVERSEER- About to perform handshake");
                await PerformHandshakeAsync();
                _logger.LogDebug("OVERSEER- Handshake completed");

                connectionStopwatch.Stop();
                if (_enablePerformanceMetrics)
                {
                    lock (_circuitBreakerLock)
                    {
                        _totalConnectionAttemptTime += connectionStopwatch.Elapsed;
                        _connectionAttemptTimeCount++;
                    }
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("OVERSEER- Connection attempt timed out after {Timeout} seconds", _connectionTimeout.TotalSeconds);
                lock (_connectionStateLock)
                {
                    _isConnected = false;
                }
                if (_enablePerformanceMetrics)
                {
                    lock (_circuitBreakerLock)
                    {
                        _timeoutFailureCount++;
                    }
                }
                RecordConnectionAttempt(false);
            }
            catch (Exception ex)
            {
                _logger.LogInformation("OVERSEER- Exception in AttemptConnectionAsync: {Message}", ex.Message);
                lock (_connectionStateLock)
                {
                    _isConnected = false;
                }
                if (_enablePerformanceMetrics)
                {
                    lock (_circuitBreakerLock)
                    {
                        if (ex.Message.Contains("auth", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase))
                        {
                            _authFailureCount++;
                        }
                        else if (ex is System.Net.Http.HttpRequestException || ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase))
                        {
                            _networkFailureCount++;
                        }
                        else
                        {
                            _otherFailureCount++;
                        }
                    }
                }
                RecordConnectionAttempt(false);
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
                var context = scope.ServiceProvider.GetRequiredService<BacklashBotData.Data.Interfaces.IBacklashBotContext>();

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
            var startTime = DateTime.UtcNow;
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
                _logger.LogInformation("OVERSEER- Overseer discovery failed - this is normal and will be retried on next cycle. Error: {Error}", ex.Message);
            }
            finally
            {
                var duration = DateTime.UtcNow - startTime;
                lock (_circuitBreakerLock)
                {
                    _totalDiscoveryTime += duration;
                    _discoveryOperationCount++;
                }
                _logger.LogDebug("OVERSEER- Discovery operation completed in {Duration} ms", duration.TotalMilliseconds);
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
                _logger.LogInformation("OVERSEER- Failed during overseer discovery process. Will retry on next cycle. Error: {Error}", ex.Message);
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
                   _handshakeCompleted &&
                   _hubConnection.State == HubConnectionState.Connected;
        }

        /// <summary>
        /// Checks if the circuit breaker is currently open, preventing connection attempts.
        /// </summary>
        /// <returns>True if the circuit breaker is open, false otherwise.</returns>
        private bool IsCircuitBreakerOpen()
        {
            lock (_circuitBreakerLock)
            {
                if (_circuitBreakerFailureCount >= _circuitBreakerFailureThreshold)
                {
                    var timeSinceLastFailure = DateTime.UtcNow - _circuitBreakerLastFailure;
                    if (timeSinceLastFailure < _circuitBreakerTimeout)
                    {
                        return true; // Circuit is open
                    }
                    else
                    {
                        // Reset circuit breaker after timeout
                        _circuitBreakerFailureCount = 0;
                        _circuitBreakerLastFailure = DateTime.MinValue;
                        _logger.LogInformation("OVERSEER- Circuit breaker reset after timeout");
                    }
                }
                return false; // Circuit is closed
            }
        }

        /// <summary>
        /// Ensures that a valid authentication token is available for the connection.
        /// Refreshes the token if it is expired or missing.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task EnsureValidAuthTokenAsync()
        {
            // Check if token is expired or missing
            if (string.IsNullOrEmpty(_authToken) || DateTime.UtcNow >= _authTokenExpiry)
            {
                _logger.LogInformation("OVERSEER- Refreshing auth token");
                await RefreshAuthTokenAsync();
            }
        }

        /// <summary>
        /// Refreshes the authentication token by generating a new one.
        /// In a production environment, this would typically call an authentication service.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task RefreshAuthTokenAsync()
        {
            try
            {
                // For now, generate a simple token. In a real implementation, this would call an auth service.
                _authToken = Guid.NewGuid().ToString();
                _authTokenExpiry = DateTime.UtcNow.AddHours(1); // Token valid for 1 hour
                _logger.LogInformation("OVERSEER- Auth token refreshed, expires at {Expiry}", _authTokenExpiry);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogInformation("OVERSEER- Failed to refresh auth token: {Error}", ex.Message);
                _authToken = null;
                _authTokenExpiry = DateTime.MinValue;
            }
        }

        /// <summary>
        /// Records the result of a connection attempt for metrics and circuit breaker tracking.
        /// </summary>
        /// <param name="success">True if the connection attempt was successful, false otherwise.</param>
        private void RecordConnectionAttempt(bool success)
        {
            lock (_circuitBreakerLock)
            {
                _connectionAttemptCount++;
                if (success)
                {
                    _connectionSuccessCount++;
                    _circuitBreakerFailureCount = 0; // Reset on success
                }
                else
                {
                    _circuitBreakerFailureCount++;
                    _circuitBreakerLastFailure = DateTime.UtcNow;
                }
            }
        }

        /// <summary>
        /// Gets the total number of connection attempts made to overseer servers.
        /// </summary>
        public int ConnectionAttemptCount => _connectionAttemptCount;

        /// <summary>
        /// Gets the number of successful connections to overseer servers.
        /// </summary>
        public int ConnectionSuccessCount => _connectionSuccessCount;

        /// <summary>
        /// Gets the total time spent on overseer discovery operations.
        /// </summary>
        public TimeSpan TotalDiscoveryTime => _totalDiscoveryTime;

        /// <summary>
        /// Gets the number of overseer discovery operations performed.
        /// </summary>
        public int DiscoveryOperationCount => _discoveryOperationCount;

        /// <summary>
        /// Gets the current count of circuit breaker failures.
        /// </summary>
        public int CircuitBreakerFailureCount => _circuitBreakerFailureCount;

        /// <summary>
        /// Gets the total time spent on handshake operations.
        /// </summary>
        public TimeSpan TotalHandshakeTime => _totalHandshakeTime;

        /// <summary>
        /// Gets the number of handshake operations performed.
        /// </summary>
        public int HandshakeCount => _handshakeCount;

        /// <summary>
        /// Gets the total time spent on check-in operations.
        /// </summary>
        public TimeSpan TotalCheckInTime => _totalCheckInTime;

        /// <summary>
        /// Gets the number of check-in operations performed.
        /// </summary>
        public int CheckInCount => _checkInCount;

        /// <summary>
        /// Gets the total time spent on connection attempt operations.
        /// </summary>
        public TimeSpan TotalConnectionAttemptTime => _totalConnectionAttemptTime;

        /// <summary>
        /// Gets the number of connection attempt operations performed.
        /// </summary>
        public int ConnectionAttemptTimeCount => _connectionAttemptTimeCount;

        /// <summary>
        /// Gets the number of messages sent.
        /// </summary>
        public int MessagesSent => _messagesSent;

        /// <summary>
        /// Gets the number of messages received.
        /// </summary>
        public int MessagesReceived => _messagesReceived;

        /// <summary>
        /// Gets the count of timeout failures.
        /// </summary>
        public int TimeoutFailureCount => _timeoutFailureCount;

        /// <summary>
        /// Gets the count of authentication failures.
        /// </summary>
        public int AuthFailureCount => _authFailureCount;

        /// <summary>
        /// Gets the count of network failures.
        /// </summary>
        public int NetworkFailureCount => _networkFailureCount;

        /// <summary>
        /// Gets the count of other failures.
        /// </summary>
        public int OtherFailureCount => _otherFailureCount;

        /// <summary>
        /// Gets the current performance metrics for the overseer client service.
        /// Includes connection success rates, discovery timing, and circuit breaker status.
        /// Additional metrics are included only if performance metrics are enabled.
        /// </summary>
        /// <returns>A dictionary containing metric names and values.</returns>
        public Dictionary<string, object> GetMetrics()
        {
            lock (_circuitBreakerLock)
            {
                var successRate = _connectionAttemptCount > 0 ? (double)_connectionSuccessCount / _connectionAttemptCount : 0.0;
                var avgDiscoveryTime = _discoveryOperationCount > 0 ? _totalDiscoveryTime.TotalMilliseconds / _discoveryOperationCount : 0.0;

                var metrics = new Dictionary<string, object>
                {
                    ["ConnectionAttempts"] = _connectionAttemptCount,
                    ["ConnectionSuccesses"] = _connectionSuccessCount,
                    ["ConnectionSuccessRate"] = successRate,
                    ["CircuitBreakerFailures"] = _circuitBreakerFailureCount,
                    ["DiscoveryOperations"] = _discoveryOperationCount,
                    ["AverageDiscoveryTimeMs"] = avgDiscoveryTime,
                    ["TotalDiscoveryTimeMs"] = _totalDiscoveryTime.TotalMilliseconds,
                    ["IsCircuitBreakerOpen"] = IsCircuitBreakerOpen()
                };

                if (_enablePerformanceMetrics)
                {
                    var avgHandshakeTime = _handshakeCount > 0 ? _totalHandshakeTime.TotalMilliseconds / _handshakeCount : 0.0;
                    var avgCheckInTime = _checkInCount > 0 ? _totalCheckInTime.TotalMilliseconds / _checkInCount : 0.0;
                    var avgConnectionAttemptTime = _connectionAttemptTimeCount > 0 ? _totalConnectionAttemptTime.TotalMilliseconds / _connectionAttemptTimeCount : 0.0;
                    var messagesPerSecond = (DateTime.UtcNow - _lastThroughputReset).TotalSeconds > 0 ? (_messagesSent + _messagesReceived) / (DateTime.UtcNow - _lastThroughputReset).TotalSeconds : 0.0;

                    metrics.Add("AverageHandshakeTimeMs", avgHandshakeTime);
                    metrics.Add("TotalHandshakeTimeMs", _totalHandshakeTime.TotalMilliseconds);
                    metrics.Add("HandshakeCount", _handshakeCount);
                    metrics.Add("AverageCheckInTimeMs", avgCheckInTime);
                    metrics.Add("TotalCheckInTimeMs", _totalCheckInTime.TotalMilliseconds);
                    metrics.Add("CheckInCount", _checkInCount);
                    metrics.Add("AverageConnectionAttemptTimeMs", avgConnectionAttemptTime);
                    metrics.Add("TotalConnectionAttemptTimeMs", _totalConnectionAttemptTime.TotalMilliseconds);
                    metrics.Add("ConnectionAttemptTimeCount", _connectionAttemptTimeCount);
                    metrics.Add("MessagesSent", _messagesSent);
                    metrics.Add("MessagesReceived", _messagesReceived);
                    metrics.Add("MessagesPerSecond", messagesPerSecond);
                    metrics.Add("TimeoutFailureCount", _timeoutFailureCount);
                    metrics.Add("AuthFailureCount", _authFailureCount);
                    metrics.Add("NetworkFailureCount", _networkFailureCount);
                    metrics.Add("OtherFailureCount", _otherFailureCount);
                }

                return metrics;
            }
        }

        private async Task PerformHandshakeAsync()
        {
            await PerformHandshakeAsyncInternal();
        }

        private async Task PerformHandshakeAsyncInternal()
        {
            if (_hubConnection == null)
            {
                _logger.LogInformation("OVERSEER- Cannot send handshake: HubConnection is null");
                return;
            }

            _logger.LogDebug("OVERSEER- Checking connection state for handshake: IsConnected={IsConnected}, State={State}",
                _isConnected, _hubConnection.State);

            if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
            {
                _logger.LogInformation("OVERSEER- Cannot send handshake: Connection not established (State={State})",
                    _hubConnection?.State ?? HubConnectionState.Disconnected);
                return;
            }

            try
            {
                // Reset handshake completed flag
                _handshakeCompleted = false;

                // Ensure auth token is valid
                await EnsureValidAuthTokenAsync();

                // Delay to ensure connection is fully established
                await Task.Delay(1000);

                var handshakeRequest = new HandshakeRequest
                {
                    ClientId = _clientId,
                    ClientName = _clientName,
                    ClientType = _clientType,
                    AuthToken = _authToken
                };

                _logger.LogInformation("OVERSEER- Sending handshake with ClientId={ClientId}, ClientName={ClientName}, ClientType={ClientType}, AuthToken={HasToken}",
                    handshakeRequest.ClientId, handshakeRequest.ClientName, handshakeRequest.ClientType, !string.IsNullOrEmpty(handshakeRequest.AuthToken));

                var stopwatch = Stopwatch.StartNew();
                HandshakeResponse response;
                try
                {
                    response = await _hubConnection.InvokeAsync<HandshakeResponse>("Handshake", handshakeRequest);
                    stopwatch.Stop();
                    _logger.LogInformation("OVERSEER- Handshake InvokeAsync completed successfully");
                }
                catch (Exception invokeEx)
                {
                    _logger.LogWarning("OVERSEER- Handshake InvokeAsync failed: {Message}", invokeEx.Message);
                    throw;
                }

                if (_enablePerformanceMetrics)
                {
                    lock (_circuitBreakerLock)
                    {
                        _totalHandshakeTime += stopwatch.Elapsed;
                        _handshakeCount++;
                        _messagesSent++;
                    }
                }

                _logger.LogInformation("OVERSEER- Handshake response received: Success={Success}, Message={Message}", response.Success, response.Message);

                if (response.Success)
                {
                    _handshakeCompleted = true;
                    _logger.LogInformation("OVERSEER- Handshake completed successfully, client is now registered");
                }
                else
                {
                    _logger.LogWarning("OVERSEER- Handshake failed: {Message}", response.Message);
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("cannot be called if the connection is not active"))
            {
                _logger.LogInformation("OVERSEER- Connection became inactive during handshake attempt. Will mark as disconnected. Error: {Error}", ex.Message);
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

            if (!_handshakeCompleted)
            {
                _logger.LogDebug("OVERSEER- Handshake not completed yet, skipping CheckIn (will retry on next cycle)");
                return;
            }

            try
            {
                // Post metrics to central performance monitor
                var metrics = GetMetrics();
                RecordOverseerClientServiceMetrics(metrics, _enablePerformanceMetrics);

                // Log metrics every 10 check-ins
                if (_connectionSuccessCount > 0 && _connectionSuccessCount % 10 == 0)
                {
                    if (_enablePerformanceMetrics)
                    {
                        _logger.LogInformation("OVERSEER- Performance Metrics: Attempts={Attempts}, Successes={Successes}, SuccessRate={Rate:P2}, AvgDiscoveryTime={AvgTime:F2}ms, AvgHandshakeTime={Handshake:F2}ms, AvgCheckInTime={CheckIn:F2}ms, MessagesSent={Sent}, MessagesReceived={Received}, CircuitBreakerOpen={Open}",
                            metrics["ConnectionAttempts"], metrics["ConnectionSuccesses"], metrics["ConnectionSuccessRate"], metrics["AverageDiscoveryTimeMs"], metrics["AverageHandshakeTimeMs"], metrics["AverageCheckInTimeMs"], metrics["MessagesSent"], metrics["MessagesReceived"], metrics["IsCircuitBreakerOpen"]);
                    }
                    else
                    {
                        _logger.LogInformation("OVERSEER- Performance Metrics: Attempts={Attempts}, Successes={Successes}, SuccessRate={Rate:P2}, AvgDiscoveryTime={AvgTime:F2}ms, CircuitBreakerOpen={Open}",
                            metrics["ConnectionAttempts"], metrics["ConnectionSuccesses"], metrics["ConnectionSuccessRate"], metrics["AverageDiscoveryTimeMs"], metrics["IsCircuitBreakerOpen"]);
                    }
                }

                _logger.LogDebug("OVERSEER- Preparing CheckIn data...");

                // Validate that we're still connected to the most recent overseer
                await ValidateCurrentOverseerAsync();

                var markets = await _serviceFactory.GetMarketDataService().FetchWatchedMarketsAsync();
                var errorHandler = _serviceFactory.GetBacklashErrorHandler();

                // Get brain instance configuration from database
                BacklashDTOs.Data.BrainInstanceDTO? brainInstance = null;
                using var scope = _serviceFactory.GetScopeManager().CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<BacklashBotData.Data.Interfaces.IBacklashBotContext>();
                brainInstance = await context.GetBrainInstanceByName(_clientName);

                var dataCache = _serviceFactory.GetDataCache();
                var checkInData = new CheckInData
                {
                    BrainInstanceName = _clientName,
                    Markets = markets,
                    ErrorCount = errorHandler.ErrorCount,
                    LastSnapshot = errorHandler.LastSuccessfulSnapshot == DateTime.MinValue
                        ? (DateTime?)null
                        : errorHandler.LastSuccessfulSnapshot,
                    IsStartingUp = _centralPerformanceMonitor.IsStartingUp,
                    IsShuttingDown = _centralPerformanceMonitor.IsShuttingDown,
                    CurrentCpuUsage = _centralPerformanceMonitor.GetCurrentCpuUsage(),
                    EventQueueAvg = _centralPerformanceMonitor.GetEventQueueAvg(),
                    TickerQueueAvg = _centralPerformanceMonitor.GetTickerQueueAvg(),
                    NotificationQueueAvg = _centralPerformanceMonitor.GetNotificationQueueAvg(),
                    OrderbookQueueAvg = _centralPerformanceMonitor.GetOrderbookQueueAvg(),
                    LastRefreshCycleSeconds = _centralPerformanceMonitor.GetLastRefreshCycleSeconds(),
                    LastRefreshCycleInterval = _centralPerformanceMonitor.GetLastRefreshCycleInterval(),
                    LastRefreshMarketCount = _centralPerformanceMonitor.GetLastRefreshMarketCount(),
                    LastRefreshUsagePercentage = _centralPerformanceMonitor.GetLastRefreshUsagePercentage(),
                    LastRefreshTimeAcceptable = _centralPerformanceMonitor.GetLastRefreshTimeAcceptable(),
                    LastPerformanceSampleDate = _centralPerformanceMonitor.GetLastPerformanceSampleDate(),
                    IsWebSocketConnected = _centralPerformanceMonitor.IsWebSocketConnected,
                    PortfolioValue = dataCache.PortfolioValue
                };

                _logger.LogDebug("OVERSEER- Sending CheckIn to overseer: {MarketCount} markets, ErrorCount: {ErrorCount}, LastSnapshot: {LastSnapshot}",
                    markets.Count, errorHandler.ErrorCount, checkInData.LastSnapshot);

                var stopwatch = Stopwatch.StartNew();
                await _hubConnection.InvokeAsync("CheckIn", checkInData);
                stopwatch.Stop();

                if (_enablePerformanceMetrics)
                {
                    lock (_circuitBreakerLock)
                    {
                        _totalCheckInTime += stopwatch.Elapsed;
                        _checkInCount++;
                        _messagesSent++;
                    }
                }

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
            if (_enablePerformanceMetrics)
            {
                lock (_circuitBreakerLock)
                {
                    _messagesReceived++;
                }
            }
            _logger.LogInformation("OVERSEER- Handshake response received from overseer: Success={Success}, Message={Message}", response.Success, response.Message);

            if (response.Success)
            {
                _handshakeCompleted = true;
                _logger.LogInformation("OVERSEER- Handshake completed successfully, client is now registered");
            }
            else
            {
                _logger.LogWarning("OVERSEER- Handshake failed: {Message}", response.Message);
            }
        }

        private void HandleCheckInResponse(CheckInResponse response)
        {
            if (_enablePerformanceMetrics)
            {
                lock (_circuitBreakerLock)
                {
                    _messagesReceived++;
                }
            }
            _logger.LogInformation("OVERSEER- CheckIn response received from overseer: Success={Success}, Message={Message}, TargetTickers={Count}", response.Success, response.Message, response.TargetTickers.Length);
        }

        private void HandleMessageResponse(MessageResponse response)
        {
            if (_enablePerformanceMetrics)
            {
                lock (_circuitBreakerLock)
                {
                    _messagesReceived++;
                }
            }
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

        private async Task HandleReconnected(string? connectionId)
        {
            lock (_connectionStateLock)
            {
                _isConnected = true;
            }

            // Reset handshake completed flag on reconnection
            _handshakeCompleted = false;

            _logger.LogInformation("OVERSEER- Successfully reconnected to overseer with connection ID: {ConnectionId}", connectionId);

            // Re-perform handshake on reconnection
            try
            {
                await PerformHandshakeAsync();
                _logger.LogInformation("OVERSEER- Handshake completed after reconnection");
            }
            catch (Exception ex)
            {
                _logger.LogWarning("OVERSEER- Failed to perform handshake after reconnection. Error: {Error}", ex.Message);
                // If handshake fails, mark as not connected
                lock (_connectionStateLock)
                {
                    _isConnected = false;
                }
            }
        }

        private void RecordOverseerClientServiceMetrics(Dictionary<string, object> metrics, bool enablePerformanceMetrics)
        {
            string className = nameof(OverseerClientService);
            string category = "Connection";

            // Define all possible metrics with their properties
            var allMetrics = new Dictionary<string, (string Name, string Description, string Unit, string VisualType, double? MinThreshold, double? WarningThreshold, double? CriticalThreshold)>
            {
                ["ConnectionAttempts"] = ("Connection Attempts", "Total number of connection attempts made", "count", "Counter", null, null, null),
                ["ConnectionSuccesses"] = ("Connection Successes", "Number of successful connections", "count", "Counter", null, null, null),
                ["ConnectionSuccessRate"] = ("Connection Success Rate", "Rate of successful connections", "%", "ProgressBar", 0, 50, 80),
                ["CircuitBreakerFailures"] = ("Circuit Breaker Failures", "Number of circuit breaker failures", "count", "Counter", null, null, null),
                ["DiscoveryOperations"] = ("Discovery Operations", "Number of overseer discovery operations", "count", "Counter", null, null, null),
                ["AverageDiscoveryTimeMs"] = ("Average Discovery Time", "Average time spent on discovery operations", "ms", "NumericDisplay", null, null, null),
                ["TotalDiscoveryTimeMs"] = ("Total Discovery Time", "Total time spent on discovery operations", "ms", "NumericDisplay", null, null, null),
                ["IsCircuitBreakerOpen"] = ("Circuit Breaker Status", "Whether the circuit breaker is currently open", "state", "TrafficLight", 0, 0.5, 1),
                ["AverageHandshakeTimeMs"] = ("Average Handshake Time", "Average time for handshake operations", "ms", "NumericDisplay", null, null, null),
                ["TotalHandshakeTimeMs"] = ("Total Handshake Time", "Total time spent on handshake operations", "ms", "NumericDisplay", null, null, null),
                ["HandshakeCount"] = ("Handshake Count", "Number of handshake operations", "count", "Counter", null, null, null),
                ["AverageCheckInTimeMs"] = ("Average Check-In Time", "Average time for check-in operations", "ms", "NumericDisplay", null, null, null),
                ["TotalCheckInTimeMs"] = ("Total Check-In Time", "Total time spent on check-in operations", "ms", "NumericDisplay", null, null, null),
                ["CheckInCount"] = ("Check-In Count", "Number of check-in operations", "count", "Counter", null, null, null),
                ["AverageConnectionAttemptTimeMs"] = ("Average Connection Attempt Time", "Average time for connection attempts", "ms", "NumericDisplay", null, null, null),
                ["TotalConnectionAttemptTimeMs"] = ("Total Connection Attempt Time", "Total time spent on connection attempts", "ms", "NumericDisplay", null, null, null),
                ["ConnectionAttemptTimeCount"] = ("Connection Attempt Time Count", "Number of connection attempt time measurements", "count", "Counter", null, null, null),
                ["MessagesSent"] = ("Messages Sent", "Number of messages sent", "count", "Counter", null, null, null),
                ["MessagesReceived"] = ("Messages Received", "Number of messages received", "count", "Counter", null, null, null),
                ["MessagesPerSecond"] = ("Messages Per Second", "Messages processed per second", "msg/s", "NumericDisplay", null, null, null),
                ["TimeoutFailureCount"] = ("Timeout Failures", "Number of timeout failures", "count", "Counter", null, null, null),
                ["AuthFailureCount"] = ("Auth Failures", "Number of authentication failures", "count", "Counter", null, null, null),
                ["NetworkFailureCount"] = ("Network Failures", "Number of network failures", "count", "Counter", null, null, null),
                ["OtherFailureCount"] = ("Other Failures", "Number of other failures", "count", "Counter", null, null, null)
            };

            if (enablePerformanceMetrics)
            {
                // Send all metrics in the dictionary as enabled
                foreach (var kvp in metrics)
                {
                    if (allMetrics.TryGetValue(kvp.Key, out var metricInfo))
                    {
                        double value = kvp.Key == "ConnectionSuccessRate" ? Convert.ToDouble(kvp.Value) * 100 : Convert.ToDouble(kvp.Value);
                        if (kvp.Key == "IsCircuitBreakerOpen")
                        {
                            value = Convert.ToBoolean(kvp.Value) ? 1.0 : 0.0;
                        }

                        switch (metricInfo.VisualType)
                        {
                            case "Counter":
                                _centralPerformanceMonitor.RecordCounterMetric(className, kvp.Key, metricInfo.Name, metricInfo.Description, value, metricInfo.Unit, category);
                                break;
                            case "ProgressBar":
                                _centralPerformanceMonitor.RecordProgressBarMetric(className, kvp.Key, metricInfo.Name, metricInfo.Description, value, metricInfo.Unit, category, metricInfo.MinThreshold ?? 0, metricInfo.WarningThreshold ?? 0, metricInfo.CriticalThreshold ?? 0);
                                break;
                            case "NumericDisplay":
                                _centralPerformanceMonitor.RecordNumericDisplayMetric(className, kvp.Key, metricInfo.Name, metricInfo.Description, value, metricInfo.Unit, category);
                                break;
                            case "TrafficLight":
                                _centralPerformanceMonitor.RecordTrafficLightMetric(className, kvp.Key, metricInfo.Name, metricInfo.Description, value, metricInfo.Unit, category, metricInfo.MinThreshold ?? 0, metricInfo.WarningThreshold ?? 0, metricInfo.CriticalThreshold ?? 0);
                                break;
                        }
                    }
                }
            }
            else
            {
                // Send all possible metrics as disabled
                foreach (var kvp in allMetrics)
                {
                    _centralPerformanceMonitor.RecordDisabledMetric(className, kvp.Key, kvp.Value.Name, kvp.Value.Description, 0, "disabled", category);
                }
            }
        }
    }
}
