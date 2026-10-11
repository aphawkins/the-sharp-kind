// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Items;

// The special items of types 11 (`$2ECC`) and 29 (`$35B0`): with the enemies killed (`$2E79`, `$2E61`), the six enemy
// slots put on a show of sprites the game builds at `$7D00` (`$069B` clears the six of them first), and then are handed
// back as they were.
//
//   * `$2EEA` keeps each slot's state, X, Y and sprite base (`$4700`-`$4730`), takes it off the screen and makes it the
//     show's state. The border and the background flash six times, three frames apart, with the game paused; then every
//     slot is put at the show's place.
//   * 11: one picture, a diamond (`$A854`), in all six slots. State `$13` (`$2A18`) flies them apart, each its own way
//     (`$A7A3`, `$A7A8`), until slot 2 is above `$50`.
//   * 29: six pictures (`$A877`, put in three bytes a row upwards in `$7D40` and downwards in `$7E00`), a giant in a block
//     of three by two. State `$14` (`$2A74`) has it swell and shrink (`$D017`, `$D01D`) every fourth frame, and it goes
//     at a shrink once `$08` is `$3C`.
//   * `$2A3F`: the pictures are cleared, and every slot whose kept state is below `$11` (food, by now) comes back.
//
// The reference's names here are wrong: `$A7B0` is the swollen giant's places and `$A7BC` the giant's own.
internal sealed class SpriteShow
{
    internal const byte DiamondState = 0x13;
    internal const byte GiantState = 0x14;

    internal const byte FirstPointer = 0xF4;
    internal const int PictureCount = 6;
    internal const int PictureBytes = 0x40;

    private const int FirstEnemy = 2;
    private const int Slots = 6;

    private const byte DiamondX = 0x88;
    private const byte DiamondY = 0x91;
    private const byte GiantX = 0x78;
    private const byte GiantY = 0x7C;

    private const byte ShowColour = 0x07;
    private const byte Entered = 0xFF;
    private const byte NoSeconds = 0xFF;

    private const int Flashes = 6;
    private const int FlashFrames = 3;
    private const byte FlashColour = 0x05;
    private const byte FlashSwap = 0x02;
    private const int FlashOver = (Flashes * FlashFrames) + 1;

    private const byte DiamondColourSwap = 0x05;
    private const byte GiantColourSwap = 0x03;
    private const byte GiantFrames = 0x02;
    private const byte LeavesAbove = 0x50;
    private const byte LeavesAt = 0x3C;
    private const byte SwellBits = 0xFC;
    private const byte CounterMask = 0x03;

    private const byte KeptBelow = 0x11;
    private const byte BackColour = 0x0E;

    // `$A854`: the diamond.
    private static readonly byte[] s_diamond =
    [
        0x00, 0x28, 0x00, 0x00, 0xAA, 0x00, 0x02, 0xAA, 0x80, 0x02, 0xAA, 0x80, 0x0A, 0xAA, 0xA0, 0x0A, 0xAA, 0xA0,
        0x0A, 0xAA, 0xA0, 0x0A, 0xAA, 0xA0, 0x02, 0xAA, 0x80, 0x02, 0xAA, 0x80, 0x00, 0xAA, 0x00, 0x00, 0x28,
    ];

    // `$A877`: the giant's face, nine rows of three bytes, which run on past the diamond's last byte.
    private static readonly byte[] s_giant =
    [
        0x00, 0x20, 0x00, 0x33, 0x23, 0x30, 0xDD, 0xED, 0xDC, 0xD5, 0xA9, 0x5C, 0x35, 0xA9, 0x70, 0xD6, 0xAA, 0x5C,
        0xDA, 0xAA, 0x9C, 0xAA, 0x9A, 0xA8, 0xAA, 0x56, 0xAA,
    ];

    // `$A7A3` and `$A7A8`, by slot: how far a diamond goes each turn.
    private static readonly byte[] s_diamondX = [0x0A, 0x03, 0x00, 0x04, 0x04, 0x00, 0xFC, 0xFC];
    private static readonly byte[] s_diamondY = [0x00, 0xFC, 0xFC, 0xFE, 0x02, 0x04, 0x02, 0xFE];

    // `$A7B0`/`$A7B6` swollen, and `$A7BC`/`$A7C2` not, from slot 2.
    private static readonly byte[] s_swollenX = [0x54, 0x84, 0xB4, 0x54, 0x84, 0xB4];
    private static readonly byte[] s_swollenY = [0x67, 0x67, 0x67, 0x91, 0x91, 0x91];
    private static readonly byte[] s_giantX = [0x78, 0x90, 0xA8, 0x78, 0x90, 0xA8];
    private static readonly byte[] s_giantY = [0x7C, 0x7C, 0x7C, 0x91, 0x91, 0x91];

    private readonly EntityTable _entities;
    private readonly LevelTimer _timer;

    private readonly byte[] _pictures = new byte[PictureCount * PictureBytes];
    private readonly byte[] _keptState = new byte[Slots];
    private readonly byte[] _keptX = new byte[Slots];
    private readonly byte[] _keptY = new byte[Slots];
    private readonly byte[] _keptBase = new byte[Slots];

    private bool _giant;
    private int _flash = FlashOver;

    internal SpriteShow(EntityTable entities, LevelTimer timer)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(timer);

        _entities = entities;
        _timer = timer;
    }

    // `$7D00`-`$7E7F`: sprites `$F4` to `$F9`.
    internal Span<byte> Pictures => _pictures;

    // `$4700`, `$4710`, `$4720` and `$4730`.
    internal Span<byte> KeptState => _keptState;

    internal Span<byte> KeptX => _keptX;

    internal Span<byte> KeptY => _keptY;

    internal Span<byte> KeptBase => _keptBase;

    // Whether a slot is in the show, so that sprites `$F4` onwards are its pictures.
    internal bool On
    {
        get
        {
            for (int slot = FirstEnemy; slot < EntityTable.Capacity; slot++)
            {
                if (_entities.State[slot] is DiamondState or GiantState)
                {
                    return true;
                }
            }

            return false;
        }
    }

    // The six flashes of `$2EEA`, while they last.
    internal IGameHold Flash => new FlashHold(this);

    // `$2ECC`, from `$2ED1`: the diamond, and the slots made state `$13`. The caller has killed the enemies.
    internal void Diamonds()
    {
        Clear();
        s_diamond.CopyTo(_pictures, 0);
        Begin(DiamondState, giant: false);
    }

    // `$35B0`: the giant's six pictures, and the slots made state `$14`. The caller has killed the enemies.
    internal void Giant()
    {
        Clear();
        _pictures[0x3E] = 0x2A;
        _pictures[0xC2] = 0x2A;
        _pictures[0xBC] = 0xA0;
        _pictures[0x140] = 0xA0;

        int up = 0x7C;
        int down = 0x100;
        int source = s_giant.Length - 1;

        while (source >= 0)
        {
            for (int offset = 2; offset >= 0; offset--)
            {
                _pictures[up + offset] = s_giant[source];
                _pictures[down + offset] = s_giant[source];
                source--;
            }

            up -= 3;
            down += 3;
        }

        Begin(GiantState, giant: true);
    }

    // `$2A18` and `$2A74`: a slot's turn in state `$13` or `$14`, with `$08`.
    internal void Step(int slot, byte counter)
    {
        if (_entities.State[slot] == DiamondState)
        {
            Fly(slot);
        }
        else
        {
            Swell(slot, counter);
        }
    }

    // `$069B`.
    private void Clear() => Array.Clear(_pictures);

    // `$2EEA`, to the flashes; `inc $4A` before it.
    private void Begin(byte state, bool giant)
    {
        _giant = giant;
        _entities.EnemyCount++;

        for (int enemy = Slots - 1; enemy >= 0; enemy--)
        {
            int slot = enemy + FirstEnemy;

            _keptState[enemy] = _entities.State[slot];
            _keptX[enemy] = _entities.X[slot];
            _keptY[enemy] = _entities.Y[slot];
            _keptBase[enemy] = _entities.SpriteBase[slot];

            _entities.SpriteBase[slot] = FirstPointer;
            _entities.Frame[slot] = 0;
            _entities.X[slot] = 0;
            _entities.Y[slot] = 0;
            _entities.FlashTimer[slot] = 0;
            _entities.HoldTimer[slot] = 0;
            _entities.State[slot] = state;
            _entities.Mode[slot] = Entered;
            _entities.Colour[slot] = ShowColour;
        }

        _timer.Seconds = NoSeconds;
        _flash = 0;
    }

    // `$2F51`, after the flashes, and for 29 the rest of `$35B0`.
    private byte Finish(byte counter)
    {
        (byte x, byte y) = _giant ? (GiantX, GiantY) : (DiamondX, DiamondY);

        for (int slot = FirstEnemy; slot < EntityTable.Capacity; slot++)
        {
            _entities.X[slot] = x;
            _entities.Y[slot] = y;
        }

        if (!_giant)
        {
            return counter;
        }

        counter &= CounterMask;
        Place(s_giantX, s_giantY);

        for (int enemy = 1; enemy < Slots; enemy++)
        {
            _entities.SpriteBase[enemy + FirstEnemy] = (byte)(FirstPointer + enemy);
        }

        return counter;
    }

    // `$2A18`-`$2A3E`.
    private void Fly(int slot)
    {
        _entities.X[slot] += s_diamondX[slot];
        _entities.Y[slot] += s_diamondY[slot];
        _entities.Colour[slot] ^= DiamondColourSwap;
        _entities.FlashTimer[slot] = 0;

        if (slot == FirstEnemy && _entities.Y[slot] < LeavesAbove)
        {
            HandBack();
        }
    }

    // `$2A74`-`$2AC8`.
    private void Swell(int slot, byte counter)
    {
        _entities.Colour[slot] ^= GiantColourSwap;

        if ((counter & GiantFrames) != 0 || slot != FirstEnemy)
        {
            return;
        }

        _entities.SpriteExpand ^= SwellBits;

        if (_entities.SpriteExpand != 0)
        {
            Place(s_swollenX, s_swollenY);
            return;
        }

        Place(s_giantX, s_giantY);

        if (counter >= LeavesAt)
        {
            HandBack();
        }
    }

    private void Place(byte[] x, byte[] y)
    {
        for (int enemy = Slots - 1; enemy >= 0; enemy--)
        {
            _entities.X[enemy + FirstEnemy] = x[enemy];
            _entities.Y[enemy + FirstEnemy] = y[enemy];
        }
    }

    // `$2A3F`-`$2A73`.
    private void HandBack()
    {
        Clear();
        _entities.EnemyCount = 0;

        for (int enemy = Slots - 1; enemy >= 0; enemy--)
        {
            int slot = enemy + FirstEnemy;

            if (_keptState[enemy] < KeptBelow)
            {
                _entities.State[slot] = _keptState[enemy];
                _entities.X[slot] = _keptX[enemy];
                _entities.Y[slot] = _keptY[enemy];
                _entities.SpriteBase[slot] = _keptBase[enemy];
                _entities.Colour[slot] = BackColour;
            }
            else
            {
                _entities.State[slot] = 0;
                _entities.X[slot] = 0;
                _entities.Y[slot] = 0;
            }
        }
    }

    // `$2F2D`-`$2F4E`: the border and background yellow, then EOR 2 and a wait of three frames, six times, then black.
    private sealed class FlashHold(SpriteShow show) : IGameHold
    {
        public bool Paused => true;

        public int? Border
        {
            get
            {
                if (show._flash is <= 0 or > Flashes * FlashFrames)
                {
                    return null;
                }

                int flash = (show._flash - 1) / FlashFrames;

                return flash % 2 == 0 ? FlashColour : FlashColour ^ FlashSwap;
            }
        }

        public bool Frame()
        {
            if (show._flash >= Flashes * FlashFrames)
            {
                return false;
            }

            show._flash++;
            return true;
        }

        public void Pass()
        {
        }

        public byte Finish(byte counter)
        {
            show._flash = FlashOver;
            return show.Finish(counter);
        }
    }
}
