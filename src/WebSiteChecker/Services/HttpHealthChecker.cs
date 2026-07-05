using System.Diagnostics;
using System.Net;
using System.Net.Http;
using WebSiteChecker.Helpers;
using WebSiteChecker.Models;

namespace WebSiteChecker.Services;

public class HttpHealthChecker
{
    private const int MaxRedirects = 10;

    private static readonly HttpClient SharedClient = new(new SafeRedirectHandler())
    {
        DefaultRequestHeaders = { { "User-Agent", "WebSiteChecker/1.0" } }
    };

    public async Task<SiteCheckResult> CheckAsync(MonitoredSite site, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (!UrlSafetyValidator.IsUrlSafe(site.Url, out var safetyError))
            {
                stopwatch.Stop();
                return BuildResult(site.Id, false, null, stopwatch.ElapsedMilliseconds, safetyError);
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(site.TimeoutSeconds));

            using var headRequest = new HttpRequestMessage(HttpMethod.Head, site.Url);
            using var headResponse = await SharedClient.SendAsync(
                headRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cts.Token);

            stopwatch.Stop();

            if (headResponse.StatusCode == HttpStatusCode.MethodNotAllowed ||
                headResponse.StatusCode == HttpStatusCode.NotImplemented)
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
            return BuildResult(site.Id, false, null, stopwatch.ElapsedMilliseconds, ToPublicErrorMessage(ex));
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

    private static string ToPublicErrorMessage(Exception ex)
    {
        if (ex is InvalidOperationException)
            return ex.Message;

        if (ex.InnerException is InvalidOperationException inner)
            return inner.Message;

        Debug.WriteLine($"HTTP check error: {ex}");
        return "Bağlantı hatası.";
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

    private sealed class SafeRedirectHandler : HttpClientHandler
    {
        public SafeRedirectHandler()
        {
            AllowAutoRedirect = false;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            for (var redirectCount = 0; redirectCount <= MaxRedirects; redirectCount++)
            {
                if (request.RequestUri is null)
                    throw new InvalidOperationException("İzin verilmeyen yönlendirme adresi.");

                if (!UrlSafetyValidator.IsUriSafe(request.RequestUri, out var safetyError))
                    throw new InvalidOperationException(safetyError ?? "İzin verilmeyen yönlendirme adresi.");

                var response = await base.SendAsync(request, cancellationToken);

                if (!IsRedirectStatusCode(response.StatusCode))
                    return response;

                var location = response.Headers.Location;
                if (location is null)
                    return response;

                response.Dispose();

                request.RequestUri = location.IsAbsoluteUri
                    ? location
                    : new Uri(request.RequestUri, location);

                if (request.Method == HttpMethod.Post || request.Method == HttpMethod.Put)
                    request.Method = HttpMethod.Get;

                request.Headers.Host = null;
            }

            throw new InvalidOperationException("Çok fazla yönlendirme algılandı.");
        }

        private static bool IsRedirectStatusCode(HttpStatusCode statusCode) =>
            statusCode is HttpStatusCode.MovedPermanently
                or HttpStatusCode.Redirect
                or HttpStatusCode.RedirectMethod
                or HttpStatusCode.TemporaryRedirect
                or HttpStatusCode.PermanentRedirect;
    }
}
