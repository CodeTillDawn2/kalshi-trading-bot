using BacklashBotData.Data.Interfaces;
using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.Management.Interfaces;
using BacklashInterfaces.Constants;
using BacklashDTOs.Configuration;
using Microsoft.Extensions.Options;

namespace BacklashBot.Services
{
    /// <summary>
    /// Service responsible for calculating interest scores for Kalshi markets based on various trading metrics.
    /// This service evaluates market attractiveness by analyzing spread characteristics, trading volume,
    /// liquidity patterns, and market continuity to provide quantitative scores for market selection.
    ///
    /// Features:
    /// - Configurable cache duration for performance optimization
    /// - Comprehensive performance metrics collection and monitoring
    /// - Input validation for market tickers and weight parameters
    /// - Thread-safe operations for concurrent access
    /// - Automatic metrics logging with configurable intervals
    /// </summary>
    public class InterestScoreService : IInterestScoreService
    {
        private readonly ILogger<IInterestScoreService> _logger;
        private readonly InterestScoreConfig _config;
        private Dictionary<string, (double P90, double P95, double P99, double MaxValue, DateTime LastUpdated)> percentileThresholdsCache = new();
        private (double MaxBidSum, double MaxVolume, DateTime LastUpdated) maxMarketValuesCache;

        // Performance metrics
        private int _cacheHits = 0;
        private int _cacheMisses = 0;
        private List<TimeSpan> _scoringOperationTimes = new();
        private DateTime _lastMetricsLog = DateTime.UtcNow;

        /// <summary>
        /// Records a cache hit for performance metrics.
        /// Increments the cache hit counter in a thread-safe manner when performance metrics are enabled.
        /// Cache hits indicate successful reuse of cached percentile thresholds and market values.
        /// </summary>
        private void RecordCacheHit()
        {
            if (_config.EnablePerformanceMetrics)
            {
                Interlocked.Increment(ref _cacheHits);
                LogMetricsIfNeeded();
            }
        }

        /// <summary>
        /// Records a cache miss for performance metrics.
        /// Increments the cache miss counter in a thread-safe manner when performance metrics are enabled.
        /// Cache misses indicate that percentile thresholds and market values needed to be recalculated.
        /// </summary>
        private void RecordCacheMiss()
        {
            if (_config.EnablePerformanceMetrics)
            {
                Interlocked.Increment(ref _cacheMisses);
                LogMetricsIfNeeded();
            }
        }

        /// <summary>
        /// Records the duration of a scoring operation for performance metrics.
        /// Stores the operation timing in a thread-safe collection for performance analysis.
        /// Maintains a rolling history of operation times with automatic cleanup of old entries.
        /// </summary>
        /// <param name="duration">The time taken for the scoring operation.</param>
        private void RecordScoringOperationTime(TimeSpan duration)
        {
            if (_config.EnablePerformanceMetrics)
            {
                lock (_scoringOperationTimes)
                {
                    _scoringOperationTimes.Add(duration);
                    if (_scoringOperationTimes.Count > _config.MaxPerformanceMetricsHistory)
                    {
                        _scoringOperationTimes.RemoveAt(0);
                    }
                }
                LogMetricsIfNeeded();
            }
        }

        /// <summary>
        /// Validates market ticker symbols for null, empty, or invalid values.
        /// Ensures all ticker symbols are non-null, non-empty, and contain meaningful content.
        /// </summary>
        /// <param name="tickers">Collection of ticker symbols to validate.</param>
        /// <exception cref="ArgumentException">Thrown when tickers contain invalid values.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the tickers collection is null.</exception>
        private void ValidateTickers(IEnumerable<string> tickers)
        {
            if (tickers == null)
            {
                throw new ArgumentNullException(nameof(tickers), "Tickers collection cannot be null.");
            }

            var invalidTickers = tickers.Where(t => string.IsNullOrWhiteSpace(t)).ToList();
            if (invalidTickers.Any())
            {
                throw new ArgumentException($"Invalid ticker symbols found: {string.Join(", ", invalidTickers)}", nameof(tickers));
            }
        }

        /// <summary>
        /// Validates weight parameters to ensure they are within valid ranges (0.0 to 1.0).
        /// Weight parameters control the relative importance of different scoring factors and must
        /// be normalized to prevent invalid scoring calculations.
        /// </summary>
        /// <param name="weights">Dictionary of weight parameter names and values to validate.</param>
        /// <exception cref="ArgumentException">Thrown when any weight parameter is outside the 0.0-1.0 range.</exception>
        private void ValidateWeights(Dictionary<string, double> weights)
        {
            foreach (var (name, value) in weights)
            {
                if (value < 0.0 || value > 1.0)
                {
                    throw new ArgumentException($"Weight parameter '{name}' must be between 0.0 and 1.0, but was {value}.", name);
                }
            }
        }

        /// <summary>
        /// Logs performance metrics if enough time has passed since the last log.
        /// Periodically outputs cache hit rates, average operation times, and request counts
        /// to provide insights into system performance and cache efficiency.
        /// Logging occurs every 5 minutes when performance metrics are enabled.
        /// </summary>
        private void LogMetricsIfNeeded()
        {
            if (_config.EnablePerformanceMetrics && DateTime.UtcNow - _lastMetricsLog > TimeSpan.FromMinutes(5))
            {
                var hits = _cacheHits;
                var misses = _cacheMisses;
                var totalRequests = hits + misses;
                var hitRate = totalRequests > 0 ? (double)hits / totalRequests * 100 : 0;

                double avgOperationTime = 0;
                lock (_scoringOperationTimes)
                {
                    if (_scoringOperationTimes.Count > 0)
                    {
                        avgOperationTime = _scoringOperationTimes.Average(t => t.TotalMilliseconds);
                    }
                }

                _logger.LogInformation("InterestScoreService Performance Metrics - Cache Hit Rate: {HitRate:F2}%, Avg Operation Time: {AvgTime:F2}ms, Total Requests: {TotalRequests}",
                    hitRate, avgOperationTime, totalRequests);

                _lastMetricsLog = DateTime.UtcNow;
            }
        }
        /// <summary>
        /// Computes percentile thresholds and max values for market metrics.
        /// This method fetches all market liquidity states and calculates percentiles for volume, liquidity, and open interest,
        /// as well as maximum values for bid sums and volume rates. Results are cached for performance.
        /// </summary>
        /// <param name="dbContext">Database context for accessing market data.</param>
        private async Task ComputePercentileThresholdsAndMaxValuesAsync(IBacklashBotContext dbContext)
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

        /// <summary>
        /// Ensures that percentile thresholds and max values are up to date.
        /// Checks if recalculation is needed based on cache duration and updates cache if necessary.
        /// </summary>
        /// <param name="dbContext">Database context for accessing market data.</param>
        private async Task EnsurePercentileThresholdsAndMaxValuesAsync(IBacklashBotContext dbContext)
        {
            bool needsRecalculation = !percentileThresholdsCache.Any() ||
                percentileThresholdsCache.Values.Any(t => DateTime.UtcNow - t.LastUpdated > TimeSpan.FromHours(_config.CacheDurationHours)) ||
                DateTime.UtcNow - maxMarketValuesCache.LastUpdated > TimeSpan.FromHours(_config.CacheDurationHours);
            if (needsRecalculation)
            {
                RecordCacheMiss();
                await ComputePercentileThresholdsAndMaxValuesAsync(dbContext);
            }
            else
            {
                RecordCacheHit();
            }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="InterestScoreService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for recording service operations and errors.</param>
        /// <param name="config">The configuration options for the interest score service, including cache duration and performance metrics settings.</param>
        public InterestScoreService(ILogger<IInterestScoreService> logger, IOptions<InterestScoreConfig> config)
        {
            _logger = logger;
            _config = config.Value;
        }

        /// <summary>
        /// Calculates interest scores for a collection of market tickers using configurable weighting factors.
        /// This method fetches current market data from the API and computes scores for each ticker,
        /// handling errors gracefully by assigning default scores to failed calculations.
        ///
        /// Features:
        /// - Input validation for tickers and weight parameters
        /// - Performance metrics collection for operation timing
        /// - Thread-safe processing with cancellation support
        /// - Comprehensive error handling with detailed logging
        /// </summary>
        /// <param name="scopeFactory">Factory for creating service scopes to access dependencies.</param>
        /// <param name="tickers">Collection of market ticker symbols to score. Must not be null or contain empty/whitespace values.</param>
        /// <param name="spreadTightnessWeight">Weight factor for spread tightness component (0.0-1.0). Must be within valid range.</param>
        /// <param name="spreadWidthWeight">Weight factor for spread width component (0.0-1.0). Must be within valid range.</param>
        /// <param name="volumeWeight">Weight factor for volume component (0.0-1.0). Must be within valid range.</param>
        /// <param name="volumePercentileWeight">Weight factor for volume percentile component (0.0-1.0). Must be within valid range.</param>
        /// <param name="liquidityPercentileWeight">Weight factor for liquidity percentile component (0.0-1.0). Must be within valid range.</param>
        /// <param name="openInterestPercentileWeight">Weight factor for open interest percentile component (0.0-1.0). Must be within valid range.</param>
        /// <returns>A list of tuples containing ticker symbols and their calculated interest scores.</returns>
        /// <exception cref="ArgumentException">Thrown when tickers are invalid or weights are outside the 0.0-1.0 range.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the tickers collection is null.</exception>
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
            // Input validation
            ValidateTickers(tickers);
            ValidateWeights(new Dictionary<string, double>
            {
                ["spreadTightnessWeight"] = spreadTightnessWeight,
                ["spreadWidthWeight"] = spreadWidthWeight,
                ["volumeWeight"] = volumeWeight,
                ["volumePercentileWeight"] = volumePercentileWeight,
                ["liquidityPercentileWeight"] = liquidityPercentileWeight,
                ["openInterestPercentileWeight"] = openInterestPercentileWeight
            });

            var uniqueTickers = tickers.Distinct().ToArray();
            var marketScores = new List<(string Ticker, double Score)>();
            try
            {
                using var scope = scopeFactory.CreateScope();
                var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();
                var dbContext = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(300));
                var token = cts.Token;
                _logger.LogDebug("API: Need to calculate market {0} interest scores... refetching from API", uniqueTickers);
                await apiService.FetchMarketsAsync(tickers: uniqueTickers);
                await EnsurePercentileThresholdsAndMaxValuesAsync(dbContext);
                // Bulk fetch markets and snapshot counts
                var markets = await Task.WhenAll(uniqueTickers.Select(t => dbContext.GetMarketByTicker(t)));
                var marketsDict = uniqueTickers.Zip(markets, (t, m) => new { t, m }).ToDictionary(x => x.t, x => x.m);
                var snapshotCounts = await Task.WhenAll(uniqueTickers.Select(t => dbContext.GetSnapshotCount(t)));
                var snapshotDict = uniqueTickers.Zip(snapshotCounts, (t, c) => new { t, c }).ToDictionary(x => x.t, x => x.c);

                foreach (var ticker in uniqueTickers)
                {
                    token.ThrowIfCancellationRequested();
                    double score;
                    try
                    {
                        var market = marketsDict[ticker];
                        long snapshotCount = snapshotDict[ticker];
                        var (finalScore, _) = await CalculateMarketInterestScoreAsync(
                            market,
                            snapshotCount,
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
        /// Calculates a comprehensive interest score for a specific market.
        /// The score is computed using multiple weighted factors including spread characteristics,
        /// trading volume, liquidity, open interest, and market continuity. Thresholds and
        /// maximum values are cached for performance and recalculated periodically based on
        /// configurable cache duration settings.
        ///
        /// Features:
        /// - Input validation for weight parameters
        /// - Configurable cache duration for performance optimization
        /// - Performance metrics collection for operation timing
        /// - Cache hit/miss tracking for monitoring efficiency
        /// - Comprehensive error handling with detailed logging
        /// </summary>
        /// <param name="market">The market data object to score. Must not be null.</param>
        /// <param name="snapshotCount">The number of snapshots for the market.</param>
        /// <param name="spreadTightnessWeight">Weight for spread tightness factor (0.0-1.0). Must be within valid range.</param>
        /// <param name="spreadWidthWeight">Weight for spread width factor (0.0-1.0). Must be within valid range.</param>
        /// <param name="volumeWeight">Weight for volume factor (0.0-1.0). Must be within valid range.</param>
        /// <param name="volumePercentileWeight">Weight for volume percentile factor (0.0-1.0). Must be within valid range.</param>
        /// <param name="liquidityPercentileWeight">Weight for liquidity percentile factor (0.0-1.0). Must be within valid range.</param>
        /// <param name="openInterestPercentileWeight">Weight for open interest percentile factor (0.0-1.0). Must be within valid range.</param>
        /// <param name="continuityWeight">Weight for market continuity factor (0.0-1.0). Must be within valid range.</param>
        /// <returns>A tuple containing the final score and detailed score components.</returns>
        /// <exception cref="ArgumentException">Thrown when weights are outside the 0.0-1.0 range.</exception>
        public async Task<(double score,
    (double spreadTightness, double spreadWidth, double volume, double volumePercentile, double liquidityPercentile, double openInterestPercentile, double continuity) scoreParts)>
CalculateMarketInterestScoreAsync(
    dynamic market,
    long snapshotCount,
    double spreadTightnessWeight = 0.2,
    double spreadWidthWeight = 0.15,
    double volumeWeight = 0.20,
    double volumePercentileWeight = 0.1,
    double liquidityPercentileWeight = 0.09,
    double openInterestPercentileWeight = 0.065,
    double continuityWeight = 0.145)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                // Input validation
                ValidateWeights(new Dictionary<string, double>
                {
                    ["spreadTightnessWeight"] = spreadTightnessWeight,
                    ["spreadWidthWeight"] = spreadWidthWeight,
                    ["volumeWeight"] = volumeWeight,
                    ["volumePercentileWeight"] = volumePercentileWeight,
                    ["liquidityPercentileWeight"] = liquidityPercentileWeight,
                    ["openInterestPercentileWeight"] = openInterestPercentileWeight,
                    ["continuityWeight"] = continuityWeight
                });


                if (market == null)
                {
                    _logger.LogWarning("No market data found.");
                    return (0.0, (0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0));
                }

                // Calculate score
                if (KalshiConstants.IsMarketStatusEnded(market.status) || market.yes_bid == 0 || market.no_bid == 0)
                {
                    _logger.LogDebug("MarketInterestScore 0.0 for market due to market being ended.");
                    return (0.0, (0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0));
                }

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

                _logger.LogInformation("Calculated interest score {Score} for market.", finalScore);
                RecordScoringOperationTime(DateTime.UtcNow - startTime);
                return (finalScore, scoreParts);
            }
            catch (Exception ex)
            {
                RecordScoringOperationTime(DateTime.UtcNow - startTime);
                _logger.LogError(ex, "Failed to calculate MarketInterestScore for market.");
                throw;
            }
        }

        /// <summary>
        /// Gets current performance metrics for the interest score service.
        /// Returns cache hit/miss statistics and operation timing information.
        /// </summary>
        /// <returns>A tuple containing cache hits, cache misses, average operation time in milliseconds, and total operations count.</returns>
        public (int CacheHits, int CacheMisses, double AverageOperationTimeMs, int TotalOperations) GetPerformanceMetrics()
        {
            var hits = _cacheHits;
            var misses = _cacheMisses;
            double avgTime = 0;
            int count = 0;
            lock (_scoringOperationTimes)
            {
                if (_scoringOperationTimes.Count > 0)
                {
                    avgTime = _scoringOperationTimes.Average(t => t.TotalMilliseconds);
                    count = _scoringOperationTimes.Count;
                }
            }
            return (hits, misses, avgTime, count);
        }

        /// <summary>
        /// Gets the cache hit rate as a percentage.
        /// </summary>
        /// <returns>The cache hit rate (0.0 to 100.0).</returns>
        public double GetCacheHitRate()
        {
            var (hits, misses, _, _) = GetPerformanceMetrics();
            var total = hits + misses;
            return total > 0 ? (double)hits / total * 100.0 : 0.0;
        }

    }
}
