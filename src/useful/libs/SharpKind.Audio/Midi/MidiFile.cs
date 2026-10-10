// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Audio.Midi;

// A Standard MIDI File (format 0 or 1, ticks per quarter note), its tracks merged into one list of channel messages timed
// in output samples through the file's tempo map. System exclusive and every meta event but tempo are skipped.
internal sealed class MidiFile
{
    private const int DefaultTempo = 500_000; // microseconds per quarter note: 120 beats a minute

    private MidiFile(IReadOnlyList<MidiEvent> events, int length)
    {
        Events = events;
        Length = length;
    }

    private enum EventKind
    {
        Channel = 0,
        Tempo = 1,
        EndOfTrack = 2,
    }

    internal IReadOnlyList<MidiEvent> Events { get; }

    // In samples: the end of the last track.
    internal int Length { get; }

    internal static MidiFile Read(string path) => Parse(File.ReadAllBytes(path), path);

    internal static MidiFile Parse(byte[] data, string name)
    {
        Reader reader = new(data, name);

        if (reader.Ascii4() != "MThd")
        {
            throw new SharpKindException($"'{name}' is not a MIDI file.");
        }

        int headerEnd = reader.Int32() + reader.Position;
        int format = reader.Int16();
        _ = reader.Int16(); // the track count; the chunks themselves are read
        int division = reader.Int16();
        reader.Position = headerEnd;

        if (format > 1)
        {
            throw new SharpKindException($"'{name}' is a format {format} MIDI file; only formats 0 and 1 are supported.");
        }

        if ((division & 0x8000) != 0 || division == 0)
        {
            throw new SharpKindException($"'{name}' is timed in SMPTE frames; only ticks per quarter note are supported.");
        }

        List<RawEvent> raw = [];
        while (!reader.AtEnd)
        {
            string id = reader.Ascii4();
            int end = reader.Int32() + reader.Position;
            if (id == "MTrk")
            {
                ReadTrack(reader, end, raw);
            }

            reader.Position = end;
        }

        return Time(raw, division);
    }

    private static void ReadTrack(Reader reader, int end, List<RawEvent> raw)
    {
        long tick = 0;
        int runningStatus = 0;

        while (reader.Position < end)
        {
            tick += reader.VariableLength();
            int status = reader.Byte();

            if (status == 0xFF)
            {
                ReadMeta(reader, tick, raw);
            }
            else if (status is 0xF0 or 0xF7)
            {
                int length = reader.VariableLength();
                reader.Position += length;
            }
            else
            {
                runningStatus = ReadChannelMessage(reader, tick, status, runningStatus, raw);
            }
        }
    }

    private static void ReadMeta(Reader reader, long tick, List<RawEvent> raw)
    {
        int type = reader.Byte();
        int length = reader.VariableLength();
        int next = reader.Position + length;

        if (type == 0x51 && length == 3)
        {
            raw.Add(new(tick, EventKind.Tempo, (reader.Byte() << 16) | (reader.Byte() << 8) | reader.Byte(), 0, 0));
        }
        else if (type == 0x2F)
        {
            raw.Add(new(tick, EventKind.EndOfTrack, 0, 0, 0));
        }

        reader.Position = next;
    }

    // Returns the status to run on with.
    private static int ReadChannelMessage(Reader reader, long tick, int status, int runningStatus, List<RawEvent> raw)
    {
        if (status < 0x80)
        {
            // Running status: the byte read was the first data byte of a repeat of the last message.
            reader.Position--;
            status = runningStatus == 0
                ? throw new SharpKindException($"'{reader.Name}' has running status before any status byte.")
                : runningStatus;
        }

        int command = status & 0xF0;
        int data1 = reader.Byte();
        int data2 = command is 0xC0 or 0xD0 ? 0 : reader.Byte();
        raw.Add(new(tick, EventKind.Channel, status, data1, data2));

        return status;
    }

    // Tracks merge by tick, keeping track and file order for events on the same tick; the tempo map turns ticks into samples.
    private static MidiFile Time(List<RawEvent> raw, int division)
    {
        List<MidiEvent> events = [];
        double seconds = 0;
        long lastTick = 0;
        int tempo = DefaultTempo;
        int length = 0;

        foreach (RawEvent e in raw.OrderBy(e => e.Tick))
        {
            seconds += (e.Tick - lastTick) * tempo / (1_000_000.0 * division);
            lastTick = e.Tick;
            int sample = (int)Math.Round(seconds * SoftwareSound.SampleRate);
            length = Math.Max(length, sample);

            switch (e.Kind)
            {
                case EventKind.Tempo:
                    tempo = e.Status;
                    break;
                case EventKind.Channel:
                    events.Add(new(sample, e.Status & 0x0F, e.Status & 0xF0, e.Data1, e.Data2));
                    break;
            }
        }

        return new(events, length);
    }

    // For a tempo event, Status holds the tempo in microseconds per quarter note.
    private readonly record struct RawEvent(long Tick, EventKind Kind, int Status, int Data1, int Data2);

    // Reads big-endian values, and turns running off the end of the file into a SharpKindException.
    private sealed class Reader(byte[] data, string name)
    {
        internal string Name { get; } = name;

        internal int Position { get; set; }

        internal bool AtEnd => Position + 8 > data.Length;

        internal int Byte()
            => Position < data.Length ? data[Position++] : throw new SharpKindException($"'{Name}' is truncated.");

        internal int Int16() => (Byte() << 8) | Byte();

        internal int Int32() => (Int16() << 16) | Int16();

        internal string Ascii4() => new([(char)Byte(), (char)Byte(), (char)Byte(), (char)Byte()]);

        internal int VariableLength()
        {
            int value = 0;
            for (int i = 0; i < 4; i++)
            {
                int b = Byte();
                value = (value << 7) | (b & 0x7F);
                if ((b & 0x80) == 0)
                {
                    break;
                }
            }

            return value;
        }
    }
}
