# Następne kroki

## Iteracja 1 - gotowe

- Utworzono strukturę projektu.
- Dodano testowalny rdzeń kalkulacji futures.
- Dodano presety MNQ, NQ, MES, ES.
- Dodano wyliczanie ilości kontraktów z ryzyka USD i procentu salda.
- Dodano pierwszy szkielet `MpRiskRewardPanel.cs` dla NinjaTrader 8 Drawing Tools.

## Iteracja 2 - render panelu w NinjaTrader

1. Przenieść `NinjaScript/DrawingTools/MpRiskRewardPanel.cs` do `Documents\NinjaTrader 8\bin\Custom\DrawingTools`.
2. Skompilować w NinjaScript Editor.
3. Poprawić sygnatury API, jeśli lokalna wersja NT8 wymaga korekt.
4. Dodać SharpDX render:
   - zielona strefa TP,
   - czerwona strefa SL,
   - szary pasek Entry,
   - teksty po polsku.
5. Dodać aktywne przeciąganie Entry/SL/TP.

## Iteracja 3 - ergonomia

1. Dodać parametry:
   - tryb Long/Short,
   - kontrakty,
   - risk USD,
   - risk procent salda.
2. Dodać szybkie zmiany ilości:
   - 1,
   - 2,
   - 5,
   - 10,
   - plus/minus.
3. Dodać walidacje wizualne:
   - brak SL,
   - SL po złej stronie,
   - TP po złej stronie,
   - ryzyko przekracza limit.

## Iteracja 4 - składanie zleceń

1. Najpierw wyłącznie konto Sim.
2. Dodać przyciski:
   - `Kup Market`,
   - `Sprzedaj Market`,
   - `Anuluj`.
3. Po wypełnieniu market order ustawić SL/TP.
4. Użyć OCO lub ATM, po decyzji technicznej w NinjaTraderze.
5. Zablokować albo ukryć obsługę kont live w pierwszej wersji tradingowej.
