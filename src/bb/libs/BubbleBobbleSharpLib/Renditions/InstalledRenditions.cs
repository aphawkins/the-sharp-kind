// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Renditions;

namespace BubbleBobbleSharpLib.Renditions;

/// <summary>What the rendition loader found.</summary>
/// <param name="Chosen">The rendition the game will draw itself with.</param>
/// <param name="Folder">Where the chosen rendition, and so its artwork, was loaded from.</param>
/// <param name="Installed">Every rendition found, in name order.</param>
public sealed record InstalledRenditions(IBbRendition Chosen, string Folder, IReadOnlyList<IBbRendition> Installed)
{
    /// <summary>Gets every installed rendition's name, in order.</summary>
    public IReadOnlyList<string> Names => [.. Installed.Select(r => r.Name)];
}
