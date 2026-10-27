using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TEO_Airline_Project.Models;
using TEO_Airline_Project.Models.BookingModels;

namespace TEO_Airline_Project.Controllers
{
    [ApiController]
    [Route("api/lookup")]
    public class LookupController : Controller
    {
        private readonly AirlineBookingDBContext _context;
        private readonly ILogger<LookupController> _logger;

        public LookupController(AirlineBookingDBContext context, ILogger<LookupController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("mine")]
        public async Task<IActionResult> Mine()
        {
            var userId = GetSessionUserId();
            if (userId is null)
            {
                return Unauthorized(new LookupResponse
                {
                    Ok = false,
                    Message = "Vui lòng đăng nhập để xem các đơn đã đặt."
                });
            }

            await BookingMaintenance.ExpireOverdueBookingsAsync(_context, _logger);

            var bookings = await QueryWithDetails()
                .Where(b => b.UserId == userId.Value)
                .OrderByDescending(b => b.BookingDate)
                .Take(50)
                .ToListAsync();

            return Ok(new LookupResponse
            {
                Ok = true,
                Bookings = bookings.Select(Map).ToList()
            });
        }

        [HttpPost]
        public async Task<IActionResult> Find([FromBody] LookupRequest request)
        {
            await BookingMaintenance.ExpireOverdueBookingsAsync(_context, _logger);

            var code = (request.Code ?? "").Trim().ToUpperInvariant();
            if (code.Length is < 4 or > 12)
            {
                return Ok(new LookupResponse { Ok = false, Message = "Vui lòng nhập đúng mã đặt chỗ (ví dụ: X7K9P2)." });
            }

            var contact = (request.Contact ?? "").Trim();
            if (contact.Length == 0)
            {
                return Ok(new LookupResponse { Ok = false, Message = "Nhập email hoặc số điện thoại đã dùng khi đặt chỗ." });
            }

            var booking = await QueryWithDetails().FirstOrDefaultAsync(b => b.BookingPnr == code);
            if (booking is null || !ContactMatches(booking, contact))
            {
                return Ok(new LookupResponse
                {
                    Ok = false,
                    Message = "Không tìm thấy đơn phù hợp. Kiểm tra lại mã đặt chỗ và email/số điện thoại."
                });
            }

            return Ok(new LookupResponse { Ok = true, Bookings = new List<LookupBookingDto> { Map(booking) } });
        }

        private IQueryable<Booking> QueryWithDetails() =>
            _context.Bookings.AsNoTracking()
                .Include(b => b.Passengers)
                .Include(b => b.Tickets).ThenInclude(t => t.Flight).ThenInclude(f => f.Route)
                .Include(b => b.Tickets).ThenInclude(t => t.Seat);

        private static bool ContactMatches(Booking booking, string contact)
        {
            if (string.Equals(booking.ContactEmail?.Trim(), contact, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var phone = NormalizePhone(contact);
            return NormalizePhone(booking.ContactPhone ?? "") == phone && phone.Length > 0;
        }

        private static string NormalizePhone(string value)
        {
            var digits = new string(value.Where(character => !char.IsWhiteSpace(character) &&
                character is not '.' and not '-' and not '(' and not ')').ToArray());
            return digits.StartsWith("+84", StringComparison.Ordinal) ? "0" + digits[3..] : digits;
        }

        private static LookupBookingDto Map(Booking booking)
        {
            var tickets = booking.Tickets
                .OrderBy(t => t.Flight.DepartureTime)
                .ThenBy(t => t.TicketId)
                .ToList();

            var flights = tickets
                .GroupBy(t => t.FlightId)
                .Select(group => group.First())
                .OrderBy(t => t.Flight.DepartureTime)
                .Select(t => new LookupFlightDto
                {
                    DirectionLabel = t.Direction == "Return" ? "Chuyến về" : "Chuyến đi",
                    FlightNumber = t.Flight.FlightNumber,
                    Date = t.Flight.DepartureTime.ToString("dd/MM/yyyy"),
                    DepartureTime = t.Flight.DepartureTime.ToString("HH:mm"),
                    ArrivalTime = t.Flight.ArrivalTime.ToString("HH:mm"),
                    DepartureCode = t.Flight.Route.DepartureAirportCode,
                    ArrivalCode = t.Flight.Route.ArrivalAirportCode
                })
                .ToList();

            var passengers = booking.Passengers
                .OrderBy(p => p.PassengerId)
                .Select(p =>
                {
                    var passengerSeats = tickets
                        .Where(t => t.PassengerId == p.PassengerId && t.Seat is not null)
                        .Select(t => $"{t.Seat.SeatNumber} ({(t.Direction == "Return" ? "về" : "đi")})")
                        .ToList();

                    return new LookupPassengerDto
                    {
                        FullName = p.FullName,
                        TypeLabel = TypeLabel(p.PassengerType),
                        SeatLabel = passengerSeats.Count > 0 ? string.Join(", ", passengerSeats) : "Không ghế"
                    };
                })
                .ToList();

            var seats = tickets
                .Where(t => t.Seat is not null)
                .Select(t => $"{t.Seat.SeatNumber} · {t.Flight.FlightNumber} {(t.Direction == "Return" ? "về" : "đi")}")
                .ToList();

            var expiresAt = booking.Status == "PendingPayment" && booking.BookingDate.HasValue
                ? booking.BookingDate.Value.AddMinutes(booking.PaymentMethod == "MoMo"
                    ? BookingMaintenance.MomoHoldMinutes
                    : BookingMaintenance.CounterHoldMinutes)
                : (DateTime?)null;

            var (statusLabel, statusTone) = StatusStyle(booking.Status);
            var firstDeparture = tickets.FirstOrDefault()?.Flight.DepartureTime;

            return new LookupBookingDto
            {
                Pnr = booking.BookingPnr,
                Status = booking.Status,
                StatusLabel = statusLabel,
                StatusTone = statusTone,
                TripLabel = booking.TripType == "RoundTrip" ? "Khứ hồi" : "Một chiều",
                ContactName = booking.ContactFullName ?? "",
                ContactEmail = booking.ContactEmail ?? "",
                ContactPhone = booking.ContactPhone ?? "",
                BookingDate = booking.BookingDate,
                TotalAmount = booking.TotalAmount,
                DiscountAmount = booking.DiscountAmount,
                PaymentMethod = booking.PaymentMethod ?? "",
                PaymentLabel = booking.PaymentMethod == "MoMo" ? "MoMo" : "Tiền mặt / chuyển khoản",
                ExpiresAt = expiresAt,
                DepartureTime = firstDeparture,
                Flights = flights,
                Passengers = passengers,
                Seats = seats
            };
        }

        private static (string Label, string Tone) StatusStyle(string? status) => status switch
        {
            "Confirmed" => ("Đã xác nhận", "success"),
            "PendingPayment" => ("Chờ thanh toán", "pending"),
            "Expired" => ("Hết hạn giữ chỗ", "muted"),
            "Cancelled" => ("Đã huỷ", "danger"),
            _ => (status ?? "Không rõ", "muted")
        };

        private static string TypeLabel(string? type) => type switch
        {
            "Adult" => "Người lớn",
            "Child" => "Trẻ em",
            "Infant" => "Em bé",
            _ => type ?? ""
        };

        private int? GetSessionUserId()
        {
            var raw = HttpContext.Session.GetString("UserId");
            return int.TryParse(raw, out var id) ? id : null;
        }
    }
}
