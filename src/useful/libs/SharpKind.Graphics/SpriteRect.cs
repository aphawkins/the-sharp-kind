// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Graphics;

/// <summary>
/// One sprite's place in an atlas bitmap: the source rectangle
/// <see cref="IGraphics.DrawImagePart"/> samples, in sheet pixels.
/// </summary>
/// <param name="X">The left edge of the sprite in the sheet.</param>
/// <param name="Y">The top edge of the sprite in the sheet.</param>
/// <param name="Width">The sprite's width in pixels.</param>
/// <param name="Height">The sprite's height in pixels.</param>
public readonly record struct SpriteRect(int X, int Y, int Width, int Height);
