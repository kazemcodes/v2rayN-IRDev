@echo off
REM Windows Build Script for v2rayN (Batch Version)
REM This script builds the v2rayN application with Iranian sanctions bypass features

echo === v2rayN Windows Build Script ===
echo.

REM Check if .NET SDK is installed
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK is not installed.
    echo Please install .NET 8.0 SDK from https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo ✓ .NET SDK found

REM Create output directory
if not exist "Release\windows-64" (
    mkdir "Release\windows-64"
    echo ✓ Created output directory
)

REM Clean previous builds
echo 🧹 Cleaning previous builds...
dotnet clean v2rayN/v2rayN.sln -c Release

REM Restore packages
echo 📦 Restoring NuGet packages...
dotnet restore v2rayN/v2rayN.sln
if errorlevel 1 (
    echo ERROR: Failed to restore packages
    pause
    exit /b 1
)

echo ✓ Packages restored successfully

REM Build the main v2rayN WPF application
echo 🔨 Building v2rayN (WPF)...
dotnet publish v2rayN/v2rayN/v2rayN.csproj -c Release -r win-x64 --self-contained=false -p:EnableWindowsTargeting=true -o Release/windows-64
if errorlevel 1 (
    echo ERROR: Failed to build v2rayN WPF application
    pause
    exit /b 1
)

echo ✓ v2rayN WPF application built successfully

REM Build AmazTool
echo 🔨 Building AmazTool...
dotnet publish v2rayN/AmazTool/AmazTool.csproj -c Release -r win-x64 --self-contained=false -p:EnableWindowsTargeting=true -o Release/windows-64
if errorlevel 1 (
    echo ERROR: Failed to build AmazTool
    pause
    exit /b 1
)

echo ✓ AmazTool built successfully

REM Copy configuration files
echo 📋 Copying configuration files...
if not exist "Release\windows-64\config" (
    mkdir "Release\windows-64\config"
)

REM Copy config files if they exist
if exist "v2rayN\ServiceLib\Sample\custom_routing_transparent_mirrors" (
    copy "v2rayN\ServiceLib\Sample\custom_routing_transparent_mirrors" "Release\windows-64\config\" >nul
    echo   ✓ Copied transparent routing rules
)

if exist "v2rayN\ServiceLib\Sample\dns_transparent_mirrors_v2ray" (
    copy "v2rayN\ServiceLib\Sample\dns_transparent_mirrors_v2ray" "Release\windows-64\config\" >nul
    echo   ✓ Copied transparent DNS config
)

if exist "v2rayN\ServiceLib\Sample\setup_android_development.bat" (
    copy "v2rayN\ServiceLib\Sample\setup_android_development.bat" "Release\windows-64\config\" >nul
    echo   ✓ Copied setup script
)

if exist "v2rayN\ServiceLib\Sample\README_ANDROID_IRAN_SANCTIONS.md" (
    copy "v2rayN\ServiceLib\Sample\README_ANDROID_IRAN_SANCTIONS.md" "Release\windows-64\config\" >nul
    echo   ✓ Copied documentation
)

REM Create a simple launcher
echo @echo off> "Release\windows-64\Start v2rayN.bat"
echo REM v2rayN Launcher>> "Release\windows-64\Start v2rayN.bat"
echo echo Starting v2rayN with Iranian sanctions bypass...>> "Release\windows-64\Start v2rayN.bat"
echo echo.>> "Release\windows-64\Start v2rayN.bat"
echo if not exist "v2rayN.exe" (>> "Release\windows-64\Start v2rayN.bat"
echo     echo Error: v2rayN.exe not found>> "Release\windows-64\Start v2rayN.bat"
echo     pause>> "Release\windows-64\Start v2rayN.bat"
echo     exit /b 1>> "Release\windows-64\Start v2rayN.bat"
echo )>> "Release\windows-64\Start v2rayN.bat"
echo start "" "v2rayN.exe">> "Release\windows-64\Start v2rayN.bat"
echo echo v2rayN started successfully!>> "Release\windows-64\Start v2rayN.bat"
echo pause>> "Release\windows-64\Start v2rayN.bat"

REM Create README
echo v2rayN Windows Build> "Release\windows-64\README.txt"
echo ===================>> "Release\windows-64\README.txt"
echo.>> "Release\windows-64\README.txt"
echo This is a custom build of v2rayN with Iranian sanctions bypass features.>> "Release\windows-64\README.txt"
echo.>> "Release\windows-64\README.txt"
echo Quick Start:>> "Release\windows-64\README.txt"
echo 1. Run "Start v2rayN.bat">> "Release\windows-64\README.txt"
echo 2. Apply Iran preset (Menu → Regional Presets → Iran)>> "Release\windows-64\README.txt"
echo 3. Load configuration files from the config/ directory>> "Release\windows-64\README.txt"
echo.>> "Release\windows-64\README.txt"
echo Built on: %DATE% %TIME%>> "Release\windows-64\README.txt"

echo.
echo === Build Complete! ===
echo.
echo Output directory: Release\windows-64
echo.
echo To run v2rayN:
echo 1. Open Release\windows-64 folder
echo 2. Run "Start v2rayN.bat"
echo.
echo Files created:
dir /b "Release\windows-64"
echo.
echo 🎉 Enjoy your Iranian sanctions bypass enabled v2rayN!
pause
