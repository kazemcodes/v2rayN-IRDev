using System.Net;
using System.Net.Http;
using ServiceLib.Common;

namespace ServiceLib.Services.CoreConfig;

public class TransparentMirrorService
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, string> _mirrorMappings;
    private bool _isEnabled;

    public TransparentMirrorService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        _mirrorMappings = new Dictionary<string, string>
        {
            // Maven Google → Myket
            { "maven.google.com", "https://maven.myket.ir" },
            { "dl.google.com", "https://maven.myket.ir" },

            // Maven Central → Myket
            { "repo.maven.apache.org", "https://maven.myket.ir" },
            { "repo1.maven.org", "https://maven.myket.ir" },

            // Sonatype → Myket
            { "central.sonatype.org", "https://maven.myket.ir" },
            { "oss.sonatype.org", "https://maven.myket.ir" },

            // Gradle → EN Mirror
            { "gradle.org", "https://en-mirror.ir" },
            { "services.gradle.org", "https://en-mirror.ir" },
            { "plugins.gradle.org", "https://en-mirror.ir" },

            // JCenter → Myket
            { "jcenter.bintray.com", "https://maven.myket.ir" },
            { "bintray.com", "https://maven.myket.ir" }
        };

        _isEnabled = true;
    }

    /// <summary>
    /// Check if transparent mirroring should be enabled
    /// </summary>
    public async Task<bool> ShouldEnableTransparentMirroringAsync()
    {
        if (!_isEnabled)
        {
            return false;
        }

        try
        {
            // Test if Iranian mirrors are accessible
            var testUrls = new[]
            {
                "https://maven.myket.ir",
                "https://en-mirror.ir"
            };

            foreach (var url in testUrls)
            {
                try
                {
                    var response = await _httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                }
                catch
                {
                    // Continue to next mirror
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Logging.SaveLog("TransparentMirrorService", ex);
            return false;
        }
    }

    /// <summary>
    /// Get mirror URL for original repository
    /// </summary>
    public string GetMirrorUrl(string originalUrl)
    {
        try
        {
            var uri = new Uri(originalUrl);
            var host = uri.Host.ToLower();

            // Check if we have a mirror mapping for this host
            if (_mirrorMappings.TryGetValue(host, out var mirrorBaseUrl))
            {
                // Replace the original host with mirror host and keep the path
                var mirrorUri = new Uri(mirrorBaseUrl);
                var newUrl = $"{mirrorUri.Scheme}://{mirrorUri.Host}{uri.PathAndQuery}";
                return newUrl;
            }

            // If no mapping found, return original URL
            return originalUrl;
        }
        catch
        {
            return originalUrl;
        }
    }

    /// <summary>
    /// Get all mirror mappings
    /// </summary>
    public Dictionary<string, string> GetMirrorMappings()
    {
        return new Dictionary<string, string>(_mirrorMappings);
    }

    /// <summary>
    /// Enable or disable transparent mirroring
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;
        Logging.SaveLog($"TransparentMirrorService: Transparent mirroring {(enabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// Test if a mirror is working
    /// </summary>
    public async Task<bool> TestMirrorAsync(string mirrorUrl)
    {
        try
        {
            var response = await _httpClient.GetAsync(mirrorUrl);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get the best working mirror for a domain
    /// </summary>
    public async Task<string> GetBestMirrorAsync(string domain)
    {
        if (_mirrorMappings.TryGetValue(domain, out var mirrorUrl))
        {
            if (await TestMirrorAsync(mirrorUrl))
            {
                return mirrorUrl;
            }
        }

        // If primary mirror fails, try alternatives
        var alternatives = new Dictionary<string, List<string>>
        {
            { "maven.google.com", new List<string> { "https://en-mirror.ir", "https://maven.aliyun.com/repository/central" } },
            { "repo.maven.apache.org", new List<string> { "https://maven.aliyun.com/repository/central", "https://maven.myket.ir" } },
            { "gradle.org", new List<string> { "https://maven.myket.ir", "https://maven.aliyun.com/repository/central" } }
        };

        if (alternatives.TryGetValue(domain, out var altMirrors))
        {
            foreach (var altMirror in altMirrors)
            {
                if (await TestMirrorAsync(altMirror))
                {
                    return altMirror;
                }
            }
        }

        return string.Empty;
    }

    public async Task EnableTransparentMirroringAsync()
    {
        try
        {
            // This would enable transparent mirroring in the VPN configuration
            Logging.SaveLog("TransparentMirrorService: Enabling transparent mirroring");

            // TODO: Implement actual transparent mirroring setup
            // This could involve:
            // 1. Setting up DNS rules for repository domains
            // 2. Configuring routing rules for mirrors
            // 3. Enabling host file modifications

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"TransparentMirrorService: Error enabling transparent mirroring - {ex.Message}");
            throw;
        }
    }
}
