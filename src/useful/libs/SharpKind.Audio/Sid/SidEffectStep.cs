// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Audio.Sid;

// One effect a sound starts. A frequency and sustain/release given together override the effect's, as NOISE2 does; a
// layered step plays even if the effect is already playing (the C64's "effect + 128"); a delayed step waits that many frames.
internal readonly record struct SidEffectStep(SidEffect Effect, byte? Frequency, byte? SustainRelease, bool Layer, int DelayFrames);
