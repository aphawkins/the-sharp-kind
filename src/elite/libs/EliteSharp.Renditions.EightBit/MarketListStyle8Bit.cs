// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind.UI;

namespace EliteSharp.Renditions.EightBit;

/// <summary>
/// How the market looks on the 320x256 canvas and its fixed 8x8 font. A
/// first-draft layout, not derived from the 16-bit one - see
/// docs/backlog-roadmap.md's "Author the 8-bit view layouts" item. It drops the
/// 16-bit screen's separate "Unit" column (the unit is folded into the quantity
/// text instead, e.g. "20t") since five columns does not fit a 320px screen at
/// 8px per character.
/// <para>
/// Seventeen rows is what fits: the first is at row 5 and the cash line is at
/// row 23, and the two must not meet. That number is <see
/// cref="MarketListStyle.VisibleRows"/> now rather than an assumption, so a
/// goods set with more than seventeen wares scrolls instead of drawing over the
/// cash.
/// </para>
/// </summary>
internal static class MarketListStyle8Bit
{
    private const int NameColumn = 1;
    private const int UnitRightColumn = 16;
    private const int PriceRightColumn = 24;
    private const int ForSaleRightColumn = 29;
    private const int InHoldRightColumn = 37;
    private const int HeaderRow = 3;
    private const int FirstRow = 5;
    private const int CashRow = 23;
    private const int VisibleRows = CashRow - FirstRow - 1;

    internal static MarketListStyle Create(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        ControlColors white = ControlColors.TextOnly(surface.Palette["White"]);
        ControlColors green = ControlColors.TextOnly(surface.Palette["Green"]);
        ControlColors selected = new(surface.Palette["Red"], surface.Palette["White"]);

        // The cursor block spans the viewport rather than the text columns, as
        // the hand-drawn screen did: two pixels in from each edge.
        float rowsLeft = surface.Layout.ViewportLeft + 2;

        return new(
            RowStyle: new(nameof(FontType.Small), white, selected, white),
            HeadingStyle: new(nameof(FontType.Small), green, green, green),
            Columns:
            [
                Cell(surface, rowsLeft, NameColumn, TextAlignment.Left),
                Cell(surface, rowsLeft, UnitRightColumn, TextAlignment.Left),
                Cell(surface, rowsLeft, PriceRightColumn, TextAlignment.Right),
                Cell(surface, rowsLeft, ForSaleRightColumn, TextAlignment.Right),
                Cell(surface, rowsLeft, ForSaleRightColumn, TextAlignment.Left),
                Cell(surface, rowsLeft, InHoldRightColumn, TextAlignment.Right),
                Cell(surface, rowsLeft, InHoldRightColumn, TextAlignment.Left),
            ],
            Headings:
            [
                Heading(surface, "PRODUCT", NameColumn, HeaderRow + 1, TextAlignment.Left),
                Heading(surface, "UNIT", UnitRightColumn + 2, HeaderRow + 1, TextAlignment.Right),
                Heading(surface, "UNIT", PriceRightColumn + 1, HeaderRow, TextAlignment.Right),
                Heading(surface, "PRICE", PriceRightColumn + 1, HeaderRow + 1, TextAlignment.Right),
                Heading(surface, "QUANTITY", ForSaleRightColumn + 5, HeaderRow, TextAlignment.Right),
                Heading(surface, "FOR SALE", ForSaleRightColumn + 5, HeaderRow + 1, TextAlignment.Right),
                Heading(surface, "IN", InHoldRightColumn + 1, HeaderRow, TextAlignment.Right),
                Heading(surface, "HOLD", InHoldRightColumn + 2, HeaderRow + 1, TextAlignment.Right),
            ],
            RowsLeft: rowsLeft,
            FirstRowY: Row(surface, FirstRow),
            RowHeight: BaseView8Bit.RowHeight,
            RowWidth: 316,
            VisibleRows: VisibleRows,
            CashCaption: Heading(surface, "Cash:", NameColumn, CashRow, TextAlignment.Left),
            CashAmount: Heading(surface, string.Empty, InHoldRightColumn, CashRow, TextAlignment.Right));
    }

    // Measured from the viewport, not the row's own margin, then the row's left is taken back off.
    private static TableColumn Cell(IViewSurface surface, float rowsLeft, int column, TextAlignment alignment)
        => new(Column(surface, column) - rowsLeft, alignment);

    private static PlacedText Heading(IViewSurface surface, string text, int column, int row, TextAlignment alignment)
        => new(text, Column(surface, column), Row(surface, row), alignment);

    private static float Column(IViewSurface surface, int column)
        => surface.Layout.ViewportLeft + (column * BaseView8Bit.CharacterWidth);

    private static float Row(IViewSurface surface, int row)
        => surface.Layout.ViewportTop + (row * BaseView8Bit.RowHeight);
}
