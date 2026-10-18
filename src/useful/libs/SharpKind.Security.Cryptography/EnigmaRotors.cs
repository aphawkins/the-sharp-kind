// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography;

/// <summary>
/// Enigma rotor settings.
/// </summary>
public sealed class EnigmaRotors : IEnigmaRotors
{
    private IReadOnlyDictionary<EnigmaRotorPosition, IEnigmaRotor> _rotors = new Dictionary<EnigmaRotorPosition, IEnigmaRotor>();

    /// <summary>
    /// Initializes a new instance of the <see cref="EnigmaRotors"/> class.
    /// </summary>
    public EnigmaRotors() => _rotors = new Dictionary<EnigmaRotorPosition, IEnigmaRotor>
        {
            { EnigmaRotorPosition.Fastest, new EnigmaRotor() { RotorNumber = EnigmaRotorNumber.I } },
            { EnigmaRotorPosition.Second, new EnigmaRotor() { RotorNumber = EnigmaRotorNumber.II } },
            { EnigmaRotorPosition.Third, new EnigmaRotor() { RotorNumber = EnigmaRotorNumber.III } },
        };

    /// <summary>
    /// Initializes a new instance of the <see cref="EnigmaRotors"/> class.
    /// </summary>
    /// <param name="rotors">The rotors.</param>
    public EnigmaRotors(IReadOnlyDictionary<EnigmaRotorPosition, IEnigmaRotor> rotors) => Rotors = rotors;

    /// <summary>
    /// Gets the rotor positions.
    /// </summary>
    /// <returns>The rotor positions.</returns>
    public static IEnumerable<EnigmaRotorPosition> RotorPositions
        => [
            EnigmaRotorPosition.Fastest,
            EnigmaRotorPosition.Second,
            EnigmaRotorPosition.Third,
        ];

    /// <summary>
    /// Gets all the rotors.
    /// </summary>
    /// <returns>All the rotors.</returns>
    public static IList<EnigmaRotorNumber> RotorSet
        => [
            EnigmaRotorNumber.I,
            EnigmaRotorNumber.II,
            EnigmaRotorNumber.III,
            EnigmaRotorNumber.IV,
            EnigmaRotorNumber.V,
            EnigmaRotorNumber.VI,
            EnigmaRotorNumber.VII,
            EnigmaRotorNumber.VIII,
        ];

    /// <inheritdoc />
    public IReadOnlyDictionary<EnigmaRotorPosition, IEnigmaRotor> Rotors
    {
        get => _rotors;

        init
        {
            ArgumentNullException.ThrowIfNull(value);

            if (value[EnigmaRotorPosition.Fastest].RotorNumber == value[EnigmaRotorPosition.Second].RotorNumber
                || value[EnigmaRotorPosition.Fastest].RotorNumber == value[EnigmaRotorPosition.Third].RotorNumber
                || value[EnigmaRotorPosition.Second].RotorNumber == value[EnigmaRotorPosition.Third].RotorNumber)
            {
                throw new ArgumentException("This rotor is already in use.", nameof(value));
            }

            _rotors = value;
        }
    }

    /// <inheritdoc />
    public IEnigmaRotor this[EnigmaRotorPosition position] => _rotors[position];

    /// <inheritdoc />
    /// <remarks>
    /// The fastest rotor steps on every key press. The middle rotor steps when the fastest rotor is on its notch, and
    /// also when the middle rotor is on its own notch, which is the double step. The slowest rotor steps with the
    /// middle rotor when the middle rotor is on its own notch.
    /// </remarks>
    public void AdvanceRotors()
    {
        bool middleOnNotch = IsOnNotch(EnigmaRotorPosition.Second);
        bool fastestOnNotch = IsOnNotch(EnigmaRotorPosition.Fastest);

        if (middleOnNotch)
        {
            Advance(EnigmaRotorPosition.Third);
        }

        if (middleOnNotch || fastestOnNotch)
        {
            Advance(EnigmaRotorPosition.Second);
        }

        Advance(EnigmaRotorPosition.Fastest);
    }

    private bool IsOnNotch(EnigmaRotorPosition position)
        => _rotors[position].Notches.Contains(_rotors[position].CurrentSetting, StringComparison.Ordinal);

    private void Advance(EnigmaRotorPosition position)
        => _rotors[position].CurrentSetting
            = (char)(((_rotors[position].CurrentSetting + 1 - 'A') % 26) + 'A');
}
