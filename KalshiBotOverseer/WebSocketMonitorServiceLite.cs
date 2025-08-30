using KalshiBotAPI.WebSockets.Interfaces;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmokehouseBot.KalshiAPI.Interfaces;
using SmokehouseBot.Management.Interfaces;
using SmokehouseBot.Services.Interfaces;
using SmokehouseDTOs.KalshiAPI;
using System.Threading;

namespace KalshiBotOverseer
{
    public class WebSocketMonitorServiceLite : IWebSocketMonitorService
    {
        private readonly ILogger<WebSocketMonitorServiceLite> _logger;
        private bool _isConnected = false;
        private Task _monitorTask;
        private readonly IServiceScopeFactory _scopeFactory;
        private IKalshiWebSocketClient _webSocketClient;

        private bool _exchangeStatus;
        private bool _tradingStatus;
        private DateTime _lastWebSocketTimestamp;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public CancellationToken CancellationToken { get; private set; }

        public WebSocketMonitorServiceLite(
            ILogger<WebSocketMonitorServiceLite> logger,
            IKalshiWebSocketClient webSocketClient,
            IServiceScopeFactory scopeFactory)
        {
            CancellationToken = _cancellationTokenSource.Token;
            _scopeFactory = scopeFactory;
            _webSocketClient = webSocketClient;
            _logger = logger;
            _logger.LogDebug("WebSocketMonitorServiceLite instance created");
        }

        public void StartServices(CancellationToken cancellationToken)
        {
            _logger.LogDebug("WebSocketMonitorServiceLite starting...");
            try
            {
                _logger.LogDebug("Starting exchange status monitoring...");
                _monitorTask = Task.Run(async () =>
                {
                    await MonitorExchangeStatusAsync();
                }, CancellationToken);
                _logger.LogDebug("Started exchange status monitoring");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting WebSocketMonitorServiceLite");
                throw;
            }
            _logger.LogDebug("WebSocketMonitorServiceLite started.");
        }

        public async Task StopServicesAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("WebSocketMonitorServiceLite.StopAsync called at {0}, CancellationToken.IsCancellationRequested={IsRequested}", DateTime.UtcNow,
                CancellationToken.IsCancellationRequested);
            try
            {
                if (_isConnected)
                {
                    try
                    {
                        await _webSocketClient.StopServicesAsync();
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
                _logger.LogDebug("WebSocketMonitorServiceLite.StopAsync completed at {0}", DateTime.UtcNow);
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
            _logger.LogDebug("MonitorExchangeStatusAsync started at {0}, Immediate={Immediate}, CancellationToken.IsCancellationRequested={IsRequested}", DateTime.UtcNow, immediate, 
                CancellationToken.IsCancellationRequested);
            try
            {
                if (!immediate)
                {
                    while (!CancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            _logger.LogDebug("Checking exchange status, Current _isConnected: {IsConnected}", _isConnected);
                            using var scope = _scopeFactory.CreateScope();
                            var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();
                            ExchangeStatus status = await apiService.GetExchangeStatusAsync();
                            _exchangeStatus = status.exchange_active;
                            _tradingStatus = status.trading_active;
                            _logger.LogDebug("Updated DataCache.ExchangeStatus to {Status} and TradingStatus to {tradingStatus}", _exchangeStatus, _tradingStatus);

                            if (status.exchange_active && !_isConnected)
                            {
                                _logger.LogInformation("Exchange is active, connecting WebSocket");
                                var client = _webSocketClient;
                                await client.ConnectAsync();

                                // Subscribe only to Fill, MarketLifecycle, and EventLifecycle (global, no market tickers)
                                await client.SubscribeToChannelAsync("fill", new string[0]);
                                await client.SubscribeToChannelAsync("market_lifecycle", new string[0]);
                                await client.SubscribeToChannelAsync("event_lifecycle", new string[0]);

                                _isConnected = true;
                                _logger.LogDebug("WebSocket connected and subscribed to Fill, MarketLifecycle, and EventLifecycle successfully");
                            }
                            else if (!status.exchange_active && _isConnected)
                            {
                                _logger.LogWarning("Exchange is inactive, resetting WebSocket connection");
                                _lastWebSocketTimestamp = DateTime.UtcNow;
                                await _webSocketClient.ResetConnectionAsync();
                                _isConnected = false;
                                _logger.LogWarning("WebSocket connection reset due to inactive exchange, will try again in one minute");
                            }

                            await Task.Delay(TimeSpan.FromMinutes(1), CancellationToken).ConfigureAwait(false);
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
                            _exchangeStatus = false;
                            _tradingStatus = false;
                            await Task.Delay(TimeSpan.FromMinutes(5), CancellationToken).ConfigureAwait(false);
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
                        _exchangeStatus = status.exchange_active;
                        _tradingStatus = status.trading_active;
                        _logger.LogDebug("Updated DataCache.ExchangeStatus to {Status} and TradingStatus to {tradingStatus}", _exchangeStatus, _tradingStatus);

                        if (status.exchange_active && !_isConnected)
                        {
                            _logger.LogInformation("Exchange is active, connecting WebSocket");
                            var client = _webSocketClient;
                            await client.ConnectAsync(0);

                            // Subscribe only to Fill, MarketLifecycle, and EventLifecycle (global, no market tickers)
                            await client.SubscribeToChannelAsync("fill", new string[0]);
                            await client.SubscribeToChannelAsync("market_lifecycle", new string[0]);
                            await client.SubscribeToChannelAsync("event_lifecycle", new string[0]);

                            _isConnected = true;
                            _logger.LogInformation("WebSocket connected and subscribed to Fill, MarketLifecycle, and EventLifecycle successfully");
                        }
                        else if (!status.exchange_active && _isConnected)
                        {
                            _logger.LogWarning("Exchange is inactive, resetting WebSocket connection");
                            await _webSocketClient.ResetConnectionAsync();
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
                        _exchangeStatus = false;
                        _tradingStatus = false;
                    }
                }
            }
            finally
            {
                _logger.LogDebug("MonitorExchangeStatusAsync completed at {0}, CancellationToken.IsCancellationRequested={IsRequested}", DateTime.UtcNow, 
                    CancellationToken.IsCancellationRequested);
            }
        }
    }
}