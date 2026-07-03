using CommunityToolkit.Mvvm.ComponentModel;
using WebSiteChecker.Models;

namespace WebSiteChecker.ViewModels;

public partial class SiteListItemViewModel : ObservableObject
{
    public Guid Id { get; init; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private int _intervalSeconds;

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private SiteStatus _status = SiteStatus.Unknown;

    [ObservableProperty]
    private string _statusText = "Bilinmiyor";

    [ObservableProperty]
    private string _lastCheckedText = "-";

    [ObservableProperty]
    private string _responseTimeText = "-";

    [ObservableProperty]
    private string _lastError = string.Empty;

    public int TimeoutSeconds { get; set; } = 10;
    public int ExpectedStatusCode { get; set; } = 200;

    public MonitoredSite ToModel() => new()
    {
        Id = Id,
        Name = Name,
        Url = Url,
        IntervalSeconds = IntervalSeconds,
        TimeoutSeconds = TimeoutSeconds,
        ExpectedStatusCode = ExpectedStatusCode,
        IsEnabled = IsEnabled
    };

    public void UpdateFromRuntime(SiteRuntimeState state)
    {
        Status = state.Status;
        StatusText = state.Status switch
        {
            SiteStatus.Up => "Erişilebilir",
            SiteStatus.Down => "Erişilemiyor",
            SiteStatus.Checking => "Kontrol ediliyor...",
            _ => "Bilinmiyor"
        };

        LastCheckedText = state.LastCheckedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
        ResponseTimeText = state.LastResponseTimeMs.HasValue ? $"{state.LastResponseTimeMs} ms" : "-";
        LastError = state.LastError ?? string.Empty;
    }
}
