// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using SharpKind.Graphics;

namespace BubbleBobbleSharp.Renditions.EightBit;

/// <summary>Draws the water and the fire, which the game draws as characters.</summary>
internal sealed class EffectsView8Bit : IView<EffectsModel>
{
    // effect-chars.tga: $1C, $1F, $40 and $41, in that order.
    private const int SourceCellWidth = 4;
    private const int CellHeight = 8;

    // Colour RAM for a water cell ($1BAF); the fire is written without colour, so it wears the playfield's.
    private const int WaterColourRam = 0x0B;

    private const string Sheet = "EffectChars";

    private static readonly int[] s_codes = [0x1C, 0x1F, 0x40, 0x41];

    private readonly IGraphics _graphics;
    private readonly BbViewLayout _layout;
    private readonly MulticolourSheet _water;
    private readonly MulticolourSheet _fire;

    internal EffectsView8Bit(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        _graphics = surface.Graphics;
        _layout = surface.Layout;
        _water = new(surface, Sheet, Sheet + ".Water");
        _fire = new(surface, Sheet, Sheet + ".Fire");
    }

    public void Draw(EffectsModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        _water.Paint(model.Colours, WaterColourRam);
        _fire.Paint(model.Colours);

        for (int cell = EffectsModel.WaterCapacity - 1; cell >= 0; cell--)
        {
            if (model.ShowsWater(cell))
            {
                DrawCell(_water, EffectsModel.WaterCharacter, model.WaterColumn(cell), model.WaterRow(cell));
            }
        }

        for (int strip = 0; strip < EffectsModel.FireCapacity; strip++)
        {
            DrawStrip(model, strip);
        }
    }

    private void DrawStrip(EffectsModel model, int strip)
    {
        byte character = model.StripCharacter(strip);

        if (character == 0)
        {
            return;
        }

        for (int offset = 0; offset < model.StripLength(strip); offset++)
        {
            DrawCell(_fire, character, model.StripColumn(strip) + offset, model.StripRow(strip));
        }
    }

    private void DrawCell(MulticolourSheet sheet, byte character, int column, int row)
    {
        int index = Array.IndexOf(s_codes, character);

        if (index < 0)
        {
            return;
        }

        _graphics.DrawImagePart(
            sheet.Name,
            _layout.Playfield.Cell(column, row),
            new(SourceCellWidth * 2, CellHeight),
            new(index * SourceCellWidth, 0),
            new(SourceCellWidth, CellHeight));
    }
}
