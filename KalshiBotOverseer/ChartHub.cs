using Microsoft.AspNetCore.SignalR;
using KalshiBotOverseer.Services;
using KalshiBotAPI.Configuration;
using KalshiBotAPI.KalshiAPI;
using KalshiBotAPI.Websockets;
using KalshiBotAPI.WebSockets.Interfaces;
using KalshiBotData.Data;
using KalshiBotData.Data.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace KalshiBotOverseer
{
    public class ChartHub : Hub
    {
        private static readonly HashSet<string> _connectedClients = new HashSet<string>();
        private static readonly ConcurrentDictionary<string, ClientInfo> _clientInfo = new();
        private readonly ILogger<ChartHub> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public ChartHub(
            ILogger<ChartHub> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public override async Task OnConnectedAsync()
        {
            lock (_connectedClients)
            {
                _connectedClients.Add(Context.ConnectionId);
            }

            var httpContext = Context.GetHttpContext();
            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            _logger.LogInformation("Client connected: {ConnectionId} from IP: {IPAddress}. Total clients: {ClientCount}",
                Context.ConnectionId, ipAddress, _connectedClients.Count);

            await base.OnConnectedAsync();
        }

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
                _clientInfo.Clear();
            }
        }

        public async Task Handshake(string clientId, string clientName, string clientType)
        {
            _logger.LogInformation("Handshake request from client: {ClientId}, Name: {ClientName}, Type: {ClientType}",
                clientId, clientName, clientType);

            try
            {
                var httpContext = Context.GetHttpContext();
                var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                // Store client information
                var clientInfo = new ClientInfo
                {
                    ClientId = clientId,
                    ClientName = clientName,
                    ClientType = clientType,
                    IPAddress = ipAddress,
                    ConnectionId = Context.ConnectionId,
                    LastSeen = DateTime.UtcNow
                };

                _clientInfo[Context.ConnectionId] = clientInfo;

                // Log to database if available
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

                    var signalRClient = new BacklashDTOs.SignalRClient
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

                    await context.AddOrUpdateSignalRClient(signalRClient);
                    _logger.LogInformation("Client registered in database: {ClientId} from {IPAddress}", clientId, ipAddress);
                }
                catch (Exception dbEx)
                {
                    _logger.LogWarning(dbEx, "Failed to log client to database: {ClientId}", clientId);
                }

                // Send handshake response
                var response = new
                {
                    Success = true,
                    AuthToken = GenerateAuthToken(clientId, clientName),
                    Message = "Handshake successful"
                };

                await Clients.Caller.SendAsync("HandshakeResponse", response);
                _logger.LogInformation("Handshake completed for client: {ClientId}", clientId);
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

        public async Task CheckIn(CheckInData checkInData)
        {
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

                _logger.LogInformation("CheckIn received from {ClientId} ({ClientName}): {MarketCount} markets, ErrorCount: {ErrorCount}, LastSnapshot: {LastSnapshot}",
                    clientInfo.ClientId, clientInfo.ClientName,
                    checkInData.Markets?.Count ?? 0,
                    checkInData.ErrorCount,
                    checkInData.LastSnapshot);

                // Update client last seen
                clientInfo.LastSeen = DateTime.UtcNow;

                // Log CheckIn data to database if needed
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

                    // Update client last seen
                    var signalRClient = await context.GetSignalRClient(clientInfo.ClientId);
                    if (signalRClient != null)
                    {
                        signalRClient.LastSeen = DateTime.UtcNow;
                        await context.AddOrUpdateSignalRClient(signalRClient);
                    }

                    // Log CheckIn data
                    // Note: CheckInLog type may not exist yet, commenting out for now
                    /*
                    var checkInLog = new BacklashDTOs.CheckInLog
                    {
                        ClientId = clientInfo.ClientId,
                        ClientName = clientInfo.ClientName,
                        IPAddress = clientInfo.IPAddress,
                        MarketsWatched = checkInData.Markets,
                        ErrorCount = checkInData.ErrorCount,
                        LastSnapshot = checkInData.LastSnapshot,
                        Timestamp = DateTime.UtcNow
                    };

                    await context.AddCheckInLog(checkInLog);
                    */
                }
                catch (Exception dbEx)
                {
                    _logger.LogWarning(dbEx, "Failed to log CheckIn to database for client: {ClientId}", clientInfo.ClientId);
                }

                // Broadcast CheckIn data to all connected clients (including web UI)
                await Clients.All.SendAsync("CheckInUpdate", new
                {
                    BrainInstanceName = clientInfo.ClientName,
                    MarketCount = checkInData.Markets?.Count ?? 0,
                    ErrorCount = checkInData.ErrorCount,
                    LastSnapshot = checkInData.LastSnapshot,
                    LastCheckIn = DateTime.UtcNow
                });

                // Send acknowledgment
                await Clients.Caller.SendAsync("CheckInResponse", new
                {
                    Success = true,
                    Message = "CheckIn received successfully",
                    Timestamp = DateTime.UtcNow
                });

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
        }

        public async Task SendOverseerMessage(string messageType, string message)
        {
            _logger.LogInformation("Sending message to Overseer: {MessageType} - {Message}", messageType, message);

            // This method can be used by clients to send messages to the overseer
            // For now, just acknowledge receipt
            await Clients.Caller.SendAsync("MessageResponse", new
            {
                Success = true,
                MessageType = messageType,
                Message = "Message received by overseer"
            });
        }

        private string GenerateAuthToken(string clientId, string clientName)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var input = $"{clientId}:{clientName}:{DateTime.UtcNow.Date:yyyy-MM-dd}";
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public class ClientInfo
        {
            public string ClientId { get; set; } = "";
            public string ClientName { get; set; } = "";
            public string ClientType { get; set; } = "";
            public string IPAddress { get; set; } = "";
            public string ConnectionId { get; set; } = "";
            public DateTime LastSeen { get; set; }
        }
    }

    public class CheckInData
    {
        public List<string>? Markets { get; set; }
        public long ErrorCount { get; set; }
        public DateTime? LastSnapshot { get; set; }
    }
}