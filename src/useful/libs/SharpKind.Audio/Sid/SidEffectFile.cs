// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpKind.Audio.Sid;

// A .sidfx file: rows of C64 Elite's sound-effect tables (SFXPR, SFXCNT, SFXFQ, SFXCR, SFXATK, SFXSUS, SFXFRCH, SFXVCH), and
// the sounds a game asks for by name, each a list of steps that start effects. Every step is resolved to its effect when the
// file is read.
internal sealed class SidEffectFile
{
    // An effect's number + 1 is kept in the low six bits of a voice's flag.
    private const int MaxEffects = 63;

    // Disallow unmapped members so a misspelled field fails at load rather than as a wrong sound later.
    private static readonly JsonSerializerOptions s_options = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    private SidEffectFile(IReadOnlyDictionary<string, IReadOnlyList<SidEffectStep>> sounds) => Sounds = sounds;

    internal IReadOnlyDictionary<string, IReadOnlyList<SidEffectStep>> Sounds { get; }

    internal static SidEffectFile Read(string path) => Parse(File.ReadAllText(path), path);

    internal static SidEffectFile Parse(string json, string name)
    {
        FileJson file;
        try
        {
            file = JsonSerializer.Deserialize<FileJson>(json, s_options)
                ?? throw new SharpKindException($"'{name}' is empty.");
        }
        catch (JsonException ex)
        {
            throw new SharpKindException($"'{name}' is not a valid .sidfx file: {ex.Message}", ex);
        }

        if (file.Effects.Count > MaxEffects)
        {
            throw new SharpKindException($"'{name}' has {file.Effects.Count} effects; at most {MaxEffects} are allowed.");
        }

        Dictionary<string, SidEffect> effects = [];
        foreach ((string effectName, EffectJson row) in file.Effects)
        {
            effects[effectName] = row.ToEffect(effects.Count, $"'{name}' effect '{effectName}'");
        }

        Dictionary<string, IReadOnlyList<SidEffectStep>> sounds = [];
        foreach ((string soundName, List<StepJson> steps) in file.Sounds)
        {
            sounds[soundName] = [.. steps.Select(step => step.ToStep(effects, $"'{name}' sound '{soundName}'"))];
        }

        return new(sounds);
    }

    private static byte Byte(int value, string field, string where)
        => value is >= 0 and <= 255 ? (byte)value : throw new SharpKindException($"{where}: {field} {value} is not 0-255.");

    internal sealed class FileJson
    {
        public required Dictionary<string, EffectJson> Effects { get; init; }

        public required Dictionary<string, List<StepJson>> Sounds { get; init; }
    }

    internal sealed class EffectJson
    {
        public required int Priority { get; init; }

        public required int Frames { get; init; }

        public required int Frequency { get; init; }

        public required int Control { get; init; }

        public required int AttackDecay { get; init; }

        public required int SustainRelease { get; init; }

        public required int FrequencyChange { get; init; }

        public required int VolumeMask { get; init; }

        public SidEffect ToEffect(int index, string where)
        {
            sbyte frequencyChange = FrequencyChange is >= sbyte.MinValue and <= sbyte.MaxValue
                ? (sbyte)FrequencyChange
                : throw new SharpKindException($"{where}: FrequencyChange {FrequencyChange} is not -128-127.");

            return new(
                index,
                Byte(Priority, nameof(Priority), where),
                Byte(Frames, nameof(Frames), where),
                Byte(Frequency, nameof(Frequency), where),
                Byte(Control, nameof(Control), where),
                Byte(AttackDecay, nameof(AttackDecay), where),
                Byte(SustainRelease, nameof(SustainRelease), where),
                frequencyChange,
                Byte(VolumeMask, nameof(VolumeMask), where));
        }
    }

    internal sealed class StepJson
    {
        public required string Effect { get; init; }

        public int? Frequency { get; init; }

        public int? SustainRelease { get; init; }

        public bool Layer { get; init; }

        public int DelayFrames { get; init; }

        public SidEffectStep ToStep(Dictionary<string, SidEffect> effects, string where)
        {
            if (!effects.TryGetValue(Effect, out SidEffect effect))
            {
                throw new SharpKindException($"{where}: there is no effect '{Effect}'.");
            }

            // NOISE2 takes a frequency and a sustain/release together.
            return Frequency.HasValue != SustainRelease.HasValue
                ? throw new SharpKindException($"{where}: give both Frequency and SustainRelease, or neither.")
                : new(
                    effect,
                    Frequency is int frequency ? Byte(frequency, nameof(Frequency), where) : null,
                    SustainRelease is int sustainRelease ? Byte(sustainRelease, nameof(SustainRelease), where) : null,
                    Layer,
                    Byte(DelayFrames, nameof(DelayFrames), where));
        }
    }
}
