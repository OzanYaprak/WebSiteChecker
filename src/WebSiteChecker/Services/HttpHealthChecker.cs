using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using WebSiteChecker.Helpers;
using WebSiteChecker.Models;

namespace WebSiteChecker.Services;

public class HttpHealthChecker
{
    private const int MaxRedirects = 10;
    private static readonly HttpRequestOptionsKey<bool> AllowPrivateNetworksKey = new("AllowPrivateNetworks");
    private static readonly HttpRequestOptionsKey<Uri> OriginalRequestUriKey = new("OriginalRequestUri");

    private static readonly HttpClient SharedClient = new(new SafeRedirectHandler())
    {
        DefaultRequestHeaders = { { "User-Agent", "WebSiteChecker/1.0" } }
    };

    public async Task<SiteCheckResult> CheckAsync(MonitoredSite site, CancellationToken cancellationToken = default)
    {
        var maxAttempts = site.RetryCount + 1;
        SiteCheckResult? lastResult = null;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (attempt > 0)
                await Task.Delay(SiteLimits.RetryDelayMilliseconds, cancellationToken);

            lastResult = await CheckOnceAsync(site, cancellationToken);

            if (lastResult.IsSuccess || !ShouldRetry(lastResult))
                return lastResult;
        }

        return lastResult!;
    }

    private async Task<SiteCheckResult> CheckOnceAsync(MonitoredSite site, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (!UrlSafetyValidator.IsUrlSafe(site.Url, out var safetyError, site.AllowPrivateNetworks))
            {
                stopwatch.Stop();
                return BuildResult(site.Id, false, null, stopwatch.ElapsedMilliseconds, safetyError);
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(site.TimeoutSeconds));

            using var headRequest = new HttpRequestMessage(HttpMethod.Head, site.Url);
            headRequest.Options.Set(AllowPrivateNetworksKey, site.AllowPrivateNetworks);
            headRequest.Options.Set(OriginalRequestUriKey, headRequest.RequestUri!);
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

    private static bool ShouldRetry(SiteCheckResult result)
    {
        if (result.IsSuccess)
            return false;

        if (result.ErrorMessage is "İstek zaman aşımına uğradı." or "Bağlantı hatası.")
            return true;

        if (result.StatusCode is 502 or 503 or 504)
            return true;

        return false;
    }

    private async Task<SiteCheckResult> CheckWithGetAsync(
        MonitoredSite site,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        stopwatch.Restart();

        using var getRequest = new HttpRequestMessage(HttpMethod.Get, site.Url);
        getRequest.Options.Set(AllowPrivateNetworksKey, site.AllowPrivateNetworks);
        getRequest.Options.Set(OriginalRequestUriKey, getRequest.RequestUri!);
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
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is InvalidOperationException)
                return current.Message;

            if (current is HttpRequestException { Message: var message }
                && message.Contains("Yerel veya özel ağ", StringComparison.Ordinal))
                return message;
        }

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

    private sealed class SafeRedirectHandler : DelegatingHandler
    {
        public SafeRedirectHandler()
        {
            InnerHandler = new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                // TCP bağlantısı kurulurken IP'yi tekrar doğrula (DNS rebinding koruması):
                // ön kontroldeki DNS cevabı ile istek anındaki cevap farklı olabilir.
                ConnectCallback = ConnectWithValidationAsync
            };
        }

        private static async ValueTask<Stream> ConnectWithValidationAsync(
            SocketsHttpConnectionContext context,
            CancellationToken cancellationToken)
        {
            // Kurumsal proxy üzerinden giden isteklerde önce proxy'ye (özel IP) bağlanılır.
            // CONNECT aşamasında RequestUri de proxy adresini gösterir; sistem proxy'sini tanı.
            if (IsConfiguredProxyEndpoint(context))
                return await ConnectDirectAsync(context, cancellationToken);

            var allowPrivate = context.InitialRequestMessage is not null
                && context.InitialRequestMessage.Options.TryGetValue(AllowPrivateNetworksKey, out var allow)
                && allow;

            var host = context.DnsEndPoint.Host;
            IPAddress[] addresses = IPAddress.TryParse(host, out var literal)
                ? [literal]
                : await Dns.GetHostAddressesAsync(host, cancellationToken);

            var permitted = allowPrivate
                ? addresses
                : addresses.Where(address => !UrlSafetyValidator.IsBlockedIpAddress(address)).ToArray();

            if (permitted.Length == 0)
                throw new HttpRequestException("Yerel veya özel ağ adreslerine istek gönderilemez.");

            return await ConnectAsync(permitted, context.DnsEndPoint.Port, cancellationToken);
        }

        private static bool IsConfiguredProxyEndpoint(SocketsHttpConnectionContext context)
        {
            var host = context.DnsEndPoint.Host;
            var port = context.DnsEndPoint.Port;
            var probeUri = context.InitialRequestMessage?.Options.TryGetValue(OriginalRequestUriKey, out var original) == true
                ? original
                : context.InitialRequestMessage?.RequestUri;

            if (probeUri is null)
                return false;

            var proxyUri = HttpClient.DefaultProxy.GetProxy(probeUri);
            if (proxyUri is null)
                return false;

            return string.Equals(proxyUri.Host, host.TrimEnd('.'), StringComparison.OrdinalIgnoreCase)
                && proxyUri.Port == port;
        }

        private static async ValueTask<Stream> ConnectDirectAsync(
            SocketsHttpConnectionContext context,
            CancellationToken cancellationToken)
        {
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(context.DnsEndPoint, cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        private static async ValueTask<Stream> ConnectAsync(
            IPAddress[] addresses,
            int port,
            CancellationToken cancellationToken)
        {
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(addresses, port, cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            for (var redirectCount = 0; redirectCount <= MaxRedirects; redirectCount++)
            {
                if (request.RequestUri is null)
                    throw new InvalidOperationException("İzin verilmeyen yönlendirme adresi.");

                if (!UrlSafetyValidator.IsUriSafe(
                        request.RequestUri,
                        out var safetyError,
                        request.Options.TryGetValue(AllowPrivateNetworksKey, out var allowPrivate) && allowPrivate))
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

                request.Options.Set(OriginalRequestUriKey, request.RequestUri);

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
