namespace Aspire.Infrastructure.Media;

/// <summary>
/// Where the photographs are. One directory per dream, one per image inside
/// it, three WebP files inside that (D7, D23). nginx serves the tree as
/// <c>/media/</c> in production; the API serves it on a laptop. Nothing else
/// is ever written under the root, so deleting a dream's directory is
/// deleting its photographs.
/// </summary>
public sealed class MediaStore(string root)
{
    public static readonly string[] Sizes = ["thumb", "screen", "full"];

    public string Root { get; } = Path.GetFullPath(root);

    public string DirectoryOf(Guid dreamId) => Path.Combine(Root, dreamId.ToString("D"));

    public string DirectoryOf(Guid dreamId, Guid imageId) =>
        Path.Combine(DirectoryOf(dreamId), imageId.ToString("D"));

    public string PathOf(Guid dreamId, Guid imageId, string size) =>
        Path.Combine(DirectoryOf(dreamId, imageId), $"{size}.webp");

    /// <summary>The URL nginx answers in production, and the API on a laptop.</summary>
    public static string UrlOf(Guid dreamId, Guid imageId, string size) =>
        $"/media/{dreamId:D}/{imageId:D}/{size}.webp";

    /// <summary>
    /// The root is there and this process can write into it — checked once,
    /// at start, rather than the first time somebody uploads a photograph.
    ///
    /// <c>Directory.CreateDirectory</c> is not that check. On a root that
    /// already exists it succeeds without touching the disk, so a volume that
    /// belongs to somebody else passes it and then throws in the worker, where
    /// the failure costs the row: the upload answers 202, the photograph never
    /// resizes, and the board shows the sky as if nothing had been sent (D26).
    /// A file written and deleted is the check that cannot pass by accident.
    /// </summary>
    public void EnsureWritable()
    {
        var probe = Path.Combine(Root, $".writable-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(Root);
            File.WriteAllBytes(probe, []);
            File.Delete(probe);
        }
        catch (Exception e) when (e is UnauthorizedAccessException or IOException)
        {
            throw new InvalidOperationException(
                $"The media root '{Root}' is not writable by '{Environment.UserName}'. " +
                "Every photograph would upload and then disappear, so the server stops here " +
                "instead. See docs/DEPLOYMENT.md, \"A photograph uploads and then disappears\".",
                e);
        }
    }

    public void DeleteImage(Guid dreamId, Guid imageId) => DeleteTree(DirectoryOf(dreamId, imageId));

    public void DeleteDream(Guid dreamId) => DeleteTree(DirectoryOf(dreamId));

    private static void DeleteTree(string directory)
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }
}
