using Microsoft.Win32;

namespace WebSiteChecker.Services;

public static class StartupRegistryHelper
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "WebSiteChecker";
    private const string MinimizedFlag = "--minimized";

    public static bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            return key?.GetValue(AppName) is string;
        }
        catch
        {
            return false;
        }
    }

    public static void SetStartupEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true)
                ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, true);

            if (!enabled)
            {
                key.DeleteValue(AppName, throwOnMissingValue: false);
                return;
            }

            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath))
                return;

            key.SetValue(AppName, $"\"{exePath}\" {MinimizedFlag}");
        }
        catch
        {
            // Kayıt defteri erişimi reddedildiyse yoksay
        }
    }
}
