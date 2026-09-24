// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Numerics;
using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharp.Renditions.EightBit;
using SharpKind.Graphics;
using SharpKind.Graphics.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Views;

// $1844's L_1934 and L_186B: one 2x2 block of characters per item that shows, at its level cell,
// from the sheet painted in its own colour RAM byte.
public sealed class ItemView8BitTests
{
    // A level colour byte of $69: entries 01 and 10 are colours 6 and 9.
    private const int Colours = 0x69;

    // Level 1's cells, and the food $10's and the special item $1E's art and colour.
    private static readonly byte[] s_column = [9, 21];
    private static readonly byte[] s_row = [7, 7];
    private static readonly byte[] s_art = [0x26, 0x1A];
    private static readonly byte[] s_colour = [0x0B, 0x0C];

    [Fact]
    public void DrawsNothingWhileBothAreHidden() => Assert.Empty(Draw([0x90, 0x9E]).ImageParts);

    // The special item first, then the food, each two characters square.
    [Fact]
    public void DrawsEachItemThatShowsAtItsCell()
    {
        RecordingGraphics graphics = Draw([0x10, 0x1E]);
        BbViewLayout layout = new(320, 200);

        Assert.Equal(["ItemChars.Special.Recoloured", "ItemChars.Food.Recoloured"], graphics.ImageParts.Select(x => x.ImageType));
        Assert.Equal(layout.Playfield.Cell(21, 7), graphics.ImageParts[0].Position);
        Assert.Equal(layout.Playfield.Cell(9, 7), graphics.ImageParts[1].Position);
        Assert.Equal(new Vector2(16, 16), graphics.ImageParts[1].Size);
        Assert.Equal(new Vector2(0x26 * 8, 0), graphics.ImageParts[1].SourcePosition);
        Assert.Equal(new Vector2(8, 16), graphics.ImageParts[1].SourceSize);
    }

    [Fact]
    public void DrawsOnlyTheItemThatShows()
    {
        RecordingGraphics graphics = Draw([0x10, 0x9E]);

        Assert.Equal(["ItemChars.Food.Recoloured"], graphics.ImageParts.Select(x => x.ImageType));
    }

    // Entry 11 is the low three bits of the item's colour RAM byte, not the playfield's 5.
    [Fact]
    public void PaintsEachItemInItsOwnColour()
    {
        RecordingGraphics graphics = Draw([0x10, 0x1E]);

        FastBitmap food = graphics.Image("ItemChars.Food.Recoloured");
        FastBitmap special = graphics.Image("ItemChars.Special.Recoloured");

        Assert.Equal(TestSurface.Colour(6), food.GetPixel(1, 0));
        Assert.Equal(TestSurface.Colour(9), food.GetPixel(2, 0));
        Assert.Equal(TestSurface.Colour(0x0B & 7), food.GetPixel(3, 0));
        Assert.Equal(TestSurface.Colour(0x0C & 7), special.GetPixel(3, 0));
    }

    private static RecordingGraphics Draw(byte[] type)
    {
        RecordingGraphics graphics = new(320, 200);
        EightBitRendition rendition = new();

        rendition.CreateItemView(new TestSurface(graphics, "ItemChars"))
            .Draw(new ItemsModel(type, s_column, s_row, s_art, s_colour, Colours));

        return graphics;
    }
}
