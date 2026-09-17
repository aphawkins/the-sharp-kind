// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpKind.Assets;

// Builds asset paths as <Category>/<file> under wherever this locator was pointed. A game wanting
// both its own assets and a rendition's composes two of these.
public sealed class AssetLocator : IAssetLocator
{
    private const string AssetManifestFilename = "AssetManifest.json";
    private const string DefaultRendition = "16-bit";
    private const string ImagesCategory = "Images";

    // Every font kind (sheet, .fon, TrueType) shares one folder; only the manifest entry differs.
    private const string FontsCategory = "Fonts";
    private const string ModelsCategory = "Models";
    private const string PaletteCategory = "Palette";

    // Disallow unmapped members so a misspelled manifest section fails fast rather than as a missing asset later.
    private static readonly JsonSerializerOptions s_manifestOptions = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    private readonly AssetManifest _assetManifest = new();
    private readonly string _baseDirectory;

    internal AssetLocator(AssetManifest assetManifest, string baseDirectory, string rendition)
    {
        ArgumentNullException.ThrowIfNull(assetManifest);

        // The name becomes a directory segment, so it may not climb out of the assets folder.
        if (string.IsNullOrWhiteSpace(rendition) || rendition.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new SharpKindException($"'{rendition}' cannot be used as a folder name, so no assets could be found for it.");
        }

        _assetManifest = assetManifest;
        _baseDirectory = Path.Combine(baseDirectory, "Assets");
        Rendition = rendition;
    }

    public string Rendition { get; }

    public AssetColourLimits Colours => _assetManifest.Colours;

    public string PalettePath => CategoryPath(PaletteCategory, _assetManifest.Palette);

    public IDictionary<string, BitmapFontAsset> FontBitmaps
        => _assetManifest.Fonts.Bitmap.ToDictionary(
            x => x.Key,
            x => new BitmapFontAsset(CategoryPath(FontsCategory, x.Value.File), x.Value));

    public IDictionary<string, FonFontAsset> FontFons
        => _assetManifest.Fonts.Fon.ToDictionary(
            x => x.Key,
            x => new FonFontAsset(CategoryPath(FontsCategory, x.Value.File), x.Value.PixelHeight));

    public IDictionary<string, TrueTypeFontAsset> FontTrueTypes
        => _assetManifest.Fonts.TrueType.ToDictionary(
            x => x.Key,
            x => new TrueTypeFontAsset(CategoryPath(FontsCategory, x.Value.File), x.Value.PointSize));

    public IDictionary<string, string> ImagePaths
        => _assetManifest.Images.ToDictionary(x => x.Key, x => CategoryPath(ImagesCategory, x.Value));

    public IDictionary<string, string> MusicPaths
        => _assetManifest.Music.ToDictionary(x => x.Key, x => Path.Combine(_baseDirectory, "Music", x.Value));

    public IDictionary<string, string> SfxPaths
        => _assetManifest.Sfx.ToDictionary(x => x.Key, x => Path.Combine(_baseDirectory, "SFX", x.Value));

    public IDictionary<string, string> SoundFontPaths
        => _assetManifest.SoundFonts.ToDictionary(x => x.Key, x => Path.Combine(_baseDirectory, "SoundFonts", x.Value));

    public IDictionary<string, string> ModelPaths
        => _assetManifest.Models.ToDictionary(x => x.Key, x => CategoryPath(ModelsCategory, x.Value));

    public static AssetLocator Create() => Create(DefaultRendition);

    public static AssetLocator Create(string rendition)
        => CreateFrom(Path.GetDirectoryName(AppContext.BaseDirectory) ?? string.Empty, rendition);

    public static AssetLocator Create(Stream manifestStream, string baseDirectory)
        => Create(manifestStream, baseDirectory, DefaultRendition);

    public static AssetLocator Create(Stream manifestStream, string baseDirectory, string rendition)
    {
        ArgumentNullException.ThrowIfNull(manifestStream);

        return new(Deserialize(manifestStream), baseDirectory, rendition);
    }

    /// <summary>
    /// Reads the manifest in an Assets folder under <paramref name="baseDirectory"/>
    /// and resolves everything it names against it. A rendition's assets live
    /// beside its assembly rather than beside the executable, so the game
    /// builds one of these per place it keeps assets.
    /// </summary>
    /// <param name="baseDirectory">The directory the Assets folder sits in.</param>
    /// <param name="rendition">The name to label this set with, for messages.</param>
    /// <returns>A locator over that folder.</returns>
    public static AssetLocator CreateFrom(string baseDirectory, string rendition)
        => new(ReadManifest(Path.Combine(baseDirectory, "Assets", AssetManifestFilename)), baseDirectory, rendition);

    private static AssetManifest Deserialize(Stream manifestStream)
    {
        try
        {
            return JsonSerializer.Deserialize<AssetManifest>(manifestStream, s_manifestOptions)
                ?? throw new SharpKindException("Failed to read asset manifest from provided stream.");
        }
        catch (JsonException ex)
        {
            throw new SharpKindException("Failed to read asset manifest from provided stream.", ex);
        }
    }

    private static AssetManifest ReadManifest(string path)
    {
        try
        {
            using FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return JsonSerializer.Deserialize<AssetManifest>(stream, s_manifestOptions)
                ?? throw new SharpKindException($"Asset manifest file is empty: {path}");
        }
        catch (Exception ex) when (ex is not SharpKindException)
        {
            throw new SharpKindException($"Failed to read asset manifest file: {path}", ex);
        }
    }

    private string CategoryPath(string category, string file) => Path.Combine(_baseDirectory, category, file);
}
