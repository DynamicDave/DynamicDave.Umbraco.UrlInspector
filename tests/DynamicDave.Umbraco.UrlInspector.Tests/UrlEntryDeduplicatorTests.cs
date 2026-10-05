using DynamicDave.Umbraco.UrlInspector.Models;
using DynamicDave.Umbraco.UrlInspector.Services;
using Xunit;

namespace DynamicDave.Umbraco.Tests;

public class UrlEntryDeduplicatorTests
{
    private static UrlEntry Ok(string culture, string url) => new() { Culture = culture, Url = url, Status = "ok" };
    private static UrlEntry Missing(string culture) => new() { Culture = culture, Url = null, Status = "missing", Message = "unpublished" };

    [Fact]
    public void Prefers_absolute_over_relative_for_same_culture_and_path()
    {
        var result = UrlEntryDeduplicator.Deduplicate(new[]
        {
            Ok("en-US", "/en/services/"),
            Ok("en-US", "https://localhost:44378/en/services/"),
        });

        var single = Assert.Single(result);
        Assert.Equal("https://localhost:44378/en/services/", single.Url);
    }

    [Fact]
    public void Keeps_distinct_cultures_and_paths()
    {
        var result = UrlEntryDeduplicator.Deduplicate(new[]
        {
            Ok("nl-NL", "https://localhost:44378/nl/diensten/"),
            Ok("en-US", "https://localhost:44378/en/services/"),
            Ok("en-US", "https://localhost:44378/en/other/"),
        });

        Assert.Equal(3, result.Count);
        Assert.Equal("nl-NL", result[0].Culture);
    }

    [Fact]
    public void Ignores_trailing_slash_and_case_when_matching()
    {
        var result = UrlEntryDeduplicator.Deduplicate(new[]
        {
            Ok("en-US", "/EN/Services"),
            Ok("en-US", "https://localhost:44378/en/services/"),
        });

        Assert.Equal("https://localhost:44378/en/services/", Assert.Single(result).Url);
    }

    [Fact]
    public void Prefers_entry_with_url_over_missing_for_same_culture()
    {
        var result = UrlEntryDeduplicator.Deduplicate(new[]
        {
            Missing("en-US"),
            Ok("en-US", "/en/services/"),
        });

        Assert.Equal("/en/services/", Assert.Single(result).Url);
    }

    [Fact]
    public void Keeps_missing_entry_when_culture_has_no_url()
    {
        var result = UrlEntryDeduplicator.Deduplicate(new[] { Missing("da"), Ok("en-US", "/en/") });

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Culture == "da" && r.Url is null);
    }

    [Fact]
    public void Keeps_same_path_on_different_absolute_hosts()
    {
        var result = UrlEntryDeduplicator.Deduplicate(new[]
        {
            Ok("en-US", "https://a.example/en/"),
            Ok("en-US", "https://b.example/en/"),
        });

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Keeps_first_occurrence_order_and_handles_null_culture()
    {
        var result = UrlEntryDeduplicator.Deduplicate(new[]
        {
            Ok(null!, "/a/"),
            Ok(null!, "https://x.example/a/"),
            Ok(null!, "/b/"),
        });

        Assert.Equal(new[] { "https://x.example/a/", "/b/" }, result.Select(r => r.Url));
    }

    [Fact]
    public void Same_path_in_different_cultures_stays_separate()
    {
        var result = UrlEntryDeduplicator.Deduplicate(new[] { Ok("nl-NL", "/x/"), Ok("en-US", "/x/") });

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Relative_only_entries_are_kept()
    {
        var result = UrlEntryDeduplicator.Deduplicate(new[] { Ok("nl-NL", "/a/"), Ok("nl-NL", "/b/") });

        Assert.Equal(new[] { "/a/", "/b/" }, result.Select(r => r.Url));
    }

    [Fact]
    public void Several_missing_entries_in_one_culture_collapse_to_one()
    {
        var result = UrlEntryDeduplicator.Deduplicate(new[] { Missing("da"), Missing("da") });

        Assert.Single(result);
    }
}
