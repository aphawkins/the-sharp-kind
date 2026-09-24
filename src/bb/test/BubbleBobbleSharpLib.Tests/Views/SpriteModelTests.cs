// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Views;

// $1805 is short enough to prove outright: the position bytes pass through, and the pointer and the
// colour are a mask, an add and one override. These check each of them separately, because a model
// that passed the wrong byte through would still draw something.
public sealed class SpriteModelTests
{
    // A slot's position bytes reach the VIC's registers untouched - $1809 and $180E are a load and a
    // store with nothing between them - so the model carries them as they stand.
    [Theory]
    [InlineData(0x2C, 0xDD)]
    [InlineData(0xEC, 0xDD)]
    [InlineData(0x00, 0xFF)]
    public void CarriesThePositionBytesUntouched(byte x, byte y)
    {
        SpriteModel model = Model(x: Slots(x), y: Slots(y));

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
        => Assert.Equal(sprite, Model(frame: Slots(frame)).Sprite(0));

    // $8598 is added to the masked frame, and the sheet starts at pointer $60 - so a player's base of
    // $60 lands at its first sprite, and an enemy's is counted from the same place.
    [Theory]
    [InlineData(0x60, 0x00, 0x00)]
    [InlineData(0x73, 0x00, 0x13)]
    [InlineData(0x73, 0x0A, 0x1D)]
    [InlineData(0xBB, 0x04, 0x5F)]
    [InlineData(0xA1, 0x1F, 0x60)]
    public void AddsTheSpriteBase(byte spriteBase, byte frame, int sprite)
    {
        SpriteModel model = Model(frame: Slots(frame), spriteBase: Slots(spriteBase));

        Assert.Equal(sprite, model.Sprite(0));
        Assert.True(model.IsDrawn(0));
    }

    // A pointer outside $5800's 97 sprites is not drawn. Food's base, $F2, is the one the port makes,
    // and its sprites are built at run time by $2921, which is not translated.
    [Theory]
    [InlineData(0xF2, 0x02)]
    [InlineData(0x5F, 0x00)]
    [InlineData(0xA2, 0x1F)]
    public void DrawsNothingOutsideTheSheet(byte spriteBase, byte frame)
        => Assert.False(Model(frame: Slots(frame), spriteBase: Slots(spriteBase)).IsDrawn(0));

    // $1817. A slot whose state byte is zero holds nothing.
    [Theory]
    [InlineData(0x00, false)]
    [InlineData(0x01, true)]
    [InlineData(0x0F, true)]
    public void ReadsAZeroStateByteAsAnEmptySlot(byte state, bool drawn)
        => Assert.Equal(drawn, Model(state: Slots(state)).IsDrawn(0));

    // The slots are independent, which is the whole reason the reference indexes these arrays rather
    // than naming bytes for each.
    [Fact]
    public void KeepsTheSlotsApart()
    {
        SpriteModel model = Model(
            state: [0x01, 0x00, 0x02, 0, 0, 0, 0, 0x09],
            x: [0x2C, 0xEC, 0x80, 0, 0, 0, 0, 0x40],
            y: [0xDD, 0x40, 0x55, 0, 0, 0, 0, 0x66],
            frame: [0x00, 0x04, 0x03, 0, 0, 0, 0, 0x01],
            spriteBase: [0x60, 0x60, 0x73, 0, 0, 0, 0, 0xBB],
            colour: [0x05, 0x03, 0x0C, 0, 0, 0, 0, 0x02]);

        Assert.Equal(
            [true, false, true, false, false, false, false, true],
            Enumerable.Range(0, SpriteModel.Capacity).Select(model.IsDrawn));
        Assert.Equal(0xEC, model.X(1));
        Assert.Equal(0x40, model.X(7));
        Assert.Equal(0x55, model.Y(2));
        Assert.Equal(0x04, model.Sprite(1));
        Assert.Equal(0x16, model.Sprite(2));
        Assert.Equal(0x5C, model.Sprite(7));
        Assert.Equal(0x0C, model.Colour(2));
        Assert.Equal(0x02, model.Colour(7));
    }

    // The VIC's sprite colour register is four bits wide, so a colour byte says no more than which
    // of the sixteen it is.
    [Theory]
    [InlineData(0x05, 0x05)]
    [InlineData(0x03, 0x03)]
    [InlineData(0x1D, 0x0D)]
    public void MasksTheColourToANibble(byte colour, int entry)
        => Assert.Equal(entry, Model(colour: Slots(colour)).Colour(0));

    // $1822. With its flash timer running, a slot below state $11 takes $8570's colour: 10 for every
    // enemy, and each player's own colour for the players.
    [Theory]
    [InlineData(2, 0x02, 0xFF, 0x0A)]
    [InlineData(7, 0x09, 0x01, 0x0A)]
    [InlineData(2, 0x10, 0xFF, 0x0A)]
    [InlineData(2, 0x02, 0x00, 0x0C)]
    [InlineData(2, 0x11, 0xFF, 0x0C)]
    [InlineData(0, 0x01, 0xFE, 0x05)]
    [InlineData(1, 0x01, 0xFE, 0x03)]
    public void FlashesInTheSlotsFlashColour(int slot, byte state, byte flashTimer, int colour)
    {
        byte[] states = new byte[SpriteModel.Capacity];
        byte[] timers = new byte[SpriteModel.Capacity];
        byte[] colours = new byte[SpriteModel.Capacity];
        states[slot] = state;
        timers[slot] = flashTimer;
        colours[slot] = 0x0C;

        SpriteModel model = Model(state: states, colour: colours, flashTimer: timers);

        Assert.Equal(colour, model.Colour(slot));
    }

    // Every one of the seven is indexed by slot, so a caller handing over fewer bytes than there are
    // sprites has the wrong arrays rather than a smaller game.
    [Fact]
    public void RefusesFewerBytesThanThereAreSprites()
    {
        byte[] all = new byte[SpriteModel.Capacity];
        byte[] one = new byte[1];

        Assert.Throws<ArgumentException>("state", () => new SpriteModel(one, all, all, all, all, all, all));
        Assert.Throws<ArgumentException>("x", () => new SpriteModel(all, one, all, all, all, all, all));
        Assert.Throws<ArgumentException>("y", () => new SpriteModel(all, all, one, all, all, all, all));
        Assert.Throws<ArgumentException>("frame", () => new SpriteModel(all, all, all, one, all, all, all));
        Assert.Throws<ArgumentException>("spriteBase", () => new SpriteModel(all, all, all, all, one, all, all));
        Assert.Throws<ArgumentException>("colour", () => new SpriteModel(all, all, all, all, all, one, all));
        Assert.Throws<ArgumentException>("flashTimer", () => new SpriteModel(all, all, all, all, all, all, one));
    }

    // Slot 0 holds the byte given; the other seven are zero.
    private static byte[] Slots(byte first)
    {
        byte[] slots = new byte[SpriteModel.Capacity];
        slots[0] = first;

        return slots;
    }

    private static SpriteModel Model(
        byte[]? state = null,
        byte[]? x = null,
        byte[]? y = null,
        byte[]? frame = null,
        byte[]? spriteBase = null,
        byte[]? colour = null,
        byte[]? flashTimer = null)
        => new(
            state ?? Slots(0x01),
            x ?? Slots(0x00),
            y ?? Slots(0x00),
            frame ?? Slots(0x00),
            spriteBase ?? Slots(0x60),
            colour ?? Slots(0x00),
            flashTimer ?? Slots(0x00));
}
