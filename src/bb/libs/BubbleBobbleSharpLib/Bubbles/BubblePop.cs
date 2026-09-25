// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Bubbles;

// What the sprite renderer's pass does to the slots it draws: chiefly the pop, and what happens to
// the enemy inside a popped bubble.
//
// **This is the sprite renderer's, not the AI's.** $E90E draws the eighteen slots, from $11 down, and
// jumps through a vector per type: `jmp ($0400 + type + $0C)`, with the table at $040C. Most vectors
// only draw. The ones below change a slot:
//
//   * $16, $7BD4. A bubble just blown becomes type $00 before it is first drawn.
//   * $18 to $22, $3CB2. A caught enemy is not drawn as part of its bubble: its own hardware sprite,
//     slot 2 onwards, is moved onto the bubble and given the frame $1090 kept in $AA42.
//   * $34, $3D2D. The bubble a player has just touched, which $0D02 typed $34. Every bubble within
//     $18 pixels is popped with it, for the same player. Then $3D77.
//   * $3A, $3D77. A bubble whose time ran out, which $13BE typed $3A.
//   * $3D77 itself: a bubble with an enemy in it lets the enemy out dead, through $1A6F, and counts
//     it in the player's chain. Any other goes to $3DB0: ten points if a player popped it, a
//     letter of EXTEND for a letter bubble, and the first frame of the pop, $3C.
//   * $3C, $3E, $40, $38 and $36 are the pop's frames, one a pass, and $36 empties the slot.
//
// After the loop, $E97F scores each player's chain once it has stopped growing: 1000 for one enemy,
// doubling for each after it.
//
// **The drawing itself is recorded, not done.** Drawn records each slot's software-sprite entry into
// object-sprites.tga - $00-$14 (a bubble, `type x2 + $A9C4`) and $34/$3A/$3C, $3E/$40 (the pop's first
// and later frames, `frame + $A9C4`) - since the screen shows what this pass drew before $0CF2 moves
// anything. Nothing is recorded for a caught enemy ($18-$22, its own hardware sprite draws it) or for
// $36, $38, $42, $48, $4A and the specials, which item 2d has yet to translate.
//
// **Not here:** the special bubbles' own arms at $3E02 - $06, $0A and $08. Only a special bubble
// reaches them, and nothing translated makes one, so they throw rather than guess.
internal sealed class BubblePop
{
    // Nothing drawn this pass - a free or hidden slot, a caught enemy (its own hardware sprite draws
    // it), or a type not translated yet (item 2d). Internal: ObjectView8Bit needs it to skip a slot.
    internal const byte NotDrawn = 0xFF;

    // The types this class steps. $3C to $40 and $38 to $36 are the frames of the pop.
    private const byte BlownType = 0x16;
    private const byte FloatingType = 0x00;
    private const byte TouchedType = 0x34;
    private const byte ExpiredType = 0x3A;
    private const byte PopFrom = 0x3C;
    private const byte PopMiddle = 0x3E;
    private const byte PopLate = 0x40;
    private const byte PopLast = 0x38;
    private const byte GoneType = 0x36;

    // $E931's `bmi`. Nothing at or above $80 is drawn or stepped.
    private const byte Hidden = 0x80;

    // $00-$14: a bubble or lightning bubble, drawn by the render pass before this one moves anything.
    private const byte LastFloatingType = 0x14;

    // docs/bb-port-plan.md, item 2c: object-sprites.tga holds entries 0-43 (type x2 + $A9C4), then
    // pop frames 44-51 (0-3 for the early frames, 4-7 for the late ones, also + $A9C4).
    private const int PopFrameBase = 44;

    // $3D38. Only bubbles and the things in them are popped along with the first.
    private const byte ChainBelow = 0x24;

    // $3D4E and $3D5F. Wider than any other box in the game.
    private const byte ChainRange = 0x18;

    // $3D7A and $3D7E. A bubble with an enemy in it: $1090 typed it $18 plus twice the enemy.
    private const byte CapturedFrom = 0x18;
    private const byte CapturedBelow = 0x24;

    // $3DB6 to $3DC0. Ten points for a bubble, a hundred for types $06 to $0B.
    private const byte BubbleScore = 0x01;
    private const byte SpecialScore = 0x0A;
    private const byte SpecialFrom = 0x06;
    private const byte SpecialBelow = 0x0C;

    // $3DD5 and $3DD9. The EXTEND letter bubbles.
    private const byte LetterFrom = 0x0C;
    private const byte LetterBelow = 0x16;

    // $3E02, $3E2C and $3E5E. The special bubbles' own arms.
    private const byte ItemType = 0x06;
    private const byte PlatformType = 0x0A;
    private const byte VanishType = 0x08;

    // $AB53 and on into $AB55: one bit per EXTEND letter.
    private static readonly byte[] s_letterBits = [0x01, 0x02, 0x04, 0x08, 0x10, 0x20];

    // $AB52, indexed by the chain count: $AB52 itself, then the same $AB53 and $AB55 bits read as
    // BCD, added a byte above the lowest - 1000, 2000, 4000 and on.
    private static readonly byte[] s_chainScores = [0x05, 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80];

    private readonly ObjectTable _objects;
    private readonly EntityTable _entities;
    private readonly PlayerTable _players;
    private readonly FoodDrop _food;
    private readonly Scores _scores;

    // $46 and $47, each player's chain, and $48 and $49, the same as $E90E began.
    private readonly byte[] _chain = new byte[PlayerTable.Capacity];
    private readonly byte[] _chainBefore = new byte[PlayerTable.Capacity];

    // What this pass drew for each slot, an index into object-sprites.tga, or NotDrawn. Recorded here
    // because the screen shows the drawing this pass made before $0CF2 moves anything - by the time a
    // later pass runs, the slot's own position and type may already have moved on.
    private readonly byte[] _drawn = new byte[ObjectTable.Capacity];

    internal BubblePop(ObjectTable objects, EntityTable entities, PlayerTable players, FoodDrop food, Scores scores)
    {
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(food);
        ArgumentNullException.ThrowIfNull(scores);

        _objects = objects;
        _entities = entities;
        _players = players;
        _food = food;
        _scores = scores;
    }

    // $46 and $47: how many enemies each player's chain has let out so far.
    internal Span<byte> Chain => _chain;

    // What this pass drew for each of the eighteen slots. See object-sprites.tga and _drawn.
    internal Span<byte> Drawn => _drawn;

    // $E90E's loop and $E97F after it.
    internal void Update()
    {
        _chain.CopyTo(_chainBefore, 0);

        for (int slot = ObjectTable.Capacity - 1; slot >= 0; slot--)
        {
            Step(slot);
        }

        for (int player = PlayerTable.Capacity - 1; player >= 0; player--)
        {
            Score(player);
        }
    }

    private void Step(int slot)
    {
        byte type = _objects.Type[slot];

        _drawn[slot] = type switch
        {
            <= LastFloatingType => Entry(type, slot),
            TouchedType or ExpiredType or PopFrom => PopFrame(0, slot),
            PopMiddle or PopLate => PopFrame(4, slot),
            _ => NotDrawn,
        };

        switch (type)
        {
            case >= Hidden:
                break;

            // $7BD4. Drawn as $00 above: the type becomes it before this pass draws the slot.
            case BlownType:
                _objects.Type[slot] = FloatingType;
                _drawn[slot] = Entry(FloatingType, slot);
                break;
            case >= CapturedFrom and < CapturedBelow:
                Carry(slot);
                break;
            case TouchedType:
                Spread(slot);
                Release(slot);
                break;
            case ExpiredType:
                Release(slot);
                break;

            // $3CD3: two `inc`s.
            case PopFrom:
                _objects.Type[slot] = PopMiddle;
                break;

            // $3CDB.
            case PopMiddle:
                _objects.Type[slot] = PopLate;
                break;

            // $3CDF.
            case PopLate:
                _objects.Type[slot] = PopLast;
                break;

            // $3DA9.
            case PopLast:
                _objects.Type[slot] = GoneType;
                break;

            // $7BDB.
            case GoneType:
                _objects.Type[slot] = ObjectTable.FreeType;
                _objects.Column[slot] = 0;
                _objects.Row[slot] = 0;
                break;
        }
    }

    // type x2 + $A9C4: entries 0-43 of object-sprites.tga, the plain bubbles and lightning bubble.
    private byte Entry(byte type, int slot) => (byte)((type * 2) + _objects.SubX[slot]);

    // base (0 or 4) + $A9C4: entries 44-51, the pop animation's eight frames.
    private byte PopFrame(int frameBase, int slot) => (byte)(PopFrameBase + frameBase + _objects.SubX[slot]);

    // $3CB2. The type is $18 plus twice the enemy, and its sprite is slot 2 onwards.
    private void Carry(int slot)
    {
        int entity = ((_objects.Type[slot] - CapturedFrom) >> 1) + 2;

        _entities.Frame[entity] = _objects.EnemyType[slot];
        _entities.FlashTimer[entity] = 0;
        _entities.X[entity] = _objects.X[slot];
        _entities.Y[entity] = _objects.Y[slot];
    }

    // $3D2D. The AI counter is the popping slot's, tested on every turn of the loop: a bubble still
    // being moved pops nothing else. What $3D66 keeps is the type the bubble had.
    private void Spread(int slot)
    {
        if (_objects.State[slot] != 0)
        {
            return;
        }

        for (int other = ObjectTable.Capacity - 1; other >= 0; other--)
        {
            if (other == slot
                || _objects.Type[other] >= ChainBelow
                || Distance.Absolute(_objects.X[slot], _objects.X[other]) >= ChainRange
                || Distance.Absolute(_objects.Y[slot], _objects.Y[other]) >= ChainRange)
            {
                continue;
            }

            _objects.EnemyType[other] = _objects.Type[other];
            _objects.Type[other] = TouchedType;
            _objects.Direction[other] = _objects.Direction[slot];
        }
    }

    // $3D77.
    private void Release(int slot)
    {
        byte held = _objects.EnemyType[slot];

        if (held is < CapturedFrom or >= CapturedBelow)
        {
            Burst(slot, held);
            return;
        }

        // $3D82. The enemy goes back into its slot in the state $1090 kept, and $1A6F kills it
        // from there. The chain count is the food it will be.
        int enemy = (held - CapturedFrom) >> 1;
        int player = _objects.Direction[slot];

        _entities.State[enemy + 2] = _objects.Variant[slot];
        _food.Kill(enemy);

        // $3D91. The sprite comes back on, whichever way $142E's flicker left it.
        _entities.SpriteEnable |= (byte)(1 << (enemy + 2));

        _chain[player]++;
        _entities.AttackTimer[enemy + 2] = _chain[player];

        _objects.Type[slot] = GoneType;
    }

    // $3DB0. held is the type the bubble had before it was touched.
    private void Burst(int slot, byte held)
    {
        if (_objects.Type[slot] == TouchedType)
        {
            int player = _objects.Direction[slot];
            byte score = held is >= SpecialFrom and < SpecialBelow ? SpecialScore : BubbleScore;

            _scores.Add(Scores.Last(player & 0x01), score);

            if (held is >= LetterFrom and < LetterBelow)
            {
                Letter(player, held);
            }
        }

        if (held is ItemType or PlatformType or VanishType)
        {
            throw new NotSupportedException($"special bubble ${held:X2}'s pop at $3E02 is not translated");
        }

        _objects.Type[slot] = PopFrom;
    }

    // $3DDD. One bit per letter, in twos from $0C: E, X, T, N and D. The fourth bit is the second E,
    // so only $0C reaches it, and only once the player holds the first.
    private void Letter(int player, byte held)
    {
        int letter = (held - LetterFrom) >> 1;

        if (letter >= 3)
        {
            letter++;
        }

        if (letter == 0 && (_players.ExtendLetters[player] & s_letterBits[0]) != 0)
        {
            letter = 3;
        }

        _players.ExtendLetters[player] |= s_letterBits[letter];
    }

    // $E97F. A chain that grew during this pass waits for the next one.
    private void Score(int player)
    {
        byte count = _chain[player];

        if (count == 0 || count != _chainBefore[player])
        {
            return;
        }

        _chain[player] = 0;
        _scores.Add(Scores.Last(player) - 1, s_chainScores[count]);
    }
}
