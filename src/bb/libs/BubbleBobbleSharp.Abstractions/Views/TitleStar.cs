// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>One star of a front-end screen: a character cell, and which of the two star characters it is.</summary>
/// <param name="Column">The column.</param>
/// <param name="Row">The row.</param>
/// <param name="Big">Whether it is the big star ($FF) rather than the small one ($FE).</param>
public sealed record TitleStar(int Column, int Row, bool Big);
