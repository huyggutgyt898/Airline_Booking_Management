namespace TEO_Airline_Project.Models.ViewModels;

public sealed class AdminDashboardViewModel
{
    public IReadOnlyList<Airport> Airports { get; init; } = [];

    public IReadOnlyList<AdminRouteListItemViewModel> Routes { get; init; } = [];

    public int TotalRouteCount { get; init; }

    public int TotalFlightCount { get; init; }

    public int FilteredRouteCount { get; init; }

    public int RoutePage { get; init; } = 1;

    public int RoutePageCount { get; init; } = 1;

    public string RouteSearch { get; init; } = "";

    public string RouteTypeFilter { get; init; } = "";

    public RouteInputModel RouteInput { get; init; } = new();

    public string RouteDialogMode { get; init; } = "create";

    public bool OpenRouteDialog { get; init; }

    public AirportInputModel AirportInput { get; init; } = new();

    public string AirportDialogMode { get; init; } = "create";

    public bool OpenAirportDialog { get; init; }
}
