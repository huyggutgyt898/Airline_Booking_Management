using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TEO_Airline_Project.Models;

namespace TEO_Airline_Project.Controllers
{
    public class AuthController : Controller
    {
        private readonly AirlineBookingDBContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            AirlineBookingDBContext context,
            IPasswordHasher<User> passwordHasher,
            ILogger<AuthController> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string? identity, string? password)
        {
            identity = identity?.Trim();
            ViewData["Identity"] = identity;

            if (!string.IsNullOrWhiteSpace(identity) && !string.IsNullOrEmpty(password))
            {
                var user = await _context.Users.FirstOrDefaultAsync(
                    u => u.Email == identity || u.Phone == identity || u.Username == identity);

                if (user is not null && user.IsActive != false &&
                    !string.IsNullOrEmpty(user.PasswordHash))
                {
                    var hashBytes = new byte[192];
                    if (Convert.TryFromBase64String(user.PasswordHash, hashBytes, out var bytesWritten) &&
                        bytesWritten > 0)
                    {
                        var result = _passwordHasher.VerifyHashedPassword(
                            user, user.PasswordHash, password);

                        if (result != PasswordVerificationResult.Failed)
                        {
                            HttpContext.Session.SetString("UserId", user.UserId.ToString());
                            HttpContext.Session.SetString("Username", user.Username);
                            HttpContext.Session.SetString("FullName", user.FullName ?? "");
                            HttpContext.Session.SetString("Role", user.Role ?? "Customer");

                            return RedirectToAction("Index", "Home");
                        }
                    }
                    else
                    {
                        _logger.LogWarning(
                            "User {UserId} has an invalid password hash and must reset their password.",
                            user.UserId);
                    }
                }
            }

            ViewData["ErrorMessage"] = "Thông tin đăng nhập không chính xác.";
            return View();
        }
    }
}