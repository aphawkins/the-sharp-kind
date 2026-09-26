// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Players;

// $28A5 in spawn-handlers.s, state $0E: a player caught by an enemy spins, falls, and then lies still.
//
// The reference calls it "spawn_animation". It is the player's death. $1D32 puts a player in $0E, and
// this runs once a pass until it moves them on to $0F, whose handler at $28E8 is a bare `rts`: the
// dead player waits there for $045C.
//
// **$86D8 marks the start.** The first call finds it not $12, sets it to $12, and starts $8610 at
// $20. Nothing here puts $86D8 back: $7F53, player_death_respawn, clears it when $045C brings the
// player back (item 3d), and VICE shows every death starting from zero.
//
// $8610 then counts down, one a call:
//
//   * $1F to $00, the spin. Frame is 0 or $0C to $0E, from bits 1 and 2 of the count. A player caught
//     mid-jump or mid-fall drops three pixels a call, wrapping from $F5 to $15. Nothing updates $87A0
//     or $87F0 while they die, so it is the state they were caught in that decides, for the whole
//     spin: a player caught in the air falls through floors.
//   * $FF to $C8, lying still. Frame is $0F to $12, from the same bits. At $C8 the state goes to $0F.
internal sealed class PlayerDying
{
    // $28AD. What $86D8 holds once the death has started.
    private const byte Started = 0x12;

    // $28B6. The count the spin starts from.
    private const byte SpinCount = 0x20;

    // $28CB and $28EE. The frames the spin and the lying still are drawn with.
    private const byte SpinFrame = 0x0B;
    private const byte StillFrame = 0x0F;

    // $28F5. The count at which the player is dead.
    private const byte DeadCount = 0xC8;

    // $28DE to $28E4. The fall, and the vertical wrap every mover uses.
    private const byte FallStep = 0x03;
    private const byte Bottom = 0xF5;
    private const byte Top = 0x15;

    private readonly EntityTable _entities;

    internal PlayerDying(EntityTable entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        _entities = entities;
    }

    // $28A5.
    internal void Step(int player)
    {
        _entities.FlashTimer[player] = 0;

        if (_entities.LeapFlag[player] != Started)
        {
            _entities.LeapFlag[player] = Started;
            _entities.AnimationTimer[player] = SpinCount;
        }

        byte count = --_entities.AnimationTimer[player];

        // $28C3's `bmi`, to $28E9.
        if ((count & 0x80) != 0)
        {
            _entities.Frame[player] = (byte)(((count >> 1) & 0x03) + StillFrame);

            if (count == DeadCount)
            {
                _entities.State[player]++;
            }

            return;
        }

        // $28C4 to $28CE. Bits 1 and 2 of the count; zero stays zero, the rest are $0C to $0E.
        int frame = (count >> 1) & 0x03;
        _entities.Frame[player] = (byte)(frame == 0 ? 0 : frame + SpinFrame);

        // $28D1 to $28D9. A player part way up a jump falls; one not rising falls unless on the ground.
        if ((_entities.RiseCounter[player] & 0x80) != 0 && (_entities.GroundState[player] & 0x80) != 0)
        {
            return;
        }

        byte y = (byte)(_entities.Y[player] + FallStep);
        _entities.Y[player] = y >= Bottom ? Top : y;
    }
}
