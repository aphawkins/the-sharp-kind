// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// Simulates the Enigma encoding machine.
/// </summary>
public sealed class EnigmaSymmetric : SymmetricAlgorithm
{
    /// <summary>
    /// The separator between values in a key field.
    /// </summary>
    internal const char KeyDelimiter = ' ';

    /// <summary>
    /// The number of fields in the key.
    /// </summary>
    private const int KeyParts = 4;

    /// <summary>
    /// The separator between key fields.
    /// </summary>
    private const char KeySeparator = '|';

    private Enigma _algorithm;

    /// <summary>
    /// The rotor settings that a message starts from. <see cref="IV"/> follows the rotors as they step, so it is not
    /// the start once a message has been processed.
    /// </summary>
    private byte[] _startIv = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="EnigmaSymmetric"/> class.
    /// </summary>
    public EnigmaSymmetric()
    {
        _algorithm = new Enigma(new EnigmaSettings());
        Reset();
        RememberStart();
    }

    /// <inheritdoc />
    public override byte[] Key
    {
        // Example:
        // "reflector|rotors|ring|plugboard"
        // "B|III II I|03 02 01|DN GR IS KC QX TM PV HY FW BJ"
        get
        {
            StringBuilder key = new();

            // Reflector
            key.Append(_algorithm.Settings.Reflector.ReflectorNumber.ToString())
                .Append(KeySeparator)
                .Append(RotorOrderString(_algorithm.Settings.Rotors))
                .Append(KeySeparator)
                .Append(RotorRingString(_algorithm.Settings.Rotors))
                .Append(KeySeparator)
                .Append(PlugboardString(_algorithm.Settings.Plugboard));

            return Encoding.Unicode.GetBytes(key.ToString());
        }

        set
        {
            try
            {
                _algorithm = new Enigma(GetSettingsKey(value));
                RememberStart();
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Error parsing Key.", nameof(Key), ex);
            }

            base.Key = value;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Reading this gives the rotor settings as they are now, which change as text is processed.
    /// </remarks>
    public override byte[] IV
    {
        // Example: G M Y
        get => Encoding.Unicode.GetBytes(RotorSettingString(_algorithm.Settings.Rotors));

        set
        {
            try
            {
                _algorithm = new Enigma(GetSettingsIv(_algorithm.Settings, value));
                RememberStart();
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Error parsing IV.", nameof(IV), ex);
            }

            base.IV = value;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Starts from the rotor settings the message started from, not the settings the last message left behind.
    /// </remarks>
    public override ICryptoTransform CreateDecryptor() => CreateDecryptor(Key, _startIv);

    /// <inheritdoc />
    public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV)
    {
        Key = rgbKey;
        IV = rgbIV ?? [];
        return new ClassicalSymmetricTransform(_algorithm, CipherTransformMode.Decrypt);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Starts from the rotor settings the message started from, not the settings the last message left behind.
    /// </remarks>
#pragma warning disable CA5401 // The IV is the rotor start, which the caller chooses.
    public override ICryptoTransform CreateEncryptor() => CreateEncryptor(Key, _startIv);
#pragma warning restore CA5401

    /// <inheritdoc />
    public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV)
    {
        Key = rgbKey;
        IV = rgbIV ?? [];
        return new ClassicalSymmetricTransform(_algorithm, CipherTransformMode.Encrypt);
    }

    /// <inheritdoc />
    public override void GenerateIV()
    {
        _algorithm.Settings.Rotors[EnigmaRotorPosition.Fastest].CurrentSetting = GetRandomRotorCurrentSetting();
        _algorithm.Settings.Rotors[EnigmaRotorPosition.Second].CurrentSetting = GetRandomRotorCurrentSetting();
        _algorithm.Settings.Rotors[EnigmaRotorPosition.Third].CurrentSetting = GetRandomRotorCurrentSetting();
        RememberStart();
        IVValue = IV;
    }

    /// <inheritdoc />
    public override void GenerateKey()
    {
        _algorithm.GenerateSettings();
        RememberStart();
        KeyValue = Key;
    }

    /// <inheritdoc/>
    public override string ToString() => _algorithm.CipherName;

    private static EnigmaSettings GetSettingsKey(byte[] key)
    {
        string keyString = Encoding.Unicode.GetString(key);
        string[] parts = keyString.Split([KeySeparator], StringSplitOptions.None);

        if (parts.Length != KeyParts)
        {
            throw new ArgumentException("Incorrect number of key parts.", nameof(key));
        }

        IEnigmaReflector reflector = ParseEnigmaReflectorNumber(parts[0]);
        Dictionary<EnigmaRotorPosition, EnigmaRotorNumber> rotorNumbers = ParseEnigmaRotorNumbers(parts[1]);
        Dictionary<EnigmaRotorPosition, int> rings = ParseEnigmaRings(parts[2]);

        IReadOnlyDictionary<EnigmaRotorPosition, IEnigmaRotor> list = new Dictionary<EnigmaRotorPosition, IEnigmaRotor>
        {
            {
                EnigmaRotorPosition.Fastest,
                new EnigmaRotor()
                    { RotorNumber = rotorNumbers[EnigmaRotorPosition.Fastest], RingPosition = rings[EnigmaRotorPosition.Fastest] }
            },
            {
                EnigmaRotorPosition.Second,
                new EnigmaRotor()
                    { RotorNumber = rotorNumbers[EnigmaRotorPosition.Second], RingPosition = rings[EnigmaRotorPosition.Second] }
            },
            {
                EnigmaRotorPosition.Third,
                new EnigmaRotor()
                    { RotorNumber = rotorNumbers[EnigmaRotorPosition.Third], RingPosition = rings[EnigmaRotorPosition.Third] }
            },
        };

        EnigmaRotors rotors = new(list);

        EnigmaPlugboard plugboard = ParsePlugboard(parts[3]);

        return new EnigmaSettings() { Reflector = reflector, Rotors = rotors, Plugboard = plugboard };
    }

    private static IEnigmaSettings GetSettingsIv(IEnigmaSettings settings, byte[] iv)
    {
        string initVector = iv != null ? Encoding.Unicode.GetString(iv) : string.Empty;

        Dictionary<EnigmaRotorPosition, char> rotorSettings = ParseEnigmaRotorSettings(initVector);

        settings.Rotors[EnigmaRotorPosition.Fastest].CurrentSetting = rotorSettings[EnigmaRotorPosition.Fastest];
        settings.Rotors[EnigmaRotorPosition.Second].CurrentSetting = rotorSettings[EnigmaRotorPosition.Second];
        settings.Rotors[EnigmaRotorPosition.Third].CurrentSetting = rotorSettings[EnigmaRotorPosition.Third];

        return settings;
    }

    private static EnigmaReflector ParseEnigmaReflectorNumber(string reflector) => reflector.Length > 1 ||
        !char.IsLetter(reflector[0]) ||
        !Enum.TryParse(reflector, out EnigmaReflectorNumber reflectorNumber)
            ? throw new ArgumentException("Incorrect reflector.", nameof(reflector))
            : new EnigmaReflector() { ReflectorNumber = reflectorNumber };

    private static Dictionary<EnigmaRotorPosition, EnigmaRotorNumber> ParseEnigmaRotorNumbers(string rotorNumbers)
    {
        const int rotorPositionsCount = 3;
        string[] rotors = rotorNumbers.Split(' ');
        Dictionary<EnigmaRotorPosition, EnigmaRotorNumber> newRotors = [];

        if (rotors.Length == 0)
        {
            throw new ArgumentException("No rotors specified.", nameof(rotorNumbers));
        }

        if (rotors.Length > rotorPositionsCount)
        {
            throw new ArgumentException("Too many rotors specified.", nameof(rotorNumbers));
        }

        if (rotors.Length < rotorPositionsCount)
        {
            throw new ArgumentException("Too few rotors specified.", nameof(rotorNumbers));
        }

        for (int i = 0; i < rotors.Length; i++)
        {
            string rotor = rotors.Reverse().ToList()[i];
            if (string.IsNullOrEmpty(rotor))
            {
                throw new ArgumentException("Null or empty rotor specified.", nameof(rotorNumbers));
            }

            if (!Enum.TryParse(rotor, out EnigmaRotorNumber rotorNumber)
                || rotorNumber.ToString() != rotor)
            {
                throw new ArgumentException($"Invalid rotor number {rotor}.", nameof(rotorNumbers));
            }

            newRotors.Add((EnigmaRotorPosition)i, rotorNumber);
        }

        return newRotors;
    }

    private static Dictionary<EnigmaRotorPosition, int> ParseEnigmaRings(string ringSettings)
    {
        const int rotorPositionsCount = 3;
        string[] rings = ringSettings.Split(' ');

        if (rings.Length == 0)
        {
            throw new ArgumentException("No rings specified.", nameof(ringSettings));
        }

        if (rings.Length > rotorPositionsCount)
        {
            throw new ArgumentException("Too many rings specified.", nameof(ringSettings));
        }

        if (rings.Length < rotorPositionsCount)
        {
            throw new ArgumentException("Too few rings specified.", nameof(ringSettings));
        }

        if (rings[0].Length == 0)
        {
            throw new ArgumentException("No rings specified.", nameof(ringSettings));
        }

        for (int i = 0; i < rings.Length; i++)
        {
            if (rings[i].Length != 2)
            {
                throw new ArgumentException("Ring number format incorrect.", nameof(ringSettings));
            }

            if (!int.TryParse(rings[i], out _))
            {
                throw new ArgumentException("Ring number is not a number.", nameof(ringSettings));
            }
        }

        return new Dictionary<EnigmaRotorPosition, int>
        {
            { EnigmaRotorPosition.Fastest, int.Parse(rings[2], CultureInfo.InvariantCulture) },
            { EnigmaRotorPosition.Second, int.Parse(rings[1], CultureInfo.InvariantCulture) },
            { EnigmaRotorPosition.Third, int.Parse(rings[0], CultureInfo.InvariantCulture) },
        };
    }

    private static Dictionary<EnigmaRotorPosition, char> ParseEnigmaRotorSettings(string rotorSettings)
    {
        const int rotorPositionsCount = 3;
        string[] rotorSetting = rotorSettings.Split(' ');

        return rotorSetting.Length == 0
            ? throw new ArgumentException("No rotor settings specified.", nameof(rotorSettings))
            : rotorSetting.Length > rotorPositionsCount
            ? throw new ArgumentException("Too many rotor settings specified.", nameof(rotorSettings))
            : rotorSetting.Length < rotorPositionsCount
            ? throw new ArgumentException("Too few rotor settings specified.", nameof(rotorSettings))
            : rotorSetting[0].Length == 0
            ? throw new ArgumentException("No rotor settings specified.", nameof(rotorSettings))
            : new Dictionary<EnigmaRotorPosition, char>
        {
            { EnigmaRotorPosition.Fastest, rotorSetting[2][0] },
            { EnigmaRotorPosition.Second, rotorSetting[1][0] },
            { EnigmaRotorPosition.Third, rotorSetting[0][0] },
        };
    }

    private static EnigmaPlugboard ParsePlugboard(string plugboard)
    {
        List<EnigmaPlugboardPair> pairs = [];
        string[] rawPairs = plugboard.Split(KeyDelimiter);

        // No plugs specified
        if (rawPairs.Length == 1 && rawPairs[0].Length == 0)
        {
            return new EnigmaPlugboard(pairs);
        }

        // Check for plugs made up of pairs
        foreach (string rawPair in rawPairs)
        {
            if (rawPair.Length != 2)
            {
                throw new ArgumentException("Setting must be a pair.", nameof(plugboard));
            }

            if (pairs.Any(pair => pair.From == rawPair[0]))
            {
                throw new ArgumentException("Setting already set.", nameof(plugboard));
            }

            pairs.Add(new() { From = rawPair[0], To = rawPair[1] });
        }

        return new EnigmaPlugboard(pairs);
    }

    private static string RotorSettingString(IEnigmaRotors settings)
    {
        StringBuilder key = new();

        foreach (KeyValuePair<EnigmaRotorPosition, IEnigmaRotor> position in settings.Rotors.Reverse().ToArray())
        {
            key.Append(position.Value.CurrentSetting)
                .Append(KeyDelimiter);
        }

        if (settings.Rotors.Count > 0)
        {
            key.Remove(key.Length - 1, 1);
        }

        return key.ToString();
    }

    private static string RotorOrderString(IEnigmaRotors settings)
    {
        StringBuilder key = new();

        foreach (KeyValuePair<EnigmaRotorPosition, IEnigmaRotor> position in settings.Rotors.Reverse().ToArray())
        {
            key.Append(position.Value.RotorNumber.ToString())
                .Append(KeyDelimiter);
        }

        if (settings.Rotors.Count > 0)
        {
            key.Remove(key.Length - 1, 1);
        }

        return key.ToString();
    }

    private static string RotorRingString(IEnigmaRotors settings)
    {
        StringBuilder key = new();

        foreach (KeyValuePair<EnigmaRotorPosition, IEnigmaRotor> position in settings.Rotors.Reverse().ToArray())
        {
            key.Append(CultureInfo.InvariantCulture, $"{position.Value.RingPosition:00}")
                .Append(KeyDelimiter);
        }

        if (settings.Rotors.Count > 0)
        {
            key.Remove(key.Length - 1, 1);
        }

        return key.ToString();
    }

    private static string PlugboardString(IEnigmaPlugboard plugboard)
    {
        StringBuilder key = new();
        IReadOnlyDictionary<char, char> substitutions = plugboard.Substitutions();

        foreach (KeyValuePair<char, char> pair in substitutions)
        {
            key.Append(pair.Key)
                .Append(pair.Value)
                .Append(KeyDelimiter);
        }

        if (substitutions.Count > 0
            && key.Length > 0)
        {
            key.Remove(key.Length - 1, 1);
        }

        return key.ToString();
    }

    private static char GetRandomRotorCurrentSetting() => "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[RandomNumberGenerator.GetInt32(0, 26)];

    private void RememberStart() => _startIv = Encoding.Unicode.GetBytes(RotorSettingString(_algorithm.Settings.Rotors));

    private void Reset()
    {
#pragma warning disable CA5358 // Do Not Use Unsafe Cipher Modes - this cipher is inherently unsafe
        ModeValue = CipherMode.ECB;
#pragma warning restore CA5358
        PaddingValue = PaddingMode.None;
        KeySizeValue = 16;
        BlockSizeValue = 16 * 5;
        FeedbackSizeValue = 16;
        LegalBlockSizesValue = new KeySizes[1];
        LegalBlockSizesValue[0] = new KeySizes(0, int.MaxValue, 16);
        LegalKeySizesValue = new KeySizes[1];
        LegalKeySizesValue[0] = new KeySizes(0, int.MaxValue, 16);
        KeyValue = [];
        IVValue = [];
    }
}
