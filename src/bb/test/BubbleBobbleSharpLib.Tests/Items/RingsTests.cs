// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using BubbleBobbleSharpLib.Tests.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Items;

// $2483, $22B4 and $23D7, worked by hand off the reference: ten points a step, a drift or a bubble
// while the ring is on, added to the player's lowest score byte by $7C21.
public sealed class RingsTests
{
    private const byte Idle = 0xFF;
    private const byte PushRight = Idle & unchecked((byte)~0x08);

    // Row 5, column 5, both fine offsets zero - where the walk's wall check runs.
    private const byte StartX = 0x44;
    private const byte StartY = 0x55;

    [Theory]
    [InlineData(0, 0x00, 0x00)]
    [InlineData(0, 0xFF, 0x01)]
    [InlineData(1, 0xFF, 0x01)]
    public void EachRingScoresTenPointsWhileItIsOn(int player, byte ring, byte score)
    {
        PlayerTable players = new();
        Scores scores = new();
        Rings rings = new(players, scores);
        players.DriftRing[player] = ring;
        players.WalkRing[player] = ring;
        players.BlowRing[player] = ring;

        rings.Drift(player);
        rings.Walk(player);
        rings.Blow(player);

        Assert.Equal((byte)(score * 3), scores.Bytes[Scores.Last(player)]);
    }

    // $22B4, after the wall check at $22A6: a step scores, and a wall that stops the step does not.
    [Theory]
    [InlineData(false, 0x01)]
    [InlineData(true, 0x00)]
    public void TheWalkRingScoresEachStepTheWallAllows(bool wall, byte score)
    {
        (EntityTable entities, PlayerTable players, Scores scores, Rings rings) = Create();
        players.WalkRing[0] = 0xFF;
        entities.X[0] = StartX;
        entities.Y[0] = StartY;
        entities.BubbleTimer[0] = 0xFF;
        PlayerMovement movement = new(entities, TestBlow.Of(entities), rings);

        movement.Step(0, PushRight, wall ? Map(6, 8) : Map());

        Assert.Equal(score, scores.Bytes[2]);
    }

    // $2483 is the jump's way into the drift, and it scores. A fall joins at $248D and does not.
    [Fact]
    public void TheDriftRingScoresOnlyFromTheJump()
    {
        (EntityTable entities, PlayerTable players, Scores scores, Rings rings) = Create();
        players.DriftRing[0] = 0xFF;
        entities.X[0] = StartX;
        entities.Y[0] = StartY;
        PlayerDrift drift = new(entities, new(entities, TestBlow.Of(entities)), rings);
        PlayerCell cell = PlayerCell.Of(StartX, StartY);

        drift.Step(0, Idle, cell, Map());

        Assert.Equal(0x00, scores.Bytes[2]);

        drift.Enter(0, Idle, cell, Map());

        Assert.Equal(0x01, scores.Bytes[2]);
    }

    // $23D7, as the bubble is made on the third frame of the blow.
    [Fact]
    public void TheBlowRingScoresTheBubble()
    {
        (EntityTable entities, PlayerTable players, Scores scores, Rings rings) = Create();
        players.BlowRing[0] = 0xFF;
        entities.State[0] = PlayerFrame.PlayingState;
        entities.X[0] = 0x8C;
        entities.Y[0] = 0xB5;
        entities.RiseCounter[0] = 0xFF;
        entities.FallCounter[0] = 0xFF;
        entities.GroundState[0] = 0xFF;
        entities.BubbleTimer[0] = 0xFF;
        ObjectTable objects = new();
        BubbleBlow blow = new(players, entities, objects, rings);

        blow.Blow(0);

        for (int frame = 0; frame < 2; frame++)
        {
            blow.Step(0, entities.BubbleTimer[0], PlayerCell.Of(entities.X[0], entities.Y[0]));
        }

        Assert.Equal(0x00, scores.Bytes[2]);

        blow.Step(0, entities.BubbleTimer[0], PlayerCell.Of(entities.X[0], entities.Y[0]));

        Assert.Equal(BubbleBlow.BubbleType, objects.Type[ObjectTable.Capacity - 1]);
        Assert.Equal(0x01, scores.Bytes[2]);
    }

    private static (EntityTable Entities, PlayerTable Players, Scores Scores, Rings Rings) Create()
    {
        PlayerTable players = new();
        Scores scores = new();
        return (new(), players, scores, new(players, scores));
    }

    // An open level, or one with a single solid cell at map row and column.
    private static SolidMap Map(int? row = null, int? column = null) => SolidMap.Build(
        new Level
        {
            Number = 1,
            Bitmap = [.. Enumerable.Range(0, 23).Select(r => r + 1 == row
                ? new string('.', column!.Value) + "#" + new string('.', 31 - column.Value)
                : new string('.', 32))],
        },
        []);
}
