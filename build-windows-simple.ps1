# Simple Windows Build Script for v2rayN
# This script builds the v2rayN application with Iranian sanctions bypass features

Write-Host "=== v2rayN Windows Build Script ===" -ForegroundColor Green

# Check if .NET SDK is installed
$dotnetVersion = dotnet --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Error "Error: .NET SDK is not installed. Please install .NET 8.0 SDK from https://dotnet.microsoft.com/download"
    exit 1
}

Write-Host "✓ .NET SDK version: $dotnetVersion" -ForegroundColor Green

# Create output directory
$OutputPath = ".\Release\windows-64"
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force
    Write-Host "✓ Created output directory: $OutputPath" -ForegroundColor Green
}

# Clean previous builds
Write-Host "🧹 Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean v2rayN/v2rayN.sln -c Release

# Restore packages
Write-Host "📦 Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore v2rayN/v2rayN.sln

if ($LASTEXITCODE -ne 0) {
    Write-Error "Error: Failed to restore packages"
    exit 1
}

Write-Host "✓ Packages restored successfully" -ForegroundColor Green

# Build the main v2rayN WPF application
Write-Host "🔨 Building v2rayN (WPF)..." -ForegroundColor Yellow
dotnet publish v2rayN/v2rayN/v2rayN.csproj -c Release -r win-x64 --self-contained=false -p:EnableWindowsTargeting=true -o $OutputPath

if ($LASTEXITCODE -ne 0) {
    Write-Error "Error: Failed to build v2rayN WPF application"
    exit 1
}

Write-Host "✓ v2rayN WPF application built successfully" -ForegroundColor Green

# Build AmazTool
Write-Host "🔨 Building AmazTool..." -ForegroundColor Yellow
dotnet publish v2rayN/AmazTool/AmazTool.csproj -c Release -r win-x64 --self-contained=false -p:EnableWindowsTargeting=true -o $OutputPath

if ($LASTEXITCODE -ne 0) {
    Write-Error "Error: Failed to build AmazTool"
    exit 1
}

Write-Host "✓ AmazTool built successfully" -ForegroundColor Green

# Copy configuration files
Write-Host "📋 Copying configuration files..." -ForegroundColor Yellow

$configDir = "$OutputPath\config"
if (-not (Test-Path $configDir)) {
    New-Item -ItemType Directory -Path $configDir -Force
}

$configFiles = @(
    "v2rayN\ServiceLib\Sample\custom_routing_transparent_mirrors",
    "v2rayN\ServiceLib\Sample\dns_transparent_mirrors_v2ray",
    "v2rayN\ServiceLib\Sample\setup_android_development.bat",
    "v2rayN\ServiceLib\Sample\README_ANDROID_IRAN_SANCTIONS.md"
)

foreach ($file in $configFiles) {
    if (Test-Path $file) {
        Copy-Item $file $configDir -Force
        Write-Host "  ✓ Copied $(Split-Path $file -Leaf)" -ForegroundColor Green
    } else {
        Write-Host "  ⚠ Skipped $(Split-Path $file -Leaf) (not found)" -ForegroundColor Yellow
    }
}

# Create launcher script
$launcherScript = @"
@echo off
echo Starting v2rayN with Iranian sanctions bypass...
echo.
if not exist "v2rayN.exe" (
    echo Error: v2rayN.exe not found
    pause
    exit /b 1
)
start "" "v2rayN.exe"
echo v2rayN started successfully!
pause
"@

$launcherScript | Out-File -FilePath "$OutputPath\Start v2rayN.bat" -Encoding UTF8

# Create README
$readme = @"
v2rayN Windows Build
===================

This is a custom build of v2rayN with Iranian sanctions bypass features.

Quick Start:
1. Run "Start v2rayN.bat" to launch the application
2. Apply the Iran preset (Menu → Regional Presets → Iran)
3. Load configuration files from the config/ directory

Built on: $(Get-Date)
"@

$readme | Out-File -FilePath "$OutputPath\README.txt" -Encoding UTF8

Write-Host ""
Write-Host "=== Build Complete! ===" -ForegroundColor Green
Write-Host ""
Write-Host "Output directory: $OutputPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "To run v2rayN:" -ForegroundColor White
Write-Host "1. Navigate to: $OutputPath" -ForegroundColor White
Write-Host "2. Run: Start v2rayN.bat" -ForegroundColor White
Write-Host ""
Write-Host "Files created:" -ForegroundColor White
Get-ChildItem $OutputPath | ForEach-Object {
    Write-Host "  - $($_.Name)" -ForegroundColor Gray
}
Write-Host ""
Write-Host "🎉 Enjoy your Iranian sanctions bypass enabled v2rayN!" -ForegroundColor Green

