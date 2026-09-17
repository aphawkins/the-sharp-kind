// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Assets;

//// JSON serializable

// A Windows .fon font's declaration. A .fon may hold several strikes of the same face, so the size wanted is named here.
public class FonFontEntry
{
    public string File { get; set; } = string.Empty;

    // Nearest strike the file holds is used.
    public int PixelHeight { get; set; }
}
