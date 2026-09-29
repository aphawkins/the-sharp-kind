// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Levels;

/// <summary>One item of a bonus room: where a player has to reach for it, and the level cell of its top-left character.</summary>
/// <param name="X">The item's X, in sprite pixels (<c>$4600</c>).</param>
/// <param name="Y">The item's Y, in sprite pixels (<c>$4700</c>).</param>
/// <param name="Column">The level column of its top-left character.</param>
/// <param name="Row">The level row of its top-left character.</param>
internal readonly record struct RoomItem(byte X, byte Y, int Column, int Row);
