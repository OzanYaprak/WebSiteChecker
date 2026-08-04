using MailKit.Security;
using WebSiteChecker.Helpers;
using WebSiteChecker.Models;
using Xunit;

namespace WebSiteChecker.SecurityTests;

public class SmtpConnectionHelperTests
{
    [Theory]
    [InlineData(465, true, SecureSocketOptions.SslOnConnect)]
    [InlineData(587, true, SecureSocketOptions.StartTls)]
    [InlineData(25, true, SecureSocketOptions.Auto)]
    [InlineData(587, false, SecureSocketOptions.StartTlsWhenAvailable)]
    public void ResolveSecureSocketOptions_ReturnsExpectedValue(int port, bool useSsl, SecureSocketOptions expected)
    {
        Assert.Equal(expected, SmtpConnectionHelper.ResolveSecureSocketOptions(port, useSsl));
    }

    [Theory]
    [InlineData("abcd efgh ijkl mnop", "abcdefghijklmnop")]
    [InlineData("  secret  ", "secret")]
    [InlineData("BARbunYA!34", "BARbunYA!34")]
    [InlineData(null, null)]
    public void NormalizeAppPassword_NormalizesInput(string? input, string? expected)
    {
        Assert.Equal(expected, SmtpConnectionHelper.NormalizeAppPassword(input));
    }

    [Fact]
    public void ValidateSettings_RequiresPassword()
    {
        var settings = new SmtpSettings
        {
            Host = "smtp.gmail.com",
            Port = 587,
            Username = "user@gmail.com",
            FromAddress = "user@gmail.com",
            ToAddress = "user@gmail.com"
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            SmtpConnectionHelper.ValidateSettings(settings, password: null));

        Assert.Contains("şifre", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveUsername_FallsBackToFromAddress()
    {
        var settings = new SmtpSettings
        {
            FromAddress = "user@gmail.com"
        };

        Assert.Equal("user@gmail.com", SmtpConnectionHelper.ResolveUsername(settings));
    }

    [Fact]
    public void ValidateSenderAlignment_RejectsMismatchedFromAndUsername()
    {
        var settings = new SmtpSettings
        {
            Host = "eposta.saglik.gov.tr",
            Port = 587,
            Username = "hssgm.noreply@saglik.gov.tr",
            FromAddress = "baska.kullanici@saglik.gov.tr",
            ToAddress = "alici@saglik.gov.tr"
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            SmtpConnectionHelper.ValidateSenderAlignment(settings));

        Assert.Contains("aynı hesap", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SaglikGovTrPreset_MatchesCorporateDefaults()
    {
        var settings = SmtpPresets.SaglikGovTr();

        Assert.Equal("eposta.saglik.gov.tr", settings.Host);
        Assert.Equal(587, settings.Port);
        Assert.False(settings.UseSsl);
        Assert.Equal("hssgm.noreply@saglik.gov.tr", settings.Username);
        Assert.Equal("hssgm.noreply@saglik.gov.tr", settings.FromAddress);
    }
}
