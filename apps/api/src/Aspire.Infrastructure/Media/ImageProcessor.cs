using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Aspire.Infrastructure.Media;

/// <summary>
/// A photograph in, three WebP files out (PLAN.md §4). Turned the way the
/// phone meant it, then stripped of everything the phone wrote into it —
/// where it was taken, on what, when — because the files are served to
/// anyone who has the URL, and a dream's URL is not a secret worth keeping.
/// Never scaled up: a small picture stays its size in every file.
/// </summary>
public static class ImageProcessor
{
    /// <summary>
    /// The longest edge per size. Screen is what a phone's board shows at
    /// full width; full is the archive, and the most the client sends.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, int> LongestEdge = new Dictionary<string, int>
    {
        ["thumb"] = 400,
        ["screen"] = 1280,
        ["full"] = 2048
    };

    private const int Quality = 82;

    /// <summary>Whether ImageSharp can read it at all: the upload's gate.</summary>
    public static async Task<bool> IsImageAsync(Stream source, CancellationToken ct = default)
    {
        try
        {
            await Image.IdentifyAsync(source, ct);
            return true;
        }
        catch (UnknownImageFormatException)
        {
            return false;
        }
        catch (InvalidImageContentException)
        {
            return false;
        }
        finally
        {
            if (source.CanSeek) source.Position = 0;
        }
    }

    /// <param name="pathFor">Where each size goes, by its name.</param>
    /// <returns>The full size's width and height.</returns>
    public static async Task<(int Width, int Height)> ProcessAsync(
        Stream source,
        Func<string, string> pathFor,
        CancellationToken ct = default)
    {
        using var image = await Image.LoadAsync(source, ct);
        image.Mutate(x => x.AutoOrient());
        image.Metadata.ExifProfile = null;
        image.Metadata.XmpProfile = null;
        image.Metadata.IptcProfile = null;

        var encoder = new WebpEncoder { Quality = Quality };
        var full = (image.Width, image.Height);

        foreach (var (size, edge) in LongestEdge)
        {
            var path = pathFor(size);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var scale = Math.Min(1.0, (double)edge / Math.Max(image.Width, image.Height));
            if (scale < 1.0)
            {
                var width = Math.Max(1, (int)Math.Round(image.Width * scale));
                var height = Math.Max(1, (int)Math.Round(image.Height * scale));
                using var copy = image.Clone(x => x.Resize(width, height));
                await copy.SaveAsync(path, encoder, ct);
                if (size == "full") full = (copy.Width, copy.Height);
            }
            else
            {
                await image.SaveAsync(path, encoder, ct);
            }
        }

        return full;
    }
}
