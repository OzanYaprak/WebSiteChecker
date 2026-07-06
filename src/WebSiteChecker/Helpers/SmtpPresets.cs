using WebSiteChecker.Models;

namespace WebSiteChecker.Helpers;

public static class SmtpPresets
{
    public const string SaglikGovTrHost = "eposta.saglik.gov.tr";
    public const string SaglikGovTrSender = "hssgm.noreply@saglik.gov.tr";

    public static SmtpSettings SaglikGovTr(string? toAddress = null) => new()
    {
        Host = SaglikGovTrHost,
        Port = 587,
        UseSsl = false,
        Username = SaglikGovTrSender,
        FromAddress = SaglikGovTrSender,
        ToAddress = string.IsNullOrWhiteSpace(toAddress) ? string.Empty : toAddress.Trim(),
        AlertCooldownMinutes = 30,
        SendRecoveryEmail = true
    };

    public static void ApplySaglikGovTr(SmtpSettings settings, string? toAddress = null)
    {
        var preset = SaglikGovTr(toAddress);
        settings.Host = preset.Host;
        settings.Port = preset.Port;
        settings.UseSsl = preset.UseSsl;
        settings.Username = preset.Username;
        settings.FromAddress = preset.FromAddress;

        if (!string.IsNullOrWhiteSpace(preset.ToAddress))
            settings.ToAddress = preset.ToAddress;
        else if (string.IsNullOrWhiteSpace(settings.ToAddress))
            settings.ToAddress = string.Empty;
    }

    public static SmtpSettings Gmail() => new()
    {
        Host = "smtp.gmail.com",
        Port = 587,
        UseSsl = true,
        AlertCooldownMinutes = 30,
        SendRecoveryEmail = true
    };

    public static void ApplyGmail(SmtpSettings settings, string? email = null)
    {
        var preset = Gmail();
        settings.Host = preset.Host;
        settings.Port = preset.Port;
        settings.UseSsl = preset.UseSsl;

        if (!string.IsNullOrWhiteSpace(email))
        {
            settings.Username = email.Trim();
            settings.FromAddress = email.Trim();
            if (string.IsNullOrWhiteSpace(settings.ToAddress))
                settings.ToAddress = email.Trim();
        }
    }
}
