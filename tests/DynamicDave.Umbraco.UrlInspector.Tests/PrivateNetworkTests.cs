using System.Net;
using DynamicDave.Umbraco.UrlInspector.Services;
using Xunit;

namespace DynamicDave.Umbraco.Tests;

public class PrivateNetworkTests
{
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.0.0.5")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("192.168.1.10")]
    [InlineData("169.254.169.254")]
    [InlineData("100.64.0.1")]
    [InlineData("0.0.0.0")]
    [InlineData("224.0.0.1")]
    [InlineData("255.255.255.255")]
    [InlineData("::1")]
    [InlineData("::")]
    [InlineData("fe80::1")]
    [InlineData("fd00::1")]
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("::ffff:169.254.169.254")]
    public void Internal_addresses_are_blocked(string address) => Assert.True(PrivateNetwork.IsBlocked(IPAddress.Parse(address)));

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("172.15.0.1")]
    [InlineData("172.32.0.1")]
    [InlineData("100.63.255.255")]
    [InlineData("192.169.0.1")]
    [InlineData("2a00:1450:4001:80b::200e")]
    [InlineData("::ffff:8.8.8.8")]
    public void Public_addresses_are_allowed(string address) => Assert.False(PrivateNetwork.IsBlocked(IPAddress.Parse(address)));
}
