using KalshiBotData.Data.Interfaces;
using Microsoft.Extensions.Options;
using BacklashDTOs.Configuration;
using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.Management.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashDTOs;
using BacklashDTOs.Data;
using BacklashInterfaces.Constants;
using BacklashBot.State.Interfaces;
using TradingStrategies.Configuration;

namespace BacklashBot.Management
{
    public class ManagedMarketManagerService : BaseMarketManagerService
    {

        public ManagedMarketManagerService(IServiceFactory serviceFactory,
            ILogger<IMarketManagerService> logger,
            IServiceScopeFactory scopeFactory,
            ICentralPerformanceMonitor performanceMonitor,
            IOptions<ExecutionConfig> executionConfig,
            IOptions<TradingConfig> tradingConfig,
            IScopeManagerService scopeManagerService,
            IStatusTrackerService statusTrackerService,
            IBrainStatusService brainStatus)
            : base(serviceFactory, logger, scopeFactory, performanceMonitor, executionConfig, tradingConfig, scopeManagerService, statusTrackerService, brainStatus)
        {
        }




        public override async Task MonitorWatchList(BrainInstanceDTO brain, PerformanceMetrics metrics)
        {
            if (MonitoringWatchList) return;
            MonitoringWatchList = true;
            try
            {
                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
                int removedCount = await RemoveEndedMarkets(context);
                if (removedCount > 0)
                    _logger.LogInformation("BRAIN: Removed {Count} markets due to them being ended.", removedCount);

                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var token = cts.Token;

                if (_performanceMonitor.LastPerformanceSampleDate == null)
                {
                    _logger.LogInformation("Stats: No performance metric available for MarketRefreshService.");
                    MonitoringWatchList = false;
                    return;
                }

                var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();
                var marketDataService = _serviceFactory.GetMarketDataService();
                if (marketDataService == null)
                {
                    MonitoringWatchList = false;
                    return;
                }

                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();

                var watchedMarkets = await marketDataService.FetchWatchedMarketsAsync();
                int actualMarketCount = watchedMarkets.Count();

                double maxDif = watchedMarkets.Count() * .05;

                if ((Math.Abs(metrics.CurrentCount - watchedMarkets.Count()) <= maxDif) && metrics.CurrentUsage > 0)
                {
                    _recentMarketAdjustment = false;
                    _logger.LogDebug("Stats: Markets settled, reset adjustment flag.");
                }

                if (_recentMarketAdjustment
                    || (Math.Abs(metrics.CurrentCount - watchedMarkets.Count()) > maxDif)
                    || MarketsToAddAfterReset.Count() > 0)
                {
                    _logger.LogDebug("Stats: Waiting for markets to settle. Percentage={Percentage:F2}%, CurrentCount={CurrentCount}, ActualCount={ActualCount}, RefreshUsage={RefreshUsage}, QueueUsage={QueueUsage}, MarketsToAddAfterReset={MarketsToAdd}, RecentAdjustment={RecentAdjustment}",
                        metrics.CurrentUsage, metrics.CurrentCount, actualMarketCount, Math.Round(metrics.CurrentUsage, 2), _performanceMonitor.GetQueueHighCountPercentage(), MarketsToAddAfterReset.Count(), _recentMarketAdjustment);
                    MonitoringWatchList = false;
                    return;
                }

                int actualTarget = CalculateTarget(metrics, brain);
                brain.TargetWatches = actualTarget;
                await context.AddOrUpdateBrainInstance(brain);

                _recentMarketAdjustment = true;
                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();

                if (actualTarget < actualMarketCount && metrics.CurrentUsage > brain.UsageMax)
                {
                    int toRemove = actualMarketCount - actualTarget;
                    _logger.LogInformation("BRAIN: Usage is too high - attempting to remove up to {ToRemove} markets.", toRemove);

                    int removed = await RemoveLowestInterestMarkets(context, apiService, brain, toRemove, token);
                    _logger.LogInformation("BRAIN: Usage too high: {Percentage:F2}%. Removed {Count} markets to target {ActualTarget} markets",
                        metrics.CurrentUsage, removed, actualTarget);
                }
                else if (actualTarget > actualMarketCount && metrics.CurrentUsage < brain.UsageMin)
                {
                    BrainInstanceDTO? dto = await context.GetBrainInstance(_executionConfig.BrainInstance ?? "");
                    _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();
                    List<string> addedMarkets = await AddHighInterestMarkets(context, apiService, actualTarget - actualMarketCount, dto?.MinimumInterest ?? 0);
                    var dataCache = _serviceFactory.GetDataCache();
                    if (addedMarkets.Count > 0 && dataCache?.WatchedMarkets != null)
                    {
                        _logger.LogInformation("BRAIN: Usage too low: {Percentage:F2}% with {ActualMarketCount} markets. Added up to {Count} markets.",
                            metrics.CurrentUsage, actualMarketCount, actualTarget - actualMarketCount);
                        foreach (var ticker in addedMarkets)
                            _logger.LogInformation("BRAIN: Added {Market} to watch list during Monitor Watch List", ticker);
                    }
                }
                else
                {
                    BrainInstanceDTO? dto = await context.GetBrainInstance(_executionConfig.BrainInstance ?? "");
                    _logger.LogInformation("BRAIN: Usage within acceptable range: {Percentage:F2}%. Checking for uninteresting markets.", metrics.CurrentUsage);
                    int removed = await RemoveUninterestingMarkets(context, apiService, brain, dto?.MinimumInterest ?? 0);
                    _logger.LogInformation("BRAIN: Removed {Removed} uninteresting markets.", removed);
                }
                _firstWatchUpdate = false;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("MonitorWatchList was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to monitor watch list");
            }
            MonitoringWatchList = false;
        }


    }
}