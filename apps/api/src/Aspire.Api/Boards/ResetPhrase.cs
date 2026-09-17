using System.Globalization;
using System.Text;

namespace Aspire.Api.Boards;

/// <summary>
/// The sentence that unlocks starting over (D80) — Prosper's, word for word,
/// so the one destructive thing in either app is the same thing to do.
///
/// A phrase rather than „jsi si jistý?“: a confirm is dismissed by the same
/// tap that opened it, and by the second time it is seen that tap is muscle
/// memory. Thirteen characters cannot be. The screen checks it on the
/// keystroke (<c>dreams/reset.ts</c>) and this checks it again, because the
/// request that empties a board should not be one a stray script can make
/// by knowing a URL.
/// </summary>
public static class ResetPhrase
{
    public const string Text = "začínám znovu";

    /// <summary>
    /// Case, diacritics and runs of whitespace are forgiven. The deliberateness
    /// comes from typing thirteen characters, not from finding „č“ on a phone
    /// keyboard — and a confirmation that takes three attempts is one people
    /// stop reading.
    /// </summary>
    public static bool Matches(string? typed) => Fold(typed) == Fold(Text);

    private static string Fold(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var letters = new StringBuilder();
        foreach (var c in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                letters.Append(char.ToLowerInvariant(c));
            }
        }

        return string.Join(' ', letters.ToString()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
