namespace WebSiteChecker.Models;

public class AlertState
{
    public Guid SiteId { get; set; }
    public bool IsDown { get; set; }
    public DateTime? LastDownAlertSentAt { get; set; }
    public DateTime? LastRecoveryAlertSentAt { get; set; }
}
