using System.Net;
using System.Net.Http;
using ServiceLib.Common;

namespace ServiceLib.Services.CoreConfig;

public class TransparentMirrorService
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, string> _mirrorMappings;
    private bool _isEnabled;
    private string _primaryMirror = "maven.myket.ir";
    private List<string> _fallbackMirrors = new();
    private bool _enableFallbackToGoogle = false;

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
            Logging.SaveLog("TransparentMirrorService: Enabling transparent mirroring for Iranian developers");

            // Test and select the best working mirrors
            var workingMirrors = await TestAndSelectBestMirrorsAsync();
            
            if (workingMirrors.Count == 0)
            {
                Logging.SaveLog("TransparentMirrorService: No working mirrors found, enabling fallback to Google repos with Iranian DNS");
                _enableFallbackToGoogle = true;
            }
            else
            {
                Logging.SaveLog($"TransparentMirrorService: Found {workingMirrors.Count} working mirrors, transparent mirroring enabled");
                _primaryMirror = workingMirrors.First();
                _fallbackMirrors = workingMirrors.Skip(1).ToList();
            }

            // The actual DNS and routing configuration is applied by V2rayDnsService
            Logging.SaveLog("TransparentMirrorService: Transparent mirroring configuration completed");
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"TransparentMirrorService: Error enabling transparent mirroring - {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Test all available mirrors and select the best performing ones
    /// </summary>
    private async Task<List<string>> TestAndSelectBestMirrorsAsync()
    {
        var mirrors = new List<(string Name, long ResponseTime)>();
        var testUrls = new Dictionary<string, string>
        {
            ["maven.myket.ir"] = "https://maven.myket.ir/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.pom",
            ["en-mirror.ir"] = "https://en-mirror.ir/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.pom",
            ["maven.aliyun.com"] = "https://maven.aliyun.com/repository/central/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.pom",
            ["mirrors.huaweicloud.com"] = "https://mirrors.huaweicloud.com/repository/maven/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.pom",
            ["mirrors.cloud.tencent.com"] = "https://mirrors.cloud.tencent.com/nexus/repository/maven-public/androidx/appcompat/appcompat/1.4.2/appcompat-1.4.2.pom"
        };

        foreach (var mirror in testUrls)
        {
            try
            {
                Logging.SaveLog($"Testing mirror: {mirror.Key}");
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                var response = await _httpClient.GetAsync(mirror.Value, HttpCompletionOption.ResponseHeadersRead);
                stopwatch.Stop();

                if (response.IsSuccessStatusCode)
                {
                    mirrors.Add((mirror.Key, stopwatch.ElapsedMilliseconds));
                    Logging.SaveLog($"Mirror {mirror.Key}: ✅ Working ({stopwatch.ElapsedMilliseconds}ms)");
                }
                else
                {
                    Logging.SaveLog($"Mirror {mirror.Key}: ❌ Failed with {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Logging.SaveLog($"Mirror {mirror.Key}: ❌ Exception: {ex.Message}");
            }
        }

        // Sort by response time (fastest first)
        var workingMirrors = mirrors
            .OrderBy(m => m.ResponseTime)
            .Select(m => m.Name)
            .ToList();

        if (workingMirrors.Count > 0)
        {
            Logging.SaveLog($"Best mirrors (sorted by speed): {string.Join(" → ", workingMirrors)}");
        }
        else
        {
            Logging.SaveLog("⚠️ No mirrors are working, will use fallback to Google repos with Iranian DNS");
        }

        return workingMirrors;
    }

    /// <summary>
    /// Get the current primary mirror for use by other services
    /// </summary>
    public string GetPrimaryMirror() => _primaryMirror;

    /// <summary>
    /// Get all fallback mirrors
    /// </summary>
    public List<string> GetFallbackMirrors() => _fallbackMirrors;

    /// <summary>
    /// Check if fallback to Google repos is enabled
    /// </summary>
    public bool ShouldFallbackToGoogle() => _enableFallbackToGoogle;
}
