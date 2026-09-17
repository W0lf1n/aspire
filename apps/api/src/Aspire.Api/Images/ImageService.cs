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
public sealed class ImageService(AppDbContext db, MediaStore media, ImageQueue queue, long maxBoardBytes = ImageService.MaxBoardBytes)
{
    /// <summary>PLAN.md §7's cap. The client sends at most 2048 px, well under it.</summary>
    public const long MaxUploadBytes = 10 * 1024 * 1024;

    /// <summary>
    /// How full a board can be (D64). A hundred dreams at two photographs is
    /// about 150 MB, so two gigabytes is a ceiling on something other than
    /// dreaming — and the number the settings screen shows the person before
    /// the sentence ever does.
    /// </summary>
    public const long MaxBoardBytes = 2L * 1024 * 1024 * 1024;

    /// <summary>The sentence for a sixth photograph; a 409, as a full board is.</summary>
    public static readonly string TooMany = $"Sen má nejvýš {Dream.PhotosMax} fotek. Některou nejdřív smaž.";

    /// <summary>The sentence for a board at its ceiling; the endpoint answers it with a 409.</summary>
    public const string BoardFull = "Nástěnka je plná. Smaž pár fotek, které už nepotřebuješ.";

    /// <summary>
    /// What a board holds on the server: how many photographs, and what their
    /// files weigh together. One sum over the column rather than a walk of
    /// the disk, which is why the column exists (D64).
    /// </summary>
    public async Task<(int Photographs, long Bytes)> UsageOfAsync(string boardId, CancellationToken ct = default)
    {
        var rows = db.DreamImages.Where(i => db.Dreams.Any(d => d.Id == i.DreamId && d.BoardId == boardId));
        return (await rows.CountAsync(ct), await rows.SumAsync(i => i.Bytes, ct));
    }

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

        // Five dreamt photographs and no sixth (D82). Rows still being resized
        // count: they are photographs the dream is about to have.
        if (kind == DreamImageKind.Dreamt &&
            await db.DreamImages.CountAsync(i => i.DreamId == dream.Id && i.Kind == kind, ct) >= Dream.PhotosMax)
        {
            return (null, TooMany);
        }

        // The upload's own length stands in for the files it will become:
        // close enough for a ceiling two gigabytes away, and known before
        // anything is written.
        var (_, held) = await UsageOfAsync(dream.BoardId, ct);
        if (held + length > maxBoardBytes) return (null, BoardFull);

        var image = new DreamImage
        {
            Id = Guid.NewGuid(),
            DreamId = dream.Id,
            // Behind the last one, not „how many there are“: with a photograph
            // taken out of the middle the count is a number two rows already
            // have, and the tie would fall to the id — which is random (D82).
            SortOrder = (await db.DreamImages
                .Where(i => i.DreamId == dream.Id)
                .MaxAsync(i => (int?)i.SortOrder, ct) ?? -1) + 1,
            Kind = kind,
            // The crop travels with the upload, because a photograph is
            // positioned before it is sent: on the add screen there is no
            // dream to hang a second request on yet (D54).
            FocusX = focal?.FocusX ?? DreamImage.Centre,
            FocusY = focal?.FocusY ?? DreamImage.Centre,
            Zoom = focal?.Zoom ?? DreamImage.NoZoom,
            Fit = focal?.Fit ?? PhotoFit.Fill,
            Mat = focal?.Mat ?? PhotoMat.Night,
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
        Touch(dream);
        await db.SaveChangesAsync(ct);
        await queue.EnqueueAsync(image.Id, ct);
        return (image, null);
    }

    public async Task<bool> RemoveAsync(Dream dream, Guid imageId, CancellationToken ct = default)
    {
        var image = await db.DreamImages.FirstOrDefaultAsync(i => i.Id == imageId && i.DreamId == dream.Id, ct);
        if (image is null) return false;

        db.DreamImages.Remove(image);
        Touch(dream);
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
        image.Fit = input.Fit ?? image.Fit;
        image.Mat = input.Mat ?? image.Mat;
        Touch(dream);
        await db.SaveChangesAsync(ct);
        return (image, null);
    }

    /// <summary>
    /// The dreamt photographs in the order given: the first is the cover every
    /// small surface shows, the rest the collage's cells (D82).
    ///
    /// The whole order in one request, as Teď's is, so the five are never
    /// half-ordered. It has to name every dreamt photograph the dream has and
    /// nothing else — a list that leaves one out would leave it with a number
    /// somebody else now has. The achieved photograph is not in this order and
    /// is not touched.
    /// </summary>
    public async Task<string?> ReorderAsync(Dream dream, IReadOnlyList<Guid> ids, CancellationToken ct = default)
    {
        var dreamt = await db.DreamImages
            .Where(i => i.DreamId == dream.Id && i.Kind == DreamImageKind.Dreamt)
            .ToListAsync(ct);

        if (ids.Distinct().Count() != ids.Count ||
            ids.Count != dreamt.Count ||
            ids.Any(id => dreamt.All(i => i.Id != id)))
        {
            return "Tohle nejsou fotky tohohle snu.";
        }

        var byId = dreamt.ToDictionary(i => i.Id);
        for (var i = 0; i < ids.Count; i++) byId[ids[i]].SortOrder = i;

        Touch(dream);
        await db.SaveChangesAsync(ct);
        return null;
    }

    /// <summary>
    /// A photograph put on a dream, taken off it or moved is the dream being
    /// changed, and the date on its screen says so (D77). The dream is the
    /// caller's tracked row, or is attached here when it is not, so the stamp
    /// rides in the same save as the photograph.
    /// </summary>
    private void Touch(Dream dream)
    {
        dream.UpdatedAt = DateTimeOffset.UtcNow;
        if (db.Entry(dream).State == EntityState.Detached)
        {
            db.Dreams.Attach(dream).Property(d => d.UpdatedAt).IsModified = true;
        }
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

    /// <summary>
    /// The rows made before there was a column for what their files weigh,
    /// weighed now: the worker's sweep does this once, and a row it has done
    /// is never zero again. A processed photograph whose files are gone
    /// weighs nothing, which is also true.
    /// </summary>
    public async Task MeasureAsync(CancellationToken ct = default)
    {
        var unweighed = await db.DreamImages
            .Where(i => i.ProcessedAt != null && i.Bytes == 0)
            .ToListAsync(ct);
        if (unweighed.Count == 0) return;

        foreach (var image in unweighed)
        {
            image.Bytes = media.BytesOf(image.DreamId, image.Id);
        }

        await db.SaveChangesAsync(ct);
    }

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
            var made = await ImageProcessor.ProcessAsync(
                source, size => media.PathOf(image.DreamId, imageId, size), ct);

            image.Width = made.Width;
            image.Height = made.Height;
            image.Bytes = made.Bytes;
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
