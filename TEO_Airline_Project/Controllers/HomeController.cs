using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Text.RegularExpressions;
using TEO_Airline_Project.Models;
using TEO_Airline_Project.Models.ViewModels;
using TEO_Airline_Project.Services;

namespace TEO_Airline_Project.Controllers
{
    public class HomeController : Controller
    {
        private readonly AirlineBookingDBContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ILogger<HomeController> _logger;

        private static readonly Regex EmailPattern =
            new(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.Compiled);

        private static readonly Regex PhonePattern =
            new(@"^(0\d{9}|\+84\d{9})$", RegexOptions.Compiled);

        public HomeController(
            AirlineBookingDBContext context,
            IPasswordHasher<User> passwordHasher,
            ILogger<HomeController> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (RedirectForRole() is { } redirect)
            {
                return redirect;
            }

            return View();
        }

        public IActionResult Lookup()
        {
            if (RedirectForRole() is { } redirect)
            {
                return redirect;
            }

            return View();
        }

        public IActionResult Rewards()
        {
            if (RedirectForRole() is { } redirect)
            {
                return redirect;
            }

            return View();
        }

        private IActionResult? RedirectForRole()
        {
            var role = HttpContext.Session.GetString("Role")?.Trim().ToUpperInvariant();
            if (role == "ADMIN")
            {
                return RedirectToAction(nameof(Admin));
            }

            if (role == "STAFF")
            {
                return RedirectToAction(nameof(Counter));
            }

            if (!string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserId")) &&
                role != "CUSTOMER")
            {
                return RedirectToAction("Login", "Auth");
            }

            return null;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Admin(
            int routePage = 1,
            string? routeSearch = null,
            string? routeType = null,
            int flightPage = 1,
            string? flightSearch = null,
            string? flightStatus = null,
            int? aircraftId = null,
            int? pricingFlightId = null,
            string? promotionSearch = null,
            string? promotionStatus = null,
            string? dashboardPeriod = "month")
        {
            if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!string.Equals(
                    HttpContext.Session.GetString("Role")?.Trim(),
                    "Admin",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToRoleHome();
            }

            var model = await AdminDashboardViewModelBuilder.BuildAsync(
                _context,
                routePage,
                routeSearch,
                routeType,
                flightPage: flightPage,
                flightSearch: flightSearch,
                flightStatus: flightStatus,
                selectedAircraftId: aircraftId,
                pricingFlightId: pricingFlightId,
                promotionSearch: promotionSearch,
                promotionStatus: promotionStatus,
                dashboardPeriod: dashboardPeriod);

            return View("admin", model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Counter(string? screen = null)
        {
            if (RedirectIfNotStaff() is { } redirect)
            {
                return redirect;
            }

            var userId = GetSessionUserId();
            var user = await _context.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(candidate =>
                    candidate.UserId == userId && candidate.Role == "Staff");
            if (user is null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Auth");
            }

            return View("counter", CreateStaffProfileViewModel(
                user,
                profileMessage: TempData["ProfileMessage"] as string,
                passwordMessage: TempData["PasswordMessage"] as string));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveStaffProfile(
            string? fullName,
            string? email,
            string? phone)
        {
            if (RedirectIfNotStaff() is { } redirect)
            {
                return redirect;
            }

            var user = await GetCurrentStaffAsync();
            if (user is null)
            {
                return RedirectToAction("Login", "Auth");
            }

            fullName = fullName?.Trim();
            email = email?.Trim();
            phone = NormalizeStaffPhone(phone);

            var error = ValidateStaffProfile(fullName, email, phone);
            if (error is null && await _context.Users.AnyAsync(candidate =>
                    candidate.UserId != user.UserId && candidate.Email == email))
            {
                error = "Email này đã được sử dụng.";
            }

            if (error is null && await _context.Users.AnyAsync(candidate =>
                    candidate.UserId != user.UserId && candidate.Phone == phone))
            {
                error = "Số điện thoại này đã được sử dụng.";
            }

            if (error is not null)
            {
                return View("counter", CreateStaffProfileViewModel(
                    user,
                    fullName,
                    email,
                    phone,
                    profileError: error));
            }

            user.FullName = fullName!;
            user.Email = email!;
            user.Phone = phone!;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
            {
                _logger.LogInformation(
                    exception,
                    "Staff {UserId} tried to update their profile with an email already in use.",
                    user.UserId);
                return View("counter", CreateStaffProfileViewModel(
                    user,
                    fullName,
                    email,
                    phone,
                    profileError: "Email này vừa được một tài khoản khác sử dụng. Vui lòng nhập email khác."));
            }

            HttpContext.Session.SetString("FullName", user.FullName);
            TempData["ProfileMessage"] = "Đã lưu thông tin hồ sơ.";
            return RedirectToAction(nameof(Counter), new { screen = "profile" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStaffPassword(
            string? currentPassword,
            string? newPassword,
            string? confirmPassword)
        {
            if (RedirectIfNotStaff() is { } redirect)
            {
                return redirect;
            }

            var user = await GetCurrentStaffAsync();
            if (user is null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var error = ValidateStaffPassword(newPassword, confirmPassword);
            if (error is not null)
            {
                return View("counter", CreateStaffProfileViewModel(
                    user,
                    passwordError: error));
            }

            if (!HasValidPasswordHashEncoding(user.PasswordHash))
            {
                _logger.LogWarning(
                    "Staff {UserId} has an invalid password hash and cannot change their password.",
                    user.UserId);
                return View("counter", CreateStaffProfileViewModel(
                    user,
                    passwordError: "Không thể xác minh mật khẩu hiện tại. Vui lòng liên hệ quản trị viên."));
            }

            if (string.IsNullOrEmpty(currentPassword))
            {
                return View("counter", CreateStaffProfileViewModel(
                    user,
                    passwordError: "Vui lòng nhập mật khẩu hiện tại."));
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                currentPassword!);
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                return View("counter", CreateStaffProfileViewModel(
                    user,
                    passwordError: "Mật khẩu hiện tại không chính xác."));
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword!);
            await _context.SaveChangesAsync();

            TempData["PasswordMessage"] = "Đã cập nhật mật khẩu.";
            return RedirectToAction(nameof(Counter), new { screen = "profile" });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private IActionResult RedirectToRoleHome()
        {
            return HttpContext.Session.GetString("Role")?.Trim().ToUpperInvariant() switch
            {
                "CUSTOMER" => RedirectToAction(nameof(Index)),
                "ADMIN" => RedirectToAction(nameof(Admin)),
                "STAFF" => RedirectToAction(nameof(Counter)),
                _ => RedirectToAction("Login", "Auth")
            };
        }

        private IActionResult? RedirectIfNotStaff()
        {
            if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Login", "Auth");
            }

            return string.Equals(
                    HttpContext.Session.GetString("Role")?.Trim(),
                    "Staff",
                    StringComparison.OrdinalIgnoreCase)
                ? null
                : RedirectToRoleHome();
        }

        private async Task<User?> GetCurrentStaffAsync()
        {
            var userId = GetSessionUserId();
            if (userId is null)
            {
                return null;
            }

            return await _context.Users.SingleOrDefaultAsync(
                user => user.UserId == userId.Value && user.Role == "Staff");
        }

        private int? GetSessionUserId()
        {
            return int.TryParse(HttpContext.Session.GetString("UserId"), out var userId)
                ? userId
                : null;
        }

        private static StaffProfileViewModel CreateStaffProfileViewModel(
            User user,
            string? fullName = null,
            string? email = null,
            string? phone = null,
            string? profileError = null,
            string? passwordError = null,
            string? profileMessage = null,
            string? passwordMessage = null)
        {
            return new StaffProfileViewModel
            {
                UserId = user.UserId,
                FullName = fullName ?? user.FullName ?? "",
                Email = email ?? user.Email ?? "",
                Phone = phone ?? user.Phone ?? "",
                RoleLabel = user.Role switch
                {
                    "Staff" => "Nhân viên quầy",
                    "Admin" => "Quản trị viên",
                    "Customer" => "Hành khách",
                    _ => user.Role ?? ""
                },
                ProfileError = profileError,
                PasswordError = passwordError,
                ProfileMessage = profileMessage,
                PasswordMessage = passwordMessage
            };
        }

        private static string? ValidateStaffProfile(
            string? fullName,
            string? email,
            string? phone)
        {
            if (string.IsNullOrWhiteSpace(fullName) ||
                fullName.Length < 2 ||
                fullName.Length > 100)
            {
                return "Họ và tên phải có từ 2 đến 100 ký tự.";
            }

            if (string.IsNullOrWhiteSpace(email) ||
                email.Length > 100 ||
                !EmailPattern.IsMatch(email))
            {
                return "Email không đúng định dạng hoặc vượt quá 100 ký tự.";
            }

            if (string.IsNullOrWhiteSpace(phone) || !PhonePattern.IsMatch(phone))
            {
                return "Số điện thoại không đúng định dạng. Ví dụ: 0912345678.";
            }

            return null;
        }

        private static string? ValidateStaffPassword(
            string? newPassword,
            string? confirmPassword)
        {
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 8)
            {
                return "Mật khẩu mới phải có ít nhất 8 ký tự.";
            }

            if (!newPassword.Any(char.IsLetter) ||
                !newPassword.Any(character =>
                    !char.IsLetterOrDigit(character) && !char.IsWhiteSpace(character)))
            {
                return "Mật khẩu mới phải bao gồm chữ cái và ký tự đặc biệt.";
            }

            if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
            {
                return "Mật khẩu xác nhận không khớp.";
            }

            return null;
        }

        private static string? NormalizeStaffPhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return phone?.Trim();
            }

            return new string(phone
                .Where(character =>
                    !char.IsWhiteSpace(character) &&
                    character is not '-' and not '.' and not '(' and not ')')
                .ToArray());
        }

        private static bool HasValidPasswordHashEncoding(string? passwordHash)
        {
            if (string.IsNullOrEmpty(passwordHash))
            {
                return false;
            }

            var hashBytes = new byte[192];
            return Convert.TryFromBase64String(passwordHash, hashBytes, out var bytesWritten) &&
                bytesWritten > 0;
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException exception)
        {
            return exception.GetBaseException() is SqlException
            {
                Number: 2601 or 2627
            };
        }
    }
}
