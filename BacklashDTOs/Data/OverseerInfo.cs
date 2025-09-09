using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BacklashDTOs.Data
{
    [Table("t_OverseerInfo")]
    public class OverseerInfo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string HostName { get; set; }

        [Required]
        [MaxLength(45)]
        public string IPAddress { get; set; }

        [Required]
        public int Port { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        public DateTime? LastHeartbeat { get; set; }

        [Required]
        public bool IsActive { get; set; }

        [MaxLength(100)]
        public string? ServiceName { get; set; }

        [MaxLength(50)]
        public string? Version { get; set; }

        public OverseerInfo()
        {
            HostName = string.Empty;
            IPAddress = string.Empty;
            Port = 5000;
            StartTime = DateTime.UtcNow;
            IsActive = true;
            ServiceName = "KalshiBotOverseer";
            LastHeartbeat = DateTime.UtcNow;
        }
    }
}