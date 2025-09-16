
namespace KalshiBotData.Models
{
    /// <summary>
    /// Represents a category or type classification for markets in the Kalshi trading system.
    /// This entity provides a way to group and categorize different types of trading markets
    /// based on their characteristics, such as binary outcome markets, range markets, or
    /// other specialized market types. Market types help in filtering, analysis, and
    /// applying different trading strategies based on market characteristics.
    /// </summary>
    public class MarketType
    {
        /// <summary>
        /// Gets or sets the unique identifier for this market type.
        /// This serves as the primary key in the database.
        /// </summary>
        public int MarketTypeID { get; set; }

        /// <summary>
        /// Gets or sets the descriptive name or label for this market type.
        /// This provides a human-readable description of the market category
        /// (e.g., "Binary Outcome", "Numeric Range", "Date-based").
        /// </summary>
        public required string MarketTypeDescription { get; set; }
    }
}
