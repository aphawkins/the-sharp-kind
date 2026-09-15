// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind;

namespace EliteSharp.Renditions.EightBit;

/// <summary>
/// The 8-bit planet data screen: a first-draft 320x256 layout, not derived
/// from the 16-bit one - see docs/backlog-roadmap.md's "Author the 8-bit
/// view layouts" item. Labels are shortened from the 16-bit view's own
/// (label text is view chrome, not model content) so the value column can
/// start well before 320px; exact spacing is expected to be refined visually.
/// </summary>
internal sealed class PlanetDataView8Bit : BaseView8Bit, IView<PlanetDataModel>
{
    private const int LabelColumn = 1;
    private const int ValueColumn = 10;
    private const int FirstRow = 4;
    private const int DescriptionRow = 13;

    private readonly IViewSurface _surface;
    private readonly FastColor _colorGreen;
    private readonly FastColor _colorWhite;

    internal PlanetDataView8Bit(IViewSurface surface)
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

        int row = FirstRow;

        if (model.Distance.Length > 0)
        {
            DrawRow(ref row, "Distance:", model.Distance);
        }

        DrawRow(ref row, "Economy:", model.Economy);
        DrawRow(ref row, "Govt:", model.Government);
        DrawRow(ref row, "Tech:", model.TechLevel);
        DrawRow(ref row, "Pop:", model.Population);
        DrawRow(ref row, "Product:", model.Productivity);
        DrawRow(ref row, "Radius:", model.Radius);

        DrawTextPretty(new(Column(LabelColumn), Row(DescriptionRow)), 304, model.Description);
    }

    private void DrawRow(ref int row, string label, string value)
    {
        _surface.Graphics.DrawTextLeft(new(Column(LabelColumn), Row(row)), label, nameof(FontType.Small), _colorGreen);
        _surface.Graphics.DrawTextLeft(new(Column(ValueColumn), Row(row)), value, nameof(FontType.Small), _colorWhite);
        row++;
    }
}
