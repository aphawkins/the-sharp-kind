// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharp.Renditions.EightBit;
using SharpKind.Graphics.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Views;

// The fruit is drawn in its own colour RAM byte, so its slot's cell comes from a copy of the sheet repainted for it.
public sealed class ObjectView8BitColourTests
{
    private const int Colours = 0x69;

    [Fact]
    public void DrawsEachObjectFromTheSheetPaintedInItsOwnColour()
    {
        byte[] drawn = [4, 72, .. Enumerable.Repeat(ObjectsModel.NotDrawn, 16)];
        byte[] zeros = new byte[ObjectsModel.Capacity];
        byte[] colours = [0x0D, 0x0B, .. new byte[16]];

        RecordingGraphics graphics = new(320, 200);

        new EightBitRendition().CreateObjectView(new TestSurface(graphics, "ObjectSprites"))
            .Draw(new ObjectsModel(drawn, zeros, zeros, zeros, Colours) { SlotColours = colours });

        Assert.Equal(["ObjectSprites.11.Recoloured", "ObjectSprites.13.Recoloured"], graphics.ImageParts.Select(x => x.ImageType));
        Assert.Equal(TestSurface.Colour(5), graphics.Image("ObjectSprites.13.Recoloured").GetPixel(3, 0));
        Assert.Equal(TestSurface.Colour(3), graphics.Image("ObjectSprites.11.Recoloured").GetPixel(3, 0));
    }
}
