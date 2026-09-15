// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind;

namespace EliteSharp.Renditions.SixteenBit;

/// <summary>
/// The 16-bit quit confirmation: the 512-space layout, and nothing else.
/// </summary>
internal sealed class QuitView16Bit : BaseView16Bit, IView<QuitModel>
{
    private readonly IViewSurface _surface;
    private readonly FastColor _colorGold;

    internal QuitView16Bit(IViewSurface surface)
        : base(surface)
    {
        _surface = surface;

        _colorGold = surface.Palette["Gold"];
    }

    public void Draw(QuitModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        DrawViewHeader(model.Header);

        _surface.Graphics.DrawTextCentre(_surface.Layout.ViewportCentre.Y, model.Prompt, nameof(FontType.Large), _colorGold);
    }
}
