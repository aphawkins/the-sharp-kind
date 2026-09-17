// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using SharpKind.Abstraction.Renditions;

namespace BubbleBobbleSharp.Abstractions.Renditions;

/// <summary>
/// Everything one rendition of the game draws, written in its own assembly
/// and found at startup - the same door Elite's renditions come through. A
/// whole presentation is then an assembly rather than a branch in the game's
/// composition root.
/// <para>
/// One rendition ships today, the C64 the port is translated from. The shape
/// is here so a second - an arcade or Amiga presentation - is an assembly
/// rather than a rewrite.
/// </para>
/// </summary>
public interface IBbRendition
{
    /// <summary>
    /// Gets which machine this rendition stands in for. One of a closed set
    /// the engine defines, not a name the rendition chooses.
    /// </summary>
    public Rendition Rendition { get; }

    /// <summary>
    /// Gets the name this rendition is known by - in the config file, and in
    /// the folder its assets sit in. Derived from <see cref="Rendition"/>
    /// rather than declared, so the two cannot disagree.
    /// </summary>
    public string Name => RenditionNames.Of(Rendition);

    /// <summary>
    /// Gets the width in pixels this rendition draws at. The game renders at
    /// this size and the window magnifies it, so a rendition picks its own
    /// resolution rather than being handed one.
    /// </summary>
    public int ScreenWidth { get; }

    /// <summary>
    /// Gets the height in pixels this rendition draws at.
    /// </summary>
    public int ScreenHeight { get; }

    /// <summary>
    /// Gets the window scales this rendition offers, smallest first. A scale
    /// magnifies the rendered pixels at presentation, so what is sensible
    /// depends on how big this rendition already draws.
    /// </summary>
    public IReadOnlyList<int> WindowScales => [1];

    /// <summary>
    /// Gets the window scale a player who has never chosen one gets. Stated
    /// rather than taken as the largest of <see cref="WindowScales"/>: which
    /// window a rendition wants to open at is its own choice, not a
    /// consequence of what it will tolerate.
    /// </summary>
    public int DefaultWindowScale => 1;
}
