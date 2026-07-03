using Microsoft.Extensions.Hosting;
using WebSiteChecker.Models;

namespace WebSiteChecker.Services;

public class WebsiteMonitorService : BackgroundService, IWebsiteMonitorService
{
    private readonly ConfigStore _configStore;
    private readonly HttpHealthChecker _healthChecker;
    private readonly SmtpEmailNotifier _emailNotifier;
    private readonly AlertStateTracker _alertStateTracker;
    private readonly Dictionary<Guid, SiteRuntimeState> _runtimeStates = new();
    private readonly Dictionary<Guid, CancellationTokenSource> _siteLoops = new();
    private readonly object _lock = new();
    private List<MonitoredSite> _sites = [];
    private bool _isPaused;

    public WebsiteMonitorService(
        ConfigStore configStore,
        HttpHealthChecker healthChecker,
        SmtpEmailNotifier emailNotifier,
        AlertStateTracker alertStateTracker)
    {
        _configStore = configStore;
        _healthChecker = healthChecker;
        _emailNotifier = emailNotifier;
        _alertStateTracker = alertStateTracker;
    }

    public bool IsPaused
    {
        get { lock (_lock) return _isPaused; }
    }

    public IReadOnlyDictionary<Guid, SiteRuntimeState> RuntimeStates
    {
        get
        {
            lock (_lock)
                return _runtimeStates.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }

    public event EventHandler<SiteRuntimeState>? SiteStateChanged;

    public void Pause()
    {
        lock (_lock)
            _isPaused = true;
    }

    public void Resume()
    {
        lock (_lock)
            _isPaused = false;
    }

    public void ReloadSites()
    {
        lock (_lock)
        {
            _sites = _configStore.LoadSites();
            _alertStateTracker.Reload();

            foreach (var id in _siteLoops.Keys.ToList())
                StopSiteLoop(id);

            foreach (var site in _sites)
            {
                if (!_runtimeStates.ContainsKey(site.Id))
                    _runtimeStates[site.Id] = new SiteRuntimeState { SiteId = site.Id };

                if (site.IsEnabled)
                    StartSiteLoop(site);
            }
        }
    }

    public async Task CheckSiteNowAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        MonitoredSite? site;
        lock (_lock)
            site = _sites.FirstOrDefault(s => s.Id == siteId);

        if (site is null)
            return;

        await RunCheckAsync(site, cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ReloadSites();

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            foreach (var id in _siteLoops.Keys.ToList())
                StopSiteLoop(id);
        }

        return base.StopAsync(cancellationToken);
    }

    private void StartSiteLoop(MonitoredSite site)
    {
        var cts = new CancellationTokenSource();
        _siteLoops[site.Id] = cts;
        _ = SiteLoopAsync(site, cts.Token);
    }

    private void StopSiteLoop(Guid siteId)
    {
        if (_siteLoops.TryGetValue(siteId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
            _siteLoops.Remove(siteId);
        }
    }

    private async Task SiteLoopAsync(MonitoredSite site, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!IsPaused && site.IsEnabled)
                await RunCheckAsync(site, cancellationToken);

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(5, site.IntervalSeconds)), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunCheckAsync(MonitoredSite site, CancellationToken cancellationToken)
    {
        UpdateRuntimeState(site.Id, SiteStatus.Checking, null, null, null, null);

        var result = await _healthChecker.CheckAsync(site, cancellationToken);
        var status = result.IsSuccess ? SiteStatus.Up : SiteStatus.Down;

        UpdateRuntimeState(site.Id, status, result.CheckedAt, result.ResponseTimeMs, result.StatusCode, result.ErrorMessage);

        try
        {
            await HandleAlertsAsync(site, result, cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Alert handling failed: {ex.Message}");
        }
    }

    private async Task HandleAlertsAsync(MonitoredSite site, SiteCheckResult result, CancellationToken cancellationToken)
    {
        var smtpSettings = _configStore.LoadSmtpSettings();

        if (!result.IsSuccess)
        {
            if (_alertStateTracker.ShouldSendDownAlert(site.Id, smtpSettings.AlertCooldownMinutes))
            {
                await _emailNotifier.SendDownAlertAsync(site, result, cancellationToken);
                _alertStateTracker.MarkDownAlertSent(site.Id);
            }
        }
        else if (_alertStateTracker.ShouldSendRecoveryAlert(site.Id))
        {
            if (smtpSettings.SendRecoveryEmail)
                await _emailNotifier.SendRecoveryAlertAsync(site, result, cancellationToken);

            _alertStateTracker.MarkRecovered(site.Id);
        }
    }

    private void UpdateRuntimeState(
        Guid siteId,
        SiteStatus status,
        DateTime? checkedAt,
        long? responseTimeMs,
        int? statusCode,
        string? error)
    {
        SiteRuntimeState state;
        lock (_lock)
        {
            if (!_runtimeStates.TryGetValue(siteId, out state!))
            {
                state = new SiteRuntimeState { SiteId = siteId };
                _runtimeStates[siteId] = state;
            }

            state.Status = status;
            if (checkedAt.HasValue)
                state.LastCheckedAt = checkedAt;
            if (responseTimeMs.HasValue)
                state.LastResponseTimeMs = responseTimeMs;
            if (statusCode.HasValue)
                state.LastStatusCode = statusCode;
            if (error is not null)
                state.LastError = error;
            else if (status == SiteStatus.Up)
                state.LastError = null;
        }

        SiteStateChanged?.Invoke(this, state);
    }
}
