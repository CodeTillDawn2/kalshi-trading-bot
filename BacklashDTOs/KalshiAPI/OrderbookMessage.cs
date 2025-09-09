using System.Text.Json;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents a message containing order book data, either as a snapshot or delta update.
    /// </summary>
    public class OrderbookMessage
    {
        public int Sid { get; set; }

        public long Seq { get; set; }

        public string OfferType { get; set; } // "SNP" or "DEL"

        public string MarketTicker { get; set; }

        public List<PriceLevel> YesOrders { get; set; } // For snapshot

        public List<PriceLevel> NoOrders { get; set; }  // For snapshot

        public int? Price { get; set; }                 // For delta

        public int? Delta { get; set; }                 // For delta

        public string Side { get; set; }                // For delta

        /// <summary>
        /// Initializes a new instance of the OrderbookMessage class from JSON data.
        /// </summary>
        /// <param name="data">The JSON data to parse.</param>
        /// <param name="offerType">The type of offer ("SNP" or "DEL").</param>
        public OrderbookMessage(JsonElement data, string offerType)
        {
            Sid = data.GetProperty("sid").GetInt32();
            Seq = data.GetProperty("seq").GetInt64();
            OfferType = offerType;
            var msg = data.GetProperty("msg");
            MarketTicker = msg.GetProperty("market_ticker").GetString() ?? string.Empty;

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
            else // DEL
            {
                Price = msg.GetProperty("price").GetInt32();
                Delta = msg.GetProperty("delta").GetInt32();
                Side = msg.GetProperty("side").GetString() ?? string.Empty;
            }
        }
    }


}
