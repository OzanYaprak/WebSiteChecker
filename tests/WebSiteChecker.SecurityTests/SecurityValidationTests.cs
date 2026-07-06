using WebSiteChecker.Helpers;
using WebSiteChecker.Models;
using Xunit;

namespace WebSiteChecker.SecurityTests;

public class InputSanitizerTests
{
    [Theory]
    [InlineData("normal-site")]
    [InlineData("hssgm")]
    public void ContainsHeaderInjectionChars_ReturnsFalseForSafeInput(string input)
    {
        Assert.False(InputSanitizer.ContainsHeaderInjectionChars(input));
    }

    [Theory]
    [InlineData("test\r\nBcc: attacker@evil.com")]
    [InlineData("line1\nline2")]
    [InlineData("bad\0name")]
    public void ContainsHeaderInjectionChars_DetectsDangerousCharacters(string input)
    {
        Assert.True(InputSanitizer.ContainsHeaderInjectionChars(input));
    }

    [Fact]
    public void SanitizeForEmailText_RemovesControlCharacters()
    {
        var sanitized = InputSanitizer.SanitizeForEmailText("test\r\nBcc: evil");

        Assert.Equal("testBcc: evil", sanitized);
    }
}

public class SiteInputValidatorTests
{
    private static MonitoredSite CreateValidSite() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Example",
        Url = "https://example.com",
        IntervalSeconds = 60,
        TimeoutSeconds = 10,
        ExpectedStatusCode = 200,
        IsEnabled = true
    };

    [Fact]
    public void TryValidate_RejectsHeaderInjectionInName()
    {
        var site = CreateValidSite();
        site.Name = "evil\r\nBcc: attacker@evil.com";

        var result = SiteInputValidator.TryValidate(site, out var error);

        Assert.False(result);
        Assert.Contains("geçersiz karakter", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryValidate_RejectsPrivateNetworkUrl()
    {
        var site = CreateValidSite();
        site.Url = "http://127.0.0.1";

        var result = SiteInputValidator.TryValidate(site, out _);

        Assert.False(result);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(100000)]
    public void TryValidate_EnforcesIntervalLimits(int interval)
    {
        var site = CreateValidSite();
        site.IntervalSeconds = interval;

        var result = SiteInputValidator.TryValidate(site, out _);

        Assert.False(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(120)]
    public void TryValidate_EnforcesTimeoutLimits(int timeout)
    {
        var site = CreateValidSite();
        site.TimeoutSeconds = timeout;

        var result = SiteInputValidator.TryValidate(site, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryValidate_AllowsPrivateNetworkUrlWhenFlagEnabled()
    {
        var site = CreateValidSite();
        site.Url = "http://127.0.0.1";
        site.AllowPrivateNetworks = true;

        var result = SiteInputValidator.TryValidate(site, out var error);

        Assert.True(result);
        Assert.Null(error);
    }

    [Fact]
    public void TryValidate_AcceptsValidSite()
    {
        var site = CreateValidSite();

        var result = SiteInputValidator.TryValidate(site, out var error);

        Assert.True(result);
        Assert.Null(error);
    }
}

public class SiteLimitsTests
{
    [Fact]
    public void SiteLimits_HasExpectedGuardrails()
    {
        Assert.Equal(50, SiteLimits.MaxSites);
        Assert.Equal(10, SiteLimits.MaxConcurrentChecks);
        Assert.True(SiteLimits.MaxTimeoutSeconds <= 60);
        Assert.True(SiteLimits.MaxIntervalSeconds >= SiteLimits.MinIntervalSeconds);
    }
}
