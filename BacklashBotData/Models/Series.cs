namespace KalshiBotData.Models
{
/// <summary>Series</summary>
/// <summary>Series</summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
    public class Series
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
/// <summary>
/// </summary>
    {
/// <summary>
/// </summary>
/// <summary>
/// </summary>
        public required string series_ticker { get; set; }
/// <summary>
/// </summary>
        public required string frequency { get; set; }
/// <summary>
/// </summary>
        public required string title { get; set; }
/// <summary>Gets or sets the category.</summary>
/// <summary>Gets or sets the category.</summary>
        public required string category { get; set; }
/// <summary>Gets or sets the LastModifiedDate.</summary>
/// <summary>Gets or sets the CreatedDate.</summary>
        public required string contract_url { get; set; }
/// <summary>Gets or sets the Tags.</summary>
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
/// <summary>Gets or sets the Events.</summary>
        public required ICollection<SeriesTag> Tags { get; set; }
        public required ICollection<SeriesSettlementSource> SettlementSources { get; set; }

        public List<Event>? Events { get; set; }
    }
}
