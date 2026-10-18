// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Security.Cryptography;
using System.Text;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// Transforms one UTF-16 character at a time. A surrogate pair is held until both halves arrive,
/// so that the cipher sees the whole character.
/// </summary>
internal sealed class ClassicalSymmetricTransform : ICryptoTransform
{
    private const int CharSize = 2;
    private readonly CipherTransformMode _transformMode;
    private readonly Encoding _encoding = new UnicodeEncoding(false, false);
    private readonly Decoder _decoder;

    internal ClassicalSymmetricTransform(ICipher cipher, CipherTransformMode transformMode)
    {
        _transformMode = transformMode;
        _decoder = _encoding.GetDecoder();
        Cipher = cipher;
    }

    public bool CanReuseTransform => true;

    public bool CanTransformMultipleBlocks => false;

    public int InputBlockSize => CharSize;

    /// <summary>
    /// Gets the output block size, which is room for a surrogate pair.
    /// </summary>
    public int OutputBlockSize => CharSize * 2;

    public ICipher Cipher { get; }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <inheritdoc />
    public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
    {
        if (inputCount <= 0)
        {
            // Nothing to do
            return 0;
        }

        ArgumentNullException.ThrowIfNull(inputBuffer);

        ArgumentNullException.ThrowIfNull(outputBuffer);

        if (inputOffset < 0 || inputBuffer.Length < (inputOffset + inputCount))
        {
            throw new ArgumentException("Input buffer not long enough.", nameof(inputBuffer));
        }

        byte[] outputBytes = Transform(inputBuffer, inputOffset, inputCount, flush: false);

        // The cipher drops some characters, such as the Enigma's digits.
        int count = Math.Min(outputBytes.Length, OutputBlockSize);
        Array.Copy(outputBytes, 0, outputBuffer, outputOffset, count);
        return count;
    }

    /// <inheritdoc />
    public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
    {
        ArgumentNullException.ThrowIfNull(inputBuffer);

        return Transform(inputBuffer, inputOffset, Math.Max(inputCount, 0), flush: true);
    }

    private byte[] Transform(byte[] inputBuffer, int inputOffset, int inputCount, bool flush)
    {
        char[] input = new char[_decoder.GetCharCount(inputBuffer, inputOffset, inputCount, flush)];
        _ = _decoder.GetChars(inputBuffer, inputOffset, inputCount, input, 0, flush);

        if (input.Length == 0)
        {
            // Half of a surrogate pair, which waits for the other half.
            return [];
        }

        string inputString = new(input);

        string outputString = _transformMode switch
        {
            CipherTransformMode.Encrypt => Cipher.Encrypt(inputString),
            CipherTransformMode.Decrypt => Cipher.Decrypt(inputString),
            _ => throw new CryptographicException($"Unsupported transform mode {_transformMode}."),
        };

        return _encoding.GetBytes(outputString);
    }
}
