using System;

namespace KalshiBotData.Models
{
    public class ExchangeSchedule
    {
        public long ExchangeScheduleID { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }

        // Navigation properties
        public ICollection<MaintenanceWindow> MaintenanceWindows { get; set; } = new List<MaintenanceWindow>();
        public ICollection<StandardHours> StandardHours { get; set; } = new List<StandardHours>();
    }
}