// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Enemies;

internal sealed class EnemySpawner
{
    private const int EnemyBase = 2;

    private const int EnemySlots = 6;

    private const int XOffset = 0x14;
    private const int YOffset = 0x15;

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

    internal void Spawn(IList<EnemySpawn> enemies)
    {
        ArgumentNullException.ThrowIfNull(enemies);

        Clear();

        _entities.HoldTimer[0] = 0;
        _entities.HoldTimer[1] = 0;
        _entities.EnemyCount = 0;

        for (int i = 0; i < enemies.Count; i++)
        {
            Place(EnemyBase + i, enemies[i]);
            _entities.EnemyCount++;
        }
    }

    private void Clear()
    {
        for (int slot = EnemyBase; slot < EnemyBase + EnemySlots; slot++)
        {
            _entities.State[slot] = 0;
            _entities.X[slot] = 0;
            _entities.Y[slot] = 0;
        }
    }

    private void Place(int slot, EnemySpawn enemy)
    {
        _entities.HoldTimer[slot] = (byte)(enemy.Delay << 1);

        int state = enemy.Type + 2;
        _entities.State[slot] = (byte)state;

        int x = (enemy.X << 3) + XOffset;
        _entities.X[slot] = (byte)x;
        _entities.Y[slot] = (byte)((enemy.Y << 3) + YOffset + (x >> 8));

        _entities.Heading[slot] = (byte)((enemy.Byte1Low << 1) | (enemy.MoveLeft ? 1 : 0));

        _entities.Frame[slot] = enemy.FaceLeft ? s_spawnTables[FrameTable + state] : (byte)0;

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
