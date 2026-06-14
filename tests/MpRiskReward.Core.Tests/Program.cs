using MpRiskReward.Core;

static void EqualDouble(double expected, double actual, string name, double tolerance = 0.000001)
{
    if (Math.Abs(expected - actual) > tolerance)
        throw new Exception($"{name}: expected {expected}, actual {actual}");
}

static void EqualInt(int expected, int actual, string name)
{
    if (expected != actual)
        throw new Exception($"{name}: expected {expected}, actual {actual}");
}

var mnqLong = RiskRewardCalculator.Calculate(new RiskRewardInput(
    TradeDirection.Long,
    FuturesInstrumentSpec.MNQ,
    EntryPrice: 23642.25,
    StopLossPrice: 23632.25,
    TakeProfitPrice: 23672.25,
    Quantity: 5));

EqualDouble(40, mnqLong.StopTicks, "MNQ long stop ticks");
EqualDouble(120, mnqLong.TargetTicks, "MNQ long target ticks");
EqualDouble(100, mnqLong.GrossRisk, "MNQ long gross risk");
EqualDouble(300, mnqLong.GrossReward, "MNQ long gross reward");
EqualDouble(3, mnqLong.RiskRewardRatio, "MNQ long RR");

var nqShort = RiskRewardCalculator.Calculate(new RiskRewardInput(
    TradeDirection.Short,
    FuturesInstrumentSpec.NQ,
    EntryPrice: 23642.25,
    StopLossPrice: 23652.25,
    TakeProfitPrice: 23622.25,
    Quantity: 2));

EqualDouble(40, nqShort.StopTicks, "NQ short stop ticks");
EqualDouble(80, nqShort.TargetTicks, "NQ short target ticks");
EqualDouble(400, nqShort.GrossRisk, "NQ short gross risk");
EqualDouble(800, nqShort.GrossReward, "NQ short gross reward");
EqualDouble(2, nqShort.RiskRewardRatio, "NQ short RR");

EqualInt(10, RiskRewardCalculator.QuantityForFixedDollarRisk(
    FuturesInstrumentSpec.MNQ,
    entryPrice: 23642.25,
    stopLossPrice: 23622.25,
    maxDollarRisk: 400), "MNQ fixed dollar quantity");

EqualInt(2, RiskRewardCalculator.QuantityForBalancePercentRisk(
    FuturesInstrumentSpec.MNQ,
    entryPrice: 23642.25,
    stopLossPrice: 23622.25,
    accountBalance: 50000,
    riskPercent: 0.2), "MNQ balance percent quantity");

Console.WriteLine("All RiskRewardCalculator tests passed.");
