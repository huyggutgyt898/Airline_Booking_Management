using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TEO_Airline_Project.Models;
using TEO_Airline_Project.Models.BookingModels;

namespace TEO_Airline_Project.Controllers
{
    [ApiController]
    [Route("api/booking")]
    public class BookingController : Controller
    {
        private const int MomoHoldMinutes = BookingMaintenance.MomoHoldMinutes;
        private const int CounterHoldMinutes = BookingMaintenance.CounterHoldMinutes;
        private const decimal ExtraSeatFee = 150000m;
        private const string DraftSessionKey = "Booking.Draft";
        private const string CounterDraftSessionKey = "Booking.CounterDraft";
        private const string PnrAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private static readonly string[] CabinColumns = { "A", "B", "C", "D", "E", "F" };

        private readonly AirlineBookingDBContext _context;
        private readonly ILogger<BookingController> _logger;

        public BookingController(AirlineBookingDBContext context, ILogger<BookingController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /* ---------- Danh mục & ngữ cảnh ---------- */

        [HttpGet("airports")]
        public async Task<IActionResult> Airports()
        {
            var airports = await _context.Airports
                .OrderBy(a => a.City)
                .Select(a => new { a.AirportCode, a.City, a.AirportName })
                .ToListAsync();

            return Ok(airports.Select(a => new
            {
                code = a.AirportCode,
                city = a.City,
                name = a.AirportName,
                label = $"{a.City} ({a.AirportCode})"
            }));
        }

        [HttpGet("context")]
        public async Task<IActionResult> Context()
        {
            var userId = GetSessionUserId();
            if (userId is null)
            {
                return Ok(new
                {
                    isAuthenticated = false,
                    fullName = "",
                    email = "",
                    phone = "",
                    loyaltyPoints = 0,
                    redeemedPromoCodes = Array.Empty<string>()
                });
            }

            var user = await _context.Users.AsNoTracking()
                .Where(u => u.UserId == userId.Value)
                .Select(u => new { u.FullName, u.Email, u.Phone, u.LoyaltyPoints })
                .FirstOrDefaultAsync();

            if (user is null)
            {
                return Ok(new
                {
                    isAuthenticated = false,
                    fullName = "",
                    email = "",
                    phone = "",
                    loyaltyPoints = 0,
                    redeemedPromoCodes = Array.Empty<string>()
                });
            }

            var redeemed = await _context.UserPromotions.AsNoTracking()
                .Where(up => up.UserId == userId.Value && up.IsUsed != true)
                .Select(up => up.Promo.Code)
                .ToListAsync();

            return Ok(new
            {
                isAuthenticated = true,
                fullName = user.FullName ?? "",
                email = user.Email ?? "",
                phone = user.Phone ?? "",
                loyaltyPoints = user.LoyaltyPoints ?? 0,
                redeemedPromoCodes = redeemed
            });
        }

        /* ---------- Tìm chuyến bay ---------- */

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] SearchRequest request)
        {
            await ExpireOverdueBookingsAsync();

            var origin = (request.Origin ?? "").Trim().ToUpperInvariant();
            var destination = (request.Destination ?? "").Trim().ToUpperInvariant();
            if (origin.Length != 3 || destination.Length != 3)
            {
                return Ok(new SearchResponse { Ok = false, Message = "Vui lòng chọn điểm đi và điểm đến từ danh sách sân bay." });
            }

            if (origin == destination)
            {
                return Ok(new SearchResponse { Ok = false, Message = "Điểm đi và điểm đến cần khác nhau." });
            }

            if (!TryParseDate(request.DepartureDate, out var departureDate))
            {
                return Ok(new SearchResponse { Ok = false, Message = "Ngày đi không hợp lệ." });
            }

            var seatClass = NormalizeSeatClass(request.SeatClass);
            var seatCount = Math.Max(1, request.Adults + request.Children);

            var outbound = await LoadFlightsAsync(origin, destination, departureDate, seatClass, seatCount);
            var response = new SearchResponse { Ok = true, Outbound = outbound };

            if (outbound.Count == 0)
            {
                response.Message = $"Không có chuyến bay {origin} → {destination} vào ngày {departureDate:dd/MM/yyyy}.";
            }

            if (NormalizeTripType(request.TripType) == "RoundTrip")
            {
                if (!TryParseDate(request.ReturnDate, out var returnDate))
                {
                    response.Ok = false;
                    response.Message = "Vui lòng chọn ngày về cho hành trình khứ hồi.";
                    return Ok(response);
                }

                if (returnDate < departureDate)
                {
                    response.Ok = false;
                    response.Message = "Ngày về không thể trước ngày đi.";
                    return Ok(response);
                }

                response.Inbound = await LoadFlightsAsync(destination, origin, returnDate, seatClass, seatCount);
                if (response.Inbound.Count == 0)
                {
                    response.Message = $"Không có chuyến bay chiều về {destination} → {origin} vào ngày {returnDate:dd/MM/yyyy}.";
                }
            }

            return Ok(response);
        }

        private async Task<List<SearchFlightDto>> LoadFlightsAsync(
            string origin, string destination, DateOnly date, string seatClass, int seatsNeeded)
        {
            var dayStart = date.ToDateTime(TimeOnly.MinValue);
            var dayEnd = dayStart.AddDays(1);
            var now = DateTime.Now;

            var flights = await _context.Flights.AsNoTracking()
                .Where(f => f.Status == "Scheduled"
                            && f.Route.DepartureAirportCode == origin
                            && f.Route.ArrivalAirportCode == destination
                            && f.DepartureTime >= dayStart
                            && f.DepartureTime < dayEnd
                            && f.DepartureTime > now)
                .OrderBy(f => f.DepartureTime)
                .Select(f => new
                {
                    f.FlightId,
                    f.FlightNumber,
                    f.DepartureTime,
                    f.ArrivalTime,
                    f.AircraftId,
                    AircraftModel = f.Aircraft.Model,
                    DepartureCode = f.Route.DepartureAirportCode,
                    ArrivalCode = f.Route.ArrivalAirportCode,
                    DepartureCity = f.Route.DepartureAirportCodeNavigation.City,
                    ArrivalCity = f.Route.ArrivalAirportCodeNavigation.City,
                    Price = f.FlightPricings
                        .Where(p => p.SeatClass == seatClass && p.PassengerType == "Adult")
                        .Select(p => p.BasePrice + p.TaxAmount + p.ServiceFee)
                        .FirstOrDefault(),
                    ChildPrice = f.FlightPricings
                        .Where(p => p.SeatClass == seatClass && p.PassengerType == "Child")
                        .Select(p => p.BasePrice + p.TaxAmount + p.ServiceFee)
                        .FirstOrDefault(),
                    InfantPrice = f.FlightPricings
                        .Where(p => p.SeatClass == seatClass && p.PassengerType == "Infant")
                        .Select(p => p.BasePrice + p.TaxAmount + p.ServiceFee)
                        .FirstOrDefault()
                })
                .ToListAsync();

            if (flights.Count == 0)
            {
                return new List<SearchFlightDto>();
            }

            var flightIds = flights.Select(f => f.FlightId).ToList();

            var seatTotals = await _context.Seats.AsNoTracking()
                .Where(s => s.SeatClass == seatClass)
                .GroupBy(s => s.AircraftId)
                .Select(g => new { AircraftId = g.Key, Count = g.Count() })
                .ToListAsync();

            var occupied = await _context.Tickets.AsNoTracking()
                .Where(t => flightIds.Contains(t.FlightId) && t.SeatId != null && t.Status != "Cancelled")
                .Join(_context.Seats, t => t.SeatId, s => (int?)s.SeatId, (t, s) => new { t.FlightId, s.SeatClass })
                .Where(x => x.SeatClass == seatClass)
                .GroupBy(x => x.FlightId)
                .Select(g => new { FlightId = g.Key, Count = g.Count() })
                .ToListAsync();

            return flights.Select(f =>
            {
                var total = seatTotals.FirstOrDefault(s => s.AircraftId == f.AircraftId)?.Count ?? 0;
                var taken = occupied.FirstOrDefault(o => o.FlightId == f.FlightId)?.Count ?? 0;
                return new SearchFlightDto
                {
                    FlightId = f.FlightId,
                    FlightNumber = f.FlightNumber,
                    Date = f.DepartureTime.ToString("yyyy-MM-dd"),
                    DepartureTime = f.DepartureTime.ToString("HH:mm"),
                    ArrivalTime = f.ArrivalTime.ToString("HH:mm"),
                    DurationMinutes = (int)Math.Round((f.ArrivalTime - f.DepartureTime).TotalMinutes),
                    DepartureCode = f.DepartureCode,
                    ArrivalCode = f.ArrivalCode,
                    DepartureCity = f.DepartureCity,
                    ArrivalCity = f.ArrivalCity,
                    AircraftModel = f.AircraftModel,
                    PricePerAdult = f.Price,
                    PricePerChild = f.ChildPrice,
                    PricePerInfant = f.InfantPrice,
                    SeatsLeft = Math.Max(0, total - taken)
                };
            }).Where(f => f.SeatsLeft >= seatsNeeded).ToList();
        }

        /* ---------- Sơ đồ ghế ---------- */

        [HttpGet("seatmap")]
        public async Task<IActionResult> SeatMap(int flightId, string? seatClass)
        {
            await ExpireOverdueBookingsAsync();

            var normalizedClass = NormalizeSeatClass(seatClass);
            var flight = await _context.Flights.AsNoTracking()
                .Where(f => f.FlightId == flightId)
                .Select(f => new
                {
                    f.FlightId,
                    f.FlightNumber,
                    f.AircraftId,
                    AircraftModel = f.Aircraft.Model,
                    f.DepartureTime
                })
                .FirstOrDefaultAsync();

            if (flight is null)
            {
                return NotFound(new { ok = false, message = "Không tìm thấy chuyến bay." });
            }

            var seats = await _context.Seats.AsNoTracking()
                .Where(s => s.AircraftId == flight.AircraftId && s.SeatClass == normalizedClass)
                .Select(s => new { s.SeatId, s.SeatNumber })
                .ToListAsync();

            var occupiedSeatIds = await _context.Tickets.AsNoTracking()
                .Where(t => t.FlightId == flightId && t.SeatId != null && t.Status != "Cancelled")
                .Select(t => t.SeatId!.Value)
                .ToListAsync();

            var occupied = occupiedSeatIds.ToHashSet();

            var rows = seats
                .Select(s =>
                {
                    var number = s.SeatNumber;
                    var splitAt = 0;
                    while (splitAt < number.Length && char.IsDigit(number[splitAt])) splitAt++;
                    var rowNumber = int.Parse(number[..splitAt], CultureInfo.InvariantCulture);
                    var column = number[splitAt..].ToUpperInvariant();
                    return new { s.SeatId, s.SeatNumber, Row = rowNumber, Column = column };
                })
                .GroupBy(s => s.Row)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var cells = CabinColumns.Select(column =>
                    {
                        var seat = g.FirstOrDefault(s => s.Column == column);
                        if (seat is null) return null;
                        return new
                        {
                            seatId = seat.SeatId,
                            number = seat.SeatNumber,
                            column,
                            occupied = occupied.Contains(seat.SeatId),
                            extra = IsExtraSeat(normalizedClass, g.Key),
                            extraFee = IsExtraSeat(normalizedClass, g.Key) ? ExtraSeatFee : 0m
                        };
                    }).ToArray();

                    return new { row = g.Key, cells };
                })
                .ToList();

            return Ok(new
            {
                ok = true,
                flightId = flight.FlightId,
                flightNumber = flight.FlightNumber,
                aircraftModel = flight.AircraftModel,
                seatClass = normalizedClass,
                columns = CabinColumns,
                rows
            });
        }

        /* ---------- Báo giá (quote) ---------- */

        [HttpPost("quote")]
        public async Task<IActionResult> Quote([FromBody] QuoteRequest request)
        {
            var computed = await BuildQuoteAsync(request);
            if (computed.Response is null)
            {
                return BadRequest(new QuoteResponse { Ok = false, Message = computed.Error });
            }

            HttpContext.Session.SetString(DraftSessionKey, JsonSerializer.Serialize(request));

            return Ok(computed.Response);
        }

        [HttpPost("counter/quote")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CounterQuote([FromBody] QuoteRequest request)
        {
            if (await GetCurrentStaffIdAsync() is null)
            {
                return Unauthorized(new QuoteResponse { Ok = false, Message = "Phiên nhân viên không hợp lệ. Vui lòng đăng nhập lại." });
            }

            if (NormalizePaymentMethod(request.PaymentMethod) is not ("Cash" or "Transfer"))
            {
                return BadRequest(new QuoteResponse { Ok = false, Message = "Vui lòng chọn tiền mặt hoặc chuyển khoản tại quầy." });
            }

            request.PromoCode = null;
            var computed = await BuildQuoteAsync(request);
            if (computed.Response is null)
            {
                return BadRequest(new QuoteResponse { Ok = false, Message = computed.Error });
            }

            HttpContext.Session.SetString(CounterDraftSessionKey, JsonSerializer.Serialize(request));
            return Ok(computed.Response);
        }

        /* ---------- Xác nhận đặt chỗ ---------- */

        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm()
        {
            return await ConfirmDraftAsync(isCounterBooking: false, staffId: null);
        }

        [HttpPost("counter/confirm")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmAtCounter()
        {
            var staffId = await GetCurrentStaffIdAsync();
            if (staffId is null)
            {
                return Unauthorized(new ConfirmResponse { Ok = false, Message = "Phiên nhân viên không hợp lệ. Vui lòng đăng nhập lại." });
            }

            return await ConfirmDraftAsync(isCounterBooking: true, staffId.Value);
        }

        private async Task<IActionResult> ConfirmDraftAsync(bool isCounterBooking, int? staffId)
        {
            var draftKey = isCounterBooking ? CounterDraftSessionKey : DraftSessionKey;
            var draft = HttpContext.Session.GetString(draftKey);
            if (string.IsNullOrWhiteSpace(draft))
            {
                return BadRequest(new ConfirmResponse { Ok = false, Message = "Phiên đặt vé đã hết. Vui lòng tìm lại chuyến bay." });
            }

            QuoteRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<QuoteRequest>(draft);
            }
            catch (JsonException)
            {
                request = null;
            }

            if (request is null)
            {
                return BadRequest(new ConfirmResponse { Ok = false, Message = "Phiên đặt vé không hợp lệ. Vui lòng tìm lại chuyến bay." });
            }

            if (isCounterBooking)
            {
                request.PromoCode = null;
                if (NormalizePaymentMethod(request.PaymentMethod) is not ("Cash" or "Transfer"))
                {
                    return BadRequest(new ConfirmResponse { Ok = false, Message = "Phương thức thanh toán tại quầy không hợp lệ." });
                }
            }

            var computed = await BuildQuoteAsync(request);
            if (computed.Response is null || computed.Plan is null)
            {
                return BadRequest(new ConfirmResponse { Ok = false, Message = computed.Error });
            }

            var quote = computed.Response;
            var plan = computed.Plan;

            var userId = isCounterBooking ? null : GetSessionUserId();
            var paidAt = isCounterBooking ? DateTime.Now : (DateTime?)null;
            var booking = new Booking
            {
                BookingPnr = await GeneratePnrAsync(),
                Channel = isCounterBooking ? "Counter" : "Online",
                UserId = userId,
                CreatedByStaffId = staffId,
                TripType = quote.TripType,
                ContactFullName = request.ContactFullName!.Trim(),
                ContactEmail = request.ContactEmail!.Trim(),
                ContactPhone = NormalizePhone(request.ContactPhone!),
                BookingDate = DateTime.Now,
                SubTotal = quote.SubTotal,
                DiscountAmount = quote.Discount,
                TotalAmount = quote.Total,
                PaymentMethod = NormalizePaymentMethod(request.PaymentMethod),
                Status = isCounterBooking ? "Confirmed" : "PendingPayment",
                PromoId = plan.Promotion?.PromoId
            };

            foreach (var passenger in plan.Passengers)
            {
                booking.Passengers.Add(new Passenger
                {
                    FullName = passenger.Input.FullName!.Trim(),
                    Gender = passenger.Gender,
                    Dob = passenger.Dob,
                    PassengerType = passenger.Type,
                    PassportNumber = passenger.Input.DocumentNumber!.Trim()
                });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                var createdPassengers = booking.Passengers.OrderBy(p => p.PassengerId).ToList();
                for (var index = 0; index < plan.Passengers.Count; index++)
                {
                    var planned = plan.Passengers[index];
                    if (planned.Type == "Infant" && planned.AccompanyingIndex is int adultIndex &&
                        adultIndex >= 0 && adultIndex < createdPassengers.Count)
                    {
                        createdPassengers[index].AccompanyingPassengerId = createdPassengers[adultIndex].PassengerId;
                    }
                }

                var ticketIndex = 1;
                for (var index = 0; index < plan.Passengers.Count; index++)
                {
                    var planned = plan.Passengers[index];
                    foreach (var ticket in planned.Tickets)
                    {
                        booking.Tickets.Add(new Ticket
                        {
                            BookingId = booking.BookingId,
                            PassengerId = createdPassengers[index].PassengerId,
                            FlightId = ticket.FlightId,
                            SeatId = ticket.SeatId,
                            TicketNumber = $"TK{booking.BookingPnr}{ticketIndex:00}",
                            Direction = ticket.Direction,
                            SeatClass = quote.SeatClass,
                            Price = ticket.Base + ticket.SeatFee,
                            TaxAmount = ticket.Tax,
                            ServiceFee = ticket.Fee,
                            Status = isCounterBooking ? "Issued" : "PendingPayment"
                        });
                        ticketIndex++;
                    }
                }

                _context.Payments.Add(new Payment
                {
                    BookingId = booking.BookingId,
                    PaymentType = "Payment",
                    Method = booking.PaymentMethod,
                    Amount = quote.Total,
                    TransactionCode = isCounterBooking
                        ? $"COUNTER-{booking.BookingPnr}"
                        : $"PENDING-{booking.BookingPnr}",
                    Status = isCounterBooking ? "Success" : "Pending",
                    ConfirmedByStaffId = staffId,
                    PaidAt = paidAt
                });

                if (plan.Promotion is not null)
                {
                    var trackedPromotion = await _context.Promotions
                        .FirstOrDefaultAsync(p => p.PromoId == plan.Promotion.PromoId);
                    if (trackedPromotion is not null)
                    {
                        trackedPromotion.UsedCount = (trackedPromotion.UsedCount ?? 0) + 1;
                    }
                }

                var pointsEarned = 0;
                if (userId is not null)
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId.Value);
                    if (user is not null)
                    {
                        if (plan.Promotion is not null && (plan.Promotion.PointsRequired ?? 0) > 0)
                        {
                            var points = plan.Promotion.PointsRequired!.Value;
                            var redeemed = await _context.UserPromotions
                                .Where(up => up.UserId == user.UserId && up.PromoId == plan.Promotion.PromoId && up.IsUsed != true)
                                .OrderBy(up => up.UserPromoId)
                                .FirstOrDefaultAsync();
                            if (redeemed is not null)
                            {
                                redeemed.IsUsed = true;
                            }

                            user.LoyaltyPoints = Math.Max(0, (user.LoyaltyPoints ?? 0) - points);
                            _context.LoyaltyTransactions.Add(new LoyaltyTransaction
                            {
                                UserId = user.UserId,
                                BookingId = booking.BookingId,
                                Points = -points,
                                TransactionType = "Redeem",
                                Description = $"Đổi voucher {plan.Promotion.Code} cho đặt chỗ {booking.BookingPnr}",
                                CreatedAt = DateTime.Now
                            });
                        }

                        pointsEarned = (int)Math.Floor(quote.Total / 10000m);
                        if (pointsEarned > 0)
                        {
                            user.LoyaltyPoints = (user.LoyaltyPoints ?? 0) + pointsEarned;
                            _context.LoyaltyTransactions.Add(new LoyaltyTransaction
                            {
                                UserId = user.UserId,
                                BookingId = booking.BookingId,
                                Points = pointsEarned,
                                TransactionType = "Earn",
                                Description = $"Tích điểm từ đặt chỗ {booking.BookingPnr}",
                                CreatedAt = DateTime.Now
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                HttpContext.Session.Remove(draftKey);

                return Ok(new ConfirmResponse
                {
                    Ok = true,
                    BookingPnr = booking.BookingPnr,
                    BookingId = booking.BookingId,
                    Status = booking.Status,
                    TotalAmount = booking.TotalAmount,
                    ExpiresAt = quote.ExpiresAt,
                    PointsEarned = pointsEarned,
                    Lines = quote.Lines,
                    Flights = quote.Flights,
                    PassengerSummaries = plan.Passengers
                        .Select(p => $"{p.Input.FullName} · {TypeLabel(p.Type)}")
                        .ToList(),
                    SeatSummaries = plan.Passengers
                        .SelectMany(p => p.Tickets)
                        .Where(t => !string.IsNullOrEmpty(t.SeatNumber))
                        .Select(t => $"{t.SeatNumber} · {t.FlightNumber} {(t.Direction == "Return" ? "về" : "đi")}")
                        .ToList()
                });
            }
            catch (DbUpdateException exception)
            {
                await transaction.RollbackAsync();
                _logger.LogError(exception, "Không thể tạo đặt chỗ");
                return Conflict(new ConfirmResponse
                {
                    Ok = false,
                    Message = "Ghế bạn chọn vừa được người khác đặt. Vui lòng chọn ghế khác và thử lại."
                });
            }
        }

        private async Task<int?> GetCurrentStaffIdAsync()
        {
            if (!string.Equals(
                    HttpContext.Session.GetString("Role")?.Trim(),
                    "Staff",
                    StringComparison.OrdinalIgnoreCase) ||
                !int.TryParse(HttpContext.Session.GetString("UserId"), out var userId))
            {
                return null;
            }

            return await _context.Users.AsNoTracking()
                .Where(user => user.UserId == userId && user.Role == "Staff")
                .Select(user => (int?)user.UserId)
                .FirstOrDefaultAsync();
        }

        /* ---------- Đổi điểm lấy voucher ---------- */

        public sealed class RedeemRequest
        {
            public string? PromoCode { get; set; }
        }

        [HttpPost("redeem")]
        public async Task<IActionResult> Redeem([FromBody] RedeemRequest request)
        {
            var userId = GetSessionUserId();
            if (userId is null)
            {
                return Unauthorized(new { ok = false, message = "Vui lòng đăng nhập để đổi voucher bằng điểm." });
            }

            var code = (request.PromoCode ?? "").Trim().ToUpperInvariant();
            var promotion = await _context.Promotions.FirstOrDefaultAsync(p => p.Code == code);
            if (promotion is null)
            {
                return NotFound(new { ok = false, message = "Mã khuyến mãi không tồn tại." });
            }

            var pointsRequired = promotion.PointsRequired ?? 0;
            if (pointsRequired <= 0)
            {
                return BadRequest(new { ok = false, message = "Mã này không cần đổi bằng điểm, bạn có thể nhập trực tiếp khi thanh toán." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId.Value);
            if (user is null)
            {
                return Unauthorized(new { ok = false, message = "Không tìm thấy tài khoản." });
            }

            var available = user.LoyaltyPoints ?? 0;
            if (available < pointsRequired)
            {
                return BadRequest(new { ok = false, message = $"Bạn cần {pointsRequired:N0} điểm để đổi voucher này (hiện có {available:N0} điểm)." });
            }

            var alreadyRedeemed = await _context.UserPromotions
                .AnyAsync(up => up.UserId == user.UserId && up.PromoId == promotion.PromoId && up.IsUsed != true);
            if (alreadyRedeemed)
            {
                return BadRequest(new { ok = false, message = $"Bạn đã đổi voucher {code} rồi." });
            }

            user.LoyaltyPoints = available - pointsRequired;
            _context.UserPromotions.Add(new UserPromotion
            {
                UserId = user.UserId,
                PromoId = promotion.PromoId,
                RedeemedAt = DateTime.Now,
                IsUsed = false
            });
            _context.LoyaltyTransactions.Add(new LoyaltyTransaction
            {
                UserId = user.UserId,
                Points = -pointsRequired,
                TransactionType = "Redeem",
                Description = $"Đổi voucher {code}",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                ok = true,
                message = $"Đổi voucher {code} thành công. Nhập mã này ở bước thanh toán để được giảm giá.",
                promoCode = code,
                loyaltyPoints = user.LoyaltyPoints ?? 0
            });
        }

        /* ---------- Danh sách khuyến mãi ---------- */

        [HttpGet("promotions")]
        public async Task<IActionResult> Promotions()
        {
            var today = DateTime.Now;
            var promos = await _context.Promotions.AsNoTracking()
                .Where(p => p.StartDate <= today && p.EndDate >= today)
                .OrderBy(p => p.PointsRequired ?? 0)
                .ThenBy(p => p.PromoId)
                .Select(p => new
                {
                    p.Code,
                    p.DiscountPercent,
                    p.DiscountAmount,
                    p.MinOrderValue,
                    p.PointsRequired,
                    p.StartDate,
                    p.EndDate,
                    p.UsageLimit,
                    p.UsedCount
                })
                .ToListAsync();

            var userId = GetSessionUserId();
            var loyaltyPoints = 0;
            var redeemedCodes = new List<string>();

            if (userId is not null)
            {
                loyaltyPoints = await _context.Users.AsNoTracking()
                    .Where(u => u.UserId == userId.Value)
                    .Select(u => u.LoyaltyPoints ?? 0)
                    .FirstOrDefaultAsync();

                redeemedCodes = await _context.UserPromotions.AsNoTracking()
                    .Where(up => up.UserId == userId.Value && up.IsUsed != true)
                    .Select(up => up.Promo.Code)
                    .ToListAsync();
            }

            return Ok(new
            {
                loyaltyPoints,
                redeemedPromoCodes = redeemedCodes,
                promotions = promos.Select(p => new
                {
                    code = p.Code,
                    discountLabel = p.DiscountPercent is decimal percent
                        ? $"Giảm {percent.ToString("0.##", VietnameseCulture)}%"
                        : $"Giảm {Money(p.DiscountAmount ?? 0)}",
                    conditionLabel = (p.MinOrderValue ?? 0) > 0
                        ? $"Áp dụng cho đơn từ {Money(p.MinOrderValue!.Value)}"
                        : "Không yêu cầu giá trị đơn tối thiểu",
                    validityLabel = $"{p.StartDate:dd/MM}–{p.EndDate:dd/MM/yyyy}",
                    pointsRequired = p.PointsRequired ?? 0,
                    usageLimit = p.UsageLimit,
                    usedCount = p.UsedCount ?? 0
                })
            });
        }

        /* ---------- Tính toán dùng chung ---------- */

        private sealed class PlannedPassenger
        {
            public PassengerInput Input { get; set; } = new();
            public string Type { get; set; } = "Adult";
            public string Gender { get; set; } = "Male";
            public DateOnly Dob { get; set; }
            public int? AccompanyingIndex { get; set; }
            public List<PlannedTicket> Tickets { get; } = new();
        }

        private sealed class PlannedTicket
        {
            public int FlightId { get; set; }
            public string Direction { get; set; } = "Outbound";
            public string FlightNumber { get; set; } = "";
            public int? SeatId { get; set; }
            public string SeatNumber { get; set; } = "";
            public decimal Base { get; set; }
            public decimal Tax { get; set; }
            public decimal Fee { get; set; }
            public decimal SeatFee { get; set; }
        }

        private sealed class QuotePlan
        {
            public List<PlannedPassenger> Passengers { get; } = new();
            public Promotion? Promotion { get; set; }
            public int SeatCount { get; set; }
        }

        private sealed class QuoteComputation
        {
            public QuoteResponse? Response { get; set; }
            public QuotePlan? Plan { get; set; }
            public string? Error { get; set; }
        }

        private async Task<QuoteComputation> BuildQuoteAsync(QuoteRequest request)
        {
            await ExpireOverdueBookingsAsync();

            var contactError = ValidateContact(request);
            if (contactError is not null)
            {
                return new QuoteComputation { Error = contactError };
            }

            var seatClass = NormalizeSeatClass(request.SeatClass);
            var tripType = NormalizeTripType(request.TripType);
            var paymentMethod = NormalizePaymentMethod(request.PaymentMethod);

            if (request.Passengers.Count == 0)
            {
                return new QuoteComputation { Error = "Chưa có thông tin hành khách." };
            }

            if (request.Passengers.Count > 9)
            {
                return new QuoteComputation { Error = "Mỗi đặt chỗ tối đa 9 hành khách." };
            }

            var outboundFlight = await LoadFlightViewAsync(request.OutboundFlightId);
            if (outboundFlight is null)
            {
                return new QuoteComputation { Error = "Chuyến bay chiều đi không tồn tại." };
            }

            if (outboundFlight.DepartureTime <= DateTime.Now || outboundFlight.Status != "Scheduled")
            {
                return new QuoteComputation { Error = "Chuyến bay chiều đi không còn nhận đặt chỗ." };
            }

            FlightView? returnFlight = null;
            if (tripType == "RoundTrip")
            {
                if (request.ReturnFlightId is null or 0)
                {
                    return new QuoteComputation { Error = "Vui lòng chọn chuyến bay chiều về." };
                }

                returnFlight = await LoadFlightViewAsync(request.ReturnFlightId.Value);
                if (returnFlight is null)
                {
                    return new QuoteComputation { Error = "Chuyến bay chiều về không tồn tại." };
                }

                if (returnFlight.DepartureTime <= outboundFlight.DepartureTime)
                {
                    return new QuoteComputation { Error = "Chuyến bay chiều về phải khởi hành sau chuyến chiều đi." };
                }
            }

            var flightIds = new List<int> { request.OutboundFlightId };
            if (request.ReturnFlightId is int rid) flightIds.Add(rid);

            var pricing = await _context.FlightPricings.AsNoTracking()
                .Where(p => flightIds.Contains(p.FlightId) && p.SeatClass == seatClass)
                .ToListAsync();

            var departureDate = DateOnly.FromDateTime(outboundFlight.DepartureTime);
            var plan = new QuotePlan();
            var adultCount = 0;

            for (var index = 0; index < request.Passengers.Count; index++)
            {
                var input = request.Passengers[index];
                var type = NormalizePassengerType(input.Type);
                if (type is null)
                {
                    return new QuoteComputation { Error = $"Loại hành khách #{index + 1} không hợp lệ." };
                }

                var fullName = (input.FullName ?? "").Trim();
                if (fullName.Length < 2)
                {
                    return new QuoteComputation { Error = $"Vui lòng nhập họ tên hành khách #{index + 1}." };
                }

                if (!TryParseDate(input.Dob, out var dob))
                {
                    return new QuoteComputation { Error = $"Ngày sinh hành khách #{index + 1} không hợp lệ." };
                }

                if (dob > departureDate)
                {
                    return new QuoteComputation { Error = $"Ngày sinh hành khách #{index + 1} phải trước ngày khởi hành." };
                }

                var age = AgeAt(dob, departureDate);
                var error = type switch
                {
                    "Adult" when age < 12 => $"Hành khách #{index + 1} là người lớn nên phải từ 12 tuổi trở lên (hiện {age} tuổi).",
                    "Child" when age < 2 || age >= 12 => $"Hành khách #{index + 1} là trẻ em nên phải từ 2 đến dưới 12 tuổi (hiện {age} tuổi).",
                    "Infant" when age >= 2 => $"Hành khách #{index + 1} là em bé nên phải dưới 2 tuổi (hiện {age} tuổi).",
                    _ => null
                };
                if (error is not null)
                {
                    return new QuoteComputation { Error = error };
                }

                var documentNumber = (input.DocumentNumber ?? "").Trim();
                if (documentNumber.Length < 5)
                {
                    return new QuoteComputation { Error = $"Số giấy tờ của hành khách #{index + 1} không hợp lệ." };
                }

                if (type == "Adult") adultCount++;

                plan.Passengers.Add(new PlannedPassenger
                {
                    Input = input,
                    Type = type,
                    Gender = NormalizeGender(input.Gender),
                    Dob = dob,
                    AccompanyingIndex = input.AccompanyingIndex
                });
            }

            if (adultCount == 0)
            {
                return new QuoteComputation { Error = "Đặt chỗ cần ít nhất một hành khách người lớn." };
            }

            var infantCount = plan.Passengers.Count(p => p.Type == "Infant");
            if (infantCount > adultCount)
            {
                return new QuoteComputation { Error = "Mỗi em bé phải đi cùng một người lớn." };
            }

            foreach (var infant in plan.Passengers.Where(p => p.Type == "Infant"))
            {
                if (infant.AccompanyingIndex is not int adultIndex || adultIndex < 0 ||
                    adultIndex >= plan.Passengers.Count || plan.Passengers[adultIndex].Type != "Adult")
                {
                    return new QuoteComputation { Error = $"Vui lòng chọn người lớn đi cùng cho em bé {infant.Input.FullName}." };
                }
            }

            var seatRequired = plan.Passengers.Where(p => p.Type != "Infant").ToList();
            plan.SeatCount = seatRequired.Count;

            /* ----- Ghế chiều đi ----- */
            var outboundSeats = await LoadSelectedSeatsAsync(
                request.OutboundFlightId, seatClass, request.Seats.Outbound, plan.SeatCount, "chiều đi");
            if (outboundSeats.Error is not null)
            {
                return new QuoteComputation { Error = outboundSeats.Error };
            }

            /* ----- Ghế chiều về ----- */
            List<SeatInfo> returnSeats = new();
            if (tripType == "RoundTrip")
            {
                var loaded = await LoadSelectedSeatsAsync(
                    request.ReturnFlightId!.Value, seatClass, request.Seats.Return, plan.SeatCount, "chiều về");
                if (loaded.Error is not null)
                {
                    return new QuoteComputation { Error = loaded.Error };
                }

                returnSeats = loaded.Seats;
            }

            /* ----- Giá từng hành khách ----- */
            for (var index = 0; index < plan.Passengers.Count; index++)
            {
                var passenger = plan.Passengers[index];
                var price = pricing.FirstOrDefault(p => p.FlightId == request.OutboundFlightId && p.PassengerType == passenger.Type);
                if (price is null)
                {
                    return new QuoteComputation { Error = $"Chuyến bay chưa có giá cho hạng {SeatClassLabel(seatClass)} · {TypeLabel(passenger.Type)}." };
                }

                passenger.Tickets.Add(new PlannedTicket
                {
                    FlightId = request.OutboundFlightId,
                    Direction = "Outbound",
                    FlightNumber = outboundFlight.FlightNumber,
                    Base = price.BasePrice,
                    Tax = price.TaxAmount,
                    Fee = price.ServiceFee
                });

                if (tripType == "RoundTrip" && returnFlight is not null)
                {
                    var returnPrice = pricing.FirstOrDefault(p => p.FlightId == returnFlight.FlightId && p.PassengerType == passenger.Type);
                    if (returnPrice is null)
                    {
                        return new QuoteComputation { Error = $"Chuyến bay chiều về chưa có giá cho hạng {SeatClassLabel(seatClass)} · {TypeLabel(passenger.Type)}." };
                    }

                    passenger.Tickets.Add(new PlannedTicket
                    {
                        FlightId = returnFlight.FlightId,
                        Direction = "Return",
                        FlightNumber = returnFlight.FlightNumber,
                        Base = returnPrice.BasePrice,
                        Tax = returnPrice.TaxAmount,
                        Fee = returnPrice.ServiceFee
                    });
                }
            }

            /* ----- Gán ghế ----- */
            var seatIndex = 0;
            foreach (var passenger in plan.Passengers.Where(p => p.Type != "Infant"))
            {
                var outboundSeat = outboundSeats.Seats[seatIndex];
                var ticket = passenger.Tickets.First(t => t.Direction == "Outbound");
                ticket.SeatId = outboundSeat.SeatId;
                ticket.SeatNumber = outboundSeat.Number;
                ticket.SeatFee = outboundSeat.ExtraFee;

                if (tripType == "RoundTrip")
                {
                    var returnSeat = returnSeats[seatIndex];
                    var returnTicket = passenger.Tickets.First(t => t.Direction == "Return");
                    returnTicket.SeatId = returnSeat.SeatId;
                    returnTicket.SeatNumber = returnSeat.Number;
                    returnTicket.SeatFee = returnSeat.ExtraFee;
                }

                seatIndex++;
            }

            /* ----- Tổng hợp dòng giá ----- */
            var lines = new List<QuoteLineDto>();
            decimal subTotal = 0;

            foreach (var direction in new[] { "Outbound", "Return" })
            {
                var tickets = plan.Passengers.SelectMany(p => p.Tickets).Where(t => t.Direction == direction).ToList();
                if (tickets.Count == 0) continue;

                var directionLabel = direction == "Outbound" ? "chiều đi" : "chiều về";
                var flightLabel = tickets[0].FlightNumber;
                var routeLabel = direction == "Outbound"
                    ? $"{outboundFlight.DepartureCode} → {outboundFlight.ArrivalCode}"
                    : $"{returnFlight!.DepartureCode} → {returnFlight.ArrivalCode}";

                foreach (var passenger in plan.Passengers)
                {
                    var ticket = passenger.Tickets.FirstOrDefault(t => t.Direction == direction);
                    if (ticket is null) continue;

                    var label = $"Vé {TypeLabel(passenger.Type).ToLower()} · {flightLabel} ({routeLabel})";
                    AddLine(lines, label, ticket.Base);
                    subTotal += ticket.Base;
                }

                var tax = tickets.Sum(t => t.Tax);
                if (tax > 0)
                {
                    AddLine(lines, $"Thuế, phí sân bay · {flightLabel} ({directionLabel})", tax);
                    subTotal += tax;
                }

                var fee = tickets.Sum(t => t.Fee);
                if (fee > 0)
                {
                    AddLine(lines, $"Phí dịch vụ · {flightLabel} ({directionLabel})", fee);
                    subTotal += fee;
                }

                var seatFee = tickets.Sum(t => t.SeatFee);
                if (seatFee > 0)
                {
                    AddLine(lines, $"Phụ phí chỗ ngồi chọn trước · {flightLabel} ({directionLabel})", seatFee);
                    subTotal += seatFee;
                }
            }

            var promoResult = await EvaluatePromotionAsync(request.PromoCode, subTotal);
            var discount = promoResult.Applied ? promoResult.Discount : 0;
            var total = Math.Max(0, subTotal - discount);
            var holdMinutes = paymentMethod == "MoMo" ? MomoHoldMinutes : CounterHoldMinutes;

            plan.Promotion = promoResult.Applied ? promoResult.Promotion : null;

            var response = new QuoteResponse
            {
                Ok = true,
                TripType = tripType,
                SeatClass = seatClass,
                Lines = lines,
                SubTotal = subTotal,
                Discount = discount,
                Total = total,
                PromoApplied = promoResult.Applied,
                PromoCode = promoResult.Applied ? promoResult.Promotion!.Code : null,
                PromoMessage = promoResult.Message,
                HoldMinutes = holdMinutes,
                ExpiresAt = DateTime.Now.AddMinutes(holdMinutes),
                SeatCount = plan.SeatCount,
                Flights = BuildFlightSummaries(outboundFlight, returnFlight)
            };

            return new QuoteComputation { Response = response, Plan = plan };
        }

        private sealed class SeatInfo
        {
            public int SeatId { get; set; }
            public string Number { get; set; } = "";
            public decimal ExtraFee { get; set; }
        }

        private async Task<(List<SeatInfo> Seats, string? Error)> LoadSelectedSeatsAsync(
            int flightId, string seatClass, List<int> seatIds, int requiredCount, string directionLabel)
        {
            var distinct = seatIds.Distinct().ToList();
            if (distinct.Count != requiredCount || distinct.Count != seatIds.Count)
            {
                return (new List<SeatInfo>(), $"Vui lòng chọn đủ {requiredCount} ghế cho {directionLabel}.");
            }

            var flightAircraft = await _context.Flights.AsNoTracking()
                .Where(f => f.FlightId == flightId)
                .Select(f => f.AircraftId)
                .FirstOrDefaultAsync();

            var seats = await _context.Seats.AsNoTracking()
                .Where(s => distinct.Contains(s.SeatId) && s.AircraftId == flightAircraft && s.SeatClass == seatClass)
                .Select(s => new { s.SeatId, s.SeatNumber })
                .ToListAsync();

            if (seats.Count != distinct.Count)
            {
                return (new List<SeatInfo>(), $"Ghế đã chọn cho {directionLabel} không thuộc hạng {SeatClassLabel(seatClass)}.");
            }

            var occupied = await _context.Tickets.AsNoTracking()
                .Where(t => t.FlightId == flightId && t.SeatId != null && distinct.Contains(t.SeatId.Value) && t.Status != "Cancelled")
                .Select(t => t.SeatId!.Value)
                .ToListAsync();

            if (occupied.Count > 0)
            {
                return (new List<SeatInfo>(), $"Một số ghế đã chọn cho {directionLabel} vừa được đặt. Vui lòng chọn ghế khác.");
            }

            return (seats.Select(s =>
            {
                var row = int.Parse(new string(s.SeatNumber.TakeWhile(char.IsDigit).ToArray()), CultureInfo.InvariantCulture);
                return new SeatInfo
                {
                    SeatId = s.SeatId,
                    Number = s.SeatNumber,
                    ExtraFee = IsExtraSeat(seatClass, row) ? ExtraSeatFee : 0m
                };
            }).ToList(), null);
        }

        private sealed class PromotionResult
        {
            public Promotion? Promotion { get; set; }
            public bool Applied { get; set; }
            public decimal Discount { get; set; }
            public string? Message { get; set; }
        }

        private async Task<PromotionResult> EvaluatePromotionAsync(string? code, decimal subTotal)
        {
            var normalized = (code ?? "").Trim().ToUpperInvariant();
            if (normalized.Length == 0)
            {
                return new PromotionResult();
            }

            var promotion = await _context.Promotions.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Code == normalized);

            if (promotion is null)
            {
                return new PromotionResult { Message = "Mã khuyến mãi không tồn tại." };
            }

            var today = DateTime.Now;
            if (today < promotion.StartDate || today > promotion.EndDate)
            {
                return new PromotionResult { Message = $"Mã {normalized} không nằm trong thời gian áp dụng." };
            }

            if ((promotion.UsedCount ?? 0) >= promotion.UsageLimit)
            {
                return new PromotionResult { Message = $"Mã {normalized} đã hết lượt sử dụng." };
            }

            if (subTotal < (promotion.MinOrderValue ?? 0))
            {
                return new PromotionResult
                {
                    Message = $"Đơn hàng cần đạt tối thiểu {Money(promotion.MinOrderValue ?? 0)} để dùng mã {normalized}."
                };
            }

            var pointsRequired = promotion.PointsRequired ?? 0;
            if (pointsRequired > 0)
            {
                var userId = GetSessionUserId();
                if (userId is null)
                {
                    return new PromotionResult { Message = $"Mã {normalized} cần {pointsRequired:N0} điểm. Vui lòng đăng nhập để sử dụng." };
                }

                var user = await _context.Users.AsNoTracking()
                    .Where(u => u.UserId == userId.Value)
                    .Select(u => new { u.LoyaltyPoints })
                    .FirstOrDefaultAsync();

                if (user is null || (user.LoyaltyPoints ?? 0) < pointsRequired)
                {
                    return new PromotionResult
                    {
                        Message = $"Bạn cần {pointsRequired:N0} điểm để dùng mã {normalized} (hiện có {user?.LoyaltyPoints ?? 0:N0} điểm)."
                    };
                }

                var redeemed = await _context.UserPromotions.AsNoTracking()
                    .AnyAsync(up => up.UserId == userId.Value && up.PromoId == promotion.PromoId && up.IsUsed != true);
                if (!redeemed)
                {
                    return new PromotionResult { Message = $"Bạn cần đổi voucher {normalized} bằng điểm trước khi áp dụng." };
                }
            }

            var discount = promotion.DiscountPercent is decimal percent
                ? Math.Round(subTotal * percent / 100m, 0)
                : Math.Min(subTotal, promotion.DiscountAmount ?? 0);

            return new PromotionResult
            {
                Promotion = promotion,
                Applied = true,
                Discount = discount,
                Message = $"Đã áp dụng mã {normalized}: giảm {Money(discount)}."
            };
        }

        private static void AddLine(List<QuoteLineDto> lines, string label, decimal amount)
        {
            var existing = lines.FirstOrDefault(l => l.Label == label);
            if (existing is not null)
            {
                existing.Amount += amount;
                return;
            }

            lines.Add(new QuoteLineDto { Label = label, Amount = amount });
        }

        private sealed class FlightView
        {
            public int FlightId { get; set; }
            public string FlightNumber { get; set; } = "";
            public DateTime DepartureTime { get; set; }
            public DateTime ArrivalTime { get; set; }
            public int AircraftId { get; set; }
            public string Status { get; set; } = "";
            public string DepartureCode { get; set; } = "";
            public string ArrivalCode { get; set; } = "";
        }

        private Task<FlightView?> LoadFlightViewAsync(int flightId) =>
            _context.Flights.AsNoTracking()
                .Where(f => f.FlightId == flightId)
                .Select(f => new FlightView
                {
                    FlightId = f.FlightId,
                    FlightNumber = f.FlightNumber,
                    DepartureTime = f.DepartureTime,
                    ArrivalTime = f.ArrivalTime,
                    AircraftId = f.AircraftId,
                    Status = f.Status,
                    DepartureCode = f.Route.DepartureAirportCode,
                    ArrivalCode = f.Route.ArrivalAirportCode
                })
                .FirstOrDefaultAsync();

        private static List<FlightSummaryDto> BuildFlightSummaries(FlightView outbound, FlightView? returnFlight)
        {
            var list = new List<FlightSummaryDto> { BuildFlightSummary(outbound, "Outbound") };
            if (returnFlight is not null)
            {
                list.Add(BuildFlightSummary(returnFlight, "Return"));
            }

            return list;
        }

        private static FlightSummaryDto BuildFlightSummary(FlightView flight, string direction) => new()
        {
            FlightId = flight.FlightId,
            FlightNumber = flight.FlightNumber,
            Date = flight.DepartureTime.ToString("yyyy-MM-dd"),
            DepartureTime = flight.DepartureTime.ToString("HH:mm"),
            ArrivalTime = flight.ArrivalTime.ToString("HH:mm"),
            DepartureCode = flight.DepartureCode,
            ArrivalCode = flight.ArrivalCode,
            Direction = direction
        };

        /* ---------- Tiện ích ---------- */

        private int? GetSessionUserId()
        {
            var raw = HttpContext.Session.GetString("UserId");
            return int.TryParse(raw, out var id) ? id : null;
        }

        private async Task ExpireOverdueBookingsAsync()
        {
            await BookingMaintenance.ExpireOverdueBookingsAsync(_context, _logger);
        }

        private async Task<string> GeneratePnrAsync()
        {
            for (var attempt = 0; attempt < 12; attempt++)
            {
                var candidate = new string(Enumerable.Range(0, 6)
                    .Select(_ => PnrAlphabet[RandomNumberGenerator.GetInt32(PnrAlphabet.Length)])
                    .ToArray());

                var exists = await _context.Bookings.AnyAsync(b => b.BookingPnr == candidate);
                if (!exists)
                {
                    return candidate;
                }
            }

            return $"T{DateTime.Now:HHmmss}";
        }

        private static string? ValidateContact(QuoteRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ContactFullName) || request.ContactFullName.Trim().Length < 2)
            {
                return "Vui lòng nhập họ tên người liên hệ.";
            }

            var email = (request.ContactEmail ?? "").Trim();
            if (email.Length == 0 || !System.Text.RegularExpressions.Regex.IsMatch(
                    email, @"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$"))
            {
                return "Email người liên hệ không hợp lệ.";
            }

            var phone = NormalizePhone(request.ContactPhone ?? "");
            if (!System.Text.RegularExpressions.Regex.IsMatch(phone, @"^(0\d{9}|\+84\d{9})$"))
            {
                return "Số điện thoại người liên hệ không hợp lệ. Ví dụ: 0912345678.";
            }

            return null;
        }

        private static string NormalizePhone(string phone) =>
            new(phone.Where(c => !char.IsWhiteSpace(c) && c != '-' && c != '.' && c != '(' && c != ')').ToArray());

        private static bool TryParseDate(string? value, out DateOnly date) =>
            DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

        private static int AgeAt(DateOnly dob, DateOnly onDate)
        {
            var age = onDate.Year - dob.Year;
            if (dob.AddYears(age) > onDate) age--;
            return age;
        }

        private static string NormalizeSeatClass(string? value) =>
            (value ?? "").Trim().ToUpperInvariant() switch
            {
                "BUSINESS" or "THƯƠNG GIA" or "THUONG GIA" => "Business",
                _ => "Economy"
            };

        private static string NormalizeTripType(string? value) =>
            string.Equals(value, "RoundTrip", StringComparison.OrdinalIgnoreCase) ? "RoundTrip" : "OneWay";

        private static string NormalizePaymentMethod(string? value) =>
            (value ?? "").Trim().ToUpperInvariant() switch
            {
                "CASH" or "COUNTER" => "Cash",
                "TRANSFER" => "Transfer",
                _ => "MoMo"
            };

        private static string? NormalizePassengerType(string? value) =>
            (value ?? "").Trim().ToUpperInvariant() switch
            {
                "ADULT" => "Adult",
                "CHILD" => "Child",
                "INFANT" => "Infant",
                _ => null
            };

        private static string NormalizeGender(string? value) =>
            (value ?? "").Trim().ToUpperInvariant() switch
            {
                "FEMALE" or "NỮ" or "NU" => "Female",
                "OTHER" => "Other",
                _ => "Male"
            };

        private static bool IsExtraSeat(string seatClass, int row) =>
            seatClass == "Economy" && row is >= 5 and <= 6;

        private static string TypeLabel(string type) => type switch
        {
            "Child" => "Trẻ em",
            "Infant" => "Em bé",
            _ => "Người lớn"
        };

        private static string SeatClassLabel(string seatClass) =>
            seatClass == "Business" ? "Thương gia" : "Phổ thông";

        private static readonly CultureInfo VietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");

        private static string Money(decimal amount) => $"{amount.ToString("N0", VietnameseCulture)} đ";
    }
}
