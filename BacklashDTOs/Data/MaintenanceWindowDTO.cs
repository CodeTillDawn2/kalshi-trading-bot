namespace BacklashDTOs.Data
{
    /// <summary>
    /// Data transfer object for maintenance window data.
    /// </summary>
    public class MaintenanceWindowDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier for the maintenance window.
        /// </summary>
        public long MaintenanceWindowID { get; set; }

        /// <summary>
        /// Gets or sets the exchange schedule identifier.
        /// </summary>
        public long ExchangeScheduleID { get; set; }

        /// <summary>
        /// Gets or sets the start date and time of the maintenance window.
        /// </summary>
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// Gets or sets the end date and time of the maintenance window.
        /// </summary>
        public DateTime EndDateTime { get; set; }

        /// <summary>
        /// Gets or sets the creation date.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the last modified date.
        /// </summary>
        public DateTime LastModifiedDate { get; set; }
    }
}
