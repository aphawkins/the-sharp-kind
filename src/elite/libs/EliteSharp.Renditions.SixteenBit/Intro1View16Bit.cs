// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind;

namespace EliteSharp.Renditions.SixteenBit;

/// <summary>
/// The 16-bit title screen: the 512-space layout, and nothing else. The
/// rolling Cobra behind it is drawn by the universe, not here.
/// </summary>
internal sealed class Intro1View16Bit : BaseView16Bit, IView<Intro1Model>
{
    // The credits stack upwards from the prompt, one line every 20px.
    private const float CreditSpacing = 20;

    private readonly IViewSurface _surface;
    private readonly FastColor _colorGold;
    private readonly FastColor _colorWhite;

    internal Intro1View16Bit(IViewSurface surface)
        : base(surface)
    {
        _surface = surface;

        _colorGold = surface.Palette["Gold"];
        _colorWhite = surface.Palette["White"];
    }

    public void Draw(Intro1Model model)
    {
        ArgumentNullException.ThrowIfNull(model);

        _surface.Graphics.DrawImageCentre(nameof(ImageType.EliteText), _surface.Layout.ViewportTop + 10);

        float y = _surface.Layout.ViewportHeight - 90;
        foreach (string credit in model.Credits)
        {
            _surface.Graphics.DrawTextCentre(y, credit, nameof(FontType.Small), _colorWhite);
            y += CreditSpacing;
        }

        _surface.Graphics.DrawTextCentre(_surface.Layout.ViewportHeight - 30, model.Prompt, nameof(FontType.Large), _colorGold);
    }
}
