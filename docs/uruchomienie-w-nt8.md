# Uruchomienie prototypu w NinjaTrader 8

## Status obecny

Aktualny plik `MpRiskRewardPanel.cs` jest szkieletem custom Drawing Tool dla NinjaTrader 8. Nadaje się do pierwszej próby kompilacji i dopasowania do lokalnego API NT8, ale nie jest jeszcze pełnym panelem produkcyjnym.

Na tym etapie:

- rdzeń kalkulacji PnL jest gotowy i przetestowany poza NT8,
- plik NinjaScript jest przygotowany jako punkt startowy,
- moduł zleceń market/SL/TP nie jest jeszcze aktywny,
- testy tradingowe mają być wykonywane wyłącznie na koncie Sim/Playback.

## Krok 1 - skopiuj plik Drawing Tool

Skopiuj plik:

```text
E:\Multipro\Trading\NinjaTrader Panel\NinjaScript\DrawingTools\MpRiskRewardPanel.cs
```

do folderu NinjaTrader:

```text
Documents\NinjaTrader 8\bin\Custom\DrawingTools
```

Pełna ścieżka zwykle wygląda tak:

```text
C:\Users\<TwojUser>\Documents\NinjaTrader 8\bin\Custom\DrawingTools
```

## Krok 2 - skompiluj NinjaScript

1. Uruchom NinjaTrader 8.
2. Otwórz `New > NinjaScript Editor`.
3. W drzewku po prawej znajdź folder `DrawingTools`.
4. Otwórz `MpRiskRewardPanel.cs`.
5. Kliknij prawym przyciskiem w edytorze.
6. Wybierz `Compile`.

Jeśli pojawią się błędy kompilacji, skopiuj ich treść do projektu. Pierwsza kompilacja custom Drawing Tool często wymaga dopasowania sygnatur metod do konkretnej wersji NT8.

## Krok 3 - otwórz wykres testowy

1. Otwórz wykres `MNQ`.
2. Upewnij się, że konto ustawione w platformie to `Sim101` albo inne konto symulacyjne.
3. Nie testuj pierwszych wersji na koncie live.

## Krok 4 - dodaj narzędzie na wykres

Po poprawnej kompilacji narzędzie powinno być dostępne w menu narzędzi rysowania:

```text
Drawing Tools > MP Risk/Reward Panel
```

Następnie kliknij na wykresie w miejscu planowanego wejścia.

## Krok 5 - ustaw parametry

W oknie właściwości narzędzia ustaw:

- `Kontrakty`: np. 1, 2, 5, 10,
- `Kierunek`: Long albo Short,
- `Tick size`: dla MNQ zwykle 0.25,
- `Wartosc ticka`: dla MNQ 0.50.

## Oczekiwane ograniczenia prototypu

Obecny prototyp może wymagać korekt po pierwszej kompilacji w NT8. W następnej iteracji trzeba dodać:

- pełny render SharpDX,
- aktywne przeciąganie Entry/SL/TP,
- polskie etykiety na panelu,
- dokładne odwzorowanie kolorów ze screena,
- przyciski i logikę Sim order dopiero po stabilnym panelu.

