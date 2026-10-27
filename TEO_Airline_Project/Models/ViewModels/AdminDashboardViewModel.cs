namespace TEO_Airline_Project.Models.ViewModels;

public sealed class AdminDashboardViewModel
{
    public IReadOnlyList<Airport> Airports { get; init; } = [];

    public AirportInputModel AirportInput { get; init; } = new();

    public string AirportDialogMode { get; init; } = "create";

    public bool OpenAirportDialog { get; init; }
}
