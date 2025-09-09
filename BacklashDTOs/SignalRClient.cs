using System;

namespace BacklashDTOs
{
    public class SignalRClient
    {
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public string IPAddress { get; set; }
        public string AuthToken { get; set; }
        public string ClientType { get; set; } // 'overseer', 'dashboard', etc.
        public bool IsActive { get; set; } = true;
        public DateTime? LastSeen { get; set; }
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public string? ConnectionId { get; set; } // SignalR connection ID
    }
}