using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using todolist.Models;
using todolist.Services;
namespace todolist.Controllers

{

    [Route("Account")]

    public class AccountController : Controller

    {

        private readonly UserService _userService;



        public AccountController(UserService userService)

        {

            _userService = userService;

        }



        [HttpPost("Login")]

        [AllowAnonymous]

        [IgnoreAntiforgeryToken]

        public async Task<IActionResult> Login([FromForm] string email, [FromForm] string password, [FromForm] bool rememberMe)

        {

            // 1. Validate credentials using UserService

            var (success, user, error) = await _userService.ValidateUserAsync(email, password);



            if (!success || user == null)

            {

                return Redirect($"/login?ErrorMessage={Uri.EscapeDataString(error ?? "Invalid credentials")}");

            }



            // 2. CHECK DATABASE COLUMN: IsMfaEnabled (Set by Admin toggle)

            // If IsMfaEnabled is FALSE (toggle is OFF): Skip OTP for ANY user (Admin or Normal User)

            if (!user.IsMfaEnabled)

            {

                user.MfaVerified = true;

                await _userService.UpdateUserAsync(user);

                await SignInUserAsync(user, rememberMe);

                return RedirectToDashboard(user);

            }



            // 3. If IsMfaEnabled is TRUE (toggle is ON): Reset session state & require OTP every time

            user.MfaVerified = false;

            await _userService.UpdateUserAsync(user);



            // Generate and send MFA OTP

            await _userService.GenerateAndSendMfaOtpAsync(user.Id);



            // Store user context temporarily for the verification step

            TempData["MfaUserId"] = user.Id;

            TempData["MfaRememberMe"] = rememberMe;



            // Redirect user to the OTP input page

            return Redirect("/verify-otp");

        }



        [HttpPost("VerifyOtp")]

        [AllowAnonymous]

        [IgnoreAntiforgeryToken]

        public async Task<IActionResult> VerifyOtp([FromForm] string otp)

        {

            if (TempData["MfaUserId"] is not int userId)

            {

                return Redirect("/login?ErrorMessage=" + Uri.EscapeDataString("Session expired. Please sign in again."));

            }



            bool rememberMe = TempData["MfaRememberMe"] is bool val && val;

            TempData.Keep("MfaUserId");



            // 1. Verify the code entered by the user

            bool isValid = await _userService.VerifyMfaOtpAsync(userId, otp);

            if (!isValid)

            {

                return Redirect("/verify-otp?ErrorMessage=" + Uri.EscapeDataString("Invalid or expired verification code."));

            }



            // 2. Fetch the user details

            var user = await _userService.GetUserByIdAsync(userId);

            if (user == null)

            {

                var allUsers = await _userService.GetAllUsersAsync();

                user = allUsers.FirstOrDefault(u => u.Id == userId);

            }



            if (user == null)

            {

                return Redirect("/login?ErrorMessage=" + Uri.EscapeDataString("User account not found."));

            }



            // 3. Update session MfaVerified status

            user.MfaVerified = true;

            await _userService.UpdateUserAsync(user);



            // 4. Sign the user into the application fully

            await SignInUserAsync(user, rememberMe);

            return RedirectToDashboard(user);

        }



        // Helper method to issue application sign-in claims cookie

        private async Task SignInUserAsync(User user, bool rememberMe)

        {

            var claims = new List<Claim>

{

new(ClaimTypes.NameIdentifier, user.Id.ToString()),

new(ClaimTypes.Name, string.IsNullOrWhiteSpace(user.Name) ? user.Email : user.Name),

new(ClaimTypes.Email, user.Email),

new(ClaimTypes.Role, user.Role ?? "User")

};



            var claimsIdentity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);

            var authProperties = new AuthenticationProperties { IsPersistent = rememberMe };



            await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

        }



        // Helper method to direct users based on their role

        private IActionResult RedirectToDashboard(User user)

        {

            var roleValue = (user.Role ?? string.Empty).Trim();

            var target = string.Equals(roleValue, "Admin", StringComparison.OrdinalIgnoreCase)

            ? "/admin-dashboard"

            : "/user/dashboard";



            return Redirect(target);

        }
    }

}
