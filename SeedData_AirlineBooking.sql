USE AirlineBookingDB;
GO

-- 1. Nạp dữ liệu Users (Admin, Staff, Customer)
INSERT INTO Users (Username, PasswordHash, FullName, Email, Phone, Role, LoyaltyPoints, IsActive) VALUES
('admin01', 'AQAAAAEAACcQAAAAEI...', N'Nguyễn Quản Trị', 'admin@airline.com', '0901111222', 'Admin', 0, 1),
('staff01', 'AQAAAAEAACcQAAAAEI...', N'Trần Nhân Viên', 'staff01@airline.com', '0903334444', 'Staff', 0, 1),
('hieuhm', 'AQAAAAEAACcQAAAAEI...', N'Hoàng Minh Hiếu', 'hieuhm@gmail.com', '0988777666', 'Customer', 150, 1),
('anhtuan', 'AQAAAAEAACcQAAAAEI...', N'Lê Anh Tuấn', 'anhtuan@gmail.com', '0912345678', 'Customer', 50, 1);
GO

-- 2. Nạp dữ liệu Airports (Sân bay)
INSERT INTO Airports (AirportCode, AirportName, City, Country) VALUES
('SGN', N'Sân bay Quốc tế Tân Sơn Nhất', N'Hồ Chí Minh', N'Việt Nam'),
('HAN', N'Sân bay Quốc tế Nội Bài', N'Hà Nội', N'Việt Nam'),
('DAD', N'Sân bay Quốc tế Đà Nẵng', N'Đà Nẵng', N'Việt Nam'),
('BKK', N'Sân bay Suvarnabhumi', N'Bangkok', N'Thái Lan');
GO

-- 3. Nạp dữ liệu Aircrafts (Máy bay)
INSERT INTO Aircrafts (Model, TotalSeats) VALUES
('Airbus A321', 180),
('Boeing 737', 200);
GO

-- 4. Nạp dữ liệu Seats (Tạo mẫu vài ghế cho AircraftId = 1)
INSERT INTO Seats (AircraftId, SeatNumber, SeatClass) VALUES
(1, '1A', 'Business'), (1, '1B', 'Business'),
(1, '12A', 'Economy'), (1, '12B', 'Economy'), (1, '12C', 'Economy');
GO

-- 5. Nạp dữ liệu Routes (Tuyến bay nội địa và quốc tế)
INSERT INTO Routes (DepartureAirportCode, ArrivalAirportCode, RouteType) VALUES
('SGN', 'HAN', 'Domestic'),
('HAN', 'SGN', 'Domestic'),
('SGN', 'BKK', 'International');
GO

-- 6. Nạp dữ liệu Flights (Chuyến bay)
INSERT INTO Flights (FlightNumber, RouteId, AircraftId, DepartureTime, ArrivalTime, Status) VALUES
('VJ150', 1, 1, '2026-07-10 08:00:00', '2026-07-10 10:15:00', 'Scheduled'),
('VJ151', 2, 1, '2026-07-15 14:00:00', '2026-07-15 16:15:00', 'Scheduled'),
('VJ801', 3, 1, '2026-07-20 09:30:00', '2026-07-20 11:15:00', 'Scheduled');
GO

-- 7. Nạp dữ liệu FlightPricing (Bảng giá vé)
INSERT INTO FlightPricing (FlightId, SeatClass, PassengerType, BasePrice, TaxAmount, ServiceFee) VALUES
(1, 'Economy', 'Adult', 1500000.00, 150000.00, 100000.00),
(1, 'Economy', 'Child', 1125000.00, 150000.00, 100000.00),
(1, 'Economy', 'Infant', 150000.00, 0.00, 0.00),
(1, 'Business', 'Adult', 3000000.00, 300000.00, 200000.00);
GO

-- 8. Nạp dữ liệu Promotions (Mã khuyến mãi & đổi điểm)
INSERT INTO Promotions (Code, DiscountPercent, DiscountAmount, MinOrderValue, PointsRequired, StartDate, EndDate, UsageLimit, UsedCount) VALUES
('VJSUMMER2026', 10.00, NULL, 500000.00, NULL, '2026-06-01', '2026-08-31', 100, 5),
('FREESHIP50K', NULL, 50000.00, 200000.00, NULL, '2026-01-01', '2026-12-31', 500, 12),
('VIPREDEEM100', 15.00, NULL, 0.00, 100, '2026-01-01', '2026-12-31', 50, 2);
GO

-- 9. Nạp dữ liệu UserPromotions (Khách đổi điểm lấy mã)
INSERT INTO UserPromotions (UserId, PromoId, RedeemedAt, IsUsed) VALUES
(3, 3, '2026-06-12 10:00:00', 0);
GO

-- 10. Nạp dữ liệu Bookings (Đơn đặt chỗ mẫu - Khách có tài khoản, thanh toán MoMo)
INSERT INTO Bookings (BookingPNR, Channel, UserId, CreatedByStaffId, TripType, ContactFullName, ContactEmail, ContactPhone, SubTotal, DiscountAmount, TotalAmount, PaymentMethod, Status, PromoId) VALUES
('X7K9P2', 'Online', 3, NULL, 'OneWay', N'Hoàng Minh Hiếu', 'hieuhm@gmail.com', '0988777666', 1750000.00, 175000.00, 1575000.00, 'MoMo', 'Confirmed', 1);
GO

-- 11. Nạp dữ liệu Passengers (Hành khách trong đơn)
INSERT INTO Passengers (BookingId, AccompanyingPassengerId, FullName, Gender, Dob, PassengerType, PassportNumber) VALUES
(1, NULL, N'Hoàng Minh Hiếu', 'Male', '2006-02-17', 'Adult', 'B1234567');
GO

-- 12. Nạp dữ liệu Tickets (Vé máy bay đã phát hành, gắn ghế 12A)
INSERT INTO Tickets (BookingId, PassengerId, FlightId, SeatId, TicketNumber, Direction, SeatClass, Price, TaxAmount, ServiceFee, Status) VALUES
(1, 1, 1, 3, 'TK-VJ150-001', 'Outbound', 'Economy', 1500000.00, 150000.00, 100000.00, 'Issued');
GO

-- 13. Nạp dữ liệu Payments (Giao dịch thanh toán thành công qua MoMo)
INSERT INTO Payments (BookingId, RequestId, PaymentType, Method, Amount, TransactionCode, Status, ConfirmedByStaffId, PaidAt) VALUES
(1, NULL, 'Payment', 'MoMo', 1575000.00, 'MOMO_TXN_987654321', 'Success', NULL, '2026-06-12 10:05:00');
GO

-- 14. Nạp dữ liệu LoyaltyTransactions (Ghi nhận tích điểm cho đơn hàng thành công)
INSERT INTO LoyaltyTransactions (UserId, BookingId, Points, TransactionType, Description) VALUES
(3, 1, 150, 'Earn', N'Tích điểm từ đơn hàng đặt vé PNR: X7K9P2');
GO