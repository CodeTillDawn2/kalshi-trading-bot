namespace BacklashBotData.Models
{
    /// <summary>
    /// Represents a tag or category label associated with a trading series.
    /// This entity provides a flexible tagging system for categorizing and organizing
    /// trading series based on various characteristics such as market type, theme,
    /// or other classification criteria. Tags enable efficient filtering and discovery
    /// of series with similar attributes.
    /// </summary>
    public class SeriesTag
    {
        /// <summary>
        /// Gets or sets the series ticker identifier that this tag is associated with.
        /// This links the tag to its parent series.
        /// </summary>
        public string series_ticker { get; set; }

        /// <summary>
        /// Gets or sets the tag value or label for this series.
        /// This contains the actual tag text (e.g., "politics", "sports", "finance").
        /// </summary>
        public string tag { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the associated Series entity.
        /// This provides access to the parent series and related tag information.
        /// </summary>
        public Series Series { get; set; }
    }
}
