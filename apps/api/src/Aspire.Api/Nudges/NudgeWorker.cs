using Aspire.Api.Dreams;
using Aspire.Api.Images;
using Aspire.Domain;
using Aspire.Infrastructure.Push;

namespace Aspire.Api.Nudges;

/// <summary>
/// The nudge (PLAN.md §3.6): one dream, to every device that asked to be
/// told, at each of the times it asked for — up to five a day (D72).
///
/// It wakes every minute and asks the schedule who is owed one — cheap,
/// because that is one query over a table with as many rows as there are
/// phones, and the arithmetic is a pure rule with a test (D34). A minute is
/// also the resolution the person chose the time at, so nothing is ever
/// more than a minute late.
/// </summary>
public sealed class NudgeWorker(
    IServiceScopeFactory scopes,
    VapidKeys keys,
    WebPushSender sender,
    ILogger<NudgeWorker> log) : BackgroundService
{
    private static readonly TimeSpan Tick = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stopping)
    {
        if (!keys.Configured)
        {
            // A laptop, or a VPS nobody has given a key pair yet. Saying so
            // once at start is better than a line a minute saying nothing.
            log.LogInformation("Push notifications are off: no VAPID key pair is configured.");
            return;
        }

        using var timer = new PeriodicTimer(Tick);
        while (!stopping.IsCancellationRequested)
        {
            try
            {
                await SendDueAsync(DateTimeOffset.UtcNow, stopping);
            }
            catch (Exception e) when (!stopping.IsCancellationRequested)
            {
                // One bad minute must not stop every morning after it.
                log.LogError(e, "The nudge round failed.");
            }

            try
            {
                await timer.WaitForNextTickAsync(stopping);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>
    /// One round. Every device that is due gets the dream its board would open
    /// on today — the same daily pick the reel uses, so the notification and
    /// the board agree about what today's dream is — except on the one morning
    /// a year a dream has an anniversary, which is the news instead (D57).
    /// </summary>
    internal async Task SendDueAsync(DateTimeOffset utcNow, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var nudges = scope.ServiceProvider.GetRequiredService<NudgeService>();
        var dreams = scope.ServiceProvider.GetRequiredService<DreamService>();
        var images = scope.ServiceProvider.GetRequiredService<ImageService>();

        var due = await nudges.DueAsync(utcNow, ct);
        if (due.Count == 0) return;

        // One board's dreams are read once however many of its devices are
        // due, because a household is several phones and one board — and one
        // board says one thing a morning, to every phone on it.
        var boards = new Dictionary<string, Morning>();

        foreach (var (subscription, atMinutes) in due)
        {
            if (ct.IsCancellationRequested) return;

            if (!boards.TryGetValue(subscription.BoardId, out var morning))
            {
                var board = await dreams.ListAsync(subscription.BoardId, ct);
                // The anniversary first, because it replaces rather than
                // joins: one nudge a morning, and a dream that came true a
                // year ago today outranks the dream it is somebody's turn to
                // see (D57).
                morning = Anniversary.Today(board, utcNow, subscription.UtcOffsetMinutes) is { } year
                    ? new Morning(year.Dream, year.Years)
                    : new Morning(DailyPick.From(board, utcNow, subscription.UtcOffsetMinutes), 0);
                boards[subscription.BoardId] = morning;
            }

            var dream = morning.Dream;

            // A board with nothing left to dream about has nothing to say at
            // seven in the morning. The stamp still goes on, so it is not
            // asked again every minute until the grace window closes.
            if (dream is null)
            {
                await nudges.MarkSentAsync(subscription, atMinutes, utcNow, ct);
                continue;
            }

            var photographs = await images.OfDreamsAsync([dream.Id], ct);

            var result = await sender.SendAsync(
                subscription.Endpoint,
                subscription.P256dh,
                subscription.Auth,
                morning.IsAnniversary
                    ? NudgeMessage.ForAnniversary(dream, morning.Years, photographs)
                    : NudgeMessage.For(dream, photographs),
                keys.PublicKey,
                keys.PrivateKey,
                keys.Subject,
                ct);

            switch (result)
            {
                case PushResult.Sent:
                    // Shown, in the sense that matters: it was put in front
                    // of the person. The board opens on the same dream when
                    // they tap it, and tomorrow's nudge is a different one.
                    //
                    // An anniversary stamps nothing: the dream is achieved, so
                    // it is not on the reel and the pick cannot reach it, and
                    // „shown“ is a word about the board's turn (D57). The
                    // board still opens on its own pick, untouched.
                    if (!morning.IsAnniversary)
                    {
                        await dreams.MarkShownAsync(subscription.BoardId, dream.Id, ct);
                    }

                    await nudges.MarkSentAsync(subscription, atMinutes, utcNow, ct);
                    // One line per nudge, with the clock the person reads: a
                    // send used to leave no trace at all, so a notification
                    // that arrived late could not be told from one that was
                    // sent late (D51). The endpoint is left out — it is the
                    // capability that can push to the phone.
                    log.LogInformation(
                        "Nudged board {Board} with dream {Dream} at {Local:HH:mm} local{Years}.",
                        subscription.BoardId,
                        dream.Id,
                        NudgeSchedule.LocalNow(utcNow, subscription.UtcOffsetMinutes),
                        morning.IsAnniversary ? $" — its {morning.Years}-year anniversary" : string.Empty);
                    break;

                case PushResult.Gone:
                    // The browser dropped it. So do we (D34).
                    log.LogInformation("A push subscription was gone; forgetting it.");
                    await nudges.ForgetAsync(subscription.Id, ct);
                    break;

                default:
                    // Left unstamped on purpose: within the grace window the
                    // next minute tries again, and after it the day is skipped.
                    log.LogWarning("A nudge could not be delivered; it will be tried again.");
                    break;
            }
        }
    }

    /// <summary>
    /// What a board has to say this morning: the dream, and how many years ago
    /// it came true when that is the reason it is being said at all.
    /// </summary>
    private readonly record struct Morning(Dream? Dream, int Years)
    {
        public bool IsAnniversary => Years > 0;
    }
}
