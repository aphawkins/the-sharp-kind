// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Audio.Wav;

// Plays a fully decoded track; at the end it starts again when repeating, else it goes silent.
internal sealed class SampleMusicStream(float[] samples, bool repeat) : IMusicSource
{
    private int _position;

    public void Render(Span<float> buffer)
    {
        int filled = 0;
        while (filled < buffer.Length)
        {
            if (_position >= samples.Length)
            {
                if (!repeat || samples.Length == 0)
                {
                    buffer[filled..].Clear();
                    return;
                }

                _position = 0;
            }

            int count = Math.Min(buffer.Length - filled, samples.Length - _position);
            samples.AsSpan(_position, count).CopyTo(buffer[filled..]);
            _position += count;
            filled += count;
        }
    }

    // A decoded track holds no unmanaged resources.
    public void Dispose()
    {
    }
}
