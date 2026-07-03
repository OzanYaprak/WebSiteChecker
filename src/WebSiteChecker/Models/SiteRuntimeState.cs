namespace WebSiteChecker.Models;

public class SiteRuntimeState
{
    public Guid SiteId { get; set; }
    public SiteStatus Status { get; set; } = SiteStatus.Unknown;
    public DateTime? LastCheckedAt { get; set; }
    public long? LastResponseTimeMs { get; set; }
    public int? LastStatusCode { get; set; }
    public string? LastError { get; set; }
}
