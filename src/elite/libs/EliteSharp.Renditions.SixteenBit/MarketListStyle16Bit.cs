// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind.UI;

namespace EliteSharp.Renditions.SixteenBit;

/// <summary>
/// How the market looks laid out against the 640-wide viewport: the left margin
/// (16) is unchanged from the 512-wide layout, and every other column is
/// stretched by the same 640/512 ratio the columns themselves were spread
/// across.
/// <para>
/// Nineteen rows is what fits between the first at y=55 and the cash line at
/// y=340, at fifteen pixels a row. That number is
/// <see cref="MarketListStyle.VisibleRows"/> now rather than an assumption, so a
/// goods set with more wares than that scrolls instead of drawing over the cash.
/// </para>
/// </summary>
internal static class MarketListStyle16Bit
{
    private const float NameLeft = 16;
    private const float UnitsLeft = 221;
    private const float PriceRight = 352;
    private const float ForSaleRight = 452;
    private const float InHoldRight = 566;
    private const float HeadingY = 40;
    private const float FirstRowY = 55;
    private const float RowHeight = 15;
    private const float CashY = 340;

    // Matches the hand-drawn screen's 2px inset from each edge.
    private const float RowsLeft = 2;
    private const float RowInset = 2 * RowsLeft;

    private const int VisibleRows = (int)((CashY - FirstRowY) / RowHeight);

    internal static MarketListStyle Create(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        ControlColors white = ControlColors.TextOnly(surface.Palette["White"]);
        ControlColors green = ControlColors.TextOnly(surface.Palette["Green"]);
        ControlColors selected = new(surface.Palette["LightRed"], surface.Palette["White"]);

        return new(
            RowStyle: new(nameof(FontType.Small), white, selected, white),
            HeadingStyle: new(nameof(FontType.Small), green, green, green),
            Columns:
            [
                Cell(NameLeft, TextAlignment.Left),
                Cell(UnitsLeft, TextAlignment.Left),
                Cell(PriceRight, TextAlignment.Right),
                Cell(ForSaleRight, TextAlignment.Right),
                Cell(ForSaleRight, TextAlignment.Left),
                Cell(InHoldRight, TextAlignment.Right),
                Cell(InHoldRight, TextAlignment.Left),
            ],
            Headings:
            [
                new("PRODUCT", NameLeft, HeadingY, TextAlignment.Left),
                new("UNIT", 204, HeadingY, TextAlignment.Left),
                new("PRICE", 304, HeadingY, TextAlignment.Left),
                new("FOR SALE", 389, HeadingY, TextAlignment.Left),
                new("IN HOLD", 521, HeadingY, TextAlignment.Left),
            ],
            RowsLeft: RowsLeft,
            FirstRowY: FirstRowY,
            RowHeight: RowHeight,
            RowWidth: surface.Layout.ViewportWidth - RowInset,
            VisibleRows: VisibleRows,
            CashCaption: new("Cash:", NameLeft, CashY, TextAlignment.Left),
            CashAmount: new(string.Empty, 277, CashY, TextAlignment.Right));
    }

    // Offset is from the row's left edge, which starts inside the viewport, not the text margin.
    private static TableColumn Cell(float x, TextAlignment alignment) => new(x - RowsLeft, alignment);
}
