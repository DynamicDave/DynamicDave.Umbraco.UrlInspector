using System.Text.RegularExpressions;

namespace DynamicDave.Umbraco.UrlInspector.Services;

internal static partial class RedirectPathHelper
{
    [GeneratedRegex(@"^\d+/")]
    private static partial Regex DomainPrefix();

    /// <summary>Path forms to look up as redirect source: as given, without and with trailing slash.</summary>
    public static IReadOnlyList<string> LookupCandidates(string path)
    {
        var trimmed = path.Length > 1 ? path.TrimEnd('/') : path;
        var candidates = new List<string> { path };
        if (trimmed.Length > 0 && !candidates.Contains(trimmed)) candidates.Add(trimmed);
        var slashed = trimmed.EndsWith('/') ? trimmed : trimmed + "/";
        if (!candidates.Contains(slashed)) candidates.Add(slashed);
        return candidates;
    }

    public static string Normalize(string redirectUrl)
    {
        var value = redirectUrl ?? string.Empty;
        if (!value.StartsWith('/')) value = DomainPrefix().Replace(value, string.Empty);
        return value.StartsWith('/') ? value : "/" + value;
    }
}
