// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind.UI;

namespace EliteSharp.Renditions.SixteenBit;

/// <summary>
/// How the settings screens look in the 512-space layout, against this tier's
/// proportional font. Nothing snaps to a cell here: the font has no grid.
/// <para>
/// One setting per row, the rows running consecutively down a single column
/// centred on the viewport, with the Back row near the foot of it rather than
/// immediately under the list. The value sits far enough in to leave the
/// longest name ("Graphic Style:") clear of the opening arrow.
/// </para>
/// </summary>
internal static class SettingsListStyle16Bit
{
    // A row is the font's line height, so consecutive rows leave no gap.
    private const int RowHeight = 16;
    private const int FirstRowOffset = 60;

    private const int RowWidth = 300;
    private const int ValueOffsetX = 140;
    private const int ArrowGap = 10;
    private const int BackRowWidth = 260;

    // Back row's distance up from the viewport bottom, footer's distance below it.
    private const int BackRowOffset = 80;
    private const int FooterGap = 40;

    internal static SettingsListStyle Create(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        // A settings row is never greyed out, so there is no disabled look here.
        ControlColors text = ControlColors.TextOnly(surface.Palette["White"]);
        ControlColors selected = new(surface.Palette["LightRed"], surface.Palette["White"]);

        float backRowY = surface.Layout.ViewportBottom - BackRowOffset;

        return new(
            RowStyle: new(nameof(FontType.Small), text, selected, text),
            ValueStyle: new(nameof(FontType.Small), text, text, text),
            RowsLeft: surface.Layout.ViewportCentre.X - (RowWidth / 2),
            FirstRowY: surface.Layout.ViewportTop + FirstRowOffset,
            RowHeight: RowHeight,
            RowWidth: RowWidth,
            ValueOffsetX: ValueOffsetX,
            ArrowGap: ArrowGap,
            BackRowWidth: BackRowWidth,
            BackRowY: backRowY,
            FooterY: backRowY + FooterGap,
            SnapToCell: 0);
    }
}
