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
            { "shecan-primary", "178.22.122.100" },
            { "shecan-secondary", "185.51.200.2" },
            { "radar-primary", "10.202.10.10" },
            { "radar-secondary", "10.202.10.11" },
            { "shelter-primary", "94.103.125.157" },
            { "shelter-secondary", "94.103.125.158" },
            { "electro-primary", "78.157.42.100" },
            { "electro-secondary", "78.157.42.101" },
            { "403-primary", "10.202.10.202" },
            { "403-secondary", "10.202.10.102" },
            { "begzar-primary", "185.55.226.26" },
            { "begzar-secondary", "185.55.225.25" }
        };

        _googleDomains = new HashSet<string>
        {
            "gradle.org",
            "services.gradle.org",
            "maven.google.com",
            "dl.google.com",
            "developer.android.com",
            "firebase.google.com",
            "firebase.googleapis.com",
            "firebasestorage.googleapis.com",
            "googleapis.com",
            "fonts.googleapis.com",
            "ajax.googleapis.com",
            "developers.google.com",
            "maven.myket.ir",
            "en-mirror.ir",
            "maven.aliyun.com",
            "mirrors.huaweicloud.com",
            "mirrors.cloud.tencent.com"
        };

        _currentDnsServer = "google";
        _isSanctionsActive = false;
    }

    /// <summary>
    /// Check if sanctions are active by testing Google domains
    /// </summary>
    public async Task<bool> CheckSanctionsStatusAsync()
    {
        try
        {
            var testUrls = new[]
            {
                "https://dl.google.com/dl/android/studio/ide-zips/4.2.2.0/android-studio-2021.2.1.16-windows.zip",
                "https://maven.google.com/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.aar",
                "https://developer.android.com/studio",
                "https://gradle.org/releases/",
                "https://maven.myket.ir/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.aar",
                "https://en-mirror.ir/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.aar"
            };

            int forbiddenCount = 0;
            foreach (var url in testUrls)
            {
                try
                {
                    var response = await _httpClient.GetAsync(url);
                    if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        forbiddenCount++;
                    }
                }
                catch (HttpRequestException)
                {
                    // Network error, might be sanctions
                    forbiddenCount++;
                }
                catch (TaskCanceledException)
                {
                    // Timeout, might be sanctions
                    forbiddenCount++;
                }
            }

            // If more than 50% of requests return 403, sanctions are likely active
            _isSanctionsActive = forbiddenCount > testUrls.Length / 2;
            return _isSanctionsActive;
        }
        catch (Exception ex)
        {
            Logging.SaveLog("SanctionsBypassService", ex);
            return false;
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

            Logging.SaveLog("SanctionsBypassService", $"Switched DNS server to: {_currentDnsServer}");
            return _currentDnsServer;
        }
    }

    /// <summary>
    /// Test if a DNS server is working
    /// </summary>
    public async Task<bool> TestDnsServerAsync(string dnsServer)
    {
        try
        {
            if (!_iranianDnsServers.ContainsKey(dnsServer))
            {
                return false;
            }

            var dnsIp = _iranianDnsServers[dnsServer];
            var testUrl = $"https://dns.google/resolve?name=google.com&type=A&dns={dnsIp}";

            var response = await _httpClient.GetAsync(testUrl);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
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
}
