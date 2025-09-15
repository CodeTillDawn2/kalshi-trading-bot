using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{

    /// <summary>
    /// Represents the operational status of the Kalshi exchange and trading system.
    /// </summary>
    public class ExchangeStatus
    {
        /// <summary>
        /// Gets or sets a value indicating whether the exchange is active.
        /// </summary>
        [JsonPropertyName("exchange_active")] public bool exchange_active { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether trading is active.
        /// </summary>
        [JsonPropertyName("trading_active")] public bool trading_active { get; set; }
    }
}
