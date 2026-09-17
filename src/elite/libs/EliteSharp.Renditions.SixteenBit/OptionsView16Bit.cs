// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind.UI;

namespace EliteSharp.Renditions.SixteenBit;

/// <summary>
/// The 16-bit options menu: the 512-space layout, and nothing else.
/// <para>
/// One stack, a container the rows are centred in. The rows are the width of
/// the selection bar, so a selected row's block is the bar - the Back row
/// among them, rather than a control of its own placed separately.
/// </para>
/// </summary>
internal sealed class OptionsView16Bit : BaseView16Bit, IView<OptionsModel>
{
    private const int OptionBarHeight = 15;
    private const int OptionBarWidth = 400;
    private const int OptionSpacing = 30;

    private readonly IViewSurface _surface;
    private readonly ControlStyle _style;
    private readonly Container<Label> _options;

    internal OptionsView16Bit(IViewSurface surface)
        : base(surface)
    {
        _surface = surface;

        _style = new(
            nameof(FontType.Small),
            ControlColors.TextOnly(surface.Palette["White"]),
            new(surface.Palette["LightRed"], surface.Palette["White"]),
            ControlColors.TextOnly(surface.Palette["LightGrey"]));

        _options = new(surface.Graphics, _style) { ChildAlignment = TextAlignment.Centre, Spacing = OptionSpacing };
    }

    public void Draw(OptionsModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        DrawViewHeader("GAME OPTIONS");

        FillOptions(model);

        _options.Width = OptionBarWidth;
        _options.Position = new(
            _surface.Layout.ViewportCentre.X - (OptionBarWidth / 2),
            (_surface.Layout.ViewportHeight - (OptionSpacing * model.Options.Count)) / 2);
        _options.Draw();
    }

    // Rebuilt only when the count changes; the model owns the list, not the view.
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
