using System.Threading.Channels;

namespace Aspire.Api.Images;

/// <summary>
/// The line at the resizer: image ids, in order, one reader (PLAN.md §4).
/// Unbounded, because a phone uploads one photograph at a time and the
/// worker is faster than the network that feeds it.
/// </summary>
public sealed class ImageQueue
{
    private readonly Channel<Guid> _channel =
        Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = true });

    public ValueTask EnqueueAsync(Guid imageId, CancellationToken ct = default) =>
        _channel.Writer.WriteAsync(imageId, ct);

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
