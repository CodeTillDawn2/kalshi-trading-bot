namespace KalshiBotData.Models
{
    public class Event
    {
        public required string event_ticker { get; set; }
        public required string series_ticker { get; set; }
        public required string title { get; set; }
        public string? sub_title { get; set; }
        public required string collateral_return_type { get; set; }
        public bool mutually_exclusive { get; set; }
        public required string category { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public List<Market>? Markets { get; set; }
        public Series? Series { get; set; }

    }
}
