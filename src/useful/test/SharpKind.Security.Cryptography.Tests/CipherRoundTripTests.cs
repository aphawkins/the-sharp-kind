// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using Xunit;

namespace SharpKind.Security.Cryptography.Tests;

/// <summary>
/// Pins the contract documented on <see cref="ICipher"/>: text round trips through the cipher,
/// with letters coming back upper cased and everything else untouched.
/// </summary>
public class CipherRoundTripTests
{
    public static TheoryData<string> CipherNames => [
        "Atbash",
        "ROT13",
        "Caesar",
        "Vigenere",
        "MonoAlphabetic",
        "Reflector",
    ];

    /// <summary>
    /// Gets each cipher, with what decrypting "Hello World" gives once the cipher has random settings.
    /// </summary>
    public static TheoryData<string, string> GeneratedCiphers => new()
    {
        { "Atbash", "HELLO WORLD" },
        { "ROT13", "HELLO WORLD" },
        { "Caesar", "HELLO WORLD" },
        { "Vigenere", "HELLO WORLD" },
        { "MonoAlphabetic", "HELLO WORLD" },
        { "Reflector", "HELLO WORLD" },
        { "Homophonic", "HELLO WORLD" },
        { "ADFGVX", "HELLOWORLD" },
        { "Book", "HELLOWORLD" },
        { "Playfair", "HELXLOWORLDX" },
    };

    [Theory]
    [MemberData(nameof(CipherNames))]
    public void RoundTripUpperCases(string cipherName)
    {
        const string plaintext = "The quick brown fox jumps over the lazy dog!";

        ICipher cipher = Create(cipherName);
        string ciphertext = cipher.Encrypt(plaintext);

        Assert.Equal(plaintext.ToUpperInvariant(), cipher.Decrypt(ciphertext));
    }

    [Theory]
    [MemberData(nameof(CipherNames))]
    public void NonLettersPassThrough(string cipherName)
    {
        // Includes a soft hyphen, a zero width joiner, NUL, and a character that needs a surrogate pair.
        const string punctuation = "1234567890 !\"$%^&*()­‍\0\U0001F600";

        Assert.Equal(punctuation, Create(cipherName).Encrypt(punctuation));
    }

    [Theory]
    [MemberData(nameof(CipherNames))]
    public void EncryptRejectsNull(string cipherName)
    {
        ICipher cipher = Create(cipherName);

        Assert.Throws<ArgumentNullException>(() => cipher.Encrypt(null!));
        Assert.Throws<ArgumentNullException>(() => cipher.Decrypt(null!));
    }

    [Theory]
    [MemberData(nameof(GeneratedCiphers))]
    public void RoundTripAfterGenerateSettings(string cipherName, string expected)
    {
        ICipher cipher = Create(cipherName);

        cipher.GenerateSettings();

        Assert.Equal(expected, cipher.Decrypt(cipher.Encrypt("Hello World")));
    }

    private static ICipher Create(string cipherName)
        => cipherName switch
        {
            "Atbash" => new Atbash(),
            "ROT13" => new Rot13(),
            "Caesar" => new Caesar(new CaesarSettings() { RightShift = 7 }),
            "Vigenere" => new Vigenere(new VigenereSettings() { Keyword = "LEMON" }),
            "MonoAlphabetic" => new MonoAlphabetic(
                new MonoAlphabeticSettings() { Substitutions = "BADCFEHGJILKNMPORQTSVUXWZY" }),
            "Reflector" => new Reflector(new ReflectorSettings()),
            "Homophonic" => new Homophonic(new HomophonicSettings()),
            "ADFGVX" => new Adfgvx(new AdfgvxSettings()),
            "Book" => new BookCipher(new BookCipherSettings("Hello, World!")),
            "Playfair" => new Playfair(new PlayfairSettings()),
            _ => throw new ArgumentException($"Unknown cipher '{cipherName}'.", nameof(cipherName)),
        };
}
