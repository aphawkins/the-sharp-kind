// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Views;

// The model turns the bytes the game keeps into the characters the HUD shows, which is the whole of
// what $E3D9 and $3FB0 do between them. Both rules are proved outright: a nibble is a digit, high
// nibble first, and a leading zero is a space until a digit that is not one has been seen.
public sealed class HudModelTests
{
    [Theory]

    // Every digit significant, so every digit drawn.
    [InlineData(0x12, 0x34, 0x56, "123456")]

    // The high nibble of the first byte is the leading digit, and a zero there is blanked.
    [InlineData(0x01, 0x23, 0x45, " 12345")]
    [InlineData(0x00, 0x12, 0x34, "  1234")]

    // Once a digit that is not zero has been seen, the zeros after it are digits again.
    [InlineData(0x10, 0x00, 0x00, "100000")]
    [InlineData(0x00, 0x00, 0x10, "    10")]

    // $3FB0 always writes the last digit, so a score of nothing is a single zero.
    [InlineData(0x00, 0x00, 0x00, "     0")]
    [InlineData(0x00, 0x00, 0x01, "     1")]
    public void ReadsAScoreAsSixDigitsWithLeadingZerosBlanked(byte high, byte mid, byte low, string expected)
    {
        HudModel model = Score([high, mid, low]);

        Assert.Equal(expected, model.PlayerOneScore);
    }

    [Fact]
    public void KeepsTheThreeScoresApart()
    {
        HudModel model = new([0x00, 0x01, 0x11], [0x00, 0x02, 0x22], [0x00, 0x03, 0x33], 3, 3);

        Assert.Equal("   111", model.PlayerOneScore);
        Assert.Equal("   222", model.PlayerTwoScore);
        Assert.Equal("   333", model.HighScore);
    }

    [Fact]
    public void ASixDigitScoreIsAlwaysSixCharacters()
    {
        HudModel model = Score([0x00, 0x00, 0x00]);

        Assert.Equal(HudModel.ScoreDigits, model.PlayerOneScore.Length);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public void RejectsAScoreThatIsNotThreeBytes(int bytes)
    {
        byte[] score = new byte[bytes];

        Assert.Throws<ArgumentException>(
            () => new HudModel(score, new byte[HudModel.ScoreBytes], new byte[HudModel.ScoreBytes], 3, 3));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(3, 3)]

    // The row holds seven, and $046C stops when it runs out of cells rather than out of lives.
    [InlineData(7, 7)]
    [InlineData(9, 7)]

    // A negative count means the player is out, and $046C writes nothing at all.
    [InlineData(-1, 0)]
    public void DrawsAsManyMarkersAsTheRowHolds(int lives, int expected)
        => Assert.Equal(expected, HudModel.LifeMarkers(lives));

    [Theory]
    [InlineData(3, true)]
    [InlineData(0, true)]
    [InlineData(-1, false)]
    public void APlayerIsOutOnlyOnceTheCountGoesNegative(int lives, bool expected)
        => Assert.Equal(expected, HudModel.IsPlaying(lives));

    [Fact]
    public void KeepsTheTwoPlayersLivesApart()
    {
        HudModel model = new(
            new byte[HudModel.ScoreBytes],
            new byte[HudModel.ScoreBytes],
            new byte[HudModel.ScoreBytes],
            3,
            -1);

        Assert.Equal(3, model.PlayerOneLives);
        Assert.Equal(-1, model.PlayerTwoLives);
    }

    private static HudModel Score(byte[] score)
        => new(score, new byte[HudModel.ScoreBytes], new byte[HudModel.ScoreBytes], 3, 3);
}
