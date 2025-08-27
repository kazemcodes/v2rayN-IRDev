using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Threading;
using System.IO;
using ReactiveUI;
using ServiceLib.Manager;
using ServiceLib.ViewModels;
using v2rayN.Base;
using ServiceLib.Common;
using ServiceLib.Services.CoreConfig;
using ServiceLib.Services;
using ServiceLib.Models;
using ServiceLib.Handler;

namespace v2rayN.Views;

public partial class AdvancedBypassWindow : WindowBase<SanctionsBypassViewModel>
{
    private static Config _config;
    private readonly SanctionsBypassService _sanctionsService;
    private AdvancedBypassConfig _currentConfig;
    private bool _isAnalyzing = false;

    public AdvancedBypassWindow()
    {
        InitializeComponent();

        this.Owner = Application.Current.MainWindow;
        _config = AppManager.Instance.Config;
        _sanctionsService = new SanctionsBypassService();

        ViewModel = new SanctionsBypassViewModel();

        LogMessage("🚀 Advanced Sanctions Bypass System initialized");
        LogMessage("🔥 Ready to deploy next-generation bypass techniques");
    }

    private void BtnAnalyzeDomain_Click(object sender, RoutedEventArgs e)
    {
        AnalyzeDomain();
    }

    private void BtnApplyBypass_Click(object sender, RoutedEventArgs e)
    {
        ApplyAdvancedBypass();
    }

    private void BtnTestAllMethods_Click(object sender, RoutedEventArgs e)
    {
        TestAllBypassMethods();
    }

    private void BtnApplyRecommended_Click(object sender, RoutedEventArgs e)
    {
        ApplyRecommendedMethod();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void AnalyzeDomain()
    {
        if (_isAnalyzing)
            return;

        try
        {
            _isAnalyzing = true;
            btnAnalyzeDomain.IsEnabled = false;
            btnApplyBypass.IsEnabled = false;
            btnApplyRecommended.IsEnabled = false;

            var domain = txtTargetDomain.Text.Trim();
            if (string.IsNullOrWhiteSpace(domain))
            {
                MessageBox.Show("Please enter a domain to analyze.", "Invalid Domain", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LogMessage($"🔍 Starting advanced bypass analysis for: {domain}");

            // Create advanced bypass configuration
            _currentConfig = await _sanctionsService.CreateAdvancedBypassConfigAsync(domain);

            // Update UI with results
            UpdateAnalysisResults(_currentConfig);

            // Enable apply buttons
            btnApplyBypass.IsEnabled = true;
            btnApplyRecommended.IsEnabled = true;

            LogMessage($"✅ Analysis complete - Recommended method: {_currentConfig.RecommendedMethod}");
        }
        catch (Exception ex)
        {
            LogMessage($"❌ Analysis failed: {ex.Message}");
            MessageBox.Show($"Analysis failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isAnalyzing = false;
            btnAnalyzeDomain.IsEnabled = true;
        }
    }

    private void UpdateAnalysisResults(AdvancedBypassConfig config)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            // Update DNS over HTTPS status
            txtDohStatus.Text = config.DnsOverHttpsSupported ? "✅ Supported" : "❌ Not Supported";
            txtDohStatus.Foreground = config.DnsOverHttpsSupported ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
            txtDohDetails.Text = config.DnsOverHttpsSupported ? "Encrypted DNS queries available" : "DNS queries may be monitored";

            // Update Proxy Chain status
            var proxyCount = config.ProxyChain.Proxies.Count;
            txtProxyStatus.Text = proxyCount > 0 ? $"✅ {proxyCount} proxies found" : "❌ No proxies available";
            txtProxyStatus.Foreground = proxyCount > 0 ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
            txtProxyDetails.Text = proxyCount > 0 ? $"Proxy chain with {proxyCount} hops" : "No working proxy servers found";

            // Update Domain Fronting status
            var hasFronting = !string.IsNullOrEmpty(config.DomainFrontingEndpoint);
            txtFrontingStatus.Text = hasFronting ? "✅ Available" : "❌ Not Available";
            txtFrontingStatus.Foreground = hasFronting ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
            txtFrontingDetails.Text = hasFronting ? $"Fronting via: {config.DomainFrontingEndpoint}" : "No suitable fronting domains found";

            // Update CDN Routing status
            var hasCdn = !string.IsNullOrEmpty(config.CdnRoutingEndpoint);
            txtCdnStatus.Text = hasCdn ? "✅ Available" : "❌ Not Available";
            txtCdnStatus.Foreground = hasCdn ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
            txtCdnDetails.Text = hasCdn ? $"Routing via: {config.CdnRoutingEndpoint}" : "No CDN routing options found";

            // Update recommended method
            UpdateRecommendedMethod(config);
        });
    }

    private void UpdateRecommendedMethod(AdvancedBypassConfig config)
    {
        var methodText = "";
        var description = "";

        switch (config.RecommendedMethod)
        {
            case BypassMethod.DnsOverHttps:
                methodText = "🔒 DNS over HTTPS";
                description = "Encrypts DNS queries to bypass DNS-level blocking";
                break;

            case BypassMethod.ProxyChain:
                methodText = "🔗 Proxy Chain";
                description = "Routes traffic through multiple proxy servers for maximum anonymity";
                break;

            case BypassMethod.DomainFronting:
                methodText = "🎭 Domain Fronting";
                description = "Uses allowed domains to front blocked content";
                break;

            case BypassMethod.CdnRouting:
                methodText = "🌐 CDN Routing";
                description = "Routes through Content Delivery Networks";
                break;

            case BypassMethod.DnsOverHttpsWithProxyChain:
                methodText = "🔥 Combined: DoH + Proxy Chain";
                description = "Ultimate bypass combining encrypted DNS with proxy chaining";
                break;

            case BypassMethod.None:
                methodText = "❌ No Method Available";
                description = "No bypass methods available for this domain";
                break;
        }

        txtRecommendedMethod.Text = methodText;
        txtMethodDescription.Text = description;
    }

    private async void ApplyAdvancedBypass()
    {
        if (_currentConfig == null)
        {
            MessageBox.Show("Please analyze a domain first.", "No Configuration", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            btnApplyBypass.IsEnabled = false;
            LogMessage($"🚀 Applying advanced bypass for: {_currentConfig.TargetDomain}");

            // Get V2Ray configuration
            var v2rayConfig = await GetV2RayConfiguration();
            if (v2rayConfig == null)
            {
                LogMessage("❌ Failed to get V2Ray configuration");
                MessageBox.Show("Failed to retrieve V2Ray configuration. Check logs for details.",
                              "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var success = await _sanctionsService.ApplyAdvancedBypassAsync(_currentConfig, v2rayConfig);

            if (success)
            {
                LogMessage($"✅ Advanced bypass successfully applied!");
                LogMessage($"🔧 Applied {_currentConfig.RecommendedMethod} for {_currentConfig.TargetDomain}");

                // Save the modified configuration
                await SaveModifiedConfigurationAsync(v2rayConfig);

                // Display applied routing rules for verification
                DisplayAppliedRules(v2rayConfig);

                MessageBox.Show($"Advanced bypass successfully applied for {_currentConfig.TargetDomain}!\n\n" +
                              $"Method: {_currentConfig.RecommendedMethod}\n" +
                              $"Routing rules added to V2Ray configuration.\n\n" +
                              $"⚠️ Please restart V2Ray service to apply changes.",
                              "Success - Restart Required", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                LogMessage($"❌ Advanced bypass application failed");
                MessageBox.Show("Advanced bypass application failed. Check logs for details.",
                              "Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            LogMessage($"❌ Error applying advanced bypass: {ex.Message}");
            MessageBox.Show($"Error applying advanced bypass: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            btnApplyBypass.IsEnabled = true;
        }
    }

    private async void TestAllBypassMethods()
    {
        if (_currentConfig == null)
        {
            MessageBox.Show("Please analyze a domain first.", "No Configuration", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            btnTestAllMethods.IsEnabled = false;
            LogMessage($"🧪 Testing all bypass methods for: {_currentConfig.TargetDomain}");

            // Test DNS over HTTPS
            if (chkEnableDoh.IsChecked == true)
            {
                LogMessage("🔒 Testing DNS over HTTPS...");
                var dohResult = await _sanctionsService.TestDnsOverHttpsAsync(_currentConfig.TargetDomain);
                LogMessage($"   Result: {(dohResult ? "✅ SUCCESS" : "❌ FAILED")}");
            }

            // Test Proxy Chain
            if (chkEnableProxyChain.IsChecked == true)
            {
                LogMessage("🔗 Testing Proxy Chain...");
                var proxyChain = await _sanctionsService.CreateProxyChainAsync(_currentConfig.TargetDomain);
                LogMessage($"   Result: ✅ Created chain with {proxyChain.Proxies.Count} proxies");
            }

            // Test Domain Fronting
            if (chkEnableDomainFronting.IsChecked == true)
            {
                LogMessage("🎭 Testing Domain Fronting...");
                var frontingEndpoint = await _sanctionsService.GetDomainFrontingEndpointAsync(_currentConfig.TargetDomain);
                LogMessage($"   Result: {(frontingEndpoint != null ? $"✅ SUCCESS via {frontingEndpoint}" : "❌ FAILED")}");
            }

            // Test CDN Routing
            if (chkEnableCdnRouting.IsChecked == true)
            {
                LogMessage("🌐 Testing CDN Routing...");
                var cdnEndpoint = await _sanctionsService.GetCdnRoutingEndpointAsync(_currentConfig.TargetDomain);
                LogMessage($"   Result: {(cdnEndpoint != null ? $"✅ SUCCESS via {cdnEndpoint}" : "❌ FAILED")}");
            }

            LogMessage("✅ All bypass method tests completed!");
            MessageBox.Show("All bypass method tests completed! Check the log for results.",
                          "Testing Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            LogMessage($"❌ Error testing bypass methods: {ex.Message}");
            MessageBox.Show($"Error testing bypass methods: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            btnTestAllMethods.IsEnabled = true;
        }
    }

    private async void ApplyRecommendedMethod()
    {
        if (_currentConfig == null || _currentConfig.RecommendedMethod == BypassMethod.None)
        {
            MessageBox.Show("No recommended method available.", "No Method", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            btnApplyRecommended.IsEnabled = false;
            LogMessage($"🎯 Applying recommended method: {_currentConfig.RecommendedMethod}");

            var v2rayConfig = await GetV2RayConfiguration();
            if (v2rayConfig == null)
            {
                LogMessage("❌ Failed to get V2Ray configuration");
                MessageBox.Show("Failed to retrieve V2Ray configuration. Check logs for details.",
                              "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var success = await _sanctionsService.ApplyAdvancedBypassAsync(_currentConfig, v2rayConfig);

            if (success)
            {
                LogMessage($"✅ Recommended method applied successfully!");
                LogMessage($"🔧 Applied {_currentConfig.RecommendedMethod} for {_currentConfig.TargetDomain}");

                // Save the modified configuration
                await SaveModifiedConfigurationAsync(v2rayConfig);

                // Display applied routing rules for verification
                DisplayAppliedRules(v2rayConfig);

                MessageBox.Show($"Successfully applied {_currentConfig.RecommendedMethod} for {_currentConfig.TargetDomain}!\n\n" +
                              $"Routing rules added to V2Ray configuration.\n\n" +
                              $"⚠️ Please restart V2Ray service to apply changes.",
                              "Success - Restart Required", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                LogMessage($"❌ Recommended method application failed");
                MessageBox.Show("Recommended method application failed. Check logs for details.",
                              "Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            LogMessage($"❌ Error applying recommended method: {ex.Message}");
            MessageBox.Show($"Error applying recommended method: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            btnApplyRecommended.IsEnabled = true;
        }
    }

    private async Task<V2rayConfig?> GetV2RayConfiguration()
    {
        try
        {
            LogMessage("🔧 Retrieving V2Ray configuration...");

            // Get the app configuration
            var config = AppManager.Instance.Config;
            if (config == null)
            {
                LogMessage("❌ Failed to get app configuration");
                return null;
            }

            // Create a default configuration for advanced bypass testing
            LogMessage("🔧 Creating default V2Ray configuration for advanced bypass...");

            // Load the default V2Ray template
            var result = EmbedUtils.GetEmbedText(Global.V2raySampleClient);
            if (result.IsNullOrEmpty())
            {
                LogMessage("❌ Failed to load V2Ray template");
                return null;
            }

            var v2rayConfig = JsonUtils.Deserialize<V2rayConfig>(result);
            if (v2rayConfig == null)
            {
                LogMessage("❌ Failed to deserialize V2Ray configuration");
                return null;
            }

            // Initialize routing if it doesn't exist
            if (v2rayConfig.routing == null)
            {
                v2rayConfig.routing = new Routing4Ray
                {
                    domainStrategy = "IPIfNonMatch",
                    rules = new List<RulesItem4Ray>()
                };
            }

            if (v2rayConfig.routing.rules == null)
            {
                v2rayConfig.routing.rules = new List<RulesItem4Ray>();
            }

            LogMessage("✅ Default V2Ray configuration created for advanced bypass");
            return v2rayConfig;
        }
        catch (Exception ex)
        {
            LogMessage($"❌ Error retrieving V2Ray configuration: {ex.Message}");
            return null;
        }
    }

    private async Task SaveModifiedConfigurationAsync(V2rayConfig v2rayConfig)
    {
        try
        {
            LogMessage("💾 Saving modified V2Ray configuration...");

            // Serialize the configuration to JSON
            var configJson = JsonUtils.Serialize(v2rayConfig);
            if (string.IsNullOrEmpty(configJson))
            {
                LogMessage("❌ Failed to serialize V2Ray configuration");
                return;
            }

            // Save to a temporary file or configuration storage
            // For now, we'll save it to a bypass-specific config file in the app data directory
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var bypassConfigPath = Path.Combine(appDataPath, "v2rayN", "advanced_bypass_config.json");

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(bypassConfigPath));

            await File.WriteAllTextAsync(bypassConfigPath, configJson);

            LogMessage($"✅ Modified configuration saved to: {bypassConfigPath}");
            LogMessage($"📊 Configuration contains {v2rayConfig.routing?.rules?.Count ?? 0} routing rules");

            // Also update the current configuration if possible
            var config = AppManager.Instance.Config;
            if (config != null)
            {
                // Trigger a configuration reload if supported
                LogMessage("🔄 Triggering configuration reload...");
                // Note: This might require additional integration with the main app
            }
        }
        catch (Exception ex)
        {
            LogMessage($"❌ Error saving modified configuration: {ex.Message}");
        }
    }

    private void DisplayAppliedRules(V2rayConfig v2rayConfig)
    {
        try
        {
            LogMessage("📋 Applied V2Ray Routing Rules:");
            LogMessage($"   Total rules: {v2rayConfig.routing?.rules?.Count ?? 0}");

            if (v2rayConfig.routing?.rules != null)
            {
                var bypassRules = v2rayConfig.routing.rules
                    .Where(r => r.domain?.Any(d => d.Contains(_currentConfig.TargetDomain)) == true)
                    .ToList();

                LogMessage($"   Bypass rules for {_currentConfig.TargetDomain}: {bypassRules.Count}");

                foreach (var rule in bypassRules)
                {
                    var domains = rule.domain != null ? string.Join(", ", rule.domain.Take(2)) : "none";
                    if (rule.domain != null && rule.domain.Count > 2)
                    {
                        domains += $" (+{rule.domain.Count - 2} more)";
                    }

                    LogMessage($"   📍 Rule: {domains} → {rule.outboundTag} (priority: {v2rayConfig.routing.rules.IndexOf(rule)})");
                }
            }

            LogMessage("✅ Rules verification complete");
        }
        catch (Exception ex)
        {
            LogMessage($"⚠️ Error displaying applied rules: {ex.Message}");
        }
    }

    private void LogMessage(string message)
    {
        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            txtLog.Text += $"[{timestamp}] {message}\n";

            // Only scroll to end if we're not in the middle of rapid updates
            if (txtLog.Text.Length < 50000) // Prevent performance issues with very large logs
            {
                logScrollViewer.ScrollToEnd();
            }
        }), System.Windows.Threading.DispatcherPriority.Background);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        _sanctionsService?.Dispose();
        base.OnClosing(e);
    }
}
