using System;

namespace BacklashDTOs
{
    /// <summary>
    /// Represents a SignalR client.
    /// </summary>
    public class SignalRClient
    {
        /// <summary>
        /// Gets or sets the client ID.
        /// </summary>
        public string? ClientId { get; set; }
        /// <summary>
        /// Gets or sets the client name.
        /// </summary>
        public string? ClientName { get; set; }
        /// <summary>
        /// Gets or sets the IP address.
        /// </summary>
        public string? IPAddress { get; set; }
        /// <summary>
        /// Gets or sets the authentication token.
        /// </summary>
        public string? AuthToken { get; set; }
        /// <summary>
        /// Gets or sets the client type ('overseer', 'dashboard', etc.).
        /// </summary>
        public string? ClientType { get; set; } // 'overseer', 'dashboard', etc.
        /// <summary>
        /// Gets or sets a value indicating whether the client is active.
        /// </summary>
        public bool IsActive { get; set; } = true;
        /// <summary>
        /// Gets or sets the last seen timestamp.
        /// </summary>
        public DateTime? LastSeen { get; set; }
        /// <summary>
        /// Gets or sets the registration timestamp.
        /// </summary>
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Gets or sets the SignalR connection ID.
        /// </summary>
        public string? ConnectionId { get; set; } // SignalR connection ID
    }
}