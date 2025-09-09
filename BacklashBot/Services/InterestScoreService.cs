using KalshiBotData.Data.Interfaces;
using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.Management.Interfaces;
using BacklashInterfaces.Constants;

namespace BacklashBot.Services
{

    public class InterestScoreService : IInterestScoreService
    {

        private readonly ILogger<IInterestScoreService> _logger;
        private Dictionary<string, (double P90, double P95, double P99, double MaxValue, DateTime LastUpdated)> thresholdCache = new();
        private (double MaxBidSum, double MaxVolume, DateTime LastUpdated) maxValuesCache;
        private readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);

        public InterestScoreService(ILogger<IInterestScoreService> logger)
        {
            _logger = logger;
        }


        public async Task<List<(string Ticker, double Score)>> GetMarketInterestScores(
            IServiceScopeFactory scopeFactory,
            IEnumerable<string> tickers,
            double spreadTightnessWeight = 0.2,
            double spreadWidthWeight = 0.2,
            double volumeWeight = 0.33,
            double volumePercentileWeight = 0.15,
            double liquidityPercentileWeight = 0.06,
            double openInterestPercentileWeight = 0.06)
        {

            var uniqueTickers = tickers.Distinct().ToArray();
            var marketScores = new List<(string Ticker, double Score)>();
            try
            {
                using var scope = scopeFactory.CreateScope();
                var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();
                var dbContext = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(300));
                var token = cts.Token;
                _logger.LogDebug("API: Need to calculate market {0} interest scores... refetching from API", uniqueTickers);
                await apiService.FetchMarketsAsync(tickers: uniqueTickers);

                foreach (var ticker in uniqueTickers)
                {
                    token.ThrowIfCancellationRequested();
                    double score;
                    try
                    {
                        var (finalScore, _) = await CalculateMarketInterestScoreAsync(
                            dbContext,
                            ticker,
                            spreadTightnessWeight,
                            spreadWidthWeight,
                            volumeWeight,
                            volumePercentileWeight,
                            liquidityPercentileWeight,
                            openInterestPercentileWeight);
                        score = finalScore;
                        _logger.LogInformation("Scored market {0} at {1}", ticker, score);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "BRAIN: Failed to get MarketInterestScore for {Ticker}. Using default score of 0.", ticker);
                        score = 0.0;
                    }
                    marketScores.Add((ticker, score));
                }
                return marketScores;
            }
            catch (OperationCanceledException)
            {
                return marketScores;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get market interest scores");
                return marketScores;
            }
        }

        public async Task<(double score,
    (double spreadTightness, double spreadWidth, double volume, double volumePercentile, double liquidityPercentile, double openInterestPercentile, double continuity) scoreParts)>
CalculateMarketInterestScoreAsync(
    IKalshiBotContext dbContext,
    string marketTicker,
    double spreadTightnessWeight = 0.2,
    double spreadWidthWeight = 0.15,
    double volumeWeight = 0.20,
    double volumePercentileWeight = 0.1,
    double liquidityPercentileWeight = 0.09,
    double openInterestPercentileWeight = 0.065,
    double continuityWeight = 0.145)
        {
            try
            {
                // Threshold and max values calculation
                async Task CalculateThresholdsAndMaxValuesAsync()
                {
                    var markets = await dbContext.GetMarketLiquidityStates();

                    var volumeSorted = markets.Select(m => (double)m.volume_24h).OrderBy(v => v).ToList();
                    var liquiditySorted = markets.Select(m => Math.Abs((double)m.liquidity)).OrderBy(v => v).ToList();
                    var openInterestSorted = markets.Select(m => (double)m.open_interest).OrderBy(v => v).ToList();

                    thresholdCache["volume_24h"] = (
                        volumeSorted[(int)(volumeSorted.Count() * 0.90)],
                        volumeSorted[(int)(volumeSorted.Count() * 0.95)],
                        volumeSorted[(int)(volumeSorted.Count() * 0.99)],
                        volumeSorted[^1],
                        DateTime.UtcNow
                    );

                    thresholdCache["liquidity"] = (
                        liquiditySorted[(int)(volumeSorted.Count() * 0.90)],
                        liquiditySorted[(int)(volumeSorted.Count() * 0.95)],
                        liquiditySorted[(int)(volumeSorted.Count() * 0.99)],
                        liquiditySorted[^1],
                        DateTime.UtcNow
                    );

                    thresholdCache["open_interest"] = (
                        openInterestSorted[(int)(volumeSorted.Count() * 0.90)],
                        openInterestSorted[(int)(volumeSorted.Count() * 0.95)],
                        openInterestSorted[(int)(volumeSorted.Count() * 0.99)],
                        openInterestSorted[^1],
                        DateTime.UtcNow
                    );

                    maxValuesCache = (
                        markets.Max(m => (double?)(m.yes_bid + m.no_bid)) ?? 0,
                        markets.Max(m => (double?)(m.volume_24h / 24.0)) ?? 0,
                        DateTime.UtcNow
                    );
                }

                // Check if thresholds and max values need recalculation
                bool needsRecalculation = !thresholdCache.Any() ||
                    thresholdCache.Values.Any(t => DateTime.UtcNow - t.LastUpdated > CacheDuration) ||
                    DateTime.UtcNow - maxValuesCache.LastUpdated > CacheDuration;

                if (needsRecalculation)
                {
                    await CalculateThresholdsAndMaxValuesAsync();
                }

                // Get market data
                var market = await dbContext.GetMarketByTicker(marketTicker);

                // Get snapshot count for continuity score
                long snapshotCount = await dbContext.GetSnapshotCount(marketTicker);

                if (market == null)
                {
                    _logger.LogWarning("No market data found for {MarketTicker}.", marketTicker);
                    return (0.0, (0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0));
                }

                // Calculate score
                if (KalshiConstants.MarketIsEnded(market.status) || market.yes_bid == 0 || market.no_bid == 0)
                {
                    _logger.LogDebug("MarketInterestScore 0.0 for {MarketTicker} due to market being ended.", marketTicker);
                    return (0.0, (0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0));
                }

                double CalculatePercentileScore(double value, double p90, double p95, double p99, double maxValue)
                {
                    if (value < p90) return 5 * (value / (p90 != 0 ? p90 : 1)) / 10.0;
                    if (value < p95) return (5 + 2 * ((value - p90) / (p95 - p90 != 0 ? p95 - p90 : 1))) / 10.0;
                    if (value < p99) return (7 + 2 * ((value - p95) / (p99 - p95 != 0 ? p99 - p95 : 1))) / 10.0;
                    return (9 + 1 * ((value - p99) / (maxValue - p99 != 0 ? maxValue - p99 : 1))) / 10.0;
                }

                var volumeThresholds = thresholdCache["volume_24h"];
                var liquidityThresholds = thresholdCache["liquidity"];
                var openInterestThresholds = thresholdCache["open_interest"];

                double spreadTightnessScore = spreadTightnessWeight * (1 / (1 + (market.yes_ask - market.yes_bid + market.no_ask - market.no_bid) / 2.0));
                double spreadWidthScore = spreadWidthWeight * (market.yes_ask - market.yes_bid < 3 ? (4 - (market.yes_ask - market.yes_bid)) / 4.0 : 0.0);
                double volumeScore = volumeWeight * Math.Log(1 + market.volume_24h / 24.0) / Math.Log(1 + maxValuesCache.MaxVolume);
                double volumePercentileScore = volumePercentileWeight * CalculatePercentileScore(market.volume_24h,
                    volumeThresholds.P90, volumeThresholds.P95, volumeThresholds.P99, volumeThresholds.MaxValue);
                double liquidityPercentileScore = liquidityPercentileWeight * CalculatePercentileScore(Math.Abs(market.liquidity),
                    liquidityThresholds.P90, liquidityThresholds.P95, liquidityThresholds.P99, liquidityThresholds.MaxValue);
                double openInterestPercentileScore = openInterestPercentileWeight * CalculatePercentileScore(market.open_interest,
                    openInterestThresholds.P90, openInterestThresholds.P95, openInterestThresholds.P99, openInterestThresholds.MaxValue);
                double continuityScore = continuityWeight * Math.Min(snapshotCount / 4320.0, 1.0); // 3 days = 4320 snapshots

                double rawScore = 10.0 * (
                    spreadTightnessScore +
                    spreadWidthScore +
                    volumeScore +
                    volumePercentileScore +
                    liquidityPercentileScore +
                    openInterestPercentileScore +
                    continuityScore
                );

                double closeTimeMultiplier = market.close_time != null && (market.close_time - DateTime.UtcNow).TotalHours <= 48 ? 1.25 :
                                            market.close_time != null && (market.close_time - DateTime.UtcNow).TotalDays > 90 ? 0.75 : 1.0;
                double finalScore = Math.Round(rawScore * closeTimeMultiplier, 2);

                var scoreParts = (
                    spreadTightness: spreadTightnessScore * 10.0,
                    spreadWidth: spreadWidthScore * 10.0,
                    volume: volumeScore * 10.0,
                    volumePercentile: volumePercentileScore * 10.0,
                    liquidityPercentile: liquidityPercentileScore * 10.0,
                    openInterestPercentile: openInterestPercentileScore * 10.0,
                    continuity: continuityScore * 10.0
                );

                _logger.LogDebug("Calculated MarketInterestScore {Score} for {MarketTicker}.", finalScore, marketTicker);
                return (finalScore, scoreParts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate MarketInterestScore for {MarketTicker}.", marketTicker);
                throw;
            }
        }

    }
}
