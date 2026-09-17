// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Numerics;

namespace SharpKind.Graphics;

// Draws a named sprite from a sheet at a position, optionally mirrored. It holds no frame state - every call
// reaches the backend at once - so a view can make one per sheet and keep it.
public sealed class SpriteRenderer
{
    private readonly IGraphics _graphics;
    private readonly SpriteSheet _sheet;

    public SpriteRenderer(IGraphics graphics, SpriteSheet sheet)
    {
        ArgumentNullException.ThrowIfNull(graphics);
        ArgumentNullException.ThrowIfNull(sheet);

        _graphics = graphics;
        _sheet = sheet;
    }

    public void Draw(string name, Vector2 position) => Draw(name, position, false);

    // Sprites are drawn at their own size: an 8-bit game's pixels are the screen's, and scaling is the window's
    // job, not the sprite's. A negative source width is how DrawImagePart is told to mirror.
    public void Draw(string name, Vector2 position, bool mirrored)
    {
        SpriteRect rect = _sheet.Atlas.Sprite(name);
        Vector2 size = new(rect.Width, rect.Height);
        Vector2 sourceSize = new(mirrored ? -rect.Width : rect.Width, rect.Height);

        _graphics.DrawImagePart(_sheet.ImageType, position, size, new(rect.X, rect.Y), sourceSize);
    }
}
