# Windows Build Script for v2rayN
# This script builds the v2rayN application with Iranian sanctions bypass features

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputPath = ".\Release\windows-64",
    [bool]$SelfContained = $false,
    [bool]$IncludeArm64 = $false
)

# Ensure we're in the correct directory
Set-Location $PSScriptRoot

Write-Host "=== v2rayN Windows Build Script ===" -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Cyan
Write-Host "Runtime: $Runtime" -ForegroundColor Cyan
Write-Host "Output Path: $OutputPath" -ForegroundColor Cyan
Write-Host "Self-Contained: $SelfContained" -ForegroundColor Cyan
Write-Host ""

# Check if .NET SDK is installed
$dotnetVersion = dotnet --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Error "Error: .NET SDK is not installed. Please install .NET 8.0 SDK from https://dotnet.microsoft.com/download"
    exit 1
}

Write-Host "✓ .NET SDK version: $dotnetVersion" -ForegroundColor Green

# Create output directory
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force
    Write-Host "✓ Created output directory: $OutputPath" -ForegroundColor Green
}

# Clean previous builds
Write-Host "🧹 Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean v2rayN/v2rayN.sln -c $Configuration

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
$publishArgs = @(
    "publish",
    "v2rayN/v2rayN/v2rayN.csproj",
    "-c", $Configuration,
    "-r", $Runtime,
    "-p:EnableWindowsTargeting=true",
    "-o", $OutputPath
)

if ($SelfContained) {
    $publishArgs += "--self-contained=true"
} else {
    $publishArgs += "--self-contained=false"
}

& dotnet $publishArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "Error: Failed to build v2rayN WPF application"
    exit 1
}

Write-Host "✓ v2rayN WPF application built successfully" -ForegroundColor Green

# Build AmazTool
Write-Host "🔨 Building AmazTool..." -ForegroundColor Yellow
$amazArgs = @(
    "publish",
    "v2rayN/AmazTool/AmazTool.csproj",
    "-c", $Configuration,
    "-r", $Runtime,
    "-p:EnableWindowsTargeting=true",
    "-o", $OutputPath
)

if ($SelfContained) {
    $amazArgs += "--self-contained=true"
} else {
    $amazArgs += "--self-contained=false"
}

& dotnet $amazArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "Error: Failed to build AmazTool"
    exit 1
}

Write-Host "✓ AmazTool built successfully" -ForegroundColor Green

# Build for ARM64 if requested
if ($IncludeArm64) {
    $OutputPathArm64 = $OutputPath -replace "windows-64", "windows-arm64"

    Write-Host "🔨 Building for ARM64..." -ForegroundColor Yellow

    # Build v2rayN for ARM64
    $armArgs = @(
        "publish",
        "v2rayN/v2rayN/v2rayN.csproj",
        "-c", $Configuration,
        "-r", "win-arm64",
        "-p:EnableWindowsTargeting=true",
        "-o", $OutputPathArm64
    )

    if ($SelfContained) {
        $armArgs += "--self-contained=true"
    } else {
        $armArgs += "--self-contained=false"
    }

    & dotnet $armArgs

    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Warning: Failed to build for ARM64, continuing with x64 only"
    } else {
        Write-Host "✓ ARM64 build completed" -ForegroundColor Green
    }
}

# Create a simple launcher script
$launcherScript = @"
@echo off
REM v2rayN Launcher
REM This script launches v2rayN with Iranian sanctions bypass features

echo Starting v2rayN with Iranian sanctions bypass...
echo.

REM Check if the executable exists
if not exist "v2rayN.exe" (
    echo Error: v2rayN.exe not found in current directory
    echo Please make sure you're running this from the v2rayN installation directory
    pause
    exit /b 1
)

REM Launch v2rayN
start "" "v2rayN.exe"

echo v2rayN started successfully!
echo.
echo Features enabled:
echo - Iranian sanctions bypass with automatic DNS switching
echo - Transparent repository mirroring
echo - 12 Iranian DNS servers for fallback
echo - Automatic 403 error detection and resolution
echo.
pause
"@

$launcherScript | Out-File -FilePath "$OutputPath\Start v2rayN.bat" -Encoding UTF8

# Create a README for the build
$readme = @"
v2rayN Windows Build
=====================

This is a custom build of v2rayN with Iranian sanctions bypass features.

Features:
- Automatic Iranian DNS switching for sanctions bypass
- Transparent repository mirroring (no app configuration needed)
- 12 Iranian DNS servers for fallback
- 403 error detection and automatic resolution
- GUI settings for monitoring and configuration

Quick Start:
1. Run "Start v2rayN.bat" to launch the application
2. Apply the Iran preset (Menu → Regional Presets → Iran)
3. Load transparent routing rules (custom_routing_transparent_mirrors)
4. Load transparent DNS config (dns_transparent_mirrors_v2ray)
5. Your Android Studio will now automatically use Iranian mirrors!

Configuration Files:
- custom_routing_transparent_mirrors - Transparent routing rules
- dns_transparent_mirrors_v2ray - DNS with Iranian mirrors
- setup_android_development.bat - Setup script for developers

For more information, see the documentation files in the ServiceLib/Sample/ directory.

Built on: $(Get-Date)
Configuration: $Configuration
Runtime: $Runtime
Self-Contained: $SelfContained
"@

$readme | Out-File -FilePath "$OutputPath\README.txt" -Encoding UTF8

# Copy configuration files for easy access
Write-Host "📋 Copying configuration files..." -ForegroundColor Yellow

# Create config directory
$configDir = "$OutputPath\config"
if (-not (Test-Path $configDir)) {
    New-Item -ItemType Directory -Path $configDir -Force
}

# Copy the Iranian development configuration files
$configFiles = @(
    "v2rayN\ServiceLib\Sample\custom_routing_transparent_mirrors",
    "v2rayN\ServiceLib\Sample\dns_transparent_mirrors_v2ray",
    "v2rayN\ServiceLib\Sample\dns_transparent_mirrors_singbox",
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

if ($IncludeArm64) {
    Write-Host ""
    Write-Host "ARM64 build: $($OutputPath -replace "windows-64", "windows-arm64")" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "🎉 Enjoy your Iranian sanctions bypass enabled v2rayN!" -ForegroundColor Green

