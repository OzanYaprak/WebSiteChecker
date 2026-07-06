using System.Net;
using WebSiteChecker.Helpers;
using Xunit;

namespace WebSiteChecker.SecurityTests;

public class UrlSafetyValidatorTests
{
    [Theory]
    [InlineData("http://127.0.0.1")]
    [InlineData("http://localhost")]
    [InlineData("http://192.168.1.1")]
    [InlineData("http://10.0.0.5")]
    [InlineData("http://172.16.0.1")]
    [InlineData("http://169.254.169.254")]
    [InlineData("http://0.0.0.0")]
    [InlineData("http://[::1]")]
    public void IsUrlSafe_BlocksPrivateAndLocalTargets(string url)
    {
        var result = UrlSafetyValidator.IsUrlSafe(url, out var error);

        Assert.False(result);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("https://www.hssgm.gov.tr/")]
    public void IsUrlSafe_AllowsPublicTargets(string url)
    {
        var result = UrlSafetyValidator.IsUrlSafe(url, out var error);

        Assert.True(result);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("file:///etc/passwd")]
    public void IsUrlSafe_BlocksNonHttpSchemes(string url)
    {
        var result = UrlSafetyValidator.IsUrlSafe(url, out var error);

        Assert.False(result);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void IsUrlSafe_AllowsPrivateTargetsWhenExplicitlyEnabled()
    {
        var result = UrlSafetyValidator.IsUrlSafe("http://127.0.0.1", out var error, allowPrivateNetworks: true);

        Assert.True(result);
        Assert.Null(error);
    }

    [Fact]
    public void IsBlockedIpAddress_DetectsLoopback()
    {
        Assert.True(UrlSafetyValidator.IsBlockedIpAddress(IPAddress.Loopback));
        Assert.True(UrlSafetyValidator.IsBlockedIpAddress(IPAddress.IPv6Loopback));
    }
}
