// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Audio.Midi;

// A channel message and the output sample it falls on. The command is the status byte's high nibble (0x80-0xE0).
internal readonly record struct MidiEvent(int SampleTime, int Channel, int Command, int Data1, int Data2);
