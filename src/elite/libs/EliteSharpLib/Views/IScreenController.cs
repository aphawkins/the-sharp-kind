// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using SharpKind.Abstraction;

namespace EliteSharpLib.Views;

/// <summary>
/// The behavioural half of one screen: it takes input, advances the screen's
/// own state and produces what gets drawn. The drawing half is an
/// IView it delegates to, so layout can vary per asset
/// tier while behaviour has a single home.
/// </summary>
internal interface IScreenController : IGameScreen
{
    public void HandleInput();

    /// <summary>
    /// What this screen draws into the universe layer, behind the HUD and
    /// clipped to the window region, rather than into the HUD layer that
    /// <see cref="IGameScreen.Draw"/> goes to.
    /// <para>
    /// Only the break-pattern screens have any: the pattern is seen through
    /// the canopy, so it belongs with the ships and not with the cockpit
    /// drawn over them. Empty for every other screen, which is why this has
    /// a body rather than being implemented sixteen times over.
    /// </para>
    /// </summary>
    public void DrawUniverse()
    {
    }
}
