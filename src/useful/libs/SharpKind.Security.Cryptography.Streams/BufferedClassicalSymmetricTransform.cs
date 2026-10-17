// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Security.Cryptography;
using System.Text;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// Transforms whole messages. Ciphers such as Playfair and ADFGVX need the entire text, so this
/// collects every block and runs the cipher once, in <see cref="TransformFinalBlock"/>.
/// </summary>
internal sealed class BufferedClassicalSymmetricTransform : ICryptoTransform
{
    private const int BlockSize = 2;  // 2 for Unicode, 1 for UTF8
    private readonly CipherTransformMode _transformMode;
    private readonly Encoding _encoding = new UnicodeEncoding();
    private readonly StringBuilder _buffer = new();

    internal BufferedClassicalSymmetricTransform(ICipher cipher, CipherTransformMode transformMode)
    {
        _transformMode = transformMode;
        Cipher = cipher;
    }

    public bool CanReuseTransform => true;

    public bool CanTransformMultipleBlocks => false;

    public int InputBlockSize => BlockSize;

    public int OutputBlockSize => BlockSize;

    public ICipher Cipher { get; }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <inheritdoc />
    public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
    {
        ArgumentNullException.ThrowIfNull(inputBuffer);

        if (inputBuffer.Length < inputOffset + inputCount)
        {
            throw new ArgumentException("Input buffer not long enough.", nameof(inputBuffer));
        }

        _ = _buffer.Append(_encoding.GetString(inputBuffer, inputOffset, inputCount));

        // Nothing is output until the final block.
        return 0;
    }

    public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
    {
        ArgumentNullException.ThrowIfNull(inputBuffer);

        if (inputCount > 0)
        {
            _ = TransformBlock(inputBuffer, inputOffset, inputCount, [], 0);
        }

        string input = _buffer.ToString();
        _ = _buffer.Clear();

        if (input.Length == 0)
        {
            return [];
        }

        string output = _transformMode switch
        {
            CipherTransformMode.Encrypt => Cipher.Encrypt(input),
            CipherTransformMode.Decrypt => Cipher.Decrypt(input),
            _ => throw new CryptographicException($"Unsupported transform mode {_transformMode}."),
        };

        return _encoding.GetBytes(output);
    }
}
