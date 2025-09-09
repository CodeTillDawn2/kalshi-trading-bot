using Microsoft.AspNetCore.SignalR;
using BacklashBot.Services.Interfaces;

namespace BacklashBot.Hubs
{
    public class ChartHub : Hub
    {
        private static readonly HashSet<string> _connectedClients = new HashSet<string>();
        private readonly ILogger<ChartHub> _logger;
        private readonly IServiceFactory _serviceFactory;

        public ChartHub(
            IServiceFactory serviceFactory,
            ILogger<ChartHub> logger)
        {
            _logger = logger;
            _serviceFactory = serviceFactory;
        }

        public override async Task OnConnectedAsync()
        {
            if (_serviceFactory.GetBroadcastService() != null)
            {
                lock (_connectedClients)
                {
                    _connectedClients.Add(Context.ConnectionId);
                }
                try
                {
                    _logger.LogDebug("Client connected: {ConnectionId}. Total clients: {ClientCount}", Context.ConnectionId, _connectedClients.Count);
                    await _serviceFactory.GetBroadcastService().BroadcastAllDataToClientAsync(Context.ConnectionId);
                    _logger.LogDebug("Initial data broadcast completed for client: {ConnectionId}", Context.ConnectionId);
                    await base.OnConnectedAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during OnConnectedAsync for client: {ConnectionId}", Context.ConnectionId);
                }
            }

        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            lock (_connectedClients)
            {
                _connectedClients.Remove(Context.ConnectionId);
            }
            _logger.LogDebug("Client disconnected: {ConnectionId}. Total clients: {ClientCount}", Context.ConnectionId, _connectedClients.Count);
            await base.OnDisconnectedAsync(exception);
        }

        public static bool HasConnectedClients()
        {
            lock (_connectedClients)
            {
                return _connectedClients.Any();
            }
        }

        public static void ClearConnectedClients()
        {
            lock (_connectedClients)
            {
                _connectedClients.Clear();
                // Optionally log this action if you inject a logger statically or via a service locator
            }
        }

        public async Task SubscribeToMarket(string marketTicker)
        {
            _logger.LogDebug("Subscribing to market: {MarketTicker}", marketTicker);
            if (string.IsNullOrWhiteSpace(marketTicker))
            {
                _logger.LogWarning("SubscribeToMarket failed: Market ticker is empty");
                await Clients.Caller.SendAsync("ReceiveError", "Market ticker cannot be empty");
                return;
            }

            try
            {
                await _serviceFactory.GetMarketDataService().AddMarketWatch(marketTicker);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to market: {MarketTicker}", marketTicker);
                await Clients.Caller.SendAsync("ReceiveError", $"Failed to subscribe to market: {ex.Message}");
            }
        }

        public async Task UnsubscribeFromMarket(string marketTicker)
        {
            _logger.LogInformation("Stats: Unsubscribing from market: {MarketTicker}", marketTicker);
            if (string.IsNullOrWhiteSpace(marketTicker))
            {
                _logger.LogWarning("UnsubscribeFromMarket failed: Market ticker is empty");
                await Clients.Caller.SendAsync("ReceiveError", "Market ticker cannot be empty");
                return;
            }

            try
            {
                await _serviceFactory.GetMarketDataService().UnwatchMarket(marketTicker);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsubscribing from market: {MarketTicker}", marketTicker);
                await Clients.Caller.SendAsync("ReceiveError", $"Failed to unsubscribe from market: {ex.Message}");
            }
        }
    }
}
