using WebSiteChecker.Helpers;
using Xunit;

namespace WebSiteChecker.SecurityTests;

public class SystemProxyResolverTests
{
    [Fact]
    public void TryGetVpnPacProxy_WhenAutoConfigPresent_DoesNotThrow()
    {
        // Ortamda PAC yoksa false; varsa 10.x:8080 benzeri bir adres dönebilir.
        var ok = SystemProxyResolver.TryGetVpnPacProxy(out var proxy);
        if (ok)
        {
            Assert.NotNull(proxy);
            Assert.True(proxy!.Port > 0);
        }
    }
}
