using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents the response containing the total resting order value from the Kalshi API.
    /// </summary>
    public class TotalRestingOrderValueResponse
    {
        /// <summary>
        /// Gets or sets the total value.
        /// </summary>
        [JsonPropertyName("total_value")]
        public int TotalValue { get; set; }
    }
}