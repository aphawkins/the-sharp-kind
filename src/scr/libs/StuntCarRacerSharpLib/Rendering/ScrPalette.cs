// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using System.Globalization;
using SharpKind;
using SharpKind.Assets;
using SharpKind.Assets.Palettes;

namespace StuntCarRacerSharpLib.Rendering;

// The 42-entry palette, addressed positionally matching the original's SCR_BASE_COLOUR-relative
// indexing. Indices 10-17 are car colours 1, 18-25 are car colours 2, 26 onwards are track colours.
public sealed class ScrPalette
{
    private readonly IPaletteCollection _palette;

    // Comes from the locator the composition root built for the configured tier, not created (and the manifest re-read) per construction.
    public ScrPalette(IAssetLocator assetLocator)
        : this(PaletteReader.Read((assetLocator ?? throw new ArgumentNullException(nameof(assetLocator))).PalettePath))
    {
    }

    internal ScrPalette(IPaletteCollection palette)
    {
        ArgumentNullException.ThrowIfNull(palette);
        _palette = palette;
    }

    public FastColor Colour(int colourIndex) => _palette[colourIndex.ToString(CultureInfo.InvariantCulture)];
}
