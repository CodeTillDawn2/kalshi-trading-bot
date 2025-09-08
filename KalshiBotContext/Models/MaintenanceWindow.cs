using System;

namespace KalshiBotData.Models
{
    public class MaintenanceWindow
    {
        public long MaintenanceWindowID { get; set; }
        public long ExchangeScheduleID { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }

        // Navigation property
        public ExchangeSchedule ExchangeSchedule { get; set; }
    }
}