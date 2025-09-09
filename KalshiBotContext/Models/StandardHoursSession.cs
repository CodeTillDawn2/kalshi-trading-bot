using System;

namespace KalshiBotData.Models
{
    public class StandardHoursSession
    {
        public long SessionID { get; set; }
        public long StandardHoursID { get; set; }
        public string DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }

        // Navigation property
        public StandardHours StandardHours { get; set; }
    }
}
