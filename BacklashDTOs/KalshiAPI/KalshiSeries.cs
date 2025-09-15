using BacklashDTOs.Data;
using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents a Kalshi series.
    /// </summary>
    public class KalshiSeries
    {
        /// <summary>
        /// Gets or sets the category.
        /// </summary>
        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the contract URL.
        /// </summary>
        [JsonPropertyName("contract_url")]
        public string ContractUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the frequency.
        /// </summary>
        [JsonPropertyName("frequency")]
        public string Frequency { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of settlement sources.
        /// </summary>
        [JsonPropertyName("settlement_sources")]
        public List<SeriesSettlementSourceDTO> SettlementSources { get; set; } = new List<SeriesSettlementSourceDTO>();

        /// <summary>
        /// Gets or sets the list of tags.
        /// </summary>
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the ticker.
        /// </summary>
        [JsonPropertyName("ticker")]
        public string Ticker { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
    }
}
