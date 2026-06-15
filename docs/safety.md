# Safety Model

MrRexoRR can submit orders. The safety model is intentionally visible and simple.

## Account Selection

By default, the panel attempts to use the account selected in Chart Trader on the current chart.

The `Account override` property can force a specific account name. If it is empty, Chart Trader detection is used.

Before trading, verify the active account in NinjaTrader and confirm it matches your intended test or live environment.

## Entry Orders

The panel determines entry order type from the OP level:

- OP at current price: Market
- Long OP below current price: Limit
- Long OP above current price: Stop Market
- Short OP above current price: Limit
- Short OP below current price: Stop Market

The panel checks for live market data before order submission.

## Bracket Orders

Protective orders are submitted only after entry fill.

For 1TP:

- one SL order,
- one TP order,
- one OCO pair.

For 2TP or 3TP:

- separate OCO pairs are created per target slice,
- each target has its own protective stop quantity,
- filling one target cancels only the matching stop slice.

## Duplicate Submission Guards

The panel includes:

- trade button click throttling,
- pending panel-entry detection,
- active position guard,
- active panel-order guard.

These guards reduce accidental duplicate entries, but they are not a substitute for user verification.

## BE

The `BE` button moves active protective stop orders for the current chart instrument to:

- Long: average entry plus `BE buffer ticks`
- Short: average entry minus `BE buffer ticks`

Default buffer is 1 tick.

## CLOSE

The `CLOSE` button calls NinjaTrader account flattening for the chart instrument. It is intended to close the current position and cancel active orders for that instrument.

Always verify the result in NinjaTrader's Orders, Executions, and Positions tabs.
