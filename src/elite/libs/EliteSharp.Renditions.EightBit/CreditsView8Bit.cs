// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind.UI;

namespace EliteSharp.Renditions.EightBit;

/// <summary>
/// The 8-bit credits screen: the version, the names under it, and the Back row
/// at the foot. The credits are word-wrapped rather than drawn one centred
/// line each - the longest is wider than this tier's 40-character row - which
/// is why they share <see cref="TextWrap"/> with
/// <see cref="IBaseView.DrawTextPretty"/> but not its drawing: these rows are
/// centred rather than left-aligned.
/// <para>
/// Every centred row snaps to the character cell, as everywhere else in this
/// tier: pixel-centring an odd-length string lands it on half a cell. The Back
/// row is the width of the options menu's selection bar, so the two screens'
/// selected rows are the same shape.
/// </para>
/// </summary>
internal sealed class CreditsView8Bit : BaseView8Bit, IView<CreditsModel>
{
    private const int VersionRow = 7;
    private const int CreditsFirstRow = 10;

    // The row and the width the settings screens' Back row uses, so every way
    // out of the options family sits in the same place and is the same size.
    private const int BackRow = 19;
    private const int BackBarWidth = 200;
    private const int BackBarHeight = 8;

    // 320px of 8x8 characters, less a character of margin either side.
    private const int MaxCharsPerLine = 38;

    private readonly IViewSurface _surface;
    private readonly ControlStyle _style;
    private readonly Container<Label> _credits;
    private readonly Label _version;
    private readonly Label _back;

    internal CreditsView8Bit(IViewSurface surface)
        : base(surface)
    {
        _surface = surface;

        _style = new(
            nameof(FontType.Small),
            ControlColors.TextOnly(surface.Palette["White"]),
            new(surface.Palette["Red"], surface.Palette["White"]),
            ControlColors.TextOnly(surface.Palette["LightGray"]));

        _credits = new(surface.Graphics, _style) { ChildAlignment = TextAlignment.Centre, Spacing = EightBitGrid.RowHeight };

        _version = new(surface.Graphics, _style, new TextSetting())
        {
            Alignment = TextAlignment.Centre,
            Width = surface.Layout.ViewportWidth,
            SnapToCell = EightBitGrid.CharacterWidth,
        };

        // Always selected: it is the only thing on this screen to do.
        _back = new(surface.Graphics, _style, new TextSetting("Back"))
        {
            Alignment = TextAlignment.Centre,
            Width = BackBarWidth,
            Height = BackBarHeight,
            SnapToCell = EightBitGrid.CharacterWidth,
            State = ControlState.Selected,
        };
    }

    public void Draw(CreditsModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        DrawViewHeader("CREDITS");

        _version.Position = new(_surface.Layout.ViewportLeft, Row(VersionRow));
        _version.Setting.Value = model.Version;
        _version.Draw();

        List<string> lines = [];
        foreach (string credit in model.Credits)
        {
            lines.AddRange(TextWrap.Split(credit, MaxCharsPerLine));
        }

        FillCredits(lines);

        _credits.Width = _surface.Layout.ViewportWidth;
        _credits.Position = new(_surface.Layout.ViewportLeft, Row(CreditsFirstRow));
        _credits.Draw();

        _back.Position = new(_surface.Layout.ViewportCentre.X - (BackBarWidth / 2), Row(BackRow));
        _back.Draw();
    }

    // The rows are rebuilt only when the count changes, which it does when a
    // credit wraps to a different number of lines.
    private void FillCredits(List<string> lines)
    {
        if (_credits.Children.Count != lines.Count)
        {
            _credits.Clear();
            for (int i = 0; i < lines.Count; i++)
            {
                _credits.Add(new Label(_surface.Graphics, _style, new TextSetting())
                {
                    Alignment = TextAlignment.Centre,
                    Width = _surface.Layout.ViewportWidth,
                    SnapToCell = EightBitGrid.CharacterWidth,
                });
            }
        }

        for (int i = 0; i < lines.Count; i++)
        {
            _credits.Children[i].Setting.Value = lines[i];
        }
    }
}
