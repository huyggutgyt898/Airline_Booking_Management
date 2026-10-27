namespace TEO_Airline_Project.Models.ViewModels;

public sealed class AdminRouteListItemViewModel
{
    public int RouteId { get; init; }

    public string DepartureAirportCode { get; init; } = "";

    public string DepartureAirportCity { get; init; } = "";

    public string ArrivalAirportCode { get; init; } = "";

    public string ArrivalAirportCity { get; init; } = "";

    public string RouteType { get; init; } = "";

    public int FlightCount { get; init; }
}
