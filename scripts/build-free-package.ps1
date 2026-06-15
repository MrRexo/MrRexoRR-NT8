param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$sourceFile = Join-Path $repoRoot "NinjaScript\DrawingTools\MrRexoRR.cs"
$distDir = Join-Path $repoRoot "dist"
$buildRoot = Join-Path $repoRoot "tmp\build-free"
$ntBin = "C:\Program Files\NinjaTrader 8\bin"
$ntCustom = Join-Path $env:USERPROFILE "Documents\NinjaTrader 8\bin\Custom"

if (-not (Test-Path -LiteralPath $sourceFile)) {
    throw "Missing source file: $sourceFile"
}

if (-not (Test-Path -LiteralPath (Join-Path $ntBin "NinjaTrader.Core.dll"))) {
    throw "NinjaTrader 8 binaries were not found at: $ntBin"
}

Remove-Item -LiteralPath $buildRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $buildRoot | Out-Null
New-Item -ItemType Directory -Path $distDir -Force | Out-Null

$buildSource = Join-Path $buildRoot "MrRexoRR.cs"
Copy-Item -LiteralPath $sourceFile -Destination $buildSource -Force

$projectFile = Join-Path $buildRoot "MrRexoRR.Free.csproj"
@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <OutputType>Library</OutputType>
    <AssemblyName>MrRexoRR.Free</AssemblyName>
    <RootNamespace>NinjaTrader.NinjaScript.DrawingTools</RootNamespace>
    <GenerateAssemblyInfo>true</GenerateAssemblyInfo>
    <LangVersion>13.0</LangVersion>
    <UseWPF>true</UseWPF>
    <PlatformTarget>x64</PlatformTarget>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <OutputPath>bin\$Configuration\</OutputPath>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="NinjaTrader.Core"><HintPath>$ntBin\NinjaTrader.Core.dll</HintPath></Reference>
    <Reference Include="NinjaTrader.Gui"><HintPath>$ntBin\NinjaTrader.Gui.dll</HintPath></Reference>
    <Reference Include="SharpDX"><HintPath>$ntBin\SharpDX.dll</HintPath></Reference>
    <Reference Include="SharpDX.Direct2D1"><HintPath>$ntBin\SharpDX.Direct2D1.dll</HintPath></Reference>
    <Reference Include="NinjaTrader.Vendor"><HintPath>$ntCustom\NinjaTrader.Vendor.dll</HintPath></Reference>
    <Reference Include="System.ComponentModel.DataAnnotations" />
    <Reference Include="System.Windows.Forms" />
    <Reference Include="WindowsBase" />
    <Reference Include="PresentationCore" />
    <Reference Include="PresentationFramework" />
    <Reference Include="UIAutomationProvider" />
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath $projectFile -Encoding UTF8

dotnet build $projectFile -c $Configuration -v minimal

$commit = "local"
try {
    $commit = (git -C $repoRoot rev-parse --short HEAD).Trim()
} catch {
    $commit = "local"
}

$packageRoot = Join-Path $buildRoot "package"
New-Item -ItemType Directory -Path $packageRoot | Out-Null

$dllPath = Join-Path $buildRoot "bin\$Configuration\MrRexoRR.Free.dll"
if (-not (Test-Path -LiteralPath $dllPath)) {
    throw "Compiled DLL was not created: $dllPath"
}

Copy-Item -LiteralPath $dllPath -Destination (Join-Path $packageRoot "MrRexoRR.Free.dll") -Force

@"
MrRexoRR Free - compiled package
================================

This package contains a compiled DLL build of the MrRexoRR Free drawing tool.

Recommended install path:
  Documents\NinjaTrader 8\bin\Custom

Important:
- Do not install the source file and this compiled DLL at the same time on the same NinjaTrader installation.
- Close NinjaTrader before copying the DLL.
- Start NinjaTrader again and check Drawing Tools for "MrRexoRR Free".
- If NinjaTrader reports duplicate type errors, remove either the source file or the DLL and compile again.

The official NinjaTrader distribution path for end users is:
  Tools > Export > NinjaScript > Export as compiled assembly

This generated package is a local build artifact and should be tested on a clean NinjaTrader 8 installation before public release.
"@ | Set-Content -LiteralPath (Join-Path $packageRoot "INSTALL-COMPILED.txt") -Encoding UTF8

$zipPath = Join-Path $distDir "MrRexoRR-NT8-Free-compiled-$commit.zip"
Remove-Item -LiteralPath $zipPath -Force -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $packageRoot "*") -DestinationPath $zipPath

Write-Host "Created: $zipPath"
