// ChartHub.cs
using BacklashBot.Services.Interfaces;
using BacklashBotData.Data.Interfaces;
using Microsoft.AspNetCore.SignalR;
using OverseerBotShared;

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

        /// <summary>
        /// Initializes a new instance of the BacklashBotHub class.
        /// </summary>
        /// <param name="serviceFactory">Factory for creating service instances.</param>
        /// <param name="logger">Logger for recording hub operations.</param>
        public BacklashBotHub(
            IServiceFactory serviceFactory,
            ILogger<BacklashBotHub> logger)
        {
            _logger = logger;
            _serviceFactory = serviceFactory;
        }

        /// <summary>
        /// Called when a client connects to the hub.
        /// Adds the client to the connected clients collection and logs the connection.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Called when a client disconnects from the hub.
        /// Removes the client from the connected clients collection and logs the disconnection.
        /// </summary>
        /// <param name="exception">Exception that caused the disconnection, if any.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            lock (_connectedClients)
            {
                _connectedClients.Remove(Context.ConnectionId);
            }
            _logger.LogInformation("Client disconnected: {ConnectionId}. Total clients: {ClientCount}", Context.ConnectionId, _connectedClients.Count);
            await base.OnDisconnectedAsync(exception);
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
        /// Confirms to the Overseer that target tickers have been received and processed by a brain instance.
        /// Used to acknowledge successful receipt of ticker assignments from the Overseer system.
        /// </summary>
        /// <param name="brainInstanceName">Name of the brain instance that received the target tickers.</param>
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

        /// <summary>
        /// Handles the response from the Overseer confirming receipt of target tickers confirmation.
        /// Processes success/failure status and logs appropriate messages.
        /// </summary>
        /// <param name="response">Response data from the Overseer containing success status and brain instance information.</param>
        /// <returns>A completed task.</returns>
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

}
