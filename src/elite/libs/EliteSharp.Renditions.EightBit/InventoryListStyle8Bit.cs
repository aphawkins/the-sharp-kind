// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind.UI;

namespace EliteSharp.Renditions.EightBit;

/// <summary>
/// How the inventory looks on the 320x256 canvas and its fixed 8x8 font. A
/// first-draft layout, not derived from the 16-bit one - see
/// docs/backlog-roadmap.md's "Author the 8-bit view layouts" item.
/// <para>
/// The viewport is twenty-five rows and the cargo starts at row 7, so
/// seventeen rows fit above the HUD. The row on the boundary is left out: it
/// draws into the scanner's top edge. That number is
/// <see cref="InventoryListStyle.VisibleRows"/> now rather than an assumption,
/// so a hold carrying more sorts of thing than that scrolls instead of running
/// off the bottom of the screen.
/// </para>
/// </summary>
internal static class InventoryListStyle8Bit
{
    private const int LabelColumn = 1;
    private const int ValueColumn = 7;
    private const int QuantityColumn = 17;
    private const int FirstRow = 4;
    private const int CargoFirstRow = 7;

    // The 8-bit viewport is 25 rows; the last one before the HUD is dropped
    // because a glyph on it bleeds into the scanner.
    private const int ViewportRows = 25;
    private const int VisibleRows = ViewportRows - CargoFirstRow - 1;

    internal static InventoryListStyle Create(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        ControlColors white = ControlColors.TextOnly(surface.Palette["White"]);
        ControlColors green = ControlColors.TextOnly(surface.Palette["Green"]);

        float rowsLeft = Column(surface, LabelColumn);

        return new(
            RowStyle: new(nameof(FontType.Small), white, white, white),
            CaptionStyle: new(nameof(FontType.Small), green, green, green),
            Columns:
            [
                new(0, TextAlignment.Left),
                new(Column(surface, QuantityColumn) - rowsLeft, TextAlignment.Left),
            ],
            FuelCaption: Placed(surface, "Fuel:", LabelColumn, FirstRow),
            FuelValue: Placed(surface, string.Empty, ValueColumn, FirstRow),
            CashCaption: Placed(surface, "Cash:", LabelColumn, FirstRow + 1),
            CashValue: Placed(surface, string.Empty, ValueColumn, FirstRow + 1),
            RowsLeft: rowsLeft,
            FirstRowY: Row(surface, CargoFirstRow),
            RowHeight: EightBitGrid.RowHeight,
            RowWidth: (BaseView8Bit.LastTextColumn - LabelColumn) * EightBitGrid.CharacterWidth,
            VisibleRows: VisibleRows);
    }

    private static PlacedText Placed(IViewSurface surface, string text, int column, int row)
        => new(text, Column(surface, column), Row(surface, row), TextAlignment.Left);

    private static float Column(IViewSurface surface, int column) => EightBitGrid.Of(surface).Column(column);

    private static float Row(IViewSurface surface, int row) => EightBitGrid.Of(surface).Row(row);
}
