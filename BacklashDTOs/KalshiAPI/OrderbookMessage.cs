using System.Text.Json;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents a message containing order book data, either as a snapshot or delta update.
    /// </summary>
    public class OrderbookMessage
    {
        /// <summary>
        /// Gets or sets the session ID.
        /// </summary>
        public int Sid { get; set; }

        /// <summary>
        /// Gets or sets the sequence number.
        /// </summary>
        public long Seq { get; set; }

        /// <summary>
        /// Gets or sets the offer type ("SNP" or "DEL").
        /// </summary>
        public string? OfferType { get; set; } // "SNP" or "DEL"

        /// <summary>
        /// Gets or sets the market ticker.
        /// </summary>
        public string? MarketTicker { get; set; }

        /// <summary>
        /// Gets or sets the list of yes orders for snapshot.
        /// </summary>
        public List<PriceLevel>? YesOrders { get; set; } // For snapshot

        /// <summary>
        /// Gets or sets the list of no orders for snapshot.
        /// </summary>
        public List<PriceLevel>? NoOrders { get; set; }  // For snapshot

        /// <summary>
        /// Gets or sets the price for delta.
        /// </summary>
        public int? Price { get; set; }                 // For delta

        /// <summary>
        /// Gets or sets the delta for delta.
        /// </summary>
        public int? Delta { get; set; }                 // For delta

        /// <summary>
        /// Gets or sets the side for delta.
        /// </summary>
        public string? Side { get; set; }                // For delta

        /// <summary>
        /// Initializes a new instance of the OrderbookMessage class from JSON data.
        /// </summary>
        /// <param name="data">The JSON data to parse.</param>
        /// <param name="offerType">The type of offer ("SNP" or "DEL").</param>
        public OrderbookMessage(JsonElement data, string offerType)
        {
            // Handle missing properties gracefully
            Sid = data.TryGetProperty("sid", out var sidProp) ? sidProp.GetInt32() : 0;
            Seq = data.TryGetProperty("seq", out var seqProp) ? seqProp.GetInt64() : 0;
            OfferType = offerType;

            // Get the msg property which contains the actual orderbook data
            if (data.TryGetProperty("msg", out var msg))
            {
                MarketTicker = msg.TryGetProperty("market_ticker", out var tickerProp)
                    ? tickerProp.GetString() ?? string.Empty
                    : string.Empty;

                if (offerType == "SNP")
                {
                    YesOrders = new List<PriceLevel>();
                    NoOrders = new List<PriceLevel>();

                    if (msg.TryGetProperty("yes", out var yesOrders) && yesOrders.ValueKind == JsonValueKind.Array)
                    {
                        YesOrders.AddRange(yesOrders.EnumerateArray().Select(p => new PriceLevel
                        {
                            Price = p[0].GetInt32(),
                            RestingContracts = p[1].GetInt32()
                        }));
                    }

                    if (msg.TryGetProperty("no", out var noOrders) && noOrders.ValueKind == JsonValueKind.Array)
                    {
                        NoOrders.AddRange(noOrders.EnumerateArray().Select(p => new PriceLevel
                        {
                            Price = p[0].GetInt32(),
                            RestingContracts = p[1].GetInt32()
                        }));
                    }
                }
                else if (offerType == "DEL") // delta
                {
                    Price = msg.TryGetProperty("price", out var priceProp) ? priceProp.GetInt32() : null;
                    Delta = msg.TryGetProperty("delta", out var deltaProp) ? deltaProp.GetInt32() : null;
                    Side = msg.TryGetProperty("side", out var sideProp) ? sideProp.GetString() ?? string.Empty : string.Empty;
                }
            }
            else
            {
                // Fallback if msg property doesn't exist
                MarketTicker = string.Empty;
                if (offerType == "SNP")
                {
                    YesOrders = new List<PriceLevel>();
                    NoOrders = new List<PriceLevel>();
                }
            }
        }
    }


}
