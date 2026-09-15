// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind;

namespace EliteSharp.Renditions.SixteenBit;

internal sealed class ShortRangeChartView16Bit : BaseView16Bit, IView<ShortRangeChartModel>
{
    private const int TextX = 16;
    private const int NameY = 55;
    private const int DistanceY = 40;
    private const int CrossSize = 16;

    private readonly IViewSurface _surface;
    private readonly FastColor _colorGold;
    private readonly FastColor _colorGreen;
    private readonly FastColor _colorLighterRed;
    private readonly FastColor _colorWhite;

    internal ShortRangeChartView16Bit(IViewSurface surface)
        : base(surface)
    {
        _surface = surface;

        _colorGold = surface.Palette["Gold"];
        _colorGreen = surface.Palette["Green"];
        _colorLighterRed = surface.Palette["LighterRed"];
        _colorWhite = surface.Palette["White"];
    }

    public void Draw(ShortRangeChartModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        DrawViewHeader(model.Title);

        // Fuel radius
        Vector2 centre = _surface.Layout.ViewportCentre;
        float scale = _surface.Layout.DesignScale;
        float radius = model.FuelLightYears * 10 * scale;
        float crossSize = CrossSize * scale;
        Graphics.DrawCircle(centre, radius, _colorGreen);
        Graphics.DrawLine(new(centre.X, centre.Y - crossSize), new(centre.X, centre.Y + crossSize), _colorWhite);
        Graphics.DrawLine(new(centre.X - crossSize, centre.Y), new(centre.X + crossSize, centre.Y), _colorWhite);

        foreach ((Vector2 position, string name) in model.Labels)
        {
            Graphics.DrawTextLeft(position, name, nameof(FontType.Small), _colorWhite);
        }

        foreach ((Vector2 position, float size) in model.Planets)
        {
            Graphics.DrawCircleFilled(position, size, _colorGold);
        }

        // Cross
        Graphics.DrawLine(new(model.Cross.X - CrossSize, model.Cross.Y), new(model.Cross.X + CrossSize, model.Cross.Y), _colorLighterRed);
        Graphics.DrawLine(new(model.Cross.X, model.Cross.Y - CrossSize), new(model.Cross.X, model.Cross.Y + CrossSize), _colorLighterRed);

        float x = TextX + _surface.Layout.ViewportLeft;
        Graphics.DrawTextLeft(
            new(x, _surface.Layout.ViewportHeight - NameY),
            model.Caption,
            nameof(FontType.Small),
            _colorGreen);
        Graphics.DrawTextLeft(
            new(x, _surface.Layout.ViewportHeight - DistanceY),
            model.Detail,
            nameof(FontType.Small),
            _colorWhite);
    }
}
