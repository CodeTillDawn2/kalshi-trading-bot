namespace SmokehouseDTOs.Data
{
    public class AnnouncementDTO
    {
        public long AnnouncementID { get; set; }
        public DateTime DeliveryTime { get; set; }
        public string Message { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
    }
}