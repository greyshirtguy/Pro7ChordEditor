// Words: what the layers above the rules may ask of a text box's words.
//
// A text box's words are a plain `string` here, and a place in them is an `int`: .NET's
// strings count in UTF-16 units, which is how the file counts (spec/10-platforms.md,
// "Counting characters"). So there is no type for words, as the Mac app must have
// (mac/Core/Sources/ChordRules/Words.swift). What that type gives the Mac's song is
// here as a few plain functions, so that the song asks the rules what blank means and
// where a line ends, and never .NET: its own notion of white space is not promised to
// be the rules' (Text.cs).

namespace Pro7ChordEditor.ChordRules;

public static class Words
{
    /// <summary>The line break, as it is in the words: one unit, whatever the RTF wrote it as.</summary>
    public const char LineBreak = Text.LineBreak;

    /// <summary>U+200B, the zero-width space: what a chord with no words hangs on.</summary>
    public const char ZeroWidthSpace = Text.ZeroWidthSpace;

    /// <summary>U+2001, the em quad: what stands between two such chords.</summary>
    public const char EmQuad = Text.EmQuad;

    /// <summary>
    /// Whether there is nothing here to read: no units, or none but white space, as the
    /// rules count white space. A zero-width space is not white space, so a line of
    /// stand-ins is not blank.
    /// </summary>
    public static bool IsBlank(string words)
    {
        foreach (var c in words)
        {
            if (!Text.IsSpace(c))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// The lines, parted at each line break. There is always at least one; words that
    /// end with a line break end with an empty line.
    /// </summary>
    public static string[] Lines(string words) => words.Split(LineBreak);

    /// <summary>
    /// Whether these words have a stand-in in them: one of the two characters that
    /// Multitracks writes where a chord has no words to hang on.
    /// </summary>
    public static bool HasAStandIn(string words) =>
        words.Contains(ZeroWidthSpace, StringComparison.Ordinal) || words.Contains(EmQuad, StringComparison.Ordinal);
}
