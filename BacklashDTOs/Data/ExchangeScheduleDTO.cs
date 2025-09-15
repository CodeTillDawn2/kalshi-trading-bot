using System;

namespace BacklashDTOs.Data
{
    /// <summary>
    /// Data transfer object for exchange schedule data.
    /// </summary>
    public class ExchangeScheduleDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier for the exchange schedule.
        /// </summary>
        public long ExchangeScheduleID { get; set; }

        /// <summary>
        /// Gets or sets the last updated timestamp.
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Gets or sets the creation date.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the last modified date.
        /// </summary>
        public DateTime LastModifiedDate { get; set; }

        /// <summary>
        /// Gets or sets the list of maintenance windows associated with this exchange schedule.
        /// </summary>
        public List<MaintenanceWindowDTO> MaintenanceWindows { get; set; } = new List<MaintenanceWindowDTO>();

        /// <summary>
        /// Gets or sets the list of standard hours associated with this exchange schedule.
        /// </summary>
        public List<StandardHoursDTO> StandardHours { get; set; } = new List<StandardHoursDTO>();
    }
}
