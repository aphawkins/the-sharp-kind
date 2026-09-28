// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using SharpKind.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// The level's clock against the real thing.
//
// Captures/timer-level-1.txt was read out of VICE running rebb64, on 2026-09-28, on level 1 with one player and
// the enemies about: 260 game passes, each read at $0A1C, before and after any write. The passes are three IRQ
// frames apart, which is why a replay counts frames.
//
// INTERVENTIONS, so the runs of the clock that level 1 does not make on its own were read too: $A9B1 written 3
// (pass 20) and 1 (pass 130), $2B written $FF (pass 90) and $32 (pass 100), and $2D written 5 (pass 131).
//
// Each line is the bytes before and after: $08, $2A, $2B, $2C, $2D, $A9B1, $5D, $5E, $B2, $B3. A replay
// starts from a line's second half, runs the frames between it and the next line's first half, and then the
// item timers' own $1578, which is at the top of the next pass. A player dead at the start of a pass
// is respawned in it, which restarts the clock ($0504).
//
// $A9B1 at zero is not compared: $1694, step 4e's, takes the READY!! text off the screen and makes it $FF.
[Trait("Level", "Integration")]
public sealed class LevelTimerGoldenTests
{
    private const int Frame = 0;
    private const int Seconds = 1;
    private const int Frames = 2;
    private const int Start = 3;
    private const int Hurry = 4;
    private const int Ready = 5;
    private const int ItemOne = 6;
    private const int ItemTwo = 7;
    private const int StateOne = 8;
    private const int StateTwo = 9;
    private const int Length = 10;

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
    public void RunsExactlyAsTheGameDoes(int line)
    {
        List<(byte[] Before, byte[] After)> capture = Capture();
        byte[] from = capture[line].After;
        byte[] to = capture[line + 1].Before;
        Assert.Equal(Length, from.Length);

        Rig rig = new(from);

        // A player dead at $0A1C is respawned by $045C, at the top of the rest of that pass ($0504).
        if (from[StateOne] == PlayerFrame.DeadState || from[StateTwo] == PlayerFrame.DeadState)
        {
            rig.Timer.Restart();
        }

        int frames = (to[Frame] - from[Frame]) & 0xFF;

        for (int frame = 0; frame < frames; frame++)
        {
            if (rig.Timer.Advance())
            {
                rig.Items.Tick();
            }
        }

        rig.Items.Update();

        Assert.Equal(to[Seconds], rig.Timer.Seconds);
        Assert.Equal(to[Frames], rig.Timer.Frames);
        Assert.Equal(to[Start], rig.Timer.Start);
        Assert.Equal(to[Hurry], rig.Timer.Hurry);
        Assert.Equal(to[ItemOne], rig.Items.Timer[0]);
        Assert.Equal(to[ItemTwo], rig.Items.Timer[1]);

        if (rig.Timer.Ready != 0)
        {
            Assert.Equal(to[Ready], rig.Timer.Ready);
        }
    }

    [Fact]
    public void CoversTicksStopsAndTheReadyTextRunningOut()
    {
        List<(byte[] Before, byte[] After)> capture = Capture();

        Assert.Equal(260, capture.Count);
        Assert.Contains(capture.Zip(capture.Skip(1)), p => p.Second.Before[Seconds] != p.First.After[Seconds]);
        Assert.Contains(capture, c => c.Before[Frames] == LevelTimer.Stopped);
        Assert.Contains(capture, c => c.After[Ready] is > 0 and < 0x80);
        Assert.Contains(capture, c => c.Before[Hurry] == 5);
        Assert.Contains(capture.Zip(capture.Skip(1)), p => p.Second.Before[Seconds] > p.First.After[Seconds]);
    }

    private static List<(byte[] Before, byte[] After)> Capture()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Levels", "Captures", "timer-level-1.txt");

        return [.. File.ReadAllLines(path)
            .Select(line =>
            {
                string[] parts = line.Split(' ');
                return (Convert.FromHexString(parts[1]), Convert.FromHexString(parts[2]));
            })];
    }

    private sealed class Rig
    {
        internal Rig(byte[] b)
        {
            Items = new(Entities, new Scores(), new(Players, Entities), new(new FakeRandomSource()));

            Timer.Begin(0);
            Timer.Seconds = b[Seconds];
            Timer.Frames = b[Frames];
            Timer.Hurry = b[Hurry];
            Timer.Ready = b[Ready];
            Items.Timer[0] = b[ItemOne];
            Items.Timer[1] = b[ItemTwo];
        }

        internal LevelTimer Timer { get; } = new();

        internal PlayerTable Players { get; } = new();

        internal EntityTable Entities { get; } = new();

        internal LevelItems Items { get; }
    }
}
