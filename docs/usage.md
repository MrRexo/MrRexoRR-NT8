# Usage

## Add the Panel

1. Open a NinjaTrader 8 chart.
2. Enable Chart Trader if you want to use order submission.
3. Add `MrRexo Panel` from Drawing Tools.
4. Click on the chart to place the initial panel.

## Main Controls

- `BUY`: submit a long entry based on OP.
- `SELL`: submit a short entry based on OP.
- `BE`: move active protective stops to break-even with buffer.
- `CLOSE`: flatten the chart instrument.
- `L/S`: switch planned direction.
- `TP1/TP2/TP3`: switch target layout.
- `1/2/5/10/20`: quantity presets.
- `- / +`: decrease/increase quantity.
- `AUTO`: keep OP attached to current price.
- `OP=C`: reattach OP to current price.

## Dragging Levels

- Drag OP to move the whole panel.
- Drag SL to change risk.
- Drag the final TP level to change reward.
- In multi-TP mode, intermediate targets are distributed automatically.

## Shift Snap

Hold `Shift` while dragging OP, SL, or TP to snap prices.

The default snap step depends on the instrument. NQ and DAX families default to 20 points unless changed in the panel properties.

## Order Testing Checklist

Before pressing `BUY` or `SELL`:

- confirm Chart Trader account,
- confirm instrument and expiry,
- confirm quantity,
- confirm OP, SL, and TP prices,
- confirm market data is available,
- test on Playback or a non-live environment first.
