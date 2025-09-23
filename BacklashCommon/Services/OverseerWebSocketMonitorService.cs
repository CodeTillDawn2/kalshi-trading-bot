using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashDTOs.KalshiAPI;
using KalshiBotAPI.WebSockets.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BacklashCommon.Services
{
    /// <summary>
    /// Lightweight service for monitoring WebSocket connections and exchange status.
    /// This service monitors the exchange status and manages WebSocket connections without
    /// modifying BacklashBot-specific objects.
    /// </summary>
    public class OverseerWebSocketMonitorService : IWebSocketMonitorService
    {
        private readonly ILogger<OverseerWebSocketMonitorService> _logger;
        private bool _isConnected = false;
        private Task? _monitorTask;
        private readonly IServiceScopeFactory _scopeFactory;
        private IKalshiWebSocketClient _webSocketClient;

        private bool _exchangeStatus;
        private bool _tradingStatus;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Gets the cancellation token for the service.
        /// </summary>
        public CancellationToken CancellationToken { get; private set; }

        /// <summary>
        /// Initializes a new instance of the OverseerWebSocketMonitorService class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="webSocketClient">The WebSocket client.</param>
        /// <param name="scopeFactory">The service scope factory.</param>
        public OverseerWebSocketMonitorService(
            ILogger<OverseerWebSocketMonitorService> logger,
            IKalshiWebSocketClient webSocketClient,
            IServiceScopeFactory scopeFactory)
        {
            CancellationToken = _cancellationTokenSource.Token;
            _scopeFactory = scopeFactory;
            _webSocketClient = webSocketClient;
            _logger = logger;
            _logger.LogDebug("OverseerWebSocketMonitorService instance created");
        }

        /// <summary>
        /// Starts the WebSocket monitoring services.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        public void StartServices(CancellationToken cancellationToken)
        {
            _logger.LogDebug("OverseerWebSocketMonitorService starting...");
            try
            {
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                CancellationToken = _cancellationTokenSource.Token;
                _logger.LogDebug("Starting exchange status monitoring...");
                _monitorTask = Task.Run(async () =>
                {
                    await MonitorExchangeStatusAsync();
                }, CancellationToken);
                _logger.LogDebug("Started exchange status monitoring");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting OverseerWebSocketMonitorService");
                throw;
            }
            _logger.LogDebug("OverseerWebSocketMonitorService started.");
        }

        /// <summary>
        /// Shuts down the WebSocket monitoring services asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ShutdownAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("OverseerWebSocketMonitorService.StopAsync called at {0}, CancellationToken.IsCancellationRequested={IsRequested}", DateTime.UtcNow,
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
                _logger.LogDebug("OverseerWebSocketMonitorService.StopAsync completed at {0}", DateTime.UtcNow);
            }
        }

        /// <summary>
        /// Triggers an immediate WebSocket connection check asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task TriggerConnectionCheckAsync()
        {
            _logger.LogDebug("Triggering immediate WebSocket connection check");
            await MonitorExchangeStatusAsync(immediate: true);
        }

        /// <summary>
        /// Checks if the WebSocket is connected.
        /// </summary>
        /// <returns>True if connected, false otherwise.</returns>
        public bool IsConnected()
        {
            return _webSocketClient.IsConnected();
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
                            _logger.LogDebug("Updated exchange status to {Status} and trading status to {tradingStatus}", _exchangeStatus, _tradingStatus);

                            if (status.exchange_active && !_isConnected)
                            {
                                _logger.LogInformation("Exchange is active, connecting WebSocket");
                                var client = _webSocketClient;
                                await client.ConnectAsync();

                                // Check if connection was actually successful
                                if (client.IsConnected())
                                {
                                    _isConnected = true;
                                    _logger.LogDebug("WebSocket connected successfully");
                                }
                                else
                                {
                                    _logger.LogWarning("WebSocket connection attempt failed");
                                }
                            }
                            else if (!status.exchange_active && _isConnected)
                            {
                                _logger.LogWarning("Exchange is inactive, resetting WebSocket connection");
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
                        _logger.LogDebug("Updated exchange status to {Status} and trading status to {tradingStatus}", _exchangeStatus, _tradingStatus);

                        if (status.exchange_active && !_isConnected)
                        {
                            _logger.LogInformation("Exchange is active, connecting WebSocket");
                            var client = _webSocketClient;
                            await client.ConnectAsync(0);

                            // Check if connection was actually successful
                            if (client.IsConnected())
                            {
                                _isConnected = true;
                                _logger.LogInformation("WebSocket connected successfully");
                            }
                            else
                            {
                                _logger.LogWarning("WebSocket connection attempt failed");
                            }
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