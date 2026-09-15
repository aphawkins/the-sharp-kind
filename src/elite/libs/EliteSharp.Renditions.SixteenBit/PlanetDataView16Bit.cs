// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind;

namespace EliteSharp.Renditions.SixteenBit;

/// <summary>
/// The 16-bit planet data screen, laid out against the 640-wide viewport.
/// </summary>
internal sealed class PlanetDataView16Bit : BaseView16Bit, IView<PlanetDataModel>
{
    private readonly IViewSurface _surface;
    private readonly FastColor _colorGreen;
    private readonly FastColor _colorWhite;

    internal PlanetDataView16Bit(IViewSurface surface)
        : base(surface)
    {
        _surface = surface;

        _colorGreen = surface.Palette["Green"];
        _colorWhite = surface.Palette["White"];
    }

    public void Draw(PlanetDataModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        DrawViewHeader(model.Header);

        if (model.Distance.Length > 0)
        {
            _surface.Graphics.DrawTextLeft(new(16 + _surface.Layout.ViewportLeft, 42), "Distance:", nameof(FontType.Small), _colorGreen);
            _surface.Graphics.DrawTextLeft(
                new(175 + _surface.Layout.ViewportLeft, 42), model.Distance, nameof(FontType.Small), _colorWhite);
        }

        _surface.Graphics.DrawTextLeft(new(16 + _surface.Layout.ViewportLeft, 74), "Economy:", nameof(FontType.Small), _colorGreen);
        _surface.Graphics.DrawTextLeft(new(175 + _surface.Layout.ViewportLeft, 74), model.Economy, nameof(FontType.Small), _colorWhite);
        _surface.Graphics.DrawTextLeft(new(16 + _surface.Layout.ViewportLeft, 106), "Government:", nameof(FontType.Small), _colorGreen);
        _surface.Graphics.DrawTextLeft(new(175 + _surface.Layout.ViewportLeft, 106), model.Government, nameof(FontType.Small), _colorWhite);
        _surface.Graphics.DrawTextLeft(new(16 + _surface.Layout.ViewportLeft, 138), "Tech Level:", nameof(FontType.Small), _colorGreen);
        _surface.Graphics.DrawTextLeft(new(175 + _surface.Layout.ViewportLeft, 138), model.TechLevel, nameof(FontType.Small), _colorWhite);
        _surface.Graphics.DrawTextLeft(new(16 + _surface.Layout.ViewportLeft, 170), "Population:", nameof(FontType.Small), _colorGreen);
        _surface.Graphics.DrawTextLeft(new(175 + _surface.Layout.ViewportLeft, 170), model.Population, nameof(FontType.Small), _colorWhite);
        _surface.Graphics.DrawTextLeft(
            new(16 + _surface.Layout.ViewportLeft, 202), "Gross Productivity:", nameof(FontType.Small), _colorGreen);
        _surface.Graphics.DrawTextLeft(
            new(175 + _surface.Layout.ViewportLeft, 202), model.Productivity, nameof(FontType.Small), _colorWhite);
        _surface.Graphics.DrawTextLeft(new(16 + _surface.Layout.ViewportLeft, 234), "Average Radius:", nameof(FontType.Small), _colorGreen);
        _surface.Graphics.DrawTextLeft(new(175 + _surface.Layout.ViewportLeft, 234), model.Radius, nameof(FontType.Small), _colorWhite);
        DrawTextPretty(new(16 + _surface.Layout.ViewportLeft, 266), (int)_surface.Layout.ViewportWidth - 32, model.Description);
    }
}
