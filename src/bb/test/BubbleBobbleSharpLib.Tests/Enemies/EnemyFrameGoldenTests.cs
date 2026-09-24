// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Globalization;
using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using SharpKind;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Enemies;

// A whole enemy slot's frame against the real thing: $1CDB to $1D24, the drop, the hold, the anger
// and the dispatch.
//
// Each Captures/slots-N.txt was read out of VICE running rebb64, on 2026-09-23, with one player. The
// game stopped at $09A5, the level's initialisation, and SUBFLG was written there, so every level but
// 1 is an intervention. Then two exec checkpoints bracket each enemy slot: $1CDB, the top of $1CBD's
// loop, and $1D24, its end. The player pressed up every three seconds.
//
// On level 1 every enemy's $8728 was written $FF eight seconds in, which makes them angry. That is an
// intervention too.
//
// Each line is one slot's frame: the slot, the bytes before, the bytes after, and ObjectTable's
// eighteen type bytes before. A frame that changed ObjectTable has the whole of it before and after
// as well. The bytes are listed in Fields.
//
// **The other slots are the capture's too.** A leap probe can read past the level into another
// slot's arrays - see SolidMap's row tails - so every other enemy slot is loaded as the latest line
// before this one left it.
//
// **The random draws are recovered, not matched.** $E9EA exclusive-ors a CIA timer byte into $26,
// which the port takes from IRandomSource. The capture has $26 and $27 before and after, so the
// timer bytes that turn one into the other are worked out here and fed back in. That proves every
// decision taken on a draw, and nothing about which draws the game gets.
[Trait("Level", "Integration")]
public sealed class EnemyFrameGoldenTests
{
    // $CA, $DC, $EE, $AA0C, $AA1E, $A9C4, $A9D6, $A9FA and $0193, for each of the eighteen slots.
    private const int ObjectBytes = 9;

    private static readonly int[] s_levels = [1, 7, 12, 22, 36, 42, 50, 60];

    private static readonly LevelStore s_store = LevelStore.Read(Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        "levels.json"));

    private static readonly ZoneStore s_zones = ZoneStore.Read(Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        "zones.json"));

    // The order of the bytes in a line. The first ten are zero page, the next three the slot's own
    // zero-page bytes, and the rest the slot's column of $85xx to $88xx.
    private enum Fields
    {
        Counter = 0,
        RandomLow = 1,
        RandomHigh = 2,
        Freeze = 3,
        PlayerZeroState = 4,
        PlayerOneState = 5,
        PlayerZeroX = 6,
        PlayerOneX = 7,
        PlayerZeroY = 8,
        PlayerOneY = 9,
        State = 10,
        X = 11,
        Y = 12,
        Frame = 13,
        Colour = 14,
        SpriteBase = 15,
        Mode = 16,
        Heading = 17,
        AnimationTimer = 18,
        HoldTimer = 19,
        TurnTimer = 20,
        TurnInterval = 21,
        ClimbNext = 22,
        LeapFlag = 23,
        ClimbFlag = 24,
        FlashTimer = 25,
        FrameCount = 26,
        FrameMask = 27,
        RiseCounter = 28,
        FallCounter = 29,
        GroundState = 30,
        BubbleTimer = 31,
        LeftFlag = 32,
        RightFlag = 33,
        AttackTimer = 34,
        Count = 35,
    }

    public static TheoryData<int, int> Lines()
    {
        TheoryData<int, int> lines = [];

        foreach (int level in s_levels)
        {
            for (int i = 0; i < Capture(level).Count; i++)
            {
                lines.Add(level, i);
            }
        }

        return lines;
    }

    [Theory]
    [MemberData(nameof(Lines))]
    public void StepsExactlyAsTheGameDoes(int level, int line)
    {
        List<Frame> capture = Capture(level);
        Frame frame = capture[line];
        byte[] before = frame.Before;

        // $1CFB's freeze is not translated, and no capture reaches it.
        Assert.Equal(0, before[(int)Fields.Freeze]);

        EntityTable entities = new();
        ObjectTable objects = new();
        TimerReplay timer = new(before, frame.After);
        BbRandom random = new(timer)
        {
            Low = before[(int)Fields.RandomLow],
            High = before[(int)Fields.RandomHigh],
        };

        foreach (Frame other in Others(capture, line))
        {
            LoadEntities(entities, other.Slot, other.After);
        }

        LoadEntities(entities, frame.Slot, before);
        LoadObjects(objects, frame);

        EnemyFrame enemies = TestEnemies.Frame(entities, objects, random);

        enemies.Step(frame.Slot, before[(int)Fields.Counter], Map(level, entities));

        byte[] after = SaveEntities(entities, random, frame.Slot, before);
        Assert.Equal(Convert.ToHexString(frame.After), Convert.ToHexString(after));
        Assert.Equal(Convert.ToHexString(frame.ObjectsAfter ?? Objects(frame)), Convert.ToHexString(SaveObjects(objects)));
    }

    // What the captures cover, so that a thin one cannot pass for a thorough one.
    [Theory]
    [InlineData(1, 0x02)]
    [InlineData(7, 0x08)]
    [InlineData(12, 0x06)]
    [InlineData(22, 0x05)]
    [InlineData(36, 0x04)]
    [InlineData(42, 0x03)]
    [InlineData(50, 0x07)]
    [InlineData(60, 0x09)]
    public void HoldsTheClassItIsFor(int level, int state)
    {
        List<Frame> capture = Capture(level);

        Assert.All(capture, f => Assert.Equal(state, f.Before[(int)Fields.State]));
    }

    [Fact]
    public void CoversTheDropTheHoldTheAngerAndTheShots()
    {
        List<Frame> all = [.. s_levels.SelectMany(Capture)];

        Assert.Contains(all, f => f.Before[(int)Fields.Mode] < 0x80);
        Assert.Contains(all, f => f.Before[(int)Fields.HoldTimer] != 0);
        Assert.Contains(all, f => f.Before[(int)Fields.FlashTimer] != 0 && (f.Before[(int)Fields.Counter] & 0x02) == 0);
        Assert.Contains(all, f => f.ObjectsAfter != null);
    }

    private static SolidMap Map(int level, EntityTable entities)
        => SolidMap.Build(s_store.Level(level), s_zones.Zones(level), entities);

    // The last line before this one for each other slot.
    private static IEnumerable<Frame> Others(List<Frame> capture, int line)
    {
        HashSet<int> seen = [capture[line].Slot];

        for (int i = line - 1; i >= 0; i--)
        {
            if (seen.Add(capture[i].Slot))
            {
                yield return capture[i];
            }
        }
    }

    private static List<Frame> Capture(int level)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Enemies", "Captures", $"slots-{level}.txt");

        return [.. File.ReadAllLines(path).Select(Frame.Parse)];
    }

    private static void LoadEntities(EntityTable entities, int slot, byte[] b)
    {
        Assert.Equal((int)Fields.Count, b.Length);

        entities.State[0] = b[(int)Fields.PlayerZeroState];
        entities.State[1] = b[(int)Fields.PlayerOneState];
        entities.X[0] = b[(int)Fields.PlayerZeroX];
        entities.X[1] = b[(int)Fields.PlayerOneX];
        entities.Y[0] = b[(int)Fields.PlayerZeroY];
        entities.Y[1] = b[(int)Fields.PlayerOneY];

        entities.State[slot] = b[(int)Fields.State];
        entities.X[slot] = b[(int)Fields.X];
        entities.Y[slot] = b[(int)Fields.Y];
        entities.Frame[slot] = b[(int)Fields.Frame];
        entities.Colour[slot] = b[(int)Fields.Colour];
        entities.SpriteBase[slot] = b[(int)Fields.SpriteBase];
        entities.Mode[slot] = b[(int)Fields.Mode];
        entities.Heading[slot] = b[(int)Fields.Heading];
        entities.AnimationTimer[slot] = b[(int)Fields.AnimationTimer];
        entities.HoldTimer[slot] = b[(int)Fields.HoldTimer];
        entities.TurnTimer[slot] = b[(int)Fields.TurnTimer];
        entities.TurnInterval[slot] = b[(int)Fields.TurnInterval];
        entities.ClimbNext[slot] = ClimbNext(slot, b[(int)Fields.ClimbNext]);
        entities.LeapFlag[slot] = b[(int)Fields.LeapFlag];
        entities.ClimbFlag[slot] = b[(int)Fields.ClimbFlag];
        entities.FlashTimer[slot] = b[(int)Fields.FlashTimer];
        entities.FrameCount[slot] = b[(int)Fields.FrameCount];
        entities.FrameMask[slot] = b[(int)Fields.FrameMask];
        entities.RiseCounter[slot] = b[(int)Fields.RiseCounter];
        entities.FallCounter[slot] = b[(int)Fields.FallCounter];
        entities.GroundState[slot] = b[(int)Fields.GroundState];
        entities.BubbleTimer[slot] = b[(int)Fields.BubbleTimer];
        entities.LeftFlag[slot] = b[(int)Fields.LeftFlag];
        entities.RightFlag[slot] = b[(int)Fields.RightFlag];
        entities.AttackTimer[slot] = b[(int)Fields.AttackTimer];
    }

    // In the order Fields lists them.
    private static byte[] SaveEntities(EntityTable entities, BbRandom random, int slot, byte[] before) => [
        before[(int)Fields.Counter],
        random.Low,
        random.High,
        before[(int)Fields.Freeze],
        entities.State[0],
        entities.State[1],
        entities.X[0],
        entities.X[1],
        entities.Y[0],
        entities.Y[1],
        entities.State[slot],
        entities.X[slot],
        entities.Y[slot],
        entities.Frame[slot],
        entities.Colour[slot],
        entities.SpriteBase[slot],
        entities.Mode[slot],
        entities.Heading[slot],
        entities.AnimationTimer[slot],
        entities.HoldTimer[slot],
        entities.TurnTimer[slot],
        entities.TurnInterval[slot],
        (byte)(slot + (entities.ClimbNext[slot] ? 0x28 : 0x00)),
        entities.LeapFlag[slot],
        entities.ClimbFlag[slot],
        entities.FlashTimer[slot],
        entities.FrameCount[slot],
        entities.FrameMask[slot],
        entities.RiseCounter[slot],
        entities.FallCounter[slot],
        entities.GroundState[slot],
        entities.BubbleTimer[slot],
        entities.LeftFlag[slot],
        entities.RightFlag[slot],
        entities.AttackTimer[slot],
    ];

    // Only the type bytes when the frame left ObjectTable alone, which is all $EE27 reads of it.
    private static void LoadObjects(ObjectTable objects, Frame frame)
    {
        byte[] bytes = frame.ObjectsBefore ?? Objects(frame);

        for (int slot = 0; slot < ObjectTable.Capacity; slot++)
        {
            int at = slot * ObjectBytes;
            objects.Type[slot] = bytes[at];
            objects.Column[slot] = bytes[at + 1];
            objects.Row[slot] = bytes[at + 2];
            objects.X[slot] = bytes[at + 3];
            objects.Y[slot] = bytes[at + 4];
            objects.SubX[slot] = bytes[at + 5];
            objects.SubY[slot] = bytes[at + 6];
            objects.Flags[slot] = bytes[at + 7];
            objects.Direction[slot] = bytes[at + 8];
        }
    }

    // The type bytes alone, in the whole-table layout with every other byte zero.
    private static byte[] Objects(Frame frame)
    {
        byte[] bytes = new byte[ObjectTable.Capacity * ObjectBytes];

        for (int slot = 0; slot < ObjectTable.Capacity; slot++)
        {
            bytes[slot * ObjectBytes] = frame.Types[slot];
        }

        return bytes;
    }

    private static byte[] SaveObjects(ObjectTable objects)
    {
        byte[] bytes = new byte[ObjectTable.Capacity * ObjectBytes];

        for (int slot = 0; slot < ObjectTable.Capacity; slot++)
        {
            int at = slot * ObjectBytes;
            bytes[at] = objects.Type[slot];
            bytes[at + 1] = objects.Column[slot];
            bytes[at + 2] = objects.Row[slot];
            bytes[at + 3] = objects.X[slot];
            bytes[at + 4] = objects.Y[slot];
            bytes[at + 5] = objects.SubX[slot];
            bytes[at + 6] = objects.SubY[slot];
            bytes[at + 7] = objects.Flags[slot];
            bytes[at + 8] = objects.Direction[slot];
        }

        return bytes;
    }

    // $86B0 holds the slot, or the slot plus $28. Nothing else is a value it can hold.
    private static bool ClimbNext(int slot, byte raw)
    {
        Assert.True(raw == slot || raw == slot + 0x28, $"$86B0 is {raw:X2} for slot {slot}");
        return raw != slot;
    }

    private sealed record Frame(int Slot, byte[] Before, byte[] After, byte[] Types, byte[]? ObjectsBefore, byte[]? ObjectsAfter)
    {
        internal static Frame Parse(string line)
        {
            string[] parts = line.Split(' ');

            return new(
                int.Parse(parts[0], CultureInfo.InvariantCulture),
                Convert.FromHexString(parts[1]),
                Convert.FromHexString(parts[2]),
                Convert.FromHexString(parts[3]),
                parts.Length > 4 ? Convert.FromHexString(parts[4]) : null,
                parts.Length > 4 ? Convert.FromHexString(parts[5]) : null);
        }
    }

    // The CIA bytes that take $26 and $27 from before to after, found by running $E9EA's own shift
    // and fold. One draw leaves one answer. Two draws happen only when the first passed $EDE9's one
    // in four, and the first answer that passes and lands on the same bytes is taken - any of them
    // gives the same decisions, because $EDE9 reads only the low two bits of the first draw.
    private sealed class TimerReplay : IRandomSource
    {
        private readonly Queue<int> _bytes = [];

        internal TimerReplay(byte[] before, byte[] after)
        {
            byte low = before[(int)Fields.RandomLow];
            byte high = before[(int)Fields.RandomHigh];
            byte lowAfter = after[(int)Fields.RandomLow];
            byte highAfter = after[(int)Fields.RandomHigh];

            if (low == lowAfter && high == highAfter)
            {
                return;
            }

            (int mixed, byte shifted) = Fold(low, high);

            if (shifted == highAfter)
            {
                _bytes.Enqueue(lowAfter ^ mixed);
                return;
            }

            for (int first = 0; first < 0x100; first++)
            {
                byte lowFirst = (byte)(mixed ^ first);

                if ((lowFirst & 0x03) != 0)
                {
                    continue;
                }

                (int mixedSecond, byte shiftedSecond) = Fold(lowFirst, shifted);

                if (shiftedSecond == highAfter)
                {
                    _bytes.Enqueue(first);
                    _bytes.Enqueue(lowAfter ^ mixedSecond);
                    return;
                }
            }

            Assert.Fail($"no timer bytes take {low:X2}{high:X2} to {lowAfter:X2}{highAfter:X2}");
        }

        public int NextInt() => throw new NotSupportedException();

        public int Random(int toExclusive) => _bytes.Dequeue();

        public int Random(int fromInclusive, int toExclusive) => throw new NotSupportedException();

        public bool TrueOrFalse() => throw new NotSupportedException();

        public int GaussianRandom(int min, int max) => throw new NotSupportedException();

        // $E9EA without the timer: the fold, and $27 after the shift.
        private static (int Mixed, byte High) Fold(byte low, byte high)
        {
            int lowTop = low >> 7;
            byte shiftedHigh = (byte)((high << 1) | lowTop);
            byte shiftedLow = (byte)((low << 1) | (high >> 7));

            return (((shiftedHigh ^ shiftedLow) + shiftedHigh + lowTop) & 0xFF, shiftedHigh);
        }
    }
}
