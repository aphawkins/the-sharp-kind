// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Globalization;
using System.Text;

namespace SharpKind.GoldenFrames;

// Line-based text, like the traces, so a change is reviewable in a pull request without special tooling.
public static class FrameFile
{
    private const int SchemaVersion = 1;

    private const string Banner = "# frame signatures v";

    public static string Extension => ".frames";

    public static string Write(string scenarioName, IReadOnlyList<FrameSignature> frames)
    {
        ArgumentNullException.ThrowIfNull(frames);

        StringBuilder text = new();
        Line(text, Banner + SchemaVersion.ToString(CultureInfo.InvariantCulture));
        Line(text, "# scenario " + scenarioName);

        foreach (FrameSignature frame in frames)
        {
            Line(text, string.Create(CultureInfo.InvariantCulture, $"F {frame.Tick} {frame.Hash}"));
            foreach (string row in frame.Thumbnail)
            {
                Line(text, "| " + row);
            }
        }

        return text.ToString();
    }

    public static IReadOnlyList<FrameSignature> Read(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        List<FrameSignature> frames = [];
        int tick = 0;
        string hash = string.Empty;
        List<string> thumbnail = [];

        foreach (string rawLine in text.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r');
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            if (line[0] == 'F')
            {
                Flush(frames, tick, hash, thumbnail);
                string[] f = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                tick = int.Parse(f[1], CultureInfo.InvariantCulture);
                hash = f[2];
                thumbnail = [];
            }
            else
            {
                // "| " prefix so leading dots aren't mistaken for blank and trimmed by an editor.
                thumbnail.Add(line[2..]);
            }
        }

        Flush(frames, tick, hash, thumbnail);
        return frames;
    }

    private static void Flush(List<FrameSignature> frames, int tick, string hash, List<string> thumbnail)
    {
        if (hash.Length != 0)
        {
            frames.Add(new(tick, hash, thumbnail));
        }
    }

    private static void Line(StringBuilder text, string content)
    {
        _ = text.Append(content);
        _ = text.Append('\n');
    }
}
