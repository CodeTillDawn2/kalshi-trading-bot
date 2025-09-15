using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents a Kalshi event.
    /// </summary>
    public class KalshiEvent
    {
        /// <summary>
        /// Gets or sets the category of the event.
        /// </summary>
        [JsonPropertyName("category")]
        public string? Category { get; set; }

        /// <summary>
        /// Gets or sets the collateral return type.
        /// </summary>
        [JsonPropertyName("collateral_return_type")]
        public string? CollateralReturnType { get; set; }

        /// <summary>
        /// Gets or sets the event ticker.
        /// </summary>
        [JsonPropertyName("event_ticker")]
        public string? EventTicker { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the event is mutually exclusive.
        /// </summary>
        [JsonPropertyName("mutually_exclusive")]
        public bool MutuallyExclusive { get; set; }

        /// <summary>
        /// Gets or sets the series ticker.
        /// </summary>
        [JsonPropertyName("series_ticker")]
        public string? SeriesTicker { get; set; }

        /// <summary>
        /// Gets or sets the strike date.
        /// </summary>
        [JsonPropertyName("strike_date")]
        public DateTime? StrikeDate { get; set; }

        /// <summary>
        /// Gets or sets the strike period.
        /// </summary>
        [JsonPropertyName("strike_period")]
        public string? StrikePeriod { get; set; }

        /// <summary>
        /// Gets or sets the subtitle.
        /// </summary>
        [JsonPropertyName("sub_title")]
        public string? SubTitle { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the list of markets. Only populated if withNestedMarkets=true.
        /// </summary>
        [JsonPropertyName("markets")]
        public List<KalshiMarket>? Markets { get; set; } // Only populated if withNestedMarkets=true
    }
}

