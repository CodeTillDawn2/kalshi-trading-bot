using System;

namespace BacklashDTOs.Data
{
    /// <summary>
    /// Data transfer object for standard hours data.
    /// </summary>
    public class StandardHoursDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier for the standard hours.
        /// </summary>
        public long StandardHoursID { get; set; }

        /// <summary>
        /// Gets or sets the exchange schedule identifier.
        /// </summary>
        public long ExchangeScheduleID { get; set; }

        /// <summary>
        /// Gets or sets the start time.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets the creation date.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the last modified date.
        /// </summary>
        public DateTime LastModifiedDate { get; set; }

        /// <summary>
        /// Gets or sets the list of standard hours sessions.
        /// </summary>
        public List<StandardHoursSessionDTO> Sessions { get; set; } = new List<StandardHoursSessionDTO>();
    }
}
