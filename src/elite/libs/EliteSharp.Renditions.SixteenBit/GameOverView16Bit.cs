// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind;

namespace EliteSharp.Renditions.SixteenBit;

/// <summary>
/// The 16-bit game over screen: the 512-space layout, and nothing else. The
/// wreckage tumbling behind it is drawn by the universe, not here.
/// </summary>
internal sealed class GameOverView16Bit : BaseView16Bit, IView<GameOverModel>
{
    private readonly IViewSurface _surface;
    private readonly FastColor _colorGold;

    internal GameOverView16Bit(IViewSurface surface)
        : base(surface)
    {
        _surface = surface;

        _colorGold = surface.Palette["Gold"];
    }

    public void Draw(GameOverModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        _surface.Graphics.DrawTextCentre(_surface.Layout.ViewportCentre.Y, model.Message, nameof(FontType.Large), _colorGold);
    }
}
