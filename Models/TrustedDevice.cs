using System;

namespace todolist.Models
{
    public class TrustedDevice
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        // Stores a secure hash of the unique device token given to the cookie
        public string DeviceTokenHash { get; set; } = string.Empty;

        // When this trusted device permission expires (e.g., 30 days)
        public DateTime ExpiryDate { get; set; }
    }
}