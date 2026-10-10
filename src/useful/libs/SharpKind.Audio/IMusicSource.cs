// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Audio;

/// <summary>A music track streamed (not predecoded) as interleaved stereo float samples at 44.1 kHz.</summary>
public interface IMusicSource : IDisposable
{
    /// <summary>Renders the next samples, or silence once a track that does not repeat has ended.</summary>
    /// <param name="buffer">Interleaved left and right samples to fill.</param>
    public void Render(Span<float> buffer);
}
