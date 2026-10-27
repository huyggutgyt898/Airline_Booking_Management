using Microsoft.EntityFrameworkCore;
using TEO_Airline_Project.Models;
using TEO_Airline_Project.Models.ViewModels;

namespace TEO_Airline_Project.Services;

public static class AdminDashboardViewModelBuilder
{
    public const int RoutePageSize = 10;

    public static async Task<AdminDashboardViewModel> BuildAsync(
        AirlineBookingDBContext context,
        int routePage = 1,
        string? routeSearch = null,
        string? routeType = null,
        RouteInputModel? routeInput = null,
        string routeDialogMode = "create",
        bool openRouteDialog = false)
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

        return new AdminDashboardViewModel
        {
            Airports = airports,
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
            OpenRouteDialog = openRouteDialog
        };
    }
}
