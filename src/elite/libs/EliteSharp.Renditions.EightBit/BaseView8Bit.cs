// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind;
using SharpKind.Graphics;

namespace EliteSharp.Renditions.EightBit;

/// <summary>
/// The 8-bit tier's shared chrome and text helpers, authored for the 320x256
/// canvas and its fixed 8x8 font. The 8-bit palette is the machine's 16
/// colours under their web names, so there is no Gold here: the header is
/// Yellow, as the hardware this stands in for would have had it.
/// </summary>
internal class BaseView8Bit : IBaseView
{
    // The title, the frame rate and the hyperspace countdown all share this
    // row; the rule closing the header sits on the next one.
    internal const int ChromeRow = 1;
    internal const int RuleRow = 2;

    // The first row and last column a screen's content may use, the border
    // owning everything outside them.
    internal const int FirstContentRow = 3;
    internal const int LastTextColumn = 38;

    // The title screens carry no view header, so their wordmark takes the
    // chrome band and the row under the rule's place.
    internal const int TitleRow = 1;
    internal const int SubtitleRow = 3;

    // Low enough to stay clear of the cockpit view, five rows off the bottom.
    internal const int InfoMessageRow = 20;

    // The 8-bit font is a monospaced 8x8 sheet - Small and Large are the same
    // bbc-micro cell - so a row is 8 pixels and a character is 8 wide, both
    // fixed, unlike the 16-bit proportional font. The viewport is a whole
    // number of these: 40 columns by 25 rows.
    internal const int CharacterWidth = 8;
    internal const int RowHeight = 8;

    // The left and right border are two pixels wide; top and bottom are one.
    internal const int SideBorderWidth = 2;

    private readonly FastColor _colorWhite;
    private readonly FastColor _colorYellow;

    internal BaseView8Bit(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        Graphics = surface.Graphics;
        Layout = surface.Layout;
        _colorWhite = surface.Palette["White"];
        _colorYellow = surface.Palette["Yellow"];
    }

    public IGraphics Graphics { get; }

    public ViewLayout Layout { get; }

    // Drawn over the viewport's own edge, so it needs no clip juggling: the
    // rectangle is the viewport, and the view clip region already admits it.
    public void DrawBorder()
    {
        // Top
        Graphics.DrawLine(new(Layout.ViewportLeft, Layout.ViewportTop), new(Layout.ViewportWidth, Layout.ViewportTop), _colorWhite);

        // Left and right are two pixels wide, the second line one pixel
        // inboard of the first; top and bottom stay single-pixel.
        for (int i = 0; i < SideBorderWidth; i++)
        {
            // Left
            Graphics.DrawLine(
                new(Layout.ViewportLeft + i, Layout.ViewportTop),
                new(Layout.ViewportLeft + i, Layout.ViewportBottom),
                _colorWhite);

            // Right
            Graphics.DrawLine(
                new(Layout.ViewportWidth - 1 - i, Layout.ViewportTop),
                new(Layout.ViewportWidth - 1 - i, Layout.ViewportBottom),
                _colorWhite);
        }
    }

    // Row 0 and the outermost columns belong to the border, which overlays
    // their first pixel; most of this font's glyphs ink theirs, so no text
    // goes there. The chrome band is row 1, and the rule under it is row 2,
    // which leaves row 3 as the first row a screen's content may use.
    public void DrawFps(int fps)
        => Graphics.DrawTextRight(
            new(Column(LastTextColumn + 1), Row(ChromeRow)),
            $"FPS: {fps}",
            nameof(FontType.Small),
            _colorWhite);

    public void DrawHyperspaceCountdown(int countdown)
        => Graphics.DrawTextRight(
            new(Column(2), Row(ChromeRow)),
            $"{countdown}",
            nameof(FontType.Small),
            _colorWhite);

    public void DrawInfoMessage(string message)
        => DrawTextCentreOnGrid(InfoMessageRow, message, nameof(FontType.Small), _colorWhite);

    public void DrawViewHeader(string title)
    {
        DrawTextCentreOnGrid(ChromeRow, title, nameof(FontType.Large), _colorYellow);
        Graphics.DrawLine(
            new(Layout.ViewportLeft, Row(RuleRow) + 4),
            new(Layout.ViewportRight, Row(RuleRow) + 4),
            _colorWhite);
    }

    public void DrawTextPretty(Vector2 position, float width, string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        foreach (string line in TextWrap.Split(text, (int)(width / CharacterWidth)))
        {
            Graphics.DrawTextLeft(position, line, nameof(FontType.Small), _colorWhite);
            position.Y += RowHeight;
        }
    }

    /// <summary>
    /// Advances a position to the next character cell, for text whose place is
    /// decided by something other than the grid - the short range chart's
    /// planet labels sit where the plot puts them. Rounding up rather than to
    /// the nearest keeps a label clear of the blob it names, which rounding
    /// down would let it overlap. Valid because the viewport starts at the
    /// screen origin, so a cell boundary is a multiple of the cell size with
    /// nothing to offset it against.
    /// </summary>
    internal static Vector2 SnapToGrid(Vector2 position) => new(
        MathF.Ceiling(position.X / CharacterWidth) * CharacterWidth,
        MathF.Ceiling(position.Y / RowHeight) * RowHeight);

    /// <summary>
    /// Gets the y of a character row, 0 being the topmost. The viewport is 25
    /// rows tall.
    /// </summary>
    internal float Row(int row) => Layout.ViewportTop + (row * RowHeight);

    /// <summary>
    /// Gets the x of a character column, 0 being the leftmost. The viewport is
    /// 40 columns wide.
    /// </summary>
    internal float Column(int column) => Layout.ViewportLeft + (column * CharacterWidth);

    /// <summary>
    /// Draws the title screens' wordmark, in text rather than a bitmap: the
    /// 8-bit tier has only its 8x8 font, so the name is spelled out on the
    /// grid. "---- E L I T E ----" is 19 characters, which centres on the
    /// 40-column row.
    /// </summary>
    internal void DrawTitle()
    {
        DrawTextCentreOnGrid(TitleRow, "---- E L I T E ----", nameof(FontType.Large), _colorYellow);
        DrawTextCentreOnGrid(SubtitleRow, "The Sharp Kind", nameof(FontType.Small), _colorWhite);
    }

    /// <summary>
    /// Draws text centred on the column grid. Graphics.DrawTextCentre centres
    /// on the pixel, which lands an odd-length string on half a cell.
    /// </summary>
    internal void DrawTextCentreOnGrid(int row, string text, string fontType, in FastColor color)
    {
        ArgumentNullException.ThrowIfNull(text);

        int columns = (int)(Layout.ViewportWidth / CharacterWidth);
        Graphics.DrawTextLeft(new(Column((columns - text.Length) / 2), Row(row)), text, fontType, color);
    }
}
