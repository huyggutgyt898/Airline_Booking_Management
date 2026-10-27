using System.ComponentModel.DataAnnotations;

namespace TEO_Airline_Project.Models.ViewModels;

public sealed class RouteInputModel
{
    public int RouteId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn sân bay đi.")]
    [StringLength(10)]
    [Display(Name = "Sân bay đi")]
    public string DepartureAirportCode { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn sân bay đến.")]
    [StringLength(10)]
    [Display(Name = "Sân bay đến")]
    public string ArrivalAirportCode { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn loại tuyến.")]
    [StringLength(20)]
    [Display(Name = "Loại tuyến")]
    public string RouteType { get; set; } = "Domestic";
}
