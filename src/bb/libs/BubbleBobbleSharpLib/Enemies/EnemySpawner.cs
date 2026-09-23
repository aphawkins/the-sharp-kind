// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

// $39D2 in entity-spawn.s, with $1E2E in joystick-input.s before it: a level's enemy list, put into
// slots 2 to 7 as the level starts.
//
// The level display at $39A8 walks the spawn data to the level's own list, calls $1E2E and falls
// into $39D2. The walk is what LevelStore already did, so this takes the level's list as it is.
//
// **The state is the class plus two.** $39FB adds two to the three class bits and stores the sum in
// $B4, which is State two slots along. So class 3 takes state 5, which is DiagonalMover.
//
// **Classes 6 and 7 read past each table.** $3A2A and the loads after it index four eight-byte
// tables with the state, and states 8 and 9 go past the end. The tables sit one after another, so
// the 6502 reads the first two bytes of the next one. s_spawnTables holds them as one block for that
// reason. It is not rare: 133 of the 572 spawns in the game are class 6 or 7.
//
// The rest of entity-spawn.s is not here. $3A6C copies the map's top and bottom rows outwards,
// which is SolidMap's business, and $3A89 sets up a level's special bubbles by writing operands in
// the game loop, which is Phase 7's.
internal sealed class EnemySpawner
{
    // $B4, $BC and $C4 are State, X and Y two slots along.
    private const int EnemyBase = 2;

    // $1E2E: ldx #$05, then dex and bpl. Slots 2 to 7.
    private const int EnemySlots = 6;

    // $3A03's `adc #$14` and $3A0B's `adc #$15`. A column and a row of eight pixels each, with the
    // playfield's own offset added.
    private const int XOffset = 0x14;
    private const int YOffset = 0x15;

    // $3A2A to $3A47. $AB61, $AB69, $AB71 and $AB79, eight bytes each, and the first two of $AB81,
    // which is as far as state 9 reaches.
    private const int ColourTable = 0x00;
    private const int SpriteBaseTable = 0x08;
    private const int TurnTable = 0x10;
    private const int FrameTable = 0x18;

    private static readonly byte[] s_spawnTables =
    [
        0x80, 0xA0, 0x0C, 0x0F, 0x05, 0x0D, 0x04, 0x05,
        0x03, 0x0F, 0x73, 0x7F, 0x8B, 0x97, 0x9F, 0xAB,
        0xB3, 0xBB, 0x1E, 0x19, 0x00, 0x0F, 0x14, 0x14,
        0x0A, 0x14, 0x04, 0x04, 0x04, 0x04, 0x04, 0x02,
        0x02, 0x00,
    ];

    private readonly EntityTable _entities;
    private readonly BbRandom _random;

    internal EnemySpawner(EntityTable entities, BbRandom random)
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(random);

        _entities = entities;
        _random = random;
    }

    // $1E2E, then $39D2 until the list's zero byte. The zero $1E2E returns in A is the count, the
    // first slot and the first byte of the list all at once.
    internal void Spawn(IList<EnemySpawn> enemies)
    {
        ArgumentNullException.ThrowIfNull(enemies);

        Clear();

        // $39D3 and $39D6. The two players' hold timers, from the same zero.
        _entities.HoldTimer[0] = 0;
        _entities.HoldTimer[1] = 0;
        _entities.EnemyCount = 0;

        for (int i = 0; i < enemies.Count; i++)
        {
            Place(EnemyBase + i, enemies[i]);
            _entities.EnemyCount++;
        }
    }

    // $1E2E. State, X and Y of the six enemy slots, and nothing else.
    private void Clear()
    {
        for (int slot = EnemyBase; slot < EnemyBase + EnemySlots; slot++)
        {
            _entities.State[slot] = 0;
            _entities.X[slot] = 0;
            _entities.Y[slot] = 0;
        }
    }

    // $39E2 to $3A63, one three-byte record. levels.json splits the bytes into fields, so each byte
    // is read back from the part of it the 6502 masks out.
    private void Place(int slot, EnemySpawn enemy)
    {
        // $39F0. $39D2 reads the delay with `and #$3F` and `asl`, so the two flag bits drop out.
        _entities.HoldTimer[slot] = (byte)(enemy.Delay << 1);

        int state = enemy.Type + 2;
        _entities.State[slot] = (byte)state;

        // $39FF. The carry out of the column is not cleared before the row, so a column of 30 or more
        // puts the thing a pixel lower. No level has one.
        int x = (enemy.X << 3) + XOffset;
        _entities.X[slot] = (byte)x;
        _entities.Y[slot] = (byte)((enemy.Y << 3) + YOffset + (x >> 8));

        // $3A0F. The record's low three row bits one place up, and the move-left bit in bit 0.
        _entities.Heading[slot] = (byte)((enemy.Byte1Low << 1) | (enemy.MoveLeft ? 1 : 0));

        // $3A21. A thing facing left starts at the first frame of its second block.
        _entities.Frame[slot] = enemy.FaceLeft ? s_spawnTables[FrameTable + state] : (byte)0;

        // $3A3D. One draw for every enemy, whether or not anything uses it.
        byte turn = (byte)((_random.Next() & 0x1F) + s_spawnTables[TurnTable + state]);

        byte frames = s_spawnTables[FrameTable + state];
        _entities.FrameCount[slot] = frames;
        _entities.FrameMask[slot] = (byte)(frames - 1);
        _entities.TurnInterval[slot] = turn;
        _entities.TurnTimer[slot] = turn;
        _entities.SpriteBase[slot] = s_spawnTables[SpriteBaseTable + state];
        _entities.Colour[slot] = s_spawnTables[ColourTable + state];
    }
}
