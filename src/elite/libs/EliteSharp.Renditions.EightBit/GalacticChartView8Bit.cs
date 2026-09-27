// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind;

namespace EliteSharp.Renditions.EightBit;

/// <summary>
/// The 8-bit galactic chart: authored for the 320x256 canvas and its fixed
/// 8x8 font. The plot itself needs no tier-specific maths - galaxy space is
/// 256x256 and <see cref="ViewLayout.DesignScale"/> maps it onto the tier - so only
/// the divider line, the cross-hair size and the two caption rows differ from
/// the 16-bit view.
/// </summary>
internal sealed class GalacticChartView8Bit : BaseView8Bit, IView<GalacticChartModel>
{
    // Closes the plot off below its last row: galaxy y=255 maps to (255 * 0.55) + ViewportTop + 24 at Scale 1.
    private const float DividerY = 172;
    private const float CrossSize = 5;
    private const int CaptionColumn = 1;
    private const int CaptionRow = 22;
    private const int DetailRow = 23;

    // Screen pixels per galaxy unit of D. B is plotted at half this, as a light year is
    // 2.5 units of D but 5 of B: the chart's scale in light years is then the same both
    // ways, so the fuel range is a true circle. Set so galaxy y=255 clears the divider.
    private const float ChartScale = 1.1f;

    // One light year in D units (PlanetController.CalculateDistanceToPlanet).
    private const float UnitsPerLightYear = 2.5f;

    private readonly IViewSurface _surface;
    private readonly FastColor _colorGreen;
    private readonly FastColor _colorOrange;
    private readonly FastColor _colorWhite;

    internal GalacticChartView8Bit(IViewSurface surface)
        : base(surface)
    {
        _surface = surface;

        _colorGreen = surface.Palette["Green"];
        _colorOrange = surface.Palette["Orange"];
        _colorWhite = surface.Palette["White"];
    }

    // Where galaxy x=0 lands, so the 256-unit-wide plot is centred across the viewport.
    private float ChartLeft
        => _surface.Layout.ViewportLeft + ((_surface.Layout.ViewportWidth - (256 * ChartScale * _surface.Layout.DesignScale)) / 2);

    public void Draw(GalacticChartModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        DrawViewHeader(model.Title);

        _surface.Graphics.DrawLine(new(_surface.Layout.ViewportLeft, DividerY), new(_surface.Layout.ViewportRight, DividerY), _colorWhite);

        // Fuel radius
        Vector2 centre = ToScreen(model.DockedPlanet);
        float radius = model.FuelLightYears * UnitsPerLightYear * ChartScale * _surface.Layout.DesignScale;
        float fuelCrossSize = 7 * _surface.Layout.DesignScale;
        _surface.Graphics.DrawCircle(centre, radius, _colorGreen);
        _surface.Graphics.DrawLine(new(centre.X, centre.Y - fuelCrossSize), new(centre.X, centre.Y + fuelCrossSize), _colorWhite);
        _surface.Graphics.DrawLine(new(centre.X - fuelCrossSize, centre.Y), new(centre.X + fuelCrossSize, centre.Y), _colorWhite);

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

        _surface.Graphics.DrawLine(new(centre.X - CrossSize, centre.Y), new(centre.X + CrossSize, centre.Y), _colorOrange);
        _surface.Graphics.DrawLine(new(centre.X, centre.Y - CrossSize), new(centre.X, centre.Y + CrossSize), _colorOrange);

        // Text
        _surface.Graphics.DrawTextLeft(
            new(Column(CaptionColumn), Row(CaptionRow)),
            model.Caption,
            nameof(FontType.Small),
            _colorGreen);
        _surface.Graphics.DrawTextLeft(
            new(Column(CaptionColumn), Row(DetailRow)),
            model.Detail,
            nameof(FontType.Small),
            _colorWhite);
    }

    // Galaxy space (D, B) to this tier's screen coordinates.
    private Vector2 ToScreen(Vector2 galaxy) => new(
        (galaxy.X * _surface.Layout.DesignScale * ChartScale) + ChartLeft,
        (galaxy.Y * _surface.Layout.DesignScale * ChartScale / 2) + _surface.Layout.ViewportTop + 24);
}
