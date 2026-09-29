// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Levels;

// The special item of type 7 (`$A653`): a giant bubble that sweeps down the level from the top right and turns every enemy it
// passes to food. The item only sets `$58FF`, the sweeps to make (two to five). `$0A3B` then starts one, while none is under way
// (`$593F` is 0): the players' two object slots are popped, and the giant, object type `$46`, is put in both of them, in row 2 and a column
// on the right (`$1AB6`). Each pass of the game then moves it a cell left and a cell down (`$19CF`) and takes what is within
// `$30` of it: a caught enemy (`$1A6F`) and a free one. It leaves at row `$16`, and the next sweep starts.
//
// Not modelled: the border's flash when a sweep starts, which is `$7BC3`'s frames a time, and the wait of the start, which is
// not drawn either. `Start` returns the frames it took so the game can go on with the clock. What the giant is drawn with is
// `$3F28`'s ten characters, a picture the view has.
internal sealed class GiantBubble
{
    internal const byte Type = 0x46;

    // The screen rows above the object's row that its picture starts: `$3F28` draws from the cell two rows up.
    internal const int TopRows = 2;

    private const byte FirstSweeps = 2;
    private const byte SweepMask = 0x03;

    private const byte StartPhase = 4;
    private const byte FlashFrames = 5;

    private const byte FirstColumn = 0x16;
    private const byte ColumnMask = 0x07;
    private const byte FirstRow = 0x02;
    private const byte LastRow = 0x16;

    private const byte GoneType = 0x38;

    private const byte FirstCaught = 0x18;
    private const byte CaughtBelow = 0x24;

    private const byte LeftBias = 0x0C;
    private const byte TopBias = 0x0D;
    private const byte Reach = 0x30;
    private const byte MaxFree = 0x0B;
    private const byte KillBelow = 6;

    private const int FirstEnemy = 2;
    private const int Enemies = 6;

    private readonly EntityTable _entities;
    private readonly ObjectTable _objects;
    private readonly FoodDrop _food;
    private readonly BbRandom _random;
    private readonly SlotPop _slots;

    internal GiantBubble(EntityTable entities, ObjectTable objects, BubblePop pop, FoodDrop food, BbRandom random)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(pop);
        ArgumentNullException.ThrowIfNull(food);
        ArgumentNullException.ThrowIfNull(random);

        _entities = entities;
        _objects = objects;
        _food = food;
        _random = random;
        _slots = new(objects, pop);
    }

    // `$58FF`: the sweeps still to make, which a cleared level (`$16F7`) and a new one take away.
    internal byte Sweeps { get; set; }

    // `$593F`: a sweep is under way, from the flash of its start (4) to the frame it is over.
    internal byte Phase { get; set; }

    // Whether the giant is in the level to be drawn: it is the object type `$46` in slot 0, in its column and row.
    internal bool Shown => Phase != 0 && _objects.Type[0] == Type;

    internal int Column => _objects.Column[0];

    internal int Row => _objects.Row[0];

    // `$A653`: two to five sweeps.
    internal void Order() => Sweeps = (byte)((_random.Next() & SweepMask) + FirstSweeps);

    // `$06`..: a level's start clears both.
    internal void Reset()
    {
        Sweeps = 0;
        Phase = 0;
    }

    // `$16F7`, `$1621`: the sweeps to come are cancelled, and the one under way finishes.
    internal void Cancel() => Sweeps = 0;

    // `$0A3B`-`$0A45`, `$1AB6`-`$1AE7`: while there are sweeps to make and none is under way, one starts. Returns how many frames of the
    // game's own waiting it took, for the clock, or none.
    internal int Start()
    {
        if (Sweeps == 0 || Phase != 0)
        {
            return 0;
        }

        _slots.Pop();
        int frames = _slots.Wait();

        Phase = StartPhase;

        // `$1AC0`: four flashes of the border, five frames each.
        frames += StartPhase * FlashFrames;

        _objects.Type[0] = Type;
        _objects.Type[1] = Type;
        _objects.Row[0] = FirstRow;
        _objects.Column[0] = (byte)((_random.Next() & ColumnMask) + FirstColumn);

        return frames;
    }

    // `$198A`-`$1A5E`, in the pass's render: the giant goes on a cell, or, when it has gone, the sweep is over.
    internal void Sweep()
    {
        if (Phase == 0)
        {
            return;
        }

        byte type = _objects.Type[0];

        if ((sbyte)type < 0)
        {
            Phase = 0;

            if (Sweeps != 0)
            {
                Sweeps--;
            }

            return;
        }

        if (type != Type)
        {
            return;
        }

        _objects.Column[0]--;
        _objects.Row[0]++;

        byte left = (byte)((_objects.Column[0] * 8) + LeftBias);
        byte top = (byte)((_objects.Row[0] * 8) + TopBias);

        TakeCaught(left, top);
        TakeFree(left, top);

        if (_objects.Row[0] == LastRow)
        {
            _objects.Type[0] = GoneType;
            _objects.Type[1] = GoneType;
            return;
        }

        // `$1A5F`: an enemy the giant is not near is taken now and then all the same.
        byte chance = _random.Next();

        if (chance < KillBelow && IsFree(chance))
        {
            _food.Kill(chance);
        }
    }

    // Not below the edge, and less than `$30` past it.
    private static bool Within(byte value, byte edge) => value >= edge && value - edge < Reach;

    // `$19E7`-`$1A29`: each of the sixteen objects after the players' two that holds a caught enemy within the giant's reach is
    // popped, and its enemy is let out as food. The game reads the variant it lets it out in from `$AA30,x`, the slot
    // two before the one it is looking at.
    private void TakeCaught(byte left, byte top)
    {
        for (int index = ObjectTable.Capacity - FirstEnemy - 1; index >= 0; index--)
        {
            int slot = index + FirstEnemy;
            byte type = _objects.Type[slot];

            if (type is < FirstCaught or >= CaughtBelow
                || !Within(_objects.X[slot], left)
                || !Within(_objects.Y[slot], top))
            {
                continue;
            }

            int enemy = (type - FirstCaught) >> 1;

            _entities.State[enemy + FirstEnemy] = _objects.Variant[index];
            _entities.SpriteEnable |= (byte)(1 << (enemy + FirstEnemy));
            _food.Kill(enemy);
            _objects.Type[slot] = GoneType;
        }
    }

    // `$1A2E`-`$1A4F`: an enemy on the level, walking or flying, within its reach becomes food.
    private void TakeFree(byte left, byte top)
    {
        for (int enemy = Enemies - 1; enemy >= 0; enemy--)
        {
            if (IsFree(enemy)
                && Within(_entities.X[enemy + FirstEnemy], left)
                && Within(_entities.Y[enemy + FirstEnemy], top))
            {
                _food.Kill(enemy);
            }
        }
    }

    private bool IsFree(int enemy) => _entities.State[enemy + FirstEnemy] is not 0 and < MaxFree;
}
