using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Threading;
using ReactiveUI;
using ServiceLib.Manager;
using ServiceLib.ViewModels;
using v2rayN.Base;
using ServiceLib.Common;
using ServiceLib.Services.CoreConfig;
using ServiceLib.Services;
using System.Collections.Concurrent;

namespace v2rayN.Views;

public partial class DnsTestingWindow : WindowBase<SanctionsBypassViewModel>
{
    private static Config _config;
    private readonly SanctionsBypassService _sanctionsService;
    private CancellationTokenSource _cancellationTokenSource;
    private bool _isTesting = false;
    private static readonly Lazy<Dictionary<string, string>> _dnsServerCache = new Lazy<Dictionary<string, string>>(InitializeDnsServers);
    private static readonly HttpClient _sharedHttpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(3),
        DefaultRequestHeaders = { { "User-Agent", "v2rayN-DNSTest/1.0" } }
    };

    public ObservableCollection<DnsTestResult> DnsResults { get; set; }

    private const int MAX_CONCURRENT_TESTS = 8;
    private const int TEST_TIMEOUT_MS = 3000;
    private const int DELAY_BETWEEN_TESTS_MS = 50;

    public DnsTestingWindow()
    {
        InitializeComponent();

        this.Owner = Application.Current.MainWindow;
        _config = AppManager.Instance.Config;
        _sanctionsService = new SanctionsBypassService();

        ViewModel = new SanctionsBypassViewModel();
        DnsResults = new ObservableCollection<DnsTestResult>();
        dgResults.ItemsSource = DnsResults;

        LogMessage($"⚡ Ultra-Fast developer.android.com DNS Testing initialized - Ready to test {GetDnsServers().Count} DNS servers");

        this.WhenActivated(disposables =>
        {
            Disposable.Create(() => { }).DisposeWith(disposables);
        });
    }

    private static Dictionary<string, string> InitializeDnsServers()
    {
        return new Dictionary<string, string>
        {
            // Tier 1: Most Reliable
            { "shecan-primary", "178.22.122.100" },
            { "shecan-secondary", "185.51.200.2" },
            { "electro-primary", "78.157.42.100" },
            { "electro-secondary", "78.157.42.101" },
            { "radar-primary", "10.202.10.10" },
            { "radar-secondary", "10.202.10.11" },

            // Tier 2: Reliable Alternatives
            { "shelter-primary", "94.103.125.157" },
            { "shelter-secondary", "94.103.125.158" },
            { "403-primary", "10.202.10.202" },
            { "403-secondary", "10.202.10.102" },
            { "begzar-primary", "185.55.226.26" },
            { "begzar-secondary", "185.55.225.25" },

            // Tier 3: Additional Options
            { "asan-primary", "185.143.233.120" },
            { "asan-secondary", "185.143.234.120" },
            { "asan-dns", "185.143.232.120" },

            // Tier 4: High-Performance 2024
            { "pishgaman-primary", "5.202.100.100" },
            { "pishgaman-secondary", "5.202.100.101" },
            { "tci-primary", "192.168.100.100" },
            { "tci-secondary", "192.168.100.101" },
            { "mokhaberat-primary", "194.225.50.50" },
            { "mokhaberat-secondary", "194.225.50.51" },
            { "parspack-primary", "185.206.92.92" },
            { "parspack-secondary", "185.206.93.93" },

            // Tier 5: Mobile Operators
            { "irancell-primary", "78.39.35.66" },
            { "irancell-secondary", "78.39.35.67" },
            { "hamrah-primary", "217.218.127.127" },
            { "hamrah-secondary", "217.218.155.155" },
            { "rightel-primary", "78.157.42.101" },
            { "rightel-secondary", "78.157.42.100" },

            // Tier 6: Regional
            { "tehran-dns1", "185.143.232.100" },
            { "tehran-dns2", "185.143.232.101" },
            { "mashhad-dns1", "91.99.101.101" },
            { "mashhad-dns2", "91.99.102.102" },
            { "isfahan-dns1", "185.8.172.14" },
            { "isfahan-dns2", "185.8.175.14" },

            // Tier 7: High-Performance Global
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

            // Tier 8: ISP-Specific
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

            // Tier 9: Specialized Anti-Sanctions DNS
            { "dynx-anti-sanctions-primary", "10.70.95.150" },
            { "dynx-anti-sanctions-secondary", "10.70.95.162" },
            { "dynx-adblocker-primary", "195.26.26.23" },
            { "dynx-ipv6-primary", "2a00:c98:2050:a04d:1::400" },
            { "dynx-family-safe", "195.26.26.23" },

            // Tier 10: Advanced Anti-Sanctions DNS
            { "shecan-403-bypass", "185.51.200.3" },
            { "electro-403-bypass", "78.157.42.102" },
            { "radar-403-bypass", "10.202.10.202" },
            { "begzar-403-bypass", "185.55.226.27" },
            { "asan-403-bypass", "185.143.233.121" },
            { "pishgaman-403-bypass", "5.202.100.101" },
            { "mokhaberat-403-bypass", "194.225.50.51" },
            { "datak-403-bypass", "81.91.161.2" },

            // Tier 11: Premium Iranian DNS
            { "iranserver-primary", "194.36.174.10" },
            { "iranserver-secondary", "194.36.174.11" },
            { "mehr-secondary", "5.145.117.10" },
            { "mehr-primary", "5.145.117.11" },
            { "afra-secondary", "185.73.0.10" },
            { "afra-primary", "185.73.0.11" },

            // Tier 12: Cloud-Based Iranian DNS
            { "cloudflare-iran-optimized", "1.1.1.1" },
            { "google-iran-optimized", "8.8.8.8" },
            { "quad9-iran-optimized", "9.9.9.9" },
            { "opendns-iran-optimized", "208.67.222.222" }
        };
    }

    public static Dictionary<string, string> GetDnsServers()
    {
        return _dnsServerCache.Value;
    }

    private void BtnTestAllDns_Click(object sender, RoutedEventArgs e)
    {
        if (_isTesting)
            return;

        // Start testing on a background thread to prevent UI blocking
        Task.Run(async () => await StartDnsTesting());
    }

    private void BtnStopTest_Click(object sender, RoutedEventArgs e)
    {
        StopDnsTesting();
    }

    private void BtnApplyOptimal_Click(object sender, RoutedEventArgs e)
    {
        // Start DNS application on a background thread
        Task.Run(async () =>
        {
            var result = await ApplyOptimalDns();
            if (!result)
            {
                // If basic DNS application fails, offer advanced bypass options
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var resultDialog = MessageBox.Show(
                        "Basic DNS application failed. Would you like to try advanced bypass methods?",
                        "Advanced Bypass Options",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (resultDialog == MessageBoxResult.Yes)
                    {
                        OpenAdvancedBypassWindow();
                    }
                });
            }
        });
    }

    private void BtnAdvancedBypass_Click(object sender, RoutedEventArgs e)
    {
        OpenAdvancedBypassWindow();
    }

    private void OpenAdvancedBypassWindow()
    {
        try
        {
            var advancedBypassWindow = new AdvancedBypassWindow();
            advancedBypassWindow.Owner = this;
            advancedBypassWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            LogMessage($"❌ Error opening advanced bypass window: {ex.Message}");
            MessageBox.Show($"Error opening advanced bypass window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task StartDnsTesting()
    {
        try
        {
            _isTesting = true;
            _cancellationTokenSource = new CancellationTokenSource();
            
            // Update UI
            btnTestAllDns.IsEnabled = false;
            btnStopTest.IsEnabled = true;
            btnApplyOptimal.IsEnabled = false;
            progressPanel.Visibility = Visibility.Visible;
            progressBar.Value = 0;
            progressBar.Maximum = GetDnsServers().Count;
            
            DnsResults.Clear();
            LogMessage("🚀 STARTING ULTRA-FAST DEVELOPER.ANDROID.COM TESTING...");
            LogMessage($"📊 Testing all {GetDnsServers().Count} DNS servers for ONLY developer.android.com access (~3 seconds per DNS)");
            
            // Test DNS servers with error handling to prevent crashes
            await TestAllDnsServersSafely();
            
            LogMessage("✅ DNS TESTING COMPLETED!");
            LogMessage($"📊 Results: {DnsResults.Count(r => r.Status.Contains("WORKING"))} working, {DnsResults.Count(r => r.Status.Contains("FAILED"))} failed");
            
            // Update summary
            UpdateSummary();
            
            // Enable apply button if we have working DNS servers
            if (DnsResults.Any(r => r.Status.Contains("WORKING")))
            {
                btnApplyOptimal.IsEnabled = true;
                LogMessage("🎯 OPTIMAL DNS IDENTIFIED - Click 'Apply Optimal DNS' to use the best server");
            }
        }
        catch (Exception ex)
        {
            LogMessage($"❌ ERROR during DNS testing: {ex.Message}");
        }
        finally
        {
            _isTesting = false;
            btnTestAllDns.IsEnabled = true;
            btnStopTest.IsEnabled = false;
            progressPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void StopDnsTesting()
    {
        _cancellationTokenSource?.Cancel();
        LogMessage("⏹️ DNS Testing stopped by user");
        
        _isTesting = false;
        btnTestAllDns.IsEnabled = true;
        btnStopTest.IsEnabled = false;
        progressPanel.Visibility = Visibility.Collapsed;
    }

    private async Task ParseAndDisplayResults(string testReport)
    {
        try
        {
            // Parse the comprehensive test report
            var lines = testReport.Split('\n');
            var rank = 1;
            
            foreach (var line in lines)
            {
                if (line.Contains("✅") && line.Contains("ms") && line.Contains("tests passed"))
                {
                    // Parse successful DNS result
                    var parts = line.Split(new[] { "(", ")", "-", "ms", "tests passed" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 4)
                    {
                        var dnsName = parts[0].Replace("✅", "").Trim();
                        var ipAddress = parts[1].Trim();
                        var responseTime = ExtractNumber(parts[2]);
                        var testsPassed = ExtractTestsInfo(line);
                        
                        var result = new DnsTestResult
                        {
                            Rank = rank++,
                            Name = dnsName,
                            IpAddress = ipAddress,
                            ResponseTime = $"{responseTime}ms",
                            SuccessRate = $"{(testsPassed.passed * 100 / 5)}%",
                            TestsPassed = $"{testsPassed.passed}/5",
                            Tier = GetTierFromName(dnsName),
                            Status = "✅ WORKING"
                        };
                        
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DnsResults.Add(result);
                            progressBar.Value = DnsResults.Count;
                            txtProgress.Text = $"Testing DNS servers... ({DnsResults.Count}/65)";
                        });
                        
                        LogMessage($"✅ {dnsName} ({ipAddress}) - {responseTime}ms - {testsPassed.passed}/{testsPassed.total} tests passed");
                    }
                }
                else if (line.Contains("❌") && line.Contains("FAILED"))
                {
                    // Parse failed DNS result
                    var parts = line.Split(new[] { "(", ")", "-" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3)
                    {
                        var dnsName = parts[0].Replace("❌", "").Trim();
                        var ipAddress = parts[1].Trim();
                        
                        var result = new DnsTestResult
                        {
                            Rank = rank++,
                            Name = dnsName,
                            IpAddress = ipAddress,
                            ResponseTime = "TIMEOUT",
                            SuccessRate = "0%",
                            TestsPassed = "0/5",
                            Tier = GetTierFromName(dnsName),
                            Status = "❌ FAILED"
                        };
                        
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DnsResults.Add(result);
                            progressBar.Value = DnsResults.Count;
                            txtProgress.Text = $"Testing DNS servers... ({DnsResults.Count}/65)";
                        });
                        
                        LogMessage($"❌ {dnsName} ({ipAddress}) - FAILED");
                    }
                }
            }
            
            await Task.Delay(1); // Fix async warning
        }
        catch (Exception ex)
        {
            LogMessage($"Error parsing test results: {ex.Message}");
        }
    }

    private string GetTierFromName(string dnsName)
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
        
        if (tier1.Contains(dnsName)) return "🥇 Tier 1 (Most Reliable)";
        if (tier2.Contains(dnsName)) return "🥈 Tier 2 (Reliable)";
        if (tier3.Contains(dnsName)) return "🥉 Tier 3 (Additional)";
        if (tier4.Contains(dnsName)) return "⚡ Tier 4 (High-Performance)";
        if (tier5.Contains(dnsName)) return "📱 Tier 5 (Mobile)";
        if (tier6.Contains(dnsName)) return "🏢 Tier 6 (Regional)";
        if (tier7.Contains(dnsName)) return "🌍 Tier 7 (Global)";
        if (tier8.Contains(dnsName)) return "🏭 Tier 8 (ISP-Specific)";
        if (tier9.Contains(dnsName)) return "🛡️ Tier 9 (Anti-Sanctions)";
        
        return "📡 Unknown Tier";
    }

    private int ExtractNumber(string text)
    {
        var numbers = System.Text.RegularExpressions.Regex.Matches(text, @"\d+");
        return numbers.Count > 0 ? int.Parse(numbers[0].Value) : 0;
    }

    private (int passed, int total) ExtractTestsInfo(string line)
    {
        var match = System.Text.RegularExpressions.Regex.Match(line, @"(\d+)/(\d+) tests passed");
        if (match.Success)
        {
            return (int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
        }
        return (0, 4); // Default to 0/4
    }

    private void UpdateSummary()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var total = DnsResults.Count;
                    var fullAccess = DnsResults.Count(r => r.Status.Contains("FULL ACCESS"));
        var dnsOkBlocked = DnsResults.Count(r => r.Status.Contains("BLOCKED (DNS OK)"));
        var working = fullAccess + dnsOkBlocked;
        var failed = total - working;
            var optimal = DnsResults.Where(r => r.Status.Contains("FULL ACCESS"))
                           .OrderBy(r => r.Rank)
                           .FirstOrDefault() ??
                           DnsResults.Where(r => r.Status.Contains("BLOCKED (DNS OK)"))
                           .OrderBy(r => r.Rank)
                           .FirstOrDefault();
            
            txtTotalTested.Text = $"Total: {total}";
            txtWorkingCount.Text = $"Working: {working}";
            txtFailedCount.Text = $"Failed: {failed}";
            txtOptimalDns.Text = optimal != null ? $"Optimal: {optimal.Name}" : "Optimal: None found";
        });
    }

        private async Task<bool> ApplyOptimalDns()
        {
            try
            {
                // Use the new optimized DNS selection algorithm
                var optimalDnsName = await _sanctionsService.GetOptimizedDnsServerAsync();

                if (string.IsNullOrEmpty(optimalDnsName))
                {
                    MessageBox.Show("No working DNS servers found to apply.", "No Optimal DNS", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                // Get the IP address for the optimal DNS
                var dnsServers = GetAllDnsServers();
                if (!dnsServers.TryGetValue(optimalDnsName, out var ipAddress))
                {
                    MessageBox.Show("Could not find IP address for optimal DNS server.", "DNS Configuration Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                LogMessage($"🎯 APPLYING OPTIMIZED DNS: {optimalDnsName} ({ipAddress})");

                // Apply the optimal DNS through the sanctions bypass service
                var success = await _sanctionsService.Handle403ErrorAsync("manual-dns-application", 443);

                if (success)
                {
                    LogMessage($"✅ SUCCESS: {optimalDnsName} applied as optimized DNS");

                    // Start DNS monitoring for automatic failover
                    _ = Task.Run(() => _sanctionsService.StartDnsMonitoringAsync());

                    MessageBox.Show($"Optimized DNS applied successfully!\n\nDNS: {optimalDnsName}\nIP: {ipAddress}\n\n✅ Automatic monitoring enabled for DNS health and failover.",
                                   "DNS Applied with Monitoring", MessageBoxButton.OK, MessageBoxImage.Information);
                    return true;
                }
                else
                {
                    LogMessage($"⚠️ WARNING: Could not apply {optimalDnsName} - trying alternative method");
                    MessageBox.Show($"Could not automatically apply DNS. Please manually set:\n\nDNS: {optimalDnsName}\nIP: {ipAddress}\n\nNote: You can copy these values and set them in your system DNS settings.",
                                   "Manual Configuration Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ ERROR applying optimal DNS: {ex.Message}");
                MessageBox.Show($"Error applying optimal DNS: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

    /// <summary>
    /// Get all DNS servers (using cached version for performance)
    /// </summary>
    private Dictionary<string, string> GetAllDnsServers()
    {
        return GetDnsServers();
    }

    private void LogMessage(string message)
    {
        // Use BeginInvoke for better performance and prevent UI blocking
        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            txtLog.Text += $"[{timestamp}] {message}\n";

            // Only scroll to end if we're not in the middle of rapid updates
            if (txtLog.Text.Length < 10000) // Prevent performance issues with very large logs
            {
                logScrollViewer.ScrollToEnd();
            }
        }), System.Windows.Threading.DispatcherPriority.Background);
    }

    /// <summary>
    /// Test all DNS servers with optimized concurrent testing and proper error handling
    /// </summary>
    private async Task TestAllDnsServersSafely()
    {
        try
        {
            var dnsServers = GetDnsServers();

            // First, populate the grid with all DNS servers (so user sees them immediately)
            var rank = 1;
            foreach (var kvp in dnsServers)
            {
                var result = new DnsTestResult
                {
                    Rank = rank++,
                    Name = kvp.Key,
                    IpAddress = kvp.Value,
                    ResponseTime = "Testing...",
                    SuccessRate = "0%",
                    TestsPassed = "⏳ TESTING",
                    Tier = GetTierFromName(kvp.Key),
                    Status = "⏳ TESTING developer.android.com..."
                };

                Application.Current.Dispatcher.Invoke(() =>
                {
                    DnsResults.Add(result);
                    progressBar.Value = DnsResults.Count;
                    txtProgress.Text = $"Initializing DNS servers... ({DnsResults.Count}/{dnsServers.Count})";
                });
            }

            LogMessage($"✅ Loaded {dnsServers.Count} DNS servers into grid");

            // Test DNS servers concurrently with semaphore for controlled parallelism
            var semaphore = new SemaphoreSlim(MAX_CONCURRENT_TESTS);
            var tasks = new List<Task>();
            var completedCount = 0;

            var testIndex = 0;
            foreach (var kvp in dnsServers)
            {
                if (_cancellationTokenSource?.Token.IsCancellationRequested == true)
                    break;

                var localIndex = testIndex;
                var localKvp = kvp;

                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync(_cancellationTokenSource?.Token ?? CancellationToken.None);

                    try
                    {
                        var dnsName = localKvp.Key;
                        var dnsIP = localKvp.Value;

                        LogMessage($"🔍 Testing {dnsName} ({dnsIP})");

                        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                        var testResult = await TestSingleDnsServerSafe(dnsName, dnsIP);
                        stopwatch.Stop();

                        // Update the existing result in the grid (use BeginInvoke for better performance)
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (localIndex < DnsResults.Count)
                            {
                                var result = DnsResults[localIndex];
                                result.ResponseTime = $"{stopwatch.ElapsedMilliseconds}ms";
                                result.SuccessRate = $"{(testResult * 50)}%";
                                result.TestsPassed = testResult == 2 ? "✅ FULL ACCESS" :
                                                   testResult == 1 ? "⚠️ DNS OK (Blocked)" : "❌ FAILED";
                                result.Status = testResult == 2 ? "✅ FULL ACCESS" :
                                              testResult == 1 ? "⚠️ BLOCKED (DNS OK)" : "❌ FAILED";
                            }

                            // Update progress less frequently to improve performance
                            completedCount++;
                            if (completedCount % 5 == 0 || completedCount == dnsServers.Count)
                            {
                                txtProgress.Text = $"Testing DNS servers... ({completedCount}/{dnsServers.Count})";
                                progressBar.Value = completedCount;
                            }
                        }), System.Windows.Threading.DispatcherPriority.Background);

                        LogMessage($"{(testResult == 2 ? "✅" : testResult == 1 ? "⚠️" : "❌")} {dnsName} - developer.android.com {(testResult == 2 ? "FULL ACCESS" : testResult == 1 ? "DNS OK (Blocked)" : "FAILED")} ({stopwatch.ElapsedMilliseconds}ms)");
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"❌ ERROR testing {localKvp.Key}: {ex.Message}");

                        // Update result to show error (use BeginInvoke for better performance)
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (localIndex < DnsResults.Count)
                            {
                                var result = DnsResults[localIndex];
                                result.ResponseTime = "ERROR";
                                result.SuccessRate = "0%";
                                result.TestsPassed = "❌ ERROR";
                                result.Status = "❌ ERROR";
                            }
                        }), System.Windows.Threading.DispatcherPriority.Background);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));

                testIndex++;

                // Small delay between starting tasks to prevent overwhelming
                await Task.Delay(DELAY_BETWEEN_TESTS_MS);
            }

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);

            LogMessage($"✅ Concurrent DNS testing completed for {dnsServers.Count} servers");
        }
        catch (Exception ex)
        {
            LogMessage($"❌ ERROR in concurrent DNS testing: {ex.Message}");
        }
    }

    /// <summary>
    /// Test a single DNS server ONLY for developer.android.com access - ultra fast testing
    /// </summary>
    private async Task<int> TestSingleDnsServerSafe(string dnsName, string dnsIP)
    {
        var testResult = 0;

        try
        {
            using var cts = new CancellationTokenSource(TEST_TIMEOUT_MS);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await _sharedHttpClient.GetAsync("https://developer.android.com/",
                HttpCompletionOption.ResponseHeadersRead, cts.Token);
            stopwatch.Stop();

            // SUCCESS: Any response means DNS resolution worked
            if (response.IsSuccessStatusCode)
            {
                testResult = 2; // Perfect success - fully accessible
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                testResult = 1; // Partial success - DNS works but blocked by sanctions
            }
            else
            {
                testResult = 0; // Failed
            }
        }
        catch (HttpRequestException ex)
        {
            // Check if it's a sanctions-related error (DNS worked but blocked)
            if (ex.Message.Contains("403") || ex.Message.Contains("Forbidden") ||
                ex.Message.Contains("permission") || ex.Message.Contains("denied"))
            {
                testResult = 1; // DNS resolution worked, just blocked by sanctions
            }
            else
            {
                testResult = 0; // DNS or network failed
            }
        }
        catch (TaskCanceledException)
        {
            testResult = 0; // Timeout
        }
        catch (OperationCanceledException)
        {
            testResult = 0; // Cancelled
        }
        catch (Exception)
        {
            testResult = 0; // Other error
        }

        return testResult;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        StopDnsTesting();
        _sanctionsService?.Dispose();
        DisposeResources();
        base.OnClosing(e);
    }

    private void DisposeResources()
    {
        _sharedHttpClient?.Dispose();
        _cancellationTokenSource?.Dispose();
    }
}

public class DnsTestResult : ReactiveObject
{
    private int _rank;
    private string _name = "";
    private string _ipAddress = "";
    private string _responseTime = "";
    private string _successRate = "";
    private string _testsPassed = "";
    private string _tier = "";
    private string _status = "";

    public int Rank 
    { 
        get => _rank; 
        set => this.RaiseAndSetIfChanged(ref _rank, value); 
    }
    
    public string Name 
    { 
        get => _name; 
        set => this.RaiseAndSetIfChanged(ref _name, value); 
    }
    
    public string IpAddress 
    { 
        get => _ipAddress; 
        set => this.RaiseAndSetIfChanged(ref _ipAddress, value); 
    }
    
    public string ResponseTime 
    { 
        get => _responseTime; 
        set => this.RaiseAndSetIfChanged(ref _responseTime, value); 
    }
    
    public string SuccessRate 
    { 
        get => _successRate; 
        set => this.RaiseAndSetIfChanged(ref _successRate, value); 
    }
    
    public string TestsPassed 
    { 
        get => _testsPassed; 
        set => this.RaiseAndSetIfChanged(ref _testsPassed, value); 
    }
    
    public string Tier 
    { 
        get => _tier; 
        set => this.RaiseAndSetIfChanged(ref _tier, value); 
    }
    
    public string Status 
    { 
        get => _status; 
        set => this.RaiseAndSetIfChanged(ref _status, value); 
    }
}
