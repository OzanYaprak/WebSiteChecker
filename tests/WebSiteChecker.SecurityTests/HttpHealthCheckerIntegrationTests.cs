using WebSiteChecker.Models;
using WebSiteChecker.Services;
using Xunit;
using Xunit.Abstractions;

namespace WebSiteChecker.SecurityTests;

public class HttpHealthCheckerIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public HttpHealthCheckerIntegrationTests(ITestOutputHelper output) => _output = output;

    [Theory]
    [InlineData("https://hssgm.gov.tr/", true)]
    [InlineData("https://www.hssgm.gov.tr/", false)]
    [InlineData("https://interaktif.hssgm.gov.tr/", true)]
    [InlineData("https://online.hssgm.gov.tr/", true)]
    [InlineData("http://ihalethssgm.saglik.gov.tr/", true)]
    public async Task CheckAsync_WorksThroughCorporateProxy(string url, bool allowPrivate)
    {
        var checker = new HttpHealthChecker();
        var site = new MonitoredSite
        {
            Id = Guid.NewGuid(),
            Url = url,
            TimeoutSeconds = 30,
            RetryCount = 1,
            ExpectedStatusCode = 200,
            AllowPrivateNetworks = allowPrivate
        };

        var result = await checker.CheckAsync(site);

        _output.WriteLine($"URL: {url}");
        _output.WriteLine($"AllowPrivate: {allowPrivate}");
        _output.WriteLine($"Success: {result.IsSuccess}");
        _output.WriteLine($"Status: {result.StatusCode}");
        _output.WriteLine($"Error: {result.ErrorMessage}");

        Assert.True(result.IsSuccess, result.ErrorMessage ?? $"Status {result.StatusCode}");
    }
}
