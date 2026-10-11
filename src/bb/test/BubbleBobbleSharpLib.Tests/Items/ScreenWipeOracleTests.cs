// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using BubbleBobbleSharpLib.Specials;
using BubbleBobbleSharpLib.Tests.Levels;
using BubbleBobbleSharpLib.Tests.Oracle;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Items;

// The special item of type 8's wipe (`$2DD7`), against the real game run on SharpKind.Emulation.
//
// **The routine is called directly** (`Invoke`, an intervention) some frames into level 1, with `$E494`, the wait for a
// frame, made an `rts` (a call with interrupts off would wait for ever): its 25 rows take no frames and the IRQ moves
// nothing between them. The port's effect runs on the bytes before and its wait is run to the end. The slots, the objects,
// the water and the fire are compared with what the game left; and the level as the port draws it wiped with both screens
// (which cells are blocks: the port names the level's tile `$15`, where the screen has `$10`) and the colour RAM; and
// character `$FF` with a solid block.
[Trait("Level", "Integration")]
public sealed class ScreenWipeOracleTests
{
    private const int Effect = 0x2DD7;
    private const int WaitFrame = 0xE494;

    private const int Random = 0x26;
    private const int Water = 0x4D;
    private const int Fire = 0xA770;
    private const int ScreenPage = 0x30;
    private const int OtherScreen = 0x04;
    private const int ScreenColumns = 40;
    private const int SpriteEnable = 0x15;

    private static readonly LevelStore s_levels = LevelStore.Read(Asset("levels.json"));

    [Theory]
    [InlineData(20)]
    [InlineData(150)]
    [InlineData(400)]
    public void WipesExactlyAsTheGameDoes(int frames)
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        Rebb64Machine machine = new();
        machine.StartOnePlayer();
        machine.RunFrames(frames);

        byte enabled = machine.PeekVic(SpriteEnable);
        byte wait = machine.Peek(WaitFrame);

        machine.Poke(WaitFrame, 0x60);

        Capture capture = machine.Invoke(Effect, 0, 0, 20_000_000);

        machine.Poke(WaitFrame, wait);

        EntityTable entities = new();
        RamImage.Load(capture.Before, entities);
        entities.SpriteEnable = enabled;
        ObjectTable objects = new();
        RamImage.Load(capture.Before, objects);
        BbRandom random = new(new ScriptedRandomSource(capture.Timer.Select(b => (int)b)))
        {
            Low = capture.Before[Random],
            High = capture.Before[Random + 1],
        };

        LevelTimer timer = new();
        LevelItems items = new(entities, new Scores(), new ItemEffects(new PlayerTable(), entities), random);
        FoodDrop food = new(entities, new Scores(), random);
        FireStrips fire = new(entities, food);
        WaterFlow water = new(entities, random) { Mask = capture.Before[Water] };
        WorldEffects world = new(
            new(timer, entities, objects, items, random),
            new(timer),
            timer,
            entities,
            objects,
            food,
            items,
            fire,
            water,
            new(entities, timer));

        world.Apply(8, 0);

        IGameHold hold = world.Hold ?? throw new InvalidOperationException("no hold");
        int frame = 0;

        while (hold.Frame())
        {
            if (++frame % 2 == 0)
            {
                hold.Pass();
            }
        }

        hold.Finish(0);

        string expected = RamImage.Entities(capture.After);
        string actual = RamImage.Entities(entities);

        Assert.True(expected == actual, RamImage.EntityDifference(expected, actual));
        Assert.Equal(RamImage.Objects(capture.After), RamImage.Objects(objects));
        Assert.Equal(machine.PeekVic(SpriteEnable), entities.SpriteEnable);
        Assert.Equal(capture.After[Water], water.Mask);
        Assert.Equal(capture.After.AsSpan(Fire, FireStrips.Capacity).ToArray(), fire.Timers.ToArray());
        Assert.All(capture.After.AsSpan(0x47F8, 8).ToArray(), b => Assert.Equal(0xFF, b));
        Assert.All(capture.After.AsSpan(0x4FF8, 8).ToArray(), b => Assert.Equal(0xFF, b));

        PlayfieldModel field = Playfield.Build(s_levels.Level(1)).Wiped(world.Wipe.Wiped);
        int screen = ((capture.After[ScreenPage] | 0x03) << 8) - 0x300;

        for (int row = 0; row < PlayfieldModel.Rows; row++)
        {
            for (int column = 2; column < PlayfieldModel.Columns - 2; column++)
            {
                int offset = (row * ScreenColumns) + column;
                string at = $"row {row}, column {column}";

                bool block = field.Character(column, row) == PlayfieldModel.Block;
                byte shown = capture.After[screen + offset];
                byte other = capture.After[(screen ^ (OtherScreen << 8)) + offset];

                Assert.True(field.IsWiped(row), at);
                Assert.True(block == (shown == PlayfieldModel.Block), $"{at}: {shown:X2}");
                Assert.True(block == (other == PlayfieldModel.Block), $"{at}, the other screen: {other:X2}");
                Assert.Equal(PlayfieldModel.WipedColourRam, machine.PeekColour(offset));
            }
        }
    }

    private static string Asset(string name) => Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        name);
}
