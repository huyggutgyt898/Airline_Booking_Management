using System.ComponentModel.DataAnnotations;

namespace TEO_Airline_Project.Models.CounterTickets;

/// <summary>Filters staff ticket lookup by booking reference and passenger verification details.</summary>
public sealed record CounterTicketSearchRequest
{
    [Required, StringLength(10, MinimumLength = 3)]
    public required string BookingCode { get; init; }

    [StringLength(100)]
    public string? PassengerName { get; init; }

    [StringLength(100)]
    public string? Contact { get; init; }

    public DateOnly? FlightDate { get; init; }
}

/// <summary>A ticket and its current exchange or cancellation eligibility.</summary>
public sealed record CounterTicketResponse(
    int TicketId,
    string TicketNumber,
    string BookingCode,
    string BookingStatus,
    string PassengerName,
    string Contact,
    string FlightNumber,
    string OriginCode,
    string OriginCity,
    string DestinationCode,
    string DestinationCity,
    DateTimeOffset DepartureTime,
    DateTimeOffset ArrivalTime,
    string? SeatNumber,
    string SeatClass,
    string TicketStatus,
    decimal TicketFare,
    decimal TaxAmount,
    decimal ServiceFee,
    decimal AllocatedPaidAmount,
    bool CanExchange,
    decimal ExchangeSurcharge,
    string ExchangeMessage,
    bool CanCancel,
    bool FullRefund,
    decimal CancellationRefundAmount,
    string CancellationMessage,
    IReadOnlyList<CounterTicketHistoryResponse> History);

/// <summary>An earlier change or cancellation request recorded for a ticket.</summary>
public sealed record CounterTicketHistoryResponse(
    int RequestId,
    string RequestType,
    string Status,
    string Reason,
    decimal Surcharge,
    decimal PriceDifference,
    decimal RefundAmount,
    DateTimeOffset? CreatedAt,
    string? ProcessedBy);

/// <summary>A same-route flight and seat available for an eligible ticket exchange.</summary>
public sealed record CounterTicketFlightOptionResponse(
    int FlightId,
    string FlightNumber,
    DateTimeOffset DepartureTime,
    DateTimeOffset ArrivalTime,
    string OriginCode,
    string DestinationCode,
    int SeatId,
    string SeatNumber,
    decimal NewGrossFare,
    decimal PriceDifference,
    decimal Surcharge,
    decimal AmountDue);

/// <summary>The result of a completed ticket change or cancellation.</summary>
public sealed record CounterTicketOperationResponse(
    string BookingCode,
    string TicketNumber,
    string RequestType,
    string RequestStatus,
    string Message,
    string? NewFlightNumber,
    string? NewSeatNumber,
    decimal PriceDifference,
    decimal Surcharge,
    decimal RefundAmount,
    decimal AmountCollected);

/// <summary>Requests a ticket change to one of the returned flight options.</summary>
public sealed record ExchangeCounterTicketRequest
{
    [Range(1, int.MaxValue)]
    public int NewFlightId { get; init; }

    [Required, StringLength(500, MinimumLength = 3)]
    public required string Reason { get; init; }

    public bool PaymentConfirmed { get; init; }
}

/// <summary>Requests cancellation of an eligible ticket.</summary>
public sealed record CancelCounterTicketRequest
{
    [Required, StringLength(500, MinimumLength = 3)]
    public required string Reason { get; init; }

    public bool RefundConfirmed { get; init; }
}
