// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using BubbleBobbleSharp.Abstractions.Views;
using SharpKind;
using SharpKind.Assets.Palettes;
using SharpKind.Graphics;
using SharpKind.Graphics.Fakes;

namespace BubbleBobbleSharpLib.Tests.Views;

// What a view is handed, with sheets standing in for the real ones. A view repaints its multicolour
// sheets before it draws from them, so the surface has to hold something for it to repaint - and a
// stand-in the test can reason about beats the real artwork, whose colours would have to be looked up
// to say what the repaint should have produced.
internal sealed class TestSurface : IViewSurface
{
    // One character: the four entry numbers across, eight rows down, which is the shape every sheet in
    // the game is a grid of.
    internal const int SheetWidth = 4;
    internal const int SheetHeight = 8;

    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "SetImage takes the bitmap on, and the graphics disposes what it holds.")]
    internal TestSurface(RecordingGraphics graphics, params string[] sheets)
    {
        Graphics = graphics;

        foreach (string sheet in sheets)
        {
            graphics.SetImage(sheet, Sheet());
        }
    }

    public IGraphics Graphics { get; }

    public BbViewLayout Layout { get; } = new(320, 200);

    // Colour n is 0xFF0000_0n, so a colour says which entry it is and a repainted pixel says which
    // entry the level pointed at it.
    public IPaletteCollection Palette { get; } = new Palette(
        Enumerable.Range(0, 16).ToDictionary(
            x => x.ToString(CultureInfo.InvariantCulture),
            x => FastColor.FromUInt32(0xFF000000u | (uint)x)));

    internal static FastColor Colour(int index) => FastColor.FromUInt32(0xFF000000u | (uint)index);

    // A sheet as the exporter writes one: the pixel is the two-bit entry number, wearing the palette
    // colour of the same number. Column x holds entry x, so every entry is somewhere in the sheet.
    private static FastBitmap Sheet()
    {
        FastBitmap sheet = new(SheetWidth, SheetHeight);

        for (int y = 0; y < SheetHeight; y++)
        {
            for (int x = 0; x < SheetWidth; x++)
            {
                sheet.SetPixel(x, y, Colour(x));
            }
        }

        return sheet;
    }
}
