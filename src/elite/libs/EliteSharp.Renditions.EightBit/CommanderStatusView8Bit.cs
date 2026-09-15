// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind;

namespace EliteSharp.Renditions.EightBit;

/// <summary>
/// The 8-bit commander status screen: authored fresh for the 320x256 canvas
/// and its fixed 8x8 font, rather than derived from the 16-bit layout - see
/// docs/backlog-roadmap.md's "Author the 8-bit view layouts" item for why.
/// Labels are shorter than the 16-bit view's own, since the 8-bit font is
/// monospace: at 8px/character, 320px is only 40 characters wide, and the
/// widest value ("Front Military Laser", 21 chars) already needs most of a
/// row to itself, which is why equipment is one column here instead of two.
/// </summary>
internal sealed class CommanderStatusView8Bit : BaseView8Bit, IView<CommanderStatusModel>
{
    private const int LabelColumn = 1;
    private const int ValueColumn = 13;
    private const int EquipmentColumn = 1;
    private const int DataRows = 7;
    private const int FirstRow = FirstContentRow;

    // A blank row separates the data block from the equipment list.
    private const int EquipmentHeaderRow = FirstRow + DataRows + 1;
    private const int EquipmentFirstRow = EquipmentHeaderRow + 1;

    private readonly IViewSurface _surface;
    private readonly FastColor _colorGreen;
    private readonly FastColor _colorWhite;

    internal CommanderStatusView8Bit(IViewSurface surface)
        : base(surface)
    {
        _surface = surface;

        _colorGreen = surface.Palette["Green"];
        _colorWhite = surface.Palette["White"];
    }

    public void Draw(CommanderStatusModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        DrawViewHeader(model.Title);

        DrawRow(FirstRow, "System:", model.PresentSystem);
        DrawRow(FirstRow + 1, "Hyperspace:", model.HyperspaceSystem);
        DrawRow(FirstRow + 2, "Condition:", model.Condition);
        DrawRow(FirstRow + 3, "Fuel:", model.Fuel);
        DrawRow(FirstRow + 4, "Cash:", model.Cash);
        DrawRow(FirstRow + 5, "Legal:", model.LegalStatus);
        DrawRow(FirstRow + 6, "Rating:", model.Rating);

        _surface.Graphics.DrawTextLeft(
            new(Column(LabelColumn), Row(EquipmentHeaderRow)),
            "EQUIPMENT:",
            nameof(FontType.Small),
            _colorGreen);

        DrawEquipment(model.Equipment);
    }

    private void DrawRow(int row, string label, string value)
    {
        _surface.Graphics.DrawTextLeft(new(Column(LabelColumn), Row(row)), label, nameof(FontType.Small), _colorGreen);

        if (!string.IsNullOrEmpty(value))
        {
            _surface.Graphics.DrawTextLeft(new(Column(ValueColumn), Row(row)), value, nameof(FontType.Small), _colorWhite);
        }
    }

    // One column, unlike the 16-bit view's two: even the longest equipment
    // string ("Front Military Laser") already needs most of the screen's
    // 40-character width, so a second column would overlap it.
    private void DrawEquipment(IReadOnlyList<string> equipment)
    {
        int row = EquipmentFirstRow;

        foreach (string item in equipment)
        {
            _surface.Graphics.DrawTextLeft(new(Column(EquipmentColumn), Row(row)), item, nameof(FontType.Small), _colorWhite);
            row++;
        }
    }
}
