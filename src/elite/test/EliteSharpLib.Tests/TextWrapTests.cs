// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Views;

namespace EliteSharpLib.Tests;

public class TextWrapTests
{
    [Fact]
    public void SplitLeavesTextThatExactlyFitsAlone()
    {
        string text = new('a', 10);

        Assert.Equal([text], TextWrap.Split(text, 10));
    }

    [Fact]
    public void SplitLeavesShorterTextAlone() => Assert.Equal(["abc"], TextWrap.Split("abc", 10));

    [Fact]
    public void SplitReturnsNoLinesForEmptyText() => Assert.Empty(TextWrap.Split(string.Empty, 10));

    [Fact]
    public void SplitBreaksOnASpaceAndDropsIt() => Assert.Equal(["one two", "three"], TextWrap.Split("one two three", 10));

    [Fact]
    public void SplitKeepsAPunctuationBreakOnItsOwnRow()
        => Assert.Equal(["one,", "twoooooo"], TextWrap.Split("one,twoooooo", 8));

    [Fact]
    public void SplitNeverExceedsTheRowWidth()
    {
        // Pins the comma sitting one past the row width: must not draw a nine-character row.
        IReadOnlyList<string> lines = TextWrap.Split("aaaaaaaa,bbbb", 8);

        Assert.Equal(["aaaaaaaa", ",bbbb"], lines);
    }

    [Fact]
    public void SplitBreaksAWordLongerThanTheRowMidWord()
        => Assert.Equal(["aaaa", "aaaa", "aa"], TextWrap.Split(new('a', 10), 4));

    [Fact]
    public void SplitRejectsAZeroWidthRow()
        => Assert.Throws<ArgumentOutOfRangeException>(() => TextWrap.Split("abc", 0));

    [Fact]
    public void SplitRejectsNullText() => Assert.Throws<ArgumentNullException>(() => TextWrap.Split(null!, 10));
}
