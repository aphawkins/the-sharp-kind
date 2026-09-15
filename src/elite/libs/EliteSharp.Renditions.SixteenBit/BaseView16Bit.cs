// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind;
using SharpKind.Graphics;

namespace EliteSharp.Renditions.SixteenBit;

/// <summary>
/// The 16-bit tier's shared chrome and text helpers. Keeps the spacing the
/// 16-bit screens have always used, against its proportional font and the
/// tier's larger canvas.
/// </summary>
internal class BaseView16Bit : IBaseView
{
    // The 16-bit font is proportional, so this is the nominal character width
    // the word-wrap estimates against rather than a measured one.
    private const int CharacterWidth = 8;

    private readonly FastColor _colorGold;
    private readonly FastColor _colorWhite;
    private readonly float _rowHeight;

    internal BaseView16Bit(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        Graphics = surface.Graphics;
        Layout = surface.Layout;
        _rowHeight = 8 * surface.Layout.DesignScale;
        _colorGold = surface.Palette["Gold"];
        _colorWhite = surface.Palette["White"];
    }

    public IGraphics Graphics { get; }

    public ViewLayout Layout { get; }

    // Right-aligned, so the proportional font's width never has to be
    // estimated to place this.
    public void DrawFps(int fps)
        => Graphics.DrawTextRight(
            new(Layout.ViewportRight - 4, Layout.ViewportTop + 4),
            $"FPS: {fps}",
            nameof(FontType.Small),
            _colorWhite);

    public void DrawHyperspaceCountdown(int countdown)
        => Graphics.DrawTextRight(
            new(Layout.ViewportLeft + 21, Layout.ViewportTop + 4),
            $"{countdown}",
            nameof(FontType.Small),
            _colorWhite);

    public void DrawInfoMessage(string message)
        => Graphics.DrawTextCentre(
            Layout.ViewportHeight - 40,
            message,
            nameof(FontType.Small),
            _colorWhite);

    public void DrawViewHeader(string title)
    {
        Graphics.DrawTextCentre(Layout.ViewportTop + 6, title, nameof(FontType.Large), _colorGold);
        Graphics.DrawLine(
            new(Layout.ViewportLeft, Layout.ViewportTop + 35),
            new(Layout.ViewportRight, Layout.ViewportTop + 35),
            _colorWhite);
    }

    public void DrawTextPretty(Vector2 position, float width, string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        foreach (string line in TextWrap.Split(text, (int)(width / CharacterWidth)))
        {
            Graphics.DrawTextLeft(position, line, nameof(FontType.Small), _colorWhite);
            position.Y += _rowHeight;
        }
    }
}
