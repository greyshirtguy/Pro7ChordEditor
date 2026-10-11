// What the rules mean by white space and by a letter, held against .NET's own notions
// of them over every one of the 65,536 units.
//
// spec/02-chord-rules.md, "To the letter": "Hold the platform's own list against this
// one before using it." The rules use their own lists (ChordRules/Text.cs) and never
// .NET's, so nothing here can make a rule wrong. These tests are a record of where the
// two agree and where they part, so that it is known, and so that a later .NET or a
// later Unicode that moved either would be noticed here and not in a song.
//
// Mine, not vectors. The lists are spec/02's, each character by its number.

using System.Globalization;
using Xunit;

namespace Pro7ChordEditor.ChordRules.Tests;

public class TextTests
{
    // Qt's list, as spec/02 writes it out: "U+0009 to U+000D, U+0020, U+0085, U+00A0,
    // U+1680, U+2000 to U+200A, U+2028, U+2029, U+202F, U+205F and U+3000, and nothing
    // else."
    private static readonly int[] WhiteSpace =
    [
        0x0009, 0x000A, 0x000B, 0x000C, 0x000D, 0x0020, 0x0085, 0x00A0, 0x1680,
        0x2000, 0x2001, 0x2002, 0x2003, 0x2004, 0x2005, 0x2006, 0x2007, 0x2008, 0x2009, 0x200A,
        0x2028, 0x2029, 0x202F, 0x205F, 0x3000,
    ];

    [Fact]
    public void WhiteSpaceIsTheListOfThePageAndNothingElse()
    {
        var ours = Enumerable.Range(0, 0x10000).Where(unit => Text.IsSpace((char)unit)).ToArray();

        Assert.Equal(WhiteSpace, ours);
        // A zero-width space is not white space: a line of stand-ins is not blank, and
        // trimming does not empty it. (It is white space to Apple's Foundation.)
        Assert.DoesNotContain(0x200B, ours);
        Assert.Equal(((char)0x200B).ToString(), Text.Trimmed(((char)0x200B).ToString()));
    }

    // As it happens .NET's list IS Qt's, in the .NET this was built with. The rules do
    // not lean on that; this says so if it ever stops being true.
    [Fact]
    public void DotNetsOwnWhiteSpaceIsTheSameListToday()
    {
        var theirs = Enumerable.Range(0, 0x10000).Where(unit => char.IsWhiteSpace((char)unit)).ToArray();

        Assert.Equal(WhiteSpace, theirs);
    }

    // Python's, for the two rules whose reference is Python: Qt's list and the four
    // separators U+001C to U+001F.
    [Fact]
    public void PythonsWhiteSpaceIsThatListAndFourMore()
    {
        var ours = Enumerable.Range(0, 0x10000).Where(unit => Text.IsPythonSpace((char)unit)).ToArray();

        Assert.Equal(WhiteSpace.Concat([0x001C, 0x001D, 0x001E, 0x001F]).Order().ToArray(), ours);
    }

    // "A letter or a number ... is a UTF-16 unit whose Unicode category is one of the
    // letters (Lu, Ll, Lt, Lm, Lo) or one of the numbers (Nd, Nl, No)."
    [Fact]
    public void ALetterOrANumberGoesByTheUnitsOwnCategory()
    {
        UnicodeCategory[] lettersAndNumbers =
        [
            UnicodeCategory.UppercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.TitlecaseLetter,
            UnicodeCategory.ModifierLetter, UnicodeCategory.OtherLetter,
            UnicodeCategory.DecimalDigitNumber, UnicodeCategory.LetterNumber, UnicodeCategory.OtherNumber,
        ];
        for (var unit = 0; unit < 0x10000; unit++)
        {
            var expected = lettersAndNumbers.Contains(char.GetUnicodeCategory((char)unit));
            Assert.True(Text.IsLetterOrNumber((char)unit) == expected, $"U+{unit:X4}");
        }
        // Half of a pair is neither, so a letter from beyond the first 65,536 characters
        // is no letter here: it is two units.
        Assert.False(Text.IsLetterOrNumber((char)0xD835));
        Assert.False(Text.IsLetterOrNumber((char)0xDC00));
        // The two kinds of number that char.IsLetterOrDigit leaves out, and this does not:
        // a Roman numeral (U+2167) and a superscript two (U+00B2).
        Assert.True(Text.IsLetterOrNumber((char)0x2167));
        Assert.True(Text.IsLetterOrNumber((char)0x00B2));
        Assert.False(char.IsLetterOrDigit((char)0x2167));
    }

    // QString's mid and left give what is there and no more, where .NET's Substring
    // stops the program when asked for what is not.
    [Fact]
    public void MidAndLeftGiveWhatIsThereAndNoMore()
    {
        Assert.Equal("cde", Text.Mid("abcde", 2));
        Assert.Equal("cd", Text.Mid("abcde", 2, 2));
        Assert.Equal("cde", Text.Mid("abcde", 2, 99));
        Assert.Equal("", Text.Mid("abcde", 5));
        Assert.Equal("", Text.Mid("abcde", 9));
        Assert.Equal("", Text.Mid("abcde", 2, 0));
        Assert.Equal("ab", Text.Left("abcde", 2));
        Assert.Equal("abcde", Text.Left("abcde", 99));
    }
}
