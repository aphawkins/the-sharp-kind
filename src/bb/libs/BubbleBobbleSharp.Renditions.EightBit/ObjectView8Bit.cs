// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using SharpKind.Graphics;

namespace BubbleBobbleSharp.Renditions.EightBit;

/// <summary>Draws the objects: bubbles, pops, shots and the Baron.</summary>
internal sealed class ObjectView8Bit : IView<ObjectsModel>
{
    private const int SourceCellWidth = 12;
    private const int CellHeight = 16;

    private const int BottomRowOffset = 2;

    private const int SubPositionMask = 0x07;
    private const int SubPositionBias = 7;

    private const string SheetName = "ObjectSprites";

    private readonly IGraphics _graphics;
    private readonly BbViewLayout _layout;
    private readonly IViewSurface _surface;
    private readonly Dictionary<int, MulticolourSheet> _sheets = [];

    internal ObjectView8Bit(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        _graphics = surface.Graphics;
        _layout = surface.Layout;
        _surface = surface;
    }

    public void Draw(ObjectsModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

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
        MulticolourSheet sprites = Sheet(model, slot);

        float left = _layout.Playfield.Cell(model.Column(slot), 0).X;

        int bottomRow = model.Row(slot) - BottomRowOffset;
        float rowTop = _layout.Playfield.Cell(0, bottomRow).Y;
        float bottomPixel = rowTop + ((model.SubY(slot) + SubPositionBias) & SubPositionMask);
        float top = bottomPixel - (CellHeight - 1);

        _graphics.DrawImagePart(
            sprites.Name,
            new(left, top),
            new(SourceCellWidth * 2, CellHeight),
            new(model.Entry(slot) * SourceCellWidth, 0),
            new(SourceCellWidth, CellHeight));
    }

    // One repainted copy of the sheet for each colour RAM byte an object is drawn in, made when first needed.
    private MulticolourSheet Sheet(ObjectsModel model, int slot)
    {
        int colour = model.Colour(slot);

        if (!_sheets.TryGetValue(colour, out MulticolourSheet? sheet))
        {
            sheet = new(_surface, SheetName, SheetName + "." + colour);
            _sheets[colour] = sheet;
        }

        sheet.Paint(model.Colours, colour);

        return sheet;
    }
}
