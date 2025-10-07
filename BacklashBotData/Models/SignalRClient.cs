namespace BacklashBotData.Models
{
    /// <summary>
    /// Represents a client connected to the system via SignalR for real-time communication.
    /// This entity tracks client connections, authentication, and connection state for
    /// managing real-time data distribution to dashboards, overseer systems, and other
    /// connected clients. It enables secure, authenticated real-time communication
    /// between the trading bot system and its clients.
    /// </summary>
    public class SignalRClient
    {
        /// <summary>
        /// Gets or sets the unique identifier for this client.
        /// This serves as the primary key for client identification.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the human-readable name for this client.
        /// This provides a friendly identifier for the client (e.g., "Dashboard-001").
        /// </summary>
        public string ClientName { get; set; }

        /// <summary>
        /// Gets or sets the IP address of the connected client.
        /// This is used for security monitoring and connection tracking.
        /// </summary>
        public string IPAddress { get; set; }

        /// <summary>
        /// Gets or sets the authentication token for this client.
        /// This token is used to authenticate and authorize the client's connection.
        /// </summary>
        public string AuthToken { get; set; }

        /// <summary>
        /// Gets or sets the type of client connecting to the system.
        /// Common types include 'overseer', 'dashboard', 'mobile', etc.
        /// </summary>
        public string ClientType { get; set; }

        /// <summary>
        /// Gets or sets whether this client connection is currently active.
        /// This flag indicates if the client is connected and receiving updates.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the timestamp when this client was last seen active.
        /// This is used to detect stale connections and manage connection cleanup.
        /// </summary>
        public DateTime? LastSeen { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this client was first registered in the system.
        /// This marks the initial connection time for the client.
        /// </summary>
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the SignalR connection identifier for this client.
        /// This is the unique connection ID assigned by the SignalR hub.
        /// </summary>
        public string? ConnectionId { get; set; }
    }
}
