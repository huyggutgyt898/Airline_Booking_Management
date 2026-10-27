using Microsoft.EntityFrameworkCore;
using TEO_Airline_Project.Models;

namespace TEO_Airline_Project.Controllers
{
    internal static class BookingMaintenance
    {
        public const int MomoHoldMinutes = 10;
        public const int CounterHoldMinutes = 60;

        public static async Task ExpireOverdueBookingsAsync(AirlineBookingDBContext context, ILogger logger)
        {
            try
            {
                await context.Database.ExecuteSqlRawAsync(@"
UPDATE Tickets SET Status = 'Cancelled'
WHERE Status = 'PendingPayment'
  AND BookingId IN (
      SELECT BookingId FROM Bookings
      WHERE Status = 'PendingPayment'
        AND CASE WHEN PaymentMethod = 'MoMo'
                 THEN DATEADD(minute, {0}, BookingDate)
                 ELSE DATEADD(minute, {1}, BookingDate) END < GETDATE());

UPDATE Bookings SET Status = 'Expired'
WHERE Status = 'PendingPayment'
  AND CASE WHEN PaymentMethod = 'MoMo'
           THEN DATEADD(minute, {0}, BookingDate)
           ELSE DATEADD(minute, {1}, BookingDate) END < GETDATE();",
                    MomoHoldMinutes, CounterHoldMinutes);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Không thể dọn các đặt chỗ quá hạn");
            }
        }
    }
}
