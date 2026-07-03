using WebSiteChecker.Models;

namespace WebSiteChecker.Services;

public interface IWebsiteMonitorService
{
    bool IsPaused { get; }
    IReadOnlyDictionary<Guid, SiteRuntimeState> RuntimeStates { get; }

    event EventHandler<SiteRuntimeState>? SiteStateChanged;

    void Pause();
    void Resume();
    void ReloadSites();
    Task CheckSiteNowAsync(Guid siteId, CancellationToken cancellationToken = default);
}
