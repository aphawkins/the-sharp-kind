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
    private const int FramesPerPass = 2;

    private const byte FirstCounter = 0x01;

    private const byte ReleasedState = 0x0A;

    private const byte OnePlayer = 0x01;

    private const byte BonusLevel = 0x63;
    private const byte LastLevel = 0x64;

    private const byte FirstFoodMask = 0x1E;
    private const byte FirstFoodBase = 0x0A;

    private const byte StartingLives = 3;
    private const byte TwoPlayerCredits = 7;

    // $0AA3: the game over waits $96 IRQ frames, and a pass is two of them.
    private const int GameOverPasses = 0x96 / 2;

    private const byte PlayerSpriteBase = 0x60;
    private readonly BubbleBlow _blow;
    private readonly PlayerFrame _players;
    private readonly EnemyFrame _enemies;
    private readonly BubblePush _push;
    private readonly EnemyAiLoop _ai;
    private readonly PlayerDeath _death;
    private readonly EntityTimers _timers;
    private readonly EnemySpawner _spawner;
    private readonly PlayerRespawn _respawn;
    private readonly PlayerJoin _join;
    private readonly int _playing;

    private SolidMap? _map;
    private byte _subflg;
    private int _overPasses;
    private byte _counter;

    internal GameLoop(BbRandom random, int playing)
    {
        ArgumentNullException.ThrowIfNull(random);

        _playing = playing;

        Flow = new(Timer);
        Rings rings = new(PlayerTable, Scores);
        FoodDrop food = new(Entities, Scores, random);

        _blow = new(PlayerTable, Entities, Objects, rings);
        _players = BuildPlayers(Entities, _blow, rings);
        _enemies = BuildEnemies(Entities, Objects, random, food, Flow);
        _push = new(Entities, Objects);
        _death = new(Entities);
        _ai = new(Objects, Entities, new(Objects, Entities, new(Objects, Entities)), new(Objects), random, new(Objects, Entities, Timer));
        _timers = new(Objects, Entities);
        Pop = new(Objects, Entities, PlayerTable, food, Scores);
        _spawner = new(Entities, random);

        Items = new(Entities, Scores, new(PlayerTable, Entities), random)
        {
            FoodBase = (byte)((random.Next() & FirstFoodMask) + FirstFoodBase + (random.Carry ? 1 : 0)),
            Undying = OnePlayer,
        };

        _respawn = new(Entities, PlayerTable, Items, Timer);
        _join = new(Entities, PlayerTable, Scores);
        Clear = new(Timer, Entities, Objects, Items, random);
        End = new(Timer, Entities, Objects, Pop, Items, random);

        // $0956-$0977.
        PlayerTable.Lives.Fill(StartingLives);
        PlayerTable.Credits = playing == PlayerTable.Capacity ? TwoPlayerCredits : (byte)(TwoPlayerCredits + 1);
    }

    internal EntityTable Entities { get; } = new();

    // $0A64: both players out of the game.
    internal bool IsOver => (Entities.State[0] | Entities.State[1]) == 0;

    // $0A99-$0AA5: the game over has waited its time, and the game goes back to the front end (step 9).
    internal bool Finished => _overPasses >= GameOverPasses;

    internal ObjectTable Objects { get; } = new();

    internal PlayerTable PlayerTable { get; } = new();

    internal BubblePop Pop { get; }

    internal Scores Scores { get; } = new();

    internal LevelItems Items { get; }

    internal LevelTimer Timer { get; } = new();

    internal LevelFlow Flow { get; }

    internal LevelClear Clear { get; }

    internal LevelEnd End { get; }

    // $0A6A: the level's clock has run out on a cleared level, and the game goes on to the next.
    internal bool IsComplete => End.Complete != 0;

    // $0A6E-$0A8B: the next level's number is kept for each player in the game, as the round they have reached.
    // Level 100's end is $A5B7's ending, which is step 7's. Returns the number of the level to start.
    internal int EndLevel()
    {
        _subflg++;

        for (int player = 0; player < PlayerTable.Capacity; player++)
        {
            if (Entities.State[player] != 0)
            {
                PlayerTable.Round[player] = _subflg;
            }
        }

        return _subflg == LastLevel
            ? throw new NotSupportedException("the ending at $A5B7 is not translated")
            : _subflg + 1;
    }

    // $09DC-$0A05. A level that follows another ($0A96) keeps the players it has in the game.
    internal void Start(Level level, IReadOnlyList<ZoneRect> zones, int number, bool next = false)
    {
        ArgumentNullException.ThrowIfNull(level);
        ArgumentNullException.ThrowIfNull(zones);

        Entities.SpriteEnable = 0xFF;

        _map = SolidMap.Build(level, zones, Entities);

        _spawner.Spawn(level.Enemies);

        StartPlayers(next);

        _subflg = (byte)(number - 1);

        if (_subflg != BonusLevel)
        {
            Items.Setup(level, _subflg);
        }

        _enemies.Enter();

        Timer.Begin(_subflg);
        Flow.Begin(level.Colours);
        Clear.Begin(_subflg);
        End.Begin();
        _counter = FirstCounter;
        _overPasses = 0;
    }

    internal void Pass(ReadOnlySpan<byte> ports)
    {
        SolidMap map = _map ?? throw new InvalidOperationException("Start a level before the first pass.");

        // $0A99: the pause flag ($37) stops the players and the game waits. The tune (step 8) is not modelled.
        if (IsOver)
        {
            _overPasses++;
            return;
        }

        Items.Update();

        // $15E1: with time on the clock the level is checked for its last enemy.
        if (Flow.Update())
        {
            if (Timer.Seconds != 0)
            {
                Clear.Update();
            }
            else
            {
                End.Update();
            }
        }

        _blow.Tick();
        Pop.Update();
        _push.Update(map);
        _ai.Update(map);
        _timers.Update(ReleasedState);
        Items.Collect(_subflg);
        _respawn.Update(_subflg);
        _join.Update(ports);

        for (int frame = 0; frame < FramesPerPass; frame++)
        {
            if (Timer.Advance())
            {
                Items.Tick();
            }
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

    private static EnemyFrame BuildEnemies(EntityTable entities, ObjectTable objects, BbRandom random, FoodDrop food, LevelFlow flow)
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
            food,
            flow);
    }

    private void StartPlayers(bool next)
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
            if (!next)
            {
                Entities.State[player] = player < _playing ? PlayerFrame.PlayingState : (byte)0;
            }

            Entities.X[player] = PlayerRespawn.SpawnX[player];
            Entities.Y[player] = PlayerRespawn.SpawnY;
            Entities.Frame[player] = PlayerRespawn.SpawnFrame[player];
            Entities.Colour[player] = LevelEnd.SpawnColour[player];
            Entities.SpriteBase[player] = PlayerSpriteBase;

            PlayerTable.DriftRing[player] = 0;
            PlayerTable.WalkRing[player] = 0;
            PlayerTable.BlowRing[player] = 0;
        }
    }
}
