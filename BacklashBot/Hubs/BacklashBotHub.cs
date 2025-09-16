// ChartHub.cs
using Microsoft.AspNetCore.SignalR;
using BacklashBot.Services.Interfaces;
using BacklashBotData.Data.Interfaces;
using BacklashDTOs;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace BacklashBot.Hubs
{
    /// <summary>
    /// SignalR hub for managing real-time communication between the BacklashBot trading system and connected clients.
    /// Handles client connections, handshakes, check-ins, and message routing to the Overseer system.
    /// Provides methods for broadcasting trading data, confirming target tickers, and processing overseer responses.
    /// </summary>
    public class BacklashBotHub : Hub
    {
        private static readonly HashSet<string> _connectedClients = new HashSet<string>();
        private readonly ILogger<BacklashBotHub> _logger;
        private readonly IServiceFactory _serviceFactory;

        public BacklashBotHub(
            IServiceFactory serviceFactory,
            ILogger<BacklashBotHub> logger)
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
                    _logger.LogInformation("Client connected: {ConnectionId}. Total clients: {ClientCount}", Context.ConnectionId, _connectedClients.Count);
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
            _logger.LogInformation("Client disconnected: {ConnectionId}. Total clients: {ClientCount}", Context.ConnectionId, _connectedClients.Count);
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

        /// <summary>
        /// Performs handshake with a connecting client, registers them in the database, and generates an authentication token.
        /// Updates existing client information or creates new client records as needed.
        /// </summary>
        /// <param name="clientId">Unique identifier for the client</param>
        /// <param name="clientName">Name of the client (typically brain instance name)</param>
        /// <param name="clientType">Type of client (e.g., "brain", "dashboard")</param>
        public async Task Handshake(string clientId, string clientName, string clientType)
        {
            _logger.LogInformation("Handshake request from client: {ClientId}, Name: {ClientName}, Type: {ClientType}",
                clientId, clientName, clientType);

            try
            {
                using var scope = _serviceFactory.GetScopeManager().CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();

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
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var input = $"{clientId}:{clientName}:{DateTime.UtcNow.Date:yyyy-MM-dd}";
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Broadcasts check-in data from a brain instance to all connected clients, typically the Overseer.
        /// Used for periodic status updates and health monitoring of trading brain instances.
        /// </summary>
        /// <param name="checkInData">Comprehensive data about the brain's current state including markets and configuration</param>
        public async Task CheckIn(CheckInData checkInData)
        {
            _logger.LogInformation("Sending CheckIn to Overseer");

            try
            {
                // Send CheckIn data to overseer
                await Clients.All.SendAsync("CheckIn", checkInData);
                _logger.LogInformation("CheckIn sent to Overseer successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending CheckIn to Overseer");
            }
        }

        /// <summary>
        /// Sends comprehensive performance metrics from a brain instance to the Overseer for monitoring and display.
        /// This is separate from the check-in process to allow independent timing and frequency of performance data transmission.
        /// </summary>
        /// <param name="brainInstanceName">The name of the brain instance sending the metrics.</param>
        /// <param name="performanceMetrics">The comprehensive performance metrics data to send.</param>
        public async Task SendPerformanceMetrics(string brainInstanceName, object performanceMetrics)
        {
            _logger.LogInformation("Sending performance metrics for brain {BrainInstanceName}", brainInstanceName);

            try
            {
                // Send performance metrics to overseer
                await Clients.All.SendAsync("PerformanceMetricsUpdate", new
                {
                    BrainInstanceName = brainInstanceName,
                    PerformanceMetrics = performanceMetrics,
                    Timestamp = DateTime.UtcNow
                });
                _logger.LogInformation("Performance metrics sent to Overseer successfully for brain {BrainInstanceName}", brainInstanceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending performance metrics to Overseer for brain {BrainInstanceName}", brainInstanceName);
            }
        }

        /// <summary>
        /// Sends detailed performance metrics from the CentralPerformanceMonitor to all connected clients, typically the Overseer.
        /// Used for comprehensive performance monitoring and analytics beyond the basic check-in data.
        /// </summary>
        /// <param name="performanceMetrics">Detailed performance metrics including database operations, WebSocket metrics, and system performance data</param>
        public async Task SendPerformanceMetrics(PerformanceMetricsData performanceMetrics)
        {
            _logger.LogInformation("Sending PerformanceMetrics to Overseer");

            try
            {
                // Send performance metrics data to overseer
                await Clients.All.SendAsync("PerformanceMetrics", performanceMetrics);
                _logger.LogInformation("PerformanceMetrics sent to Overseer successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending PerformanceMetrics to Overseer");
            }
        }

        public async Task ConfirmTargetTickersReceived(string brainInstanceName)
        {
            _logger.LogInformation("Confirming target tickers received for brain: {BrainInstanceName}", brainInstanceName);

            try
            {
                // Send confirmation to overseer
                await Clients.All.SendAsync("ConfirmTargetTickersReceived", brainInstanceName);
                _logger.LogInformation("Target tickers confirmation sent to Overseer for brain: {BrainInstanceName}", brainInstanceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending target tickers confirmation to Overseer for brain: {BrainInstanceName}", brainInstanceName);
            }
        }

        /// <summary>
        /// Processes responses from the Overseer regarding check-in acknowledgments.
        /// Handles target ticker assignments and confirmation of successful check-in processing.
        /// </summary>
        /// <param name="response">Response data from the Overseer containing success status and target tickers</param>
        public async Task HandleCheckInResponse(CheckInResponse response)
        {
            _logger.LogInformation("Received CheckInResponse from Overseer");

            try
            {
                if (response.Success)
                {
                    _logger.LogInformation("CheckIn acknowledged by Overseer");

                    // Handle target tickers if provided
                    if (response.TargetTickers != null && response.TargetTickers.Length > 0)
                    {
                        var targetTickers = response.TargetTickers.ToList();
                        _logger.LogInformation("Received {Count} target tickers from Overseer", targetTickers.Count);

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

        /// <summary>
        /// Sends typed messages to the Overseer system for processing.
        /// Currently supports refresh_data messages to trigger data refresh operations across connected clients.
        /// </summary>
        /// <param name="messageType">Type of message being sent (e.g., "refresh_data")</param>
        /// <param name="message">Message content or payload</param>
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

        public Task HandleTargetTickersConfirmationResponse(TargetTickersConfirmationResponse response)
        {
            _logger.LogInformation("Received TargetTickersConfirmationResponse from Overseer");

            try
            {
                if (response.Success)
                {
                    _logger.LogInformation("Target tickers confirmation acknowledged by Overseer for brain: {BrainInstanceName}", response.BrainInstanceName);
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

    /// <summary>
    /// Data structure containing comprehensive information about a brain instance's current state.
    /// Used for periodic check-ins to report status, performance metrics, and configuration to the Overseer.
    /// </summary>
    public class CheckInData
    {
        // Basic brain info
        public string BrainInstanceName { get; set; }

        // Basic market data
        public List<string> Markets { get; set; }
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
    }

    /// <summary>
    /// Response structure for check-in acknowledgments from the Overseer.
    /// Contains success status, optional message, and target tickers for the brain to monitor.
    /// </summary>
    public class CheckInResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string[] TargetTickers { get; set; } = Array.Empty<string>();
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Response structure for target tickers confirmation acknowledgments.
    /// Used to confirm that the brain has successfully received and processed target market assignments.
    /// </summary>
    public class TargetTickersConfirmationResponse
    {
        public bool Success { get; set; }
        public string BrainInstanceName { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Data structure containing comprehensive performance metrics from the CentralPerformanceMonitor.
    /// Used for detailed performance monitoring and analytics, including database operations,
    /// WebSocket metrics, queue depths, and system resource utilization.
    /// </summary>
    public class PerformanceMetricsData
    {
        /// <summary>
        /// Gets or sets the name of the brain instance providing the performance metrics.
        /// </summary>
        public string? BrainInstanceName { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when these performance metrics were collected.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the database performance metrics.
        /// </summary>
        public IReadOnlyDictionary<string, (int SuccessCount, int FailureCount, TimeSpan TotalTime, double AverageTimeMs)>? DatabaseMetrics { get; set; }

        /// <summary>
        /// Gets or sets the OverseerClientService performance metrics.
        /// </summary>
        public IReadOnlyDictionary<string, object>? OverseerClientServiceMetrics { get; set; }

        /// <summary>
        /// Gets or sets the WebSocket processing time metrics in ticks.
        /// </summary>
        public ConcurrentDictionary<string, long>? WebSocketProcessingTimeTicks { get; set; }

        /// <summary>
        /// Gets or sets the WebSocket processing count metrics.
        /// </summary>
        public ConcurrentDictionary<string, int>? WebSocketProcessingCount { get; set; }

        /// <summary>
        /// Gets or sets the WebSocket buffer usage metrics in bytes.
        /// </summary>
        public ConcurrentDictionary<string, long>? WebSocketBufferUsageBytes { get; set; }

        /// <summary>
        /// Gets or sets the WebSocket operation times.
        /// </summary>
        public ConcurrentDictionary<string, TimeSpan>? WebSocketOperationTimes { get; set; }

        /// <summary>
        /// Gets or sets the WebSocket semaphore wait counts.
        /// </summary>
        public ConcurrentDictionary<string, int>? WebSocketSemaphoreWaitCount { get; set; }

        /// <summary>
        /// Gets or sets the SubscriptionManager operation metrics.
        /// </summary>
        public IReadOnlyDictionary<string, (long AverageTicks, long TotalOperations, long SuccessfulOperations)>? SubscriptionManagerOperationMetrics { get; set; }

        /// <summary>
        /// Gets or sets the SubscriptionManager lock contention metrics.
        /// </summary>
        public IReadOnlyDictionary<string, (long AcquisitionCount, long AverageWaitTicks, long ContentionCount)>? SubscriptionManagerLockMetrics { get; set; }

        /// <summary>
        /// Gets or sets the MessageProcessor total messages processed.
        /// </summary>
        public long MessageProcessorTotalMessagesProcessed { get; set; }

        /// <summary>
        /// Gets or sets the MessageProcessor total processing time in milliseconds.
        /// </summary>
        public long MessageProcessorTotalProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the MessageProcessor average processing time in milliseconds.
        /// </summary>
        public double MessageProcessorAverageProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the MessageProcessor messages per second rate.
        /// </summary>
        public double MessageProcessorMessagesPerSecond { get; set; }

        /// <summary>
        /// Gets or sets the MessageProcessor order book queue depth.
        /// </summary>
        public int MessageProcessorOrderBookQueueDepth { get; set; }

        /// <summary>
        /// Gets or sets the MessageProcessor duplicate message count.
        /// </summary>
        public int MessageProcessorDuplicateMessageCount { get; set; }

        /// <summary>
        /// Gets or sets the MessageProcessor duplicates in window.
        /// </summary>
        public int MessageProcessorDuplicatesInWindow { get; set; }

        /// <summary>
        /// Gets or sets the MessageProcessor last duplicate warning time.
        /// </summary>
        public DateTime MessageProcessorLastDuplicateWarningTime { get; set; }

        /// <summary>
        /// Gets or sets the MessageProcessor message type counts.
        /// </summary>
        public IReadOnlyDictionary<string, long>? MessageProcessorMessageTypeCounts { get; set; }

        /// <summary>
        /// Gets or sets the API execution times.
        /// </summary>
        public ConcurrentDictionary<string, List<(DateTime Timestamp, long Milliseconds)>>? ApiExecutionTimes { get; set; }

        /// <summary>
        /// Gets or sets the configurable metrics for GUI consumption.
        /// </summary>
        public IReadOnlyDictionary<string, object>? ConfigurableMetrics { get; set; }
    }
}