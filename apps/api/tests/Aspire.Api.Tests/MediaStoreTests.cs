using Aspire.Infrastructure.Media;
using Xunit;

namespace Aspire.Api.Tests;

/// <summary>
/// The start-up guard (D26). A media root the API cannot write into used to
/// pass the check and then cost a photograph per upload; these are the two
/// answers it has to give.
/// </summary>
public sealed class MediaStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"aspire-test-{Guid.NewGuid():N}");

    public MediaStoreTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void EnsureWritable_makes_the_root_and_leaves_nothing_in_it()
    {
        var root = Path.Combine(_dir, "media");
        new MediaStore(root).EnsureWritable();

        Assert.True(Directory.Exists(root));
        Assert.Empty(Directory.EnumerateFileSystemEntries(root));
    }

    [Fact]
    public void EnsureWritable_refuses_a_root_it_cannot_write_into()
    {
        // A file where the root's parent should be: unwritable on every
        // platform, unlike the ownership that caused this on the VPS.
        var file = Path.Combine(_dir, "not-a-directory");
        File.WriteAllBytes(file, []);

        var store = new MediaStore(Path.Combine(file, "media"));

        var thrown = Assert.Throws<InvalidOperationException>(store.EnsureWritable);
        Assert.Contains("not writable", thrown.Message);
    }
}
