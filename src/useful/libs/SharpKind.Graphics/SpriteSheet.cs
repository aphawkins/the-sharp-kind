// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Globalization;
using System.Numerics;

namespace SharpKind.Graphics;

// An atlas bitmap and the index into it, checked against each other once at load. The bitmap itself is loaded
// and held by the graphics backend under an image name, as every other image is, so this pairs that name with
// the index rather than decoding a second copy.
public sealed class SpriteSheet
{
    public SpriteSheet(string imageType, SpriteAtlas atlas, Vector2 sheetSize)
    {
        ArgumentNullException.ThrowIfNull(atlas);

        RequireInside(atlas, sheetSize);

        ImageType = imageType;
        Atlas = atlas;
        SheetSize = sheetSize;
    }

    public string ImageType { get; }

    public SpriteAtlas Atlas { get; }

    public Vector2 SheetSize { get; }

    // The graphics backend knows the sheet's size, since it holds the bitmap; asking it keeps the size the
    // rectangles are checked against the size they will be sampled from.
    public static SpriteSheet Load(IGraphics graphics, string imageType, string atlasPath)
    {
        ArgumentNullException.ThrowIfNull(graphics);

        return new(imageType, SpriteAtlas.Read(atlasPath), graphics.ImageSize(imageType));
    }

    // A rectangle reaching past the sheet samples whatever the decoder left beyond it, which is a wrong sprite
    // rather than a missing one. Every offender is reported at once, to avoid fixing a packed atlas one entry
    // at a time.
    private static void RequireInside(SpriteAtlas atlas, Vector2 sheetSize)
    {
        int width = (int)sheetSize.X;
        int height = (int)sheetSize.Y;

        string[] outside =
        [
            .. atlas.Names
                .Where(x => IsOutside(atlas.Sprite(x), width, height))
                .Order(StringComparer.Ordinal)
                .Select(x => $"{x} {Describe(atlas.Sprite(x))}"),
        ];

        if (outside.Length > 0)
        {
            throw new SharpKindException(
                $"{outside.Length} sprite(s) fall outside the {width}x{height} sheet: {string.Join(", ", outside)}.");
        }
    }

    private static bool IsOutside(in SpriteRect rect, int width, int height)
        => rect.Width <= 0
            || rect.Height <= 0
            || rect.X < 0
            || rect.Y < 0
            || rect.X + rect.Width > width
            || rect.Y + rect.Height > height;

    private static string Describe(in SpriteRect rect) => string.Create(
        CultureInfo.InvariantCulture,
        $"({rect.X},{rect.Y} {rect.Width}x{rect.Height})");
}
