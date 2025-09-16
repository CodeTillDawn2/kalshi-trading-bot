namespace KalshiBotData.Models
{
    /// <summary>
    /// Represents strategy configuration and metadata for trading strategies in the Kalshi bot system.
    /// This entity stores the definition and parameters of trading strategies, allowing for
    /// flexible strategy management and execution. Strategies can be stored as JSON configurations
    /// and instantiated dynamically based on market conditions and trading requirements.
    /// </summary>
    public class StratData
    {
        /// <summary>
        /// Gets or sets the unique identifier for this strategy.
        /// This serves as the primary key in the database.
        /// </summary>
        public int StratID { get; set; }

        /// <summary>
        /// Gets or sets the human-readable name of this strategy.
        /// This provides a friendly identifier for the strategy (e.g., "BollingerBreakout", "RSIMomentum").
        /// </summary>
        public string StratName { get; set; }

        /// <summary>
        /// Gets or sets the type identifier for this strategy.
        /// This categorizes the strategy (e.g., 1=Momentum, 2=MeanReversion, 3=Breakout).
        /// </summary>
        public int StratType { get; set; }

        /// <summary>
        /// Gets or sets the raw JSON configuration data for this strategy.
        /// This contains the complete strategy definition including parameters, rules, and settings.
        /// </summary>
        public string RawJSON { get; set; }
    }
}
