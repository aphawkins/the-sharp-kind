// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Assets;

//// JSON serializable

// A bitmap font sheet's layout. Glyphs run left to right, top to bottom, starting at character 0, so a sixteen-column sheet reads off the hex code: column code & 0xF, row code >> 4.
public class BitmapFontEntry
{
    public string File { get; set; } = string.Empty;

    public int CellWidth { get; set; }

    public int CellHeight { get; set; }

    public int Columns { get; set; }

    // How many times wider than the sheet each glyph of a grid font is drawn, for art authored at half
    // width or less (the C64's multicolour pixels are two hires pixels wide). Left at 1, glyphs draw as they are.
    public int WidthScale { get; set; } = 1;

    // True when glyph widths vary and each glyph is terminated by a magenta
    // marker pixel; false when every glyph fills its cell.
    public bool IsProportional { get; set; }
}
