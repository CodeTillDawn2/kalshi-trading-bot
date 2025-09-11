using KalshiBotData.Data.Interfaces;
using Microsoft.Extensions.Options;
using BacklashBot.Configuration;
using BacklashBot.Management.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashDTOs.Data;
using BacklashInterfaces.Constants;
using TradingStrategies.Classification.Interfaces;

namespace BacklashBot.Management
{
    public class MarketAnalysisHelper : IMarketAnalysisHelper
    {
        private readonly ExecutionConfig _executionConfig;
        private readonly ISnapshotPeriodHelper _snapshotPeriodHelper;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITradingSnapshotService _snapshotService;

        public MarketAnalysisHelper(IServiceScopeFactory scopeFactory, ISnapshotPeriodHelper snapshotPeriodHelper, ITradingSnapshotService snapshotService, IOptions<ExecutionConfig> executionConfig)
        {
            _snapshotService = snapshotService;
            _scopeFactory = scopeFactory;
            _snapshotPeriodHelper = snapshotPeriodHelper;
            _executionConfig = executionConfig.Value;
        }

        public async Task GenerateSnapshotGroups()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

            var tickersAnalyzed = await context.GetSnapshotGroups_cached();
            var tickerHashSet = tickersAnalyzed
                .Select(x => x.MarketTicker)
                .Distinct()
                .ToHashSet();

            HashSet<string> tickersWithSnapshots = await context.GetMarketsWithSnapshots();

            HashSet<string> tickersToAnalyze = (await context.GetMarkets(
                excludedStatuses: new HashSet<string> { KalshiConstants.Status_Active },
                excludedMarkets: tickerHashSet,
                includedMarkets: tickersWithSnapshots))
                .Select(x => x.market_ticker).ToHashSet();

            foreach (string marketTicker in tickersToAnalyze)
            {
                List<SnapshotDTO> rawSnapshots;

                try
                {
                    rawSnapshots = await context.GetSnapshots(marketTicker: marketTicker);
                    rawSnapshots = rawSnapshots.OrderBy(x => x.SnapshotDate).ToList();
                }
                catch (Exception)
                {
                    try
                    {
                        Thread.Sleep(5000);
                        rawSnapshots = await context.GetSnapshots(marketTicker: marketTicker);
                        rawSnapshots = rawSnapshots.OrderBy(x => x.SnapshotDate).ToList();
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }

                int expectedSchema = rawSnapshots[0].JSONSchemaVersion;
                foreach (var snapshot in rawSnapshots)
                {
                    if (snapshot.JSONSchemaVersion != expectedSchema)
                    {
                        // Schema mismatch - could log or handle
                        continue;
                    }
                }



                string snapshotDirectory = _executionConfig.HardDataStorageLocation;
                snapshotDirectory = Path.Combine(snapshotDirectory, "Preprocessed", "SnapshotGroups");

                // Pass SnapshotDTOs directly to SplitIntoValidGroups
                var validPeriods = _snapshotPeriodHelper.SplitIntoValidGroups(rawSnapshots, snapshotDirectory);

                await context.AddOrUpdateSnapshotGroups(validPeriods);
            }
        }
    }
}
