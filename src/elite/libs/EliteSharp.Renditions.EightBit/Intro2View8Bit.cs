// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind;

namespace EliteSharp.Renditions.EightBit;

/// <summary>
/// The 8-bit ship parade screen: authored for the 320x256 canvas and its
/// fixed 8x8 font. The ship itself is drawn by the universe, not here.
/// </summary>
internal sealed class Intro2View8Bit : BaseView8Bit, IView<Intro2Model>
{
    private const int PromptRow = 23;
    private const int ShipNameRow = 21;

    private readonly FastColor _colorYellow;
    private readonly FastColor _colorWhite;

    internal Intro2View8Bit(IViewSurface surface)
        : base(surface)
    {
        _colorYellow = surface.Palette["Yellow"];
        _colorWhite = surface.Palette["White"];
    }

    public void Draw(Intro2Model model)
    {
        ArgumentNullException.ThrowIfNull(model);

        DrawTitle();

        DrawTextCentreOnGrid(PromptRow, model.Prompt, nameof(FontType.Small), _colorYellow);

        if (model.ShipName.Length > 0)
        {
            DrawTextCentreOnGrid(ShipNameRow, model.ShipName, nameof(FontType.Small), _colorWhite);
        }
    }
}
