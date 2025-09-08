using System;

namespace SmokehouseDTOs.Data
{
    public class ExchangeScheduleDTO
    {
        public long ExchangeScheduleID { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }

        // Navigation properties
        public List<MaintenanceWindowDTO> MaintenanceWindows { get; set; } = new List<MaintenanceWindowDTO>();
        public List<StandardHoursDTO> StandardHours { get; set; } = new List<StandardHoursDTO>();
    }
}