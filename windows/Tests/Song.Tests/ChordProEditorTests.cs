// The ChordPro editor with no window in it (spec/04-chordpro-editor.md): what may be
// typed, the help with brackets, and what a pause in the typing saves.
//
// There are no vectors for these: the rules live in the reference's QML. The cases are
// those of its scripted test (reference/simple-presenter/tests/chords-scripted-test.qml,
// "ChordPro": typing over a word, into a group's name, a chord in each kind of line, a
// paste), and the page's rules one by one, as the Mac's tests of the same hold them
// (ChordProEditorTests.swift). All are MINE, NOT A VECTOR.
//
// Plain Song, as the editor has it, line for line:
//
//    0  {c: Verse 1}
//    1  [G]One two three [D]four
//    2  Five six [Em]seven eight
//    3
//    4  [C]Nine ten e[G/B]leven twelve
//    5  Thirteen
//    6
//    7  {c: Chorus}
//    8  [C]Sing it [G]out
//    9  [D]Sing it loud
//   10
//   11  {c: Outro}
//   12  [G] [D/F#] [Em]

using Google.Protobuf;
using Pro.SerializationInterop.RVProtoData;
using Pro7ChordEditor.ChordRules;
using Pro7ChordEditor.Tests;
using Xunit;
using CustomAttribute = Pro.SerializationInterop.RVProtoData.Graphics.Types.Text.Types.Attributes.Types.CustomAttribute;

namespace Pro7ChordEditor.Song.Tests;

public sealed class ChordProEditorTests : IDisposable
{
    private const string Plain = "Plain Song.pro";
    private const string Blank = "Blank Slides.pro";
    private static readonly HashSet<string> NoNames = [];
    private readonly Workbench bench = new();

    public void Dispose() => bench.Dispose();

    private static string Lines(params string[] lines) => string.Join('\n', lines);

    // The text with the first of something in it replaced: what typing there leaves.
    private static string With(string old, string made, string text)
    {
        var at = text.IndexOf(old, StringComparison.Ordinal);
        Assert.True(at >= 0, "the text has no \"" + old + "\" in it");
        return text[..at] + made + text[(at + old.Length)..];
    }

    // Types: asks the editor to take its text with the first `old` in it made `made`.
    private static ChordProEditor.Verdict Type(ChordProEditor editor, string old, string made) => editor.Take(With(old, made, editor.Text));

    private static ChordProEditor.Verdict TypeOnLine(ChordProEditor editor, int number, string text)
    {
        var lines = editor.Text.Split('\n');
        lines[number] = text;
        return editor.Take(Lines(lines));
    }

    // A box of a file on disk as "place:name" for each chord.
    private static IEnumerable<string> Said(IEnumerable<Chord> chords) => chords.Select(chord => chord.At + ":" + chord.Name);

    private static IEnumerable<string> ChordsOf(int box, string path) => Said(new OpenSong(path).TextBoxes[box].Chords);

    private static string PathOf(string testFile) =>
        Path.Combine(TestData.Root, "files", TestData.PresentationPaths().Single(path => path.EndsWith("/" + testFile, StringComparison.Ordinal)).Replace('/', Path.DirectorySeparatorChar));

    // Plain Song with one thing in its message changed, opened to be changed.
    private OpenSong PlainSongWith(string name, Action<Presentation, IReadOnlyList<TextBox>> change)
    {
        var made = Workbench.Read(PathOf(Plain));
        change(made, new OpenSong(PathOf(Plain)).TextBoxes);
        return bench.OpenAt(bench.FileOf(name, made.ToByteArray()));
    }

    private static bool IsAChord(CustomAttribute entry) => entry.AttributeCase == CustomAttribute.AttributeOneofCase.Chord;

    // Gives the n-th chord of a box another name, in the message.
    private static void Rename(int n, string element, string name, Presentation made) =>
        Workbench.TextOf(made, element).Attributes.CustomAttributes.Where(IsAChord).ElementAt(n).Chord = name;

    // ---- The text

    public static TheoryData<string> EveryTestFile() => [.. TestData.PresentationPaths()];

    [Theory]
    [MemberData(nameof(EveryTestFile))]
    public void TheTextIsTheSongsLinesAndPassesItsOwnRule(string path)
    {
        var song = new OpenSong(Path.Combine(TestData.Root, "files", path.Replace('/', Path.DirectorySeparatorChar)));
        var editor = new ChordProEditor(song);
        Assert.Equal(SongText.Text(song.TextBoxes), editor.Text);
        Assert.Equal(SongText.Lines(song.TextBoxes), editor.Lines);
        Assert.Null(editor.Barred);
        Assert.Equal(ChordProEditor.Verdict.Taken, editor.Take(editor.Text));
        // Every bracket in it is a chord `ChordsOf` reads, at the same place.
        foreach (var line in editor.Lines.Where(line => line.Kind is SongLineKind.Words or SongLineKind.ChordsAlone))
        {
            var pairs = ChordProEditor.Pairs(line.Text);
            var read = Chords.ChordsOf(line.Text);
            Assert.Equal(read.Select(chord => chord.At), pairs.Select(pair => pair.At));
            Assert.Equal(read.Select(chord => chord.Name), pairs.Select(pair => Chords.Tidied(line.Text[(pair.Open + 1)..pair.Close])));
        }
    }

    // ---- What may be typed (rule 6)

    [Fact]
    public void TheWordsCannotBeTypedOverAddedToOrTakenFrom()
    {
        var editor = new ChordProEditor(bench.Open(Plain));
        var before = editor.Text;
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(editor, "Thirteen", "Thirxteen"));
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(editor, "Thirteen", "Thirten"));
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(editor, "Thirteen", "thirteen"));
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(editor, "six ", "six  "));
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(editor, "Sing it loud", ""));
        Assert.Equal(before, editor.Text);
    }

    [Fact]
    public void AGroupsNameAndTheGapBetweenSlidesTakeNothing()
    {
        var editor = new ChordProEditor(bench.Open(Plain));
        var before = editor.Text;
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(editor, "{c: Chorus}", "{c: Chorus 2}"));
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(editor, "{c: Chorus}", "{c: Chorus}[G]"));
        // The gap before the chorus: the empty line after "Thirteen".
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(editor, "Thirteen\n\n", "Thirteen\n[G]\n"));
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(editor, "Thirteen\n\n", "Thirteen\n \n"));
        Assert.Equal(before, editor.Text);
    }

    [Fact]
    public void TheNumberOfLinesCannotChange()
    {
        var editor = new ChordProEditor(bench.Open(Plain));
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(editor, "Thirteen", "Thirteen\n"));
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(editor, "four\nFive", "fourFive"));
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(editor, "four\nFive", "four\n\nFive"));
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, editor.Take(""));
        // Not at the end either, where every line before is still what it was: a line
        // more, with or without anything on it, or the last line gone.
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, editor.Take(editor.Text + "\n"));
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, editor.Take(editor.Text + "\n[G]"));
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, editor.Take(Lines(editor.Text.Split('\n')[..^1])));
        // (A line break as Windows writes one is two characters, and is not let through:
        // the text box is to hand over one for each line's end.)
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, editor.Take(editor.Text.Replace("\n", "\r\n", StringComparison.Ordinal)));
    }

    [Fact]
    public void AChordMayBeTypedAmongTheWordsACharacterAtATime()
    {
        var editor = new ChordProEditor(bench.Open(Plain));
        // "[" brings its "]" with it (the help below); then the name, a character at a
        // time, each of which is let through.
        foreach (var (old, made) in new[]
        {
            ("Five six", "Five []six"), ("Five []six", "Five [F]six"), ("Five [F]six", "Five [F#]six"), ("Five [F#]six", "Five [F#m]six"),
            ("Five [F#m]six", "Five [F#m7]six"), ("Five [F#m7]six", "Five [F#m7/]six"), ("Five [F#m7/]six", "Five [F#m7/A]six"),
        })
        {
            Assert.Equal(ChordProEditor.Verdict.Taken, Type(editor, old, made));
        }
        Assert.Contains("Five [F#m7/A]six [Em]seven eight", editor.Text, StringComparison.Ordinal);
        // And taken out again, whole.
        Assert.Equal(ChordProEditor.Verdict.Taken, Type(editor, "[F#m7/A]", ""));
        // One after the last word, and one before the first.
        Assert.Equal(ChordProEditor.Verdict.Taken, Type(editor, "Thirteen", "[D]Thirteen[G]"));
        // Spaces round a name are let through (it is trimmed), and a small letter.
        Assert.Equal(ChordProEditor.Verdict.Taken, Type(editor, "Sing it loud", "Sing [ a ]it loud"));
    }

    [Fact]
    public void WhatIsInBracketsMustBeAbleToBecomeAChord()
    {
        var editor = new ChordProEditor(bench.Open(Plain));
        var before = editor.Text;
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(editor, "Five six", "Five [x]six"));
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(editor, "Five six", "Five [H]six"));
        // ("Coda" starts as a C chord would.)
        Assert.Equal(ChordProEditor.Verdict.Taken, Type(editor, "Five six", "Five [Coda]six"));
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(editor, "[Coda]", "[*Coda]"));
        Assert.Equal(ChordProEditor.Verdict.Taken, Type(editor, "[Coda]", ""));
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(editor, "[G/B]", "[G/B*]"));
        Assert.Equal(before, editor.Text);
    }

    [Fact]
    public void ABracketWithNoOtherOfItsPairIsNotLetThrough()
    {
        var editor = new ChordProEditor(bench.Open(Plain));
        var before = editor.Text;
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(editor, "Five six", "Five [six"));
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(editor, "Five six", "Five ]six"));
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(editor, "Five six", "Five [[G]six"));
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(editor, "Five six", "Five [G]]six"));
        // Brackets that would reach over a line break are two strays.
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(editor, "four\nFive", "four[G\n]Five"));
        Assert.Equal(before, editor.Text);
    }

    [Fact]
    public void APasteIsHeldToTheSameRuleTheWholeTextAtOnceWithChordsOrWithOtherWords()
    {
        var editor = new ChordProEditor(bench.Open(Plain));
        var pasted = Lines(
            "{c: Verse 1}",
            "[Em]One [C]two [G]three [D]four",
            "[Em]Five [C]six seven [D]eight",
            "",
            "Nine ten eleven twelve",
            "Thir[A]teen",
            "",
            "{c: Chorus}",
            "Sing it out",
            "Sing it [D]loud[G]",
            "",
            "{c: Outro}",
            "[C] [G] [D]");
        Assert.Equal(ChordProEditor.Verdict.Taken, editor.Take(pasted));
        Assert.Equal(pasted, editor.Text);
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, editor.Take(pasted.Replace("Nine ten", "Ten nine", StringComparison.Ordinal)));
        Assert.Equal(pasted, editor.Text);
    }

    [Fact]
    public void WhatIsTypedIsHeldAgainstTheWordsUnitForUnitNotAsTextThatLooksTheSame()
    {
        // One "e" with an acute accent written as one character, and as two.
        var one = "caf" + (char)0xE9;
        var two = "cafe" + (char)0x301;
        // (The same letter to anyone reading, and to Unicode once both are written its
        // one usual way; and not the same units.)
        Assert.Equal(one.Normalize(System.Text.NormalizationForm.FormC), two.Normalize(System.Text.NormalizationForm.FormC));
        Assert.NotEqual(one, two);
        IReadOnlyList<SongLine> line = [new SongLine(SongLineKind.Words, one, 0) { Box = 0, Element = "x", Words = one }];
        Assert.True(ChordProEditor.Allows(one, line, NoNames));
        Assert.True(ChordProEditor.Allows("[G]" + one, line, NoNames));
        Assert.False(ChordProEditor.Allows(two, line, NoNames));
    }

    [Fact]
    public void ALineOfChordsAloneItsChordsMayBeRenamedWithSpacesBetweenAndNothingElse()
    {
        var editor = new ChordProEditor(bench.Open(Plain));
        Assert.Equal(ChordProEditor.Verdict.Taken, Type(editor, "[G] [D/F#] [Em]", "[G] [] [Em]"));
        Assert.Equal(ChordProEditor.Verdict.Taken, Type(editor, "[G] [] [Em]", "[G] [D] [Em]"));
        Assert.Equal(ChordProEditor.Verdict.Taken, Type(editor, "[G] [D] [Em]", "[G]   [D]\t[Em] "));
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(editor, "[G]   [D]", "[G] x [D]"));
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(editor, "[G]   [D]", "[G] [x] [D]"));
    }

    [Fact]
    public void ALineOfChordsAloneTakesOneMoreChordAndOneFewerAndABlankSlidesLineTakesItsFirst()
    {
        var editor = new ChordProEditor(bench.Open(Plain));
        Assert.Equal(ChordProEditor.Verdict.Taken, Type(editor, "[G] [D/F#] [Em]", "[G] [D/F#] [Em] [C]"));
        Assert.Equal(ChordProEditor.Verdict.Taken, Type(editor, "[G] [D/F#] [Em] [C]", "[G] [Em] [C]"));
        Assert.Equal(ChordProEditor.Verdict.Taken, Type(editor, "[G] [Em] [C]", "[G] [Em] [C] []"));
        Assert.Equal(ChordProEditor.Verdict.Taken, Type(editor, "[G] [Em] [C] []", ""));
        // What is not a chord there is still not let through.
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(editor, "three [D]four", "three [D]four x"));

        // Blank Slides: line 1 is its intro, a blank slide; line 2 the gap after it.
        var blank = new ChordProEditor(bench.Open(Blank));
        Assert.Equal((SongLineKind.BlankSlide, SongLineKind.Gap), (blank.Lines[1].Kind, blank.Lines[2].Kind));
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, TypeOnLine(blank, 1, "A"));
        Assert.Equal(ChordProEditor.Verdict.Taken, TypeOnLine(blank, 1, "[A]"));
        Assert.Equal(ChordProEditor.Verdict.Taken, TypeOnLine(blank, 1, ""));
        // The line after it is the gap, and takes nothing whatever is built.
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, TypeOnLine(blank, 2, "[A]"));
        // The last slide has no text box at all, and is a line of the text all the same.
        Assert.Equal((SongLineKind.BlankSlide, ""), (blank.Lines[^1].Kind, blank.Lines[^1].Element));
    }

    // Chords With No Words, typed in: one more chord on a line as Multitracks writes it,
    // the last chords taken off a line under words, and a first chord on a blank slide
    // of each kind. After each saving the text is what the song then reads as, and all
    // of it is taken back to the byte.
    [Fact]
    public void TheShapesOfRealSongsTypedIn()
    {
        var song = bench.Open("Chords With No Words.pro");
        var original = File.ReadAllBytes(song.Path);
        var editor = new ChordProEditor(song);
        Assert.Null(editor.Barred);
        string TextAfresh() => new ChordProEditor(song).Text;

        // The fourth slide: four chords, each on a stand-in of its own.
        Assert.Equal(ChordProEditor.Verdict.Taken, Type(editor, "[E] [B] [C#m] [A]", "[E] [B] [C#m] [A] [F#m]"));
        Assert.True(editor.Save());
        Assert.Equal(Chords.Placeholders(5), song.TextBoxes[3].Words);
        Assert.Equal(["E", "B", "C#m", "A", "F#m"], song.TextBoxes[3].Chords.Select(chord => chord.Name));
        Assert.Equal(TextAfresh(), editor.Text);

        // The eighth: two chords on plain spaces, under a line of words that has two of
        // its own. Both taken off: the line is one zero-width space and a line of chords
        // alone still, and the words keep theirs. A chord typed on it again is saved on
        // it.
        var under = editor.Lines.ToList().FindIndex(line => line.Slide == 7 && line.Kind == SongLineKind.ChordsAlone);
        Assert.Equal("[B] [E]", editor.Text.Split('\n')[under]);
        Assert.Equal(ChordProEditor.Verdict.Taken, TypeOnLine(editor, under, ""));
        Assert.True(editor.Save());
        Assert.Equal(["Naked come to thee for dress", Chords.Placeholders(1)], Words.Lines(song.TextBoxes[7].Words));
        Assert.Equal([new Chord(0, "E"), new Chord(11, "A")], song.TextBoxes[7].Chords);
        Assert.Equal((SongLineKind.ChordsAlone, ""), (editor.Lines[under].Kind, editor.Text.Split('\n')[under]));
        Assert.Equal(TextAfresh(), editor.Text);
        Assert.Equal(ChordProEditor.Verdict.Taken, TypeOnLine(editor, under, "[B7]"));
        Assert.True(editor.Save());
        Assert.Equal(["Naked come to thee for dress", Chords.Placeholders(1)], Words.Lines(song.TextBoxes[7].Words));
        Assert.Equal([new Chord(0, "E"), new Chord(11, "A"), new Chord(29, "B7")], song.TextBoxes[7].Chords);
        Assert.Equal(TextAfresh(), editor.Text);

        // The ninth: three chords over two lines of words, each of which has one. One
        // more over them, and the two below it have moved on with their words.
        Assert.Equal(ChordProEditor.Verdict.Taken, Type(editor, "[A] [B] [E]", "[A] [B] [E] [B7]"));
        Assert.True(editor.Save());
        Assert.Equal(
            [new Chord(0, "A"), new Chord(2, "B"), new Chord(4, "E"), new Chord(6, "B7"), new Chord(13, "C#m"), new Chord(35, "A")],
            song.TextBoxes[8].Chords);

        // The first slide is blank and has a text box; the last has none.
        var first = editor.Lines.ToList().FindIndex(line => line.Kind == SongLineKind.BlankSlide);
        var last = editor.Lines.ToList().FindLastIndex(line => line.Kind == SongLineKind.BlankSlide);
        Assert.Equal((0, 11, ""), (editor.Lines[first].Slide, editor.Lines[last].Slide, editor.Lines[last].Element));
        Assert.Equal(ChordProEditor.Verdict.Taken, TypeOnLine(editor, first, "[E] [A]"));
        Assert.Equal(ChordProEditor.Verdict.Taken, TypeOnLine(editor, last, "[B]"));
        Assert.True(editor.Save());
        Assert.Equal(Chords.Placeholders(2), song.TextBoxes[0].Words);
        Assert.Equal(["E", "A"], song.TextBoxes[0].Chords.Select(chord => chord.Name));
        Assert.Equal(Chords.Placeholders(1), song.TextBoxes[11].Words);
        Assert.Equal(["B"], song.TextBoxes[11].Chords.Select(chord => chord.Name));
        Assert.NotEqual("", song.TextBoxes[11].Element);
        Assert.Equal((SongLineKind.ChordsAlone, SongLineKind.ChordsAlone), (editor.Lines[first].Kind, editor.Lines[last].Kind));
        Assert.Equal(TextAfresh(), editor.Text);
        Assert.Equal(Said(new OpenSong(song.Path).TextBoxes.SelectMany(box => box.Chords)), Said(song.TextBoxes.SelectMany(box => box.Chords)));
        Assert.Equal(new OpenSong(song.Path).TextBoxes.Select(box => box.Words), song.TextBoxes.Select(box => box.Words));

        // A change of mind about the last slide: its line emptied, the text box that was
        // added for it goes too.
        Assert.Equal(ChordProEditor.Verdict.Taken, TypeOnLine(editor, last, ""));
        Assert.True(editor.Save());
        Assert.True(song.TextBoxes[11].Blank);
        Assert.Equal("", song.TextBoxes[11].Element);
        Assert.Equal(SongLineKind.BlankSlide, editor.Lines[last].Kind);

        while (song.CanUndo)
        {
            song.Undo();
        }
        Assert.Equal(original, File.ReadAllBytes(song.Path));
    }

    // ---- Different from the reference, on purpose (rule 22)

    [Fact]
    public void ANameTheSongAlreadyHadThatIsNoChordIsLetThroughAsItIsAndSavedBack()
    {
        var song = PlainSongWith("Bars.pro", (made, boxes) => Rename(0, boxes[0].Element, "C /// | Am ///", made));
        var editor = new ChordProEditor(song);
        Assert.Null(editor.Barred);
        Assert.Contains("[C /// | Am ///]One two three [D]four", editor.Text, StringComparison.Ordinal);
        // Something else in its box is changed, and it is saved back as it was.
        Assert.Equal(ChordProEditor.Verdict.Taken, Type(editor, "Five six", "Five [A]six"));
        Assert.True(editor.Save());
        Assert.Equal(["0:C /// | Am ///", "14:D", "24:A", "28:Em"], ChordsOf(0, song.Path));
        // Only that name, exactly: another that is no chord is still not let through, and
        // it can be taken out and put back.
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(editor, "[C /// | Am ///]", "[C /// | Am //]"));
        Assert.Equal(ChordProEditor.Verdict.Taken, Type(editor, "[C /// | Am ///]", ""));
        Assert.Equal(ChordProEditor.Verdict.Taken, Type(editor, "One two", "[C /// | Am ///]One two"));
    }

    // ---- Words that have brackets of their own (rules 20 and 20a)

    [Fact]
    public void ASongWhoseWordsHaveSquareBracketsIsShownAndCannotBeChangedHereAndSaysSo()
    {
        var song = PlainSongWith("Brackets.pro", (made, boxes) =>
        {
            var text = Workbench.TextOf(made, boxes[1].Element);
            var rtf = text.RtfData.ToStringUtf8();
            Assert.Contains("Thirteen", rtf, StringComparison.Ordinal);
            text.RtfData = ByteString.CopyFromUtf8(rtf.Replace("Thirteen", "Thir[teen]", StringComparison.Ordinal));
        });
        var editor = new ChordProEditor(song);
        Assert.Contains("Thir[teen]", editor.Text, StringComparison.Ordinal);
        // (The page's sentence, less its last, which waits for the chord sheet: rule 20.)
        Assert.Equal("This song's words have square brackets of their own in them, so it cannot be changed as ChordPro text.", editor.Barred);
        var before = File.ReadAllBytes(song.Path);
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(editor, "Five six", "Five [A]six"));
        Assert.False(editor.Save());
        Assert.Equal(before, File.ReadAllBytes(song.Path));

        // One bracket of either kind is enough, with no other of its pair.
        foreach (var (name, bracketed) in new[] { ("Opened.pro", "Thir[teen"), ("Closed.pro", "Thir]teen") })
        {
            var one = new ChordProEditor(PlainSongWith(name, (made, boxes) =>
            {
                var text = Workbench.TextOf(made, boxes[1].Element);
                text.RtfData = ByteString.CopyFromUtf8(text.RtfData.ToStringUtf8().Replace("Thirteen", bracketed, StringComparison.Ordinal));
            }));
            Assert.Contains(bracketed, one.Text, StringComparison.Ordinal);
            Assert.Equal(editor.Barred, one.Barred);
            Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(one, "Five six", "Five [A]six"));
        }
    }

    // An RTF from its pieces, with a backslash between each two. (So that no backslash
    // need be typed into this file before a letter.)
    private static ByteString Rtf(params string[] pieces) => ByteString.CopyFrom(System.Text.Encoding.ASCII.GetBytes(string.Join((char)0x5C, pieces)));

    private static void RemoveChords(Graphics.Types.Text text)
    {
        var others = text.Attributes.CustomAttributes.Where(entry => !IsAChord(entry)).ToList();
        text.Attributes.CustomAttributes.Clear();
        text.Attributes.CustomAttributes.AddRange(others);
    }

    // A line of words with nothing on it, between two that have words. It is a line of
    // words still, and not a line of chords alone: with every chord taken out it is the
    // nothing it was. But a chord typed on it has no character to hang on (rule 16), and
    // is left in the text and never saved. (As the Mac does. No test file has such a
    // line, and nobody has said whether a real song does.)
    [Fact]
    public void AChordTypedOnALineOfWordsThatHasNoCharactersIsLetThroughAndIsNotSaved()
    {
        var song = PlainSongWith("Empty line.pro", (made, boxes) =>
        {
            var text = Workbench.TextOf(made, boxes[1].Element);
            text.RtfData = Rtf("{", "rtf1 One", "par ", "par Two}");
            RemoveChords(text);
        });
        Assert.Equal("One\n\nTwo", song.TextBoxes[1].Words);
        var editor = new ChordProEditor(song);
        Assert.Null(editor.Barred);
        var own = editor.Lines.Select((line, n) => (line, n)).Where(one => one.line.Box == 1).ToList();
        Assert.Equal([SongLineKind.Words, SongLineKind.Words, SongLineKind.Words], own.Select(one => one.line.Kind));
        Assert.Equal(["One", "", "Two"], own.Select(one => one.line.Text));
        var file = File.ReadAllBytes(song.Path);
        Assert.Equal(ChordProEditor.Verdict.Taken, TypeOnLine(editor, own[1].n, "[G]"));
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, TypeOnLine(editor, own[1].n, "[G]x"));
        Assert.False(editor.Save());
        Assert.Equal(file, File.ReadAllBytes(song.Path));
        Assert.Equal("[G]", editor.Text.Split('\n')[own[1].n]);
        // The lines beside it take theirs as any line does.
        Assert.Equal(ChordProEditor.Verdict.Taken, TypeOnLine(editor, own[2].n, "[D]Two"));
        Assert.True(editor.Save());
        Assert.Equal(["5:D"], ChordsOf(1, song.Path));
    }

    // Half a character among the words: one unit of a character that is two, with no
    // other half. A .NET string holds it as it is, and so does WPF's text box (tried), so
    // the text made from such a song is the song's words unit for unit, and by rule 20a
    // as it is written the song is not barred here. (On the Mac it is: a Swift string
    // cannot hold half a character. NOTES.md has this for the maintainer to say.)
    [Fact]
    public void WordsWithHalfACharacterInThemAreKeptUnitForUnitAndTheSongCanBeTypedIn()
    {
        var half = (char)0xD83D;
        var song = PlainSongWith("Half.pro", (made, boxes) =>
        {
            var text = Workbench.TextOf(made, boxes[1].Element);
            text.RtfData = Rtf("{", "rtf1 Thir", "u-10179?teen}");
            RemoveChords(text);
        });
        Assert.Equal("Thir" + half + "teen", song.TextBoxes[1].Words);
        var editor = new ChordProEditor(song);
        Assert.Null(editor.Barred);
        Assert.Contains("Thir" + half + "teen", editor.Text, StringComparison.Ordinal);
        var rtf = Workbench.TextOf(Workbench.Read(song.Path), song.TextBoxes[1].Element).RtfData;
        // A chord before the half, and one after it: the half is one place.
        Assert.Equal(ChordProEditor.Verdict.Taken, Type(editor, "Thir" + half + "teen", "[A]Thir" + half + "[E]teen"));
        // The half itself cannot be typed over, nor completed.
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(editor, half.ToString(), "x"));
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(editor, half.ToString(), half.ToString() + (char)0xDE00));
        Assert.True(editor.Save());
        Assert.Equal(["0:A", "5:E"], ChordsOf(1, song.Path));
        // The words are as they were, to the byte.
        Assert.Equal(rtf, Workbench.TextOf(Workbench.Read(song.Path), song.TextBoxes[1].Element).RtfData);
    }

    [Fact]
    public void ASongWithAChordWhoseNameHasABracketInItCannotBeChangedHereEitherAndSaysSo()
    {
        var song = PlainSongWith("Name.pro", (made, boxes) => Rename(0, boxes[2].Element, "A]", made));
        var editor = new ChordProEditor(song);
        Assert.Equal(
            "This song cannot be changed as ChordPro text: one of its chords has a square bracket or a line break in its name, or its words have a broken character in them.",
            editor.Barred);
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Type(editor, "Five six", "Five [A]six"));
        Assert.False(editor.Save());
    }

    [Fact]
    public void TheLineOfHelpIsThePages()
    {
        Assert.Equal(
            "Type chords in square brackets among the words: [G]  [D/F#]  [Am7].  Only the chords can be changed here; the words are the slides'.",
            ChordProEditor.Help);
    }

    // ---- Saving (rules 13 to 18)

    [Fact]
    public void AChordTypedIsInTheFileAfterASaveAtItsPlaceAmongTheWordsAndNothingTypedSavesNothing()
    {
        var song = bench.Open(Plain);
        var editor = new ChordProEditor(song);
        var original = File.ReadAllBytes(song.Path);

        Assert.False(editor.Save());
        Assert.Equal(original, File.ReadAllBytes(song.Path));
        Assert.Empty(bench.Copies);
        Assert.False(song.CanUndo);

        Type(editor, "One two", "One [A]two");
        Assert.True(editor.Save());
        Assert.Equal(["0:G", "4:A", "14:D", "28:Em"], ChordsOf(0, song.Path));
        // A copy of the file as it was is kept first, and the text is left as typed.
        Assert.Equal(original, Assert.Single(bench.Copies).Bytes);
        Assert.Contains("[G]One [A]two three [D]four", editor.Text, StringComparison.Ordinal);
        // Saved, there is nothing more to save.
        var saved = File.ReadAllBytes(song.Path);
        Assert.False(editor.Save());
        Assert.Equal(saved, File.ReadAllBytes(song.Path));
        // The other boxes are as they were.
        for (var box = 1; box <= 3; box++)
        {
            Assert.Equal(Said(song.TextBoxes[box].Chords), ChordsOf(box, song.Path));
        }
        Assert.Equal(["0:C", "10:G/B"], ChordsOf(1, song.Path));
    }

    [Fact]
    public void AChordTypedAfterTheLastWordOfALineHangsOnTheLinesLastCharacter()
    {
        var song = bench.Open(Plain);
        var editor = new ChordProEditor(song);
        Type(editor, "[D]four", "[D]four[C]");
        Type(editor, "Thirteen", "Thirteen[A]");
        Assert.True(editor.Save());
        Assert.Equal(["0:G", "14:D", "17:C", "28:Em"], ChordsOf(0, song.Path));
        Assert.Equal(["0:C", "10:G/B", "30:A"], ChordsOf(1, song.Path));
    }

    [Fact]
    public void EmptyBracketsAreLeftOutOfWhatIsSavedAndLeftInTheText()
    {
        var song = bench.Open(Plain);
        var editor = new ChordProEditor(song);
        var original = File.ReadAllBytes(song.Path);
        Type(editor, "Five six", "Five []six");
        Type(editor, "Thirteen", "Thir[ ]teen");
        Assert.False(editor.Save());
        Assert.Equal(original, File.ReadAllBytes(song.Path));
        Assert.Empty(bench.Copies);
        Assert.Contains("Five []six", editor.Text, StringComparison.Ordinal);
        Assert.Contains("Thir[ ]teen", editor.Text, StringComparison.Ordinal);
        // Filled in, it is a chord, and is saved; the other is still left in the text.
        Type(editor, "Five []six", "Five [Am]six");
        Assert.True(editor.Save());
        Assert.Equal(["0:G", "14:D", "24:Am", "28:Em"], ChordsOf(0, song.Path));
        Assert.Contains("Thir[ ]teen", editor.Text, StringComparison.Ordinal);
        // (Of what can be typed between brackets, only nothing and spaces are "not yet
        // a chord" by the rules: chord-names.json has no other that could become one
        // and is not. A name half typed, "F#m7/", is a chord already, and is saved as
        // it stands, to be saved again when it is finished.)
        Type(editor, "Thir[ ]teen", "Thir[F#m7/]teen");
        Assert.True(editor.Save());
        Assert.Equal(["0:C", "10:G/B", "27:F#m7/"], ChordsOf(1, song.Path));
        Type(editor, "Thir[F#m7/]teen", "Thir[F#m7/A]teen");
        Assert.True(editor.Save());
        Assert.Equal(["0:C", "10:G/B", "27:F#m7/A"], ChordsOf(1, song.Path));
    }

    [Fact]
    public void TheSameChordsPutOnHereAndAskedOfTheSongDirectlyGiveTheSameFile()
    {
        using var other = new Workbench();
        var here = bench.Open(Plain);
        var there = other.Open(Plain);
        var editor = new ChordProEditor(here);
        Assert.Equal(ChordProEditor.Verdict.Taken, editor.Take(Lines(
            "{c: Verse 1}",
            "[Em]One [C]two [G]three [D]four",
            "Five six seven eight[f#m7/a]",
            "",
            "[C]Nine ten e[G/B]leven twelve",
            "Thir[A]teen",
            "",
            "{c: Chorus}",
            "Sing it out",
            "[D]Sing it [  Bb ]loud",
            "",
            "{c: Outro}",
            "[C] [G] [D]")));
        Assert.True(editor.Save());

        var boxes = there.TextBoxes;
        // (As the sheet would ask: a line of chords alone first, then each box's list.)
        there.SetChordsAlone(3, boxes[3].Element, 0, ["C", "G", "D"]);
        there.SetChords(0, boxes[0].Element, [new Chord(0, "Em"), new Chord(4, "C"), new Chord(8, "G"), new Chord(14, "D"), new Chord(38, "F#m7/A")], joined: true);
        there.SetChords(1, boxes[1].Element, [new Chord(0, "C"), new Chord(10, "G/B"), new Chord(27, "A")], joined: true);
        there.SetChords(2, boxes[2].Element, [new Chord(12, "D"), new Chord(20, "Bb")], joined: true);
        Assert.Equal(File.ReadAllBytes(there.Path), File.ReadAllBytes(here.Path));
    }

    [Fact]
    public void ALineOfChordsAloneIsSavedAsItsNamesInOrderAndABracketStillEmptyIsLeftOut()
    {
        var song = bench.Open(Plain);
        var editor = new ChordProEditor(song);
        Type(editor, "[G] [D/F#] [Em]", "[G] [] [Em]");
        Type(editor, "Thirteen", "[A]Thirteen");
        Assert.True(editor.Save());
        // Both boxes are saved, in one step of undo: the line with the two chords it has
        // for now, on two stand-ins.
        Assert.Equal(["0:C", "10:G/B", "23:A"], ChordsOf(1, song.Path));
        Assert.Equal(["0:G", "2:Em"], ChordsOf(3, song.Path));
        Assert.Equal(Chords.Placeholders(2), new OpenSong(song.Path).TextBoxes[3].Words);
        Assert.Contains("[G] [] [Em]", editor.Text, StringComparison.Ordinal);
        Type(editor, "[G] [] [Em]", "[G] [d] [Em]");
        Assert.True(editor.Save());
        Assert.Equal(["0:G", "2:D", "4:Em"], ChordsOf(3, song.Path));
        Assert.Equal(song.TextBoxes[3].Words, new OpenSong(song.Path).TextBoxes[3].Words);
        Assert.Contains("[G] [d] [Em]", editor.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void ABoxWithALineOfChordsAloneAndALineOfWordsEachKeepsItsChordsWhenTheOtherIsTypedIn()
    {
        var song = bench.Open("It Is Well.pro");
        var editor = new ChordProEditor(song);
        // Its fourth box: a line of four chords alone, then a line of words with two.
        var box = song.TextBoxes[3];
        Assert.Equal(["0:Am", "2:F", "4:C", "6:G", "8:Am", "22:F"], Said(box.Chords));
        var own = editor.Lines.Where(line => line.Box == 3).ToList();
        Assert.Equal([SongLineKind.ChordsAlone, SongLineKind.Words], own.Select(line => line.Kind));
        Assert.Equal("[Am] [F] [C] [G]", own[0].Text);
        // A chord typed at the end of its words: the line of chords alone keeps its four.
        Assert.Equal(ChordProEditor.Verdict.Taken, Type(editor, own[1].Text, own[1].Text + "[E]"));
        Assert.True(editor.Save());
        var last = box.Words.Length - 1;
        Assert.Equal(["0:Am", "2:F", "4:C", "6:G", "8:Am", "22:F", last + ":E"], ChordsOf(3, song.Path));
        // One of the four renamed: the words keep their three.
        Assert.Equal(ChordProEditor.Verdict.Taken, Type(editor, "[Am] [F] [C] [G]", "[Am] [Dm] [C] [G]"));
        Assert.True(editor.Save());
        Assert.Equal(["0:Am", "2:Dm", "4:C", "6:G", "8:Am", "22:F", last + ":E"], ChordsOf(3, song.Path));
        Assert.Equal(box.Words, new OpenSong(song.Path).TextBoxes[3].Words);
    }

    [Fact]
    public void ASongWithBlankSlidesCanBeTypedInLikeAnyOtherOnItsLinesOfWords()
    {
        var song = bench.Open(Blank);
        var editor = new ChordProEditor(song);
        Assert.Equal(ChordProEditor.Verdict.Taken, Type(editor, "That saved", "That [Em]saved"));
        Assert.True(editor.Save());
        Assert.Equal(["5:Em"], ChordsOf(2, song.Path));
        // The blank slides are as they were, the one with no text box among them.
        var now = new OpenSong(song.Path).TextBoxes;
        Assert.Equal(song.TextBoxes.Select(box => box.Blank), now.Select(box => box.Blank));
        Assert.Equal(3, now.Count(box => box.Blank));
        Assert.Equal("", now[^1].Element);
    }

    [Fact]
    public void AllTheChangesOfOneSavingUndoAsOneStep()
    {
        var song = bench.Open(Plain);
        var editor = new ChordProEditor(song);
        var original = File.ReadAllBytes(song.Path);
        Type(editor, "One two", "One [A]two");
        Type(editor, "Sing it loud", "Sing it [E]loud");
        Type(editor, "[G] [D/F#] [Em]", "[Em] [D/F#] [G]");
        Assert.True(editor.Save());
        Assert.Equal(["0:G", "4:A", "14:D", "28:Em"], ChordsOf(0, song.Path));
        Assert.Equal(["0:C", "8:G", "12:D", "20:E"], ChordsOf(2, song.Path));
        Assert.Equal(["0:Em", "2:D/F#", "4:G"], ChordsOf(3, song.Path));
        song.Undo();
        Assert.Equal(original, File.ReadAllBytes(song.Path));
        Assert.False(song.CanUndo);
        // When the song changes from elsewhere, the text is made again from it.
        editor.Reload(keepingWhatIsTyped: false);
        Assert.Equal(SongText.Text(song.TextBoxes), editor.Text);
        Assert.DoesNotContain("[A]", editor.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void EachSavingIsAStepOfUndoOfItsOwnHoweverManyRoundsItTook()
    {
        var song = bench.Open(Plain);
        var editor = new ChordProEditor(song);
        var original = File.ReadAllBytes(song.Path);
        Type(editor, "One two", "One [A]two");
        Assert.True(editor.Save());
        var once = File.ReadAllBytes(song.Path);
        // A second saving, of a line of chords alone and of a box of words: two rounds.
        Type(editor, "Thirteen", "[E]Thirteen");
        Type(editor, "[G] [D/F#] [Em]", "[G] [Em]");
        Assert.True(editor.Save());
        Assert.NotEqual(once, File.ReadAllBytes(song.Path));
        song.Undo();
        Assert.Equal(once, File.ReadAllBytes(song.Path));
        Assert.True(song.CanUndo);
        song.Undo();
        Assert.Equal(original, File.ReadAllBytes(song.Path));
        Assert.False(song.CanUndo);
    }

    [Fact]
    public void ABoxIsSavedOnlyWhenWhatTheTextSaysOfItHasChanged()
    {
        // One with an untidy name, or a chord past its words, is left alone (rule 24).
        var untouched = new List<CustomAttribute>();
        var second = "";
        var song = PlainSongWith("Untidy.pro", (made, boxes) =>
        {
            // The second box: a name that is not tidy, and a chord past the end.
            Rename(0, boxes[1].Element, "c", made);
            var entries = Workbench.TextOf(made, boxes[1].Element).Attributes.CustomAttributes;
            var stray = entries.First(IsAChord).Clone();
            stray.Chord = "E";
            stray.Range = new IntRange { Start = 90, End = 91 };
            entries.Add(stray);
            untouched = [.. entries.Select(entry => entry.Clone())];
            second = boxes[1].Element;
        });
        var editor = new ChordProEditor(song);
        Assert.Contains("[c]Nine ten e[G/B]leven twelve", editor.Text, StringComparison.Ordinal);
        // A chord is typed in the FIRST box.
        Type(editor, "One two", "One [A]two");
        Assert.True(editor.Save());
        Assert.Equal(["0:G", "4:A", "14:D", "28:Em"], ChordsOf(0, song.Path));
        // The second box's entries are, to the last field, what they were.
        Assert.Equal(untouched, Workbench.TextOf(Workbench.Read(song.Path), second).Attributes.CustomAttributes);
    }

    [Fact]
    public void ASaveThatIsRefusedChangesNothingAndSaysWhy()
    {
        var song = bench.Open(Plain);
        var editor = new ChordProEditor(song);
        var original = File.ReadAllBytes(song.Path);
        Type(editor, "One two", "One [A]two");
        bench.ProPresenterIsRunning = true;
        Assert.Equal(Refusal.Why.ProPresenterIsRunning, Assert.Throws<Refusal>(() => editor.Save()).Reason);
        Assert.Equal(original, File.ReadAllBytes(song.Path));
        Assert.Empty(bench.Copies);
        // "Nothing was changed": the window then makes the text again from the song.
        editor.Reload(keepingWhatIsTyped: false);
        Assert.DoesNotContain("[A]", editor.Text, StringComparison.Ordinal);
        // Once ProPresenter has quit, the same thing typed is saved.
        bench.ProPresenterIsRunning = false;
        Type(editor, "One two", "One [A]two");
        Assert.True(editor.Save());
    }

    [Fact]
    public void WhatWasSavedBeforeARefusalStaysSavedAndTheTextStillSaysWhatWasTyped()
    {
        // Two boxes typed in; the second save of the round is refused, ProPresenter
        // having been started between the two.
        var asked = 0;
        var path = bench.CopyOf(Plain);
        var song = new OpenSong(path, new Safeguards(() => ++asked > 1, (_, _) => { }));
        var editor = new ChordProEditor(song);
        Type(editor, "One two", "One [A]two");
        Type(editor, "Thirteen", "[E]Thirteen");
        Assert.Equal(Refusal.Why.ProPresenterIsRunning, Assert.Throws<Refusal>(() => editor.Save()).Reason);
        Assert.Equal(["0:G", "4:A", "14:D", "28:Em"], ChordsOf(0, path));
        Assert.Equal(["0:C", "10:G/B"], ChordsOf(1, path));
        // The editor has read the song again, and its text is still what was typed: it
        // is the window that then makes it again from the song (rule 19a).
        Assert.Contains("[E]Thirteen", editor.Text, StringComparison.Ordinal);
        Assert.Equal(Said(new OpenSong(path).TextBoxes[0].Chords), Said(song.TextBoxes[0].Chords));
    }

    // ---- Help with the brackets (rules 9 to 11)

    [Fact]
    public void ABracketBringsItsOtherWithItAndTheCaretIsLeftBetweenThem()
    {
        const string text = "Born to raise";
        Assert.Equal(new ChordProEditor.Edit(5, 5, "[]", 6), ChordProEditor.HelpWithTyping("[", text, 5, 0));
        // Not over a selection, and not for any other character.
        Assert.Null(ChordProEditor.HelpWithTyping("[", text, 5, 2));
        Assert.Null(ChordProEditor.HelpWithTyping("G", text, 5, 0));
        Assert.Null(ChordProEditor.HelpWithTyping("[G]", text, 5, 0));
        // At the very end, and in no text at all.
        Assert.Equal(new ChordProEditor.Edit(13, 13, "[]", 14), ChordProEditor.HelpWithTyping("[", text, 13, 0));
        Assert.Equal(new ChordProEditor.Edit(0, 0, "[]", 1), ChordProEditor.HelpWithTyping("[", "", 0, 0));
    }

    [Fact]
    public void AClosingBracketTypedAtAClosingBracketStepsOverIt()
    {
        const string text = "Born [G]to raise";
        Assert.Equal(new ChordProEditor.Edit(7, 7, "", 8), ChordProEditor.HelpWithTyping("]", text, 7, 0));
        Assert.Null(ChordProEditor.HelpWithTyping("]", text, 6, 0));
        Assert.Null(ChordProEditor.HelpWithTyping("]", text, 16, 0));
        Assert.Null(ChordProEditor.HelpWithTyping("]", text, 6, 1));
    }

    [Fact]
    public void BackspaceJustAfterABracketOrDeleteJustBeforeOneTakesTheWholeChord()
    {
        //                   0123456789012345678
        const string text = "Born [G/B]to raise";
        var whole = new ChordProEditor.Edit(5, 10, "", 5);
        // Backspace after "[" and after "]".
        Assert.Equal(whole, ChordProEditor.HelpWithBackspace(text, 6, 0));
        Assert.Equal(whole, ChordProEditor.HelpWithBackspace(text, 10, 0));
        // Delete before "[" and before "]".
        Assert.Equal(whole, ChordProEditor.HelpWithDelete(text, 5, 0));
        Assert.Equal(whole, ChordProEditor.HelpWithDelete(text, 9, 0));
        // Elsewhere the key does what it always does: inside the name, among the words,
        // at the ends of the text, and over a selection.
        Assert.Null(ChordProEditor.HelpWithBackspace(text, 8, 0));
        Assert.Null(ChordProEditor.HelpWithDelete(text, 7, 0));
        Assert.Null(ChordProEditor.HelpWithBackspace(text, 3, 0));
        Assert.Null(ChordProEditor.HelpWithBackspace(text, 0, 0));
        Assert.Null(ChordProEditor.HelpWithDelete(text, 18, 0));
        Assert.Null(ChordProEditor.HelpWithBackspace(text, 6, 2));
        Assert.Null(ChordProEditor.HelpWithDelete(text, 5, 1));
        Assert.Null(ChordProEditor.HelpWithBackspace(text, 19, 0));
        Assert.Null(ChordProEditor.HelpWithDelete(text, -1, 0));
    }

    [Fact]
    public void OnlyAChordOnItsOwnLineABracketWhoseOtherIsOnAnotherLineIsLeftToTheKey()
    {
        //                   01234 5678
        const string text = "ab[G\ncd]e";
        Assert.Null(ChordProEditor.HelpWithBackspace(text, 3, 0));
        Assert.Null(ChordProEditor.HelpWithDelete(text, 2, 0));
        Assert.Null(ChordProEditor.HelpWithBackspace(text, 8, 0));
        Assert.Null(ChordProEditor.HelpWithDelete(text, 7, 0));
        // The second of two chords on a line, and the first of the next line.
        //                  0123456 789012
        const string two = "[C]a[G]\n[D]b";
        Assert.Equal(new ChordProEditor.Edit(4, 7, "", 4), ChordProEditor.HelpWithBackspace(two, 7, 0));
        Assert.Equal(new ChordProEditor.Edit(8, 11, "", 8), ChordProEditor.HelpWithDelete(two, 8, 0));
        Assert.Equal(new ChordProEditor.Edit(8, 11, "", 8), ChordProEditor.HelpWithBackspace(two, 9, 0));
        // A bracket that closes nothing, after a chord that is whole, does not take that
        // chord with it. (Text that passes the rule has no such bracket; this is the
        // reference's own care, kept.)
        Assert.Null(ChordProEditor.HelpWithBackspace("[C]ab]", 6, 0));
        Assert.Null(ChordProEditor.HelpWithDelete("[C]ab]", 5, 0));
    }

    [Fact]
    public void WhatTheHelpDoesIsThenHeldToTheRuleLikeAnythingTyped()
    {
        var editor = new ChordProEditor(bench.Open(Plain));
        ChordProEditor.Verdict Apply(ChordProEditor.Edit? edit) =>
            editor.Take(editor.Text[..edit!.Value.Start] + edit.Value.With + editor.Text[edit.Value.End..]);

        // "[" among the words is let through; in a group's name it is not.
        var caret = editor.Text.IndexOf("six", StringComparison.Ordinal);
        Assert.Equal(ChordProEditor.Verdict.Taken, Apply(ChordProEditor.HelpWithTyping("[", editor.Text, caret, 0)));
        Assert.Equal(ChordProEditor.Verdict.NotTheWords, Apply(ChordProEditor.HelpWithTyping("[", editor.Text, 3, 0)));
        // Backspace on the bracket of a chord among the words takes it; so it does on
        // one of a line of chords alone, which then has a chord fewer.
        var afterOpen = editor.Text.IndexOf("[D]four", StringComparison.Ordinal) + 1;
        Assert.Equal(ChordProEditor.Verdict.Taken, Apply(ChordProEditor.HelpWithBackspace(editor.Text, afterOpen, 0)));
        Assert.Contains("three four", editor.Text, StringComparison.Ordinal);
        Assert.EndsWith("[G] [D/F#] [Em]", editor.Text, StringComparison.Ordinal);
        Assert.Equal(ChordProEditor.Verdict.Taken, Apply(ChordProEditor.HelpWithBackspace(editor.Text, editor.Text.Length, 0)));
        Assert.EndsWith("[G] [D/F#] ", editor.Text, StringComparison.Ordinal);
    }

    // ---- Which slide, and the colours

    [Fact]
    public void TheCaretsLineSaysWhichSlideIsPickedAndASlidePickedPutsTheCaretAtTheStartOfItsFirstLine()
    {
        var editor = new ChordProEditor(bench.Open(Plain));
        var start = 0;
        var slides = new List<int?>();
        foreach (var line in editor.Text.Split('\n'))
        {
            slides.Add(editor.SlideAt(start));
            Assert.Equal(editor.SlideAt(start), editor.SlideAt(start + line.Length));
            start += line.Length + 1;
        }
        Assert.Equal([null, 0, 0, null, 1, 1, null, null, 2, 2, null, null, 3], slides);
        // "{c: Verse 1}" and its line break are 13 units: the first slide's first line
        // starts after them.
        Assert.Equal(13, editor.StartOfSlide(0));
        Assert.Equal(editor.Text.Length - "[G] [D/F#] [Em]".Length, editor.StartOfSlide(3));
        Assert.Null(editor.StartOfSlide(4));
        for (var slide = 0; slide <= 3; slide++)
        {
            Assert.Equal(slide, editor.SlideAt(editor.StartOfSlide(slide)!.Value));
        }
        // A place before the text is its start, and one past its end is its last line.
        Assert.Null(editor.SlideAt(-5));
        Assert.Equal(3, editor.SlideAt(editor.Text.Length + 5));
    }

    [Fact]
    public void ChordsAreColouredBracketsAndAllAndTheLinesInCurlyBracketsGreyed()
    {
        //                   0123456789012 3456789012345 6 78901
        const string text = "{c: Verse 1}\n[G]One [D/F#]two\n\n[] x";
        var found = ChordProEditor.Colouring(text);
        Assert.Equal([new ChordProEditor.Stretch(0, 12)], found.Greyed);
        Assert.Equal([new ChordProEditor.Stretch(13, 16), new ChordProEditor.Stretch(20, 26), new ChordProEditor.Stretch(31, 33)], found.Chords);
        Assert.Empty(ChordProEditor.Colouring("").Chords);
        Assert.Empty(ChordProEditor.Colouring("").Greyed);
        // A bracket with no other is no chord, and is not coloured.
        Assert.Equal([new ChordProEditor.Stretch(7, 10)], ChordProEditor.Colouring("a[G b c[D]").Chords);
        // A chord in a line that begins with a curly bracket is of the greyed line.
        Assert.Empty(ChordProEditor.Colouring("{c: [G]}").Chords);
    }
}
