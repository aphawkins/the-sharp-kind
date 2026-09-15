// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind.UI;

namespace EliteSharp.Renditions.SixteenBit;

/// <summary>
/// The 16-bit credits screen: the version, the names under it one line each -
/// this tier's canvas is wide enough for the longest of them - and the Back
/// row at the foot.
/// <para>
/// Back sits where the settings screens put theirs, and is the same width, so
/// every way out of the options family is in the same place.
/// </para>
/// </summary>
internal sealed class CreditsView16Bit : BaseView16Bit, IView<CreditsModel>
{
    private const int VersionOffsetY = 100;
    private const int CreditsOffsetY = 160;
    private const int CreditSpacing = 30;
    private const int BackRowOffsetY = 80;
    private const int BackBarWidth = 260;
    private const int BackBarHeight = 15;

    private readonly IViewSurface _surface;
    private readonly ControlStyle _style;
    private readonly Container<Label> _credits;
    private readonly Label _version;
    private readonly Label _back;

    internal CreditsView16Bit(IViewSurface surface)
        : base(surface)
    {
        _surface = surface;

        _style = new(
            nameof(FontType.Small),
            ControlColors.TextOnly(surface.Palette["White"]),
            new(surface.Palette["LightRed"], surface.Palette["White"]),
            ControlColors.TextOnly(surface.Palette["LightGrey"]));

        _credits = new(surface.Graphics, _style) { ChildAlignment = TextAlignment.Centre, Spacing = CreditSpacing };

        _version = new(surface.Graphics, _style, new TextSetting())
        {
            Alignment = TextAlignment.Centre,
            Width = surface.Layout.ViewportWidth,
        };

        // Always selected: it is the only thing on this screen to do.
        _back = new(surface.Graphics, _style, new TextSetting("Back"))
        {
            Alignment = TextAlignment.Centre,
            Width = BackBarWidth,
            Height = BackBarHeight,
            State = ControlState.Selected,
        };
    }

    public void Draw(CreditsModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        DrawViewHeader("CREDITS");

        _version.Position = new(_surface.Layout.ViewportLeft, _surface.Layout.ViewportTop + VersionOffsetY);
        _version.Setting.Value = model.Version;
        _version.Draw();

        FillCredits(model);

        _credits.Width = _surface.Layout.ViewportWidth;
        _credits.Position = new(_surface.Layout.ViewportLeft, _surface.Layout.ViewportTop + CreditsOffsetY);
        _credits.Draw();

        _back.Position = new(
            _surface.Layout.ViewportCentre.X - (BackBarWidth / 2),
            _surface.Layout.ViewportBottom - BackRowOffsetY);
        _back.Draw();
    }

    // The rows are rebuilt only when the count changes: it never does in
    // practice, but the model owns the list, so the view does not assume it.
    private void FillCredits(CreditsModel model)
    {
        if (_credits.Children.Count != model.Credits.Count)
        {
            _credits.Clear();
            for (int i = 0; i < model.Credits.Count; i++)
            {
                _credits.Add(new Label(_surface.Graphics, _style, new TextSetting())
                {
                    Alignment = TextAlignment.Centre,
                    Width = _surface.Layout.ViewportWidth,
                });
            }
        }

        for (int i = 0; i < model.Credits.Count; i++)
        {
            _credits.Children[i].Setting.Value = model.Credits[i];
        }
    }
}
