// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind.UI;

namespace EliteSharp.Renditions.EightBit;

/// <summary>
/// The 8-bit options menu: authored for the 320x256 canvas and its fixed 8x8
/// font. The 16-bit view's 400px selection bar overflows a 320-wide screen, so
/// this tier has its own width.
/// <para>
/// Every centred row here snaps to the character cell: this tier's font is a
/// fixed 8x8, and pixel-centring an odd-length string lands it on half a cell.
/// The Back row is one of the rows rather than a control of its own, so it
/// sits on the same bar and the same grid as the rest.
/// </para>
/// </summary>
internal sealed class OptionsView8Bit : BaseView8Bit, IView<OptionsModel>
{
    private const int OptionBarHeight = 8;
    private const int OptionBarWidth = 240;
    private const int FirstRow = 7;
    private const int RowSpacingRows = 2;

    private readonly IViewSurface _surface;
    private readonly ControlStyle _style;
    private readonly Container<Label> _options;

    internal OptionsView8Bit(IViewSurface surface)
        : base(surface)
    {
        _surface = surface;

        _style = new(
            nameof(FontType.Small),
            ControlColors.TextOnly(surface.Palette["White"]),
            new(surface.Palette["Red"], surface.Palette["White"]),
            ControlColors.TextOnly(surface.Palette["LightGray"]));

        _options = new(surface.Graphics, _style) { ChildAlignment = TextAlignment.Centre, Spacing = RowSpacingRows * RowHeight };
    }

    public void Draw(OptionsModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        DrawViewHeader("OPTIONS");

        FillOptions(model);

        _options.Width = OptionBarWidth;
        _options.Position = new(_surface.Layout.ViewportCentre.X - (OptionBarWidth / 2), Row(FirstRow));
        _options.Draw();
    }

    // The rows are rebuilt only when the count changes: it never does in
    // practice, but the model owns the list, so the view does not assume it.
    private void FillOptions(OptionsModel model)
    {
        if (_options.Children.Count != model.Options.Count)
        {
            _options.Clear();
            for (int i = 0; i < model.Options.Count; i++)
            {
                _options.Add(new Label(_surface.Graphics, _style, new TextSetting())
                {
                    Alignment = TextAlignment.Centre,
                    Width = OptionBarWidth,
                    Height = OptionBarHeight,
                    SnapToCell = CharacterWidth,
                });
            }
        }

        for (int i = 0; i < model.Options.Count; i++)
        {
            Label row = _options.Children[i];
            row.Setting.Value = model.Options[i].Label;
            row.State = (i == model.HighlightedIndex, model.Options[i].IsEnabled) switch
            {
                (true, true) => ControlState.Selected,
                (true, false) => ControlState.SelectedDisabled,
                (false, true) => ControlState.Normal,
                (false, false) => ControlState.Disabled,
            };
        }
    }
}
