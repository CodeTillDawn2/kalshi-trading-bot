namespace KalshiUI.Models
{
    public class Series
    {
        public string series_ticker { get; set; }
        public string frequency { get; set; }
        public string title { get; set; }
        public string category { get; set; }
        public string contract_url { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public ICollection<SeriesTag> Tags { get; set; }
        public ICollection<SeriesSettlementSource> SettlementSources { get; set; }

        public List<Event> Events { get; set; }
    }
}
