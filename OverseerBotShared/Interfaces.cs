using System.Threading.Tasks;

namespace OverseerBotShared
{
    /// <summary>
    /// Interface defining the contract for SignalR hub communication between BacklashBot and Overseer.
    /// Provides strongly typed methods for all hub operations to ensure compile-time validation.
    /// </summary>
    public interface IBacklashBotHub
    {
        // Client-to-Server methods (called by BacklashBot)
        Task<HandshakeResponse> Handshake(string clientId, string clientName, string clientType, string? authToken = null);
        Task<CheckInResponse> CheckIn(CheckInData checkInData);
        Task<PerformanceMetricsResponse> SendPerformanceMetrics(PerformanceMetricsData performanceMetrics);
        Task ConfirmTargetTickersReceived(string brainInstanceName);
        Task<MessageResponse> SendOverseerMessage(string messageType, string message);

        // Server-to-Client methods (handled by BacklashBot)
        Task HandleHandshakeResponse(HandshakeResponse response);
        Task HandleCheckInResponse(CheckInResponse response);
        Task HandleTargetTickersConfirmationResponse(TargetTickersConfirmationResponse response);
        Task HandlePerformanceMetricsResponse(PerformanceMetricsResponse response);
        Task HandleMessageResponse(MessageResponse response);
        Task HandleDataRefreshRequested(object refreshData);
        Task HandleBroadcastTrace(object traceData);
        Task HandleBrainStatusUpdate(BrainStatusData brainStatus);
        Task HandlePerformanceMetricsUpdate(PerformanceMetricsData performanceMetrics);
    }

    /// <summary>
    /// Interface defining the contract for Overseer hub communication.
    /// Provides strongly typed methods for all overseer hub operations.
    /// </summary>
    public interface IOverseerHub
    {
        // Client-to-Server methods (called by Overseer clients like web UI)
        Task<HandshakeResponse> Handshake(string clientId, string clientName, string clientType, string? authToken = null);

        // Server-to-Client methods (handled by Overseer)
        Task HandleCheckIn(CheckInData checkInData);
        Task HandlePerformanceMetrics(PerformanceMetricsData performanceMetrics);
        Task HandlePerformanceMetricsUpdate(string brainInstanceName, object performanceMetrics, DateTime timestamp);
        Task HandleOverseerMessage(string messageType, string message);

        // Broadcast methods
        Task BroadcastBrainStatusUpdate(BrainStatusData brainStatus);
        Task BroadcastPerformanceMetricsUpdate(PerformanceMetricsData performanceMetrics);
        Task BroadcastDataRefreshRequested(object refreshData);
        Task BroadcastTrace(object traceData);
    }

    /// <summary>
    /// Interface for strongly typed SignalR client connections.
    /// Provides methods to establish and manage typed hub connections.
    /// </summary>
    public interface ISignalRClient<T> where T : class
    {
        Task ConnectAsync(string url);
        Task DisconnectAsync();
        bool IsConnected { get; }
        T HubProxy { get; }
        Task InvokeAsync(string methodName, params object[] args);
        Task<TResponse> InvokeAsync<TResponse>(string methodName, params object[] args);
        void On<TData>(string methodName, Func<TData, Task> handler);
        void On(string methodName, Func<Task> handler);
    }

    /// <summary>
    /// Interface for SignalR connection management with resilience features.
    /// Includes automatic reconnection, circuit breaker pattern, and performance monitoring.
    /// </summary>
    public interface ISignalRConnectionManager
    {
        Task StartAsync();
        Task StopAsync();
        bool IsConnected { get; }
        string? CurrentUrl { get; }
        Task ReconnectAsync();
        Task SwitchToUrlAsync(string newUrl, string reason);

        // Performance metrics
        int ConnectionAttemptCount { get; }
        int ConnectionSuccessCount { get; }
        TimeSpan TotalConnectionTime { get; }
        int MessageSentCount { get; }
        int MessageReceivedCount { get; }
        Dictionary<string, object> GetMetrics();
    }

    /// <summary>
    /// Interface for error handling in SignalR communication.
    /// Provides centralized error processing and recovery mechanisms.
    /// </summary>
    public interface ISignalRErrorHandler
    {
        Task HandleConnectionErrorAsync(Exception exception, string context);
        Task HandleInvocationErrorAsync(Exception exception, string methodName, object[] args);
        Task HandleReconnectionErrorAsync(Exception exception);
        bool ShouldRetry(Exception exception);
        TimeSpan GetRetryDelay(int attemptCount);
    }
}