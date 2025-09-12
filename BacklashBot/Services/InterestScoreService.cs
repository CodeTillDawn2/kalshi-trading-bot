using KalshiBotData.Data.Interfaces;
using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.Management.Interfaces;
using BacklashInterfaces.Constants;

namespace BacklashBot.Services
{
    /// <summary>
    /// Service responsible for calculating interest scores for Kalshi markets based on various trading metrics.
    /// This service evaluates market attractiveness by analyzing spread characteristics, trading volume,
    /// liquidity patterns, and market continuity to provide quantitative scores for market selection.
    /// </summary>
    public class InterestScoreService : IInterestScoreService
    {
        private readonly ILogger<IInterestScoreService> _logger;
        private Dictionary<string, (double P90, double P95, double P99, double MaxValue, DateTime LastUpdated)> percentileThresholdsCache = new();
        private (double MaxBidSum, double MaxVolume, DateTime LastUpdated) maxMarketValuesCache;
        private readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);

        /// <summary>
        /// Initializes a new instance of the <see cref="InterestScoreService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for recording service operations and errors.</param>
        public InterestScoreService(ILogger<IInterestScoreService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Calculates interest scores for a collection of market tickers using configurable weighting factors.
        /// This method fetches current market data from the API and computes scores for each ticker,
        /// handling errors gracefully by assigning default scores to failed calculations.
        /// </summary>
        /// <param name="scopeFactory">Factory for creating service scopes to access dependencies.</param>
        /// <param name="tickers">Collection of market ticker symbols to score.</param>
        /// <param name="spreadTightnessWeight">Weight factor for spread tightness component (0.0-1.0).</param>
        /// <param name="spreadWidthWeight">Weight factor for spread width component (0.0-1.0).</param>
        /// <param name="volumeWeight">Weight factor for volume component (0.0-1.0).</param>
        /// <param name="volumePercentileWeight">Weight factor for volume percentile component (0.0-1.0).</param>
        /// <param name="liquidityPercentileWeight">Weight factor for liquidity percentile component (0.0-1.0).</param>
        /// <param name="openInterestPercentileWeight">Weight factor for open interest percentile component (0.0-1.0).</param>
        /// <returns>A list of tuples containing ticker symbols and their calculated interest scores.</returns>
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
                        _logger.LogInformation("Calculated interest score for market {0}: {1}", ticker, score);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to calculate interest score for market {Ticker}. Using default score of 0.", ticker);
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

        /// <summary>
        /// Calculates a comprehensive interest score for a specific market ticker.
        /// The score is computed using multiple weighted factors including spread characteristics,
        /// trading volume, liquidity, open interest, and market continuity. Thresholds and
        /// maximum values are cached for performance and recalculated periodically.
        /// </summary>
        /// <param name="dbContext">Database context for accessing market data.</param>
        /// <param name="marketTicker">The market ticker symbol to score.</param>
        /// <param name="spreadTightnessWeight">Weight for spread tightness factor.</param>
        /// <param name="spreadWidthWeight">Weight for spread width factor.</param>
        /// <param name="volumeWeight">Weight for volume factor.</param>
        /// <param name="volumePercentileWeight">Weight for volume percentile factor.</param>
        /// <param name="liquidityPercentileWeight">Weight for liquidity percentile factor.</param>
        /// <param name="openInterestPercentileWeight">Weight for open interest percentile factor.</param>
        /// <param name="continuityWeight">Weight for market continuity factor.</param>
        /// <returns>A tuple containing the final score and detailed score components.</returns>
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
                async Task ComputePercentileThresholdsAndMaxValuesAsync()
                {
                    var markets = await dbContext.GetMarketLiquidityStates();

                    var volumeSorted = markets.Select(m => (double)m.volume_24h).OrderBy(v => v).ToList();
                    var liquiditySorted = markets.Select(m => Math.Abs((double)m.liquidity)).OrderBy(v => v).ToList();
                    var openInterestSorted = markets.Select(m => (double)m.open_interest).OrderBy(v => v).ToList();

                    percentileThresholdsCache["volume_24h"] = (
                        volumeSorted[(int)(volumeSorted.Count() * 0.90)],
                        volumeSorted[(int)(volumeSorted.Count() * 0.95)],
                        volumeSorted[(int)(volumeSorted.Count() * 0.99)],
                        volumeSorted[^1],
                        DateTime.UtcNow
                    );

                    percentileThresholdsCache["liquidity"] = (
                        liquiditySorted[(int)(liquiditySorted.Count() * 0.90)],
                        liquiditySorted[(int)(liquiditySorted.Count() * 0.95)],
                        liquiditySorted[(int)(liquiditySorted.Count() * 0.99)],
                        liquiditySorted[^1],
                        DateTime.UtcNow
                    );

                    percentileThresholdsCache["open_interest"] = (
                        openInterestSorted[(int)(openInterestSorted.Count() * 0.90)],
                        openInterestSorted[(int)(openInterestSorted.Count() * 0.95)],
                        openInterestSorted[(int)(openInterestSorted.Count() * 0.99)],
                        openInterestSorted[^1],
                        DateTime.UtcNow
                    );

                    maxMarketValuesCache = (
                        markets.Max(m => (double?)(m.yes_bid + m.no_bid)) ?? 0,
                        markets.Max(m => (double?)(m.volume_24h / 24.0)) ?? 0,
                        DateTime.UtcNow
                    );
                }

                // Check if thresholds and max values need recalculation
                bool needsRecalculation = !percentileThresholdsCache.Any() ||
                    percentileThresholdsCache.Values.Any(t => DateTime.UtcNow - t.LastUpdated > CacheDuration) ||
                    DateTime.UtcNow - maxMarketValuesCache.LastUpdated > CacheDuration;

                if (needsRecalculation)
                {
                    await ComputePercentileThresholdsAndMaxValuesAsync();
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
                if (KalshiConstants.IsMarketStatusEnded(market.status) || market.yes_bid == 0 || market.no_bid == 0)
                {
                    _logger.LogDebug("MarketInterestScore 0.0 for {MarketTicker} due to market being ended.", marketTicker);
                    return (0.0, (0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0));
                }

                /// <summary>
                /// Computes a percentile-based score for a given value using predefined thresholds.
                /// The scoring system provides higher scores for values in the upper percentiles,
                /// with a maximum score of 1.0 for values at or above the 99th percentile.
                /// </summary>
                /// <param name="value">The value to score.</param>
                /// <param name="p90">The 90th percentile threshold value.</param>
                /// <param name="p95">The 95th percentile threshold value.</param>
                /// <param name="p99">The 99th percentile threshold value.</param>
                /// <param name="maxValue">The maximum value in the dataset.</param>
                /// <returns>A normalized score between 0.0 and 1.0 based on percentile ranking.</returns>
                double ComputePercentileScore(double value, double p90, double p95, double p99, double maxValue)
                {
                    if (value < p90) return 5 * (value / (p90 != 0 ? p90 : 1)) / 10.0;
                    if (value < p95) return (5 + 2 * ((value - p90) / (p95 - p90 != 0 ? p95 - p90 : 1))) / 10.0;
                    if (value < p99) return (7 + 2 * ((value - p95) / (p99 - p95 != 0 ? p99 - p95 : 1))) / 10.0;
                    return (9 + 1 * ((value - p99) / (maxValue - p99 != 0 ? maxValue - p99 : 1))) / 10.0;
                }

                var volumeThresholds = percentileThresholdsCache["volume_24h"];
                var liquidityThresholds = percentileThresholdsCache["liquidity"];
                var openInterestThresholds = percentileThresholdsCache["open_interest"];

                double spreadTightnessScore = spreadTightnessWeight * (1 / (1 + (market.yes_ask - market.yes_bid + market.no_ask - market.no_bid) / 2.0));
                double spreadWidthScore = spreadWidthWeight * (market.yes_ask - market.yes_bid < 3 ? (4 - (market.yes_ask - market.yes_bid)) / 4.0 : 0.0);
                double volumeScore = volumeWeight * Math.Log(1 + market.volume_24h / 24.0) / Math.Log(1 + maxMarketValuesCache.MaxVolume);
                double volumePercentileScore = volumePercentileWeight * ComputePercentileScore(market.volume_24h,
                    volumeThresholds.P90, volumeThresholds.P95, volumeThresholds.P99, volumeThresholds.MaxValue);
                double liquidityPercentileScore = liquidityPercentileWeight * ComputePercentileScore(Math.Abs(market.liquidity),
                    liquidityThresholds.P90, liquidityThresholds.P95, liquidityThresholds.P99, liquidityThresholds.MaxValue);
                double openInterestPercentileScore = openInterestPercentileWeight * ComputePercentileScore(market.open_interest,
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

                _logger.LogInformation("Calculated interest score {Score} for market {MarketTicker}.", finalScore, marketTicker);
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
