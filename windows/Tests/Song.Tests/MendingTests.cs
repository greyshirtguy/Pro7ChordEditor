// The checks of spec/08-repair.md, and the putting right of what they find: the page's
// "Proofs to write", held to testdata/files/expected/repairs.json.
//
//   - Each file of testdata/files: the checks find what that file says, in the damaged
//     ones and in the clean ones, where it is nothing.
//   - Each damaged file, mended as that file says (`putRight`), has exactly the chord
//     entries of the clean file it was made from.
//   - Mending changes nothing else in the file.
//
// And, MINE and not from the test data: what a mending is as a change to a song (one
// save, one step of undo, a copy kept first, refused when any save would be); what is
// left alone; the text boxes the editors do not show; and check 0, a chord that is on
// no character of its words.

using System.Text.Json;
using Google.Protobuf;
using Pro.SerializationInterop.RVProtoData;
using Pro7ChordEditor.ChordRules;
using Pro7ChordEditor.Tests;
using Xunit;

namespace Pro7ChordEditor.Song.Tests;

public sealed class MendingTests : IDisposable
{
    private const string Damaged = "damaged";
    private const string Clean = "clean";
    private readonly Workbench bench = new();

    public void Dispose() => bench.Dispose();

    private static string InTestData(string name) =>
        Path.Combine(TestData.Root, "files", TestData.PresentationPaths().Single(path => path.EndsWith("/" + name, StringComparison.Ordinal)));

    private static JsonDocument Repairs() => JsonDocument.Parse(TestData.InFiles("expected/repairs.json"));

    private static string Name(JsonElement entry) => entry.GetProperty("file").GetString()!;

    // A box's chord entries as "start-end name", in the file's order.
    private static string Entries(CheckedBox box) => string.Join(", ", box.Chords.Select(chord => chord.Start + "-" + chord.End + " " + chord.Name));

    private static string Entries(JsonElement box) => string.Join(
        ", ",
        box.GetProperty("chords").EnumerateArray().Select(chord => chord.GetProperty("start").GetInt32() + "-" + chord.GetProperty("end").GetInt32() + " " + chord.GetProperty("name").GetString()));

    private static List<string> EntriesOf(string path) => [.. Checks.Boxes(Workbench.Read(path)).Select(Entries)];

    // A presentation with every chord entry taken out of it: what a mending must leave
    // exactly as it was.
    private static Presentation WithoutChords(Presentation presentation)
    {
        var bare = presentation.Clone();
        foreach (var text in Checks.Texts(bare))
        {
            var others = text.Attributes?.CustomAttributes.Where(attribute => !Reading.IsAChord(attribute)).ToList() ?? [];
            text.Attributes?.CustomAttributes.Clear();
            text.Attributes?.CustomAttributes.AddRange(others);
        }
        return bare;
    }

    // ---- The first proof

    [Fact]
    public void TheChecksFindInEachTestFileWhatIsExpectedOfIt()
    {
        using var expected = Repairs();
        var wrong = new List<string>();
        var files = 0;
        foreach (var list in new[] { Damaged, Clean })
        {
            foreach (var entry in expected.RootElement.GetProperty(list).EnumerateArray())
            {
                var found = Checks.Read(InTestData(Name(entry)));
                files++;
                var shift = entry.GetProperty("lineBreakShift");
                var key = entry.GetProperty("key");
                var want = Said(
                    entry.GetProperty("rangesOverLineBreaks").GetInt32(),
                    new OldEditor.Shift(
                        shift.GetProperty("chordsBelowTheFirstLine").GetInt32(),
                        shift.GetProperty("atTheStartOfAWord").GetInt32(),
                        shift.GetProperty("ifMovedBack").GetInt32(),
                        shift.GetProperty("likely").GetBoolean()),
                    key.GetProperty("stated").GetString()!,
                    key.GetProperty("doubtful").GetBoolean(),
                    key.GetProperty("suggested").GetString()!,
                    key.GetProperty("likeliest").EnumerateArray().Select(one => one.GetString()!));
                // (The file has the four likeliest keys at most.)
                var got = Said(found.ShownTwice, found.Shift, found.StatedKey, found.Key.Doubtful, found.Key.Suggested, found.Key.Likeliest.Take(4));
                if (!string.Equals(got, want, StringComparison.Ordinal))
                {
                    wrong.Add($"{Name(entry)}:\n      found    {got}\n      expected {want}");
                }
                // And, whatever the numbers: a damaged file has the old editor's mark,
                // and in a clean one nothing is found.
                if (list == Damaged && found.ShownTwice == 0)
                {
                    wrong.Add($"{Name(entry)}: no chord is found to be shown twice");
                }
                if (list == Clean && (found.ShownTwice != 0 || found.Shift.Likely || found.Key.Doubtful || found.KeyIsTheOldEditors))
                {
                    wrong.Add($"{Name(entry)}: something is found in a clean file");
                }
            }
        }
        Assert.Equal(10, files);
        Assert.True(wrong.Count == 0, $"{wrong.Count} files are not found as expected. The first:\n  " + string.Join("\n  ", wrong.Take(4)));

        static string Said(int shownTwice, OldEditor.Shift shift, string stated, bool doubtful, string suggested, IEnumerable<string> likeliest) =>
            $"{shownTwice} shown twice; of {shift.ChordsBelowTheFirstLine} below a first line {shift.AtTheStartOfAWord} start a word and {shift.IfMovedBack} would, likely {shift.Likely}; "
            + $"key \"{stated}\" doubtful {doubtful}, suggested \"{suggested}\", likeliest {string.Join(" ", likeliest)}";
    }

    // Check 1 again, by another road: each test file's own description (tools/look.py)
    // says of every chord whether its range runs over a line break. All twelve files,
    // the one ProPresenter wrote and the one in the shapes of real songs among them.
    [Fact]
    public void TheChordsFoundToBeShownTwiceAreThoseEachFilesDescriptionSays()
    {
        var files = 0;
        foreach (var path in TestData.PresentationPaths())
        {
            var name = Path.GetFileNameWithoutExtension(path);
            using var described = JsonDocument.Parse(TestData.InFiles("expected/" + name + ".json"));
            var said = described.RootElement.GetProperty("textBoxes").EnumerateArray()
                .Sum(box => box.GetProperty("chords").EnumerateArray().Count(chord => chord.GetProperty("overLineBreak").GetBoolean()));
            Assert.True(said == Checks.Read(InTestData(name + ".pro")).ShownTwice, name);
            files++;
        }
        Assert.Equal(12, files);
    }

    // ---- The second proof

    [Fact]
    public void EachDamagedFileMendedAsItsEntrySaysHasExactlyTheChordEntriesOfTheFileItWasMadeFrom()
    {
        using var expected = Repairs();
        var files = 0;
        foreach (var entry in expected.RootElement.GetProperty(Damaged).EnumerateArray())
        {
            var putRight = entry.GetProperty("putRight");
            var want = putRight.GetProperty("textBoxes").EnumerateArray().Select(Entries).ToList();
            var clean = EntriesOf(InTestData(entry.GetProperty("madeFrom").GetString()!));
            var shownTwice = entry.GetProperty("rangesOverLineBreaks").GetInt32();

            var song = bench.Open(Name(entry));
            if (putRight.GetProperty("movedBack").GetBoolean())
            {
                song.MoveBack();
            }
            else
            {
                Assert.Equal(shownTwice, song.Mend());
            }
            var got = EntriesOf(song.Path);
            Assert.True(want.SequenceEqual(got), $"{Name(entry)} is mended as\n  {string.Join("\n  ", got)}\nand not as\n  {string.Join("\n  ", want)}");
            Assert.True(clean.SequenceEqual(got), $"{Name(entry)}, mended, has not the chord entries of the file it was made from");
            Assert.True(putRight.GetProperty("isThenTheFileItWasMadeFrom").GetBoolean());
            // More than its chord entries: where its key was not damaged too, the file
            // IS then the one it was made from, to the byte. (That is these files' luck
            // as much as a rule: they were damaged by a tool that writes a file as this
            // app does. It is held all the same, since it could not be so if a mending
            // touched anything it should not.)
            if (!entry.GetProperty("key").GetProperty("doubtful").GetBoolean())
            {
                Assert.True(File.ReadAllBytes(InTestData(entry.GetProperty("madeFrom").GetString()!)).AsSpan().SequenceEqual(File.ReadAllBytes(song.Path)), Name(entry));
            }
            // Nothing is then found in it that is to be mended.
            Assert.Equal(0, song.Findings.ShownTwice);
            Assert.False(song.Findings.Shift.Likely);
            Assert.Equal(0, song.Mend());
            files++;
        }
        Assert.Equal(4, files);
    }

    // The one damaged file whose chords were put on by hand: mended first (which is
    // offered, and safe) and its chords moved back afterwards (which is offered only
    // with the song shown both ways), it comes to the same.
    [Fact]
    public void ASongMendedAndThenItsChordsMovedBackIsTheSameAsOneWhoseChordsWereMovedBackAtOnce()
    {
        var clean = EntriesOf(InTestData("Amazing Grace.pro"));
        var song = bench.Open("Amazing Grace (Chords).pro");
        Assert.True(song.Findings.Shift.Likely);
        Assert.Equal(6, song.Mend());
        // Mended, its ranges are right and its chords are where they were: still a
        // character to the left on every second line.
        Assert.Equal(0, song.Findings.ShownTwice);
        Assert.True(song.Findings.Shift.Likely);
        Assert.False(clean.SequenceEqual(EntriesOf(song.Path)));
        song.MoveBack();
        Assert.True(clean.SequenceEqual(EntriesOf(song.Path)));
        Assert.False(song.Findings.Shift.Likely);
        // Each was a step of undo.
        song.Undo();
        Assert.True(song.Findings.Shift.Likely);
        Assert.Equal(0, song.Findings.ShownTwice);
        song.Undo();
        Assert.Equal(6, song.Findings.ShownTwice);
        Assert.False(song.CanUndo);
        Assert.True(TestData.InFiles("as-the-old-windows-app-left-them/Amazing Grace (Chords).pro").AsSpan().SequenceEqual(File.ReadAllBytes(song.Path)));
    }

    // Moving back moves the chords below a first line and no others, each by one
    // character for each line break above it.
    [Fact]
    public void MovingBackMovesEachChordBelowAFirstLineByOneForEachLineBreakAboveIt()
    {
        var song = bench.Open("Amazing Grace (Chords).pro");
        var before = Checks.Boxes(Workbench.Read(song.Path));
        song.MoveBack();
        var after = Checks.Boxes(Workbench.Read(song.Path));
        Assert.Equal(before.Count, after.Count);
        var moved = 0;
        for (var box = 0; box < before.Count; box++)
        {
            var words = before[box].Words;
            Assert.Equal(words, after[box].Words);
            Assert.Equal(before[box].Chords.Select(chord => chord.Name), after[box].Chords.Select(chord => chord.Name));
            for (var n = 0; n < before[box].Chords.Count; n++)
            {
                // (Where it was meant: counted along the words with their line breaks
                // left out, a chord at 58 of a box whose first line is 33 long was meant
                // for 59.)
                var was = before[box].Chords[n].Start;
                var breaksAbove = 0;
                var left = was;
                foreach (var line in words.Split('\n').SkipLast(1))
                {
                    if (left < line.Length)
                    {
                        break;
                    }
                    left -= line.Length;
                    breaksAbove++;
                }
                Assert.Equal(was + breaksAbove, after[box].Chords[n].Start);
                moved += breaksAbove > 0 ? 1 : 0;
            }
        }
        Assert.Equal(12, moved);
    }

    // ---- The third proof

    [Fact]
    public void MendingChangesNothingInAFileButTheStretchesItsChordsAreWrittenOver()
    {
        using var expected = Repairs();
        foreach (var entry in expected.RootElement.GetProperty(Damaged).EnumerateArray())
        {
            var path = bench.CopyOf(Name(entry));
            var before = Workbench.Read(path);
            var song = bench.OpenAt(path);
            Assert.True(song.Mend() > 0);
            var after = Workbench.Read(path);

            // Everything that is not a chord entry is as it was, to the byte, and in
            // the same order: text boxes, their words, their other settings, the key,
            // and whatever the file has that this app has never heard of.
            Assert.True(WithoutChords(before).ToByteArray().AsSpan().SequenceEqual(WithoutChords(after).ToByteArray()), Name(entry));
            // Every chord is still there, in its text box, with its name, starting where
            // it started: only where a chord's stretch ends has changed.
            var was = Checks.Boxes(before);
            var now = Checks.Boxes(after);
            Assert.Equal(was.Count, now.Count);
            var changed = 0;
            for (var box = 0; box < was.Count; box++)
            {
                Assert.Equal(was[box].Chords.Select(chord => (chord.Start, chord.Name)), now[box].Chords.Select(chord => (chord.Start, chord.Name)));
                changed += was[box].Chords.Zip(now[box].Chords).Count(pair => pair.First.End != pair.Second.End);
            }
            Assert.True(changed >= entry.GetProperty("rangesOverLineBreaks").GetInt32(), Name(entry));
            // And each ends where the rule for a chord's stretch says (ranges.json), so
            // one that was right before is as it was.
            for (var box = 0; box < was.Count; box++)
            {
                var right = Chords.Ranges(was[box].Words, was[box].Chords.Select(chord => new Chord(chord.Start, chord.Name)));
                for (var n = 0; n < was[box].Chords.Count; n++)
                {
                    Assert.Equal(right[n].End, now[box].Chords[n].End);
                }
            }
        }
    }

    // The song whose key the old editor reset, mended and given the key that is offered
    // for it, is then the song it was made from in everything a file says: its chords,
    // and its key.
    [Fact]
    public void TheSongWhoseKeyWasResetIsTheOneItWasMadeFromOnceMendedAndGivenTheKeyThatIsOffered()
    {
        var song = bench.Open("A Song - In A Minor Key (Chords).pro");
        var found = song.Findings;
        Assert.Equal(("Ab", true, "F#m"), (found.StatedKey, found.Key.Doubtful, found.Key.Suggested));
        // A flat, doubtful, in a song with the old editor's mark: nearly certainly its
        // doing.
        Assert.True(found.KeyIsTheOldEditors);
        song.Mend();
        // (Mended, the song has that mark no longer, and its key is only doubtful.)
        Assert.True(song.Findings.Key.Doubtful);
        Assert.False(song.Findings.KeyIsTheOldEditors);
        song.SetKey(found.Key.Suggested);
        Assert.Equal(("F#m", false, ""), (song.Findings.StatedKey, song.Findings.Key.Doubtful, song.Findings.Key.Suggested));

        var mended = Workbench.Read(song.Path);
        var clean = Workbench.Read(InTestData("A Song - In A Minor Key.pro"));
        Assert.Equal(clean.Music, mended.Music);
        Assert.True(EntriesOf(InTestData("A Song - In A Minor Key.pro")).SequenceEqual(EntriesOf(song.Path)));
        Assert.Equal(clean, mended);
        // To the byte.
        Assert.True(File.ReadAllBytes(InTestData("A Song - In A Minor Key.pro")).AsSpan().SequenceEqual(File.ReadAllBytes(song.Path)));
    }

    // ---- What a mending is, as a change to a song

    [Fact]
    public void AMendingIsOneSaveAndOneStepOfUndoWithACopyKeptFirstWhateverWasKeptBefore()
    {
        var song = bench.Open("It Is Well (Chords).pro");
        var asItWas = File.ReadAllBytes(song.Path);
        // A change before it, which keeps the copy that a first change keeps.
        song.SetKey("D");
        var withItsKey = File.ReadAllBytes(song.Path);
        Assert.Equal([CopyReason.BeforeAChange], bench.Copies.Select(copy => copy.Why));

        Assert.Equal(4, song.Mend());
        var mended = File.ReadAllBytes(song.Path);
        Assert.False(mended.AsSpan().SequenceEqual(withItsKey));
        // The copy is of the file as it was just before the mending, and says why it
        // was kept.
        Assert.Equal([CopyReason.BeforeAChange, CopyReason.BeforeMending], bench.Copies.Select(copy => copy.Why));
        Assert.True(bench.Copies[1].Bytes.AsSpan().SequenceEqual(withItsKey));
        Assert.Equal(song.Path, bench.Copies[1].Path);
        Assert.True(song.FileIsAsLastRead);

        // One undo takes all of it back, in every text box; one redo does it again.
        song.Undo();
        Assert.True(File.ReadAllBytes(song.Path).AsSpan().SequenceEqual(withItsKey));
        Assert.Equal(4, song.Findings.ShownTwice);
        song.Redo();
        Assert.True(File.ReadAllBytes(song.Path).AsSpan().SequenceEqual(mended));
        Assert.False(song.CanRedo);
        song.Undo();
        song.Undo();
        Assert.True(File.ReadAllBytes(song.Path).AsSpan().SequenceEqual(asItWas));
        Assert.False(song.CanUndo);
        // (Undoing and doing again are saves like any other, and keep no copy.)
        Assert.Equal(2, bench.Copies.Count);

        // A change after a mending keeps no further copy: one was kept in this sitting.
        song.Redo();
        song.Redo();
        var box = song.TextBoxes[0];
        song.SetChords(box.Slide, box.Element, [.. box.Chords, new Chord(1, "Am")]);
        Assert.Equal(2, bench.Copies.Count);
    }

    [Fact]
    public void AMendingIsRefusedWhenAnySaveWouldBeAndThenChangesNothing()
    {
        var path = bench.CopyOf("Plain Song (Chords).pro");
        var asItWas = File.ReadAllBytes(path);

        // Opened only to be looked at.
        var looked = new OpenSong(path);
        Assert.Equal(3, looked.Findings.ShownTwice);
        Assert.Equal(Refusal.Why.OnlyForReading, Assert.Throws<Refusal>(() => looked.Mend()).Reason);
        Assert.Equal(Refusal.Why.OnlyForReading, Assert.Throws<Refusal>(looked.MoveBack).Reason);

        var song = bench.OpenAt(path);
        // While ProPresenter is running.
        bench.ProPresenterIsRunning = true;
        Assert.Equal(Refusal.Why.ProPresenterIsRunning, Assert.Throws<Refusal>(() => song.Mend()).Reason);
        Assert.Equal(Refusal.Why.ProPresenterIsRunning, Assert.Throws<Refusal>(song.MoveBack).Reason);
        bench.ProPresenterIsRunning = false;
        // When the copy cannot be kept.
        bench.KeepingACopyFails = "The disk is full.";
        Assert.Equal(Refusal.Why.CopyNotKept, Assert.Throws<Refusal>(() => song.Mend()).Reason);
        bench.KeepingACopyFails = null;

        Assert.True(File.ReadAllBytes(path).AsSpan().SequenceEqual(asItWas));
        Assert.Empty(bench.Copies);
        Assert.False(song.CanUndo);
        Assert.Equal(3, song.Findings.ShownTwice);

        // When something else has changed the file since it was read.
        File.WriteAllBytes(path, TestData.InFiles("made-by-simple-presenter/Plain Song.pro"));
        Assert.Equal(Refusal.Why.ChangedByAnother, Assert.Throws<Refusal>(() => song.Mend()).Reason);
        Assert.True(File.ReadAllBytes(path).AsSpan().SequenceEqual(TestData.InFiles("made-by-simple-presenter/Plain Song.pro")));

        // And then, asked again of the file as it was, it is done.
        File.WriteAllBytes(path, asItWas);
        var again = bench.OpenAt(path);
        Assert.Equal(3, again.Mend());
        Assert.Single(bench.Copies);
    }

    // ---- What is left alone

    [Fact]
    public void ASongInWhichNothingIsFoundIsLeftAloneWhateverItsRangesAre()
    {
        // A clean song: nothing is saved, nothing is copied, and there is nothing to undo.
        var clean = bench.Open("Amazing Grace.pro");
        var asItWas = File.ReadAllBytes(clean.Path);
        Assert.Equal(0, clean.Mend());
        Assert.True(File.ReadAllBytes(clean.Path).AsSpan().SequenceEqual(asItWas));
        Assert.Empty(bench.Copies);
        Assert.False(clean.CanUndo);

        // A song of Simple Presenter 0.8 to 0.82: on its line of chords alone each range
        // runs on to the next chord (0 to 2, 2 to 4, 4 to 5), which crosses no line
        // break. It is not that editor's doing, is not found, and is not mended.
        var presentation = Workbench.Read(InTestData("Plain Song.pro"));
        var alone = Checks.Texts(presentation).Single(text => Chords.IsPlaceholders(Reading.WordsOf(text)));
        var entries = alone.Attributes.CustomAttributes.Where(Reading.IsAChord).ToList();
        Assert.Equal([(0, 1), (2, 3), (4, 5)], entries.Select(entry => (entry.Range.Start, entry.Range.End)));
        entries[0].Range.End = 2;
        entries[1].Range.End = 4;
        var song = bench.OpenAt(bench.FileOf("as-simple-presenter-0.82-wrote-it.pro", presentation.ToByteArray()));
        var written = File.ReadAllBytes(song.Path);
        Assert.Equal(0, song.Findings.ShownTwice);
        Assert.Equal(0, song.Mend());
        Assert.True(File.ReadAllBytes(song.Path).AsSpan().SequenceEqual(written));
        Assert.Empty(bench.Copies);
        // It is written right when that box's chords are next changed.
        var box = song.TextBoxes.Single(one => Chords.IsPlaceholders(one.Words));
        song.SetChordsAlone(box.Slide, box.Element, 0, ["G", "D/F#", "Am"]);
        Assert.Equal("0-1 G, 2-3 D/F#, 4-5 Am", EntriesOf(song.Path)[^1]);
    }

    // ---- Every text box of the file, shown by the editors or not

    [Fact]
    public void ATextBoxTheEditorsDoNotShowIsCheckedAndMendedLikeAnyOther()
    {
        // The first slide's cue is switched off: ProPresenter does not show it, and nor
        // do the editors. What the old editor wrote on it is in the file all the same.
        var presentation = Workbench.Read(InTestData("Plain Song (Chords).pro"));
        var firstId = presentation.CueGroups[0].CueIdentifiers[0].String;
        presentation.Cues.Single(cue => cue.Uuid.String == firstId).IsEnabled = false;
        var song = bench.OpenAt(bench.FileOf("a-slide-switched-off.pro", presentation.ToByteArray()));
        Assert.Equal(3, song.TextBoxes.Count);
        Assert.DoesNotContain(song.TextBoxes, box => box.Words.StartsWith("One two", StringComparison.Ordinal));

        Assert.Equal(3, song.Findings.ShownTwice);
        Assert.Equal(3, song.Mend());
        Assert.True(EntriesOf(InTestData("Plain Song.pro")).SequenceEqual(EntriesOf(song.Path)));
    }

    // A song of more slides than steps of undo are kept: its mending is still one step,
    // all of which one undo takes back.
    [Fact]
    public void TheMendingOfASongOfMoreSlidesThanStepsAreKeptIsStillAllTakenBackByOneUndo()
    {
        var presentation = Workbench.Read(InTestData("Plain Song (Chords).pro"));
        var firstId = presentation.CueGroups[0].CueIdentifiers[0].String;
        var first = presentation.Cues.Single(cue => cue.Uuid.String == firstId);
        var more = OpenSong.UndoLimit + 50;
        for (var n = 0; n < more; n++)
        {
            var twin = first.Clone();
            twin.Uuid.String = OpenSong.NewId(firstId);
            presentation.Cues.Add(twin);
        }
        var song = bench.OpenAt(bench.FileOf("a-long-song.pro", presentation.ToByteArray()));
        var asItWas = File.ReadAllBytes(song.Path);
        Assert.Equal(4 + more, song.TextBoxes.Count);

        Assert.Equal(3 + more, song.Mend());
        Assert.Equal(0, song.Findings.ShownTwice);
        song.Undo();
        Assert.True(File.ReadAllBytes(song.Path).AsSpan().SequenceEqual(asItWas));
        Assert.False(song.CanUndo);
        song.Redo();
        Assert.Equal(0, song.Findings.ShownTwice);
    }

    // ---- Check 0: a chord that is on no character of its words

    [Fact]
    public void AChordOnALineBreakOrPastTheEndOfItsWordsIsHandedOverWhereItWillBeWrittenAndSoCanBeSeen()
    {
        // "One two three four" and "Five six seven eight": the break between them is at
        // 18, and the words are 39 long. The chord that was over "four" is put on the
        // break, and another far past the end.
        var presentation = Workbench.Read(InTestData("Plain Song.pro"));
        var text = Checks.Texts(presentation).First();
        Assert.Equal("One two three four\nFive six seven eight", Reading.WordsOf(text));
        var entries = text.Attributes.CustomAttributes;
        var d = entries.Single(entry => entry.Chord == "D");
        (d.Range.Start, d.Range.End) = (18, 19);
        var stray = d.Clone();
        stray.Chord = "A";
        (stray.Range.Start, stray.Range.End) = (99, 100);
        entries.Add(stray);
        var song = bench.OpenAt(bench.FileOf("chords-on-no-character.pro", presentation.ToByteArray()));
        var written = File.ReadAllBytes(song.Path);

        // Handed over: on the character before the break, and on the last character.
        var box = song.TextBoxes[0];
        Assert.Equal([new Chord(0, "G"), new Chord(17, "D"), new Chord(28, "Em"), new Chord(38, "A")], box.Chords);
        // So the sheet has each on its line, and the text says each.
        var sheet = new ChordSheet(song);
        var line = Enumerable.Range(0, sheet.Rows.Count).First(row => sheet.Rows[row].Text == "One two three four");
        Assert.Equal([new Chord(0, "G"), new Chord(17, "D")], sheet.Rows[line].Chords);
        Assert.Equal([new Chord(9, "Em"), new Chord(19, "A")], sheet.Rows[line + 1].Chords);
        Assert.Contains("[G]One two three fou[D]r\nFive six [Em]seven eigh[A]t", SongText.Text(song.TextBoxes), StringComparison.Ordinal);
        // Nothing is saved for it: the file is as it was.
        Assert.True(File.ReadAllBytes(song.Path).AsSpan().SequenceEqual(written));
        Assert.Equal("0-14 G, 18-19 D, 28-39 Em, 99-100 A", EntriesOf(song.Path)[0]);

        // It can be dealt with: taken off like any chord, and it does not come back.
        sheet.Take(line, 17);
        Assert.Equal([new Chord(0, "G"), new Chord(28, "Em"), new Chord(38, "A")], song.TextBoxes[0].Chords);
        // And whatever is written of its box is written on a character: the other, which
        // was past the end, is in the file where it was shown.
        Assert.Equal("0-18 G, 28-38 Em, 38-39 A", EntriesOf(song.Path)[0]);
    }

    // Read as a mark of the song, such chords are still its chords, and a chord on a
    // line break has no line break strictly inside its own short range.
    [Fact]
    public void AChordOnNoCharacterIsNotOfItselfTheOldEditorsMark()
    {
        var presentation = Workbench.Read(InTestData("Plain Song.pro"));
        var text = Checks.Texts(presentation).First();
        var d = text.Attributes.CustomAttributes.Single(entry => entry.Chord == "D");
        (d.Range.Start, d.Range.End) = (18, 19);
        var song = bench.OpenAt(bench.FileOf("a-chord-on-a-break.pro", presentation.ToByteArray()));
        Assert.Equal(0, song.Findings.ShownTwice);
        Assert.Equal(0, song.Mend());
        Assert.Empty(bench.Copies);
    }
}
