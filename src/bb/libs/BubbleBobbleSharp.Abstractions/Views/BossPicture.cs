// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>The last level's giant: three sprites across and three down, hung from one place.</summary>
/// <param name="X">The X of the block's top left sprite, as any sprite's.</param>
/// <param name="Y">The Y of the block's top left sprite.</param>
/// <param name="Frame">The set of pictures it wears: 0 facing left, 9 stunned, <c>$18</c> facing right.</param>
/// <param name="Colour">The colour of its sprites.</param>
public sealed record BossPicture(byte X, byte Y, byte Frame, byte Colour);
