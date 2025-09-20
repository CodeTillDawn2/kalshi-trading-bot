using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    /// <summary>
    /// Represents an error response from the Kalshi API.
    /// </summary>
    public class ApiError
    {
        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        [JsonPropertyName("code")]
        public string Code { get; set; } = "";

        /// <summary>
        /// Gets or sets the error details.
        /// </summary>
        [JsonPropertyName("details")]
        public string Details { get; set; } = "";

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = "";

        /// <summary>
        /// Gets or sets the service that caused the error.
        /// </summary>
        [JsonPropertyName("service")]
        public string Service { get; set; } = "";
    }
}
