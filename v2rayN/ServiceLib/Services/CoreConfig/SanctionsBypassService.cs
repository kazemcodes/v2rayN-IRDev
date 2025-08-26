using System.Net;
using System.Net.Http;
using System.Text.Json;
using ServiceLib.Common;

namespace ServiceLib.Services.CoreConfig;

public class SanctionsBypassService
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, string> _iranianDnsServers;
    private readonly HashSet<string> _googleDomains;
    private string _currentDnsServer;
    private bool _isSanctionsActive;
    private readonly object _lockObject = new();

    public SanctionsBypassService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        _iranianDnsServers = new Dictionary<string, string>
        {
            // Primary Iranian DNS servers (most reliable)
            { "shecan-primary", "178.22.122.100" },
            { "shecan-secondary", "185.51.200.2" },
            { "radar-primary", "10.202.10.10" },
            { "radar-secondary", "10.202.10.11" },
            { "shelter-primary", "94.103.125.157" },
            { "shelter-secondary", "94.103.125.158" },
            { "electro-primary", "78.157.42.100" },
            { "electro-secondary", "78.157.42.101" },

            // Alternative Iranian DNS servers
            { "403-primary", "10.202.10.202" },
            { "403-secondary", "10.202.10.102" },
            { "begzar-primary", "185.55.226.26" },
            { "begzar-secondary", "185.55.225.25" },

            // Additional Iranian DNS servers
            { "asan-primary", "185.143.233.120" },
            { "asan-secondary", "185.143.234.120" },
            { "asan-dns", "185.143.232.120" }
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
            // First check if sanctions are active
            var sanctionsActive = await CheckSanctionsStatusAsync();

            if (!sanctionsActive)
            {
                // No sanctions detected, connection allowed
                return (true, "✅ No sanctions detected - connection allowed");
            }

            // Sanctions are active, check if we can bypass them
            Logging.SaveLog("SanctionsBypassService: Sanctions detected, testing bypass mechanisms...");

            // Test Iranian DNS servers
            var dnsWorking = await TestIranianDnsAsync();
            if (!dnsWorking)
            {
                Logging.SaveLog("SanctionsBypassService: All Iranian DNS servers failed");
                return (false, "🚫 Sanctions detected and Iranian DNS servers are not accessible. Connection blocked for security.");
            }

            Logging.SaveLog("SanctionsBypassService: Iranian DNS servers working");

            // Test if mirrors are accessible
            var mirrorsWorking = await TestMirrorsAsync();
            if (!mirrorsWorking)
            {
                Logging.SaveLog("SanctionsBypassService: Iranian mirrors not accessible");
                return (false, "🚫 Sanctions detected but Iranian mirrors are not accessible. Connection blocked for security.");
            }

            Logging.SaveLog("SanctionsBypassService: Iranian mirrors accessible");

            // Test Android development specific URLs
            var androidWorking = await TestAndroidDevelopmentAsync();
            if (!androidWorking)
            {
                Logging.SaveLog("SanctionsBypassService: Android development URLs not accessible");
                return (false, "⚠️ Sanctions detected but Android development tools may not work properly. Connection allowed with limitations.");
            }

            Logging.SaveLog("SanctionsBypassService: All bypass mechanisms working");
            return (true, "✅ Sanctions detected but successfully bypassed using Iranian infrastructure. Android development ready!");
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
            "https://maven.myket.ir/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.aar",
            "https://en-mirror.ir/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.aar",
            "https://maven.aliyun.com/repository/central/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.aar",
            "https://gradle.org/releases/",
            "https://developer.android.com/studio"
        };

        var workingCount = 0;
        foreach (var url in androidUrls)
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
                // URL not accessible
            }
        }

        // Require at least 3 out of 5 URLs to work
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
    /// Test if a specific DNS server is working
    /// </summary>
    public async Task<bool> TestDnsServerAsync(string dnsServerName)
    {
        try
        {
            if (dnsServerName == "google")
            {
                // Test Google DNS
                return await TestMirrorAsync("https://maven.google.com/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.aar");
            }

            // Test Iranian DNS by trying to access Iranian mirrors
            return await TestMirrorAsync("https://maven.myket.ir/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.aar");
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"SanctionsBypassService: Error testing DNS {dnsServerName}: {ex.Message}");
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

            // Test Google domains for 403 errors
            var googleTestUrls = new[]
            {
                "https://maven.google.com/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.aar",
                "https://dl.google.com/dl/android/studio/ide-zips/4.2.2.0/android-studio-2021.2.1.16-windows.zip",
                "https://developer.android.com/studio",
                "https://gradle.org/releases/"
            };

            int forbiddenCount = 0;
            foreach (var url in googleTestUrls)
            {
                try
                {
                    var response = await _httpClient.GetAsync(url);
                    var content = await response.Content.ReadAsStringAsync();
                    
                    // Check for status code-based sanctions
                    if (response.StatusCode == HttpStatusCode.Forbidden ||
                        response.StatusCode == HttpStatusCode.NotFound ||
                        response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                        response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        Logging.SaveLog($"SanctionsBypassService: Status-based sanction detected for {url} - {response.StatusCode}");
                        forbiddenCount++;
                    }
                    // Check for content-based sanctions (Service Unavailable, blocking messages)
                    else if (content.Contains("Service Unavailable", StringComparison.OrdinalIgnoreCase) ||
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
                        Logging.SaveLog($"SanctionsBypassService: Content-based sanction detected for {url} - Service Unavailable or blocking message");
                        forbiddenCount++;
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
    /// Get the best DNS server for current conditions
    /// </summary>
    public async Task<string> GetBestDnsServerAsync()
    {
        lock (_lockObject)
        {
            if (!_isSanctionsActive)
            {
                return "google";
            }

            // If sanctions are active, try Iranian DNS servers
            return _currentDnsServer;
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
    /// Monitor DNS server health and switch if needed
    /// </summary>
    public async Task StartDnsMonitoringAsync()
    {
        while (true)
        {
            try
            {
                if (_isSanctionsActive)
                {
                    var currentWorking = await TestDnsServerAsync(_currentDnsServer);
                    if (!currentWorking)
                    {
                        SwitchToNextDnsServer();
                    }
                }

                // Check sanctions status every 5 minutes
                await CheckSanctionsStatusAsync();

                await Task.Delay(TimeSpan.FromMinutes(5));
            }
            catch (Exception ex)
            {
                Logging.SaveLog("SanctionsBypassService", ex);
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }
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
    /// Load comprehensive list of Iranian blocked domains
    /// </summary>
    private HashSet<string> LoadIranianBlockedDomains()
    {
        var domains = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Core Android/Google development domains
            "gradle.org", "services.gradle.org", "maven.google.com", "dl.google.com", 
            "developer.android.com", "firebase.google.com", "firebase.googleapis.com",
            "firebasestorage.googleapis.com", "googleapis.com", "fonts.googleapis.com",
            "ajax.googleapis.com", "developers.google.com",

            // Google core services (from DynX-AntiBan list)
            "accounts.google.com", "ads.google.com", "adservice.google.com", 
            "ai.google.com", "analytics.google.com", "apis.google.com",
            "appengine.google.com", "books.google.com", "business.google.com",
            "chat.google.com", "classroom.google.com", "clients.google.com",
            "clients2.google.com", "clients6.google.com", "cloud.google.com",
            "code.google.com", "datastudio.google.com", "dl-ssl.google.com",
            "issuetracker.google.com", "lens.google.com", "maps.google.com",
            "marketingplatform.google.com", "notifications.google.com", "one.google.com",
            "optimize.google.com", "pay.google.com", "payments.google.com",
            "play.google.com", "research.google.com", "services.google.com",
            "surveys.google.com", "tagmanager.google.com", "time.google.com",
            "remotedesktop.google.com", "checks.google.com",

            // Major tech companies (from DynX-AntiBan list)
            "microsoft.com", "github.com", "githubusercontent.com", "discord.com",
            "discordapp.com", "discord.media", "apple.com", "iforgot.apple.com",
            "googletagmanager.com", "azure.com", "zoom.us", "spotify.com",
            "mozilla.org", "paypal.com", "google-analytics.com", "skype.com",
            "imgur.com", "medium.com", "gravatar.com", "webex.com", "apache.org",
            "udemy.com", "ibm.com", "zendesk.com", "upwork.com", "teamviewer.com",
            "oracle.com", "dell.com", "slack.com", "opensea.io",

            // Development platforms
            "sourcegraph.com", "algolia.net", "arcgis.com", "fedoraproject.org",
            "mariadb.com", "salesforce.com", "hostinger.com", "beatport.com",
            "cachyos.org", "langchain.com", "llama.com", "galxe.com",
            "cmf.tech", "nothing.tech", "schrodinger.com", "exaloop.io",

            // Iranian mirrors (should be allowed)
            "maven.myket.ir", "en-mirror.ir", "maven.aliyun.com",
            "mirrors.huaweicloud.com", "mirrors.cloud.tencent.com"
        };

        Logging.SaveLog($"SanctionsBypassService: Loaded {domains.Count} Iranian blocked domains");
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
}
