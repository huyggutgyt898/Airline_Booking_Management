using TEO_Airline_Project.Models.CounterTickets;

namespace TEO_Airline_Project.Services;

public interface ICounterTicketService
{
    Task<bool> IsStaffAsync(int staffId, CancellationToken cancellationToken);

    Task<CounterTicketResult<IReadOnlyList<CounterTicketResponse>>> SearchAsync(
        CounterTicketSearchRequest request,
        CancellationToken cancellationToken);

    Task<CounterTicketResult<IReadOnlyList<CounterTicketFlightOptionResponse>>> GetFlightOptionsAsync(
        int ticketId,
        CancellationToken cancellationToken);

    Task<CounterTicketResult<CounterTicketOperationResponse>> ExchangeAsync(
        int ticketId,
        int newFlightId,
        string reason,
        bool paymentConfirmed,
        int staffId,
        CancellationToken cancellationToken);

    Task<CounterTicketResult<CounterTicketOperationResponse>> CancelAsync(
        int ticketId,
        string reason,
        bool refundConfirmed,
        int staffId,
        CancellationToken cancellationToken);
}

public sealed record CounterTicketResult<T>(int? ErrorStatus, string? Error, T? Value)
{
    public bool Succeeded => ErrorStatus is null;

    public static CounterTicketResult<T> Success(T value) => new(null, null, value);

    public static CounterTicketResult<T> Failure(int status, string error) =>
        new(status, error, default);
}
