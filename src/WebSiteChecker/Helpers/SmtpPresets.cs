using WebSiteChecker.Models;

namespace WebSiteChecker.Helpers;

public static class SmtpPresets
{
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
