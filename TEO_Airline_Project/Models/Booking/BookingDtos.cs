namespace TEO_Airline_Project.Models.BookingModels;

public sealed class SearchRequest
{
    public string? Origin { get; set; }
    public string? Destination { get; set; }
    public string? DepartureDate { get; set; }
    public string? ReturnDate { get; set; }
    public string? TripType { get; set; }
    public string? SeatClass { get; set; }
    public int Adults { get; set; } = 1;
    public int Children { get; set; }
    public int Infants { get; set; }
}

public sealed class SearchFlightDto
{
    public int FlightId { get; set; }
    public string FlightNumber { get; set; } = "";
    public string Date { get; set; } = "";
    public string DepartureTime { get; set; } = "";
    public string ArrivalTime { get; set; } = "";
    public int DurationMinutes { get; set; }
    public string DepartureCode { get; set; } = "";
    public string ArrivalCode { get; set; } = "";
    public string DepartureCity { get; set; } = "";
    public string ArrivalCity { get; set; } = "";
    public string AircraftModel { get; set; } = "";
    public decimal PricePerAdult { get; set; }
    public decimal PricePerChild { get; set; }
    public decimal PricePerInfant { get; set; }
    public int SeatsLeft { get; set; }
}

public sealed class SearchResponse
{
    public bool Ok { get; set; }
    public string? Message { get; set; }
    public List<SearchFlightDto> Outbound { get; set; } = new();
    public List<SearchFlightDto> Inbound { get; set; } = new();
}

public sealed class PassengerInput
{
    public string? Type { get; set; }
    public string? FullName { get; set; }
    public string? Gender { get; set; }
    public string? Dob { get; set; }
    public string? DocumentNumber { get; set; }
    public int? AccompanyingIndex { get; set; }
}

public sealed class SeatSelectionInput
{
    public List<int> Outbound { get; set; } = new();
    public List<int> Return { get; set; } = new();
}

public sealed class QuoteRequest
{
    public string? TripType { get; set; }
    public string? SeatClass { get; set; }
    public int OutboundFlightId { get; set; }
    public int? ReturnFlightId { get; set; }
    public List<PassengerInput> Passengers { get; set; } = new();
    public SeatSelectionInput Seats { get; set; } = new();
    public string? PromoCode { get; set; }
    public string? PaymentMethod { get; set; }
    public string? ContactFullName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}

public sealed class QuoteLineDto
{
    public string Label { get; set; } = "";
    public decimal Amount { get; set; }
}

public sealed class FlightSummaryDto
{
    public int FlightId { get; set; }
    public string FlightNumber { get; set; } = "";
    public string Date { get; set; } = "";
    public string DepartureTime { get; set; } = "";
    public string ArrivalTime { get; set; } = "";
    public string DepartureCode { get; set; } = "";
    public string ArrivalCode { get; set; } = "";
    public string Direction { get; set; } = "";
}

public sealed class QuoteResponse
{
    public bool Ok { get; set; }
    public string? Message { get; set; }
    public string TripType { get; set; } = "OneWay";
    public string SeatClass { get; set; } = "Economy";
    public List<QuoteLineDto> Lines { get; set; } = new();
    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public bool PromoApplied { get; set; }
    public string? PromoCode { get; set; }
    public string? PromoMessage { get; set; }
    public int HoldMinutes { get; set; }
    public DateTime ExpiresAt { get; set; }
    public List<FlightSummaryDto> Flights { get; set; } = new();
    public int SeatCount { get; set; }
}

public sealed class ConfirmResponse
{
    public bool Ok { get; set; }
    public string? Message { get; set; }
    public string? BookingPnr { get; set; }
    public int BookingId { get; set; }
    public string Status { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int PointsEarned { get; set; }
    public List<QuoteLineDto> Lines { get; set; } = new();
    public List<FlightSummaryDto> Flights { get; set; } = new();
    public List<string> PassengerSummaries { get; set; } = new();
    public List<string> SeatSummaries { get; set; } = new();
}
