// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind;

namespace EliteSharp.Renditions.SixteenBit;

/// <summary>
/// The 16-bit cockpit window: shared by all four directions, since none
/// varies the layout, only the model's content. The hyperspace status line
/// used to be an absolute y=358, which only worked because it equalled
/// ScannerTop minus 25; it is derived now, so the only thing separating this
/// from PilotView8Bit is the two offsets. The starfield and the ship ahead are
/// drawn by the universe.
/// </summary>
internal sealed class PilotView16Bit : BaseView16Bit, IView<PilotModel>
{
    private readonly IViewSurface _surface;
    private readonly LaserDrawBase _laser;
    private readonly FastColor _colorWhite;

    internal PilotView16Bit(IViewSurface surface)
        : base(surface)
    {
        _surface = surface;
        _laser = new LaserDraw16Bit(surface);

        _colorWhite = surface.Palette["White"];
    }

    public void Draw(PilotModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (model.HyperspaceStatus.Length > 0)
        {
            _surface.Graphics.DrawTextCentre(
                _surface.Layout.ViewportHeight - 25, model.HyperspaceStatus, nameof(FontType.Small), _colorWhite);
        }

        _surface.Graphics.DrawTextCentre(_surface.Layout.ViewportTop + 10, model.ViewName, nameof(FontType.Small), _colorWhite);

        if (model.IsFiring)
        {
            _laser.DrawLaserLines(model.LaserType, model.LaserAim, model.LaserWireframe);
        }

        _laser.DrawLaserSights(model.LaserType);
    }
}
