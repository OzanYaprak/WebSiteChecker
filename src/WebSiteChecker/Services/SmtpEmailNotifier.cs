using System.IO;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Utils;
using WebSiteChecker.Helpers;
using WebSiteChecker.Models;

namespace WebSiteChecker.Services;

public class SmtpEmailNotifier
{
    private readonly ConfigStore _configStore;

    public SmtpEmailNotifier(ConfigStore configStore)
    {
        _configStore = configStore;
    }

    public async Task SendDownAlertAsync(MonitoredSite site, SiteCheckResult result, CancellationToken cancellationToken = default)
    {
        var safeName = InputSanitizer.SanitizeForEmailText(site.Name);
        var safeUrl = InputSanitizer.SanitizeForEmailText(site.Url);
        var safeError = InputSanitizer.SanitizeForEmailText(result.ErrorMessage ?? "Bilinmiyor");

        var subject = $"[{BrandAssets.DirectorateShortName}] Site Erişim Uyarısı: {safeName}";
        var body = EmailTemplateBuilder.BuildDownAlert(
            safeName,
            safeUrl,
            result.CheckedAt.ToLocalTime(),
            result.StatusCode?.ToString(),
            safeError,
            result.ResponseTimeMs);

        await SendEmailAsync(subject, body.Html, body.Plain, cancellationToken);
    }

    public async Task SendRecoveryAlertAsync(MonitoredSite site, SiteCheckResult result, CancellationToken cancellationToken = default)
    {
        var safeName = InputSanitizer.SanitizeForEmailText(site.Name);
        var safeUrl = InputSanitizer.SanitizeForEmailText(site.Url);

        var subject = $"[{BrandAssets.DirectorateShortName}] Site Tekrar Erişilebilir: {safeName}";
        var body = EmailTemplateBuilder.BuildRecoveryAlert(
            safeName,
            safeUrl,
            result.CheckedAt.ToLocalTime(),
            result.StatusCode,
            result.ResponseTimeMs);

        await SendEmailAsync(subject, body.Html, body.Plain, cancellationToken);
    }

    public async Task SendTestEmailAsync(CancellationToken cancellationToken = default)
    {
        var subject = $"[{BrandAssets.DirectorateShortName}] Test E-postası";
        var body = EmailTemplateBuilder.BuildTestEmail();

        await SendEmailAsync(subject, body.Html, body.Plain, cancellationToken);
    }

    private async Task SendEmailAsync(
        string subject,
        string htmlBody,
        string plainBody,
        CancellationToken cancellationToken)
    {
        var settings = _configStore.LoadSmtpSettings();
        var password = _configStore.LoadSmtpPassword();

        SmtpConnectionHelper.ValidateSettings(settings, password);

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(settings.FromAddress.Trim()));
        message.To.Add(MailboxAddress.Parse(settings.ToAddress.Trim()));
        message.Subject = subject;
        message.Body = CreateBrandedBody(htmlBody, plainBody);

        using var client = new SmtpClient();
        var secureOptions = SmtpConnectionHelper.ResolveSecureSocketOptions(settings.Port, settings.UseSsl);
        var username = SmtpConnectionHelper.ResolveUsername(settings);

        try
        {
            await client.ConnectAsync(settings.Host.Trim(), settings.Port, secureOptions, cancellationToken);
            await client.AuthenticateAsync(username, password!, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (AuthenticationException ex)
        {
            throw new InvalidOperationException(
                "SMTP kimlik doğrulaması başarısız. Kullanıcı adı ve şifreyi kontrol edin.",
                ex);
        }
        catch (SmtpCommandException ex)
        {
            var detailedMessage = SmtpConnectionHelper.DescribeSmtpRejection(ex, settings);
            throw new InvalidOperationException(
                detailedMessage ?? $"SMTP sunucusu isteği reddetti: {ex.Message}",
                ex);
        }
        catch (SmtpProtocolException ex)
        {
            throw new InvalidOperationException($"SMTP bağlantı hatası: {ex.Message}", ex);
        }
    }

    private static MimeEntity CreateBrandedBody(string htmlBody, string plainBody)
    {
        var builder = new BodyBuilder
        {
            HtmlBody = htmlBody,
            TextBody = plainBody
        };

        using var logoStream = BrandAssets.OpenLogoStream();
        var logoCopy = new MemoryStream();
        logoStream.CopyTo(logoCopy);
        logoCopy.Position = 0;

        var logo = (MimePart)builder.LinkedResources.Add("hssgm-logo.png", logoCopy, ContentType.Parse("image/png"));
        logo.ContentId = EmailTemplateBuilder.LogoContentId;
        logo.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
        logo.ContentTransferEncoding = ContentEncoding.Base64;

        return builder.ToMessageBody();
    }
}
