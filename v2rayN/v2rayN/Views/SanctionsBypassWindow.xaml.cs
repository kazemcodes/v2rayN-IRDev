using System.Reactive.Disposables;
using System.Windows;
using ReactiveUI;
using ServiceLib.Manager;
using ServiceLib.ViewModels;
using v2rayN.Base;
using ServiceLib.Common;
using ServiceLib.Services.CoreConfig;

namespace v2rayN.Views;

public partial class SanctionsBypassWindow : WindowBase<SanctionsBypassViewModel>
{
    private static Config _config;

    public SanctionsBypassWindow()
    {
        InitializeComponent();

        this.Owner = Application.Current.MainWindow;
        _config = AppManager.Instance.Config;

        ViewModel = new SanctionsBypassViewModel();

        // Load Iranian DNS server options
        var iranianDnsServers = new[]
        {
            "electro-primary", "electro-secondary", "radar-primary", "radar-secondary",
            "shelter-primary", "shelter-secondary", "403-primary", "403-secondary",
            "begzar-primary", "begzar-secondary", "shecan-primary", "shecan-secondary",
            "asan-primary", "asan-secondary", "asan-dns"
        };
        cmbPreferredIranianDnsServer.ItemsSource = iranianDnsServers;

        this.WhenActivated(disposables =>
        {
            // Override the save command to call our SaveConfiguration method
            btnSave.Click += (s, e) => SaveConfiguration();
            this.OneWayBind(ViewModel, vm => vm.TestConnectionCommand, v => v.btnTestConnection.Command).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.UpdateDomainsListCommand, v => v.btnUpdateDomainsList.Command).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.ApplyIranPresetCommand, v => v.btnApplyIranPreset.Command).DisposeWith(disposables);

            // Bind configuration properties
            this.Bind(ViewModel, vm => vm.EnableSanctionsDetection, v => v.chkEnableSanctionsDetection.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.EnableIranianDnsAutoSwitch, v => v.chkEnableIranianDnsAutoSwitch.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.EnableTransparentMirroring, v => v.chkEnableTransparentMirroring.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.EnableHardBlockOnFailure, v => v.chkEnableHardBlockOnFailure.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.AutoUpdateBlockedDomainsList, v => v.chkAutoUpdateBlockedDomainsList.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.PreferredIranianDnsServer, v => v.cmbPreferredIranianDnsServer.SelectedValue).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SanctionsCheckIntervalMinutes, v => v.txtSanctionsCheckInterval.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.DnsTimeoutSeconds, v => v.txtDnsTimeout.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.CurrentStatus, v => v.txtCurrentStatus.Text).DisposeWith(disposables);

            btnCancel.Click += (s, e) => this.Close();
        });

        LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        try
        {
            var iranConfig = _config.IranSanctionsBypassItem ?? new ServiceLib.Models.IranSanctionsBypassItem();
            
            ViewModel.EnableSanctionsDetection = iranConfig.EnableSanctionsDetection;
            ViewModel.EnableIranianDnsAutoSwitch = iranConfig.EnableIranianDnsAutoSwitch;
            ViewModel.EnableTransparentMirroring = iranConfig.EnableTransparentMirroring;
            ViewModel.EnableHardBlockOnFailure = iranConfig.EnableHardBlockOnFailure;
            ViewModel.AutoUpdateBlockedDomainsList = iranConfig.AutoUpdateBlockedDomainsList;
            ViewModel.PreferredIranianDnsServer = iranConfig.PreferredIranianDnsServer;
            ViewModel.SanctionsCheckIntervalMinutes = iranConfig.SanctionsCheckIntervalMinutes.ToString();
            ViewModel.DnsTimeoutSeconds = iranConfig.DnsTimeoutSeconds.ToString();

            Logging.SaveLog("SanctionsBypassWindow: Configuration loaded successfully");
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"SanctionsBypassWindow: Error loading configuration - {ex.Message}");
        }
    }

    private async void SaveConfiguration()
    {
        try
        {
            // Disable the save button to prevent multiple clicks
            btnSave.IsEnabled = false;
            btnSave.Content = "Saving...";
            
            Logging.SaveLog("SanctionsBypassWindow: STARTING SAVE OPERATION...");
            
            if (_config.IranSanctionsBypassItem == null)
            {
                _config.IranSanctionsBypassItem = new ServiceLib.Models.IranSanctionsBypassItem();
                Logging.SaveLog("SanctionsBypassWindow: Created new IranSanctionsBypassItem");
            }

            // Validate input values before saving
            if (!ValidateConfiguration())
            {
                return; // Validation failed, button will be re-enabled in finally block
            }

            // Save all settings with debug logging
            _config.IranSanctionsBypassItem.EnableSanctionsDetection = ViewModel.EnableSanctionsDetection;
            _config.IranSanctionsBypassItem.EnableIranianDnsAutoSwitch = ViewModel.EnableIranianDnsAutoSwitch;
            _config.IranSanctionsBypassItem.EnableTransparentMirroring = ViewModel.EnableTransparentMirroring;
            _config.IranSanctionsBypassItem.EnableHardBlockOnFailure = ViewModel.EnableHardBlockOnFailure;
            _config.IranSanctionsBypassItem.AutoUpdateBlockedDomainsList = ViewModel.AutoUpdateBlockedDomainsList;
            _config.IranSanctionsBypassItem.PreferredIranianDnsServer = ViewModel.PreferredIranianDnsServer;

            Logging.SaveLog($"SanctionsBypassWindow: SAVING SETTINGS - " +
                $"Detection:{ViewModel.EnableSanctionsDetection}, " +
                $"DNS:{ViewModel.EnableIranianDnsAutoSwitch}, " +
                $"Mirroring:{ViewModel.EnableTransparentMirroring}, " +
                $"DNS Server:{ViewModel.PreferredIranianDnsServer}");

            if (int.TryParse(ViewModel.SanctionsCheckIntervalMinutes, out var interval) && interval > 0)
                _config.IranSanctionsBypassItem.SanctionsCheckIntervalMinutes = interval;

            if (int.TryParse(ViewModel.DnsTimeoutSeconds, out var timeout) && timeout > 0)
                _config.IranSanctionsBypassItem.DnsTimeoutSeconds = timeout;

            // Use ConfigureAwait(false) to prevent deadlock in UI context
            Logging.SaveLog("SanctionsBypassWindow: Starting async save with 15 second timeout...");
            
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            
            try
            {
                var saveResult = await ConfigHandler.SaveConfig(_config).ConfigureAwait(false);
                
                // Use Dispatcher.Invoke for UI updates after async operation
                Dispatcher.Invoke(() =>
                {
                    Logging.SaveLog($"SanctionsBypassWindow: SaveConfig completed successfully with result: {saveResult}");
                    if (saveResult == 0)
                    {
                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("Failed to save configuration. Please try again.", "Save Error", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                });
            }
            catch (OperationCanceledException)
            {
                Logging.SaveLog("SanctionsBypassWindow: Save operation timed out after 15 seconds");
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Configuration save operation timed out. Please try again.", "Timeout", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"SanctionsBypassWindow: Error saving configuration - {ex.Message}");
            Logging.SaveLog($"SanctionsBypassWindow: Exception details - {ex}");
            
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
        finally
        {
            // Ensure button is re-enabled on UI thread
            Dispatcher.Invoke(() =>
            {
                if (btnSave != null)
                {
                    btnSave.IsEnabled = true;
                    if (btnSave.Content.ToString() == "Saving...")
                        btnSave.Content = "Save";
                }
            });
        }
    }

    private bool ValidateConfiguration()
    {
        // Validate numeric inputs
        if (!int.TryParse(ViewModel.SanctionsCheckIntervalMinutes, out var interval) || interval <= 0)
        {
            MessageBox.Show("Sanctions check interval must be a positive number.", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (!int.TryParse(ViewModel.DnsTimeoutSeconds, out var timeout) || timeout <= 0)
        {
            MessageBox.Show("DNS timeout must be a positive number.", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        // Validate DNS server selection
        if (string.IsNullOrWhiteSpace(ViewModel.PreferredIranianDnsServer))
        {
            MessageBox.Show("Please select a preferred Iranian DNS server.", "Validation Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        return true;
    }
}






