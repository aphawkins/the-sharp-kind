// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>A rule across a front-end screen: a row of the double-line character ($1E).</summary>
/// <param name="Column">The first column.</param>
/// <param name="Row">The row.</param>
/// <param name="Length">How many characters long it is.</param>
public sealed record TitleRule(int Column, int Row, int Length);
