using System.Net;
using System.Net.Sockets;

namespace Aspire.Infrastructure.Net;

/// <summary>
/// Whether the API may open a connection to an address (D56).
///
/// Fetching a picture from a link somebody pasted means this server makes a
/// request on their behalf, and a request made from inside the VPS reaches
/// things nothing on the internet can: the Postgres container, the Docker
/// daemon, a cloud provider's metadata service on 169.254.169.254. That is
/// server-side request forgery, and the only reliable fence is to refuse the
/// *address* rather than the name — a name can resolve to anything, and can
/// resolve to something different the second time it is asked.
///
/// Pure, so every range is a test rather than something anybody has to
/// reason about twice.
/// </summary>
public static class PrivateAddress
{
    /// <summary>
    /// Whether this address is somewhere on the public internet, and so
    /// somewhere this server may fetch from.
    ///
    /// Everything not obviously public is refused rather than everything
    /// known-bad being listed: the list of things reachable from inside a
    /// network is not one anybody finishes writing.
    /// </summary>
    public static bool IsPublic(IPAddress address)
    {
        // An IPv4 address wearing an IPv6 coat is still that IPv4 address, and
        // ::ffff:127.0.0.1 is still loopback.
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();

        if (IPAddress.IsLoopback(address)) return false;
        if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any)) return false;

        return address.AddressFamily switch
        {
            AddressFamily.InterNetwork => IsPublicV4(address),
            AddressFamily.InterNetworkV6 => IsPublicV6(address),
            // Anything that is not IP is not something to connect to.
            _ => false
        };
    }

    private static bool IsPublicV4(IPAddress address)
    {
        var b = address.GetAddressBytes();

        return b[0] switch
        {
            // 0.0.0.0/8 — "this network", and 10/8 — private.
            0 or 10 => false,
            // 127/8 — loopback, caught above as well.
            127 => false,
            // 100.64/10 — carrier-grade NAT, which is a provider's own network.
            100 when b[1] >= 64 && b[1] <= 127 => false,
            // 169.254/16 — link-local, where cloud metadata services live.
            169 when b[1] == 254 => false,
            // 172.16/12 — private, which is where Docker puts its networks.
            172 when b[1] >= 16 && b[1] <= 31 => false,
            // 192.0.0/24 and 192.0.2/24 — protocol assignments and TEST-NET-1.
            192 when b[1] == 0 && (b[2] == 0 || b[2] == 2) => false,
            // 192.168/16 — private.
            192 when b[1] == 168 => false,
            // 198.18/15 — benchmarking, and 198.51.100/24 — TEST-NET-2.
            198 when b[1] == 18 || b[1] == 19 => false,
            198 when b[1] == 51 && b[2] == 100 => false,
            // 203.0.113/24 — TEST-NET-3.
            203 when b[1] == 0 && b[2] == 113 => false,
            // 224/4 multicast and 240/4 reserved, which takes 255.255.255.255.
            >= 224 => false,
            _ => true
        };
    }

    private static bool IsPublicV6(IPAddress address)
    {
        if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal) return false;
        if (address.IsIPv6Multicast) return false;
        if (address.IsIPv6UniqueLocal) return false;

        var b = address.GetAddressBytes();

        // ::/128 unspecified and ::1/128 loopback are caught above; what is
        // left to refuse by hand is 64:ff9b::/96 and ::ffff:0:0/96, the two
        // ways an IPv4 address hides inside an IPv6 one, and 2001:db8::/32,
        // the documentation range.
        if (b[0] == 0x00 && b[1] == 0x64 && b[2] == 0xff && b[3] == 0x9b) return false;
        if (b[0] == 0x20 && b[1] == 0x01 && b[2] == 0x0d && b[3] == 0xb8) return false;

        return true;
    }
}
