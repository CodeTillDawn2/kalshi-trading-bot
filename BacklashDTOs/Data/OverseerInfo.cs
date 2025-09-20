using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BacklashDTOs.Data
{
    /// <summary>
    /// Entity class for overseer information stored in the database.
    /// </summary>
    [Table("t_OverseerInfo")]
    public class OverseerInfo
    {
        /// <summary>
        /// Gets or sets the unique identifier for the overseer info record.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the hostname of the overseer.
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string HostName { get; set; }

        /// <summary>
        /// Gets or sets the IP address of the overseer.
        /// </summary>
        [Required]
        [MaxLength(45)]
        public string IPAddress { get; set; }

        /// <summary>
        /// Gets or sets the port number the overseer is running on.
        /// </summary>
        [Required]
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets the start time of the overseer.
        /// </summary>
        [Required]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the last heartbeat timestamp.
        /// </summary>
        public DateTime? LastHeartbeat { get; set; }

        /// <summary>
        /// Gets or sets whether the overseer is active.
        /// </summary>
        [Required]
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        [MaxLength(100)]
        public string? ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the version of the overseer.
        /// </summary>
        [MaxLength(50)]
        public string? Version { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OverseerInfo"/> class with default values.
        /// </summary>
        public OverseerInfo()
        {
            HostName = string.Empty;
            IPAddress = string.Empty;
            Port = 5000;
            StartTime = DateTime.UtcNow;
            IsActive = true;
            ServiceName = "BacklashOverseer";
            LastHeartbeat = DateTime.UtcNow;
        }
    }
}
