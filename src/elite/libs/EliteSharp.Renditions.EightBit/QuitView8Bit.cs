// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind;

namespace EliteSharp.Renditions.EightBit;

/// <summary>
/// The 8-bit quit confirmation: authored for the 320x256 canvas and its fixed
/// 8x8 font. The prompt is Small here where the 16-bit view uses Large - both
/// map to the same 8x8 cells at this tier, and Small is what the rest of the
/// 8-bit screens use.
/// </summary>
internal sealed class QuitView8Bit : BaseView8Bit, IView<QuitModel>
{
    private readonly FastColor _colorYellow;

    internal QuitView8Bit(IViewSurface surface)
        : base(surface)
        => _colorYellow = surface.Palette["Yellow"];

    public void Draw(QuitModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        DrawViewHeader(model.Header);

        DrawTextCentreOnGrid(12, model.Prompt, nameof(FontType.Small), _colorYellow);
    }
}
