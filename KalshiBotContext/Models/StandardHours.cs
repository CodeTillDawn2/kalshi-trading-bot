using System;

namespace KalshiBotData.Models
{
    public class StandardHours
    {
        public long StandardHoursID { get; set; }
        public long ExchangeScheduleID { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }

        // Navigation properties
        public ExchangeSchedule ExchangeSchedule { get; set; }
        public ICollection<StandardHoursSession> Sessions { get; set; } = new List<StandardHoursSession>();
    }
}
