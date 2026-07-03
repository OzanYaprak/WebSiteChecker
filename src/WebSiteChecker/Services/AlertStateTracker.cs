using WebSiteChecker.Models;

namespace WebSiteChecker.Services;

public class AlertStateTracker
{
    private readonly ConfigStore _configStore;
    private readonly Dictionary<Guid, AlertState> _states = new();
    private readonly object _lock = new();

    public AlertStateTracker(ConfigStore configStore)
    {
        _configStore = configStore;
        Reload();
    }

    public void Reload()
    {
        lock (_lock)
        {
            _states.Clear();
            foreach (var state in _configStore.LoadAlertStates())
                _states[state.SiteId] = state;
        }
    }

    public bool ShouldSendDownAlert(Guid siteId, int cooldownMinutes)
    {
        lock (_lock)
        {
            if (!_states.TryGetValue(siteId, out var state))
                return true;

            if (state.IsDown)
                return false;

            if (state.LastDownAlertSentAt.HasValue &&
                DateTime.UtcNow - state.LastDownAlertSentAt.Value < TimeSpan.FromMinutes(cooldownMinutes))
                return false;

            return true;
        }
    }

    public bool ShouldSendRecoveryAlert(Guid siteId)
    {
        lock (_lock)
        {
            if (!_states.TryGetValue(siteId, out var state))
                return false;

            return state.IsDown;
        }
    }

    public void MarkDownAlertSent(Guid siteId)
    {
        lock (_lock)
        {
            if (!_states.TryGetValue(siteId, out var state))
            {
                state = new AlertState { SiteId = siteId };
                _states[siteId] = state;
            }

            state.IsDown = true;
            state.LastDownAlertSentAt = DateTime.UtcNow;
            Persist();
        }
    }

    public void MarkRecovered(Guid siteId)
    {
        lock (_lock)
        {
            if (!_states.TryGetValue(siteId, out var state))
            {
                state = new AlertState { SiteId = siteId };
                _states[siteId] = state;
            }

            state.IsDown = false;
            state.LastRecoveryAlertSentAt = DateTime.UtcNow;
            Persist();
        }
    }

    private void Persist()
    {
        _configStore.SaveAlertStates(_states.Values);
    }
}
