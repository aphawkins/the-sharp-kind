// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind;

namespace EliteSharp.Renditions.SixteenBit;

/// <summary>
/// The 16-bit ship parade screen: the 512-space layout, and nothing else.
/// The ship itself is drawn by the universe, not here.
/// </summary>
internal sealed class Intro2View16Bit : BaseView16Bit, IView<Intro2Model>
{
    private readonly IViewSurface _surface;
    private readonly FastColor _colorGold;
    private readonly FastColor _colorWhite;

    internal Intro2View16Bit(IViewSurface surface)
        : base(surface)
    {
        _surface = surface;

        _colorGold = surface.Palette["Gold"];
        _colorWhite = surface.Palette["White"];
    }

    public void Draw(Intro2Model model)
    {
        ArgumentNullException.ThrowIfNull(model);

        _surface.Graphics.DrawImageCentre(nameof(ImageType.EliteText), _surface.Layout.ViewportTop + 10);

        _surface.Graphics.DrawTextCentre(_surface.Layout.ViewportHeight - 30, model.Prompt, nameof(FontType.Large), _colorGold);

        if (model.ShipName.Length > 0)
        {
            _surface.Graphics.DrawTextCentre(_surface.Layout.ViewportHeight - 60, model.ShipName, nameof(FontType.Small), _colorWhite);
        }
    }
}
