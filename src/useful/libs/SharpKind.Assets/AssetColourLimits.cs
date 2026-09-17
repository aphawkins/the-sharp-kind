// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Assets;

//// JSON serializable

// A rendition's own colour limits, declared alongside its assets since the game can't know a stranger's. Defaults mean no constraint.
public sealed class AssetColourLimits
{
    // Cap is on the union across the whole set, not per image - one palette per machine.
    public int MaxColours { get; set; } = int.MaxValue;

    // True for indexed-colour hardware (every pixel is a palette entry); false for direct-colour hardware (palette just names colours).
    public bool PaletteNamesEveryColour { get; set; }

    // Bits per channel the hardware could drive; 8 = no constraint, 4 = 12-bit DAC (16 levels/channel).
    public int ChannelBits { get; set; } = 8;
}
