
namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents a price level in the order book with price and resting contract details.
    /// </summary>
    public class PriceLevel
    {
        public int Price { get; set; }

        public int RestingContracts { get; set; }
    }
}
