// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Numerics;
using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharp.Renditions.EightBit;
using SharpKind.Graphics.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Views;

// $1B7D's water cells and $1319's strips of fire: each a character of effect-chars.tga at a level cell.
public sealed class EffectsView8BitTests
{
    private const int Colours = 0x69;

    private static readonly byte[] s_columns = [10, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19];
    private static readonly byte[] s_rows = [8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8];
    private static readonly byte[] s_none = [0, 0, 0, 0, 0];

    [Fact]
    public void DrawsNothingWithNoWaterAndNoFire() => Assert.Empty(Draw(false, s_none, s_none, s_none, s_none).ImageParts);

    // The tail is not drawn; the other ten cells are, head first.
    [Fact]
    public void DrawsTenWaterCellsButNotTheTail()
    {
        RecordingGraphics graphics = Draw(true, s_none, s_none, s_none, s_none);
        BbViewLayout layout = new(320, 200);

        Assert.Equal(10, graphics.ImageParts.Count);
        Assert.All(graphics.ImageParts, part => Assert.Equal("EffectChars.Water.Recoloured", part.ImageType));
        Assert.Equal(layout.Playfield.Cell(19, 8), graphics.ImageParts[0].Position);
        Assert.Equal(layout.Playfield.Cell(10, 8), graphics.ImageParts[9].Position);
        Assert.Equal(new Vector2(8, 8), graphics.ImageParts[0].Size);
        Assert.Equal(new Vector2(4, 0), graphics.ImageParts[0].SourcePosition);
        Assert.Equal(new Vector2(4, 8), graphics.ImageParts[0].SourceSize);
    }

    // $1B8B and $1B8F: a row above 4 or at $1D is not drawn.
    [Fact]
    public void DoesNotDrawWaterOutsideTheLevel()
    {
        byte[] rows = [0x1D, 0x1D, 0x03, 0x04, 0x1C, 0x1D, 0x1D, 0x1D, 0x1D, 0x1D, 0x1D];

        RecordingGraphics graphics = Draw(true, s_none, s_none, s_none, s_none, rows);

        Assert.Equal(2, graphics.ImageParts.Count);
    }

    // A strip is as many cells as its length, in the character its timer chose.
    [Fact]
    public void DrawsAStripAsALengthOfOneCharacter()
    {
        byte[] columns = [5, 0, 0, 0, 0];
        byte[] rows = [12, 0, 0, 0, 0];
        byte[] lengths = [3, 0, 0, 0, 0];
        byte[] characters = [0x41, 0, 0, 0, 0];

        RecordingGraphics graphics = Draw(false, columns, rows, lengths, characters);
        BbViewLayout layout = new(320, 200);

        Assert.Equal(3, graphics.ImageParts.Count);
        Assert.All(graphics.ImageParts, part => Assert.Equal("EffectChars.Fire.Recoloured", part.ImageType));
        Assert.Equal(layout.Playfield.Cell(5, 12), graphics.ImageParts[0].Position);
        Assert.Equal(layout.Playfield.Cell(7, 12), graphics.ImageParts[2].Position);
        Assert.Equal(new Vector2(12, 0), graphics.ImageParts[0].SourcePosition);
    }

    // Water's colour RAM is $0B, so entry 11 is 3; the fire wears the playfield's $0D, so 5.
    [Fact]
    public void PaintsTheWaterCyanAndTheFireInThePlayfieldColour()
    {
        byte[] characters = [0x1C, 0, 0, 0, 0];
        byte[] lengths = [1, 0, 0, 0, 0];

        RecordingGraphics graphics = Draw(true, s_none, s_none, lengths, characters);

        Assert.Equal(TestSurface.Colour(3), graphics.Image("EffectChars.Water.Recoloured").GetPixel(3, 0));
        Assert.Equal(TestSurface.Colour(5), graphics.Image("EffectChars.Fire.Recoloured").GetPixel(3, 0));
    }

    private static RecordingGraphics Draw(bool water, byte[] columns, byte[] rows, byte[] lengths, byte[] characters)
        => Draw(water, columns, rows, lengths, characters, s_rows);

    private static RecordingGraphics Draw(bool water, byte[] columns, byte[] rows, byte[] lengths, byte[] characters, byte[] waterRows)
    {
        RecordingGraphics graphics = new(320, 200);
        EightBitRendition rendition = new();

        rendition.CreateEffectsView(new TestSurface(graphics, "EffectChars"))
            .Draw(new EffectsModel(water, s_columns, waterRows, columns, rows, lengths, characters, Colours));

        return graphics;
    }
}
