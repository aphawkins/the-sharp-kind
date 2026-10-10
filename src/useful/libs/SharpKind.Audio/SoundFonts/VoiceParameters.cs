// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Audio.SoundFonts;

// Everything a voice needs to play one note of one instrument zone: sample offsets are absolute indices into the sample
// data, tunings are in semitones and cents, attenuation and sustain are centibels, and envelope times are timecents.
internal readonly record struct VoiceParameters(
    int SampleStart,
    int SampleEnd,
    int LoopStart,
    int LoopEnd,
    int SampleRate,
    int RootKey,
    int PitchCorrection,
    int CoarseTune,
    int FineTune,
    int ScaleTuning,
    int Pan,
    int Attenuation,
    int DelayVolEnv,
    int AttackVolEnv,
    int HoldVolEnv,
    int DecayVolEnv,
    int SustainVolEnv,
    int ReleaseVolEnv,
    int SampleModes,
    int ExclusiveClass);
