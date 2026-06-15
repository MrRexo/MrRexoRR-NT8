# Contributing

Thanks for your interest in contributing to MrRexoRR.

This project can submit orders, so changes must be reviewed with extra care. Small UI changes are welcome, but order handling, account selection, OCO logic, and position management changes need clear testing notes.

## Development Workflow

1. Fork the repository.
2. Create a feature branch.
3. Keep changes focused.
4. Test in NinjaTrader 8 on Playback or a non-live environment.
5. Open a pull request with:
   - what changed,
   - how it was tested,
   - screenshots or screen recording for UI changes,
   - any known risk or limitation.

## Coding Guidelines

- Keep NinjaScript changes compatible with NinjaTrader 8 Desktop.
- Avoid adding external runtime dependencies to the Drawing Tool.
- Keep order-submission defaults conservative.
- Never assume an account name is safe.
- Prefer explicit status messages over silent behavior.
- Keep UI text short enough for compact chart toolbars.

## Pull Requests That Need Extra Review

These areas require careful review and manual testing:

- order submission,
- OCO grouping,
- flatten/close behavior,
- break-even stop changes,
- account detection,
- quantity synchronization,
- partial fill handling,
- reconnect behavior.

## Reporting Bugs

When reporting a bug, include:

- NinjaTrader 8 version,
- instrument and expiry,
- connection type: live, delayed, Playback,
- account type used for testing,
- screenshots of the panel and Orders/Executions tabs,
- exact steps to reproduce.
