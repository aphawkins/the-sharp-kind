// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Globalization;
using System.Numerics;
using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharp.Renditions.EightBit;
using BubbleBobbleSharpLib.Graphics;
using SharpKind.Assets;
using SharpKind.Assets.Palettes;
using SharpKind.Graphics;
using SharpKind.Graphics.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Views;

// Level 1's food as $1844 draws it, pixel for pixel against the C64.
//
// Captures/items-level-1.txt was read out of VICE running rebb64, on 2026-09-24, with one player and
// no intervention. Once $52 showed the food, the game stopped at $0A4B and the bytes were recorded:
// $4E to $60, the item's four cells in both screens and colour RAM, the characters at $4210 and
// $4A10, and the VIC-II's colours. The screenshot (items-level-1.png) was taken at the next $E90E,
// and its last block is the food's 16x16 pixels, at screen 104,91 inside VICE's 32x35 border, as C64
// colour numbers.
//
// The food is type $19 at column 9, row 7 (offset $0121), colour $09, on level 1's colour byte $21:
// $D022 is 2 and $D023 is 1, as the capture reads them.
[Trait("Level", "Integration")]
public sealed class ItemView8BitGoldenTests
{
    private const int FoodType = 0x19;
    private const int Column = 9;
    private const int Row = 7;
    private const byte FoodArt = 0x26;
    private const byte FoodColour = 0x09;
    private const int LevelColours = 0x21;

    // VICE's screenshot puts the level's top-left at 32,35; the port's is 0,0.
    private const int BorderLeft = 32;
    private const int BorderTop = 35;
    private const int ScreenLeft = 104;
    private const int ScreenTop = 91;
    private const int Block = 16;

    private static readonly string s_rendition = Path.Combine(
        AppContext.BaseDirectory, "Renditions", "BubbleBobbleSharp.Renditions.EightBit");

    // $4E/$50 and the four cells: 42 44 over 43 45, the characters in column order, every cell $x9.
    [Fact]
    public void TheCaptureDrewTheFoodWhereAndHowThePortSays()
    {
        string[] capture = Capture();

        Assert.Equal("item 0 type 19 offset 0121 colour 09", capture[1]);
        Assert.Equal(Column + (Row * 40), 0x0121);
        Assert.Equal("  5000+ 42444345", capture[2]);
        Assert.Equal("  D800+ f9f9f9f9", capture[4]);
    }

    [Fact]
    public void DrawsTheFoodPixelForPixelAsTheC64Does()
    {
        string[] expected = Capture()[^Block..];
        RecordingGraphics graphics = new(320, 200);
        AssetSet assets = AssetSet.Load(AssetLocator.CreateFrom(s_rendition, "8-bit"));
        graphics.SetImage("ItemChars", assets.Images["ItemChars"]);
        BbViewSurface surface = new(graphics, new ScreenLayout(320, 200), AssetLocator.CreateFrom(s_rendition, "8-bit"));

        new EightBitRendition().CreateItemView(surface).Draw(new ItemsModel(
            [FoodType, 0x8F], [Column, 21], [Row, 7], [FoodArt, 0x12], [FoodColour, 0x0D], LevelColours));

        (string sheet, Vector2 position, _, Vector2 source, _) = Assert.Single(graphics.ImageParts);
        Assert.Equal(new(ScreenLeft - BorderLeft, ScreenTop - BorderTop), position);

        FastBitmap painted = graphics.Image(sheet);
        for (int y = 0; y < Block; y++)
        {
            char[] row = new char[Block];
            for (int x = 0; x < Block; x++)
            {
                row[x] = Index(surface.Palette, painted.GetPixel((int)source.X + (x / 2), (int)source.Y + y).Argb);
            }

            Assert.Equal(expected[y], new string(row));
        }
    }

    private static char Index(IPaletteCollection palette, uint argb)
    {
        for (int i = 0; i < 16; i++)
        {
            if (palette[i.ToString(CultureInfo.InvariantCulture)].Argb == argb)
            {
                return i.ToString("x", CultureInfo.InvariantCulture)[0];
            }
        }

        return '?';
    }

    private static string[] Capture() => File.ReadAllLines(
        Path.Combine(AppContext.BaseDirectory, "Views", "Captures", "items-level-1.txt"));
}
