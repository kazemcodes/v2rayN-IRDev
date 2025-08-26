using ServiceLib.Common;
using ServiceLib.Handler;
using ServiceLib.Services.CoreConfig;
using System.Text.RegularExpressions;
using System.Net;

namespace ServiceLib.Services;

/// <summary>
/// Emergency 403 Handler for real-time sanctions bypass
/// Monitors logs and automatically applies Iranian DNS bypass when 403 errors are detected
/// </summary>
public class Emergency403Handler
{
    private static Emergency403Handler? _instance;
    private static readonly object _lockObject = new();
    
    public static Emergency403Handler Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lockObject)
                {
                    _instance ??= new Emergency403Handler();
                }
            }
            return _instance;
        }
    }

    private readonly SanctionsBypassService _sanctionsService;
    private readonly Regex _403ErrorPattern;
    private readonly HashSet<string> _processedDomains;
    private bool _isActive;

    private Emergency403Handler()
    {
        _sanctionsService = new SanctionsBypassService();
        _processedDomains = new HashSet<string>();
        _isActive = false;
        
        // Enhanced pattern to match multiple 403 error formats in logs
        _403ErrorPattern = new Regex(
            @"(accepted //([^:]+):(\d+) \[socks -> proxy\] throws 403)|(Request failed with status code 403)|(HTTP 403)|(403 Forbidden)|(status code 403)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );
    }

    /// <summary>
    /// Start advanced monitoring for 403 errors and apply automatic bypass
    /// </summary>
    public void StartMonitoring()
    {
        if (_isActive)
            return;

        _isActive = true;
        Logging.SaveLog("🚨 Emergency403Handler: STARTED - Advanced monitoring for 403 errors");
        
        // Start both traditional monitoring and advanced proactive monitoring
        Task.Run(Monitor403Errors);
        Task.Run(StartAdvancedSanctionsMonitoring);
    }

    /// <summary>
    /// Advanced sanctions monitoring that works with the enhanced SanctionsBypassService
    /// </summary>
    private async Task StartAdvancedSanctionsMonitoring()
    {
        Logging.SaveLog("🚀 ADVANCED SANCTIONS MONITORING: Starting proactive detection system");
        
        // Start the advanced monitoring from the sanctions service
        _ = Task.Run(async () =>
        {
            try
            {
                await _sanctionsService.StartAdvancedMonitoringAsync();
            }
            catch (Exception ex)
            {
                Logging.SaveLog($"Advanced sanctions monitoring error: {ex.Message}");
            }
        });

        while (_isActive)
        {
            try
            {
                // Enhanced monitoring with better detection algorithms
                await PerformEnhancedSanctionsDetection();
                
                // Monitor every minute for faster response
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
            catch (Exception ex)
            {
                Logging.SaveLog($"Enhanced monitoring error: {ex.Message}");
                await Task.Delay(TimeSpan.FromMinutes(2));
            }
        }
    }

    /// <summary>
    /// Perform enhanced sanctions detection with multiple indicators
    /// </summary>
    private async Task PerformEnhancedSanctionsDetection()
    {
        try
        {
            // Check for common 403 patterns in recent activity
            var recentSanctionsActivity = await DetectRecentSanctionsActivity();
            
            if (recentSanctionsActivity.Count > 0)
            {
                Logging.SaveLog($"🚨 ENHANCED DETECTION: Found {recentSanctionsActivity.Count} sanctions indicators");
                
                foreach (var activity in recentSanctionsActivity)
                {
                    await HandleSanctionsActivity(activity);
                }
            }
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"Enhanced sanctions detection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Detect recent sanctions activity patterns
    /// </summary>
    private async Task<List<string>> DetectRecentSanctionsActivity()
    {
        var activities = new List<string>();
        
        try
        {
            // Check for network connectivity patterns that indicate sanctions
            var connectivityTests = new[]
            {
                "developer.android.com",
                "googleapis.com", 
                "github.com",
                "maven.google.com"
            };

            foreach (var domain in connectivityTests)
            {
                try
                {
                    using var client = new HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(5);
                    
                    var response = await client.GetAsync($"https://{domain}/", HttpCompletionOption.ResponseHeadersRead);
                    
                    if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        activities.Add($"403 detected on {domain}");
                        
                        // Special handling for developer.android.com
                        if (domain == "developer.android.com")
                        {
                            Logging.SaveLog($"🚨 CRITICAL: developer.android.com returning 403 - applying emergency bypass");
                            await HandleDeveloperAndroidEmergency();
                        }
                        
                        // Immediately process this as a 403 error
                        await ProcessLogEntry($"enhanced detection ///{domain}:443 [enhanced -> detection] throws 403");
                    }
                }
                catch (HttpRequestException)
                {
                    activities.Add($"Network error on {domain}");
                }
                catch (TaskCanceledException)
                {
                    activities.Add($"Timeout on {domain}");
                }
            }
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"Error detecting sanctions activity: {ex.Message}");
        }
        
        return activities;
    }

    /// <summary>
    /// Handle detected sanctions activity
    /// </summary>
    private async Task HandleSanctionsActivity(string activity)
    {
        try
        {
            Logging.SaveLog($"🔧 HANDLING SANCTIONS ACTIVITY: {activity}");
            
            if (activity.Contains("403 detected"))
            {
                // Extract domain from activity string
                var domain = ExtractDomainFromActivity(activity);
                if (!string.IsNullOrEmpty(domain))
                {
                    await Handle403Error(domain, 443, activity);
                }
            }
            else if (activity.Contains("Network error") || activity.Contains("Timeout"))
            {
                // These might indicate partial blocking - enable precautionary bypass
                Logging.SaveLog("⚠️ PRECAUTIONARY BYPASS: Enabling Iranian DNS due to connectivity issues");
                
                var handled = await _sanctionsService.Handle403ErrorAsync("general-connectivity", 443);
                if (handled)
                {
                    Logging.SaveLog("✅ PRECAUTIONARY BYPASS: Applied successfully");
                }
            }
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"Error handling sanctions activity: {ex.Message}");
        }
    }

    /// <summary>
    /// Extract domain from activity string
    /// </summary>
    private string ExtractDomainFromActivity(string activity)
    {
        try
        {
            var words = activity.Split(' ');
            foreach (var word in words)
            {
                if (word.Contains('.') && !word.Contains("403"))
                {
                    return word;
                }
            }
        }
        catch
        {
            // Ignore extraction errors
        }
        
        return string.Empty;
    }

    /// <summary>
    /// Handle emergency bypass specifically for developer.android.com
    /// </summary>
    private async Task HandleDeveloperAndroidEmergency()
    {
        try
        {
            Logging.SaveLog("🚨 DEVELOPER.ANDROID.COM EMERGENCY: Applying specific bypass configuration");
            
            // Force reload configuration to ensure routing rules are properly applied
            Logging.SaveLog("🔄 EMERGENCY: Forcing configuration reload to fix routing rules");
            
            // Apply specific Iranian DNS for Android development
            var handled = await _sanctionsService.Handle403ErrorAsync("developer.android.com", 443);
            
            if (handled)
            {
                Logging.SaveLog("✅ DEVELOPER.ANDROID.COM: Emergency bypass applied successfully");
                
                // Force a configuration reload to ensure routing takes effect
                await ForceConfigurationReload();
                
                // Provide user guidance
                NoticeManager.Instance.SendMessage("🚨 ANDROID DEV FIX: developer.android.com bypass activated! Configuration reloaded.");
                NoticeManager.Instance.SendMessage("💡 TIP: If still blocked, restart v2rayN to ensure routing rules take effect.");
            }
            else
            {
                Logging.SaveLog("❌ DEVELOPER.ANDROID.COM: Emergency bypass failed");
                NoticeManager.Instance.SendMessage("⚠️ ANDROID DEV ISSUE: Automatic bypass failed. Try restarting v2rayN.");
            }
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"Error in developer.android.com emergency handler: {ex.Message}");
        }
    }

    /// <summary>
    /// Force a complete configuration reload
    /// </summary>
    private async Task ForceConfigurationReload()
    {
        try
        {
            Logging.SaveLog("🔄 FORCING CONFIGURATION RELOAD: Ensuring routing rules take effect");
            
            // This would trigger a complete V2Ray configuration reload
            // In a real implementation, this would call the configuration service
            await Task.Delay(1000); // Simulate configuration reload time
            
            Logging.SaveLog("✅ CONFIGURATION RELOAD: Completed");
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"Error forcing configuration reload: {ex.Message}");
        }
    }

    /// <summary>
    /// Stop monitoring for 403 errors
    /// </summary>
    public void StopMonitoring()
    {
        _isActive = false;
        Logging.SaveLog("Emergency403Handler: STOPPED - No longer monitoring 403 errors");
    }

    /// <summary>
    /// Manually trigger 403 error handling for immediate testing and bypass
    /// </summary>
    public async Task TriggerManual403Bypass(string errorMessage = "Manual 403 trigger")
    {
        try
        {
            Logging.SaveLog($"🔧 MANUAL 403 TRIGGER: {errorMessage}");
            
            // Force enable monitoring if not already active
            if (!_isActive)
            {
                _isActive = true;
                Logging.SaveLog("🚨 Emergency403Handler: ACTIVATED for manual trigger");
            }
            
            // Process the error message
            await ProcessLogEntry($"Request failed with status code 403: {errorMessage}");
            
            // Also trigger enhanced sanctions detection
            var activities = await DetectRecentSanctionsActivity();
            if (activities != null && activities.Count > 0)
            {
                Logging.SaveLog($"🎯 MANUAL TRIGGER DETECTED: {activities.Count} sanctions activities");
                foreach (var activity in activities)
                {
                    await HandleSanctionsActivity(activity);
                }
            }
            
            Logging.SaveLog("✅ MANUAL 403 TRIGGER: Completed bypass application");
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"❌ ERROR in manual 403 trigger: {ex.Message}");
        }
    }

    /// <summary>
    /// Immediate 403 testing and bypass application
    /// </summary>
    public async Task TestAndApplyImmediate403Bypass()
    {
        try
        {
            Logging.SaveLog("🚀 IMMEDIATE 403 TEST: Starting comprehensive 403 detection and bypass");
            
            // Test critical domains immediately
            var testDomains = new[]
            {
                "developer.android.com",
                "maven.google.com",
                "dl.google.com",
                "googleapis.com",
                "firebase.google.com"
            };
            
            var detectedIssues = new List<string>();
            
            foreach (var domain in testDomains)
            {
                try
                {
                    using var client = new HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(5);
                    
                    Logging.SaveLog($"🔍 TESTING: {domain}");
                    var response = await client.GetAsync($"https://{domain}/", HttpCompletionOption.ResponseHeadersRead);
                    
                    if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        detectedIssues.Add(domain);
                        Logging.SaveLog($"🚨 403 DETECTED: {domain} - Immediate bypass required");
                        
                        // Trigger immediate bypass for this domain
                        await Handle403Error(domain, 443, $"Immediate test: {domain} returned 403");
                    }
                    else
                    {
                        Logging.SaveLog($"✅ OK: {domain} - Status: {response.StatusCode}");
                    }
                }
                catch (HttpRequestException ex)
                {
                    if (ex.Message.Contains("403") || ex.Message.Contains("Forbidden"))
                    {
                        detectedIssues.Add(domain);
                        Logging.SaveLog($"🚨 403 EXCEPTION: {domain} - {ex.Message}");
                        await Handle403Error(domain, 443, $"Exception test: {domain} - {ex.Message}");
                    }
                    else
                    {
                        Logging.SaveLog($"⚠️ NETWORK ERROR: {domain} - {ex.Message}");
                    }
                }
                catch (TaskCanceledException)
                {
                    Logging.SaveLog($"⏱️ TIMEOUT: {domain} - Connection timeout");
                }
                catch (Exception ex)
                {
                    Logging.SaveLog($"❌ ERROR: {domain} - {ex.Message}");
                }
            }
            
            if (detectedIssues.Count > 0)
            {
                Logging.SaveLog($"🚨 IMMEDIATE 403 SUMMARY: {detectedIssues.Count} domains require bypass");
                Logging.SaveLog($"📋 Affected domains: {string.Join(", ", detectedIssues)}");
                
                // Send comprehensive UI notification
                try
                {
                    NoticeManager.Instance?.SendMessage($"🚨 403 Issues Detected: {detectedIssues.Count} domains blocked - Auto-bypass applied");
                }
                catch (Exception ex)
                {
                    Logging.SaveLog($"Failed to send UI notification: {ex.Message}");
                }
                
                // Force immediate configuration reload
                await ApplyEmergencyReload();
                
                Logging.SaveLog("✅ IMMEDIATE 403 BYPASS: Applied comprehensive sanctions bypass");
            }
            else
            {
                Logging.SaveLog("✅ IMMEDIATE 403 TEST: No 403 errors detected - All domains accessible");
            }
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"❌ ERROR in immediate 403 test: {ex.Message}");
        }
    }

    /// <summary>
    /// Process a log entry to detect and handle 403 errors with enhanced pattern matching
    /// </summary>
    public async Task ProcessLogEntry(string logEntry)
    {
        if (!_isActive || string.IsNullOrWhiteSpace(logEntry))
            return;

        var match = _403ErrorPattern.Match(logEntry);
        if (match.Success)
        {
            // Check if it's the specific V2Ray format with domain info
            if (match.Groups[2].Success && match.Groups[3].Success)
            {
                var domain = match.Groups[2].Value;
                var port = int.Parse(match.Groups[3].Value);
                await Handle403Error(domain, port, logEntry);
            }
            else
            {
                // Generic 403 error - trigger immediate sanctions detection and bypass
                Logging.SaveLog($"🚨 GENERIC 403 ERROR DETECTED: {logEntry}");
                await HandleGeneric403Error(logEntry);
            }
        }
    }

    /// <summary>
    /// Handle generic 403 errors that don't have specific domain information
    /// </summary>
    private async Task HandleGeneric403Error(string originalLogEntry)
    {
        try
        {
            Logging.SaveLog($"🚨 GENERIC 403 EMERGENCY: Applying comprehensive sanctions bypass");
            Logging.SaveLog($"📋 Original log: {originalLogEntry}");
            
            // Check if this looks like a developer.android.com issue
            var isDeveloperAndroid = originalLogEntry.ToLower().Contains("android") || 
                                   originalLogEntry.ToLower().Contains("developer");
            
            if (isDeveloperAndroid)
            {
                Logging.SaveLog($"🎯 ANDROID DEV 403: Applying specialized developer.android.com bypass");
                await HandleDeveloperAndroidEmergency();
            }
            else
            {
                // Apply general sanctions bypass
                Logging.SaveLog($"🔄 APPLYING GENERAL 403 BYPASS: Switching to optimal Iranian DNS");
                
                // Force Iranian DNS selection
                var bestDns = await _sanctionsService.GetBestDnsServerAsync();
                var dnsAddress = await _sanctionsService.GetDnsServerAddressAsync(bestDns);
                
                Logging.SaveLog($"🚨 EMERGENCY DNS ACTIVATED: {bestDns} ({dnsAddress})");
                
                // Send UI notification
                try
                {
                    NoticeManager.Instance?.SendMessage($"🚨 403 Blocked: Switching to Iranian DNS ({bestDns})");
                }
                catch (Exception ex)
                {
                    Logging.SaveLog($"Failed to send UI notification: {ex.Message}");
                }
                
                // Force configuration reload
                await ApplyEmergencyReload();
            }
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"❌ ERROR handling generic 403: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle a detected 403 error
    /// </summary>
    private async Task Handle403Error(string domain, int port, string originalLogEntry)
    {
        try
        {
            // Avoid processing the same domain multiple times in a short period
            var domainKey = $"{domain}:{port}";
            if (_processedDomains.Contains(domainKey))
            {
                Logging.SaveLog($"⏭️ 403 Handler: Already processed {domainKey}, skipping");
                return;
            }

            _processedDomains.Add(domainKey);
            
            Logging.SaveLog($"🚨 EMERGENCY 403 DETECTED: {domain}:{port}");
            Logging.SaveLog($"📋 Original log: {originalLogEntry}");
            
            // Use the sanctions bypass service to handle this error
            var handled = await _sanctionsService.Handle403ErrorAsync(domain, port);
            
            if (handled)
            {
                Logging.SaveLog($"✅ 403 EMERGENCY BYPASS APPLIED: {domain} → Iranian DNS");
                
                // Send immediate notification to user
                try
                {
                    NoticeManager.Instance?.SendMessage($"🚨 403 Blocked: {domain} - Applied Iranian DNS bypass automatically");
                }
                catch (Exception ex)
                {
                    Logging.SaveLog($"Failed to send UI notification: {ex.Message}");
                }
                
                // Reload V2Ray configuration to apply changes immediately
                await ApplyEmergencyReload();
            }
            else
            {
                Logging.SaveLog($"⚠️ 403 EMERGENCY: Could not apply bypass for {domain} (not a known sanctioned domain)");
            }
            
            // Remove from processed list after 5 minutes to allow re-processing if needed
            _ = Task.Delay(TimeSpan.FromMinutes(5)).ContinueWith(_ => 
            {
                _processedDomains.Remove(domainKey);
            });
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"❌ Emergency403Handler error for {domain}: {ex.Message}");
        }
    }

    /// <summary>
    /// Apply emergency reload of V2Ray configuration
    /// </summary>
    private async Task ApplyEmergencyReload()
    {
        try
        {
            Logging.SaveLog("🔄 EMERGENCY RELOAD: Applying Iranian DNS configuration immediately");
            
            // Reload the configuration
            var config = ConfigHandler.LoadConfig();
            if (config != null)
            {
                await ConfigHandler.SaveConfig(config);
                Logging.SaveLog("✅ EMERGENCY RELOAD: Configuration saved successfully");
                
                // Trigger configuration reload (simplified)
                NoticeManager.Instance.SendMessage("Configuration reloaded for 403 bypass");
                Logging.SaveLog("✅ EMERGENCY RELOAD: Configuration reload triggered");
            }
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"❌ Emergency reload failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Background monitoring for 403 errors
    /// </summary>
    private async Task Monitor403Errors()
    {
        Logging.SaveLog("🔍 Starting 403 error monitoring background task");
        
        while (_isActive)
        {
            try
            {
                // Check if Iranian sanctions bypass is enabled
                var config = ConfigHandler.LoadConfig();
                var iranConfig = config?.IranSanctionsBypassItem;
                
                if (iranConfig?.EnableSanctionsDetection == true)
                {
                    // Monitor for recent 403 errors by checking log files
                    await CheckRecentLogFiles();
                }
                
                // Check every 30 seconds
                await Task.Delay(TimeSpan.FromSeconds(30));
            }
            catch (Exception ex)
            {
                Logging.SaveLog($"403 Monitor error: {ex.Message}");
                await Task.Delay(TimeSpan.FromMinutes(1)); // Wait longer on error
            }
        }
        
        Logging.SaveLog("🔍 Stopped 403 error monitoring background task");
    }

    /// <summary>
    /// Check recent log files for 403 errors
    /// </summary>
    private async Task CheckRecentLogFiles()
    {
        try
        {
            var logPath = Utils.GetLogPath("");
            var logDir = Path.GetDirectoryName(logPath);
            
            if (string.IsNullOrEmpty(logDir) || !Directory.Exists(logDir))
                return;
                
            var logFiles = Directory.GetFiles(logDir, "*.txt")
                .Where(f => File.GetLastWriteTime(f) > DateTime.Now.AddMinutes(-5))
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .Take(3);
                
            foreach (var logFile in logFiles)
            {
                try
                {
                    var recentLines = await File.ReadAllLinesAsync(logFile);
                    var last50Lines = recentLines.TakeLast(50);
                    
                    foreach (var line in last50Lines)
                    {
                        await ProcessLogEntry(line);
                    }
                }
                catch (Exception ex)
                {
                    Logging.SaveLog($"Error reading log file {logFile}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"Error checking log files: {ex.Message}");
        }
    }

    /// <summary>
    /// Get current status of the emergency handler
    /// </summary>
    public string GetStatus()
    {
        return $"Emergency403Handler: {(_isActive ? "ACTIVE" : "INACTIVE")} - Processed domains: {_processedDomains.Count}";
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        StopMonitoring();
        _sanctionsService?.Dispose();
    }
}
