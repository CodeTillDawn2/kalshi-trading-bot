/// <summary>
/// Central orchestrator for the KalshiBot Overseer system that manages WebSocket connections,
/// handles real-time market data events, coordinates periodic data fetching, and maintains
/// system health monitoring. This class serves as the main entry point for overseer operations,
/// integrating with WebSocket clients, database services, and SignalR hubs to provide
/// comprehensive monitoring and control of the trading bot ecosystem.
/// </summary>
using KalshiBotAPI.WebSockets.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BacklashBot.KalshiAPI.Interfaces;
using BacklashDTOs;
using KalshiBotData.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using System.Linq;
using KalshiBotOverseer.Services;
using Microsoft.Extensions.Options;

namespace KalshiBotOverseer
{
    /// <summary>
    /// Configuration options for the Overseer system.
    /// </summary>
    public class OverseerConfig
    {
        /// <summary>
        /// Gets or sets the interval in minutes for periodic API data fetching.
        /// Default is 10 minutes.
        /// </summary>
        public int ApiFetchIntervalMinutes { get; set; } = 10;

        /// <summary>
        /// Gets or sets the interval in minutes for periodic system info logging.
        /// Default is 1 minute.
        /// </summary>
        public int SystemInfoLogIntervalMinutes { get; set; } = 1;

        /// <summary>
        /// Gets or sets the batch size for SignalR broadcast operations.
        /// Default is 10 to prevent payload size issues.
        /// </summary>
        public int SignalRBatchSize { get; set; } = 10;

        /// <summary>
        /// Gets or sets the batch size for brain persistence logging operations.
        /// Default is 50 for better performance with large brain sets.
        /// </summary>
        public int BrainBatchSize { get; set; } = 50;
    }

    /// <summary>
    /// Central orchestrator for the KalshiBot Overseer system that manages WebSocket connections,
    /// handles real-time market data events, coordinates periodic data fetching, and maintains
    /// system health monitoring. This class serves as the main entry point for overseer operations,
    /// integrating with WebSocket clients, database services, and SignalR hubs to provide
    /// comprehensive monitoring and control of the trading bot ecosystem.
    ///
    /// Key features:
    /// - Configurable API fetch intervals (default: 10 minutes)
    /// - Configurable system info logging intervals (default: 1 minute)
    /// - Performance metrics collection for WebSocket events and API operations
    /// - Input validation for event data processing to prevent null reference exceptions
    /// - Progress reporting for long-running API fetch operations
    /// - Batch processing for brain persistence logging (default batch size: 50)
    /// - Configurable SignalR broadcast batch sizes (default: 10)
    /// </summary>
    public class Overseer : IDisposable
    {
        private readonly IKalshiWebSocketClient _webSocketClient;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<Overseer> _logger;
        private readonly IHubContext<OverseerHub> _hubContext;
        private readonly OverseerConfig _config;
        private Timer? _apiFetchTimer;
        private CancellationTokenSource? _apiFetchCancellationTokenSource;
        private Timer? _systemInfoLogTimer;
        private CancellationTokenSource? _systemInfoLogCancellationTokenSource;
        private bool _disposed = false;

        // Configuration options (loaded from appsettings.json)
        private readonly TimeSpan _apiFetchInterval; // Configurable API fetch interval
        private readonly TimeSpan _systemInfoLogInterval; // Configurable logging interval
        private readonly int _signalRBatchSize; // Configurable SignalR broadcast batch size
        private readonly int _brainBatchSize; // Configurable batch size for brain persistence logging

        // Performance metrics
        private int _webSocketEventCount = 0;
        private TimeSpan _totalApiFetchTime = TimeSpan.Zero;
        private int _apiFetchCount = 0;

        // SignalR batching
        private readonly List<object> _pendingCheckInUpdates = new();
        private readonly object _batchLock = new();

        /// <summary>
        /// Initializes a new instance of the Overseer class with required dependencies.
        /// </summary>
        /// <param name="webSocketClient">WebSocket client for real-time market data streaming.</param>
        /// <param name="scopeFactory">Factory for creating service scopes for dependency injection.</param>
        /// <param name="logger">Logger for recording system events and diagnostics.</param>
        /// <param name="hubContext">SignalR hub context for real-time client communication.</param>
        /// <param name="config">Configuration options for the overseer system.</param>
        public Overseer(IKalshiWebSocketClient webSocketClient, IServiceScopeFactory scopeFactory, ILogger<Overseer> logger, IHubContext<OverseerHub> hubContext, IOptions<OverseerConfig> config)
        {
            _webSocketClient = webSocketClient;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _hubContext = hubContext;
            _config = config.Value;

            // Initialize configuration-based intervals
            _apiFetchInterval = TimeSpan.FromMinutes(_config.ApiFetchIntervalMinutes);
            _systemInfoLogInterval = TimeSpan.FromMinutes(_config.SystemInfoLogIntervalMinutes);
            _signalRBatchSize = _config.SignalRBatchSize;
            _brainBatchSize = _config.BrainBatchSize;
        }

        /// <summary>
        /// Starts the overseer system by initializing logging, subscribing to WebSocket events,
        /// and beginning periodic data fetching and system monitoring operations.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task Start()
        {
            // Log system information to database for monitoring
            await LogSystemInfoAsync();

            // Log comprehensive brain persistence state for debugging and monitoring
            await LogBrainPersistenceStateAsync();

            // Subscribe to WebSocket events for real-time market data processing
            _webSocketClient.FillReceived += HandleFillEvent;
            _webSocketClient.MarketLifecycleReceived += HandleMarketLifecycleEvent;
            _webSocketClient.EventLifecycleReceived += HandleEventLifecycleEvent;

            _logger?.LogInformation("Subscribed to Fill, MarketLifecycle, and EventLifecycle events.");

            StartApiDataFetchTimer();
            StartSystemInfoLoggingTimer();
        }

        /// <summary>
        /// Stops the overseer system by unsubscribing from events and halting periodic operations.
        /// </summary>
        public void Stop()
        {
            Unsubscribe();
            _logger?.LogInformation("Unsubscribed from events.");
        }

        /// <summary>
        /// Unsubscribes from all WebSocket events to prevent memory leaks and ensure clean shutdown.
        /// </summary>
        private void Unsubscribe()
        {
            if (_disposed) return;
            _webSocketClient.FillReceived -= HandleFillEvent;
            _webSocketClient.MarketLifecycleReceived -= HandleMarketLifecycleEvent;
            _webSocketClient.EventLifecycleReceived -= HandleEventLifecycleEvent;
        }

        /// <summary>
        /// Handles fill events from the WebSocket feed, processing trade execution data.
        /// Currently logs the event for monitoring purposes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing fill data.</param>
        private void HandleFillEvent(object? sender, FillEventArgs e)
        {
            if (e == null)
            {
                _logger?.LogWarning("Received null FillEventArgs");
                return;
            }

            _webSocketEventCount++;
            _logger?.LogInformation("Received Fill event: {EventData}", e);
        }

        /// <summary>
        /// Handles market lifecycle events from the WebSocket feed, processing market state changes.
        /// Currently logs the event for monitoring purposes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing market lifecycle data.</param>
        private void HandleMarketLifecycleEvent(object? sender, MarketLifecycleEventArgs e)
        {
            if (e == null)
            {
                _logger?.LogWarning("Received null MarketLifecycleEventArgs");
                return;
            }

            _webSocketEventCount++;
            _logger?.LogInformation("Received MarketLifecycle event: {EventData}", e);
        }

        /// <summary>
        /// Handles event lifecycle events from the WebSocket feed, processing event state changes
        /// and coordinating with brain instances through check-in data.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing event lifecycle data.</param>
        private async void HandleEventLifecycleEvent(object? sender, EventLifecycleEventArgs e)
        {
            if (e == null)
            {
                _logger?.LogWarning("Received null EventLifecycleEventArgs");
                return;
            }

            _webSocketEventCount++;
            _logger?.LogInformation("Received EventLifecycle event: {EventData}", e);

            // Process check-in data from brain instances
            try
            {
                var checkInData = JsonSerializer.Deserialize<CheckInData>(e.Data.GetRawText());
                if (checkInData != null && !string.IsNullOrEmpty(checkInData.BrainInstanceName))
                {
                    var update = new
                    {
                        BrainInstanceName = checkInData.BrainInstanceName,
                        MarketCount = checkInData.Markets?.Count ?? 0,
                        ErrorCount = checkInData.ErrorCount,
                        LastSnapshot = checkInData.LastSnapshot,
                        LastCheckIn = DateTime.UtcNow,
                        IsStartingUp = checkInData.IsStartingUp,
                        IsShuttingDown = checkInData.IsShuttingDown
                    };

                    List<object> updatesToSend = null!;
                    lock (_batchLock)
                    {
                        _pendingCheckInUpdates.Add(update);
                        if (_pendingCheckInUpdates.Count >= _signalRBatchSize)
                        {
                            updatesToSend = new List<object>(_pendingCheckInUpdates);
                            _pendingCheckInUpdates.Clear();
                        }
                    }

                    if (updatesToSend != null)
                    {
                        await SendBatchedCheckInUpdatesAsync(updatesToSend);
                    }

                    _logger?.LogInformation("Queued CheckInUpdate for bot with {MarketCount} markets", checkInData.Markets?.Count ?? 0);
                }
                else
                {
                    _logger?.LogWarning("Deserialized CheckInData is null or invalid");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to process check-in from EventLifecycle event");
            }
        }

        /// <summary>
        /// Starts periodic fetching of announcements and exchange schedule data at the configured interval.
        /// This ensures the system stays synchronized with Kalshi's API data.
        /// The interval is configurable via appsettings.json (OverseerConfig.ApiFetchIntervalMinutes).
        /// </summary>
        public void StartApiDataFetchTimer()
        {
            if (_apiFetchTimer != null)
            {
                _logger?.LogWarning("Periodic API data fetching is already running");
                return;
            }

            _apiFetchCancellationTokenSource = new CancellationTokenSource();
            _apiFetchTimer = new Timer(async _ => await FetchApiDataPeriodicallyAsync(), null, TimeSpan.Zero, _apiFetchInterval);

            _logger?.LogInformation("Started periodic API data fetching every {Interval}", _apiFetchInterval);
        }

        /// <summary>
        /// Stops periodic fetching of API data and cleans up resources.
        /// </summary>
        public void StopApiDataFetchTimer()
        {
            if (_apiFetchTimer != null)
            {
                _apiFetchTimer.Dispose();
                _apiFetchTimer = null;
            }

            if (_apiFetchCancellationTokenSource != null)
            {
                _apiFetchCancellationTokenSource.Cancel();
                _apiFetchCancellationTokenSource.Dispose();
                _apiFetchCancellationTokenSource = null;
            }

            _logger?.LogInformation("Stopped periodic API data fetching");
        }

        /// <summary>
        /// Starts periodic logging of system information at the configured interval for monitoring purposes.
        /// The interval is configurable via appsettings.json (OverseerConfig.SystemInfoLogIntervalMinutes).
        /// </summary>
        public void StartSystemInfoLoggingTimer()
        {
            if (_systemInfoLogTimer != null)
            {
                _logger?.LogWarning("Periodic system info logging is already running");
                return;
            }

            _systemInfoLogCancellationTokenSource = new CancellationTokenSource();
            _systemInfoLogTimer = new Timer(async _ => await LogSystemInfoPeriodicallyAsync(), null, TimeSpan.Zero, _systemInfoLogInterval);

            _logger?.LogInformation("Started periodic system info logging every {Interval}", _systemInfoLogInterval);
        }

        /// <summary>
        /// Stops periodic logging of system information and cleans up resources.
        /// </summary>
        public void StopSystemInfoLoggingTimer()
        {
            if (_systemInfoLogTimer != null)
            {
                _systemInfoLogTimer.Dispose();
                _systemInfoLogTimer = null;
            }

            if (_systemInfoLogCancellationTokenSource != null)
            {
                _systemInfoLogCancellationTokenSource.Cancel();
                _systemInfoLogCancellationTokenSource.Dispose();
                _systemInfoLogCancellationTokenSource = null;
            }

            _logger?.LogInformation("Stopped periodic system info logging");
        }

        /// <summary>
        /// Periodically fetches announcements and exchange schedule data from Kalshi API
        /// to keep the system synchronized with current market information.
        /// </summary>
        private async Task FetchApiDataPeriodicallyAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                if (_apiFetchCancellationTokenSource?.IsCancellationRequested == true)
                    return;

                using var scope = _scopeFactory.CreateScope();
                var kalshiApiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();

                _logger?.LogInformation("Starting periodic API data fetch at {Timestamp}", DateTime.UtcNow);

                // Fetch announcements
                _logger?.LogInformation("Fetching announcements...");
                var announcementsResult = await kalshiApiService.FetchAnnouncementsAsync();
                _logger?.LogInformation("Announcements fetch completed: {ProcessedCount} processed, {ErrorCount} errors",
                    announcementsResult.ProcessedCount, announcementsResult.ErrorCount);

                // Fetch exchange schedule
                _logger?.LogInformation("Fetching exchange schedule...");
                var exchangeScheduleResult = await kalshiApiService.FetchExchangeScheduleAsync();
                _logger?.LogInformation("Exchange schedule fetch completed: {ProcessedCount} processed, {ErrorCount} errors",
                    exchangeScheduleResult.ProcessedCount, exchangeScheduleResult.ErrorCount);

                stopwatch.Stop();
                _totalApiFetchTime += stopwatch.Elapsed;
                _apiFetchCount++;

                _logger?.LogInformation("Periodic API data fetch completed successfully at {Timestamp} in {Duration}ms", DateTime.UtcNow, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger?.LogError(ex, "Error during periodic API data fetch: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Logs system information including IP address and hostname to the database
        /// for monitoring and discovery purposes.
        /// </summary>
        private async Task LogSystemInfoAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<KalshiBotData.Data.Interfaces.IKalshiBotContext>();

                // Get local IP address
                var hostName = System.Net.Dns.GetHostName();

                var localIP = System.Net.Dns.GetHostEntry(hostName).AddressList
                    .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?
                    .ToString() ?? "127.0.0.1";

                var overseerInfo = new BacklashDTOs.Data.OverseerInfo
                {
                    HostName = hostName,
                    IPAddress = localIP,
                    Port = 5000, // Default port
                    StartTime = DateTime.UtcNow,
                    IsActive = true,
                    ServiceName = "KalshiBotOverseer",
                    LastHeartbeat = DateTime.UtcNow
                };

                await context.AddOrUpdateOverseerInfo(overseerInfo);
                _logger?.LogInformation("System info logged: {HostName} at {IPAddress}:{Port}", hostName, localIP, 5000);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to log system info to database");
            }
        }

        /// <summary>
        /// Periodically logs system information for monitoring purposes.
        /// </summary>
        private async Task LogSystemInfoPeriodicallyAsync()
        {
            try
            {
                if (_systemInfoLogCancellationTokenSource?.IsCancellationRequested == true)
                    return;

                await LogSystemInfoAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during periodic system info logging: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Logs comprehensive information about all brain persistence objects for monitoring and debugging.
        /// Provides a summary of brain states, configurations, and performance metrics.
        /// Uses batch processing with configurable batch size for better performance with large brain sets.
        /// </summary>
        private async Task LogBrainPersistenceStateAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var brainService = scope.ServiceProvider.GetRequiredService<BrainPersistenceService>();

                var allBrains = brainService.GetAllBrains().ToList();
                var brainCount = allBrains.Count;

                _logger?.LogInformation("Brain persistence state: {BrainCount} brain instances found", brainCount);

                // Batch processing for better performance
                for (int i = 0; i < brainCount; i += _brainBatchSize)
                {
                    var batch = allBrains.Skip(i).Take(_brainBatchSize);
                    foreach (var brain in batch)
                    {
                        _logger?.LogInformation("Brain '{BrainName}': Mode={Mode}, Markets={CurrentCount}/{TargetCount}, Errors={ErrorCount}, Connected={WebSocketStatus}",
                            brain.BrainInstanceName,
                            brain.Mode,
                            brain.CurrentMarketTickers?.Count ?? 0,
                            brain.TargetMarketTickers?.Count ?? 0,
                            brain.ErrorCount,
                            brain.IsWebSocketConnected);

                        // Log market tickers summary (first 5 for brevity)
                        if (brain.CurrentMarketTickers != null && brain.CurrentMarketTickers.Count > 0)
                        {
                            var tickersToShow = brain.CurrentMarketTickers.Take(5);
                            _logger?.LogInformation("Brain '{BrainName}' markets: {Tickers}{Overflow}",
                                brain.BrainInstanceName,
                                string.Join(", ", tickersToShow),
                                brain.CurrentMarketTickers.Count > 5 ? $" (+{brain.CurrentMarketTickers.Count - 5} more)" : "");
                        }
                    }

                    // Small delay between batches to prevent log flooding
                    if (i + _brainBatchSize < brainCount)
                    {
                        await Task.Delay(100);
                    }
                }

                _logger?.LogInformation("Completed brain persistence state logging");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to log brain persistence state");
            }
        }

        /// <summary>
        /// Sends batched check-in updates to SignalR clients.
        /// Batch size is configurable via appsettings.json (OverseerConfig.SignalRBatchSize).
        /// </summary>
        private async Task SendBatchedCheckInUpdatesAsync(List<object> updates)
        {
            try
            {
                foreach (var update in updates)
                {
                    await _hubContext.Clients.All.SendAsync("CheckInUpdate", update);
                }
                _logger?.LogInformation("Sent {Count} batched CheckInUpdates", updates.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to send batched CheckInUpdates");
            }
        }

        /// <summary>
        /// Disposes of the overseer and cleans up all resources including timers and event subscriptions.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            StopApiDataFetchTimer();
            StopSystemInfoLoggingTimer();
            Unsubscribe();

            // Send any remaining batched updates
            lock (_batchLock)
            {
                if (_pendingCheckInUpdates.Count > 0)
                {
                    // Note: In a real implementation, you'd want to await this, but Dispose is sync
                    // For now, we'll just clear the list
                    _pendingCheckInUpdates.Clear();
                }
            }

            _disposed = true;
        }
    }
}
