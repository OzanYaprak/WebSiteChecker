using System.Net;
using System.Net.Sockets;

namespace WebSiteChecker.Helpers;

public static class UrlSafetyValidator
{
    private static readonly HashSet<string> BlockedHostNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "localhost",
        "0.0.0.0",
        "metadata.google.internal"
    };

    public static bool IsUrlSafe(string url, out string? error)
    {
        error = null;

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

        if (!IsHostAllowed(uri, out error))
            return false;

        if (IPAddress.TryParse(uri.Host, out var literalIp))
        {
            if (IsBlockedIpAddress(literalIp))
            {
                error = "Yerel veya özel ağ adreslerine istek gönderilemez.";
                return false;
            }

            return true;
        }

        try
        {
            var addresses = Dns.GetHostAddresses(uri.Host);
            if (addresses.Length == 0)
            {
                error = "Sunucu adresi çözümlenemedi.";
                return false;
            }

            foreach (var address in addresses)
            {
                if (IsBlockedIpAddress(address))
                {
                    error = "Yerel veya özel ağ adreslerine istek gönderilemez.";
                    return false;
                }
            }
        }
        catch (SocketException)
        {
            error = "Sunucu adresi çözümlenemedi.";
            return false;
        }

        return true;
    }

    public static bool IsUriSafe(Uri uri, out string? error)
    {
        return IsUrlSafe(uri.ToString(), out error);
    }

    private static bool IsHostAllowed(Uri uri, out string? error)
    {
        error = null;
        var host = uri.Host;

        if (string.IsNullOrWhiteSpace(host))
        {
            error = "Geçerli bir sunucu adresi girin.";
            return false;
        }

        if (BlockedHostNames.Contains(host))
        {
            error = "Bu sunucu adresine izin verilmiyor.";
            return false;
        }

        if (host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
        {
            error = "Yerel ağ sunucu adlarına izin verilmiyor.";
            return false;
        }

        return true;
    }

    public static bool IsBlockedIpAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
            return true;

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] switch
            {
                0 => true,
                10 => true,
                127 => true,
                169 when bytes[1] == 254 => true,
                172 when bytes[1] is >= 16 and <= 31 => true,
                192 when bytes[1] == 168 => true,
                _ => false
            };
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.Equals(IPAddress.IPv6Loopback))
                return true;

            var bytes = address.GetAddressBytes();
            if (bytes[0] == 0xFE && (bytes[1] & 0xC0) == 0x80)
                return true;

            if ((bytes[0] & 0xFE) == 0xFC)
                return true;
        }

        return false;
    }
}
