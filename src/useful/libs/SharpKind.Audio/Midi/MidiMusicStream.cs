// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Audio.SoundFonts;

namespace SharpKind.Audio.Midi;

// A MIDI tune played through a SoundFont.
internal sealed class MidiMusicStream(MidiFile file, SoundFont soundFont, bool repeat) : IMusicSource
{
    private readonly MidiSequencer _sequencer = new(file, new Synthesizer(soundFont), repeat);

    public void Render(Span<float> buffer) => _sequencer.Render(buffer);

    // The synthesizer holds no unmanaged resources.
    public void Dispose()
    {
    }
}
