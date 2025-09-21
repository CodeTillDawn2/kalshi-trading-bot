using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace OverseerBotShared
{
    /// <summary>
    /// Strongly typed SignalR client implementation that provides compile-time validation
    /// and enhanced error handling for communication between BacklashBot and Overseer.
    /// </summary>
    public class TypedSignalRClient : ISignalRClient<IBacklashBotHub>, IDisposable
    {
        private readonly HubConnection _connection;
        private readonly ILogger<TypedSignalRClient> _logger;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the TypedSignalRClient.
        /// </summary>
        /// <param name="connection">The SignalR hub connection.</param>
        /// <param name="logger">The logger instance.</param>
        public TypedSignalRClient(HubConnection connection, ILogger<TypedSignalRClient> logger)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Set up connection event handlers
            _connection.Closed += HandleConnectionClosed;
            _connection.Reconnected += HandleReconnected;
        }

        /// <summary>
        /// Gets the typed hub proxy for strongly typed method calls.
        /// </summary>
        public IBacklashBotHub HubProxy => (IBacklashBotHub)this;

        /// <summary>
        /// Gets a value indicating whether the connection is currently active.
        /// </summary>
        public bool IsConnected => _connection.State == HubConnectionState.Connected;

        /// <summary>
        /// Starts the SignalR connection.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ConnectAsync(string url)
        {
            try
            {
                if (_connection.State == HubConnectionState.Disconnected)
                {
                    await _connection.StartAsync();
                    _logger.LogInformation("SignalR connection established to {Url}", url);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to SignalR hub at {Url}", url);
                throw;
            }
        }

        /// <summary>
        /// Stops the SignalR connection.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DisconnectAsync()
        {
            try
            {
                if (_connection.State != HubConnectionState.Disconnected)
                {
                    await _connection.StopAsync();
                    _logger.LogInformation("SignalR connection stopped");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping SignalR connection");
                throw;
            }
        }

        /// <summary>
        /// Invokes a hub method without expecting a response.
        /// </summary>
        /// <param name="methodName">The name of the method to invoke.</param>
        /// <param name="args">The arguments to pass to the method.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(string methodName, params object[] args)
        {
            try
            {
                await _connection.InvokeCoreAsync(methodName, args);
                _logger.LogDebug("Invoked SignalR method: {MethodName}", methodName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking SignalR method: {MethodName}", methodName);
                throw;
            }
        }

        /// <summary>
        /// Invokes a hub method and expects a typed response.
        /// </summary>
        /// <typeparam name="TResponse">The type of the expected response.</typeparam>
        /// <param name="methodName">The name of the method to invoke.</param>
        /// <param name="args">The arguments to pass to the method.</param>
        /// <returns>A task representing the asynchronous operation with the typed response.</returns>
        public async Task<TResponse> InvokeAsync<TResponse>(string methodName, params object[] args)
        {
            try
            {
                var result = await _connection.InvokeCoreAsync<TResponse>(methodName, args);
                _logger.LogDebug("Invoked SignalR method: {MethodName} with typed response", methodName);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking SignalR method: {MethodName}", methodName);
                throw;
            }
        }

        /// <summary>
        /// Registers a handler for a specific method.
        /// </summary>
        /// <typeparam name="TData">The type of data expected in the callback.</typeparam>
        /// <param name="methodName">The name of the method to handle.</param>
        /// <param name="handler">The handler function.</param>
        public void On<TData>(string methodName, Func<TData, Task> handler)
        {
            _connection.On(methodName, handler);
            _logger.LogDebug("Registered handler for SignalR method: {MethodName}", methodName);
        }

        /// <summary>
        /// Registers a handler for a method that doesn't expect parameters.
        /// </summary>
        /// <param name="methodName">The name of the method to handle.</param>
        /// <param name="handler">The handler function.</param>
        public void On(string methodName, Func<Task> handler)
        {
            _connection.On(methodName, handler);
            _logger.LogDebug("Registered parameterless handler for SignalR method: {MethodName}", methodName);
        }

        #region IBacklashBotHub Implementation

        // Client-to-Server methods
        public async Task<HandshakeResponse> Handshake(string clientId, string clientName, string clientType, string? authToken = null)
        {
            var args = authToken != null ? new object[] { clientId, clientName, clientType, authToken } : new object[] { clientId, clientName, clientType };
            return await InvokeAsync<HandshakeResponse>("Handshake", args);
        }

        public async Task<CheckInResponse> CheckIn(CheckInData checkInData)
        {
            return await InvokeAsync<CheckInResponse>("CheckIn", checkInData);
        }



        public async Task ConfirmTargetTickersReceived(string brainInstanceName)
        {
            await InvokeAsync("ConfirmTargetTickersReceived", brainInstanceName);
        }

        public async Task<PerformanceMetricsResponse> SendPerformanceMetrics(PerformanceMetricsData performanceMetrics)
        {
            return await InvokeAsync<PerformanceMetricsResponse>("SendPerformanceMetrics", performanceMetrics);
        }

        public async Task<MessageResponse> SendOverseerMessage(string messageType, string message)
        {
            return await InvokeAsync<MessageResponse>("SendOverseerMessage", messageType, message);
        }

        // Server-to-Client methods (handlers)
        public void HandleHandshakeResponse(Func<HandshakeResponse, Task> handler)
        {
            On("HandshakeResponse", handler);
        }

        public void HandleCheckInResponse(Func<CheckInResponse, Task> handler)
        {
            On("CheckInResponse", handler);
        }

        public void HandleTargetTickersConfirmationResponse(Func<TargetTickersConfirmationResponse, Task> handler)
        {
            On("TargetTickersConfirmationResponse", handler);
        }

        public void HandlePerformanceMetricsResponse(Func<PerformanceMetricsResponse, Task> handler)
        {
            On("PerformanceMetricsResponse", handler);
        }

        public void HandleMessageResponse(Func<MessageResponse, Task> handler)
        {
            On("OverseerMessageReceived", handler);
        }

        public void HandleDataRefreshRequested(Func<object, Task> handler)
        {
            On("DataRefreshRequested", handler);
        }

        public void HandleBroadcastTrace(Func<object, Task> handler)
        {
            On("BroadcastTrace", handler);
        }

        public void HandleBrainStatusUpdate(Func<BrainStatusData, Task> handler)
        {
            On("BrainStatusUpdate", handler);
        }

        public void HandlePerformanceMetricsUpdate(Func<PerformanceMetricsData, Task> handler)
        {
            On("PerformanceMetricsUpdate", handler);
        }

        #endregion

        private Task HandleConnectionClosed(Exception? exception)
        {
            if (exception != null)
            {
                _logger.LogWarning(exception, "SignalR connection closed unexpectedly");
            }
            else
            {
                _logger.LogInformation("SignalR connection closed gracefully");
            }
            return Task.CompletedTask;
        }

        private Task HandleReconnected(string? connectionId)
        {
            _logger.LogInformation("SignalR connection reestablished with ID: {ConnectionId}", connectionId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Disposes the client and cleans up resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _connection?.DisposeAsync().GetAwaiter().GetResult();
                }
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Factory class for creating typed SignalR clients.
    /// </summary>
    public static class SignalRClientFactory
    {
        /// <summary>
        /// Creates a new typed SignalR client.
        /// </summary>
        /// <param name="url">The URL of the SignalR hub.</param>
        /// <param name="logger">The logger instance.</param>
        /// <returns>A new instance of TypedSignalRClient.</returns>
        public static TypedSignalRClient CreateClient(string url, ILogger<TypedSignalRClient> logger)
        {
            var connection = new HubConnectionBuilder()
                .WithUrl(url)
                .WithAutomaticReconnect()
                .Build();

            return new TypedSignalRClient(connection, logger);
        }
    }
}
