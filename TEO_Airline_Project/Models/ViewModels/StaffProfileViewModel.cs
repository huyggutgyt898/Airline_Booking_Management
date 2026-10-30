namespace TEO_Airline_Project.Models.ViewModels;

public sealed class StaffProfileViewModel
{
    public int UserId { get; init; }

    public string FullName { get; init; } = "";

    public string Email { get; init; } = "";

    public string Phone { get; init; } = "";

    public string RoleLabel { get; init; } = "";

    public string? ProfileError { get; init; }

    public string? ProfileMessage { get; init; }

    public string? PasswordError { get; init; }

    public string? PasswordMessage { get; init; }
}
