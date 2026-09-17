// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharpLib.Views;

namespace EliteSharpLib.Tests.GoldenTrace;

// Deliberately not a rendered frame: the frame-rate rework changes when things happen, not what
// they look like, and a pixel comparison would fail for unrelated reasons.
//
// No player position: Elite holds the player at the origin and moves the universe past it, so player motion shows up in every object's Location.
internal sealed record TraceSample(
    int Tick,
    Screen Screen,
    bool IsDocked,
    bool IsGameOver,
    int MCount,
    float MessageCount,
    float LaserTemp,
    float Roll,
    float Pitch,
    float Speed,
    float Energy,
    float ShieldFront,
    float ShieldRear,
    float Fuel,
    float CabinTemperature,
    float Altitude,
    IReadOnlyList<TraceObject> Objects);
