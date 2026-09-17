// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

namespace EliteSharpLib.Tests.GoldenTrace;

// The player is always at the origin in Elite, so a slot's Location is already relative to the ship.
//
// ExpDelta is here because it's the one piece of game state the renderer owns: EliteDraw seeds and
// advances it. Without it in the trace, a simulate/compose split would only show up indirectly.
internal readonly record struct TraceObject(
    int Slot,
    string Type,
    float X,
    float Y,
    float Z,
    float RotX,
    float RotZ,
    string Flags,
    float ExpDelta);
