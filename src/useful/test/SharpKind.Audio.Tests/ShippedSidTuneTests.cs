// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Emulation.Sid;

namespace SharpKind.Audio.Tests;

// For listening, not for the build: renders the start of every song of the shipped .sid files to WAV files in the temp
// folder, and reports how long each plays before going quiet (up to five minutes).
public sealed class ShippedSidTuneTests(ITestOutputHelper output)
{
    private const int SampleRate = 44100;
    private const int WavSeconds = 30;
    private const int MaxSeconds = 300;

    [Theory(Explicit = true)]
    [InlineData("src/elite/libs/EliteSharp.Renditions.EightBit/Assets/Music/elite.sid")]
    [InlineData("src/bb/libs/BubbleBobbleSharp.Renditions.EightBit/Assets/Music/bubble-bobble.sid")]
    public void RenderEverySong(string tunePath)
    {
        PsidFile tune = PsidFile.Read(File.ReadAllBytes(Path.Combine(RepoRoot(), tunePath)));
        output.WriteLine($"{tune.Name} ({tune.Songs} songs, start song {tune.StartSong})");

        for (int song = 1; song <= tune.Songs; song++)
        {
            SidSongPlayer player = new(new SidMachine(), tune, song, repeat: false);
            float[] block = new float[2 * SampleRate / 10];
            List<float> start = [];
            int blocks = 0;

            while (!player.Finished && blocks < MaxSeconds * 10)
            {
                player.Render(block);
                if (start.Count < 2 * SampleRate * WavSeconds)
                {
                    start.AddRange(block);
                }

                blocks++;
            }

            string wav = Path.Combine(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(tunePath)}-{song}.wav");
            WriteWav(wav, start);

            // A song is over after two seconds of quiet, so those are not part of it.
            string length = player.Finished ? $"{(blocks / 10.0) - 2:F1} s" : $"over {MaxSeconds} s";
            output.WriteLine($"  song {song}: {length}, {wav}");
        }
    }

    private static string RepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "TheSharpKind.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("The repository root was not found.");
    }

    private static void WriteWav(string path, List<float> samples)
    {
        using BinaryWriter writer = new(File.Create(path));
        int length = samples.Count * sizeof(short);

        writer.Write("RIFF"u8);
        writer.Write(36 + length);
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write(16);
        writer.Write((short)1); // PCM
        writer.Write((short)2);
        writer.Write(SampleRate);
        writer.Write(SampleRate * 2 * sizeof(short));
        writer.Write((short)(2 * sizeof(short)));
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(length);

        foreach (float sample in samples)
        {
            writer.Write((short)(Math.Clamp(sample, -1f, 1f) * short.MaxValue));
        }
    }
}
