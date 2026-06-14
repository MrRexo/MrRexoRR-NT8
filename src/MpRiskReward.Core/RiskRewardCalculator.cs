namespace MpRiskReward.Core;

public static class RiskRewardCalculator
{
    public static RiskRewardResult Calculate(RiskRewardInput input)
    {
        input.Validate();

        double stopTicks = Math.Abs(input.EntryPrice - input.StopLossPrice) / input.Instrument.TickSize;
        double targetTicks = Math.Abs(input.TakeProfitPrice - input.EntryPrice) / input.Instrument.TickSize;
        double grossRisk = stopTicks * input.Instrument.TickValue * input.Quantity;
        double grossReward = targetTicks * input.Instrument.TickValue * input.Quantity;
        double commission = input.CommissionPerContractRoundTurn * input.Quantity;
        double netRisk = grossRisk + commission;
        double netReward = Math.Max(0, grossReward - commission);
        double ratio = grossRisk == 0 ? 0 : grossReward / grossRisk;

        return new RiskRewardResult(
            input.Direction,
            input.Instrument.Symbol,
            input.Quantity,
            RoundToTick(input.EntryPrice, input.Instrument.TickSize),
            RoundToTick(input.StopLossPrice, input.Instrument.TickSize),
            RoundToTick(input.TakeProfitPrice, input.Instrument.TickSize),
            stopTicks,
            targetTicks,
            grossRisk,
            grossReward,
            netRisk,
            netReward,
            ratio,
            input.Instrument.Currency);
    }

    public static int QuantityForFixedDollarRisk(
        FuturesInstrumentSpec instrument,
        double entryPrice,
        double stopLossPrice,
        double maxDollarRisk)
    {
        instrument.Validate();

        if (maxDollarRisk <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxDollarRisk), "Ryzyko w dolarach musi być większe od zera.");

        double stopTicks = Math.Abs(entryPrice - stopLossPrice) / instrument.TickSize;
        if (stopTicks <= 0)
            throw new ArgumentException("Odległość Stop Loss od wejścia musi być większa od zera.");

        double riskPerContract = stopTicks * instrument.TickValue;
        return Math.Max(1, (int)Math.Floor(maxDollarRisk / riskPerContract));
    }

    public static int QuantityForBalancePercentRisk(
        FuturesInstrumentSpec instrument,
        double entryPrice,
        double stopLossPrice,
        double accountBalance,
        double riskPercent)
    {
        if (accountBalance <= 0)
            throw new ArgumentOutOfRangeException(nameof(accountBalance), "Saldo konta musi być większe od zera.");

        if (riskPercent <= 0)
            throw new ArgumentOutOfRangeException(nameof(riskPercent), "Procent ryzyka musi być większy od zera.");

        double maxDollarRisk = accountBalance * riskPercent / 100.0;
        return QuantityForFixedDollarRisk(instrument, entryPrice, stopLossPrice, maxDollarRisk);
    }

    public static double RoundToTick(double price, double tickSize)
    {
        if (tickSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(tickSize), "Tick size musi być większy od zera.");

        return Math.Round(price / tickSize, MidpointRounding.AwayFromZero) * tickSize;
    }
}

