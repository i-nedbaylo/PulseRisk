namespace PulseRisk.Domain.Services;

public readonly record struct PositionCalculationState
{
    public PositionCalculationState(decimal netVolume, decimal averagePrice)
    {
        if (averagePrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(averagePrice), "Average price must not be negative.");
        }

        if (netVolume == 0 && averagePrice != 0)
        {
            throw new ArgumentException("Flat position must have zero average price.", nameof(averagePrice));
        }

        if (netVolume != 0 && averagePrice == 0)
        {
            throw new ArgumentException("Open position must have non-zero average price.", nameof(averagePrice));
        }

        NetVolume = netVolume;
        AveragePrice = averagePrice;
    }

    public decimal NetVolume { get; }

    public decimal AveragePrice { get; }

    public static PositionCalculationState Flat { get; } = new(0, 0);
}
