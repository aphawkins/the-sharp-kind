// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Players;
using BubbleBobbleSharpLib.Specials;
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
            types[i] = rig.Objects.Slot(Slot).Type;
        }

        Assert.Equal([0x3C, 0x3E, 0x40, 0x38, 0x36, 0xFF], types);
        Assert.Equal(0x01, rig.Scores.Bytes[5]);
        Assert.Equal(0, rig.Objects.Slot(Slot).Column);
        Assert.Equal(0, rig.Objects.Slot(Slot).Row);
    }

    // $7BD4. A bubble just blown is type $00 by the time it is drawn.
    [Fact]
    public void ABlownBubbleBecomesTypeZero()
    {
        Rig rig = new();
        Place(rig, Slot, 0x16);

        rig.Pop.Update();

        Assert.Equal(0x00, rig.Objects.Slot(Slot).Type);
    }

    // $3CB2. $1E is enemy 3, slot 5: its sprite goes where the bubble is, in the frame $AA42 holds.
    [Fact]
    public void ACaughtEnemysSpriteFollowsItsBubble()
    {
        Rig rig = new();
        Place(rig, Slot, 0x1E);
        rig.Objects.Slot(Slot).EnemyType = 0x07;
        rig.Entities.Slot(5).FlashTimer = 0xFF;

        rig.Pop.Update();

        Assert.Equal(0x1E, rig.Objects.Slot(Slot).Type);
        Assert.Equal(0x80, rig.Entities.Slot(5).X);
        Assert.Equal(0x80, rig.Entities.Slot(5).Y);
        Assert.Equal(0x07, rig.Entities.Slot(5).Frame);
        Assert.Equal(0x00, rig.Entities.Slot(5).FlashTimer);
    }

    // $3D77 to $3DB0 for $3A: a bubble that ran out pops without scoring.
    [Fact]
    public void AnExpiredBubblePopsWithoutScoring()
    {
        Rig rig = new();
        Place(rig, Slot, 0x3A);
        rig.Objects.Slot(Slot).EnemyType = 0x04;

        rig.Pop.Update();

        Assert.Equal(0x3C, rig.Objects.Slot(Slot).Type);
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
        rig.Objects.Slot(12).X = (byte)(0x80 + apart);
        Place(rig, 13, 0x48);

        rig.Pop.Update();

        Assert.Equal(popped ? 0x34 : 0x04, rig.Objects.Slot(12).Type);
        Assert.Equal(0x48, rig.Objects.Slot(13).Type);

        if (popped)
        {
            Assert.Equal(0x04, rig.Objects.Slot(12).EnemyType);
            Assert.Equal(1, rig.Objects.Slot(12).Direction);
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

        Assert.Equal(0x3C, rig.Objects.Slot(8).Type);
        Assert.Equal(0x34, rig.Objects.Slot(12).Type);
    }

    // $3D3C. A bubble still under way pops nothing else.
    [Fact]
    public void AMovingBubblePopsNothingElse()
    {
        Rig rig = new();
        Touched(rig, Slot, 0x04, player: 0);
        rig.Objects.Slot(Slot).State = 0x05;
        Place(rig, 8, 0x04);

        rig.Pop.Update();

        Assert.Equal(0x04, rig.Objects.Slot(8).Type);
    }

    // $3D82. $1C is enemy 2, slot 4. It goes back in the state $1090 kept, $1A6F kills it, and the
    // bubble goes straight to $36.
    [Fact]
    public void LetsTheEnemyOutDead()
    {
        Rig rig = new();
        Touched(rig, Slot, 0x1C, player: 0);
        rig.Objects.Slot(Slot).Variant = 0x03;
        rig.Entities.EnemyCount = 5;

        rig.Pop.Update();

        Assert.Equal(0x0B, rig.Entities.Slot(4).State);
        Assert.Equal(0x03, rig.Entities.Slot(4).LeftFlag);
        Assert.Equal(4, rig.Entities.EnemyCount);
        Assert.Equal(1, rig.Pop.Chain[0]);
        Assert.Equal(0x01, rig.Entities.Slot(4).AttackTimer);
        Assert.Equal(0x36, rig.Objects.Slot(Slot).Type);
        Assert.Equal(new byte[6], rig.Scores.Bytes.ToArray());
    }

    // $3D91. The enemy's sprite comes back on in $D015, whichever way $142E's flicker left it, and
    // no other slot's bit moves.
    [Fact]
    public void TurnsTheEnemysSpriteBackOn()
    {
        Rig rig = new();
        Touched(rig, Slot, 0x1C, player: 0);
        rig.Entities.SpriteEnable = 0xE3;

        rig.Pop.Update();

        Assert.Equal(0xF3, rig.Entities.SpriteEnable);
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

    // $3E06-$3E29: a water bubble with no water running starts it at the bubble's cell and is gone at once.
    [Fact]
    public void AWaterBubbleStartsTheWaterAndVanishes()
    {
        Rig rig = new();
        Touched(rig, Slot, 0x06, player: 0);

        rig.Pop.Update();

        Assert.Equal(0xFF, rig.Water.Mask);
        Assert.All(rig.Water.Columns.ToArray(), c => Assert.Equal(0x0D, c));
        Assert.All(rig.Water.Rows.ToArray(), r => Assert.Equal(0x0E, r));
        Assert.Equal(0x36, rig.Objects.Slot(Slot).Type);
        Assert.Equal(BubblePop.NotDrawn, rig.Pop.Drawn[Slot]);
    }

    // $3E08: water already running, so the bubble just pops.
    [Fact]
    public void AWaterBubbleWhileWaterRunsPops()
    {
        Rig rig = new();
        rig.Water.Mask = 0x04;
        Touched(rig, Slot, 0x06, player: 0);

        rig.Pop.Update();

        Assert.Equal(0x04, rig.Water.Mask);
        Assert.Equal(0x3C, rig.Objects.Slot(Slot).Type);
    }

    // $3E1B: a row of $1C or more leaves no room below, so the cells' columns change but the water does not start.
    [Fact]
    public void AWaterBubbleTooLowPopsAndLeavesItsColumns()
    {
        Rig rig = new();
        Touched(rig, Slot, 0x06, player: 0);
        rig.Objects.Slot(Slot).Row = 0x1C;

        rig.Pop.Update();

        Assert.Equal(0, rig.Water.Mask);
        Assert.Equal(0x0D, rig.Water.Columns[0]);
        Assert.Equal(0x1D, rig.Water.Rows[0]);
        Assert.Equal(0x3C, rig.Objects.Slot(Slot).Type);
    }

    // $E758 picks a cell by $A9C4 and keeps $A9D6, which the fall toggles; $3E94 draws the bolt, and clears OLDTXT.
    [Theory]
    [InlineData(0x24, 0, 0x04, 67, 0x04)]
    [InlineData(0x24, 3, 0x00, 70, 0x00)]
    [InlineData(0x26, 0, 0x04, 71, 0x00)]
    public void TheFireDropAndTheBoltAreDrawnByTheirOwnVectors(byte type, byte subX, byte subY, int cell, byte drawnSubY)
    {
        Rig rig = new();
        Place(rig, Slot, type);
        rig.Objects.Slot(Slot).SubX = subX;
        rig.Objects.Slot(Slot).SubY = subY;

        rig.Pop.Update();

        Assert.Equal(cell, rig.Pop.Drawn[Slot]);
        Assert.Equal(drawnSubY, rig.Pop.DrawnSubY[Slot]);
    }

    // $3F88 draws the fruit from the round's item art, in the fruit's own colour RAM byte; everything else wears the playfield's.
    [Fact]
    public void TheFruitIsDrawnFromTheRoundsArtInItsOwnColour()
    {
        Rig rig = new();
        Place(rig, Slot, 0x4C);
        rig.Objects.Slot(Slot).Behaviour = 0x0B;
        rig.Objects.Slot(Slot).SubY = 0x04;
        Place(rig, Slot - 1, 0x00);
        rig.Pop.FruitArt = 0x03;

        rig.Pop.Update();

        Assert.Equal(75, rig.Pop.Drawn[Slot]);
        Assert.Equal(0x04, rig.Pop.DrawnSubY[Slot]);
        Assert.Equal(0x0B, rig.Pop.DrawnColour[Slot]);
        Assert.Equal(0x0D, rig.Pop.DrawnColour[Slot - 1]);
    }

    // $3E30-$3E5B: the bolt starts on the grid and goes the way the player who burst it faces.
    [Theory]
    [InlineData(0, 0x84)]
    [InlineData(3, 0x84)]
    [InlineData(4, 0x04)]
    [InlineData(7, 0x04)]
    [InlineData(8, 0x84)]
    [InlineData(9, 0x84)]
    [InlineData(10, 0x04)]
    public void AThunderBubbleBecomesABoltThrownTheWayThePlayerFaces(byte frame, byte direction)
    {
        Rig rig = new();
        Touched(rig, Slot, 0x0A, player: 1);
        rig.Entities.Slot(1).Frame = frame;
        rig.Objects.Slot(Slot).X = 0x83;

        rig.Pop.Update();

        Assert.Equal(0x26, rig.Objects.Slot(Slot).Type);
        Assert.Equal(0x7C, rig.Objects.Slot(Slot).X);
        Assert.Equal(direction, rig.Objects.Slot(Slot).Direction);
    }

    // $3E5E-$3E6B: a fire bubble becomes the falling fire, with $A9D6 cleared.
    [Fact]
    public void AFireBubbleBecomesTheFallingFire()
    {
        Rig rig = new();
        Touched(rig, Slot, 0x08, player: 0);
        rig.Objects.Slot(Slot).SubY = 0x04;

        rig.Pop.Update();

        Assert.Equal(0x24, rig.Objects.Slot(Slot).Type);
        Assert.Equal(0, rig.Objects.Slot(Slot).SubY);
    }

    // $3EFD, $E752, $3ED4 and $3E77 pick a cell by $A9C4, and clear OLDTXT.
    [Theory]
    [InlineData(0x28, 0, 58)]
    [InlineData(0x28, 3, 61)]
    [InlineData(0x2A, 0, 56)]
    [InlineData(0x2A, 1, 57)]
    [InlineData(0x2C, 0, 62)]
    [InlineData(0x2C, 3, 65)]
    [InlineData(0x32, 2, 66)]
    public void AShotIsDrawnByItsOwnVector(byte type, byte subX, byte cell)
    {
        Rig rig = new();
        Place(rig, Slot, type);
        rig.Objects.Slot(Slot).SubX = subX;
        rig.Objects.Slot(Slot).SubY = 0x04;

        rig.Pop.Update();

        Assert.Equal(cell, rig.Pop.Drawn[Slot]);
        Assert.Equal(0, rig.Pop.DrawnSubY[Slot]);
        Assert.Equal(type, rig.Objects.Slot(Slot).Type);
    }

    // $E767. $30 | $E765 by slot | bit 1 of $597F, after $E90E counts it up.
    [Theory]
    [InlineData(0, 0x2E, 0x00, 54)]
    [InlineData(1, 0x30, 0x00, 52)]
    [InlineData(0, 0x2E, 0x01, 55)]
    [InlineData(1, 0x30, 0x02, 53)]
    [InlineData(1, 0x30, 0x03, 52)]
    public void TheBaronIsDrawnBySlotAndCounter(int slot, byte type, byte counter, byte cell)
    {
        Rig rig = new();
        Place(rig, slot, type);
        rig.Pop.Counter = counter;

        rig.Pop.Update();

        Assert.Equal(cell, rig.Pop.Drawn[slot]);
    }

    // $E779 draws at $A9D6.
    [Fact]
    public void ABubbleIsDrawnOnItsOwnSubRow()
    {
        Rig rig = new();
        Place(rig, Slot, 0x02);
        rig.Objects.Slot(Slot).SubY = 0x04;

        rig.Pop.Update();

        Assert.Equal(0x04, rig.Pop.DrawnSubY[Slot]);
    }

    // $E779 draws at $DC and $EE as they are when $E90E reaches the slot; the mover ($0CF2) changes them afterwards,
    // and what is on the screen stays the picture made before.
    [Fact]
    public void ABubbleStaysDrawnWhereItWasWhenTheMoverMovesIt()
    {
        Rig rig = new();
        Place(rig, Slot, 0x02);
        rig.Objects.Slot(Slot).Column = 12;
        rig.Objects.Slot(Slot).Row = 20;

        rig.Pop.Update();
        rig.Objects.Slot(Slot).Row = 19;
        rig.Objects.Slot(Slot).Column = 13;

        Assert.Equal(12, rig.Pop.DrawnColumn[Slot]);
        Assert.Equal(20, rig.Pop.DrawnRow[Slot]);
    }

    // Past its table, a shot's vector would read the next table.
    [Theory]
    [InlineData(0x2A, 2)]
    [InlineData(0x28, 4)]
    public void AShotTurnedPastItsTableThrows(byte type, byte subX)
    {
        Rig rig = new();
        Place(rig, Slot, type);
        rig.Objects.Slot(Slot).SubX = subX;

        Assert.Throws<NotSupportedException>(rig.Pop.Update);
    }

    private static void Touched(Rig rig, int slot, byte held, int player)
    {
        Place(rig, slot, 0x34);
        rig.Objects.Slot(slot).EnemyType = held;
        rig.Objects.Slot(slot).Direction = (byte)player;
    }

    private static void Place(Rig rig, int slot, byte type)
    {
        rig.Objects.Slot(slot).Type = type;
        rig.Objects.Slot(slot).X = 0x80;
        rig.Objects.Slot(slot).Y = 0x80;
        rig.Objects.Slot(slot).Column = 0x0C;
        rig.Objects.Slot(slot).Row = 0x0D;
    }

    private sealed class Rig
    {
        internal Rig()
        {
            Water = new(Entities, new(new FakeRandomSource()));
            Pop = new(Objects, Entities, Players, new(Entities, Scores, new(new FakeRandomSource())), Scores, Water);
        }

        internal WaterFlow Water { get; }

        internal ObjectTable Objects { get; } = new();

        internal EntityTable Entities { get; } = new();

        internal PlayerTable Players { get; } = new();

        internal Scores Scores { get; } = new();

        internal BubblePop Pop { get; }
    }
}
