namespace MpRiskReward.Core;

public sealed record RiskRewardResult(
    TradeDirection Direction,
    string Symbol,
    int Quantity,
    double EntryPrice,
    double StopLossPrice,
    double TakeProfitPrice,
    double StopTicks,
    double TargetTicks,
    double GrossRisk,
    double GrossReward,
    double NetRisk,
    double NetReward,
    double RiskRewardRatio,
    string Currency);

