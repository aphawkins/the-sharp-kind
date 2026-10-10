// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Audio.SoundFonts;

// The generator values of one preset or instrument zone, and which of them the file set.
internal sealed class Zone
{
    internal const int GeneratorCount = 64;

    // Key and velocity ranges are a low byte and a high byte; 0-127 is "every key".
    private const short FullRange = 127 << 8;

    private readonly short[] _values = new short[GeneratorCount];
    private readonly bool[] _set = new bool[GeneratorCount];

    internal int KeyLow => _values[(int)GeneratorType.KeyRange] & 0xFF;

    internal int KeyHigh => (_values[(int)GeneratorType.KeyRange] >> 8) & 0xFF;

    internal int VelocityLow => _values[(int)GeneratorType.VelRange] & 0xFF;

    internal int VelocityHigh => (_values[(int)GeneratorType.VelRange] >> 8) & 0xFF;

    internal short this[GeneratorType type] => _values[(int)type];

    // An instrument zone starts from the SoundFont defaults: its values are absolute.
    internal static Zone InstrumentDefaults()
    {
        Zone zone = PresetDefaults();
        zone._values[(int)GeneratorType.DelayVolEnv] = -12000;
        zone._values[(int)GeneratorType.AttackVolEnv] = -12000;
        zone._values[(int)GeneratorType.HoldVolEnv] = -12000;
        zone._values[(int)GeneratorType.DecayVolEnv] = -12000;
        zone._values[(int)GeneratorType.ReleaseVolEnv] = -12000;
        zone._values[(int)GeneratorType.ScaleTuning] = 100;
        zone._values[(int)GeneratorType.OverridingRootKey] = -1;
        zone._values[(int)GeneratorType.Keynum] = -1;
        zone._values[(int)GeneratorType.Velocity] = -1;
        return zone;
    }

    // A preset zone starts from zero: its values are added to the instrument's.
    internal static Zone PresetDefaults()
    {
        Zone zone = new();
        zone._values[(int)GeneratorType.KeyRange] = FullRange;
        zone._values[(int)GeneratorType.VelRange] = FullRange;
        return zone;
    }

    internal bool IsSet(GeneratorType type) => _set[(int)type];

    internal bool Contains(int key, int velocity)
        => key >= KeyLow && key <= KeyHigh && velocity >= VelocityLow && velocity <= VelocityHigh;

    internal void Set(int type, short value)
    {
        if (type is >= 0 and < GeneratorCount)
        {
            _values[type] = value;
            _set[type] = true;
        }
    }

    // A copy of this zone with every value another zone set laid over it, as a zone inherits from its global zone.
    internal Zone With(Zone overrides)
    {
        Zone zone = new();
        _values.CopyTo(zone._values, 0);
        _set.CopyTo(zone._set, 0);

        for (int i = 0; i < GeneratorCount; i++)
        {
            if (overrides._set[i])
            {
                zone._values[i] = overrides._values[i];
                zone._set[i] = true;
            }
        }

        return zone;
    }
}
