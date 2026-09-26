// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Players;

// $1D32, worked by hand off the reference. Player 1 at ($40, $80) is measured from ($42, $82).
public sealed class PlayerDeathTests
{
    private const int Enemy = 3;

    [Theory]
    [InlineData(0x4D, 0x82, 0x0E)]
    [InlineData(0x4E, 0x82, 0x01)]
    [InlineData(0x37, 0x82, 0x0E)]
    [InlineData(0x36, 0x82, 0x01)]
    [InlineData(0x42, 0x77, 0x0E)]
    [InlineData(0x42, 0x76, 0x01)]
    [InlineData(0x42, 0x8D, 0x0E)]
    [InlineData(0x42, 0x8E, 0x01)]
    public void AnEnemyWithinElevenPixelsKills(byte x, byte y, byte expected)
    {
        EntityTable entities = Rig(0x40, 0x80, x, y, 0x02);

        new PlayerDeath(entities).Update();

        Assert.Equal(expected, entities.State[0]);
    }

    // $1D4D and $1D4F. An empty slot and a popped enemy do not kill; $0A, the last walking state, does.
    [Theory]
    [InlineData(0x00, 0x01)]
    [InlineData(0x0B, 0x01)]
    [InlineData(0x0A, 0x0E)]
    [InlineData(0x01, 0x0E)]
    public void OnlyStatesOneToTenKill(byte state, byte expected)
    {
        EntityTable entities = Rig(0x40, 0x80, 0x42, 0x82, state);

        new PlayerDeath(entities).Update();

        Assert.Equal(expected, entities.State[0]);
    }

    // $1D3D's carry. X $FE + 2 wraps, and the carry makes the Y point $83: an enemy at $8E is $0B away.
    [Fact]
    public void AWrappedXCarriesIntoY()
    {
        EntityTable entities = Rig(0xFE, 0x80, 0x00, 0x8E, 0x02);

        new PlayerDeath(entities).Update();

        Assert.Equal(0x0E, entities.State[0]);
    }

    // $1D36. A player not walking about is left alone.
    [Fact]
    public void ADyingPlayerIsNotKilledAgain()
    {
        EntityTable entities = Rig(0x40, 0x80, 0x42, 0x82, 0x02);
        entities.State[0] = 0x10;

        new PlayerDeath(entities).Update();

        Assert.Equal(0x10, entities.State[0]);
    }

    private static EntityTable Rig(byte playerX, byte playerY, byte enemyX, byte enemyY, byte enemyState)
    {
        EntityTable entities = new();
        entities.State[0] = PlayerFrame.PlayingState;
        entities.X[0] = playerX;
        entities.Y[0] = playerY;
        entities.State[Enemy] = enemyState;
        entities.X[Enemy] = enemyX;
        entities.Y[Enemy] = enemyY;

        return entities;
    }
}
