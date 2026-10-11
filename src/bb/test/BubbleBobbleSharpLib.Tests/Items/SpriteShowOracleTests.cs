// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using BubbleBobbleSharpLib.Tests.Enemies;
using BubbleBobbleSharpLib.Tests.Levels;
using BubbleBobbleSharpLib.Tests.Oracle;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Items;

// The sprites' show of the special items of types 11 (`$2ECC`, the diamonds) and 29 (`$35B0`, the giant), against the real
// game run on SharpKind.Emulation.
//
// **The routine is called directly** (`Invoke`, an intervention) some frames into level 1, when the enemies are out, with
// `$E494`, the wait for a frame, made an `rts` (a call with interrupts off would wait for ever): its flashes take no frames, so `$08` is
// as it was. The port's effect runs on the bytes before, its six frames of flashes are run, and the slots, the objects, the
// sprites `$F4`-`$F9` (`$7D00`), what is kept (`$4700`), `$2A` and `$08` are compared with what the game left. Then the game
// runs on, and each turn of a slot in state `$13` or `$14` is bracketed (`$1CDB` to `$1D24`) and worked by the port from
// the bytes before, with `$D017`, until the slots are handed back.
[Trait("Level", "Integration")]
public sealed class SpriteShowOracleTests
{
    private const int Diamonds = 0x2ECC;
    private const int Giant = 0x35B0;
    private const int WaitFrame = 0xE494;

    private const int Counter = 0x08;
    private const int Random = 0x26;
    private const int Seconds = 0x2A;
    private const int Pictures = 0x7D00;
    private const int Kept = 0x4700;
    private const int Expand = 0x17;
    private const int SpriteEnable = 0x15;

    private const int SlotStart = 0x1CDB;
    private const int SlotEnd = 0x1D24;

    private static readonly LevelStore s_levels = LevelStore.Read(Asset("levels.json"));
    private static readonly ZoneStore s_zones = ZoneStore.Read(Asset("zones.json"));

    [Theory]
    [InlineData(11, 20)]
    [InlineData(11, 150)]
    [InlineData(11, 400)]
    [InlineData(29, 20)]
    [InlineData(29, 150)]
    [InlineData(29, 400)]
    public void StartsTheShowExactlyAsTheGameDoes(int type, int frames)
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        Rebb64Machine machine = new();
        machine.StartOnePlayer();
        machine.RunFrames(frames);

        byte enabled = machine.PeekVic(SpriteEnable);
        Capture capture = Start(machine, type);

        EntityTable entities = new();
        RamImage.Load(capture.Before, entities);
        entities.SpriteEnable = enabled;
        ObjectTable objects = new();
        RamImage.Load(capture.Before, objects);
        LevelTimer timer = new() { Seconds = capture.Before[Seconds] };
        SpriteShow show = new(entities, timer);

        WorldEffects world = World(capture, entities, objects, timer, show);
        world.Apply(type, 0);

        IGameHold hold = world.Hold ?? throw new InvalidOperationException("no hold");
        int waited = 0;

        while (hold.Frame())
        {
            waited++;
        }

        byte counter = hold.Finish(capture.Before[Counter]);

        Assert.Equal(18, waited);
        Assert.True(hold.Paused);

        string expected = RamImage.Entities(capture.After);
        string actual = RamImage.Entities(entities);

        Assert.True(expected == actual, RamImage.EntityDifference(expected, actual));
        Assert.Equal(RamImage.Objects(capture.After), RamImage.Objects(objects));
        Assert.Equal(machine.PeekVic(SpriteEnable), entities.SpriteEnable);
        Assert.Equal(capture.After.AsSpan(Pictures, show.Pictures.Length).ToArray(), show.Pictures.ToArray());
        Assert.Equal(capture.After.AsSpan(Kept, 6).ToArray(), show.KeptState.ToArray());
        Assert.Equal(capture.After.AsSpan(Kept + 0x10, 6).ToArray(), show.KeptX.ToArray());
        Assert.Equal(capture.After.AsSpan(Kept + 0x20, 6).ToArray(), show.KeptY.ToArray());
        Assert.Equal(capture.After.AsSpan(Kept + 0x30, 6).ToArray(), show.KeptBase.ToArray());
        Assert.Equal(capture.After[Seconds], timer.Seconds);
        Assert.Equal(capture.After[Counter], counter);
    }

    [Theory]
    [InlineData(11, 150)]
    [InlineData(11, 400)]
    [InlineData(29, 150)]
    [InlineData(29, 400)]
    public void RunsTheShowExactlyAsTheGameDoes(int type, int frames)
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        SolidMap map = SolidMap.Build(s_levels.Level(1), s_zones.Zones(1));
        Rebb64Machine machine = new();
        machine.StartOnePlayer();
        machine.RunFrames(frames);
        Start(machine, type);

        int turns = 0;
        int swells = 0;
        bool handedBack = false;

        for (int turn = 0; turn < 2000 && !handedBack; turn++)
        {
            int slot = 7 - (turn % 6);

            if (!InShow(machine, slot))
            {
                continue;
            }

            byte expand = 0;
            Capture capture = machine.Bracket(
                SlotStart,
                SlotEnd,
                300_000,
                m => expand = m.PeekVic(Expand),
                m => m.Cpu.X == slot && InShow(m, slot));

            Compare(capture, slot, expand, machine.PeekVic(Expand), map, $"turn {turn}, slot {slot}");

            turns++;
            swells += expand != machine.PeekVic(Expand) ? 1 : 0;
            handedBack = !InShow(machine, slot);
        }

        Assert.True(handedBack, $"not handed back after {turns} turns");
        Assert.True(type == 11 || swells > 0, $"{swells} swells");
    }

    private static bool InShow(Rebb64Machine machine, int slot)
        => machine.Peek(0xB2 + slot) is SpriteShow.DiamondState or SpriteShow.GiantState;

    // A turn the port takes from the bytes before, against the bytes the game left.
    private static void Compare(Capture capture, int slot, byte expand, byte expanded, SolidMap map, string context)
    {
        EntityTable entities = new();
        RamImage.Load(capture.Before, entities);
        entities.SpriteExpand = expand;

        SpriteShow show = new(entities, new LevelTimer());
        Load(capture.Before, show);

        BbRandom random = Rng(capture);
        LevelFlow flow = new(new LevelTimer());
        EnemyFrame frame = TestEnemies.Frame(entities, new ObjectTable(), random, new Scores(), flow);
        frame.Show = show;
        frame.Step(slot, capture.Before[Counter], map);

        string expected = RamImage.Entities(capture.After);
        string actual = RamImage.Entities(entities);

        Assert.True(expected == actual, $"{context}: {RamImage.EntityDifference(expected, actual)}");
        Assert.True(expanded == entities.SpriteExpand, $"{context}: $D017 {expanded:X2}, not {entities.SpriteExpand:X2}");
        Assert.True(
            capture.After.AsSpan(Pictures, show.Pictures.Length).SequenceEqual(show.Pictures),
            $"{context}: the pictures differ");
    }

    private static void Load(byte[] ram, SpriteShow show)
    {
        ram.AsSpan(Pictures, show.Pictures.Length).CopyTo(show.Pictures);
        ram.AsSpan(Kept, 6).CopyTo(show.KeptState);
        ram.AsSpan(Kept + 0x10, 6).CopyTo(show.KeptX);
        ram.AsSpan(Kept + 0x20, 6).CopyTo(show.KeptY);
        ram.AsSpan(Kept + 0x30, 6).CopyTo(show.KeptBase);
    }

    private static WorldEffects World(Capture capture, EntityTable entities, ObjectTable objects, LevelTimer timer, SpriteShow show)
    {
        BbRandom random = Rng(capture);
        LevelFlow flow = new(timer);
        LevelItems items = new(entities, new Scores(), new ItemEffects(new PlayerTable(), entities), random);
        LevelClear clear = new(timer, entities, objects, items, random);
        FoodDrop food = new(entities, new Scores(), random);

        return new(clear, flow, timer, entities, objects, food, items, new(entities, food), new(entities, random), show);
    }

    private static BbRandom Rng(Capture capture) => new(new ScriptedRandomSource(capture.Timer.Select(b => (int)b)))
    {
        Low = capture.Before[Random],
        High = capture.Before[Random + 1],
    };

    // `$E494` is made an `rts` for the length of the effect, as an intervention.
    private static Capture Start(Rebb64Machine machine, int type)
    {
        byte wait = machine.Peek(WaitFrame);

        machine.Poke(WaitFrame, 0x60);

        Capture capture = machine.Invoke(type == 11 ? Diamonds : Giant, 0, 0, 1_000_000);

        machine.Poke(WaitFrame, wait);

        return capture;
    }

    private static string Asset(string name) => Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        name);
}
