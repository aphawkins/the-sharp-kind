// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using SharpKind.Graphics;

namespace BubbleBobbleSharp.Renditions.EightBit;

/// <summary>
/// Draws a bubble or a pop frame as a software sprite: object-sprites.tga's
/// entries, composed and positioned the way $E779/$3CE5 and
/// sprite-composer.s do. See docs/bb-port-plan.md, item 2c.
/// <para>
/// A cell is twelve multicolour pixels wide and sixteen rows tall, doubled to
/// twenty-four on screen exactly as the playfield's characters are - see
/// <see cref="SidebarView8Bit"/>. object-sprites.tga holds every cell in one
/// row, so a slot's entry is also its column.
/// </para>
/// <para>
/// Position is not the row and column a hardware sprite would use. The
/// graphic is pre-shifted by $A9C4 x2 inside its box, so the art's left edge
/// is $AA0C minus $14, in every VICE sample taken so far. Vertically, $EE
/// minus two is the character row the bottom of the cell sits in, and within
/// that row the bottom pixel is at ($A9D6 + 7) and 7 - the box is sixteen
/// pixels tall and builds upwards from there. Not yet proven against a VICE
/// golden frame; see item 2c's verify step.
/// </para>
/// </summary>
internal sealed class ObjectView8Bit : IView<ObjectsModel>
{
    // A multicolour cell: twelve entry numbers across in the sheet, twenty-four pixels on screen,
    // sixteen rows tall either way.
    private const int SourceCellWidth = 12;
    private const int CellHeight = 16;

    // The art's left edge, relative to $AA0C - see the class remarks.
    private const int LeftOffset = 0x14;

    // $EE holds the character row the bottom of the cell sits in, two below the row this pass
    // composes into first.
    private const int BottomRowOffset = 2;

    // A C64 character row is eight pixels; ($A9D6 + 7) & 7 finds the bottom pixel within it.
    private const int SubPositionMask = 0x07;
    private const int SubPositionBias = 7;

    private const string Sheet = "ObjectSprites";

    private readonly IGraphics _graphics;
    private readonly BbViewLayout _layout;
    private readonly MulticolourSheet _sprites;

    internal ObjectView8Bit(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        _graphics = surface.Graphics;
        _layout = surface.Layout;
        _sprites = new(surface, Sheet);
    }

    public void Draw(ObjectsModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        _sprites.Paint(model.Colours);

        for (int slot = ObjectsModel.Capacity - 1; slot >= 0; slot--)
        {
            if (model.IsDrawn(slot))
            {
                DrawSlot(model, slot);
            }
        }
    }

    private void DrawSlot(ObjectsModel model, int slot)
    {
        float left = model.X(slot) - LeftOffset;

        int bottomRow = model.Row(slot) - BottomRowOffset;
        float rowTop = _layout.Playfield.Cell(0, bottomRow).Y;
        float bottomPixel = rowTop + ((model.SubY(slot) + SubPositionBias) & SubPositionMask);
        float top = bottomPixel - (CellHeight - 1);

        _graphics.DrawImagePart(
            _sprites.Name,
            new(left, top),
            new(SourceCellWidth * 2, CellHeight),
            new(model.Entry(slot) * SourceCellWidth, 0),
            new(SourceCellWidth, CellHeight));
    }
}
