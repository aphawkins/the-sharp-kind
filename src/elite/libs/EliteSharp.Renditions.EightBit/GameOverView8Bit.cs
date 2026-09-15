// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind;

namespace EliteSharp.Renditions.EightBit;

/// <summary>
/// The 8-bit game over screen: authored for the 320x256 canvas and its fixed
/// 8x8 font. The wreckage tumbling behind it is drawn by the universe, not
/// here.
/// </summary>
internal sealed class GameOverView8Bit : BaseView8Bit, IView<GameOverModel>
{
    private readonly FastColor _colorYellow;

    internal GameOverView8Bit(IViewSurface surface)
        : base(surface)
        => _colorYellow = surface.Palette["Yellow"];

    public void Draw(GameOverModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        DrawTextCentreOnGrid(12, model.Message, nameof(FontType.Small), _colorYellow);
    }
}
