// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using Xunit;

namespace SharpKind.Security.Cryptography.Tests;

public class MonoAlphabeticSettingsTests
{
    [Fact]
    public void CtorEmpty()
    {
        MonoAlphabeticSettings settings = new();
        Assert.Equal("ABCDEFGHIJKLMNOPQRSTUVWXYZ", settings.CharacterSet);
        Assert.Equal(0, settings.SubstitutionCount);
        Assert.Equal("ABCDEFGHIJKLMNOPQRSTUVWXYZ", settings.Substitutions);
    }

    [Theory]
    [InlineData("", "ABC")] // No character set
    [InlineData(" ABCD", "ABCD")] // Character set spacing
    [InlineData("AB CD", "ABCD")] // Character set spacing
    [InlineData("ABCD ", "ABCD")] // Character set spacing
    public void CtorCharacterSetInvalid(string characterSet, string substitutions)
        => Assert.Throws<ArgumentException>(
            nameof(characterSet),
            () => new MonoAlphabeticSettings()
            {
                CharacterSet = characterSet,
                Substitutions = substitutions,
            });

    [Theory]
    [InlineData("ABC", "")] // No substitutions
    [InlineData("ABC", "AB")] // Too few subs
    [InlineData("ABC", "ABCA")] // Too many subs
    [InlineData("ABCD", " ABCD")] // Subs spacing
    [InlineData("ABCD", "ABCD ")] // Subs spacing
    [InlineData("ABCD", "AB CD")] // Subs spacing
    [InlineData("ABCD", "aBCD")] // Subs incorrect case
    [InlineData("ABCD", "AACD")] // Incorrect subs letters
    public void CtorSubstitutionsInvalid(string characterSet, string substitutions)
        => Assert.Throws<ArgumentException>(
            nameof(substitutions),
            () => new MonoAlphabeticSettings()
            {
                CharacterSet = characterSet,
                Substitutions = substitutions,
            });

    [Theory]
    [InlineData("ABC", "ABC", 0)]
    [InlineData("VWXYZ", "WVYXZ", 4)]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ", "BADCEFGHIJKLMNOPQRSTUVWXYZ", 4)]
    [InlineData("ØABC", "BAØC", 2)]
    public void CtorSettings(string characterSet, string substitutions, int substitutionCount)
    {
        MonoAlphabeticSettings settings = new()
        {
            CharacterSet = characterSet,
            Substitutions = substitutions,
        };
        Assert.Equal(characterSet, settings.CharacterSet);
        Assert.Equal(substitutionCount, settings.SubstitutionCount);
        Assert.Equal(substitutions, settings.Substitutions);
    }

    [Theory]
    [InlineData("ABC", "ABC", 'Ø', 'A', "substitution")]
    [InlineData("ABC", "ABC", 'A', 'Ø', "newSubstitution")]
    public void GetSubstitutionsInvalid(string characterSet, string substitutions, char from, char to, string parameterName)
    {
        MonoAlphabeticSettings settings = new()
        {
            CharacterSet = characterSet,
            Substitutions = substitutions,
        };

        Assert.Throws<ArgumentException>(parameterName, () => settings.SetSubstitution(from, to));
    }

    [Theory]
    [InlineData("ABC", "ABC", 'A', 'A')]
    public void GetSubstitutionsValid(string characterSet, string substitutions, char from, char to)
    {
        MonoAlphabeticSettings settings = new()
        {
            CharacterSet = characterSet,
            Substitutions = substitutions,
        };
        settings.SetSubstitution(from, to);

        Assert.Equal(to, settings.GetSubstitution(from));
    }

    [Fact]
    public void SubstitutionsBeforeCharacterSet()
        => Assert.Throws<InvalidOperationException>(() => new MonoAlphabeticSettings()
        {
            Substitutions = "BACDEFGHIJKLMNOPQRSTUVWXYZ",
            CharacterSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
        });

    [Fact]
    public void EqualSettings()
    {
        MonoAlphabeticSettings initialised = new() { CharacterSet = "ABC", Substitutions = "BAC" };
        MonoAlphabeticSettings substituted = new() { CharacterSet = "ABC" };
        substituted.SetSubstitution('A', 'B');

        Assert.Equal(initialised, substituted);
        Assert.Equal(initialised.GetHashCode(), substituted.GetHashCode());
        Assert.NotEqual(initialised, new MonoAlphabeticSettings() { CharacterSet = "ABC" });
    }

    [Theory]
    [InlineData('­')] // Soft hyphen
    [InlineData('‍')] // Zero width joiner
    public void IgnorableCharactersHaveNoSubstitution(char letter)
    {
        MonoAlphabeticSettings settings = new() { Substitutions = "BADCFEHGJILKNMPORQTSVUXWZY" };
        Assert.Equal(letter, settings.GetSubstitution(letter));
        Assert.Equal(letter, settings.Reverse(letter));
    }

    [Fact]
    public void Reverse()
    {
        MonoAlphabeticSettings settings = new();
        settings.SetSubstitution('A', 'B');
        Assert.Equal('A', settings.Reverse('B'));
    }

    [Fact]
    public void ReverseInvalid()
    {
        MonoAlphabeticSettings settings = new();
        Assert.Equal('a', settings.Reverse('a'));
    }

    [Fact]
    public void ReverseUndoesEverySubstitution()
    {
        MonoAlphabeticSettings settings = new();
        settings.SetSubstitution('A', 'D');
        settings.SetSubstitution('B', 'Q');
        settings.SetSubstitution('X', 'M');

        foreach (char letter in settings.CharacterSet)
        {
            Assert.Equal(letter, settings.Reverse(settings.GetSubstitution(letter)));
        }
    }

    [Theory]
    [InlineData("ABC", "BAC", 'A', 'C', "CAB", 3)]
    [InlineData("ABC", "BCA", 'B', 'B', "CBA", 2)]
    [InlineData("ABC", "BAC", 'A', 'A', "ABC", 0)] // Reset
    [InlineData("ABC", "BCA", 'C', 'A', "BCA", 3)] // Existing
    [InlineData("ABC", "ABC", 'A', 'B', "BAC", 2)]
    public void SetSubstitutionChange(
        string characterSet,
        string substitutions,
        char from,
        char to,
        string newSubstitutions,
        int substitutionCount)
    {
        MonoAlphabeticSettings settings = new()
        {
            CharacterSet = characterSet,
            Substitutions = substitutions,
        };
        settings.SetSubstitution(from, to);

        Assert.Equal(to, settings.GetSubstitution(from));
        Assert.Equal(newSubstitutions, settings.Substitutions);
        Assert.Equal(substitutionCount, settings.SubstitutionCount);
    }

    [Theory]
    [InlineData("ABC", "ABC", 'A', 'Ø', 0, "newSubstitution")]
    [InlineData("ABC", "ABC", 'Ø', 'A', 0, "substitution")]
    public void SetSubstitutionInvalid(
        string characterSet,
        string substitutions,
        char from,
        char to,
        int substitutionCount,
        string parameterName)
    {
        MonoAlphabeticSettings settings = new()
        {
            CharacterSet = characterSet,
            Substitutions = substitutions,
        };

        Assert.Throws<ArgumentException>(parameterName, () => settings.SetSubstitution(from, to));
        Assert.Equal(substitutionCount, settings.SubstitutionCount);
        Assert.Equal(substitutions, settings.Substitutions);
    }
}
