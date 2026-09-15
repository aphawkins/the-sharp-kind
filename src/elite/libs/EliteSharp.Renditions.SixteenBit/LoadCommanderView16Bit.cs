// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind;

namespace EliteSharp.Renditions.SixteenBit;

/// <summary>
/// The 16-bit load-commander screen: the 512-space layout, and nothing else.
/// </summary>
internal sealed class LoadCommanderView16Bit : BaseView16Bit, IView<LoadCommanderModel>
{
    private readonly IViewSurface _surface;
    private readonly FastColor _colorWhite;
    private readonly FastColor _colorGold;

    internal LoadCommanderView16Bit(IViewSurface surface)
        : base(surface)
    {
        _surface = surface;

        _colorGold = surface.Palette["Gold"];
        _colorWhite = surface.Palette["White"];
    }

    public void Draw(LoadCommanderModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        DrawViewHeader("LOAD COMMANDER");

        _surface.Graphics.DrawTextCentre(75, "Please enter commander name:", nameof(FontType.Small), _colorWhite);
        _surface.Graphics.DrawRectangleCentre(100, 312, 50, _colorWhite);
        _surface.Graphics.DrawTextCentre(112, model.Name, nameof(FontType.Large), _colorWhite);

        if (model.ErrorMessage.Length > 0)
        {
            _surface.Graphics.DrawTextCentre(175, model.ErrorMessage, nameof(FontType.Large), _colorGold);
            _surface.Graphics.DrawTextCentre(200, "Press SPACE to continue.", nameof(FontType.Small), _colorWhite);
        }
    }
}
