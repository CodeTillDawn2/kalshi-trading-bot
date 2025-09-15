namespace BacklashDTOs.Data
{
    /// <summary>
    /// Data transfer object for announcements.
    /// </summary>
    public class AnnouncementDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier for the announcement.
        /// </summary>
        public long AnnouncementID { get; set; }

        /// <summary>
        /// Gets or sets the delivery time of the announcement.
        /// </summary>
        public DateTime DeliveryTime { get; set; }

        /// <summary>
        /// Gets or sets the message content of the announcement.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets the status of the announcement.
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets the type of the announcement.
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Gets or sets the creation date of the announcement.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the last modified date of the announcement.
        /// </summary>
        public DateTime LastModifiedDate { get; set; }
    }
}
