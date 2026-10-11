// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Views;

// $2E20-$2E38, the wipe's row: of the 28 cells inside the walls (columns 2 to 29), `$10`-`$1E` stay and the rest become `$FF`.
public sealed class PlayfieldModelTests
{
    [Theory]
    [InlineData(0x20, 0xFF)]
    [InlineData(0x0A, 0xFF)]
    [InlineData(0x0F, 0xFF)]
    [InlineData(0x10, 0x10)]
    [InlineData(0x15, 0x15)]
    [InlineData(0x1E, 0x1E)]
    [InlineData(0x1F, 0xFF)]
    public void AWipedCellIsABlockUnlessItIsTheTile(byte character, byte wiped)
    {
        PlayfieldModel model = Model(character).Wiped(PlayfieldModel.Rows);

        Assert.Equal(wiped, model.Character(2, 0));
        Assert.Equal(wiped, model.Character(29, 24));
    }

    [Fact]
    public void TheWallsColumnsAreNotWiped()
    {
        PlayfieldModel model = Model(0x20).Wiped(PlayfieldModel.Rows);

        Assert.Equal(0x20, model.Character(1, 24));
        Assert.Equal(0x20, model.Character(30, 24));
        Assert.False(model.IsWiped(1, 24));
        Assert.True(model.IsWiped(2, 24));
    }

    [Fact]
    public void TheWipeGoesUpFromTheBottomRow()
    {
        PlayfieldModel model = Model(0x20).Wiped(3);

        Assert.False(model.IsWiped(21));
        Assert.True(model.IsWiped(22));
        Assert.Equal(0x20, model.Character(5, 21));
        Assert.Equal(PlayfieldModel.Block, model.Character(5, 22));
    }

    // The scroll to the next level carries the old level out wiped, and the freeze's colours do not undo it.
    [Fact]
    public void TheWipeStaysThroughAScrollAndNewColours()
    {
        PlayfieldModel model = Model(0x20).Wiped(4);

        Assert.Equal(4, model.Scrolled(-3, 0, 20).WipedRows);
        Assert.Equal(4, model.WithColours(0x12).WipedRows);
    }

    private static PlayfieldModel Model(byte character)
    {
        byte[] characters = new byte[PlayfieldModel.Columns * PlayfieldModel.Rows];
        Array.Fill(characters, character);

        return new(0, 0x69, characters);
    }
}
