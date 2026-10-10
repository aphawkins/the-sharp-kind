// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Audio.Midi;

// Plays a MIDI file's events into a synthesizer, each on its own sample, and starts again at the end when repeating, with
// the notes still sounding ringing on into the next time round.
internal sealed class MidiSequencer(MidiFile file, Synthesizer synthesizer, bool repeat)
{
    private int _next;
    private int _position;

    internal Synthesizer Synthesizer { get; } = synthesizer;

    // Every event has been sent and the file's length has passed; voices may still be ringing.
    internal bool EndOfSequence => !repeat && _next >= file.Events.Count && _position >= file.Length;

    internal void Render(Span<float> stereo)
    {
        IReadOnlyList<MidiEvent> events = file.Events;
        int frames = stereo.Length / 2;
        int frame = 0;

        while (frame < frames)
        {
            while (_next < events.Count && events[_next].SampleTime <= _position)
            {
                MidiEvent e = events[_next++];
                Synthesizer.ProcessMessage(e.Channel, e.Command, e.Data1, e.Data2);
            }

            if (repeat && _next >= events.Count && _position >= file.Length && file.Length > 0)
            {
                _next = 0;
                _position = 0;
                continue;
            }

            int until = _next < events.Count ? events[_next].SampleTime : repeat && file.Length > _position ? file.Length : int.MaxValue;
            int count = Math.Min(frames - frame, until - _position);

            Synthesizer.Render(stereo.Slice(frame * 2, count * 2));
            frame += count;
            _position += count;
        }
    }
}
