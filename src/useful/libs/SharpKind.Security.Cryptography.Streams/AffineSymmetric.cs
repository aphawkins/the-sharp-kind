// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Globalization;
using System.Text;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// The Affine cipher. The key is the multiplier and the shift separated by a comma, such as "5,8".
/// </summary>
public sealed class AffineSymmetric : ClassicalSymmetricAlgorithm
{
    private Affine _algorithm = new(new AffineSettings());

    /// <inheritdoc />
    public override byte[] Key
    {
        get => Encoding.Unicode.GetBytes(
            string.Create(CultureInfo.InvariantCulture, $"{_algorithm.Settings.Multiplier},{_algorithm.Settings.Shift}"));

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

    private static AffineSettings GetSettings(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);

        string[] parts = Encoding.Unicode.GetString(key).Split(',');

        if (parts.Length != 2
            || !int.TryParse(parts[0], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int multiplier)
            || !int.TryParse(parts[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int shift))
        {
            throw new ArgumentException("Value must be two numbers separated by a comma.", nameof(key));
        }

        try
        {
            return new AffineSettings() { Multiplier = multiplier, Shift = shift };
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new ArgumentOutOfRangeException(nameof(key), ex.Message);
        }
    }
}
