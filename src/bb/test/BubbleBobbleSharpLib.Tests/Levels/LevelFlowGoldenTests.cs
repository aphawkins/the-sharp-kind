// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using BubbleBobbleSharpLib.Tests.Enemies;
using SharpKind.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// The freeze against the real thing.
//
// Captures/freeze-level-1.txt was read out of VICE running rebb64, on 2026-09-28, on level 1: 130 game passes,
// each read at $0A1C, before and after any write, three IRQ frames apart.
//
// INTERVENTIONS. Nothing translated starts a freeze (the time-stop effect is step 6's), so at passes 10, 60
// and 100 what $2F68-$2F7C does was written: $1C and $1E EOR 7, $67 to 8, 1 and 3, and $2B to $FF.
//
// Each line is the bytes before and after: $08, $67, $1C, $1E, $2A, $2B; then for enemy slots 2 to 7 their
// state ($B4-$B9), X ($BC-$C1), Y ($C4-$C9) and frame ($8522-$8527).
//
// A replay of the flow runs the frames to the next pass and then $15C9; a replay of the enemies runs the
// port's turn for a pass that began frozen, whose enemies must not have moved by the next line.
[Trait("Level", "Integration")]
public sealed class LevelFlowGoldenTests
{
    private const int Frame = 0;
    private const int Freeze = 1;
    private const int BackgroundOne = 2;
    private const int BackgroundTwo = 3;
    private const int Seconds = 4;
    private const int Frames = 5;
    private const int Enemies = 6;
    private const int Slots = 6;
    private const int Length = Enemies + (4 * Slots);

    private static readonly SolidMap s_map = SolidMap.Build(
        new Level { Number = 1, Bitmap = [.. Enumerable.Repeat(new string('.', 32), 23)] },
        []);

    public static TheoryData<int> Steps()
    {
        TheoryData<int> lines = [];

        for (int i = 0; i < Capture().Count - 1; i++)
        {
            lines.Add(i);
        }

        return lines;
    }

    [Theory]
    [MemberData(nameof(Steps))]
    public void CountsTheFreezeDownExactlyAsTheGameDoes(int line)
    {
        List<(byte[] Before, byte[] After)> capture = Capture();
        byte[] from = capture[line].After;
        byte[] to = capture[line + 1].Before;
        Assert.Equal(Length, from.Length);

        LevelTimer timer = new();
        LevelFlow flow = new(timer);
        timer.Begin(0);
        flow.Begin((from[BackgroundOne] << 4) | from[BackgroundTwo]);
        flow.Freeze = from[Freeze];
        timer.Frames = from[Frames];
        timer.Seconds = from[Seconds];

        int frames = (to[Frame] - from[Frame]) & 0xFF;

        for (int frame = 0; frame < frames; frame++)
        {
            timer.Advance();
        }

        flow.Update();

        Assert.Equal(to[Freeze], flow.Freeze);
        Assert.Equal(to[BackgroundOne], flow.BackgroundOne);
        Assert.Equal(to[BackgroundTwo], flow.BackgroundTwo);
        Assert.Equal(to[Frames], timer.Frames);
        Assert.Equal(to[Seconds], timer.Seconds);
    }

    [Theory]
    [MemberData(nameof(Steps))]
    public void HoldsTheEnemiesStillExactlyAsTheGameDoes(int line)
    {
        List<(byte[] Before, byte[] After)> capture = Capture();
        byte[] from = capture[line].After;
        byte[] to = capture[line + 1].Before;

        if (from[Freeze] == 0)
        {
            return;
        }

        EntityTable entities = new();
        ObjectTable objects = new();
        BbRandom random = new(new FakeRandomSource());

        for (int slot = 0; slot < Slots; slot++)
        {
            entities.State[2 + slot] = from[Enemies + slot];
            entities.X[2 + slot] = from[Enemies + Slots + slot];
            entities.Y[2 + slot] = from[Enemies + (2 * Slots) + slot];
            entities.Frame[2 + slot] = from[Enemies + (3 * Slots) + slot];
            entities.Mode[2 + slot] = 0xFF;
        }

        entities.State[0] = PlayerFrame.PlayingState;
        LevelFlow flow = new(new()) { Freeze = from[Freeze] };
        EnemyFrame frame = TestEnemies.Frame(entities, objects, random, new(), flow);

        frame.Step(0, s_map);

        for (int slot = 0; slot < Slots; slot++)
        {
            byte state = from[Enemies + slot];

            if (state is not 0 and < 0x0B)
            {
                Assert.Equal(to[Enemies + Slots + slot], entities.X[2 + slot]);
                Assert.Equal(to[Enemies + (2 * Slots) + slot], entities.Y[2 + slot]);
                Assert.Equal(to[Enemies + (3 * Slots) + slot], entities.Frame[2 + slot]);
            }
        }
    }

    [Fact]
    public void CoversThreeFreezesAndTheEnemiesMovingOnEitherSide()
    {
        List<(byte[] Before, byte[] After)> capture = Capture();

        Assert.Equal(130, capture.Count);
        Assert.Equal(3, capture.Count(c => c.After[Freeze] != c.Before[Freeze]));
        Assert.Contains(capture, c => c.Before[Freeze] == 7);
        Assert.Contains(capture, c => c.Before[BackgroundOne] != c.After[BackgroundOne]);
        Assert.Contains(
            capture.Zip(capture.Skip(1)),
            p => p.First.After[Freeze] == 0 && p.Second.Before[Enemies + Slots] != p.First.After[Enemies + Slots]);
    }

    private static List<(byte[] Before, byte[] After)> Capture()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Levels", "Captures", "freeze-level-1.txt");

        return [.. File.ReadAllLines(path)
            .Select(line =>
            {
                string[] parts = line.Split(' ');
                return (Convert.FromHexString(parts[1]), Convert.FromHexString(parts[2]));
            })];
    }
}
