// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Abstraction.Renditions;

/// <summary>
/// The machine a game stands in for when it draws itself. A closed set, so a
/// config file, a folder name and a settings screen cannot disagree about what
/// exists. A game ships whichever of them it has art for.
/// </summary>
public enum Rendition
{
    /// <summary>
    /// An 8-bit home machine: small canvas, fixed-cell font, few colours.
    /// Elite's BBC Micro and Bubble Bobble's Commodore 64.
    /// </summary>
    EightBit = 0,

    /// <summary>
    /// A 16-bit machine: larger canvas, wider palette.
    /// </summary>
    SixteenBit = 1,

    /// <summary>
    /// No machine in particular - the game drawn as it would be now.
    /// </summary>
    Modern = 2,
}
