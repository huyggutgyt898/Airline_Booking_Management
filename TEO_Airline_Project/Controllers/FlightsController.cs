using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TEO_Airline_Project.Models;
using TEO_Airline_Project.Models.ViewModels;
using TEO_Airline_Project.Services;

namespace TEO_Airline_Project.Controllers;

public class FlightsController : Controller
{
    private const string FlightInputPrefix = nameof(AdminDashboardViewModel.FlightInput);

    private readonly AirlineBookingDBContext _context;
    private readonly ILogger<FlightsController> _logger;

    public FlightsController(
        AirlineBookingDBContext context,
        ILogger<FlightsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(Prefix = FlightInputPrefix)] FlightInputModel input,
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

        Normalize(input);
        ModelState.Clear();
        TryValidateModel(input, FlightInputPrefix);
        await ValidateFlightInputAsync(input);
        if (!ModelState.IsValid)
        {
            return await RenderAdminWithInputAsync(
                input, "create", routePage, routeSearch, routeType,
                flightPage, flightSearch, flightStatus);
        }

        _context.Flights.Add(new Flight
        {
            FlightNumber = input.FlightNumber,
            RouteId = input.RouteId,
            AircraftId = input.AircraftId,
            DepartureTime = input.DepartureTime!.Value,
            ArrivalTime = input.ArrivalTime!.Value,
            Status = input.Status
        });

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not create flight {FlightNumber}.", input.FlightNumber);
            TempData["FlightError"] = "Không thể thêm chuyến bay do lỗi cơ sở dữ liệu. Vui lòng kiểm tra thông tin và thử lại.";
            return RedirectToFlightList(routePage, routeSearch, routeType, flightPage, flightSearch, flightStatus);
        }

        TempData["FlightSuccess"] = $"Đã thêm chuyến bay {input.FlightNumber}.";
        return RedirectToFlightList(routePage, routeSearch, routeType, flightPage, flightSearch, flightStatus);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int flightId,
        [Bind(Prefix = FlightInputPrefix)] FlightInputModel input,
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

        var flight = await _context.Flights.FirstOrDefaultAsync(item => item.FlightId == flightId);
        if (flight is null)
        {
            TempData["FlightError"] = "Không tìm thấy chuyến bay cần cập nhật.";
            return RedirectToFlightList(routePage, routeSearch, routeType, flightPage, flightSearch, flightStatus);
        }

        input.FlightId = flight.FlightId;
        Normalize(input);
        ModelState.Clear();
        TryValidateModel(input, FlightInputPrefix);
        await ValidateFlightInputAsync(input);
        if (!ModelState.IsValid)
        {
            return await RenderAdminWithInputAsync(
                input, "edit", routePage, routeSearch, routeType,
                flightPage, flightSearch, flightStatus);
        }

        flight.FlightNumber = input.FlightNumber;
        flight.RouteId = input.RouteId;
        flight.AircraftId = input.AircraftId;
        flight.DepartureTime = input.DepartureTime!.Value;
        flight.ArrivalTime = input.ArrivalTime!.Value;
        flight.Status = input.Status;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not update flight {FlightId}.", flightId);
            TempData["FlightError"] = "Không thể cập nhật chuyến bay do lỗi cơ sở dữ liệu. Vui lòng kiểm tra thông tin và thử lại.";
            return RedirectToFlightList(routePage, routeSearch, routeType, flightPage, flightSearch, flightStatus);
        }

        TempData["FlightSuccess"] = $"Đã cập nhật chuyến bay {input.FlightNumber}.";
        return RedirectToFlightList(routePage, routeSearch, routeType, flightPage, flightSearch, flightStatus);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        int flightId,
        string flightNumber,
        string confirmationFlightNumber,
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

        var flight = await _context.Flights.FirstOrDefaultAsync(item => item.FlightId == flightId);
        if (flight is null)
        {
            TempData["FlightError"] = "Không tìm thấy chuyến bay cần xóa.";
            return RedirectToFlightList(routePage, routeSearch, routeType, flightPage, flightSearch, flightStatus);
        }

        if (!string.Equals(flight.FlightNumber, flightNumber, StringComparison.Ordinal) ||
            !string.Equals(flight.FlightNumber, confirmationFlightNumber, StringComparison.Ordinal))
        {
            TempData["FlightError"] = "Không thể xóa chuyến bay vì số hiệu xác nhận không khớp.";
            return RedirectToFlightList(routePage, routeSearch, routeType, flightPage, flightSearch, flightStatus);
        }

        var hasRelatedData =
            await _context.Tickets.AnyAsync(ticket => ticket.FlightId == flightId) ||
            await _context.FlightPricings.AnyAsync(pricing => pricing.FlightId == flightId) ||
            await _context.BookingRequests.AnyAsync(request => request.NewFlightId == flightId);
        if (hasRelatedData)
        {
            _logger.LogWarning(
                "Could not delete flight {FlightId} because related tickets, pricing, or booking requests exist.",
                flightId);
            TempData["FlightError"] =
                $"Không thể xóa chuyến {flight.FlightNumber} vì vẫn còn vé, yêu cầu đổi chuyến hoặc giá liên quan.";
            return RedirectToFlightList(routePage, routeSearch, routeType, flightPage, flightSearch, flightStatus);
        }

        _context.Flights.Remove(flight);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsSqlError(exception, 547))
        {
            _logger.LogWarning(exception, "Could not delete flight {FlightId} because related data exists.", flightId);
            TempData["FlightError"] =
                $"Không thể xóa chuyến {flight.FlightNumber} vì dữ liệu liên quan đang sử dụng chuyến bay này.";
            return RedirectToFlightList(routePage, routeSearch, routeType, flightPage, flightSearch, flightStatus);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not delete flight {FlightId}.", flightId);
            TempData["FlightError"] = "Không thể xóa chuyến bay do lỗi cơ sở dữ liệu. Vui lòng thử lại.";
            return RedirectToFlightList(routePage, routeSearch, routeType, flightPage, flightSearch, flightStatus);
        }

        TempData["FlightSuccess"] = $"Đã xóa chuyến bay {flight.FlightNumber}.";
        return RedirectToFlightList(routePage, routeSearch, routeType, flightPage, flightSearch, flightStatus);
    }

    private async Task ValidateFlightInputAsync(FlightInputModel input)
    {
        if (!AdminFlightStatus.Options.Any(option => option.Value == input.Status))
        {
            ModelState.AddModelError(
                $"{FlightInputPrefix}.{nameof(input.Status)}",
                "Trạng thái chuyến bay không hợp lệ.");
        }

        if (input.DepartureTime.HasValue &&
            input.ArrivalTime.HasValue &&
            input.ArrivalTime.Value <= input.DepartureTime.Value)
        {
            ModelState.AddModelError(
                $"{FlightInputPrefix}.{nameof(input.ArrivalTime)}",
                "Thời gian đến phải sau thời gian khởi hành.");
        }

        if (!await _context.Routes.AnyAsync(route => route.RouteId == input.RouteId))
        {
            ModelState.AddModelError(
                $"{FlightInputPrefix}.{nameof(input.RouteId)}",
                "Tuyến bay đã chọn không còn tồn tại.");
        }

        if (!await _context.Aircrafts.AnyAsync(aircraft => aircraft.AircraftId == input.AircraftId))
        {
            ModelState.AddModelError(
                $"{FlightInputPrefix}.{nameof(input.AircraftId)}",
                "Máy bay đã chọn không còn tồn tại.");
        }
    }

    private static void Normalize(FlightInputModel input)
    {
        input.FlightNumber = input.FlightNumber?.Trim() ?? "";
        input.Status = input.Status?.Trim() ?? "";
    }

    private async Task<IActionResult> RenderAdminWithInputAsync(
        FlightInputModel input,
        string mode,
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
            flightInput: input,
            flightDialogMode: mode,
            openFlightDialog: true);

        return View("~/Views/Home/admin.cshtml", model);
    }

    private IActionResult? RedirectForNonAdmin()
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
            return HttpContext.Session.GetString("Role")?.Trim().ToUpperInvariant() switch
            {
                "CUSTOMER" => RedirectToAction("Index", "Home"),
                "STAFF" => RedirectToAction("Counter", "Home"),
                _ => RedirectToAction("Login", "Auth")
            };
        }

        return null;
    }

    private IActionResult RedirectToFlightList(
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
            flightStatus
        });
        return Redirect($"{url}#flights");
    }

    private static bool IsSqlError(DbUpdateException exception, int number) =>
        exception.GetBaseException() is SqlException sqlException &&
        sqlException.Number == number;
}
