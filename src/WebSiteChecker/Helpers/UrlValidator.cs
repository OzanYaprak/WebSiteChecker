namespace WebSiteChecker.Helpers;

public static class UrlValidator
{
    public static bool IsValidHttpUrl(string url, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(url))
        {
            error = "URL boş olamaz.";
            return false;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            error = "Geçerli bir URL girin.";
            return false;
        }

        if (uri.Scheme is not "http" and not "https")
        {
            error = "URL http:// veya https:// ile başlamalıdır.";
            return false;
        }

        if (!UrlSafetyValidator.IsUrlSafe(url, out error))
            return false;

        return true;
    }
}

