using DynamicDave.Umbraco.UrlInspector.Models;

namespace DynamicDave.Umbraco.UrlInspector.Services;

internal static class UrlEntryDeduplicator
{
    /// <summary>
    /// Collapses entries that point at the same (culture, path): prefers the absolute form, and an entry with a URL
    /// over a missing one. Entries on different absolute hosts are kept. First-occurrence order is preserved.
    /// </summary>
    public static IReadOnlyList<UrlEntry> Deduplicate(IEnumerable<UrlEntry> entries)
    {
        var result = new List<UrlEntry>();
        var withUrl = entries.Where(e => !string.IsNullOrEmpty(e.Url)).ToList();
        var culturesWithUrl = withUrl.Select(e => e.Culture ?? string.Empty).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var group in entries.GroupBy(Key))
        {
            var first = group.First();
            if (string.IsNullOrEmpty(first.Url))
            {
                // Missing entries only survive when the culture has no URL at all.
                if (!culturesWithUrl.Contains(first.Culture ?? string.Empty))
                    result.Add(first);
                continue;
            }

            var absolute = group.Where(e => TryGetAbsolute(e.Url!, out _)).ToList();
            if (absolute.Count == 0)
            {
                result.Add(first);
                continue;
            }

            result.AddRange(absolute.GroupBy(e => TryGetAbsolute(e.Url!, out var u) ? u!.Host : string.Empty, StringComparer.OrdinalIgnoreCase).Select(g => g.First()));
        }

        return OrderByFirstOccurrence(entries, result);
    }

    private static IReadOnlyList<UrlEntry> OrderByFirstOccurrence(IEnumerable<UrlEntry> original, List<UrlEntry> kept)
    {
        // Position of a kept entry = position of the first original entry sharing its group key.
        var firstIndex = new Dictionary<(string, string), int>();
        var i = 0;
        foreach (var e in original)
        {
            firstIndex.TryAdd(Key(e), i);
            i++;
        }

        return kept.OrderBy(k => firstIndex[Key(k)]).ToList();
    }

    private static (string Culture, string Path) Key(UrlEntry e)
        => ((e.Culture ?? string.Empty).ToLowerInvariant(), NormalizePath(e.Url));

    private static string NormalizePath(string? url)
    {
        if (string.IsNullOrEmpty(url)) return string.Empty;
        var path = TryGetAbsolute(url, out var abs) ? abs!.AbsolutePath : url;
        var cut = path.IndexOfAny(['?', '#']);
        if (cut >= 0) path = path[..cut];
        return (path.Length > 1 ? path.TrimEnd('/') : path).ToLowerInvariant();
    }

    private static bool TryGetAbsolute(string url, out Uri? uri)
        => Uri.TryCreate(url, UriKind.Absolute, out uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
