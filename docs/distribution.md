# Distribution

MrRexoRR Free can be distributed in two ways.

## Source Package

The source package is the default community-friendly option.

Users copy:

```text
NinjaScript/DrawingTools/MrRexoRR.cs
```

to:

```text
Documents\NinjaTrader 8\bin\Custom\DrawingTools
```

Then they compile NinjaScript inside NinjaTrader 8.

## Compiled Package

The official NinjaTrader workflow for a compiled assembly is:

```text
Tools > Export > NinjaScript > Export as compiled assembly
```

This creates a NinjaScript archive ZIP that can be imported on another NinjaTrader 8 installation.

For local release preparation, this repository also includes:

```powershell
.\scripts\build-free-package.ps1
```

The script creates a local ZIP in:

```text
dist/
```

That package contains a compiled `MrRexoRR.Free.dll` plus manual installation notes.

Important: do not install the source file and compiled DLL at the same time on the same NinjaTrader installation. That can create duplicate type conflicts.

Before attaching a compiled package to a public GitHub Release, test it on a clean NinjaTrader 8 installation or a separate VM.
