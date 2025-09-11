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

                // Validate CheckInData
                if (checkInData == null)
                {
                    _logger.LogWarning("CheckIn received with null data from client: {ConnectionId}", Context.ConnectionId);
                    await Clients.Caller.SendAsync("CheckInResponse", new
                    {
                        Success = false,
                        Message = "CheckIn data is null."
                    });
                    return;
                }

                if (string.IsNullOrEmpty(checkInData.BrainInstanceName))
                {
                    _logger.LogWarning("CheckIn received with missing BrainInstanceName from client: {ConnectionId}", Context.ConnectionId);
                    await Clients.Caller.SendAsync("CheckInResponse", new
                    {
                        Success = false,
                        Message = "BrainInstanceName is required."
                    });
                    return;
                }

                _logger.LogInformation("CheckIn received from {ClientId} ({ClientName}): {MarketCount} markets, ErrorCount: {ErrorCount}, LastSnapshot: {LastSnapshot}",
                    clientInfo.ClientId, clientInfo.ClientName,
                    checkInData.Markets?.Count ?? 0,
                    checkInData.ErrorCount,
                    checkInData.LastSnapshot);

                // Verify brain name consistency between handshake and CheckIn
                if (clientInfo.ClientName != checkInData.BrainInstanceName)
                {
                    _logger.LogWarning("Brain name mismatch! Handshake: '{HandshakeName}', CheckIn: '{CheckInName}' for client {ClientId}",
                        clientInfo.ClientName, checkInData.BrainInstanceName, clientInfo.ClientId);
                }

                // Log database brain data availability
                var dbBrainExists = _brainService.GetBrain(clientInfo.ClientName ?? "") != null;
                _logger.LogInformation("Database brain data check: ClientName='{ClientName}', ExistsInDB={Exists}",
                    clientInfo.ClientName, dbBrainExists);

                // Update current market tickers in persistence service
                if (!string.IsNullOrEmpty(clientInfo.ClientName))
                {
                    try
                    {
                        _brainService.UpdateCurrentMarketTickers(clientInfo.ClientName, checkInData.Markets ?? new List<string>());

                        // Update historical metrics
                        _brainService.UpdateMetricHistory(clientInfo.ClientName, "CpuUsage", checkInData.CurrentCpuUsage);
                        _brainService.UpdateMetricHistory(clientInfo.ClientName, "EventQueue", checkInData.EventQueueAvg);
                        _brainService.UpdateMetricHistory(clientInfo.ClientName, "TickerQueue", checkInData.TickerQueueAvg);
                        _brainService.UpdateMetricHistory(clientInfo.ClientName, "NotificationQueue", checkInData.NotificationQueueAvg);
                        _brainService.UpdateMetricHistory(clientInfo.ClientName, "OrderbookQueue", checkInData.OrderbookQueueAvg);
                        _brainService.UpdateMetricHistory(clientInfo.ClientName, "MarketCount", checkInData.Markets?.Count ?? 0);
                        _brainService.UpdateMetricHistory(clientInfo.ClientName, "Error", checkInData.ErrorCount);
                    }
                    catch (Exception brainEx)
                    {
                        _logger.LogWarning(brainEx, "Failed to update brain service for {ClientName}", clientInfo.ClientName);
                    }
                }

                // Update client last seen
                clientInfo.LastSeen = DateTime.UtcNow;

                // Get target market tickers for this brain
                var targetTickers = new string[0];
                if (!string.IsNullOrEmpty(clientInfo.ClientName))
                {
                    try
                    {
                        targetTickers = _brainService.GetTargetMarketTickers(clientInfo.ClientName).ToArray();
                    }
                    catch (Exception tickerEx)
                    {
                        _logger.LogWarning(tickerEx, "Failed to get target tickers for {ClientName}", clientInfo.ClientName);
                        targetTickers = new string[0];
                    }
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

                // Get existing brain data for historical metrics
                BrainPersistence existingBrain;
                if (!string.IsNullOrEmpty(clientInfo.ClientName))
                {
                    existingBrain = _brainService.GetBrain(clientInfo.ClientName);
                    _logger.LogInformation("Retrieved brain persistence data for '{BrainName}': CpuHistory={CpuCount}, EventHistory={EventCount}, ErrorHistory={ErrorCount}",
                        clientInfo.ClientName,
                        existingBrain.CpuUsageHistory?.Count ?? 0,
                        existingBrain.EventQueueHistory?.Count ?? 0,
                        existingBrain.ErrorHistory?.Count ?? 0);
                }
                else
                {
                    _logger.LogWarning("ClientName is null or empty, using default brain data");
                    existingBrain = new BrainPersistence { BrainInstanceName = "" };
                }

                // Create comprehensive brain status data
                var brainStatus = new BrainStatusData
                {
                    brainInstanceName = checkInData.BrainInstanceName,

                    // Basic market data
                    markets = checkInData.Markets,
                    errorCount = checkInData.ErrorCount,
                    lastSnapshot = checkInData.LastSnapshot,
                    isStartingUp = checkInData.IsStartingUp,
                    isShuttingDown = checkInData.IsShuttingDown,

                    // Brain configuration
                    watchPositions = checkInData.WatchPositions,
                    watchOrders = checkInData.WatchOrders,
                    managedWatchList = checkInData.ManagedWatchList,
                    captureSnapshots = checkInData.CaptureSnapshots,
                    targetWatches = checkInData.TargetWatches,
                    minimumInterest = checkInData.MinimumInterest,
                    usageMin = checkInData.UsageMin,
                    usageMax = checkInData.UsageMax,

                    // Performance metrics
                    currentCpuUsage = checkInData.CurrentCpuUsage,
                    eventQueueAvg = checkInData.EventQueueAvg,
                    tickerQueueAvg = checkInData.TickerQueueAvg,
                    notificationQueueAvg = checkInData.NotificationQueueAvg,
                    orderbookQueueAvg = checkInData.OrderbookQueueAvg,
                    lastRefreshCycleSeconds = checkInData.LastRefreshCycleSeconds,
                    lastRefreshCycleInterval = checkInData.LastRefreshCycleInterval,
                    lastRefreshMarketCount = checkInData.LastRefreshMarketCount,
                    lastRefreshUsagePercentage = checkInData.LastRefreshUsagePercentage,
                    lastRefreshTimeAcceptable = checkInData.LastRefreshTimeAcceptable,
                    lastPerformanceSampleDate = checkInData.LastPerformanceSampleDate,

                    // Connection status
                    isWebSocketConnected = checkInData.IsWebSocketConnected,

                    // Market watch data
                    watchedMarkets = checkInData.WatchedMarkets
                };

                // Broadcast comprehensive brain status to all connected clients (including web UI)
                // (A) log how many clients are connected on this hub instance
                var connections = _connectedClients.Count;
                _logger.LogInformation("[ChartHub] Broadcasting BrainStatusUpdate for {Brain} to {Count} connections",
                    brainStatus.brainInstanceName, connections);

                // (B) send the real payload
                await Clients.All.SendAsync("BrainStatusUpdate", brainStatus);

                // (C) send a tiny trace ping so the browser can prove it received *something* even if deserialization ever failed
                await Clients.All.SendAsync("BroadcastTrace", new
                {
                    kind = "BrainStatusUpdate",
                    brain = brainStatus.brainInstanceName,
                    marketCount = brainStatus.markets?.Count ?? 0,
                    serverUtc = DateTime.UtcNow
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
            _logger.LogInformation("Received SendOverseerMessage: {MessageType} - {Message}", messageType, message);

            try
            {
                // Handle different message types
                switch (messageType.ToLower())
                {
                    case "refresh_data":
                        // Broadcast refresh request to all connected clients
                        await Clients.All.SendAsync("DataRefreshRequested", new
                        {
                            MessageType = messageType,
                            Message = message,
                            Timestamp = DateTime.UtcNow,
                            RequestedBy = Context.ConnectionId
                        });
                        break;

                    default:
                        _logger.LogWarning("Unknown message type received: {MessageType}", messageType);
                        break;
                }

                // Send confirmation back to caller
                await Clients.Caller.SendAsync("OverseerMessageReceived", new
                {
                    Success = true,
                    MessageType = messageType,
                    Timestamp = DateTime.UtcNow,
                    Message = "Message processed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SendOverseerMessage: {MessageType}", messageType);
                await Clients.Caller.SendAsync("OverseerMessageReceived", new
                {
                    Success = false,
                    MessageType = messageType,
                    Message = $"Failed to process message: {ex.Message}"
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
        // Basic brain info
        public string? BrainInstanceName { get; set; }

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
        public double LastRefreshCycleSeconds { get; set; }
        public double LastRefreshCycleInterval { get; set; }
        public double LastRefreshMarketCount { get; set; }
        public double LastRefreshUsagePercentage { get; set; }
        public bool LastRefreshTimeAcceptable { get; set; }
        public DateTime? LastPerformanceSampleDate { get; set; }

        // Connection status
        public bool IsWebSocketConnected { get; set; }

        // Market watch data
        public List<MarketWatchData>? WatchedMarkets { get; set; }
    }
}