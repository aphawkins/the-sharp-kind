// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using SharpKind.Graphics;

namespace BubbleBobbleSharp.Renditions.EightBit;

/// <summary>Draws the level's two items.</summary>
internal sealed class ItemView8Bit : IView<ItemsModel>
{
    private const int SourceBlockWidth = 8;
    private const int BlockHeight = 16;

    private const string Sheet = "ItemChars";

    private readonly IGraphics _graphics;
    private readonly BbViewLayout _layout;
    private readonly MulticolourSheet[] _sheets;

    internal ItemView8Bit(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        _graphics = surface.Graphics;
        _layout = surface.Layout;
        _sheets = [new(surface, Sheet, Sheet + ".Food"), new(surface, Sheet, Sheet + ".Special")];
    }

    public void Draw(ItemsModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        for (int item = ItemsModel.Capacity - 1; item >= 0; item--)
        {
            if (!model.Shows(item))
            {
                continue;
            }

            MulticolourSheet sheet = _sheets[item];
            sheet.Paint(model.Colours, model.Colour(item));

            _graphics.DrawImagePart(
                sheet.Name,
                _layout.Playfield.Cell(model.Column(item), model.Row(item)),
                new(SourceBlockWidth * 2, BlockHeight),
                new(model.Art(item) * SourceBlockWidth, 0),
                new(SourceBlockWidth, BlockHeight));
        }
    }
}
