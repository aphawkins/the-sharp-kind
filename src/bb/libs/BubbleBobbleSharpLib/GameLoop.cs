// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib;

// game-loop.s: a level's start at $09DC, and one pass of the main loop at $0A07, with the raster
// interrupt's share of the pass beside it. Every translated routine is called from here, in the order
// the reference calls it.
//
// **A pass is two 50Hz frames.** The main loop waits for $08 to move twice before it goes round
// again, and the IRQ calls $1CBD, the players and the enemies, on every second frame. So the game
// runs at 25 passes a second and $1CBD runs once in each; this calls it at the end of the pass.
//
// **Gaps, named so they are not mistaken for finished work:** $0BED (bubbles from the corners),
// $0AAB (a player against bubbles and enemies), $32C1 (EXTEND), the level timer and the hurry-up,
// and level completion. Each is an item in bb-port-plan.md. The bonus level's items throw, so level
// 100 starts without them.
internal sealed class GameLoop
{
    // $09FD. The IRQ counts fifty frames to a second, which is twenty-five passes.
    private const int PassesPerSecond = 25;

    // $0A03 zeroes $08, and the first frame of the pass that calls $1CBD is odd.
    private const byte FirstCounter = 0x01;

    // $0654. What a released enemy's state becomes, outside the bonus rounds.
    private const byte ReleasedState = 0x0A;

    // $098A. A one-player game: only player 1 has not died.
    private const byte OnePlayer = 0x01;

    // SUBFLG of the bonus level, which is the hundredth.
    private const byte BonusLevel = 0x63;

    // $0942 and $0944. The first level's food starts somewhere in $0A to $28.
    private const byte FirstFoodMask = 0x1E;
    private const byte FirstFoodBase = 0x0A;

    // $04BB and $05C5: where a life starts, which way it faces and its colour.
    private const byte SpawnY = 0xDD;

    // $451E copies $47BD into $8598 once, at boot: $60 for both players, the pointer $5800's sprites
    // start at. Nothing writes a player's byte again, so setting it with the rest of a life is the same.
    private const byte PlayerSpriteBase = 0x60;
    private static readonly byte[] s_spawnX = [0x2C, 0xEC];
    private static readonly byte[] s_spawnFrame = [0x00, 0x04];
    private static readonly byte[] s_spawnColour = [0x05, 0x03];

    private readonly BubbleBlow _blow;
    private readonly PlayerFrame _players;
    private readonly EnemyFrame _enemies;
    private readonly EnemyAiLoop _ai;
    private readonly EntityTimers _timers;
    private readonly EnemySpawner _spawner;
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
        _ai = new(Objects, Entities, new(Objects, Entities, new(Objects, Entities)), new(Objects), random, new(Objects, Entities));
        _timers = new(Objects, Entities);
        Pop = new(Objects, Entities, PlayerTable, food, Scores);
        _spawner = new(Entities, random);

        // $093F to $0946 and $098A: what a new game leaves for the first level's items.
        Items = new(Entities, Scores, new(PlayerTable, Entities), random)
        {
            FoodBase = (byte)((random.Next() & FirstFoodMask) + FirstFoodBase + (random.Carry ? 1 : 0)),
            Undying = OnePlayer,
        };
    }

    internal EntityTable Entities { get; } = new();

    internal ObjectTable Objects { get; } = new();

    internal PlayerTable PlayerTable { get; } = new();

    // $E90E's own record of what it drew, for the object layer - see BubblePop.Drawn.
    internal BubblePop Pop { get; }

    internal Scores Scores { get; } = new();

    internal LevelItems Items { get; }

    // $09DC to $0A05, for the level at `number`, counted from 1.
    internal void Start(Level level, IReadOnlyList<ZoneRect> zones, int number)
    {
        ArgumentNullException.ThrowIfNull(level);
        ArgumentNullException.ThrowIfNull(zones);

        // $09CD, just before $09DC: every sprite on.
        Entities.SpriteEnable = 0xFF;

        // $09DF, decompress_level_data.
        _map = SolidMap.Build(level, zones, Entities);

        // $392A, the level display, which walks to the level's list and spawns it.
        _spawner.Spawn(level.Enemies);

        // $09E5, $05C5.
        StartPlayers();

        // $09F1, $2B31.
        _subflg = (byte)(number - 1);

        if (_subflg != BonusLevel)
        {
            Items.Setup(level, _subflg);
        }

        // $09FD and $0A03.
        _passesToSecond = PassesPerSecond;
        _counter = FirstCounter;
    }

    // One pass: $0A07 to $0A57, then the IRQ's $1CBD.
    internal void Pass(ReadOnlySpan<byte> ports)
    {
        SolidMap map = _map ?? throw new InvalidOperationException("Start a level before the first pass.");

        Items.Update();
        _blow.Tick();
        Pop.Update();
        _ai.Update(map);
        _timers.Update(ReleasedState);
        Items.Collect(_subflg);

        // $06C6, once a second.
        if (--_passesToSecond == 0)
        {
            _passesToSecond = PassesPerSecond;
            Items.Tick();
        }

        // $1CBD: slots 7 to 2, then 1 and 0.
        _enemies.Step(_counter, map);
        _players.Step(ports, map);
        _counter += 2;
    }

    // $1CBD down to $267A, built once, innermost first.
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

    // $1CBD's enemy arm, with every handler behind it.
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

    // $04BB and $05C5, as far as they are translated.
    private void StartPlayers()
    {
        // $0620 and $062B. Every object slot back to free.
        Objects.Reset();

        for (int player = 0; player < PlayerTable.Capacity; player++)
        {
            Entities.State[player] = player < _playing ? PlayerFrame.PlayingState : (byte)0;
            Entities.X[player] = s_spawnX[player];
            Entities.Y[player] = SpawnY;
            Entities.Frame[player] = s_spawnFrame[player];
            Entities.Colour[player] = s_spawnColour[player];
            Entities.SpriteBase[player] = PlayerSpriteBase;

            // $04C4 to $04CB and $05F5: the counters a standing player reads as idle.
            Entities.RiseCounter[player] = 0xFF;
            Entities.FallCounter[player] = 0xFF;
            Entities.GroundState[player] = 0xFF;
            Entities.BubbleTimer[player] = 0xFF;

            // $0656. The rings last a level.
            PlayerTable.DriftRing[player] = 0;
            PlayerTable.WalkRing[player] = 0;
            PlayerTable.BlowRing[player] = 0;
        }
    }
}
