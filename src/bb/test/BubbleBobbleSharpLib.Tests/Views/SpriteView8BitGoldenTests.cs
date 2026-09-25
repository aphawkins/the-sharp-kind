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

// The hardware sprites as $1805 sets them, pixel for pixel against the C64: the players, every enemy
// class walking, a caught enemy riding its bubble, and an enemy popped out of one.
//
// Captures/sprites-level-1.txt was read out of VICE running rebb64, on 2026-09-25, on level 1 with one
// player. Every pass the game stopped at $1843, $1805's rts, and the bytes it had just read were
// recorded - $B2, $BA, $C2, $8520, $8598, $8548 and $8728 - with what it had written: the VIC's
// registers and the pointers at $53F8. Then it stopped at the next $1805, two frames on, so the
// registers had held for a whole frame, and the frame was read with the binary monitor's display
// command (0x84) as C64 colour numbers. The sprite registers read the same at both stops in every
// sample. The picture starts at raster line 50, and column 0 is at x 136.
//
// Runs run1 and run2 are the game untouched: the stick fired and walked, and the player died and
// came back. The cls runs are an intervention: level 1 only has class 0, so before each pass $8598
// was written for slots 2 to 7 with another class's base ($7F to $BB), and the enemies walked in that
// class's sprites. Pointers past $C0, which forcing class 0's walk frames onto $BB makes, were left
// out. One case per base and pointer was kept, fully on screen, with no lower-numbered sprite over
// it (the VIC gives those priority); every such sample matched. Three more did not, and all three were slot
// 4 under slot 3's food, which is drawn from $7C80 onwards and which the port has no art for yet.
//
// **What the capture found.** A caught enemy's $B2 is zero while it rides its bubble, and the C64
// draws it there all the same (pointer $7E), because the VIC reads $D015, not $B2. The port used to
// hide every slot whose state byte was zero. $D015's own bits move as $142E flickers a caught enemy:
// the last case is one with its bit clear, and the frame shows nothing of it.
[Trait("Level", "Integration")]
public sealed class SpriteView8BitGoldenTests
{
    private const int Width = 24;
    private const int Height = 21;
    private const int OriginX = 24;
    private const int OriginY = 50;

    private static readonly string s_rendition = Path.Combine(
        AppContext.BaseDirectory, "Renditions", "BubbleBobbleSharp.Renditions.EightBit");

    [Fact]
    public void TheCaptureCoversThePlayersEveryClassACaughtEnemyAndAFlicker()
    {
        List<Case> cases = Cases();

        Assert.Equal(54, cases.Count);
        Assert.Contains(cases, c => c.Slot < 2);
        Assert.Equal(
            [0x60, 0x73, 0x7F, 0x8B, 0x97, 0x9F, 0xAB, 0xB3, 0xBB],
            cases.Select(c => (int)c.Base).Distinct().Order());
        Assert.Contains(cases, c => c.State == 0 && c.Pointer == 0x7E && c.Enabled);
        Assert.Contains(cases, c => c.State is 0x0B or 0x0C);
        Assert.Contains(cases, c => !c.Enabled);
    }

    // $1805 has no arithmetic between the slot's bytes and the VIC's registers but the pointer's mask
    // and add, and $1822's flash.
    [Fact]
    public void SetsTheRegistersTheC64Did()
    {
        foreach (Case @case in Cases())
        {
            SpriteModel model = Model(@case);

            Assert.True(@case.RegisterX == model.X(@case.Slot), @case.Name);
            Assert.True(@case.RegisterY == model.Y(@case.Slot), @case.Name);
            Assert.True(@case.Pointer == model.Sprite(@case.Slot) + 0x60, @case.Name);
            Assert.True(@case.RegisterColour == model.Colour(@case.Slot), @case.Name);
            Assert.True(@case.Enabled == model.IsDrawn(@case.Slot), @case.Name);
        }
    }

    [Fact]
    public void DrawsEachSpritePixelForPixelAsTheC64Does()
    {
        foreach (Case @case in Cases())
        {
            RecordingGraphics graphics = new(320, 200);
            AssetSet assets = AssetSet.Load(AssetLocator.CreateFrom(s_rendition, "8-bit"));
            graphics.SetImage("SpritesGame", assets.Images["SpritesGame"]);
            BbViewSurface surface = new(graphics, new ScreenLayout(320, 200), AssetLocator.CreateFrom(s_rendition, "8-bit"));

            new EightBitRendition().CreateSpriteView(surface).Draw(Model(@case));

            if (!@case.Enabled)
            {
                Assert.Empty(graphics.ImageParts);
                continue;
            }

            (string sheet, Vector2 position, _, Vector2 source, _) = Assert.Single(graphics.ImageParts);
            Assert.True(new Vector2(@case.X - OriginX, @case.Y - OriginY) == position, $"{@case.Name}: drawn at {position}");

            FastBitmap painted = graphics.Image(sheet);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
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

    // The case's slot holds its own bytes; the other seven are empty, with no base, so no pointer of
    // theirs reaches the sheet.
    private static SpriteModel Model(Case @case)
    {
        byte[] state = new byte[SpriteModel.Capacity];
        byte[] x = new byte[SpriteModel.Capacity];
        byte[] y = new byte[SpriteModel.Capacity];
        byte[] frame = new byte[SpriteModel.Capacity];
        byte[] spriteBase = new byte[SpriteModel.Capacity];
        byte[] colour = new byte[SpriteModel.Capacity];
        byte[] flash = new byte[SpriteModel.Capacity];

        state[@case.Slot] = @case.State;
        x[@case.Slot] = @case.X;
        y[@case.Slot] = @case.Y;
        frame[@case.Slot] = @case.Frame;
        spriteBase[@case.Slot] = @case.Base;
        colour[@case.Slot] = @case.Colour;
        flash[@case.Slot] = @case.Flash;

        return new(state, x, y, frame, spriteBase, colour, flash, @case.Enable);
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
            Path.Combine(AppContext.BaseDirectory, "Views", "Captures", "sprites-level-1.txt"));
        List<Case> cases = [];
        for (int i = 0; i < lines.Length; i += 3 + Height)
        {
            string[] bytes = lines[i + 1].Trim().Split(' ');
            string[] vic = lines[i + 2].Trim().Split(' ');
            cases.Add(new(
                lines[i],
                int.Parse(lines[i].Split(' ')[^1], CultureInfo.InvariantCulture),
                Hex(bytes[1]),
                Hex(bytes[3]),
                Hex(bytes[5]),
                Hex(bytes[7]),
                Hex(bytes[9]),
                Hex(bytes[11]),
                Hex(bytes[13]),
                Hex(bytes[15]),
                Hex(vic[2]),
                Hex(vic[4]),
                Hex(vic[6]),
                Hex(vic[8]),
                [.. lines[(i + 3)..(i + 3 + Height)].Select(l => l.Trim())]));
        }

        return cases;
    }

    private static byte Hex(string text) => byte.Parse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    private sealed record Case(
        string Name,
        int Slot,
        byte State,
        byte X,
        byte Y,
        byte Frame,
        byte Base,
        byte Colour,
        byte Flash,
        byte Enable,
        byte RegisterX,
        byte RegisterY,
        byte RegisterColour,
        byte Pointer,
        string[] Pixels)
    {
        public bool Enabled => (Enable & (1 << Slot)) != 0;
    }
}
