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

    public static bool IsUrlSafe(string url, out string? error, bool allowPrivateNetworks = false)
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

        if (!IsHostAllowed(uri, allowPrivateNetworks, out error))
            return false;

        if (allowPrivateNetworks)
            return true;

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

            var hasPermittedAddress = addresses.Any(address => !IsBlockedIpAddress(address));
            if (!hasPermittedAddress)
            {
                error = "Yerel veya özel ağ adreslerine istek gönderilemez.";
                return false;
            }
        }
        catch (SocketException)
        {
            error = "Sunucu adresi çözümlenemedi.";
            return false;
        }

        return true;
    }

    public static bool IsUriSafe(Uri uri, out string? error, bool allowPrivateNetworks = false)
    {
        return IsUrlSafe(uri.ToString(), out error, allowPrivateNetworks);
    }

    private static bool IsHostAllowed(Uri uri, bool allowPrivateNetworks, out string? error)
    {
        error = null;
        // "localhost." gibi sondaki nokta ile blok listesinin aşılmasını engelle
        var host = uri.Host.TrimEnd('.');

        if (string.IsNullOrWhiteSpace(host))
        {
            error = "Geçerli bir sunucu adresi girin.";
            return false;
        }

        if (allowPrivateNetworks)
            return true;

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
        // ::ffff:127.0.0.1 gibi IPv4-mapped adreslerle filtrenin aşılmasını engelle
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        if (IPAddress.IsLoopback(address))
            return true;

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] switch
            {
                0 => true,
                10 => true,
                100 when bytes[1] is >= 64 and <= 127 => true, // CGNAT 100.64.0.0/10
                127 => true,
                169 when bytes[1] == 254 => true,
                172 when bytes[1] is >= 16 and <= 31 => true,
                192 when bytes[1] == 0 && bytes[2] == 0 => true, // 192.0.0.0/24 (IETF özel amaçlı)
                192 when bytes[1] == 168 => true,
                198 when bytes[1] is 18 or 19 => true, // 198.18.0.0/15 (benchmark)
                >= 224 => true, // multicast, rezerve ve broadcast
                _ => false
            };
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.Equals(IPAddress.IPv6Loopback) || address.Equals(IPAddress.IPv6Any))
                return true;

            var bytes = address.GetAddressBytes();
            if (bytes[0] == 0xFE && (bytes[1] & 0xC0) == 0x80) // link-local fe80::/10
                return true;

            if ((bytes[0] & 0xFE) == 0xFC) // unique local fc00::/7
                return true;

            if (bytes[0] == 0xFF) // multicast ff00::/8
                return true;

            // NAT64 64:ff9b::/96 üzerinden gömülü IPv4 ile bypass engelle
            if (bytes[0] == 0x00 && bytes[1] == 0x64 && bytes[2] == 0xFF && bytes[3] == 0x9B)
                return true;
        }

        return false;
    }
}
