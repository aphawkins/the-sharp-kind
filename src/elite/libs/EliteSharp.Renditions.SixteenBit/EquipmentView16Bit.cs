// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind;

namespace EliteSharp.Renditions.SixteenBit;

/// <summary>
/// The 16-bit equip-ship screen, laid out against the 640-wide viewport.
/// </summary>
internal sealed class EquipmentView16Bit : BaseView16Bit, IView<EquipmentModel>
{
    private readonly IViewSurface _surface;
    private readonly FastColor _colorLightGrey;
    private readonly FastColor _colorLightRed;
    private readonly FastColor _colorWhite;

    internal EquipmentView16Bit(IViewSurface surface)
        : base(surface)
    {
        _surface = surface;

        _colorWhite = surface.Palette["White"];
        _colorLightGrey = surface.Palette["LightGrey"];
        _colorLightRed = surface.Palette["LightRed"];
    }

    public void Draw(EquipmentModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        DrawViewHeader("EQUIP SHIP");

        float y = 55;

        foreach (EquipmentRow row in model.Rows)
        {
            if (row.IsHighlighted)
            {
                _surface.Graphics.DrawRectangleFilled(new(2, y + 1), (int)_surface.Layout.ViewportWidth - 4, 15, _colorLightRed);
            }

            FastColor color = row.IsAffordable ? _colorWhite : _colorLightGrey;
            int x = row.IsIndented ? 50 : 16;
            _surface.Graphics.DrawTextLeft(new(x, y), row.Name, nameof(FontType.Small), color);

            if (row.Price.Length > 0)
            {
                _surface.Graphics.DrawTextRight(new(578, y), row.Price, nameof(FontType.Small), color);
            }

            y += 15;
        }

        _surface.Graphics.DrawTextLeft(new(16, 340), model.Cash, nameof(FontType.Small), _colorWhite);
    }
}
