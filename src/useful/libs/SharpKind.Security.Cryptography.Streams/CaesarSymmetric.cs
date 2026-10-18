// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Globalization;
using System.Text;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// The Caesar cipher.
/// </summary>
public sealed class CaesarSymmetric : ClassicalSymmetricAlgorithm
{
    private Caesar _algorithm = new(new CaesarSettings());

    /// <inheritdoc />
    public override byte[] Key
    {
        get => Encoding.Unicode.GetBytes($"{_algorithm.Settings.RightShift}");

        set
        {
            _algorithm = new(GetSettings(value));
            base.Key = value;
        }
    }

    /// <inheritdoc />
    private protected override ICipher Cipher => _algorithm;

    /// <inheritdoc />
    public override void GenerateKey()
    {
        _algorithm.GenerateSettings();
        KeyValue = Key;
    }

    private static CaesarSettings GetSettings(byte[] key) => key == null
            ? throw new ArgumentNullException(nameof(key))
            : !int.TryParse(
                Encoding.Unicode.GetString(key),
                NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out int rightShift)
            ? throw new ArgumentException("Value must be a number.", nameof(key))
            : rightShift is < 0 or > 25
            ? throw new ArgumentOutOfRangeException(nameof(key), "Value must be between 0 and 25.")
            : new CaesarSettings() { RightShift = rightShift };
}
