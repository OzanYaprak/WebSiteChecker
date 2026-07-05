namespace WebSiteChecker.Helpers;

public static class SiteLimits
{
    public const int MaxSites = 50;
    public const int MinIntervalSeconds = 5;
    public const int MaxIntervalSeconds = 86400;
    public const int MinTimeoutSeconds = 1;
    public const int MaxTimeoutSeconds = 60;
    public const int MaxConcurrentChecks = 10;
}
