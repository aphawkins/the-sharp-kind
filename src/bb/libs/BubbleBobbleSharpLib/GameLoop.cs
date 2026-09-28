// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib;

internal sealed class GameLoop
{
    private const int PassesPerSecond = 25;

    private const byte FirstCounter = 0x01;

    private const byte ReleasedState = 0x0A;

    private const byte OnePlayer = 0x01;

    private const byte BonusLevel = 0x63;

    private const byte FirstFoodMask = 0x1E;
    private const byte FirstFoodBase = 0x0A;

    private const byte StartingLives = 3;

    private const byte PlayerSpriteBase = 0x60;
    private static readonly byte[] s_spawnColour = [0x05, 0x03];

    private readonly BubbleBlow _blow;
    private readonly PlayerFrame _players;
    private readonly EnemyFrame _enemies;
    private readonly BubblePush _push;
    private readonly EnemyAiLoop _ai;
    private readonly PlayerDeath _death;
    private readonly EntityTimers _timers;
    private readonly EnemySpawner _spawner;
    private readonly PlayerRespawn _respawn;
    private readonly int _playing;

    private SolidMap? _map;
    private byte _subflg;
    private int _passesToSecond;
    private byte _counter;

    internal GameLoop(BbRandom random, int playing)
    {
        ArgumentNullException.ThrowIfNull(random);

        _playing = playing;

        Rings rings = new(PlayerTable, Scores);
        FoodDrop food = new(Entities, Scores, random);

        _blow = new(PlayerTable, Entities, Objects, rings);
        _players = BuildPlayers(Entities, _blow, rings);
        _enemies = BuildEnemies(Entities, Objects, random, food);
        _push = new(Entities, Objects);
        _death = new(Entities);
        _ai = new(Objects, Entities, new(Objects, Entities, new(Objects, Entities)), new(Objects), random, new(Objects, Entities));
        _timers = new(Objects, Entities);
        Pop = new(Objects, Entities, PlayerTable, food, Scores);
        _spawner = new(Entities, random);

        Items = new(Entities, Scores, new(PlayerTable, Entities), random)
        {
            FoodBase = (byte)((random.Next() & FirstFoodMask) + FirstFoodBase + (random.Carry ? 1 : 0)),
            Undying = OnePlayer,
        };

        _respawn = new(Entities, PlayerTable, Items);

        // $0956.
        PlayerTable.Lives.Fill(StartingLives);
    }

    internal EntityTable Entities { get; } = new();

    internal ObjectTable Objects { get; } = new();

    internal PlayerTable PlayerTable { get; } = new();

    internal BubblePop Pop { get; }

    internal Scores Scores { get; } = new();

    internal LevelItems Items { get; }

    internal void Start(Level level, IReadOnlyList<ZoneRect> zones, int number)
    {
        ArgumentNullException.ThrowIfNull(level);
        ArgumentNullException.ThrowIfNull(zones);

        Entities.SpriteEnable = 0xFF;

        _map = SolidMap.Build(level, zones, Entities);

        _spawner.Spawn(level.Enemies);

        StartPlayers();

        _subflg = (byte)(number - 1);

        if (_subflg != BonusLevel)
        {
            Items.Setup(level, _subflg);
        }

        _enemies.Enter();

        _passesToSecond = PassesPerSecond;
        _counter = FirstCounter;
    }

    internal void Pass(ReadOnlySpan<byte> ports)
    {
        SolidMap map = _map ?? throw new InvalidOperationException("Start a level before the first pass.");

        Items.Update();
        _blow.Tick();
        Pop.Update();
        _push.Update(map);
        _ai.Update(map);
        _timers.Update(ReleasedState);
        Items.Collect(_subflg);
        _respawn.Update(_subflg);

        if (--_passesToSecond == 0)
        {
            _passesToSecond = PassesPerSecond;
            Items.Tick();
        }

        _enemies.Step(_counter, map);
        _players.Step(ports, _counter, map);
        _death.Update();
        _counter += 2;
    }

    private static PlayerFrame BuildPlayers(EntityTable entities, BubbleBlow blow, Rings rings)
    {
        PlayerSteer steer = new(entities, blow);
        PlayerDrift drift = new(entities, steer, rings);
        PlayerDescent descent = new(entities);

        return new PlayerFrame(
            entities,
            new PlayerMovement(entities, blow, rings),
            new PlayerJump(entities, drift, new PlayerLanding(entities, descent)),
            new PlayerFall(entities, steer),
            blow);
    }

    private static EnemyFrame BuildEnemies(EntityTable entities, ObjectTable objects, BbRandom random, FoodDrop food)
    {
        EnemyChase chase = new(entities);
        PlayerDescent descent = new(entities);

        return new(
            entities,
            new(entities, chase, new(entities), descent),
            new(entities, objects, random),
            new(entities, chase, descent, random),
            new(entities),
            new(entities, objects, descent, random),
            food);
    }

    private void StartPlayers()
    {
        Objects.Reset();

        for (int slot = 0; slot < EntityTable.Capacity; slot++)
        {
            Entities.RiseCounter[slot] = 0xFF;
            Entities.FallCounter[slot] = 0xFF;
            Entities.GroundState[slot] = 0xFF;
            Entities.BubbleTimer[slot] = 0xFF;
            Entities.LeapFlag[slot] = 0;
            Entities.ClimbFlag[slot] = 0;
            Entities.ClimbNext[slot] = false;

            if (slot >= PlayerTable.Capacity)
            {
                Entities.FlashTimer[slot] = 0;
            }
        }

        for (int player = 0; player < PlayerTable.Capacity; player++)
        {
            Entities.State[player] = player < _playing ? PlayerFrame.PlayingState : (byte)0;
            Entities.X[player] = PlayerRespawn.SpawnX[player];
            Entities.Y[player] = PlayerRespawn.SpawnY;
            Entities.Frame[player] = PlayerRespawn.SpawnFrame[player];
            Entities.Colour[player] = s_spawnColour[player];
            Entities.SpriteBase[player] = PlayerSpriteBase;

            PlayerTable.DriftRing[player] = 0;
            PlayerTable.WalkRing[player] = 0;
            PlayerTable.BlowRing[player] = 0;
        }
    }
}
