using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace WebSiteChecker.Helpers;

/// <summary>
/// Windows PAC (kurulum betiği) ve F5 SSL VPN ara sunucu ayarlarını çözümler.
/// Bazı VPN PAC betikleri istemciye DIRECT döner; kurumsal siteler yalnızca
/// betikte tanımlı PROXY üzerinden erişilebilir — bu durumda o adresi zorlarız.
/// </summary>
public static partial class SystemProxyResolver
{
    private static readonly TimeSpan SuccessCacheDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan FailureCacheDuration = TimeSpan.FromSeconds(5);
    private static readonly object Sync = new();
    private static DateTime _cacheUntil = DateTime.MinValue;
    private static Uri? _cachedVpnProxy;
    private static bool _cacheHasValue;

    /// <summary>
    /// HTTP kontrolleri için kullanılacak proxy.
    /// VPN PAC aktifse betikteki PROXY adresi; değilse sistem varsayılanı.
    /// </summary>
    public static IWebProxy CreateProxy() => new DynamicWebProxy();

    public static bool TryGetVpnPacProxy(out Uri? proxyUri)
    {
        lock (Sync)
        {
            if (DateTime.UtcNow < _cacheUntil)
            {
                proxyUri = _cachedVpnProxy;
                return _cacheHasValue && proxyUri is not null;
            }

            _cacheHasValue = TryResolveConfiguredProxy(out _cachedVpnProxy);
            _cacheUntil = DateTime.UtcNow.Add(_cacheHasValue ? SuccessCacheDuration : FailureCacheDuration);
            proxyUri = _cachedVpnProxy;
            return _cacheHasValue && proxyUri is not null;
        }
    }

    /// <summary>
    /// Verilen uç noktanın yapılandırılmış ara sunucu (PAC veya el ile) olup olmadığını söyler.
    /// </summary>
    public static bool IsConfiguredProxyEndpoint(string host, int port)
    {
        if (!TryGetVpnPacProxy(out var proxy) || proxy is null)
            return false;

        var proxyHost = proxy.Host.TrimEnd('.');
        host = host.TrimEnd('.');

        if (proxy.Port != port)
            return false;

        if (string.Equals(proxyHost, host, StringComparison.OrdinalIgnoreCase))
            return true;

        if (IPAddress.TryParse(host, out var connectIp) &&
            IPAddress.TryParse(proxyHost, out var proxyIp))
            return connectIp.Equals(proxyIp);

        return false;
    }

    private static bool TryResolveConfiguredProxy(out Uri? proxyUri)
    {
        if (TryResolveVpnPacProxy(out proxyUri))
            return true;

        return TryResolveManualProxy(out proxyUri);
    }

    private static bool TryResolveVpnPacProxy(out Uri? proxyUri)
    {
        proxyUri = null;

        var autoConfigUrl = ReadInternetSetting("AutoConfigURL") as string;
        if (string.IsNullOrWhiteSpace(autoConfigUrl))
            return false;

        if (!Uri.TryCreate(autoConfigUrl.Trim(), UriKind.Absolute, out var pacUri))
            return false;

        var pacText = DownloadPacScript(pacUri);
        if (string.IsNullOrWhiteSpace(pacText))
            return false;

        // Tercihen Local fonksiyonundaki PROXY satırı (F5: FindProxyForURL_Local).
        var localProxy = LocalProxyRegex().Match(pacText);
        if (localProxy.Success && TryParseProxyEndpoint(localProxy.Groups[1].Value, out proxyUri))
            return true;

        var anyProxy = AnyProxyRegex().Match(pacText);
        return anyProxy.Success && TryParseProxyEndpoint(anyProxy.Groups[1].Value, out proxyUri);
    }

    private static bool TryResolveManualProxy(out Uri? proxyUri)
    {
        proxyUri = null;

        if (ReadInternetSetting("ProxyEnable") is not int enabled || enabled == 0)
            return false;

        if (ReadInternetSetting("ProxyServer") is not string proxyServer || string.IsNullOrWhiteSpace(proxyServer))
            return false;

        // "host:port" veya "http=host:port;https=host:port"
        foreach (var part in proxyServer.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var value = part.Contains('=', StringComparison.Ordinal)
                ? part[(part.IndexOf('=', StringComparison.Ordinal) + 1)..]
                : part;

            if (TryParseProxyEndpoint(value, out proxyUri))
                return true;
        }

        return false;
    }

    /// <summary>
    /// PAC betiği daima doğrudan indirilir; proxy üzerinden geçmeye çalışmak
    /// VPN başlangıcında zaman aşımına yol açar.
    /// </summary>
    private static string? DownloadPacScript(Uri pacUri)
    {
        try
        {
            using var handler = new HttpClientHandler { UseProxy = false };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(3) };
            return client.GetStringAsync(pacUri).GetAwaiter().GetResult();
        }
        catch
        {
            return null;
        }
    }

    private static object? ReadInternetSetting(string name)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Internet Settings");
            return key?.GetValue(name);
        }
        catch
        {
            return null;
        }
    }

    private static bool TryParseProxyEndpoint(string value, out Uri? proxyUri)
    {
        proxyUri = null;
        value = value.Trim().TrimEnd(';').Trim();
        if (string.IsNullOrWhiteSpace(value))
            return false;

        // "host:port" veya "http://host:port"
        if (!value.Contains("://", StringComparison.Ordinal))
            value = "http://" + value;

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            return false;

        if (uri.Port <= 0)
            return false;

        proxyUri = uri;
        return true;
    }

    [GeneratedRegex(
        @"FindProxyForURL_Local\s*\([^)]*\)\s*\{[\s\S]*?PROXY\s+([^\s""';]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LocalProxyRegex();

    [GeneratedRegex(
        @"\bPROXY\s+(\d{1,3}(?:\.\d{1,3}){3}:\d{1,5})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AnyProxyRegex();

    private sealed class DynamicWebProxy : IWebProxy
    {
        public ICredentials? Credentials { get; set; } = CredentialCache.DefaultCredentials;

        public Uri? GetProxy(Uri destination)
        {
            if (TryGetVpnPacProxy(out var vpnProxy) && vpnProxy is not null)
                return vpnProxy;

            return HttpClient.DefaultProxy.GetProxy(destination);
        }

        public bool IsBypassed(Uri host)
        {
            if (TryGetVpnPacProxy(out var vpnProxy) && vpnProxy is not null)
                return false;

            return HttpClient.DefaultProxy.IsBypassed(host);
        }
    }
}
