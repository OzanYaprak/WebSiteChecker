using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using WebSiteChecker.Models;

namespace WebSiteChecker.Helpers;

public static class SmtpConnectionHelper
{
    public static SecureSocketOptions ResolveSecureSocketOptions(int port, bool useSsl)
    {
        // SSL kapalı olsa bile sunucu STARTTLS destekliyorsa şifreleme kullan;
        // desteklemiyorsa eski davranış (düz metin) korunur, işleyiş bozulmaz.
        if (!useSsl)
            return SecureSocketOptions.StartTlsWhenAvailable;

        return port switch
        {
            465 => SecureSocketOptions.SslOnConnect,
            587 => SecureSocketOptions.StartTls,
            _ => SecureSocketOptions.Auto
        };
    }

    public static string? NormalizeAppPassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return null;

        // Gmail uygulama parolalarındaki boşlukları temizle; diğer karakterlere dokunma
        return password.Replace(" ", string.Empty);
    }

    public static string ResolveUsername(SmtpSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.Username))
            return settings.Username.Trim();

        return settings.FromAddress.Trim();
    }

    public static void ValidateSettings(SmtpSettings settings, string? password)
    {
        if (string.IsNullOrWhiteSpace(settings.Host))
            throw new InvalidOperationException("SMTP sunucu adresi tanımlı değil.");

        if (settings.Port is < 1 or > 65535)
            throw new InvalidOperationException("SMTP port numarası geçersiz.");

        if (string.IsNullOrWhiteSpace(settings.FromAddress))
            throw new InvalidOperationException("Gönderen e-posta adresi tanımlı değil.");

        if (string.IsNullOrWhiteSpace(settings.ToAddress))
            throw new InvalidOperationException("Alıcı e-posta adresi tanımlı değil.");

        if (!TryParseMailbox(settings.FromAddress, out _))
            throw new InvalidOperationException("Gönderen e-posta adresi geçersiz.");

        if (!TryParseMailbox(settings.ToAddress, out _))
            throw new InvalidOperationException("Alıcı e-posta adresi geçersiz.");

        var username = ResolveUsername(settings);
        if (string.IsNullOrWhiteSpace(username))
            throw new InvalidOperationException("SMTP kullanıcı adı tanımlı değil.");

        if (!string.IsNullOrWhiteSpace(settings.Username) && !TryParseMailbox(settings.Username, out _))
            throw new InvalidOperationException("SMTP kullanıcı adı geçerli bir e-posta adresi değil.");

        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("SMTP şifresi tanımlı değil. SMTP Ayarları'ndan şifrenizi kaydedin.");

        ValidateSenderAlignment(settings);
    }

    public static void ValidateSenderAlignment(SmtpSettings settings)
    {
        if (!TryParseMailbox(settings.FromAddress, out var fromMailbox))
            return;

        var username = ResolveUsername(settings);
        if (!TryParseMailbox(username, out var userMailbox))
            return;

        if (string.Equals(fromMailbox.Address, userMailbox.Address, StringComparison.OrdinalIgnoreCase))
            return;

        throw new InvalidOperationException(
            $"Gönderen adresi ({fromMailbox.Address}) ile SMTP kullanıcı adı ({userMailbox.Address}) aynı hesap olmalıdır. " +
            "Sunucuya hangi hesapla giriş yapıyorsanız gönderen de o adres olmalıdır. Alıcı alanı farklı olabilir.");
    }

    public static string? DescribeSmtpRejection(SmtpCommandException ex, SmtpSettings settings)
    {
        var message = ex.Message;
        if (!message.Contains("5.7.60", StringComparison.OrdinalIgnoreCase)
            && !message.Contains("permissions to send as", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var username = ResolveUsername(settings);
        return $"""
            SMTP sunucusu gönderen adresini reddetti (5.7.60).

            Bu hata genelde şu anlama gelir:
            - SMTP kullanıcı adı: {username}
            - Gönderen: {settings.FromAddress.Trim()}

            Bu iki alan birebir aynı hesap olmalıdır.
            Şifre de bu hesaba ait olmalıdır (ör. hssgm.noreply@saglik.gov.tr).

            Alıcı alanı bildirim alacağınız kişisel adresiniz olabilir; sorun gönderen/kullanıcı eşleşmesindedir.
            """;
    }

    public static bool TryParseMailbox(string address, out MailboxAddress mailbox)
    {
        mailbox = null!;

        if (string.IsNullOrWhiteSpace(address))
            return false;

        try
        {
            mailbox = MailboxAddress.Parse(address.Trim());
            return mailbox.Address.Contains('@', StringComparison.Ordinal);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
