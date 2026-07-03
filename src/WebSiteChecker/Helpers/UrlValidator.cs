using System.Windows;

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

        return true;
    }
}

public static class DialogHelper
{
    public static void ShowError(string message, string title = "Hata")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public static void ShowInfo(string message, string title = "Bilgi")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public static bool Confirm(string message, string title = "Onay")
    {
        return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }
}
