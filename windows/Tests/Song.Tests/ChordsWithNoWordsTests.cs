// Chords with no words: a chord more or fewer on a line of chords alone, and chords on a
// blank slide. spec/01-file-format.md, "Chords with no words" and "Chords onto a blank
// slide"; spec/05-song-and-saving.md, request 2, to the letter. It is the one case in
// which the app writes anything but chord entries, and the one in which it adds
// anything to a slide, so it is tried here on the song the test data has in the shapes
// real songs have (Chords With No Words: tools/make-fixtures.py lists its slides), and
// on Blank Slides.
//
// The plan's proofs for this that a test can make (spec/11-plan.md, M7):
//
//   - the RTF of every other line of a changed box is byte for byte what it was;
//   - a blank slide whose chords are taken off again in the same sitting leaves the
//     file byte for byte what it was.
//
// The third, that a song so changed opens in ProPresenter with its chords in place, is
// the maintainer's to see, once there is a window to put a chord on with.
//
// (The song's part of this was built at milestone M2 on Windows, the pages having it to
// the letter by then: windows/NOTES.md.)

using Google.Protobuf;
using Pro.SerializationInterop.RVProtoData;
using Pro7ChordEditor.ChordRules;
using Xunit;
using CustomAttribute = Pro.SerializationInterop.RVProtoData.Graphics.Types.Text.Types.Attributes.Types.CustomAttribute;

namespace Pro7ChordEditor.Song.Tests;

public sealed class ChordsWithNoWordsTests : IDisposable
{
    private const string Shaped = "Chords With No Words.pro";
    private const char Stand = Words.ZeroWidthSpace;
    private readonly Workbench bench = new();

    public void Dispose() => bench.Dispose();

    /// <summary>Every line of chords alone of the song in the shapes of real ones: its box, and which line.</summary>
    public static TheoryData<int, int> EveryLineOfChordsAlone()
    {
        using var bench = new Workbench();
        var data = new TheoryData<int, int>();
        var boxes = bench.Open(Shaped).TextBoxes;
        for (var b = 0; b < boxes.Count; b++)
        {
            var lines = Words.Lines(boxes[b].Words);
            for (var line = 0; line < lines.Length; line++)
            {
                if (!boxes[b].Blank && Chords.IsPlaceholders(lines[line]))
                {
                    data.Add(b, line);
                }
            }
        }
        return data;
    }

    // A chord more, a chord fewer, two more, and all of them renamed: after each, the
    // line is that many stand-ins with its chords on them, every chord of every other
    // line is on the character it was on, nothing in the file has changed but that
    // box's RTF and chord entries, and of the RTF not a byte outside that line.
    [Theory]
    [MemberData(nameof(EveryLineOfChordsAlone))]
    public void ALineOfChordsAloneTakesAChordMoreOrFewerAndNothingElseChanges(int boxNumber, int line)
    {
        var song = bench.Open(Shaped);
        var asItWas = File.ReadAllBytes(song.Path);
        var box = song.TextBoxes[boxNumber];
        var had = ChordsOn(box, line).Select(chord => chord.Name).ToList();
        Assert.NotEmpty(had);

        foreach (var names in new List<string>[] { [.. had, "F#m"], had.Take(had.Count - 1).ToList(), [.. had, "A", "B7"], had.Select(name => name + "sus4").ToList() })
        {
            var before = song.TextBoxes[boxNumber];
            var fileBefore = Workbench.Read(song.Path);
            // The character the line hangs its chords on as it is now: a plain space if
            // it has spaces and no zero-width space, else a zero-width space. (A line
            // that was left with no chord hangs them on zero-width spaces from then on.)
            var lineBefore = Words.Lines(before.Words)[line];
            var standIn = !lineBefore.Contains(Stand, StringComparison.Ordinal) && lineBefore.Contains(' ', StringComparison.Ordinal) ? ' ' : Stand;

            song.SetChordsAlone(box.Slide, box.Element, line, names);

            var after = song.TextBoxes[boxNumber];
            var lines = Words.Lines(after.Words);
            // The line: one stand-in for each chord, in the character it hung them on,
            // or one zero-width space where there is none; and its chords on them.
            Assert.Equal(names.Count == 0 ? Stand.ToString() : Chords.Placeholders(names.Count, standIn), lines[line]);
            Assert.Equal(names, ChordsOn(after, line).Select(chord => chord.Name));
            Assert.Equal(Enumerable.Range(0, names.Count).Select(n => n * 2), ChordsOn(after, line).Select(chord => chord.At - StartOf(after, line)));
            // Every other line: its words, and its chords on the characters they were on.
            for (var other = 0; other < lines.Length; other++)
            {
                if (other != line)
                {
                    Assert.Equal(Words.Lines(before.Words)[other], lines[other]);
                    Assert.Equal(
                        ChordsOn(before, other).Select(chord => (chord.At - StartOf(before, other), chord.Name)),
                        ChordsOn(after, other).Select(chord => (chord.At - StartOf(after, other), chord.Name)));
                }
            }
            NothingElseChanged(fileBefore, Workbench.Read(song.Path), box.Element, line);
            // And it is in the file, as another reads it.
            Assert.Equal(after.Words, new OpenSong(song.Path).TextBoxes[boxNumber].Words);
            Assert.Equal(after.Chords, new OpenSong(song.Path).TextBoxes[boxNumber].Chords);
        }

        // Each was one step of undo, whatever it wrote; and undone, the file is what it
        // was, to the byte.
        for (var step = 0; step < 4; step++)
        {
            Assert.True(song.CanUndo);
            song.Undo();
        }
        Assert.False(song.CanUndo);
        Assert.Equal(asItWas, File.ReadAllBytes(song.Path));
    }

    // "Where the line is those stand-ins already (its chords renamed, or put in another
    // order), only the chord entries change, and not a byte of the RTF."
    [Theory]
    [MemberData(nameof(EveryLineOfChordsAlone))]
    public void ChordsRenamedOrPutInAnotherOrderChangeNoByteOfTheRtf(int boxNumber, int line)
    {
        var song = bench.Open(Shaped);
        var box = song.TextBoxes[boxNumber];
        var rtf = Workbench.TextOf(Workbench.Read(song.Path), box.Element).RtfData;
        var had = ChordsOn(box, line).Select(chord => chord.Name).ToList();

        song.SetChordsAlone(box.Slide, box.Element, line, had.AsEnumerable().Reverse().Select(name => name + "7").ToList());

        Assert.Equal(rtf, Workbench.TextOf(Workbench.Read(song.Path), box.Element).RtfData);
        Assert.Equal(box.Words, song.TextBoxes[boxNumber].Words);
        Assert.Equal(had.AsEnumerable().Reverse().Select(name => name + "7"), ChordsOn(song.TextBoxes[boxNumber], line).Select(chord => chord.Name));

        // Given the names it has, in the order it has them, nothing is changed at all.
        var again = bench.Open(Shaped);
        again.SetChordsAlone(box.Slide, box.Element, line, had);
        Assert.False(again.CanUndo);
    }

    // "With no names the line is one zero-width space, whatever it hung its chords on,
    // and is a line of chords alone still: one can be put on it again", and the first
    // of them without a byte of the RTF changing. (The maintainer's, 2026-10-12.)
    [Theory]
    [MemberData(nameof(EveryLineOfChordsAlone))]
    public void ALineThatLosesItsLastChordKeepsOneStandInAndTakesChordsAgain(int boxNumber, int line)
    {
        var song = bench.Open(Shaped);
        var box = song.TextBoxes[boxNumber];

        song.SetChordsAlone(box.Slide, box.Element, line, []);

        var emptied = song.TextBoxes[boxNumber];
        Assert.False(emptied.Blank);
        Assert.Equal(box.Element, emptied.Element);
        Assert.Equal(Stand.ToString(), Words.Lines(emptied.Words)[line]);
        Assert.Empty(ChordsOn(emptied, line));
        // (So it is still handed over as a line of chords alone: not a blank slide, and
        // not left out as a stray space would be.)
        Assert.True(Chords.IsPlaceholders(Words.Lines(emptied.Words)[line]));

        var rtf = Workbench.TextOf(Workbench.Read(song.Path), box.Element).RtfData;
        song.SetChordsAlone(box.Slide, box.Element, line, ["Em"]);

        Assert.Equal(rtf, Workbench.TextOf(Workbench.Read(song.Path), box.Element).RtfData);
        Assert.Equal(["Em"], ChordsOn(song.TextBoxes[boxNumber], line).Select(chord => chord.Name));
        // A second after it hangs on a zero-width space too, that being then the
        // stand-in the line has.
        song.SetChordsAlone(box.Slide, box.Element, line, ["Em", "D"]);
        Assert.Equal(Chords.Placeholders(2), Words.Lines(song.TextBoxes[boxNumber].Words)[line]);
    }

    // A name that is nothing, or nothing but spaces, is left out: it gets no stand-in,
    // as it would get no entry. And every name is tidied.
    [Fact]
    public void ANameThatIsNothingGetsNoStandInAndNamesAreTidied()
    {
        var song = bench.Open(Shaped);
        var box = song.TextBoxes.First(one => !one.Blank && Chords.IsPlaceholders(one.Words));

        song.SetChordsAlone(box.Slide, box.Element, 0, [" g ", "", "   ", "f#m/a"]);

        var after = song.TextBoxes.Single(one => one.Slide == box.Slide);
        Assert.Equal(["G", "F#m/A"], after.Chords.Select(chord => chord.Name));
        Assert.Equal(3, after.Words.Length);
    }

    // ---- Chords onto a blank slide

    // A blank slide that has a text box: its first chords give the box words, one
    // zero-width space for each chord, after the opening of the nearest box that has
    // words. Only the box's RTF and its chord entries change. And taken off again in the
    // same sitting, the file is byte for byte what it was.
    [Theory]
    [InlineData(Shaped, 0)]
    [InlineData(Shaped, 10)]
    [InlineData("Blank Slides.pro", 0)]
    [InlineData("Blank Slides.pro", 3)]
    public void ABlankSlidesBoxIsGivenWordsAndAChangeOfMindLeavesNoTrace(string file, int slide)
    {
        var song = bench.Open(file);
        var asItWas = File.ReadAllBytes(song.Path);
        var fileBefore = Workbench.Read(song.Path);
        var blank = song.TextBoxes.Single(box => box.Slide == slide);
        Assert.True(blank.Blank);
        Assert.NotEqual("", blank.Element);

        // (No chords for a blank slide is nothing to do, and nothing is made for it.)
        song.SetChordsAlone(slide, blank.Element, 0, []);
        Assert.False(song.CanUndo);

        song.SetChordsAlone(slide, blank.Element, 0, ["G", "D/F#", "Em"]);

        var given = song.TextBoxes.Single(box => box.Slide == slide);
        Assert.False(given.Blank);
        Assert.Equal(blank.Element, given.Element);
        Assert.Equal(Chords.Placeholders(3), given.Words);
        Assert.Equal([new Chord(0, "G"), new Chord(2, "D/F#"), new Chord(4, "Em")], given.Chords);
        // Only that box's RTF and chord entries: where it is on the slide, its size, its
        // fill and its other settings are as they were.
        NothingElseChanged(fileBefore, Workbench.Read(song.Path), blank.Element, line: null);
        // Its RTF opens as the nearest box with words does, up to where those words begin.
        var model = NearestWithWords(song, slide);
        var rtf = Workbench.TextOf(Workbench.Read(song.Path), blank.Element).RtfData.ToByteArray();
        var opening = Workbench.TextOf(fileBefore, model.Element).RtfData.ToByteArray();
        Assert.True(rtf.AsSpan(0, CommonPrefix(rtf, opening)).Length > 20, "the box's RTF does not open as its neighbour's does");

        // It is a line of chords alone now, and takes a chord more like any other.
        song.SetChordsAlone(slide, blank.Element, 0, ["G", "D/F#", "Em", "C"]);
        Assert.Equal(Chords.Placeholders(4), song.TextBoxes.Single(box => box.Slide == slide).Words);

        // A change of mind: the last chord taken off again, in the same sitting.
        song.SetChordsAlone(slide, blank.Element, 0, []);
        Assert.Equal(asItWas, File.ReadAllBytes(song.Path));
        var again = song.TextBoxes.Single(box => box.Slide == slide);
        Assert.True(again.Blank);
        Assert.Equal("", again.Words);
    }

    // "Once the song is closed the app remembers nothing. Chords taken off another day
    // leave the box as any line of chords alone is left: with the opening it was given
    // and one stand-in."
    [Fact]
    public void ChordsTakenOffABlankSlideAnotherDayLeaveOneStandIn()
    {
        var song = bench.Open("Blank Slides.pro");
        var blank = song.TextBoxes.First(box => box.Blank && box.Element.Length > 0);
        song.SetChordsAlone(blank.Slide, blank.Element, 0, ["G"]);

        var anotherDay = bench.OpenAt(song.Path);
        anotherDay.SetChordsAlone(blank.Slide, blank.Element, 0, []);

        var left = anotherDay.TextBoxes.Single(box => box.Slide == blank.Slide);
        Assert.False(left.Blank);
        Assert.Equal(Stand.ToString(), left.Words);
        Assert.Empty(left.Chords);
        // And it takes chords again.
        anotherDay.SetChordsAlone(blank.Slide, blank.Element, 0, ["Am"]);
        Assert.Equal([new Chord(0, "Am")], anotherDay.TextBoxes.Single(box => box.Slide == blank.Slide).Chords);
    }

    // A blank slide with no text box at all: its first chords give it one, a copy of
    // the nearest that has words, with a new id, a name no other element of the slide
    // has, nothing tying it to another slide's things, and the stand-ins for its words.
    [Theory]
    [InlineData(Shaped, 11)]
    [InlineData("Blank Slides.pro", 6)]
    public void ASlideWithNoTextBoxIsGivenOneAndAChangeOfMindTakesItOffAgain(string file, int slide)
    {
        var song = bench.Open(file);
        var asItWas = File.ReadAllBytes(song.Path);
        var fileBefore = Workbench.Read(song.Path);
        var bare = song.TextBoxes.Single(box => box.Slide == slide);
        Assert.True(bare.Blank);
        Assert.Equal("", bare.Element);
        var idsBefore = ElementsOf(fileBefore).Select(element => element.Element_?.Uuid?.String ?? "").ToList();

        // (Nothing is made for a slide until a chord is put on it.)
        song.SetChordsAlone(slide, "", 0, []);
        Assert.False(song.CanUndo);

        song.SetChordsAlone(slide, "", 0, ["E", "B"]);

        var given = song.TextBoxes.Single(box => box.Slide == slide);
        Assert.False(given.Blank);
        Assert.Equal(Chords.Placeholders(2), given.Words);
        Assert.Equal([new Chord(0, "E"), new Chord(2, "B")], given.Chords);
        // One element more in the whole file, and it is the new box.
        var fileAfter = Workbench.Read(song.Path);
        var added = Assert.Single(ElementsOf(fileAfter), element => !idsBefore.Contains(element.Element_?.Uuid?.String ?? ""));
        Assert.Equal(given.Element, added.Element_.Uuid.String);
        Assert.Equal(idsBefore.Count + 1, ElementsOf(fileAfter).Count);
        // It is a copy of the nearest box that has words: placed, sized and set as that
        // one is. Its id is written as the file's others are.
        var modelId = NearestWithWords(song, slide).Element;
        var model = ElementsOf(fileBefore).Single(element => element.Element_?.Uuid?.String == modelId);
        Assert.Equal(model.Element_.Bounds, added.Element_.Bounds);
        Assert.Equal(model.Element_.Fill, added.Element_.Fill);
        Assert.Equal(model.Element_.Text.Attributes.Font, added.Element_.Text.Attributes.Font);
        Assert.Equal(model.Element_.Uuid.String.Any(char.IsAsciiLetterLower), added.Element_.Uuid.String.Any(char.IsAsciiLetterLower));
        Assert.True(Guid.TryParse(added.Element_.Uuid.String, out _));
        Assert.Equal(model.Element_.Name, added.Element_.Name);
        // What tied the other box to things on its own slide is cleared.
        Assert.Null(added.BuildIn);
        Assert.Null(added.BuildOut);
        Assert.Empty(added.ChildBuilds);
        Assert.Empty(added.DataLinks);
        // Nothing else in the file has changed: with the new box taken off, it is what
        // it was.
        var without = fileAfter.Clone();
        foreach (var elements in from cue in without.Cues from action in cue.Actions select action.Slide?.Presentation?.BaseSlide?.Elements)
        {
            var index = elements?.ToList().FindIndex(element => element.Element_?.Uuid?.String == given.Element) ?? -1;
            if (index >= 0)
            {
                elements!.RemoveAt(index);
            }
        }
        Assert.Equal(fileBefore, without);

        // Asked again as a slide with no box, when it has one by now: refused.
        var refusal = Assert.Throws<Refusal>(() => song.SetChordsAlone(slide, "", 0, ["A"]));
        Assert.Equal(Refusal.Why.NoLongerBlank, refusal.Reason);
        Assert.Equal("That slide is no longer blank. Nothing was changed.", refusal.Sentence);

        // It is a line of chords alone now, named by its new id.
        song.SetChordsAlone(slide, given.Element, 0, ["E"]);
        Assert.Equal(Stand.ToString(), song.TextBoxes.Single(box => box.Slide == slide).Words);

        // A change of mind: its last chord taken off in the same sitting takes the box
        // off the slide again, and the file is byte for byte what it was.
        song.SetChordsAlone(slide, given.Element, 0, []);
        Assert.Equal(asItWas, File.ReadAllBytes(song.Path));
        Assert.Equal("", song.TextBoxes.Single(box => box.Slide == slide).Element);
    }

    // A second box given to a slide that already has an element of the copied name is
    // named with a number after it.
    [Fact]
    public void ANameThatIsTakenIsGivenANumber()
    {
        Assert.Equal("Lyrics", OpenSong.NameNotAmong("Lyrics", []));
        Assert.Equal("Lyrics 2", OpenSong.NameNotAmong("Lyrics", ["Lyrics"]));
        Assert.Equal("Lyrics 4", OpenSong.NameNotAmong("Lyrics", ["Lyrics 3", "Lyrics", "Lyrics 2"]));
        // An id is written as the one it is modelled on is: in capitals, unless that has
        // a small letter in it.
        Assert.DoesNotContain(OpenSong.NewId("7B8FC577-1FCD-48A5-8C73-7201D02DEC81"), char.IsAsciiLetterLower);
        Assert.DoesNotContain(OpenSong.NewId("7b8fc577-1fcd-48a5-8c73-7201d02dec81"), char.IsAsciiLetterUpper);
    }

    // ---- What is refused

    // A song that has no text box with words in it has nothing to model a box on, nor
    // an opening to take: chords cannot be put on its blank slides.
    [Fact]
    public void ASongWithNoWordsAnywhereHasNothingToModelABlankSlidesBoxOn()
    {
        const string sentence = "This song has no text box with words in it to model one on, so chords cannot be put on a slide that has none. Nothing was changed.";
        var whole = Workbench.Read(bench.CopyOf("Blank Slides.pro"));
        var reader = new OpenSong(bench.CopyOf("Blank Slides.pro"));
        var withWords = reader.TextBoxes.Where(box => !box.Blank).Select(box => box.Element).ToHashSet();
        // Every cue that has words is taken out, the blank ones left.
        foreach (var cue in whole.Cues.ToList())
        {
            if (cue.Actions.Any(action => (action.Slide?.Presentation?.BaseSlide?.Elements ?? []).Any(element => withWords.Contains(element.Element_?.Uuid?.String ?? ""))))
            {
                whole.Cues.Remove(cue);
            }
        }
        var song = bench.OpenAt(bench.FileOf("only-blank.pro", whole.ToByteArray()));
        Assert.All(song.TextBoxes, box => Assert.True(box.Blank));
        var asItWas = File.ReadAllBytes(song.Path);

        foreach (var blank in song.TextBoxes)
        {
            var refusal = Assert.Throws<Refusal>(() => song.SetChordsAlone(blank.Slide, blank.Element, 0, ["G"]));
            Assert.Equal(Refusal.Why.NoBoxToModelOn, refusal.Reason);
            Assert.Equal(sentence, refusal.Sentence);
        }
        Assert.Equal(asItWas, File.ReadAllBytes(song.Path));
        Assert.Empty(bench.Copies);
    }

    // A line whose stand-ins cannot be found cleanly in the box's RTF: the change is
    // refused. Here the RTF is one that ends its line of stand-ins with a character
    // written with two bytes that are also the next character's (a made-up thing, of the
    // kind testdata/vectors/stand-ins-in-rtf.json has among its refusals): every
    // numbered character is followed by a letter to be passed over, which the app does
    // not write.
    [Fact]
    public void ALineThatCannotBeChangedSafelyInItsRtfIsRefused()
    {
        const string sentence = "A chord could not be added to this line, or taken from it: its text box is written in a way that cannot be changed here safely. Nothing was changed.";
        var path = bench.CopyOf(Shaped);
        var box = new OpenSong(path).TextBoxes.First(one => !one.Blank && Chords.IsPlaceholders(one.Words) && one.Words.Contains(Stand, StringComparison.Ordinal));
        var whole = Workbench.Read(path);
        // Two zero-width spaces with an em quad between, each a numbered character with
        // a question mark after it to be passed over, by a reader that does not know
        // numbered characters: as an RTF may be written where "uc1" holds.
        byte[] rtf = [.. "{"u8, 0x5C, .. "rtf1"u8, 0x5C, .. "uc1 "u8, 0x5C, .. "u8203?"u8, 0x5C, .. "u8193?"u8, 0x5C, .. "u8203?}"u8];
        Assert.Equal(Chords.Placeholders(2), RtfWords.Read(rtf));
        Workbench.TextOf(whole, box.Element).RtfData = ByteString.CopyFrom(rtf);
        var song = bench.OpenAt(bench.FileOf("passed-over.pro", whole.ToByteArray()));
        var asItWas = File.ReadAllBytes(song.Path);

        var refusal = Assert.Throws<Refusal>(() => song.SetChordsAlone(box.Slide, box.Element, 0, ["G", "D", "Em"]));

        Assert.Equal(Refusal.Why.CannotBeWritten, refusal.Reason);
        Assert.Equal(sentence, refusal.Sentence);
        Assert.Equal(asItWas, File.ReadAllBytes(song.Path));
        Assert.False(song.CanUndo);
        // Its chords can still be renamed, which writes no RTF.
        song.SetChordsAlone(box.Slide, box.Element, 0, ["G", "D"]);
        Assert.Equal(["G", "D"], song.TextBoxes.Single(one => one.Slide == box.Slide).Chords.Select(chord => chord.Name));
    }

    // An empty line of a box is not a line of chords alone, and is not made one: nor is
    // a line of words.
    [Fact]
    public void AnEmptyLineAndALineOfWordsAreNotMadeStandIns()
    {
        const string sentence = "That is not a line of chords with no words. Nothing was changed.";
        var path = bench.CopyOf("Plain Song.pro");
        var first = new OpenSong(path).TextBoxes[0];
        var whole = Workbench.Read(path);
        // Words, an empty line, and words.
        byte[] rtf = [.. "{"u8, 0x5C, .. "rtf1 One"u8, 0x5C, 0x0A, 0x5C, 0x0A, .. "Three}"u8];
        Workbench.TextOf(whole, first.Element).RtfData = ByteString.CopyFrom(rtf);
        var song = bench.OpenAt(bench.FileOf("an-empty-line.pro", whole.ToByteArray()));
        Assert.Equal("One\n\nThree", song.TextBoxes[0].Words);

        foreach (var line in new[] { 0, 1, 2 })
        {
            var refusal = Assert.Throws<Refusal>(() => song.SetChordsAlone(first.Slide, first.Element, line, ["G"]));
            Assert.Equal(Refusal.Why.NotChordsAlone, refusal.Reason);
            Assert.Equal(sentence, refusal.Sentence);
        }
    }

    // ---- What the tests above ask of a file

    // The chords of one line of a box, in order.
    private static List<Chord> ChordsOn(TextBox box, int line)
    {
        var start = StartOf(box, line);
        var length = Words.Lines(box.Words)[line].Length;
        return box.Chords.Where(chord => chord.At >= start && chord.At < start + Math.Max(1, length)).ToList();
    }

    private static int StartOf(TextBox box, int line) => Words.Lines(box.Words).Take(line).Sum(before => before.Length + 1);

    // The nearest text box that is shown and is not blank: on a slide before this one if
    // there is one, else on one after.
    private static TextBox NearestWithWords(OpenSong song, int slide)
    {
        var boxes = song.TextBoxes.Where(box => !box.Blank && box.Words.Length > 0 && box.Slide != slide).ToList();
        return boxes.LastOrDefault(box => box.Slide < slide) ?? boxes.First(box => box.Slide > slide);
    }

    private static List<Slide.Types.Element> ElementsOf(Presentation presentation) =>
        (from cue in presentation.Cues
         from action in cue.Actions
         from element in action.Slide?.Presentation?.BaseSlide?.Elements ?? []
         select element).ToList();

    private static int CommonPrefix(byte[] one, byte[] other) => one.AsSpan().CommonPrefixLength(other);

    // Between two states of a file, nothing has changed but one text box's RTF and its
    // chord entries: with those two put back as they were, the file is what it was. And
    // of the RTF, where a line is named, not a byte outside that line's own.
    private static void NothingElseChanged(Presentation before, Presentation after, string element, int? line)
    {
        var was = Workbench.TextOf(before, element);
        var now = Workbench.TextOf(after.Clone(), element);
        var restored = after.Clone();
        var text = Workbench.TextOf(restored, element);
        if (line is { } changedLine)
        {
            var from = RtfWords.SpanOfLine(was.RtfData.Span, changedLine)!.Value;
            var oldRtf = was.RtfData.Span;
            var newRtf = now.RtfData.Span;
            var tail = oldRtf.Length - from.To;
            Assert.True(newRtf.Length >= from.From + tail, "the RTF is shorter than what lies outside the line");
            Assert.True(oldRtf[..from.From].SequenceEqual(newRtf[..from.From]), "the RTF before the line was changed");
            Assert.True(oldRtf[from.To..].SequenceEqual(newRtf[(newRtf.Length - tail)..]), "the RTF after the line was changed");
        }
        text.RtfData = was.RtfData;
        if (was.Attributes is null)
        {
            text.Attributes = null;
        }
        else
        {
            text.Attributes ??= new Graphics.Types.Text.Types.Attributes();
            var others = text.Attributes.CustomAttributes.Where(entry => entry.AttributeCase != CustomAttribute.AttributeOneofCase.Chord).ToList();
            Assert.Equal(was.Attributes.CustomAttributes.Where(entry => entry.AttributeCase != CustomAttribute.AttributeOneofCase.Chord), others);
            text.Attributes.CustomAttributes.Clear();
            text.Attributes.CustomAttributes.AddRange(was.Attributes.CustomAttributes.Select(entry => entry.Clone()));
        }
        Assert.Equal(before, restored);
    }
}
