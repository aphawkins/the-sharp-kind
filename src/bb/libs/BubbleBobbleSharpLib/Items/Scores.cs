// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Items;

// $0400 to $0405 and $7C26: the two players' scores, three bytes of BCD each, most significant first.
//
// $7C26 is entered with a BCD pair in A and a byte's index in Y. It adds in decimal mode and carries
// towards $0400, so an index names the column the value lands in: the last byte of a score is its
// lowest two digits, and the byte before it the two above.
internal sealed class Scores
{
    // $AB51, and $AB52 after it: the index of each player's last byte.
    private static readonly int[] s_last = [0x02, 0x05];

    private readonly byte[] _bytes = new byte[6];

    // $0400. Player one's three bytes, then player two's.
    internal ReadOnlySpan<byte> Bytes => _bytes;

    // $AB51,x. The index of a player's lowest byte.
    internal static int Last(int player) => s_last[player];

    // $7C26. The first add is the value, and each carry after it adds one to the byte before, until
    // there is no carry or $0400 has taken it. A carry out of $0400 is lost, as it is on the 6502.
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

    // `adc` with the decimal flag set, as the 6510 does it: each nibble is corrected by six once it
    // passes nine. That is not the same as reading the bytes as numbers when one is not valid BCD -
    // $A79A's last entry is $0A, and $00 plus $0A is $10.
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
