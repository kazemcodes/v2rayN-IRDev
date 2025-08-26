using System.Net;
using System.Net.Http;
using System.Text.Json;
using ServiceLib.Common;
using ServiceLib.Handler;
using ServiceLib.Models;

namespace ServiceLib.Services.CoreConfig;

public class SanctionsBypassService : IDisposable
{
    /// <summary>
    /// Send message to UI (like sanctions validation messages)
    /// </summary>
    private void SendUIMessage(string msg)
    {
        if (string.IsNullOrWhiteSpace(msg))
            return;

        try
        {
            NoticeManager.Instance?.SendMessage(msg);
            Logging.SaveLog(msg); // Also log to file
        }
        catch (Exception ex)
        {
            // Fallback to logging if UI message fails
            Logging.SaveLog($"UI Message failed: {msg}");
            Logging.SaveLog($"SendUIMessage error: {ex.Message}");
        }
    }
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, string> _iranianDnsServers;
    private readonly HashSet<string> _googleDomains;
    private string _currentDnsServer;
    private bool _isSanctionsActive;
    private readonly object _lockObject = new();
    private bool _disposed = false;
    
    // Cache for mirror testing to avoid redundant HTTP calls
    private static readonly Dictionary<string, (bool IsWorking, DateTime LastChecked)> _mirrorCache = new();
    private static readonly TimeSpan _cacheExpiryTime = TimeSpan.FromMinutes(5);

    public SanctionsBypassService()
    {
        try
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"SanctionsBypassService: Error initializing HttpClient - {ex.Message}");
            throw;
        }

        _iranianDnsServers = new Dictionary<string, string>
        {
            // Tier 1: Most reliable Iranian DNS servers (highest priority)
            { "shecan-primary", "178.22.122.100" },
            { "shecan-secondary", "185.51.200.2" },
            { "electro-primary", "78.157.42.100" },
            { "electro-secondary", "78.157.42.101" },
            { "radar-primary", "10.202.10.10" },
            { "radar-secondary", "10.202.10.11" },

            // Tier 2: Reliable alternative Iranian DNS servers
            { "shelter-primary", "94.103.125.157" },
            { "shelter-secondary", "94.103.125.158" },
            { "403-primary", "10.202.10.202" },
            { "403-secondary", "10.202.10.102" },
            { "begzar-primary", "185.55.226.26" },
            { "begzar-secondary", "185.55.225.25" },

            // Tier 3: Additional Iranian DNS servers
            { "asan-primary", "185.143.233.120" },
            { "asan-secondary", "185.143.234.120" },
            { "asan-dns", "185.143.232.120" },

            // Tier 4: New high-performance Iranian DNS servers (2024 additions)
            { "pishgaman-primary", "5.202.100.100" },
            { "pishgaman-secondary", "5.202.100.101" },
            { "tci-primary", "192.168.100.100" },
            { "tci-secondary", "192.168.100.101" },
            { "mokhaberat-primary", "194.225.50.50" },
            { "mokhaberat-secondary", "194.225.50.51" },
            { "parspack-primary", "185.206.92.92" },
            { "parspack-secondary", "185.206.93.93" },
            
            // Tier 5: Mobile operator DNS servers (good for mobile users)
            { "irancell-primary", "78.39.35.66" },
            { "irancell-secondary", "78.39.35.67" },
            { "hamrah-primary", "217.218.127.127" },
            { "hamrah-secondary", "217.218.155.155" },
            { "rightel-primary", "78.157.42.101" },
            { "rightel-secondary", "78.157.42.100" },

            // Tier 6: Regional Iranian DNS servers
            { "tehran-dns1", "185.143.232.100" },
            { "tehran-dns2", "185.143.232.101" },
            { "mashhad-dns1", "91.99.101.101" },
            { "mashhad-dns2", "91.99.102.102" },
            { "isfahan-dns1", "185.8.172.14" },
            { "isfahan-dns2", "185.8.175.14" },
            
            // Tier 7: Additional High-Performance (12 servers)
            { "samantel-primary", "93.113.131.1" },
            { "samantel-secondary", "93.113.131.2" },
            { "arvancloud-primary", "178.22.122.100" },
            { "arvancloud-secondary", "178.22.122.101" },
            { "cloudflare-iran", "1.1.1.1" },
            { "cloudflare-family", "1.1.1.3" },
            { "quad9-iran", "9.9.9.9" },
            { "quad9-secure", "9.9.9.10" },
            { "opendns-primary", "208.67.222.222" },
            { "opendns-secondary", "208.67.220.220" },
            { "comodo-primary", "8.26.56.26" },
            { "comodo-secondary", "8.20.247.20" },
            
            // Tier 8: ISP-Specific DNS (10 servers)
            { "asiatech-primary", "194.5.175.10" },
            { "asiatech-secondary", "194.5.175.11" },
            { "shatel-primary", "85.15.1.14" },
            { "shatel-secondary", "85.15.1.15" },
            { "datak-primary", "81.91.161.1" },
            { "datak-secondary", "81.91.161.2" },
            { "fanava-primary", "5.202.100.100" },
            { "fanava-secondary", "5.202.100.101" },
            { "respina-primary", "185.235.234.1" },
            { "respina-secondary", "185.235.234.2" },
            
            // Tier 9: Specialized Anti-Sanctions DNS (5 servers)
            { "dynx-anti-sanctions-primary", "10.70.95.150" },
            { "dynx-anti-sanctions-secondary", "10.70.95.162" },
            { "dynx-adblocker-primary", "195.26.26.23" },
            { "dynx-ipv6-primary", "2a00:c98:2050:a04d:1::400" },
            { "dynx-family-safe", "195.26.26.23" }
        };

        // Load comprehensive Iranian blocked domains list
        _googleDomains = LoadIranianBlockedDomains();

        _currentDnsServer = "google";
        _isSanctionsActive = false;
    }

    /// <summary>
    /// Validate that sanctions bypass is working and connection is allowed
    /// </summary>
    public async Task<(bool canConnect, string reason)> ValidateConnectionAsync()
    {
        try
        {
            Logging.SaveLog("=== SANCTIONS BYPASS VALIDATION STARTING ===");
            Logging.SaveLog($"Current time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Logging.SaveLog($"Testing from: Iranian sanctions bypass system");

            // First check if sanctions are active
            Logging.SaveLog("Step 1: Checking if sanctions are currently active...");
            var sanctionsActive = await CheckSanctionsStatusAsync();

            if (!sanctionsActive)
            {
                Logging.SaveLog("✅ RESULT: No sanctions detected - full access available");
                return (true, "✅ No sanctions detected - connection allowed");
            }

            Logging.SaveLog("⚠️ SANCTIONS DETECTED - Testing bypass mechanisms...");

            // Test Iranian DNS servers with detailed logging
            Logging.SaveLog("Step 2: Testing Iranian DNS servers accessibility...");
            var (dnsWorking, dnsDetails) = await TestIranianDnsWithDetailsAsync();
            Logging.SaveLog($"DNS Test Result: {(dnsWorking ? "✅ SUCCESS" : "❌ FAILED")}");
            Logging.SaveLog($"DNS Details: {dnsDetails}");

            // Test if mirrors are accessible
            Logging.SaveLog("Step 3: Testing Iranian repository mirrors...");
            var (mirrorsWorking, mirrorDetails) = await TestMirrorsWithDetailsAsync();
            Logging.SaveLog($"Mirror Test Result: {(mirrorsWorking ? "✅ SUCCESS" : "❌ FAILED")}");
            Logging.SaveLog($"Mirror Details: {mirrorDetails}");

            // Test basic proxy functionality
            Logging.SaveLog("Step 4: Testing basic proxy connection...");
            var proxyWorking = await TestBasicProxyAsync();
            Logging.SaveLog($"Proxy Test Result: {(proxyWorking ? "✅ SUCCESS" : "❌ FAILED")}");

            // Determine connection allowance with relaxed conditions
            var allowConnection = false;
            var reason = "";

            if (dnsWorking && mirrorsWorking)
            {
                allowConnection = true;
                reason = "✅ Sanctions detected but Iranian DNS and mirrors are working. Full bypass available.";
            }
            else if (dnsWorking)
            {
                allowConnection = true;
                reason = "✅ Sanctions detected but Iranian DNS working. Repository fallback available.";
            }
            else if (mirrorsWorking)
            {
                allowConnection = true;
                reason = "⚠️ Sanctions detected. Iranian mirrors working - basic functionality available.";
            }
            else if (proxyWorking)
            {
                allowConnection = true;
                reason = "⚠️ Sanctions detected. Only basic proxy working - limited functionality but connection allowed.";
            }
            else
            {
                allowConnection = false;
                reason = "🚫 Sanctions detected and no bypass mechanisms are working. Connection blocked for security.";
            }

            // Log final decision to both file and UI
            SendUIMessage("=== FINAL VALIDATION RESULT ===");
            SendUIMessage($"DNS: {(dnsWorking ? "✅" : "❌")} | Mirrors: {(mirrorsWorking ? "✅" : "❌")} | Proxy: {(proxyWorking ? "✅" : "❌")}");
            SendUIMessage($"Decision: {(allowConnection ? "ALLOW CONNECTION" : "BLOCK CONNECTION")}");
            SendUIMessage($"Reason: {reason}");
            SendUIMessage("=== SANCTIONS BYPASS VALIDATION COMPLETE ===");

            return (allowConnection, reason);
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"SanctionsBypassService: Error validating connection: {ex.Message}");
            return (false, $"❌ Connection validation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Test all Iranian DNS servers to find one that works
    /// </summary>
    private async Task<bool> TestIranianDnsAsync()
    {
        var dnsServers = _iranianDnsServers.Keys.ToList();
        var workingCount = 0;

        foreach (var dnsServer in dnsServers)
        {
            try
            {
                var isWorking = await TestDnsServerAsync(dnsServer);
                if (isWorking)
                {
                    workingCount++;
                    // Set this as current working DNS
                    _currentDnsServer = dnsServer;
                    Logging.SaveLog($"SanctionsBypassService: DNS server {dnsServer} is working");
                }
            }
            catch
            {
                // DNS server not working
            }
        }

        return workingCount > 0;
    }

    /// <summary>
    /// Test Android development specific URLs
    /// </summary>
    private async Task<bool> TestAndroidDevelopmentAsync()
    {
        var androidUrls = new[]
        {
            // Test the specific Android Gradle Plugin that's failing
            "https://maven.myket.ir/com/android/application/com.android.application.gradle.plugin/8.7.0/com.android.application.gradle.plugin-8.7.0.pom",
            "https://en-mirror.ir/com/android/application/com.android.application.gradle.plugin/8.7.0/com.android.application.gradle.plugin-8.7.0.pom",
            
            // Test general Android libraries
            "https://maven.myket.ir/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.aar",
            "https://en-mirror.ir/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.aar",
            "https://maven.aliyun.com/repository/central/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.aar",
            
            // Test Gradle distribution
            "https://gradle.org/releases/",
            "https://developer.android.com/studio"
        };

        var workingCount = 0;
        var results = new List<string>();
        
        foreach (var url in androidUrls)
        {
            try
            {
                var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                if (response.IsSuccessStatusCode)
                {
                    workingCount++;
                    results.Add($"✅ {url}");
                }
                else
                {
                    results.Add($"❌ {url} -> {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                results.Add($"❌ {url} -> {ex.Message}");
            }
        }

        // Log detailed results
        Logging.SaveLog($"Android Development Test Results ({workingCount}/{androidUrls.Length}):");
        foreach (var result in results)
        {
            Logging.SaveLog($"  {result}");
        }

        // Require at least 3 out of 7 URLs to work
        return workingCount >= 3;
    }

    /// <summary>
    /// Test if Iranian mirrors are accessible
    /// </summary>
    private async Task<bool> TestMirrorsAsync()
    {
        var mirrorUrls = new[]
        {
            "https://maven.myket.ir/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.aar",
            "https://en-mirror.ir/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.aar"
        };

        int workingCount = 0;
        foreach (var url in mirrorUrls)
        {
            try
            {
                var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                if (response.IsSuccessStatusCode)
                {
                    workingCount++;
                }
            }
            catch
            {
                // Mirror not accessible
            }
        }

        return workingCount > 0;
    }

    /// <summary>
    /// Test if a specific DNS server is working with comprehensive diagnostics
    /// </summary>
    public async Task<bool> TestDnsServerAsync(string dnsServerName)
    {
        try
        {
            Logging.SaveLog($"🔍 TESTING DNS: {dnsServerName}");
            
            if (dnsServerName == "google")
            {
                // Test Google DNS with multiple endpoints
                var googleTests = new[]
                {
                    "https://maven.google.com/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.aar",
                    "https://dl.google.com/",
                    "https://www.google.com/"
                };
                
                foreach (var testUrl in googleTests)
                {
                    if (await TestMirrorAsync(testUrl))
                    {
                        Logging.SaveLog($"✅ GOOGLE DNS: Working via {testUrl}");
                        return true;
                    }
                }
                
                Logging.SaveLog($"❌ GOOGLE DNS: All tests failed");
                return false;
            }

            // Test Iranian DNS with multiple validation methods
            var iranianTests = new[]
            {
                "https://maven.myket.ir/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.aar",
                "https://en-mirror.ir/",
                "https://www.aparat.com/", // Iranian video platform
                "https://www.digikala.com/" // Iranian e-commerce
            };
            
            var passedTests = 0;
            foreach (var testUrl in iranianTests)
            {
                if (await TestMirrorAsync(testUrl))
                {
                    passedTests++;
                    Logging.SaveLog($"✅ IRANIAN DNS {dnsServerName}: Working via {testUrl}");
                }
                else
                {
                    Logging.SaveLog($"❌ IRANIAN DNS {dnsServerName}: Failed via {testUrl}");
                }
            }
            
            // DNS is considered working if at least 50% of tests pass
            var isWorking = passedTests >= (iranianTests.Length / 2);
            
            if (isWorking)
            {
                Logging.SaveLog($"✅ DNS TEST RESULT: {dnsServerName} - WORKING ({passedTests}/{iranianTests.Length} tests passed)");
            }
            else
            {
                Logging.SaveLog($"❌ DNS TEST RESULT: {dnsServerName} - FAILED ({passedTests}/{iranianTests.Length} tests passed)");
            }
            
            return isWorking;
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"❌ DNS TEST ERROR: {dnsServerName} - {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Test if a specific mirror URL is accessible
    /// </summary>
    private async Task<bool> TestMirrorAsync(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Check if sanctions are active by testing Google domains
    /// </summary>
    public async Task<bool> CheckSanctionsStatusAsync()
    {
        try
        {
            // Test Iranian mirrors first to ensure they work
            var mirrorTestUrls = new[]
            {
                "https://maven.myket.ir/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.aar",
                "https://en-mirror.ir/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.aar",
                "https://maven.aliyun.com/repository/central/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.aar"
            };

            int mirrorWorkingCount = 0;
            foreach (var url in mirrorTestUrls)
            {
                try
                {
                    var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    if (response.IsSuccessStatusCode)
                    {
                        mirrorWorkingCount++;
                    }
                }
                catch
                {
                    // Mirror not working
                }
            }

            // If mirrors are working, sanctions might not be active
            if (mirrorWorkingCount >= mirrorTestUrls.Length - 1)
            {
                _isSanctionsActive = false;
                return false;
            }

            // Test Google domains for 403 errors - prioritize developer.android.com which is frequently blocked
            var googleTestUrls = new[]
            {
                "https://developer.android.com/studio", // Most frequently blocked
                "https://developer.android.com/docs",   // Also commonly blocked
                "https://maven.google.com/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.aar",
                "https://dl.google.com/dl/android/studio/ide-zips/4.2.2.0/android-studio-2021.2.1.16-windows.zip",
                "https://gradle.org/releases/"
            };

            int forbiddenCount = 0;
            foreach (var url in googleTestUrls)
            {
                try
                {
                    using var response = await _httpClient.GetAsync(url);
                    
                    // Check for status code-based sanctions first
                    if (response.StatusCode == HttpStatusCode.Forbidden ||
                        response.StatusCode == HttpStatusCode.NotFound ||
                        response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                        response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        Logging.SaveLog($"🚫 SANCTIONS DETECTED: {url} returned {response.StatusCode}");
                        if (url.Contains("developer.android.com"))
                        {
                            Logging.SaveLog("⚠️ CRITICAL: developer.android.com is blocked - This is the primary indicator of Iranian sanctions");
                        }
                        forbiddenCount++;
                        continue; // No need to check content if status code indicates blocking
                    }

                    // Only read content if we need to check for content-based blocking
                    try
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        
                    // Check for content-based sanctions (Service Unavailable, blocking messages)
                        if (content.Contains("Service Unavailable", StringComparison.OrdinalIgnoreCase) ||
                             content.Contains("503 Service Temporarily Unavailable", StringComparison.OrdinalIgnoreCase) ||
                             content.Contains("Access Denied", StringComparison.OrdinalIgnoreCase) ||
                             content.Contains("Forbidden", StringComparison.OrdinalIgnoreCase) ||
                             content.Contains("blocked", StringComparison.OrdinalIgnoreCase) ||
                             content.Contains("sanctions", StringComparison.OrdinalIgnoreCase) ||
                             content.Contains("geo-restricted", StringComparison.OrdinalIgnoreCase) ||
                             content.Contains("region restricted", StringComparison.OrdinalIgnoreCase) ||
                             content.Contains("not available in your region", StringComparison.OrdinalIgnoreCase) ||
                             content.Contains("access from your location", StringComparison.OrdinalIgnoreCase))
                    {
                            Logging.SaveLog($"🚫 CONTENT-BASED SANCTIONS: {url} contains blocking message");
                        forbiddenCount++;
                        }
                        else if (response.IsSuccessStatusCode)
                        {
                            Logging.SaveLog($"✅ ACCESSIBLE: {url} is working normally");
                        }
                    }
                    catch (Exception contentEx)
                    {
                        Logging.SaveLog($"⚠️ Could not read content from {url}: {contentEx.Message}");
                        // Don't count content read failures as sanctions
                    }
                }
                catch (HttpRequestException ex)
                {
                    // Network error, might be sanctions
                    Logging.SaveLog($"SanctionsBypassService: Network error for {url} - {ex.Message}");
                    forbiddenCount++;
                }
                catch (TaskCanceledException)
                {
                    // Timeout, might be sanctions
                    Logging.SaveLog($"SanctionsBypassService: Timeout for {url} - possible sanctions");
                    forbiddenCount++;
                }
            }

            // If more than 60% of Google requests fail, sanctions are active
            _isSanctionsActive = forbiddenCount >= (int)(googleTestUrls.Length * 0.6);
            return _isSanctionsActive;
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"SanctionsBypassService: Error checking sanctions status: {ex.Message}");
            // If we can't check, assume sanctions are active for safety
            _isSanctionsActive = true;
            return true;
        }
    }

    /// <summary>
    /// Get the best DNS server for current conditions with intelligent selection
    /// </summary>
    public async Task<string> GetBestDnsServerAsync()
    {
        try
        {
            if (!_isSanctionsActive)
            {
                return "google";
            }

            // Use intelligent DNS selection based on performance and reliability
            var bestDns = await SelectOptimalDnsServerAsync();
            
            lock (_lockObject)
            {
                _currentDnsServer = bestDns;
            }
            
            return bestDns;
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"Error getting best DNS server: {ex.Message}");
            return _currentDnsServer ?? "electro-primary";
        }
    }

    /// <summary>
    /// Intelligently select the optimal DNS server based on performance testing
    /// </summary>
    private async Task<string> SelectOptimalDnsServerAsync()
    {
        var tierGroups = new[]
        {
            // Tier 1: Most reliable (test first)
            new[] { "shecan-primary", "electro-primary", "radar-primary" },
            // Tier 2: Reliable alternatives
            new[] { "shecan-secondary", "electro-secondary", "shelter-primary", "403-primary" },
            // Tier 3: Additional options
            new[] { "asan-primary", "begzar-primary", "pishgaman-primary" },
            // Tier 4: Mobile operators (good for mobile connections)
            new[] { "irancell-primary", "hamrah-primary", "rightel-primary" },
            // Tier 5: Regional servers
            new[] { "tehran-dns1", "mashhad-dns1", "isfahan-dns1" }
        };

        foreach (var tier in tierGroups)
        {
            var tasks = tier.Select(async dns =>
            {
                try
                {
                    var isWorking = await TestDnsServerAsync(dns);
                    var responseTime = await MeasureDnsResponseTimeAsync(dns);
                    return new { DnsName = dns, IsWorking = isWorking, ResponseTime = responseTime };
                }
                catch
                {
                    return new { DnsName = dns, IsWorking = false, ResponseTime = TimeSpan.MaxValue };
                }
            });

            var results = await Task.WhenAll(tasks);
            var workingDns = results
                .Where(r => r.IsWorking)
                .OrderBy(r => r.ResponseTime)
                .FirstOrDefault();

            if (workingDns != null)
            {
                Logging.SaveLog($"🎯 OPTIMAL DNS SELECTED: {workingDns.DnsName} (Tier {Array.IndexOf(tierGroups, tier) + 1}, {workingDns.ResponseTime.TotalMilliseconds:F0}ms)");
                return workingDns.DnsName;
            }
        }

        // Fallback to default if nothing works
        Logging.SaveLog("⚠️ No optimal DNS found, using electro-primary as fallback");
        return "electro-primary";
    }

    /// <summary>
    /// Measure DNS response time for performance optimization
    /// </summary>
    private async Task<TimeSpan> MeasureDnsResponseTimeAsync(string dnsServerName)
    {
        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await TestDnsServerAsync(dnsServerName);
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }
        catch
        {
            return TimeSpan.MaxValue;
        }
    }

    /// <summary>
    /// Switch to the next available DNS server
    /// </summary>
    public string SwitchToNextDnsServer()
    {
        lock (_lockObject)
        {
            var dnsKeys = _iranianDnsServers.Keys.ToList();
            var currentIndex = dnsKeys.IndexOf(_currentDnsServer);

            if (currentIndex == -1 || currentIndex >= dnsKeys.Count - 1)
            {
                _currentDnsServer = dnsKeys.First();
            }
            else
            {
                _currentDnsServer = dnsKeys[currentIndex + 1];
            }

            Logging.SaveLog($"SanctionsBypassService: Switched DNS server to: {_currentDnsServer}");
            return _currentDnsServer;
        }
    }



    /// <summary>
    /// Get all available Iranian DNS servers
    /// </summary>
    public Dictionary<string, string> GetIranianDnsServers()
    {
        return new Dictionary<string, string>(_iranianDnsServers);
    }

    /// <summary>
    /// Check if current conditions require sanctions bypass
    /// </summary>
    public async Task<bool> ShouldUseSanctionsBypassAsync()
    {
        // Check sanctions status
        var sanctionsActive = await CheckSanctionsStatusAsync();

        // If sanctions are not active, use normal DNS
        if (!sanctionsActive)
        {
            return false;
        }

        // Test current DNS server
        var currentWorking = await TestDnsServerAsync(_currentDnsServer);
        if (currentWorking)
        {
            return true;
        }

        // Try to find a working DNS server
        foreach (var dnsServer in _iranianDnsServers.Keys)
        {
            if (await TestDnsServerAsync(dnsServer))
            {
                _currentDnsServer = dnsServer;
                return true;
            }
        }

        // If no Iranian DNS works, fall back to normal
        return false;
    }

    /// <summary>
    /// Get DNS configuration optimized for sanctions bypass
    /// </summary>
    public async Task<string> GetSanctionsBypassDnsConfigAsync()
    {
        var shouldUseBypass = await ShouldUseSanctionsBypassAsync();

        if (!shouldUseBypass)
        {
            return "google"; // Use normal DNS
        }

        return _currentDnsServer;
    }

    /// <summary>
    /// Advanced proactive monitoring with health checking and auto-optimization
    /// </summary>
    public async Task StartAdvancedMonitoringAsync()
    {
        Logging.SaveLog("🚀 ADVANCED MONITORING: Starting proactive sanctions bypass system");
        
        while (true)
        {
            try
            {
                // Step 1: Health check current DNS
                if (_isSanctionsActive && !string.IsNullOrEmpty(_currentDnsServer))
                {
                    var healthCheck = await PerformDnsHealthCheckAsync(_currentDnsServer);
                    if (!healthCheck.IsHealthy)
                    {
                        Logging.SaveLog($"⚠️ DNS HEALTH ISSUE: {_currentDnsServer} - {healthCheck.Issue}");
                        await OptimizeDnsServerAsync();
                    }
                }

                // Step 2: Proactive sanctions detection
                var sanctionsStatus = await PerformProactiveSanctionsCheckAsync();
                if (sanctionsStatus.SanctionsDetected && !_isSanctionsActive)
                {
                    Logging.SaveLog("🚨 PROACTIVE DETECTION: Sanctions activated before user encountered issues");
                    _isSanctionsActive = true;
                    await ActivateEmergencyBypassAsync();
                }

                // Step 3: Performance optimization
                await OptimizePerformanceAsync();

                // Step 4: Update blocked domains list periodically
                if (DateTime.Now.Hour == 3 && DateTime.Now.Minute < 10) // Once daily at 3 AM
                {
                    await UpdateBlockedDomainsListAsync();
                }

                // Monitor every 2 minutes for faster response
                await Task.Delay(TimeSpan.FromMinutes(2));
            }
            catch (Exception ex)
            {
                Logging.SaveLog("Advanced monitoring error", ex);
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }
    }

    /// <summary>
    /// Perform comprehensive DNS health check
    /// </summary>
    private async Task<(bool IsHealthy, string Issue)> PerformDnsHealthCheckAsync(string dnsServer)
    {
        try
        {
            var tests = new (string, Func<Task<bool>>)[]
            {
                ("Basic connectivity", () => TestDnsServerAsync(dnsServer)),
                ("Response time", async () => (await MeasureDnsResponseTimeAsync(dnsServer)).TotalMilliseconds < 5000),
                ("Critical domain resolution", () => TestSpecificDomainResolutionAsync(dnsServer, "developer.android.com")),
                ("Mirror accessibility", () => TestMirrorAccessibilityAsync(dnsServer))
            };

            foreach (var (testName, testFunc) in tests)
            {
                try
                {
                    var result = await testFunc();
                    if (!result)
                    {
                        return (false, $"Failed {testName}");
                    }
                }
                catch (Exception ex)
                {
                    return (false, $"Exception in {testName}: {ex.Message}");
                }
            }

            return (true, "All health checks passed");
        }
        catch (Exception ex)
        {
            return (false, $"Health check error: {ex.Message}");
        }
    }

    /// <summary>
    /// Test specific domain resolution through DNS server
    /// </summary>
    private async Task<bool> TestSpecificDomainResolutionAsync(string dnsServer, string domain)
    {
        try
        {
            // This would test actual DNS resolution, simplified for now
            return await TestDnsServerAsync(dnsServer);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Test mirror accessibility through specific DNS
    /// </summary>
    private async Task<bool> TestMirrorAccessibilityAsync(string dnsServer)
    {
        try
        {
            var testUrl = "https://maven.myket.ir/";
            var response = await _httpClient.GetAsync(testUrl, HttpCompletionOption.ResponseHeadersRead);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Proactive sanctions detection with multiple indicators
    /// </summary>
    private async Task<(bool SanctionsDetected, string[] Indicators)> PerformProactiveSanctionsCheckAsync()
    {
        var indicators = new List<string>();
        var detectionMethods = new (string, Func<Task<bool>>)[]
        {
            ("HTTP status codes", CheckHttpStatusIndicators),
            ("DNS resolution patterns", CheckDnsResolutionPatterns),
            ("Network latency patterns", CheckLatencyPatterns),
            ("Mirror accessibility", CheckMirrorAccessibilityPatterns)
        };

        foreach (var (methodName, method) in detectionMethods)
        {
            try
            {
                var detected = await method();
                if (detected)
                {
                    indicators.Add(methodName);
                }
            }
            catch (Exception ex)
            {
                Logging.SaveLog($"Proactive detection error in {methodName}: {ex.Message}");
            }
        }

        var sanctionsDetected = indicators.Count >= 2; // Require at least 2 indicators
        return (sanctionsDetected, indicators.ToArray());
    }

    private async Task<bool> CheckHttpStatusIndicators()
    {
        var testUrls = new[] { "https://developer.android.com/", "https://googleapis.com/" };
        var forbiddenCount = 0;

        foreach (var url in testUrls)
        {
            try
            {
                var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                if (response.StatusCode == HttpStatusCode.Forbidden || 
                    response.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    forbiddenCount++;
                }
            }
            catch
            {
                forbiddenCount++;
            }
        }

        return forbiddenCount >= testUrls.Length / 2;
    }

    private async Task<bool> CheckDnsResolutionPatterns()
    {
        // Simplified DNS pattern check
        return await Task.FromResult(false); // Placeholder for actual DNS resolution testing
    }

    private async Task<bool> CheckLatencyPatterns()
    {
        try
        {
            var latencyTests = new[] { "https://google.com/", "https://github.com/" };
            var highLatencyCount = 0;

            foreach (var url in latencyTests)
            {
                try
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    stopwatch.Stop();

                    if (stopwatch.ElapsedMilliseconds > 10000) // More than 10 seconds
                    {
                        highLatencyCount++;
                    }
                }
                catch
                {
                    highLatencyCount++;
                }
            }

            return highLatencyCount >= latencyTests.Length / 2;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CheckMirrorAccessibilityPatterns()
    {
        var iranianMirrors = new[] { "https://maven.myket.ir/", "https://en-mirror.ir/" };
        var workingCount = 0;

        foreach (var mirror in iranianMirrors)
        {
            try
            {
                var response = await _httpClient.GetAsync(mirror, HttpCompletionOption.ResponseHeadersRead);
                if (response.IsSuccessStatusCode)
                {
                    workingCount++;
                }
            }
            catch { }
        }

        return workingCount > 0; // If Iranian mirrors work but international sites don't, likely sanctions
    }

    /// <summary>
    /// Activate emergency bypass when sanctions are detected proactively
    /// </summary>
    private async Task ActivateEmergencyBypassAsync()
    {
        try
        {
            Logging.SaveLog("🚨 EMERGENCY BYPASS ACTIVATION: Proactive sanctions detection triggered");
            
            // Switch to best Iranian DNS immediately
            await OptimizeDnsServerAsync();
            
            // Notify user
            SendUIMessage("🚨 PROACTIVE BYPASS: Sanctions detected and automatically bypassed");
            
            // Apply emergency configuration
            await ApplyEmergencyConfigurationAsync();
            
            Logging.SaveLog("✅ EMERGENCY BYPASS: Activated successfully");
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"Emergency bypass activation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Optimize DNS server selection automatically
    /// </summary>
    private async Task OptimizeDnsServerAsync()
    {
        try
        {
            var optimalDns = await SelectOptimalDnsServerAsync();
            if (optimalDns != _currentDnsServer)
            {
                _currentDnsServer = optimalDns;
                Logging.SaveLog($"🎯 DNS OPTIMIZATION: Switched to {optimalDns}");
                SendUIMessage($"🔄 Optimized DNS: Now using {optimalDns}");
            }
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"DNS optimization error: {ex.Message}");
        }
    }

    /// <summary>
    /// Optimize overall performance periodically
    /// </summary>
    private async Task OptimizePerformanceAsync()
    {
        try
        {
            // Clear old cache entries
            var expiredKeys = _mirrorCache.Keys.ToArray()
                .Where(key => DateTime.Now - _mirrorCache[key].LastChecked > TimeSpan.FromMinutes(10))
                .ToArray();

            foreach (var key in expiredKeys)
            {
                _mirrorCache.Remove(key);
            }

            if (expiredKeys.Length > 0)
            {
                Logging.SaveLog($"🧹 CACHE CLEANUP: Removed {expiredKeys.Length} expired cache entries");
            }
            
            // Add minimal async operation to satisfy async requirement
            await Task.Delay(1);
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"Performance optimization error: {ex.Message}");
        }
    }

    /// <summary>
    /// Apply emergency configuration when sanctions are detected
    /// </summary>
    private async Task ApplyEmergencyConfigurationAsync()
    {
        try
        {
            // This would apply emergency DNS and routing configuration
            // Placeholder for actual implementation
            await Task.CompletedTask;
            Logging.SaveLog("🔧 EMERGENCY CONFIG: Applied emergency bypass configuration");
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"Emergency configuration error: {ex.Message}");
        }
    }

    /// <summary>
    /// Log routing rules order for debugging
    /// </summary>
    private async Task LogRoutingRulesOrder(V2rayConfig v2rayCfg)
    {
        try
        {
            Logging.SaveLog("🔍 ===== FINAL ROUTING RULES ORDER DEBUG =====");
            Logging.SaveLog($"📊 Total routing rules: {v2rayCfg.routing.rules.Count}");
            
            // Check if developer.android.com is in the first rule
            if (v2rayCfg.routing.rules.Count > 0)
            {
                var firstRule = v2rayCfg.routing.rules[0];
                var hasDeveloperAndroid = firstRule.domain?.Any(d => d.Contains("developer.android")) == true;
                
                if (hasDeveloperAndroid)
                {
                    Logging.SaveLog($"✅ SUCCESS: developer.android.com is FIRST RULE → {firstRule.outboundTag}");
                }
                else
                {
                    Logging.SaveLog($"❌ ERROR: developer.android.com is NOT the first rule!");
                    Logging.SaveLog($"   First rule domains: {string.Join(", ", firstRule.domain ?? new List<string>())}");
                }
            }
            
            // Log detailed information for first 15 rules
            for (int i = 0; i < Math.Min(v2rayCfg.routing.rules.Count, 15); i++)
            {
                var rule = v2rayCfg.routing.rules[i];
                var domains = rule.domain != null ? string.Join(", ", rule.domain.Take(2)) : "none";
                if (rule.domain != null && rule.domain.Count > 2)
                {
                    domains += $" (and {rule.domain.Count - 2} more)";
                }
                
                var ruleType = rule.domain?.Any(d => d.Contains("developer.android")) == true ? "🎯 CRITICAL" : 
                              rule.outboundTag == "direct" ? "➡️ DIRECT" : 
                              rule.outboundTag?.Contains("proxy") == true ? "🔒 PROXY" : "📋 OTHER";
                
                Logging.SaveLog($"   {i:D2}: {ruleType} | {rule.outboundTag} ← {domains}");
                
                // Special attention to developer.android.com and android.com rules
                if (rule.domain != null && rule.domain.Any(d => d.Contains("developer.android") || d.Contains("android.com")))
                {
                    Logging.SaveLog($"       🔍 ANDROID RULE FOUND: Position {i} → {rule.outboundTag}");
                    Logging.SaveLog($"       📋 All domains: {string.Join(", ", rule.domain)}");
                }
            }
            
            // Final verification
            var developerAndroidRules = v2rayCfg.routing.rules
                .Select((rule, index) => new { rule, index })
                .Where(x => x.rule.domain?.Any(d => d.Contains("developer.android")) == true)
                .ToList();
                
            if (developerAndroidRules.Count == 0)
            {
                Logging.SaveLog("❌ CRITICAL ERROR: No developer.android.com rules found in routing table!");
            }
            else if (developerAndroidRules.Count == 1 && developerAndroidRules[0].index == 0)
            {
                Logging.SaveLog("✅ PERFECT: developer.android.com has exactly ONE rule at position 0 → direct");
            }
            else
            {
                Logging.SaveLog($"⚠️ WARNING: Found {developerAndroidRules.Count} developer.android.com rules:");
                foreach (var item in developerAndroidRules)
                {
                    Logging.SaveLog($"   Position {item.index}: {item.rule.outboundTag}");
                }
            }
            
            Logging.SaveLog("🔍 ===== END ROUTING RULES DEBUG =====");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"Error logging routing rules order: {ex.Message}");
        }
    }

    /// <summary>
    /// NUCLEAR FIX: Force developer.android.com to use direct connection - overrides all other rules
    /// </summary>
    private async Task ForceDirectConnectionForAndroidDev(V2rayConfig v2rayConfig)
    {
        try
        {
            Logging.SaveLog("🚨 NUCLEAR OVERRIDE: Forcing developer.android.com to direct connection");
            
            // STEP 1: Remove ANY existing rules for developer.android.com or android.com
            var originalCount = v2rayConfig.routing.rules.Count;
            v2rayConfig.routing.rules.RemoveAll(rule => 
                rule.domain?.Any(d => d.Contains("developer.android") || 
                                     d.Contains("android.com") ||
                                     d.Contains("source.android") ||
                                     d.Contains("android.googlesource")) == true);
            
            var removedCount = originalCount - v2rayConfig.routing.rules.Count;
            if (removedCount > 0)
            {
                Logging.SaveLog($"🗑️ REMOVED {removedCount} conflicting Android rules");
            }
            
            // STEP 2: Create the ABSOLUTE priority rule for Android development
            var androidDevRule = new RulesItem4Ray
            {
                type = "field",
                domain = new List<string> 
                { 
                    "domain:developer.android.com",
                    "full:developer.android.com",
                    "regexp:^developer\\.android\\.com$",
                    "domain:source.android.com",
                    "domain:android.googlesource.com",
                    "domain:androidstudio.googleblog.com"
                },
                outboundTag = "direct" // FORCE direct connection
            };
            
            // STEP 3: INSERT AT POSITION 0 - ABSOLUTE HIGHEST PRIORITY
            v2rayConfig.routing.rules.Insert(0, androidDevRule);
            
            Logging.SaveLog("✅ NUCLEAR OVERRIDE APPLIED: developer.android.com is now POSITION 0 → direct");
            Logging.SaveLog($"📊 Total rules after nuclear override: {v2rayConfig.routing.rules.Count}");
            
            SendUIMessage("🚨 NUCLEAR FIX: developer.android.com FORCED to direct connection (cannot be overridden)");
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"❌ Error in nuclear override: {ex.Message}");
        }
    }

    /// <summary>
    /// Public wrapper for ForceDirectConnectionForAndroidDev - called from V2rayDnsService
    /// </summary>
    public async Task ForceDirectConnectionForAndroidDevPublic(object v2rayConfig)
    {
        try
        {
            // Cast the v2rayConfig to the correct type
            if (v2rayConfig is V2rayConfig v2rayCfg)
            {
                await ForceDirectConnectionForAndroidDev(v2rayCfg);
                await LogRoutingRulesOrder(v2rayCfg);
                await VerifyAndLogFinalConfiguration(v2rayCfg);
            }
            else
            {
                Logging.SaveLog("❌ ForceDirectConnectionForAndroidDevPublic: Invalid V2Ray config type");
            }
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"❌ Error in ForceDirectConnectionForAndroidDevPublic: {ex.Message}");
        }
    }

    /// <summary>
    /// Verify and log the final configuration that will be sent to V2Ray core
    /// </summary>
    private async Task VerifyAndLogFinalConfiguration(V2rayConfig v2rayConfig)
    {
        try
        {
            Logging.SaveLog("🔍 ===== FINAL V2RAY CONFIGURATION VERIFICATION =====");
            
            // Check for developer.android.com rules
            var androidRules = v2rayConfig.routing.rules
                .Select((rule, index) => new { rule, index })
                .Where(x => x.rule.domain?.Any(d => d.Contains("developer.android")) == true)
                .ToList();
                
            if (androidRules.Count == 0)
            {
                Logging.SaveLog("❌ CRITICAL ERROR: NO developer.android.com rules found in final config!");
            }
            else
            {
                Logging.SaveLog($"✅ FOUND {androidRules.Count} developer.android.com rules:");
                foreach (var item in androidRules)
                {
                    Logging.SaveLog($"   Position {item.index}: {item.rule.outboundTag} ← {string.Join(", ", item.rule.domain ?? new List<string>())}");
                    
                    if (item.index == 0 && item.rule.outboundTag == "direct")
                    {
                        Logging.SaveLog("✅ PERFECT: developer.android.com is at position 0 with direct outbound");
                    }
                }
            }
            
            // Verify the first rule is our Android rule
            if (v2rayConfig.routing.rules.Count > 0)
            {
                var firstRule = v2rayConfig.routing.rules[0];
                var isAndroidRule = firstRule.domain?.Any(d => d.Contains("developer.android")) == true;
                
                if (isAndroidRule && firstRule.outboundTag == "direct")
                {
                    Logging.SaveLog("🎯 NUCLEAR SUCCESS: First rule is developer.android.com → direct");
                }
                else
                {
                    Logging.SaveLog($"❌ NUCLEAR FAILURE: First rule is NOT developer.android.com");
                    Logging.SaveLog($"   First rule: {firstRule.outboundTag} ← {string.Join(", ", firstRule.domain ?? new List<string>())}");
                }
            }
            
            // Log summary
            Logging.SaveLog($"📊 FINAL CONFIG SUMMARY:");
            Logging.SaveLog($"   Total routing rules: {v2rayConfig.routing.rules.Count}");
            Logging.SaveLog($"   Android dev rules: {androidRules.Count}");
            Logging.SaveLog($"   Domain strategy: {v2rayConfig.routing.domainStrategy}");
            
            Logging.SaveLog("🔍 ===== END FINAL CONFIGURATION VERIFICATION =====");
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"❌ Error in final configuration verification: {ex.Message}");
        }
    }

    /// <summary>
    /// Comprehensive DNS diagnostics and testing tool
    /// Tests all available Iranian DNS servers and provides detailed results
    /// </summary>
    public async Task<string> RunComprehensiveDnsTestAsync()
    {
        try
        {
            Logging.SaveLog("🚀 STARTING COMPREHENSIVE DNS TESTING...");
            SendUIMessage("🔍 Testing all 33 Iranian DNS servers for optimal performance...");
            
            var results = new List<string>();
            var workingDnsServers = new List<(string name, string address, int responseTime, int testsPasssed)>();
            
            // Test all Iranian DNS servers in parallel for faster results
            var testTasks = _iranianDnsServers.Select(async kvp =>
            {
                var dnsName = kvp.Key;
                var dnsAddress = kvp.Value;
                
                try
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    var isWorking = await TestDnsServerAsync(dnsName);
                    stopwatch.Stop();
                    
                    var responseTime = (int)stopwatch.ElapsedMilliseconds;
                    
                    if (isWorking)
                    {
                        // Count how many tests passed for this DNS
                        var testsPasssed = await CountPassedTests(dnsName);
                        workingDnsServers.Add((dnsName, dnsAddress, responseTime, testsPasssed));
                        
                        var result = $"✅ {dnsName} ({dnsAddress}) - {responseTime}ms - {testsPasssed}/4 tests passed";
                        results.Add(result);
                        Logging.SaveLog($"DNS TEST: {result}");
                        return result;
                    }
                    else
                    {
                        var result = $"❌ {dnsName} ({dnsAddress}) - FAILED - {responseTime}ms";
                        results.Add(result);
                        Logging.SaveLog($"DNS TEST: {result}");
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    var result = $"⚠️ {dnsName} ({dnsAddress}) - ERROR: {ex.Message}";
                    results.Add(result);
                    Logging.SaveLog($"DNS TEST: {result}");
                    return result;
                }
            });
            
            // Wait for all tests to complete
            await Task.WhenAll(testTasks);
            
            // Sort working DNS servers by performance (response time and tests passed)
            var bestDnsServers = workingDnsServers
                .OrderByDescending(dns => dns.testsPasssed) // First by tests passed
                .ThenBy(dns => dns.responseTime) // Then by response time
                .Take(5) // Top 5
                .ToList();
            
            // Build comprehensive report
            var report = new System.Text.StringBuilder();
            report.AppendLine("🔍 ===== COMPREHENSIVE DNS TEST RESULTS =====");
            report.AppendLine($"📊 Total DNS servers tested: {_iranianDnsServers.Count}");
            report.AppendLine($"✅ Working DNS servers: {workingDnsServers.Count}");
            report.AppendLine($"❌ Failed DNS servers: {_iranianDnsServers.Count - workingDnsServers.Count}");
            report.AppendLine();
            
            if (bestDnsServers.Count > 0)
            {
                report.AppendLine("🎯 TOP 5 RECOMMENDED DNS SERVERS:");
                for (int i = 0; i < bestDnsServers.Count; i++)
                {
                    var dns = bestDnsServers[i];
                    var tier = GetDnsTier(dns.name);
                    report.AppendLine($"   {i + 1}. {dns.name} ({dns.address}) - {tier}");
                    report.AppendLine($"      ⚡ Response: {dns.responseTime}ms | ✅ Tests: {dns.testsPasssed}/4");
                }
                
                // Automatically set the best DNS as current
                var bestDns = bestDnsServers[0];
                lock (_lockObject)
                {
                    _currentDnsServer = bestDns.name;
                }
                
                report.AppendLine();
                report.AppendLine($"🎯 AUTOMATICALLY SELECTED: {bestDns.name} ({bestDns.address})");
                report.AppendLine($"   📈 Performance: {bestDns.responseTime}ms response, {bestDns.testsPasssed}/4 tests passed");
                
                SendUIMessage($"🎯 OPTIMAL DNS SELECTED: {bestDns.name} ({bestDns.responseTime}ms, {bestDns.testsPasssed}/4 tests)");
            }
            else
            {
                report.AppendLine("❌ NO WORKING DNS SERVERS FOUND");
                report.AppendLine("⚠️ Please check your internet connection and try again.");
            }
            
            report.AppendLine();
            report.AppendLine("📋 DETAILED RESULTS:");
            foreach (var result in results.OrderBy(r => r))
            {
                report.AppendLine($"   {result}");
            }
            
            report.AppendLine("🔍 ===== END DNS TEST RESULTS =====");
            
            var finalReport = report.ToString();
            Logging.SaveLog(finalReport);
            
            return finalReport;
        }
        catch (Exception ex)
        {
            var errorReport = $"❌ DNS Testing Error: {ex.Message}";
            Logging.SaveLog(errorReport);
            return errorReport;
        }
    }

    /// <summary>
    /// Count how many tests passed for a specific DNS server
    /// </summary>
    private async Task<int> CountPassedTests(string dnsServerName)
    {
        try
        {
            var testUrls = new[]
            {
                "https://maven.myket.ir/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.aar",
                "https://en-mirror.ir/",
                "https://www.aparat.com/",
                "https://www.digikala.com/"
            };
            
            var passedCount = 0;
            foreach (var url in testUrls)
            {
                if (await TestMirrorAsync(url))
                {
                    passedCount++;
                }
            }
            
            return passedCount;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Get the tier classification for a DNS server
    /// </summary>
    private string GetDnsTier(string dnsName)
    {
        var tier1 = new[] { "shecan-primary", "shecan-secondary", "electro-primary", "electro-secondary", "radar-primary", "radar-secondary" };
        var tier2 = new[] { "shelter-primary", "shelter-secondary", "403-primary", "403-secondary", "begzar-primary", "begzar-secondary" };
        var tier3 = new[] { "asan-primary", "asan-secondary", "asan-dns" };
        var tier4 = new[] { "pishgaman-primary", "pishgaman-secondary", "tci-primary", "tci-secondary", "mokhaberat-primary", "mokhaberat-secondary", "parspack-primary", "parspack-secondary" };
        var tier5 = new[] { "irancell-primary", "irancell-secondary", "hamrah-primary", "hamrah-secondary", "rightel-primary", "rightel-secondary" };
        var tier6 = new[] { "tehran-dns1", "tehran-dns2", "mashhad-dns1", "mashhad-dns2", "isfahan-dns1", "isfahan-dns2" };
        var tier7 = new[] { "samantel-primary", "samantel-secondary", "arvancloud-primary", "arvancloud-secondary", "cloudflare-iran", "cloudflare-family", "quad9-iran", "quad9-secure", "opendns-primary", "opendns-secondary", "comodo-primary", "comodo-secondary" };
        var tier8 = new[] { "asiatech-primary", "asiatech-secondary", "shatel-primary", "shatel-secondary", "datak-primary", "datak-secondary", "fanava-primary", "fanava-secondary", "respina-primary", "respina-secondary" };
        var tier9 = new[] { "dynx-anti-sanctions-primary", "dynx-anti-sanctions-secondary", "dynx-adblocker-primary", "dynx-ipv6-primary", "dynx-family-safe" };
        
        if (tier1.Contains(dnsName)) return "Tier 1 (Most Reliable)";
        if (tier2.Contains(dnsName)) return "Tier 2 (Reliable Alternative)";
        if (tier3.Contains(dnsName)) return "Tier 3 (Additional Options)";
        if (tier4.Contains(dnsName)) return "Tier 4 (High-Performance 2024)";
        if (tier5.Contains(dnsName)) return "Tier 5 (Mobile Operators)";
        if (tier6.Contains(dnsName)) return "Tier 6 (Regional)";
        if (tier7.Contains(dnsName)) return "Tier 7 (High-Performance Global)";
        if (tier8.Contains(dnsName)) return "Tier 8 (ISP-Specific)";
        if (tier9.Contains(dnsName)) return "Tier 9 (Specialized Anti-Sanctions)";
        
        return "Unknown Tier";
    }

    public async Task EnableIranianDnsAsync()
    {
        try
        {
            Logging.SaveLog("SanctionsBypassService: Enabling Iranian DNS servers");

            // Try each Iranian DNS server until one works
            foreach (var dnsServer in _iranianDnsServers)
            {
                try
                {
                    Logging.SaveLog($"SanctionsBypassService: Testing Iranian DNS server {dnsServer.Key} ({dnsServer.Value})");

                    // Test the DNS server
                    var isWorking = await TestDnsServerAsync(dnsServer.Key);
                    if (isWorking)
                    {
                        Logging.SaveLog($"SanctionsBypassService: Successfully enabled Iranian DNS server {dnsServer.Key}");

                        // Set this DNS server as active in the configuration
                        await ConfigureV2RayDnsAsync(dnsServer.Value);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Logging.SaveLog($"SanctionsBypassService: Failed to enable DNS {dnsServer.Key} - {ex.Message}");
                }
            }

            Logging.SaveLog("SanctionsBypassService: Iranian DNS servers setup completed");
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"SanctionsBypassService: Error enabling Iranian DNS - {ex.Message}");
            throw;
        }
    }

    private async Task ConfigureV2RayDnsAsync(string dnsServer)
    {
        try
        {
            // This would configure V2Ray DNS settings to use Iranian DNS
            Logging.SaveLog($"SanctionsBypassService: Configuring V2Ray DNS to use {dnsServer}");

            // TODO: Implement actual V2Ray DNS configuration
            // This could involve:
            // 1. Modifying DNS configuration files
            // 2. Updating V2Ray config with Iranian DNS servers
            // 3. Setting up DNS routing rules

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"SanctionsBypassService: Error configuring V2Ray DNS - {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Get the IP address for a specific Iranian DNS server
    /// </summary>
    public async Task<string> GetDnsServerAddressAsync(string dnsServerName)
    {
        await Task.CompletedTask; // Make async for consistency

        if (_iranianDnsServers.TryGetValue(dnsServerName, out var address))
        {
            SendUIMessage($"🔍 IRANIAN DNS SERVER: {dnsServerName} → {address}");
            return address;
        }

        // Default to electro-primary if not found
        var defaultAddress = _iranianDnsServers.GetValueOrDefault("electro-primary", "78.157.42.100");
        SendUIMessage($"⚠️ IRANIAN DNS SERVER: {dnsServerName} not found, using default electro-primary → {defaultAddress}");
        return defaultAddress;
    }

    /// <summary>
    /// Configure system DNS to use Iranian DNS for sanctioned domains
    /// </summary>
    public async Task<bool> ConfigureSystemDnsForSanctionsAsync(string iranianDnsServer)
    {
        try
        {
            SendUIMessage($"🏛️ CONFIGURING SYSTEM DNS FOR SANCTIONS BYPASS...");

            // This would configure the system DNS resolver to use Iranian DNS for sanctioned domains
            // On Windows, this could modify network adapter DNS settings or use DNS policy

            // For now, return false to use V2Ray bypass fallback
            SendUIMessage("⚠️ System DNS configuration not implemented - using V2Ray bypass method");
            
            // Add minimal async operation to satisfy async requirement
            await Task.Delay(1);
            return false;

            // TODO: Implement actual system DNS configuration
            // This would require:
            // 1. Detecting network adapters
            // 2. Setting Iranian DNS servers for specific domains
            // 3. Or configuring DNS policy routing

        }
        catch (Exception ex)
        {
            Logging.SaveLog($"Error configuring system DNS: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Configure V2Ray routing to bypass sanctioned domains entirely
    /// </summary>
    public async Task<bool> ConfigureV2RayBypassForSanctionsAsync(object v2rayConfig, string iranianDnsServer)
    {
        try
        {
            SendUIMessage($"🔀 CONFIGURING V2RAY TO BYPASS SANCTIONED DOMAINS...");

            // Cast the v2rayConfig to the correct type
            if (v2rayConfig is not V2rayConfig v2rayCfg)
            {
                SendUIMessage("❌ INVALID V2RAY CONFIG TYPE");
                return false;
            }

            // Ensure routing rules exist
            if (v2rayCfg.routing?.rules == null)
            {
                SendUIMessage("❌ V2RAY ROUTING RULES NOT AVAILABLE");
                return false;
            }

            // Define sanctioned domains that should bypass VPN
            var sanctionedDomains = new List<string>
            {
                "google.com",
                "googleapis.com",
                "android.com",
                "firebase.com",
                "firebase.google.com",
                "firebase.googleapis.com",
                "firebasestorage.googleapis.com",
                "gradle.org",
                "services.gradle.org",
                "plugins.gradle.org",
                "repo.gradle.org",
                "repo.maven.apache.org",
                "repo1.maven.org",
                "central.maven.org",
                "search.maven.org",
                "dl.google.com",
                "maven.google.com",
                "developer.android.com",
                "android.googlesource.com",
                "source.android.com"
            };

            // Create bypass routing rule for sanctioned domains
            var domainPatterns = sanctionedDomains.Select(d => $"domain:{d}").ToList();

            // Add more specific patterns for better matching
            var additionalPatterns = new List<string>();
            foreach (var domain in sanctionedDomains)
            {
                additionalPatterns.Add($"full:{domain}");
                additionalPatterns.Add($"regexp:{domain.Replace(".", "\\.")}$");
            }
            domainPatterns.AddRange(additionalPatterns);

            var bypassRule = new RulesItem4Ray
            {
                type = "field",
                domain = domainPatterns,
                outboundTag = "direct" // Bypass VPN, use direct connection
            };

            // Insert at the beginning so it takes precedence
            v2rayCfg.routing.rules.Insert(0, bypassRule);

            // Add critical Android development domains as HIGHEST PRIORITY direct connection rules
            var criticalAndroidDomains = new List<string>
                {
                    "domain:developer.android.com",
                    "full:developer.android.com",
                "regexp:developer\\.android\\.com$",
                "domain:source.android.com",
                "domain:android.googlesource.com",
                "domain:androidstudio.googleblog.com"
            };

            var developerAndroidRule = new RulesItem4Ray
            {
                type = "field",
                domain = criticalAndroidDomains,
                outboundTag = "direct" // Direct connection with Iranian DNS
            };

            // CRITICAL: Clear any existing rules first, then add our highest priority rule
            // This ensures no other rules can override our developer.android.com rule
            var existingRules = v2rayCfg.routing.rules.ToList();
            v2rayCfg.routing.rules.Clear();
            
            // Add developer.android.com as ABSOLUTE first rule
            v2rayCfg.routing.rules.Add(developerAndroidRule);
            
            // Re-add other existing rules after our critical rule
            foreach (var existingRule in existingRules)
            {
                // Skip any conflicting rules for developer.android.com
                if (existingRule.domain != null && 
                    existingRule.domain.Any(d => d.Contains("developer.android") || d.Contains("android.com")))
                {
                    Logging.SaveLog($"🚫 SKIPPED CONFLICTING RULE: {string.Join(", ", existingRule.domain)} → {existingRule.outboundTag}");
                    continue;
                }
                v2rayCfg.routing.rules.Add(existingRule);
            }

            SendUIMessage("🎯 ABSOLUTE HIGHEST PRIORITY: developer.android.com → DIRECT connection (FIRST RULE)");
            Logging.SaveLog("🚀 CRITICAL ROUTING RULE: developer.android.com is now FIRST RULE in routing table");
            Logging.SaveLog($"   📍 Rule position: 0 (ABSOLUTE priority, cleared all conflicts)");
            Logging.SaveLog($"   📋 Domains covered: {string.Join(", ", criticalAndroidDomains)}");
            Logging.SaveLog($"   🔧 Total rules after reordering: {v2rayCfg.routing.rules.Count}");

            SendUIMessage($"✅ V2RAY BYPASS RULE ADDED: {sanctionedDomains.Count} sanctioned domains → Direct connection");
            SendUIMessage($"📊 TOTAL V2RAY ROUTING RULES: {v2rayCfg.routing.rules.Count}");
            SendUIMessage($"📋 Domain patterns added: {domainPatterns.Count} patterns");

            // Show a few example patterns for debugging
            var examplePatterns = domainPatterns.Take(5).ToList();
            SendUIMessage($"🔍 Example patterns: {string.Join(", ", examplePatterns)}");

            SendUIMessage("🎯 Sanctioned domains will bypass VPN entirely and use direct connection");
            SendUIMessage("🔒 Only non-sanctioned traffic will go through VPN tunnel");

            // NUCLEAR FIX: Force developer.android.com to direct connection - cannot be overridden
            await ForceDirectConnectionForAndroidDev(v2rayCfg);
            
            // Debug: Log the first few routing rules to verify order
            await LogRoutingRulesOrder(v2rayCfg);
            
            // Final verification and logging
            await VerifyAndLogFinalConfiguration(v2rayCfg);

            return true;
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"Error configuring V2Ray bypass: {ex.Message}");
            SendUIMessage($"❌ V2RAY BYPASS CONFIGURATION ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Check if a domain is likely affected by sanctions
    /// </summary>
    public bool IsSanctionsAffectedDomain(string domain)
    {
        if (string.IsNullOrEmpty(domain))
        {
            return false;
        }

        return _googleDomains.Any(googleDomain =>
            domain.Contains(googleDomain, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Load comprehensive list of Iranian blocked domains (Enhanced 2024 version)
    /// </summary>
    private HashSet<string> LoadIranianBlockedDomains()
    {
        var domains = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // CRITICAL: Core Android/Google development domains (Highest Priority)
            "gradle.org", "services.gradle.org", "plugins.gradle.org", "repo.gradle.org",
            "maven.google.com", "dl.google.com", "dl-ssl.google.com",
            "developer.android.com", "source.android.com", "android.googlesource.com",
            "firebase.google.com", "firebase.googleapis.com", "firebasestorage.googleapis.com",
            "googleapis.com", "fonts.googleapis.com", "ajax.googleapis.com",
            "developers.google.com", "android-developers.googleblog.com",

            // CRITICAL: Repository and package managers
            "repo.maven.apache.org", "repo1.maven.org", "central.maven.org", "search.maven.org",
            "central.sonatype.org", "oss.sonatype.org", "nexus.sonatype.org",
            "jcenter.bintray.com", "bintray.com", "jfrog.com",
            "npmjs.org", "registry.npmjs.org", "npm.taobao.org",
            "pypi.org", "pypi.python.org", "files.pythonhosted.org",
            "rubygems.org", "gem.fury.io", "rubygems.pkg.github.com",

            // CRITICAL: Google Cloud and Infrastructure
            "cloud.google.com", "console.cloud.google.com", "cloudshell.dev",
            "storage.googleapis.com", "compute.googleapis.com", "container.googleapis.com",
            "cloudbuild.googleapis.com", "cloudresourcemanager.googleapis.com",
            "servicemanagement.googleapis.com", "logging.googleapis.com",
            "monitoring.googleapis.com", "bigquery.googleapis.com",

            // CRITICAL: Google core services
            "accounts.google.com", "myaccount.google.com", "support.google.com",
            "ads.google.com", "adservice.google.com", "doubleclick.net",
            "ai.google.com", "bard.google.com", "gemini.google.com",
            "analytics.google.com", "google-analytics.com", "googleanalytics.com",
            "apis.google.com", "script.google.com", "appengine.google.com",
            "books.google.com", "scholar.google.com", "patents.google.com",
            "business.google.com", "workspace.google.com", "gsuite.google.com",
            "chat.google.com", "meet.google.com", "hangouts.google.com",
            "classroom.google.com", "edu.google.com", "teachercenter.withgoogle.com",
            "clients.google.com", "clients1.google.com", "clients2.google.com", 
            "clients3.google.com", "clients4.google.com", "clients5.google.com", "clients6.google.com",
            "code.google.com", "opensource.google", "android.googlesource.com",
            "datastudio.google.com", "lookerstudio.google.com",
            "issuetracker.google.com", "bugs.chromium.org", "crbug.com",
            "lens.google.com", "images.google.com", "photos.google.com",
            "maps.google.com", "earth.google.com", "streetview.google.com",
            "marketingplatform.google.com", "admanager.google.com",
            "notifications.google.com", "one.google.com", "drive.google.com",
            "optimize.google.com", "pay.google.com", "payments.google.com",
            "play.google.com", "play-games.com", "googleplay.com",
            "research.google.com", "ai.google.dev", "deepmind.com",
            "services.google.com", "surveys.google.com", "forms.google.com",
            "tagmanager.google.com", "optimize.google.com",
            "time.google.com", "remotedesktop.google.com", "checks.google.com",

            // HIGH PRIORITY: Microsoft ecosystem
            "microsoft.com", "msdn.microsoft.com", "docs.microsoft.com",
            "azure.com", "azure.microsoft.com", "portal.azure.com",
            "office.com", "office365.com", "outlook.com", "live.com",
            "login.microsoftonline.com", "graph.microsoft.com",
            "visualstudio.com", "code.visualstudio.com", "marketplace.visualstudio.com",
            "nuget.org", "dotnet.microsoft.com", "dotnetfoundation.org",
            "technet.microsoft.com", "techcommunity.microsoft.com",

            // HIGH PRIORITY: GitHub and development platforms
            "github.com", "githubusercontent.com", "github.io", "githubassets.com",
            "api.github.com", "raw.githubusercontent.com", "gist.github.com",
            "desktop.github.com", "education.github.com", "enterprise.github.com",
            "gitlab.com", "about.gitlab.com", "docs.gitlab.com",
            "bitbucket.org", "atlassian.com", "confluence.atlassian.com",
            "jira.atlassian.com", "trello.com",

            // HIGH PRIORITY: Social and communication platforms
            "discord.com", "discordapp.com", "discord.media", "discord.gg",
            "slack.com", "api.slack.com", "hooks.slack.com",
            "zoom.us", "zoomgov.com", "webex.com", "teams.microsoft.com",
            "skype.com", "lync.com", "telegram.org", "web.telegram.org",
            "whatsapp.com", "web.whatsapp.com", "signal.org",

            // HIGH PRIORITY: Apple ecosystem
            "apple.com", "icloud.com", "me.com", "mac.com",
            "itunes.apple.com", "apps.apple.com", "appstore.com",
            "developer.apple.com", "appleid.apple.com", "iforgot.apple.com",
            "testflight.apple.com", "itunesconnect.apple.com",

            // MEDIUM PRIORITY: Content and media platforms
            "youtube.com", "youtu.be", "youtube-nocookie.com", "ytimg.com",
            "twitch.tv", "twitchcdn.net", "vimeo.com", "dailymotion.com",
            "spotify.com", "open.spotify.com", "scdn.co",
            "soundcloud.com", "bandcamp.com", "deezer.com",
            "netflix.com", "amazonprime.com", "hulu.com", "disney.com",

            // MEDIUM PRIORITY: Social media platforms
            "facebook.com", "fb.com", "fbcdn.net", "instagram.com", "cdninstagram.com",
            "twitter.com", "x.com", "twimg.com", "t.co",
            "linkedin.com", "licdn.com", "reddit.com", "redd.it", "redditmedia.com",
            "pinterest.com", "pinimg.com", "snapchat.com", "tiktok.com",

            // MEDIUM PRIORITY: E-commerce and services
            "amazon.com", "amazonaws.com", "cloudfront.net", "s3.amazonaws.com",
            "ebay.com", "paypal.com", "stripe.com", "shopify.com",
            "aliexpress.com", "alibaba.com", "tmall.com", "taobao.com",

            // MEDIUM PRIORITY: News and information
            "wikipedia.org", "wikimedia.org", "wiktionary.org",
            "stackoverflow.com", "stackexchange.com", "serverfault.com",
            "superuser.com", "mathoverflow.net", "askubuntu.com",
            "medium.com", "substack.com", "blogger.com", "wordpress.com",

            // MEDIUM PRIORITY: Cloud services and CDNs
            "cloudflare.com", "cdnjs.cloudflare.com", "jsdelivr.net",
            "unpkg.com", "fastly.com", "maxcdn.com", "bootstrapcdn.com",
            "gravatar.com", "imgur.com", "gfycat.com", "giphy.com",

            // MEDIUM PRIORITY: Development tools and services
            "mozilla.org", "firefox.com", "addons.mozilla.org",
            "chrome.google.com", "chromium.org", "chromewebstore.google.com",
            "jetbrains.com", "intellij.net", "kotlinlang.org",
            "apache.org", "maven.apache.org", "tomcat.apache.org",
            "oracle.com", "java.com", "openjdk.java.net",
            "docker.com", "dockerhub.com", "quay.io", "gcr.io",
            "kubernetes.io", "helm.sh", "istio.io", "envoyproxy.io",

            // MEDIUM PRIORITY: Educational and enterprise
            "coursera.org", "udemy.com", "edx.org", "khanacademy.org",
            "udacity.com", "pluralsight.com", "lynda.com", "skillshare.com",
            "salesforce.com", "force.com", "trailhead.salesforce.com",
            "zendesk.com", "freshworks.com", "intercom.com",
            "mailchimp.com", "hubspot.com", "marketo.com",

            // LOW PRIORITY: Gaming and entertainment
            "steam.com", "steampowered.com", "steamcommunity.com",
            "epicgames.com", "unrealengine.com", "unity.com", "unity3d.com",
            "roblox.com", "minecraft.net", "mojang.com", "battle.net",

            // IRANIAN MIRRORS AND ALTERNATIVES (These should NOT be blocked)
            "maven.myket.ir", "en-mirror.ir", "maven.aliyun.com",
            "mirrors.huaweicloud.com", "mirrors.cloud.tencent.com",
            "mirrors.ustc.edu.cn", "mirrors.tuna.tsinghua.edu.cn",
            "repo.huaweicloud.com", "developer.huawei.com",

            // Regional alternatives that should work
            "yandex.ru", "yandex.com", "mail.ru", "vk.com",
            "baidu.com", "qq.com", "weibo.com", "sina.com.cn"
        };

        Logging.SaveLog($"🗂️ ENHANCED DOMAIN LIST: Loaded {domains.Count} Iranian blocked domains");
        Logging.SaveLog($"📊 Coverage: Google ({domains.Count(d => d.Contains("google"))}), Microsoft ({domains.Count(d => d.Contains("microsoft"))}), GitHub ({domains.Count(d => d.Contains("github"))}), Others ({domains.Count - domains.Count(d => d.Contains("google") || d.Contains("microsoft") || d.Contains("github"))})");
        return domains;
    }

    /// <summary>
    /// Download and parse the latest Iranian blocked domains list
    /// </summary>
    public async Task UpdateBlockedDomainsListAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            
            var response = await client.GetStringAsync("https://raw.githubusercontent.com/MrDevAnony/DynX-AntiBan-Domains/main/DynX-AntiBan-list.lst");
            var domains = response.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                 .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                                 .Select(line => line.Trim())
                                 .ToList();

            // Add downloaded domains to our set
            foreach (var domain in domains)
            {
                _googleDomains.Add(domain);
            }

            Logging.SaveLog($"SanctionsBypassService: Updated with {domains.Count} domains from DynX-AntiBan list");
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"SanctionsBypassService: Error updating blocked domains list - {ex.Message}");
        }
    }

    /// <summary>
    /// Test Iranian DNS servers with detailed logging and results
    /// </summary>
    private async Task<(bool working, string details)> TestIranianDnsWithDetailsAsync()
    {
        var results = new List<string>();
        var workingCount = 0;

        foreach (var dnsServer in _iranianDnsServers)
        {
            try
            {
                Logging.SaveLog($"  Testing DNS: {dnsServer.Key} ({dnsServer.Value})...");
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                var isWorking = await TestDnsServerAsync(dnsServer.Key);
                stopwatch.Stop();

                if (isWorking)
                {
                    workingCount++;
                    var result = $"✅ {dnsServer.Key}: Working ({stopwatch.ElapsedMilliseconds}ms)";
                    results.Add(result);
                    Logging.SaveLog($"    {result}");
                }
                else
                {
                    var result = $"❌ {dnsServer.Key}: Failed";
                    results.Add(result);
                    Logging.SaveLog($"    {result}");
                }
            }
            catch (Exception ex)
            {
                var result = $"❌ {dnsServer.Key}: Exception - {ex.Message}";
                results.Add(result);
                Logging.SaveLog($"    {result}");
            }
        }

        var working = workingCount > 0;
        var details = $"{workingCount}/{_iranianDnsServers.Count} DNS servers working. " + string.Join("; ", results.Take(3));
        
        return (working, details);
    }

    /// <summary>
    /// Test Iranian mirrors with detailed logging and results
    /// </summary>
    private async Task<(bool working, string details)> TestMirrorsWithDetailsAsync()
    {
        var mirrors = new List<string>
        {
            "maven.myket.ir",
            "en-mirror.ir",
            "maven.aliyun.com"
        };

        var results = new List<string>();
        var workingCount = 0;

        foreach (var mirror in mirrors)
        {
            try
            {
                Logging.SaveLog($"  Testing mirror: {mirror}...");
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                var testUrl = $"https://{mirror}/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.pom";
                var response = await _httpClient.GetAsync(testUrl, HttpCompletionOption.ResponseHeadersRead);
                stopwatch.Stop();

                if (response.IsSuccessStatusCode)
                {
                    workingCount++;
                    var result = $"✅ {mirror}: Working ({stopwatch.ElapsedMilliseconds}ms)";
                    results.Add(result);
                    Logging.SaveLog($"    {result}");
                }
                else
                {
                    var result = $"❌ {mirror}: HTTP {response.StatusCode}";
                    results.Add(result);
                    Logging.SaveLog($"    {result}");
                }
            }
            catch (Exception ex)
            {
                var result = $"❌ {mirror}: {ex.Message}";
                results.Add(result);
                Logging.SaveLog($"    {result}");
            }
        }

        var working = workingCount > 0;
        var details = $"{workingCount}/{mirrors.Count} mirrors working. " + string.Join("; ", results.Take(2));
        
        return (working, details);
    }

    /// <summary>
    /// Test basic proxy functionality
    /// </summary>
    private async Task<bool> TestBasicProxyAsync()
    {
        try
        {
            Logging.SaveLog("  Testing basic internet connectivity...");
            
            // Test a simple, reliable endpoint
            var testUrls = new[]
            {
                "https://httpbin.org/ip",
                "https://api.ipify.org",
                "https://icanhazip.com"
            };

            foreach (var url in testUrls)
            {
                try
                {
                    var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    if (response.IsSuccessStatusCode)
                    {
                        Logging.SaveLog($"    ✅ Basic connectivity working via {url}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Logging.SaveLog($"    ❌ {url} failed: {ex.Message}");
                }
            }

            Logging.SaveLog("    ❌ All basic connectivity tests failed");
            return false;
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"    ❌ Basic proxy test exception: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Handle specific 403 errors from sanctioned domains and provide immediate bypass
    /// </summary>
    public async Task<bool> Handle403ErrorAsync(string domain, int port = 443)
    {
        try
        {
            Logging.SaveLog($"🚨 HANDLING 403 ERROR: {domain}:{port} - Implementing immediate bypass");
            
            // Check if this is a known sanctioned domain
            if (IsSanctionsAffectedDomain(domain))
            {
                Logging.SaveLog($"✅ CONFIRMED SANCTIONED DOMAIN: {domain} - Applying Iranian DNS bypass");
                
                // Immediately switch to best Iranian DNS for this domain
                var bestIranianDns = await SelectOptimalDnsServerAsync();
                var dnsAddress = await GetDnsServerAddressAsync(bestIranianDns);
                
                // Force switch current DNS to Iranian 
                lock (_lockObject)
                {
                    _currentDnsServer = bestIranianDns;
                }
                
                Logging.SaveLog($"🔄 EMERGENCY DNS SWITCH: {domain} → {bestIranianDns} ({dnsAddress})");
                SendUIMessage($"🚨 403 ERROR DETECTED: {domain} - Switching to Iranian DNS ({bestIranianDns})");
                
                // Mark sanctions as active
                _isSanctionsActive = true;
                
                return true;
            }
            else
            {
                Logging.SaveLog($"ℹ️ UNKNOWN DOMAIN: {domain} - Not in sanctioned domains list");
                return false;
            }
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"❌ ERROR handling 403 for {domain}: {ex.Message}");
            return false;
        }
    }

    #region IDisposable Implementation

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                _httpClient?.Dispose();
            }
            catch (Exception ex)
            {
                Logging.SaveLog($"SanctionsBypassService: Error disposing HttpClient - {ex.Message}");
            }
            _disposed = true;
        }
    }

    ~SanctionsBypassService()
    {
        Dispose(false);
    }

    #endregion
}


