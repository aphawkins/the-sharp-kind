// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Assets;
using SharpKind.Graphics;

namespace SharpKind.SDL;

/// <summary>
/// Builds the set of font kinds a rendition can be drawn with. It is composed
/// here because one of the three needs SDL, and both backends are built here -
/// so both get the same set, which is what makes them draw the same text.
/// </summary>
public static class FontRasterisers
{
    /// <summary>
    /// Every kind the rendition declared something for, with the wanted one
    /// selected. The rendition's own sheets are always present, and stand in
    /// for a kind it declared nothing for.
    /// </summary>
    /// <param name="assets">The rendition's loaded assets.</param>
    /// <param name="assetLocator">Where its TrueType faces are declared.</param>
    /// <param name="kind">The kind wanted.</param>
    /// <returns>The set, ready to draw with.</returns>
    public static FontRasteriserSet Load(AssetSet assets, IAssetLocator assetLocator, FontKind kind)
    {
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(assetLocator);

        Dictionary<FontKind, IFontRasteriser> rasterisers = new()
        {
            { FontKind.Bitmap, new BitmapFontRasteriser(assets.BitmapFonts) },
        };

        // Loading a kind never declared would mean inventing a size and face for it, and neither is the engine's to choose.
        if (assets.FonFonts.Count > 0)
        {
            RequireEveryFontType(assets.BitmapFonts.Keys, assets.FonFonts.Keys, FontKind.Fon, assetLocator.Rendition);
            rasterisers[FontKind.Fon] = new FonFontRasteriser(assets.FonFonts);
        }

        if (assetLocator.FontTrueTypes.Count > 0)
        {
            RequireEveryFontType(
                assets.BitmapFonts.Keys,
                assetLocator.FontTrueTypes.Keys,
                FontKind.TrueType,
                assetLocator.Rendition);

            rasterisers[FontKind.TrueType] = TrueTypeRasteriser.Load(assetLocator.FontTrueTypes);
        }

        return new(rasterisers, kind);
    }

    // Declaring nothing for a kind is a valid choice (the rendition's sheets stand in); declaring
    // some of it is a mistake caught here rather than waiting for a screen to draw the missing type.
    internal static void RequireEveryFontType(
        IEnumerable<string> sheets,
        IEnumerable<string> declared,
        FontKind kind,
        string rendition)
    {
        string[] missing = [.. sheets.Except(declared, StringComparer.Ordinal).Order(StringComparer.Ordinal)];

        if (missing.Length > 0)
        {
            throw new SharpKindException(
                $"The {rendition} asset set declares {kind} fonts but not for {string.Join(", ", missing)}.");
        }
    }
}
