// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Security.Cryptography;

/// <summary>
/// Settings for the book cipher.
/// </summary>
public sealed record BookCipherSettings : IBookCipherSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BookCipherSettings"/> class.
    /// </summary>
    /// <param name="keyText">The key text, which must hold at least one letter.</param>
    /// <exception cref="ArgumentException">The key text holds no letters.</exception>
    public BookCipherSettings(string keyText)
    {
        ArgumentNullException.ThrowIfNull(keyText);

        KeyText = keyText.Any(static c => Alphabet.IndexOf(c) >= 0)
            ? keyText
            : throw new ArgumentException("The key text must contain a letter.", nameof(keyText));
    }

    /// <inheritdoc />
    public string KeyText { get; }
}
