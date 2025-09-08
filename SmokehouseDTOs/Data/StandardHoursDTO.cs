using System;

namespace SmokehouseDTOs.Data
{
    public class StandardHoursDTO
    {
        public long StandardHoursID { get; set; }
        public long ExchangeScheduleID { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }

        // Navigation properties
        public List<StandardHoursSessionDTO> Sessions { get; set; } = new List<StandardHoursSessionDTO>();
    }
}