// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Abstraction.Renditions;

/// <summary>
/// The machine a game stands in for when it draws itself. Every game in the
/// repo offers the same three, so they are the engine's rather than any one
/// game's, and a game ships whichever of them it has art for.
/// <para>
/// A closed set rather than a name each rendition chooses. It is what makes a
/// rendition a comparable thing across games - Elite's 8-bit tier and Bubble
/// Bobble's C64 are the same answer to the same question - and it means a
/// config file, a folder name and a settings screen cannot disagree about
/// what exists.
/// </para>
/// </summary>
public enum Rendition
{
    /// <summary>
    /// An 8-bit home machine: a small canvas, a fixed-cell font and a handful
    /// of colours. Elite's BBC Micro tier and Bubble Bobble's Commodore 64.
    /// </summary>
    EightBit = 0,

    /// <summary>
    /// A 16-bit machine: a larger canvas and a wider palette.
    /// </summary>
    SixteenBit = 1,

    /// <summary>
    /// Neither, and no machine in particular: what the game would look like
    /// drawn now, without standing in for anything.
    /// </summary>
    Modern = 2,
}
