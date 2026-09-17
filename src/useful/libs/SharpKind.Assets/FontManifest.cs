// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Assets;

//// JSON serializable

// Fonts grouped by kind, keyed by font type (Small/Large) matching FontKind. A kind with nothing declared is simply absent - only bitmap sheets are guaranteed.
public class FontManifest
{
    public Dictionary<string, BitmapFontEntry> Bitmap { get; init; } = [];

    public Dictionary<string, FonFontEntry> Fon { get; init; } = [];

    public Dictionary<string, TrueTypeFontEntry> TrueType { get; init; } = [];
}
