using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Infrastructure.Persistence.Converters;

internal static class ValueObjectConverters
{
    public static readonly ValueConverter<Symbol, string> Symbol = new(
        symbol => symbol.Value,
        value => new Symbol(value));

    public static readonly ValueConverter<Price, decimal> Price = new(
        price => price.Value,
        value => new Price(value));

    public static readonly ValueConverter<Volume, decimal> Volume = new(
        volume => volume.Value,
        value => new Volume(value));
}

