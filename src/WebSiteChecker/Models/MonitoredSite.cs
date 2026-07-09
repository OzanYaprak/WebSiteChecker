namespace WebSiteChecker.Models;

public class MonitoredSite
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int IntervalSeconds { get; set; } = 60;
    public int TimeoutSeconds { get; set; } = 10;
    /// <summary>
    /// İlk deneme başarısız olduğunda yapılacak ek deneme sayısı (0 = tekrar yok).
    /// </summary>
    public int RetryCount { get; set; }
    public int ExpectedStatusCode { get; set; } = 200;
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// VPN veya kurum içi ağdaki hedeflere (özel IP, yönlendirme) izin verir.
    /// Varsayılan kapalıdır; yalnızca güvenilen iç siteler için açın.
    /// </summary>
    public bool AllowPrivateNetworks { get; set; }
}
