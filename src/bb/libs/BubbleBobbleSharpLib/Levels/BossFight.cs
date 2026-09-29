// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Levels;

// The last level (SUBFLG `$63`): there are no enemies, but a giant bounces about the level, which kills a player it touches. Its
// picture is nine sprites (the enemy slots' sprites), and its place, frame, direction and colour are the bytes `$BC`-`$BF`
// and `$C0`. A player who takes the crystal (`$7C3C`) blows bubbles that turn to thunder, and a thunder bubble's bolt (`$26`)
// that reaches the giant is a hit (`$1134`); the fiftieth stuns it for `$82` passes, and touching it then wins the game. A
// giant that is not touched in time gets up again, angry, and with forty hits to go.
internal sealed class BossFight
{
    internal const byte Level = 0x63;

    // The frame the giant wears when it is stunned, and the two it wears the rest of the time ($BE flips between them).
    internal const byte StunnedFrame = 0x09;

    private const byte FirstHits = 0x32;
    private const byte StunPasses = 0x82;
    private const byte AngryHits = 0x28;
    private const byte FlickerBelow = 0x11;

    private const byte BoltType = 0x26;
    private const byte PopType = 0x3A;

    private const byte AppearX = 0x74;
    private const byte AppearY = 0x65;
    private const byte AppearDirection = 0x06;
    private const byte AppearColour = 0x05;
    private const byte PresentX = 0x8D;
    private const byte PresentY = 0xB3;
    private const byte ParadeLimit = 0x23;
    private const byte AngryColour = 0x0A;

    private const byte LeftLimit = 0x28;
    private const byte RightLimit = 0xC0;
    private const byte TopLimit = 0x3A;
    private const byte BottomLimit = 0xB8;
    private const byte Step = 0x02;
    private const byte FlipFrame = 0x18;

    private const byte ReachWide = 0x40;
    private const byte ReachTall = 0x38;
    private const byte NearBelow = 0xF8;

    private const byte BoltLeft = 0x14;
    private const byte BoltTop = 0x15;

    private const int FirstEnemy = 2;
    private const int BlowReload = 0x08;
    private const int Objects = 0x12;

    private readonly EntityTable _entities;
    private readonly ObjectTable _objects;
    private readonly PlayerTable _players;
    private readonly LevelItems _items;
    private readonly RoundState _round;
    private readonly LevelTimer _timer;
    private readonly LevelEnd _end;

    private byte _wasFrame;

    internal BossFight(
        EntityTable entities,
        ObjectTable objects,
        PlayerTable players,
        LevelItems items,
        RoundState round,
        LevelTimer timer,
        LevelEnd end)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(round);
        ArgumentNullException.ThrowIfNull(timer);
        ArgumentNullException.ThrowIfNull(end);

        _entities = entities;
        _objects = objects;
        _players = players;
        _items = items;
        _round = round;
        _timer = timer;
        _end = end;
    }

    // `$5AFF`: the giant is about.
    internal bool Active { get; private set; }

    // `$BC`, `$BD`: where the sprite of its top left is, as any sprite's.
    internal byte X { get; set; }

    internal byte Y { get; set; }

    // `$BE`: which of the three sets of pictures it wears: 0 facing left, `$18` facing right, 9 stunned.
    internal byte Frame { get; set; }

    // `$BF`: bit 0 is right and bit 3 down, and bits 1 and 2 turn with them.
    internal byte Direction { get; set; }

    // `$C0`: the sprites' colour.
    internal byte Colour { get; set; }

    // `$5BBF`: the hits it has left before it is stunned, and, while it is, the passes.
    internal byte Hits { get; set; }

    // `$A5B8`: the player that touched it when it was stunned, which the ending pays.
    internal int Winner { get; private set; }

    // `$D015`'s bits for the giant's sprites: while it is about to get up they go on and off.
    internal bool Shown { get; private set; } = true;

    // `$06..`: a level's start clears the giant, and the last level's (`$2C71`-`$2C89`) readies it.
    internal void Begin(byte subflg)
    {
        Active = false;
        Shown = true;

        if (subflg != Level)
        {
            return;
        }

        Hits = FirstHits;
        Patch();
    }

    // `$7F83`-`$7FA0` alone, for a player who comes back.
    internal void Patch()
    {
        _items.ShowBossItems();
        _players.BlowReload[0] = BlowReload;
        _players.BlowReload[1] = BlowReload;
        _entities.EnemyCount = BlowReload;
        _round.ThunderBlow = true;
    }

    // `$16C1`-`$16D8`: the READY!! is gone, and the giant comes, and the clock is stopped for good.
    internal void Appear()
    {
        X = AppearX;
        Y = AppearY;
        Direction = AppearDirection;
        Colour = AppearColour;
        Frame = 0;
        Active = true;
        Shown = true;
        _timer.Frames = LevelTimer.Stopped;
    }

    // `$A5FD`-`$A61A`: the giant of the ending, on the blank screen.
    internal void Present()
    {
        X = PresentX;
        Y = PresentY;
        Frame = 0;
        Direction = 0;
        Colour = AppearColour;
        Active = true;
        Shown = true;
    }

    // `$0840`-`$0866`, at each IRQ of the ending (SUBFLG `$64`): the giant walks left to `$23`, and right to the edge, a pixel at a
    // time, turning about there.
    internal void Parade()
    {
        if ((Direction & 0x01) == 0)
        {
            X--;

            if (X == ParadeLimit)
            {
                Reverse();
            }

            return;
        }

        X++;

        if (X == 0)
        {
            X--;
            Reverse();
        }
    }

    // `$0A38`'s `jsr $1134`, in place of the special bubbles' corner spawner: the bolts that reach the giant, and what a stun does.
    internal void Hit()
    {
        if (Frame == StunnedFrame)
        {
            Recover();
            return;
        }

        for (int slot = Objects - 1; slot >= 0; slot--)
        {
            if (_objects.Type[slot] != BoltType || !Reaches(slot))
            {
                continue;
            }

            _objects.Type[slot] = PopType;
            _objects.EnemyType[slot] = PopType;
            Hits--;
        }

        if ((sbyte)Hits >= 0)
        {
            return;
        }

        _wasFrame = Frame;
        Frame = StunnedFrame;
        Hits = StunPasses;
    }

    // `$1D84`-`$1E2D`, at the end of each of the IRQ's turns: an angry giant moves twice.
    internal void Move()
    {
        if (_entities.FlashTimer[FirstEnemy] != 0)
        {
            Turn();
            Colour = AngryColour;
        }

        Turn();
    }

    // `$1155`-`$115D`, `$1161`-`$116C`: the distance from the giant's edge is under the reach, or, if it is behind that, no more than
    // eight.
    private static bool Within(byte at, byte edge, byte reach)
    {
        byte difference = (byte)(at - edge);

        return at >= edge ? difference < reach : difference >= NearBelow || difference < reach;
    }

    // `$118F`-`$11B6`: the stun counts down, the giant flickering at the last, and gets up angry.
    private void Recover()
    {
        Hits--;

        if (Hits == 0)
        {
            Hits = AngryHits;
            _entities.FlashTimer[FirstEnemy] = 0xFF;
            Frame = _wasFrame;
            Shown = true;
            return;
        }

        if (Hits < FlickerBelow && (Hits & 1) == 0)
        {
            Shown = !Shown;
        }
    }

    // Whether a bolt is in the giant's reach: the bolt's own sprite place, which is its cell, against the giant's.
    private bool Reaches(int slot)
    {
        byte y = (byte)((_objects.Row[slot] << 3) + BoltTop);
        byte x = (byte)((_objects.Column[slot] << 3) + BoltLeft);

        return Within(y, Y, ReachTall) && Within(x, X, ReachWide);
    }

    // `$1D96`-`$1E2D`.
    private void Turn()
    {
        Advance();
        Collide();
    }

    private void Advance()
    {
        if (Frame == StunnedFrame)
        {
            return;
        }

        bool right = (Direction & 0x01) != 0;

        if (right ? X < RightLimit : X >= LeftLimit)
        {
            X = (byte)(right ? X + Step : X - Step);
        }
        else
        {
            Direction ^= 0x03;
            Frame ^= FlipFrame;
        }

        bool down = (Direction & 0x08) != 0;

        if (down ? Y < BottomLimit : Y >= TopLimit)
        {
            Y = (byte)(down ? Y + Step : Y - Step);
        }
        else
        {
            Direction ^= 0x0C;
        }
    }

    private void Reverse()
    {
        Direction ^= 0x01;
        Frame ^= FlipFrame;
    }

    // `$1DEA`-`$1E2D`: a player in play, player 2 first, within a box of the giant is killed, or, if the giant is stunned, wins.
    private void Collide()
    {
        for (int player = PlayerTable.Capacity - 1; player >= 0; player--)
        {
            if (_entities.State[player] != PlayerFrame.PlayingState
                || !Within(_entities.X[player], X, ReachWide)
                || !Within(_entities.Y[player], Y, ReachTall))
            {
                continue;
            }

            if (Frame != StunnedFrame)
            {
                _entities.State[player] = PlayerFrame.DyingState;
                continue;
            }

            Winner = player;
            _end.Skip();
            Active = false;
            Empty();
            return;
        }
    }

    // `$1E2E`: the giant's bytes are the enemy slots' own, so they go with them.
    private void Empty()
    {
        X = 0;
        Y = 0;
        Frame = 0;
        Direction = 0;
        Colour = 0;

        for (int slot = FirstEnemy; slot < EntityTable.Capacity; slot++)
        {
            _entities.State[slot] = 0;
            _entities.X[slot] = 0;
            _entities.Y[slot] = 0;
        }
    }
}
