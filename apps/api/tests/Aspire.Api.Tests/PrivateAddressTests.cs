using System.Net;
using Aspire.Infrastructure.Net;
using Xunit;

namespace Aspire.Api.Tests;

/// <summary>
/// Which addresses the API may fetch a picture from (D56).
///
/// This is the whole fence around server-side request forgery, so every range
/// in it is here. A hole is not something anybody notices from the outside:
/// the feature keeps working perfectly while the server reads whatever is on
/// its own network.
/// </summary>
public sealed class PrivateAddressTests
{
    [Theory]
    // The real internet, which is the whole point of the feature.
    [InlineData("1.1.1.1")]
    [InlineData("8.8.8.8")]
    [InlineData("151.101.1.84")]
    [InlineData("2606:4700:4700::1111")]
    [InlineData("2a00:1450:4014:80e::200e")]
    public void The_internet_is_reachable(string address)
    {
        Assert.True(PrivateAddress.IsPublic(IPAddress.Parse(address)));
    }

    [Theory]
    // The API talking to itself.
    [InlineData("127.0.0.1")]
    [InlineData("127.1.2.3")]
    [InlineData("::1")]
    [InlineData("0.0.0.0")]
    [InlineData("::")]
    // The Docker network the database is on.
    [InlineData("172.17.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("10.0.0.1")]
    [InlineData("192.168.1.1")]
    // Where a cloud provider keeps the credentials.
    [InlineData("169.254.169.254")]
    [InlineData("fe80::1")]
    // A provider's own network, between the box and the internet.
    [InlineData("100.64.0.1")]
    [InlineData("100.127.255.255")]
    // Multicast and reserved.
    [InlineData("224.0.0.1")]
    [InlineData("255.255.255.255")]
    [InlineData("ff02::1")]
    // IPv6 private space.
    [InlineData("fc00::1")]
    [InlineData("fd12:3456::1")]
    public void Everything_inside_the_network_is_refused(string address)
    {
        Assert.False(PrivateAddress.IsPublic(IPAddress.Parse(address)));
    }

    [Theory]
    // An IPv4 address wearing an IPv6 coat is still that IPv4 address — the
    // oldest way past a check that only looks at one family.
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("::ffff:169.254.169.254")]
    [InlineData("::ffff:10.0.0.1")]
    [InlineData("::ffff:192.168.0.1")]
    public void A_private_address_hiding_in_an_IPv6_one_is_still_refused(string address)
    {
        Assert.False(PrivateAddress.IsPublic(IPAddress.Parse(address)));
    }

    [Fact]
    public void A_mapped_public_address_is_still_reachable()
    {
        // The unwrapping must not refuse everything it unwraps.
        Assert.True(PrivateAddress.IsPublic(IPAddress.Parse("::ffff:8.8.8.8")));
    }

    [Theory]
    // Ranges nothing real answers on, so nothing is lost by refusing them and
    // they are a standing invitation otherwise.
    [InlineData("192.0.0.1")]
    [InlineData("192.0.2.1")]
    [InlineData("198.18.0.1")]
    [InlineData("198.51.100.1")]
    [InlineData("203.0.113.1")]
    [InlineData("2001:db8::1")]
    [InlineData("64:ff9b::7f00:1")]
    public void The_documentation_and_benchmark_ranges_are_refused(string address)
    {
        Assert.False(PrivateAddress.IsPublic(IPAddress.Parse(address)));
    }
}
