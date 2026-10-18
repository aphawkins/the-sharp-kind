// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Xunit;

#pragma warning disable CA5351, CA5390 // The broken DES that stages 9 and 10 use, and its hard-coded keys

namespace SharpKind.Security.Cryptography.Tests;

/// <summary>
/// Simon Singh's 'The Code Book' cipher challenge. Singh gave permission for the ciphertexts to be
/// published on the web.
/// </summary>
/// <remarks>
/// All ten stages are solved. Stages 9 and 10 use .NET's DES and BigInteger, not a cipher of this library.
/// </remarks>
public class SinghCipherChallengeTests
{
    [Fact]
    public void Stage1MonoAlphabetic()
    {
        const string ciphertext = SinghCiphertexts.Stage1;

        const string plaintext = SinghPlaintexts.Stage1;

        IMonoAlphabeticSettings settings = new MonoAlphabeticSettings() { Substitutions = SinghKeys.Stage1Substitutions };
        MonoAlphabetic cipher = new(settings);
        System.Diagnostics.Debug.WriteLine(cipher.Decrypt(ciphertext));
        Assert.Equal(plaintext, cipher.Decrypt(ciphertext));
        Assert.Equal(ciphertext, cipher.Encrypt(plaintext));
    }

    [Fact]
    public void Stage2Caesar()
    {
        const string ciphertext = SinghCiphertexts.Stage2;
        const string plaintext = SinghPlaintexts.Stage2;

        Caesar cipher = new(
            new CaesarSettings()
            {
                RightShift = SinghKeys.Stage2Shift,
            });
        Assert.Equal(plaintext, cipher.Decrypt(ciphertext));
        Assert.Equal(ciphertext, cipher.Encrypt(plaintext));
    }

    [Fact]
    public void Stage3Homophonic()
    {
        const string ciphertext = SinghCiphertexts.Stage3;
        const string plaintext = SinghPlaintexts.Stage3;

        // Singh's A takes a different symbol on each line, so only decrypting reproduces his text.
        Homophonic cipher = new(new HomophonicSettings(SinghKeys.Stage3Homophones));
        Assert.Equal(plaintext, cipher.Decrypt(ciphertext));
        Assert.Equal(plaintext, cipher.Decrypt(cipher.Encrypt(plaintext)));
    }

    [Fact]
    public void Stage4Vigenere()
    {
        const string ciphertext = SinghCiphertexts.Stage4;

        const string plaintext = SinghPlaintexts.Stage4;

        Vigenere cipher = new(
            new VigenereSettings()
            {
                Keyword = SinghKeys.Stage4Keyword,
            });
        Assert.Equal(ciphertext, cipher.Encrypt(plaintext));
        Assert.Equal(plaintext, cipher.Decrypt(ciphertext));
    }

    [Fact]
    public void Stage5Book()
    {
        const string ciphertext = SinghCiphertexts.Stage5;
        const string plaintext = SinghPlaintexts.Stage5;

        BookCipher cipher = new(new BookCipherSettings(SinghKeys.Stage5KeyText));
        Assert.Equal(plaintext, cipher.Decrypt(ciphertext));
    }

    [Fact]
    public void Stage6Playfair()
    {
        const string ciphertext = SinghCiphertexts.Stage6;

        // Singh's message runs on without spaces, with Q padding the end.
        const string plaintext = SinghPlaintexts.Stage6;

        Playfair cipher = new(new PlayfairSettings() { Keyword = SinghKeys.Stage6Keyword });
        Assert.Equal(plaintext, cipher.Decrypt(ciphertext));
        Assert.Equal(ciphertext, cipher.Encrypt(plaintext));
    }

    [Fact]
    public void Stage7Fractionating()
    {
        const string ciphertext = SinghCiphertexts.Stage7;
        const string plaintext = SinghPlaintexts.Stage7;

        Adfgvx cipher = new(new AdfgvxSettings(SinghKeys.Stage7Labels, SinghKeys.Stage7Square, SinghKeys.Stage7Keyword));
        Assert.Equal(plaintext, cipher.Decrypt(ciphertext));
        Assert.Equal(ciphertext, cipher.Encrypt(plaintext));
    }

    [Fact]
    public void Stage8Enigma()
    {
        const string ciphertext = SinghCiphertexts.Stage8;

        (string reflector, string wheels, string rings, string plugs, string messageKey) = SinghKeys.Stage8Enigma();
        EnigmaSettings settings = EnigmaFactory.Settings(reflector, wheels, rings, plugs, messageKey);
        IEnigmaRotors rotors = settings.Rotors;

        const string plaintext = SinghPlaintexts.Stage8;

        Enigma cipher = new(settings);
        string newPlaintext = cipher.Decrypt(ciphertext);
        Assert.Equal(plaintext, newPlaintext);
        Assert.Equal('R', rotors[EnigmaRotorPosition.Fastest].CurrentSetting);
        Assert.Equal('A', rotors[EnigmaRotorPosition.Second].CurrentSetting);
        Assert.Equal('A', rotors[EnigmaRotorPosition.Third].CurrentSetting);
    }

    [Fact]
    public void Stage9Des()
    {
        byte[] key = Convert.FromHexString(SinghKeys.Stage9DesKey);

        using DES des = DES.Create();
        des.Key = key;
        string plaintext = Encoding.ASCII.GetString(des.DecryptEcb(UuDecoder.Decode(SinghCiphertexts.Stage9), PaddingMode.None));

        Assert.StartsWith(SinghPlaintexts.Stage9, plaintext, StringComparison.Ordinal);
    }

    [Fact]
    public void Stage10Rsa()
    {
        BigInteger modulus = ParseDigits(SinghKeys.Modulus);
        BigInteger factor = ParseDigits(SinghKeys.Factor);
        BigInteger exponent = SinghKeys.Stage10Exponent;

        Assert.Equal(BigInteger.Zero, modulus % factor);

        BigInteger otherFactor = modulus / factor;
        BigInteger totient = (factor - 1) * (otherFactor - 1);
        BigInteger privateExponent = ModInverse(exponent, totient);
        BigInteger enciphered = ParseDigits(SinghCiphertexts.Stage10Short);

        byte[] tripleDesKey = BigInteger.ModPow(enciphered, privateExponent, modulus).ToByteArray(isUnsigned: true, isBigEndian: true);
        Assert.Equal(16, tripleDesKey.Length);

        // Singh meant triple DES, but only the first half of the key is used: it is single DES.
        byte[] key = tripleDesKey[..8];
        Assert.Equal(SinghKeys.Stage10DesKey, Convert.ToHexString(key));

        using DES des = DES.Create();
        des.Key = key;
        string plaintext = Encoding.ASCII.GetString(des.DecryptEcb(UuDecoder.Decode(SinghCiphertexts.Stage10Long), PaddingMode.None));

        Assert.StartsWith(SinghPlaintexts.Stage10, plaintext, StringComparison.Ordinal);
    }

    private static BigInteger ParseDigits(string digits)
        => BigInteger.Parse(string.Concat(digits.Where(char.IsDigit)), CultureInfo.InvariantCulture);

    private static BigInteger ModInverse(in BigInteger value, in BigInteger modulus)
    {
        (BigInteger previousRemainder, BigInteger remainder) = (value, modulus);
        (BigInteger previousCoefficient, BigInteger coefficient) = (BigInteger.One, BigInteger.Zero);

        while (!remainder.IsZero)
        {
            BigInteger quotient = previousRemainder / remainder;
            (previousRemainder, remainder) = (remainder, previousRemainder - (quotient * remainder));
            (previousCoefficient, coefficient) = (coefficient, previousCoefficient - (quotient * coefficient));
        }

        return ((previousCoefficient % modulus) + modulus) % modulus;
    }
}
