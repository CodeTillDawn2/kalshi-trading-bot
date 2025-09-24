using BacklashDTOs;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;
using TradingStrategies.ML.SpikePrediction;
using TradingStrategies.Trading.Overseer;
using static BacklashInterfaces.Enums.StrategyEnums;

namespace TradingStrategies.Strategies.Strats
{
    /// <summary>
    /// ML-based strategy that uses machine learning to predict 15% price spikes within 10 minutes.
    /// Uses Random Forest classifier trained on historical market data with feature engineering
    /// including lagged prices, volumes, and technical indicators.
    /// </summary>
    public class MLSpikePredictionStrat : Strat
    {
        public string Name { get; private set; }
        public override double Weight { get; }

        private readonly ILogger _logger;
        private readonly SpikePredictionModel _mlModel;
        private readonly SpikePredictionConfig _config;

        /// <summary>
        /// Initializes a new instance of the ML Spike Prediction strategy.
        /// </summary>
        /// <param name="name">Strategy name for identification</param>
        /// <param name="weight">Strategy weight in composite decision-making</param>
        /// <param name="logger">Logger for recording strategy operations</param>
        /// <param name="mlModel">Pre-trained ML model for spike prediction</param>
        /// <param name="config">Configuration parameters for the ML model</param>
        public MLSpikePredictionStrat(
            string name = "MLSpikePrediction",
            double weight = 1.0,
            ILogger? logger = null,
            SpikePredictionModel? mlModel = null,
            SpikePredictionConfig? config = null)
        {
            Name = name;
            Weight = weight;
            _logger = logger ?? Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("MLSpikePredictionStrat");
            _config = config ?? new SpikePredictionConfig();

            // Initialize ML model - in production, this should be injected or loaded from saved model
            var mlLogger = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<SpikePredictionModel>();
            _mlModel = mlModel ?? new SpikePredictionModel(mlLogger, null, _config);

            // Try to load existing model, but don't fail if it doesn't exist
            try
            {
                _mlModel.LoadModel();
                _logger.LogInformation("ML Spike Prediction model loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not load ML model, strategy will return None actions until model is trained. Exception: {ExceptionMessage}, Inner: {InnerExceptionMessage}", ex.Message, ex.InnerException?.Message ?? "None");
            }
        }

        public override ActionDecision GetAction(MarketSnapshot snapshot, MarketSnapshot? previousSnapshot, int simulationPosition = 0)
        {
            if (snapshot == null || !snapshot.ChangeMetricsMature)
            {
                return new ActionDecision
                {
                    Type = ActionType.None,
                    Price = 0,
                    Quantity = 0,
                    Memo = "not_mature"
                };
            }

            try
            {
                // Get recent snapshots for lagged feature computation
                // In a real implementation, this would come from a snapshot cache or service
                var recentSnapshots = GetRecentSnapshots(snapshot);

                // Run ML prediction
                var prediction = _mlModel.PredictSpike(snapshot, recentSnapshots);

                // Build detailed memo with ML results
                var memo = BuildMLMemo(snapshot, prediction, recentSnapshots);

                // Decision logic based on prediction probability
                var decision = MakeDecisionBasedOnPrediction(snapshot, prediction, simulationPosition);

                decision.Memo = memo;
                return decision;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ML Spike Prediction strategy for market {Market}", snapshot.MarketTicker);

                return new ActionDecision
                {
                    Type = ActionType.None,
                    Price = 0,
                    Quantity = 0,
                    Memo = $"ml_error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Retrieves recent snapshots for lagged feature computation.
        /// In production, this would use a proper snapshot cache service.
        /// </summary>
        private List<MarketSnapshot> GetRecentSnapshots(MarketSnapshot current)
        {
            // Placeholder: In real implementation, this would query recent snapshots
            // from a cache or database for the same market ticker
            return new List<MarketSnapshot> { current };
        }

        /// <summary>
        /// Makes trading decision based on ML prediction results.
        /// </summary>
        private ActionDecision MakeDecisionBasedOnPrediction(MarketSnapshot snapshot, SpikePredictionResult prediction, int simulationPosition)
        {
            // Use configurable prediction threshold
            if (prediction.Probability >= _config.PredictionThreshold)
            {
                // High probability of spike - take defensive action
                if (simulationPosition > 0)
                {
                    // Exit long position to avoid potential spike
                    return new ActionDecision
                    {
                        Type = ActionType.Exit,
                        Price = snapshot.BestNoBid, // Sell at best No bid
                        Quantity = Math.Abs(simulationPosition)
                    };
                }
                else if (simulationPosition < 0)
                {
                    // Exit short position to avoid potential spike
                    return new ActionDecision
                    {
                        Type = ActionType.Exit,
                        Price = snapshot.BestYesBid, // Buy at best Yes bid
                        Quantity = Math.Abs(simulationPosition)
                    };
                }
                else
                {
                    // No position - could consider entering based on spike direction
                    // For now, just signal caution
                    return new ActionDecision
                    {
                        Type = ActionType.None,
                        Price = 0,
                        Quantity = 0
                    };
                }
            }
            else
            {
                // Low probability of spike - normal operation
                return new ActionDecision
                {
                    Type = ActionType.None,
                    Price = 0,
                    Quantity = 0
                };
            }
        }

        /// <summary>
        /// Builds detailed memo with ML prediction results and market context.
        /// </summary>
        private string BuildMLMemo(MarketSnapshot snapshot, SpikePredictionResult prediction, List<MarketSnapshot> recentSnapshots)
        {
            var inv = CultureInfo.InvariantCulture;
            string F(double d) => double.IsNaN(d) || double.IsInfinity(d) ? "NaN" : d.ToString("0.##", inv);
            string I(int x) => x.ToString(inv);
            string YN(bool b) => b ? "Yes" : "No";

            var lines = new List<string>
            {
                $"ML Spike Prediction Strategy | Market: {snapshot.MarketTicker}",
                $"Timestamp: {snapshot.Timestamp:yyyy-MM-dd HH:mm:ss}",
                $"Spike Probability: {F(prediction.Probability * 100)}%",
                $"Predicted Spike: {YN(prediction.PredictedSpike)}",
                $"Threshold: {F(_config.PredictionThreshold * 100)}%",
                $"Decision: {(prediction.Probability >= _config.PredictionThreshold ? "HIGH_RISK" : "LOW_RISK")}",

                // Market context
                $"Best Yes Bid: {F(snapshot.BestYesBid)} | Best Yes Ask: {F(snapshot.BestYesAsk)}",
                $"Spread: {I(snapshot.YesSpread)} | Depth Ratio: {F(snapshot.TotalBidContracts_Yes > 0 ? (snapshot.DepthAtBestYesBid / (double)snapshot.TotalBidContracts_Yes) : 0)}",

                // Technical indicators
                $"RSI: {snapshot.RSI_Short?.ToString("0.#", inv) ?? "N/A"}",
                $"MACD: {snapshot.MACD_Medium.MACD?.ToString("0.###", inv) ?? "N/A"}",
                $"ATR: {snapshot.ATR_Medium?.ToString("0.###", inv) ?? "N/A"}",

                // Volume and activity
                $"Trade Rate/min: {F(snapshot.TradeRatePerMinute_Yes)}",
                $"Volume/min: {F(snapshot.TradeVolumePerMinute_Yes)}",
                $"Recent Volume (1h): {F(snapshot.RecentVolume_LastHour)}",

                // Model features (if available)
                $"Slope Short: {F(snapshot.YesBidSlopePerMinute_Short)}",
                $"Slope Medium: {F(snapshot.YesBidSlopePerMinute_Medium)}",

                // Recent snapshots count
                $"Recent Snapshots Used: {recentSnapshots.Count}"
            };

            return string.Join(Environment.NewLine, lines);
        }

        public override string ToJson()
        {
            return JsonSerializer.Serialize(new
            {
                type = "MLSpikePrediction",
                name = Name,
                weight = Weight,
                config = new
                {
                    spikeThreshold = _config.SpikeThreshold,
                    predictionWindow = _config.PredictionWindow,
                    lagMinutes = _config.LagMinutes,
                    predictionThreshold = _config.PredictionThreshold,
                    numberOfTrees = _config.NumberOfTrees,
                    minimumLeafSize = _config.MinimumLeafSize,
                    modelPath = _config.ModelPath
                }
            });
        }
    }
}
