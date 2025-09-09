using Microsoft.AspNetCore.SignalR;
using KalshiBotData.Data.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using BacklashDTOs;
using System.Security.Cryptography;
using System.Text;

namespace KalshiBotOverseer
{
    public class OverseerHub : Hub
    {
        private readonly ILogger<OverseerHub> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public OverseerHub(ILogger<OverseerHub> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var clientId = httpContext?.Request.Query["clientId"].ToString();
            var authToken = httpContext?.Request.Query["authToken"].ToString();

            if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(authToken))
            {
                if (await ValidateClient(clientId, authToken))
                {
                    await UpdateClientConnection(clientId, Context.ConnectionId);
                    _logger.LogInformation("Authenticated client connected: {ClientId}", clientId);
                }
                else
                {
                    _logger.LogWarning("Failed authentication for client: {ClientId}", clientId);
                    Context.Abort();
                    return;
                }
            }
            else
            {
                _logger.LogWarning("Missing authentication parameters for connection: {ConnectionId}", Context.ConnectionId);
                Context.Abort();
                return;
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogDebug("Client disconnected: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task Handshake(string clientId, string clientName, string clientType)
        {
            _logger.LogInformation("Handshake request from client: {ClientId}, Name: {ClientName}, Type: {ClientType}",
                clientId, clientName, clientType);

            try
            {
                using var scope = _scopeFactory.CreateScope();
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

        public async Task CheckIn(object checkInData)
        {
            _logger.LogInformation("Received CheckIn from client: {ConnectionId}", Context.ConnectionId);

            try
            {
                // Process CheckIn data - extract markets, error count, last snapshot
                // This can be expanded to update overseer state or trigger actions
                await Clients.Caller.SendAsync("CheckInReceived", new
                {
                    Success = true,
                    Timestamp = DateTime.UtcNow,
                    Message = "CheckIn processed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CheckIn from client: {ConnectionId}", Context.ConnectionId);
                await Clients.Caller.SendAsync("CheckInReceived", new
                {
                    Success = false,
                    Message = $"CheckIn processing failed: {ex.Message}"
                });
            }
        }

        private async Task<bool> ValidateClient(string clientId, string authToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

                var client = await context.GetSignalRClient(clientId);
                if (client == null || !client.IsActive)
                    return false;

                // Validate auth token
                var expectedToken = GenerateAuthToken(client.ClientId, client.ClientName);
                if (authToken != expectedToken)
                    return false;

                // Update last seen
                await context.UpdateSignalRClientLastSeen(clientId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating client credentials for {ClientId}", clientId);
                return false;
            }
        }

        private async Task UpdateClientConnection(string clientId, string connectionId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
                await context.UpdateSignalRClientConnection(clientId, connectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating client connection for {ClientId}", clientId);
            }
        }

        private string GenerateAuthToken(string clientId, string clientName)
        {
            // Simple token generation - in production, use proper JWT or secure tokens
            using var sha256 = SHA256.Create();
            var input = $"{clientId}:{clientName}:{DateTime.UtcNow.Date:yyyy-MM-dd}";
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}