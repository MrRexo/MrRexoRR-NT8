# Notatki implementacyjne

## Decyzja architektoniczna

Docelowym typem dodatku jest NinjaTrader 8 Custom Drawing Tool, ponieważ oficjalna dokumentacja NinjaTrader opisuje Drawing Tools jako mechanizm do własnego renderowania kształtów na wykresie oraz obsługi zdarzeń myszy takich jak `OnMouseDown`, `OnMouseMove` i `OnMouseUp`.

Źródła:

- https://ninjatrader.com/support/helpguides/nt8/drawing_tools.htm
- https://ninjatrader.com/support/helpguides/nt8/onmousedown.htm
- https://ninjatrader.com/support/helpguides/nt8/onrender.htm

## Faza 1

Faza 1 jest celowo bezpieczna:

- brak wysyłania realnych zleceń,
- ręczna liczba kontraktów,
- jeden Entry, jeden SL i jeden TP,
- obliczenia testowane poza NinjaTraderem.

## Faza 2

Faza 2 doda pełną interakcję w NinjaTrader:

- przeciągane uchwyty Entry/SL/TP,
- odświeżanie panelu podczas ruchu myszy,
- render stref profit/risk podobny do MetaTradera,
- polskie etykiety i skróty tradingowe.

## Faza 3

Faza 3 doda trading:

- przyciski `Kup Market` i `Sprzedaj Market`,
- blokadę wejścia bez SL,
- automatyczne ustawienie SL/TP po wejściu,
- testy tylko na Sim/Playback,
- brak obsługi kont live w pierwszej wersji modułu zleceń.

## Otwarte decyzje dla fazy zleceń

- ATM Strategy czy własne zarządzanie zleceniami NinjaScript.
- Czy przesuwanie SL/TP po wejściu ma modyfikować aktywne zlecenia.
- Czy zlecenia ochronne mają być tworzone jako OCO.
- Jak obsłużyć częściowe wypełnienie zlecenia market.
