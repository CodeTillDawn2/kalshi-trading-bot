using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using TradingSimulator.Strategies;

namespace TradingSimulator
{
    /// <summary>
    /// Represents a point in time with a price value and optional memo for market data visualization.
    /// This record is used throughout the trading simulator to store timestamped price information
    /// with associated metadata for charting and analysis purposes.
    /// </summary>
    public record PricePoint(DateTime Date, double Price, string? Memo = null)
    {
        /// <summary>
        /// Gets the memo split into individual parts, trimmed and filtered for empty entries.
        /// This property provides easy access to parsed memo components for analysis.
        /// Enhanced with null safety and validation.
        /// </summary>
        public List<string> MemoParts
        {
            get
            {
                if (Price < 0) throw new InvalidOperationException("Price cannot be negative.");
                return Memo == null
                    ? new List<string>()
                    : Memo.Split(',', StringSplitOptions.RemoveEmptyEntries)
                          .Select(part => part.Trim())
                          .Where(part => !string.IsNullOrWhiteSpace(part))
                          .ToList();
            }
        }
    }

    /// <summary>
    /// Represents a trade point with timestamp, price, and trading decision.
    /// This record encapsulates the essential data for a trading action in the simulator.
    /// </summary>
    public record TradePoint(DateTime Date, double Price, TradingDecisionEnum Decision);

    /// <summary>
    /// Contains cached market data for a specific market, including profit/loss, position information,
    /// and various lists of price points representing different market events and states.
    /// This class serves as the primary data structure for storing processed market simulation results
    /// that can be serialized to JSON for persistence and later visualization in the GUI.
    /// Enhanced with validation, deep cloning, and serialization controls.
    /// </summary>
    public class CachedMarketData : ICloneable
    {
        /// <summary>
        /// Gets or sets the market ticker symbol this cached data represents.
        /// </summary>
        [JsonPropertyName("market")]
        public string Market { get; set; }

        /// <summary>
        /// Gets or sets the final profit and loss from the market simulation.
        /// </summary>
        [JsonPropertyName("pnl")]
        public double PnL { get; set; }

        /// <summary>
        /// Gets or sets the final simulated position at the end of the market processing.
        /// </summary>
        [JsonPropertyName("simulatedPosition")]
        public int SimulatedPosition { get; set; }

        /// <summary>
        /// Gets or sets the average cost of the position.
        /// </summary>
        [JsonPropertyName("averageCost")]
        public double AverageCost { get; set; }

        /// <summary>
        /// Gets or sets the list of bid price points over time.
        /// </summary>
        [JsonPropertyName("bidPoints")]
        public List<PricePoint> BidPoints { get; set; }

        /// <summary>
        /// Gets or sets the list of ask price points over time.
        /// </summary>
        [JsonPropertyName("askPoints")]
        public List<PricePoint> AskPoints { get; set; }

        /// <summary>
        /// Gets or sets the list of buy trade points where positions were entered.
        /// </summary>
        [JsonPropertyName("buyPoints")]
        public List<PricePoint> BuyPoints { get; set; }

        /// <summary>
        /// Gets or sets the list of sell trade points where positions were exited.
        /// </summary>
        [JsonPropertyName("sellPoints")]
        public List<PricePoint> SellPoints { get; set; }

        /// <summary>
        /// Gets or sets the list of exit points where positions were closed.
        /// </summary>
        [JsonPropertyName("exitPoints")]
        public List<PricePoint> ExitPoints { get; set; }

        /// <summary>
        /// Gets or sets the list of event points marking significant market events.
        /// </summary>
        [JsonPropertyName("eventPoints")]
        public List<PricePoint> EventPoints { get; set; }

        /// <summary>
        /// Gets or sets the list of points where long positions were intended but not executed.
        /// </summary>
        [JsonPropertyName("intendedLongPoints")]
        public List<PricePoint> IntendedLongPoints { get; set; }

        /// <summary>
        /// Gets or sets the list of points where short positions were intended but not executed.
        /// </summary>
        [JsonPropertyName("intendedShortPoints")]
        public List<PricePoint> IntendedShortPoints { get; set; }

        /// <summary>
        /// Gets or sets the list of position size points over time.
        /// </summary>
        [JsonPropertyName("positionPoints")]
        public List<PricePoint> PositionPoints { get; set; }

        /// <summary>
        /// Gets or sets the list of average cost points over time.
        /// </summary>
        [JsonPropertyName("averageCostPoints")]
        public List<PricePoint> AverageCostPoints { get; set; }

        /// <summary>
        /// Gets or sets the list of resting orders count points over time.
        /// </summary>
        [JsonPropertyName("restingOrdersPoints")]
        public List<PricePoint> RestingOrdersPoints { get; set; }

        /// <summary>
        /// Gets or sets the list of points where orderbook discrepancies were detected.
        /// </summary>
        [JsonPropertyName("discrepancyPoints")]
        public List<PricePoint> DiscrepancyPoints { get; set; } = new List<PricePoint>();

        /// <summary>
        /// Gets or sets the list of points where candlestick patterns were detected.
        /// </summary>
        [JsonPropertyName("patternPoints")]
        public List<PricePoint> PatternPoints { get; set; } = new List<PricePoint>();

        /// <summary>
        /// Initializes a new instance of CachedMarketData with validation.
        /// </summary>
        public CachedMarketData()
        {
            // Initialize lists to prevent null reference exceptions
            BidPoints = new List<PricePoint>();
            AskPoints = new List<PricePoint>();
            BuyPoints = new List<PricePoint>();
            SellPoints = new List<PricePoint>();
            ExitPoints = new List<PricePoint>();
            EventPoints = new List<PricePoint>();
            IntendedLongPoints = new List<PricePoint>();
            IntendedShortPoints = new List<PricePoint>();
            PositionPoints = new List<PricePoint>();
            AverageCostPoints = new List<PricePoint>();
            RestingOrdersPoints = new List<PricePoint>();
        }

        /// <summary>
        /// Validates the data integrity of the CachedMarketData instance.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Market))
                throw new InvalidOperationException("Market cannot be null or empty.");
            if (AverageCost < 0)
                throw new InvalidOperationException("AverageCost cannot be negative.");
            // Add more validations as needed
        }

        /// <summary>
        /// Creates a deep clone of the CachedMarketData instance.
        /// </summary>
        public object Clone()
        {
            var clone = new CachedMarketData
            {
                Market = this.Market,
                PnL = this.PnL,
                SimulatedPosition = this.SimulatedPosition,
                AverageCost = this.AverageCost,
                BidPoints = this.BidPoints.ConvertAll(p => p with { }),
                AskPoints = this.AskPoints.ConvertAll(p => p with { }),
                BuyPoints = this.BuyPoints.ConvertAll(p => p with { }),
                SellPoints = this.SellPoints.ConvertAll(p => p with { }),
                ExitPoints = this.ExitPoints.ConvertAll(p => p with { }),
                EventPoints = this.EventPoints.ConvertAll(p => p with { }),
                IntendedLongPoints = this.IntendedLongPoints.ConvertAll(p => p with { }),
                IntendedShortPoints = this.IntendedShortPoints.ConvertAll(p => p with { }),
                PositionPoints = this.PositionPoints.ConvertAll(p => p with { }),
                AverageCostPoints = this.AverageCostPoints.ConvertAll(p => p with { }),
                RestingOrdersPoints = this.RestingOrdersPoints.ConvertAll(p => p with { }),
                DiscrepancyPoints = this.DiscrepancyPoints.ConvertAll(p => p with { }),
                PatternPoints = this.PatternPoints.ConvertAll(p => p with { })
            };
            return clone;
        }

        /// <summary>
        /// Serializes the CachedMarketData to JSON with performance metrics.
        /// </summary>
        public string SerializeWithMetrics(out TimeSpan serializationTime)
        {
            var stopwatch = Stopwatch.StartNew();
            string json = JsonSerializer.Serialize(this);
            stopwatch.Stop();
            serializationTime = stopwatch.Elapsed;
            return json;
        }

        /// <summary>
        /// Deserializes JSON to CachedMarketData with performance metrics.
        /// </summary>
        public static CachedMarketData DeserializeWithMetrics(string json, out TimeSpan deserializationTime)
        {
            var stopwatch = Stopwatch.StartNew();
            var data = JsonSerializer.Deserialize<CachedMarketData>(json);
            stopwatch.Stop();
            deserializationTime = stopwatch.Elapsed;
            return data ?? throw new JsonException("Failed to deserialize CachedMarketData.");
        }
    }

}
