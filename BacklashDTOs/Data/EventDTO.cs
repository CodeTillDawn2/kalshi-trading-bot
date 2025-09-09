namespace BacklashDTOs.Data
{
    public class EventDTO
    {
        public string event_ticker { get; set; }
        public string series_ticker { get; set; }
        public string title { get; set; }
        public string? sub_title { get; set; }
        public string collateral_return_type { get; set; }
        public bool mutually_exclusive { get; set; }
        public string category { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public SeriesDTO? Series { get; set; }
    }
}
