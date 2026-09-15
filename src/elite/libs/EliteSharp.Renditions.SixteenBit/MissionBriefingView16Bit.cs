// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind;

namespace EliteSharp.Renditions.SixteenBit;

/// <summary>
/// The 16-bit mission messages, laid out against the 640-wide viewport. The
/// layout keys off what the briefing holds rather than which mission sent it -
/// a long message that runs the height of the screen is blocked around
/// whatever is pictured beside it, while a short one sits in the middle under
/// its headline. That is how the original laid these out, and it is what lets a
/// mission the game was never built against be drawn.
/// </summary>
internal sealed class MissionBriefingView16Bit : BaseView16Bit, IMissionBriefingView
{
    /// <summary>
    /// A message in two blocks runs past whatever is pictured on the right, so
    /// the upper block stops short of it and the lower one runs full width.
    /// </summary>
    private const int UpperBlockTop = 50;
    private const int UpperBlockWidth = 300;
    private const int LowerBlockTop = 200;
    private const int LowerBlockWidth = 600;

    /// <summary>
    /// A message in one block is set on its own in the middle of the screen,
    /// under the headline if there is one.
    /// </summary>
    private const int SingleBlockLeft = 120;
    private const int SingleBlockTop = 132;
    private const int SingleBlockWidth = 400;

    private const int BlockLeft = 16;
    private const int HeadlineTop = 100;
    private const int FooterTop = 330;

    private readonly IViewSurface _surface;
    private readonly FastColor _colorGold;

    internal MissionBriefingView16Bit(IViewSurface surface)
        : base(surface)
    {
        _surface = surface;

        _colorGold = surface.Palette["Gold"];
    }

    public Vector4 ShipLocation => new(200, 90, 600, 0);

    public void Draw(MissionBriefingModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (model.Paragraphs.Count == 0)
        {
            return;
        }

        DrawViewHeader("INCOMING MESSAGE");

        if (model.HasHeadline)
        {
            _surface.Graphics.DrawTextCentre(HeadlineTop, model.Headline, nameof(FontType.Large), _colorGold);
        }

        if (model.Paragraphs.Count == 1)
        {
            DrawTextPretty(new(SingleBlockLeft, SingleBlockTop), SingleBlockWidth, model.Paragraphs[0]);
        }
        else
        {
            DrawTextPretty(new(BlockLeft, UpperBlockTop), UpperBlockWidth, model.Paragraphs[0]);
            DrawTextPretty(new(BlockLeft, LowerBlockTop), LowerBlockWidth, model.Paragraphs[1]);
        }

        if (model.ShowPortrait)
        {
            _surface.Graphics.DrawImage(nameof(ImageType.Blake), new(480, 46));
        }

        _surface.Graphics.DrawTextCentre(FooterTop, "Press space to continue.", nameof(FontType.Large), _colorGold);
    }
}
