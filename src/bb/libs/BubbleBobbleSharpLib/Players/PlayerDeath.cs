// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Players;

// $1D32 in joystick-input.s, the end of $1CBD: an enemy touching a player kills them.
//
// The reference heads it "player-to-player collision". It is a player against the six enemy slots,
// once all eight slots have moved. A player in state 1 with an enemy in state 1 to $0A close enough in
// both axes goes to state $0E, dying. Nothing else is written; the enemy walks on.
//
// **The box is not centred on the player.** The `cmp #$01` that let the player through leaves the
// carry set, so `adc #$01` adds two to X, and that add's own carry goes into Y's `adc #$02`. The
// point measured from is X + 2 and Y + 2, or Y + 3 when X + 2 wraps.
//
// **$5AFF.** $1D2A skips this when $5AFF is not zero, and jumps to $1D84 instead, which returns at
// once on every level but 99. Only untranslated code sets it - the hurry-up at $16CD, the bonus
// stage at $370C, $F06D and $7EC1 - so in translated play it is zero and this always runs.
internal sealed class PlayerDeath
{
    // $1D36. Only a player walking about can be killed.
    private const byte PlayingState = 0x01;

    // $1D4F. Enemy states 1 to $0A are walking about; zero is an empty slot, and $0B upwards popped.
    private const byte DeadlyStates = 0x0B;

    // $1D5E and $1D6D. Twelve pixels, as one unsigned byte after an absolute difference.
    private const byte Range = 0x0C;

    // $1D6F. What a caught player becomes.
    private const byte DyingState = 0x0E;

    // $1D49. Slots 2 to 7.
    private const int FirstEnemy = 2;

    private readonly EntityTable _entities;

    internal PlayerDeath(EntityTable entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        _entities = entities;
    }

    // $1D32. Player 2 first, then player 1.
    internal void Update()
    {
        for (int player = PlayerTable.Capacity - 1; player >= 0; player--)
        {
            if (_entities.State[player] != PlayingState)
            {
                continue;
            }

            // $1D3B to $1D48. The carry is set from the `cmp #$01` above.
            int x = _entities.X[player] + 1 + 1;
            byte y = (byte)(_entities.Y[player] + 2 + (x > 0xFF ? 1 : 0));

            for (int enemy = EntityTable.Capacity - 1; enemy >= FirstEnemy; enemy--)
            {
                byte state = _entities.State[enemy];

                if (state is 0 or >= DeadlyStates)
                {
                    continue;
                }

                if (Distance(_entities.X[enemy], (byte)x) < Range && Distance(_entities.Y[enemy], y) < Range)
                {
                    _entities.State[player] = DyingState;
                }
            }
        }
    }

    // $1D53 to $1D5E. `sbc`, and on a borrow `eor #$FF` then `adc #$01` with the carry clear.
    private static byte Distance(byte from, byte to)
        => from >= to ? (byte)(from - to) : (byte)(to - from);
}
