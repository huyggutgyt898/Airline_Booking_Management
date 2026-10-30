using System.Globalization;

namespace TEO_Airline_Project.Services;

public static class SeatPricingRules
{
    public const decimal ExtraSeatFee = 150000m;

    public static decimal GetExtraFee(string seatClass, int row) =>
        IsExtraSeat(seatClass, row) ? ExtraSeatFee : 0m;

    public static decimal GetExtraFee(string seatClass, string seatNumber)
    {
        var rowText = new string(seatNumber.TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(rowText, NumberStyles.None, CultureInfo.InvariantCulture, out var row)
            ? GetExtraFee(seatClass, row)
            : 0m;
    }

    public static bool IsExtraSeat(string seatClass, int row) =>
        seatClass == "Economy" && row is >= 5 and <= 6;
}
