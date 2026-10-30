using Microsoft.EntityFrameworkCore;
using TEO_Airline_Project.Models;
using TEO_Airline_Project.Models.ViewModels;

namespace TEO_Airline_Project.Services;

public static class AdminOverviewSummaryBuilder
{
    private static readonly string[] SupportedPeriods =
        ["day", "week", "month", "quarter", "year", "all"];

    public static async Task<AdminOverviewSummaryViewModel> BuildAsync(
        AirlineBookingDBContext context,
        string? requestedPeriod)
    {
        var period = SupportedPeriods.Contains(requestedPeriod, StringComparer.OrdinalIgnoreCase)
            ? requestedPeriod!.ToLowerInvariant()
            : "month";
        var now = DateTime.Now;
        var (periodStart, periodEnd, caption) = GetPeriodRange(period, now);
        var queryEnd = periodEnd < now ? periodEnd : now;

        var paymentsQuery = context.Payments
            .AsNoTracking()
            .Where(payment =>
                (payment.PaymentType == "Payment" ||
                 payment.PaymentType == "Surcharge" ||
                 payment.PaymentType == "Refund") &&
                payment.Status == "Success" &&
                payment.PaidAt.HasValue &&
                payment.PaidAt.Value < queryEnd);
        if (periodStart is DateTime start)
        {
            paymentsQuery = paymentsQuery.Where(payment => payment.PaidAt!.Value >= start);
        }

        var dailyRevenue = await paymentsQuery
            .GroupBy(payment => payment.PaidAt!.Value.Date)
            .Select(group => new
            {
                Date = group.Key,
                Amount = group.Sum(payment =>
                    payment.PaymentType == "Refund" ? -payment.Amount : payment.Amount)
            })
            .ToListAsync();

        var bookingsQuery = context.Bookings
            .AsNoTracking()
            .Where(booking =>
                booking.BookingDate.HasValue &&
                booking.BookingDate.Value < queryEnd);
        if (periodStart is DateTime bookingStart)
        {
            bookingsQuery = bookingsQuery.Where(
                booking => booking.BookingDate!.Value >= bookingStart);
        }

        var bookingCount = await bookingsQuery.CountAsync();
        var firstBookingDate = period == "all"
            ? await bookingsQuery.Select(booking => booking.BookingDate).MinAsync()
            : null;

        var ticketsQuery = context.Tickets
            .AsNoTracking()
            .Where(ticket =>
                ticket.Booking.BookingDate.HasValue &&
                ticket.Booking.BookingDate.Value < queryEnd);
        if (periodStart is DateTime ticketStart)
        {
            ticketsQuery = ticketsQuery.Where(
                ticket => ticket.Booking.BookingDate!.Value >= ticketStart);
        }

        var dailyTickets = await ticketsQuery
            .GroupBy(ticket => ticket.Booking.BookingDate!.Value.Date)
            .Select(group => new
            {
                Date = group.Key,
                SoldCount = group.Count(ticket =>
                    ticket.Status == "Issued" ||
                    ticket.Status == "CheckedIn" ||
                    ticket.Status == "Changed"),
                CancelledCount = group.Count(ticket =>
                    ticket.Status == "Cancelled" &&
                    ticket.Booking.Status != "Expired")
            })
            .ToListAsync();

        var requestQuery = context.BookingRequests
            .AsNoTracking()
            .Where(request =>
                request.RequestType == "Change" &&
                request.Status == "Approved" &&
                request.CreatedAt.HasValue &&
                request.CreatedAt.Value < queryEnd);
        if (periodStart is DateTime requestStart)
        {
            requestQuery = requestQuery.Where(
                request => request.CreatedAt!.Value >= requestStart);
        }

        var changedRequestCount = await requestQuery.CountAsync();

        var flightsQuery = context.Flights
            .AsNoTracking()
            .Where(flight =>
                flight.Status != "Cancelled" &&
                flight.DepartureTime < queryEnd);
        if (periodStart is DateTime flightStart)
        {
            flightsQuery = flightsQuery.Where(
                flight => flight.DepartureTime >= flightStart);
        }

        var seatCapacity = await flightsQuery
            .SumAsync(flight => (int?)flight.Aircraft.TotalSeats) ?? 0;
        var occupiedSeatCount = await context.Tickets
            .AsNoTracking()
            .Where(ticket =>
                ticket.SeatId.HasValue &&
                ticket.Status != "Cancelled" &&
                ticket.Flight.Status != "Cancelled" &&
                ticket.Flight.DepartureTime < queryEnd)
            .Where(ticket =>
                periodStart == null || ticket.Flight.DepartureTime >= periodStart.Value)
            .CountAsync();

        var earliestDate = new[]
            {
                firstBookingDate,
                dailyRevenue.Count > 0 ? dailyRevenue.Min(item => (DateTime?)item.Date) : null,
                dailyTickets.Count > 0 ? dailyTickets.Min(item => (DateTime?)item.Date) : null
            }
            .Min() ?? now.Date;
        var buckets = CreateBuckets(period, periodStart ?? earliestDate.Date, now);

        return new AdminOverviewSummaryViewModel
        {
            Period = period,
            PeriodCaption = caption,
            Revenue = dailyRevenue.Sum(item => item.Amount),
            BookingCount = bookingCount,
            SeatCapacity = seatCapacity,
            OccupiedSeatCount = occupiedSeatCount,
            SeatOccupancyRate = seatCapacity == 0
                ? null
                : Math.Round(occupiedSeatCount * 100m / seatCapacity, 1),
            SoldTicketCount = dailyTickets.Sum(item => item.SoldCount),
            CancelledTicketCount = dailyTickets.Sum(item => item.CancelledCount),
            ChangedRequestCount = changedRequestCount,
            ChartPoints = buckets.Select(bucket => new AdminOverviewChartPointViewModel
            {
                Label = bucket.Label,
                Revenue = dailyRevenue
                    .Where(item => item.Date >= bucket.Start && item.Date < bucket.End)
                    .Sum(item => item.Amount),
                SoldTicketCount = dailyTickets
                    .Where(item => item.Date >= bucket.Start && item.Date < bucket.End)
                    .Sum(item => item.SoldCount)
            }).ToList()
        };
    }

    private static (DateTime? Start, DateTime End, string Caption) GetPeriodRange(
        string period,
        DateTime now)
    {
        var today = now.Date;
        return period switch
        {
            "day" => (today, today.AddDays(1), today.ToString("dd/MM/yyyy")),
            "week" => GetWeekRange(today),
            "quarter" => GetQuarterRange(today),
            "year" => GetYearRange(today),
            "all" => (null, now.Date.AddDays(1), "Toàn bộ lịch sử"),
            _ => GetMonthRange(today)
        };
    }

    private static (DateTime Start, DateTime End, string Caption) GetWeekRange(DateTime today)
    {
        var start = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var end = start.AddDays(7);
        return (start, end, $"{start:dd/MM/yyyy} – {end.AddDays(-1):dd/MM/yyyy}");
    }

    private static (DateTime Start, DateTime End, string Caption) GetMonthRange(DateTime today)
    {
        var start = new DateTime(today.Year, today.Month, 1);
        var end = start.AddMonths(1);
        return (start, end, $"{start:dd}–{end.AddDays(-1):dd} tháng {today:MM}, {today:yyyy}");
    }

    private static (DateTime Start, DateTime End, string Caption) GetQuarterRange(DateTime today)
    {
        var quarter = (today.Month - 1) / 3 + 1;
        var start = new DateTime(today.Year, (quarter - 1) * 3 + 1, 1);
        return (start, start.AddMonths(3), $"Quý {quarter}, {today:yyyy}");
    }

    private static (DateTime Start, DateTime End, string Caption) GetYearRange(DateTime today)
    {
        var start = new DateTime(today.Year, 1, 1);
        return (start, start.AddYears(1), $"Năm {today:yyyy}");
    }

    private static IReadOnlyList<ChartBucket> CreateBuckets(
        string period,
        DateTime start,
        DateTime now)
    {
        if (period == "day")
        {
            return Enumerable.Range(0, 6)
                .Select(index =>
                {
                    var bucketStart = start.AddHours(index * 4);
                    return new ChartBucket(
                        bucketStart,
                        bucketStart.AddHours(4),
                        $"{bucketStart:HH}–{bucketStart.AddHours(4):HH}h");
                })
                .ToList();
        }

        if (period == "week")
        {
            return Enumerable.Range(0, 6)
                .Select(index =>
                {
                    var bucketStart = start.AddDays(index == 5 ? 5 : index);
                    var bucketEnd = index == 5 ? start.AddDays(7) : bucketStart.AddDays(1);
                    return new ChartBucket(
                        bucketStart,
                        bucketEnd,
                        index == 5
                            ? "Cuối tuần"
                            : bucketStart.ToString(
                                "ddd",
                                System.Globalization.CultureInfo.GetCultureInfo("vi-VN")));
                })
                .ToList();
        }

        if (period == "month")
        {
            var monthEnd = start.AddMonths(1);
            return Enumerable.Range(0, 6)
                .Select(index =>
                {
                    var bucketStart = start.AddDays(index * 5);
                    var bucketEnd = bucketStart.AddDays(5) < monthEnd
                        ? bucketStart.AddDays(5)
                        : monthEnd;
                    return new ChartBucket(
                        bucketStart,
                        bucketEnd,
                        $"{bucketStart:dd}–{bucketEnd.AddDays(-1):dd}");
                })
                .ToList();
        }

        if (period == "quarter")
        {
            return Enumerable.Range(0, 3)
                .Select(index =>
                {
                    var bucketStart = start.AddMonths(index);
                    return new ChartBucket(
                        bucketStart,
                        bucketStart.AddMonths(1),
                        bucketStart.ToString("MMM", System.Globalization.CultureInfo.GetCultureInfo("vi-VN")));
                })
                .ToList();
        }

        if (period == "year")
        {
            return Enumerable.Range(0, 6)
                .Select(index =>
                {
                    var bucketStart = start.AddMonths(index * 2);
                    return new ChartBucket(
                        bucketStart,
                        bucketStart.AddMonths(2),
                        $"T{bucketStart.Month}–T{bucketStart.Month + 1}");
                })
                .ToList();
        }

        var historyStart = start.Date;
        var historyEnd = now.Date.AddDays(1);
        var dayCount = Math.Max(1, (historyEnd - historyStart).Days);
        var bucketCount = Math.Min(6, dayCount);
        return Enumerable.Range(0, bucketCount)
            .Select(index =>
            {
                var bucketStart = historyStart.AddDays((long)index * dayCount / bucketCount);
                var bucketEnd = historyStart.AddDays((long)(index + 1) * dayCount / bucketCount);
                return new ChartBucket(
                    bucketStart,
                    bucketEnd,
                    $"{bucketStart:MM/yy}–{bucketEnd.AddDays(-1):MM/yy}");
            })
            .ToList();
    }

    private sealed record ChartBucket(DateTime Start, DateTime End, string Label);
}
