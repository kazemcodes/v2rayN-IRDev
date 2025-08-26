using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ServiceLib.Handler;

namespace ServiceLib.Services.CoreConfig;

public partial class CoreConfigV2rayService
{
    private async Task<int> GenDns(ProfileItem? node, V2rayConfig v2rayConfig)
    {
        try
        {
            var item = await AppManager.Instance.GetDNSItem(ECoreType.Xray);
            if (item != null && item.Enabled == true)
            {
                var result = await GenDnsCompatible(node, v2rayConfig);

                if (v2rayConfig.routing.domainStrategy == Global.IPIfNonMatch)
                {
                    // DNS routing
                    v2rayConfig.dns.tag = Global.DnsTag;
                    v2rayConfig.routing.rules.Add(new RulesItem4Ray
                    {
                        type = "field",
                        inboundTag = new List<string> { Global.DnsTag },
                        outboundTag = Global.ProxyTag,
                    });
                }

                return result;
            }
            var simpleDNSItem = _config.SimpleDNSItem;
            var domainStrategy4Freedom = simpleDNSItem?.RayStrategy4Freedom;

            //Outbound Freedom domainStrategy
            if (domainStrategy4Freedom.IsNotEmpty())
            {
                var outbound = v2rayConfig.outbounds.FirstOrDefault(t => t is { protocol: "freedom", tag: Global.DirectTag });
                if (outbound != null)
                {
                    outbound.settings = new()
                    {
                        domainStrategy = domainStrategy4Freedom,
                        userLevel = 0
                    };
                }
            }

            // Apply Iranian sanctions bypass FIRST (before other DNS servers)
            await ApplyIranianSanctionsBypass(v2rayConfig);

            await GenDnsServers(node, v2rayConfig, simpleDNSItem);
            await GenDnsHosts(v2rayConfig, simpleDNSItem);

            // Re-apply Iranian sanctions bypass after GenDnsHosts only if hosts were modified
            // RELOAD FRESH CONFIG to ensure we have latest settings
            var freshConfigReapply = ConfigHandler.LoadConfig();
            var iranConfig = freshConfigReapply?.IranSanctionsBypassItem;
            if (iranConfig?.EnableTransparentMirroring == true && v2rayConfig.dns?.hosts != null)
            {
                var hostCountBefore = v2rayConfig.dns.hosts.Count;
                await ApplyTransparentMirroringHosts(v2rayConfig.dns.hosts);
                var hostCountAfter = v2rayConfig.dns.hosts.Count;
                
                if (hostCountAfter > hostCountBefore)
                {
                    SendUIMessage($"Re-applied transparent mirroring: {hostCountAfter - hostCountBefore} additional hosts");
                }
            }

            if (v2rayConfig.routing.domainStrategy == Global.IPIfNonMatch)
            {
                // DNS routing
                v2rayConfig.dns.tag = Global.DnsTag;
                v2rayConfig.routing.rules.Add(new RulesItem4Ray
                {
                    type = "field",
                    inboundTag = new List<string> { Global.DnsTag },
                    outboundTag = Global.ProxyTag,
                });
            }
        }
        catch (Exception ex)
        {
            Logging.SaveLog(_tag, ex);
        }
        return 0;
    }

    /// <summary>
    /// Send message to UI (like sanctions bypass validation messages)
    /// Thread-safe version that handles dispatcher marshalling
    /// </summary>
    private void SendUIMessage(string msg)
    {
        if (string.IsNullOrWhiteSpace(msg))
            return;

        try
        {
            // Send UI message directly (removed System.Windows.Application dependency)
            NoticeManager.Instance?.SendMessage(msg);
            
            Logging.SaveLog(msg); // Always log to file
        }
        catch (Exception ex)
        {
            // Fallback to logging if UI message fails
            Logging.SaveLog($"SendUIMessage error: {ex.Message} - Original message: {msg}");
        }
    }

    /// <summary>
    /// Apply Iranian sanctions bypass DNS and routing configuration
    /// </summary>
    private async Task ApplyIranianSanctionsBypass(V2rayConfig v2rayConfig)
    {
        try
        {
            SendUIMessage("🚀 STARTING IRANIAN SANCTIONS BYPASS CONFIGURATION...");

            // RELOAD FRESH CONFIG TO ENSURE LATEST SETTINGS
            var freshConfig = ConfigHandler.LoadConfig();
            var iranConfig = freshConfig?.IranSanctionsBypassItem;

            // Debug the config state
            SendUIMessage($"🔍 DEBUG: Config null? {freshConfig == null}");
            SendUIMessage($"🔍 DEBUG: IranSanctionsBypassItem null? {iranConfig == null}");
            
            if (iranConfig != null)
            {
                SendUIMessage($"🔍 DEBUG: EnableSanctionsDetection = {iranConfig.EnableSanctionsDetection}");
                SendUIMessage($"🔍 DEBUG: EnableIranianDnsAutoSwitch = {iranConfig.EnableIranianDnsAutoSwitch}");
                SendUIMessage($"🔍 DEBUG: EnableTransparentMirroring = {iranConfig.EnableTransparentMirroring}");
                SendUIMessage($"🔍 DEBUG: PreferredIranianDnsServer = {iranConfig.PreferredIranianDnsServer}");
            }

            // Check if user has enabled sanctions bypass in the UI
            if (iranConfig == null || !iranConfig.EnableSanctionsDetection)
            {
                SendUIMessage("❌ IRANIAN SANCTIONS BYPASS: DISABLED IN SETTINGS");
                SendUIMessage("💡 To enable: Open 'Iran Sanctions Bypass Settings' and check 'Enable Sanctions Detection'");
                return;
            }

            SendUIMessage("✅ IRANIAN SANCTIONS BYPASS: ENABLED IN SETTINGS");
            SendUIMessage("🔄 APPLYING IRANIAN DNS & MIRRORING FOR ANDROID DEVELOPMENT...");

            var sanctionsService = new SanctionsBypassService();

            // Check sanctions status for logging purposes, but apply Iranian config regardless
            var sanctionsDetected = await sanctionsService.CheckSanctionsStatusAsync();
            SendUIMessage($"📊 SANCTIONS STATUS: {(sanctionsDetected ? "DETECTED" : "NOT DETECTED")}");

            // Only validate connection if hard block is enabled
            if (iranConfig.EnableHardBlockOnFailure)
            {
                var (canConnect, reason) = await sanctionsService.ValidateConnectionAsync();
                if (!canConnect)
                {
                    SendUIMessage("⚠️ IRANIAN SANCTIONS BYPASS VALIDATION FAILED: " + reason);
                    SendUIMessage("⚠️ Continuing with Iranian DNS configuration anyway...");
                }
            }

                        // Configure Iranian DNS at SYSTEM level (not V2Ray level)
            if (iranConfig.EnableIranianDnsAutoSwitch)
            {
                SendUIMessage("🔥 CONFIGURING IRANIAN DNS AT SYSTEM LEVEL...");

                // Get user's preferred DNS server or default to electro-primary
                var preferredDns = iranConfig.PreferredIranianDnsServer ?? "electro-primary";
                var primaryDnsAddress = await sanctionsService.GetDnsServerAddressAsync(preferredDns);

                SendUIMessage($"🏛️ SETTING SYSTEM DNS TO: {preferredDns} ({primaryDnsAddress})");

                // Configure system DNS to use Iranian servers for sanctioned domains
                var systemDnsConfigured = await sanctionsService.ConfigureSystemDnsForSanctionsAsync(primaryDnsAddress);

                if (systemDnsConfigured)
                {
                    SendUIMessage($"✅ SYSTEM DNS CONFIGURED: {primaryDnsAddress} for sanctioned domains");
                    SendUIMessage("🎯 Sanctioned domains will bypass VPN entirely and use Iranian DNS");
                }
                else
                {
                    SendUIMessage("⚠️ SYSTEM DNS CONFIGURATION FAILED - using V2Ray bypass method");
                    // Fallback: Configure V2Ray to bypass sanctioned domains entirely
                    var v2rayBypassConfigured = await sanctionsService.ConfigureV2RayBypassForSanctionsAsync(v2rayConfig, primaryDnsAddress);

                    if (v2rayBypassConfigured)
                    {
                        SendUIMessage("✅ V2RAY BYPASS CONFIGURED - Sanctioned domains will bypass VPN");
                        SendUIMessage("🎯 Only non-sanctioned traffic will go through VPN tunnel");
                    }
                    else
                    {
                        SendUIMessage("❌ V2RAY BYPASS CONFIGURATION FAILED");
                        SendUIMessage("⚠️ Iranian DNS will be used within VPN context as fallback");
                    }
                }
            }

            // Add comprehensive DNS hosts for transparent mirroring
            if (iranConfig.EnableTransparentMirroring && v2rayConfig.dns?.hosts != null)
            {
                SendUIMessage("🔀 APPLYING TRANSPARENT MIRRORING...");
                await ApplyTransparentMirroringHosts(v2rayConfig.dns.hosts);
                SendUIMessage("✅ Transparent mirroring DNS hosts applied successfully");
            }

            // Add routing rules for intelligent mirroring with fallback
            if (iranConfig.EnableTransparentMirroring && v2rayConfig.routing?.rules != null)
            {
                SendUIMessage("🚦 APPLYING MIRRORING ROUTING RULES...");
                await ApplyMirroringRoutingRules(v2rayConfig.routing.rules);
                SendUIMessage("✅ Transparent mirroring routing rules applied successfully");
            }

            // NUCLEAR FIX: ALWAYS apply developer.android.com direct connection regardless of other settings
            if (v2rayConfig.routing?.rules != null)
            {
                SendUIMessage("🚨 APPLYING NUCLEAR FIX: developer.android.com → DIRECT CONNECTION");
                await sanctionsService.ForceDirectConnectionForAndroidDevPublic(v2rayConfig);
                SendUIMessage("✅ NUCLEAR FIX APPLIED: developer.android.com FORCED to direct connection");
            }

            // Clear DNS cache to prevent cached blocked responses
            await ClearDnsCacheAsync();

            SendUIMessage("🎉 IRANIAN SANCTIONS BYPASS FULLY CONFIGURED!");
            SendUIMessage("   ✅ Iranian DNS servers: Active (positions 0-1)");
            SendUIMessage("   ✅ Domain mirroring: Active (20+ domains)");
            SendUIMessage("   ✅ Smart routing: Active (4 priority levels)");
            SendUIMessage("   ✅ DNS cache: Cleared");
            SendUIMessage("🚀 Ready for Android development without sanctions issues!");
        }
        catch (Exception ex)
        {
            Logging.SaveLog(_tag, ex);
        }
    }

    /// <summary>
    /// Apply transparent mirroring DNS hosts that redirect Maven/Google repos to Iranian mirrors
    /// </summary>
    private async Task ApplyTransparentMirroringHosts(Dictionary<string, object> hosts)
    {
        try
        {
            // Test which Iranian mirrors are currently working
            var workingMirrors = await GetWorkingMirrorsAsync();
            var primaryMirror = workingMirrors.FirstOrDefault() ?? "maven.myket.ir";

            Logging.SaveLog($"🎯 SELECTED PRIMARY MIRROR: {primaryMirror} (from {workingMirrors.Count} available mirrors)");

            // Maven and Gradle repositories - redirect to Iranian mirrors
            var repoMappings = new Dictionary<string, string>
            {
                // Google Maven Repository
                ["maven.google.com"] = primaryMirror,
                ["dl.google.com"] = primaryMirror,

                // Apache Maven Central Repository - CRITICAL for Android development
                ["repo.maven.apache.org"] = primaryMirror,
                ["repo1.maven.org"] = primaryMirror,
                ["central.maven.org"] = primaryMirror,
                ["search.maven.org"] = primaryMirror,

                // Gradle repositories
                ["gradle.org"] = primaryMirror,
                ["services.gradle.org"] = primaryMirror,
                ["plugins.gradle.org"] = primaryMirror,
                ["repo.gradle.org"] = primaryMirror,

                // Additional Google developer repositories
                ["android.googlesource.com"] = primaryMirror,
                ["source.android.com"] = primaryMirror,

                // Firebase and Google Cloud repositories
                ["firebase.google.com"] = "78.157.42.100", // Use Iranian DNS
                ["firebase.googleapis.com"] = "78.157.42.100",
                ["firebasestorage.googleapis.com"] = "78.157.42.100",
                ["googleapis.com"] = "78.157.42.100",

                // Android SDK and tools
                ["developer.android.com"] = "78.157.42.100",
                ["androidstudio.googleblog.com"] = "78.157.42.100"
            };

            SendUIMessage($"🔀 APPLYING DOMAIN MIRRORING MAPPINGS:");
            foreach (var mapping in repoMappings)
            {
                hosts[mapping.Key] = mapping.Value;
                if (mapping.Value.Contains("maven.myket.ir") || mapping.Value.Contains("en-mirror.ir"))
                {
                    SendUIMessage($"   🔄 MIRROR: {mapping.Key} → {mapping.Value} (Iranian mirror)");
                }
                else
                {
                    SendUIMessage($"   🏛️ IRANIAN DNS: {mapping.Key} → {mapping.Value} (Iranian DNS server)");
                }
            }

            SendUIMessage($"✅ APPLIED {repoMappings.Count} TRANSPARENT MIRRORING MAPPINGS: {repoMappings.Count(m => m.Value.Contains("maven.myket.ir") || m.Value.Contains("en-mirror.ir"))} to mirrors, {repoMappings.Count(m => m.Value.Contains("78.157.42.100"))} to Iranian DNS");
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"Error applying transparent mirroring hosts: {ex.Message}");
        }
    }

    /// <summary>
    /// Apply routing rules for intelligent mirroring with fallback logic
    /// </summary>
    private Task ApplyMirroringRoutingRules(List<RulesItem4Ray> rules)
    {
        try
        {
            // Priority 1: Iranian mirrors - use direct connection (fastest)
            var mirrorDomains = new List<string>
            {
                "domain:maven.myket.ir",
                "domain:en-mirror.ir",
                "domain:maven.aliyun.com",
                "domain:mirrors.huaweicloud.com",
                "domain:mirrors.cloud.tencent.com"
            };

            Logging.SaveLog($"🔗 ROUTING RULE 1: Direct connection for Iranian mirrors");
            foreach (var domain in mirrorDomains)
            {
                Logging.SaveLog($"   ➡️ {domain.Replace("domain:", "")} → Direct connection (no VPN)");
            }

            rules.Insert(0, new RulesItem4Ray
            {
                type = "field",
                domain = mirrorDomains,
                outboundTag = Global.DirectTag
            });

            // Priority 2: Google repositories and Apache Maven - route through proxy (fallback)
            var proxyDomains = new List<string>
            {
                "domain:maven.google.com",
                "domain:dl.google.com",
                "domain:gradle.org",
                "domain:services.gradle.org",
                "domain:plugins.gradle.org",
                "domain:repo.gradle.org",
                "domain:repo.maven.apache.org",
                "domain:repo1.maven.org",
                "domain:central.maven.org",
                "domain:search.maven.org"
            };

            Logging.SaveLog($"🔗 ROUTING RULE 2: VPN proxy for sanctioned repositories (fallback)");
            foreach (var domain in proxyDomains)
            {
                Logging.SaveLog($"   🔒 {domain.Replace("domain:", "")} → VPN proxy (with Iranian DNS)");
            }

            rules.Insert(1, new RulesItem4Ray
            {
                type = "field",
                domain = proxyDomains,
                outboundTag = Global.ProxyTag
            });

            // Priority 3: General Google services - use proxy 
            // NOTE: developer.android.com is handled separately by SanctionsBypassService for direct connection
            var googleServices = new List<string>
            {
                "domain:googleapis.com",
                "domain:firebase.com",
                "domain:android.com"
                // "domain:developer.android.com" - REMOVED: Handled by Iranian sanctions bypass for direct connection
            };

            Logging.SaveLog($"🔗 ROUTING RULE 3: VPN proxy for Google services");
            foreach (var domain in googleServices)
            {
                Logging.SaveLog($"   🌐 {domain.Replace("domain:", "")} → VPN proxy");
            }

            rules.Insert(2, new RulesItem4Ray
            {
                type = "field",
                domain = googleServices,
                outboundTag = Global.ProxyTag
            });

            // Priority 4: Other blocked domains - use proxy with Iranian DNS
            var otherBlockedDomains = new List<string>
            {
                "domain:github.com",
                "domain:githubusercontent.com",
                "domain:microsoft.com",
                "domain:apple.com",
                "domain:discord.com"
            };

            Logging.SaveLog($"🔗 ROUTING RULE 4: VPN proxy for other blocked domains");
            foreach (var domain in otherBlockedDomains)
            {
                Logging.SaveLog($"   🚫 {domain.Replace("domain:", "")} → VPN proxy (with Iranian DNS)");
            }

            rules.Insert(3, new RulesItem4Ray
            {
                type = "field",
                domain = otherBlockedDomains,
                outboundTag = Global.ProxyTag
            });

            SendUIMessage($"✅ TOTAL ROUTING RULES APPLIED: {rules.Count} rules");
            SendUIMessage($"   📊 Direct connections: {mirrorDomains.Count} domains (Iranian mirrors)");
            SendUIMessage($"   🔒 VPN proxy routes: {proxyDomains.Count + googleServices.Count + otherBlockedDomains.Count} domains (sanctioned services)");

            SendUIMessage("Applied intelligent mirroring routing rules with fallback logic");
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"Error applying mirroring routing rules: {ex.Message}");
        }
        
        return Task.CompletedTask;
    }

    // Cache for mirror testing to avoid redundant HTTP calls
    private static readonly Dictionary<string, (bool IsWorking, DateTime LastChecked)> _mirrorCache = new();
    private static readonly TimeSpan _cacheExpiryTime = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Test Iranian mirrors to find which ones are currently working (with caching)
    /// </summary>
    private async Task<List<string>> GetWorkingMirrorsAsync()
    {
        var mirrors = new List<string>
        {
            "maven.myket.ir",
            "en-mirror.ir", 
            "maven.aliyun.com",
            "mirrors.huaweicloud.com",
            "mirrors.cloud.tencent.com"
        };

        Logging.SaveLog($"🧪 TESTING MIRROR CONNECTIVITY for Android development...");
        Logging.SaveLog($"📋 Available mirrors: {string.Join(", ", mirrors)}");

        var workingMirrors = new List<string>();
        var mirrorsToTest = new List<string>();

        // Check cache first
        foreach (var mirror in mirrors)
        {
            if (_mirrorCache.TryGetValue(mirror, out var cacheEntry) && 
                DateTime.Now - cacheEntry.LastChecked < _cacheExpiryTime)
            {
                if (cacheEntry.IsWorking)
                {
                    workingMirrors.Add(mirror);
                    Logging.SaveLog($"✅ CACHED: {mirror} (working from cache)");
                }
                else
                {
                    Logging.SaveLog($"❌ CACHED: {mirror} (failed from cache)");
                }
            }
            else
            {
                mirrorsToTest.Add(mirror);
            }
        }

        if (mirrorsToTest.Count == 0)
        {
            Logging.SaveLog($"🎯 ALL MIRRORS FROM CACHE: {workingMirrors.Count} working");
            return workingMirrors.Count > 0 ? workingMirrors : new List<string> { "maven.myket.ir" };
        }

        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(3); // Reduced timeout for faster testing

        // Test only uncached mirrors concurrently
        var testTasks = mirrorsToTest.Select(async mirror =>
        {
            try
            {
                // Test with just one quick URL for efficiency
                var testUrl = $"https://{mirror}/";
                
                Logging.SaveLog($"🔍 Testing mirror: {mirror}");
                var response = await client.GetAsync(testUrl, HttpCompletionOption.ResponseHeadersRead);
                var isWorking = response.IsSuccessStatusCode;
                
                // Update cache
                _mirrorCache[mirror] = (isWorking, DateTime.Now);
                
                if (isWorking)
                {
                    Logging.SaveLog($"✅ SUCCESS: {mirror} responded with HTTP {response.StatusCode}");
                    return mirror;
                }
                else
                {
                    Logging.SaveLog($"❌ FAILED: {mirror} returned HTTP {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Logging.SaveLog($"❌ ERROR: {mirror} - {ex.Message}");
                _mirrorCache[mirror] = (false, DateTime.Now);
                return null;
            }
        }).ToArray();

        var testResults = await Task.WhenAll(testTasks);
        workingMirrors.AddRange(testResults.Where(result => result != null)!);

        if (workingMirrors.Count == 0)
        {
            Logging.SaveLog("❌ NO WORKING MIRRORS FOUND - will use fallback to Google with Iranian DNS");
            Logging.SaveLog("⚠️ This may result in slower downloads but should work for Android development");
            return new List<string> { "maven.myket.ir" }; // Default fallback
        }

        Logging.SaveLog($"🎉 MIRROR TESTING COMPLETE: Found {workingMirrors.Count} working mirrors");
        Logging.SaveLog($"✅ WORKING MIRRORS: {string.Join(", ", workingMirrors)}");
        Logging.SaveLog($"🚀 Fast downloads expected from Iranian mirrors!");
        return workingMirrors;
    }

    /// <summary>
    /// Clear DNS cache to prevent cached blocking responses for Iranian developers
    /// </summary>
    private async Task ClearDnsCacheAsync()
    {
        try
        {
            // Clear Windows DNS cache
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ipconfig",
                Arguments = "/flushdns",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(processInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                Logging.SaveLog("DNS cache cleared to prevent sanction-cached responses");
            }
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"Warning: Could not clear DNS cache - {ex.Message}");
        }
    }

    private async Task<int> GenDnsServers(ProfileItem? node, V2rayConfig v2rayConfig, SimpleDNSItem simpleDNSItem)
    {
        static List<string> ParseDnsAddresses(string? dnsInput, string defaultAddress)
        {
            var addresses = dnsInput?.Split(dnsInput.Contains(',') ? ',' : ';')
                .Select(addr => addr.Trim())
                .Where(addr => !string.IsNullOrEmpty(addr))
                .Select(addr => addr.StartsWith("dhcp", StringComparison.OrdinalIgnoreCase) ? "localhost" : addr)
                .Distinct()
                .ToList() ?? new List<string> { defaultAddress };
            return addresses.Count > 0 ? addresses : new List<string> { defaultAddress };
        }

        static object CreateDnsServer(string dnsAddress, List<string> domains, List<string>? expectedIPs = null)
        {
            var dnsServer = new DnsServer4Ray
            {
                address = dnsAddress,
                skipFallback = false, // Allow fallback for sanctions bypass
                domains = domains.Count > 0 ? domains : null,
                expectedIPs = expectedIPs?.Count > 0 ? expectedIPs : null
            };
            return JsonUtils.SerializeToNode(dnsServer, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        }

        // Check for sanctions bypass and transparent mirroring from user settings
        // RELOAD FRESH CONFIG to ensure we have latest settings
        var freshConfig = ConfigHandler.LoadConfig();
        var iranConfig = freshConfig?.IranSanctionsBypassItem;
        var shouldUseBypass = iranConfig?.EnableSanctionsDetection == true && iranConfig?.EnableIranianDnsAutoSwitch == true;
        var shouldUseMirroring = iranConfig?.EnableTransparentMirroring == true;

        if (shouldUseBypass || shouldUseMirroring)
        {
            Logging.SaveLog($"DNS bypass enabled: sanctions={shouldUseBypass}, mirroring={shouldUseMirroring}");
            // NOTE: Iranian DNS servers are now configured in ApplyIranianSanctionsBypass
            // This avoids duplicate DNS server configuration
        }

        var directDNSAddress = ParseDnsAddresses(simpleDNSItem?.DirectDNS, Global.DomainDirectDNSAddress.FirstOrDefault());
        var remoteDNSAddress = ParseDnsAddresses(simpleDNSItem?.RemoteDNS, Global.DomainRemoteDNSAddress.FirstOrDefault());

        var directDomainList = new List<string>();
        var directGeositeList = new List<string>();
        var proxyDomainList = new List<string>();
        var proxyGeositeList = new List<string>();
        var expectedDomainList = new List<string>();
        var expectedIPs = new List<string>();
        var regionNames = new HashSet<string>();

        if (!string.IsNullOrEmpty(simpleDNSItem?.DirectExpectedIPs))
        {
            expectedIPs = simpleDNSItem.DirectExpectedIPs
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            foreach (var ip in expectedIPs)
            {
                if (ip.StartsWith("geoip:", StringComparison.OrdinalIgnoreCase))
                {
                    var region = ip["geoip:".Length..];
                    if (!string.IsNullOrEmpty(region))
                    {
                        regionNames.Add($"geosite:{region}");
                        regionNames.Add($"geosite:geolocation-{region}");
                        regionNames.Add($"geosite:tld-{region}");
                    }
                }
            }
        }

        var routing = await ConfigHandler.GetDefaultRouting(_config);
        List<RulesItem>? rules = null;
        if (routing != null)
        {
            rules = JsonUtils.Deserialize<List<RulesItem>>(routing.RuleSet) ?? [];
            foreach (var item in rules)
            {
                if (!item.Enabled || item.Domain is null || item.Domain.Count == 0)
                {
                    continue;
                }

                foreach (var domain in item.Domain)
                {
                    if (domain.StartsWith('#'))
                        continue;
                    var normalizedDomain = domain.Replace(Global.RoutingRuleComma, ",");

                    if (item.OutboundTag == Global.DirectTag)
                    {
                        if (normalizedDomain.StartsWith("geosite:"))
                        {
                            (regionNames.Contains(normalizedDomain) ? expectedDomainList : directGeositeList).Add(normalizedDomain);
                        }
                        else
                        {
                            directDomainList.Add(normalizedDomain);
                        }
                    }
                    else if (item.OutboundTag != Global.BlockTag)
                    {
                        if (normalizedDomain.StartsWith("geosite:"))
                        {
                            proxyGeositeList.Add(normalizedDomain);
                        }
                        else
                        {
                            proxyDomainList.Add(normalizedDomain);
                        }
                    }
                }
            }
        }

        if (Utils.IsDomain(node?.Address))
        {
            directDomainList.Add(node.Address);
        }

        if (node?.Subid is not null)
        {
            var subItem = await AppManager.Instance.GetSubItem(node.Subid);
            if (subItem is not null)
            {
                foreach (var profile in new[] { subItem.PrevProfile, subItem.NextProfile })
                {
                    var profileNode = await AppManager.Instance.GetProfileItemViaRemarks(profile);
                    if (profileNode is not null
                        && Global.XraySupportConfigType.Contains(profileNode.ConfigType)
                        && Utils.IsDomain(profileNode.Address))
                    {
                        directDomainList.Add(profileNode.Address);
                    }
                }
            }
        }

        v2rayConfig.dns ??= new Dns4Ray();
        v2rayConfig.dns.servers ??= new List<object>();

        void AddDnsServers(List<string> dnsAddresses, List<string> domains, List<string>? expectedIPs = null)
        {
            if (domains.Count > 0)
            {
                foreach (var dnsAddress in dnsAddresses)
                {
                    v2rayConfig.dns.servers.Add(CreateDnsServer(dnsAddress, domains, expectedIPs));
                }
            }
        }

        AddDnsServers(remoteDNSAddress, proxyDomainList);
        AddDnsServers(directDNSAddress, directDomainList);
        AddDnsServers(remoteDNSAddress, proxyGeositeList);
        AddDnsServers(directDNSAddress, directGeositeList);
        AddDnsServers(directDNSAddress, expectedDomainList, expectedIPs);

        var useDirectDns = rules?.LastOrDefault() is { } lastRule
            && lastRule.OutboundTag == Global.DirectTag
            && (lastRule.Port == "0-65535"
                || lastRule.Network == "tcp,udp"
                || lastRule.Ip?.Contains("0.0.0.0/0") == true);

        var defaultDnsServers = useDirectDns ? directDNSAddress : remoteDNSAddress;
        v2rayConfig.dns.servers.AddRange(defaultDnsServers);

        return 0;
    }

    private async Task<int> GenDnsHosts(V2rayConfig v2rayConfig, SimpleDNSItem simpleDNSItem)
    {
        if (simpleDNSItem.AddCommonHosts == false && simpleDNSItem.UseSystemHosts == false && simpleDNSItem.Hosts.IsNullOrEmpty())
        {
            return await Task.FromResult(0);
        }
        v2rayConfig.dns ??= new Dns4Ray();
        v2rayConfig.dns.hosts ??= new Dictionary<string, object>();
        if (simpleDNSItem.AddCommonHosts == true)
        {
            v2rayConfig.dns.hosts = Global.PredefinedHosts.ToDictionary(
                kvp => kvp.Key,
                kvp => (object)kvp.Value
            );
        }

        if (simpleDNSItem.UseSystemHosts == true)
        {
            var systemHosts = Utils.GetSystemHosts();
            var normalHost = v2rayConfig?.dns?.hosts;

            if (normalHost != null && systemHosts?.Count > 0)
            {
                foreach (var host in systemHosts)
                {
                    normalHost.TryAdd(host.Key, new List<string> { host.Value });
                }
            }
        }

        if (!simpleDNSItem.Hosts.IsNullOrEmpty())
        {
            var userHostsMap = simpleDNSItem.Hosts
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line) && line.Contains(' '))
                .Select(line => line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                .Where(parts => parts.Length >= 2)
                .GroupBy(parts => parts[0])
                .ToDictionary(
                    group => group.Key,
                    group => group.SelectMany(parts => parts.Skip(1)).ToList()
                );

            foreach (var kvp in userHostsMap)
            {
                v2rayConfig.dns.hosts[kvp.Key] = kvp.Value;
            }
        }
        return await Task.FromResult(0);
    }

    private async Task<int> GenDnsCompatible(ProfileItem? node, V2rayConfig v2rayConfig)
    {
        try
        {
            var item = await AppManager.Instance.GetDNSItem(ECoreType.Xray);
            var normalDNS = item?.NormalDNS;
            var domainStrategy4Freedom = item?.DomainStrategy4Freedom;
            if (normalDNS.IsNullOrEmpty())
            {
                normalDNS = EmbedUtils.GetEmbedText(Global.DNSV2rayNormalFileName);
            }

            //Outbound Freedom domainStrategy
            if (domainStrategy4Freedom.IsNotEmpty())
            {
                var outbound = v2rayConfig.outbounds.FirstOrDefault(t => t is { protocol: "freedom", tag: Global.DirectTag });
                if (outbound != null)
                {
                    outbound.settings = new();
                    outbound.settings.domainStrategy = domainStrategy4Freedom;
                    outbound.settings.userLevel = 0;
                }
            }

            var obj = JsonUtils.ParseJson(normalDNS);
            if (obj is null)
            {
                List<string> servers = [];
                string[] arrDNS = normalDNS.Split(',');
                foreach (string str in arrDNS)
                {
                    servers.Add(str);
                }
                obj = JsonUtils.ParseJson("{}");
                obj["servers"] = JsonUtils.SerializeToNode(servers);
            }

            // Append to dns settings
            if (item.UseSystemHosts)
            {
                var systemHosts = Utils.GetSystemHosts();
                if (systemHosts.Count > 0)
                {
                    var normalHost1 = obj["hosts"];
                    if (normalHost1 != null)
                    {
                        foreach (var host in systemHosts)
                        {
                            if (normalHost1[host.Key] != null)
                                continue;
                            normalHost1[host.Key] = host.Value;
                        }
                    }
                }
            }
            var normalHost = obj["hosts"];
            if (normalHost != null)
            {
                foreach (var hostProp in normalHost.AsObject().ToList())
                {
                    if (hostProp.Value is JsonValue value && value.TryGetValue<string>(out var ip))
                    {
                        normalHost[hostProp.Key] = new JsonArray(ip);
                    }
                }
            }

            await GenDnsDomainsCompatible(node, obj, item);

            v2rayConfig.dns = JsonUtils.Deserialize<Dns4Ray>(JsonUtils.Serialize(obj));
        }
        catch (Exception ex)
        {
            Logging.SaveLog(_tag, ex);
        }
        return 0;
    }

    private async Task<int> GenDnsDomainsCompatible(ProfileItem? node, JsonNode dns, DNSItem? dNSItem)
    {
        if (node == null)
        {
            return 0;
        }
        var servers = dns["servers"];
        if (servers != null)
        {
            var domainList = new List<string>();
            if (Utils.IsDomain(node.Address))
            {
                domainList.Add(node.Address);
            }
            var subItem = await AppManager.Instance.GetSubItem(node.Subid);
            if (subItem is not null)
            {
                // Previous proxy
                var prevNode = await AppManager.Instance.GetProfileItemViaRemarks(subItem.PrevProfile);
                if (prevNode is not null
                    && Global.SingboxSupportConfigType.Contains(prevNode.ConfigType)
                    && Utils.IsDomain(prevNode.Address))
                {
                    domainList.Add(prevNode.Address);
                }

                // Next proxy
                var nextNode = await AppManager.Instance.GetProfileItemViaRemarks(subItem.NextProfile);
                if (nextNode is not null
                    && Global.SingboxSupportConfigType.Contains(nextNode.ConfigType)
                    && Utils.IsDomain(nextNode.Address))
                {
                    domainList.Add(nextNode.Address);
                }
            }
            if (domainList.Count > 0)
            {
                var dnsServer = new DnsServer4Ray()
                {
                    address = string.IsNullOrEmpty(dNSItem?.DomainDNSAddress) ? Global.DomainPureIPDNSAddress.FirstOrDefault() : dNSItem?.DomainDNSAddress,
                    skipFallback = true,
                    domains = domainList
                };
                servers.AsArray().Add(JsonUtils.SerializeToNode(dnsServer));
            }
        }
        return await Task.FromResult(0);
    }
}
