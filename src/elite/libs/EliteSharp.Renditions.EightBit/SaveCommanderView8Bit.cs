// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind;

namespace EliteSharp.Renditions.EightBit;

/// <summary>
/// The 8-bit save-commander screen: a first-draft 320x256 layout, not
/// derived from the 16-bit one - see docs/backlog-roadmap.md's "Author the
/// 8-bit view layouts" item. Exact spacing is expected to be refined
/// visually.
/// </summary>
internal sealed class SaveCommanderView8Bit : BaseView8Bit, IView<SaveCommanderModel>
{
    private const int BoxWidth = 280;
    private const int BoxHeight = 24;
    private const int BoxY = 64;

    private readonly IViewSurface _surface;
    private readonly FastColor _colorWhite;
    private readonly FastColor _colorYellow;

    internal SaveCommanderView8Bit(IViewSurface surface)
        : base(surface)
    {
        _surface = surface;

        _colorYellow = surface.Palette["Yellow"];
        _colorWhite = surface.Palette["White"];
    }

    public void Draw(SaveCommanderModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        DrawViewHeader("SAVE COMMANDER");

        DrawTextCentreOnGrid(6, "Please enter commander name:", nameof(FontType.Small), _colorWhite);
        _surface.Graphics.DrawRectangle(new(_surface.Layout.ViewportCentre.X - (BoxWidth / 2), BoxY), BoxWidth, BoxHeight, _colorWhite);
        DrawTextCentreOnGrid(9, model.Name, nameof(FontType.Small), _colorWhite);

        if (model.StatusMessage.Length > 0)
        {
            DrawTextCentreOnGrid(14, model.StatusMessage, nameof(FontType.Small), _colorYellow);
            DrawTextCentreOnGrid(16, "Press SPACE to continue.", nameof(FontType.Small), _colorWhite);
        }
    }
}
