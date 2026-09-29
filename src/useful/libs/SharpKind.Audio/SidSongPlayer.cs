// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Emulation.Sid;

namespace SharpKind.Audio;

/// <summary>
/// One song of a SID tune, played by <see cref="SidMachine"/> as a stream of interleaved stereo float samples at 44.1 kHz.
/// A tune has no marked end, so the song is taken to be over when it has gone quiet for a couple of seconds after sounding.
/// </summary>
public sealed class SidSongPlayer
{
    private const int QuietFramesToEnd = 2 * 44100;
    private const float Quiet = 0.0005f;

    private readonly PsidFile _tune;
    private readonly int _song;
    private readonly bool _repeat;
    private readonly SidMachine _machine = new();

    private bool _sounded;
    private int _quietFrames;

    /// <summary>Initializes a new instance of the <see cref="SidSongPlayer"/> class.</summary>
    /// <param name="path">The SID file, with a <c>#n</c> for the song (see <see cref="MusicPath"/>).</param>
    /// <param name="repeat">Whether to start the song again when it ends.</param>
    public SidSongPlayer(string path, bool repeat)
    {
        ArgumentNullException.ThrowIfNull(path);

        (string file, int? song) = MusicPath.Parse(path);

        _tune = PsidFile.Read(File.ReadAllBytes(file));
        _song = song ?? _tune.StartSong;
        _repeat = repeat;

        _machine.StartMusic(_tune, _song);
    }

    /// <summary>Gets a value indicating whether the song has ended, and is not going to repeat.</summary>
    public bool Finished { get; private set; }

    /// <summary>Renders the next samples, or silence once the song is over.</summary>
    /// <param name="buffer">Interleaved left and right samples to fill.</param>
    public void Render(Span<float> buffer)
    {
        if (Finished)
        {
            buffer.Clear();
            return;
        }

        _machine.Render(buffer);
        Watch(buffer);
    }

    private void Watch(ReadOnlySpan<float> buffer)
    {
        for (int i = 0; i < buffer.Length; i += 2)
        {
            if (Math.Abs(buffer[i]) > Quiet)
            {
                _sounded = true;
                _quietFrames = 0;
            }
            else
            {
                _quietFrames++;
            }
        }

        if (!_sounded || _quietFrames < QuietFramesToEnd)
        {
            return;
        }

        if (_repeat)
        {
            _machine.StartMusic(_tune, _song);
            _sounded = false;
            _quietFrames = 0;
        }
        else
        {
            Finished = true;
        }
    }
}
