// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind;

namespace EliteSharp.Renditions.SixteenBit;

/// <summary>
/// The 16-bit save-commander screen, laid out against the 640-wide viewport.
/// </summary>
internal sealed class SaveCommanderView16Bit : BaseView16Bit, IView<SaveCommanderModel>
{
    private readonly IViewSurface _surface;
    private readonly FastColor _colorWhite;
    private readonly FastColor _colorGold;

    internal SaveCommanderView16Bit(IViewSurface surface)
        : base(surface)
    {
        _surface = surface;

        _colorGold = surface.Palette["Gold"];
        _colorWhite = surface.Palette["White"];
    }

    public void Draw(SaveCommanderModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        DrawViewHeader("SAVE COMMANDER");

        _surface.Graphics.DrawTextCentre(75, "Please enter commander name:", nameof(FontType.Small), _colorWhite);
        _surface.Graphics.DrawRectangle(new(_surface.Layout.ViewportCentre.X - 156, 100), 312, 50, _colorWhite);
        _surface.Graphics.DrawTextCentre(112, model.Name, nameof(FontType.Large), _colorWhite);

        if (model.StatusMessage.Length > 0)
        {
            _surface.Graphics.DrawTextCentre(175, model.StatusMessage, nameof(FontType.Large), _colorGold);
            _surface.Graphics.DrawTextCentre(200, "Press SPACE to continue.", nameof(FontType.Small), _colorWhite);
        }
    }
}
