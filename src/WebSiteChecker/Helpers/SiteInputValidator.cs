using WebSiteChecker.Models;

namespace WebSiteChecker.Helpers;

public static class SiteInputValidator
{
    public static bool TryValidate(MonitoredSite site, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(site.Name))
        {
            error = "Site adı boş olamaz.";
            return false;
        }

        if (InputSanitizer.ContainsHeaderInjectionChars(site.Name))
        {
            error = "Site adı geçersiz karakterler içeriyor.";
            return false;
        }

        if (!UrlValidator.IsValidHttpUrl(site.Url, out error, site.AllowPrivateNetworks))
            return false;

        if (site.IntervalSeconds < SiteLimits.MinIntervalSeconds || site.IntervalSeconds > SiteLimits.MaxIntervalSeconds)
        {
            error = $"Kontrol aralığı {SiteLimits.MinIntervalSeconds}-{SiteLimits.MaxIntervalSeconds} saniye arasında olmalıdır.";
            return false;
        }

        if (site.TimeoutSeconds < SiteLimits.MinTimeoutSeconds || site.TimeoutSeconds > SiteLimits.MaxTimeoutSeconds)
        {
            error = $"Zaman aşımı {SiteLimits.MinTimeoutSeconds}-{SiteLimits.MaxTimeoutSeconds} saniye arasında olmalıdır.";
            return false;
        }

        if (site.RetryCount < SiteLimits.MinRetryCount || site.RetryCount > SiteLimits.MaxRetryCount)
        {
            error = $"Tekrar deneme sayısı {SiteLimits.MinRetryCount}-{SiteLimits.MaxRetryCount} arasında olmalıdır.";
            return false;
        }

        if (site.ExpectedStatusCode is < 100 or > 599)
        {
            error = "Geçerli bir HTTP durum kodu girin (100-599).";
            return false;
        }

        return true;
    }
}
