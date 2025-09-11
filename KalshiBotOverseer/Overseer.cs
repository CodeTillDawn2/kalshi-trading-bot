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

            // Subscribe to the specific events
            _webSocketClient.FillReceived += OnFillReceived;
            _webSocketClient.MarketLifecycleReceived += OnMarketLifecycleReceived;
            _webSocketClient.EventLifecycleReceived += OnEventLifecycleReceived;

            _logger?.LogInformation("Subscribed to Fill, MarketLifecycle, and EventLifecycle events.");

            StartPeriodicApiFetching();
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
                        BrainInstanceName = "UnknownBot", // We don't have this info from WebSocket events
                        MarketCount = checkInData.Markets?.Count ?? 0,
                        ErrorCount = checkInData.ErrorCount,
                        LastSnapshot = checkInData.LastSnapshot,
                        LastCheckIn = DateTime.UtcNow,
                        IsStartingUp = checkInData.IsStartingUp,
                        IsShuttingDown = checkInData.IsShuttingDown
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
            Unsubscribe();
            _disposed = true;
        }
    }
}
