// The chord sheet's model on real files: what the vectors cannot hold, since their songs
// are lists and not files. Each thing done on the sheet is asked of an `OpenSong`, which
// saves it, and the FILE is then read again (on copies of testdata/files, in a folder
// of the test's own).
//
// MINE, NOT A VECTOR, each of them. They expect what the Mac's tests of the same expect
// (ChordSheetOnAFileTests.swift).

using Pro7ChordEditor.ChordRules;
using Pro7ChordEditor.Tests;
using Xunit;

namespace Pro7ChordEditor.Song.Tests;

public sealed class ChordSheetOnAFileTests : IDisposable
{
    private const string Grace = "Amazing Grace.pro";
    private const string Shaped = "Chords With No Words.pro";
    private readonly Workbench bench = new();

    public void Dispose() => bench.Dispose();

    // The first row that is a line of words with at least this many of them.
    private static int WordsRow(ChordSheet sheet, int after = -1, int words = 3) =>
        Enumerable.Range(0, sheet.Rows.Count).First(row => row > after && sheet.Usable(row) && !sheet.Rows[row].Alone && sheet.Stops(row).Count >= words);

    private static int RowWhere(ChordSheet sheet, Func<SheetRow, bool> what) => Enumerable.Range(0, sheet.Rows.Count).First(row => what(sheet.Rows[row]));

    // A text box, said whole: every part of it that a song hands over.
    private static string Said(TextBox box) =>
        StandInSong.Said(box.Slide, box.Element, box.Words, box.Chords) + " group " + Spelt.Of(box.Group) + " starts " + Spelt.B(box.StartsItsGroup)
        + " blank " + Spelt.B(box.Blank) + " colour " + box.GroupColour;

    private static IEnumerable<string> Said(IEnumerable<TextBox> boxes) => boxes.Select(Said);

    private static IEnumerable<string> OnDisk(OpenSong song) => Said(new OpenSong(song.Path).TextBoxes);

    private static IEnumerable<string> Names(SheetRow row) => row.Chords.Select(chord => chord.Name);

    [Fact]
    public void AChordPutOnTheSheetIsInTheFileInThatTextBoxAndDeleteTakesItOffAgain()
    {
        var song = bench.Open(Grace);
        var sheet = new ChordSheet(song);
        var before = song.TextBoxes;
        var row = WordsRow(sheet);
        var line = sheet.Rows[row];
        // A place of the line that has no chord yet.
        var at = sheet.Stops(row).First(stop => sheet.ChordAt(row, stop).Length == 0);
        sheet.SetSpot(row, at);
        sheet.PutOfKey(4);
        var name = sheet.InKey[3];
        Assert.Equal(name, sheet.ChordAt(row, at));

        var read = new OpenSong(song.Path).TextBoxes;
        Assert.Contains(new Chord(line.Start + at, name), read[line.Block].Chords);
        for (var index = 0; index < read.Count; index++)
        {
            if (index != line.Block)
            {
                Assert.Equal(Said(before[index]), Said(read[index]));
            }
        }
        sheet.TakeAtSpot();
        Assert.Equal(Said(before), OnDisk(song));
    }

    [Fact]
    public void AChordMovedToAnotherSlideIsTwoChangesThatUndoAsOneAndLeaveTheFilesBytesAsTheyWere()
    {
        var song = bench.Open(Grace);
        var bytes = File.ReadAllBytes(song.Path);
        var sheet = new ChordSheet(song);
        var from = Enumerable.Range(0, sheet.Rows.Count).First(row => sheet.Usable(row) && !sheet.Rows[row].Alone && sheet.Rows[row].Chords.Count > 0);
        var to = Enumerable.Range(0, sheet.Rows.Count).First(row => sheet.Usable(row) && !sheet.Rows[row].Alone && sheet.Rows[row].Slide != sheet.Rows[from].Slide);
        var chord = sheet.Rows[from].Chords[0];
        sheet.Lift(from, chord.At);
        sheet.Carry(to, 1);
        sheet.Drop(copy: false);
        Assert.Equal((chord.Name, ""), (sheet.ChordAt(to, 1), sheet.ChordAt(from, chord.At)));
        Assert.Equal(new Spot(to, 1), sheet.Spot);
        Assert.NotEqual(bytes, File.ReadAllBytes(song.Path));
        song.Undo();
        sheet.Reload();
        Assert.False(song.CanUndo);
        Assert.Equal((chord.Name, ""), (sheet.ChordAt(from, chord.At), sheet.ChordAt(to, 1)));
        Assert.Equal(bytes, File.ReadAllBytes(song.Path));
    }

    [Fact]
    public void AGroupsChordsPastedOntoAnotherGroupChangeEachLinesOwnTextBoxAndUndoAsOne()
    {
        var song = bench.Open(Grace);
        var bytes = File.ReadAllBytes(song.Path);
        var sheet = new ChordSheet(song);
        var first = Enumerable.Range(0, sheet.Rows.Count).First(row => sheet.Usable(row) && !sheet.Rows[row].Alone && sheet.Rows[row].Chords.Count > 0);
        var nextGroup = Enumerable.Range(0, sheet.Rows.Count).First(row => row > first && sheet.Rows[row].Kind == SheetRowKind.Group);
        var target = WordsRow(sheet, after: nextGroup, words: 1);
        sheet.SetSpot(first, 0);
        Assert.Contains("copied from", sheet.Copy(whole: true), StringComparison.Ordinal);
        sheet.SetSpot(target, 0);
        var pasted = sheet.Paste();
        Assert.True(pasted is { Lines: > 1, Refused: 0 });
        Assert.NotEqual(bytes, File.ReadAllBytes(song.Path));
        Assert.Equal(Names(sheet.Rows[first]).Take(sheet.Rows[target].Chords.Count), Names(sheet.Rows[target]));
        // More than one slide's text box was changed, and each is in the file.
        Assert.True(song.TextBoxes.Zip(new OpenSong(PathOf(Grace)).TextBoxes).Count(pair => Said(pair.First) != Said(pair.Second)) > 1);
        Assert.Equal(Said(song.TextBoxes), OnDisk(song));
        song.Undo();
        Assert.False(song.CanUndo);
        Assert.Equal(bytes, File.ReadAllBytes(song.Path));
    }

    // The test file itself, which no test changes.
    private static string PathOf(string testFile) =>
        Path.Combine(TestData.Root, "files", TestData.PresentationPaths().Single(path => path.EndsWith("/" + testFile, StringComparison.Ordinal)).Replace('/', Path.DirectorySeparatorChar));

    [Fact]
    public void AChordDraggedOntoASlideThatHasNoTextBoxAndALineOfChordsAlonePastedOntoABlankSlide()
    {
        var song = bench.Open(Shaped);
        var bytes = File.ReadAllBytes(song.Path);
        var sheet = new ChordSheet(song);
        var said = new List<string>();
        sheet.Refused = refusal => said.Add(refusal.Sentence);
        var four = RowWhere(sheet, row => row.Slide == 3 && row.Alone);
        var first = RowWhere(sheet, row => row.Slide == 0 && row.Blank);
        var last = RowWhere(sheet, row => row.Slide == 11 && row.Blank);
        Assert.Equal(["E", "B", "C#m", "A"], Names(sheet.Rows[four]));

        // The second of the four, dragged to the last slide, which has nothing on it.
        sheet.Lift(four, 2);
        sheet.Carry(last, 0);
        sheet.Drop(copy: false);
        Assert.Equal(["E", "C#m", "A"], Names(sheet.Rows[four]));
        Assert.Equal([new Chord(0, "B")], sheet.Rows[last].Chords);
        Assert.False(sheet.Rows[last].Blank);
        Assert.NotEqual("", song.TextBoxes[11].Element);
        Assert.Equal(Chords.Placeholders(3), song.TextBoxes[3].Words);
        Assert.Equal(Said(song.TextBoxes), OnDisk(song));
        song.Undo();
        Assert.False(song.CanUndo);
        Assert.Equal(bytes, File.ReadAllBytes(song.Path));
        sheet.Reload();

        // The line of four, copied, and pasted onto the first slide, which is blank.
        sheet.SetSpot(four, 0);
        Assert.NotNull(sheet.Copy(whole: false));
        sheet.SetSpot(first, 0);
        Assert.Equal(new ChordSheet.Pasted(Lines: 1, Chords: 4), sheet.Paste());
        Assert.Equal(["E", "B", "C#m", "A"], Names(sheet.Rows[first]));
        Assert.Equal([0, 2, 4, 6], sheet.Rows[first].Chords.Select(chord => chord.At));
        Assert.Equal(Chords.Placeholders(4), song.TextBoxes[0].Words);
        Assert.Equal(Said(song.TextBoxes), OnDisk(song));
        song.Undo();
        Assert.False(song.CanUndo);
        Assert.Equal(bytes, File.ReadAllBytes(song.Path));
        Assert.Empty(said);
    }

    // The last chord taken off a line of chords alone that has words over it: the line
    // is one zero-width space, its row is there still with the spot on it, and a chord
    // is put on it again. So for one that is the whole of its slide.
    // As the Mac's `carriedInFromTheStrip`, on the same file: one of the key's chords
    // carried in from the strip and let go on a place is in the file, as its key would
    // have put it with the spot there; undone, the file is byte for byte what it was.
    [Fact]
    public void OneOfTheKeysChordsCarriedInFromTheStripIsInTheFileAsItsKeyWouldHavePutIt()
    {
        var song = bench.Open(Shaped);
        var bytes = File.ReadAllBytes(song.Path);
        var sheet = new ChordSheet(song);
        var said = new List<string>();
        sheet.Refused = refusal => said.Add(refusal.Sentence);
        var name = sheet.InKey[3];
        var words = WordsRow(sheet);
        var at = sheet.Stops(words).First(stop => sheet.ChordAt(words, stop).Length == 0);
        var four = RowWhere(sheet, row => row.Slide == 3 && row.Alone);
        var first = RowWhere(sheet, row => row.Slide == 0 && row.Blank);
        var last = RowWhere(sheet, row => row.Slide == 11 && row.Blank);
        Assert.Equal(4, sheet.Rows[four].Chords.Count);
        Assert.Equal("", song.TextBoxes[11].Element);

        // The song as it is read from its file: each box's words and chords. (Not its
        // boxes' ids: the box a slide with none is given has a new one each time.)
        List<string> Read() => [.. new OpenSong(song.Path).TextBoxes.Select(box => Spelt.N(box.Slide) + " " + Spelt.Of(box.Words) + " " + Spelt.Of(box.Chords))];

        foreach (var (row, place) in new[] { (words, at), (four, 2), (four, 8), (first, 0), (last, 0) })
        {
            var which = "row " + Spelt.N(row) + ", place " + Spelt.N(place);
            sheet.Carry(name, row, place);
            Assert.Equal(new Spot(row, place), sheet.Spot);
            Assert.True(sheet.DropOfKey(), which);
            Assert.Equal((name, new Spot(row, place)), (sheet.ChordAt(row, place), sheet.Spot));
            var carried = Read();
            Assert.False(bytes.AsSpan().SequenceEqual(File.ReadAllBytes(song.Path)), which);
            song.Undo();
            Assert.False(song.CanUndo);
            Assert.True(bytes.AsSpan().SequenceEqual(File.ReadAllBytes(song.Path)), which);
            sheet.Reload();
            // The key 4, with the spot there, makes the same of the file.
            sheet.SetSpot(row, place);
            sheet.PutOfKey(4);
            Assert.Equal(carried, Read());
            song.Undo();
            Assert.True(bytes.AsSpan().SequenceEqual(File.ReadAllBytes(song.Path)), which);
            sheet.Reload();
        }
        // In a chord's place it took that chord's; past the last it was one more.
        sheet.Carry(name, four, 2);
        sheet.DropOfKey();
        Assert.Equal(["E", name, "C#m", "A"], Names(sheet.Rows[four]));
        sheet.Carry(name, four, 8);
        sheet.DropOfKey();
        Assert.Equal(["E", name, "C#m", "A", name], Names(sheet.Rows[four]));
        song.Undo();
        song.Undo();
        Assert.False(song.CanUndo);
        sheet.Reload();

        // Let go off the places: nothing is asked of the song at all.
        sheet.Carry(name, words, at);
        sheet.CarryOff();
        Assert.False(sheet.DropOfKey());
        Assert.True(bytes.AsSpan().SequenceEqual(File.ReadAllBytes(song.Path)));
        Assert.False(song.CanUndo);
        Assert.Empty(said);

        // While ProPresenter is running it is refused like any change, in the song's
        // own sentence, and the file is as it was.
        bench.ProPresenterIsRunning = true;
        sheet.Carry(name, words, at);
        Assert.False(sheet.DropOfKey());
        Assert.Equal(["ProPresenter is running. Quit it to change chords here. Nothing was changed."], said);
        Assert.Equal(("", null), (sheet.ChordAt(words, at), sheet.Dragged));
        Assert.True(bytes.AsSpan().SequenceEqual(File.ReadAllBytes(song.Path)));
    }

    [Fact]
    public void TheLastChordTakenOffALineOfChordsAloneLeavesItALineOfChordsAloneStill()
    {
        var song = bench.Open(Shaped);
        var bytes = File.ReadAllBytes(song.Path);
        var sheet = new ChordSheet(song);
        var said = new List<string>();
        sheet.Refused = refusal => said.Add(refusal.Sentence);
        var rows = sheet.Rows.Count;
        var row = RowWhere(sheet, one => one.Slide == 7 && one.Alone);
        Assert.Equal(["B", "E"], Names(sheet.Rows[row]));
        sheet.SetSpot(row, 0);
        sheet.TakeAtSpot();
        Assert.Equal([new Chord(0, "E")], sheet.Rows[row].Chords);
        Assert.Equal(" ", sheet.Rows[row].Text);
        sheet.SetSpot(row, 0);
        sheet.TakeAtSpot();
        // Its row is as it was but for its chords: a line of chords alone with none,
        // that takes the spot. It hung its chords on plain spaces; it is one zero-width
        // space now, which a stray space cannot be taken for.
        Assert.Equal(rows, sheet.Rows.Count);
        Assert.True(sheet.Rows[row].Alone && !sheet.Rows[row].Blank && sheet.Usable(row));
        Assert.Empty(sheet.Rows[row].Chords);
        Assert.Equal(Chords.Placeholders(1), sheet.Rows[row].Text);
        Assert.Equal(new Spot(row, 0), sheet.Spot);
        Assert.Equal([0], sheet.Stops(row));
        Assert.Equal(["Naked come to thee for dress", Chords.Placeholders(1)], Words.Lines(song.TextBoxes[7].Words));
        Assert.Equal(["E", "A"], song.TextBoxes[7].Chords.Select(chord => chord.Name));
        // A chord put on it again, and a second after it.
        Assert.True(sheet.Put(row, 0, "F#m"));
        Assert.True(sheet.Put(row, 2, "B"));
        Assert.Equal([new Chord(0, "F#m"), new Chord(2, "B")], sheet.Rows[row].Chords);
        Assert.Equal([new Chord(0, "E"), new Chord(11, "A"), new Chord(29, "F#m"), new Chord(31, "B")], song.TextBoxes[7].Chords);
        Assert.Equal(Chords.Placeholders(2), Words.Lines(song.TextBoxes[7].Words)[1]);
        Assert.Equal(Said(song.TextBoxes), OnDisk(song));
        Assert.Empty(said);
        while (song.CanUndo)
        {
            song.Undo();
        }
        sheet.Reload();
        Assert.Equal(rows, sheet.Rows.Count);
        Assert.Equal(["B", "E"], Names(sheet.Rows[row]));
        Assert.Equal(bytes, File.ReadAllBytes(song.Path));

        // The fourth slide is nothing but a line of four chords on zero-width spaces.
        // With all four taken off it is that line still, and not a blank slide.
        var whole = RowWhere(sheet, one => one.Slide == 3 && one.Alone);
        for (var times = 0; times < 4; times++)
        {
            sheet.SetSpot(whole, 0);
            sheet.TakeAtSpot();
        }
        Assert.True(sheet.Rows[whole].Alone && !sheet.Rows[whole].Blank);
        Assert.Empty(sheet.Rows[whole].Chords);
        Assert.Equal(Chords.Placeholders(1), song.TextBoxes[3].Words);
        Assert.False(song.TextBoxes[3].Blank);
        Assert.True(sheet.Put(whole, 0, "E"));
        Assert.Equal([new Chord(0, "E")], song.TextBoxes[3].Chords);
        Assert.Equal(Chords.Placeholders(1), song.TextBoxes[3].Words);
        while (song.CanUndo)
        {
            song.Undo();
        }
        Assert.Equal(bytes, File.ReadAllBytes(song.Path));

        // The seventh hangs its one chord on one plain space, and is the whole of its
        // slide. With the chord taken off it is neither a blank slide nor gone, as a box
        // that held a stray space would be: it is the line still.
        sheet.Reload();
        var one = RowWhere(sheet, row7 => row7.Slide == 6 && row7.Alone);
        Assert.Equal(" ", sheet.Rows[one].Text);
        sheet.SetSpot(one, 0);
        sheet.TakeAtSpot();
        Assert.True(sheet.Rows[one].Alone && !sheet.Rows[one].Blank);
        Assert.Equal(Chords.Placeholders(1), sheet.Rows[one].Text);
        Assert.False(song.TextBoxes[6].Blank);
        Assert.Equal(Chords.Placeholders(1), song.TextBoxes[6].Words);
        Assert.Equal(Said(song.TextBoxes), OnDisk(song));
        song.Undo();
        Assert.Equal(bytes, File.ReadAllBytes(song.Path));
    }

    // On a line of chords alone a chord is renamed, one more is put at its end and one
    // is taken off, and each is in the file; so is the first chord of a blank slide,
    // which Delete takes off again leaving the file's bytes as they were.
    [Fact]
    public void OnEveryLineOfChordsAloneOfEveryTestFileAChordIsRenamedOneMoreIsPutAndOneIsTakenOff()
    {
        var lines = 0;
        var blanks = 0;
        var names = TestData.PresentationPaths().Where(path => path.StartsWith("made-by-simple-presenter/", StringComparison.Ordinal)).Select(path => Path.GetFileName(path)).Append(Shaped).Distinct();
        foreach (var name in names)
        {
            var song = bench.Open(name);
            var sheet = new ChordSheet(song);
            var said = new List<string>();
            sheet.Refused = refusal => said.Add(refusal.Sentence);
            for (var row = 0; row < sheet.Rows.Count; row++)
            {
                if (!sheet.Usable(row) || !sheet.Rows[row].Alone)
                {
                    continue;
                }
                var which = name + ", row " + Spelt.N(row);
                var bytes = File.ReadAllBytes(song.Path);
                var chords = sheet.Rows[row].Chords;
                var had = chords.Select(chord => chord.Name).ToList();
                // One more at its end (on a blank slide, its first).
                Assert.True(sheet.Put(row, chords.Count * 2, "Em"), which);
                Assert.Equal(had.Append("Em"), Names(sheet.Rows[row]));
                Assert.Equal(Enumerable.Range(0, chords.Count + 1).Select(slot => slot * 2), sheet.Rows[row].Chords.Select(chord => chord.At));
                Assert.True(sheet.Rows[row].Alone && !sheet.Rows[row].Blank, which);
                Assert.Equal(Said(song.TextBoxes), OnDisk(song));
                if (chords.Count == 0)
                {
                    // A blank slide's first chord, taken off again.
                    blanks++;
                    Assert.True(sheet.Take(row, 0), which);
                    Assert.True(sheet.Rows[row].Blank, which);
                    Assert.Empty(sheet.Rows[row].Chords);
                    Assert.True(bytes.AsSpan().SequenceEqual(File.ReadAllBytes(song.Path)), which);
                }
                else
                {
                    lines++;
                    // Its first renamed, and then taken off: the rest move up.
                    Assert.True(sheet.Put(row, 0, "Bm7"), which);
                    Assert.Equal(had.Skip(1).Prepend("Bm7").Append("Em"), Names(sheet.Rows[row]));
                    Assert.True(sheet.Take(row, 0), which);
                    Assert.Equal(had.Skip(1).Append("Em"), Names(sheet.Rows[row]));
                    Assert.Equal(Enumerable.Range(0, chords.Count).Select(slot => slot * 2), sheet.Rows[row].Chords.Select(chord => chord.At));
                    Assert.Equal(Said(song.TextBoxes), OnDisk(song));
                    // And every step of it taken back.
                    while (song.CanUndo)
                    {
                        song.Undo();
                    }
                    sheet.Reload();
                    Assert.Equal(chords, sheet.Rows[row].Chords);
                    Assert.True(bytes.AsSpan().SequenceEqual(File.ReadAllBytes(song.Path)), which);
                }
            }
            Assert.Empty(said);
        }
        Assert.True(lines >= 9 && blanks >= 6, Spelt.N(lines) + " lines of chords alone and " + Spelt.N(blanks) + " blank slides were tried");
    }
}
