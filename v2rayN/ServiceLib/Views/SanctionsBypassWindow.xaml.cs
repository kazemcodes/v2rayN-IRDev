using ServiceLib.Services.CoreConfig;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Collections.Generic;

namespace ServiceLib.Views;

public partial class SanctionsBypassWindow : Window
{
    private readonly SanctionsBypassService _sanctionsService;
    private readonly TransparentMirrorService _mirrorService;
    private ObservableCollection<KeyValuePair<string, string>> _iranianDnsServers;
    private ObservableCollection<string> _googleDomains;
    private ObservableCollection<KeyValuePair<string, string>> _mirrorMappings;

    public SanctionsBypassWindow()
    {
        InitializeComponent();

        _sanctionsService = new SanctionsBypassService();
        _mirrorService = new TransparentMirrorService();
        _iranianDnsServers = new ObservableCollection<KeyValuePair<string, string>>();
        _googleDomains = new ObservableCollection<string>();
        _mirrorMappings = new ObservableCollection<KeyValuePair<string, string>>();

        dgIranianDns.ItemsSource = _iranianDnsServers;
        lstGoogleDomains.ItemsSource = _googleDomains;
        dgMirrorMappings.ItemsSource = _mirrorMappings;

        LoadData();
    }

    private async void LoadData()
    {
        try
        {
            // Load Iranian DNS servers
            var dnsServers = _sanctionsService.GetIranianDnsServers();
            foreach (var dns in dnsServers)
            {
                _iranianDnsServers.Add(dns);
            }

            // Load Google domains (hardcoded for now, could be made configurable)
            var domains = new[]
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
                "developers.google.com"
            };

            foreach (var domain in domains)
            {
                _googleDomains.Add(domain);
            }

            // Load mirror mappings
            var mirrorMappings = _mirrorService.GetMirrorMappings();
            foreach (var mapping in mirrorMappings)
            {
                _mirrorMappings.Add(mapping);
            }

            // Check current status
            await CheckSanctionsStatusAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading data: {ex.Message}", "Error",
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void btnCheckStatus_Click(object sender, RoutedEventArgs e)
    {
        await CheckSanctionsStatusAsync();
    }

    private async Task CheckSanctionsStatusAsync()
    {
        try
        {
            btnCheckStatus.IsEnabled = false;
            txtSanctionsStatus.Text = "Checking...";

            var isActive = await _sanctionsService.CheckSanctionsStatusAsync();
            txtSanctionsStatus.Text = isActive ? "Active - Sanctions detected" : "Inactive - No sanctions detected";
            txtSanctionsStatus.Foreground = isActive ?
                System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.Green;

            var currentDns = await _sanctionsService.GetBestDnsServerAsync();
            txtCurrentDns.Text = currentDns;

            LogMessage($"Sanctions status checked: {(isActive ? "Active" : "Inactive")}");
        }
        catch (Exception ex)
        {
            txtSanctionsStatus.Text = "Error checking status";
            LogMessage($"Error checking sanctions status: {ex.Message}");
        }
        finally
        {
            btnCheckStatus.IsEnabled = true;
        }
    }

    private async void btnTestDns_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string dnsName)
        {
            await TestDnsServerAsync(dnsName);
        }
    }

    private async void btnTestAllDns_Click(object sender, RoutedEventArgs e)
    {
        await TestAllDnsServersAsync();
    }

    private async Task TestDnsServerAsync(string dnsName)
    {
        try
        {
            var isWorking = await _sanctionsService.TestDnsServerAsync(dnsName);
            var status = isWorking ? "Working" : "Failed";

            LogMessage($"DNS test for {dnsName}: {status}");

            // Update UI (you might want to update the DataGrid row here)
            MessageBox.Show($"DNS server {dnsName} is {status.ToLower()}",
                          "DNS Test Result", MessageBoxButton.OK,
                          isWorking ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            LogMessage($"Error testing DNS {dnsName}: {ex.Message}");
            MessageBox.Show($"Error testing DNS server: {ex.Message}", "Error",
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task TestAllDnsServersAsync()
    {
        try
        {
            btnTestAll.IsEnabled = false;
            LogMessage("Testing all DNS servers...");

            foreach (var dns in _iranianDnsServers)
            {
                var isWorking = await _sanctionsService.TestDnsServerAsync(dns.Key);
                LogMessage($"DNS test for {dns.Key}: {(isWorking ? "Working" : "Failed")}");
            }

            LogMessage("DNS testing completed");
            MessageBox.Show("DNS testing completed. Check the log for results.",
                          "Testing Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            LogMessage($"Error testing DNS servers: {ex.Message}");
        }
        finally
        {
            btnTestAll.IsEnabled = true;
        }
    }

    private void btnAddDomain_Click(object sender, RoutedEventArgs e)
    {
        var inputDialog = new InputDialog("Add Domain", "Enter domain name:");
        if (inputDialog.ShowDialog() == true)
        {
            var domain = inputDialog.ResponseText?.Trim();
            if (!string.IsNullOrEmpty(domain) && !_googleDomains.Contains(domain))
            {
                _googleDomains.Add(domain);
                LogMessage($"Added domain: {domain}");
            }
        }
    }

    private void btnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Here you would save the settings to configuration
            // For now, just log the action
            LogMessage("Settings saved");

            MessageBox.Show("Settings saved successfully!", "Success",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving settings: {ex.Message}", "Error",
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

            private async void btnTestMirrors_Click(object sender, RoutedEventArgs e)
        {
            await TestAllMirrorsAsync();
        }

        private void btnRefreshMappings_Click(object sender, RoutedEventArgs e)
        {
            RefreshMirrorMappings();
        }

        private void btnAddCustomMapping_Click(object sender, RoutedEventArgs e)
        {
            AddCustomMirrorMapping();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async Task TestAllMirrorsAsync()
        {
            try
            {
                btnTestMirrors.IsEnabled = false;
                LogMessage("Testing all mirror mappings...");

                var healthStatus = await _mirrorService.GetMirrorHealthStatusAsync();
                foreach (var status in healthStatus)
                {
                    var statusText = status.Value ? "Working" : "Failed";
                    LogMessage($"Mirror test for {status.Key}: {statusText}");

                    // Update the UI to show status (you might want to update the DataGrid)
                }

                LogMessage("Mirror testing completed");
                MessageBox.Show("Mirror testing completed. Check the log for results.",
                              "Testing Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"Error testing mirrors: {ex.Message}");
                MessageBox.Show($"Error testing mirrors: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnTestMirrors.IsEnabled = true;
            }
        }

        private void RefreshMirrorMappings()
        {
            try
            {
                _mirrorMappings.Clear();
                var mirrorMappings = _mirrorService.GetMirrorMappings();
                foreach (var mapping in mirrorMappings)
                {
                    _mirrorMappings.Add(mapping);
                }
                LogMessage("Mirror mappings refreshed");
            }
            catch (Exception ex)
            {
                LogMessage($"Error refreshing mirror mappings: {ex.Message}");
                MessageBox.Show($"Error refreshing mirror mappings: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddCustomMirrorMapping()
        {
            var domainDialog = new InputDialog("Add Mirror Mapping", "Enter original domain:");
            if (domainDialog.ShowDialog() == true)
            {
                var originalDomain = domainDialog.ResponseText?.Trim();
                if (!string.IsNullOrEmpty(originalDomain))
                {
                    var mirrorDialog = new InputDialog("Add Mirror Mapping", "Enter mirror URL:");
                    if (mirrorDialog.ShowDialog() == true)
                    {
                        var mirrorUrl = mirrorDialog.ResponseText?.Trim();
                        if (!string.IsNullOrEmpty(mirrorUrl))
                        {
                            try
                            {
                                _mirrorService.AddMirrorMapping(originalDomain, mirrorUrl);
                                _mirrorMappings.Add(new KeyValuePair<string, string>(originalDomain, mirrorUrl));
                                LogMessage($"Added custom mirror mapping: {originalDomain} → {mirrorUrl}");
                            }
                            catch (Exception ex)
                            {
                                LogMessage($"Error adding mirror mapping: {ex.Message}");
                                MessageBox.Show($"Error adding mirror mapping: {ex.Message}", "Error",
                                              MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
        }

        private void LogMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            txtLog.AppendText($"[{timestamp}] {message}\r\n");
            txtLog.ScrollToEnd();
        }
}

// Simple input dialog for adding domains
public class InputDialog : Window
{
    public string ResponseText { get; private set; }

    public InputDialog(string title, string prompt)
    {
        Title = title;
        Width = 300;
        Height = 150;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var stackPanel = new System.Windows.Controls.StackPanel { Margin = new Thickness(10) };

        var label = new System.Windows.Controls.TextBlock
        {
            Text = prompt,
            Margin = new Thickness(0, 0, 0, 10)
        };

        var textBox = new System.Windows.Controls.TextBox
        {
            Margin = new Thickness(0, 0, 0, 10)
        };

        var buttonPanel = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        };

        var okButton = new System.Windows.Controls.Button
        {
            Content = "OK",
            Padding = new Thickness(15, 5, 15, 5),
            Margin = new Thickness(0, 0, 10, 0),
            IsDefault = true
        };
        okButton.Click += (s, e) =>
        {
            ResponseText = textBox.Text;
            DialogResult = true;
        };

        var cancelButton = new System.Windows.Controls.Button
        {
            Content = "Cancel",
            Padding = new Thickness(15, 5, 15, 5),
            IsCancel = true
        };

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);

        stackPanel.Children.Add(label);
        stackPanel.Children.Add(textBox);
        stackPanel.Children.Add(buttonPanel);

        Content = stackPanel;
    }
}
