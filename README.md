# NinjaTrader Panel

Interaktywny panel Risk/Reward dla NinjaTrader 8 Desktop, wzorowany na panelu z MetaTradera.

## Aktualny zakres

Pierwszy etap buduje bezpieczne MVP:

- rdzeń kalkulacji PnL dla futures,
- obsługa ręcznej liczby kontraktów,
- obsługa wyliczania kontraktów z ryzyka w USD lub procentu salda,
- założenia UI dla jednego Entry, jednego obowiązkowego SL i jednego TP,
- szkielet NinjaScript custom Drawing Tool.

Składanie realnych zleceń market + SL/TP nie jest aktywne w MVP. Zostanie dodane po przetestowaniu panelu na Sim/Playback.

## Struktura

- `plan-panelu-ninjatrader.md` - wymagania, plan i decyzje.
- `src/MpRiskReward.Core` - testowalny rdzeń kalkulacji.
- `tests/MpRiskReward.Core.Tests` - proste testy bez zależności zewnętrznych.
- `NinjaScript/DrawingTools` - pliki przeznaczone do przeniesienia do NinjaTrader 8.
- `docs` - notatki techniczne i decyzje bezpieczeństwa.

## Uruchomienie testów rdzenia

```powershell
dotnet run --project .\tests\MpRiskReward.Core.Tests\MpRiskReward.Core.Tests.csproj
```

## Docelowa instalacja NinjaScript

Pliki z `NinjaScript/DrawingTools` będą przenoszone do:

```text
Documents\NinjaTrader 8\bin\Custom\DrawingTools
```

Następnie trzeba skompilować NinjaScript w NinjaTrader 8 przez NinjaScript Editor.

