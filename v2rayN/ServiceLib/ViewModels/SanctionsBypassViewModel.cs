using ReactiveUI;
using ServiceLib.Services.CoreConfig;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using ServiceLib.Common;

namespace ServiceLib.ViewModels;

public class SanctionsBypassViewModel : ReactiveObject
{
    private readonly SanctionsBypassService _sanctionsService;
    private readonly TransparentMirrorService _mirrorService;
    
    // Configuration Properties
    private bool _enableSanctionsDetection = true;
    private bool _enableIranianDnsAutoSwitch = true;
    private bool _enableTransparentMirroring = true;
    private bool _enableHardBlockOnFailure = true;
    private bool _autoUpdateBlockedDomainsList = true;
    private string _preferredIranianDnsServer = "electro-primary";
    private string _sanctionsCheckIntervalMinutes = "5";
    private string _dnsTimeoutSeconds = "10";
    private string _currentStatus = "Ready";

    public SanctionsBypassViewModel()
    {
        _sanctionsService = new SanctionsBypassService();
        _mirrorService = new TransparentMirrorService();

        // Initialize commands
        SaveCommand = ReactiveCommand.Create(Save);
        TestConnectionCommand = ReactiveCommand.CreateFromTask(TestConnectionAsync);
        UpdateDomainsListCommand = ReactiveCommand.CreateFromTask(UpdateDomainsListAsync);
        ApplyIranPresetCommand = ReactiveCommand.CreateFromTask(ApplyIranPresetAsync);
    }

    // Configuration Properties
    public bool EnableSanctionsDetection
    {
        get => _enableSanctionsDetection;
        set => this.RaiseAndSetIfChanged(ref _enableSanctionsDetection, value);
    }

    public bool EnableIranianDnsAutoSwitch
    {
        get => _enableIranianDnsAutoSwitch;
        set => this.RaiseAndSetIfChanged(ref _enableIranianDnsAutoSwitch, value);
    }

    public bool EnableTransparentMirroring
    {
        get => _enableTransparentMirroring;
        set => this.RaiseAndSetIfChanged(ref _enableTransparentMirroring, value);
    }

    public bool EnableHardBlockOnFailure
    {
        get => _enableHardBlockOnFailure;
        set => this.RaiseAndSetIfChanged(ref _enableHardBlockOnFailure, value);
    }

    public bool AutoUpdateBlockedDomainsList
    {
        get => _autoUpdateBlockedDomainsList;
        set => this.RaiseAndSetIfChanged(ref _autoUpdateBlockedDomainsList, value);
    }

    public string PreferredIranianDnsServer
    {
        get => _preferredIranianDnsServer;
        set => this.RaiseAndSetIfChanged(ref _preferredIranianDnsServer, value);
    }

    public string SanctionsCheckIntervalMinutes
    {
        get => _sanctionsCheckIntervalMinutes;
        set => this.RaiseAndSetIfChanged(ref _sanctionsCheckIntervalMinutes, value);
    }

    public string DnsTimeoutSeconds
    {
        get => _dnsTimeoutSeconds;
        set => this.RaiseAndSetIfChanged(ref _dnsTimeoutSeconds, value);
    }

    public string CurrentStatus
    {
        get => _currentStatus;
        set => this.RaiseAndSetIfChanged(ref _currentStatus, value);
    }

    // Commands
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> TestConnectionCommand { get; }
    public ReactiveCommand<Unit, Unit> UpdateDomainsListCommand { get; }
    public ReactiveCommand<Unit, Unit> ApplyIranPresetCommand { get; }

    private void Save()
    {
        // The actual saving is handled by the Window's SaveConfiguration method
        CurrentStatus = "Configuration saved successfully";
        Logging.SaveLog("SanctionsBypassViewModel: Save command executed");
    }

    private async Task TestConnectionAsync()
    {
        try
        {
            CurrentStatus = "Testing Iranian sanctions bypass...";
            
            var (canConnect, reason) = await _sanctionsService.ValidateConnectionAsync();
            
            if (canConnect)
            {
                CurrentStatus = "✅ Iranian sanctions bypass is working properly!";
                Logging.SaveLog("SanctionsBypassViewModel: Test connection successful");
            }
            else
            {
                CurrentStatus = $"❌ Test failed: {reason}";
                Logging.SaveLog($"SanctionsBypassViewModel: Test connection failed - {reason}");
            }
        }
        catch (Exception ex)
        {
            CurrentStatus = $"❌ Test error: {ex.Message}";
            Logging.SaveLog($"SanctionsBypassViewModel: Test connection error - {ex.Message}");
        }
    }

    private async Task UpdateDomainsListAsync()
    {
        try
        {
            CurrentStatus = "Updating blocked domains list from GitHub...";
            
            await _sanctionsService.UpdateBlockedDomainsListAsync();
            
            CurrentStatus = "✅ Blocked domains list updated successfully!";
            Logging.SaveLog("SanctionsBypassViewModel: Domains list updated successfully");
        }
        catch (Exception ex)
        {
            CurrentStatus = $"❌ Update failed: {ex.Message}";
            Logging.SaveLog($"SanctionsBypassViewModel: Domains list update failed - {ex.Message}");
        }
    }

    private async Task ApplyIranPresetAsync()
    {
        try
        {
            CurrentStatus = "Applying Iran preset configuration...";
            
            // Enable all Iranian bypass features
            EnableSanctionsDetection = true;
            EnableIranianDnsAutoSwitch = true;
            EnableTransparentMirroring = true;
            EnableHardBlockOnFailure = true;
            AutoUpdateBlockedDomainsList = true;
            PreferredIranianDnsServer = "electro-primary";
            SanctionsCheckIntervalMinutes = "5";
            DnsTimeoutSeconds = "10";

            // Enable Iranian DNS and mirroring
            await _sanctionsService.EnableIranianDnsAsync();
            await _mirrorService.EnableTransparentMirroringAsync();
            
            CurrentStatus = "✅ Iran preset applied successfully! All features enabled.";
            Logging.SaveLog("SanctionsBypassViewModel: Iran preset applied successfully");
        }
        catch (Exception ex)
        {
            CurrentStatus = $"❌ Preset application failed: {ex.Message}";
            Logging.SaveLog($"SanctionsBypassViewModel: Iran preset application failed - {ex.Message}");
        }
    }
}