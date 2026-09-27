// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using SharpKind;

namespace BubbleBobbleSharpLib;

internal sealed class BbRandom
{
    private readonly IRandomSource _timer;

    internal BbRandom(IRandomSource timer)
    {
        ArgumentNullException.ThrowIfNull(timer);

        _timer = timer;
    }

    internal byte Low { get; set; }

    internal byte High { get; set; }

    internal bool Carry { get; private set; }

    internal byte Next()
    {
        int lowTop = Low >> 7;
        int highTop = High >> 7;

        High = (byte)((High << 1) | lowTop);
        Low = (byte)((Low << 1) | highTop);

        int mixed = (High ^ Low) + High + lowTop;
        Carry = mixed > 0xFF;

        Low = (byte)((mixed & 0xFF) ^ _timer.Random(0x100));
        return Low;
    }
}
