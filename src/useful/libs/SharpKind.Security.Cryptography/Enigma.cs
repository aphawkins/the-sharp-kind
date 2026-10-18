// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// Simulates the Enigma encoding machine.
/// </summary>
public sealed class Enigma : ICipher
{
    private Dictionary<EnigmaRotorPosition, char> _initialRotorSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="Enigma"/> class.
    /// </summary>
    /// <param name="settings">The cipher's settings.</param>
    public Enigma(IEnigmaSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Settings = settings;
        _initialRotorSettings = CaptureRotorSettings(settings);
    }

    /// <inheritdoc />
    public string CipherName => "Enigma M3";

    /// <summary>
    /// Gets settings.
    /// </summary>
    public IEnigmaSettings Settings { get; private set; }

    /// <inheritdoc />
    public override string ToString() => CipherName;

    /// <inheritdoc />
    public string Decrypt(string ciphertext) => Encrypt(ciphertext);

    /// <summary>
    /// Encrypts a plaintext string.
    /// </summary>
    /// <remarks>
    /// The machine only had letter keys and a space bar, so anything that is neither a letter
    /// nor a space is dropped rather than passed through. Encrypting advances the rotors: to
    /// encrypt or decrypt a second message with the same key, call <see cref="Reset"/> first.
    /// </remarks>
    /// <param name="plaintext">The text to encrypt.</param>
    /// <returns>The encrypted text.</returns>
    public string Encrypt(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        StringBuilder output = new(plaintext.Length);

        foreach (char inputChar in plaintext)
        {
            if (inputChar == ' ')
            {
                output.Append(' ');
            }
            else if (Alphabet.IndexOf(inputChar) >= 0)
            {
                output.Append(Encrypt(char.ToUpperInvariant(inputChar)));
            }
        }

        return output.ToString();
    }

    /// <summary>
    /// Winds the rotors back to the position they were in when the current settings were set,
    /// so that the same key can be used to encrypt or decrypt another message.
    /// </summary>
    public void Reset()
    {
        foreach ((EnigmaRotorPosition position, char setting) in _initialRotorSettings)
        {
            Settings.Rotors[position].CurrentSetting = setting;
        }
    }

    /// <summary>
    /// Generates random settings.
    /// </summary>
    public void GenerateSettings()
    {
        Settings = EnigmaSettingsGenerator.Generate();
        _initialRotorSettings = CaptureRotorSettings(Settings);
    }

    private static Dictionary<EnigmaRotorPosition, char> CaptureRotorSettings(IEnigmaSettings settings)
        => EnigmaRotors.RotorPositions.ToDictionary(
            position => position,
            position => settings.Rotors[position].CurrentSetting);

    /// <summary>
    /// Encrypt a plaintext letter into an enciphered letter.  Decipher works in the same way as encipher.
    /// </summary>
    /// <param name="letter">The plaintext letter to encipher.</param>
    /// <returns>The encrypted letter.</returns>
    /// <exception cref="InvalidOperationException">The settings let a letter encrypt to itself.</exception>
    private char Encrypt(char letter)
    {
        // Advance the rotors one position
        Settings.Rotors.AdvanceRotors();

        // Plugboard
        char newLetter = Settings.Plugboard.GetSubstitution(letter);

        // Go thru the rotors forwards
        newLetter = Settings.Rotors[EnigmaRotorPosition.Fastest].Forward(newLetter);
        newLetter = Settings.Rotors[EnigmaRotorPosition.Second].Forward(newLetter);
        newLetter = Settings.Rotors[EnigmaRotorPosition.Third].Forward(newLetter);

        // Go thru the relector
        newLetter = Settings.Reflector.Reflect(newLetter);

        // Go thru the rotors backwards
        newLetter = Settings.Rotors[EnigmaRotorPosition.Third].Backward(newLetter);
        newLetter = Settings.Rotors[EnigmaRotorPosition.Second].Backward(newLetter);
        newLetter = Settings.Rotors[EnigmaRotorPosition.Fastest].Backward(newLetter);

        newLetter = Settings.Plugboard.GetSubstitution(newLetter);

        // A letter cannot encrypt to itself; if it does the reflector or plugboard is malformed,
        // and checking it in release builds too keeps that from silently producing bad ciphertext.
        return letter == newLetter
            ? throw new InvalidOperationException(
                $"The letter '{letter}' encrypted to itself, so the reflector or plugboard is invalid.")
            : newLetter;
    }
}
