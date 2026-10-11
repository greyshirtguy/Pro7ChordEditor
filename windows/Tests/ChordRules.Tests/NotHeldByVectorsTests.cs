// Rules that no vector holds, each held here by a case of its own.
//
// How they were found. With every vector passing, each rule was broken in one small way
// in turn (a hundred and sixty-two ways in all, in a clone made for it) and the tests
// run again. Most breakages were caught by a vector. Those here were not: the rule
// could be got wrong and every file of testdata/vectors would still pass. The Mac app's
// session found its own list the same way (mac/NOTES.md, "At M1"), most of which the
// vectors have held since they were made again; these are what breaking the Windows
// port found beyond that.
//
// MINE, NOT VECTORS. What each expects was READ from the reference's code
// (reference/simple-presenter/src/chords.cpp and rtf.cpp; tools/keyfit.py and
// tools/make-fixtures.py for the two new rules), or from the page that states the rule,
// and not got by running anything. If one ever disagrees with a vector made later, the
// vector is right. windows/NOTES.md lists them as cases wanted in the vectors, for both
// apps.

using Xunit;

namespace Pro7ChordEditor.ChordRules.Tests;

public class NotHeldByVectorsTests
{
    // ---- A chord's name

    // chordCharacter: `c.isLetterOrNumber()`. A letter or a number of any script may be
    // written in a chord, each unit asked by itself (spec/02, "To the letter"). The
    // vectors have none from outside ASCII but the signs the rule names one by one.
    [Fact]
    public void ALetterOrANumberOfAnyScriptMayBeWrittenInAChord()
    {
        Assert.True(Chords.CouldBecome("C" + (char)0x03B1));  // a Greek small alpha: a letter
        Assert.True(Chords.CouldBecome("C" + (char)0x00B2));  // a superscript two: a number that is no digit
        Assert.True(Chords.CouldBecome("C" + (char)0x2167));  // a Roman numeral eight: a number that is a letter
        Assert.True(Chords.CouldBecome("C" + (char)0x0661));  // an Arabic-Indic one: a digit
        Assert.True(Chords.IsChord("C" + (char)0x03B1));
        // A letter from beyond the first 65,536 characters (a mathematical bold A) is
        // two units, and neither is a letter.
        Assert.False(Chords.CouldBecome("C" + (char)0xD835 + (char)0xDC00));
        Assert.False(Chords.IsChord("C" + (char)0xD835 + (char)0xDC00));
    }

    // placeIn: `while (off > 6) off -= 12;`. A note exactly six semitones from the note
    // its letter has in the scale is written sharp, six times, and not flat. Nothing
    // sensible is ever so written; it is the edge of "the nearest way round".
    [Fact]
    public void ExactlyHalfWayRoundIsWrittenSharp()
    {
        Assert.Equal("######4", Chords.Notated("F######", "C", (int)Notation.Numbers));
    }

    // ---- A ChordPro file read

    // parseSong: a comment that names a part marks it (`marked = true`), and inside a
    // marked part an empty line is only a gap (spec/09, rule 5). The vectors have a gap
    // inside a part that {start_of_verse} began, and none inside one a comment named.
    [Fact]
    public void AnEmptyLineInsideAPartThatACommentNamedIsOnlyAGap()
    {
        var song = Chords.ParseSong("{c: Verse 1}\nOne\n\nTwo");

        var only = Assert.Single(song.Sections);
        Assert.Equal("Verse 1", only.Name);
        Assert.Equal(["One", "Two"], only.Lines.Select(line => line.Text));
    }

    // headingOf: `( ?\d+[a-z]?)?$`. The one letter that may end a part's name comes
    // after a number ("Verse 2b"). With no number before it, it is no part of a name,
    // and the line is words.
    [Fact]
    public void ALetterWithNoNumberBeforeItDoesNotMakeAHeading()
    {
        var song = Chords.ParseSong("Verse a\nwords");

        var only = Assert.Single(song.Sections);
        Assert.Equal("", only.Name);
        Assert.Equal(["Verse a", "words"], only.Lines.Select(line => line.Text));
        // (And with the number, it is one.)
        Assert.Equal("Verse 2b", Chords.ParseSong("verse 2B\nwords").Sections[0].Name);
    }

    // parseSong: `normal.replace("\r\n", "\n").replace('\r', '\n')`. Line ends of any
    // kind (spec/09, rule 1). The one test song with Windows line ends would read the
    // same without this, a carriage return at a line's end being white space that is
    // trimmed; a file with carriage returns alone would be one line.
    [Fact]
    public void ACarriageReturnAloneEndsALine()
    {
        var song = Chords.ParseSong("Verse 1\rOne\rTwo");

        var only = Assert.Single(song.Sections);
        Assert.Equal("Verse 1", only.Name);
        Assert.Equal(["One", "Two"], only.Lines.Select(line => line.Text));
    }

    // parseSong: `match.captured(2).trimmed()`. The expression itself takes plain spaces
    // off a value's ends, and trimmed() then takes the rest of what is white space: a
    // no-break space, here.
    [Fact]
    public void ADirectivesValueLosesEveryKindOfWhiteSpaceAtItsEnds()
    {
        var song = Chords.ParseSong("{title: " + (char)0x00A0 + "Amazing Grace" + (char)0x00A0 + "}\nwords");

        Assert.Equal("Amazing Grace", song.Title);
    }

    // ---- The words of an RTF

    // Parser::parse, `case '{': ... m_pendingSkip = 0;`. An opening brace ends the
    // passing over of the character that stands in for a numbered one. Here the
    // numbered character is 233, an e with an acute accent, and nothing says how many to
    // pass over, so it is one: without the brace it would be the x.
    [Fact]
    public void AnOpeningBraceEndsThePassingOverAfterANumberedCharacter()
    {
        var words = RtfWords.Read(Rtf("{", "rtf1 ", "u233 {x}y}"));

        Assert.Equal([(char)0x00E9, 'x', 'y'], words.ToCharArray());
    }

    // Parser::controlWord: the nine groups that are not words. The vectors have the font
    // table, the colour table and some of the others; a picture and the two tables of
    // lists are in none of them. (ProPresenter on Windows was seen to write a list
    // table: mac/NOTES.md.)
    [Theory]
    [InlineData("fonttbl")]
    [InlineData("colortbl")]
    [InlineData("info")]
    [InlineData("stylesheet")]
    [InlineData("pict")]
    [InlineData("header")]
    [InlineData("footer")]
    [InlineData("listtable")]
    [InlineData("listoverridetable")]
    public void WhatIsInAGroupThatIsNotWordsIsNotWords(string group)
    {
        Assert.Equal("words", RtfWords.Read(Rtf("{", "rtf1{", group + " junk}words}")));
    }

    // Parser::control: `if (m_state.destination == Destination::Body) endParagraph();`.
    // A backslash at the end of a line is a paragraph break only among the words.
    [Fact]
    public void AParagraphBreakInsideATableIsNotOneInTheWords()
    {
        Assert.Equal("words", RtfWords.Read(Rtf("{", "rtf1{", "fonttbl", "\n}words}")));
    }

    // RtfWords.WithTheOpeningOf reads back what it made, and answers with nothing if
    // that is not the stand-ins. The vectors ask only for zero-width spaces, which
    // always read back. A plain space after an opening that ends in a control word with
    // no space of its own is that word's end, and not text: so it is refused. (The model
    // is an RTF whose one character, an e with an acute accent written as a backslash,
    // an apostrophe and "e9", follows the control word f0 with nothing between.)
    [Fact]
    public void ANewRtfThatWouldNotReadBackIsRefused()
    {
        var model = Rtf("{", "rtf1", "f0", "'e9}");
        Assert.Equal([(char)0x00E9], RtfWords.Read(model).ToCharArray());

        Assert.Null(RtfWords.WithTheOpeningOf(model, " "));
        // (A zero-width space begins with a backslash, and is not taken for anything.)
        var made = RtfWords.WithTheOpeningOf(model, Chords.Placeholders(1));
        Assert.NotNull(made);
        Assert.Equal(Chords.Placeholders(1), RtfWords.Read(made));
    }

    // ---- Which key a song is likely in (tools/keyfit.py)

    // "only if the song has four chords or more": three are not enough to say anything.
    [Fact]
    public void ThreeChordsAreTooFewToSuggestAKeyAndFourAreEnough()
    {
        Assert.Equal("", KeyFit.Likely(["G", "C", "D"], "").Suggested);
        Assert.Equal("G", KeyFit.Likely(["G", "C", "D", "G"], "").Suggested);
    }

    // "some key fits three quarters of them": three of four, exactly, is enough. (G, C
    // and D are G's own; B flat is not, and no key has more than two of the four
    // besides G and its relative minor.)
    [Fact]
    public void AKeyThatFitsExactlyThreeQuartersIsEnough()
    {
        var answer = KeyFit.Likely(["G", "C", "D", "Bb"], "");

        Assert.Equal(0.75, answer.Scores["G"]);
        Assert.Equal(0.75, answer.Scores.Values.Max());
        Assert.Equal("G", answer.Suggested);
    }

    // The Python's expression ends `(.*)$`, and its "." is anything but a line break: a
    // name with a line break inside it is no chord, and is not counted. Three chords and
    // one such name are three chords.
    [Fact]
    public void ANameWithALineBreakInItIsNoChordToTheKeyFit()
    {
        Assert.Equal("", KeyFit.Likely(["C", "C", "C", "C\nx"], "").Suggested);
        Assert.Equal("C", KeyFit.Likely(["C", "C", "C", "C x"], "").Suggested);
    }

    // The Python's expression: `(#+|b+|...)?`, a run of ONE sign. "C#b5" is C sharp with
    // a flattened fifth, not C: its sharp is its note's and its flat is not. Four of
    // them are four chords on D flat's own note.
    [Fact]
    public void ANotesSharpsAreNotFollowedByFlatsInTheKeyFit()
    {
        Assert.Equal("Db", KeyFit.Likely(["C#b5", "C#b5", "C#b5", "C#b5"], "").Suggested);
    }

    // A name is trimmed as Python trims (`strip`), which takes off the four separators
    // U+001C to U+001F as well as what the rest of the rules call white space.
    [Fact]
    public void AChordsNameIsTrimmedAsPythonTrimsForTheKeyFit()
    {
        Assert.Equal("G", KeyFit.Likely([(char)0x001C + "G", "C", "D", "G" + (char)0x001F], "").Suggested);
    }

    // ---- Where the first editor meant a chord to be (tools/make-fixtures.py)

    // "If moving them puts at least four more chords, and at least a quarter more of all
    // those chords, on the starts of words" (spec/08, check 2). Each song here is one
    // text box: a first line, and under it a long word and some words of one letter.
    // A chord saved one short of a small word's start is on a space as it is and on the
    // word if moved; a chord saved inside the long word is on no word's start either
    // way.
    [Theory]
    [InlineData(4, 12, true)]   // four more, of sixteen: a quarter exactly
    [InlineData(4, 13, false)]  // four more, of seventeen: less than a quarter
    [InlineData(3, 9, false)]   // three more, of twelve: a quarter, but not four
    [InlineData(5, 15, true)]
    public void MovingMustPutFourMoreChordsAndAQuarterMoreOnTheStartsOfWords(int onWordsIfMoved, int inTheLongWord, bool likely)
    {
        const string first = "first";
        var longWord = new string('x', 30);
        var words = first + "\n" + longWord + string.Concat(Enumerable.Repeat(" w", onWordsIfMoved));
        var secondLine = first.Length + 1;
        // One short of each small word's start, which is the space before it...
        var starts = Enumerable.Range(0, onWordsIfMoved).Select(n => secondLine + longWord.Length + 2 * n).ToList();
        // ...and places well inside the long word.
        starts.AddRange(Enumerable.Range(0, inTheLongWord).Select(n => secondLine + 2 + n));

        var shift = OldEditor.LineBreakShift([(words, starts)]);

        Assert.Equal(onWordsIfMoved + inTheLongWord, shift.ChordsBelowTheFirstLine);
        Assert.Equal(0, shift.AtTheStartOfAWord);
        Assert.Equal(onWordsIfMoved, shift.IfMovedBack);
        Assert.Equal(likely, shift.Likely);
    }

    // line_break_shift: a text box with no line break in it has no chord below a first
    // line, and shows nothing either way.
    [Fact]
    public void ABoxOfOneLineHasNoChordBelowItsFirstLine()
    {
        var shift = OldEditor.LineBreakShift([("Amazing grace", [0, 8])]);

        Assert.Equal(new OldEditor.Shift(0, 0, 0, false), shift);
    }

    // An RTF from its pieces, with a backslash between each two: Rtf("{", "rtf1 x}") is
    // an opening brace, a backslash, and "rtf1 x}". (So that no backslash need be typed
    // into this file before a letter: windows/NOTES.md says what became of one before a
    // "u".)
    private static byte[] Rtf(params string[] pieces) => System.Text.Encoding.ASCII.GetBytes(string.Join((char)0x5C, pieces));
}
