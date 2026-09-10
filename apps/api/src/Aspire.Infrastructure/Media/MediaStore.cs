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

    public void DeleteImage(Guid dreamId, Guid imageId) => DeleteTree(DirectoryOf(dreamId, imageId));

    public void DeleteDream(Guid dreamId) => DeleteTree(DirectoryOf(dreamId));

    private static void DeleteTree(string directory)
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }
}
