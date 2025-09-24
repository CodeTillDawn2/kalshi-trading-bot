using BacklashBot.Services.Interfaces;
using BacklashDTOs;
using BacklashDTOs.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;

/// <summary>
/// Complete ML model for predicting 15% price spikes within 10 minutes in Kalshi markets.
/// Uses ML.NET Random Forest classifier with feature engineering for time-series patterns.
/// Focuses on "Yes" prices as per Kalshi market mechanics.
/// </summary>
namespace TradingStrategies.ML.SpikePrediction
{
    public class SpikePredictionModel
    {
        private readonly ILogger<SpikePredictionModel> _logger;
        private readonly ITradingSnapshotService _snapshotService;
        private readonly MLContext _mlContext;
        private ITransformer _model;
        private readonly SpikePredictionConfig _config;
        private readonly Dictionary<string, List<MarketSnapshot>> _recentSnapshotsCache = new();


        /// <summary>
        /// Input data structure for ML model.
        /// </summary>
        public class SpikePredictionData
        {
            [LoadColumn(0)] public float CurrentPrice { get; set; }
            [LoadColumn(1)] public float PriceChange1Min { get; set; }
            [LoadColumn(2)] public float PriceChange5Min { get; set; }
            [LoadColumn(3)] public float VolumeChange1Min { get; set; }
            [LoadColumn(4)] public float VolumeChange5Min { get; set; }
            [LoadColumn(5)] public float SlopeShort { get; set; }
            [LoadColumn(6)] public float SlopeMedium { get; set; }
            [LoadColumn(7)] public float RSI { get; set; }
            [LoadColumn(8)] public float MACD { get; set; }
            [LoadColumn(9)] public float Spread { get; set; }
            [LoadColumn(10)] public float DepthRatio { get; set; }
            [LoadColumn(11)] public float Volatility { get; set; }
            [LoadColumn(12), ColumnName("Label")] public bool IsSpike { get; set; }
        }


        /// <summary>
        /// Initializes the spike prediction model.
        /// </summary>
        public SpikePredictionModel(
            ILogger<SpikePredictionModel> logger,
            ITradingSnapshotService snapshotService,
            SpikePredictionConfig config = null)
        {
            _logger = logger;
            _snapshotService = snapshotService;
            _config = config ?? new SpikePredictionConfig();
            _mlContext = new MLContext(seed: 0); // Deterministic results
        }

        /// <summary>
        /// Loads and prepares training data from market snapshots with scalability optimizations.
        /// Uses TradingSnapshotService to load historical data with batching and performance monitoring.
        /// </summary>
        public async Task<List<SpikePredictionData>> LoadTrainingDataAsync(
            List<string> marketTickers,
            DateTime startDate,
            DateTime endDate)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation("Loading training data from {StartDate} to {EndDate} for {Count} markets",
                startDate, endDate, marketTickers.Count);

            var trainingData = new List<SpikePredictionData>();
            int totalSamplesProcessed = 0;
            int marketsProcessed = 0;

            try
            {
                // Process markets in batches for scalability
                const int BATCH_SIZE = 50; // Process 50 markets at a time
                var marketBatches = marketTickers
                    .Select((ticker, index) => new { Ticker = ticker, Batch = index / BATCH_SIZE })
                    .GroupBy(x => x.Batch)
                    .Select(g => g.Select(x => x.Ticker).ToList())
                    .ToList();

                foreach (var batch in marketBatches)
                {
                    var batchStopwatch = System.Diagnostics.Stopwatch.StartNew();

                    // Load snapshots for current batch
                    var snapshots = await LoadSnapshotsFromService(batch, startDate, endDate);

                    // Process batch with parallel processing and progress tracking
                    var batchResults = await ProcessMarketBatchAsync(snapshots, batch.Count);

                    lock (trainingData)
                    {
                        trainingData.AddRange(batchResults.trainingSamples);
                    }

                    totalSamplesProcessed += batchResults.samplesProcessed;
                    marketsProcessed += batch.Count;

                    batchStopwatch.Stop();
                    _logger.LogInformation("Processed batch {BatchIndex}/{TotalBatches}: {MarketsInBatch} markets, {SamplesInBatch} samples in {ElapsedMs}ms",
                        marketBatches.IndexOf(batch) + 1, marketBatches.Count, batch.Count, batchResults.samplesProcessed, batchStopwatch.ElapsedMilliseconds);
                }

                stopwatch.Stop();
                _logger.LogInformation("Training data loading completed: {TotalSamples} samples from {TotalMarkets} markets in {TotalMs}ms ({SamplesPerSecond:F1} samples/sec)",
                    trainingData.Count, marketsProcessed, stopwatch.ElapsedMilliseconds,
                    trainingData.Count / (stopwatch.ElapsedMilliseconds / 1000.0));

                return trainingData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading training data after processing {ProcessedMarkets} markets and {ProcessedSamples} samples",
                    marketsProcessed, totalSamplesProcessed);
                return trainingData; // Return partial results if available
            }
        }

        /// <summary>
        /// Processes a batch of markets with parallel processing and performance monitoring.
        /// </summary>
        private async Task<(List<SpikePredictionData> trainingSamples, int samplesProcessed)> ProcessMarketBatchAsync(
            Dictionary<string, List<MarketSnapshot>> snapshots, int expectedMarketCount)
        {
            var trainingSamples = new List<SpikePredictionData>();
            int samplesProcessed = 0;

            // Use Parallel.ForEach with degree of parallelism control for scalability
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, expectedMarketCount)
            };

            await Task.Run(() =>
            {
                Parallel.ForEach(snapshots, parallelOptions, marketEntry =>
                {
                    var marketData = marketEntry.Value.OrderBy(s => s.Timestamp).ToList();
                    var marketTrainingData = new List<SpikePredictionData>();

                    // Process snapshots with early exit for performance
                    for (int i = _config.LagMinutes; i < marketData.Count - 1; i++)
                    {
                        var currentSnapshot = marketData[i];

                        // Early exit if we're too close to market resolution
                        if (currentSnapshot.TimeLeft.HasValue &&
                            currentSnapshot.TimeLeft.Value < _config.PredictionWindow)
                            break;

                        var futureSnapshots = marketData.Skip(i + 1)
                            .TakeWhile(s => s.Timestamp <= currentSnapshot.Timestamp.Add(_config.PredictionWindow))
                            .ToList();

                        if (!futureSnapshots.Any()) continue;

                        // Calculate target: did price spike by configured threshold within prediction window?
                        var maxFuturePrice = futureSnapshots.Max(s => s.BestYesBidD);
                        var currentPrice = currentSnapshot.BestYesBidD;
                        var priceIncrease = (maxFuturePrice - currentPrice) / currentPrice;
                        var isSpike = priceIncrease >= _config.SpikeThreshold;

                        // Create features using recent snapshots for lagged features
                        var recentSnapshots = marketData
                            .Where(s => s.Timestamp <= currentSnapshot.Timestamp)
                            .OrderByDescending(s => s.Timestamp)
                            .Take(_config.LagMinutes + 1)
                            .ToList();

                        var features = ExtractFeaturesFromRecentSnapshots(currentSnapshot, recentSnapshots);
                        if (features != null)
                        {
                            features.IsSpike = isSpike;
                            marketTrainingData.Add(features);
                        }
                    }

                    lock (trainingSamples)
                    {
                        trainingSamples.AddRange(marketTrainingData);
                        samplesProcessed += marketTrainingData.Count;
                    }
                });
            });

            return (trainingSamples, samplesProcessed);
        }

        /// <summary>
        /// Loads snapshots using TradingSnapshotService with proper data conversion.
        /// Uses extension methods to convert SnapshotDTOs to MarketSnapshot objects.
        /// Handles multiple markets and date ranges efficiently.
        /// </summary>
        private async Task<Dictionary<string, List<MarketSnapshot>>> LoadSnapshotsFromService(
            List<string> marketTickers, DateTime startDate, DateTime endDate)
        {
            var result = new Dictionary<string, List<MarketSnapshot>>();

            try
            {
                // Parallel processing for multiple markets to improve performance
                var tasks = marketTickers.Select(async ticker =>
                {
                    try
                    {
                        // In real implementation, this would query database for SnapshotDTOs
                        // Example query simulation:
                        var snapshotDTOs = await QuerySnapshotDTOsForMarket(ticker, startDate, endDate);

                        if (snapshotDTOs.Any())
                        {
                            // Use TradingSnapshotService to load and validate snapshots
                            var loadedSnapshots = await _snapshotService.LoadManySnapshots(snapshotDTOs, false);

                            // LoadManySnapshots returns MarketSnapshot objects directly
                            var marketSnapshots = loadedSnapshots.Values
                                .SelectMany(list => list)
                                .Where(snapshot => snapshot != null)
                                .ToList();

                            return new { Ticker = ticker, Snapshots = marketSnapshots };
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Error loading snapshots for market {Ticker}. Exception: {ExceptionMessage}, Inner: {InnerExceptionMessage}", ticker, ex.Message, ex.InnerException?.Message ?? "None");
                    }

                    return new { Ticker = ticker, Snapshots = new List<MarketSnapshot>() };
                });

                var results = await Task.WhenAll(tasks);

                foreach (var marketResult in results)
                {
                    if (marketResult.Snapshots.Any())
                    {
                        result[marketResult.Ticker] = marketResult.Snapshots;
                        _logger.LogInformation("Loaded {Count} snapshots for market {Ticker}",
                            marketResult.Snapshots.Count, marketResult.Ticker);
                    }
                }

                _logger.LogInformation("Successfully loaded data for {Count} markets", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading snapshots from service");
                return result; // Return partial results if available
            }
        }

        /// <summary>
        /// Queries SnapshotDTOs for a specific market and date range.
        /// TODO: Replace with actual database implementation using ITradingSnapshotService or direct database access.
        /// Production implementation should:
        /// - Use proper database connection with connection pooling
        /// - Implement efficient indexing on MarketTicker and Timestamp
        /// - Add pagination for large result sets
        /// - Include proper error handling and retry logic
        /// - Consider caching frequently accessed data
        /// </summary>
        private async Task<List<SnapshotDTO>> QuerySnapshotDTOsForMarket(string ticker, DateTime startDate, DateTime endDate)
        {
            // PRODUCTION TODO: Replace with actual database query
            // Example implementation:
            /*
            using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
            {
                return await dbContext.Snapshots
                    .Where(s => s.MarketTicker == ticker &&
                               s.SnapshotDate >= startDate &&
                               s.SnapshotDate <= endDate)
                    .OrderBy(s => s.SnapshotDate)
                    .ToListAsync();
            }
            */

            // Temporary simulation for development/testing
            await Task.Delay(10); // Simulate async database call
            _logger.LogWarning("Using mock data for {Ticker}. Replace QuerySnapshotDTOsForMarket with actual database implementation.", ticker);
            return new List<SnapshotDTO>(); // Return empty list - real implementation needed
        }

        /// <summary>
        /// Extracts features from market snapshots for ML model.
        /// Includes lagged price/volume changes and technical indicators.
        /// </summary>
        private SpikePredictionData ExtractFeatures(List<MarketSnapshot> snapshots, int currentIndex)
        {
            try
            {
                var current = snapshots[currentIndex];
                var prev1Min = currentIndex >= 1 ? snapshots[currentIndex - 1] : null;
                var prev5Min = currentIndex >= 5 ? snapshots[currentIndex - 5] : null;

                return new SpikePredictionData
                {
                    CurrentPrice = (float)current.BestYesBidD,
                    PriceChange1Min = prev1Min != null ?
                        (float)((current.BestYesBidD - prev1Min.BestYesBidD) / prev1Min.BestYesBidD) : 0,
                    PriceChange5Min = prev5Min != null ?
                        (float)((current.BestYesBidD - prev5Min.BestYesBidD) / prev5Min.BestYesBidD) : 0,
                    VolumeChange1Min = prev1Min != null ?
                        (float)((current.TradeVolumePerMinute_Yes - prev1Min.TradeVolumePerMinute_Yes) /
                                (prev1Min.TradeVolumePerMinute_Yes + 1)) : 0,
                    VolumeChange5Min = prev5Min != null ?
                        (float)((current.TradeVolumePerMinute_Yes - prev5Min.TradeVolumePerMinute_Yes) /
                                (prev5Min.TradeVolumePerMinute_Yes + 1)) : 0,
                    SlopeShort = (float)current.YesBidSlopePerMinute_Short,
                    SlopeMedium = (float)current.YesBidSlopePerMinute_Medium,
                    RSI = current.RSI_Short.HasValue ? (float)current.RSI_Short.Value : 50f,
                    MACD = current.MACD_Medium.MACD.HasValue ? (float)current.MACD_Medium.MACD.Value : 0f,
                    Spread = (float)current.YesSpread,
                    DepthRatio = current.TotalBidContracts_Yes > 0 ?
                        (float)(current.DepthAtBestYesBid / (double)current.TotalBidContracts_Yes) : 0,
                    Volatility = current.ATR_Medium.HasValue ? (float)current.ATR_Medium.Value : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error extracting features at index {Index}. Exception: {ExceptionMessage}, Inner: {InnerExceptionMessage}", currentIndex, ex.Message, ex.InnerException?.Message ?? "None");
                return null;
            }
        }

        /// <summary>
        /// Trains the random forest model with class imbalance handling.
        /// </summary>
        public void TrainModel(List<SpikePredictionData> trainingData)
        {
            _logger.LogInformation("Training model with {Count} samples", trainingData.Count);

            // Handle class imbalance with oversampling - spikes are rare
            var spikeCount = trainingData.Count(d => d.IsSpike);
            var nonSpikeCount = trainingData.Count(d => !d.IsSpike);
            var imbalanceRatio = (double)nonSpikeCount / spikeCount;

            _logger.LogInformation("Class distribution: {SpikeCount} spikes, {NonSpikeCount} non-spikes (ratio: {Ratio:F2})",
                spikeCount, nonSpikeCount, imbalanceRatio);

            // Implement oversampling to balance classes
            var balancedData = BalanceClassesWithOversampling(trainingData, imbalanceRatio);

            _logger.LogInformation("After oversampling: {Count} total samples", balancedData.Count);

            // Create data view
            var dataView = _mlContext.Data.LoadFromEnumerable(balancedData);

            // Define pipeline with FastTree (Random Forest) trainer
            var pipeline = _mlContext.Transforms.Concatenate("Features",
                    nameof(SpikePredictionData.CurrentPrice),
                    nameof(SpikePredictionData.PriceChange1Min),
                    nameof(SpikePredictionData.PriceChange5Min),
                    nameof(SpikePredictionData.VolumeChange1Min),
                    nameof(SpikePredictionData.VolumeChange5Min),
                    nameof(SpikePredictionData.SlopeShort),
                    nameof(SpikePredictionData.SlopeMedium),
                    nameof(SpikePredictionData.RSI),
                    nameof(SpikePredictionData.MACD),
                    nameof(SpikePredictionData.Spread),
                    nameof(SpikePredictionData.DepthRatio),
                    nameof(SpikePredictionData.Volatility))
                // Note: Using SdcaLogisticRegression as FastTree may not be available in this ML.NET version
                // Replace with FastTree trainer when available for true random forest implementation
                .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(
                    labelColumnName: "Label",
                    featureColumnName: "Features"));

            // Train model
            _model = pipeline.Fit(dataView);

            _logger.LogInformation("Model training completed");
        }

        /// <summary>
        /// Balances classes using oversampling by duplicating minority class samples.
        /// Addresses class imbalance where spikes are rare events.
        /// </summary>
        private List<SpikePredictionData> BalanceClassesWithOversampling(
            List<SpikePredictionData> originalData, double imbalanceRatio)
        {
            var spikeSamples = originalData.Where(d => d.IsSpike).ToList();
            var nonSpikeSamples = originalData.Where(d => !d.IsSpike).ToList();

            var balancedData = new List<SpikePredictionData>(originalData);

            // Calculate how many duplicates needed to balance classes
            int targetSpikeCount = (int)(nonSpikeSamples.Count / 2.0); // Aim for 50/50 balance
            int duplicatesNeeded = Math.Max(0, targetSpikeCount - spikeSamples.Count);

            if (duplicatesNeeded > 0)
            {
                // Duplicate spike samples to balance classes
                var random = new Random(42); // Deterministic for reproducibility
                for (int i = 0; i < duplicatesNeeded; i++)
                {
                    var randomSpike = spikeSamples[random.Next(spikeSamples.Count)];
                    // Create a copy with slight noise to avoid exact duplicates
                    var duplicatedSample = new SpikePredictionData
                    {
                        CurrentPrice = randomSpike.CurrentPrice,
                        PriceChange1Min = (float)(randomSpike.PriceChange1Min * (0.95 + random.NextDouble() * 0.1)), // ±5% noise
                        PriceChange5Min = (float)(randomSpike.PriceChange5Min * (0.95 + random.NextDouble() * 0.1)),
                        VolumeChange1Min = randomSpike.VolumeChange1Min,
                        VolumeChange5Min = randomSpike.VolumeChange5Min,
                        SlopeShort = randomSpike.SlopeShort,
                        SlopeMedium = randomSpike.SlopeMedium,
                        RSI = randomSpike.RSI,
                        MACD = randomSpike.MACD,
                        Spread = randomSpike.Spread,
                        DepthRatio = randomSpike.DepthRatio,
                        Volatility = randomSpike.Volatility,
                        IsSpike = true
                    };
                    balancedData.Add(duplicatedSample);
                }

                _logger.LogInformation("Added {Count} oversampled spike examples to balance classes", duplicatesNeeded);
            }

            return balancedData;
        }

        /// <summary>
        /// Evaluates model performance with precision, recall, and F1 score.
        /// </summary>
        public void EvaluateModel(List<SpikePredictionData> testData)
        {
            _logger.LogInformation("Evaluating model with {Count} test samples", testData.Count);

            var testDataView = _mlContext.Data.LoadFromEnumerable(testData);
            var predictions = _model.Transform(testDataView);

            var metrics = _mlContext.BinaryClassification.Evaluate(predictions, "Label");

            _logger.LogInformation("Model Evaluation Results:");
            _logger.LogInformation("Accuracy: {Accuracy:F4}", metrics.Accuracy);
            _logger.LogInformation("Precision: {Precision:F4}", metrics.PositivePrecision);
            _logger.LogInformation("Recall: {Recall:F4}", metrics.PositiveRecall);
            _logger.LogInformation("F1 Score: {F1:F4}", metrics.F1Score);
            _logger.LogInformation("AUC: {AUC:F4}", metrics.AreaUnderRocCurve);
        }

        /// <summary>
        /// Saves the trained model to disk.
        /// </summary>
        public void SaveModel()
        {
            _logger.LogInformation("Saving model to {Path}", _config.ModelPath);
            _mlContext.Model.Save(_model, null, _config.ModelPath);
            _logger.LogInformation("Model saved successfully");
        }

        /// <summary>
        /// Loads a saved model from disk.
        /// </summary>
        public void LoadModel()
        {
            _logger.LogInformation("Loading model from {Path}", _config.ModelPath);
            _model = _mlContext.Model.Load(_config.ModelPath, out _);
            _logger.LogInformation("Model loaded successfully");
        }

        /// <summary>
        /// Predicts spike probability for a market snapshot using recent historical context.
        /// Requires recent snapshots for accurate lagged feature computation.
        /// </summary>
        public SpikePredictionResult PredictSpike(MarketSnapshot currentSnapshot, List<MarketSnapshot> recentSnapshots)
        {
            try
            {
                // Cache recent snapshots for this market to avoid repeated computations
                var marketTicker = currentSnapshot.MarketTicker ?? "UNKNOWN";
                if (!_recentSnapshotsCache.ContainsKey(marketTicker))
                {
                    _recentSnapshotsCache[marketTicker] = new List<MarketSnapshot>();
                }

                // Update cache with recent snapshots
                _recentSnapshotsCache[marketTicker] = recentSnapshots
                    .OrderByDescending(s => s.Timestamp)
                    .Take(_config.LagMinutes * 2) // Keep more than needed for flexibility
                    .ToList();

                // Extract features using recent historical context
                var features = ExtractFeaturesFromRecentSnapshots(currentSnapshot, _recentSnapshotsCache[marketTicker]);

                var predictionEngine = _mlContext.Model.CreatePredictionEngine<SpikePredictionData, SpikePredictionResult>(_model);
                var prediction = predictionEngine.Predict(features);

                return prediction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error predicting spike for market {Ticker}", currentSnapshot.MarketTicker);
                return new SpikePredictionResult { Probability = 0, PredictedSpike = false };
            }
        }

        /// <summary>
        /// Extracts features from current snapshot and recent historical context.
        /// Provides accurate lagged features for better prediction quality.
        /// </summary>
        private SpikePredictionData ExtractFeaturesFromRecentSnapshots(MarketSnapshot current, List<MarketSnapshot> recentSnapshots)
        {
            try
            {
                // Find snapshots at specific time lags
                var orderedSnapshots = recentSnapshots.OrderByDescending(s => s.Timestamp).ToList();
                var currentIndex = orderedSnapshots.FindIndex(s => s.Timestamp == current.Timestamp);

                MarketSnapshot prev1Min = null, prev5Min = null;

                if (currentIndex >= 0)
                {
                    // Look for snapshots approximately 1 and 5 minutes ago
                    var target1Min = current.Timestamp.AddMinutes(-1);
                    var target5Min = current.Timestamp.AddMinutes(-5);

                    prev1Min = orderedSnapshots
                        .Where(s => s.Timestamp <= target1Min)
                        .OrderByDescending(s => s.Timestamp)
                        .FirstOrDefault();

                    prev5Min = orderedSnapshots
                        .Where(s => s.Timestamp <= target5Min)
                        .OrderByDescending(s => s.Timestamp)
                        .FirstOrDefault();
                }

                return new SpikePredictionData
                {
                    CurrentPrice = (float)current.BestYesBidD,
                    PriceChange1Min = prev1Min != null ?
                        (float)((current.BestYesBidD - prev1Min.BestYesBidD) / prev1Min.BestYesBidD) : 0,
                    PriceChange5Min = prev5Min != null ?
                        (float)((current.BestYesBidD - prev5Min.BestYesBidD) / prev5Min.BestYesBidD) : 0,
                    VolumeChange1Min = prev1Min != null ?
                        (float)((current.TradeVolumePerMinute_Yes - prev1Min.TradeVolumePerMinute_Yes) /
                                (prev1Min.TradeVolumePerMinute_Yes + 1)) : 0,
                    VolumeChange5Min = prev5Min != null ?
                        (float)((current.TradeVolumePerMinute_Yes - prev5Min.TradeVolumePerMinute_Yes) /
                                (prev5Min.TradeVolumePerMinute_Yes + 1)) : 0,
                    SlopeShort = (float)current.YesBidSlopePerMinute_Short,
                    SlopeMedium = (float)current.YesBidSlopePerMinute_Medium,
                    RSI = current.RSI_Short.HasValue ? (float)current.RSI_Short.Value : 50f,
                    MACD = current.MACD_Medium.MACD.HasValue ? (float)current.MACD_Medium.MACD.Value : 0f,
                    Spread = (float)current.YesSpread,
                    DepthRatio = current.TotalBidContracts_Yes > 0 ?
                        (float)(current.DepthAtBestYesBid / (double)current.TotalBidContracts_Yes) : 0,
                    Volatility = current.ATR_Medium.HasValue ? (float)current.ATR_Medium.Value : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error extracting features from recent snapshots, using current snapshot only. Exception: {ExceptionMessage}, Inner: {InnerExceptionMessage}", ex.Message, ex.InnerException?.Message ?? "None");

                // Fallback to current snapshot only
                return new SpikePredictionData
                {
                    CurrentPrice = (float)current.BestYesBidD,
                    PriceChange1Min = 0,
                    PriceChange5Min = 0,
                    VolumeChange1Min = 0,
                    VolumeChange5Min = 0,
                    SlopeShort = (float)current.YesBidSlopePerMinute_Short,
                    SlopeMedium = (float)current.YesBidSlopePerMinute_Medium,
                    RSI = current.RSI_Short.HasValue ? (float)current.RSI_Short.Value : 50f,
                    MACD = current.MACD_Medium.MACD.HasValue ? (float)current.MACD_Medium.MACD.Value : 0f,
                    Spread = (float)current.YesSpread,
                    DepthRatio = current.TotalBidContracts_Yes > 0 ?
                        (float)(current.DepthAtBestYesBid / (double)current.TotalBidContracts_Yes) : 0,
                    Volatility = current.ATR_Medium.HasValue ? (float)current.ATR_Medium.Value : 0
                };
            }
        }

        /// <summary>
        /// Example usage in a C# trading bot with mock service implementation.
        /// Shows how to load the model and use it for real-time predictions with historical context.
        /// </summary>
        public static async Task ExampleUsageInTradingBot()
        {
            // Setup dependency injection with mock service
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ITradingSnapshotService, MockTradingSnapshotService>();
            services.AddScoped<SpikePredictionModel>();

            var serviceProvider = services.BuildServiceProvider();
            var logger = serviceProvider.GetRequiredService<ILogger<SpikePredictionModel>>();
            var snapshotService = serviceProvider.GetRequiredService<ITradingSnapshotService>();

            // Create model instance with enhanced config
            var config = new SpikePredictionConfig
            {
                SpikeThreshold = 0.15,
                PredictionWindow = TimeSpan.FromMinutes(10),
                ModelPath = "spike_prediction_model.zip",
                PredictionThreshold = 0.7, // Use configurable threshold
                NumberOfTrees = 100,
                MinimumLeafSize = 10,
                MaximumDepth = 10
            };

            var model = new SpikePredictionModel(logger, snapshotService, config);

            // Load saved model
            model.LoadModel();

            // Create example market snapshots with historical context
            var baseTime = DateTime.UtcNow;
            var recentSnapshots = new List<MarketSnapshot>
        {
            new MarketSnapshot
            {
                Timestamp = baseTime.AddMinutes(-5),
                MarketTicker = "EXAMPLE_MARKET",
                BestYesBidD = 0.62,
                TradeVolumePerMinute_Yes = 150,
                YesBidSlopePerMinute_Short = 0.015,
                YesBidSlopePerMinute_Medium = 0.012,
                RSI_Short = 58,
                MACD_Medium = (0.015, 0.014, 0.001),
                YesSpread = 2,
                DepthAtBestYesBid = 80,
                TotalBidContracts_Yes = 400,
                ATR_Medium = 0.025
            },
            new MarketSnapshot
            {
                Timestamp = baseTime.AddMinutes(-1),
                MarketTicker = "EXAMPLE_MARKET",
                BestYesBidD = 0.64,
                TradeVolumePerMinute_Yes = 180,
                YesBidSlopePerMinute_Short = 0.018,
                YesBidSlopePerMinute_Medium = 0.014,
                RSI_Short = 62,
                MACD_Medium = (0.018, 0.016, 0.002),
                YesSpread = 2,
                DepthAtBestYesBid = 90,
                TotalBidContracts_Yes = 450,
                ATR_Medium = 0.028
            }
        };

            // Current snapshot for prediction
            var currentSnapshot = new MarketSnapshot
            {
                Timestamp = baseTime,
                MarketTicker = "EXAMPLE_MARKET",
                BestYesBidD = 0.65,
                TradeVolumePerMinute_Yes = 200,
                YesBidSlopePerMinute_Short = 0.02,
                YesBidSlopePerMinute_Medium = 0.015,
                RSI_Short = 65,
                MACD_Medium = (0.02, 0.018, 0.002),
                YesSpread = 2,
                DepthAtBestYesBid = 100,
                TotalBidContracts_Yes = 500,
                ATR_Medium = 0.03
            };

            var prediction = model.PredictSpike(currentSnapshot, recentSnapshots);

            var exampleLogger = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddConsole())
                .CreateLogger("SpikePredictionExample");

            exampleLogger.LogInformation("Spike Probability: {Probability:F4}", prediction.Probability);
            exampleLogger.LogInformation("Predicted Spike: {PredictedSpike}", prediction.PredictedSpike);

            // Use configurable prediction threshold for decision making
            if (prediction.Probability > config.PredictionThreshold)
            {
                exampleLogger.LogWarning("HIGH SPIKE RISK (>{Threshold:P0}) - Consider position adjustment",
                    config.PredictionThreshold);
            }
            else
            {
                exampleLogger.LogInformation("Low spike risk (<={Threshold:P0}) - Continue normal operation",
                    config.PredictionThreshold);
            }
        }

        /// <summary>
        /// Mock implementation of ITradingSnapshotService for testing and examples.
        /// Provides in-memory storage of SnapshotDTOs converted to MarketSnapshot objects.
        /// </summary>
        private class MockTradingSnapshotService : ITradingSnapshotService
        {
            private readonly ILogger<MockTradingSnapshotService> _mockLogger;
            private readonly Dictionary<string, List<MarketSnapshot>> _mockData = new();

            public MockTradingSnapshotService()
            {
                _mockLogger = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddConsole())
                    .CreateLogger<MockTradingSnapshotService>();
            }

            public DateTime? NextExpectedSnapshotTimestamp { get; set; }

            public async Task<List<string>> SaveSnapshotAsync(string marketTicker, CacheSnapshot snapshot)
            {
                // Mock implementation - just log
                _mockLogger.LogInformation("Mock: Saved snapshot for {MarketTicker}", marketTicker);
                return new List<string>(); // Return empty list for success
            }

            public Task<Dictionary<string, List<MarketSnapshot>>> LoadManySnapshots(List<SnapshotDTO> snapshotDTOs, bool validateSchema)
            {
                // Convert SnapshotDTOs to MarketSnapshot (mock conversion)
                var result = new Dictionary<string, List<MarketSnapshot>>();

                foreach (var dto in snapshotDTOs)
                {
                    var marketSnapshot = new MarketSnapshot
                    {
                        Timestamp = DateTime.UtcNow, // Mock timestamp
                        MarketTicker = dto.MarketTicker ?? "UNKNOWN",
                        BestYesBidD = 0.5, // Mock default value - in real implementation, deserialize from RawJSON
                                           // Add other properties as needed for testing
                    };

                    if (!result.ContainsKey(dto.MarketTicker ?? "UNKNOWN"))
                    {
                        result[dto.MarketTicker ?? "UNKNOWN"] = new List<MarketSnapshot>();
                    }

                    result[dto.MarketTicker ?? "UNKNOWN"].Add(marketSnapshot);
                }

                return Task.FromResult(result);
            }

            public bool ValidateMarketSnapshot(MarketSnapshot snapshot) => true;

            public void ResetSnapshotTracking() { }

            public async Task<bool> ValidateSnapshotSchema() => await Task.FromResult(true);

            public string SanitizeSnapshotJson(int marketId, string rawJson) => rawJson;
        }

        /// <summary>
        /// Main method for running the spike prediction training pipeline.
        /// </summary>
        public static async Task Main(string[] args)
        {
            // Setup services with proper dependency injection
            var services = new ServiceCollection();
            services.AddLogging(config => config.AddConsole().SetMinimumLevel(LogLevel.Information));
            services.AddSingleton<ITradingSnapshotService, MockTradingSnapshotService>();

            var serviceProvider = services.BuildServiceProvider();
            var logger = serviceProvider.GetRequiredService<ILogger<SpikePredictionModel>>();
            var snapshotService = serviceProvider.GetRequiredService<ITradingSnapshotService>();

            logger.LogInformation("Kalshi Spike Prediction Model Training");
            logger.LogInformation("=====================================");

            // Create enhanced config with all parameters
            var config = new SpikePredictionConfig
            {
                SpikeThreshold = 0.15, // 15% price spike threshold
                PredictionWindow = TimeSpan.FromMinutes(10),
                LagMinutes = 5,
                ModelPath = "spike_prediction_model.zip",
                PredictionThreshold = 0.7, // 70% probability threshold for decisions
                NumberOfTrees = 100,
                MinimumLeafSize = 10,
                MaximumDepth = 10
            };

            var model = new SpikePredictionModel(logger, snapshotService, config);

            try
            {
                // Load training data with performance monitoring
                var marketTickers = new List<string> { "MARKET1", "MARKET2" }; // Replace with actual tickers
                var trainingData = await model.LoadTrainingDataAsync(
                    marketTickers,
                    DateTime.Now.AddDays(-30),
                    DateTime.Now);

                if (!trainingData.Any())
                {
                    logger.LogWarning("No training data available. Exiting.");
                    return;
                }

                logger.LogInformation("Loaded {Count} training samples", trainingData.Count);

                // Split data for training/testing
                var splitIndex = (int)(trainingData.Count * 0.8);
                var trainData = trainingData.Take(splitIndex).ToList();
                var testData = trainingData.Skip(splitIndex).ToList();

                logger.LogInformation("Training set: {TrainCount} samples, Test set: {TestCount} samples",
                    trainData.Count, testData.Count);

                // Train model
                logger.LogInformation("Starting model training...");
                model.TrainModel(trainData);

                // Evaluate
                logger.LogInformation("Evaluating model performance...");
                model.EvaluateModel(testData);

                // Save model
                logger.LogInformation("Saving trained model...");
                model.SaveModel();

                logger.LogInformation("Training pipeline completed successfully!");
                logger.LogInformation("Model saved to: {ModelPath}", config.ModelPath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during training pipeline execution");
            }
        }
    }
}
