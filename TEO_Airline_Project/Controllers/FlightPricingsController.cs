using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TEO_Airline_Project.Models;
using TEO_Airline_Project.Models.ViewModels;
using TEO_Airline_Project.Services;

namespace TEO_Airline_Project.Controllers;

public class FlightPricingsController : Controller
{
    private const string InputPrefix = nameof(AdminDashboardViewModel.FlightPricingInput);
    private const string EditInputPrefix = nameof(AdminDashboardViewModel.FlightPricingEditInput);
    private static readonly (string SeatClass, string PassengerType)[] PricingTiers =
    [
        ("Economy", "Adult"),
        ("Economy", "Child"),
        ("Economy", "Infant"),
        ("Business", "Adult"),
        ("Business", "Child"),
        ("Business", "Infant")
    ];

    private readonly AirlineBookingDBContext _context;
    private readonly ILogger<FlightPricingsController> _logger;

    public FlightPricingsController(
        AirlineBookingDBContext context,
        ILogger<FlightPricingsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(Prefix = InputPrefix)] FlightPricingInputModel input,
        int? selectedPricingFlightId = null)
    {
        if (RedirectForNonAdmin() is { } redirect)
        {
            return redirect;
        }

        input.Lines ??= new FlightPricingInputModel().Lines;
        for (var index = 0; index < input.Lines.Count && index < PricingTiers.Length; index++)
        {
            input.Lines[index] ??= new FlightPricingLineInputModel
            {
                SeatClass = PricingTiers[index].SeatClass,
                PassengerType = PricingTiers[index].PassengerType
            };
        }
        ModelState.Clear();
        TryValidateModel(input, InputPrefix);
        ValidateTiers(input);

        var flightExists = input.FlightId > 0 &&
            await _context.Flights.AnyAsync(flight => flight.FlightId == input.FlightId);
        var flightAlreadyHasPricing = flightExists &&
            await _context.FlightPricings.AnyAsync(pricing => pricing.FlightId == input.FlightId);

        if (!flightExists)
        {
            ModelState.AddModelError($"{InputPrefix}.{nameof(input.FlightId)}", "Vui lòng chọn một chuyến bay hợp lệ.");
        }
        else if (flightAlreadyHasPricing)
        {
            ModelState.AddModelError(
                $"{InputPrefix}.{nameof(input.FlightId)}",
                "Chuyến bay này đã có bảng giá. Hãy chọn chuyến bay chưa có mức giá.");
        }

        if (!ModelState.IsValid)
        {
            return await RenderAdminWithInputAsync(
                input, null, selectedPricingFlightId, openCreate: true);
        }

        var prices = input.Lines.Select(line => new FlightPricing
        {
            FlightId = input.FlightId,
            SeatClass = line.SeatClass,
            PassengerType = line.PassengerType,
            BasePrice = line.BasePrice!.Value,
            TaxAmount = line.TaxAmount!.Value,
            ServiceFee = line.ServiceFee!.Value
        }).ToList();

        _context.FlightPricings.AddRange(prices);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(
                exception,
                "Could not create pricing for flight {FlightId}.",
                input.FlightId);
            TempData["PricingError"] =
                "Không thể lưu bảng giá cho chuyến bay. Vui lòng kiểm tra dữ liệu và thử lại.";
            return RedirectToPricing(input.FlightId);
        }

        TempData["PricingSuccess"] = "Đã thêm đủ sáu mức giá cho chuyến bay.";
        return RedirectToPricing(input.FlightId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int priceId,
        [Bind(Prefix = EditInputPrefix)] FlightPricingEditInputModel input,
        int? pricingFlightId = null)
    {
        if (RedirectForNonAdmin() is { } redirect)
        {
            return redirect;
        }

        var pricing = await _context.FlightPricings
            .FirstOrDefaultAsync(item => item.PriceId == priceId);
        if (pricing is null)
        {
            TempData["PricingError"] = "Không tìm thấy mức giá cần cập nhật.";
            return RedirectToPricing(pricingFlightId);
        }

        ModelState.Clear();
        TryValidateModel(input, EditInputPrefix);
        if (!ModelState.IsValid)
        {
            return await RenderAdminWithInputAsync(
                null, input, pricing.FlightId, openEdit: true, editPriceId: priceId);
        }

        pricing.BasePrice = input.BasePrice!.Value;
        pricing.TaxAmount = input.TaxAmount!.Value;
        pricing.ServiceFee = input.ServiceFee!.Value;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not update pricing {PriceId}.", priceId);
            TempData["PricingError"] =
                "Không thể cập nhật mức giá. Vui lòng kiểm tra dữ liệu và thử lại.";
            return RedirectToPricing(pricing.FlightId);
        }

        TempData["PricingSuccess"] = "Đã cập nhật mức giá.";
        return RedirectToPricing(pricing.FlightId);
    }

    [HttpGet]
    public async Task<IActionResult> Export()
    {
        if (RedirectForNonAdmin() is { } redirect)
        {
            return redirect;
        }

        var prices = await _context.FlightPricings
            .AsNoTracking()
            .OrderBy(pricing => pricing.Flight.DepartureTime)
            .ThenBy(pricing => pricing.Flight.FlightNumber)
            .ThenBy(pricing => pricing.SeatClass)
            .ThenBy(pricing => pricing.PassengerType)
            .Select(pricing => new FlightPricingExportRow
            {
                FlightNumber = pricing.Flight.FlightNumber,
                DepartureAirportCode = pricing.Flight.Route.DepartureAirportCode,
                ArrivalAirportCode = pricing.Flight.Route.ArrivalAirportCode,
                DepartureTime = pricing.Flight.DepartureTime,
                AircraftModel = pricing.Flight.Aircraft.Model,
                SeatClass = pricing.SeatClass,
                PassengerType = pricing.PassengerType,
                BasePrice = pricing.BasePrice,
                TaxAmount = pricing.TaxAmount,
                ServiceFee = pricing.ServiceFee
            })
            .ToListAsync();

        var content = FlightPricingExcelExporter.Create(prices);
        var fileName = $"TEO_BangGia_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return File(
            content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    private void ValidateTiers(FlightPricingInputModel input)
    {
        if (input.Lines is null || input.Lines.Count != PricingTiers.Length)
        {
            ModelState.AddModelError(
                $"{InputPrefix}.{nameof(input.Lines)}",
                "Bảng giá cần đủ sáu tổ hợp hạng ghế và loại khách.");
            return;
        }

        for (var index = 0; index < PricingTiers.Length; index++)
        {
            if (!string.Equals(input.Lines[index].SeatClass, PricingTiers[index].SeatClass, StringComparison.Ordinal) ||
                !string.Equals(input.Lines[index].PassengerType, PricingTiers[index].PassengerType, StringComparison.Ordinal))
            {
                ModelState.AddModelError(
                    $"{InputPrefix}.{nameof(input.Lines)}",
                    "Không thể thay đổi hạng ghế hoặc loại khách của các mức giá.");
                return;
            }
        }
    }

    private async Task<IActionResult> RenderAdminWithInputAsync(
        FlightPricingInputModel? createInput,
        FlightPricingEditInputModel? editInput,
        int? selectedPricingFlightId,
        bool openCreate = false,
        bool openEdit = false,
        int? editPriceId = null)
    {
        var model = await AdminDashboardViewModelBuilder.BuildAsync(
            _context,
            pricingFlightId: selectedPricingFlightId,
            flightPricingInput: createInput,
            openFlightPricingDialog: openCreate,
            editingFlightPriceId: editPriceId,
            flightPricingEditInput: editInput,
            openFlightPricingEditDialog: openEdit);

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

    private IActionResult RedirectToPricing(int? flightId)
    {
        var url = Url.Action("Admin", "Home", new { pricingFlightId = flightId });
        return Redirect($"{url}#pricing");
    }
}
