using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TEO_Airline_Project.Models;
using TEO_Airline_Project.Models.ViewModels;
using TEO_Airline_Project.Services;

namespace TEO_Airline_Project.Controllers;

public class AirportsController : Controller
{
    private readonly AirlineBookingDBContext _context;
    private readonly ILogger<AirportsController> _logger;

    public AirportsController(
        AirlineBookingDBContext context,
        ILogger<AirportsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(Prefix = nameof(AdminDashboardViewModel.AirportInput))] AirportInputModel input)
    {
        if (RedirectForNonAdmin() is { } redirect)
        {
            return redirect;
        }

        Normalize(input);
        ModelState.Clear();
        if (!TryValidateModel(input, nameof(AdminDashboardViewModel.AirportInput)))
        {
            return await RenderAdminWithInputAsync(input, "create");
        }

        if (await _context.Airports.AnyAsync(airport => airport.AirportCode == input.Code))
        {
            ModelState.AddModelError(
                $"{nameof(AdminDashboardViewModel.AirportInput)}.{nameof(input.Code)}",
                "Mã sân bay này đã tồn tại.");
            return await RenderAdminWithInputAsync(input, "create");
        }

        _context.Airports.Add(new Airport
        {
            AirportCode = input.Code,
            AirportName = input.Name,
            City = input.City,
            Country = input.Country
        });

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsSqlError(exception, 2601, 2627))
        {
            _logger.LogWarning(exception, "Could not create airport {AirportCode}.", input.Code);
            ModelState.AddModelError(
                $"{nameof(AdminDashboardViewModel.AirportInput)}.{nameof(input.Code)}",
                "Không thể lưu sân bay. Hãy kiểm tra mã sân bay có bị trùng không.");
            return await RenderAdminWithInputAsync(input, "create");
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not save airport {AirportCode}.", input.Code);
            ModelState.AddModelError(
                string.Empty,
                "Không thể lưu sân bay do lỗi cơ sở dữ liệu. Vui lòng thử lại.");
            return await RenderAdminWithInputAsync(input, "create");
        }

        TempData["AirportSuccess"] = $"Đã thêm sân bay {input.Code}.";
        return RedirectToAirportList();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string? airportCode,
        [Bind(Prefix = nameof(AdminDashboardViewModel.AirportInput))] AirportInputModel input)
    {
        if (RedirectForNonAdmin() is { } redirect)
        {
            return redirect;
        }

        if (string.IsNullOrWhiteSpace(airportCode))
        {
            TempData["AirportError"] = "Thiếu mã sân bay cần cập nhật.";
            return RedirectToAirportList();
        }

        airportCode = airportCode.Trim().ToUpperInvariant();
        var airport = await _context.Airports
            .FirstOrDefaultAsync(item => item.AirportCode == airportCode);
        if (airport is null)
        {
            TempData["AirportError"] = "Không tìm thấy sân bay cần cập nhật.";
            return RedirectToAirportList();
        }

        input.Code = airport.AirportCode;
        Normalize(input);
        ModelState.Clear();
        if (!TryValidateModel(input, nameof(AdminDashboardViewModel.AirportInput)))
        {
            return await RenderAdminWithInputAsync(input, "edit");
        }

        airport.AirportName = input.Name;
        airport.City = input.City;
        airport.Country = input.Country;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not update airport {AirportCode}.", airportCode);
            TempData["AirportError"] = "Không thể cập nhật sân bay do lỗi cơ sở dữ liệu. Vui lòng thử lại.";
            return RedirectToAirportList();
        }

        TempData["AirportSuccess"] = $"Đã cập nhật sân bay {airportCode}.";
        return RedirectToAirportList();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string? airportCode, string? confirmationCode)
    {
        if (RedirectForNonAdmin() is { } redirect)
        {
            return redirect;
        }

        if (string.IsNullOrWhiteSpace(airportCode))
        {
            TempData["AirportError"] = "Thiếu mã sân bay cần xóa.";
            return RedirectToAirportList();
        }

        airportCode = airportCode.Trim().ToUpperInvariant();
        var airport = await _context.Airports
            .FirstOrDefaultAsync(item => item.AirportCode == airportCode);
        if (airport is null)
        {
            TempData["AirportError"] = "Không tìm thấy sân bay cần xóa.";
            return RedirectToAirportList();
        }

        if (!string.Equals(confirmationCode, airport.AirportCode, StringComparison.Ordinal))
        {
            TempData["AirportError"] =
                $"Mã xác nhận không chính xác. Chưa xóa sân bay {airport.AirportCode}.";
            return RedirectToAirportList();
        }

        var isUsedByRoute = await _context.Routes.AnyAsync(route =>
            route.DepartureAirportCode == airportCode ||
            route.ArrivalAirportCode == airportCode);
        if (isUsedByRoute)
        {
            TempData["AirportError"] =
                $"Không thể xóa sân bay {airportCode} vì đang được sử dụng trong tuyến bay.";
            return RedirectToAirportList();
        }

        _context.Airports.Remove(airport);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsSqlError(exception, 547))
        {
            _logger.LogWarning(exception, "Could not delete airport {AirportCode}.", airportCode);
            TempData["AirportError"] =
                $"Không thể xóa sân bay {airportCode} vì dữ liệu liên quan đang sử dụng sân bay này.";
            return RedirectToAirportList();
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not delete airport {AirportCode}.", airportCode);
            TempData["AirportError"] =
                "Không thể xóa sân bay do lỗi cơ sở dữ liệu. Vui lòng thử lại.";
            return RedirectToAirportList();
        }

        TempData["AirportSuccess"] = $"Đã xóa sân bay {airportCode}.";
        return RedirectToAirportList();
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

    private async Task<IActionResult> RenderAdminWithInputAsync(
        AirportInputModel input,
        string dialogMode)
    {
        var model = await AdminDashboardViewModelBuilder.BuildAsync(
            _context,
            airportInput: input,
            airportDialogMode: dialogMode,
            openAirportDialog: true);

        return View("~/Views/Home/admin.cshtml", model);
    }

    private IActionResult RedirectToAirportList()
    {
        return Redirect($"{Url.Action("Admin", "Home")}#airports");
    }

    private static void Normalize(AirportInputModel input)
    {
        input.Code = (input.Code ?? "").Trim().ToUpperInvariant();
        input.Name = (input.Name ?? "").Trim();
        input.City = (input.City ?? "").Trim();
        input.Country = (input.Country ?? "").Trim();
    }

    private static bool IsSqlError(DbUpdateException exception, params int[] numbers)
    {
        return exception.GetBaseException() is SqlException sqlException &&
            numbers.Contains(sqlException.Number);
    }
}
