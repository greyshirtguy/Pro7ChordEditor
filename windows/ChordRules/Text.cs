// What the rules mean by white space, by a letter, and by taking the white space off
// the ends of a piece of text.
//
// The rules were written in C++ for Qt, and the answers in testdata/vectors are what
// Qt's own notions of these gave. .NET has notions of its own under nearly the same
// names, which agree for most text and not for all, so none of them is used here: each
// list is written out, as spec/02-chord-rules.md has it ("To the letter"), and the tests
// hold .NET's list against this one so that it is known where they part.
//
// The Mac app's is mac/Core/Sources/ChordRules/Units.swift. It has much more in it,
// because Swift's strings do not count in UTF-16 units and .NET's do: there a string is
// turned into its units on the way in and back on the way out, and here it is used as
// it is.
//
// The characters that cannot be seen are named here, once, by their numbers.

namespace Pro7ChordEditor.ChordRules;

internal static class Text
{
    /// <summary>The line break, as it is in a text box's words: one unit, whatever the RTF wrote it as.</summary>
    internal const char LineBreak = '\n';

    /// <summary>U+200B, the zero-width space: what a chord with no words hangs on.</summary>
    internal const char ZeroWidthSpace = (char)0x200B;

    /// <summary>U+2001, the em quad: what stands between two such chords.</summary>
    internal const char EmQuad = (char)0x2001;

    /// <summary>U+266F and U+266D, the music signs, which a note may be written with in place of "#" and "b".</summary>
    internal const char MusicSharp = (char)0x266F;
    internal const char MusicFlat = (char)0x266D;

    /// <summary>
    /// Qt's QChar::isSpace: tab to carriage return, space, U+0085, U+00A0, and what
    /// Unicode calls a space, line or paragraph separator. A zero-width space is NOT on
    /// the list, which is why a line of stand-ins is not blank.
    /// </summary>
    internal static bool IsSpace(char c)
    {
        int unit = c;
        return (unit >= 0x0009 && unit <= 0x000D) || unit == 0x0020 || unit == 0x0085 || unit == 0x00A0 || unit == 0x1680
            || (unit >= 0x2000 && unit <= 0x200A) || unit == 0x2028 || unit == 0x2029 || unit == 0x202F || unit == 0x205F
            || unit == 0x3000;
    }

    /// <summary>
    /// White space as Python counts it, for the two rules whose reference is Python
    /// (KeyFit.cs, OldEditor.cs): Qt's list, and the four separators U+001C to U+001F.
    /// </summary>
    internal static bool IsPythonSpace(char c) => IsSpace(c) || (c >= (char)0x001C && c <= (char)0x001F);

    /// <summary>
    /// Qt's QChar::isLetterOrNumber: a unit whose Unicode category is one of the letters
    /// or one of the numbers. Each unit is asked by itself, so a letter from beyond the
    /// first 65,536 characters is neither: it is two units. (Not char.IsLetterOrDigit,
    /// which leaves two kinds of number out.)
    /// </summary>
    internal static bool IsLetterOrNumber(char c) => char.IsLetter(c) || char.IsNumber(c);

    internal static bool IsAsciiLetter(char c) => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');

    internal static bool IsAsciiDigit(char c) => c >= '0' && c <= '9';

    /// <summary>The small letter of a capital A to Z; anything else as it is.</summary>
    internal static char AsciiLowered(char c) => c >= 'A' && c <= 'Z' ? (char)(c + 0x20) : c;

    /// <summary>The capital of a small letter a to z; anything else as it is.</summary>
    internal static char AsciiRaised(char c) => c >= 'a' && c <= 'z' ? (char)(c - 0x20) : c;

    /// <summary>QString::trimmed: without the white space at its ends.</summary>
    internal static string Trimmed(string text) => Trimmed(text, IsSpace);

    /// <summary>Python's str.strip.</summary>
    internal static string PythonStripped(string text) => Trimmed(text, IsPythonSpace);

    private static string Trimmed(string text, Func<char, bool> isSpace)
    {
        var start = 0;
        var end = text.Length;
        while (start < end && isSpace(text[start]))
        {
            start++;
        }
        while (end > start && isSpace(text[end - 1]))
        {
            end--;
        }
        return text[start..end];
    }

    /// <summary>
    /// QString::mid: from a place to the end, or so many units of it. What is asked for
    /// beyond the ends is not there, where .NET's Substring would stop the program.
    /// </summary>
    internal static string Mid(string text, int from, int length = int.MaxValue)
    {
        var start = Math.Clamp(from, 0, text.Length);
        var end = length >= text.Length - start ? text.Length : Math.Max(start, start + length);
        return text[start..end];
    }

    /// <summary>QString::left: the first so many units.</summary>
    internal static string Left(string text, int length) => Mid(text, 0, length);
}
