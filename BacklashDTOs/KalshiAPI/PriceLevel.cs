
namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents a price level in the order book with price and resting contract details.
    /// </summary>
    public class PriceLevel
    {
        /// <summary>
        /// Gets or sets the price.
        /// </summary>
        public int Price { get; set; }

        /// <summary>
        /// Gets or sets the number of resting contracts.
        /// </summary>
        public int RestingContracts { get; set; }
    }
}
