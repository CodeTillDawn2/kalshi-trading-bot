
using BacklashDTOs.Converters;
using System.Text.Json.Serialization;

namespace BacklashBotData.Models
{
    /// <summary>
    /// Represents a comprehensive snapshot of market state and trading activity at a specific point in time.
    /// This entity captures detailed market data including pricing, volume, velocity metrics, and order book
    /// information for analysis, backtesting, and trading strategy evaluation. Snapshots serve as the
    /// primary data structure for understanding market behavior and making informed trading decisions.
    /// </summary>
    public class Snapshot
    {
        /// <summary>
        /// Gets or sets the market ticker identifier for this snapshot.
        /// This identifies the specific market contract that this snapshot represents.
        /// </summary>
        public string MarketTicker { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this snapshot was captured.
        /// This provides the exact point in time that this market state represents.
        /// </summary>
        public DateTime SnapshotDate { get; set; }

        /// <summary>
        /// Gets or sets the version of the JSON schema used for this snapshot.
        /// This ensures compatibility and proper deserialization of snapshot data.
        /// </summary>
        public int JSONSchemaVersion { get; set; }

        /// <summary>
        /// Gets or sets whether the change metrics in this snapshot are considered mature.
        /// Mature metrics indicate that the snapshot has sufficient historical data for reliable analysis.
        /// </summary>
        public bool ChangeMetricsMature { get; set; }

        /// <summary>
        /// Gets or sets the size of the trading position at the time of this snapshot.
        /// This represents the net exposure in the market contract.
        /// </summary>
        public int PositionSize { get; set; }

        /// <summary>
        /// Gets or sets the velocity (rate of change) of the top yes bid price per minute.
        /// This measures how quickly the best yes bid is changing.
        /// </summary>
        public double? VelocityPerMinute_Top_Yes_Bid { get; set; }

        /// <summary>
        /// Gets or sets the velocity (rate of change) of the top no bid price per minute.
        /// This measures how quickly the best no bid is changing.
        /// </summary>
        public double? VelocityPerMinute_Top_No_Bid { get; set; }

        /// <summary>
        /// Gets or sets the velocity (rate of change) of the bottom yes bid price per minute.
        /// This measures how quickly the worst yes bid is changing.
        /// </summary>
        public double? VelocityPerMinute_Bottom_Yes_Bid { get; set; }

        /// <summary>
        /// Gets or sets the velocity (rate of change) of the bottom no bid price per minute.
        /// This measures how quickly the worst no bid is changing.
        /// </summary>
        public double? VelocityPerMinute_Bottom_No_Bid { get; set; }

        /// <summary>
        /// Gets or sets the order volume for yes bids in the recent period.
        /// This represents the total number of yes orders placed.
        /// </summary>
        public double? OrderVolume_Yes_Bid { get; set; }

        /// <summary>
        /// Gets or sets the order volume for no bids in the recent period.
        /// This represents the total number of no orders placed.
        /// </summary>
        public double? OrderVolume_No_Bid { get; set; }

        /// <summary>
        /// Gets or sets the trade volume for yes positions in the recent period.
        /// This represents the total number of yes contracts traded.
        /// </summary>
        public double? TradeVolume_Yes { get; set; }

        /// <summary>
        /// Gets or sets the trade volume for no positions in the recent period.
        /// This represents the total number of no contracts traded.
        /// </summary>
        public double? TradeVolume_No { get; set; }

        /// <summary>
        /// Gets or sets the average trade size for yes positions.
        /// This represents the typical number of contracts per yes trade.
        /// </summary>
        public double? AverageTradeSize_Yes { get; set; }

        /// <summary>
        /// Gets or sets the average trade size for no positions.
        /// This represents the typical number of contracts per no trade.
        /// </summary>
        public double? AverageTradeSize_No { get; set; }

        /// <summary>
        /// Gets or sets the market type identifier for this snapshot.
        /// This categorizes the market (e.g., binary outcome, range, etc.).
        /// </summary>
        public int? MarketTypeID { get; set; }

        /// <summary>
        /// Gets or sets whether this snapshot has been validated for data integrity.
        /// Validated snapshots are considered reliable for analysis and trading decisions.
        /// </summary>
        public bool? IsValidated { get; set; }

        /// <summary>
        /// Gets or sets the raw JSON data containing the complete snapshot information.
        /// This field uses a custom JSON converter to preserve the original data format.
        /// </summary>
        [JsonPropertyName("r")]
        [JsonConverter(typeof(RawJsonStringConverter))]
        public string RawJSON { get; set; }

        /// <summary>
        /// Gets or sets the brain instance that captured this snapshot.
        /// This identifies which trading bot instance created this snapshot.
        /// </summary>
        public string? BrainInstance { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the associated Market entity.
        /// This provides access to the market details and related trading information.
        /// </summary>
        public Market Market { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the associated MarketType entity.
        /// This provides access to the market type classification and related data.
        /// </summary>
        public MarketType MarketType { get; set; }
    }
}
