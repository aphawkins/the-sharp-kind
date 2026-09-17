// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

namespace EliteSharp.Abstractions.Views;

/// <summary>
/// Breaks text into rows of at most a given number of characters. Splitting
/// only, no drawing: the tiers' <see cref="IBaseView.DrawTextPretty"/> draw
/// the rows left-aligned, and the 8-bit options screen centres the same rows
/// and stacks them upwards from the bottom of the viewport, so a helper that
/// drew as it wrapped could serve neither of them.
/// </summary>
public static class TextWrap
{
    /// <summary>
    /// Splits <paramref name="text"/> into rows of at most
    /// <paramref name="maxChars"/> characters, breaking on a space, comma or
    /// period where there is one and mid-word where there is not.
    /// </summary>
    public static IReadOnlyList<string> Split(string text, int maxChars)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxChars, 1);

        List<string> lines = [];
        int start = 0;

        while (start < text.Length)
        {
            // What is left already fits, so there is nothing to break.
            if (text.Length - start <= maxChars)
            {
                lines.Add(text[start..]);
                break;
            }

            // Scanned backwards from the last character that fits - starting one further on can run a row past its width.
            int i = start + maxChars - 1;

            while (i > start && text[i] is not ' ' and not ',' and not '.')
            {
                i--;
            }

            if (i == start)
            {
                // Word is longer than the row is wide - break it mid-word.
                lines.Add(text.Substring(start, maxChars));
                start += maxChars;
                continue;
            }

            // A comma/period stays on the row; a space is dropped, or it would push a centred row half a character off.
            lines.Add(text[start..(text[i] == ' ' ? i : i + 1)]);
            start = i + 1;
        }

        return lines;
    }
}
