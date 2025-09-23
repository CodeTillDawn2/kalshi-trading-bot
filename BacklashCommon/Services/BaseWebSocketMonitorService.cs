using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using BacklashDTOs.KalshiAPI;
using BacklashInterfaces.PerformanceMetrics;
using KalshiBotAPI.WebSockets.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BacklashCommon.Services
{
    /// <summary>
    /// Base service for monitoring WebSocket connections and exchange status.
    /// This service provides the core functionality for monitoring exchange status and managing WebSocket connections.
    /// Derived classes can override the monitoring logic for specific requirements.
    /// </summary>
    public abstract class BaseWebSocketMonitorService : IWebSocketMonitorService
    {
        /// <summary>
        /// Logger for recording service operations and errors.
        /// </summary>
        protected readonly ILogger<IWebSocketMonitorService> _logger;

        /// <summary>
        /// Performance monitor for tracking metrics.
        /// </summary>
        protected readonly IPerformanceMonitor _performanceMonitor;

        /// <summary>
        /// Status tracker for bot initialization completion.
        /// </summary>
        protected readonly IBotReadyStatus _readyStatus;

        /// <summary>
        /// Indicates whether the WebSocket is currently connected.
        /// </summary>
        protected bool _isConnected = false;

        /// <summary>
        /// The background monitoring task.
        /// </summary>
        protected Task? _monitorTask;

        /// <summary>
        /// Factory for creating service scopes for dependency resolution.
        /// </summary>
        protected readonly IServiceScopeFactory _scopeFactory;

        /// <summary>
        /// The WebSocket client for managing connections.
        /// </summary>
        protected IKalshiWebSocketClient _webSocketClient;

        /// <summary>
        /// Cancellation token source for managing service shutdown.
        /// </summary>
        protected CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Gets the cancellation token for the service.
        /// </summary>
        public CancellationToken CancellationToken { get; private set; }

        /// <summary>
        /// Initializes a new instance of the BaseWebSocketMonitorService class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="webSocketClient">The WebSocket client.</param>
        /// <param name="scopeFactory">The service scope factory.</param>
        /// <param name="performanceMonitor">The performance monitor for metrics.</param>
        /// <param name="readyStatus">The bot ready status for initialization checking.</param>
        public BaseWebSocketMonitorService(
            ILogger<IWebSocketMonitorService> logger,
            IKalshiWebSocketClient webSocketClient,
            IServiceScopeFactory scopeFactory,
            IPerformanceMonitor performanceMonitor,
            IBotReadyStatus readyStatus)
        {
            CancellationToken = _cancellationTokenSource.Token;
            _scopeFactory = scopeFactory;
            _webSocketClient = webSocketClient;
            _performanceMonitor = performanceMonitor;
            _readyStatus = readyStatus;
            _logger = logger;
        }

        /// <summary>
        /// Starts the WebSocket monitoring services.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        public void StartServices(CancellationToken cancellationToken)
        {
            _logger.LogDebug("BaseWebSocketMonitorService starting...");
            try
            {
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                CancellationToken = _cancellationTokenSource.Token;
                _logger.LogDebug("Starting exchange status monitoring...");
                _monitorTask = Task.Run(async () =>
                {
                    await MonitorAndManageWebSocketConnectionAsync();
                }, CancellationToken);
                _logger.LogDebug("Started exchange status monitoring");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting BaseWebSocketMonitorService");
                throw;
            }
            _logger.LogDebug("BaseWebSocketMonitorService started.");
        }

        /// <summary>
        /// Shuts down the WebSocket monitoring services asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ShutdownAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("BaseWebSocketMonitorService.StopAsync called at {0}, CancellationToken.IsCancellationRequested={IsRequested}", DateTime.UtcNow,
                CancellationToken.IsCancellationRequested);
            _cancellationTokenSource.Cancel();
            try
            {
                if (_isConnected)
                {
                    try
                    {
                        await _webSocketClient.ShutdownAsync();
                        _isConnected = false;
                        _logger.LogDebug("WebSocket connection closed.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error closing WebSocket connection during shutdown");
                    }
                }
                if (_monitorTask != null && !_monitorTask.IsCompleted)
                {
                    _logger.LogDebug("Waiting for exchange status monitoring task to complete...");
                    try
                    {
                        await _monitorTask.ConfigureAwait(false);
                        _logger.LogDebug("Exchange status monitoring task completed.");
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogDebug("Exchange status monitoring task canceled as expected.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error waiting for exchange status monitoring task to complete");
                    }
                }
            }
            finally
            {
                _logger.LogDebug("BaseWebSocketMonitorService.StopAsync completed at {0}", DateTime.UtcNow);
            }
        }

        /// <summary>
        /// Triggers an immediate WebSocket connection check asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task TriggerConnectionCheckAsync()
        {
            _logger.LogDebug("Triggering immediate WebSocket connection check");
            await MonitorAndManageWebSocketConnectionAsync(immediate: true);
        }

        /// <summary>
        /// Checks if the WebSocket is connected.
        /// </summary>
        /// <returns>True if connected, false otherwise.</returns>
        public bool IsConnected()
        {
            return _webSocketClient.IsConnected();
        }

        /// <summary>
        /// Core monitoring method that checks exchange status and manages WebSocket connection state.
        /// This method runs continuously (or once if immediate) to monitor exchange availability and
        /// connect/disconnect the WebSocket client as appropriate.
        /// </summary>
        /// <param name="immediate">If true, performs a single check and returns. If false, runs continuous monitoring loop.</param>
        /// <returns>A task representing the asynchronous monitoring operation.</returns>
        /// <remarks>
        /// The monitoring logic:
        /// - Checks exchange status via API call
        /// - Connects WebSocket when exchange is active
        /// - Disconnects WebSocket when exchange becomes inactive
        /// - Handles errors with appropriate retry delays
        /// - Respects cancellation tokens for graceful shutdown
        /// </remarks>
        protected abstract Task MonitorAndManageWebSocketConnectionAsync(bool immediate = false);
    }
}