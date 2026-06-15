# Installation

MrRexoRR is distributed as a NinjaTrader 8 NinjaScript Drawing Tool.

## Manual Source Installation

1. Close or minimize NinjaTrader 8.
2. Copy:

   ```text
   NinjaScript\DrawingTools\MrRexoRR.cs
   ```

   to:

   ```text
   Documents\NinjaTrader 8\bin\Custom\DrawingTools
   ```

3. Open NinjaTrader 8.
4. Open `New > NinjaScript Editor`.
5. In the NinjaScript Editor, right-click and choose `Compile`.
6. Open a chart.
7. Add the tool from the chart Drawing Tools menu.

The visible tool name is:

```text
MrRexo Panel
```

## Updating

When updating from source:

1. Replace the existing `MrRexoRR.cs` file in the NinjaTrader DrawingTools folder.
2. Compile NinjaScript again.
3. Remove old panel instances from open charts.
4. Add a fresh panel instance.

NinjaTrader may keep old serialized Drawing Tool instances inside a workspace, so adding a new instance is recommended after code updates.

## Troubleshooting

### The tool does not appear in the menu

- Confirm `MrRexoRR.cs` is in `Documents\NinjaTrader 8\bin\Custom\DrawingTools`.
- Compile NinjaScript.
- Restart NinjaTrader if needed.

### Compile errors

Open an issue with:

- NinjaTrader version,
- the exact compiler error,
- line number,
- screenshot of the NinjaScript Editor error list.

### Orders are rejected

Common causes:

- market is closed,
- no live/delayed/Playback market data,
- wrong instrument expiry,
- Chart Trader account is not available,
- broker or prop account restrictions.

Always test on Playback or a non-live account first.
