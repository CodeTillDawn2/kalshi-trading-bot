using System.Text.Json.Serialization;

namespace BacklashDTOs.KalshiAPI
{
    public class ApiError
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = "";

        [JsonPropertyName("details")]
        public string Details { get; set; } = "";

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";

        [JsonPropertyName("service")]
        public string Service { get; set; } = "";
    }
}