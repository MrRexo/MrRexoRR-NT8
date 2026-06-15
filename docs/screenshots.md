# Screenshot Guide

Use this guide to prepare clean screenshots for the GitHub README.

## General Rules

- Use Playback or a non-live testing environment.
- Hide or crop account numbers, balances, broker identifiers, and personal information.
- Prefer a clean dark chart template.
- Use the same instrument across screenshots, preferably `MNQ`.
- Use English panel language for public screenshots.
- Avoid showing real PnL, funded account names, or proprietary workspace details.
- Save images as PNG.

Recommended folder:

```text
assets/screenshots
```

## Recommended Screenshot Set

### 1. Panel Overview

Filename:

```text
assets/screenshots/01-panel-overview.png
```

Scenario:

- Add `MrRexo Panel` to an MNQ chart.
- Use 1 contract.
- Show OP, SL, TP, R:R, QTY, risk and total profit labels.
- Keep Chart Trader hidden or cropped out.

Purpose:

Shows the main visual risk/reward panel.

### 2. Multi-TP Mode

Filename:

```text
assets/screenshots/02-multi-tp-mode.png
```

Scenario:

- Switch to `TP3`.
- Use 3 contracts.
- Show TP1, TP2, TP3 distributed evenly.
- Make sure total profit and risk labels are readable.

Purpose:

Shows scale-out planning and target distribution.

### 3. Order Toolbar

Filename:

```text
assets/screenshots/03-order-toolbar.png
```

Scenario:

- Focus on the fixed toolbar.
- Show colored `BUY`, `SELL`, `BE`, `CLOSE` buttons.
- Show `L/S`, `TP`, and quantity presets.
- Crop tightly enough that account details are not visible.

Purpose:

Shows execution controls without exposing account information.

### 4. Bracket Orders

Filename:

```text
assets/screenshots/04-bracket-orders.png
```

Scenario:

- Use Playback or non-live account.
- Submit one small test order.
- Capture the chart and/or Orders tab showing TP and SL OCO orders.
- Redact account or order IDs if visible.

Purpose:

Shows that bracket orders are created after entry fill.

### 5. BE and CLOSE

Filename:

```text
assets/screenshots/05-break-even-close.png
```

Scenario:

- Open a small Playback/non-live test position.
- Show BE buffer setting or status text after pressing `BE`.
- Show toolbar with `BE` and `CLOSE`.
- Do not show account number.

Purpose:

Shows position management controls.

## README Embedding

After adding screenshots, update `README.md` like this:

```md
![Panel overview](assets/screenshots/01-panel-overview.png)
![Multi-TP mode](assets/screenshots/02-multi-tp-mode.png)
![Order toolbar](assets/screenshots/03-order-toolbar.png)
```

Keep the README to 2-3 screenshots. Put the rest in documentation if needed.
