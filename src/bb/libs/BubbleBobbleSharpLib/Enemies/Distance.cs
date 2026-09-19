// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Enemies;

// $0D1A, $102B and $106F: the same four instructions three times over, and the reason they have a
// name here is that all three are the same trap.
//
// The 6502 subtracts, and then negates the answer if the subtraction borrowed. The negate is
// `eor #$FF` followed by `adc #$01`, and that `adc` runs with the carry *clear* - the borrow left it
// that way. So it is a two's complement negate and not an off-by-one, and a translation that reads
// the `adc #$01` as "add one" is wrong by one on every difference that goes the other way.
internal static class Distance
{
    internal static byte Absolute(byte left, byte right)
    {
        int difference = left - right;

        return (byte)(difference < 0 ? -difference : difference);
    }
}
