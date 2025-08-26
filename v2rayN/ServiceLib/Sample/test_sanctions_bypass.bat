@echo off
REM Sanctions Bypass Test Script (Batch Version)
REM Run this script to test if your Iranian sanctions bypass is working

echo 🧪 Testing Iranian Sanctions Bypass...
echo ==========================================

echo.
echo Testing Iranian mirrors...
echo.

REM Test Iranian mirrors
set "iranian_urls[0]=https://maven.myket.ir/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.aar"
set "iranian_urls[1]=https://en-mirror.ir/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.aar"
set "iranian_urls[2]=https://maven.aliyun.com/repository/central/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.aar"

set total_count=3
set working_count=0
set iranian_working=0

REM Test Myket
echo Testing Myket mirror...
curl -s -o nul -w "%%{http_code}" "%iranian_urls[0]%" | findstr "200" >nul
if %errorlevel% equ 0 (
    echo ✅ Myket mirror is working
    set /a working_count+=1
    set /a iranian_working+=1
) else (
    echo ❌ Myket mirror failed
)

REM Test EN Mirror
echo Testing EN Mirror...
curl -s -o nul -w "%%{http_code}" "%iranian_urls[1]%" | findstr "200" >nul
if %errorlevel% equ 0 (
    echo ✅ EN Mirror is working
    set /a working_count+=1
    set /a iranian_working+=1
) else (
    echo ❌ EN Mirror failed
)

REM Test Alibaba
echo Testing Alibaba Cloud mirror...
curl -s -o nul -w "%%{http_code}" "%iranian_urls[2]%" | findstr "200" >nul
if %errorlevel% equ 0 (
    echo ✅ Alibaba Cloud mirror is working
    set /a working_count+=1
    set /a iranian_working+=1
) else (
    echo ❌ Alibaba Cloud mirror failed
)

echo.
echo 📊 Test Results:
echo ==========================================
echo Total mirrors tested: %total_count%
echo Working mirrors: %working_count%
echo Iranian mirrors working: %iranian_working%

if %iranian_working% geq 2 (
    echo.
    echo 🎉 SUCCESS: Iranian sanctions bypass is working!
    echo Your VPN connection should work for Android development.
) else if %working_count% geq 1 (
    echo.
    echo ⚠️ PARTIAL: Some mirrors working, others blocked
    echo Sanctions may still be active. Check your VPN connection.
) else (
    echo.
    echo ❌ FAILED: Sanctions bypass not working
    echo Check your VPN connection and Iranian DNS settings.
)

echo.
echo 🔧 Troubleshooting:
echo 1. Make sure v2rayN is running and connected
echo 2. Apply Iran regional preset in v2rayN
echo 3. Load the sanctions bypass configuration files
echo 4. Check the Sanctions Bypass Settings window
echo 5. Try different Iranian DNS servers

echo.
echo Press any key to exit...
pause >nul

