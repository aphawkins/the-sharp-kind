// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Globalization;
using System.Numerics;
using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharp.Renditions.EightBit;
using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Graphics;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Players;
using SharpKind.Assets;
using SharpKind.Assets.Palettes;
using SharpKind.Fakes;
using SharpKind.Graphics;
using SharpKind.Graphics.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Views;

// Bubbles and a pop frame as $E779 and $3CE5 draw them, pixel for pixel against the C64.
//
// Captures/objects-level-1.txt was read out of VICE running rebb64, on 2026-09-25, on level 1 with one
// player and no intervention: the stick blew bubbles and walked into one. Every pass the game stopped
// at $0A4B and the eighteen slots' $CA, $DC, $EE, $A9C4, $A9D6, $AA0C and $AA1E were recorded; then it
// stopped at the next $E90E and the frame was read with the binary monitor's display command (0x84),
// as C64 colour numbers.
//
// The screen is double-buffered ($D018 swaps between $41 and $53), so the frame on show at a pass's
// $E90E is the one drawn in the pass before last. Each case pairs one pass's bytes with the frame read
// a pass later. The level's picture starts at raster line 50, not 51: YSCROLL is 2. Each case's box is
// the 24x16 pixels at the port's own cell position, and every sample whose visible pixels all
// matched was kept, one per type, $A9C4 and $A9D6. Samples under a hardware sprite (the player) or
// under another bubble were left out.
//
// Not captured: pop frames other than 4 ($40 with $A9C4 0), and $A9C4 3 with $A9D6 4 or 6.
[Trait("Level", "Integration")]
public sealed class ObjectView8BitGoldenTests
{
    private const int LevelColours = 0x21;
    private const int CellWidth = 24;
    private const int CellHeight = 16;

    private static readonly string s_rendition = Path.Combine(
        AppContext.BaseDirectory, "Renditions", "BubbleBobbleSharp.Renditions.EightBit");

    [Fact]
    public void TheCaptureCoversEveryShiftAndAPopFrame()
    {
        List<Case> cases = Cases();

        Assert.Equal(19, cases.Count);
        Assert.Equal([0, 1, 2, 3], cases.Select(c => (int)c.SubX).Distinct().Order());
        Assert.Equal([0, 2, 4, 6], cases.Select(c => (int)c.SubY).Distinct().Order());
        Assert.Contains(cases, c => c.Type == 0x40);
    }

    [Fact]
    public void DrawsEachBubblePixelForPixelAsTheC64Does()
    {
        foreach (Case @case in Cases())
        {
            RecordingGraphics graphics = new(320, 200);
            AssetSet assets = AssetSet.Load(AssetLocator.CreateFrom(s_rendition, "8-bit"));
            graphics.SetImage("ObjectSprites", assets.Images["ObjectSprites"]);
            BbViewSurface surface = new(graphics, new ScreenLayout(320, 200), AssetLocator.CreateFrom(s_rendition, "8-bit"));

            new EightBitRendition().CreateObjectView(surface).Draw(Model(@case));

            (string sheet, Vector2 position, _, Vector2 source, _) = Assert.Single(graphics.ImageParts);
            Assert.True(new Vector2(@case.Left, @case.Top) == position, $"{@case.Name}: drawn at {position}");

            FastBitmap painted = graphics.Image(sheet);
            for (int y = 0; y < CellHeight; y++)
            {
                for (int x = 0; x < CellWidth; x++)
                {
                    uint argb = painted.GetPixel((int)source.X + (x / 2), (int)source.Y + y).Argb;
                    if ((argb >> 24) != 0)
                    {
                        Assert.True(
                            @case.Pixels[y][x] == Index(surface.Palette, argb),
                            $"{@case.Name}: pixel {x},{y}");
                    }
                }
            }
        }
    }

    // BubblePop's own record of what $E90E drew, with the slot's position beside it.
    private static ObjectsModel Model(Case @case)
    {
        ObjectTable objects = new();
        EntityTable entities = new();
        Scores scores = new();
        BubblePop pop = new(objects, entities, new PlayerTable(), new(entities, scores, new(new FakeRandomSource())), scores);

        objects.Type[@case.Slot] = @case.Type;
        objects.Column[@case.Slot] = @case.Column;
        objects.Row[@case.Slot] = @case.Row;
        objects.SubX[@case.Slot] = @case.SubX;
        objects.SubY[@case.Slot] = @case.SubY;
        objects.X[@case.Slot] = @case.X;
        objects.Y[@case.Slot] = @case.Y;
        pop.Update();

        return new(pop.Drawn, objects.Column, objects.Row, objects.SubY, LevelColours);
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

    private static List<Case> Cases()
    {
        string[] lines = File.ReadAllLines(
            Path.Combine(AppContext.BaseDirectory, "Views", "Captures", "objects-level-1.txt"));
        List<Case> cases = [];
        for (int i = 0; i < lines.Length; i += 3 + CellHeight)
        {
            string[] head = lines[i].Split(' ');
            string[] bytes = lines[i + 1].Trim().Split(' ');
            string[] screen = lines[i + 2].Trim().Split(' ')[1].Split(',');
            cases.Add(new(
                lines[i],
                int.Parse(head[^1], CultureInfo.InvariantCulture),
                Hex(bytes[1]),
                Hex(bytes[3]),
                Hex(bytes[5]),
                Hex(bytes[7]),
                Hex(bytes[9]),
                Hex(bytes[11]),
                Hex(bytes[13]),
                int.Parse(screen[0], CultureInfo.InvariantCulture),
                int.Parse(screen[1], CultureInfo.InvariantCulture),
                [.. lines[(i + 3)..(i + 3 + CellHeight)].Select(l => l.Trim())]));
        }

        return cases;
    }

    private static byte Hex(string text) => byte.Parse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    private sealed record Case(
        string Name,
        int Slot,
        byte Type,
        byte Column,
        byte Row,
        byte SubX,
        byte SubY,
        byte X,
        byte Y,
        int Left,
        int Top,
        string[] Pixels);
}
