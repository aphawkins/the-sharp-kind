// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Items;

// $2D65's handlers that are translated, worked by hand off the reference.
public sealed class ItemEffectsTests
{
    // $7F53's values, which a player starts with.
    [Fact]
    public void APlayerStartsWithPlainBubbles()
    {
        PlayerTable players = new();

        Assert.Equal([0x08, 0x88, 0x04, 0x04], Blow(players, 1));
        Assert.Equal([0x00, 0x00, 0x00], Rings(players, 1));
    }

    // $2DAB, $2DB2, $2DCD and $2DC7, which is all three.
    [Theory]
    [InlineData(3, 0x03, 0x88, 0x04, 0x04)]
    [InlineData(4, 0x08, 0x8C, 0xFF, 0x04)]
    [InlineData(5, 0x08, 0x88, 0x04, 0xFF)]
    [InlineData(10, 0x03, 0x8C, 0xFF, 0xFF)]
    public void CandiesChangeWhatABlowMakes(int type, byte reload, byte state, byte variant, byte kind)
    {
        (PlayerTable players, ItemEffects effects) = Create();

        effects.Apply(type, 1);

        Assert.Equal([reload, state, variant, kind], Blow(players, 1));
        Assert.Equal([0x08, 0x88, 0x04, 0x04], Blow(players, 0));
    }

    // $2F5F, $2F62 and $2F65: the rings are turned on by a decrement from zero.
    [Theory]
    [InlineData(12, 0xFF, 0x00, 0x00)]
    [InlineData(13, 0x00, 0xFF, 0x00)]
    [InlineData(14, 0x00, 0x00, 0xFF)]
    public void RingsTurnOnOneEach(int type, byte drift, byte walk, byte blow)
    {
        (PlayerTable players, ItemEffects effects) = Create();

        effects.Apply(type, 0);

        Assert.Equal([drift, walk, blow], Rings(players, 0));
    }

    // $2DBE. Every ring and every candy.
    [Fact]
    public void TheCrystalGivesEverything()
    {
        (PlayerTable players, ItemEffects effects) = Create();

        effects.Apply(9, 0);

        Assert.Equal([0xFF, 0xFF, 0xFF], Rings(players, 0));
        Assert.Equal([0x03, 0x8C, 0xFF, 0xFF], Blow(players, 0));
    }

    // final_routine.
    [Fact]
    public void TypeZeroFlashesThePlayer()
    {
        PlayerTable players = new();
        EntityTable entities = new();

        new ItemEffects(players, entities).Apply(0, 1);

        Assert.Equal(0xFE, entities.FlashTimer[1]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(20)]
    [InlineData(34)]
    public void ThrowsForAnEffectNotTranslated(int type)
    {
        (_, ItemEffects effects) = Create();

        Assert.Throws<NotSupportedException>(() => effects.Apply(type, 0));
    }

    private static (PlayerTable Players, ItemEffects Effects) Create()
    {
        PlayerTable players = new();
        return (players, new(players, new()));
    }

    private static byte[] Blow(PlayerTable p, int i) => [p.BlowReload[i], p.BlowState[i], p.BlowVariant[i], p.BlowType[i]];

    private static byte[] Rings(PlayerTable p, int i) => [p.DriftRing[i], p.WalkRing[i], p.BlowRing[i]];
}
