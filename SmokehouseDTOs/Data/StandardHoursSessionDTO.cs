using System;

namespace SmokehouseDTOs.Data
{
    public class StandardHoursSessionDTO
    {
        public long SessionID { get; set; }
        public long StandardHoursID { get; set; }
        public string DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
    }
}