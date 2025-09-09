namespace BacklashDTOs.Data
{
    public class SeriesDTO
    {
        public string series_ticker { get; set; }
        public string frequency { get; set; }
        public string title { get; set; }
        public string category { get; set; }
        public string contract_url { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public ICollection<SeriesTagDTO>? Tags { get; set; }
        public ICollection<SeriesSettlementSourceDTO>? SettlementSources { get; set; }

    }
}
