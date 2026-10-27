namespace TEO_Airline_Project.Models.BookingModels;

public sealed class LookupRequest
{
    public string? Code { get; set; }
    public string? Contact { get; set; }
}

public sealed class LookupBookingDto
{
    public string Pnr { get; set; } = "";
    public string Status { get; set; } = "";
    public string StatusLabel { get; set; } = "";
    public string StatusTone { get; set; } = "muted";
    public string TripLabel { get; set; } = "";
    public string ContactName { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public string ContactPhone { get; set; } = "";
    public DateTime? BookingDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal? DiscountAmount { get; set; }
    public string PaymentMethod { get; set; } = "";
    public string PaymentLabel { get; set; } = "";
    public DateTime? ExpiresAt { get; set; }
    public DateTime? DepartureTime { get; set; }
    public List<LookupFlightDto> Flights { get; set; } = new();
    public List<LookupPassengerDto> Passengers { get; set; } = new();
    public List<string> Seats { get; set; } = new();
}

public sealed class LookupFlightDto
{
    public string DirectionLabel { get; set; } = "";
    public string FlightNumber { get; set; } = "";
    public string Date { get; set; } = "";
    public string DepartureTime { get; set; } = "";
    public string ArrivalTime { get; set; } = "";
    public string DepartureCode { get; set; } = "";
    public string ArrivalCode { get; set; } = "";
}

public sealed class LookupPassengerDto
{
    public string FullName { get; set; } = "";
    public string TypeLabel { get; set; } = "";
    public string SeatLabel { get; set; } = "";
}

public sealed class LookupResponse
{
    public bool Ok { get; set; }
    public string? Message { get; set; }
    public List<LookupBookingDto> Bookings { get; set; } = new();
}
