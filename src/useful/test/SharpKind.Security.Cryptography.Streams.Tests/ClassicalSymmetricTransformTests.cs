// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Security.Cryptography;
using System.Text;
using Xunit;

#pragma warning disable CA5390 // Do not hard-code encryption key

namespace SharpKind.Security.Cryptography.Streams.Tests;

/// <summary>
/// The classical stream ciphers must give the same text however it reaches a <see cref="CryptoStream"/>: a character
/// at a time, in chunks, or from the middle of a larger buffer.
/// </summary>
public class ClassicalSymmetricTransformTests
{
    private const string Plaintext = "THE QUICK BROWN FOX JUMPS OVER THE LAZY DOG";

    public static TheoryData<string> Ciphers => ["Atbash", "Caesar", "ROT13", "MonoAlphabetic"];

    [Theory]
    [MemberData(nameof(Ciphers))]
    public void ChunksMatchOneCharacterAtATime(string name)
    {
        using SymmetricAlgorithm cipher = Create(name);
        string expected = CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, Plaintext);

        foreach (int chunkLength in new[] { 1, 2, 5, 43 })
        {
            using ICryptoTransform transform = cipher.CreateEncryptor();
            Assert.Equal(expected, CryptoStreamHelper.Transform(transform, Plaintext, chunkLength));
        }
    }

    [Theory]
    [MemberData(nameof(Ciphers))]
    public void OffsetIntoTheWrittenBuffer(string name)
    {
        using SymmetricAlgorithm cipher = Create(name);
        string expected = CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, Plaintext);

        using ICryptoTransform transform = cipher.CreateEncryptor();
        Assert.Equal(expected, CryptoStreamHelper.Transform(transform, Plaintext, 7, paddingBefore: 3));
    }

    [Theory]
    [MemberData(nameof(Ciphers))]
    public void DecryptingChunksReturnsThePlaintext(string name)
    {
        using SymmetricAlgorithm cipher = Create(name);
        string ciphertext = CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, Plaintext);

        using ICryptoTransform transform = cipher.CreateDecryptor();
        Assert.Equal(Plaintext, CryptoStreamHelper.Transform(transform, ciphertext, 6));
    }

    [Theory]
    [MemberData(nameof(Ciphers))]
    public void SurrogatePairSplitAcrossWrites(string name)
    {
        const string text = "A\U0001F600B";

        using SymmetricAlgorithm cipher = Create(name);
        string ciphertext = CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, text);
        Assert.Equal("\U0001F600", ciphertext.Substring(1, 2));

        // One character at a time writes each half of the pair on its own.
        using ICryptoTransform transform = cipher.CreateDecryptor();
        Assert.Equal(text, CryptoStreamHelper.Transform(transform, ciphertext, 1));
    }

    [Fact]
    public void NulPassesThrough()
    {
        using SymmetricAlgorithm cipher = new AtbashSymmetric();
        Assert.Equal("Z\0Y", CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, "A\0B"));
    }

    [Fact]
    public void BufferedSurrogatePairSplitAcrossWrites()
    {
        using SymmetricAlgorithm cipher = new HomophonicSymmetric();
        using ICryptoTransform transform = cipher.CreateEncryptor();
        Assert.Equal("A\U0001F600B", CryptoStreamHelper.Transform(transform, "a\U0001F600b", 1));
    }

    [Fact]
    public void CaesarWriteOfSeveralCharacters()
    {
        using SymmetricAlgorithm cipher = new CaesarSymmetric { Key = Encoding.Unicode.GetBytes("3") };
        using ICryptoTransform transform = cipher.CreateEncryptor();
        Assert.Equal("DEFGHI", CryptoStreamHelper.Transform(transform, "ABCDEF", 6));
    }

    private static SymmetricAlgorithm Create(string name)
        => name switch
        {
            "Atbash" => new AtbashSymmetric(),
            "Caesar" => new CaesarSymmetric { Key = Encoding.Unicode.GetBytes("7") },
            "ROT13" => new Rot13Symmetric(),
            "MonoAlphabetic" => new MonoAlphabeticSymmetric
            {
                Key = Encoding.Unicode.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZ|MFANXIWPBSHGLTCQKVRJUDYZEO"),
            },
            _ => throw new ArgumentException($"Unknown cipher '{name}'.", nameof(name)),
        };
}
