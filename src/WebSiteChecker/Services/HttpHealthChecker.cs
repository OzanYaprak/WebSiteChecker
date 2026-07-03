using System.Diagnostics;
using System.Net.Http;
using WebSiteChecker.Models;

namespace WebSiteChecker.Services;

public class HttpHealthChecker
{
    private static readonly HttpClient SharedClient = new(new HttpClientHandler
    {
        AllowAutoRedirect = true
    })
    {
        DefaultRequestHeaders = { { "User-Agent", "WebSiteChecker/1.0" } }
    };

    public async Task<SiteCheckResult> CheckAsync(MonitoredSite site, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(site.TimeoutSeconds));

            using var headRequest = new HttpRequestMessage(HttpMethod.Head, site.Url);
            using var headResponse = await SharedClient.SendAsync(
                headRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cts.Token);

            stopwatch.Stop();

            if (headResponse.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed ||
                headResponse.StatusCode == System.Net.HttpStatusCode.NotImplemented)
            {
                return await CheckWithGetAsync(site, stopwatch, cts.Token);
            }

            var statusCode = (int)headResponse.StatusCode;
            return BuildResult(site.Id, statusCode == site.ExpectedStatusCode,
                statusCode, stopwatch.ElapsedMilliseconds, null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            return BuildResult(site.Id, false, null, stopwatch.ElapsedMilliseconds, "İstek zaman aşımına uğradı.");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return BuildResult(site.Id, false, null, stopwatch.ElapsedMilliseconds, ex.Message);
        }
    }

    private async Task<SiteCheckResult> CheckWithGetAsync(
        MonitoredSite site,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        stopwatch.Restart();

        using var getRequest = new HttpRequestMessage(HttpMethod.Get, site.Url);
        using var getResponse = await SharedClient.SendAsync(
            getRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        stopwatch.Stop();

        var statusCode = (int)getResponse.StatusCode;
        return BuildResult(site.Id, statusCode == site.ExpectedStatusCode,
            statusCode, stopwatch.ElapsedMilliseconds, null);
    }

    private static SiteCheckResult BuildResult(
        Guid siteId,
        bool isSuccess,
        int? statusCode,
        long responseTimeMs,
        string? errorMessage)
    {
        if (!isSuccess && string.IsNullOrEmpty(errorMessage) && statusCode.HasValue)
            errorMessage = $"Beklenmeyen durum kodu: {statusCode}";

        return new SiteCheckResult
        {
            SiteId = siteId,
            IsSuccess = isSuccess,
            StatusCode = statusCode,
            ResponseTimeMs = responseTimeMs,
            ErrorMessage = errorMessage,
            CheckedAt = DateTime.UtcNow
        };
    }
}
