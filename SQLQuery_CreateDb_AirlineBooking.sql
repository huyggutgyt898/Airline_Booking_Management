-- ====================================================================
-- TẠO CƠ SỞ DỮ LIỆU ĐẶT VÉ MÁY BAY TRỰC TUYẾN
-- ====================================================================
CREATE DATABASE AirlineBookingDB;
GO

USE AirlineBookingDB;
GO

-- 1. Bảng Users (Người dùng, Nhân viên, Quản trị viên)
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username VARCHAR(50) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Email VARCHAR(100) UNIQUE NOT NULL,
    Phone VARCHAR(20) NOT NULL,
    Role VARCHAR(20) NOT NULL CHECK (Role IN ('Customer', 'Staff', 'Admin')),
    LoyaltyPoints INT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- 2. Bảng Airports (Sân bay)
CREATE TABLE Airports (
    AirportCode VARCHAR(10) PRIMARY KEY,
    AirportName NVARCHAR(150) NOT NULL,
    City NVARCHAR(100) NOT NULL,
    Country NVARCHAR(100) NOT NULL
);
GO

-- 3. Bảng Aircrafts (Máy bay)
CREATE TABLE Aircrafts (
    AircraftId INT IDENTITY(1,1) PRIMARY KEY,
    Model NVARCHAR(100) NOT NULL,
    TotalSeats INT NOT NULL
);
GO

-- 4. Bảng Seats (Sơ đồ ghế gốc theo máy bay)
CREATE TABLE Seats (
    SeatId INT IDENTITY(1,1) PRIMARY KEY,
    AircraftId INT NOT NULL FOREIGN KEY REFERENCES Aircrafts(AircraftId),
    SeatNumber VARCHAR(10) NOT NULL,
    SeatClass VARCHAR(30) NOT NULL CHECK (SeatClass IN ('Economy', 'Business')),
    CONSTRAINT UQ_Aircraft_SeatNumber UNIQUE (AircraftId, SeatNumber)
);
GO

-- 5. Bảng Routes (Tuyến bay)
CREATE TABLE Routes (
    RouteId INT IDENTITY(1,1) PRIMARY KEY,
    DepartureAirportCode VARCHAR(10) NOT NULL FOREIGN KEY REFERENCES Airports(AirportCode),
    ArrivalAirportCode VARCHAR(10) NOT NULL FOREIGN KEY REFERENCES Airports(AirportCode),
    RouteType VARCHAR(20) NOT NULL CHECK (RouteType IN ('Domestic', 'International')),
    CONSTRAINT UQ_Route_Airports UNIQUE (DepartureAirportCode, ArrivalAirportCode),
    CONSTRAINT CHK_DifferentAirports CHECK (DepartureAirportCode <> ArrivalAirportCode)
);
GO

-- 6. Bảng Flights (Chuyến bay)
CREATE TABLE Flights (
    FlightId INT IDENTITY(1,1) PRIMARY KEY,
    FlightNumber VARCHAR(20) NOT NULL,
    RouteId INT NOT NULL FOREIGN KEY REFERENCES Routes(RouteId),
    AircraftId INT NOT NULL FOREIGN KEY REFERENCES Aircrafts(AircraftId),
    DepartureTime DATETIME NOT NULL,
    ArrivalTime DATETIME NOT NULL,
    Status VARCHAR(30) DEFAULT 'Scheduled' CHECK (Status IN ('Scheduled', 'Delayed', 'Cancelled', 'Completed')),
    CONSTRAINT CHK_FlightTime CHECK (ArrivalTime > DepartureTime)
);
GO

-- 7. Bảng FlightPricing (Bảng giá vé cơ bản, thuế, phí theo chuyến bay)
CREATE TABLE FlightPricing (
    PriceId INT IDENTITY(1,1) PRIMARY KEY,
    FlightId INT NOT NULL FOREIGN KEY REFERENCES Flights(FlightId),
    SeatClass VARCHAR(30) NOT NULL,
    PassengerType VARCHAR(30) NOT NULL CHECK (PassengerType IN ('Adult', 'Child', 'Infant')),
    BasePrice DECIMAL(18,2) NOT NULL,
    TaxAmount DECIMAL(18,2) NOT NULL,
    ServiceFee DECIMAL(18,2) NOT NULL,
    CONSTRAINT UQ_Flight_Pricing UNIQUE (FlightId, SeatClass, PassengerType)
);
GO

-- 8. Bảng Promotions (Mã khuyến mãi)
CREATE TABLE Promotions (
    PromoId INT IDENTITY(1,1) PRIMARY KEY,
    Code VARCHAR(50) UNIQUE NOT NULL,
    DiscountPercent DECIMAL(5,2) NULL,
    DiscountAmount DECIMAL(18,2) NULL,
    MinOrderValue DECIMAL(18,2) DEFAULT 0,
    PointsRequired INT NULL, -- Điểm cần để đổi lấy mã này
    StartDate DATETIME NOT NULL,
    EndDate DATETIME NOT NULL,
    UsageLimit INT NOT NULL,
    UsedCount INT DEFAULT 0,
    CONSTRAINT CHK_Promo_Discount CHECK (DiscountPercent IS NOT NULL OR DiscountAmount IS NOT NULL),
    CONSTRAINT CHK_Promo_Usage CHECK (UsedCount <= UsageLimit)
);
GO

-- 9. Bảng UserPromotions (Mã giảm giá cá nhân do khách đổi bằng điểm)
CREATE TABLE UserPromotions (
    UserPromoId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    PromoId INT NOT NULL FOREIGN KEY REFERENCES Promotions(PromoId),
    RedeemedAt DATETIME DEFAULT GETDATE(),
    IsUsed BIT DEFAULT 0
);
GO

-- 10. Bảng Bookings (Đơn đặt chỗ / PNR) - Đã đổi BookingCode -> BookingPNR và bỏ PaymentDeadline
CREATE TABLE Bookings (
    BookingId INT IDENTITY(1,1) PRIMARY KEY,
    BookingPNR VARCHAR(10) UNIQUE NOT NULL,
    Channel VARCHAR(20) NOT NULL CHECK (Channel IN ('Online', 'Counter')),
    UserId INT NULL FOREIGN KEY REFERENCES Users(UserId),
    CreatedByStaffId INT NULL FOREIGN KEY REFERENCES Users(UserId),
    TripType VARCHAR(20) NOT NULL CHECK (TripType IN ('OneWay', 'RoundTrip')),
    ContactFullName NVARCHAR(100) NOT NULL,
    ContactEmail VARCHAR(100) NOT NULL,
    ContactPhone VARCHAR(20) NOT NULL,
    BookingDate DATETIME DEFAULT GETDATE(),
    SubTotal DECIMAL(18,2) NOT NULL,
    DiscountAmount DECIMAL(18,2) DEFAULT 0,
    TotalAmount DECIMAL(18,2) NOT NULL,
    PaymentMethod VARCHAR(30) NOT NULL CHECK (PaymentMethod IN ('MoMo', 'Cash', 'Transfer')),
    Status VARCHAR(30) DEFAULT 'PendingPayment' CHECK (Status IN ('PendingPayment', 'Confirmed', 'Cancelled', 'Expired')),
    PromoId INT NULL FOREIGN KEY REFERENCES Promotions(PromoId)
);
GO

-- 11. Bảng Passengers (Hành khách trong đơn)
CREATE TABLE Passengers (
    PassengerId INT IDENTITY(1,1) PRIMARY KEY,
    BookingId INT NOT NULL FOREIGN KEY REFERENCES Bookings(BookingId),
    AccompanyingPassengerId INT NULL FOREIGN KEY REFERENCES Passengers(PassengerId),
    FullName NVARCHAR(100) NOT NULL,
    Gender VARCHAR(10) NOT NULL CHECK (Gender IN ('Male', 'Female', 'Other')),
    Dob DATE NOT NULL,
    PassengerType VARCHAR(30) NOT NULL CHECK (PassengerType IN ('Adult', 'Child', 'Infant')),
    PassportNumber VARCHAR(50) NULL
);
GO

-- 12. Bảng Tickets (Chi tiết vé từng chặng, khóa ghế độc lập)
CREATE TABLE Tickets (
    TicketId INT IDENTITY(1,1) PRIMARY KEY,
    BookingId INT NOT NULL FOREIGN KEY REFERENCES Bookings(BookingId),
    PassengerId INT NOT NULL FOREIGN KEY REFERENCES Passengers(PassengerId),
    FlightId INT NOT NULL FOREIGN KEY REFERENCES Flights(FlightId),
    SeatId INT NULL FOREIGN KEY REFERENCES Seats(SeatId),
    TicketNumber VARCHAR(30) UNIQUE NOT NULL,
    Direction VARCHAR(20) NOT NULL CHECK (Direction IN ('Outbound', 'Return')),
    SeatClass VARCHAR(30) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    TaxAmount DECIMAL(18,2) NOT NULL,
    ServiceFee DECIMAL(18,2) NOT NULL,
    Status VARCHAR(30) DEFAULT 'PendingPayment' CHECK (Status IN ('PendingPayment', 'Issued', 'CheckedIn', 'Changed', 'Cancelled')),
    CheckInTime DATETIME NULL
);
GO

-- Tạo Unique Filtered Index để chống trùng ghế trên cùng một chuyến bay đang hoạt động
CREATE UNIQUE INDEX UX_Tickets_Flight_Seat
ON Tickets(FlightId, SeatId)
WHERE SeatId IS NOT NULL AND Status NOT IN ('Cancelled', 'Changed');
GO

-- 13. Bảng Payments (Quản lý dòng tiền: thanh toán, hoàn tiền, phụ phí)
CREATE TABLE Payments (
    PaymentId INT IDENTITY(1,1) PRIMARY KEY,
    BookingId INT NOT NULL FOREIGN KEY REFERENCES Bookings(BookingId),
    RequestId INT NULL,
    PaymentType VARCHAR(20) NOT NULL CHECK (PaymentType IN ('Payment', 'Refund', 'Surcharge')),
    Method VARCHAR(30) NOT NULL CHECK (Method IN ('MoMo', 'Cash', 'Transfer')),
    Amount DECIMAL(18,2) NOT NULL,
    TransactionCode VARCHAR(100) NULL,
    Status VARCHAR(30) DEFAULT 'Pending' CHECK (Status IN ('Pending', 'Success', 'Failed')),
    ConfirmedByStaffId INT NULL FOREIGN KEY REFERENCES Users(UserId),
    PaidAt DATETIME NULL
);
GO

-- 14. Bảng BookingRequests (Xử lý Đổi / Hủy vé)
CREATE TABLE BookingRequests (
    RequestId INT IDENTITY(1,1) PRIMARY KEY,
    TicketId INT NOT NULL FOREIGN KEY REFERENCES Tickets(TicketId),
    RequestType VARCHAR(20) NOT NULL CHECK (RequestType IN ('Change', 'Cancel')),
    RequestedByUserId INT NULL FOREIGN KEY REFERENCES Users(UserId),
    Reason NVARCHAR(MAX) NULL,
    NewFlightId INT NULL FOREIGN KEY REFERENCES Flights(FlightId),
    NewSeatId INT NULL FOREIGN KEY REFERENCES Seats(SeatId),
    PriceDifference DECIMAL(18,2) DEFAULT 0,
    Surcharge DECIMAL(18,2) DEFAULT 0,
    RefundAmount DECIMAL(18,2) DEFAULT 0,
    Status VARCHAR(30) DEFAULT 'Pending' CHECK (Status IN ('Pending', 'Approved', 'Rejected')),
    ProcessedByStaffId INT NULL FOREIGN KEY REFERENCES Users(UserId),
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- 15. Bảng LoyaltyTransactions (Lịch sử tích điểm & đổi điểm)
CREATE TABLE LoyaltyTransactions (
    TransactionId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    BookingId INT NULL FOREIGN KEY REFERENCES Bookings(BookingId),
    Points INT NOT NULL,
    TransactionType VARCHAR(30) NOT NULL CHECK (TransactionType IN ('Earn', 'Redeem')),
    Description NVARCHAR(255) NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- Tạo các Index tối ưu truy vấn
CREATE NONCLUSTERED INDEX IX_Flights_Route_Departure ON Flights(RouteId, DepartureTime);
CREATE NONCLUSTERED INDEX IX_Bookings_ContactEmail ON Bookings(ContactEmail);
CREATE NONCLUSTERED INDEX IX_Tickets_FlightId ON Tickets(FlightId);
CREATE NONCLUSTERED INDEX IX_Bookings_UserId ON Bookings(UserId);
GO