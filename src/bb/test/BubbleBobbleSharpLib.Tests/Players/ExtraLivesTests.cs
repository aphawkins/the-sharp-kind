// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Players;

// $F1AC, worked by hand off the reference.
public sealed class ExtraLivesTests
{
    // $F1BC-$F1C7: the first mark is the score's middle byte reaching $30 (30,000).
    [Fact]
    public void TheFirstMarkIsThirtyThousand()
    {
        (ExtraLives extra, PlayerTable players, Scores scores) = Build();
        players.Lives[0] = 3;

        scores.Add(1, 0x29);
        extra.Update();
        Assert.Equal(3, players.Lives[0]);

        scores.Add(1, 0x01);
        extra.Update();
        Assert.Equal(4, players.Lives[0]);
        Assert.Equal(1, players.ExtraLifeStep[0]);
        Assert.Equal(0, players.ExtraLifeByte[0]);
    }

    // $F1D0-$F1D8: the first award moves to the top byte, whose mark is $01 (100,000); the life is given once.
    [Fact]
    public void TheSecondMarkIsOneHundredThousandAndEachIsGivenOnce()
    {
        (ExtraLives extra, PlayerTable players, Scores scores) = Build();
        players.Lives[0] = 3;
        scores.Add(1, 0x30);

        extra.Update();
        extra.Update();
        Assert.Equal(4, players.Lives[0]);

        scores.Add(0, 0x01);
        extra.Update();
        extra.Update();
        Assert.Equal(5, players.Lives[0]);
        Assert.Equal(2, players.ExtraLifeStep[0]);
    }

    // $F1AE-$F1B5: player 2 reads its own three bytes.
    [Fact]
    public void ThePlayersAreSeparate()
    {
        (ExtraLives extra, PlayerTable players, Scores scores) = Build();
        players.Lives[0] = 3;
        players.Lives[1] = 3;
        players.ExtraLifeByte[1] = 4;
        scores.Add(4, 0x35);

        extra.Update();

        Assert.Equal(3, players.Lives[0]);
        Assert.Equal(4, players.Lives[1]);
    }

    // $F1B0-$F1B5: a score byte above the mark's own byte, on the first step, is a life at once.
    [Fact]
    public void ANonZeroTopByteBeforeTheFirstStepIsALife()
    {
        (ExtraLives extra, PlayerTable players, Scores scores) = Build();
        players.Lives[0] = 3;
        players.ExtraLifeByte[0] = 1;
        scores.Add(0, 0x01);

        extra.Update();

        Assert.Equal(4, players.Lives[0]);
    }

    private static (ExtraLives Extra, PlayerTable Players, Scores Scores) Build()
    {
        PlayerTable players = new();
        Scores scores = new();
        return (new(players, scores), players, scores);
    }
}
