// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography;

/// <summary>
/// The Atbash cipher.
/// </summary>
public sealed class AtbashSymmetric : ClassicalSymmetricAlgorithm
{
    private readonly Atbash _algorithm = new();

    /// <inheritdoc />
    public override byte[] Key
    {
        get => [];
        set => _ = value;
    }

    /// <inheritdoc />
    private protected override ICipher Cipher => _algorithm;

    /// <inheritdoc />
    public override void GenerateKey()
    {
        KeyValue = [];
        Key = KeyValue;
    }
}
