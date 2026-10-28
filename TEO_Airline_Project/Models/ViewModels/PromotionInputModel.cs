using System.ComponentModel.DataAnnotations;

namespace TEO_Airline_Project.Models.ViewModels;

public sealed class PromotionInputModel : IValidatableObject
{
    [Required(ErrorMessage = "Vui lòng nhập mã khuyến mãi.")]
    [StringLength(50, ErrorMessage = "Mã khuyến mãi không được vượt quá 50 ký tự.")]
    public string Code { get; set; } = "";

    [Range(typeof(decimal), "0.01", "100", ErrorMessage = "Phần trăm giảm phải từ 0,01 đến 100.")]
    public decimal? DiscountPercent { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99", ErrorMessage = "Số tiền giảm phải lớn hơn 0.")]
    public decimal? DiscountAmount { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Giá trị đơn tối thiểu không hợp lệ.")]
    public decimal? MinOrderValue { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Điểm đổi không hợp lệ.")]
    public int? PointsRequired { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu.")]
    public DateTime? StartDate { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc.")]
    public DateTime? EndDate { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Giới hạn lượt dùng phải lớn hơn 0.")]
    public int UsageLimit { get; set; } = 1;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if ((DiscountPercent is null) == (DiscountAmount is null))
        {
            yield return new ValidationResult(
                "Nhập đúng một hình thức giảm: phần trăm hoặc số tiền.",
                [nameof(DiscountPercent), nameof(DiscountAmount)]);
        }

        if (StartDate is DateTime startDate &&
            EndDate is DateTime endDate &&
            startDate.Date > endDate.Date)
        {
            yield return new ValidationResult(
                "Ngày kết thúc phải bằng hoặc sau ngày bắt đầu.",
                [nameof(EndDate)]);
        }
    }
}
