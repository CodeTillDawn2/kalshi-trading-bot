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
using KalshiBotOverseer.Models;

namespace KalshiBotOverseer
{
    public class ChartHub : Hub
    {
        private static readonly HashSet<string> _connectedClients = new HashSet<string>();
        private static readonly ConcurrentDictionary<string, ClientInfo> _clientInfo = new();
        private readonly ILogger<ChartHub> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly BrainPersistenceService _brainService;

        public ChartHub(
            ILogger<ChartHub> logger,
            IServiceScopeFactory scopeFactory,
            BrainPersistenceService brainService)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _brainService = brainService;
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
            _logger.LogInformation("CheckIn received from connection: {ConnectionId}", Context.ConnectionId);

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

                // Update current market tickers in persistence service
                if (!string.IsNullOrEmpty(clientInfo.ClientName))
                {
                    _brainService.UpdateCurrentMarketTickers(clientInfo.ClientName, checkInData.Markets ?? new List<string>());
                }

                // Update client last seen
                clientInfo.LastSeen = DateTime.UtcNow;

                // Get target market tickers for this brain
                var targetTickers = new string[0];
                if (!string.IsNullOrEmpty(clientInfo.ClientName))
                {
                    targetTickers = _brainService.GetTargetMarketTickers(clientInfo.ClientName).ToArray();
                }

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

                    // Log CheckIn data - removed commented CheckInLog code as type doesn't exist
                }
                catch (Exception dbEx)
                {
                    _logger.LogWarning(dbEx, "Failed to log CheckIn to database for client: {ClientId}", clientInfo.ClientId);
                }

                // Create comprehensive brain status data
                var brainStatus = new BrainStatusData
                {
                    BrainInstanceName = clientInfo.ClientName ?? "",
                    BrainLock = null, // Will be populated from database if needed

                    // Configuration from CheckInData
                    WatchPositions = checkInData.WatchPositions,
                    WatchOrders = checkInData.WatchOrders,
                    ManagedWatchList = checkInData.ManagedWatchList,
                    CaptureSnapshots = checkInData.CaptureSnapshots,
                    TargetWatches = checkInData.TargetWatches,
                    MinimumInterest = checkInData.MinimumInterest,
                    UsageMin = checkInData.UsageMin,
                    UsageMax = checkInData.UsageMax,

                    // Status information
                    LastSeen = DateTime.UtcNow,
                    IsStartingUp = checkInData.IsStartingUp,
                    IsShuttingDown = checkInData.IsShuttingDown,
                    IsWebSocketConnected = checkInData.IsWebSocketConnected,

                    // Performance metrics
                    CurrentCpuUsage = checkInData.CurrentCpuUsage,
                    EventQueueAvg = checkInData.EventQueueAvg,
                    TickerQueueAvg = checkInData.TickerQueueAvg,
                    NotificationQueueAvg = checkInData.NotificationQueueAvg,
                    OrderbookQueueAvg = checkInData.OrderbookQueueAvg,

                    // Market data
                    MarketCount = checkInData.Markets?.Count ?? 0,
                    ErrorCount = checkInData.ErrorCount,
                    LastSnapshot = checkInData.LastSnapshot,
                    LastCheckIn = DateTime.UtcNow,

                    // Market watch data
                    WatchedMarkets = checkInData.WatchedMarkets ?? new List<MarketWatchData>(),

                    // Current and target tickers
                    CurrentMarketTickers = checkInData.Markets ?? new List<string>(),
                    TargetMarketTickers = targetTickers.ToList()
                };

                // Broadcast comprehensive brain status to all connected clients (including web UI)
                await Clients.All.SendAsync("BrainStatusUpdate", brainStatus);

                // Send acknowledgment with target tickers
                await Clients.Caller.SendAsync("CheckInResponse", new
                {
                    Success = true,
                    Message = "CheckIn received successfully",
                    TargetTickers = targetTickers,
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
        // Basic market data
        public List<string>? Markets { get; set; }
        public long ErrorCount { get; set; }
        public DateTime? LastSnapshot { get; set; }
        public bool IsStartingUp { get; set; }
        public bool IsShuttingDown { get; set; }

        // Brain configuration
        public bool WatchPositions { get; set; }
        public bool WatchOrders { get; set; }
        public bool ManagedWatchList { get; set; }
        public bool CaptureSnapshots { get; set; }
        public int TargetWatches { get; set; }
        public double MinimumInterest { get; set; }
        public double UsageMin { get; set; }
        public double UsageMax { get; set; }

        // Performance metrics
        public double CurrentCpuUsage { get; set; }
        public double EventQueueAvg { get; set; }
        public double TickerQueueAvg { get; set; }
        public double NotificationQueueAvg { get; set; }
        public double OrderbookQueueAvg { get; set; }

        // Connection status
        public bool IsWebSocketConnected { get; set; }

        // Market watch data
        public List<MarketWatchData>? WatchedMarkets { get; set; }
    }
}