# 🔍 Iranian DNS Testing Script
# Tests all Iranian DNS servers for optimal performance

Write-Host "🚀 Starting Iranian DNS Server Testing..." -ForegroundColor Cyan

# Define Iranian DNS servers by tier
$DnsServers = @{
    # Tier 1: Most Reliable
    "electro-primary" = "78.157.42.100"
    "shecan-primary" = "178.22.122.100"
    "electro-secondary" = "78.157.42.101"
    "shecan-secondary" = "185.51.200.2"
    "radar-primary" = "10.202.10.10"
    "radar-secondary" = "10.202.10.11"
    
    # Tier 2: Reliable Alternatives
    "403-primary" = "10.202.10.202"
    "begzar-primary" = "185.55.226.26"
    "shelter-primary" = "94.103.125.157"
    "403-secondary" = "10.202.10.102"
    "begzar-secondary" = "185.55.225.25"
    "shelter-secondary" = "94.103.125.158"
    
    # Tier 3: Additional Options
    "asan-primary" = "185.143.233.120"
    "asan-secondary" = "185.143.234.120"
    "asan-dns" = "185.143.232.120"
    
    # Tier 4: High-Performance 2024
    "pishgaman-primary" = "5.202.100.100"
    "pishgaman-secondary" = "5.202.100.101"
    "tci-primary" = "192.168.100.100"
    "tci-secondary" = "192.168.100.101"
    "mokhaberat-primary" = "194.225.50.50"
    "mokhaberat-secondary" = "194.225.50.51"
    "parspack-primary" = "185.206.92.92"
    "parspack-secondary" = "185.206.93.93"
    
    # Tier 5: Mobile Operators
    "irancell-primary" = "78.39.35.66"
    "irancell-secondary" = "78.39.35.67"
    "hamrah-primary" = "217.218.127.127"
    "hamrah-secondary" = "217.218.155.155"
    "rightel-primary" = "78.157.42.101"
    "rightel-secondary" = "78.157.42.100"
    
    # Tier 6: Regional
    "tehran-dns1" = "185.143.232.100"
    "tehran-dns2" = "185.143.232.101"
    "mashhad-dns1" = "91.99.101.101"
    "mashhad-dns2" = "91.99.102.102"
    "isfahan-dns1" = "185.8.172.14"
    "isfahan-dns2" = "185.8.175.14"
    
    # Tier 7: High-Performance Global
    "samantel-primary" = "93.113.131.1"
    "samantel-secondary" = "93.113.131.2"
    "arvancloud-primary" = "178.22.122.100"
    "arvancloud-secondary" = "178.22.122.101"
    "cloudflare-iran" = "1.1.1.1"
    "cloudflare-family" = "1.1.1.3"
    "quad9-iran" = "9.9.9.9"
    "quad9-secure" = "9.9.9.10"
    "opendns-primary" = "208.67.222.222"
    "opendns-secondary" = "208.67.220.220"
    "comodo-primary" = "8.26.56.26"
    "comodo-secondary" = "8.20.247.20"
    
    # Tier 8: ISP-Specific
    "asiatech-primary" = "194.5.175.10"
    "asiatech-secondary" = "194.5.175.11"
    "shatel-primary" = "85.15.1.14"
    "shatel-secondary" = "85.15.1.15"
    "datak-primary" = "81.91.161.1"
    "datak-secondary" = "81.91.161.2"
    "fanava-primary" = "5.202.100.100"
    "fanava-secondary" = "5.202.100.101"
    "respina-primary" = "185.235.234.1"
    "respina-secondary" = "185.235.234.2"
}

# Test domains
$TestDomains = @(
    "developer.android.com",
    "maven.google.com", 
    "www.google.com"
)

$Results = @()

Write-Host "📊 Testing $($DnsServers.Count) DNS servers with $($TestDomains.Count) test domains..." -ForegroundColor Yellow

foreach ($DnsEntry in $DnsServers.GetEnumerator()) {
    $DnsName = $DnsEntry.Key
    $DnsIP = $DnsEntry.Value
    
    Write-Host "🔍 Testing $DnsName ($DnsIP)..." -ForegroundColor White
    
    $TotalTime = 0
    $SuccessCount = 0
    
    foreach ($Domain in $TestDomains) {
        try {
            $StartTime = Get-Date
            $Result = nslookup $Domain $DnsIP 2>$null
            $EndTime = Get-Date
            $Duration = ($EndTime - $StartTime).TotalMilliseconds
            
            if ($Result -match "Address:") {
                $SuccessCount++
                $TotalTime += $Duration
                Write-Host "  ✅ $Domain - $([math]::Round($Duration))ms" -ForegroundColor Green
            } else {
                Write-Host "  ❌ $Domain - FAILED" -ForegroundColor Red
            }
        }
        catch {
            Write-Host "  ⚠️ $Domain - ERROR: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
    
    $AvgTime = if ($SuccessCount -gt 0) { [math]::Round($TotalTime / $SuccessCount) } else { 999999 }
    $SuccessRate = [math]::Round(($SuccessCount / $TestDomains.Count) * 100)
    
    $Results += [PSCustomObject]@{
        Name = $DnsName
        IP = $DnsIP
        AvgResponseTime = $AvgTime
        SuccessRate = $SuccessRate
        SuccessCount = $SuccessCount
        Status = if ($SuccessCount -ge 2) { "✅ WORKING" } else { "❌ FAILED" }
    }
}

Write-Host "`n🎯 ===== DNS TEST RESULTS =====" -ForegroundColor Cyan
Write-Host "📊 Total DNS servers tested: $($DnsServers.Count)" -ForegroundColor White
$WorkingCount = ($Results | Where-Object { $_.SuccessCount -ge 2 }).Count
Write-Host "✅ Working DNS servers: $WorkingCount" -ForegroundColor Green
Write-Host "❌ Failed DNS servers: $($DnsServers.Count - $WorkingCount)" -ForegroundColor Red

Write-Host "`n🏆 TOP 5 RECOMMENDED DNS SERVERS:" -ForegroundColor Yellow
$TopDNS = $Results | Where-Object { $_.SuccessCount -ge 2 } | Sort-Object SuccessRate -Descending | Sort-Object AvgResponseTime | Select-Object -First 5

$Rank = 1
foreach ($DNS in $TopDNS) {
    $Color = switch ($Rank) {
        1 { "Green" }
        2 { "Yellow" }
        3 { "Cyan" }
        default { "White" }
    }
    
    Write-Host "   $Rank. $($DNS.Name) ($($DNS.IP))" -ForegroundColor $Color
    Write-Host "      ⚡ Response: $($DNS.AvgResponseTime)ms | ✅ Success: $($DNS.SuccessRate)% | Tests: $($DNS.SuccessCount)/$($TestDomains.Count)" -ForegroundColor White
    $Rank++
}

if ($TopDNS.Count -gt 0) {
    $BestDNS = $TopDNS[0]
    Write-Host "`n🎯 RECOMMENDED FOR v2rayN:" -ForegroundColor Green
    Write-Host "   DNS Name: $($BestDNS.Name)" -ForegroundColor White
    Write-Host "   DNS Address: $($BestDNS.IP)" -ForegroundColor White
    Write-Host "   Performance: $($BestDNS.AvgResponseTime)ms avg, $($BestDNS.SuccessRate)% success rate" -ForegroundColor White
    
    Write-Host "`n📋 TO APPLY THIS DNS:" -ForegroundColor Cyan
    Write-Host "   1. Open v2rayN → Settings → Sanctions Bypass Settings" -ForegroundColor White
    Write-Host "   2. Set 'Preferred Iranian DNS Server' to: $($BestDNS.Name)" -ForegroundColor White
    Write-Host "   3. Enable 'Enable Iranian DNS Auto-Switch'" -ForegroundColor White
    Write-Host "   4. Save and restart v2rayN" -ForegroundColor White
} else {
    Write-Host "`n❌ NO WORKING DNS SERVERS FOUND" -ForegroundColor Red
    Write-Host "⚠️ Please check your internet connection and try again." -ForegroundColor Yellow
}

Write-Host "`n📋 DETAILED RESULTS:" -ForegroundColor White
$Results | Sort-Object SuccessRate -Descending | Sort-Object AvgResponseTime | Format-Table -AutoSize

Write-Host "🔍 ===== END DNS TEST RESULTS =====" -ForegroundColor Cyan
Write-Host "💡 TIP: Run this script periodically to find the best DNS for your current network conditions." -ForegroundColor Yellow
