// ChartHub.cs
using Microsoft.AspNetCore.SignalR;
using BacklashBot.Services.Interfaces;
using KalshiBotData.Models;
using KalshiBotData.Data.Interfaces;
using BacklashDTOs;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;

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

        public async Task CheckIn(CheckInData checkInData)
        {
            _logger.LogInformation("Sending CheckIn to Overseer");

            try
            {
                // Send CheckIn data to overseer
                await Clients.All.SendAsync("CheckIn", checkInData);
                _logger.LogDebug("CheckIn sent to Overseer successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending CheckIn to Overseer");
            }
        }

        public async Task ConfirmTargetTickersReceived(string brainInstanceName)
        {
            _logger.LogInformation("Confirming target tickers received for brain: {BrainInstanceName}", brainInstanceName);

            try
            {
                // Send confirmation to overseer
                await Clients.All.SendAsync("ConfirmTargetTickersReceived", brainInstanceName);
                _logger.LogDebug("Target tickers confirmation sent to Overseer for brain: {BrainInstanceName}", brainInstanceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending target tickers confirmation to Overseer for brain: {BrainInstanceName}", brainInstanceName);
            }
        }

        public async Task HandleCheckInResponse(CheckInResponse response)
        {
            _logger.LogInformation("Received CheckInResponse from Overseer");

            try
            {
                if (response.Success)
                {
                    _logger.LogDebug("CheckIn acknowledged by Overseer");

                    // Handle target tickers if provided
                    if (response.TargetTickers != null && response.TargetTickers.Length > 0)
                    {
                        var targetTickers = response.TargetTickers.ToList();
                        _logger.LogDebug("Received {Count} target tickers from Overseer", targetTickers.Count);

                        // TODO: Process target tickers - update local market watching list
                        // For now, just confirm receipt
                        await ConfirmTargetTickersReceived("BacklashBot"); // Use appropriate brain instance name
                    }
                }
                else
                {
                    _logger.LogWarning("CheckIn failed: {Message}", response.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling CheckInResponse");
            }
        }

        public Task HandleTargetTickersConfirmationResponse(TargetTickersConfirmationResponse response)
        {
            _logger.LogInformation("Received TargetTickersConfirmationResponse from Overseer");

            try
            {
                if (response.Success)
                {
                    _logger.LogDebug("Target tickers confirmation acknowledged by Overseer for brain: {BrainInstanceName}", response.BrainInstanceName);
                }
                else
                {
                    _logger.LogWarning("Target tickers confirmation failed: {Message}", response.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling TargetTickersConfirmationResponse");
            }

            return Task.CompletedTask;
        }
    }

    public class CheckInData
    {
        // Basic brain info
        public string BrainInstanceName { get; set; } = "";

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
        public TimeSpan? LastRefreshCycleInterval { get; set; }
        public int LastRefreshMarketCount { get; set; }
        public double LastRefreshUsagePercentage { get; set; }
        public bool LastRefreshTimeAcceptable { get; set; }
        public DateTime? LastPerformanceSampleDate { get; set; }

        // Connection status
        public bool IsWebSocketConnected { get; set; }
    }

    public class CheckInResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string[] TargetTickers { get; set; } = Array.Empty<string>();
        public DateTime Timestamp { get; set; }
    }

    public class TargetTickersConfirmationResponse
    {
        public bool Success { get; set; }
        public string BrainInstanceName { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }
}