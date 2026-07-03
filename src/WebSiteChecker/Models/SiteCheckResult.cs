namespace WebSiteChecker.Models;

public class SiteCheckResult
{
    public Guid SiteId { get; init; }
    public bool IsSuccess { get; init; }
    public int? StatusCode { get; init; }
    public long ResponseTimeMs { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;
}
