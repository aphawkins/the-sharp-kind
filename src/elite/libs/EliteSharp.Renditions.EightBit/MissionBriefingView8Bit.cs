// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharp.Abstractions.Assets;
using EliteSharp.Abstractions.Views;
using SharpKind;

namespace EliteSharp.Renditions.EightBit;

/// <summary>
/// The 8-bit mission messages: a first-draft 320x256 layout, not derived from
/// the 16-bit one - see docs/backlog-roadmap.md's "Author the 8-bit view
/// layouts" item. Exact spacing is expected to be refined visually. As with the
/// 16-bit view, the layout keys off what the briefing holds rather than which
/// mission sent it.
/// </summary>
internal sealed class MissionBriefingView8Bit : BaseView8Bit, IMissionBriefingView
{
    private const int TextColumn = 1;

    /// <summary>
    /// A message in two blocks runs past whatever is pictured on the right, so
    /// the upper block stops short of it and the lower one runs full width.
    /// </summary>
    private const int UpperBlockRow = 3;
    private const int UpperBlockColumns = 32;
    private const int UpperBlockColumnsBesidePortrait = 31;

    /// <summary>
    /// A message in one block sits where the lower block of a long one would,
    /// under the headline if there is one.
    /// </summary>
    private const int LowerBlockRow = 13;
    private const int LowerBlockColumns = 39;

    private const int HeadlineRow = 3;
    private const int FooterRow = 23;

    private readonly IViewSurface _surface;
    private readonly FastColor _colorYellow;

    internal MissionBriefingView8Bit(IViewSurface surface)
        : base(surface)
    {
        _surface = surface;

        _colorYellow = surface.Palette["Yellow"];
    }

    public Vector4 ShipLocation => new(330, 80, 700, 0);

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
            DrawTextCentreOnGrid(HeadlineRow, model.Headline, nameof(FontType.Large), _colorYellow);
        }

        if (model.Paragraphs.Count > 1)
        {
            int upperColumns = model.ShowPortrait ? UpperBlockColumnsBesidePortrait : UpperBlockColumns;
            DrawTextPretty(new(Column(TextColumn), Row(UpperBlockRow)), Column(upperColumns), model.Paragraphs[0]);
        }

        DrawTextPretty(
            new(Column(TextColumn), Row(LowerBlockRow)),
            Column(LowerBlockColumns),
            model.Paragraphs[^1]);

        if (model.ShowPortrait)
        {
            _surface.Graphics.DrawImage(nameof(ImageType.Blake), new(Column(33), Row(3)));
        }

        DrawTextCentreOnGrid(FooterRow, "Press space to continue.", nameof(FontType.Small), _colorYellow);
    }
}
