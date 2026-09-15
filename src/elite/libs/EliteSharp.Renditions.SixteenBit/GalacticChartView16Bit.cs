// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind;

namespace EliteSharp.Renditions.SixteenBit;

/// <summary>
/// The 16-bit galactic chart, laid out against the 640-wide viewport: the
/// star field's own space is still 512-wide, so <see cref="ToScreen"/>
/// centres it in the wider viewport with a fixed offset rather than
/// stretching it.
/// </summary>
internal sealed class GalacticChartView16Bit : BaseView16Bit, IView<GalacticChartModel>
{
    private readonly IViewSurface _surface;
    private readonly FastColor _colorGreen;
    private readonly FastColor _colorLighterRed;
    private readonly FastColor _colorWhite;

    internal GalacticChartView16Bit(IViewSurface surface)
        : base(surface)
    {
        _surface = surface;

        _colorGreen = surface.Palette["Green"];
        _colorLighterRed = surface.Palette["LighterRed"];
        _colorWhite = surface.Palette["White"];
    }

    public void Draw(GalacticChartModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        // Header
        DrawViewHeader(model.Title);

        _surface.Graphics.DrawLine(
            new(0 + _surface.Layout.ViewportLeft, 36 + 258), new(_surface.Layout.ViewportRight, 36 + 258), _colorWhite);

        // Fuel radius
        Vector2 centre = ToScreen(model.DockedPlanet);
        float radius = model.FuelLightYears * 2.5f * _surface.Layout.DesignScale;
        float cross_size = 7 * _surface.Layout.DesignScale;
        _surface.Graphics.DrawCircle(centre, radius, _colorGreen);
        _surface.Graphics.DrawLine(new(centre.X, centre.Y - cross_size), new(centre.X, centre.Y + cross_size), _colorWhite);
        _surface.Graphics.DrawLine(new(centre.X - cross_size, centre.Y), new(centre.X + cross_size, centre.Y), _colorWhite);

        // Planets
        foreach (GalacticChartStar star in model.Stars)
        {
            Vector2 pixel = ToScreen(star.Position);
            _surface.Graphics.DrawPixel(pixel, _colorWhite);

            if (star.IsWide)
            {
                _surface.Graphics.DrawPixel(new(pixel.X + 1, pixel.Y), _colorWhite);
            }
        }

        // Cross
        centre = ToScreen(model.Cross);

        _surface.Graphics.DrawLine(new(centre.X - 8, centre.Y), new(centre.X + 8, centre.Y), _colorLighterRed);
        _surface.Graphics.DrawLine(new(centre.X, centre.Y - 8), new(centre.X, centre.Y + 8), _colorLighterRed);

        // Text
        _surface.Graphics
            .DrawTextLeft(
                new(16 + _surface.Layout.ViewportLeft, _surface.Layout.ViewportHeight - 55),
                model.Caption,
                nameof(FontType.Small),
                _colorGreen);
        _surface.Graphics
            .DrawTextLeft(
                new(16 + _surface.Layout.ViewportLeft, _surface.Layout.ViewportHeight - 40),
                model.Detail,
                nameof(FontType.Small),
                _colorWhite);
    }

    // Galaxy space (D, B) to this tier's screen coordinates. The star
    // field's own span is 512-wide regardless of the viewport, so it is
    // centred in the wider 640-wide viewport with a fixed +64 offset.
    private Vector2 ToScreen(Vector2 galaxy) => new(
        (galaxy.X * _surface.Layout.DesignScale) + _surface.Layout.ViewportLeft + 64,
        (galaxy.Y * _surface.Layout.DesignScale / 2) + (18 * _surface.Layout.DesignScale) + 1);
}
