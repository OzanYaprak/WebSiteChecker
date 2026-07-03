using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WebSiteChecker.Models;

namespace WebSiteChecker.Services;

public class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _dataDirectory;
    private readonly string _sitesPath;
    private readonly string _smtpPath;
    private readonly string _passwordPath;
    private readonly string _alertStatePath;
    private readonly object _lock = new();

    public ConfigStore()
    {
        _dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WebSiteChecker");
        Directory.CreateDirectory(_dataDirectory);

        _sitesPath = Path.Combine(_dataDirectory, "sites.json");
        _smtpPath = Path.Combine(_dataDirectory, "smtp-settings.json");
        _passwordPath = Path.Combine(_dataDirectory, "smtp-password.dat");
        _alertStatePath = Path.Combine(_dataDirectory, "monitor-state.json");
    }

    public string DataDirectory => _dataDirectory;

    public List<MonitoredSite> LoadSites()
    {
        lock (_lock)
        {
            if (!File.Exists(_sitesPath))
                return [];

            var json = File.ReadAllText(_sitesPath);
            return JsonSerializer.Deserialize<List<MonitoredSite>>(json, JsonOptions) ?? [];
        }
    }

    public void SaveSites(IEnumerable<MonitoredSite> sites)
    {
        lock (_lock)
        {
            var json = JsonSerializer.Serialize(sites.ToList(), JsonOptions);
            File.WriteAllText(_sitesPath, json);
        }
    }

    public SmtpSettings LoadSmtpSettings()
    {
        lock (_lock)
        {
            if (!File.Exists(_smtpPath))
                return new SmtpSettings();

            var json = File.ReadAllText(_smtpPath);
            return JsonSerializer.Deserialize<SmtpSettings>(json, JsonOptions) ?? new SmtpSettings();
        }
    }

    public void SaveSmtpSettings(SmtpSettings settings)
    {
        lock (_lock)
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(_smtpPath, json);
        }
    }

    public string? LoadSmtpPassword()
    {
        lock (_lock)
        {
            if (!File.Exists(_passwordPath))
                return null;

            var encrypted = File.ReadAllBytes(_passwordPath);
            var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
    }

    public void SaveSmtpPassword(string? password)
    {
        lock (_lock)
        {
            if (string.IsNullOrEmpty(password))
            {
                if (File.Exists(_passwordPath))
                    File.Delete(_passwordPath);
                return;
            }

            var data = Encoding.UTF8.GetBytes(password);
            var encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(_passwordPath, encrypted);
        }
    }

    public List<AlertState> LoadAlertStates()
    {
        lock (_lock)
        {
            if (!File.Exists(_alertStatePath))
                return [];

            var json = File.ReadAllText(_alertStatePath);
            return JsonSerializer.Deserialize<List<AlertState>>(json, JsonOptions) ?? [];
        }
    }

    public void SaveAlertStates(IEnumerable<AlertState> states)
    {
        lock (_lock)
        {
            var json = JsonSerializer.Serialize(states.ToList(), JsonOptions);
            File.WriteAllText(_alertStatePath, json);
        }
    }
}
