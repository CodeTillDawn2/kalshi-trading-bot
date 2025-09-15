
namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents the response containing an event from the Kalshi API.
    /// </summary>
    public class EventResponse
    {
        /// <summary>
        /// Gets or sets the Kalshi event.
        /// </summary>
        public KalshiEvent? Event { get; set; }
    }
}
