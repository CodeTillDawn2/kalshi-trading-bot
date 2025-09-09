using System;

namespace BacklashDTOs.Data
{
    public class MaintenanceWindowDTO
    {
        public long MaintenanceWindowID { get; set; }
        public long ExchangeScheduleID { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
    }
}
