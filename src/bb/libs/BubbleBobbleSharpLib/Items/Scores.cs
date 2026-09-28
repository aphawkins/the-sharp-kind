// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Items;

internal sealed class Scores
{
    private static readonly int[] s_last = [0x02, 0x05];

    private readonly byte[] _bytes = new byte[6];

    internal ReadOnlySpan<byte> Bytes => _bytes;

    internal static int Last(int player) => s_last[player];

    // $0540-$054F: a player's three bytes.
    internal void Clear(int player) => _bytes.AsSpan(s_last[player] - 2, 3).Clear();

    internal void Add(int index, byte value)
    {
        byte add = value;

        for (int i = index; i >= 0; i--)
        {
            (_bytes[i], bool carry) = AddDecimal(_bytes[i], add);

            if (!carry)
            {
                return;
            }

            add = 0x01;
        }
    }

    private static (byte Sum, bool Carry) AddDecimal(byte a, byte b)
    {
        int low = (a & 0x0F) + (b & 0x0F);

        if (low > 0x09)
        {
            low += 0x06;
        }

        int high = (a >> 4) + (b >> 4) + (low > 0x0F ? 1 : 0);

        if (high > 0x09)
        {
            high += 0x06;
        }

        return ((byte)((high << 4) | (low & 0x0F)), high > 0x0F);
    }
}
