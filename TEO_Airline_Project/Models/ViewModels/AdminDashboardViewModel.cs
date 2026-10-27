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

    public IReadOnlyList<AdminFlightListItemViewModel> Flights { get; init; } = [];

    public IReadOnlyList<AdminRouteOptionViewModel> FlightRouteOptions { get; init; } = [];

    public IReadOnlyList<AdminAircraftOptionViewModel> FlightAircraftOptions { get; init; } = [];

    public int FilteredFlightCount { get; init; }

    public int FlightPage { get; init; } = 1;

    public int FlightPageCount { get; init; } = 1;

    public string FlightSearch { get; init; } = "";

    public string FlightStatusFilter { get; init; } = "";

    public FlightInputModel FlightInput { get; init; } = new();

    public string FlightDialogMode { get; init; } = "create";

    public bool OpenFlightDialog { get; init; }
}

public sealed class AdminFlightListItemViewModel
{
    public int FlightId { get; init; }

    public int RouteId { get; init; }

    public int AircraftId { get; init; }

    public string FlightNumber { get; init; } = "";

    public string DepartureAirportCode { get; init; } = "";

    public string DepartureCity { get; init; } = "";

    public string ArrivalAirportCode { get; init; } = "";

    public string ArrivalCity { get; init; } = "";

    public DateTime DepartureTime { get; init; }

    public DateTime ArrivalTime { get; init; }

    public string AircraftModel { get; init; } = "";

    public string Status { get; init; } = "";
}

public sealed class AdminRouteOptionViewModel
{
    public int RouteId { get; init; }

    public string DepartureAirportCode { get; init; } = "";

    public string DepartureCity { get; init; } = "";

    public string ArrivalAirportCode { get; init; } = "";

    public string ArrivalCity { get; init; } = "";
}

public sealed class AdminAircraftOptionViewModel
{
    public int AircraftId { get; init; }

    public string Model { get; init; } = "";
}
