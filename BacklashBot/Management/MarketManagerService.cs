using KalshiBotData.Data.Interfaces;
using Microsoft.Extensions.Options;
using BacklashBot.Configuration;
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
    public class MarketManagerService : BaseMarketManagerService
    {
        private IMarketManagerService _managedService;
        private IMarketManagerService _unmanagedService;

        public MarketManagerService(IServiceFactory serviceFactory,
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
            _managedService = new ManagedMarketManagerService(serviceFactory, logger, scopeFactory, performanceMonitor, executionConfig, tradingConfig, scopeManagerService, statusTrackerService, brainStatus);
            _unmanagedService = new UnmanagedMarketManagerService(serviceFactory, logger, scopeFactory, performanceMonitor, executionConfig, tradingConfig, scopeManagerService, statusTrackerService, brainStatus);
        }

        public void ClearMarketsToReset()
        {
            lock (_resetLock)
            {
                MarketsToAddAfterReset.Clear();
                MarketsToReset.Clear();
            }
        }


        public async Task HandleMarketResets()
        {
            try
            {
                await RefreshMarkets();
                await ResetMarkets();
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("HandleMarketResets was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to handle market resets: {Message}", ex.Message);
            }
        }

        public void TriggerMarketReset(string marketTicker)
        {
            lock (_resetLock)
            {
                MarketsToReset.Add(marketTicker);
            }
        }


        public override async Task MonitorWatchList(BrainInstanceDTO brain, PerformanceMetrics metrics)
        {
            if (brain.ManagedWatchList)
            {
                await _managedService.MonitorWatchList(brain, metrics);
            }
            else
            {
                await _unmanagedService.MonitorWatchList(brain, metrics);
            }
        }







    }
}
