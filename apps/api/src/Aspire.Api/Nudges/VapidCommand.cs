using Aspire.Infrastructure.Push;

namespace Aspire.Api.Nudges;

/// <summary>
/// <c>vapid</c>, run in the API's own image, beside <c>board</c>. It prints
/// a fresh key pair for the operator to paste into the environment.
///
/// <code>
/// docker compose run --rm -T api vapid
/// </code>
///
/// A pair is generated once and then never again: the public half is baked
/// into every browser subscription the server has ever handed out, so a new
/// one silently orphans all of them (D34). The command says so, because the
/// person running it a second time will not have read this.
/// </summary>
public static class VapidCommand
{
    public static bool IsVapidCommand(string[] args) => args is ["vapid"];

    public static Task<int> RunAsync(TextWriter output, string? existingPublicKey = null)
    {
        var (publicKey, privateKey) = WebPushCrypto.NewVapidKeys();

        if (!string.IsNullOrWhiteSpace(existingPublicKey))
        {
            output.WriteLine("# WARNING: this server already has a VAPID key pair.");
            output.WriteLine("# Replacing it silently breaks every notification anybody has");
            output.WriteLine("# already subscribed to; they will have to turn them on again.");
            output.WriteLine();
        }

        output.WriteLine("# A new VAPID key pair. Put these in the environment and restart the API.");
        output.WriteLine("# Subject is how a push service reaches you about a problem.");
        output.WriteLine();
        output.WriteLine($"Push__PublicKey={publicKey}");
        output.WriteLine($"Push__PrivateKey={privateKey}");
        output.WriteLine("Push__Subject=mailto:you@example.com");
        return Task.FromResult(0);
    }
}
