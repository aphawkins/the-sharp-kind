// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using BubbleBobbleSharpLib.Tests.Oracle;
using SharpKind;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// The last level's giant (`$1D84`-`$1E2D`), against the real game run on SharpKind.Emulation. The routine is a plain subroutine
// with no waits, so it is called as `jsr` would from the middle of level 100, over a set of places, frames, directions and
// players (an intervention: the bytes are put where the game would have them).
[Trait("Level", "Integration")]
public sealed class BossFightOracleTests
{
    private const int X = 0xBC;
    private const int Y = 0xBD;
    private const int Frame = 0xBE;
    private const int Direction = 0xBF;
    private const int Colour = 0xC0;
    private const int Angry = 0x872A;
    private const int Active = 0x5AFF;
    private const int HitsAt = 0x5BBF;

    [Fact]
    public void MovesAndKillsAsTheGameDoes()
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        Rebb64Machine machine = new();
        machine.StartOnePlayer();
        machine.EnterLevel(100);

        uint seed = 7;
        int Next(int low, int high)
        {
            seed = unchecked((seed * 1664525) + 1013904223);

            return low + (int)((seed >> 8) % (uint)(high - low));
        }

        (byte X, byte Y)[] edges = [(0x28, 0x3A), (0x26, 0x38), (0xC0, 0xB8), (0xC2, 0xBA), (0x74, 0x65), (0x30, 0x40)];
        byte[] frames = [0x00, 0x18, 0x09];

        for (int trial = 0; trial < 400; trial++)
        {
            (byte x, byte y) = trial < 60
                ? edges[trial % edges.Length]
                : ((byte)Next(0x20, 0xC8), (byte)Next(0x30, 0xC0));
            byte frame = frames[Next(0, 3)];
            byte direction = (byte)(Next(0, 2) | (Next(0, 2) << 3));
            byte angry = (byte)(Next(0, 3) == 0 ? 0xFF : 0);
            byte playerX = (byte)(x + Next(-0x10, 0x48));
            byte playerY = (byte)(y + Next(-0x10, 0x40));

            machine.Poke(X, x);
            machine.Poke(Y, y);
            machine.Poke(Frame, frame);
            machine.Poke(Direction, direction);
            machine.Poke(Colour, 5);
            machine.Poke(Angry, angry);
            machine.Poke(Active, 1);
            machine.Poke(0xB2, PlayerFrame.PlayingState);
            machine.Poke(0xB3, (byte)(trial % 3 == 0 ? PlayerFrame.PlayingState : 0));
            machine.Poke(0xBA, playerX);
            machine.Poke(0xBB, (byte)(playerX + 0x10));
            machine.Poke(0xC2, playerY);
            machine.Poke(0xC3, (byte)(playerY + 0x08));

            Capture capture = machine.Invoke(0x1D84, 0, 0, 100_000);

            GameLoop loop = new(new(new RandomSource(new Random(1))), 2);
            RamImage.Load(capture.Before, loop.Entities);
            loop.Boss.Begin(BossFight.Level);
            loop.Boss.Appear();
            loop.Boss.X = x;
            loop.Boss.Y = y;
            loop.Boss.Frame = frame;
            loop.Boss.Direction = direction;
            loop.Entities.Slot(2).FlashTimer = angry;
            loop.Boss.Move();

            string where = $"trial {trial}: boss ({x:X2},{y:X2}) frame {frame:X2} direction {direction:X2} angry {angry:X2}";

            Assert.True(capture.After[X] == loop.Boss.X, $"{where}: X {capture.After[X]:X2} v {loop.Boss.X:X2}");
            Assert.True(capture.After[Y] == loop.Boss.Y, $"{where}: Y {capture.After[Y]:X2} v {loop.Boss.Y:X2}");
            Assert.True(capture.After[Frame] == loop.Boss.Frame, $"{where}: frame");
            Assert.True(capture.After[Direction] == loop.Boss.Direction, $"{where}: direction");
            Assert.True(capture.After[Colour] == loop.Boss.Colour, $"{where}: colour");
            Assert.True(capture.After[0xB2] == loop.Entities.Slot(0).State, $"{where}: player 1's state");
            Assert.True(capture.After[0xB3] == loop.Entities.Slot(1).State, $"{where}: player 2's state");
            Assert.True(capture.After[Active] != 0 == loop.Boss.Active, $"{where}: active");
        }
    }

    // The hit and stun routine (`$1134`) is also a plain subroutine, called in turn as the game does each pass of the level: bolts
    // put where they reach the giant or miss, and a hit count that runs down to a stun, the stun's passes and the getting up.
    [Fact]
    public void TakesHitsAndRecoversAsTheGameDoes()
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        Rebb64Machine machine = new();
        machine.StartOnePlayer();
        machine.EnterLevel(100);

        Sequence random = new(11);

        byte[] frames = [0x00, 0x18];

        for (int trial = 0; trial < 12; trial++)
        {
            byte x = (byte)random.Next(0x28, 0xC0);
            byte y = (byte)random.Next(0x3A, 0xB8);
            byte frame = frames[random.Next(0, 2)];

            machine.Poke(X, x);
            machine.Poke(Y, y);
            machine.Poke(Frame, frame);
            machine.Poke(HitsAt, (byte)random.Next(0, 4));
            machine.PokeVic(0x15, 0xFC);

            PlaceBolts(machine, x, y, random);

            machine.Poke(Angry, 0);

            Capture first = machine.Invoke(0x1134, 0, 0, 100_000);
            GameLoop loop = new(new(new RandomSource(new Random(1))), 2);
            RamImage.Load(first.Before, loop.Objects);
            RamImage.Load(first.Before, loop.Entities);
            loop.Boss.Begin(BossFight.Level);
            loop.Boss.Appear();
            loop.Boss.X = x;
            loop.Boss.Y = y;
            loop.Boss.Frame = frame;
            loop.Boss.Hits = first.Before[HitsAt];

            Capture capture = first;

            for (int pass = 0; pass < 150; pass++)
            {
                if (pass > 0)
                {
                    capture = machine.Invoke(0x1134, 0, 0, 100_000);
                }

                loop.Boss.Hit();

                Compare(machine, capture, loop, $"trial {trial} pass {pass}");
            }
        }
    }

    private static void PlaceBolts(Rebb64Machine machine, byte x, byte y, Sequence random)
    {
        // Bolts round about the giant's place, so that some reach it and some do not.
        for (int slot = 0; slot < 0x12; slot++)
        {
            machine.Poke(0xCA + slot, (byte)(random.Next(0, 4) < 2 ? 0x26 : 0x00));
            machine.Poke(0xDC + slot, (byte)Math.Clamp(((x - 0x14) >> 3) + random.Next(-4, 12), 0, 0x1F));
            machine.Poke(0xEE + slot, (byte)Math.Clamp(((y - 0x15) >> 3) + random.Next(-4, 10), 0, 0x18));
            machine.Poke(0xAA42 + slot, 0);
        }
    }

    private static void Compare(Rebb64Machine machine, Capture capture, GameLoop loop, string where)
    {
        Assert.True(capture.After[Frame] == loop.Boss.Frame, $"{where}: frame {capture.After[Frame]:X2} v {loop.Boss.Frame:X2}");
        Assert.True(capture.After[HitsAt] == loop.Boss.Hits, $"{where}: hits {capture.After[HitsAt]:X2} v {loop.Boss.Hits:X2}");
        Assert.True(capture.After[Angry] == loop.Entities.Slot(2).FlashTimer, $"{where}: angry");
        Assert.True(machine.PeekVic(0x15) == 0xFC == loop.Boss.Shown, $"{where}: shown");

        for (int slot = 0; slot < 0x12; slot++)
        {
            Assert.True(capture.After[0xCA + slot] == loop.Objects.Slot(slot).Type, $"{where}: bolt {slot}");
        }
    }

    // A repeatable run of numbers (an LCG), for places and counts.
    private sealed class Sequence(uint seed)
    {
        private uint _seed = seed;

        internal int Next(int low, int high)
        {
            _seed = unchecked((_seed * 1664525) + 1013904223);

            return low + (int)((_seed >> 8) % (uint)(high - low));
        }
    }
}
