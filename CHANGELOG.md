# Changelog

All notable changes to this project will be documented in this file.

The format follows the spirit of Keep a Changelog, and this project uses simple date-based pre-release tracking until the first tagged release.

## Unreleased

### Added

- NinjaTrader 8 Drawing Tool: `MrRexoRR`.
- Interactive OP/SL/TP chart panel.
- 1TP, 2TP, and 3TP modes.
- Equal distribution of intermediate targets.
- Quantity controls and presets.
- Long/short switching.
- Shift snap for OP, SL, and TP.
- Futures instrument presets for common index, metals, and micro contracts.
- Optional order submission through NinjaTrader account API.
- Bracket protection after entry fill.
- Separate OCO pairs for multi-TP exits.
- BE action with configurable tick buffer.
- CLOSE action for the chart instrument.
- Chart Trader account detection with optional account override.
- PL/EN/DE label support.

### Changed

- Renamed the NinjaScript class and file to `MrRexoRR`.
- Default panel language is English.
- Default NQ/DAX snap step is 20 points.

### Safety

- Added duplicate-click protection for trade buttons.
- Added guards against submitting a new panel entry when position or panel orders are already active on the instrument.
- Added live market data preflight for order submission.
