// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text.Json;

namespace SharpKind.Graphics;

// A sprite sheet's index: the name a game draws by, mapped to the rectangle that name occupies in the sheet.
// Names are the game's own, so lookup is ordinal and a miss is a fault rather than an empty draw - a sprite
// asked for and silently not drawn is the harder bug of the two.
public sealed class SpriteAtlas
{
    private readonly Dictionary<string, SpriteRect> _sprites;

    public SpriteAtlas(IReadOnlyDictionary<string, SpriteRect> sprites)
    {
        ArgumentNullException.ThrowIfNull(sprites);

        _sprites = new(sprites, StringComparer.Ordinal);
    }

    public IReadOnlyCollection<string> Names => _sprites.Keys;

    public int Count => _sprites.Count;

    public static SpriteAtlas Read(string path)
    {
        Dictionary<string, SpriteEntry> entries =
            JsonSerializer.Deserialize<Dictionary<string, SpriteEntry>>(File.ReadAllText(path)) ?? [];

        return new(entries.ToDictionary(
            x => x.Key,
            x => new SpriteRect(x.Value.X, x.Value.Y, x.Value.Width, x.Value.Height),
            StringComparer.Ordinal));
    }

    // Most sheets are a plain grid of equal cells, where an index file would say nothing the dimensions do not.
    // Names run "{prefix}-{n}" in reading order, so a sheet is usable before anyone knows what its cells depict -
    // which is the state a translation is in until the routine that draws each one turns up.
    public static SpriteAtlas Grid(string prefix, int cellWidth, int cellHeight, int columns, int rows)
    {
        if (cellWidth <= 0 || cellHeight <= 0 || columns <= 0 || rows <= 0)
        {
            throw new SharpKindException(
                $"A {columns}x{rows} grid of {cellWidth}x{cellHeight} cells describes no sprites.");
        }

        Dictionary<string, SpriteRect> sprites = new(StringComparer.Ordinal);

        for (int index = 0; index < columns * rows; index++)
        {
            int column = index % columns;
            int row = index / columns;

            sprites[$"{prefix}-{index}"] = new(column * cellWidth, row * cellHeight, cellWidth, cellHeight);
        }

        return new(sprites);
    }

    public SpriteRect Sprite(string name)
        => _sprites.TryGetValue(name, out SpriteRect rect) ? rect
            : throw new SharpKindException($"The sprite atlas has no sprite named '{name}'.");

    public bool Has(string name) => _sprites.ContainsKey(name);
}
