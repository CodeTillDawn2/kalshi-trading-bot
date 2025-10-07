namespace BacklashBotData.Models
{
    /// <summary>
    /// Represents a system announcement or notification in the Kalshi trading bot ecosystem.
    /// This entity stores important messages, alerts, or updates that need to be communicated
    /// to users or other system components. Announcements can have different types and statuses
    /// to categorize their importance and delivery state.
    /// </summary>
    public class Announcement
    {
        /// <summary>
        /// Gets or sets the unique identifier for this announcement.
        /// This serves as the primary key in the database.
        /// </summary>
        public long AnnouncementID { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this announcement should be delivered or was delivered.
        /// This helps in scheduling and tracking the timing of announcements.
        /// </summary>
        public DateTime DeliveryTime { get; set; }

        /// <summary>
        /// Gets or sets the actual message content of the announcement.
        /// This contains the text that will be displayed to users or processed by other systems.
        /// </summary>
        public required string Message { get; set; }

        /// <summary>
        /// Gets or sets the current status of the announcement (e.g., "pending", "delivered", "read").
        /// This tracks the lifecycle state of the announcement.
        /// </summary>
        public required string Status { get; set; }

        /// <summary>
        /// Gets or sets the type or category of the announcement (e.g., "system", "market", "alert").
        /// This helps in filtering and prioritizing different types of announcements.
        /// </summary>
        public required string Type { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this announcement was first created in the system.
        /// This is used for auditing and tracking the age of announcements.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this announcement was last modified.
        /// This helps track updates to announcement content or status.
        /// </summary>
        public DateTime LastModifiedDate { get; set; }
    }
}
