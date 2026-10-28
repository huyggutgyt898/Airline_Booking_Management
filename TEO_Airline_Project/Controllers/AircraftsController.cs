using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TEO_Airline_Project.Models;
using TEO_Airline_Project.Models.ViewModels;
using TEO_Airline_Project.Services;

namespace TEO_Airline_Project.Controllers;

public class AircraftsController : Controller
{
    private const string AircraftInputPrefix = nameof(AdminDashboardViewModel.AircraftInput);
    private static readonly char[] SeatLetters = ['A', 'B', 'C', 'D', 'E', 'F'];

    private readonly AirlineBookingDBContext _context;
    private readonly ILogger<AircraftsController> _logger;

    public AircraftsController(
        AirlineBookingDBContext context,
        ILogger<AircraftsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(Prefix = AircraftInputPrefix)] AircraftInputModel input,
        int routePage = 1,
        string? routeSearch = null,
        string? routeType = null,
        int flightPage = 1,
        string? flightSearch = null,
        string? flightStatus = null,
        int? selectedAircraftId = null)
    {
        if (RedirectForNonAdmin() is { } redirect)
        {
            return redirect;
        }

        Normalize(input);
        TryValidateModel(input, AircraftInputPrefix);
        ValidateBusinessRows(input);
        if (!ModelState.IsValid)
        {
            return await RenderAdminWithInputAsync(
                input, "create", selectedAircraftId,
                routePage, routeSearch, routeType,
                flightPage, flightSearch, flightStatus);
        }

        var seats = new List<Seat>(input.RowCount * input.SeatsPerRow);
        for (var row = 1; row <= input.RowCount; row++)
        {
            var seatClass = row <= input.BusinessRows ? "Business" : "Economy";
            for (var column = 0; column < input.SeatsPerRow; column++)
            {
                var rowNumber = row.ToString("D2", System.Globalization.CultureInfo.InvariantCulture);
                seats.Add(new Seat
                {
                    SeatNumber = $"{rowNumber}{SeatLetters[column]}",
                    SeatClass = seatClass
                });
            }
        }

        var aircraft = new Aircraft
        {
            Model = input.Model,
            TotalSeats = seats.Count,
            Seats = seats
        };
        _context.Aircrafts.Add(aircraft);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not create aircraft {AircraftModel}.", input.Model);
            ModelState.AddModelError(
                string.Empty,
                "Không thể lưu máy bay và sơ đồ ghế. Vui lòng kiểm tra dữ liệu và thử lại.");
            return await RenderAdminWithInputAsync(
                input, "create", selectedAircraftId,
                routePage, routeSearch, routeType,
                flightPage, flightSearch, flightStatus);
        }

        TempData["AircraftSuccess"] = $"Đã thêm máy bay {aircraft.Model} cùng {seats.Count} ghế.";
        return RedirectToAircraftScreen(
            aircraft.AircraftId,
            routePage, routeSearch, routeType,
            flightPage, flightSearch, flightStatus);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int aircraftId,
        [Bind(Prefix = AircraftInputPrefix)] AircraftInputModel input,
        int routePage = 1,
        string? routeSearch = null,
        string? routeType = null,
        int flightPage = 1,
        string? flightSearch = null,
        string? flightStatus = null)
    {
        if (RedirectForNonAdmin() is { } redirect)
        {
            return redirect;
        }

        var aircraft = await _context.Aircrafts
            .FirstOrDefaultAsync(item => item.AircraftId == aircraftId);
        if (aircraft is null)
        {
            TempData["AircraftError"] = "Không tìm thấy máy bay cần cập nhật.";
            return RedirectToAircraftScreen(
                null,
                routePage, routeSearch, routeType,
                flightPage, flightSearch, flightStatus);
        }

        Normalize(input);
        TryValidateModel(input, AircraftInputPrefix);
        if (!ModelState.IsValid)
        {
            return await RenderAdminWithInputAsync(
                input, "edit", aircraftId,
                routePage, routeSearch, routeType,
                flightPage, flightSearch, flightStatus);
        }

        aircraft.Model = input.Model;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not update aircraft {AircraftId}.", aircraftId);
            ModelState.AddModelError(
                string.Empty,
                "Không thể cập nhật tên model máy bay. Vui lòng thử lại.");
            return await RenderAdminWithInputAsync(
                input, "edit", aircraftId,
                routePage, routeSearch, routeType,
                flightPage, flightSearch, flightStatus);
        }

        TempData["AircraftSuccess"] = $"Đã cập nhật model thành {aircraft.Model}.";
        return RedirectToAircraftScreen(
            aircraftId,
            routePage, routeSearch, routeType,
            flightPage, flightSearch, flightStatus);
    }

    private async Task<IActionResult> RenderAdminWithInputAsync(
        AircraftInputModel input,
        string dialogMode,
        int? selectedAircraftId,
        int routePage,
        string? routeSearch,
        string? routeType,
        int flightPage,
        string? flightSearch,
        string? flightStatus)
    {
        var model = await AdminDashboardViewModelBuilder.BuildAsync(
            _context,
            routePage,
            routeSearch,
            routeType,
            flightPage: flightPage,
            flightSearch: flightSearch,
            flightStatus: flightStatus,
            selectedAircraftId: selectedAircraftId,
            aircraftInput: input,
            aircraftDialogMode: dialogMode,
            openAircraftDialog: true);

        return View("~/Views/Home/admin.cshtml", model);
    }

    private IActionResult? RedirectForNonAdmin()
    {
        if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserId")))
        {
            return RedirectToAction("Login", "Auth");
        }

        if (string.Equals(
                HttpContext.Session.GetString("Role")?.Trim(),
                "Admin",
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return HttpContext.Session.GetString("Role")?.Trim().ToUpperInvariant() switch
        {
            "CUSTOMER" => RedirectToAction("Index", "Home"),
            "STAFF" => RedirectToAction("Counter", "Home"),
            _ => RedirectToAction("Login", "Auth")
        };
    }

    private IActionResult RedirectToAircraftScreen(
        int? aircraftId,
        int routePage,
        string? routeSearch,
        string? routeType,
        int flightPage,
        string? flightSearch,
        string? flightStatus)
    {
        var url = Url.Action("Admin", "Home", new
        {
            routePage,
            routeSearch,
            routeType,
            flightPage,
            flightSearch,
            flightStatus,
            aircraftId
        });

        return Redirect($"{url}#aircraft");
    }

    private static void Normalize(AircraftInputModel input)
    {
        input.Model = (input.Model ?? "").Trim();
    }

    private void ValidateBusinessRows(AircraftInputModel input)
    {
        if (input.BusinessRows > input.RowCount)
        {
            ModelState.AddModelError(
                $"{AircraftInputPrefix}.{nameof(input.BusinessRows)}",
                "Số hàng thương gia không được vượt quá tổng số hàng ghế.");
        }
    }
}
