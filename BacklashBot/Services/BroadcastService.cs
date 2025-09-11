// BroadcastService.cs
using Microsoft.AspNetCore.SignalR;
using BacklashBot.Hubs;
using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using System.Diagnostics;
using BacklashBot.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using KalshiBotData.Models;
using BacklashBot.Management;

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
                var performanceTracker = _serviceFactory.GetPerformanceMonitor();
                if (errorHandler == null || performanceTracker == null) return;

                var lastSnapshot = errorHandler.LastSuccessfulSnapshot;
                var lastErrorDate = errorHandler.LastErrorDate;

                // Get queue averages and CPU usage
                (double eventQueueAvg, double tickerQueueAvg, double notificationQueueAvg, double orderBookQueueAvg) = performanceTracker.GetQueueCountRollingAverages();
                var currentCpuUsage = performanceTracker.LastRefreshUsagePercentage;
                var webSocketService = _serviceFactory.GetWebSocketHostedService();
                var isWebSocketConnected = webSocketService?.IsConnected() ?? false;

                // Get brain instance name and status from Performance Monitor
                var isStartingUp = performanceTracker.IsStartingUp;
                var isShuttingDown = performanceTracker.IsShuttingDown;

                var brainInstanceName = performanceTracker.BrainInstance;
                _logger.LogInformation("BROADCAST: Creating CheckInData with BrainInstanceName='{BrainInstanceName}'", brainInstanceName);

                var checkInData = new CheckInData
                {
                    BrainInstanceName = brainInstanceName,
                    Markets = markets,
                    ErrorCount = errorHandler.ErrorCount,
                    LastSnapshot = lastSnapshot == DateTime.MinValue ? (DateTime?)null : lastSnapshot,
                    IsStartingUp = isStartingUp,
                    IsShuttingDown = isShuttingDown,
                    WatchPositions = false, // Set based on configuration if available
                    WatchOrders = false, // Set based on configuration if available
                    ManagedWatchList = false, // Set based on configuration if available
                    CaptureSnapshots = false, // Set based on configuration if available
                    TargetWatches = 0, // Set based on configuration if available
                    MinimumInterest = 0.0, // Set based on configuration if available
                    UsageMin = 0.0, // Set based on configuration if available
                    UsageMax = 0.0, // Set based on configuration if available
                    CurrentCpuUsage = currentCpuUsage,
                    EventQueueAvg = eventQueueAvg,
                    TickerQueueAvg = tickerQueueAvg,
                    NotificationQueueAvg = notificationQueueAvg,
                    OrderbookQueueAvg = orderBookQueueAvg,
                    IsWebSocketConnected = isWebSocketConnected,
                    LastRefreshCycleSeconds = performanceTracker.LastRefreshCycleSeconds,
                    LastRefreshCycleInterval = TimeSpan.FromSeconds(performanceTracker.LastRefreshCycleInterval),
                    LastRefreshMarketCount = performanceTracker.LastRefreshMarketCount,
                    LastRefreshUsagePercentage = performanceTracker.LastRefreshUsagePercentage,
                    LastRefreshTimeAcceptable = performanceTracker.LastRefreshTimeAcceptable,
                    LastPerformanceSampleDate = performanceTracker.LastPerformanceSampleDate
                };

                await _hubContext.Clients.All.SendAsync("CheckIn", checkInData, cancellationToken);
                var perfMonitor = _serviceFactory.GetPerformanceMonitor();
                _logger.LogDebug("CheckIn broadcasted from {BrainInstanceName} with {MarketCount} markets, ErrorCount: {ErrorCount}, LastSnapshot: {LastSnapshot}, LastErrorDate: {LastErrorDate}",
                    perfMonitor?.BrainInstance ?? "Unknown", markets.Count, errorHandler.ErrorCount, lastSnapshot, lastErrorDate);
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
            if (marketDataService == null) return new List<string>();
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