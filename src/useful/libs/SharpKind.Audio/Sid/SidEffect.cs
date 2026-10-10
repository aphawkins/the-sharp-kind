// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Audio.Sid;

// One row of C64 Elite's sound-effect tables. The index tells effects apart while they play.
internal readonly record struct SidEffect(
    int Index,
    byte Priority,
    byte Frames,
    byte Frequency,
    byte Control,
    byte AttackDecay,
    byte SustainRelease,
    sbyte FrequencyChange,
    byte VolumeMask);
