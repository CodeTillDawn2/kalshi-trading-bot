using Microsoft.ML.Data;

/// <summary>
/// Data structures for the spike prediction ML model.
/// </summary>
namespace TradingStrategies.ML.SpikePrediction
{
    /// <summary>
    /// Prediction result structure.
    /// </summary>
    public class SpikePredictionResult
    {
        [ColumnName("PredictedLabel")] public bool PredictedSpike { get; set; }
        public float Probability { get; set; }
        public float Score { get; set; }
    }

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
    /// Configuration for spike prediction parameters.
    /// </summary>
    public class SpikePredictionConfig
    {
        public double SpikeThreshold { get; set; } = 0.15; // 15% spike
        public TimeSpan PredictionWindow { get; set; } = TimeSpan.FromMinutes(10);
        public int LagMinutes { get; set; } = 5; // Look back 5 minutes for features
        public int MaxFeatures { get; set; } = 12; // Limit to 12 features
        public string ModelPath { get; set; } = "spike_prediction_model.zip";
        public int NumberOfTrees { get; set; } = 100;
        public int MinimumLeafSize { get; set; } = 10;
        public int MaximumDepth { get; set; } = 10; // Maximum depth for random forest trees
        public double PredictionThreshold { get; set; } = 0.7; // Threshold for decision making
    }
}
