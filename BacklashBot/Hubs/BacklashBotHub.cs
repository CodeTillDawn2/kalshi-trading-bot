// ChartHub.cs
using BacklashBot.Configuration;
using BacklashBot.Services.Interfaces;
using BacklashInterfaces.PerformanceMetrics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using OverseerBotShared;
using System.Collections.Concurrent;
using System.Diagnostics;

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
        private static readonly ConcurrentDictionary<string, long> _eventCounts = new();
        private static readonly ConcurrentDictionary<string, List<long>> _durations = new();
        private static readonly ConcurrentDictionary<string, double> _gauges = new();
        private static Timer? _aggregationTimer;
        private static IPerformanceMonitor? _staticPerformanceMonitor;
        private static bool _metricsEnabled;
        private static int _aggregationIntervalMinutes;
        private readonly ILogger<BacklashBotHub> _logger;
        private readonly IServiceFactory _serviceFactory;
        private readonly IPerformanceMonitor _performanceMonitor;
        private readonly IOptions<BacklashBotHubConfig> _config;

        /// <summary>
        /// Initializes a new instance of the BacklashBotHub class.
        /// </summary>
        /// <param name="serviceFactory">Factory for creating service instances.</param>
        /// <param name="logger">Logger for recording hub operations.</param>
        /// <param name="performanceMonitor">Monitor for recording performance metrics.</param>
        /// <param name="config">Configuration for BacklashBotHub settings.</param>
        public BacklashBotHub(
            IServiceFactory serviceFactory,
            ILogger<BacklashBotHub> logger,
            IPerformanceMonitor performanceMonitor,
            IOptions<BacklashBotHubConfig> config)
        {
            _logger = logger;
            _serviceFactory = serviceFactory;
            _performanceMonitor = performanceMonitor;
            _config = config;

            if (_staticPerformanceMonitor == null)
            {
                _staticPerformanceMonitor = performanceMonitor;
                _metricsEnabled = config.Value.EnablePerformanceMetrics;
                _aggregationIntervalMinutes = config.Value.AggregationIntervalMinutes;
                _aggregationTimer ??= new Timer(AggregateAndPostMetrics, null, TimeSpan.Zero, TimeSpan.FromMinutes(_aggregationIntervalMinutes));
            }
        }

        private static void AggregateAndPostMetrics(object? state)
        {
            if (_staticPerformanceMonitor == null) return;

            if (_metricsEnabled)
            {
                foreach (var kvp in _eventCounts)
                {
                    _staticPerformanceMonitor.RecordCounterMetric("BacklashBotHub", kvp.Key + "_per_minute", "Aggregated " + kvp.Key, "Total count per minute", kvp.Value, "count", "Hub");
                }
                foreach (var kvp in _durations)
                {
                    var list = kvp.Value;
                    if (list.Any())
                    {
                        double avg = list.Average();
                        _staticPerformanceMonitor.RecordSpeedDialMetric("BacklashBotHub", kvp.Key + "_avg_per_minute", "Average " + kvp.Key, "Average duration per minute", avg, "ms", "Hub");
                    }
                }
                foreach (var kvp in _gauges)
                {
                    _staticPerformanceMonitor.RecordNumericDisplayMetric("BacklashBotHub", kvp.Key, kvp.Key, "Current " + kvp.Key, kvp.Value, "count", "Hub");
                }
            }
            else
            {
                foreach (var kvp in _eventCounts)
                {
                    _staticPerformanceMonitor.RecordDisabledMetric("BacklashBotHub", kvp.Key + "_per_minute", "Aggregated " + kvp.Key, "Total count per minute", 0, "", "Hub");
                }
                foreach (var kvp in _durations)
                {
                    _staticPerformanceMonitor.RecordDisabledMetric("BacklashBotHub", kvp.Key + "_avg_per_minute", "Average " + kvp.Key, "Average duration per minute", 0, "", "Hub");
                }
                foreach (var kvp in _gauges)
                {
                    _staticPerformanceMonitor.RecordDisabledMetric("BacklashBotHub", kvp.Key, kvp.Key, "Current " + kvp.Key, 0, "", "Hub");
                }
            }

            _eventCounts.Clear();
            _durations.Clear();
            // gauges persist as current values
        }

        /// <summary>
        /// Called when a client connects to the hub.
        /// Adds the client to the connected clients collection and logs the connection.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task OnConnectedAsync()
        {
            bool enabled = _config.Value.EnablePerformanceMetrics;
            Stopwatch? stopwatch = enabled ? Stopwatch.StartNew() : null;
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
                    if (enabled)
                    {
                        stopwatch!.Stop();
                        _durations.GetOrAdd("onconnected_duration", _ => new List<long>()).Add(stopwatch.ElapsedMilliseconds);
                        _eventCounts.AddOrUpdate("client_connected", 1, (_, v) => v + 1);
                        _gauges["connected_clients"] = _connectedClients.Count;
                    }
                }
                catch (Exception ex)
                {
                    if (enabled)
                    {
                        stopwatch!.Stop();
                        _durations.GetOrAdd("onconnected_duration", _ => new List<long>()).Add(stopwatch.ElapsedMilliseconds);
                        _eventCounts.AddOrUpdate("client_connected_error", 1, (_, v) => v + 1);
                    }
                    _logger.LogError(ex, "Error during OnConnectedAsync for client: {ConnectionId}", Context.ConnectionId);
                }
            }
        }

        /// <summary>
        /// Called when a client disconnects from the hub.
        /// Removes the client from the connected clients collection and logs the disconnection.
        /// </summary>
        /// <param name="exception">Exception that caused the disconnection, if any.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            bool enabled = _config.Value.EnablePerformanceMetrics;
            Stopwatch? stopwatch = enabled ? Stopwatch.StartNew() : null;
            lock (_connectedClients)
            {
                _connectedClients.Remove(Context.ConnectionId);
            }
            try
            {
                _logger.LogInformation("Client disconnected: {ConnectionId}. Total clients: {ClientCount}", Context.ConnectionId, _connectedClients.Count);
                await base.OnDisconnectedAsync(exception);
                if (enabled)
                {
                    stopwatch!.Stop();
                    _durations.GetOrAdd("ondisconnected_duration", _ => new List<long>()).Add(stopwatch.ElapsedMilliseconds);
                    _eventCounts.AddOrUpdate("client_disconnected", 1, (_, v) => v + 1);
                    _gauges["connected_clients"] = _connectedClients.Count;
                }
            }
            catch (Exception ex)
            {
                if (enabled)
                {
                    stopwatch!.Stop();
                    _durations.GetOrAdd("ondisconnected_duration", _ => new List<long>()).Add(stopwatch.ElapsedMilliseconds);
                    _eventCounts.AddOrUpdate("client_disconnected_error", 1, (_, v) => v + 1);
                }
                _logger.LogError(ex, "Error during OnDisconnectedAsync for client: {ConnectionId}", Context.ConnectionId);
            }
        }

        /// <summary>
        /// Checks if there are any connected clients.
        /// </summary>
        /// <returns>True if there are connected clients, otherwise false.</returns>
        public static bool HasConnectedClients()
        {
            lock (_connectedClients)
            {
                return _connectedClients.Any();
            }
        }

        /// <summary>
        /// Clears all connected clients from the collection.
        /// Used for resetting the client state.
        /// </summary>
        public static void ClearConnectedClients()
        {
            lock (_connectedClients)
            {
                _connectedClients.Clear();
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
            bool enabled = _config.Value.EnablePerformanceMetrics;
            Stopwatch? stopwatch = enabled ? Stopwatch.StartNew() : null;
            _logger.LogInformation("Sending CheckIn to Overseer");

            try
            {
                // Send CheckIn data to overseer
                await Clients.All.SendAsync("CheckIn", checkInData);
                if (enabled)
                {
                    stopwatch!.Stop();
                    _durations.GetOrAdd("checkin_duration", _ => new List<long>()).Add(stopwatch.ElapsedMilliseconds);
                    _eventCounts.AddOrUpdate("checkin_success", 1, (_, v) => v + 1);
                }
                _logger.LogInformation("CheckIn sent to Overseer successfully");
            }
            catch (Exception ex)
            {
                if (enabled)
                {
                    stopwatch!.Stop();
                    _durations.GetOrAdd("checkin_duration", _ => new List<long>()).Add(stopwatch.ElapsedMilliseconds);
                    _eventCounts.AddOrUpdate("checkin_failure", 1, (_, v) => v + 1);
                }
                _logger.LogError(ex, "Error sending CheckIn to Overseer");
            }
        }



        /// <summary>
        /// Confirms to the Overseer that target tickers have been received and processed by a brain instance.
        /// Used to acknowledge successful receipt of ticker assignments from the Overseer system.
        /// </summary>
        /// <param name="brainInstanceName">Name of the brain instance that received the target tickers.</param>
        public async Task ConfirmTargetTickersReceived(string brainInstanceName)
        {
            bool enabled = _config.Value.EnablePerformanceMetrics;
            Stopwatch? stopwatch = enabled ? Stopwatch.StartNew() : null;
            _logger.LogInformation("Confirming target tickers received for brain: {BrainInstanceName}", brainInstanceName);

            try
            {
                // Send confirmation to overseer
                await Clients.All.SendAsync("ConfirmTargetTickersReceived", brainInstanceName);
                if (enabled)
                {
                    stopwatch!.Stop();
                    _durations.GetOrAdd("confirm_tickers_duration", _ => new List<long>()).Add(stopwatch.ElapsedMilliseconds);
                    _eventCounts.AddOrUpdate("confirm_tickers_success", 1, (_, v) => v + 1);
                }
                _logger.LogInformation("Target tickers confirmation sent to Overseer for brain: {BrainInstanceName}", brainInstanceName);
            }
            catch (Exception ex)
            {
                if (enabled)
                {
                    stopwatch!.Stop();
                    _durations.GetOrAdd("confirm_tickers_duration", _ => new List<long>()).Add(stopwatch.ElapsedMilliseconds);
                    _eventCounts.AddOrUpdate("confirm_tickers_failure", 1, (_, v) => v + 1);
                }
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
            bool enabled = _config.Value.EnablePerformanceMetrics;
            Stopwatch? stopwatch = enabled ? Stopwatch.StartNew() : null;
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
                    if (enabled)
                    {
                        stopwatch!.Stop();
                        _durations.GetOrAdd("handle_checkin_response_duration", _ => new List<long>()).Add(stopwatch.ElapsedMilliseconds);
                        _eventCounts.AddOrUpdate("checkin_response_success", 1, (_, v) => v + 1);
                    }
                }
                else
                {
                    if (enabled)
                    {
                        stopwatch!.Stop();
                        _durations.GetOrAdd("handle_checkin_response_duration", _ => new List<long>()).Add(stopwatch.ElapsedMilliseconds);
                        _eventCounts.AddOrUpdate("checkin_response_failure", 1, (_, v) => v + 1);
                    }
                    _logger.LogWarning("CheckIn failed: {Message}", response.Message);
                }
            }
            catch (Exception ex)
            {
                if (enabled)
                {
                    stopwatch!.Stop();
                    _durations.GetOrAdd("handle_checkin_response_duration", _ => new List<long>()).Add(stopwatch.ElapsedMilliseconds);
                    _eventCounts.AddOrUpdate("checkin_response_error", 1, (_, v) => v + 1);
                }
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
            bool enabled = _config.Value.EnablePerformanceMetrics;
            Stopwatch? stopwatch = enabled ? Stopwatch.StartNew() : null;
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
                if (enabled)
                {
                    stopwatch!.Stop();
                    _durations.GetOrAdd("send_overseer_message_duration", _ => new List<long>()).Add(stopwatch.ElapsedMilliseconds);
                    _eventCounts.AddOrUpdate("send_overseer_message_success", 1, (_, v) => v + 1);
                }
            }
            catch (Exception ex)
            {
                if (enabled)
                {
                    stopwatch!.Stop();
                    _durations.GetOrAdd("send_overseer_message_duration", _ => new List<long>()).Add(stopwatch.ElapsedMilliseconds);
                    _eventCounts.AddOrUpdate("send_overseer_message_failure", 1, (_, v) => v + 1);
                }
                _logger.LogError(ex, "Error processing SendOverseerMessage: {MessageType}", messageType);
                await Clients.Caller.SendAsync("OverseerMessageReceived", new
                {
                    Success = false,
                    MessageType = messageType,
                    Message = $"Failed to process message: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Handles the response from the Overseer confirming receipt of target tickers confirmation.
        /// Processes success/failure status and logs appropriate messages.
        /// </summary>
        /// <param name="response">Response data from the Overseer containing success status and brain instance information.</param>
        /// <returns>A completed task.</returns>
        public Task HandleTargetTickersConfirmationResponse(TargetTickersConfirmationResponse response)
        {
            bool enabled = _config.Value.EnablePerformanceMetrics;
            Stopwatch? stopwatch = enabled ? Stopwatch.StartNew() : null;
            _logger.LogInformation("Received TargetTickersConfirmationResponse from Overseer");

            try
            {
                if (response.Success)
                {
                    if (enabled)
                    {
                        stopwatch!.Stop();
                        _durations.GetOrAdd("handle_tickers_confirmation_duration", _ => new List<long>()).Add(stopwatch.ElapsedMilliseconds);
                        _eventCounts.AddOrUpdate("tickers_confirmation_success", 1, (_, v) => v + 1);
                    }
                    _logger.LogInformation("Target tickers confirmation acknowledged by Overseer for brain: {BrainInstanceName}", response.BrainInstanceName);
                }
                else
                {
                    if (enabled)
                    {
                        stopwatch!.Stop();
                        _durations.GetOrAdd("handle_tickers_confirmation_duration", _ => new List<long>()).Add(stopwatch.ElapsedMilliseconds);
                        _eventCounts.AddOrUpdate("tickers_confirmation_failure", 1, (_, v) => v + 1);
                    }
                    _logger.LogWarning("Target tickers confirmation failed: {Message}", response.Message);
                }
            }
            catch (Exception ex)
            {
                if (enabled)
                {
                    stopwatch!.Stop();
                    _durations.GetOrAdd("handle_tickers_confirmation_duration", _ => new List<long>()).Add(stopwatch.ElapsedMilliseconds);
                    _eventCounts.AddOrUpdate("tickers_confirmation_error", 1, (_, v) => v + 1);
                }
                _logger.LogError(ex, "Error handling TargetTickersConfirmationResponse");
            }

            return Task.CompletedTask;
        }
    }

}
