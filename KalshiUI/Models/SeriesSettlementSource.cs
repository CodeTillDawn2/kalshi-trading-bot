namespace KalshiUI.Models
{
    public class SeriesSettlementSource
    {
        public string series_ticker { get; set; }
        public string name { get; set; }
        public string url { get; set; }

        // Navigation property
        public Series Series { get; set; }
    }

}
