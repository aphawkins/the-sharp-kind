// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Globalization;
using System.Text;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// The Rail Fence cipher. The key is the number of rails, from 2 to 25. Transposition moves letters
/// across the whole message, so the message is transformed at once.
/// </summary>
public sealed class RailFenceSymmetric : ClassicalSymmetricAlgorithm
{
    private RailFence _algorithm = new(new RailFenceSettings());

    /// <inheritdoc />
    public override byte[] Key
    {
        get => Encoding.Unicode.GetBytes(_algorithm.Settings.Rails.ToString(CultureInfo.InvariantCulture));

        set
        {
            _algorithm = new(GetSettings(value));
            base.Key = value;
        }
    }

    /// <inheritdoc />
    private protected override ICipher Cipher => _algorithm;

    /// <inheritdoc />
    private protected override bool NeedsWholeMessage => true;

    /// <inheritdoc />
    public override void GenerateKey()
    {
        _algorithm.GenerateSettings();
        KeyValue = Key;
    }

    private static RailFenceSettings GetSettings(byte[] key) => key == null
            ? throw new ArgumentNullException(nameof(key))
            : !int.TryParse(
                Encoding.Unicode.GetString(key),
                NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out int rails)
            ? throw new ArgumentException("Value must be a number.", nameof(key))
            : rails < 2 || rails > RailFenceSettings.MaxRails
            ? throw new ArgumentOutOfRangeException(nameof(key), "Value must be between 2 and 25.")
            : new RailFenceSettings() { Rails = rails };
}
