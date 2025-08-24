using System.Text.Json.Serialization;

namespace SmokehouseDTOs.KalshiAPI
{

    /// <summary>
    /// Represents the operational status of the Kalshi exchange and trading system.
    /// </summary>
    public class ExchangeStatus
    {
        [JsonPropertyName("exchange_active")] public bool exchange_active { get; set; }
        [JsonPropertyName("trading_active")] public bool trading_active { get; set; }
    }
}
