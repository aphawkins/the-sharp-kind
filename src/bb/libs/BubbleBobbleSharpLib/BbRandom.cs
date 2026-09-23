// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using SharpKind;

namespace BubbleBobbleSharpLib;

// $E9EA in sprite-composer.s, `prng_update`: the game's one random number generator.
//
// Two bytes of state, $26 and $27, shifted as one sixteen-bit value and folded together - and then
// exclusive-ored with CIA1_TBLO, the low byte of a free-running CIA timer. Section 1 of the plan puts
// CIA emulation out of scope, so that byte is drawn from an injected source instead. The shift and
// the fold are the reference's exactly; the sequence as a whole cannot be, because the machine's
// sequence depends on how many cycles passed between calls.
//
// So no capture of a random choice can be matched byte for byte. What can be matched is everything
// that does not depend on the value: $0D86 calls this for every slot it reaches, and a lone bubble
// ends at $0E00 whatever it returns.
internal sealed class BbRandom
{
    private readonly IRandomSource _timer;

    internal BbRandom(IRandomSource timer)
    {
        ArgumentNullException.ThrowIfNull(timer);

        _timer = timer;
    }

    // $26, which the reference names RESHO after the BASIC work area it borrows, and $27. Nothing
    // seeds either for play: $2BFC saves them, seeds both with the level number for its own use, and
    // puts them back.
    internal byte Low { get; set; }

    internal byte High { get; set; }

    // $E9EA. The carry the `adc` uses is the one `rol RESHO` left, which is the top bit $26 had on
    // the way in - the `asl` at the start shifted a copy in A and not $26 itself.
    internal byte Next()
    {
        int lowTop = Low >> 7;
        int highTop = High >> 7;

        High = (byte)((High << 1) | lowTop);
        Low = (byte)((Low << 1) | highTop);

        int mixed = ((High ^ Low) + High + lowTop) & 0xFF;

        Low = (byte)(mixed ^ _timer.Random(0x100));
        return Low;
    }
}
