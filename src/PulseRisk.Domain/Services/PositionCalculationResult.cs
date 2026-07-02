namespace PulseRisk.Domain.Services;

public readonly record struct PositionCalculationResult(decimal NetVolume, decimal AveragePrice);
