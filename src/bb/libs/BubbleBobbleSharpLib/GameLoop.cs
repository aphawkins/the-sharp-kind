// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Diagnostics.CodeAnalysis;
using BubbleBobbleSharp.Abstractions.Views;
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
    private const int TicksPerCycle = 3;
    private const int IrqOnlyPhase = 2;

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
    private readonly PlayerRespawn _respawn;
    private readonly PlayerJoin _join;
    private readonly Queue<Song> _songs = new();
    private readonly int _playing;

    private SolidMap? _map;
    private byte _subflg;
    private int _overPasses;
    private byte _counter;
    private int _phase;
    private int _extendAwards;
    private int _respawns;
    private int _bonusRounds;
    private int _baronCalls;
    private bool _wasOver;

    // $5C3F: the last song the game started, which $05C5 looks at.
    private Song? _song;

    // The wait a routine of the game's is in, and where the pass that called it goes on from once it is over.
    private IGameHold? _hold;
    private Resume _resume;

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
        Ending = BuildEnding();
        _respawn.Boss = Boss;
        Clear.ReadyEnded = AppearBoss;
        Bonus = new(Entities, Round, End.SavedFlash);
        World = BuildWorld(random, food);
        _extend = new(PlayerTable, End, World.ResetObjects, random);
        effects.World = World.Apply;
        Room = new(Entities, Scores, Timer, Items, World.ResetObjects);
        World.Room = StartRoom;
        ConnectSuperRoom();

        // $0956-$0977.
        PlayerTable.Lives.Fill(StartingLives);
        PlayerTable.Credits = playing == PlayerTable.Capacity ? TwoPlayerCredits : (byte)(TwoPlayerCredits + 1);
    }

    private enum Resume
    {
        Start = 0,
        AfterGiant = 1,
        AfterCollect = 2,
        AfterCheck = 3,
        AfterExtend = 4,
    }

    internal EntityTable Entities { get; } = new();

    // $0A64: both players out of the game.
    internal bool IsOver => (Entities.Slot(0).State | Entities.Slot(1).State) == 0;

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

    internal SuperBonusRoom SuperRoom { get; private set; }

    internal BossFight Boss { get; }

    internal GameEnding Ending { get; }

    internal BonusRound Bonus { get; }

    internal GiantBubble Giant { get; private set; }

    internal SpriteShow Show { get; private set; }

    // The colour a routine of the game's has put in the border and the background while it waits, or none.
    internal int? Border => _hold?.Border;

    // The HURRY UP! scroll's text, while it is on the screen.
    internal HurryText? Hurry => End.Hurrying is { Row: int row } scroll ? new(row, scroll.ColourRam, scroll.Backed) : null;

    // The special item of type 8's wipe: how many rows of the level, from the bottom, and whether it is still going up.
    internal int WipedRows => World.Wipe.Wiped;

    internal bool Wiping => World.Wipe.Wiping;

    internal RoundState Round { get; } = new();

    internal LevelItems Items { get; }

    internal LevelTimer Timer { get; } = new();

    internal LevelFlow Flow { get; }

    internal LevelClear Clear { get; }

    internal LevelEnd End { get; }

    // The effects that act on the level, such as an item's skip to a later one.
    internal WorldEffects World { get; }

    // $0A6A: the level's clock has run out on a cleared level, and the game goes on to the next.
    internal bool IsComplete => End.Complete != 0;

    // $F1AC. Made on first use: the constructor is as coupled as the analyser lets it be.
    private ExtraLives ExtraLives => field ??= new(PlayerTable, Scores);

    // The next song the game has asked for ($F477's music_start), or none. The song of a level's start is the first the
    // game asks for: $08E7 for the first level, and $05C5 for the others when the level's tune is not already playing.
    internal Song? TakeSong() => _songs.TryDequeue(out Song song) ? song : null;

    // $0A6E-$0A8B: the next level's number is kept for each player in the game, as the round they have reached.
    // Level 100's end is $A5B7's ending, which starts and returns nothing. Otherwise returns the number of the level to start.
    internal int? EndLevel()
    {
        _subflg++;

        for (int player = 0; player < PlayerTable.Capacity; player++)
        {
            if (Entities.Slot(player).State != 0)
            {
                PlayerTable.Round[player] = _subflg;
            }
        }

        if (_subflg == LastLevel)
        {
            Ending.Begin();
            return null;
        }

        return _subflg + 1;
    }

    // $7FB2-$7FDD: an item that skips levels has taken the game on. Returns the number of the level to start, having set
    // up the levels between only as far as the bonus rounds' schedule ($0688) goes, or none.
    internal int? SkipLevel()
    {
        if (World.SkipTo is not int target)
        {
            return null;
        }

        World.SkipTo = null;

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
        World.Level = _subflg;
        World.Cleared = false;
        World.SkipTo = null;
        World.Wipe.Reset();
        PlayerTable.Shots.Fill(0xFF);

        Items.Setup(level, _subflg);
        Boss.Begin(_subflg);
        Giant.Reset();

        _enemies.Enter();

        Timer.Begin(_subflg);
        Flow.Begin(level.Colours);
        Clear.Begin(_subflg);
        End.Begin();
        Round.Begin();
        _counter = FirstCounter;
        _overPasses = 0;
        _wasOver = false;
        if (!next)
        {
            Ask(Song.Theme);
        }
        else if (StartsLevelSong())
        {
            Ask(LevelSong());
        }
    }

    // Which block of item art each slot is drawn from, for an enemy that has become food ($2921 builds its sprite from
    // it at run time), or -1: a food past the end of the art is not drawn.
    internal int[] FoodArt()
    {
        int[] art = new int[EntityTable.Capacity];

        for (int slot = 0; slot < art.Length; slot++)
        {
            byte food = Entities.Slot(slot).State == FoodDrop.FoodState ? Food.Number(slot) : byte.MaxValue;

            art[slot] = food < LevelItems.FoodKinds ? LevelItems.FoodArt(food) : -1;
        }

        return art;
    }

    // The sidebar with the EXTEND bonus's rolled letters in its player's places while they are, and then none.
    internal SidebarModel Rolled(SidebarModel sidebar)
    {
        ArgumentNullException.ThrowIfNull(sidebar);

        return _extend.Running switch
        {
            { Roll: IReadOnlyList<int> roll, Player: 0 } => sidebar with { PlayerOneRoll = roll },
            { Roll: IReadOnlyList<int> roll } => sidebar with { PlayerTwoRoll = roll },
            { Unlettered: true, Player: 0 } => sidebar with { PlayerOneLetters = 0 },
            { Unlettered: true } => sidebar with { PlayerTwoLetters = 0 },
            _ => sidebar,
        };
    }

    // The EXTEND bonus's screen in place of the level, or none.
    internal ExtendPicture? ExtendShown(PlayfieldModel playfield)
    {
        ArgumentNullException.ThrowIfNull(playfield);

        return _extend.Picture(playfield.Tile, playfield.Colours);
    }

    // The level with the HURRY UP! text on it as it is now.
    internal PlayfieldModel Hurried(PlayfieldModel playfield)
    {
        ArgumentNullException.ThrowIfNull(playfield);

        return playfield.WithHurry(Hurry);
    }

    // One tick of the host's clock, which is two IRQ frames. The main loop takes three frames a pass (measured in VICE, level 1,
    // 260 passes), so the passes come two ticks in three and the third tick is the IRQ alone: the players and enemies keep
    // $1CBD's 25 a second while what the main loop counts runs at the 16.7 a second it has on the C64. Rooms, waits and the
    // game over have loops of their own and take every tick as a pass.
    internal void Tick(ReadOnlySpan<byte> ports)
    {
        if (IsOver || Room.Active || SuperRoom.Active || _hold is not null || _map is not SolidMap map)
        {
            Pass(ports);
            return;
        }

        _phase = (_phase + 1) % TicksPerCycle;

        if (_phase == IrqOnlyPhase)
        {
            Irq(ports, map);
            return;
        }

        Pass(ports);
    }

    internal void Pass(ReadOnlySpan<byte> ports)
    {
        SolidMap map = _map ?? throw new InvalidOperationException("Start a level before the first pass.");

        // $0A99: the pause flag ($37) stops the players and the game waits, to the game over's song.
        if (IsOver)
        {
            if (!_wasOver)
            {
                _wasOver = true;
                Ask(Song.GameOver);
            }

            _overPasses++;
            return;
        }

        if (Room.Active)
        {
            RoomPass(ports, map);
            return;
        }

        if (SuperRoom.Active)
        {
            SuperRoomPass(ports, map);
            return;
        }

        if (_hold is not null)
        {
            HoldPass(ports, map);
            return;
        }

        Run(ports, map, Resume.Start);
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

    // The pass from where it is to go on from: its start, or after a routine that waited.
    private void Run(ReadOnlySpan<byte> ports, SolidMap map, Resume from)
    {
        if (from == Resume.Start)
        {
            // $0A10: a score that has passed a mark earns a life, before the pass's first update.
            ExtraLives.Update();
        }

        if (from == Resume.Start && Check())
        {
            return;
        }

        if (from == Resume.AfterExtend)
        {
            Tail(ports, map);
            return;
        }

        if (from is Resume.Start or Resume.AfterCheck)
        {
            Head(map);

            // $0A3B: a sweep of the giant bubble starts, with the pause flag up.
            if (Giant.Begin())
            {
                Wait(Giant, Resume.AfterGiant);
                return;
            }
        }

        if (from != Resume.AfterCollect)
        {
            Pop.FruitArt = LevelItems.FoodArt(Clear.BonusType);
            Pop.Update();
            _push.Update(map);
            _ai.Update(map);
            _timers.Update(Round.Release);
            Items.Collect(_subflg);

            // `$3621` never comes back to the pass: its end pops it (`$37BB`).
            if (SuperRoom.Active)
            {
                return;
            }

            if (World.Hold is IGameHold hold)
            {
                World.Hold = null;
                Wait(hold, Resume.AfterCollect);
                return;
            }
        }

        Tail(ports, map);
    }

    // The pass's start, to the level's check. Returns whether it is waiting on the hurry-up or the Barons' call.
    private bool Check()
    {
        Items.Update();

        // $15E1: with time on the clock the level is checked for its last enemy.
        if (!Flow.Update())
        {
            return false;
        }

        if (Timer.Seconds != 0)
        {
            Clear.Update();
            return false;
        }

        End.Update();

        // $3AB8 and $1621: the hurry-up and the Barons' call wait in themselves, the call with the round start's tune asked for
        // before it does.
        if (End.Hold is not IGameHold hold)
        {
            return false;
        }

        NoteLives();
        End.Hold = null;
        Wait(hold, Resume.AfterCheck);
        return true;
    }

    private void Head(SolidMap map)
    {
        // $0A19: the fire on the platforms burns.
        Fire.Update(Pop.Counter);

        _blow.Tick();

        // $198A: the giant bubble's sweep is in the render, which is the first thing a pass does.
        Giant.Sweep();

        if (_subflg == BossFight.Level)
        {
            Boss.Hit();
        }
        else
        {
            _bubbles.Update(map);
        }
    }

    private void Tail(ReadOnlySpan<byte> ports, SolidMap map)
    {
        // $0A57: the EXTEND bonus waits in itself, and is looked at again for the other player once it is over.
        _extend.Update();

        if (_extend.Hold is IGameHold hold)
        {
            _extend.Hold = null;
            NoteLives();
            Wait(hold, Resume.AfterExtend);
            return;
        }

        _respawn.Update(_subflg);
        NoteLives();
        _join.Update(ports);

        Irq(ports, map);
        _death.Update();
    }

    // What the IRQ does in the two frames of a tick: the level's clock, the water, and the enemies and players ($1CBD).
    private void Irq(ReadOnlySpan<byte> ports, SolidMap map)
    {
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

        _counter += 2;
    }

    private void Wait(IGameHold hold, Resume resume)
    {
        _hold = hold;
        _resume = resume;
    }

    // Two frames of a routine's wait, with the IRQ's work: the clock and its counter, and, unless the game is paused, what
    // the IRQ moves (`$1CBD`) and the routine's own look at the players (`$045C`). Once the routine is over, the pass that
    // called it goes on.
    private void HoldPass(ReadOnlySpan<byte> ports, SolidMap map)
    {
        IGameHold hold = _hold!;
        bool waiting = true;

        for (int frame = 0; frame < FramesPerPass && waiting; frame++)
        {
            waiting = hold.Frame();

            if (waiting)
            {
                if (Timer.Advance())
                {
                    Items.Tick();
                }

                _counter++;
            }
        }

        if (!hold.Paused)
        {
            if (Water.Mask != 0)
            {
                Water.Update(map);
            }

            _enemies.Step(_counter, map);
            _players.Step(ports, _counter, map);
            _death.Update();
            _respawn.Update(_subflg);
            NoteLives();
            _join.Update(ports);
        }

        hold.Pass();

        if (waiting)
        {
            return;
        }

        _hold = null;
        _counter = hold.Finish(_counter);
        Run(ports, map, _resume);
    }

    // The effects that act on the level, with the giant bubble the effect of type 7 orders and a cleared level cancels.
    [MemberNotNull(nameof(Giant), nameof(Show))]
    private WorldEffects BuildWorld(BbRandom random, FoodDrop food)
    {
        Giant = new(Entities, Objects, Pop, food, random);
        Clear.Giant = Giant;
        End.Giant = Giant;
        End.Play = Ask;
        Show = new(Entities, Timer);
        _enemies.Show = Show;

        return new(Clear, Flow, Timer, Entities, Objects, food, Items, Fire, Water, Show, Bonus, Giant);
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

    [MemberNotNull(nameof(SuperRoom))]
    private void ConnectSuperRoom()
    {
        SuperRoom = new(Entities, Scores, Timer, Items, Flow, World.ResetObjects);
        World.SuperRoom = StartSuperRoom;
    }

    // `$3621`: the room is the level's map until the level ends, SUBFLG has been counted on (`$3690`), and the song is its own.
    private void StartSuperRoom()
    {
        SuperRoom.Begin();
        Boss.SuperRoom();
        _map = SolidMap.Build(SuperBonusRoom.Map(_subflg + 1), [], Entities);
        Pop.Drawn.Fill(BubblePop.NotDrawn);
        Water.Mask = 0;
        _subflg++;
        World.Level = _subflg;
        Ask(Song.SuperBonus);
    }

    // As a bonus room's pass. The room's end is the level's (`$37C1`), with the level's tune back.
    private void SuperRoomPass(ReadOnlySpan<byte> ports, SolidMap map)
    {
        for (int frame = 0; frame < FramesPerPass; frame++)
        {
            if (SuperRoom.Frame())
            {
                End.Skip();
                Ask(Song.Resume);
                return;
            }
        }

        _players.Step(ports, _counter, map);
        _death.Update();
        _counter += 2;
    }

    private void StartPlayers(bool next)
    {
        Objects.Reset();

        for (int slot = 0; slot < EntityTable.Capacity; slot++)
        {
            Entities.Slot(slot).RiseCounter = 0xFF;
            Entities.Slot(slot).FallCounter = 0xFF;
            Entities.Slot(slot).GroundState = 0xFF;
            Entities.Slot(slot).BubbleTimer = 0xFF;
            Entities.Slot(slot).LeapFlag = 0;
            Entities.Slot(slot).ClimbFlag = 0;
            Entities.Slot(slot).ClimbNext = false;

            if (slot >= PlayerTable.Capacity)
            {
                Entities.Slot(slot).FlashTimer = 0;
            }
        }

        for (int player = 0; player < PlayerTable.Capacity; player++)
        {
            if (!next)
            {
                Entities.Slot(player).State = player < _playing ? PlayerFrame.PlayingState : (byte)0;
            }

            // $E56D-$E58F: only a player in the game is brought to its starting place; the other stays where it was, which
            // is (0, 0) ($0986, $0988, $04F0), off the screen.
            bool inGame = Entities.Slot(player).State != 0;

            Entities.Slot(player).X = inGame ? PlayerRespawn.SpawnX[player] : (byte)0;
            Entities.Slot(player).Y = inGame ? PlayerRespawn.SpawnY : (byte)0;
            Entities.Slot(player).Frame = PlayerRespawn.SpawnFrame[player];
            Entities.Slot(player).Colour = LevelEnd.SpawnColour[player];
            Entities.Slot(player).SpriteBase = PlayerSpriteBase;

            PlayerTable.DriftRing[player] = 0;
            PlayerTable.WalkRing[player] = 0;
            PlayerTable.BlowRing[player] = 0;
        }
    }

    // $05C5 and $0491: a level's tune, and the one that comes back after a death; the last level has its own.
    // $16C1: the READY!! has gone, and on the last level the giant comes.
    private GameEnding BuildEnding() => new(Entities, Scores, Boss, Ask);

    private void AppearBoss()
    {
        if (_subflg == BossFight.Level)
        {
            Boss.Appear();
        }
    }

    // $05AD: a song is started, and kept in $5C3F.
    private void Ask(Song song)
    {
        _song = song;
        _songs.Enqueue(song);
    }

    // $05DD-$05F0: the level's tune is started again unless the theme, its resume or the super bonus's tune is what is
    // playing (below the final theme in the song table, or the super bonus's); level 99's always is.
    private bool StartsLevelSong()
        => _subflg == BonusLevel || _song is not (Song.Theme or Song.Resume or Song.SuperBonus);

    // $04A0-$04B8: a death resumes the level's tune only over a song from the game over's on in the song table, and not
    // over the super bonus's; the level's own tunes and the bonus round's play on.
    private bool ResumesOnDeath() => _song is >= Song.GameOver and not Song.SuperBonus;

    private Song LevelSong() => _subflg == BonusLevel ? Song.FinalTheme : Song.Resume;

    private void NoteLives()
    {
        if (_extend.Awards != _extendAwards)
        {
            _extendAwards = _extend.Awards;
            Ask(Song.Extend);
        }

        if (_baronCalls != End.BaronCalls)
        {
            _baronCalls = End.BaronCalls;
            Ask(Song.RoundStart);
        }

        if (_bonusRounds != Bonus.Rounds)
        {
            _bonusRounds = Bonus.Rounds;
            Ask(Song.BonusRound);
        }

        if (_respawns != _respawn.Respawns)
        {
            _respawns = _respawn.Respawns;

            if (ResumesOnDeath())
            {
                Ask(Song.Resume);
            }
        }
    }
}
