# MrRexo Hacker - NinjaTrader 8 chart template

Utworzony plik:

```text
C:\Users\rexob\Documents\NinjaTrader 8\templates\Chart\MrRexo Hacker.xml
```

Zmieniony został też template serii minutowej:

```text
C:\Users\rexob\Documents\NinjaTrader 8\templates\ChartSeries\Minute.xml
```

Backup poprzedniej wersji:

```text
C:\Users\rexob\Documents\NinjaTrader 8\templates\ChartSeries\Minute.xml.bak-MrRexo-20260614-182140
```

## Co zmienia

- ciemne tło wykresu,
- niebiesko-cyjanowy tekst i akcenty,
- wyłączony grid poziomy i pionowy,
- monospaced font `Cascadia Mono`,
- świeczki:
  - wzrostowe: białe,
  - spadkowe: czarne,
- wstęgi:
  - szybka `WMA(33)`: czerwona, półprzezroczysta linia,
  - wolna `WMA(144)`: niebieska, półprzezroczysta linia,
- linie zleceń/pozycji w Chart Trader:
  - target: green,
  - stop: red,
  - entry: amber,
  - limit: cyan,
- kolory PnL dopasowane do ciemnego stylu.

Template bazuje na istniejącym `MrRexo.xml`, więc zachowuje Twoje aktualne wskaźniki i układ.

Uwaga: samo wypełnienie tła pomiędzy dwiema liniami WMA wymaga osobnego wskaźnika typu region/fill. Template zmienia kolory i przezroczystość istniejących linii WMA, ale nie tworzy nowego wypełnienia między nimi, jeśli takiego wskaźnika nie było w bazowym template.

## Jak włączyć

1. Otwórz wykres w NinjaTrader 8.
2. Kliknij prawym na wykresie.
3. Wybierz `Templates > Load`.
4. Wybierz `MrRexo Hacker`.

Jeśli template nie pojawi się od razu na liście, zrestartuj NinjaTrader 8.
