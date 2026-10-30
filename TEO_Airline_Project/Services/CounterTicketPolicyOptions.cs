namespace TEO_Airline_Project.Services;

public sealed class CounterTicketPolicyOptions
{
    public decimal ExchangeSurchargePercent { get; init; }
    public decimal PartialCancellationRefundPercent { get; init; }
}
