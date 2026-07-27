using WebSiteChecker.Helpers;
using Xunit;

namespace WebSiteChecker.SecurityTests;

public class EmailTemplateBuilderTests
{
    [Fact]
    public void BuildDownAlert_ContainsBrandingAndEncodedContent()
    {
        var (html, plain) = EmailTemplateBuilder.BuildDownAlert(
            "Test<script>",
            "https://example.com",
            new DateTime(2026, 7, 6, 10, 0, 0),
            "500",
            "Timeout",
            1200);

        Assert.Contains("cid:hssgm-logo", html);
        Assert.Contains("Test&lt;script&gt;", html);
        Assert.Contains(BrandAssets.BrandRed, html);
        Assert.Contains("SF Pro Display", html);
        Assert.Contains(BrandAssets.ApplicationName, plain);
    }

    [Fact]
    public void BuildTestEmail_ContainsModernLayoutMarkers()
    {
        var (html, plain) = EmailTemplateBuilder.BuildTestEmail();

        Assert.Contains("Test E-postası", html);
        Assert.Contains("Test Bildirimi", html);
        Assert.Contains(BrandAssets.BrandRed, html);
        Assert.Contains("SMTP bağlantısı başarılı", plain);
    }
}
