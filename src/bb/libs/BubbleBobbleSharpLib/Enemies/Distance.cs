// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Enemies;

internal static class Distance
{
    internal static byte Absolute(byte left, byte right)
    {
        int difference = left - right;

        return (byte)(difference < 0 ? -difference : difference);
    }
}
