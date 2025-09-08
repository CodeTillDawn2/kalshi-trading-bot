namespace KalshiBotData.Models
{
    public class Series
    {
        public required string series_ticker { get; set; }
        public required string frequency { get; set; }
        public required string title { get; set; }
        public required string category { get; set; }
        public required string contract_url { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public required ICollection<SeriesTag> Tags { get; set; }
        public required ICollection<SeriesSettlementSource> SettlementSources { get; set; }

        public List<Event>? Events { get; set; }
    }
}
