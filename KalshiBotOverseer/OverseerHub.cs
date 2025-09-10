// OverseerHub.cs
using Microsoft.AspNetCore.SignalR;
using KalshiBotData.Data.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using BacklashDTOs;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Concurrent;
using KalshiBotOverseer.Models;

namespace KalshiBotOverseer
{
    public class OverseerHub : Hub
    {
        private readonly ILogger<OverseerHub> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private static readonly ConcurrentDictionary<string, BrainPersistence> _brainPersistenceCache = new ConcurrentDictionary<string, BrainPersistence>();

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

        public async Task CheckIn(CheckInData checkInData)
        {
            _logger.LogInformation("Received CheckIn from client: {ConnectionId}", Context.ConnectionId);

            try
            {
                // Update in-memory BrainPersistence
                var brainInstanceName = checkInData.BrainInstanceName;
                var brainPersistence = _brainPersistenceCache.GetOrAdd(brainInstanceName, _ => new BrainPersistence
                {
                    BrainInstanceName = brainInstanceName,
                    LastSeen = DateTime.UtcNow
                });

                // Update all properties
                brainPersistence.CurrentMarketTickers = new HashSet<string>(checkInData.Markets ?? new List<string>());
                brainPersistence.ErrorCount = checkInData.ErrorCount;
                brainPersistence.LastSnapshot = checkInData.LastSnapshot;
                brainPersistence.IsStartingUp = checkInData.IsStartingUp;
                brainPersistence.IsShuttingDown = checkInData.IsShuttingDown;
                brainPersistence.WatchPositions = checkInData.WatchPositions;
                brainPersistence.WatchOrders = checkInData.WatchOrders;
                brainPersistence.ManagedWatchList = checkInData.ManagedWatchList;
                brainPersistence.CaptureSnapshots = checkInData.CaptureSnapshots;
                brainPersistence.TargetWatches = checkInData.TargetWatches;
                brainPersistence.MinimumInterest = checkInData.MinimumInterest;
                brainPersistence.UsageMin = checkInData.UsageMin;
                brainPersistence.UsageMax = checkInData.UsageMax;
                brainPersistence.IsWebSocketConnected = checkInData.IsWebSocketConnected;

                // Update metric histories with deduplication based on LastPerformanceSampleDate or current time
                var timestamp = checkInData.LastPerformanceSampleDate;

                // Helper method to add metric if not duplicate
                void AddMetric(List<MetricHistory> history, double value)
                {
                    if (!history.Any(m => m.Timestamp == timestamp))
                    {
                        history.Add(new MetricHistory { Timestamp = timestamp, Value = value });
                    }
                }

                // Add queue and CPU metrics
                AddMetric(brainPersistence.CpuUsageHistory, checkInData.CurrentCpuUsage);
                AddMetric(brainPersistence.EventQueueHistory, checkInData.EventQueueAvg);
                AddMetric(brainPersistence.TickerQueueHistory, checkInData.TickerQueueAvg);
                AddMetric(brainPersistence.NotificationQueueHistory, checkInData.NotificationQueueAvg);
                AddMetric(brainPersistence.OrderbookQueueHistory, checkInData.OrderbookQueueAvg);

                // Add market count and error count to histories
                AddMetric(brainPersistence.MarketCountHistory, checkInData.Markets?.Count ?? 0);
                AddMetric(brainPersistence.ErrorHistory, checkInData.ErrorCount);

                // Add refresh metrics
                AddMetric(brainPersistence.RefreshCycleSecondsHistory, checkInData.LastRefreshCycleSeconds);
                AddMetric(brainPersistence.RefreshCycleIntervalHistory, checkInData.LastRefreshCycleInterval);
                AddMetric(brainPersistence.RefreshMarketCountHistory, checkInData.LastRefreshMarketCount);
                AddMetric(brainPersistence.RefreshUsagePercentageHistory, checkInData.LastRefreshUsagePercentage);
                AddMetric(brainPersistence.PerformanceSampleDateHistory, checkInData.LastPerformanceSampleDate.Ticks);

                brainPersistence.LastRefreshTimeAcceptable = checkInData.LastRefreshTimeAcceptable;

                // Send response to caller
                await Clients.Caller.SendAsync("CheckInReceived", new
                {
                    Success = true,
                    Timestamp = DateTime.UtcNow,
                    Message = "CheckIn processed successfully"
                });

                // Broadcast CheckInUpdate to all connected clients
                await Clients.All.SendAsync("CheckInUpdate", new
                {
                    BrainInstanceName = brainInstanceName,
                    MarketCount = checkInData.Markets?.Count ?? 0,
                    ErrorCount = checkInData.ErrorCount,
                    LastSnapshot = checkInData.LastSnapshot,
                    LastCheckIn = DateTime.UtcNow,
                    IsStartingUp = checkInData.IsStartingUp,
                    IsShuttingDown = checkInData.IsShuttingDown,
                    WatchPositions = checkInData.WatchPositions,
                    WatchOrders = checkInData.WatchOrders,
                    ManagedWatchList = checkInData.ManagedWatchList,
                    CaptureSnapshots = checkInData.CaptureSnapshots,
                    TargetWatches = checkInData.TargetWatches,
                    MinimumInterest = checkInData.MinimumInterest,
                    UsageMin = checkInData.UsageMin,
                    UsageMax = checkInData.UsageMax,
                    CurrentCpuUsage = checkInData.CurrentCpuUsage,
                    EventQueueAvg = checkInData.EventQueueAvg,
                    TickerQueueAvg = checkInData.TickerQueueAvg,
                    NotificationQueueAvg = checkInData.NotificationQueueAvg,
                    OrderbookQueueAvg = checkInData.OrderbookQueueAvg,
                    IsWebSocketConnected = checkInData.IsWebSocketConnected,
                    LastRefreshCycleSeconds = checkInData.LastRefreshCycleSeconds,
                    LastRefreshCycleInterval = checkInData.LastRefreshCycleInterval,
                    LastRefreshMarketCount = checkInData.LastRefreshMarketCount,
                    LastRefreshUsagePercentage = checkInData.LastRefreshUsagePercentage,
                    LastRefreshTimeAcceptable = checkInData.LastRefreshTimeAcceptable,
                    LastPerformanceSampleDate = checkInData.LastPerformanceSampleDate
                });

                _logger.LogInformation("Processed CheckIn for bot {BrainInstanceName} with {MarketCount} markets", brainInstanceName, checkInData.Markets?.Count ?? 0);
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