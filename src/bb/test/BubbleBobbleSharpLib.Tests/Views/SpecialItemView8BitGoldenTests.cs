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

// Level 1's special item, and the `$18` item that flashes, as $1844 draws them, pixel for pixel against the C64.
//
// Captures/items-special-level-1.txt was read out of VICE running rebb64, on 2026-10-17, with
// `.claude/skills/vice-drive/scripts/capture_items.py`. **It is an intervention:** the special item is hidden on level 1, so at
// an $E90E stop $53 was written with a type whose bit 7 is clear (and the lives topped up). The game's own $1844 then drew the
// 2x2 block from the characters $46-$49 that the level's set-up had copied in, and $2CB7 flipped $60 with EOR 5 each pass while
// the type was $18. Each case is a later $E90E stop: the zero page from $4E, the colour RAM under the block, the characters,
// and the 16x16 pixels of the frame on show at the block's cell (column 21, row 7). The VIC reads colour RAM live and $1844
// writes it a frame into the pass, so the frame on show at a stop was drawn in the colour the stop before read: a case's colour
// is that one (`shown` in the capture).
//
// The special item is drawn from its own sheet, apart from the food's, so its position and colouring are not covered by
// `ItemView8BitGoldenTests`.
[Trait("Level", "Integration")]
public sealed class SpecialItemView8BitGoldenTests
{
    private const int Block = 16;
    private const int Column = 21;
    private const int Row = 7;
    private const byte SpecialArt = 0x09;
    private const byte FlashType = 0x18;
    private const int LevelColours = 0x21;

    private static readonly string s_rendition = Path.Combine(
        AppContext.BaseDirectory, "Renditions", "BubbleBobbleSharp.Renditions.EightBit");

    [Fact]
    public void TheCaptureHasThePlainItemAndBothColoursOfTheFlash()
    {
        List<Case> cases = Cases();

        Assert.Equal(8, cases.Count);
        Assert.All(cases, c => Assert.Equal(Column + (Row * 40), c.Offset));
        Assert.Equal([0x0A], cases.Where(c => c.Type == 0).Select(c => (int)c.Colour).Distinct().Order());
        Assert.Equal([0x0A, 0x0F], cases.Where(c => c.Type == FlashType).Select(c => (int)c.Colour).Distinct().Order());
    }

    [Fact]
    public void DrawsTheSpecialItemPixelForPixelAsTheC64Does()
    {
        foreach (Case @case in Cases())
        {
            RecordingGraphics graphics = new(320, 200);
            AssetSet assets = AssetSet.Load(AssetLocator.CreateFrom(s_rendition, "8-bit"));
            graphics.SetImage("ItemChars", assets.Images["ItemChars"]);
            BbViewSurface surface = new(graphics, new ScreenLayout(320, 200), AssetLocator.CreateFrom(s_rendition, "8-bit"));

            new EightBitRendition().CreateItemView(surface).Draw(new ItemsModel(
                [0x8F, @case.Type], [9, Column], [7, Row], [0x26, SpecialArt], [0x0D, @case.Colour], LevelColours));

            (string sheet, Vector2 position, _, Vector2 source, _) = Assert.Single(graphics.ImageParts);
            Assert.Equal(new(Column * 8, Row * 8), position);

            FastBitmap painted = graphics.Image(sheet);
            for (int y = 0; y < Block; y++)
            {
                char[] row = new char[Block];
                for (int x = 0; x < Block; x++)
                {
                    row[x] = Index(surface.Palette, painted.GetPixel((int)source.X + (x / 2), (int)source.Y + y).Argb);
                }

                Assert.True(@case.Pixels[y] == new string(row), $"{@case.Name}: row {y} is {new string(row)}, the C64's {@case.Pixels[y]}");
            }
        }
    }

    private static char Index(IPaletteCollection palette, uint argb)
    {
        // Bit-pair 00 is drawn transparent: what shows through is the screen's background, black.
        if (argb >> 24 == 0)
        {
            return '0';
        }

        for (int i = 0; i < 16; i++)
        {
            if (palette[i.ToString(CultureInfo.InvariantCulture)].Argb == argb)
            {
                return i.ToString("x", CultureInfo.InvariantCulture)[0];
            }
        }

        return '?';
    }

    private static List<Case> Cases()
    {
        string[] lines = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Views", "Captures", "items-special-level-1.txt"));
        List<Case> cases = [];

        for (int i = 0; i < lines.Length; i += 6 + Block)
        {
            string[] head = lines[i].Split(' ');
            cases.Add(new(
                lines[i],
                byte.Parse(head[4], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                int.Parse(head[6], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                byte.Parse(head[8], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                [.. lines[(i + 6)..(i + 6 + Block)].Select(l => l.Trim())]));
        }

        return cases;
    }

    private sealed record Case(string Name, byte Type, int Offset, byte Colour, string[] Pixels);
}
