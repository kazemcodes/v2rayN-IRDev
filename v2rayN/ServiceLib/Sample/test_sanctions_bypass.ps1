# Sanctions Bypass Test Script
# Run this script to test if your Iranian sanctions bypass is working

Write-Host "🧪 Testing Iranian Sanctions Bypass..." -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Yellow

# Test URLs
$testUrls = @(
    "https://maven.myket.ir/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.aar",
    "https://en-mirror.ir/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.aar",
    "https://maven.aliyun.com/repository/central/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.aar",
    "https://gradle.org/releases/",
    "https://developer.android.com/studio",
    "https://maven.google.com/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.aar"
)

$workingCount = 0
$iranianWorkingCount = 0

Write-Host "Testing Iranian mirrors..." -ForegroundColor White

foreach ($url in $testUrls) {
    try {
        $response = Invoke-WebRequest -Uri $url -Method Head -TimeoutSec 10
        if ($response.StatusCode -eq 200) {
            Write-Host "✅ $url" -ForegroundColor Green
            $workingCount++

            # Check if it's an Iranian mirror
            if ($url -like "*myket.ir*" -or $url -like "*en-mirror.ir*" -or $url -like "*aliyun*" -or $url -like "*huawei*") {
                $iranianWorkingCount++
            }
        } else {
            Write-Host "❌ $url (Status: $($response.StatusCode))" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "❌ $url ($($_.Exception.Message))" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "📊 Test Results:" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Yellow
Write-Host "Total URLs tested: $($testUrls.Length)"
Write-Host "Working URLs: $workingCount"
Write-Host "Iranian mirrors working: $iranianWorkingCount"

if ($iranianWorkingCount -ge 2) {
    Write-Host ""
    Write-Host "🎉 SUCCESS: Iranian sanctions bypass is working!" -ForegroundColor Green
    Write-Host "Your VPN connection should work for Android development." -ForegroundColor Green
} elseif ($workingCount -ge 3) {
    Write-Host ""
    Write-Host "⚠️ PARTIAL: Some mirrors working, others blocked" -ForegroundColor Yellow
    Write-Host "Sanctions may still be active. Check your VPN connection." -ForegroundColor Yellow
} else {
    Write-Host ""
    Write-Host "❌ FAILED: Sanctions bypass not working" -ForegroundColor Red
    Write-Host "Check your VPN connection and Iranian DNS settings." -ForegroundColor Red
}

Write-Host ""
Write-Host "🔧 Troubleshooting:" -ForegroundColor Cyan
Write-Host "1. Make sure v2rayN is running and connected" -ForegroundColor White
Write-Host "2. Apply Iran regional preset in v2rayN" -ForegroundColor White
Write-Host "3. Load the sanctions bypass configuration files" -ForegroundColor White
Write-Host "4. Check the Sanctions Bypass Settings window" -ForegroundColor White
Write-Host "5. Try different Iranian DNS servers" -ForegroundColor White

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

