using Aspire.Domain;
using Aspire.Infrastructure;
using Aspire.Infrastructure.Media;
using Microsoft.EntityFrameworkCore;

namespace Aspire.Api.Images;

/// <summary>
/// A dream's photographs: the upload, the resize, the removal.
///
/// An upload waits in the system's temp directory until the worker takes
/// it: never under the media root, which is served to anyone with the URL,
/// so nothing with its EXIF still on it is ever a request away. A restart
/// in between loses the staged file, and the worker's sweep drops the row
/// rather than leave it waiting.
/// </summary>
public sealed class ImageService(AppDbContext db, MediaStore media, ImageQueue queue)
{
    /// <summary>PLAN.md §7's cap. The client sends at most 2048 px, well under it.</summary>
    public const long MaxUploadBytes = 10 * 1024 * 1024;

    public Task<List<DreamImage>> OfDreamsAsync(IReadOnlyCollection<Guid> dreamIds, CancellationToken ct = default) =>
        db.DreamImages
            .Where(i => dreamIds.Contains(i.DreamId))
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Id)
            .ToListAsync(ct);

    /// <summary>
    /// Takes the upload, makes the row, and puts the id in the line. The
    /// dream is this board's; the caller checked. The problem, when there is
    /// one, is the sentence for the screen.
    /// </summary>
    public async Task<(DreamImage? Image, string? Problem)> AddAsync(
        Dream dream, Stream upload, long length, DreamImageKind kind = DreamImageKind.Dreamt,
        FocalInput? focal = null,
        CancellationToken ct = default)
    {
        if (length <= 0) return (null, "Vyber fotku.");
        if (length > MaxUploadBytes) return (null, "Fotka je moc velká, nejvýš 10 MB.");
        if (focal is not null && Problem(focal) is { } wrong) return (null, wrong);

        var image = new DreamImage
        {
            Id = Guid.NewGuid(),
            DreamId = dream.Id,
            SortOrder = await db.DreamImages.CountAsync(i => i.DreamId == dream.Id, ct),
            Kind = kind,
            // The crop travels with the upload, because a photograph is
            // positioned before it is sent: on the add screen there is no
            // dream to hang a second request on yet (D54).
            FocusX = focal?.FocusX ?? DreamImage.Centre,
            FocusY = focal?.FocusY ?? DreamImage.Centre,
            Zoom = focal?.Zoom ?? DreamImage.NoZoom,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Staged first, then looked at: the request's stream cannot be
        // rewound, and the check needs to read the header and step back.
        var staged = StagedPath(image.Id);
        await using (var file = File.Create(staged))
        {
            await upload.CopyToAsync(file, ct);
        }

        bool isImage;
        await using (var file = File.OpenRead(staged))
        {
            isImage = await ImageProcessor.IsImageAsync(file, ct);
        }

        // Closed first: Windows will not delete a file that is still open.
        if (!isImage)
        {
            File.Delete(staged);
            return (null, "Tohle není obrázek.");
        }

        db.DreamImages.Add(image);
        await db.SaveChangesAsync(ct);
        await queue.EnqueueAsync(image.Id, ct);
        return (image, null);
    }

    public async Task<bool> RemoveAsync(Dream dream, Guid imageId, CancellationToken ct = default)
    {
        var image = await db.DreamImages.FirstOrDefaultAsync(i => i.Id == imageId && i.DreamId == dream.Id, ct);
        if (image is null) return false;

        db.DreamImages.Remove(image);
        await db.SaveChangesAsync(ct);
        media.DeleteImage(dream.Id, imageId);
        File.Delete(StagedPath(imageId));
        return true;
    }

    /// <summary>
    /// Where this photograph is looked at, and how close (D54). A field left
    /// out keeps what it had, so a screen that only moved the point does not
    /// have to send the zoom back.
    ///
    /// The three files on disk are not touched: the crop is metadata, which
    /// is why changing it is instant and costs no resize — and why it is right
    /// for every shape at once rather than for the one it was cropped to.
    /// </summary>
    public async Task<(DreamImage? Image, string? Problem)> MoveAsync(
        Dream dream, Guid imageId, FocalInput input, CancellationToken ct = default)
    {
        if (Problem(input) is { } problem) return (null, problem);

        var image = await db.DreamImages.FirstOrDefaultAsync(i => i.Id == imageId && i.DreamId == dream.Id, ct);
        if (image is null) return (null, null);

        image.FocusX = input.FocusX ?? image.FocusX;
        image.FocusY = input.FocusY ?? image.FocusY;
        image.Zoom = input.Zoom ?? image.Zoom;
        await db.SaveChangesAsync(ct);
        return (image, null);
    }

    /// <summary>The sentence for a crop that is not one, or null.</summary>
    public static string? Problem(FocalInput input)
    {
        if (OutOfUnit(input.FocusX) || OutOfUnit(input.FocusY)) return "Výřez je mimo fotku.";

        if (input.Zoom is { } zoom &&
            (double.IsNaN(zoom) || zoom < DreamImage.NoZoom || zoom > DreamImage.MaxZoom))
        {
            return $"Přiblížení je mezi {DreamImage.NoZoom:0} a {DreamImage.MaxZoom:0}.";
        }

        return null;
    }

    private static bool OutOfUnit(double? value) =>
        value is { } v && (double.IsNaN(v) || v < 0 || v > 1);

    /// <summary>What a restart left behind, for the worker's sweep.</summary>
    public Task<List<Guid>> UnprocessedAsync(CancellationToken ct = default) =>
        db.DreamImages
            .Where(i => i.ProcessedAt == null)
            .OrderBy(i => i.Id)
            .Select(i => i.Id)
            .ToListAsync(ct);

    /// <summary>
    /// The worker's step: three sizes from the staged upload, then the row
    /// says so. A row whose upload is gone is dropped; a file that turns out
    /// not to be a photograph takes its row with it and throws, so the
    /// worker can say why.
    /// </summary>
    public async Task ProcessAsync(Guid imageId, CancellationToken ct = default)
    {
        var image = await db.DreamImages.FirstOrDefaultAsync(i => i.Id == imageId, ct);
        if (image is null) return;

        var staged = StagedPath(imageId);
        if (!File.Exists(staged))
        {
            db.DreamImages.Remove(image);
            await db.SaveChangesAsync(ct);
            return;
        }

        try
        {
            await using var source = File.OpenRead(staged);
            var (width, height) = await ImageProcessor.ProcessAsync(
                source, size => media.PathOf(image.DreamId, imageId, size), ct);

            image.Width = width;
            image.Height = height;
            image.ProcessedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            media.DeleteImage(image.DreamId, imageId);
            db.DreamImages.Remove(image);
            await db.SaveChangesAsync(ct);
            throw;
        }
        finally
        {
            File.Delete(staged);
        }
    }

    /// <summary>Where an upload waits: the temp directory, under a name only this process makes.</summary>
    internal static string StagedPath(Guid imageId) =>
        Path.Combine(Path.GetTempPath(), $"aspire-upload-{imageId:N}");
}
