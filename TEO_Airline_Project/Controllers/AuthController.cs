using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
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
            if (!string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserId")))
            {
                var redirect = GetRoleHomeRedirect(HttpContext.Session.GetString("Role"));
                if (redirect is not null)
                {
                    return redirect;
                }
            }

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

                if (user is not null && !string.IsNullOrEmpty(user.PasswordHash))
                {
                    var hashBytes = new byte[192];
                    if (Convert.TryFromBase64String(user.PasswordHash, hashBytes, out var bytesWritten) &&
                        bytesWritten > 0)
                    {
                        var result = _passwordHasher.VerifyHashedPassword(
                            user, user.PasswordHash, password);

                        if (result != PasswordVerificationResult.Failed)
                        {
                            if (user.IsActive == false)
                            {
                                ViewData["ErrorMessage"] = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ bộ phận hỗ trợ để được trợ giúp.";
                                return View();
                            }

                            var roleRedirect = GetRoleHomeRedirect(user.Role);
                            if (roleRedirect is null)
                            {
                                HttpContext.Session.Clear();
                                _logger.LogWarning(
                                    "User {UserId} has an unsupported role {Role}.",
                                    user.UserId,
                                    user.Role);
                                ViewData["ErrorMessage"] = "Vai trò tài khoản chưa được hỗ trợ. Vui lòng liên hệ quản trị viên.";
                                return View();
                            }

                            HttpContext.Session.Clear();
                            HttpContext.Session.SetString("UserId", user.UserId.ToString());
                            HttpContext.Session.SetString("Username", user.Username);
                            HttpContext.Session.SetString("FullName", user.FullName ?? "");
                            HttpContext.Session.SetString("Role", user.Role.Trim());

                            return roleRedirect;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        private static readonly Regex EmailPattern =
            new(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.Compiled);

        private static readonly Regex PhonePattern =
            new(@"^(0\d{9}|\+84\d{9})$", RegexOptions.Compiled);

        [HttpGet]
        public IActionResult Register()
        {
            if (!string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserId")))
            {
                var redirect = GetRoleHomeRedirect(HttpContext.Session.GetString("Role"));
                if (redirect is not null)
                {
                    return redirect;
                }
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            string? fullName,
            string? email,
            string? phone,
            string? password,
            string? confirmPassword,
            bool acceptTerms)
        {
            fullName = fullName?.Trim();
            email = email?.Trim();
            phone = NormalizePhone(phone);

            ViewData["FullName"] = fullName;
            ViewData["Email"] = email;
            ViewData["Phone"] = phone;
            ViewData["AcceptTerms"] = acceptTerms;

            var error = ValidateRegistration(fullName, email, phone, password, confirmPassword, acceptTerms);
            if (error is null && await _context.Users.AnyAsync(u => u.Email == email))
            {
                error = "Email này đã được sử dụng.";
            }

            if (error is null && await _context.Users.AnyAsync(u => u.Phone == phone))
            {
                error = "Số điện thoại này đã được sử dụng.";
            }

            if (error is not null)
            {
                ViewData["ErrorMessage"] = error;
                return View();
            }

            var user = new User
            {
                Username = await GenerateUsernameAsync(email!),
                FullName = fullName!,
                Email = email!,
                Phone = phone!,
                Role = "Customer",
                LoyaltyPoints = 0,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, password!);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Tạo tài khoản thành công. Vui lòng đăng nhập để tiếp tục.";
            return RedirectToAction(nameof(Login));
        }

        private static string? NormalizePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return phone?.Trim();
            }

            return new string(phone
                .Where(ch => !char.IsWhiteSpace(ch) && ch is not '-' and not '.' and not '(' and not ')')
                .ToArray());
        }

        private static string? ValidateRegistration(
            string? fullName,
            string? email,
            string? phone,
            string? password,
            string? confirmPassword,
            bool acceptTerms)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return "Vui lòng nhập họ và tên.";
            }

            if (fullName.Length < 2)
            {
                return "Họ và tên phải có ít nhất 2 ký tự.";
            }

            if (fullName.Length > 200)
            {
                return "Họ và tên không được vượt quá 200 ký tự.";
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                return "Vui lòng nhập email.";
            }

            if (email.Length > 100 || !EmailPattern.IsMatch(email))
            {
                return "Email không đúng định dạng.";
            }

            if (string.IsNullOrWhiteSpace(phone))
            {
                return "Vui lòng nhập số điện thoại.";
            }

            if (!PhonePattern.IsMatch(phone))
            {
                return "Số điện thoại không đúng định dạng. Ví dụ: 0912345678.";
            }

            if (string.IsNullOrEmpty(password))
            {
                return "Vui lòng nhập mật khẩu.";
            }

            if (password.Length < 8)
            {
                return "Mật khẩu phải có ít nhất 8 ký tự.";
            }

            if (!password.Any(char.IsLetter) ||
                !password.Any(ch => !char.IsLetterOrDigit(ch) && !char.IsWhiteSpace(ch)))
            {
                return "Mật khẩu phải bao gồm cả chữ cái và ký tự đặc biệt.";
            }

            if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
            {
                return "Mật khẩu xác nhận không khớp.";
            }

            if (!acceptTerms)
            {
                return "Bạn cần đồng ý với Điều khoản sử dụng để tạo tài khoản.";
            }

            return null;
        }

        private async Task<string> GenerateUsernameAsync(string email)
        {
            var localPart = email.Split('@')[0];
            var baseName = new string(localPart
                .Where(ch => char.IsAsciiLetterOrDigit(ch) || ch is '.' or '_' or '-')
                .ToArray())
                .Trim('.', '_', '-');

            if (string.IsNullOrEmpty(baseName))
            {
                baseName = "teo";
            }

            if (baseName.Length > 40)
            {
                baseName = baseName[..40];
            }

            var candidate = baseName;
            for (var suffix = 1; await _context.Users.AnyAsync(u => u.Username == candidate); suffix++)
            {
                candidate = $"{baseName}{suffix}";
            }

            return candidate;
        }

        private IActionResult? GetRoleHomeRedirect(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return null;
            }

            return role.Trim().ToUpperInvariant() switch
            {
                "CUSTOMER" => RedirectToAction("Index", "Home"),
                "ADMIN" => RedirectToAction("Admin", "Home"),
                "STAFF" => RedirectToAction("Counter", "Home"),
                _ => null
            };
        }
    }
}