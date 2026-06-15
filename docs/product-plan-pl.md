# Plan przygotowania panelu Risk/Reward dla NinjaTrader 8

## Cel

Zbudować narzędzie dla NinjaTrader 8 możliwie najbliższe panelowi z MetaTradera: interaktywny prostokąt pozycji na wykresie, z poziomem wejścia, stop lossem, take profitami, liczbą kontraktów i przeliczaniem PnL na żywo podczas przeciągania poziomów myszką.

Docelowy efekt:

- użytkownik wywołuje narzędzie na wykresie,
- klika/ustawia poziom wejścia,
- przeciąga SL i TP bezpośrednio na wykresie,
- panel natychmiast pokazuje zysk, stratę, punkty/ticki, R:R i wartości dla całej pozycji,
- wygląd jest zbliżony do obecnego panelu MetaTrader: zielona strefa profitu, czerwona strefa ryzyka, szary pasek wejścia, etykiety TP/SL/OP, kilka poziomów TP.

## Ustalone wymagania użytkownika

- Platforma docelowa: wyłącznie NinjaTrader 8 Desktop.
- Główny instrument: mikro NASDAQ, czyli przede wszystkim MNQ.
- Typowa wielkość pozycji: 1, 2, 5, 10 kontraktów oraz inne ręcznie wybrane wartości.
- Panel ma działać interaktywnie jak w MetaTraderze:
  - użytkownik ustawia poziomy myszką,
  - przesunięcie SL/TP natychmiast zmienia prognozowany zysk/stratę.
- Docelowo panel ma składać zlecenie market po świadomej decyzji użytkownika.
- Po wejściu market narzędzie ma ustawić wcześniej przygotowane Stop Loss i Take Profit.
- Testy modułu składania zleceń będą prowadzone wyłącznie na koncie symulacyjnym.
- Stop Loss jest wymagany zawsze.
- Take Profit: jeden poziom TP.
- Liczba kontraktów:
  - ręcznie: np. 1, 2, 5, 10,
  - opcjonalnie automatycznie z ryzyka w USD,
  - opcjonalnie automatycznie z procentu salda konta.
- Język interfejsu: polski.
- Wygląd: możliwie wierne odzwierciedlenie panelu z MetaTradera.

## Rekomendowana forma techniczna

Pierwszy wybór: NinjaTrader 8 Custom Drawing Tool w NinjaScript/C#.

Powody:

- naturalna obsługa interakcji myszką,
- możliwość przeciągania punktów/poziomów,
- własne renderowanie na wykresie przez SharpDX,
- zachowanie najbardziej podobne do narzędzia z MetaTradera,
- możliwość zapisu parametrów wraz z wykresem/szablonem.

Alternatywa: Indicator z parametrami.

Nadaje się do prostszego MVP, ale jest słabszy, jeśli głównym wymaganiem jest przeciąganie poziomów na wykresie tak jak w MetaTraderze.

## Zakres MVP

MVP powinno zawierać:

- tryb Long/Short,
- liczba kontraktów,
- poziom Entry,
- poziom Stop Loss,
- jeden poziom Take Profit,
- automatyczne obliczanie:
  - liczby ticków do SL,
  - liczby ticków do TP,
  - straty w USD,
  - zysku w USD,
  - R:R,
  - punktów/pipsów w zależności od instrumentu,
- wizualne strefy:
  - zielona profit zone,
  - czerwona risk zone,
  - szary pasek Entry/OP,
  - etykiety na poziomach,
- aktualizacja wartości w trakcie przeciągania.
- zabezpieczenie: brak możliwości wysłania zlecenia bez ustawionego SL.

## Zakres wersji docelowej

Po MVP warto dodać:

- szybkie przyciski L+/L- lub Qty+/Qty-,
- automatyczne wyliczanie wielkości pozycji z maksymalnego ryzyka, np. risk $250,
- automatyczne wyliczanie wielkości pozycji z procentu salda konta,
- presety instrumentów: MNQ, NQ, MES, ES, M2K, RTY, MCL, CL, MGC, GC,
- obsługa prowizji,
- ustawienia kolorów i fontów,
- zapamiętywanie ostatnich parametrów,
- opcjonalne tworzenie zleceń ATM/OCO po zatwierdzeniu.

Po obecnych ustaleniach wiele targetów nie jest wymagane w pierwszej wersji. Priorytetem jest jeden TP i jeden obowiązkowy SL.

## Moduł składania zleceń

Docelowy przepływ:

1. Użytkownik ustawia na wykresie Entry, SL i TP.
2. Panel pokazuje prognozowany risk/reward dla wybranej liczby kontraktów.
3. Użytkownik wybiera kierunek Long albo Short.
4. Użytkownik klika wyraźny przycisk potwierdzający wejście market.
5. Narzędzie wysyła zlecenie market.
6. Po wypełnieniu wejścia narzędzie ustawia powiązany SL i TP.

Wymagane zabezpieczenia:

- blokada wejścia bez SL,
- blokada wejścia bez TP, jeśli tryb wymaga TP,
- walidacja, czy SL jest po poprawnej stronie Entry dla Long/Short,
- walidacja, czy TP jest po poprawnej stronie Entry dla Long/Short,
- ostrzeżenie, jeśli wyliczone ryzyko przekracza limit ustawiony przez użytkownika,
- pierwszy etap testów wyłącznie na Sim/Playback,
- brak obsługi kont live w pierwszej wersji tradingowej.

Do rozstrzygnięcia technicznego:

- czy zlecenia realizować przez własną logikę NinjaScript, czy przez ATM Strategy,
- jak obsłużyć częściowe wypełnienia market order,
- czy SL/TP mają być OCO,
- czy modyfikacja poziomów po wejściu ma aktualizować aktywne zlecenia.

## Główne obliczenia

Podstawowy wzór:

```text
PnL = liczba_kontraktów * liczba_ticków * wartość_ticka
```

Liczba ticków:

```text
ticks = abs(priceA - priceB) / tickSize
```

Przykład MNQ:

```text
tickSize = 0.25
tickValue = 0.50 USD
20 ticków * 5 kontraktów * 0.50 USD = 50 USD
```

Przykład NQ:

```text
tickSize = 0.25
tickValue = 5.00 USD
20 ticków * 5 kontraktów * 5.00 USD = 500 USD
```

## Zachowanie interaktywne

Narzędzie powinno mieć co najmniej trzy uchwyty:

- Entry anchor,
- Stop Loss anchor,
- Take Profit anchor.

Przeciągnięcie dowolnego uchwytu powinno:

- przeliczyć wszystkie wartości,
- przerysować strefy,
- zachować kierunek Long/Short albo automatycznie go zmienić, jeśli TP/SL znajdą się po odwrotnych stronach Entry,
- zaokrąglić cenę do tick size instrumentu.

## Wygląd do odtworzenia z MetaTradera

Elementy widoczne na screenie:

- górny ciemnozielony pasek TP z tekstem:
  - mnożnik/poziom, np. `(M:3)`,
  - cena TP,
  - wartość w USD,
- jasna zielona strefa zysku,
- poziomy pośrednie L1/L2 z:
  - lot/quantity,
  - zysk w USD,
  - suma w USD,
- środkowy szary pasek:
  - `OP: cena wejścia`,
  - `P: wielkość pozycji`,
  - `[R:R x.xx]`,
  - przycisk/menu po prawej,
- jasnoczerwona strefa ryzyka,
- centralna wartość odległości/ryzyka, np. `5212 p.`,
- dolny czerwony pasek SL:
  - `SL: cena`,
  - strata w USD,
- małe przyciski boczne `L+` i `L-`.

## Etapy prac

1. Doprecyzowanie wymagań i makiety.
2. Przygotowanie szkieletu projektu NinjaScript.
3. MVP jako Drawing Tool:
   - Entry/SL/TP,
   - przeciąganie poziomów,
   - podstawowe PnL,
   - prosty render.
4. Odtworzenie wyglądu panelu MetaTrader.
5. Multi-target: TP1/TP2/TP3.
6. Presety instrumentów i walidacja tick value.
7. Testy na Market Replay / Sim.
8. Paczka importowalna do NinjaTrader 8.

## Pytania do wywiadu

### Platforma i workflow

1. Platforma: NinjaTrader 8 Desktop. Ustalone.
2. Panel ma docelowo wysyłać zlecenia market i ustawiać SL/TP. Ustalone.
3. Czy używasz Chart Trader, ATM Strategies, SuperDOM, czy głównie sam wykres?
4. Czy narzędzie ma działać na jednym wykresie, czy być globalne na kilku wykresach tego samego instrumentu?

### Instrumenty

5. Główny instrument: mikro NASDAQ/MNQ. Ustalone.
6. Czy handlujesz także pełnym NQ, czy wyłącznie MNQ?
7. Czy wartości ticka mają być pobierane automatycznie z NinjaTrader, czy chcesz mieć własną tabelę presetów?

### Wielkość pozycji

8. Ręczna liczba kontraktów plus opcjonalne wyliczanie z ryzyka USD/procentu salda. Ustalone.
9. Pozycja ma mieć jeden wspólny TP. Ustalone.
10. Czy panel ma pokazywać wynik brutto, czy po prowizjach?

### Take Profit / Stop Loss

11. Jeden TP. Ustalone.
12. SL zawsze jeden i obowiązkowy. Ustalone.
13. Czy poziomy TP mają być ustawiane ręcznie, czy automatycznie jako mnożniki ryzyka, np. 1R, 2R, 3R?
14. Czy panel ma obsługiwać trailing stop albo przesunięcie SL do BE?

### Interakcja

15. Jak chcesz wywoływać panel: z menu Drawing Tools, skrótem klawiszowym, przyciskiem na wykresie?
16. Czy poziom Entry ma być ustawiany kliknięciem, przeciąganiem, czy pobierany z aktualnej ceny Bid/Ask/Last?
17. Czy po przeciągnięciu TP poniżej Entry narzędzie ma automatycznie przełączyć się na Short?
18. Czy chcesz boczne przyciski jak na screenie, np. L+ i L-, do dodawania/usuwania poziomów?

### Wygląd

19. Wygląd możliwie blisko panelu MetaTrader. Ustalone.
20. Tekst po polsku. Ustalone.
21. Jakie skróty mają zostać: OP, SL, TP, R:R, P, L1/L2?
22. Czy panel ma być półprzezroczysty, żeby było widać świece pod spodem?

### Dokładność i walidacja

23. Czy wynik ma być pokazywany w USD, punktach, tickach, czy we wszystkich trzech formatach?
24. Czy zaokrąglamy ceny zawsze do tick size instrumentu?
25. Czy uwzględniamy spread/slippage w symulacji ryzyka?

## Decyzje wstępne

Na ten moment zakładamy:

- platforma: NinjaTrader 8 Desktop,
- typ dodatku: Custom Drawing Tool,
- język: NinjaScript/C#,
- pierwszy instrument testowy: MNQ,
- pierwsza wersja: jeden Entry, jeden obowiązkowy SL, jeden TP, jedna liczba kontraktów,
- późniejsza wersja: wejście market z automatycznym SL/TP, risk sizing z USD/procentu salda i dokładniejsze odwzorowanie panelu ze screena.
