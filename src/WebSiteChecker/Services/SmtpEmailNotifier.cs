using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
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

        var subject = $"[WebSiteChecker] DOWN: {safeName} ({safeUrl})";
        var body = $"""
            Web sitesi erişilemiyor.

            Site: {safeName}
            URL: {safeUrl}
            Kontrol zamanı: {result.CheckedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}
            Durum kodu: {result.StatusCode?.ToString() ?? "Yok"}
            Hata: {safeError}
            Yanıt süresi: {result.ResponseTimeMs} ms

            — WebSiteChecker
            """;

        await SendEmailAsync(subject, body, cancellationToken);
    }

    public async Task SendRecoveryAlertAsync(MonitoredSite site, SiteCheckResult result, CancellationToken cancellationToken = default)
    {
        var safeName = InputSanitizer.SanitizeForEmailText(site.Name);
        var safeUrl = InputSanitizer.SanitizeForEmailText(site.Url);

        var subject = $"[WebSiteChecker] RECOVERED: {safeName} ({safeUrl})";
        var body = $"""
            Web sitesi tekrar erişilebilir durumda.

            Site: {safeName}
            URL: {safeUrl}
            Kontrol zamanı: {result.CheckedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}
            Durum kodu: {result.StatusCode}
            Yanıt süresi: {result.ResponseTimeMs} ms

            — WebSiteChecker
            """;

        await SendEmailAsync(subject, body, cancellationToken);
    }

    public async Task SendTestEmailAsync(CancellationToken cancellationToken = default)
    {
        var subject = "[WebSiteChecker] Test e-postası";
        var body = """
            Bu bir test e-postasıdır.

            SMTP ayarlarınız doğru yapılandırılmış görünüyor.

            — WebSiteChecker
            """;

        await SendEmailAsync(subject, body, cancellationToken);
    }

    private async Task SendEmailAsync(string subject, string body, CancellationToken cancellationToken)
    {
        var settings = _configStore.LoadSmtpSettings();
        var password = _configStore.LoadSmtpPassword();

        ValidateSettings(settings, password);

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(settings.ToAddress));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        var secureOptions = settings.UseSsl
            ? (settings.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls)
            : SecureSocketOptions.StartTlsWhenAvailable;

        await client.ConnectAsync(settings.Host, settings.Port, secureOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(settings.Username))
            await client.AuthenticateAsync(settings.Username, password!, cancellationToken);

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private static void ValidateSettings(SmtpSettings settings, string? password)
    {
        if (string.IsNullOrWhiteSpace(settings.Host))
            throw new InvalidOperationException("SMTP sunucu adresi tanımlı değil.");

        if (string.IsNullOrWhiteSpace(settings.FromAddress))
            throw new InvalidOperationException("Gönderen e-posta adresi tanımlı değil.");

        if (string.IsNullOrWhiteSpace(settings.ToAddress))
            throw new InvalidOperationException("Alıcı e-posta adresi tanımlı değil.");

        if (!string.IsNullOrWhiteSpace(settings.Username) && string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("SMTP şifresi tanımlı değil.");
    }
}
