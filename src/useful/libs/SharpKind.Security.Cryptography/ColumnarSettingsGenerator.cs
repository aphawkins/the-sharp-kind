// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Security.Cryptography;

namespace SharpKind.Security.Cryptography;

/// <summary>
/// Columnar transposition key generator.
/// </summary>
internal static class ColumnarSettingsGenerator
{
    public static ColumnarSettings Generate()
    {
        // A single column reads the text straight back, so always use at least two.
        int length = RandomNumberGenerator.GetInt32(2, ColumnarSettings.MaxKeywordLength + 1);
        char[] keyword = new char[length];

        for (int i = 0; i < length; i++)
        {
            keyword[i] = (char)('A' + RandomNumberGenerator.GetInt32(0, Alphabet.Length));
        }

        return new ColumnarSettings() { Keyword = new string(keyword) };
    }
}
