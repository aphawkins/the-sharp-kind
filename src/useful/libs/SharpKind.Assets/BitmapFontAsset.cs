// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Assets;

// A bitmap font sheet's resolved path and its layout, as handed to the
// graphics backend.
public sealed class BitmapFontAsset
{
    public BitmapFontAsset(string path, BitmapFontEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        Path = path;
        CellWidth = entry.CellWidth;
        CellHeight = entry.CellHeight;
        Columns = entry.Columns;
        IsProportional = entry.IsProportional;
    }

    public string Path { get; }

    public int CellWidth { get; }

    public int CellHeight { get; }

    public int Columns { get; }

    public bool IsProportional { get; }
}
