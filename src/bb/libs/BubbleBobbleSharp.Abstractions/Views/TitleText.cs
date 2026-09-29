// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>One line of the title screen's text: where it starts, and the colour RAM's colour it is in.</summary>
/// <param name="Column">The first column.</param>
/// <param name="Row">The row.</param>
/// <param name="Colour">The palette entry.</param>
/// <param name="Text">The text.</param>
public sealed record TitleText(int Column, int Row, int Colour, string Text);
