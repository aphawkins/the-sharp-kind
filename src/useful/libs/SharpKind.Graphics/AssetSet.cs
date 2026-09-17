// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Globalization;
using Microsoft.Extensions.Logging;
using SharpKind.Assets;
using SharpKind.Assets.Palettes;

namespace SharpKind.Graphics;

// Every image and bitmap font for the active tier, decoded once up front so both backends share one place the
// colour budget is checked. Sheets are always decoded regardless of font kind selected, since every other kind
// falls back to them.
public sealed class AssetSet
{
    private AssetSet(
        Dictionary<string, FastBitmap> images,
        Dictionary<string, BitmapFont> bitmapFonts,
        Dictionary<string, FonFont> fonFonts,
        AssetColourBudget budget)
    {
        Images = images;
        BitmapFonts = bitmapFonts;
        FonFonts = fonFonts;
        Budget = budget;
    }

    public Dictionary<string, FastBitmap> Images { get; }

    public Dictionary<string, BitmapFont> BitmapFonts { get; }

    // .fon strikes the rendition declares, if any. Unlike sheets, a strike is 1 bit per pixel and takes the drawn colour, so it contributes nothing to the budget below.
    public Dictionary<string, FonFont> FonFonts { get; }

    public AssetColourBudget Budget { get; }

    public static AssetSet Load(IAssetLocator assetLocator) => Load(assetLocator, null);

    public static AssetSet Load(IAssetLocator assetLocator, ILogger? logger)
    {
        ArgumentNullException.ThrowIfNull(assetLocator);

        RequireEveryFile(assetLocator);

        Dictionary<string, FastBitmap> images = assetLocator.ImagePaths.ToDictionary(
            x => x.Key,
            x => ImageReader.Read(x.Value));

        Dictionary<string, FastBitmap> fontBitmaps = assetLocator.FontBitmaps.ToDictionary(
            x => x.Key,
            x => ImageReader.Read(x.Value.Path));

        Dictionary<string, FonFont> fonFonts = assetLocator.FontFons.ToDictionary(
            x => x.Key,
            x => FonFontReader.Read(x.Value.Path, x.Value.PixelHeight));

        AssetColourBudget budget = Measure(assetLocator, images, fontBitmaps, PaletteColours(assetLocator));
        Validate(budget, logger);

        return new(
            images,
            fontBitmaps.ToDictionary(x => x.Key, x => new BitmapFont(x.Value, assetLocator.FontBitmaps[x.Key])),
            fonFonts,
            budget);
    }

    // Reports every missing file at once rather than one at a time, to avoid whack-a-mole when filling in a new rendition's asset set.
    private static void RequireEveryFile(IAssetLocator assetLocator)
    {
        string[] missing =
        [
            .. assetLocator.ImagePaths
                .Concat(assetLocator.FontBitmaps.ToDictionary(x => x.Key, x => x.Value.Path))
                .Concat(assetLocator.FontFons.ToDictionary(x => x.Key, x => x.Value.Path))
                .Where(x => !File.Exists(x.Value))
                .Select(x => $"{x.Key} ({Path.GetFileName(x.Value)})")
                .Order(),
        ];

        if (missing.Length > 0)
        {
            throw new SharpKindException(
                $"The {assetLocator.Rendition} asset set is missing {missing.Length} file(s): {string.Join(", ", missing)}.");
        }
    }

    // The named palette counts against the budget like any other asset - colours drawn with have to be showable whether they arrive as pixels or a name.
    private static HashSet<uint> PaletteColours(IAssetLocator assetLocator)
        => string.IsNullOrEmpty(assetLocator.PalettePath) ? []
            : !File.Exists(assetLocator.PalettePath)
            ? throw new SharpKindException(
                $"The {assetLocator.Rendition} asset set is missing its palette: {assetLocator.PalettePath}")
            : [.. PaletteReader.Read(assetLocator.PalettePath).Values.Select(x => x.Argb)];

    private static AssetColourBudget Measure(
        IAssetLocator assetLocator,
        Dictionary<string, FastBitmap> images,
        Dictionary<string, FastBitmap> fontBitmaps,
        HashSet<uint> paletteColours)
    {
        HashSet<uint> distinct = [.. paletteColours];
        Dictionary<string, int> perAsset = new() { ["Palette"] = paletteColours.Count };
        Dictionary<string, uint[]> outsidePalette = [];
        Dictionary<string, uint[]> offGrid = [];
        int channelBits = assetLocator.Colours.ChannelBits;
        int partialAlpha = 0;

        AddOffGrid(offGrid, "Palette", paletteColours, channelBits);

        foreach (KeyValuePair<string, FastBitmap> asset in images.Concat(fontBitmaps))
        {
            (HashSet<uint> assetColours, int assetPartialAlpha) = Scan(asset.Value);

            perAsset[asset.Key] = assetColours.Count;
            distinct.UnionWith(assetColours);
            partialAlpha += assetPartialAlpha;

            AddOffGrid(offGrid, asset.Key, assetColours, channelBits);

            // A set with no palette at all has nothing to be a subset of, so
            // there is nothing to report rather than everything.
            if (paletteColours.Count == 0)
            {
                continue;
            }

            uint[] unnamed = [.. assetColours.Where(x => !paletteColours.Contains(x)).Order()];

            if (unnamed.Length > 0)
            {
                outsidePalette[asset.Key] = unnamed;
            }
        }

        return new(assetLocator.Rendition, assetLocator.Colours, distinct.Count, partialAlpha, perAsset, outsidePalette, offGrid);
    }

    // Colours the tier's DAC couldn't produce, recorded (not just counted) since fixing one needs knowing which colour to snap.
    private static void AddOffGrid(
        Dictionary<string, uint[]> offGrid,
        string asset,
        HashSet<uint> colours,
        int channelBits)
    {
        uint[] strays = [.. colours.Where(x => !AssetColourBudget.IsOnGrid(x, channelBits)).Order()];

        if (strays.Length > 0)
        {
            offGrid[asset] = strays;
        }
    }

    private static (HashSet<uint> Colours, int PartialAlpha) Scan(FastBitmap asset)
    {
        HashSet<uint> colours = [];
        int partialAlpha = 0;

        for (int y = 0; y < asset.Height; y++)
        {
            for (int x = 0; x < asset.Width; x++)
            {
                uint argb = asset.GetPixel(x, y).Argb;
                uint alpha = argb >> 24;

                if (alpha == 0)
                {
                    continue;
                }

                if (alpha != 0xFF)
                {
                    partialAlpha++;
                }

                colours.Add(argb);
            }
        }

        return (colours, partialAlpha);
    }

    // A set that breaks budget fails at startup rather than rendering something the tier couldn't show. Per-asset
    // breakdown is logged first so the message names which files to check. See docs/asset-structure.md.
    private static void Validate(AssetColourBudget budget, ILogger? logger)
    {
        if (budget.IsWithinBudget && budget.IsWithinPalette && budget.IsOnColourGrid && budget.PartialAlphaCount == 0)
        {
            return;
        }

        if (logger is not null)
        {
            foreach (KeyValuePair<string, int> asset in budget.PerAsset.OrderByDescending(x => x.Value))
            {
                LogMessages.AssetColourCount(logger, asset.Key, asset.Value);
            }
        }

        if (!budget.IsWithinBudget)
        {
            string counted = $"{budget.ColourCount} distinct opaque colours against a cap of {budget.Cap}";
            throw new SharpKindException($"Asset colour cap exceeded for {budget.Rendition}: {counted}.");
        }

        if (!budget.IsWithinPalette)
        {
            string rule = $"{budget.Rendition} says its palette is its whole colour set, so every asset colour has to be one it names";

            throw new SharpKindException(
                $"{rule}, but {budget.OutsidePalette.Count} asset(s) use colours it does not: {Offenders(budget.OutsidePalette)}.");
        }

        if (!budget.IsOnColourGrid)
        {
            int bits = budget.Limits.ChannelBits;
            string drives = $"{budget.Rendition} drives {bits} bits per channel";
            string rule = $"{drives}, so every colour has to sit on one of its {1 << bits} levels per channel";

            throw new SharpKindException(
                $"{rule}, but {budget.OffGrid.Count} asset(s) use colours between them: {Offenders(budget.OffGrid)}.");
        }

        string partial = $"{budget.PartialAlphaCount} asset pixels have partial alpha";
        throw new SharpKindException($"{partial}; the renderer needs alpha to be either 0 or 255.");
    }

    // Names each asset with its offending colours, so the message says exactly what to open and look for.
    private static string Offenders(IReadOnlyDictionary<string, uint[]> assets)
        => string.Join(
            "; ",
            assets
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => $"{x.Key} ({Hexes(x.Value)})"));

    private static string Hexes(uint[] colours)
        => string.Join(", ", colours.Select(c => c.ToString("X8", CultureInfo.InvariantCulture)));
}
