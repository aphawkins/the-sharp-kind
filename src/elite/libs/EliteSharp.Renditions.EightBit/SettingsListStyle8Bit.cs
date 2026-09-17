// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind.UI;

namespace EliteSharp.Renditions.EightBit;

/// <summary>
/// How the settings screens look on the 320x256 canvas and its fixed 8x8
/// font. Everything here is a whole number of character cells, and centred
/// text snaps to one: pixel-centring an odd-length string on this grid lands
/// it on half a cell.
/// <para>
/// One setting per row, name and value side by side, with no blank row
/// between them. The value sits at column 24, which leaves the longest name
/// ("Graphic Style:", ending at column 20) clear of the opening arrow at 22;
/// the widest value ("Wireframe") then ends at column 32 and the closing
/// arrow takes 34, inside the 40-column row. The Back row carries neither, so
/// it keeps the narrower block the settings had before the arrows needed the
/// room.
/// </para>
/// </summary>
internal static class SettingsListStyle8Bit
{
    private const int FirstRow = 6;
    private const int RowHeightRows = 1;
    private const int BackRow = 19;
    private const int FooterRow = 21;
    private const int MarginColumn = 7;
    private const int ValueColumn = 24;
    private const int RowWidthColumns = 28;
    private const int BackRowWidthColumns = 25;
    private const int ArrowGapColumns = 1;

    internal static SettingsListStyle Create(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        // A settings row is never greyed out, so there is no disabled look
        // here beyond the normal one.
        ControlColors text = ControlColors.TextOnly(surface.Palette["White"]);
        ControlColors selected = new(surface.Palette["Red"], surface.Palette["White"]);

        return new(
            RowStyle: new(nameof(FontType.Small), text, selected, text),
            ValueStyle: new(nameof(FontType.Small), text, text, text),
            RowsLeft: EightBitGrid.Of(surface).Column(MarginColumn),
            FirstRowY: Row(surface, FirstRow),
            RowHeight: RowHeightRows * EightBitGrid.RowHeight,
            RowWidth: RowWidthColumns * EightBitGrid.CharacterWidth,
            ValueOffsetX: (ValueColumn - MarginColumn) * EightBitGrid.CharacterWidth,
            ArrowGap: ArrowGapColumns * EightBitGrid.CharacterWidth,
            BackRowWidth: BackRowWidthColumns * EightBitGrid.CharacterWidth,
            BackRowY: Row(surface, BackRow),
            FooterY: Row(surface, FooterRow),
            SnapToCell: EightBitGrid.CharacterWidth);
    }

    private static float Row(IViewSurface surface, int row) => EightBitGrid.Of(surface).Row(row);
}
