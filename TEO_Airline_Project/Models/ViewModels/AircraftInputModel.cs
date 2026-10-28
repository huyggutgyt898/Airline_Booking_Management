using System.ComponentModel.DataAnnotations;

namespace TEO_Airline_Project.Models.ViewModels;

public sealed class AircraftInputModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên model máy bay.")]
    [StringLength(100, ErrorMessage = "Tên model không được vượt quá 100 ký tự.")]
    [Display(Name = "Model máy bay")]
    public string Model { get; set; } = "";

    [Range(1, 99, ErrorMessage = "Số hàng ghế phải từ 1 đến 99.")]
    [Display(Name = "Số hàng ghế")]
    public int RowCount { get; set; } = 30;

    [Range(1, 6, ErrorMessage = "Số ghế mỗi hàng phải từ 1 đến 6.")]
    [Display(Name = "Số ghế mỗi hàng")]
    public int SeatsPerRow { get; set; } = 6;

    [Range(0, 99, ErrorMessage = "Số hàng thương gia phải từ 0 đến 99.")]
    [Display(Name = "Số hàng thương gia")]
    public int BusinessRows { get; set; }
}
