namespace MpRiskReward.Core;

public sealed record RiskRewardInput(
    TradeDirection Direction,
    FuturesInstrumentSpec Instrument,
    double EntryPrice,
    double StopLossPrice,
    double TakeProfitPrice,
    int Quantity,
    double CommissionPerContractRoundTurn = 0)
{
    public void Validate()
    {
        Instrument.Validate();

        if (EntryPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(EntryPrice), "Cena wejścia musi być większa od zera.");

        if (StopLossPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(StopLossPrice), "Stop Loss musi być większy od zera.");

        if (TakeProfitPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(TakeProfitPrice), "Take Profit musi być większy od zera.");

        if (Quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(Quantity), "Liczba kontraktów musi być większa od zera.");

        if (CommissionPerContractRoundTurn < 0)
            throw new ArgumentOutOfRangeException(nameof(CommissionPerContractRoundTurn), "Prowizja nie może być ujemna.");

        if (Direction == TradeDirection.Long)
        {
            if (StopLossPrice >= EntryPrice)
                throw new ArgumentException("Dla pozycji Long Stop Loss musi być poniżej ceny wejścia.");

            if (TakeProfitPrice <= EntryPrice)
                throw new ArgumentException("Dla pozycji Long Take Profit musi być powyżej ceny wejścia.");
        }
        else
        {
            if (StopLossPrice <= EntryPrice)
                throw new ArgumentException("Dla pozycji Short Stop Loss musi być powyżej ceny wejścia.");

            if (TakeProfitPrice >= EntryPrice)
                throw new ArgumentException("Dla pozycji Short Take Profit musi być poniżej ceny wejścia.");
        }
    }
}

