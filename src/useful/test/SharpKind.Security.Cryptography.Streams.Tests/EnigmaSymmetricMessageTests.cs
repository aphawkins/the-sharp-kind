// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Security.Cryptography;
using System.Text;
using SharpKind.Security.Cryptography.Tests;
using Xunit;

#pragma warning disable CA5390 // Do not hard-code encryption key
#pragma warning disable CA5401 // The IV is the rotor start, which these tests choose

namespace SharpKind.Security.Cryptography.Streams.Tests;

/// <summary>
/// An Enigma stream cipher must start every message from the key and the start settings, however many messages the
/// one object has handled, and however the text reaches it.
/// </summary>
public class EnigmaSymmetricMessageTests
{
    private const string Key = "B|III II I|01 06 01|AS EI JN KL MU OT";
    private const string Start = "A L L";
    private const string Plaintext = "THE QUICK BROWN FOX JUMPS OVER THE LAZY DOG";

    [Theory]
    [MemberData(nameof(EnigmaReferenceVectors.Vectors), MemberType = typeof(EnigmaReferenceVectors))]
    public void ReferenceVectors(
        string reflector,
        string wheels,
        string rings,
        string plugs,
        string start,
        string plaintext,
        string ciphertext)
    {
        using SymmetricAlgorithm enigma = Create($"{reflector}|{wheels}|{rings}|{plugs}", start);
        Assert.Equal(ciphertext, CipherMethods.SymmetricTransform(enigma, CipherTransformMode.Encrypt, plaintext));
    }

    [Fact]
    public void SameMessageTwiceGivesTheSameCiphertext()
    {
        using SymmetricAlgorithm enigma = Create(Key, Start);

        string first = CipherMethods.SymmetricTransform(enigma, CipherTransformMode.Encrypt, Plaintext);
        string second = CipherMethods.SymmetricTransform(enigma, CipherTransformMode.Encrypt, Plaintext);

        Assert.Equal(first, second);
    }

    [Fact]
    public void EncryptThenDecryptOnTheSameObject()
    {
        using SymmetricAlgorithm enigma = Create(Key, Start);

        string ciphertext = CipherMethods.SymmetricTransform(enigma, CipherTransformMode.Encrypt, Plaintext);

        Assert.NotEqual(Plaintext, ciphertext);
        Assert.Equal(Plaintext, CipherMethods.SymmetricTransform(enigma, CipherTransformMode.Decrypt, ciphertext));
    }

    [Fact]
    public void IvFollowsTheRotorsButTheNextMessageStartsFromTheStart()
    {
        using SymmetricAlgorithm enigma = Create(Key, Start);

        _ = CipherMethods.SymmetricTransform(enigma, CipherTransformMode.Encrypt, "ABC");

        Assert.NotEqual(Start, Encoding.Unicode.GetString(enigma.IV));

        using SymmetricAlgorithm fresh = Create(Key, Start);
        string expected = CipherMethods.SymmetricTransform(fresh, CipherTransformMode.Encrypt, Plaintext);

        using ICryptoTransform transform = enigma.CreateEncryptor();
        Assert.Equal(expected, CryptoStreamHelper.Transform(transform, Plaintext, Plaintext.Length));
    }

    [Fact]
    public void ANewIvStartsTheNextMessage()
    {
        using SymmetricAlgorithm enigma = Create(Key, Start);
        _ = CipherMethods.SymmetricTransform(enigma, CipherTransformMode.Encrypt, Plaintext);

        enigma.IV = Encoding.Unicode.GetBytes("B C D");
        string actual = CipherMethods.SymmetricTransform(enigma, CipherTransformMode.Encrypt, Plaintext);

        using SymmetricAlgorithm fresh = Create(Key, "B C D");
        string expected = CipherMethods.SymmetricTransform(fresh, CipherTransformMode.Encrypt, Plaintext);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(7)]
    [InlineData(43)]
    public void ChunksOfAnyLengthGiveTheSameCiphertext(int chunkLength)
    {
        using SymmetricAlgorithm enigma = Create(Key, Start);
        string expected = CipherMethods.SymmetricTransform(enigma, CipherTransformMode.Encrypt, Plaintext);

        using ICryptoTransform transform = enigma.CreateEncryptor();
        Assert.Equal(expected, CryptoStreamHelper.Transform(transform, Plaintext, chunkLength));
    }

    [Fact]
    public void OffsetIntoTheWrittenBuffer()
    {
        using SymmetricAlgorithm enigma = Create(Key, Start);
        string expected = CipherMethods.SymmetricTransform(enigma, CipherTransformMode.Encrypt, Plaintext);

        using ICryptoTransform transform = enigma.CreateEncryptor();
        Assert.Equal(expected, CryptoStreamHelper.Transform(transform, Plaintext, 7, paddingBefore: 3));
    }

    private static EnigmaSymmetric Create(string key, string start)
        => new() { Key = Encoding.Unicode.GetBytes(key), IV = Encoding.Unicode.GetBytes(start) };
}
