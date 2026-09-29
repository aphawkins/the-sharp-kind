// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>The banner above the C64's screen, which is house chrome the reference does not have.</summary>
/// <param name="Height">The banner's height in pixels; none if the screen has no room for it.</param>
public sealed record BannerModel(float Height);
