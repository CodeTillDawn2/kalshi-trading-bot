namespace BacklashBotData.Models
{
    /// <summary>
    /// Represents a milestone in the Kalshi trading ecosystem.
    /// Milestones track important events, deadlines, or significant occurrences
    /// related to markets, events, or trading activities.
    /// </summary>
    public class Milestone
    {
        /// <summary>
        /// Gets or sets the unique identifier for this milestone.
        /// This serves as the primary key in the database.
        /// </summary>
        public required string Id { get; set; }

        /// <summary>
        /// Gets or sets the category of the milestone.
        /// </summary>
        public required string Category { get; set; }

        /// <summary>
        /// Gets or sets additional details about the milestone as JSON.
        /// This can contain structured data specific to the milestone type.
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// Gets or sets the end date of the milestone, if any.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Gets or sets the last time this milestone was updated.
        /// </summary>
        public DateTime LastUpdatedTs { get; set; }

        /// <summary>
        /// Gets or sets the notification message for the milestone.
        /// </summary>
        public required string NotificationMessage { get; set; }

        /// <summary>
        /// Gets or sets the list of event tickers directly related to the outcome of this milestone.
        /// </summary>
        public List<string> PrimaryEventTickers { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of event tickers related to this milestone.
        /// </summary>
        public List<string> RelatedEventTickers { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the source id of milestone if available.
        /// </summary>
        public string? SourceId { get; set; }

        /// <summary>
        /// Gets or sets the start date of the milestone.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Gets or sets the title of the milestone.
        /// </summary>
        public required string Title { get; set; }

        /// <summary>
        /// Gets or sets the type of the milestone.
        /// </summary>
        public required string Type { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this milestone was first created in the system.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this milestone was last modified.
        /// </summary>
        public DateTime LastModifiedDate { get; set; }
    }
}