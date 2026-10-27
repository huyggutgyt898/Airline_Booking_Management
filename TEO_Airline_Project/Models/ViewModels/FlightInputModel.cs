using System.ComponentModel.DataAnnotations;

namespace TEO_Airline_Project.Models.ViewModels;

public sealed class FlightInputModel
{
    public int FlightId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số hiệu chuyến bay.")]
    [StringLength(20, ErrorMessage = "Số hiệu chuyến bay không được vượt quá 20 ký tự.")]
    [Display(Name = "Số hiệu chuyến bay")]
    public string FlightNumber { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn tuyến bay.")]
    [Display(Name = "Tuyến bay")]
    public int RouteId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn máy bay.")]
    [Display(Name = "Máy bay")]
    public int AircraftId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thời gian khởi hành.")]
    [Display(Name = "Thời gian khởi hành")]
    public DateTime? DepartureTime { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thời gian đến.")]
    [Display(Name = "Thời gian đến")]
    public DateTime? ArrivalTime { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn trạng thái.")]
    [StringLength(30)]
    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = AdminFlightStatus.Scheduled;
}

public static class AdminFlightStatus
{
    public const string Scheduled = "Scheduled";
    public const string Delayed = "Delayed";
    public const string Cancelled = "Cancelled";
    public const string Completed = "Completed";

    public static IReadOnlyList<(string Value, string Label, string BadgeClass)> Options { get; } =
    [
        (Scheduled, "Theo lịch", "admin-badge-success"),
        (Delayed, "Trễ chuyến", "admin-badge-warning"),
        (Cancelled, "Đã hủy", "admin-badge-neutral"),
        (Completed, "Đã hoàn tất", "admin-badge-neutral")
    ];

    public static string LabelFor(string? status) =>
        Options.FirstOrDefault(option => option.Value == status).Label ?? status ?? "";

    public static string BadgeClassFor(string? status) =>
        Options.FirstOrDefault(option => option.Value == status).BadgeClass ?? "admin-badge-neutral";
}
