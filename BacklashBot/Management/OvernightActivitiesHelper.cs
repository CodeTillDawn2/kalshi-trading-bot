// OvernightActivitiesHelper.cs
using KalshiBotData.Data.Interfaces;
using Microsoft.Extensions.Options;
using BacklashDTOs.Configuration;
using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.Management.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashDTOs.Data;
using BacklashInterfaces.Constants;

namespace BacklashBot.Management
{
    public class OvernightActivitiesHelper : IOvernightActivitiesHelper
    {
        private readonly ILogger<IOvernightActivitiesHelper> _logger;
        private readonly IMarketAnalysisHelper _analysisHelper;
        private readonly ExecutionConfig _executionConfig;
        private readonly ISqlDataService _sqlDataService;

        public OvernightActivitiesHelper(ILogger<IOvernightActivitiesHelper> logger, IInterestScoreService interestScoreHelper,
            IMarketAnalysisHelper analysisHelper, IOptions<ExecutionConfig> executionConfig, ISqlDataService sqlDataService)
        {
            _logger = logger;
            _analysisHelper = analysisHelper;
            _executionConfig = executionConfig.Value;
            _sqlDataService = sqlDataService;
        }

        public async Task RunOvernightTasks(IServiceScopeFactory scopeFactory, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("OVERNIGHT-Running overnight tasks.");

            cancellationToken.ThrowIfCancellationRequested();

            DateTime cutoff = DateTime.UtcNow;

            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
            var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();
            await RefreshMarketsByStatus(scopeFactory, KalshiConstants.Status_Open, cancellationToken);
            _logger.LogInformation("OVERNIGHT-Refreshed all open markets.");
            await RefreshLikelyClosedMarkets(scopeFactory, cutoff, false, cancellationToken);
            _logger.LogInformation("OVERNIGHT-Refreshed all likely closed markets.");
            await CalculateOvernightMarketInterestScores(scopeFactory, cancellationToken);
            _logger.LogInformation("OVERNIGHT-Calculated missing market interest scores.");
            await _sqlDataService.ImportSnapshotsFromFilesAsync(cancellationToken);
            await _sqlDataService.ImportSnapshotsFromFilesAsync(cancellationToken); //Do twice to make sure we have as few outstanding snapshots as possible when we generate groups
            _logger.LogInformation("OVERNIGHT-Imported snapshots from files.");
            await RemoveOldWatches(scopeFactory);
            _logger.LogInformation("OVERNIGHT-Removed old market watches.");
            await _analysisHelper.GenerateSnapshotGroups();
            _logger.LogInformation("OVERNIGHT-Generated snapshot groups.");
            await DeleteUnrecordedMarkets(scopeFactory, cancellationToken);
            _logger.LogInformation("OVERNIGHT-Deleted ended markets which were never recorded.");
        }

        public async Task DeleteUnrecordedMarkets(IServiceScopeFactory scopeFactory, CancellationToken cancellationToken)
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
            HashSet<string> inactiveMarkets = await context.GetInactiveMarketsWithNoSnapshots();

            foreach (string marketTicker in inactiveMarkets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await context.DeleteMarket(marketTicker);
            }
        }

        public async Task DeleteProcessedSnapshots(IServiceScopeFactory scopeFactory, CancellationToken cancellationToken)
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
            HashSet<string> inactiveMarkets = await context.GetProcessedMarkets();

            string hardDataStorageLocation = _executionConfig.HardDataStorageLocation;

            foreach (string marketTicker in inactiveMarkets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string filepath = Path.Combine(hardDataStorageLocation, "Candlesticks", marketTicker);
                if (Directory.Exists(filepath))
                    Directory.Delete(filepath);
            }
        }

        private async Task RemoveOldWatches(IServiceScopeFactory scopeFactory)
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
            await context.RemoveClosedWatches();
        }

        private async Task RefreshLikelyClosedMarkets(IServiceScopeFactory scopeFactory, DateTime cutoff, bool isRetry, CancellationToken cancellationToken)
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
            var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();

            List<MarketDTO> MarketsWhichAreLikelyClosed = await context.GetMarkets(
                includedStatuses: null,
                excludedStatuses: new HashSet<string> { KalshiConstants.Status_Finalized,
                                                        KalshiConstants.Status_Inactive,
                                                        KalshiConstants.Status_Initialized,
                                                        KalshiConstants.Status_Bad,
                                                        KalshiConstants.Status_Closed,
                                                        KalshiConstants.Status_Settled },
                maxAPILastFetchTime: cutoff
            );

            const int batchSize = 20;
            int errors = 0;
            for (int i = 0; i < MarketsWhichAreLikelyClosed.Count; i += batchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var batch = MarketsWhichAreLikelyClosed
                    .Skip(i)
                    .Take(batchSize)
                    .Select(m => m.market_ticker)
                    .ToArray();
                (int processed, int error) = await apiService.FetchMarketsAsync(tickers: batch);
                await Task.Delay(5000, cancellationToken);
                errors += error;
            }
            if (errors > 0)
            {
                if (isRetry)
                {
                    _logger.LogWarning("Logged {0} errors during overnight market retry.", errors);
                }
                else
                {
                    _logger.LogWarning("Logged {0} errors during overnight market refresh. Will attempt again later.", errors);
                    await Task.Delay(1800000, cancellationToken);
                    await RefreshLikelyClosedMarkets(scopeFactory, cutoff, true, cancellationToken);
                }
            }
        }

        private async Task RefreshMarketsByStatus(IServiceScopeFactory scopeFactory, string Status, CancellationToken cancellationToken)
        {
            using var scope = scopeFactory.CreateScope();
            var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();
            await apiService.FetchMarketsAsync(status: Status);
        }

        private async Task CalculateOvernightMarketInterestScores(IServiceScopeFactory scopeFactory, CancellationToken cancellationToken)
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
            var interestScoreHelper = scope.ServiceProvider.GetRequiredService<IInterestScoreService>();
            var overnightInterestScoresData = await context.GetMarkets(includedStatuses: new HashSet<string> { KalshiConstants.Status_Active },
                maxInterestScoreDate: DateTime.Now.AddHours(-12));

            foreach (MarketDTO market in overnightInterestScoresData)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    double score = (await interestScoreHelper.CalculateMarketInterestScoreAsync(context, market.market_ticker)).score;

                    MarketWatchDTO? marketWatch = await context.GetMarketWatch(market.market_ticker);

                    if (marketWatch == null)
                    {
                        marketWatch = new MarketWatchDTO() { market_ticker = market.market_ticker, InterestScore = score, InterestScoreDate = DateTime.Now };
                    }
                    else
                    {
                        marketWatch.InterestScore = score;
                        marketWatch.InterestScoreDate = DateTime.Now;
                    }
                    await context.AddOrUpdateMarketWatch(marketWatch);
                }
                catch (Exception)
                {
                    _logger.LogWarning("Failed to calculate interest score for market {MarketTicker}.", market.market_ticker);
                }
            }
        }
    }
}
