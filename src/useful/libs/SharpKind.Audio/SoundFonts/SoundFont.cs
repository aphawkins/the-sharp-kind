// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpKind.Audio.SoundFonts;

// A SoundFont 2 file: its 16-bit sample data and the presets, instruments and samples that play it. Modulators, the
// INFO list and 24-bit sample data are not read.
internal sealed class SoundFont
{
    internal const int PercussionBank = 128;

    private const int PresetHeaderSize = 38;
    private const int BagSize = 4;
    private const int GeneratorSize = 4;
    private const int InstrumentHeaderSize = 22;
    private const int SampleHeaderSize = 46;
    private const int NameSize = 20;

    private SoundFont(
        short[] sampleData,
        IReadOnlyList<Preset> presets,
        IReadOnlyList<Instrument> instruments,
        IReadOnlyList<SoundFontSample> samples)
    {
        SampleData = sampleData;
        Presets = presets;
        Instruments = instruments;
        Samples = samples;
    }

    internal ReadOnlyMemory<short> SampleData { get; }

    internal IReadOnlyList<Preset> Presets { get; }

    internal IReadOnlyList<Instrument> Instruments { get; }

    internal IReadOnlyList<SoundFontSample> Samples { get; }

    internal static SoundFont Read(string path)
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes(path);

        if (file.Length < 12 || !file[..4].SequenceEqual("RIFF"u8) || !file.Slice(8, 4).SequenceEqual("sfbk"u8))
        {
            throw new SharpKindException($"'{path}' is not a SoundFont 2 file.");
        }

        ReadOnlySpan<byte> body = Chunk(file, 0, path)[4..];
        ReadOnlySpan<byte> sdta = List(body, "sdta"u8, path);
        ReadOnlySpan<byte> pdta = List(body, "pdta"u8, path);

        short[] sampleData = MemoryMarshal.Cast<byte, short>(SubChunk(sdta, "smpl"u8, path)).ToArray();

        ReadOnlySpan<byte> pgen = SubChunk(pdta, "pgen"u8, path);
        ReadOnlySpan<byte> igen = SubChunk(pdta, "igen"u8, path);

        List<Instrument> instruments = ReadInstruments(SubChunk(pdta, "inst"u8, path), SubChunk(pdta, "ibag"u8, path), igen);
        List<Preset> presets = ReadPresets(SubChunk(pdta, "phdr"u8, path), SubChunk(pdta, "pbag"u8, path), pgen);
        List<SoundFontSample> samples = ReadSamples(SubChunk(pdta, "shdr"u8, path));

        return new(sampleData, presets, instruments, samples);
    }

    // Every voice one note starts: one for each instrument zone, inside each preset zone, whose ranges hold the key and velocity.
    internal IReadOnlyList<VoiceParameters> FindVoices(int bank, int program, int key, int velocity)
    {
        Preset? preset = FindPreset(bank, program);
        if (preset is null)
        {
            return [];
        }

        List<VoiceParameters> voices = [];

        foreach (Zone presetZone in preset.Zones)
        {
            if (!presetZone.Contains(key, velocity))
            {
                continue;
            }

            Instrument instrument = Instruments[(ushort)presetZone[GeneratorType.Instrument]];

            foreach (Zone instrumentZone in instrument.Zones)
            {
                if (instrumentZone.Contains(key, velocity))
                {
                    voices.Add(Combine(presetZone, instrumentZone));
                }
            }
        }

        return voices;
    }

    private static ReadOnlySpan<byte> Chunk(ReadOnlySpan<byte> data, int offset, string path)
    {
        if (offset + 8 > data.Length)
        {
            throw new SharpKindException($"'{path}' is truncated.");
        }

        int size = (int)Math.Min(BinaryPrimitives.ReadUInt32LittleEndian(data[(offset + 4)..]), (uint)(data.Length - offset - 8));
        return data.Slice(offset + 8, size);
    }

    // The content of the first sub-chunk with this id; chunks are word-aligned.
    private static ReadOnlySpan<byte> SubChunk(ReadOnlySpan<byte> data, ReadOnlySpan<byte> id, string path)
    {
        int offset = 0;
        while (offset + 8 <= data.Length)
        {
            ReadOnlySpan<byte> content = Chunk(data, offset, path);
            if (data.Slice(offset, 4).SequenceEqual(id))
            {
                return content;
            }

            offset += 8 + content.Length + (content.Length % 2);
        }

        throw new SharpKindException($"'{path}' has no '{Encoding.ASCII.GetString(id)}' chunk.");
    }

    // The content, after its type, of the LIST chunk of this type.
    private static ReadOnlySpan<byte> List(ReadOnlySpan<byte> data, ReadOnlySpan<byte> type, string path)
    {
        int offset = 0;
        while (offset + 8 <= data.Length)
        {
            ReadOnlySpan<byte> content = Chunk(data, offset, path);
            if (data.Slice(offset, 4).SequenceEqual("LIST"u8) && content.Length >= 4 && content[..4].SequenceEqual(type))
            {
                return content[4..];
            }

            offset += 8 + content.Length + (content.Length % 2);
        }

        throw new SharpKindException($"'{path}' has no '{Encoding.ASCII.GetString(type)}' list.");
    }

    private static string Name(ReadOnlySpan<byte> record)
    {
        ReadOnlySpan<byte> name = record[..NameSize];
        int end = name.IndexOf((byte)0);
        return Encoding.Latin1.GetString(end < 0 ? name : name[..end]);
    }

    private static ushort Word(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]);

    // The last record of each header table is a terminator that only closes the record before it.
    private static List<Instrument> ReadInstruments(ReadOnlySpan<byte> inst, ReadOnlySpan<byte> ibag, ReadOnlySpan<byte> igen)
    {
        List<Instrument> instruments = [];
        int count = (inst.Length / InstrumentHeaderSize) - 1;

        for (int i = 0; i < count; i++)
        {
            ReadOnlySpan<byte> record = inst.Slice(i * InstrumentHeaderSize, InstrumentHeaderSize);
            int bagStart = Word(record, NameSize);
            int bagEnd = Word(inst, ((i + 1) * InstrumentHeaderSize) + NameSize);

            List<Zone> zones = ReadZones(bagStart, bagEnd, ibag, igen, Zone.InstrumentDefaults(), GeneratorType.SampleId);
            instruments.Add(new(Name(record), zones));
        }

        return instruments;
    }

    private static List<Preset> ReadPresets(ReadOnlySpan<byte> phdr, ReadOnlySpan<byte> pbag, ReadOnlySpan<byte> pgen)
    {
        List<Preset> presets = [];
        int count = (phdr.Length / PresetHeaderSize) - 1;

        for (int i = 0; i < count; i++)
        {
            ReadOnlySpan<byte> record = phdr.Slice(i * PresetHeaderSize, PresetHeaderSize);
            int bagStart = Word(record, NameSize + 4);
            int bagEnd = Word(phdr, ((i + 1) * PresetHeaderSize) + NameSize + 4);

            List<Zone> zones = ReadZones(bagStart, bagEnd, pbag, pgen, Zone.PresetDefaults(), GeneratorType.Instrument);
            presets.Add(new(Name(record), bank: Word(record, NameSize + 2), patch: Word(record, NameSize), zones));
        }

        return presets;
    }

    // A first zone without the zone's defining generator (sample or instrument) is the global zone, which every other zone starts from.
    private static List<Zone> ReadZones(
        int bagStart,
        int bagEnd,
        ReadOnlySpan<byte> bags,
        ReadOnlySpan<byte> generators,
        Zone defaults,
        GeneratorType defining)
    {
        List<Zone> raw = [];
        for (int bag = bagStart; bag < bagEnd; bag++)
        {
            int generatorStart = Word(bags, bag * BagSize);
            int generatorEnd = Word(bags, (bag + 1) * BagSize);

            Zone zone = new();
            for (int g = generatorStart; g < generatorEnd; g++)
            {
                ReadOnlySpan<byte> record = generators.Slice(g * GeneratorSize, GeneratorSize);
                zone.Set(Word(record, 0), BinaryPrimitives.ReadInt16LittleEndian(record[2..]));
            }

            raw.Add(zone);
        }

        Zone start = defaults;
        if (raw.Count > 0 && !raw[0].IsSet(defining))
        {
            start = defaults.With(raw[0]);
            raw.RemoveAt(0);
        }

        return [.. raw.Where(z => z.IsSet(defining)).Select(start.With)];
    }

    private static List<SoundFontSample> ReadSamples(ReadOnlySpan<byte> shdr)
    {
        List<SoundFontSample> samples = [];
        int count = (shdr.Length / SampleHeaderSize) - 1;

        for (int i = 0; i < count; i++)
        {
            ReadOnlySpan<byte> record = shdr.Slice((i * SampleHeaderSize) + NameSize, SampleHeaderSize - NameSize);
            samples.Add(new(
                Start: BinaryPrimitives.ReadInt32LittleEndian(record),
                End: BinaryPrimitives.ReadInt32LittleEndian(record[4..]),
                LoopStart: BinaryPrimitives.ReadInt32LittleEndian(record[8..]),
                LoopEnd: BinaryPrimitives.ReadInt32LittleEndian(record[12..]),
                SampleRate: BinaryPrimitives.ReadInt32LittleEndian(record[16..]),
                OriginalPitch: record[20],
                PitchCorrection: (sbyte)record[21]));
        }

        return samples;
    }

    // The requested preset; else, for percussion, the bank's first kit, and otherwise the same program in bank 0 and then
    // the bank's first program.
    private Preset? FindPreset(int bank, int program)
    {
        Preset? Find(int b, int p) => Presets.FirstOrDefault(x => x.Bank == b && x.Patch == p);

        return Find(bank, program)
            ?? (bank == PercussionBank ? null : Find(0, program))
            ?? Find(bank, 0);
    }

    // The instrument zone's values are absolute; the preset zone's are added to them, except for the generators only an
    // instrument may hold.
    private VoiceParameters Combine(Zone preset, Zone instrument)
    {
        int Sum(GeneratorType type) => instrument[type] + preset[type];

        SoundFontSample sample = Samples[(ushort)instrument[GeneratorType.SampleId]];
        int limit = SampleData.Length;

        // A preset may not move sample addresses (SoundFont 2.01, 8.1.3), so only the instrument's offsets count.
        int Address(int baseAddress, GeneratorType fine, GeneratorType coarse)
            => Math.Clamp(baseAddress + instrument[fine] + (instrument[coarse] * 32768), 0, limit);

        int overridingRootKey = instrument[GeneratorType.OverridingRootKey];

        return new(
            SampleStart: Address(sample.Start, GeneratorType.StartAddrsOffset, GeneratorType.StartAddrsCoarseOffset),
            SampleEnd: Address(sample.End, GeneratorType.EndAddrsOffset, GeneratorType.EndAddrsCoarseOffset),
            LoopStart: Address(sample.LoopStart, GeneratorType.StartloopAddrsOffset, GeneratorType.StartloopAddrsCoarseOffset),
            LoopEnd: Address(sample.LoopEnd, GeneratorType.EndloopAddrsOffset, GeneratorType.EndloopAddrsCoarseOffset),
            SampleRate: sample.SampleRate,
            RootKey: overridingRootKey >= 0 ? overridingRootKey : sample.OriginalPitch,
            PitchCorrection: sample.PitchCorrection,
            CoarseTune: Sum(GeneratorType.CoarseTune),
            FineTune: Sum(GeneratorType.FineTune),
            ScaleTuning: Sum(GeneratorType.ScaleTuning),
            Pan: Sum(GeneratorType.Pan),
            Attenuation: Sum(GeneratorType.InitialAttenuation),
            DelayVolEnv: Sum(GeneratorType.DelayVolEnv),
            AttackVolEnv: Sum(GeneratorType.AttackVolEnv),
            HoldVolEnv: Sum(GeneratorType.HoldVolEnv),
            DecayVolEnv: Sum(GeneratorType.DecayVolEnv),
            SustainVolEnv: Sum(GeneratorType.SustainVolEnv),
            ReleaseVolEnv: Sum(GeneratorType.ReleaseVolEnv),
            SampleModes: instrument[GeneratorType.SampleModes] & 3,
            ExclusiveClass: instrument[GeneratorType.ExclusiveClass]);
    }
}
