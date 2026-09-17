// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Assets;

namespace SharpKind.Graphics;

// Outcome of checking a rendition's asset set against the limits it declared. Cap applies to the union across the whole set, not per image - one palette per machine.
public sealed class AssetColourBudget
{
    internal AssetColourBudget(
        string rendition,
        AssetColourLimits limits,
        int colourCount,
        int partialAlphaCount,
        Dictionary<string, int> perAsset,
        Dictionary<string, uint[]> outsidePalette,
        Dictionary<string, uint[]> offGrid)
    {
        Rendition = rendition;
        Limits = limits;
        ColourCount = colourCount;
        PartialAlphaCount = partialAlphaCount;
        PerAsset = perAsset;
        OutsidePalette = outsidePalette;
        OffGrid = offGrid;
    }

    public string Rendition { get; }

    // What the rendition declared about itself; the checks below run against this.
    public AssetColourLimits Limits { get; }

    // Distinct opaque colours across the whole set. Fully transparent pixels
    // are excluded - they carry no colour.
    public int ColourCount { get; }

    // Pixels whose alpha is neither 0 nor 255. The renderer treats
    // transparency as binary, so anything in between is an authoring mistake.
    public int PartialAlphaCount { get; }

    public IReadOnlyDictionary<string, int> PerAsset { get; }

    // Per asset, opaque colours not named by the palette. Always populated for logging; only enforced when the rendition says its palette is exhaustive.
    public IReadOnlyDictionary<string, uint[]> OutsidePalette { get; }

    // Per asset (plus "Palette" itself), opaque colours the rendition's DAC couldn't produce. Empty when channels are already 8-bit.
    public IReadOnlyDictionary<string, uint[]> OffGrid { get; }

    public int Cap => Limits.MaxColours;

    public bool IsWithinBudget => ColourCount <= Cap;

    public bool IsWithinPalette => !Limits.PaletteNamesEveryColour || OutsidePalette.Count == 0;

    public bool IsOnColourGrid => OffGrid.Count == 0;

    // An n-bit channel widens to 8 by replication (4 bits: 0x00, 0x11...0xFF, reaching true white unlike a left
    // shift's 0xF0 ceiling). Alpha isn't a DAC channel; checked separately as PartialAlphaCount.
    public static bool IsOnGrid(uint argb, int channelBits)
    {
        if (channelBits >= 8)
        {
            return true;
        }

        int top = (1 << channelBits) - 1;

        for (int shift = 0; shift <= 16; shift += 8)
        {
            int channel = (int)((argb >> shift) & 0xFF);

            // The nearest level's own expansion has to come back to the
            // channel itself, otherwise it sits between two of them.
            if (NearestLevel(channel, top) != channel)
            {
                return false;
            }
        }

        return true;
    }

    // Nearest rendition level for an 8-bit channel value, widened back to 8 bits.
    public static int NearestLevel(int channel, int topLevel)
        => (int)Math.Round((double)channel * topLevel / 255, MidpointRounding.AwayFromZero) * 255 / topLevel;
}
