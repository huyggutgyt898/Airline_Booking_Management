using System.ComponentModel.DataAnnotations;

namespace TEO_Airline_Project.Models.ViewModels;

public sealed class FlightPricingInputModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn chuyến bay.")]
    public int FlightId { get; set; }

    [MinLength(6, ErrorMessage = "Cần nhập đủ sáu mức giá.")]
    [MaxLength(6, ErrorMessage = "Chỉ được nhập đủ sáu mức giá.")]
    public List<FlightPricingLineInputModel> Lines { get; set; } =
    [
        new() { SeatClass = "Economy", PassengerType = "Adult" },
        new() { SeatClass = "Economy", PassengerType = "Child" },
        new() { SeatClass = "Economy", PassengerType = "Infant" },
        new() { SeatClass = "Business", PassengerType = "Adult" },
        new() { SeatClass = "Business", PassengerType = "Child" },
        new() { SeatClass = "Business", PassengerType = "Infant" }
    ];
}

public sealed class FlightPricingLineInputModel
{
    public string SeatClass { get; set; } = "";

    public string PassengerType { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập giá cơ bản.")]
    [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Giá cơ bản phải từ 0 trở lên.")]
    public decimal? BasePrice { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiền thuế.")]
    [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Tiền thuế phải từ 0 trở lên.")]
    public decimal? TaxAmount { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập phí dịch vụ.")]
    [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Phí dịch vụ phải từ 0 trở lên.")]
    public decimal? ServiceFee { get; set; }
}

public sealed class FlightPricingEditInputModel
{
    [Required(ErrorMessage = "Vui lòng nhập giá cơ bản.")]
    [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Giá cơ bản phải từ 0 trở lên.")]
    public decimal? BasePrice { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiền thuế.")]
    [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Tiền thuế phải từ 0 trở lên.")]
    public decimal? TaxAmount { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập phí dịch vụ.")]
    [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Phí dịch vụ phải từ 0 trở lên.")]
    public decimal? ServiceFee { get; set; }
}
