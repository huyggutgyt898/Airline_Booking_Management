using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TEO_Airline_Project.Models;
using TEO_Airline_Project.Models.ViewModels;
using TEO_Airline_Project.Services;

namespace TEO_Airline_Project.Controllers
{
    public class HomeController : Controller
    {
        private readonly AirlineBookingDBContext _context;

        public HomeController(AirlineBookingDBContext context)
        {
            _context = context;
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
        public IActionResult Counter()
        {
            if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!string.Equals(
                    HttpContext.Session.GetString("Role")?.Trim(),
                    "Staff",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToRoleHome();
            }

            return View("counter");
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
    }
}
