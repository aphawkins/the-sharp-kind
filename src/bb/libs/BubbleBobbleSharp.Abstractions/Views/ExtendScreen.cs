// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>Which of the EXTEND bonus's screens (<c>$32CD</c>) is shown in place of the level.</summary>
public enum ExtendScreen
{
    /// <summary>All of it black, the score panel too, and no sprites (<c>$E374</c>).</summary>
    Cleared = 0,

    /// <summary>The screen of fruit, with the box, the player's text and the EXTEND banner over it (<c>$3389</c>-<c>$33EF</c>).</summary>
    Fruit = 1,

    /// <summary>The other screen, black but for the player's text, which the fruit takes turns with (<c>$33FC</c>).</summary>
    Text = 2,
}
