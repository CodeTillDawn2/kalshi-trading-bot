using Microsoft.AspNetCore.SignalR;
using BacklashBot.Hubs;
using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using System.Diagnostics;
using BacklashBot.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace BacklashBot.Services
{
    public class BroadcastService : IBroadcastService
    {
        private readonly IHubContext<ChartHub> _hubContext;
        private readonly IServiceFactory _serviceFactory;
        private readonly ILogger<IBroadcastService> _logger;
        private Task? _checkInBroadcastTask;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IScopeManagerService _scopeManagerService;
        private readonly IStatusTrackerService _statusTracker;
        private readonly ExecutionConfig _executionConfig;

        public BroadcastService(
            IHubContext<ChartHub> hubContext,
            IServiceFactory serviceFactory,
            IStatusTrackerService statusTracker,
            IServiceScopeFactory scopeFactory,
            ILogger<IBroadcastService> logger,
            IScopeManagerService scopeManagerService,
            IOptions<ExecutionConfig> executionConfig)
        {
            _scopeManagerService = scopeManagerService;
            _hubContext = hubContext;
            _statusTracker = statusTracker;
            _serviceFactory = serviceFactory;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _executionConfig = executionConfig.Value;
        }

        public async Task StartServicesAsync()
        {
            try
            {
                _logger.LogDebug("BroadcastService starting...");

                var cancellationToken = _statusTracker.GetCancellationToken();

                // Only keep the 30-second CheckIn broadcast loop - no automatic market data broadcasting
                _checkInBroadcastTask = Task.Run(async () =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            if (ChartHub.HasConnectedClients())
                            {
                                await BroadcastCheckInAsync();
                            }
                            else
                            {
                                _logger.LogDebug("No clients connected, skipping CheckIn broadcast.");
                            }
                            await Task.Delay(30000, cancellationToken); // 30 seconds
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogDebug("CheckIn broadcast task canceled.");
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error in CheckIn broadcast cycle.");
                        }
                    }
                }, cancellationToken);

                _logger.LogDebug("BroadcastService started with automatic check-ins only.");
                await Task.CompletedTask;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("BroadcastService.StartAsync stopped due to cancellation");
                return;
            }
        }

        private async Task BroadcastCheckInAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            var cancellationToken = _statusTracker.GetCancellationToken();
            cancellationToken.ThrowIfCancellationRequested();
            if (!ChartHub.HasConnectedClients())
            {
                _logger.LogDebug("No clients connected, skipping CheckIn broadcast.");
                return;
            }

            _logger.LogDebug("Broadcasting CheckIn");
            try
            {
                var markets = await GetWatchedMarketsAsync();
                var errorHandler = _serviceFactory.GetBacklashErrorHandler();
                var lastSnapshot = errorHandler.LastSuccessfulSnapshot;
                var lastErrorDate = errorHandler.LastErrorDate;

                // Get brain instance name from configuration
                var brainInstanceName = _executionConfig.BrainInstance ?? "Unknown";

                var checkInData = new
                {
                    BrainInstanceName = brainInstanceName,
                    Markets = markets,
                    ErrorCount = errorHandler.ErrorCount,
                    LastSnapshot = lastSnapshot == DateTime.MinValue ? (DateTime?)null : lastSnapshot,
                    LastErrorDate = lastErrorDate == DateTime.MinValue ? (DateTime?)null : lastErrorDate,
                    Timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients.All.SendAsync("CheckIn", checkInData, cancellationToken);
                _logger.LogDebug("CheckIn broadcasted from {BrainInstanceName} with {MarketCount} markets, ErrorCount: {ErrorCount}, LastSnapshot: {LastSnapshot}, LastErrorDate: {LastErrorDate}",
                    brainInstanceName, markets.Count, errorHandler.ErrorCount, lastSnapshot, lastErrorDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting CheckIn");
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        private async Task<List<string>> GetWatchedMarketsAsync()
        {
            var cancellationToken = _statusTracker.GetCancellationToken();
            cancellationToken.ThrowIfCancellationRequested();
            var marketDataService = _serviceFactory.GetMarketDataService();
            var markets = await marketDataService.FetchWatchedMarketsAsync();
            return markets;
        }

        public async Task StopServicesAsync()
        {
            _logger.LogDebug("BroadcastService stopping...");
            try
            {
                var tasksToWait = new List<Task>();
                if (_checkInBroadcastTask != null) tasksToWait.Add(_checkInBroadcastTask);
                if (tasksToWait.Any())
                {
                    await Task.WhenAll(tasksToWait).ConfigureAwait(false);
                }
                ChartHub.ClearConnectedClients();
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("BroadcastService tasks canceled as expected.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping BroadcastService.");
            }
            _logger.LogDebug("BroadcastService stopped.");
        }




        public void Dispose()
        {
            _checkInBroadcastTask?.Dispose();
        }






    }
}
