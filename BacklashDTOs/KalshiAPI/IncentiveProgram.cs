using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents an incentive program from the Kalshi API.
    /// </summary>
    public class IncentiveProgram
    {
        /// <summary>
        /// Gets or sets the end date of the incentive program.
        /// </summary>
        [JsonPropertyName("end_date")]
        public string EndDate { get; set; } = "";

        /// <summary>
        /// Gets or sets the ID of the incentive program.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        /// <summary>
        /// Gets or sets the type of incentive.
        /// </summary>
        [JsonPropertyName("incentive_type")]
        public string IncentiveType { get; set; } = "";

        /// <summary>
        /// Gets or sets the market ticker.
        /// </summary>
        [JsonPropertyName("market_ticker")]
        public string MarketTicker { get; set; } = "";

        /// <summary>
        /// Gets or sets a value indicating whether the incentive has been paid out.
        /// </summary>
        [JsonPropertyName("paid_out")]
        public bool PaidOut { get; set; }

        /// <summary>
        /// Gets or sets the period reward.
        /// </summary>
        [JsonPropertyName("period_reward")]
        public int PeriodReward { get; set; }

        /// <summary>
        /// Gets or sets the start date of the incentive program.
        /// </summary>
        [JsonPropertyName("start_date")]
        public string StartDate { get; set; } = "";
    }
}