SELECT
    -- Doanh thu thực thu: thanh toán + phụ thu - hoàn tiền thành công (toàn thời gian).
    (
        SELECT COALESCE(SUM(
            CASE WHEN p.PaymentType = 'Refund'
                 THEN -p.Amount
                 ELSE p.Amount
            END
        ), 0)
        FROM Payments p
        WHERE p.Status = 'Success'
          AND p.PaymentType IN ('Payment', 'Surcharge', 'Refund')
    ) AS Revenue,

    -- Số đơn đặt chỗ tạo, gồm mọi trạng thái (toàn thời gian).
    (
        SELECT COUNT(*)
        FROM Bookings b
    ) AS BookingCount,

    -- Vé đã bán, tính theo ngày đặt chỗ; không tính vé chờ thanh toán hoặc đã hủy (toàn thời gian).
    (
        SELECT COUNT(*)
        FROM Tickets t
        JOIN Bookings b ON b.BookingId = t.BookingId
        WHERE b.Status = 'Confirmed'
          AND t.Status IN ('Issued', 'CheckedIn')
    ) AS SoldTicketCount,

    -- Vé đã hủy; loại các vé bị hủy do đơn giữ chỗ hết hạn thanh toán (toàn thời gian).
    (
        SELECT COUNT(*)
        FROM Tickets t
        JOIN Bookings b ON b.BookingId = t.BookingId
        WHERE b.Status <> 'Expired'
          AND t.Status = 'Cancelled'
    ) AS CancelledTicketCount,

    -- Yêu cầu đổi vé đã được duyệt, tính theo ngày gửi yêu cầu (toàn thời gian).
    (
        SELECT COUNT(*)
        FROM BookingRequests r
        WHERE r.RequestType = 'Change'
          AND r.Status = 'Approved'
    ) AS ApprovedChangeCount;