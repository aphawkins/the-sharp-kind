// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Audio.SoundFonts;

// One sample header: where a sample sits in the sample data, its loop, and the pitch it was recorded at.
internal readonly record struct SoundFontSample(
    int Start,
    int End,
    int LoopStart,
    int LoopEnd,
    int SampleRate,
    int OriginalPitch,
    int PitchCorrection);
