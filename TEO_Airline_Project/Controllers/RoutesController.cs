using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TEO_Airline_Project.Models;
using TEO_Airline_Project.Models.ViewModels;
using TEO_Airline_Project.Services;
using AirportRoute = TEO_Airline_Project.Models.Route;

namespace TEO_Airline_Project.Controllers;

public class RoutesController : Controller
{
    private const string RouteInputPrefix = nameof(AdminDashboardViewModel.RouteInput);

    private readonly AirlineBookingDBContext _context;
    private readonly ILogger<RoutesController> _logger;

    public RoutesController(
        AirlineBookingDBContext context,
        ILogger<RoutesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(Prefix = RouteInputPrefix)] RouteInputModel input,
        int routePage = 1,
        string? routeSearch = null,
        string? routeType = null)
    {
        if (RedirectForNonAdmin() is { } redirect)
        {
            return redirect;
        }

        Normalize(input);
        await ValidateRouteInputAsync(input);
        if (!ModelState.IsValid)
        {
            return await RenderAdminWithInputAsync(input, "create", routePage, routeSearch, routeType);
        }

        if (await _context.Routes.AnyAsync(route =>
                route.DepartureAirportCode == input.DepartureAirportCode &&
                route.ArrivalAirportCode == input.ArrivalAirportCode))
        {
            ModelState.AddModelError(
                string.Empty,
                "Tuyến bay giữa hai sân bay này đã tồn tại.");
            return await RenderAdminWithInputAsync(input, "create", routePage, routeSearch, routeType);
        }

        _context.Routes.Add(new AirportRoute
        {
            DepartureAirportCode = input.DepartureAirportCode,
            ArrivalAirportCode = input.ArrivalAirportCode,
            RouteType = input.RouteType
        });

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsSqlError(exception, 2601, 2627))
        {
            _logger.LogWarning(
                exception,
                "Could not create duplicate route {DepartureAirportCode} to {ArrivalAirportCode}.",
                input.DepartureAirportCode,
                input.ArrivalAirportCode);
            ModelState.AddModelError(string.Empty, "Tuyến bay giữa hai sân bay này đã tồn tại.");
            return await RenderAdminWithInputAsync(input, "create", routePage, routeSearch, routeType);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not create route.");
            ModelState.AddModelError(
                string.Empty,
                "Không thể lưu tuyến bay do lỗi cơ sở dữ liệu. Vui lòng kiểm tra sân bay và thử lại.");
            return await RenderAdminWithInputAsync(input, "create", routePage, routeSearch, routeType);
        }

        TempData["RouteSuccess"] = "Đã thêm tuyến bay.";
        return RedirectToRouteList(routePage, routeSearch, routeType);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int routeId,
        [Bind(Prefix = RouteInputPrefix)] RouteInputModel input,
        int routePage = 1,
        string? routeSearch = null,
        string? routeType = null)
    {
        if (RedirectForNonAdmin() is { } redirect)
        {
            return redirect;
        }

        var route = await _context.Routes.FirstOrDefaultAsync(item => item.RouteId == routeId);
        if (route is null)
        {
            TempData["RouteError"] = "Không tìm thấy tuyến bay cần cập nhật.";
            return RedirectToRouteList(routePage, routeSearch, routeType);
        }

        input.RouteId = route.RouteId;
        Normalize(input);
        await ValidateRouteInputAsync(input);
        if (!ModelState.IsValid)
        {
            return await RenderAdminWithInputAsync(input, "edit", routePage, routeSearch, routeType);
        }

        if (await _context.Routes.AnyAsync(item =>
                item.RouteId != routeId &&
                item.DepartureAirportCode == input.DepartureAirportCode &&
                item.ArrivalAirportCode == input.ArrivalAirportCode))
        {
            ModelState.AddModelError(
                string.Empty,
                "Tuyến bay giữa hai sân bay này đã tồn tại.");
            return await RenderAdminWithInputAsync(input, "edit", routePage, routeSearch, routeType);
        }

        route.DepartureAirportCode = input.DepartureAirportCode;
        route.ArrivalAirportCode = input.ArrivalAirportCode;
        route.RouteType = input.RouteType;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsSqlError(exception, 2601, 2627))
        {
            _logger.LogWarning(exception, "Could not update route {RouteId} due to a duplicate route.", routeId);
            ModelState.AddModelError(string.Empty, "Tuyến bay giữa hai sân bay này đã tồn tại.");
            return await RenderAdminWithInputAsync(input, "edit", routePage, routeSearch, routeType);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not update route {RouteId}.", routeId);
            ModelState.AddModelError(
                string.Empty,
                "Không thể cập nhật tuyến bay do lỗi cơ sở dữ liệu. Vui lòng kiểm tra sân bay và thử lại.");
            return await RenderAdminWithInputAsync(input, "edit", routePage, routeSearch, routeType);
        }

        TempData["RouteSuccess"] = $"Đã cập nhật tuyến bay R-{routeId:D3}.";
        return RedirectToRouteList(routePage, routeSearch, routeType);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        int routeId,
        int routePage = 1,
        string? routeSearch = null,
        string? routeType = null)
    {
        if (RedirectForNonAdmin() is { } redirect)
        {
            return redirect;
        }

        var route = await _context.Routes.FirstOrDefaultAsync(item => item.RouteId == routeId);
        if (route is null)
        {
            TempData["RouteError"] = "Không tìm thấy tuyến bay cần xóa.";
            return RedirectToRouteList(routePage, routeSearch, routeType);
        }

        if (await _context.Flights.AnyAsync(flight => flight.RouteId == routeId))
        {
            TempData["RouteError"] =
                $"Không thể xóa tuyến bay R-{routeId:D3} vì tuyến này đang có chuyến bay.";
            return RedirectToRouteList(routePage, routeSearch, routeType);
        }

        _context.Routes.Remove(route);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsSqlError(exception, 547))
        {
            _logger.LogWarning(exception, "Could not delete route {RouteId} because related data exists.", routeId);
            TempData["RouteError"] =
                $"Không thể xóa tuyến bay R-{routeId:D3} vì dữ liệu chuyến bay liên quan đang sử dụng tuyến này.";
            return RedirectToRouteList(routePage, routeSearch, routeType);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not delete route {RouteId}.", routeId);
            TempData["RouteError"] = "Không thể xóa tuyến bay do lỗi cơ sở dữ liệu. Vui lòng thử lại.";
            return RedirectToRouteList(routePage, routeSearch, routeType);
        }

        TempData["RouteSuccess"] = $"Đã xóa tuyến bay R-{routeId:D3}.";
        return RedirectToRouteList(routePage, routeSearch, routeType);
    }

    private async Task<IActionResult> RenderAdminWithInputAsync(
        RouteInputModel input,
        string dialogMode,
        int routePage,
        string? routeSearch,
        string? routeType)
    {
        var model = await AdminDashboardViewModelBuilder.BuildAsync(
            _context,
            routePage,
            routeSearch,
            routeType,
            input,
            dialogMode,
            openRouteDialog: true);

        return View("~/Views/Home/admin.cshtml", model);
    }

    private IActionResult RedirectToRouteList(int routePage, string? routeSearch, string? routeType)
    {
        return Redirect(Url.Action("Admin", "Home", new
        {
            routePage,
            routeSearch,
            routeType
        }) + "#routes");
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

    private async Task ValidateRouteInputAsync(RouteInputModel input)
    {
        if (!string.IsNullOrEmpty(input.DepartureAirportCode) &&
            !string.IsNullOrEmpty(input.ArrivalAirportCode) &&
            input.DepartureAirportCode == input.ArrivalAirportCode)
        {
            ModelState.AddModelError(
                $"{RouteInputPrefix}.{nameof(input.ArrivalAirportCode)}",
                "Sân bay đi và sân bay đến phải khác nhau.");
        }

        if (input.RouteType is not ("Domestic" or "International"))
        {
            ModelState.AddModelError(
                $"{RouteInputPrefix}.{nameof(input.RouteType)}",
                "Vui lòng chọn loại tuyến hợp lệ.");
        }

        var requestedAirportCodes = new[]
        {
            input.DepartureAirportCode,
            input.ArrivalAirportCode
        }.Where(code => !string.IsNullOrEmpty(code)).Distinct().ToArray();
        var existingAirportCodes = await _context.Airports
            .Where(airport => requestedAirportCodes.Contains(airport.AirportCode))
            .Select(airport => airport.AirportCode)
            .ToListAsync();

        if (!string.IsNullOrEmpty(input.DepartureAirportCode) &&
            !existingAirportCodes.Contains(input.DepartureAirportCode))
        {
            ModelState.AddModelError(
                $"{RouteInputPrefix}.{nameof(input.DepartureAirportCode)}",
                "Sân bay đi không tồn tại.");
        }

        if (!string.IsNullOrEmpty(input.ArrivalAirportCode) &&
            !existingAirportCodes.Contains(input.ArrivalAirportCode))
        {
            ModelState.AddModelError(
                $"{RouteInputPrefix}.{nameof(input.ArrivalAirportCode)}",
                "Sân bay đến không tồn tại.");
        }
    }

    private static void Normalize(RouteInputModel input)
    {
        input.DepartureAirportCode = (input.DepartureAirportCode ?? "").Trim().ToUpperInvariant();
        input.ArrivalAirportCode = (input.ArrivalAirportCode ?? "").Trim().ToUpperInvariant();
        input.RouteType = (input.RouteType ?? "").Trim();
    }

    private static bool IsSqlError(DbUpdateException exception, params int[] numbers)
    {
        return exception.GetBaseException() is SqlException sqlException &&
            numbers.Contains(sqlException.Number);
    }
}
