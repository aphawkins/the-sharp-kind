// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

namespace EliteSharpLib.Views;

// A control that must act once per press, from a source that only says
// whether it is down. Held, it fires on the first update and no other.
internal sealed class Edge
{
    private bool _wasHeld;

    internal bool Pressed(bool held)
    {
        bool pressed = held && !_wasHeld;
        _wasHeld = held;

        return pressed;
    }
}
