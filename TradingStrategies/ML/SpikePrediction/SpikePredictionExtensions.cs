using BacklashDTOs;

/// <summary>
/// Extension methods for spike prediction integration.
/// </summary>
namespace TradingStrategies.ML.SpikePrediction
{
    /// <summary>
    /// Extension methods for easier integration with MarketSnapshot.
    /// </summary>
    public static class SpikePredictionExtensions
    {
        /// <summary>
        /// Extension method to predict spike probability for MarketSnapshot using recent historical context.
        /// Requires list of recent snapshots (last LagMinutes) for accurate lagged feature computation.
        /// Focuses exclusively on "Yes" prices as per Kalshi market mechanics.
        /// </summary>
        public static SpikePredictionResult PredictSpike(this MarketSnapshot snapshot, List<MarketSnapshot> recentSnapshots, SpikePredictionModel model)
        {
            return model.PredictSpike(snapshot, recentSnapshots);
        }
    }
}
