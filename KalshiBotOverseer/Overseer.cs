// Overseer.cs
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

namespace KalshiBotOverseer
{
    public class Overseer : IDisposable
    {
        private readonly IKalshiWebSocketClient _webSocketClient;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<Overseer> _logger;
        private readonly IHubContext<ChartHub> _hubContext;
        private Timer? _apiFetchTimer;
        private CancellationTokenSource? _apiFetchCancellationTokenSource;
        private Timer? _overseerLogTimer;
        private CancellationTokenSource? _overseerLogCancellationTokenSource;
        private bool _disposed = false;

        public Overseer(IKalshiWebSocketClient webSocketClient, IServiceScopeFactory scopeFactory, ILogger<Overseer> logger, IHubContext<ChartHub> hubContext)
        {
            _webSocketClient = webSocketClient;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _hubContext = hubContext;
        }

        public async Task Start()
        {
            // Log overseer IP to database
            await LogOverseerInfoAsync();

            // Log all BrainPersistence information for debugging
            await LogAllBrainPersistenceInfoAsync();

            // Subscribe to the specific events
            _webSocketClient.FillReceived += OnFillReceived;
            _webSocketClient.MarketLifecycleReceived += OnMarketLifecycleReceived;
            _webSocketClient.EventLifecycleReceived += OnEventLifecycleReceived;

            _logger?.LogInformation("Subscribed to Fill, MarketLifecycle, and EventLifecycle events.");

            StartPeriodicApiFetching();
            StartPeriodicOverseerLogging();
        }

        public void Stop()
        {
            Unsubscribe();
            _logger?.LogInformation("Unsubscribed from events.");
        }

        private void Unsubscribe()
        {
            if (_disposed) return;
            _webSocketClient.FillReceived -= OnFillReceived;
            _webSocketClient.MarketLifecycleReceived -= OnMarketLifecycleReceived;
            _webSocketClient.EventLifecycleReceived -= OnEventLifecycleReceived;
        }

        private void OnFillReceived(object? sender, FillEventArgs e)
        {
            // Handle the fill event (e.g., log or process)
            _logger?.LogInformation("Received Fill event: {EventData}", e);

            // Fill event processed
        }

        private void OnMarketLifecycleReceived(object? sender, MarketLifecycleEventArgs e)
        {
            // Handle the market lifecycle event
            _logger?.LogInformation("Received MarketLifecycle event: {EventData}", e);

            // Market lifecycle event processed
        }

        private async void OnEventLifecycleReceived(object? sender, EventLifecycleEventArgs e)
        {
            // Handle the event lifecycle event
            _logger?.LogInformation("Received EventLifecycle event: {EventData}", e);

            // Check if this is a check-in from a bot
            try
            {
                // e.Data is JsonElement, deserialize it
                var checkInData = JsonSerializer.Deserialize<CheckInData>(e.Data.GetRawText());
                if (checkInData != null)
                {
                    // Send CheckInUpdate to all connected clients
                    await _hubContext.Clients.All.SendAsync("CheckInUpdate", new
                    {
                        brainInstanceName = checkInData.BrainInstanceName,
                        marketCount = checkInData.Markets?.Count ?? 0,
                        errorCount = checkInData.ErrorCount,
                        lastSnapshot = checkInData.LastSnapshot,
                        lastCheckIn = DateTime.UtcNow,
                        isStartingUp = checkInData.IsStartingUp,
                        isShuttingDown = checkInData.IsShuttingDown
                    });

                    _logger?.LogInformation("Sent CheckInUpdate for bot with {MarketCount} markets", checkInData.Markets?.Count ?? 0);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to process check-in from EventLifecycle event");
            }

            // Event lifecycle event processed
        }

        /// <summary>
        /// Starts periodic fetching of announcements and exchange schedule data every minute
        /// </summary>
        public void StartPeriodicApiFetching()
        {
            if (_apiFetchTimer != null)
            {
                _logger?.LogWarning("Periodic API fetching is already running");
                return;
            }

            _apiFetchCancellationTokenSource = new CancellationTokenSource();
            _apiFetchTimer = new Timer(async _ => await FetchApiDataAsync(), null, TimeSpan.Zero, TimeSpan.FromMinutes(10));

            _logger?.LogInformation("Started periodic API fetching every minute");
        }

        /// <summary>
        /// Stops periodic fetching of API data
        /// </summary>
        public void StopPeriodicApiFetching()
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

            _logger?.LogInformation("Stopped periodic API fetching");
        }

        /// <summary>
        /// Starts periodic logging of overseer info every minute
        /// </summary>
        public void StartPeriodicOverseerLogging()
        {
            if (_overseerLogTimer != null)
            {
                _logger?.LogWarning("Periodic overseer logging is already running");
                return;
            }

            _overseerLogCancellationTokenSource = new CancellationTokenSource();
            _overseerLogTimer = new Timer(async _ => await LogOverseerInfoPeriodicallyAsync(), null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

            _logger?.LogInformation("Started periodic overseer logging every minute");
        }

        /// <summary>
        /// Stops periodic logging of overseer info
        /// </summary>
        public void StopPeriodicOverseerLogging()
        {
            if (_overseerLogTimer != null)
            {
                _overseerLogTimer.Dispose();
                _overseerLogTimer = null;
            }

            if (_overseerLogCancellationTokenSource != null)
            {
                _overseerLogCancellationTokenSource.Cancel();
                _overseerLogCancellationTokenSource.Dispose();
                _overseerLogCancellationTokenSource = null;
            }

            _logger?.LogInformation("Stopped periodic overseer logging");
        }

        /// <summary>
        /// Fetches announcements and exchange schedule data from Kalshi API
        /// </summary>
        private async Task FetchApiDataAsync()
        {
            try
            {
                if (_apiFetchCancellationTokenSource?.IsCancellationRequested == true)
                    return;

                using var scope = _scopeFactory.CreateScope();
                var kalshiApiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();

                _logger?.LogInformation("Starting periodic API data fetch at {Timestamp}", DateTime.UtcNow);

                // Fetch announcements
                var announcementsResult = await kalshiApiService.FetchAnnouncementsAsync();
                _logger?.LogInformation("Announcements fetch completed: {ProcessedCount} processed, {ErrorCount} errors",
                    announcementsResult.ProcessedCount, announcementsResult.ErrorCount);

                // Fetch exchange schedule
                var exchangeScheduleResult = await kalshiApiService.FetchExchangeScheduleAsync();
                _logger?.LogInformation("Exchange schedule fetch completed: {ProcessedCount} processed, {ErrorCount} errors",
                    exchangeScheduleResult.ProcessedCount, exchangeScheduleResult.ErrorCount);

                _logger?.LogInformation("Periodic API data fetch completed successfully at {Timestamp}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during periodic API data fetch: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Logs overseer information including IP address to database
        /// </summary>
        private async Task LogOverseerInfoAsync()
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
                _logger?.LogInformation("OVERSEER- Overseer info: {HostName} at {IPAddress}:{Port}", hostName, localIP, 5000);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "OVERSEER- Failed to log overseer info to database");
            }
        }

        /// <summary>
        /// Periodically logs overseer info
        /// </summary>
        private async Task LogOverseerInfoPeriodicallyAsync()
        {
            try
            {
                if (_overseerLogCancellationTokenSource?.IsCancellationRequested == true)
                    return;

                await LogOverseerInfoAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during periodic overseer logging: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Logs comprehensive information about all BrainPersistence objects for debugging
        /// </summary>
        private async Task LogAllBrainPersistenceInfoAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var brainService = scope.ServiceProvider.GetRequiredService<BrainPersistenceService>();

                _logger?.LogInformation("BRAIN-PERSISTENCE-DEBUG: BrainPersistence logging initialized. Service available: {Available}",
                    brainService != null);

                var allBrains = brainService.GetAllBrains();
                var brainCount = 0;

                foreach (var brain in allBrains)
                {
                    brainCount++;
                    _logger?.LogInformation("BRAIN-PERSISTENCE-DEBUG: Brain '{BrainName}' - Mode: {Mode}, CurrentMarkets: {CurrentMarketCount}, TargetMarkets: {TargetMarketCount}, ErrorCount: {ErrorCount}, LastSeen: {LastSeen}, IsWebSocketConnected: {WebSocketStatus}",
                        brain.BrainInstanceName,
                        brain.Mode,
                        brain.CurrentMarketTickers?.Count ?? 0,
                        brain.TargetMarketTickers?.Count ?? 0,
                        brain.ErrorCount,
                        brain.LastSeen,
                        brain.IsWebSocketConnected);

                    // Log configuration settings
                    _logger?.LogInformation("BRAIN-PERSISTENCE-DEBUG: Brain '{BrainName}' Config - WatchPositions: {WatchPositions}, WatchOrders: {WatchOrders}, ManagedWatchList: {ManagedWatchList}, CaptureSnapshots: {CaptureSnapshots}, TargetWatches: {TargetWatches}",
                        brain.BrainInstanceName,
                        brain.WatchPositions,
                        brain.WatchOrders,
                        brain.ManagedWatchList,
                        brain.CaptureSnapshots,
                        brain.TargetWatches);

                    // Log performance settings
                    _logger?.LogInformation("BRAIN-PERSISTENCE-DEBUG: Brain '{BrainName}' Performance - MinInterest: {MinInterest}, UsageMin: {UsageMin}, UsageMax: {UsageMax}, IsStartingUp: {StartingUp}, IsShuttingDown: {ShuttingDown}",
                        brain.BrainInstanceName,
                        brain.MinimumInterest,
                        brain.UsageMin,
                        brain.UsageMax,
                        brain.IsStartingUp,
                        brain.IsShuttingDown);

                    // Log historical data counts
                    _logger?.LogInformation("BRAIN-PERSISTENCE-DEBUG: Brain '{BrainName}' History Counts - CPU: {CpuCount}, Events: {EventCount}, Tickers: {TickerCount}, Notifications: {NotificationCount}, Orderbook: {OrderbookCount}, Markets: {MarketCount}, Errors: {ErrorCount}",
                        brain.BrainInstanceName,
                        brain.CpuUsageHistory?.Count ?? 0,
                        brain.EventQueueHistory?.Count ?? 0,
                        brain.TickerQueueHistory?.Count ?? 0,
                        brain.NotificationQueueHistory?.Count ?? 0,
                        brain.OrderbookQueueHistory?.Count ?? 0,
                        brain.MarketCountHistory?.Count ?? 0,
                        brain.ErrorHistory?.Count ?? 0);

                    // Log refresh metrics history counts
                    _logger?.LogInformation("BRAIN-PERSISTENCE-DEBUG: Brain '{BrainName}' Refresh History - CycleSeconds: {CycleSecondsCount}, CycleInterval: {CycleIntervalCount}, MarketCount: {MarketCountHistory}, Usage%: {UsagePercentCount}, SampleDates: {SampleDateCount}, LastRefreshAcceptable: {LastRefreshAcceptable}",
                        brain.BrainInstanceName,
                        brain.RefreshCycleSecondsHistory?.Count ?? 0,
                        brain.RefreshCycleIntervalHistory?.Count ?? 0,
                        brain.RefreshMarketCountHistory?.Count ?? 0,
                        brain.RefreshUsagePercentageHistory?.Count ?? 0,
                        brain.PerformanceSampleDateHistory?.Count ?? 0,
                        brain.LastRefreshTimeAcceptable);

                    // Log current market tickers (first 10 for brevity)
                    if (brain.CurrentMarketTickers != null && brain.CurrentMarketTickers.Count > 0)
                    {
                        var tickersToShow = brain.CurrentMarketTickers.Take(10);
                        _logger?.LogInformation("BRAIN-PERSISTENCE-DEBUG: Brain '{BrainName}' Current Tickers ({TotalCount}): {Tickers}",
                            brain.BrainInstanceName,
                            brain.CurrentMarketTickers.Count,
                            string.Join(", ", tickersToShow));
                    }

                    // Log target market tickers (first 10 for brevity)
                    if (brain.TargetMarketTickers != null && brain.TargetMarketTickers.Count > 0)
                    {
                        var tickersToShow = brain.TargetMarketTickers.Take(10);
                        _logger?.LogInformation("BRAIN-PERSISTENCE-DEBUG: Brain '{BrainName}' Target Tickers ({TotalCount}): {Tickers}",
                            brain.BrainInstanceName,
                            brain.TargetMarketTickers.Count,
                            string.Join(", ", tickersToShow));
                    }
                }

                _logger?.LogInformation("BRAIN-PERSISTENCE-DEBUG: Completed logging for {BrainCount} brain instances", brainCount);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "BRAIN-PERSISTENCE-DEBUG: Failed to log BrainPersistence information");
            }
        }

        /// <summary>
        /// Manually triggers a single API data fetch (useful for testing)
        /// </summary>
        public async Task TriggerManualApiFetchAsync()
        {
            _logger?.LogInformation("Manual API fetch triggered");
            await FetchApiDataAsync();
        }

        public void Dispose()
        {
            if (_disposed) return;
            StopPeriodicApiFetching();
            StopPeriodicOverseerLogging();
            Unsubscribe();
            _disposed = true;
        }
    }
}
