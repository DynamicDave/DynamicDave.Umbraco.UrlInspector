using DynamicDave.Umbraco.UrlInspector.Services;
using Xunit;

namespace DynamicDave.Umbraco.Tests;

public class UrlTestPolicyTests
{
    private static readonly string[] Hosts = Allow("https://www.klant.nl/", "https://localhost/");

    private static string[] Allow(params string[] urls) => urls.Select(u => UrlTestPolicy.AuthorityOf(new Uri(u))).ToArray();

    [Fact] public void Allowed_host_over_https() => Assert.True(UrlTestPolicy.IsAllowed(new Uri("https://www.klant.nl/diensten/"), Hosts));
    [Fact] public void Host_comparison_is_case_insensitive() => Assert.True(UrlTestPolicy.IsAllowed(new Uri("https://WWW.Klant.NL/"), Hosts));
    [Fact] public void Other_port_on_allowed_host_is_denied() => Assert.False(UrlTestPolicy.IsAllowed(new Uri("https://localhost:44381/x"), Hosts));
    [Fact] public void Explicit_default_port_equals_implicit() => Assert.True(UrlTestPolicy.IsAllowed(new Uri("https://www.klant.nl:443/x"), Hosts));
    [Fact] public void Allowed_explicit_port_matches() => Assert.True(UrlTestPolicy.IsAllowed(new Uri("http://localhost:5000/x"), Allow("http://localhost:5000/")));
    [Fact] public void Http_on_https_only_host_is_denied() => Assert.False(UrlTestPolicy.IsAllowed(new Uri("http://www.klant.nl/"), Hosts));
    [Fact] public void Authority_is_lowercase_host_and_effective_port() => Assert.Equal("www.klant.nl:443", UrlTestPolicy.AuthorityOf(new Uri("https://WWW.Klant.NL/")));
    [Fact] public void Foreign_host_is_denied() => Assert.False(UrlTestPolicy.IsAllowed(new Uri("https://evil.example/"), Hosts));
    [Fact] public void Metadata_ip_is_denied() => Assert.False(UrlTestPolicy.IsAllowed(new Uri("http://169.254.169.254/latest/meta-data"), Hosts));
    [Fact] public void Lookalike_suffix_is_denied() => Assert.False(UrlTestPolicy.IsAllowed(new Uri("https://www.klant.nl.evil.example/"), Hosts));
    [Fact] public void File_scheme_is_denied() => Assert.False(UrlTestPolicy.IsAllowed(new Uri("file:///c:/windows/win.ini"), Hosts));
    [Fact] public void Ftp_scheme_is_denied() => Assert.False(UrlTestPolicy.IsAllowed(new Uri("ftp://www.klant.nl/"), Hosts));
    [Fact] public void Overlong_url_is_rejected() => Assert.False(UrlTestPolicy.TryResolveTarget("https://www.klant.nl/" + new string('a', UrlTestPolicy.MaxUrlLength), null, out _));

    private static readonly Uri Base = new("https://www.klant.nl/");

    [Fact]
    public void Root_relative_resolves_against_base()
    {
        Assert.True(UrlTestPolicy.TryResolveTarget("/nl/diensten/", Base, out var t));
        Assert.Equal("https://www.klant.nl/nl/diensten/", t.ToString());
    }

    [Fact] public void Root_relative_without_base_is_rejected() => Assert.False(UrlTestPolicy.TryResolveTarget("/x", null, out _));
    [Fact] public void Protocol_relative_is_rejected() => Assert.False(UrlTestPolicy.TryResolveTarget("//evil.example/x", Base, out _));
    [Fact] public void Backslash_protocol_relative_is_rejected() => Assert.False(UrlTestPolicy.TryResolveTarget("/\\evil.example", Base, out _));
    [Fact] public void File_url_is_rejected() => Assert.False(UrlTestPolicy.TryResolveTarget("file:///etc/passwd", Base, out _));
    [Fact] public void Ftp_url_is_rejected() => Assert.False(UrlTestPolicy.TryResolveTarget("ftp://x", Base, out _));
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("relative/path")]
    public void Empty_or_non_absolute_input_is_rejected(string? raw) => Assert.False(UrlTestPolicy.TryResolveTarget(raw, Base, out _));

    [Fact]
    public void Userinfo_trick_resolves_but_is_denied()
    {
        Assert.True(UrlTestPolicy.TryResolveTarget("https://www.klant.nl@evil.example/", Base, out var t));
        Assert.Equal("evil.example", t.Host);
        Assert.False(UrlTestPolicy.IsAllowed(t, Hosts));
    }

    [Fact]
    public void Userinfo_on_allowed_host_is_allowed()
    {
        Assert.True(UrlTestPolicy.TryResolveTarget("https://evil@www.klant.nl/", Base, out var t));
        Assert.Equal("www.klant.nl", t.Host);
        Assert.True(UrlTestPolicy.IsAllowed(t, Hosts));
    }

    [Fact] public void Alternate_port_on_allowed_host_is_denied() => Assert.False(UrlTestPolicy.IsAllowed(new Uri("http://www.klant.nl:8080/"), Hosts));
    [Fact] public void Empty_allow_list_denies_everything() => Assert.False(UrlTestPolicy.IsAllowed(new Uri("https://www.klant.nl/"), []));
}
