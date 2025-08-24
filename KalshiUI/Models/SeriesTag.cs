namespace KalshiUI.Models
{
    public class SeriesTag
    {
        public string series_ticker { get; set; }
        public string tag { get; set; }

        // Navigation property
        public Series Series { get; set; }
    }
}
