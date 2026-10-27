using System.ComponentModel.DataAnnotations;

namespace TEO_Airline_Project.Models.ViewModels;

public sealed class AirportInputModel
{
    [Required(ErrorMessage = "Vui lòng nhập mã sân bay.")]
    [StringLength(10, ErrorMessage = "Mã sân bay không được vượt quá 10 ký tự.")]
    [Display(Name = "Mã sân bay")]
    public string Code { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập tên sân bay.")]
    [StringLength(150, ErrorMessage = "Tên sân bay không được vượt quá 150 ký tự.")]
    [Display(Name = "Tên sân bay")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập thành phố.")]
    [StringLength(100, ErrorMessage = "Thành phố không được vượt quá 100 ký tự.")]
    [Display(Name = "Thành phố")]
    public string City { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập quốc gia.")]
    [StringLength(100, ErrorMessage = "Quốc gia không được vượt quá 100 ký tự.")]
    [Display(Name = "Quốc gia")]
    public string Country { get; set; } = "";
}
