using DynamicDave.Umbraco.UrlInspector.Services;
using Xunit;

namespace DynamicDave.Umbraco.Tests;

public class RedirectPathHelperTests
{
    [Theory]
    [InlineData("/old-page", "/old-page")]
    [InlineData("old-page", "/old-page")]
    [InlineData("1078/old-page", "/old-page")]
    [InlineData("1078/diensten/website", "/diensten/website")]
    [InlineData("/2024/archive", "/2024/archive")]   // a leading slash means no domain-id prefix
    [InlineData("", "/")]
    public void Normalizes_redirect_urls(string input, string expected)
        => Assert.Equal(expected, RedirectPathHelper.Normalize(input));

    [Theory]
    [InlineData("/nl/diensten/", new[] { "/nl/diensten/", "/nl/diensten" })]
    [InlineData("/nl/diensten", new[] { "/nl/diensten", "/nl/diensten/" })]
    [InlineData("/", new[] { "/" })]
    public void Lookup_candidates_cover_trailing_slash_forms(string path, string[] expected)
        => Assert.Equal(expected, RedirectPathHelper.LookupCandidates(path));
}
