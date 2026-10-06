using DynamicDave.Umbraco.UrlInspector.Models;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;

namespace DynamicDave.Umbraco.UrlInspector.Services;

public class UrlInspectorService(
    IContentService contentService,
    IIdKeyMap idKeyMap,
    IPublishedUrlInfoProvider urlInfoProvider,
    IRedirectUrlService redirectUrlService,
    IHostingEnvironment hostingEnvironment,
    IHttpClientFactory httpClientFactory)
{
    // The configured application URL (never the request Host header, which is client controlled).
    private Uri? BaseUri => hostingEnvironment.ApplicationMainUrl;

    public async Task<UrlInspectorResponse?> InspectAsync(Guid key)
    {
        var content = GetContent(key);
        if (content is null) return null;

        var infos = await urlInfoProvider.GetAllAsync(content);
        var urls = UrlEntryDeduplicator.Deduplicate(infos.Select(i =>
        {
            var raw = i.Url?.ToString();
            return new UrlEntry
            {
                Culture = i.Culture,
                Url = string.IsNullOrEmpty(raw) ? null : Absolutize(raw),
                Status = string.IsNullOrEmpty(raw) ? "missing" : "ok",
                Message = i.Message,
            };
        })).ToList();

        var incoming = redirectUrlService.GetContentRedirectUrls(key).Select(r => new RedirectEntry
        {
            From = RedirectPathHelper.Normalize(r.Url),
            To = (urls.FirstOrDefault(u => u.Url is not null && u.Culture == r.Culture) ?? urls.FirstOrDefault(u => u.Url is not null))?.Url,
            Culture = r.Culture,
        }).OrderBy(r => r.From, StringComparer.Ordinal).ToList();

        // Outgoing: one of this page's own URLs is itself registered as a redirect source to another page.
        // Limitation: sources stored with a "<domainRootId>/" prefix are not queried (no cheap lookup API).
        RedirectEntry? outgoing = null;
        foreach (var url in urls.Where(u => u.Url is not null))
        {
            var path = Uri.TryCreate(url.Url, UriKind.Absolute, out var abs) && IsHttp(abs) ? abs.AbsolutePath : url.Url!;
            foreach (var candidate in RedirectPathHelper.LookupCandidates(path))
            {
                var hit = redirectUrlService.GetMostRecentRedirectUrl(candidate, url.Culture);
                if (hit is null || hit.ContentKey == key) continue;

                outgoing = new RedirectEntry
                {
                    From = path,
                    To = await ResolveDestinationAsync(hit.ContentKey, hit.Culture ?? url.Culture),
                    Culture = url.Culture,
                };
                break;
            }

            if (outgoing is not null) break;
        }

        var canonical = urls.FirstOrDefault(u => u.Url is not null)?.Url;
        return new UrlInspectorResponse { Urls = urls, IncomingRedirects = incoming, OutgoingRedirect = outgoing, Canonical = canonical };
    }

    /// <summary>Authorities (host + effective port) the URL tester may contact: the document's own absolute URLs plus the configured application URL.</summary>
    public IReadOnlyList<string> GetAllowedAuthorities(UrlInspectorResponse inspected)
        => inspected.Urls
            .Where(u => u.Url is not null)
            .Select(u => Uri.TryCreate(u.Url, UriKind.Absolute, out var uri) && IsHttp(uri) ? UrlTestPolicy.AuthorityOf(uri) : null)
            .Append(BaseUri is { } b && IsHttp(b) ? UrlTestPolicy.AuthorityOf(b) : null)
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public async Task<TestUrlResponse> TestAsync(string? rawUrl, IEnumerable<string> allowedAuthorities, CancellationToken cancellationToken = default)
    {
        var trimmed = rawUrl?.Trim();
        if (trimmed is not null && trimmed.StartsWith('/') && !trimmed.StartsWith("//") && !trimmed.StartsWith("/\\") && BaseUri is null)
            return new TestUrlResponse { Allowed = false, Message = "No base URL" };

        if (!UrlTestPolicy.TryResolveTarget(rawUrl, BaseUri, out var parsed))
            return new TestUrlResponse { Allowed = false, Message = "Invalid URL" };

        if (!UrlTestPolicy.IsAllowed(parsed, allowedAuthorities))
            return new TestUrlResponse { Allowed = false, Message = "Host not allowed" };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            var client = httpClientFactory.CreateClient(Constants.HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Head, parsed);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            return new TestUrlResponse
            {
                Allowed = true,
                StatusCode = (int)response.StatusCode,
                Location = response.Headers.Location?.ToString(),
            };
        }
        catch (HttpRequestException ex) when (ex.InnerException is PrivateNetworkBlockedException)
        {
            return new TestUrlResponse { Allowed = false, Message = "Address not allowed" };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            var message = ex is TaskCanceledException
                ? (cts.IsCancellationRequested ? "Timed out" : "Request cancelled")
                : "Request failed";
            return new TestUrlResponse { Allowed = true, Message = message };
        }
    }

    private async Task<string?> ResolveDestinationAsync(Guid targetKey, string? culture)
    {
        var target = GetContent(targetKey);
        if (target is null) return null;

        var infos = (await urlInfoProvider.GetAllAsync(target)).Where(i => i.Url is not null).ToList();
        var info = infos.FirstOrDefault(i => i.Culture == culture) ?? infos.FirstOrDefault();
        return info?.Url?.ToString() is { Length: > 0 } u ? Absolutize(u) : null;
    }

    // Not IContentService.GetById(Guid): Umbraco 18 moved it to IContentServiceBase<T>, so a call compiled against
    // Umbraco 17 fails there with a MissingMethodException. GetById(int) is on IContentService in both.
    private IContent? GetContent(Guid key)
    {
        var id = idKeyMap.GetIdForKey(key, UmbracoObjectTypes.Document);
        return id.Success ? contentService.GetById(id.Result) : null;
    }

    private static bool IsHttp(Uri uri) => uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;

    // Relative URLs stay relative when there is no configured application URL.
    private string Absolutize(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var abs) && IsHttp(abs)) return url;
        var baseUri = BaseUri;
        return baseUri is not null && url.StartsWith('/') && Uri.TryCreate(baseUri, url, out var resolved) ? resolved.ToString() : url;
    }
}
