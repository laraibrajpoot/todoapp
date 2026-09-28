using System.ComponentModel.DataAnnotations;

namespace todolist.Models
{
    public class User
    {
        public string? ProfilePicture { get; set; }
        public int Id { get; set; }

        // Optional full name for display
        public string? Name { get; set; }

        // Optional department name
        public string? Department { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "User"; // "Admin" or "User"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsEmailVerified { get; set; } = false;
        public string? EmailOtp { get; set; }
        public DateTime? OtpExpiry { get; set; }

        // PERMANENT SETTING (Controlled by Admin on AdminUsers.razor)
        // Determines whether this user is forced to enter an OTP every time they log in.
        public bool IsMfaEnabled { get; set; } = false;

        // TEMPORARY SESSION STATUS (Updated during login flow)
        // Tracks whether the user has successfully entered their OTP for the current login attempt.
        public bool MfaVerified { get; set; } = false;

        // Password Reset Fields
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
    }
}