namespace BacklashDTOs.Data
{
    /// <summary>
    /// Data transfer object for series tag data.
    /// </summary>
    public class SeriesTagDTO
    {
        /// <summary>
        /// Gets or sets the series ticker symbol.
        /// </summary>
        public string? series_ticker { get; set; }

        /// <summary>
        /// Gets or sets the tag value.
        /// </summary>
        public string? tag { get; set; }
    }
}
