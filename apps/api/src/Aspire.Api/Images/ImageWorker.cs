namespace Aspire.Api.Images;

/// <summary>
/// One worker, resizing in the background so an upload answers as soon as
/// the file has landed (PLAN.md §4). On start it sweeps: a row left
/// unprocessed by a restart is finished if its upload is still there and
/// dropped if it is not, so nothing waits forever for a worker that will
/// never come. The same sweep weighs the photographs made before the row
/// kept their size (D64), once, and then has nothing to weigh.
/// </summary>
public sealed class ImageWorker(
    ImageQueue queue,
    IServiceScopeFactory scopes,
    ILogger<ImageWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await SweepAsync(stoppingToken);

        await foreach (var imageId in queue.ReadAllAsync(stoppingToken))
        {
            await ProcessOneAsync(imageId, stoppingToken);
        }
    }

    private async Task SweepAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var images = scope.ServiceProvider.GetRequiredService<ImageService>();
        foreach (var imageId in await images.UnprocessedAsync(ct))
        {
            await queue.EnqueueAsync(imageId, ct);
        }

        await images.MeasureAsync(ct);
    }

    private async Task ProcessOneAsync(Guid imageId, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var images = scope.ServiceProvider.GetRequiredService<ImageService>();
        try
        {
            await images.ProcessAsync(imageId, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            // The service has already dropped the row and its files; the
            // person sees the sky where the photograph was and tries again.
            log.LogError(e, "Image {ImageId} could not be processed", imageId);
        }
    }
}
