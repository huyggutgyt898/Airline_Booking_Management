using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TEO_Airline_Project.Models;
using TEO_Airline_Project.Models.CounterTickets;

namespace TEO_Airline_Project.Services;

public sealed class CounterTicketService : ICounterTicketService
{
    private static readonly TimeSpan ExchangeCutoff = TimeSpan.FromDays(5);
    private static readonly TimeSpan FullRefundCutoff = TimeSpan.FromDays(3);
    private static readonly TimeSpan CancellationCutoff = TimeSpan.FromHours(12);

    private readonly AirlineBookingDBContext _context;
    private readonly CounterTicketPolicyOptions _policy;
    private readonly ILogger<CounterTicketService> _logger;

    public CounterTicketService(
        AirlineBookingDBContext context,
        IOptions<CounterTicketPolicyOptions> policy,
        ILogger<CounterTicketService> logger)
    {
        _context = context;
        _policy = policy.Value;
        _logger = logger;
    }

    public Task<bool> IsStaffAsync(int staffId, CancellationToken cancellationToken) =>
        _context.Users.AsNoTracking().AnyAsync(
            user => user.UserId == staffId && user.Role == "Staff",
            cancellationToken);

    public async Task<CounterTicketResult<IReadOnlyList<CounterTicketResponse>>> SearchAsync(
        CounterTicketSearchRequest request,
        CancellationToken cancellationToken)
    {
        var bookingCode = request.BookingCode.Trim().ToUpperInvariant();
        var passengerName = request.PassengerName?.Trim();
        var contact = request.Contact?.Trim();
        if (string.IsNullOrWhiteSpace(passengerName) && string.IsNullOrWhiteSpace(contact))
        {
            return CounterTicketResult<IReadOnlyList<CounterTicketResponse>>.Failure(
                StatusCodes.Status400BadRequest,
                "Nhập tên hành khách hoặc email/số điện thoại để xác minh đặt chỗ.");
        }

        var query = _context.Tickets.AsNoTracking()
            .Where(ticket => ticket.Booking.BookingPnr == bookingCode);
        if (!string.IsNullOrWhiteSpace(passengerName))
        {
            query = query.Where(ticket => ticket.Passenger.FullName.Contains(passengerName));
        }

        if (!string.IsNullOrWhiteSpace(contact))
        {
            query = query.Where(ticket =>
                ticket.Booking.ContactPhone.Contains(contact) ||
                ticket.Booking.ContactEmail.Contains(contact));
        }

        if (request.FlightDate is DateOnly flightDate)
        {
            var start = flightDate.ToDateTime(TimeOnly.MinValue);
            var end = start.AddDays(1);
            query = query.Where(ticket =>
                ticket.Flight.DepartureTime >= start &&
                ticket.Flight.DepartureTime < end);
        }

        var tickets = await IncludeTicketDetails(query)
            .OrderBy(ticket => ticket.Flight.DepartureTime)
            .ThenBy(ticket => ticket.TicketNumber)
            .Take(50)
            .ToListAsync(cancellationToken);

        if (tickets.Count == 0)
        {
            return CounterTicketResult<IReadOnlyList<CounterTicketResponse>>.Success(
                Array.Empty<CounterTicketResponse>());
        }

        var bookingIds = tickets.Select(ticket => ticket.BookingId).Distinct().ToList();
        var payments = await _context.Payments.AsNoTracking()
            .Where(payment =>
                bookingIds.Contains(payment.BookingId) &&
                payment.Status == "Success" &&
                (payment.PaymentType == "Payment" ||
                 payment.PaymentType == "Surcharge" ||
                 payment.PaymentType == "Refund"))
            .Select(payment => new
            {
                payment.BookingId,
                payment.PaymentType,
                payment.Amount
            })
            .ToListAsync(cancellationToken);
        var netPaidByBooking = payments
            .GroupBy(payment => payment.BookingId)
            .ToDictionary(
                group => group.Key,
                group => Math.Max(
                    0m,
                    group.Sum(payment => payment.PaymentType == "Refund"
                        ? -payment.Amount
                        : payment.Amount)));

        var activeFares = await _context.Tickets.AsNoTracking()
            .Where(ticket =>
                bookingIds.Contains(ticket.BookingId) &&
                (ticket.Status == "Issued" ||
                 ticket.Status == "Changed" ||
                 ticket.Status == "CheckedIn"))
            .Select(ticket => new
            {
                ticket.BookingId,
                Fare = ticket.Price + ticket.TaxAmount + ticket.ServiceFee
            })
            .ToListAsync(cancellationToken);
        var activeFareByBooking = activeFares
            .GroupBy(ticket => ticket.BookingId)
            .ToDictionary(group => group.Key, group => group.Sum(ticket => ticket.Fare));

        var ticketIds = tickets.Select(ticket => ticket.TicketId).ToList();
        var history = await LoadHistoryAsync(ticketIds, cancellationToken);
        var now = DateTime.Now;
        var responses = tickets.Select(ticket =>
        {
            var netPaid = netPaidByBooking.GetValueOrDefault(ticket.BookingId);
            var bookingFare = activeFareByBooking.GetValueOrDefault(ticket.BookingId);
            var allocatedPaid = AllocatePaidAmount(ticket, netPaid, bookingFare);
            return ToResponse(
                ticket,
                allocatedPaid,
                history.GetValueOrDefault(ticket.TicketId) ?? [],
                now);
        }).ToList();

        return CounterTicketResult<IReadOnlyList<CounterTicketResponse>>.Success(responses);
    }

    public async Task<CounterTicketResult<IReadOnlyList<CounterTicketFlightOptionResponse>>> GetFlightOptionsAsync(
        int ticketId,
        CancellationToken cancellationToken)
    {
        var ticket = await LoadTicketAsync(ticketId, tracked: false, cancellationToken);
        if (ticket is null)
        {
            return CounterTicketResult<IReadOnlyList<CounterTicketFlightOptionResponse>>.Failure(
                StatusCodes.Status404NotFound,
                "Không tìm thấy vé.");
        }

        var now = DateTime.Now;
        if (!CanExchange(ticket, now))
        {
            return CounterTicketResult<IReadOnlyList<CounterTicketFlightOptionResponse>>.Failure(
                StatusCodes.Status409Conflict,
                "Vé không còn đủ điều kiện đổi. Việc đổi phải được thực hiện trước giờ khởi hành ít nhất 5 ngày.");
        }

        var options = await BuildFlightOptionsAsync(ticket, now, cancellationToken);
        return CounterTicketResult<IReadOnlyList<CounterTicketFlightOptionResponse>>.Success(options);
    }

    public async Task<CounterTicketResult<CounterTicketOperationResponse>> ExchangeAsync(
        int ticketId,
        int newFlightId,
        string reason,
        bool paymentConfirmed,
        int staffId,
        CancellationToken cancellationToken)
    {
        if (!paymentConfirmed)
        {
            return CounterTicketResult<CounterTicketOperationResponse>.Failure(
                StatusCodes.Status400BadRequest,
                "Xác nhận đã thu tiền phụ phí/chênh lệch tại quầy trước khi đổi vé.");
        }

        var normalizedReason = reason.Trim();
        if (normalizedReason.Length < 3)
        {
            return CounterTicketResult<CounterTicketOperationResponse>.Failure(
                StatusCodes.Status400BadRequest,
                "Vui lòng nhập lý do đổi vé.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        try
        {
            var ticket = await LoadTicketAsync(ticketId, tracked: true, cancellationToken);
            if (ticket is null)
            {
                return CounterTicketResult<CounterTicketOperationResponse>.Failure(
                    StatusCodes.Status404NotFound,
                    "Không tìm thấy vé.");
            }

            var now = DateTime.Now;
            if (!CanExchange(ticket, now))
            {
                return CounterTicketResult<CounterTicketOperationResponse>.Failure(
                    StatusCodes.Status409Conflict,
                    "Vé không còn đủ điều kiện đổi. Việc đổi phải được thực hiện trước giờ khởi hành ít nhất 5 ngày.");
            }

            if (!await IsStaffAsync(staffId, cancellationToken))
            {
                return CounterTicketResult<CounterTicketOperationResponse>.Failure(
                    StatusCodes.Status401Unauthorized,
                    "Phiên nhân viên không hợp lệ. Vui lòng đăng nhập lại.");
            }

            var options = await BuildFlightOptionsAsync(ticket, now, cancellationToken);
            var option = options.SingleOrDefault(candidate => candidate.FlightId == newFlightId);
            if (option is null)
            {
                return CounterTicketResult<CounterTicketOperationResponse>.Failure(
                    StatusCodes.Status409Conflict,
                    "Chuyến bay đã chọn không còn ghế phù hợp hoặc không còn mở bán. Vui lòng tải lại danh sách chuyến.");
            }

            var request = new BookingRequest
            {
                TicketId = ticket.TicketId,
                RequestType = "Change",
                Reason = normalizedReason,
                NewFlightId = option.FlightId,
                NewSeatId = option.SeatId,
                PriceDifference = option.PriceDifference,
                Surcharge = option.Surcharge,
                RefundAmount = 0m,
                Status = "Approved",
                ProcessedByStaffId = staffId,
                CreatedAt = now
            };
            _context.BookingRequests.Add(request);
            await _context.SaveChangesAsync(cancellationToken);

            var flightPricing = await _context.FlightPricings.AsNoTracking()
                .Where(pricing =>
                    pricing.FlightId == option.FlightId &&
                    pricing.SeatClass == ticket.SeatClass &&
                    pricing.PassengerType == ticket.Passenger.PassengerType)
                .Select(pricing => new
                {
                    pricing.BasePrice,
                    pricing.TaxAmount,
                    pricing.ServiceFee
                })
                .SingleAsync(cancellationToken);

            ticket.FlightId = option.FlightId;
            ticket.SeatId = option.SeatId;
            ticket.Price = option.NewGrossFare - flightPricing.TaxAmount - flightPricing.ServiceFee;
            ticket.TaxAmount = flightPricing.TaxAmount;
            ticket.ServiceFee = flightPricing.ServiceFee;
            ticket.Status = "Changed";
            ticket.Booking.SubTotal += option.PriceDifference;
            ticket.Booking.TotalAmount += option.AmountDue;

            if (option.AmountDue > 0m)
            {
                _context.Payments.Add(new Payment
                {
                    BookingId = ticket.BookingId,
                    RequestId = request.RequestId,
                    PaymentType = "Surcharge",
                    Method = ticket.Booking.PaymentMethod,
                    Amount = option.AmountDue,
                    TransactionCode = $"CHANGE-{ticket.Booking.BookingPnr}-{request.RequestId}",
                    Status = "Success",
                    ConfirmedByStaffId = staffId,
                    PaidAt = now
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return CounterTicketResult<CounterTicketOperationResponse>.Success(
                new CounterTicketOperationResponse(
                    ticket.Booking.BookingPnr,
                    ticket.TicketNumber,
                    request.RequestType,
                    request.Status,
                    "Đổi vé thành công.",
                    option.FlightNumber,
                    option.SeatNumber,
                    option.PriceDifference,
                    option.Surcharge,
                    0m,
                    option.AmountDue));
        }
        catch (DbUpdateException exception)
        {
            _logger.LogWarning(exception, "Database conflict while exchanging ticket {TicketId}", ticketId);
            return CounterTicketResult<CounterTicketOperationResponse>.Failure(
                StatusCodes.Status409Conflict,
                "Dữ liệu vé hoặc ghế vừa thay đổi. Vui lòng tải lại thông tin trước khi thử lại.");
        }
    }

    public async Task<CounterTicketResult<CounterTicketOperationResponse>> CancelAsync(
        int ticketId,
        string reason,
        bool refundConfirmed,
        int staffId,
        CancellationToken cancellationToken)
    {
        if (!refundConfirmed)
        {
            return CounterTicketResult<CounterTicketOperationResponse>.Failure(
                StatusCodes.Status400BadRequest,
                "Xác nhận đã hoàn tiền cho khách trước khi hủy vé.");
        }

        var normalizedReason = reason.Trim();
        if (normalizedReason.Length < 3)
        {
            return CounterTicketResult<CounterTicketOperationResponse>.Failure(
                StatusCodes.Status400BadRequest,
                "Vui lòng nhập lý do hủy vé.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        try
        {
            var ticket = await LoadTicketAsync(ticketId, tracked: true, cancellationToken);
            if (ticket is null)
            {
                return CounterTicketResult<CounterTicketOperationResponse>.Failure(
                    StatusCodes.Status404NotFound,
                    "Không tìm thấy vé.");
            }

            var now = DateTime.Now;
            if (!CanCancel(ticket, now))
            {
                return CounterTicketResult<CounterTicketOperationResponse>.Failure(
                    StatusCodes.Status409Conflict,
                    "Vé chỉ được hủy khi còn ít nhất 12 tiếng trước giờ khởi hành và chưa làm thủ tục.");
            }

            if (!await IsStaffAsync(staffId, cancellationToken))
            {
                return CounterTicketResult<CounterTicketOperationResponse>.Failure(
                    StatusCodes.Status401Unauthorized,
                    "Phiên nhân viên không hợp lệ. Vui lòng đăng nhập lại.");
            }

            var netPaid = await GetNetPaidAmountAsync(ticket.BookingId, cancellationToken);
            if (netPaid <= 0m)
            {
                return CounterTicketResult<CounterTicketOperationResponse>.Failure(
                    StatusCodes.Status409Conflict,
                    "Không tìm thấy khoản thanh toán thành công để tính tiền hoàn.");
            }

            var activeTicketFares = await _context.Tickets.AsNoTracking()
                .Where(candidate =>
                    candidate.BookingId == ticket.BookingId &&
                    (candidate.Status == "Issued" ||
                     candidate.Status == "Changed" ||
                     candidate.Status == "CheckedIn"))
                .Select(candidate => new
                {
                    candidate.TicketId,
                    Fare = candidate.Price + candidate.TaxAmount + candidate.ServiceFee
                })
                .ToListAsync(cancellationToken);
            var totalActiveFare = activeTicketFares.Sum(candidate => candidate.Fare);
            var allocatedPaid = AllocatePaidAmount(ticket, netPaid, totalActiveFare);
            var fullRefund = ticket.Flight.DepartureTime - now > FullRefundCutoff;
            var refundAmount = fullRefund
                ? allocatedPaid
                : Math.Min(
                    allocatedPaid,
                    RoundMoney(ticket.Price * _policy.PartialCancellationRefundPercent / 100m));

            if (refundAmount <= 0m)
            {
                return CounterTicketResult<CounterTicketOperationResponse>.Failure(
                    StatusCodes.Status409Conflict,
                    "Số tiền hoàn tính được bằng 0; không thể ghi nhận hủy vé.");
            }

            var request = new BookingRequest
            {
                TicketId = ticket.TicketId,
                RequestType = "Cancel",
                Reason = normalizedReason,
                PriceDifference = 0m,
                Surcharge = 0m,
                RefundAmount = refundAmount,
                Status = "Approved",
                ProcessedByStaffId = staffId,
                CreatedAt = now
            };
            _context.BookingRequests.Add(request);
            await _context.SaveChangesAsync(cancellationToken);

            ticket.Status = "Cancelled";
            var hasOtherUncancelledTickets = await _context.Tickets.AsNoTracking()
                .AnyAsync(candidate =>
                    candidate.BookingId == ticket.BookingId &&
                    candidate.TicketId != ticket.TicketId &&
                    candidate.Status != "Cancelled",
                    cancellationToken);
            if (!hasOtherUncancelledTickets)
            {
                ticket.Booking.Status = "Cancelled";
            }

            _context.Payments.Add(new Payment
            {
                BookingId = ticket.BookingId,
                RequestId = request.RequestId,
                PaymentType = "Refund",
                Method = ticket.Booking.PaymentMethod,
                Amount = refundAmount,
                TransactionCode = $"REFUND-{ticket.Booking.BookingPnr}-{request.RequestId}",
                Status = "Success",
                ConfirmedByStaffId = staffId,
                PaidAt = now
            });

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return CounterTicketResult<CounterTicketOperationResponse>.Success(
                new CounterTicketOperationResponse(
                    ticket.Booking.BookingPnr,
                    ticket.TicketNumber,
                    request.RequestType,
                    request.Status,
                    fullRefund
                        ? "Hủy vé thành công; đã ghi nhận hoàn toàn bộ số tiền được phân bổ cho vé."
                        : "Hủy vé thành công; đã ghi nhận khoản hoàn một phần theo chính sách.",
                    null,
                    null,
                    0m,
                    0m,
                    refundAmount,
                    0m));
        }
        catch (DbUpdateException exception)
        {
            _logger.LogWarning(exception, "Database conflict while cancelling ticket {TicketId}", ticketId);
            return CounterTicketResult<CounterTicketOperationResponse>.Failure(
                StatusCodes.Status409Conflict,
                "Dữ liệu vé vừa thay đổi. Vui lòng tải lại thông tin trước khi thử lại.");
        }
    }

    private async Task<IReadOnlyList<CounterTicketFlightOptionResponse>> BuildFlightOptionsAsync(
        Ticket ticket,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var eligibleFlights = await _context.Flights.AsNoTracking()
            .Where(flight =>
                flight.RouteId == ticket.Flight.RouteId &&
                flight.FlightId != ticket.FlightId &&
                flight.Status == "Scheduled" &&
                flight.DepartureTime >= now.Add(ExchangeCutoff))
            .Include(flight => flight.Route)
            .ToListAsync(cancellationToken);
        if (eligibleFlights.Count == 0)
        {
            return Array.Empty<CounterTicketFlightOptionResponse>();
        }

        var flightIds = eligibleFlights.Select(flight => flight.FlightId).ToList();
        var aircraftIds = eligibleFlights.Select(flight => flight.AircraftId).Distinct().ToList();
        var pricing = await _context.FlightPricings.AsNoTracking()
            .Where(item =>
                flightIds.Contains(item.FlightId) &&
                item.SeatClass == ticket.SeatClass &&
                item.PassengerType == ticket.Passenger.PassengerType)
            .ToListAsync(cancellationToken);
        var seats = await _context.Seats.AsNoTracking()
            .Where(seat =>
                aircraftIds.Contains(seat.AircraftId) &&
                seat.SeatClass == ticket.SeatClass)
            .Select(seat => new { seat.SeatId, seat.AircraftId, seat.SeatNumber })
            .ToListAsync(cancellationToken);
        var occupied = await _context.Tickets.AsNoTracking()
            .Where(candidate =>
                flightIds.Contains(candidate.FlightId) &&
                candidate.SeatId.HasValue &&
                candidate.Status != "Cancelled")
            .Select(candidate => new { candidate.FlightId, SeatId = candidate.SeatId!.Value })
            .ToListAsync(cancellationToken);
        var occupiedSeats = occupied
            .Select(item => (item.FlightId, item.SeatId))
            .ToHashSet();

        var options = new List<CounterTicketFlightOptionResponse>();
        foreach (var flight in eligibleFlights.OrderBy(item => item.DepartureTime))
        {
            var price = pricing.SingleOrDefault(item => item.FlightId == flight.FlightId);
            if (price is null)
            {
                continue;
            }

            var seat = seats
                .Where(item =>
                    item.AircraftId == flight.AircraftId &&
                    !occupiedSeats.Contains((flight.FlightId, item.SeatId)))
                .OrderByDescending(item => item.SeatNumber == ticket.Seat?.SeatNumber)
                .ThenBy(item => item.SeatId)
                .FirstOrDefault();
            if (seat is null)
            {
                continue;
            }

            var seatFee = SeatPricingRules.GetExtraFee(ticket.SeatClass, seat.SeatNumber);
            var newTicketFare = price.BasePrice + seatFee;
            var newGrossFare = newTicketFare + price.TaxAmount + price.ServiceFee;
            var oldGrossFare = ticket.Price + ticket.TaxAmount + ticket.ServiceFee;
            var priceDifference = RoundMoney(newGrossFare - oldGrossFare);
            var surcharge = RoundMoney(ticket.Price * _policy.ExchangeSurchargePercent / 100m);
            var amountDue = RoundMoney(priceDifference + surcharge);

            if (amountDue < 0m)
            {
                continue;
            }

            options.Add(new CounterTicketFlightOptionResponse(
                flight.FlightId,
                flight.FlightNumber,
                ToDateTimeOffset(flight.DepartureTime),
                ToDateTimeOffset(flight.ArrivalTime),
                flight.Route.DepartureAirportCode,
                flight.Route.ArrivalAirportCode,
                seat.SeatId,
                seat.SeatNumber,
                newGrossFare,
                priceDifference,
                surcharge,
                amountDue));
        }

        return options;
    }

    private CounterTicketResponse ToResponse(
        Ticket ticket,
        decimal allocatedPaid,
        IReadOnlyList<CounterTicketHistoryResponse> history,
        DateTime now)
    {
        var canExchange = CanExchange(ticket, now);
        var canCancel = CanCancel(ticket, now) && allocatedPaid > 0m;
        var fullRefund = ticket.Flight.DepartureTime - now > FullRefundCutoff;
        var partialRefund = Math.Min(
            allocatedPaid,
            RoundMoney(ticket.Price * _policy.PartialCancellationRefundPercent / 100m));
        var refundAmount = fullRefund ? allocatedPaid : partialRefund;
        var active = IsActiveTicket(ticket) && ticket.CheckInTime is null;

        return new CounterTicketResponse(
            ticket.TicketId,
            ticket.TicketNumber,
            ticket.Booking.BookingPnr,
            ticket.Booking.Status,
            ticket.Passenger.FullName,
            $"{ticket.Booking.ContactPhone} · {ticket.Booking.ContactEmail}",
            ticket.Flight.FlightNumber,
            ticket.Flight.Route.DepartureAirportCode,
            ticket.Flight.Route.DepartureAirportCodeNavigation.City,
            ticket.Flight.Route.ArrivalAirportCode,
            ticket.Flight.Route.ArrivalAirportCodeNavigation.City,
            ToDateTimeOffset(ticket.Flight.DepartureTime),
            ToDateTimeOffset(ticket.Flight.ArrivalTime),
            ticket.Seat?.SeatNumber,
            ticket.SeatClass,
            ticket.Status,
            ticket.Price,
            ticket.TaxAmount,
            ticket.ServiceFee,
            allocatedPaid,
            canExchange,
            RoundMoney(ticket.Price * _policy.ExchangeSurchargePercent / 100m),
            canExchange
                ? $"Đủ điều kiện đổi; phụ phí {_policy.ExchangeSurchargePercent:0.##}% giá vé gốc."
                : "Chỉ đổi vé trước giờ khởi hành ít nhất 5 ngày, khi vé chưa làm thủ tục.",
            canCancel,
            fullRefund,
            canCancel ? refundAmount : 0m,
            canCancel
                ? fullRefund
                    ? "Hủy trước hơn 3 ngày: hoàn toàn bộ số tiền đã phân bổ cho vé."
                    : $"Hủy từ 12 giờ đến không quá 3 ngày: hoàn {_policy.PartialCancellationRefundPercent:0.##}% giá vé gốc."
                : CanCancel(ticket, now)
                    ? "Không tìm thấy thanh toán thành công để tính tiền hoàn."
                    : "Chỉ hủy vé khi còn ít nhất 12 tiếng trước giờ khởi hành và vé chưa làm thủ tục.",
            history);
    }

    private async Task<Dictionary<int, IReadOnlyList<CounterTicketHistoryResponse>>> LoadHistoryAsync(
        IReadOnlyCollection<int> ticketIds,
        CancellationToken cancellationToken)
    {
        var requests = await _context.BookingRequests.AsNoTracking()
            .Where(request => ticketIds.Contains(request.TicketId))
            .OrderByDescending(request => request.CreatedAt)
            .Select(request => new
            {
                request.TicketId,
                request.RequestId,
                request.RequestType,
                request.Status,
                request.Reason,
                request.Surcharge,
                request.PriceDifference,
                request.RefundAmount,
                request.CreatedAt,
                StaffName = request.ProcessedByStaff == null
                    ? null
                    : request.ProcessedByStaff.FullName
            })
            .ToListAsync(cancellationToken);

        return requests.GroupBy(request => request.TicketId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<CounterTicketHistoryResponse>)group
                    .Select(request => new CounterTicketHistoryResponse(
                        request.RequestId,
                        request.RequestType,
                        request.Status,
                        request.Reason ?? "",
                        request.Surcharge ?? 0m,
                        request.PriceDifference ?? 0m,
                        request.RefundAmount ?? 0m,
                        request.CreatedAt.HasValue
                            ? ToDateTimeOffset(request.CreatedAt.Value)
                            : null,
                        request.StaffName))
                    .ToList());
    }

    private async Task<Ticket?> LoadTicketAsync(
        int ticketId,
        bool tracked,
        CancellationToken cancellationToken)
    {
        IQueryable<Ticket> query = _context.Tickets;
        if (!tracked)
        {
            query = query.AsNoTracking();
        }

        return await IncludeTicketDetails(query)
            .SingleOrDefaultAsync(ticket => ticket.TicketId == ticketId, cancellationToken);
    }

    private static IQueryable<Ticket> IncludeTicketDetails(IQueryable<Ticket> query) =>
        query
            .Include(ticket => ticket.Booking)
            .Include(ticket => ticket.Passenger)
            .Include(ticket => ticket.Seat)
            .Include(ticket => ticket.Flight)
                .ThenInclude(flight => flight.Route)
                    .ThenInclude(route => route.DepartureAirportCodeNavigation)
            .Include(ticket => ticket.Flight)
                .ThenInclude(flight => flight.Route)
                    .ThenInclude(route => route.ArrivalAirportCodeNavigation);

    private async Task<decimal> GetNetPaidAmountAsync(
        int bookingId,
        CancellationToken cancellationToken)
    {
        var payments = await _context.Payments.AsNoTracking()
            .Where(payment =>
                payment.BookingId == bookingId &&
                payment.Status == "Success" &&
                (payment.PaymentType == "Payment" ||
                 payment.PaymentType == "Surcharge" ||
                 payment.PaymentType == "Refund"))
            .Select(payment => new { payment.PaymentType, payment.Amount })
            .ToListAsync(cancellationToken);
        return Math.Max(
            0m,
            payments.Sum(payment => payment.PaymentType == "Refund"
                ? -payment.Amount
                : payment.Amount));
    }

    private static decimal AllocatePaidAmount(Ticket ticket, decimal netPaid, decimal totalActiveFare)
    {
        if (ticket.Status is not ("Issued" or "Changed" or "CheckedIn") ||
            netPaid <= 0m ||
            totalActiveFare <= 0m)
        {
            return 0m;
        }

        var ticketFare = ticket.Price + ticket.TaxAmount + ticket.ServiceFee;
        return Math.Min(netPaid, RoundMoney(netPaid * ticketFare / totalActiveFare));
    }

    private static bool CanExchange(Ticket ticket, DateTime now) =>
        IsActiveTicket(ticket) &&
        ticket.CheckInTime is null &&
        ticket.Flight.Status == "Scheduled" &&
        ticket.Flight.DepartureTime - now >= ExchangeCutoff;

    private static bool CanCancel(Ticket ticket, DateTime now) =>
        IsActiveTicket(ticket) &&
        ticket.CheckInTime is null &&
        ticket.Flight.Status != "Cancelled" &&
        ticket.Flight.DepartureTime - now >= CancellationCutoff;

    private static bool IsActiveTicket(Ticket ticket) =>
        ticket.Status is "Issued" or "Changed";

    private static decimal RoundMoney(decimal amount) =>
        decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

    private static DateTimeOffset ToDateTimeOffset(DateTime value)
    {
        var localValue = DateTime.SpecifyKind(value, DateTimeKind.Local);
        return new DateTimeOffset(localValue);
    }
}
