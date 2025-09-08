// Overseer.cs
using KalshiBotAPI.WebSockets.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmokehouseBot.KalshiAPI.Interfaces;
using SmokehouseDTOs;
using System.Threading;
using System.Threading.Tasks;

namespace SmokehouseBot.Services
{
    public class Overseer : IDisposable
    {
        private readonly IKalshiWebSocketClient _webSocketClient;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<Overseer> _logger;
        private readonly SignalRService _signalRService;
        private Timer? _apiFetchTimer;
        private CancellationTokenSource? _apiFetchCancellationTokenSource;
        private bool _disposed = false;

        public Overseer(IKalshiWebSocketClient webSocketClient, IServiceScopeFactory scopeFactory, ILogger<Overseer> logger, SignalRService signalRService)
        {
            _webSocketClient = webSocketClient;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _signalRService = signalRService;
        }

        public async void Start()
        {
            // Subscribe to the specific events
            _webSocketClient.FillReceived += OnFillReceived;
            _webSocketClient.MarketLifecycleReceived += OnMarketLifecycleReceived;
            _webSocketClient.EventLifecycleReceived += OnEventLifecycleReceived;

            _logger?.LogInformation("Subscribed to Fill, MarketLifecycle, and EventLifecycle events.");

            await _signalRService.StartAsync();

            StartPeriodicApiFetching();
        }

        public async void Stop()
        {
            Unsubscribe();
            _logger?.LogInformation("Unsubscribed from events.");

            await _signalRService.StopAsync();
        }

        private void Unsubscribe()
        {
            if (_disposed) return;
            _webSocketClient.FillReceived -= OnFillReceived;
            _webSocketClient.MarketLifecycleReceived -= OnMarketLifecycleReceived;
            _webSocketClient.EventLifecycleReceived -= OnEventLifecycleReceived;
        }

        private async void OnFillReceived(object sender, FillEventArgs e)
        {
            // Handle the fill event (e.g., log or process)
            _logger?.LogInformation("Received Fill event: {EventData}", e);

            // Transmit the fill event via SignalR
            await _signalRService.SendMessageAsync("ReceiveFill", $"Fill event: {e}");
        }

        private async void OnMarketLifecycleReceived(object sender, MarketLifecycleEventArgs e)
        {
            // Handle the market lifecycle event
            _logger?.LogInformation("Received MarketLifecycle event: {EventData}", e);

            // Transmit the market lifecycle event via SignalR
            await _signalRService.SendMessageAsync("ReceiveMarketLifecycle", $"MarketLifecycle event: {e}");
        }

        private async void OnEventLifecycleReceived(object sender, EventLifecycleEventArgs e)
        {
            // Handle the event lifecycle event
            _logger?.LogInformation("Received EventLifecycle event: {EventData}", e);

            // Transmit the event lifecycle event via SignalR
            await _signalRService.SendMessageAsync("ReceiveEventLifecycle", $"EventLifecycle event: {e}");
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
            _apiFetchTimer = new Timer(async _ => await FetchApiDataAsync(), null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

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