// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using BubbleBobbleSharpLib.Specials;

namespace BubbleBobbleSharpLib;

internal sealed class GameLoop
{
    private const int FramesPerPass = 2;

    private const byte FirstCounter = 0x01;

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
    private readonly BubbleSpawner _bubbles;
    private readonly ExtendBonus _extend;
    private readonly WorldEffects _world;
    private readonly PlayerRespawn _respawn;
    private readonly PlayerJoin _join;
    private readonly Queue<Song> _songs = new();
    private readonly int _playing;

    private SolidMap? _map;
    private byte _subflg;
    private int _overPasses;
    private byte _counter;
    private int _extendAwards;
    private int _respawns;
    private int _bonusRounds;
    private int _baronCalls;
    private bool _wasOver;

    internal GameLoop(BbRandom random, int playing)
    {
        ArgumentNullException.ThrowIfNull(random);

        _playing = playing;

        Flow = new(Timer);
        Water = new(Entities, random);
        Rings rings = new(PlayerTable, Scores);
        FoodDrop food = new(Entities, Scores, random);
        Food = food;

        _blow = new(PlayerTable, Entities, Objects, rings, Round);
        _players = BuildPlayers(Entities, _blow, rings, Water);
        _enemies = BuildEnemies(Entities, Objects, random, food, Flow, Water);
        _push = new(Entities, Objects);
        _death = new(Entities);
        Fire = new(Entities, food);
        _ai = BuildAi(Entities, Objects, random, Timer, Fire, food, Scores, Flow, Round);
        _timers = new(Objects, Entities);
        Pop = new(Objects, Entities, PlayerTable, food, Scores, Water);
        _spawner = new(Entities, random);
        _bubbles = new(Objects, Entities, random);

        ItemEffects effects = new(PlayerTable, Entities);

        Items = new(Entities, Scores, effects, random)
        {
            FoodBase = (byte)((random.Next() & FirstFoodMask) + FirstFoodBase + (random.Carry ? 1 : 0)),
            Undying = OnePlayer,
        };

        _respawn = new(Entities, PlayerTable, Items, Timer, Round);
        _join = new(Entities, PlayerTable, Scores);
        Clear = new(Timer, Entities, Objects, Items, random);
        Clear.SuperBonus = new(Entities, Scores, Clear);
        _enemies.SuperBonus = Clear.SuperBonus;
        End = new(Timer, Entities, Objects, Pop, Items, random, Round);
        Boss = new(Entities, Objects, PlayerTable, Items, Round, Timer, End);
        _respawn.Boss = Boss;
        Clear.ReadyEnded = AppearBoss;
        Bonus = new(Entities, Round, End.SavedFlash);
        _extend = new(PlayerTable, End);
        _world = new(Clear, Flow, Timer, Entities, Objects, food, Items, Bonus);
        effects.World = _world.Apply;
        Room = new(Entities, Scores, Timer, Items, _world.ResetObjects);
        _world.Room = StartRoom;

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

    internal WaterFlow Water { get; }

    internal FireStrips Fire { get; }

    internal Scores Scores { get; } = new();

    internal FoodDrop Food { get; }

    internal ItemRoom Room { get; }

    internal BossFight Boss { get; }

    internal BonusRound Bonus { get; }

    internal RoundState Round { get; } = new();

    internal LevelItems Items { get; }

    internal LevelTimer Timer { get; } = new();

    internal LevelFlow Flow { get; }

    internal LevelClear Clear { get; }

    internal LevelEnd End { get; }

    // $0A6A: the level's clock has run out on a cleared level, and the game goes on to the next.
    internal bool IsComplete => End.Complete != 0;

    // The next song the game has asked for ($F477's music_start), or none. The song of a level's start is the first the
    // game asks for: $08E7 for the first level, and $05C5 for the others.
    internal Song? TakeSong() => _songs.TryDequeue(out Song song) ? song : null;

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

    // $7FB2-$7FDD: an item that skips levels has taken the game on. Returns the number of the level to start, having set
    // up the levels between only as far as the bonus rounds' schedule ($0688) goes, or none.
    internal int? SkipLevel()
    {
        if (_world.SkipTo is not int target)
        {
            return null;
        }

        _world.SkipTo = null;

        // `$7FD4`-`$7FEE` counts the level up until it is the target, a byte at a time, so a target below the level goes the long way.
        for (int level = (_subflg + 1) & 0xFF; level != target; level = (level + 1) & 0xFF)
        {
            Clear.Begin((byte)level);
        }

        _subflg = (byte)target;

        return target + 1;
    }

    // $09DC-$0A05. A level that follows another ($0A96) keeps the players it has in the game.
    internal void Start(Level level, IReadOnlyList<ZoneRect> zones, int number, bool next = false)
    {
        ArgumentNullException.ThrowIfNull(level);
        ArgumentNullException.ThrowIfNull(zones);

        Entities.SpriteEnable = 0xFF;

        _map = SolidMap.Build(level, zones, Entities);

        _spawner.Spawn(level.Enemies);
        _bubbles.Configure(level.SpawnFlags);

        StartPlayers(next);

        _subflg = (byte)(number - 1);
        _world.Level = _subflg;
        _world.Cleared = false;
        _world.SkipTo = null;
        PlayerTable.Shots.Fill(0xFF);

        Items.Setup(level, _subflg);
        Boss.Begin(_subflg);

        _enemies.Enter();

        Timer.Begin(_subflg);
        Flow.Begin(level.Colours);
        Clear.Begin(_subflg);
        End.Begin();
        Round.Begin();
        _counter = FirstCounter;
        _overPasses = 0;
        _wasOver = false;
        _songs.Enqueue(next ? LevelSong() : Song.Theme);
    }

    // Which block of item art each slot is drawn from, for an enemy that has become food ($2921 builds its sprite from
    // it at run time), or -1: a food past the end of the art is not drawn.
    internal int[] FoodArt()
    {
        int[] art = new int[EntityTable.Capacity];

        for (int slot = 0; slot < art.Length; slot++)
        {
            byte food = Entities.State[slot] == FoodDrop.FoodState ? Food.Number(slot) : byte.MaxValue;

            art[slot] = food < LevelItems.FoodKinds ? LevelItems.FoodArt(food) : -1;
        }

        return art;
    }

    internal void Pass(ReadOnlySpan<byte> ports)
    {
        SolidMap map = _map ?? throw new InvalidOperationException("Start a level before the first pass.");

        // $0A99: the pause flag ($37) stops the players and the game waits. The tune (step 8) is not modelled.
        if (IsOver)
        {
            if (!_wasOver)
            {
                _wasOver = true;
                _songs.Enqueue(Song.GameOver);
            }

            _overPasses++;
            return;
        }

        if (Room.Active)
        {
            RoomPass(ports, map);
            return;
        }

        Items.Update();

        byte hurry = Timer.Hurry;

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

        NoteSongs(hurry);

        // $0A19: the fire on the platforms burns.
        Fire.Update(Pop.Counter);

        _blow.Tick();

        if (_subflg == BossFight.Level)
        {
            Boss.Hit();
        }
        else
        {
            _bubbles.Update(map);
        }

        Pop.FruitArt = LevelItems.FoodArt(Clear.BonusType);
        Pop.Update();
        _push.Update(map);
        _ai.Update(map);
        _timers.Update(Round.Release);
        Items.Collect(_subflg);
        _extend.Update();
        _respawn.Update(_subflg);
        NoteLives();
        _join.Update(ports);

        for (int frame = 0; frame < FramesPerPass; frame++)
        {
            if (Timer.Advance())
            {
                Items.Tick();
            }
        }

        // $06AB: the IRQ moves the water before $1CBD moves anything that is in it.
        if (Water.Mask != 0)
        {
            Water.Update(map);
        }

        _enemies.Step(_counter, map);
        _players.Step(ports, _counter, map);

        if (Boss.Active)
        {
            Boss.Move();
        }

        _death.Update();
        _counter += 2;
    }

    private static PlayerFrame BuildPlayers(EntityTable entities, BubbleBlow blow, Rings rings, WaterFlow water)
    {
        PlayerSteer steer = new(entities, blow);
        PlayerDrift drift = new(entities, steer, rings);
        PlayerDescent descent = new(entities);

        return new PlayerFrame(
            entities,
            new PlayerMovement(entities, blow, rings),
            new PlayerJump(entities, drift, new PlayerLanding(entities, descent)),
            new PlayerFall(entities, steer),
            blow,
            water);
    }

    private static EnemyAiLoop BuildAi(
        EntityTable entities,
        ObjectTable objects,
        BbRandom random,
        LevelTimer timer,
        FireStrips fire,
        FoodDrop food,
        Scores scores,
        LevelFlow flow,
        RoundState round)
    {
        BubbleCollision collision = new(objects, entities);

        return new(
            objects,
            entities,
            new(objects, entities, collision, round),
            new(objects),
            random,
            new(objects, entities, timer),
            new(objects, entities, random, fire, collision, food, scores),
            flow);
    }

    private static EnemyFrame BuildEnemies(
        EntityTable entities,
        ObjectTable objects,
        BbRandom random,
        FoodDrop food,
        LevelFlow flow,
        WaterFlow water)
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
            flow,
            water);
    }

    // The item's routine waits for the room, and the IRQ goes on without it: the clock, and the players when it is
    // their turn. A pass is two frames of the room's loop.
    private void RoomPass(ReadOnlySpan<byte> ports, SolidMap map)
    {
        for (int frame = 0; frame < FramesPerPass; frame++)
        {
            Room.Frame();

            if (Timer.Advance())
            {
                Items.Tick();
            }
        }

        _players.Step(ports, _counter, map);
        _death.Update();
        _counter += 2;
    }

    // `$A428`, `$A45B`: the room clears the level's screen, of the bubbles and the water as well as what is loose in it.
    private void StartRoom(int type)
    {
        SolidMap map = _map ?? throw new InvalidOperationException("Start a level before the first pass.");

        Room.Begin(type, _subflg, map);
        Pop.Drawn.Fill(BubblePop.NotDrawn);
        Water.Mask = 0;
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

            // $E56D-$E58F: only a player in the game is brought to its starting place; the other stays where it was, which
            // is (0, 0) ($0986, $0988, $04F0), off the screen.
            bool inGame = Entities.State[player] != 0;

            Entities.X[player] = inGame ? PlayerRespawn.SpawnX[player] : (byte)0;
            Entities.Y[player] = inGame ? PlayerRespawn.SpawnY : (byte)0;
            Entities.Frame[player] = PlayerRespawn.SpawnFrame[player];
            Entities.Colour[player] = LevelEnd.SpawnColour[player];
            Entities.SpriteBase[player] = PlayerSpriteBase;

            PlayerTable.DriftRing[player] = 0;
            PlayerTable.WalkRing[player] = 0;
            PlayerTable.BlowRing[player] = 0;
        }
    }

    // $05C5 and $0491: a level's tune, and the one that comes back after a death; the last level has its own.
    // $16C1: the READY!! has gone, and on the last level the giant comes.
    private void AppearBoss()
    {
        if (_subflg == BossFight.Level)
        {
            Boss.Appear();
        }
    }

    private Song LevelSong() => _subflg == BonusLevel ? Song.FinalTheme : Song.Resume;

    // The tunes that go with the clock running out ($15E8, $3517) and with EXTEND ($32C1).
    private void NoteSongs(byte hurry)
    {
        if (hurry == 0 && Timer.Hurry == 1)
        {
            _songs.Enqueue(Song.HurryUp);
        }
        else if ((sbyte)hurry >= 0 && (sbyte)Timer.Hurry < 0)
        {
            _songs.Enqueue(Song.LevelComplete);
        }
    }

    private void NoteLives()
    {
        if (_extend.Awards != _extendAwards)
        {
            _extendAwards = _extend.Awards;
            _songs.Enqueue(Song.Extend);
        }

        if (_baronCalls != End.BaronCalls)
        {
            _baronCalls = End.BaronCalls;
            _songs.Enqueue(Song.RoundStart);
        }

        if (_bonusRounds != Bonus.Rounds)
        {
            _bonusRounds = Bonus.Rounds;
            _songs.Enqueue(Song.BonusRound);
        }

        if (_respawns != _respawn.Respawns)
        {
            _respawns = _respawn.Respawns;
            _songs.Enqueue(LevelSong());
        }
    }
}
