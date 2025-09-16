namespace KalshiBotData.Models
{
/// <summary>Event</summary>
/// <summary>Event</summary>
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
    public class Event
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
        public required string event_ticker { get; set; }
/// <summary>
/// </summary>
        public required string series_ticker { get; set; }
/// <summary>
/// </summary>
        public required string title { get; set; }
/// <summary>Gets or sets the sub_title.</summary>
/// <summary>Gets or sets the sub_title.</summary>
        public string? sub_title { get; set; }
/// <summary>Gets or sets the category.</summary>
/// <summary>Gets or sets the mutually_exclusive.</summary>
        public required string collateral_return_type { get; set; }
/// <summary>Gets or sets the Markets.</summary>
/// <summary>Gets or sets the CreatedDate.</summary>
        public bool mutually_exclusive { get; set; }
/// <summary>Gets or sets the Markets.</summary>
        public required string category { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public List<Market>? Markets { get; set; }
        public Series? Series { get; set; }

    }
}
