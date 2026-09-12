using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Aspire.Infrastructure.Media;

/// <summary>What the worker made: the full size's dimensions, and what the three files weigh together.</summary>
public readonly record struct ProcessedImage(int Width, int Height, long Bytes);

/// <summary>
/// A photograph in, three WebP files out (PLAN.md §4). Turned the way the
/// phone meant it, then stripped of everything the phone wrote into it —
/// where it was taken, on what, when — because the files are served to
/// anyone who has the URL, and a dream's URL is not a secret worth keeping.
/// Never scaled up: a small picture stays its size in every file.
///
/// The encoder is tuned once, here, and every file written after it is
/// smaller for the same picture (D23, amended by PLAN.md §24.4): quality
/// 75 rather than 82, which on a photograph at phone density is a quarter
/// of the bytes for no difference anyone can see; libwebp's slowest method,
/// which is a few per cent smaller again for seconds a background worker
/// has to spare; and Lanczos with a light sharpen after a downscale, which
/// is what makes 1280 look like a photograph rather than a soft copy of one.
/// Files already on disk are what they were — the URLs are immutable and
/// re-encoding what is already served buys nothing.
/// </summary>
public static class ImageProcessor
{
    /// <summary>
    /// The longest edge per size. Screen is what a phone's board shows at
    /// full width; full is the archive, the most the client sends, and the
    /// rung a 2× or 3× phone reads on the reel (D62).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, int> LongestEdge = new Dictionary<string, int>
    {
        ["thumb"] = 400,
        ["screen"] = 1280,
        ["full"] = 2048
    };

    /// <summary>
    /// The thumb is 40 px in a circle and a blur under the reel's picture; it
    /// can afford to be rougher than the two that are looked at.
    /// </summary>
    public static int QualityOf(string size) => size == "thumb" ? 70 : 75;

    /// <summary>
    /// Enough to put the edge back that a resample takes off, not enough to
    /// be seen as sharpening. Only after a downscale: a picture saved at its
    /// own size has lost nothing to put back.
    /// </summary>
    private const float SharpenSigma = 0.5f;

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

    /// <summary>
    /// One photograph, upright, stripped of its metadata and no longer than
    /// <paramref name="longestEdge"/>, written as JPEG (D56).
    ///
    /// What the link fetcher hands back: the client then treats it exactly
    /// like a file somebody picked, and its own downscale finds nothing left
    /// to do. JPEG rather than WebP because it is what a picked file is, and
    /// the client's path from there has one shape.
    /// </summary>
    public static async Task DownscaleAsync(
        Stream source,
        Stream destination,
        int longestEdge,
        CancellationToken ct = default)
    {
        using var image = await Image.LoadAsync(source, ct);
        image.Mutate(x => x.AutoOrient());
        // The same stripping an upload gets: a picture from somebody else's
        // site carries somebody else's metadata, and it is served from ours.
        image.Metadata.ExifProfile = null;
        image.Metadata.XmpProfile = null;
        image.Metadata.IptcProfile = null;

        var scale = Math.Min(1.0, (double)longestEdge / Math.Max(image.Width, image.Height));
        if (scale < 1.0)
        {
            image.Mutate(x => x.Resize(
                Math.Max(1, (int)Math.Round(image.Width * scale)),
                Math.Max(1, (int)Math.Round(image.Height * scale))));
        }

        await image.SaveAsync(destination, new JpegEncoder { Quality = 86 }, ct);
    }

    /// <param name="pathFor">Where each size goes, by its name.</param>
    public static async Task<ProcessedImage> ProcessAsync(
        Stream source,
        Func<string, string> pathFor,
        CancellationToken ct = default)
    {
        using var image = await Image.LoadAsync(source, ct);
        image.Mutate(x => x.AutoOrient());
        image.Metadata.ExifProfile = null;
        image.Metadata.XmpProfile = null;
        image.Metadata.IptcProfile = null;

        var full = (image.Width, image.Height);
        long bytes = 0;

        foreach (var (size, edge) in LongestEdge)
        {
            var path = pathFor(size);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var encoder = new WebpEncoder { Quality = QualityOf(size), Method = WebpEncodingMethod.BestQuality };

            var scale = Math.Min(1.0, (double)edge / Math.Max(image.Width, image.Height));
            if (scale < 1.0)
            {
                var width = Math.Max(1, (int)Math.Round(image.Width * scale));
                var height = Math.Max(1, (int)Math.Round(image.Height * scale));
                using var copy = image.Clone(x => x
                    .Resize(width, height, KnownResamplers.Lanczos3)
                    .GaussianSharpen(SharpenSigma));
                await copy.SaveAsync(path, encoder, ct);
                if (size == "full") full = (copy.Width, copy.Height);
            }
            else
            {
                await image.SaveAsync(path, encoder, ct);
            }

            bytes += new FileInfo(path).Length;
        }

        return new ProcessedImage(full.Width, full.Height, bytes);
    }
}
