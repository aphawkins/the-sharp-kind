// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>A front-end screen's characters: its stars, its rules and its text. The banner is the logo.</summary>
/// <param name="Lines">The credits and the prompt.</param>
public sealed record TitleModel(IReadOnlyList<TitleText> Lines)
{
    /// <summary>Gets the stars behind the text.</summary>
    public IReadOnlyList<TitleStar> Stars { get; init; } = [];

    /// <summary>Gets the rules across the screen.</summary>
    public IReadOnlyList<TitleRule> Rules { get; init; } = [];
}
