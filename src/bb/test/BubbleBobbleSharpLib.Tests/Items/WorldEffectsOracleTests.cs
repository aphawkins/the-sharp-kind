// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using BubbleBobbleSharpLib.Tests.Levels;
using BubbleBobbleSharpLib.Tests.Oracle;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Items;

// The special-item effects that set a few bytes, against the real game run on SharpKind.Emulation.
//
// **Each effect's routine is called directly** (`Invoke`, an intervention) at the first pass of level 1, with X the player,
// as `$2D51`'s `jsr` would. The effect then runs on the bytes before in the port, and the bytes it can write are compared:
// `$68`, `$69` and `$6A` (the bonus round scheduled), `$1C`, `$1E`, `$67` and `$2B` (the freeze), and the player's blow
// state (`$A77B` to `$A784`). The effects that wait for frames have oracle tests of their own (`ScreenWipeOracleTests`,
// `SpriteShowOracleTests`); the skips (`$7FA4`, `$7FA7`), which never return, are proved by hand.
[Trait("Level", "Integration")]
public sealed class WorldEffectsOracleTests
{
    private const int Bonus = 0x68;
    private const int Colours = 0x1C;
    private const int Freeze = 0x67;
    private const int Clock = 0x2B;
    private const int Blow = 0xA77B;

    public static TheoryData<int, ushort> Effects() => new()
    {
        { 1, 0x348A },
        { 2, 0x3495 },
        { 19, 0x3620 },
        { 21, 0x348D },
        { 23, 0x3498 },
        { 25, 0x34FA },
        { 26, 0x3502 },
        { 27, 0x350A },
        { 28, 0x35A7 },
        { 30, 0x3620 },
        { 31, 0x3620 },
        { 15, 0x2F68 },
    };

    [Theory]
    [MemberData(nameof(Effects))]
    public void DoesExactlyWhatTheGameDoes(int type, ushort address)
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        foreach (byte player in (byte[])[0, 1])
        {
            Rebb64Machine machine = new();
            machine.StartOnePlayer();

            Capture capture = machine.Invoke(address, 0, player, 10_000);
            byte[] before = capture.Before;

            EntityTable entities = new();
            LevelTimer timer = new() { Frames = before[Clock] };
            LevelFlow flow = new(timer);
            flow.Begin(0);

            // The level's two background colours are $1C and $1E.
            SetColours(flow, before[Colours], before[Colours + 2]);

            LevelClear clear = new(timer, entities, new(), Items(entities), new(new ScriptedRandomSource([])))
            {
                BonusType = before[Bonus],
                BonusArg = before[0x69],
                BonusStage = before[0x6A],
            };

            FoodDrop food = new(entities, new Scores(), new(new ScriptedRandomSource([])));
            WorldEffects world = new(
                clear,
                flow,
                timer,
                entities,
                new(),
                food,
                Items(entities),
                new(entities, food),
                new(entities, new(new ScriptedRandomSource([]))),
                new(entities, timer));

            flow.Freeze = before[Freeze];
            world.Apply(type, player);

            Assert.Equal(capture.After[Bonus], clear.BonusType);
            Assert.Equal(capture.After[0x69], clear.BonusArg);
            Assert.Equal(capture.After[0x6A], clear.BonusStage);
            Assert.Equal(capture.After[Freeze], flow.Freeze);
            Assert.Equal(capture.After[Clock], timer.Frames);
            Assert.Equal((capture.After[Colours] << 4) | capture.After[Colours + 2], flow.Colours);
        }
    }

    [Fact]
    public void GivesThunderShotsExactlyAsTheGameDoes()
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        foreach (byte player in (byte[])[0, 1])
        {
            Rebb64Machine machine = new();
            machine.StartOnePlayer();

            Capture capture = machine.Invoke(0xA64D, 0, player, 10_000);

            PlayerTable players = new();
            new ItemEffects(players, new EntityTable()).Apply(6, player);

            Assert.Equal(capture.After[Blow + 8 + player], players.Shots[player]);
            Assert.Equal(capture.Before[Blow + 8 + (1 - player)], players.Shots[1 - player]);
        }
    }

    private static LevelItems Items(EntityTable entities)
        => new(entities, new Scores(), new ItemEffects(new PlayerTable(), entities), new(new ScriptedRandomSource([])));

    private static void SetColours(LevelFlow flow, byte first, byte second) => flow.Begin((first << 4) | second);
}
