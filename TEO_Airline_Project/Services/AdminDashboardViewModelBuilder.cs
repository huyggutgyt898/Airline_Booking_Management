using Microsoft.EntityFrameworkCore;
using System.Globalization;
using TEO_Airline_Project.Models;
using TEO_Airline_Project.Models.ViewModels;

namespace TEO_Airline_Project.Services;

public static class AdminDashboardViewModelBuilder
{
    public const int RoutePageSize = 10;
    public const int FlightPageSize = 10;

    public static async Task<AdminDashboardViewModel> BuildAsync(
        AirlineBookingDBContext context,
        int routePage = 1,
        string? routeSearch = null,
        string? routeType = null,
        RouteInputModel? routeInput = null,
        string routeDialogMode = "create",
        bool openRouteDialog = false,
        int flightPage = 1,
        string? flightSearch = null,
        string? flightStatus = null,
        FlightInputModel? flightInput = null,
        string flightDialogMode = "create",
        bool openFlightDialog = false,
        int? selectedAircraftId = null,
        AircraftInputModel? aircraftInput = null,
        string aircraftDialogMode = "create",
        bool openAircraftDialog = false,
        AirportInputModel? airportInput = null,
        string airportDialogMode = "create",
        bool openAirportDialog = false)
    {
        var airports = await context.Airports
            .AsNoTracking()
            .OrderBy(airport => airport.AirportCode)
            .ToListAsync();

        var routesQuery = context.Routes.AsNoTracking();
        routeSearch = routeSearch?.Trim();
        routeType = routeType is "Domestic" or "International" ? routeType : "";
        if (!string.IsNullOrEmpty(routeSearch))
        {
            var routeIdText = routeSearch.StartsWith("R-", StringComparison.OrdinalIgnoreCase)
                ? routeSearch[2..]
                : routeSearch;
            var hasRouteId = int.TryParse(routeIdText, out var routeId);

            routesQuery = routesQuery.Where(route =>
                (hasRouteId && route.RouteId == routeId) ||
                route.DepartureAirportCode.Contains(routeSearch) ||
                route.ArrivalAirportCode.Contains(routeSearch) ||
                route.DepartureAirportCodeNavigation.City.Contains(routeSearch) ||
                route.ArrivalAirportCodeNavigation.City.Contains(routeSearch));
        }

        if (!string.IsNullOrEmpty(routeType))
        {
            routesQuery = routesQuery.Where(route => route.RouteType == routeType);
        }

        var filteredRouteCount = await routesQuery.CountAsync();
        var totalRouteCount = await context.Routes.CountAsync();
        var totalFlightCount = await context.Flights.CountAsync();
        var pageCount = Math.Max(
            1,
            (int)Math.Ceiling(filteredRouteCount / (double)RoutePageSize));
        routePage = Math.Clamp(routePage, 1, pageCount);

        var routes = await routesQuery
            .OrderBy(route => route.RouteId)
            .Skip((routePage - 1) * RoutePageSize)
            .Take(RoutePageSize)
            .Select(route => new AdminRouteListItemViewModel
            {
                RouteId = route.RouteId,
                DepartureAirportCode = route.DepartureAirportCode,
                DepartureAirportCity = route.DepartureAirportCodeNavigation.City,
                ArrivalAirportCode = route.ArrivalAirportCode,
                ArrivalAirportCity = route.ArrivalAirportCodeNavigation.City,
                RouteType = route.RouteType,
                FlightCount = context.Flights.Count(flight => flight.RouteId == route.RouteId)
            })
            .ToListAsync();

        var flightRoutes = await context.Routes
            .AsNoTracking()
            .OrderBy(route => route.DepartureAirportCode)
            .ThenBy(route => route.ArrivalAirportCode)
            .Select(route => new AdminRouteOptionViewModel
            {
                RouteId = route.RouteId,
                DepartureAirportCode = route.DepartureAirportCode,
                DepartureCity = route.DepartureAirportCodeNavigation.City,
                ArrivalAirportCode = route.ArrivalAirportCode,
                ArrivalCity = route.ArrivalAirportCodeNavigation.City
            })
            .ToListAsync();

        var flightAircraft = await context.Aircrafts
            .AsNoTracking()
            .OrderBy(aircraft => aircraft.Model)
            .Select(aircraft => new AdminAircraftOptionViewModel
            {
                AircraftId = aircraft.AircraftId,
                Model = aircraft.Model
            })
            .ToListAsync();

        var aircrafts = await context.Aircrafts
            .AsNoTracking()
            .OrderBy(aircraft => aircraft.Model)
            .ThenBy(aircraft => aircraft.AircraftId)
            .Select(aircraft => new AdminAircraftListItemViewModel
            {
                AircraftId = aircraft.AircraftId,
                Model = aircraft.Model,
                TotalSeats = context.Seats.Count(seat => seat.AircraftId == aircraft.AircraftId)
            })
            .ToListAsync();

        selectedAircraftId = aircrafts.Any(aircraft => aircraft.AircraftId == selectedAircraftId)
            ? selectedAircraftId
            : aircrafts.FirstOrDefault()?.AircraftId;

        var aircraftSeats = selectedAircraftId is int aircraftId
            ? await context.Seats
                .AsNoTracking()
                .Where(seat => seat.AircraftId == aircraftId)
                .OrderBy(seat => seat.SeatNumber)
                .Select(seat => new AdminAircraftSeatViewModel
                {
                    SeatNumber = seat.SeatNumber,
                    SeatClass = seat.SeatClass
                })
                .ToListAsync()
            : [];
        var aircraftSeatRows = BuildAircraftSeatRows(aircraftSeats);

        var flightsQuery = context.Flights.AsNoTracking();
        flightSearch = flightSearch?.Trim();
        flightStatus = AdminFlightStatus.Options.Any(option => option.Value == flightStatus)
            ? flightStatus
            : "";
        if (!string.IsNullOrEmpty(flightSearch))
        {
            flightsQuery = flightsQuery.Where(flight =>
                flight.FlightNumber.Contains(flightSearch) ||
                flight.Route.DepartureAirportCode.Contains(flightSearch) ||
                flight.Route.ArrivalAirportCode.Contains(flightSearch) ||
                flight.Route.DepartureAirportCodeNavigation.City.Contains(flightSearch) ||
                flight.Route.ArrivalAirportCodeNavigation.City.Contains(flightSearch) ||
                flight.Aircraft.Model.Contains(flightSearch));
        }

        if (!string.IsNullOrEmpty(flightStatus))
        {
            flightsQuery = flightsQuery.Where(flight => flight.Status == flightStatus);
        }

        var filteredFlightCount = await flightsQuery.CountAsync();
        var flightPageCount = Math.Max(
            1,
            (int)Math.Ceiling(filteredFlightCount / (double)FlightPageSize));
        flightPage = Math.Clamp(flightPage, 1, flightPageCount);

        var flights = await flightsQuery
            .OrderBy(flight => flight.DepartureTime)
            .ThenBy(flight => flight.FlightId)
            .Skip((flightPage - 1) * FlightPageSize)
            .Take(FlightPageSize)
            .Select(flight => new AdminFlightListItemViewModel
            {
                FlightId = flight.FlightId,
                RouteId = flight.RouteId,
                AircraftId = flight.AircraftId,
                FlightNumber = flight.FlightNumber,
                DepartureAirportCode = flight.Route.DepartureAirportCode,
                DepartureCity = flight.Route.DepartureAirportCodeNavigation.City,
                ArrivalAirportCode = flight.Route.ArrivalAirportCode,
                ArrivalCity = flight.Route.ArrivalAirportCodeNavigation.City,
                DepartureTime = flight.DepartureTime,
                ArrivalTime = flight.ArrivalTime,
                AircraftModel = flight.Aircraft.Model,
                Status = flight.Status
            })
            .ToListAsync();

        return new AdminDashboardViewModel
        {
            Airports = airports,
            Aircrafts = aircrafts,
            SelectedAircraftId = selectedAircraftId,
            AircraftSeatRows = aircraftSeatRows,
            AircraftInput = aircraftInput ?? new AircraftInputModel(),
            AircraftDialogMode = aircraftDialogMode,
            OpenAircraftDialog = openAircraftDialog,
            AirportInput = airportInput ?? new AirportInputModel(),
            AirportDialogMode = airportDialogMode,
            OpenAirportDialog = openAirportDialog,
            Routes = routes,
            TotalRouteCount = totalRouteCount,
            TotalFlightCount = totalFlightCount,
            FilteredRouteCount = filteredRouteCount,
            RoutePage = routePage,
            RoutePageCount = pageCount,
            RouteSearch = routeSearch ?? "",
            RouteTypeFilter = routeType ?? "",
            RouteInput = routeInput ?? new RouteInputModel(),
            RouteDialogMode = routeDialogMode,
            OpenRouteDialog = openRouteDialog,
            Flights = flights,
            FlightRouteOptions = flightRoutes,
            FlightAircraftOptions = flightAircraft,
            FilteredFlightCount = filteredFlightCount,
            FlightPage = flightPage,
            FlightPageCount = flightPageCount,
            FlightSearch = flightSearch ?? "",
            FlightStatusFilter = flightStatus ?? "",
            FlightInput = flightInput ?? new FlightInputModel(),
            FlightDialogMode = flightDialogMode,
            OpenFlightDialog = openFlightDialog
        };
    }

    private static IReadOnlyList<AdminAircraftSeatRowViewModel> BuildAircraftSeatRows(
        IReadOnlyList<AdminAircraftSeatViewModel> seats)
    {
        return seats
            .Select(seat =>
            {
                var prefixLength = seat.SeatNumber.TakeWhile(char.IsDigit).Count();
                var rowText = seat.SeatNumber[..prefixLength];
                var rowNumber = int.TryParse(
                    rowText,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var parsedRow)
                    ? parsedRow
                    : 0;

                return new
                {
                    Seat = new AdminAircraftSeatViewModel
                    {
                        SeatNumber = seat.SeatNumber,
                        SeatClass = seat.SeatClass,
                        Column = seat.SeatNumber[prefixLength..]
                    },
                    RowNumber = rowNumber,
                    RowLabel = rowText
                };
            })
            .GroupBy(item => item.RowNumber)
            .OrderBy(group => group.Key)
            .Select(group => new AdminAircraftSeatRowViewModel
            {
                RowNumber = group.Key == 0 ? "—" : group.First().RowLabel,
                Seats = group
                    .OrderBy(item => item.Seat.Column, StringComparer.OrdinalIgnoreCase)
                    .Select(item => item.Seat)
                    .ToList()
            })
            .ToList();
    }
}
