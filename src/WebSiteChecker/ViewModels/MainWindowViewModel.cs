using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WebSiteChecker.Helpers;
using WebSiteChecker.Models;
using WebSiteChecker.Services;
using WebSiteChecker.Views;

namespace WebSiteChecker.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ConfigStore _configStore;
    private readonly IWebsiteMonitorService _monitorService;
    private readonly SmtpEmailNotifier _emailNotifier;
    private readonly ThemeService _themeService;

    public ObservableCollection<SiteListItemViewModel> Sites { get; } = [];

    [ObservableProperty]
    private string _siteCountText = "0 site izleniyor";

    [ObservableProperty]
    private int _siteCount;

    [ObservableProperty]
    private SiteListItemViewModel? _selectedSite;

    [ObservableProperty]
    private string _monitorStatusText = "İzleme aktif";

    [ObservableProperty]
    private bool _isMonitorPaused;

    [ObservableProperty]
    private bool _isDarkMode;

    [ObservableProperty]
    private string _themeToggleText = "Koyu Mod";

    [ObservableProperty]
    private bool _runAtStartup;

    public MainWindowViewModel(
        ConfigStore configStore,
        IWebsiteMonitorService monitorService,
        SmtpEmailNotifier emailNotifier,
        ThemeService themeService)
    {
        _configStore = configStore;
        _monitorService = monitorService;
        _emailNotifier = emailNotifier;
        _themeService = themeService;

        _monitorService.SiteStateChanged += OnSiteStateChanged;
        _themeService.ThemeChanged += (_, _) => UpdateThemeState();
        UpdateThemeState();
        LoadStartupPreference();

        LoadSites();
        UpdateMonitorStatus();
    }

    private void UpdateThemeState()
    {
        IsDarkMode = _themeService.IsDarkMode;
        ThemeToggleText = IsDarkMode ? "Açık Mod" : "Koyu Mod";
    }

    private void LoadStartupPreference()
    {
        var settings = _configStore.LoadUiSettings();
        RunAtStartup = settings.RunAtStartup;
        StartupRegistryHelper.SetStartupEnabled(RunAtStartup);
    }

    partial void OnRunAtStartupChanged(bool value)
    {
        var settings = _configStore.LoadUiSettings();
        settings.RunAtStartup = value;
        _configStore.SaveUiSettings(settings);
        StartupRegistryHelper.SetStartupEnabled(value);
    }

    private void LoadSites()
    {
        Sites.Clear();
        var sites = _configStore.LoadSites();
        var runtimeStates = _monitorService.RuntimeStates;

        foreach (var site in sites)
        {
            var item = new SiteListItemViewModel
            {
                Id = site.Id,
                Name = site.Name,
                Url = site.Url,
                IntervalSeconds = site.IntervalSeconds,
                TimeoutSeconds = site.TimeoutSeconds,
                ExpectedStatusCode = site.ExpectedStatusCode,
                IsEnabled = site.IsEnabled
            };

            if (runtimeStates.TryGetValue(site.Id, out var state))
                item.UpdateFromRuntime(state);

            Sites.Add(item);
        }

        SiteCountText = Sites.Count switch
        {
            0 => "Henüz site eklenmedi",
            1 => "1 site izleniyor",
            _ => $"{Sites.Count} site izleniyor"
        };
        SiteCount = Sites.Count;
    }

    private void OnSiteStateChanged(object? sender, SiteRuntimeState state)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var item = Sites.FirstOrDefault(s => s.Id == state.SiteId);
            item?.UpdateFromRuntime(state);
        });
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        _themeService.ToggleTheme();
    }

    [RelayCommand]
    private void AddSite()
    {
        var sites = _configStore.LoadSites();
        if (sites.Count >= SiteLimits.MaxSites)
        {
            DialogHelper.ShowError($"En fazla {SiteLimits.MaxSites} site eklenebilir.");
            return;
        }

        var dialog = new AddEditSiteWindow();
        if (dialog.ShowDialogCentered() != true || dialog.ResultSite is null)
            return;

        if (sites.Count >= SiteLimits.MaxSites)
        {
            DialogHelper.ShowError($"En fazla {SiteLimits.MaxSites} site eklenebilir.");
            return;
        }

        sites.Add(dialog.ResultSite);
        _configStore.SaveSites(sites);
        _monitorService.ReloadSites();
        LoadSites();
    }

    [RelayCommand]
    private void EditSite()
    {
        if (SelectedSite is null)
        {
            DialogHelper.ShowInfo("Düzenlemek için bir site seçin.");
            return;
        }

        var existing = _configStore.LoadSites().FirstOrDefault(s => s.Id == SelectedSite.Id);
        if (existing is null)
            return;

        var dialog = new AddEditSiteWindow(existing);
        if (dialog.ShowDialogCentered() != true || dialog.ResultSite is null)
            return;

        var sites = _configStore.LoadSites();
        var index = sites.FindIndex(s => s.Id == dialog.ResultSite.Id);
        if (index >= 0)
        {
            sites[index] = dialog.ResultSite;
            _configStore.SaveSites(sites);
            _monitorService.ReloadSites();
            LoadSites();
        }
    }

    [RelayCommand]
    private void DeleteSite()
    {
        if (SelectedSite is null)
        {
            DialogHelper.ShowInfo("Silmek için bir site seçin.");
            return;
        }

        if (!DialogHelper.Confirm($"'{SelectedSite.Name}' sitesini silmek istediğinize emin misiniz?"))
            return;

        var sites = _configStore.LoadSites().Where(s => s.Id != SelectedSite.Id).ToList();
        _configStore.SaveSites(sites);
        _monitorService.ReloadSites();
        LoadSites();
    }

    [RelayCommand]
    private void OpenSmtpSettings()
    {
        var dialog = new SmtpSettingsWindow(_configStore);
        dialog.ShowDialogCentered();
    }

    [RelayCommand]
    private async Task SendTestEmailAsync()
    {
        try
        {
            await _emailNotifier.SendTestEmailAsync();
            DialogHelper.ShowInfo("Test e-postası gönderildi.");
        }
        catch (Exception ex)
        {
            DialogHelper.ShowError($"Test e-postası gönderilemedi:\n{GetSmtpErrorMessage(ex)}");
        }
    }

    private static string GetSmtpErrorMessage(Exception ex)
    {
        var messages = new List<string>();
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (!string.IsNullOrWhiteSpace(current.Message))
                messages.Add(current.Message);
        }

        return string.Join(Environment.NewLine, messages.Distinct());
    }

    [RelayCommand]
    private void TogglePause()
    {
        if (_monitorService.IsPaused)
            _monitorService.Resume();
        else
            _monitorService.Pause();

        UpdateMonitorStatus();
    }

    [RelayCommand]
    private async Task CheckNowAsync()
    {
        if (SelectedSite is null)
        {
            DialogHelper.ShowInfo("Kontrol etmek için bir site seçin.");
            return;
        }

        await _monitorService.CheckSiteNowAsync(SelectedSite.Id);
    }

    private void UpdateMonitorStatus()
    {
        IsMonitorPaused = _monitorService.IsPaused;
        MonitorStatusText = IsMonitorPaused ? "İzleme duraklatıldı" : "İzleme aktif";
    }
}
