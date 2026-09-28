using System;

using System.Collections.Generic;

using System.Security.Cryptography;

using System.Text;

using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity;

using Microsoft.EntityFrameworkCore;

using todolist.Data;

using todolist.Models;



namespace todolist.Services

{

    public class UserService

    {

        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

        private readonly EmailService _emailService;

        private readonly PasswordHasher<User> _passwordHasher = new();



        public UserService(

            IDbContextFactory<ApplicationDbContext> dbContextFactory,

            EmailService emailService)

        {

            _dbContextFactory = dbContextFactory;

            _emailService = emailService;

        }



        public async Task<List<User>> GetAllUsersAsync()

        {

            using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            return await dbContext.Users.AsNoTracking().ToListAsync();

        }



        public async Task<User?> GetUserByIdAsync(int id)

        {

            using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            return await dbContext.Users.FindAsync(id);

        }



        public async Task<User?> GetUserByEmailAsync(string email)

        {

            using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            return await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);

        }



        public async Task<bool> AddUserAsync(User user)

        {

            using var dbContext = await _dbContextFactory.CreateDbContextAsync();



            // Prevent adding duplicate users if email already exists

            var existingUser = await dbContext.Users

                .FirstOrDefaultAsync(u => u.Email.ToLower() == user.Email.ToLower());



            if (existingUser != null)

            {

                return false;

            }



            if (string.IsNullOrWhiteSpace(user.PasswordHash))

            {

                user.PasswordHash = _passwordHasher.HashPassword(user, "DefaultPassword123!");

            }

            else if (!user.PasswordHash.StartsWith("AQAAAA"))

            {

                user.PasswordHash = _passwordHasher.HashPassword(user, user.PasswordHash);

            }



            dbContext.Users.Add(user);

            return await dbContext.SaveChangesAsync() > 0;

        }



        public async Task<bool> UpdateUserAsync(User updatedUser)

        {

            using var dbContext = await _dbContextFactory.CreateDbContextAsync();



            var existingUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == updatedUser.Id);

            if (existingUser == null) return false;



            existingUser.Name = updatedUser.Name;

            existingUser.Email = updatedUser.Email;

            existingUser.Department = updatedUser.Department;

            existingUser.Role = updatedUser.Role;

            existingUser.IsMfaEnabled = updatedUser.IsMfaEnabled;  
            existingUser.MfaVerified = updatedUser.MfaVerified;
            existingUser.ProfilePicture = updatedUser.ProfilePicture;

            if (!string.IsNullOrWhiteSpace(updatedUser.PasswordHash))

            {

                existingUser.PasswordHash = updatedUser.PasswordHash;

            }



            return await dbContext.SaveChangesAsync() > 0;

        }



        public async Task<bool> DeleteUserAsync(int userId)

        {

            using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var user = await dbContext.Users.FindAsync(userId);

            if (user == null) return false;



            dbContext.Users.Remove(user);

            return await dbContext.SaveChangesAsync() > 0;

        }



        public async Task<(bool Success, User? User, string? Error)> ValidateUserAsync(string email, string password)

        {

            using var dbContext = await _dbContextFactory.CreateDbContextAsync();



            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)

            {

                return (false, null, "Invalid email or password.");

            }



            if (string.IsNullOrEmpty(user.PasswordHash))

            {

                return (false, null, "Account credentials corrupted. Please contact support.");

            }



            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);



            if (verificationResult == PasswordVerificationResult.Failed)

            {

                return (false, null, "Invalid password provided.");

            }



            return (true, user, null);

        }



        public async Task<(bool Success, bool IsExistingUser, string? ErrorMessage)> RegisterUserAsync(string email, string password, string role = "User", string name = "")

        {

            using var dbContext = await _dbContextFactory.CreateDbContextAsync();



            var existingUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());



            if (existingUser != null)

            {

                if (!existingUser.IsEmailVerified)

                {

                    existingUser.IsEmailVerified = true;

                    await dbContext.SaveChangesAsync();

                }



                return (false, true, "Account already exists. Please sign in directly.");

            }



            var otp = new Random().Next(100000, 999999).ToString();



            // 1. Attempt sending OTP email first

            bool emailSent = await _emailService.SendEmailAsync(

                email,

                "Your Sign-Up Verification Code",

                $"<p>Your verification code is: <strong>{otp}</strong>. It will expire in 10 minutes.</p>"

            );



            // 2. Abort registration if email fails

            if (!emailSent)

            {

                return (false, false, "Failed to send verification email. Please enter a valid, active email address.");

            }



            // 3. Save pending user to database with the provided Name

            var newUser = new User

            {

                Email = email,

                Name = string.IsNullOrWhiteSpace(name) ? email.Split('@')[0] : name,

                Role = role,

                IsEmailVerified = false,

                EmailOtp = otp,

                OtpExpiry = DateTime.UtcNow.AddMinutes(10)

            };



            newUser.PasswordHash = _passwordHasher.HashPassword(newUser, password);



            dbContext.Users.Add(newUser);

            await dbContext.SaveChangesAsync();



            return (true, false, null);

        }



        public async Task<(bool Success, string? Error)> VerifySignUpOtpAsync(string email, string inputOtp)

        {

            using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);



            if (user == null) return (false, "User not found.");

            if (user.EmailOtp != inputOtp) return (false, "Invalid verification code.");

            if (user.OtpExpiry < DateTime.UtcNow) return (false, "Verification code has expired.");



            user.IsEmailVerified = true;

            user.EmailOtp = null;

            user.OtpExpiry = null;



            await dbContext.SaveChangesAsync();

            return (true, null);

        }



        public async Task<(bool Success, string? Error)> ConfirmEmailTokenAsync(string email, string token)

        {

            using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);



            if (user == null)

            {

                return (false, "User account not found.");

            }



            if (user.IsEmailVerified)

            {

                return (true, null);

            }



            if (user.EmailOtp != token)

            {

                return (false, "Invalid or expired verification token.");

            }



            user.IsEmailVerified = true;

            user.EmailOtp = null;

            user.OtpExpiry = null;



            await dbContext.SaveChangesAsync();

            return (true, null);

        }



        public async Task<(bool Success, string? Error)> RequestPasswordResetAsync(string email, string baseUrl)

        {

            using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);



            if (user == null)

            {

                return (true, null);

            }



            var token = Guid.NewGuid().ToString("N");

            user.ResetToken = token;

            user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);



            await dbContext.SaveChangesAsync();



            var resetLink = $"{baseUrl.TrimEnd('/')}?token={token}&email={Uri.EscapeDataString(email)}";



            await _emailService.SendEmailAsync(

                email,

                "Reset Your Password",

                $"<p>Click the link below to reset your password:</p><p><a href='{resetLink}'>Reset Password</a></p>"

            );



            return (true, null);

        }



        public async Task<(bool Success, string? Error)> ResetPasswordAsync(string email, string token, string newPassword)

        {

            using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);



            if (user == null || user.ResetToken != token)

            {

                return (false, "Invalid reset token or request.");

            }



            if (user.ResetTokenExpiry < DateTime.UtcNow)

            {

                return (false, "Reset token has expired. Please request a new one.");

            }



            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);

            user.ResetToken = null;

            user.ResetTokenExpiry = null;



            await dbContext.SaveChangesAsync();

            return (true, null);

        }



        // ==========================================

        // MFA SIGN-IN & TRUSTED DEVICE METHODS

        // ==========================================



        public async Task<bool> GenerateAndSendMfaOtpAsync(int userId)

        {

            using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var user = await dbContext.Users.FindAsync(userId);

            if (user == null) return false;



            var otp = new Random().Next(100000, 999999).ToString();

            user.EmailOtp = otp;

            user.OtpExpiry = DateTime.UtcNow.AddMinutes(5);

            await dbContext.SaveChangesAsync();



            return await _emailService.SendEmailAsync(

                user.Email,

                "TaskFlow Sign-In Verification Code",

                $"<p>Your secure sign-in verification code is: <strong>{otp}</strong>. It expires in 5 minutes.</p>"

            );

        }



        public async Task<bool> VerifyMfaOtpAsync(int userId, string inputOtp)

        {

            using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var user = await dbContext.Users.FindAsync(userId);



            if (user == null || user.EmailOtp != inputOtp || user.OtpExpiry < DateTime.UtcNow)

            {

                return false;

            }



            user.EmailOtp = null;

            user.OtpExpiry = null;

            user.MfaVerified = true;



            await dbContext.SaveChangesAsync();

            return true;

        }



        public async Task<bool> ResetUserMfaAsync(int userId)

        {

            using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var user = await dbContext.Users.FindAsync(userId);

            if (user == null) return false;



            user.MfaVerified = false;

            await dbContext.SaveChangesAsync();

            return true;

        }



        public async Task<string> CreateTrustedDeviceAsync(int userId)

        {

            using var dbContext = await _dbContextFactory.CreateDbContextAsync();



            var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            var tokenHash = HashToken(rawToken);



            var trustedDevice = new TrustedDevice

            {

                UserId = userId,

                DeviceTokenHash = tokenHash,

                ExpiryDate = DateTime.UtcNow.AddDays(30)

            };



            dbContext.TrustedDevices.Add(trustedDevice);

            await dbContext.SaveChangesAsync();



            return rawToken;

        }



        public async Task<bool> ValidateDeviceTokenAsync(int userId, string rawToken)

        {

            if (string.IsNullOrWhiteSpace(rawToken)) return false;



            using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var tokenHash = HashToken(rawToken);



            var deviceRecord = await dbContext.TrustedDevices

                .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceTokenHash == tokenHash);



            if (deviceRecord == null || deviceRecord.ExpiryDate < DateTime.UtcNow)

            {

                if (deviceRecord != null)

                {

                    dbContext.TrustedDevices.Remove(deviceRecord);

                    await dbContext.SaveChangesAsync();

                }

                return false;

            }



            return true;

        }



        private string HashToken(string token)

        {

            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));

            return Convert.ToBase64String(bytes);

        }

    }

}