// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Graphics.Rendering;

/// <summary>
/// A direct-colour display: every channel goes to the nearest level its DAC
/// drives. Eight bits a channel is every level there is, so it passes colours
/// through untouched.
/// </summary>
public sealed class ChannelGridQuantiser(int channelBits) : IColourQuantiser
{
    // Zero means "every level there is", so nothing needs snapping.
    private readonly int _top = channelBits >= 8 ? 0 : (1 << channelBits) - 1;

    // Only 256 channel values, so the whole answer is a table: asked three times a pixel, a divide would be costly.
    private readonly byte[] _levels = BuildLevels(channelBits);

    public int Period => 1;

    // Levels are evenly spaced, so the gap is exact. Eight bits a channel
    // leaves neighbouring values one apart.
    public float LevelGap => _top == 0 ? 1f : 255f / _top;

    public FastColor Quantise(in FastColor colour, int x, int y)
        => new(colour.A, _levels[colour.R], _levels[colour.G], _levels[colour.B]);

    private static byte[] BuildLevels(int channelBits)
    {
        byte[] levels = new byte[256];

        for (int channel = 0; channel < levels.Length; channel++)
        {
            // Eight bits a channel snaps to itself, which the identity table
            // gives without a branch per pixel.
            levels[channel] = channelBits >= 8
                ? (byte)channel
                : (byte)AssetColourBudget.NearestLevel(channel, (1 << channelBits) - 1);
        }

        return levels;
    }
}
