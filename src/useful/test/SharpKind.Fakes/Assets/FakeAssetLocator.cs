// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Assets;

namespace SharpKind.Fakes.Assets;

// Minimal IAssetLocator implementation for initialize benchmark.
public sealed class FakeAssetLocator : IAssetLocator
{
    public string Rendition => "16-bit";

    public AssetColourLimits Colours { get; } = new();

    // Points at the real palette shipped alongside the consuming project's output; EliteDraw reads it unconditionally and has no fake substitute.
    public string PalettePath { get; } =
        Path.Combine(AppContext.BaseDirectory, "Assets", "Palette", "palette.json");

    public IDictionary<string, BitmapFontAsset> FontBitmaps { get; } = new Dictionary<string, BitmapFontAsset>();

    public IDictionary<string, FonFontAsset> FontFons { get; } = new Dictionary<string, FonFontAsset>();

    public IDictionary<string, TrueTypeFontAsset> FontTrueTypes { get; } = new Dictionary<string, TrueTypeFontAsset>();

    public IDictionary<string, string> ImagePaths { get; } = new Dictionary<string, string>();

    public IDictionary<string, string> MusicPaths { get; } = new Dictionary<string, string>();

    public IDictionary<string, string> SfxPaths { get; } = new Dictionary<string, string>();

    public IDictionary<string, string> ModelPaths { get; set; } = new Dictionary<string, string>();

    public IDictionary<string, string> SoundFontPaths { get; } = new Dictionary<string, string>();
}
