using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TEO_Airline_Project.Models;
using TEO_Airline_Project.Models.ViewModels;
using TEO_Airline_Project.Services;

namespace TEO_Airline_Project.Controllers;

public class PromotionsController : Controller
{
    private const string InputPrefix = nameof(AdminDashboardViewModel.PromotionInput);

    private readonly AirlineBookingDBContext _context;
    private readonly ILogger<PromotionsController> _logger;

    public PromotionsController(
        AirlineBookingDBContext context,
        ILogger<PromotionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(Prefix = InputPrefix)] PromotionInputModel input,
        string? promotionSearch = null,
        string? promotionStatus = null)
    {
        if (RedirectForNonAdmin() is { } redirect)
        {
            return redirect;
        }

        Normalize(input);
        ModelState.Clear();
        TryValidateModel(input, InputPrefix);
        await ValidateUniqueCodeAsync(input.Code);
        if (!ModelState.IsValid)
        {
            return await RenderAdminWithInputAsync(
                input, null, "create", promotionSearch, promotionStatus);
        }

        var promotion = new Promotion
        {
            Code = input.Code,
            DiscountPercent = input.DiscountPercent,
            DiscountAmount = input.DiscountAmount,
            MinOrderValue = input.MinOrderValue,
            PointsRequired = input.PointsRequired,
            StartDate = input.StartDate!.Value.Date,
            EndDate = EndOfDay(input.EndDate!.Value),
            UsageLimit = input.UsageLimit,
            UsedCount = 0
        };
        _context.Promotions.Add(promotion);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not create promotion {PromotionCode}.", input.Code);
            ModelState.AddModelError(
                string.Empty,
                "Không thể lưu mã khuyến mãi. Vui lòng kiểm tra dữ liệu và thử lại.");
            return await RenderAdminWithInputAsync(
                input, null, "create", promotionSearch, promotionStatus);
        }

        TempData["PromotionSuccess"] = $"Đã thêm mã khuyến mãi {promotion.Code}.";
        return RedirectToPromotions(promotionSearch, promotionStatus);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int promoId,
        [Bind(Prefix = InputPrefix)] PromotionInputModel input,
        string? promotionSearch = null,
        string? promotionStatus = null)
    {
        if (RedirectForNonAdmin() is { } redirect)
        {
            return redirect;
        }

        var promotion = await _context.Promotions
            .FirstOrDefaultAsync(item => item.PromoId == promoId);
        if (promotion is null)
        {
            TempData["PromotionError"] = "Không tìm thấy mã khuyến mãi cần cập nhật.";
            return RedirectToPromotions(promotionSearch, promotionStatus);
        }

        Normalize(input);
        ModelState.Clear();
        TryValidateModel(input, InputPrefix);
        await ValidateUniqueCodeAsync(input.Code, promoId);
        if (!ModelState.IsValid)
        {
            return await RenderAdminWithInputAsync(
                input, promoId, "edit", promotionSearch, promotionStatus);
        }

        promotion.Code = input.Code;
        promotion.DiscountPercent = input.DiscountPercent;
        promotion.DiscountAmount = input.DiscountAmount;
        promotion.MinOrderValue = input.MinOrderValue;
        promotion.PointsRequired = input.PointsRequired;
        promotion.StartDate = input.StartDate!.Value.Date;
        promotion.EndDate = EndOfDay(input.EndDate!.Value);
        promotion.UsageLimit = input.UsageLimit;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Could not update promotion {PromotionId}.", promoId);
            ModelState.AddModelError(
                string.Empty,
                "Không thể cập nhật mã khuyến mãi. Vui lòng kiểm tra dữ liệu và thử lại.");
            return await RenderAdminWithInputAsync(
                input, promoId, "edit", promotionSearch, promotionStatus);
        }

        TempData["PromotionSuccess"] = $"Đã cập nhật mã khuyến mãi {promotion.Code}.";
        return RedirectToPromotions(promotionSearch, promotionStatus);
    }

    [HttpGet]
    public async Task<IActionResult> Export()
    {
        if (RedirectForNonAdmin() is { } redirect)
        {
            return redirect;
        }

        var now = DateTime.Now;
        var promotions = await _context.Promotions
            .AsNoTracking()
            .OrderBy(promotion => promotion.StartDate)
            .ThenBy(promotion => promotion.Code)
            .Select(promotion => new PromotionExportRow
            {
                Code = promotion.Code,
                DiscountType = promotion.DiscountPercent != null ? "Phần trăm" : "Số tiền",
                DiscountValue = promotion.DiscountPercent ?? promotion.DiscountAmount ?? 0m,
                MinOrderValue = promotion.MinOrderValue,
                PointsRequired = promotion.PointsRequired,
                StartDate = promotion.StartDate,
                EndDate = promotion.EndDate,
                UsedCount = promotion.UsedCount ?? 0,
                UsageLimit = promotion.UsageLimit,
                Status = promotion.EndDate < now
                    ? "Đã kết thúc"
                    : promotion.StartDate > now
                        ? "Sắp diễn ra"
                        : (promotion.UsedCount ?? 0) >= promotion.UsageLimit
                            ? "Hết lượt"
                            : "Đang áp dụng"
            })
            .ToListAsync();

        var content = FlightPricingExcelExporter.CreatePromotions(promotions);
        var fileName = $"TEO_KhuyenMai_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return File(
            content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    private async Task ValidateUniqueCodeAsync(string code, int? excludedPromoId = null)
    {
        var alreadyExists = await _context.Promotions.AnyAsync(promotion =>
            promotion.Code == code &&
            (excludedPromoId == null || promotion.PromoId != excludedPromoId));
        if (alreadyExists)
        {
            ModelState.AddModelError(
                $"{InputPrefix}.{nameof(PromotionInputModel.Code)}",
                "Mã khuyến mãi này đã tồn tại.");
        }
    }

    private async Task<IActionResult> RenderAdminWithInputAsync(
        PromotionInputModel input,
        int? editingPromoId,
        string dialogMode,
        string? promotionSearch,
        string? promotionStatus)
    {
        var model = await AdminDashboardViewModelBuilder.BuildAsync(
            _context,
            promotionSearch: promotionSearch,
            promotionStatus: promotionStatus,
            promotionInput: input,
            editingPromotionId: editingPromoId,
            promotionDialogMode: dialogMode,
            openPromotionDialog: true);

        return View("~/Views/Home/admin.cshtml", model);
    }

    private IActionResult? RedirectForNonAdmin()
    {
        var userId = HttpContext.Session.GetString("UserId");
        var role = HttpContext.Session.GetString("Role")?.Trim();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Auth");
        }

        if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return role?.ToUpperInvariant() switch
            {
                "CUSTOMER" => RedirectToAction("Index", "Home"),
                "STAFF" => RedirectToAction("Counter", "Home"),
                _ => RedirectToAction("Login", "Auth")
            };
        }

        return null;
    }

    private IActionResult RedirectToPromotions(string? promotionSearch, string? promotionStatus)
    {
        var url = Url.Action(
            "Admin",
            "Home",
            new { promotionSearch, promotionStatus }) ?? "/Home/Admin";
        return Redirect($"{url}#promotions");
    }

    private static void Normalize(PromotionInputModel input)
    {
        input.Code = input.Code?.Trim() ?? "";
    }

    private static DateTime EndOfDay(DateTime date) =>
        date.Date.AddDays(1).AddTicks(-1);
}
