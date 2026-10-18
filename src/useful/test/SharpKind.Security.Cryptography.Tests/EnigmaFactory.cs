// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography.Tests;

/// <summary>
/// Builds Enigma settings from the strings of the stream cipher's key, so tests can name a machine in one line.
/// </summary>
internal static class EnigmaFactory
{
    /// <summary>
    /// Builds the rotors.
    /// </summary>
    /// <param name="wheels">The wheels, left to right, such as <c>III II I</c>.</param>
    /// <param name="rings">The ring positions, in the order of the wheels, such as <c>01 06 01</c>.</param>
    /// <param name="start">The wheel settings, in the order of the wheels, such as <c>A L L</c> or <c>ALL</c>.</param>
    /// <returns>The rotors.</returns>
    internal static EnigmaRotors Rotors(string wheels, string rings, string start)
    {
        EnigmaRotorPosition[] positions = [EnigmaRotorPosition.Third, EnigmaRotorPosition.Second, EnigmaRotorPosition.Fastest];
        string[] numbers = wheels.Split(' ');
        string[] ringNumbers = rings.Split(' ');
        string settings = start.Replace(" ", string.Empty, StringComparison.Ordinal);
        Dictionary<EnigmaRotorPosition, IEnigmaRotor> rotors = [];

        for (int i = 0; i < positions.Length; i++)
        {
            rotors[positions[i]] = new EnigmaRotor()
            {
                RotorNumber = Enum.Parse<EnigmaRotorNumber>(numbers[i]),
                RingPosition = int.Parse(ringNumbers[i], System.Globalization.CultureInfo.InvariantCulture),
                CurrentSetting = settings[i],
            };
        }

        return new EnigmaRotors() { Rotors = rotors };
    }

    /// <summary>
    /// Builds the settings of a machine.
    /// </summary>
    /// <param name="reflector">The reflector, such as <c>B</c>.</param>
    /// <param name="wheels">The wheels, left to right, such as <c>III II I</c>.</param>
    /// <param name="rings">The ring positions, in the order of the wheels.</param>
    /// <param name="plugs">The plugboard pairs, such as <c>AS EI</c>, or nothing.</param>
    /// <param name="start">The wheel settings, in the order of the wheels.</param>
    /// <returns>The settings.</returns>
    internal static EnigmaSettings Settings(string reflector, string wheels, string rings, string plugs, string start)
    {
        List<EnigmaPlugboardPair> pairs =
        [
            .. plugs.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(static pair => new EnigmaPlugboardPair() { From = pair[0], To = pair[1] }),
        ];

        return new EnigmaSettings()
        {
            Reflector = new EnigmaReflector() { ReflectorNumber = Enum.Parse<EnigmaReflectorNumber>(reflector) },
            Rotors = Rotors(wheels, rings, start),
            Plugboard = new EnigmaPlugboard(pairs),
        };
    }
}
