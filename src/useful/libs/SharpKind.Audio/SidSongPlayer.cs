// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Emulation.Sid;

namespace SharpKind.Audio;

// One song of a SID tune, played on a SidMachine shared with sound effects, as interleaved stereo float samples at 44.1 kHz.
// A tune has no marked end, so the song is taken to be over when it has gone quiet for a couple of seconds after sounding;
// then the machine stops the tune and goes back to effects.
internal sealed class SidSongPlayer
{
    private const int QuietFramesToEnd = 2 * 44100;
    private const float Quiet = 0.0005f;

    private readonly PsidFile _tune;
    private readonly int _song;
    private readonly bool _repeat;
    private readonly SidMachine _machine;

    private bool _sounded;
    private int _quietFrames;

    // The song is 1-based, or none for the tune's start song.
    internal SidSongPlayer(SidMachine machine, PsidFile tune, int? song, bool repeat)
    {
        _machine = machine;
        _tune = tune;
        _song = song ?? tune.StartSong;
        _repeat = repeat;

        _machine.StartMusic(_tune, _song);
    }

    // The song has ended and is not going to repeat.
    internal bool Finished { get; private set; }

    // The next samples, or silence once the song is over.
    internal void Render(Span<float> buffer)
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
            _machine.StopMusic();
        }
    }
}
