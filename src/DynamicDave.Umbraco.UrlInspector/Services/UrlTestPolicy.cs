namespace DynamicDave.Umbraco.UrlInspector.Services;

internal static class UrlTestPolicy
{
    public const int MaxUrlLength = 2048;

    /// <summary>Lower-case host plus effective port (default ports filled in), e.g. "www.klant.nl:443".</summary>
    public static string AuthorityOf(Uri uri) => $"{uri.Host.ToLowerInvariant()}:{uri.Port}";

    public static bool IsAllowed(Uri target, IEnumerable<string> allowedAuthorities)
    {
        if (target.Scheme != Uri.UriSchemeHttp && target.Scheme != Uri.UriSchemeHttps) return false;
        return allowedAuthorities.Any(h => string.Equals(h, AuthorityOf(target), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Resolves user input to an absolute http(s) URI. Root-relative paths are resolved against
    /// <paramref name="baseUri"/>; everything else must be an absolute http(s) URL.
    /// </summary>
    public static bool TryResolveTarget(string? raw, Uri? baseUri, out Uri target)
    {
        target = null!;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        raw = raw.Trim();
        if (raw.Length > MaxUrlLength) return false;

        if (raw.StartsWith('/'))
        {
            if (raw.StartsWith("//") || raw.StartsWith("/\\")) return false;
            if (baseUri is null || !baseUri.IsAbsoluteUri) return false;
            if (!Uri.TryCreate(baseUri, raw, out var resolved)) return false;
            target = resolved;
        }
        else
        {
            if (!Uri.TryCreate(raw, UriKind.Absolute, out var abs)) return false;
            target = abs;
        }

        return target.Scheme == Uri.UriSchemeHttp || target.Scheme == Uri.UriSchemeHttps;
    }
}
