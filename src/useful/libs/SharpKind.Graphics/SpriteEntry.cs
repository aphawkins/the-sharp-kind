// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Graphics;

//// JSON serializable

// One entry of an atlas index file: where a named sprite sits in the sheet.
public class SpriteEntry
{
    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }
}
