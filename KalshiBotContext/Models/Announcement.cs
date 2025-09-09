using System;

namespace KalshiBotData.Models
{
    public class Announcement
    {
        public long AnnouncementID { get; set; }
        public DateTime DeliveryTime { get; set; }
        public required string Message { get; set; }
        public required string Status { get; set; }
        public required string Type { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
    }
}
