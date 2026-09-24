// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Players;
using SharpKind.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Bubbles;

// $3D2D, $3D77, $3DB0, the pop's frames and $E97F, worked by hand off the reference.
public sealed class BubblePopTests
{
    private const int Slot = 10;

    // $3DB0. Ten points to the player who popped it, and then the pop's frames, one a pass.
    [Fact]
    public void APlainBubbleScoresAndPops()
    {
        Rig rig = new();
        Touched(rig, Slot, 0x04, player: 1);

        byte[] types = new byte[6];

        for (int i = 0; i < types.Length; i++)
        {
            rig.Pop.Update();
            types[i] = rig.Objects.Type[Slot];
        }

        Assert.Equal([0x3C, 0x3E, 0x40, 0x38, 0x36, 0xFF], types);
        Assert.Equal(0x01, rig.Scores.Bytes[5]);
        Assert.Equal(0, rig.Objects.Column[Slot]);
        Assert.Equal(0, rig.Objects.Row[Slot]);
    }

    // $7BD4. A bubble just blown is type $00 by the time it is drawn.
    [Fact]
    public void ABlownBubbleBecomesTypeZero()
    {
        Rig rig = new();
        Place(rig, Slot, 0x16);

        rig.Pop.Update();

        Assert.Equal(0x00, rig.Objects.Type[Slot]);
    }

    // $3CB2. $1E is enemy 3, slot 5: its sprite goes where the bubble is, in the frame $AA42 holds.
    [Fact]
    public void ACaughtEnemysSpriteFollowsItsBubble()
    {
        Rig rig = new();
        Place(rig, Slot, 0x1E);
        rig.Objects.EnemyType[Slot] = 0x07;
        rig.Entities.FlashTimer[5] = 0xFF;

        rig.Pop.Update();

        Assert.Equal(0x1E, rig.Objects.Type[Slot]);
        Assert.Equal(0x80, rig.Entities.X[5]);
        Assert.Equal(0x80, rig.Entities.Y[5]);
        Assert.Equal(0x07, rig.Entities.Frame[5]);
        Assert.Equal(0x00, rig.Entities.FlashTimer[5]);
    }

    // $3D77 to $3DB0 for $3A: a bubble that ran out pops without scoring.
    [Fact]
    public void AnExpiredBubblePopsWithoutScoring()
    {
        Rig rig = new();
        Place(rig, Slot, 0x3A);
        rig.Objects.EnemyType[Slot] = 0x04;

        rig.Pop.Update();

        Assert.Equal(0x3C, rig.Objects.Type[Slot]);
        Assert.Equal(new byte[6], rig.Scores.Bytes.ToArray());
    }

    // $3D2D. Within $18 in both axes the other bubble goes too, for the same player, keeping its
    // type. At $18 it does not, and nor does a wobbling one: $48 is past $24.
    [Theory]
    [InlineData(0x17, true)]
    [InlineData(0x18, false)]
    public void PopsTheBubblesAround(byte apart, bool popped)
    {
        Rig rig = new();
        Touched(rig, Slot, 0x04, player: 1);
        Place(rig, 12, 0x04);
        rig.Objects.X[12] = (byte)(0x80 + apart);
        Place(rig, 13, 0x48);

        rig.Pop.Update();

        Assert.Equal(popped ? 0x34 : 0x04, rig.Objects.Type[12]);
        Assert.Equal(0x48, rig.Objects.Type[13]);

        if (popped)
        {
            Assert.Equal(0x04, rig.Objects.EnemyType[12]);
            Assert.Equal(1, rig.Objects.Direction[12]);
        }
    }

    // $E931 walks down, so a bubble below the first pops in the same pass and one above in the next.
    [Fact]
    public void TheChainRunsDownTheSlotsInOnePass()
    {
        Rig rig = new();
        Touched(rig, Slot, 0x04, player: 0);
        Place(rig, 12, 0x04);
        Place(rig, 8, 0x04);

        rig.Pop.Update();

        Assert.Equal(0x3C, rig.Objects.Type[8]);
        Assert.Equal(0x34, rig.Objects.Type[12]);
    }

    // $3D3C. A bubble still under way pops nothing else.
    [Fact]
    public void AMovingBubblePopsNothingElse()
    {
        Rig rig = new();
        Touched(rig, Slot, 0x04, player: 0);
        rig.Objects.State[Slot] = 0x05;
        Place(rig, 8, 0x04);

        rig.Pop.Update();

        Assert.Equal(0x04, rig.Objects.Type[8]);
    }

    // $3D82. $1C is enemy 2, slot 4. It goes back in the state $1090 kept, $1A6F kills it, and the
    // bubble goes straight to $36.
    [Fact]
    public void LetsTheEnemyOutDead()
    {
        Rig rig = new();
        Touched(rig, Slot, 0x1C, player: 0);
        rig.Objects.Variant[Slot] = 0x03;
        rig.Entities.EnemyCount = 5;

        rig.Pop.Update();

        Assert.Equal(0x0B, rig.Entities.State[4]);
        Assert.Equal(0x03, rig.Entities.LeftFlag[4]);
        Assert.Equal(4, rig.Entities.EnemyCount);
        Assert.Equal(1, rig.Pop.Chain[0]);
        Assert.Equal(0x01, rig.Entities.AttackTimer[4]);
        Assert.Equal(0x36, rig.Objects.Type[Slot]);
        Assert.Equal(new byte[6], rig.Scores.Bytes.ToArray());
    }

    // $E97F. Three enemies in one chain are food 1, 2 and 3, and 4000 once the chain stops.
    [Fact]
    public void ScoresTheChainOnceItStops()
    {
        Rig rig = new();
        Touched(rig, 12, 0x18, player: 0);
        Place(rig, 11, 0x1A);
        Place(rig, 10, 0x1C);

        rig.Pop.Update();

        Assert.Equal([0x01, 0x02, 0x03], rig.Entities.AttackTimer[2..5].ToArray());
        Assert.Equal(3, rig.Pop.Chain[0]);
        Assert.Equal(0x00, rig.Scores.Bytes[1]);

        rig.Pop.Update();

        Assert.Equal(0, rig.Pop.Chain[0]);
        Assert.Equal(0x04, rig.Scores.Bytes[1]);
    }

    // $3DDD. One bit per type, in twos from $0C. $12 skips the fourth bit, which $0C takes once the
    // first is held.
    [Theory]
    [InlineData(0x0C, 0x00, 0x01)]
    [InlineData(0x0E, 0x00, 0x02)]
    [InlineData(0x0C, 0x01, 0x09)]
    [InlineData(0x10, 0x00, 0x04)]
    [InlineData(0x12, 0x00, 0x10)]
    [InlineData(0x14, 0x00, 0x20)]
    public void ALetterBubbleGivesALetter(byte held, byte had, byte powers)
    {
        Rig rig = new();
        Touched(rig, Slot, held, player: 1);
        rig.Players.ExtendLetters[1] = had;

        rig.Pop.Update();

        Assert.Equal(powers, rig.Players.ExtendLetters[1]);
        Assert.Equal(0x01, rig.Scores.Bytes[5]);
    }

    // $3E02. The special bubbles' own arms are not translated.
    [Theory]
    [InlineData(0x06)]
    [InlineData(0x08)]
    [InlineData(0x0A)]
    public void ThrowsOnASpecialBubblesOwnArm(byte held)
    {
        Rig rig = new();
        Touched(rig, Slot, held, player: 0);

        Assert.Throws<NotSupportedException>(rig.Pop.Update);
    }

    private static void Touched(Rig rig, int slot, byte held, int player)
    {
        Place(rig, slot, 0x34);
        rig.Objects.EnemyType[slot] = held;
        rig.Objects.Direction[slot] = (byte)player;
    }

    private static void Place(Rig rig, int slot, byte type)
    {
        rig.Objects.Type[slot] = type;
        rig.Objects.X[slot] = 0x80;
        rig.Objects.Y[slot] = 0x80;
        rig.Objects.Column[slot] = 0x0C;
        rig.Objects.Row[slot] = 0x0D;
    }

    private sealed class Rig
    {
        internal Rig() => Pop = new(
            Objects,
            Entities,
            Players,
            new(Entities, Scores, new(new FakeRandomSource())),
            Scores);

        internal ObjectTable Objects { get; } = new();

        internal EntityTable Entities { get; } = new();

        internal PlayerTable Players { get; } = new();

        internal Scores Scores { get; } = new();

        internal BubblePop Pop { get; }
    }
}
