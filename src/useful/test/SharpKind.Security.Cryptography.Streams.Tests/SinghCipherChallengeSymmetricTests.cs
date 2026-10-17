// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SharpKind.Security.Cryptography.Tests;
using Xunit;

#pragma warning disable CA5390 // Do not hard-code encryption key

namespace SharpKind.Security.Cryptography.Streams.Tests;

/// <summary>
/// Simon Singh's 'The Code Book' cipher challenge, through the stream (<see cref="SymmetricAlgorithm"/>) ciphers.
/// See <c>SinghCipherChallengeTests</c> for the plain ciphers, and stages 9 and 10, which use .NET's DES and RSA maths.
/// </summary>
public class SinghCipherChallengeSymmetricTests
{
    [Fact]
    public void Stage1MonoAlphabetic()
    {
        const string ciphertext = SinghCiphertexts.Stage1;

        const string plaintext = SinghPlaintexts.Stage1;

        byte[] key = Encoding.Unicode.GetBytes($"{SinghKeys.Stage1Alphabet}|{SinghKeys.Stage1Substitutions}");
        using SymmetricAlgorithm cipher = new MonoAlphabeticSymmetric
        {
            Key = key,
        };
        Assert.Equal(plaintext, CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Decrypt, ciphertext));
        Assert.Equal(ciphertext, CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, plaintext));
    }

    [Fact]
    public void Stage2Caesar()
    {
        const string ciphertext = SinghCiphertexts.Stage2;
        const string plaintext = SinghPlaintexts.Stage2;

        byte[] key = Encoding.Unicode.GetBytes(SinghKeys.Stage2Shift.ToString(CultureInfo.InvariantCulture));
        using SymmetricAlgorithm cipher = new CaesarSymmetric
        {
            Key = key,
        };

        Assert.Equal(plaintext, CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Decrypt, ciphertext));
        Assert.Equal(ciphertext, CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, plaintext));
    }

    [Fact]
    public void Stage8Enigma()
    {
        const string ciphertext = SinghCiphertexts.Stage8;

        const string keyString = $"{SinghKeys.Stage8Reflector}|{SinghKeys.Stage8Wheels}|{SinghKeys.Stage8Rings}|{SinghKeys.Stage8Plugs}";
        const string initVector = SinghKeys.Stage8MessageKey;
        const string newIv = SinghKeys.Stage8FinalKey;

        const string plaintext = SinghPlaintexts.Stage8;

        using SymmetricAlgorithm target = new EnigmaSymmetric()
        {
            Key = Encoding.Unicode.GetBytes(keyString),
            IV = Encoding.Unicode.GetBytes(initVector),
        };
        string s = CipherMethods.SymmetricTransform(target, CipherTransformMode.Decrypt, ciphertext);
        Assert.Equal(plaintext, s);
        Assert.Equal(keyString, Encoding.Unicode.GetString(target.Key));
        Assert.Equal(newIv, Encoding.Unicode.GetString(target.IV));
    }

    [Fact]
    public void Stage3Homophonic()
    {
        const string ciphertext = SinghCiphertexts.Stage3;
        const string plaintext = SinghPlaintexts.Stage3;

        string key = string.Join(
            '|',
            SinghKeys.Stage3Homophones.Select(static pair => $"{pair.Key}:{string.Join(',', pair.Value)}"));

        using SymmetricAlgorithm cipher = new HomophonicSymmetric { Key = Encoding.Unicode.GetBytes(key) };
        Assert.Equal(plaintext, CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Decrypt, ciphertext));
    }

    [Fact]
    public void Stage5Book()
    {
        const string ciphertext = SinghCiphertexts.Stage5;
        const string plaintext = SinghPlaintexts.Stage5;

        using SymmetricAlgorithm cipher = new BookCipherSymmetric { Key = Encoding.Unicode.GetBytes(SinghKeys.Stage5KeyText) };
        Assert.Equal(plaintext, CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Decrypt, ciphertext));
    }

    [Fact]
    public void Stage6Playfair()
    {
        const string ciphertext = SinghCiphertexts.Stage6;

        // Singh's message runs on without spaces, with Q padding the end.
        const string plaintext = SinghPlaintexts.Stage6;

        using SymmetricAlgorithm cipher = new PlayfairSymmetric { Key = Encoding.Unicode.GetBytes(SinghKeys.Stage6Keyword) };
        Assert.Equal(plaintext, CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Decrypt, ciphertext));
        Assert.Equal(ciphertext, CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, plaintext));
    }

    [Fact]
    public void Stage7Fractionating()
    {
        const string ciphertext = SinghCiphertexts.Stage7;
        const string plaintext = SinghPlaintexts.Stage7;

        string key = $"{SinghKeys.Stage7Labels}|{SinghKeys.Stage7Square}|{SinghKeys.Stage7Keyword}";

        using SymmetricAlgorithm cipher = new AdfgvxSymmetric { Key = Encoding.Unicode.GetBytes(key) };
        Assert.Equal(plaintext, CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Decrypt, ciphertext));
        Assert.Equal(ciphertext, CipherMethods.SymmetricTransform(cipher, CipherTransformMode.Encrypt, plaintext));
    }
}
