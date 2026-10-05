namespace DynamicDave.Umbraco.UrlInspector.Models;

public sealed class UrlEntry
{
    public string? Culture { get; init; }
    public string? Url { get; init; }
    public required string Status { get; init; } // "ok" | "missing"
    public string? Message { get; init; }
}

public sealed class RedirectEntry
{
    public int StatusCode { get; init; } = 301;
    public required string From { get; init; }
    public string? To { get; init; }
    public string? Culture { get; init; }
}

public sealed class UrlInspectorResponse
{
    public required IReadOnlyList<UrlEntry> Urls { get; init; }
    public required IReadOnlyList<RedirectEntry> IncomingRedirects { get; init; }
    public RedirectEntry? OutgoingRedirect { get; init; }
    public string? Canonical { get; init; }
}

public sealed class TestUrlRequest
{
    public required string Url { get; init; }
}

public sealed class TestUrlResponse
{
    public bool Allowed { get; init; }
    public int? StatusCode { get; init; }
    public string? Location { get; init; }
    public string? Message { get; init; }
}
