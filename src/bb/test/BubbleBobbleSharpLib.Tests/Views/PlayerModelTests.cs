// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Views;

// $1805 is short enough to prove outright: four of the five things it writes are the bytes it was
// handed, and the fifth is a mask. These check each of them separately, because a model that passed
// the wrong byte through would still draw something.
public sealed class PlayerModelTests
{
    // A player's position bytes reach the VIC's registers untouched - $1809 and $180E are a load and
    // a store with nothing between them - so the model carries them as they stand.
    [Theory]
    [InlineData(0x2C, 0xDD)]
    [InlineData(0xEC, 0xDD)]
    [InlineData(0x00, 0xFF)]
    public void CarriesThePositionBytesUntouched(byte x, byte y)
    {
        PlayerModel model = Model(x: [x, 0], y: [y, 0]);

        Assert.Equal(x, model.X(0));
        Assert.Equal(y, model.Y(0));
    }

    // $182F masks the frame to five bits before it becomes a sprite pointer, so the sixteens and the
    // thirty-twos of a frame byte name no sprite at all.
    [Theory]
    [InlineData(0x00, 0x00)]
    [InlineData(0x04, 0x04)]
    [InlineData(0x1F, 0x1F)]
    [InlineData(0x20, 0x00)]
    [InlineData(0xA5, 0x05)]
    [InlineData(0xFF, 0x1F)]
    public void MasksTheFrameToThirtyTwoSprites(byte frame, int sprite)
        => Assert.Equal(sprite, Model(frame: [frame, 0]).Sprite(0));

    // $8598 is $60 for both players, which is the pointer the game's own sprite data starts at - so
    // adding it back on lands at the first sprite of the sheet, and the index is the masked frame.
    [Fact]
    public void CountsSpritesFromTheFirstInTheSheet()
        => Assert.Equal(0, Model(frame: [0x00, 0x00]).Sprite(0));

    // $1817. A slot whose state byte is zero holds nobody.
    [Theory]
    [InlineData(0x00, false)]
    [InlineData(0x01, true)]
    [InlineData(0x0F, true)]
    public void ReadsAZeroStateByteAsAnEmptySlot(byte state, bool active)
        => Assert.Equal(active, Model(state: [state, 0]).IsActive(0));

    // The two players are independent, which is the whole reason the reference indexes these arrays
    // rather than naming a pair of bytes each.
    [Fact]
    public void KeepsTheTwoPlayersApart()
    {
        PlayerModel model = Model(
            state: [0x01, 0x00],
            x: [0x2C, 0xEC],
            y: [0xDD, 0x40],
            frame: [0x00, 0x04],
            colour: [0x05, 0x03]);

        Assert.True(model.IsActive(0));
        Assert.False(model.IsActive(1));
        Assert.Equal(0x2C, model.X(0));
        Assert.Equal(0xEC, model.X(1));
        Assert.Equal(0xDD, model.Y(0));
        Assert.Equal(0x40, model.Y(1));
        Assert.Equal(0x00, model.Sprite(0));
        Assert.Equal(0x04, model.Sprite(1));
        Assert.Equal(0x05, model.Colour(0));
        Assert.Equal(0x03, model.Colour(1));
    }

    // The VIC's sprite colour register is four bits wide, so a colour byte says no more than which
    // of the sixteen it is.
    [Theory]
    [InlineData(0x05, 0x05)]
    [InlineData(0x03, 0x03)]
    [InlineData(0x1D, 0x0D)]
    public void MasksTheColourToANibble(byte colour, int entry)
        => Assert.Equal(entry, Model(colour: [colour, 0]).Colour(0));

    // Every one of the five is indexed by player number, so a caller handing over fewer bytes than
    // there are players has the wrong arrays rather than a smaller game.
    [Fact]
    public void RefusesFewerBytesThanThereArePlayers()
    {
        byte[] two = new byte[PlayerModel.Capacity];
        byte[] one = new byte[1];

        Assert.Throws<ArgumentException>("state", () => new PlayerModel(one, two, two, two, two));
        Assert.Throws<ArgumentException>("x", () => new PlayerModel(two, one, two, two, two));
        Assert.Throws<ArgumentException>("y", () => new PlayerModel(two, two, one, two, two));
        Assert.Throws<ArgumentException>("frame", () => new PlayerModel(two, two, two, one, two));
        Assert.Throws<ArgumentException>("colour", () => new PlayerModel(two, two, two, two, one));
    }

    private static PlayerModel Model(
        byte[]? state = null,
        byte[]? x = null,
        byte[]? y = null,
        byte[]? frame = null,
        byte[]? colour = null)
        => new(
            state ?? [0x01, 0x01],
            x ?? [0x00, 0x00],
            y ?? [0x00, 0x00],
            frame ?? [0x00, 0x00],
            colour ?? [0x00, 0x00]);
}
