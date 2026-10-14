// The "(Chords)" copies the first Windows editor saved beside the songs it was given,
// and putting one's chords and key back into the song it was saved beside
// (Folding.cs, and OpenSong.FoldIn). spec/08-repair.md, D.
//
// MINE: the page says what folding is, and no test data says what comes of it. But the
// test files do, taken together: each file in as-the-old-windows-app-left-them is the
// copy that editor would have saved of the song of the same name in
// made-by-simple-presenter. So the original, as someone would have had it, is made here
// from that song by taking every chord off it; the copy is folded into it; and what
// comes of that is held to the song the copy was made from.

using Google.Protobuf;
using Pro.SerializationInterop.RVProtoData;
using Pro7ChordEditor.Tests;
using Xunit;
using Chord = Pro7ChordEditor.ChordRules.Chord;

namespace Pro7ChordEditor.Song.Tests;

public sealed class FoldingTests : IDisposable
{
    private readonly Workbench bench = new();

    public void Dispose() => bench.Dispose();

    private static byte[] Clean(string name) => TestData.InFiles("made-by-simple-presenter/" + name + ".pro");

    private static byte[] Copy(string name) => TestData.InFiles("as-the-old-windows-app-left-them/" + name + " (Chords).pro");

    // A song as it was before anyone put a chord on it: the test file, with every chord
    // entry taken out of every text box.
    private static byte[] WithNoChords(byte[] song)
    {
        var presentation = Presentation.Parser.ParseFrom(song);
        foreach (var text in Checks.Texts(presentation))
        {
            var others = text.Attributes?.CustomAttributes.Where(attribute => !Reading.IsAChord(attribute)).ToList() ?? [];
            text.Attributes?.CustomAttributes.Clear();
            text.Attributes?.CustomAttributes.AddRange(others);
        }
        return presentation.ToByteArray();
    }

    private static List<string> Entries(byte[] song) =>
        [.. Checks.Boxes(Presentation.Parser.ParseFrom(song)).Select(box => string.Join(", ", box.Chords.Select(chord => chord.Start + "-" + chord.End + " " + chord.Name)))];

    private static bool Same(ReadOnlySpan<byte> one, ReadOnlySpan<byte> other) => one.SequenceEqual(other);

    // ---- Which song a copy is a copy of

    [Fact]
    public void ACopysOriginalIsTheSongOfItsNameLessTheEndingTheOldEditorGaveIt()
    {
        Assert.Equal("Amazing Grace", Folding.OriginalsName("Amazing Grace (Chords)"));
        // A copy of a copy is a copy of the copy.
        Assert.Equal("Amazing Grace (Chords)", Folding.OriginalsName("Amazing Grace (Chords) (Chords)"));
        // A name that has not the ending, to the letter, is no copy's.
        Assert.Null(Folding.OriginalsName("Amazing Grace"));
        Assert.Null(Folding.OriginalsName("Amazing Grace (chords)"));
        Assert.Null(Folding.OriginalsName("Amazing Grace(Chords)"));
        Assert.Null(Folding.OriginalsName("Amazing Grace (Chords) "));
        // Nor is the ending by itself a copy of a song with no name.
        Assert.Null(Folding.OriginalsName(" (Chords)"));
        Assert.Equal("(Chords)", Folding.OriginalsName("(Chords) (Chords)"));
    }

    [Fact]
    public void TheOriginalIsLookedForBesideTheCopyAmongTheSongsListed()
    {
        var copy = bench.FileOf("Plain Song (Chords).pro", Copy("Plain Song"));
        var original = bench.FileOf("Plain Song.pro", WithNoChords(Clean("Plain Song")));
        var other = bench.FileOf("It Is Well.pro", Clean("It Is Well"));
        ListedSong Listed(string path) => new(Path.GetFileNameWithoutExtension(path), path, new FileInfo(path).Length, File.GetLastWriteTimeUtc(path));
        var songs = new[] { Listed(other), Listed(original), Listed(copy) };

        Assert.Equal(original, Folding.OriginalOf(Listed(copy), songs)?.Path);
        // A song that is no copy has none; and a copy whose original has gone has none.
        Assert.Null(Folding.OriginalOf(Listed(original), songs));
        Assert.Null(Folding.OriginalOf(Listed(copy), [Listed(other), Listed(copy)]));
        // One of the same name in another folder is not beside it.
        var elsewhere = Directory.CreateDirectory(Path.Combine(bench.Folder, "Elsewhere")).FullName;
        var far = Path.Combine(elsewhere, "Plain Song.pro");
        File.WriteAllBytes(far, Clean("Plain Song"));
        Assert.Null(Folding.OriginalOf(Listed(copy), [Listed(far), Listed(copy)]));
    }

    // ---- What folding would do, said before it is done

    [Fact]
    public void WhatFoldingWouldDoIsWorkedOutFromTheTwoFilesAndChangesNeither()
    {
        var copy = bench.FileOf("Plain Song (Chords).pro", Copy("Plain Song"));
        var bare = bench.FileOf("Plain Song.pro", WithNoChords(Clean("Plain Song")));
        var copyWas = File.ReadAllBytes(copy);
        var bareWas = File.ReadAllBytes(bare);
        Assert.Equal(new FoldPlan(SameWords: true, CopysChords: 11, OriginalsChords: 0, CopysKey: "G", OriginalsKey: "G"), Folding.Plan(copy, bare));
        Assert.True(Same(copyWas, File.ReadAllBytes(copy)) && Same(bareWas, File.ReadAllBytes(bare)));
        // Folded, there is nothing left to do, and the plan says so: the song has the
        // copy's chords, as they are once mended, and its key. One chord of the song
        // changed afterwards, and there is again.
        var done = bench.OpenAt(bare);
        done.FoldIn(copyWas);
        Assert.Equal(new FoldPlan(true, 11, 11, "G", "G", NothingToDo: true), Folding.Plan(copy, bare));
        var first = done.TextBoxes[0];
        done.SetChords(first.Slide, first.Element, [.. first.Chords.Skip(1)]);
        Assert.Equal(new FoldPlan(true, 11, 10, "G", "G", NothingToDo: false), Folding.Plan(copy, bare));
        done.Undo();
        done.SetKey("D");
        Assert.Equal(new FoldPlan(true, 11, 11, "G", "D", NothingToDo: false), Folding.Plan(copy, bare));
        done.Undo();
        done.Undo();
        Assert.True(Same(bareWas, File.ReadAllBytes(bare)));

        // An original with chords of its own: how many is said, since folding replaces them.
        var chorded = bench.FileOf("with-its-own.pro", Clean("Plain Song"));
        Assert.Equal(11, Folding.Plan(copy, chorded).OriginalsChords);
        // The copy whose key the old editor reset: it is the copy's key that would be the song's.
        var minor = bench.FileOf("A Song - In A Minor Key (Chords).pro", Copy("A Song - In A Minor Key"));
        var minorBare = bench.FileOf("A Song - In A Minor Key.pro", WithNoChords(Clean("A Song - In A Minor Key")));
        Assert.Equal(new FoldPlan(true, 9, 0, "Ab", "F#m", CopysLikelierKey: "F#m"), Folding.Plan(minor, minorBare));
        // A copy that states no key has none to doubt, whatever its chords look like.
        // (Holy Holy Holy states none, and its chords look like D.)
        var keyless = bench.FileOf("Holy Holy Holy (Chords).pro", Clean("Holy Holy Holy"));
        var keylessBare = bench.FileOf("Holy Holy Holy.pro", WithNoChords(Clean("Holy Holy Holy")));
        Assert.Equal("D", Checks.Read(keyless).Key.Suggested);
        Assert.Equal(new FoldPlan(true, 22, 0, "", ""), Folding.Plan(keyless, keylessBare));
        // Another song altogether: not the same words.
        Assert.False(Folding.Plan(copy, bench.FileOf("another.pro", Clean("It Is Well"))).SameWords);
        // And the same words but for one letter.
        var presentation = Presentation.Parser.ParseFrom(WithNoChords(Clean("Plain Song")));
        var text = Checks.Texts(presentation).First();
        var rtf = text.RtfData.ToStringUtf8();
        Assert.Contains("One two three four", rtf, StringComparison.Ordinal);
        text.RtfData = ByteString.CopyFromUtf8(rtf.Replace("One two three four", "One two three for", StringComparison.Ordinal));
        Assert.False(Folding.Plan(copy, bench.FileOf("one-letter.pro", presentation.ToByteArray())).SameWords);
        // And the same words but for a capital.
        text.RtfData = ByteString.CopyFromUtf8(rtf.Replace("One two three four", "one two three four", StringComparison.Ordinal));
        Assert.False(Folding.Plan(copy, bench.FileOf("one-capital.pro", presentation.ToByteArray())).SameWords);
        // A file that is no song is refused, and says which.
        var nonsense = bench.FileOf("Nonsense.pro", "This is not a song."u8.ToArray());
        Assert.Equal(Refusal.Why.CannotBeRead, Assert.Throws<Refusal>(() => Folding.Plan(copy, nonsense)).Reason);
        Assert.Equal(Refusal.Why.CannotBeRead, Assert.Throws<Refusal>(() => Folding.Plan(Path.Combine(bench.Folder, "Not there.pro"), bare)).Reason);
    }

    // ---- Folding

    // A text box of the original that has no settings written at all, for which the copy
    // has no chords, is left as it is: the copy has nothing to give it, and "mending
    // changes nothing else in the file" (spec/08-repair.md). (An editor's request that
    // such a box have no chords does give it an empty set of settings, as the reference
    // does: NotHeldByTheTestFilesTests. A fold is not that request.) And a song so
    // folded is found to be folded, which it would not be if the box were touched each
    // time. MINE: no page has the case, and no file to hand has such a box.
    [Fact]
    public void FoldingLeavesATextBoxWithNoSettingsAloneWhereTheCopyHasNoChordsForIt()
    {
        var theirs = Presentation.Parser.ParseFrom(Copy("Plain Song"));
        var theirFirst = Checks.Texts(theirs).First();
        var others = theirFirst.Attributes.CustomAttributes.Where(attribute => !Reading.IsAChord(attribute)).ToList();
        theirFirst.Attributes.CustomAttributes.Clear();
        theirFirst.Attributes.CustomAttributes.AddRange(others);
        var copy = bench.FileOf("Plain Song (Chords).pro", theirs.ToByteArray());

        var mine = Presentation.Parser.ParseFrom(WithNoChords(Clean("Plain Song")));
        Checks.Texts(mine).First().Attributes = null;
        var song = bench.OpenAt(bench.FileOf("Plain Song.pro", mine.ToByteArray()));
        Assert.False(Folding.Plan(copy, song.Path).NothingToDo);

        song.FoldIn(File.ReadAllBytes(copy));

        var folded = Presentation.Parser.ParseFrom(File.ReadAllBytes(song.Path));
        Assert.Null(Checks.Texts(folded).First().Attributes);
        // The copy's other chords came over all the same.
        Assert.NotEmpty(song.UsedChords);
        Assert.True(Folding.Plan(copy, song.Path).NothingToDo);
    }

    [Theory]
    [InlineData("Plain Song")]
    [InlineData("It Is Well")]
    [InlineData("A Song - In A Minor Key")]
    [InlineData("Amazing Grace")]
    public void ACopyFoldedIntoItsOriginalGivesItTheCopysChordsMendedOnTheWayAndItsKey(string name)
    {
        var copy = Copy(name);
        var bare = WithNoChords(Clean(name));
        var song = bench.OpenAt(bench.FileOf(name + ".pro", bare));
        Assert.Empty(song.UsedChords);

        song.FoldIn(copy);

        var folded = File.ReadAllBytes(song.Path);
        // (The copy, mended by itself, on a bench of its own.)
        using var aside = new Workbench();
        var copyMended = aside.OpenAt(aside.FileOf("the copy, mended.pro", copy));
        copyMended.Mend();
        // Each box has the chords of the box in its place in the copy, written as they
        // should be: what the copy itself has once it is mended.
        Assert.True(Entries(File.ReadAllBytes(copyMended.Path)).SequenceEqual(Entries(folded)), name);
        Assert.Equal(0, song.Findings.ShownTwice);
        Assert.Equal(Presentation.Parser.ParseFrom(copy).Music, Presentation.Parser.ParseFrom(folded).Music);
        // Nothing else of the song is changed.
        Assert.True(Same(bare, WithNoChords(Presentation.Parser.ParseFrom(folded).With(Presentation.Parser.ParseFrom(bare).Music))), name);
        // Where the old editor left nothing but its ranges in the copy, the song is then
        // the song the copy was made from, to the byte; where it left more, that is in
        // the song too, to be mended there as it would be in the copy.
        switch (name)
        {
            case "A Song - In A Minor Key":
                Assert.Equal("Ab", song.Key);
                Assert.True(song.Findings.Key.Doubtful);
                break;
            case "Amazing Grace":
                Assert.True(song.Findings.Shift.Likely);
                break;
            default:
                Assert.True(Same(Clean(name), folded), name);
                break;
        }

        // A copy of the song's file was kept first, as before a mending.
        Assert.Equal([CopyReason.BeforeMending], bench.Copies.Select(kept => kept.Why));
        Assert.True(Same(bare, bench.Copies[0].Bytes));
        Assert.Equal(song.Path, bench.Copies[0].Path);
        // It is one step of undo, chords and key together.
        Assert.True(song.CanUndo);
        song.Undo();
        Assert.True(Same(bare, File.ReadAllBytes(song.Path)));
        Assert.False(song.CanUndo);
        song.Redo();
        Assert.True(Same(folded, File.ReadAllBytes(song.Path)));
        // Folded again, there is nothing to do: nothing is saved, and no copy is kept.
        song.FoldIn(copy);
        Assert.True(Same(folded, File.ReadAllBytes(song.Path)));
        Assert.Single(bench.Copies);
        Assert.False(song.CanRedo);
        song.Undo();
        Assert.False(song.CanUndo);
    }

    [Fact]
    public void TheOriginalsOwnChordsAreReplacedByTheCopysAndAKeyTheCopyHasNotIsTheSongsNoLonger()
    {
        // An original that had chords of its own: those of the song, and one more.
        var own = Presentation.Parser.ParseFrom(Clean("It Is Well"));
        var first = Checks.Texts(own).First();
        var extra = first.Attributes.CustomAttributes.First(Reading.IsAChord).Clone();
        extra.Chord = "Bb7";
        (extra.Range.Start, extra.Range.End) = (1, 2);
        first.Attributes.CustomAttributes.Add(extra);
        var song = bench.OpenAt(bench.FileOf("It Is Well.pro", own.ToByteArray()));
        Assert.Contains("Bb7", song.UsedChords);

        song.FoldIn(Copy("It Is Well"));
        Assert.DoesNotContain("Bb7", song.UsedChords);
        Assert.True(Same(Clean("It Is Well"), File.ReadAllBytes(song.Path)));

        // A copy that states no key: the song then states none. (That editor always
        // wrote one, so such a copy is not its own; the rule is "the copy's", whatever
        // that is.)
        var keyless = Presentation.Parser.ParseFrom(Copy("It Is Well"));
        keyless.Music = null;
        Assert.Equal("C", song.Key);
        song.FoldIn(keyless.ToByteArray());
        Assert.Equal("", song.Key);
        // (The key alone changed: the chords were the copy's already.)
        song.Undo();
        Assert.Equal("C", song.Key);
        Assert.True(Same(Clean("It Is Well"), File.ReadAllBytes(song.Path)));
    }

    [Fact]
    public void FoldingIsRefusedWhereTheWordsDifferAndWhenAnySaveWouldBeAndThenChangesNothing()
    {
        var bare = WithNoChords(Clean("Plain Song"));
        var path = bench.FileOf("Plain Song.pro", bare);
        var song = bench.OpenAt(path);

        // Another song's copy.
        var refused = Assert.Throws<Refusal>(() => song.FoldIn(Copy("It Is Well")));
        Assert.Equal(Refusal.Why.WordsDiffer, refused.Reason);
        Assert.Equal("The two songs have not the same words, so the chords of the one cannot be put into the other. Nothing was changed.", refused.Sentence);
        // A text box more in the copy, with the same words as far as the song goes.
        var longer = Presentation.Parser.ParseFrom(Copy("Plain Song"));
        longer.Cues.Add(longer.Cues[0].Clone());
        Assert.Equal(Refusal.Why.WordsDiffer, Assert.Throws<Refusal>(() => song.FoldIn(longer.ToByteArray())).Reason);
        // What is no song.
        Assert.Equal(Refusal.Why.CannotBeRead, Assert.Throws<Refusal>(() => song.FoldIn("This is not a song."u8)).Reason);
        // While ProPresenter is running; when the copy cannot be kept; opened only to be
        // looked at.
        bench.ProPresenterIsRunning = true;
        Assert.Equal(Refusal.Why.ProPresenterIsRunning, Assert.Throws<Refusal>(() => song.FoldIn(Copy("Plain Song"))).Reason);
        bench.ProPresenterIsRunning = false;
        bench.KeepingACopyFails = "The disk is full.";
        Assert.Equal(Refusal.Why.CopyNotKept, Assert.Throws<Refusal>(() => song.FoldIn(Copy("Plain Song"))).Reason);
        bench.KeepingACopyFails = null;
        Assert.Equal(Refusal.Why.OnlyForReading, Assert.Throws<Refusal>(() => new OpenSong(path).FoldIn(Copy("Plain Song"))).Reason);

        Assert.True(Same(bare, File.ReadAllBytes(path)));
        Assert.Empty(bench.Copies);
        Assert.False(song.CanUndo);
        Assert.Empty(song.UsedChords);
        // And then it is done.
        song.FoldIn(Copy("Plain Song"));
        Assert.True(Same(Clean("Plain Song"), File.ReadAllBytes(path)));
    }

    // The copy's own file is not the song's to keep a copy of: whoever asks hands in the
    // keeping of that. It is done once it is known that there is something to save and
    // that it may be saved, and before anything is; and if it fails, nothing is saved.
    [Fact]
    public void TheKeepingOfTheCopysOwnFileIsDoneBeforeAnythingIsSavedAndOnlyIfSomethingMayBe()
    {
        var bare = WithNoChords(Clean("Plain Song"));
        var path = bench.FileOf("Plain Song.pro", bare);
        var song = bench.OpenAt(path);
        var kept = new List<byte[]>();
        void Keep() => kept.Add(File.ReadAllBytes(path));

        // Nothing may be saved: it is not asked for.
        bench.ProPresenterIsRunning = true;
        Assert.Equal(Refusal.Why.ProPresenterIsRunning, Assert.Throws<Refusal>(() => song.FoldIn(Copy("Plain Song"), Keep)).Reason);
        Assert.Empty(kept);
        bench.ProPresenterIsRunning = false;
        Assert.Equal(Refusal.Why.WordsDiffer, Assert.Throws<Refusal>(() => song.FoldIn(Copy("It Is Well"), Keep)).Reason);
        Assert.Empty(kept);
        // It fails: the folding is refused, saying why, and nothing is changed.
        var refused = Assert.Throws<Refusal>(() => song.FoldIn(Copy("Plain Song"), () => throw new IOException("There is no room.")));
        Assert.Equal(Refusal.Why.CopyNotKept, refused.Reason);
        Assert.Contains("There is no room.", refused.Sentence, StringComparison.Ordinal);
        Assert.True(Same(bare, File.ReadAllBytes(path)));
        Assert.Empty(bench.Copies);
        Assert.False(song.CanUndo);
        // It is done: once, and while the song's file is still as it was.
        song.FoldIn(Copy("Plain Song"), Keep);
        Assert.Single(kept);
        Assert.True(Same(bare, kept[0]));
        Assert.True(Same(Clean("Plain Song"), File.ReadAllBytes(path)));
        // With nothing to put in, it is not asked for again.
        song.FoldIn(Copy("Plain Song"), Keep);
        Assert.Single(kept);
    }

    // The chords go by the box's place in the file, not by its id or its slide: the old
    // editor's copy has the song's own ids, but nothing here trusts that.
    [Fact]
    public void EachBoxIsGivenTheChordsOfTheBoxInItsPlaceInTheCopy()
    {
        var copy = Presentation.Parser.ParseFrom(Copy("Plain Song"));
        // The copy's text boxes are given other ids, every one.
        foreach (var cue in copy.Cues)
        {
            foreach (var action in cue.Actions)
            {
                foreach (var element in action.Slide?.Presentation?.BaseSlide?.Elements ?? [])
                {
                    element.Element_.Uuid.String = OpenSong.NewId(element.Element_.Uuid.String);
                }
            }
        }
        var song = bench.OpenAt(bench.FileOf("Plain Song.pro", WithNoChords(Clean("Plain Song"))));
        song.FoldIn(copy.ToByteArray());
        Assert.True(Same(Clean("Plain Song"), File.ReadAllBytes(song.Path)));
        Assert.Equal([new Chord(0, "G"), new Chord(14, "D"), new Chord(28, "Em")], song.TextBoxes[0].Chords);
    }
}

internal static class PresentationsForTests
{
    /// <summary>The presentation with its `music` made this one: for holding two songs against each other in everything but their keys.</summary>
    internal static byte[] With(this Presentation presentation, Presentation.Types.Music? music)
    {
        var one = presentation.Clone();
        one.Music = music?.Clone();
        return one.ToByteArray();
    }
}
