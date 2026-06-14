namespace MpRiskReward.Core;

public sealed record FuturesInstrumentSpec(
    string Symbol,
    double TickSize,
    double TickValue,
    string Currency = "USD")
{
    public static readonly FuturesInstrumentSpec MNQ = new("MNQ", 0.25, 0.50);
    public static readonly FuturesInstrumentSpec NQ = new("NQ", 0.25, 5.00);
    public static readonly FuturesInstrumentSpec MES = new("MES", 0.25, 1.25);
    public static readonly FuturesInstrumentSpec ES = new("ES", 0.25, 12.50);

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Symbol))
            throw new ArgumentException("Symbol instrumentu jest wymagany.", nameof(Symbol));

        if (TickSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(TickSize), "Tick size musi być większy od zera.");

        if (TickValue <= 0)
            throw new ArgumentOutOfRangeException(nameof(TickValue), "Wartość ticka musi być większa od zera.");
    }
}

