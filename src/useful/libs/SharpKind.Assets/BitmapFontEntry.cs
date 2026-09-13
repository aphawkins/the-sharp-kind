// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Assets;

//// JSON serializable

// A bitmap font sheet's layout. Glyphs are laid out left to right, top to
// bottom, starting at space (ASCII 32), so a character's cell is found from
// its code alone.
public class BitmapFontEntry
{
    public string File { get; set; } = string.Empty;

    public int CellWidth { get; set; }

    public int CellHeight { get; set; }

    public int Columns { get; set; }

    // True when glyph widths vary and each glyph is terminated by a magenta
    // marker pixel; false when every glyph fills its cell.
    public bool IsProportional { get; set; }
}
