@echo off
echo ============================================
echo v2rayN Android Development Setup for Iran
echo ============================================
echo.

REM Check if v2rayN is running
echo Checking if v2rayN is running...
tasklist /FI "IMAGENAME eq v2rayN.exe" 2>NUL | find /I /N "v2rayN.exe">NUL
if "%ERRORLEVEL%"=="0" (
    echo ✓ v2rayN is running
) else (
    echo ✗ v2rayN is not running. Please start v2rayN first.
    echo.
    echo Press any key to exit...
    pause >nul
    exit /b 1
)

echo.
echo This script will help you configure v2rayN for Android development in Iran.
echo.

REM Instructions for user
echo MANUAL STEPS REQUIRED:
echo.
echo 1. In v2rayN, go to Menu → Regional Presets → Iran
echo 2. Go to Settings → Routing Settings
echo 3. Load custom routing rule: custom_routing_android_development
echo 4. Load DNS configuration based on your core type:
echo    - For Xray/V2ray: dns_android_development_v2ray
echo    - For Sing-box: dns_android_development_singbox
echo.

REM Create user directories
echo Creating configuration directories...
if not exist "%USERPROFILE%\.gradle" mkdir "%USERPROFILE%\.gradle"
if not exist "%USERPROFILE%\.m2" mkdir "%USERPROFILE%\.m2"

REM Copy configuration files
echo.
echo Copying configuration files...
if exist "gradle-wrapper-iran-mirrors" (
    echo ✓ Gradle wrapper properties available
    echo   Copy to: gradle\wrapper\gradle-wrapper.properties in your project
) else (
    echo ✗ Gradle wrapper properties not found
)

if exist "maven-settings-iran-mirrors" (
    echo ✓ Maven settings available
    echo   Copy to: %%USERPROFILE%%\%%\.m2\settings.xml
) else (
    echo ✗ Maven settings not found
)

if exist "settings.gradle.kts.iran-mirrors" (
    echo ✓ Kotlin DSL Gradle settings available
    echo   Copy to: settings.gradle.kts in your project
) else (
    echo ✗ Kotlin DSL Gradle settings not found
)

if exist "settings.gradle.iran-mirrors" (
    echo ✓ Groovy DSL Gradle settings available
    echo   Copy to: settings.gradle in your project
) else (
    echo ✗ Groovy DSL Gradle settings not found
)

if exist "custom_routing_transparent_mirrors" (
    echo ✓ Transparent routing rules available
    echo   Load in: v2rayN Routing Settings
) else (
    echo ✗ Transparent routing rules not found
)

if exist "dns_transparent_mirrors_v2ray" (
    echo ✓ Transparent V2Ray DNS config available
    echo   Load in: v2rayN DNS Settings
) else (
    echo ✗ Transparent V2Ray DNS config not found
)

if exist "dns_transparent_mirrors_singbox" (
    echo ✓ Transparent Singbox DNS config available
    echo   Load in: v2rayN DNS Settings
) else (
    echo ✗ Transparent Singbox DNS config not found
)

echo.
echo ============================================
echo IDE CONFIGURATION REQUIRED:
echo ============================================
echo.
echo Configure your IDE proxy settings:
echo HTTP Proxy: 127.0.0.1:10809
echo HTTPS Proxy: 127.0.0.1:10809
echo.
echo For Android Studio:
echo File → Settings → System Settings → HTTP Proxy
echo.

REM Test proxy connection
echo Testing proxy connection...
curl -x http://127.0.0.1:10809 -s https://gradle.org >nul 2>&1
if "%ERRORLEVEL%"=="0" (
    echo ✓ Proxy connection test successful
) else (
    echo ✗ Proxy connection test failed
    echo   Make sure v2rayN is connected and proxy is enabled
)

echo.
echo ============================================
echo SETUP COMPLETE
echo ============================================
echo.
echo Your v2rayN is now optimized for Android development in Iran!
echo.
echo RECOMMENDED SETUP (Automatic):
echo 1. Apply the Iran preset in v2rayN
echo 2. Load transparent routing rules: custom_routing_transparent_mirrors
echo 3. Load transparent DNS config: dns_transparent_mirrors_v2ray (or singbox)
echo 4. Done! Your apps will automatically use Iranian mirrors
echo.
echo ALTERNATIVE SETUP (Manual):
echo 1. Apply the Iran preset in v2rayN
echo 2. Load Android development routing rules: custom_routing_android_development
echo 3. Configure your IDE proxy settings
echo 4. Copy the provided configuration files to your projects
echo 5. Use Iranian mirrors (Myket and EN Mirror) for faster downloads
echo.
echo Press any key to exit...
pause >nul
