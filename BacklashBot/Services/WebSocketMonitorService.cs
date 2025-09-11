using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using BacklashDTOs.KalshiAPI;

namespace BacklashBot.Services
{
    public class WebSocketMonitorService : IWebSocketMonitorService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IServiceFactory _serviceFactory;
        private readonly ILogger<IWebSocketMonitorService> _logger;
        private bool _isConnected = false;
        private Task _monitorTask;
        private readonly IScopeManagerService _scopeManagerService;
        private IStatusTrackerService _statusTrackerService;
        private IBotReadyStatus _readyStatus;

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
            _logger.LogDebug("WebSocketHostedService instance created");
        }

        public void StartServices(CancellationToken cancellationToken)
        {
            _logger.LogDebug("WebSocketHostedService starting...");
            try
            {
                _logger.LogDebug("Initial cache state: InitializationCompleted.IsCompleted={IsCompleted}", _readyStatus.InitializationCompleted.Task.IsCompleted);

                _logger.LogDebug("Starting exchange status monitoring...");
                _monitorTask = Task.Run(async () =>
                {
                    await MonitorExchangeStatusAsync();
                }, _statusTrackerService.GetCancellationToken());
                _logger.LogDebug("Started exchange status monitoring");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting WebSocketHostedService");
                throw;
            }
            _logger.LogDebug("WebSocketHostedService started.");
        }

        public async Task StopServicesAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("WebSocketHostedService.StopAsync called at {0}, CancellationToken.IsCancellationRequested={IsRequested}", DateTime.UtcNow, _statusTrackerService.GetCancellationToken().IsCancellationRequested);
            try
            {
                if (_isConnected)
                {
                    try
                    {
                        await _serviceFactory.GetKalshiWebSocketClient().StopServicesAsync();
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
                _logger.LogDebug("WebSocketHostedService.StopAsync completed at {0}", DateTime.UtcNow);
            }
        }

        public async Task TriggerConnectionCheckAsync()
        {
            _logger.LogDebug("Triggering immediate WebSocket connection check");
            await MonitorExchangeStatusAsync(immediate: true);
        }

        public bool IsConnected()
        {
            return _isConnected;
        }

        private async Task MonitorExchangeStatusAsync(bool immediate = false)
        {
            _logger.LogDebug("MonitorExchangeStatusAsync started at {0}, Immediate={Immediate}, CancellationToken.IsCancellationRequested={IsRequested}", DateTime.UtcNow, immediate, _statusTrackerService.GetCancellationToken().IsCancellationRequested);
            try
            {
                if (!immediate)
                {
                    while (!_statusTrackerService.GetCancellationToken().IsCancellationRequested)
                    {
                        try
                        {
                            _logger.LogDebug("Checking exchange status, Current _isConnected: {IsConnected}", _isConnected);
                            using var scope = _scopeFactory.CreateScope();
                            var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();
                            ExchangeStatus status = await apiService.GetExchangeStatusAsync();
                            _serviceFactory.GetDataCache().ExchangeStatus = status.exchange_active;
                            _serviceFactory.GetDataCache().TradingStatus = status.trading_active;
                            _logger.LogDebug("Updated DataCache.ExchangeStatus to {Status} and TradingStatus to {tradingStatus}", _serviceFactory.GetDataCache().ExchangeStatus, _serviceFactory.GetDataCache().TradingStatus);

                            if (status.exchange_active && !_isConnected)
                            {
                                // Only connect if market data initialization is complete
                                if (_readyStatus.InitializationCompleted.Task.IsCompleted && _readyStatus.InitializationCompleted.Task.Result)
                                {
                                    _logger.LogInformation("Exchange is active and initialization complete, connecting WebSocket");
                                    await _serviceFactory.GetKalshiWebSocketClient().ConnectAsync();
                                    _isConnected = true;
                                    _logger.LogDebug("WebSocket connected successfully");
                                }
                                else
                                {
                                    _logger.LogDebug("Exchange is active but initialization not complete yet, waiting for MarketDataInitializer to finish");
                                }
                            }
                            else if (!status.exchange_active && _isConnected)
                            {
                                _logger.LogWarning("Exchange is inactive, resetting WebSocket connection");
                                _serviceFactory.GetDataCache().LastWebSocketTimestamp = DateTime.UtcNow;
                                await _serviceFactory.GetKalshiWebSocketClient().ResetConnectionAsync();
                                _isConnected = false;
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
                            _isConnected = false;
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

                        if (status.exchange_active && !_isConnected)
                        {
                            // Only connect if market data initialization is complete
                            if (_readyStatus.InitializationCompleted.Task.IsCompleted && _readyStatus.InitializationCompleted.Task.Result)
                            {
                                _logger.LogInformation("Exchange is active and initialization complete, connecting WebSocket");
                                await _serviceFactory.GetKalshiWebSocketClient().ConnectAsync(0);
                                _isConnected = true;
                                _logger.LogInformation("WebSocket connected successfully");
                            }
                            else
                            {
                                _logger.LogDebug("Exchange is active but initialization not complete yet, waiting for MarketDataInitializer to finish");
                            }
                        }
                        else if (!status.exchange_active && _isConnected)
                        {
                            _logger.LogWarning("Exchange is inactive, resetting WebSocket connection");
                            await _serviceFactory.GetKalshiWebSocketClient().ResetConnectionAsync();
                            _isConnected = false;
                            _logger.LogDebug("WebSocket connection reset due to inactive exchange");
                        }
                        else
                        {
                            _logger.LogDebug("Exchange status unchanged: Active={ExchangeActive}, Connected={IsConnected}", status.exchange_active, _isConnected);
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
                        _isConnected = false;
                        _serviceFactory.GetDataCache().ExchangeStatus = false;
                        _serviceFactory.GetDataCache().TradingStatus = false;
                    }
                }
            }
            finally
            {
                _logger.LogDebug("MonitorExchangeStatusAsync completed at {0}, CancellationToken.IsCancellationRequested={IsRequested}", DateTime.UtcNow, _statusTrackerService.GetCancellationToken().IsCancellationRequested);
            }
        }
    }
}
