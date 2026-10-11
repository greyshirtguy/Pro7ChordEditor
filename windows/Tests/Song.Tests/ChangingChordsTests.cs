// Changing a text box's chords, saving, and undo: the proofs at the foot of
// spec/05-song-and-saving.md, on copies of the test files in the workbench.
//
//   - a change is saved at once, into the song's own file, and a copy is asked for
//     first, once for a sitting;
//   - a change and its undo leave the same file, to the byte;
//   - a change to one box's chords leaves every other field of the file as it was;
//   - chords put on are written with the ranges ranges.json says;
//   - a request that would change nothing saves nothing, keeps no copy, and is no step
//     of undo.

using System.Text.Json;
using Pro.SerializationInterop.RVProtoData;
using Pro7ChordEditor.ChordRules;
using Pro7ChordEditor.Tests;
using Xunit;
using CustomAttribute = Pro.SerializationInterop.RVProtoData.Graphics.Types.Text.Types.Attributes.Types.CustomAttribute;

namespace Pro7ChordEditor.Song.Tests;

public sealed class ChangingChordsTests : IDisposable
{
    private readonly Workbench bench = new();

    public void Dispose() => bench.Dispose();

    [Fact]
    public void AChangeIsSavedAtOnceIntoTheSongsOwnFileWithACopyAskedForFirst()
    {
        var song = bench.Open("Amazing Grace.pro");
        var asItWas = File.ReadAllBytes(song.Path);
        var box = song.TextBoxes[0];
        var chords = box.Chords.Append(new Chord(box.Words.Length - 1, "Em7")).ToList();

        song.SetChords(box.Slide, box.Element, chords);

        // In the song as the editors see it, and in the file as another reads it.
        Assert.Equal(chords, song.TextBoxes[0].Chords);
        Assert.Equal(chords, new OpenSong(song.Path).TextBoxes[0].Chords);
        Assert.True(song.Bytes.Span.SequenceEqual(File.ReadAllBytes(song.Path)));
        Assert.True(song.FileIsAsLastRead);
        // The copy was of the file as it was before, and why was said.
        var copy = Assert.Single(bench.Copies);
        Assert.Equal(song.Path, copy.Path);
        Assert.Equal(CopyReason.BeforeAChange, copy.Why);
        Assert.Equal(asItWas, copy.Bytes);
        // Nothing is left beside the song: what was written beside it took its place.
        Assert.Equal([Path.GetFileName(song.Path)], Directory.EnumerateFileSystemEntries(bench.Folder).Select(Path.GetFileName));

        // A second change in the same sitting asks for no second copy.
        song.SetChords(box.Slide, box.Element, box.Chords);
        Assert.Single(bench.Copies);
        // Opened again and changed, it asks for one.
        var again = bench.OpenAt(song.Path);
        again.SetChords(box.Slide, box.Element, chords);
        Assert.Equal(2, bench.Copies.Count);
    }

    [Fact]
    public void AChangeAndItsUndoLeaveTheSameFileAndRedoTheChangedOne()
    {
        var song = bench.Open("Amazing Grace.pro");
        var asItWas = File.ReadAllBytes(song.Path);
        var box = song.TextBoxes[1];

        song.SetChords(box.Slide, box.Element, [new Chord(0, "C"), new Chord(5, "G/B")]);
        var changed = File.ReadAllBytes(song.Path);
        Assert.NotEqual(asItWas, changed);
        Assert.True(song.CanUndo);
        Assert.False(song.CanRedo);

        song.Undo();
        Assert.Equal(asItWas, File.ReadAllBytes(song.Path));
        Assert.Equal(box.Chords, song.TextBoxes[1].Chords);
        Assert.False(song.CanUndo);
        Assert.True(song.CanRedo);

        song.Redo();
        Assert.Equal(changed, File.ReadAllBytes(song.Path));
        Assert.True(song.CanUndo);
        Assert.False(song.CanRedo);

        // With nothing to undo or to redo, each is nothing to do.
        song.Redo();
        song.Undo();
        song.Undo();
        Assert.Equal(asItWas, File.ReadAllBytes(song.Path));
    }

    [Theory]
    [InlineData("Amazing Grace.pro")]
    [InlineData("Rock Of Ages (Toplady).pro")]
    [InlineData("It Is Well (Chords).pro")]
    public void AChangeToOneBoxLeavesEveryOtherFieldOfTheFileAsItWas(string file)
    {
        var song = bench.Open(file);
        var before = Workbench.Read(song.Path);
        var box = song.TextBoxes.First(one => !one.Blank && one.Words.Length > 12);

        song.SetChords(box.Slide, box.Element, [new Chord(0, "D"), new Chord(4, "A7"), new Chord(9, "Bm")]);

        // The file as it is now, with that one box's entries put back as they were, is
        // the file as it was: nothing else in it has changed.
        var after = Workbench.Read(song.Path);
        Assert.NotEqual(before, after);
        var entries = Workbench.TextOf(after, box.Element).Attributes.CustomAttributes;
        entries.Clear();
        entries.AddRange(Workbench.TextOf(before, box.Element).Attributes.CustomAttributes.Select(entry => entry.Clone()));
        Assert.Equal(before, after);
        // And one undo leaves the file what it was, to the byte.
        song.Undo();
        Assert.Equal(before, Workbench.Read(song.Path));
    }

    // spec/01, "Writing a box's chords": every entry that is not a chord is kept, in
    // its order; after them, one entry for each chord, in order of place. The file
    // ProPresenter wrote has other entries on every text box.
    [Fact]
    public void WhatIsNotAChordIsKeptInItsOrderAndTheChordsGoAfter()
    {
        var song = bench.Open("Rock Of Ages (Toplady).pro");
        var box = song.TextBoxes[0];
        var others = Workbench.TextOf(Workbench.Read(song.Path), box.Element).Attributes.CustomAttributes.ToList();
        Assert.NotEmpty(others);
        Assert.DoesNotContain(others, IsAChord);

        // Given out of order, and two in one place: the later one given stands.
        song.SetChords(box.Slide, box.Element, [new Chord(9, "A"), new Chord(0, "D"), new Chord(9, "G")]);

        var entries = Workbench.TextOf(Workbench.Read(song.Path), box.Element).Attributes.CustomAttributes.ToList();
        Assert.Equal(others, entries.Take(others.Count));
        var chords = entries.Skip(others.Count).ToList();
        Assert.All(chords, entry => Assert.True(IsAChord(entry)));
        Assert.Equal(["D", "G"], chords.Select(entry => entry.Chord));
        Assert.Equal([0, 9], chords.Select(entry => entry.Range.Start));
    }

    // Request 1 tidies every name it is handed, those of chords that were there before
    // among them, since an editor hands back the whole box.
    [Fact]
    public void EveryNameHandedInIsTidied()
    {
        var song = bench.Open("Plain Song.pro");
        var box = song.TextBoxes[0];

        song.SetChords(box.Slide, box.Element, [new Chord(0, " g "), new Chord(3, "f#m/a"), new Chord(6, "   ")]);

        Assert.Equal([new Chord(0, "G"), new Chord(3, "F#m/A")], song.TextBoxes[0].Chords);
    }

    // "The same chords put on through request 1 ... give the ranges ranges.json says."
    // Only those cases whose text is the words of a box in a test file can be tried on a
    // file. (With each name tidied, which request 1 does and Ranges does not.)
    [Fact]
    public void ChordsPutOnAreWrittenWithTheRangesTheVectorsSay()
    {
        using var vector = JsonDocument.Parse(TestData.Vector("ranges.json"));
        var tried = 0;
        foreach (var file in TestData.PresentationPaths().Where(path => path.StartsWith("made-by-simple-presenter/", StringComparison.Ordinal)))
        {
            foreach (var one in vector.RootElement.GetProperty("cases").EnumerateArray())
            {
                var text = one.GetProperty("text").GetString()!;
                var song = bench.Open(file);
                var box = song.TextBoxes.FirstOrDefault(candidate => !candidate.Blank && string.Equals(candidate.Words, text, StringComparison.Ordinal));
                if (box is null)
                {
                    continue;
                }
                tried++;
                var chords = one.GetProperty("chords").EnumerateArray()
                    .Select(chord => new Chord(chord.GetProperty("at").GetInt32(), chord.GetProperty("name").GetString()!)).ToList();

                song.SetChords(box.Slide, box.Element, chords);

                var expected = one.GetProperty("out").EnumerateArray()
                    .Select(range => (range.GetProperty("start").GetInt32(), range.GetProperty("end").GetInt32(), Chords.Tidied(range.GetProperty("name").GetString()!)))
                    .ToList();
                var written = Workbench.TextOf(Workbench.Read(song.Path), box.Element).Attributes.CustomAttributes
                    .Where(IsAChord).Select(entry => (entry.Range.Start, entry.Range.End, entry.Chord)).ToList();
                Assert.Equal(expected, written);
            }
        }
        Assert.True(tried >= 8, $"only {tried} cases of ranges.json have the words of a text box in a test file");
    }

    [Fact]
    public void ARequestThatWouldChangeNothingSavesNothingKeepsNoCopyAndIsNoStepOfUndo()
    {
        var song = bench.Open("Amazing Grace.pro");
        var asItWas = File.ReadAllBytes(song.Path);
        var written = File.GetLastWriteTimeUtc(song.Path);

        foreach (var box in song.TextBoxes)
        {
            song.SetChords(box.Slide, box.Element, box.Chords);
        }
        song.SetKey(song.Key);
        song.SetShownKey(song.ShownKey);

        Assert.Empty(bench.Copies);
        Assert.False(song.CanUndo);
        Assert.Equal(asItWas, File.ReadAllBytes(song.Path));
        Assert.Equal(written, File.GetLastWriteTimeUtc(song.Path));
    }

    // Every clean test file: each box given the chords it already has changes nothing.
    // What is read is exactly what would be written.
    [Fact]
    public void ABoxGivenTheChordsItHasIsNotChangedInAnyCleanFile()
    {
        foreach (var file in TestData.PresentationPaths().Where(path => !path.StartsWith("as-the-old-windows-app-left-them/", StringComparison.Ordinal)))
        {
            var song = bench.Open(file);
            foreach (var box in song.TextBoxes.Where(one => !one.Blank))
            {
                song.SetChords(box.Slide, box.Element, box.Chords);
            }
            Assert.False(song.CanUndo, file + ": a box given its own chords was changed");
        }
        Assert.Empty(bench.Copies);
    }

    // The files the first Windows editor left have ranges that run over line breaks.
    // Given the chords they have, their boxes ARE changed: written right, which is the
    // mending of spec/08, check 1.
    [Fact]
    public void ABoxTheFirstEditorSavedIsWrittenRightWhenItsChordsAreSetAgain()
    {
        var song = bench.Open("Plain Song (Chords).pro");
        var clean = new OpenSong(bench.CopyOf("made-by-simple-presenter/Plain Song.pro"));
        foreach (var box in song.TextBoxes)
        {
            song.SetChords(box.Slide, box.Element, box.Chords);
        }

        Assert.True(song.CanUndo);
        var mended = Workbench.Read(song.Path);
        var asClean = Workbench.Read(clean.Path);
        foreach (var (box, cleanBox) in song.TextBoxes.Zip(clean.TextBoxes))
        {
            Assert.Equal(
                Workbench.TextOf(asClean, cleanBox.Element).Attributes.CustomAttributes.Where(IsAChord),
                Workbench.TextOf(mended, box.Element).Attributes.CustomAttributes.Where(IsAChord));
        }
    }

    [Fact]
    public void ChangesThatAreJoinedUndoAndRedoAsOne()
    {
        var song = bench.Open("Amazing Grace.pro");
        var asItWas = File.ReadAllBytes(song.Path);
        var (first, second, third) = (song.TextBoxes[0], song.TextBoxes[1], song.TextBoxes[2]);

        song.SetChords(first.Slide, first.Element, [new Chord(0, "A")]);
        var afterTheFirst = File.ReadAllBytes(song.Path);
        song.SetChords(second.Slide, second.Element, [new Chord(0, "B")]);
        song.SetChords(third.Slide, third.Element, [new Chord(0, "C")], joined: true);
        var afterAll = File.ReadAllBytes(song.Path);

        // The second and third were one: back over both in one go, to after the first.
        song.Undo();
        Assert.Equal(afterTheFirst, File.ReadAllBytes(song.Path));
        song.Undo();
        Assert.Equal(asItWas, File.ReadAllBytes(song.Path));
        Assert.False(song.CanUndo);
        // And forwards: the first; then the two that were one.
        song.Redo();
        Assert.Equal(afterTheFirst, File.ReadAllBytes(song.Path));
        song.Redo();
        Assert.Equal(afterAll, File.ReadAllBytes(song.Path));
        Assert.False(song.CanRedo);
    }

    // A change marked as joined, with nothing before it to be joined to, is a step of
    // its own.
    [Fact]
    public void AFirstChangeMarkedAsJoinedIsAStepOfItsOwn()
    {
        var song = bench.Open("Amazing Grace.pro");
        var box = song.TextBoxes[0];

        song.SetChords(box.Slide, box.Element, [new Chord(0, "A")], joined: true);
        song.Undo();

        Assert.Equal(box.Chords, song.TextBoxes[0].Chords);
        Assert.False(song.CanUndo);
    }

    [Fact]
    public void ANewChangeThrowsAwayWhatCouldHaveBeenRedone()
    {
        var song = bench.Open("Amazing Grace.pro");
        var box = song.TextBoxes[0];

        song.SetChords(box.Slide, box.Element, [new Chord(0, "A")]);
        song.Undo();
        Assert.True(song.CanRedo);
        song.SetChords(box.Slide, box.Element, [new Chord(0, "B")]);

        Assert.False(song.CanRedo);
        song.Redo();
        Assert.Equal([new Chord(0, "B")], song.TextBoxes[0].Chords);
    }

    // As many steps are kept as the reference keeps, two hundred, and the oldest go.
    [Fact]
    public void TwoHundredStepsOfUndoAreKept()
    {
        var song = bench.Open("Plain Song.pro");
        var box = song.TextBoxes[0];
        for (var n = 0; n < OpenSong.UndoLimit + 5; n++)
        {
            song.SetChords(box.Slide, box.Element, [new Chord(0, n % 2 == 0 ? "A" : "B")]);
        }

        var undone = 0;
        while (song.CanUndo)
        {
            song.Undo();
            undone++;
        }

        Assert.Equal(OpenSong.UndoLimit, undone);
        // Five changes are beyond reach: the box has what the fifth left it.
        Assert.Equal([new Chord(0, "A")], song.TextBoxes[0].Chords);
    }

    private static bool IsAChord(CustomAttribute entry) => entry.AttributeCase == CustomAttribute.AttributeOneofCase.Chord;
}
