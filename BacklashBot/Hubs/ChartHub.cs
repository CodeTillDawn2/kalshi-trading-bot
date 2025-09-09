using Microsoft.AspNetCore.SignalR;
using BacklashBot.Services.Interfaces;
using KalshiBotData.Models;
using KalshiBotData.Data.Interfaces;
using BacklashDTOs;
using System.Security.Cryptography;
using System.Text;

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

        public async Task Handshake(string clientId, string clientName, string clientType)
        {
            _logger.LogInformation("Handshake request from client: {ClientId}, Name: {ClientName}, Type: {ClientType}",
                clientId, clientName, clientType);

            try
            {
                using var scope = _serviceFactory.GetScopeManager().CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

                // Get client IP address
                var httpContext = Context.GetHttpContext();
                var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                // Check if client already exists
                var existingClient = await context.GetSignalRClient(clientId);
                if (existingClient == null)
                {
                    // Register new client
                    var newClient = new BacklashDTOs.SignalRClient
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

                    await context.AddOrUpdateSignalRClient(newClient);
                    _logger.LogInformation("New client registered: {ClientId}", clientId);
                }
                else
                {
                    // Update existing client
                    existingClient.ConnectionId = Context.ConnectionId;
                    existingClient.LastSeen = DateTime.UtcNow;
                    existingClient.IsActive = true;
                    await context.AddOrUpdateSignalRClient(existingClient);
                    _logger.LogInformation("Existing client updated: {ClientId}", clientId);
                }

                // Send handshake response with auth token
                var response = new
                {
                    Success = true,
                    AuthToken = existingClient?.AuthToken ?? GenerateAuthToken(clientId, clientName),
                    Message = "Handshake successful"
                };

                await Clients.Caller.SendAsync("HandshakeResponse", response);
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

        private string GenerateAuthToken(string clientId, string clientName)
        {
            // Simple token generation - in production, use proper JWT or secure tokens
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var input = $"{clientId}:{clientName}:{DateTime.UtcNow.Date:yyyy-MM-dd}";
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public async Task ReceiveOverseerMessage(string messageType, string message)
        {
            _logger.LogInformation("Received message from Overseer: {MessageType} - {Message}", messageType, message);

            // Handle different message types from the overseer
            switch (messageType.ToLower())
            {
                case "status_request":
                    await HandleStatusRequest(message);
                    break;
                case "command":
                    await HandleCommand(message);
                    break;
                case "acknowledgment":
                    _logger.LogDebug("Acknowledgment received from Overseer: {Message}", message);
                    break;
                default:
                    _logger.LogWarning("Unknown message type from Overseer: {MessageType}", messageType);
                    break;
            }
        }

        private async Task HandleStatusRequest(string request)
        {
            _logger.LogInformation("Handling status request from Overseer: {Request}", request);

            // Send back status information
            var statusData = new
            {
                Timestamp = DateTime.UtcNow,
                MarketsWatched = await _serviceFactory.GetMarketDataService().FetchWatchedMarketsAsync(),
                ErrorCount = _serviceFactory.GetBacklashErrorHandler().ErrorCount,
                LastSnapshot = _serviceFactory.GetBacklashErrorHandler().LastSuccessfulSnapshot
            };

            await Clients.Caller.SendAsync("StatusResponse", statusData);
        }

        private async Task HandleCommand(string command)
        {
            _logger.LogInformation("Handling command from Overseer: {Command}", command);

            // Example command handling - can be expanded
            if (command.ToLower().Contains("restart"))
            {
                _logger.LogWarning("Restart command received from Overseer - not implemented yet");
                await Clients.Caller.SendAsync("CommandResponse", "Restart command acknowledged but not implemented");
            }
            else
            {
                await Clients.Caller.SendAsync("CommandResponse", $"Unknown command: {command}");
            }
        }
    }
}
