
namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents the response containing a list of orders from the Kalshi API.
    /// </summary>
    public class OrdersResponse
    {
        /// <summary>
        /// Gets or sets the pagination cursor for retrieving the next set of orders.
        /// </summary>
        public string? Cursor { get; set; }

        /// <summary>
        /// Gets or sets the list of orders retrieved in the response.
        /// </summary>
        public List<OrderApi> Orders { get; set; } = new List<OrderApi>();
    }
}
