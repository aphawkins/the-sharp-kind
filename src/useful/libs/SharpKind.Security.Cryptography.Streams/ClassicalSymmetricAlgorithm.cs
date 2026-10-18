// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Security.Cryptography;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// The parts that the classical ciphers share as a <see cref="SymmetricAlgorithm"/>. They have no IV, and their
/// key is text in UTF-16.
/// </summary>
public abstract class ClassicalSymmetricAlgorithm : SymmetricAlgorithm
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClassicalSymmetricAlgorithm"/> class.
    /// </summary>
    private protected ClassicalSymmetricAlgorithm()
    {
#pragma warning disable CA5358 // Do Not Use Unsafe Cipher Modes - this cipher is inherently unsafe
        ModeValue = CipherMode.ECB;
#pragma warning restore CA5358
        PaddingValue = PaddingMode.None;
        KeySizeValue = 16;
        BlockSizeValue = 16;
        FeedbackSizeValue = 16;
        LegalBlockSizesValue = [new KeySizes(16, 16, 16)];
        LegalKeySizesValue = [new KeySizes(0, int.MaxValue, 16)];

        KeyValue = [];
        IVValue = [];
    }

    /// <inheritdoc />
    /// <remarks>The classical ciphers have no IV, so this is always empty.</remarks>
    public override byte[] IV
    {
        get => [];
        set => _ = value;
    }

    /// <summary>
    /// Gets the cipher that does the work.
    /// </summary>
    private protected abstract ICipher Cipher { get; }

    /// <summary>
    /// Gets a value indicating whether the cipher needs the whole message at once, as Playfair does.
    /// </summary>
    private protected virtual bool NeedsWholeMessage => false;

    /// <inheritdoc />
    public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV)
        => CreateTransform(rgbKey, CipherTransformMode.Decrypt);

    /// <inheritdoc />
    public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV)
        => CreateTransform(rgbKey, CipherTransformMode.Encrypt);

    /// <inheritdoc />
    public override void GenerateIV() => IVValue = [];

    /// <inheritdoc />
    public override string ToString() => Cipher.CipherName;

    private ICryptoTransform CreateTransform(byte[] rgbKey, CipherTransformMode transformMode)
    {
        Key = rgbKey;

        return NeedsWholeMessage
            ? new BufferedClassicalSymmetricTransform(Cipher, transformMode)
            : new ClassicalSymmetricTransform(Cipher, transformMode);
    }
}
