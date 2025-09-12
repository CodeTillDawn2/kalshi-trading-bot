using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using BacklashDTOs.KalshiAPI;

namespace BacklashBot.Services
{
    /// <summary>
    /// Service responsible for monitoring the Kalshi exchange status and managing WebSocket connection lifecycle.
    /// This service periodically checks the exchange's operational status and automatically connects/disconnects
    /// the WebSocket client based on exchange availability and bot initialization state. It ensures reliable
    /// real-time data streaming while handling connection failures and exchange downtime gracefully.
    /// </summary>
    /// <remarks>
    /// The service implements a background monitoring loop that:
    /// - Checks exchange status every minute during normal operation
    /// - Connects WebSocket only after market data initialization is complete
    /// - Automatically disconnects when exchange becomes inactive
    /// - Handles connection errors with appropriate retry logic and backoff
    /// - Provides manual trigger capability for immediate connection checks
    /// </remarks>
    public class WebSocketMonitorService : IWebSocketMonitorService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IServiceFactory _serviceFactory;
        private readonly ILogger<IWebSocketMonitorService> _logger;
        private bool _isWebSocketConnected = false;
        private Task _exchangeStatusMonitorTask;
        private readonly IScopeManagerService _scopeManagerService;
        private IStatusTrackerService _statusTrackerService;
        private IBotReadyStatus _readyStatus;

        /// <summary>
        /// Initializes a new instance of the WebSocketMonitorService with required dependencies.
        /// </summary>
        /// <param name="scopeFactory">Factory for creating service scopes for dependency resolution.</param>
        /// <param name="serviceFactory">Factory providing access to various bot services including WebSocket client.</param>
        /// <param name="logger">Logger for recording service operations and errors.</param>
        /// <param name="scopeManagerService">Service for managing dependency injection scopes.</param>
        /// <param name="readyStatus">Status tracker for bot initialization completion.</param>
        /// <param name="statusTrackerService">Service for managing cancellation tokens and operation status.</param>
        public WebSocketMonitorService(
            IServiceScopeFactory scopeFactory,
            IServiceFactory serviceFactory,
            ILogger<IWebSocketMonitorService> logger,
            IScopeManagerService scopeManagerService,
            IBotReadyStatus readyStatus,
            IStatusTrackerService statusTrackerService)
        {
            _scopeManagerService = scopeManagerService;
            _serviceFactory = serviceFactory;
            _statusTrackerService = statusTrackerService;
            _scopeFactory = scopeFactory;
            _readyStatus = readyStatus;
            _logger = logger;
            _logger.LogDebug("WebSocketMonitorService instance created");
        }

        /// <summary>
        /// Starts the WebSocket monitoring services by initiating the background exchange status monitoring task.
        /// This method begins the continuous monitoring loop that checks exchange status and manages WebSocket connections.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the monitoring operation if needed.</param>
        /// <remarks>
        /// The monitoring task runs in the background and will continue until cancelled or an error occurs.
        /// The task checks exchange status every minute and manages WebSocket connection state accordingly.
        /// </remarks>
        public void StartServices(CancellationToken cancellationToken)
        {
            _logger.LogDebug("WebSocketMonitorService starting...");
            try
            {
                _logger.LogDebug("Initial cache state: InitializationCompleted.IsCompleted={IsCompleted}", _readyStatus.InitializationCompleted.Task.IsCompleted);

                _logger.LogDebug("Starting exchange status monitoring...");
                _exchangeStatusMonitorTask = Task.Run(async () =>
                {
                    await MonitorAndManageWebSocketConnectionAsync();
                }, _statusTrackerService.GetCancellationToken());
                _logger.LogDebug("Started exchange status monitoring");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting WebSocketMonitorService");
                throw;
            }
            _logger.LogDebug("WebSocketMonitorService started.");
        }

        /// <summary>
        /// Stops the WebSocket monitoring services gracefully by closing any active WebSocket connection
        /// and waiting for the monitoring task to complete.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the shutdown operation if needed.</param>
        /// <returns>A task representing the asynchronous shutdown operation.</returns>
        /// <remarks>
        /// This method ensures clean shutdown by:
        /// - Closing the WebSocket connection if currently connected
        /// - Waiting for the monitoring task to complete or be cancelled
        /// - Handling any errors during shutdown gracefully
        /// </remarks>
        public async Task StopServicesAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("WebSocketMonitorService.StopAsync called at {0}, CancellationToken.IsCancellationRequested={IsRequested}", DateTime.UtcNow, _statusTrackerService.GetCancellationToken().IsCancellationRequested);
            try
            {
                if (_isWebSocketConnected)
                {
                    try
                    {
                        await _serviceFactory.GetKalshiWebSocketClient().StopServicesAsync();
                        _isWebSocketConnected = false;
                        _logger.LogInformation("WebSocket connection closed.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error closing WebSocket connection during shutdown");
                    }
                }
                if (_exchangeStatusMonitorTask != null && !_exchangeStatusMonitorTask.IsCompleted)
                {
                    _logger.LogDebug("Waiting for exchange status monitoring task to complete...");
                    try
                    {
                        await _exchangeStatusMonitorTask.ConfigureAwait(false);
                        _logger.LogInformation("Exchange status monitoring task completed.");
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Exchange status monitoring task canceled as expected.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error waiting for exchange status monitoring task to complete");
                    }
                }
            }
            finally
            {
                _logger.LogDebug("WebSocketMonitorService.StopAsync completed at {0}", DateTime.UtcNow);
            }
        }

        /// <summary>
        /// Triggers an immediate WebSocket connection check outside the normal monitoring cycle.
        /// This method performs a one-time check of exchange status and updates connection state accordingly.
        /// </summary>
        /// <returns>A task representing the asynchronous connection check operation.</returns>
        /// <remarks>
        /// This is useful for manual intervention or when an immediate status update is required.
        /// The check is performed synchronously without waiting for the next scheduled monitoring interval.
        /// </remarks>
        public async Task TriggerConnectionCheckAsync()
        {
            _logger.LogDebug("Triggering immediate WebSocket connection check");
            await MonitorAndManageWebSocketConnectionAsync(immediate: true);
        }

        /// <summary>
        /// Gets the current WebSocket connection status.
        /// </summary>
        /// <returns>True if the WebSocket is currently connected, false otherwise.</returns>
        /// <remarks>
        /// This method provides a thread-safe way to check the current connection state.
        /// The connection state is managed internally by the monitoring logic.
        /// </remarks>
        public bool IsConnected()
        {
            return _isWebSocketConnected;
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
        /// - Updates data cache with current exchange and trading status
        /// - Connects WebSocket when exchange is active and initialization is complete
        /// - Disconnects WebSocket when exchange becomes inactive
        /// - Handles errors with appropriate retry delays
        /// - Respects cancellation tokens for graceful shutdown
        /// </remarks>
        private async Task MonitorAndManageWebSocketConnectionAsync(bool immediate = false)
        {
            _logger.LogDebug("MonitorAndManageWebSocketConnectionAsync started at {0}, Immediate={Immediate}, CancellationToken.IsCancellationRequested={IsRequested}", DateTime.UtcNow, immediate, _statusTrackerService.GetCancellationToken().IsCancellationRequested);
            try
            {
                if (!immediate)
                {
                    while (!_statusTrackerService.GetCancellationToken().IsCancellationRequested)
                    {
                        try
                        {
                            _logger.LogDebug("Checking exchange status, Current _isWebSocketConnected: {IsConnected}", _isWebSocketConnected);
                            using var scope = _scopeFactory.CreateScope();
                            var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();
                            ExchangeStatus status = await apiService.GetExchangeStatusAsync();
                            _serviceFactory.GetDataCache().ExchangeStatus = status.exchange_active;
                            _serviceFactory.GetDataCache().TradingStatus = status.trading_active;
                            _logger.LogDebug("Updated DataCache.ExchangeStatus to {Status} and TradingStatus to {tradingStatus}", _serviceFactory.GetDataCache().ExchangeStatus, _serviceFactory.GetDataCache().TradingStatus);

                            if (status.exchange_active && !_isWebSocketConnected)
                            {
                                // Only connect if market data initialization is complete
                                if (_readyStatus.InitializationCompleted.Task.IsCompleted && _readyStatus.InitializationCompleted.Task.Result)
                                {
                                    _logger.LogInformation("Exchange is active and initialization complete, connecting WebSocket");
                                    await _serviceFactory.GetKalshiWebSocketClient().ConnectAsync();
                                    _isWebSocketConnected = true;
                                    _logger.LogDebug("WebSocket connected successfully");
                                }
                                else
                                {
                                    _logger.LogDebug("Exchange is active but initialization not complete yet, waiting for MarketDataInitializer to finish");
                                }
                            }
                            else if (!status.exchange_active && _isWebSocketConnected)
                            {
                                _logger.LogWarning("Exchange is inactive, resetting WebSocket connection");
                                _serviceFactory.GetDataCache().LastWebSocketTimestamp = DateTime.UtcNow;
                                await _serviceFactory.GetKalshiWebSocketClient().ResetConnectionAsync();
                                _isWebSocketConnected = false;
                                _logger.LogWarning("WebSocket connection reset due to inactive exchange, will try again in one minute");
                            }

                            await Task.Delay(TimeSpan.FromMinutes(1), _statusTrackerService.GetCancellationToken()).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogDebug("Exchange status monitoring cancelled at {0}", DateTime.UtcNow);
                            return;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error in exchange status monitoring");
                            _isWebSocketConnected = false;
                            _serviceFactory.GetDataCache().ExchangeStatus = false;
                            _serviceFactory.GetDataCache().TradingStatus = false;
                            await Task.Delay(TimeSpan.FromMinutes(5), _statusTrackerService.GetCancellationToken()).ConfigureAwait(false);
                        }
                    }
                }
                else
                {
                    try
                    {
                        _logger.LogDebug("Performing immediate exchange status check");
                        using var scope = _scopeFactory.CreateScope();
                        var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();
                        ExchangeStatus status = await apiService.GetExchangeStatusAsync();
                        _serviceFactory.GetDataCache().ExchangeStatus = status.exchange_active;
                        _serviceFactory.GetDataCache().TradingStatus = status.trading_active;
                        _logger.LogDebug("Updated DataCache.ExchangeStatus to {Status} and TradingStatus to {tradingStatus}", _serviceFactory.GetDataCache().ExchangeStatus, _serviceFactory.GetDataCache().TradingStatus);

                        if (status.exchange_active && !_isWebSocketConnected)
                        {
                            // Only connect if market data initialization is complete
                            if (_readyStatus.InitializationCompleted.Task.IsCompleted && _readyStatus.InitializationCompleted.Task.Result)
                            {
                                _logger.LogInformation("Exchange is active and initialization complete, connecting WebSocket");
                                await _serviceFactory.GetKalshiWebSocketClient().ConnectAsync(0);
                                _isWebSocketConnected = true;
                                _logger.LogInformation("WebSocket connected successfully");
                            }
                            else
                            {
                                _logger.LogDebug("Exchange is active but initialization not complete yet, waiting for MarketDataInitializer to finish");
                            }
                        }
                        else if (!status.exchange_active && _isWebSocketConnected)
                        {
                            _logger.LogWarning("Exchange is inactive, resetting WebSocket connection");
                            await _serviceFactory.GetKalshiWebSocketClient().ResetConnectionAsync();
                            _isWebSocketConnected = false;
                            _logger.LogInformation("WebSocket connection reset due to inactive exchange");
                        }
                        else
                        {
                            _logger.LogDebug("Exchange status unchanged: Active={ExchangeActive}, Connected={IsConnected}", status.exchange_active, _isWebSocketConnected);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogDebug("Immediate exchange status check cancelled at {0}", DateTime.UtcNow);
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in immediate exchange status check");
                        _isWebSocketConnected = false;
                        _serviceFactory.GetDataCache().ExchangeStatus = false;
                        _serviceFactory.GetDataCache().TradingStatus = false;
                    }
                }
            }
            finally
            {
                _logger.LogDebug("MonitorAndManageWebSocketConnectionAsync completed at {0}, CancellationToken.IsCancellationRequested={IsRequested}", DateTime.UtcNow, _statusTrackerService.GetCancellationToken().IsCancellationRequested);
            }
        }
    }
}
