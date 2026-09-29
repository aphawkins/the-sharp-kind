// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>The title screen: the logo, drawn from characters of the charset, and the credits.</summary>
/// <param name="LogoRows">The logo's rows from the top of the screen. A character is the glyph of the logo font that its
/// code names, counted from the space; <see cref="LogoBlank"/> is a cell with nothing in it.</param>
/// <param name="LogoColour">The palette entry the logo is in.</param>
/// <param name="Lines">The text below the logo.</param>
public sealed record TitleModel(IReadOnlyList<string> LogoRows, int LogoColour, IReadOnlyList<TitleText> Lines)
{
    /// <summary>Gets the character of a logo row that has no glyph.</summary>
    public static char LogoBlank => '\0';
}
