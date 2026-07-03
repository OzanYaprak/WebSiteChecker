namespace WebSiteChecker.Models;

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public int AlertCooldownMinutes { get; set; } = 30;
    public bool SendRecoveryEmail { get; set; } = true;
}
