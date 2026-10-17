// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Levels;

// The bonus round a special item starts ($34A0, the item of type 24): every enemy still loose tumbles down to a platform
// and waits there to be touched, and the players go on in state $18 (a flashing double-speed play) until the level ends.
// The round is `RoundState`'s two bytes, which the rest of the game reads.
internal sealed class BonusRound
{
    internal const byte PlayerState = 0x18;
    internal const byte TumbleState = 0x16;

    private const byte MaxCaught = 0x0B;
    private const byte HoldingState = 0x0A;
    private const byte Entered = 0xFF;

    private const int FirstEnemy = 2;

    private readonly EntityTable _entities;
    private readonly RoundState _round;
    private readonly byte[] _savedFlash;

    internal BonusRound(EntityTable entities, RoundState round, byte[] savedFlash)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(round);
        ArgumentNullException.ThrowIfNull(savedFlash);

        _entities = entities;
        _round = round;
        _savedFlash = savedFlash;
    }

    // How many rounds have begun, for the song ($05AD, with the bonus round's tune).
    internal int Rounds { get; private set; }

    // `$34A0`-`$34F9`.
    internal void Start()
    {
        for (int slot = EntityTable.Capacity - 1; slot >= FirstEnemy; slot--)
        {
            byte state = _entities.Slot(slot).State;

            if (state is 0 or >= MaxCaught)
            {
                continue;
            }

            // An enemy already rising in its bubble keeps the state it will come back to.
            if (state != HoldingState)
            {
                _entities.Slot(slot).RiseCounter = state;
            }

            _entities.Slot(slot).State = TumbleState;
            _entities.Slot(slot).HoldTimer = 0;
            _entities.Slot(slot).Mode = Entered;
        }

        for (int player = 0; player < PlayerTable.Capacity; player++)
        {
            _savedFlash[player] = _entities.Slot(player).FlashTimer;
        }

        for (int player = PlayerTable.Capacity - 1; player >= 0; player--)
        {
            byte state = _entities.Slot(player).State;

            if (state is 0 or PlayerFrame.DyingState or PlayerFrame.DeadState)
            {
                continue;
            }

            _entities.Slot(player).State = PlayerState;
            _entities.Slot(player).FlashTimer = 0;
        }

        _round.Respawn = PlayerState;
        _round.Release = TumbleState;
        Rounds++;
    }
}
