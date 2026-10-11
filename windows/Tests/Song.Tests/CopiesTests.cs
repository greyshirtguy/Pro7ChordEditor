// The copies kept before a change, and putting one back. spec/07-backups.md, and the
// proofs at its foot. On copies of the test files, with the copies themselves kept in
// the test's own temporary folder.
//
// NEW: nothing here is in Simple Presenter, so no vector holds any of it. MINE: what
// each case expects is the page's rule, as the Mac's tests of the same hold it
// (CopiesTests.swift), with what is Windows' own beside: what a name must be to be a
// folder's here, and the computer's own time where the clocks are changed twice a year.

using System.Globalization;
using System.Text.Json;
using Pro7ChordEditor.ChordRules;
using Pro7ChordEditor.Tests;
using Xunit;

namespace Pro7ChordEditor.Song.Tests;

public sealed class CopiesTests : IDisposable
{
    private const string PlainSong = "Plain Song.pro";
    private static readonly string Backslash = ((char)0x5C).ToString();
    private static readonly KeptSong Kept = new("Sunday", "Songs", "Plain Song");
    private readonly Workbench bench = new();

    public void Dispose() => bench.Dispose();

    // 2026-10-11 09:14:03 by the clock at Greenwich, and so many days and seconds from it.
    private static DateTime Time(double days = 0, double seconds = 0) =>
        new DateTime(2026, 10, 11, 9, 14, 3, DateTimeKind.Utc).AddDays(days).AddSeconds(seconds);

    private Copies Keeper => new(Path.Combine(bench.Folder, "Own", "Backups"), "0.1", TimeZoneInfo.Utc);

    // A song opened so that its copies are really kept, by `Copies`, at a time the test
    // may move on.
    private OpenSong Open(string path, Func<DateTime> clock) => new(path, new Safeguards(
        () => bench.ProPresenterIsRunning,
        (file, why) => Keeper.Keep(file, Kept, why, clock())));

    private static IEnumerable<string> Names(IEnumerable<KeptCopy> copies) => copies.Select(copy => copy.Id);

    // ---- Keeping

    [Fact]
    public void AChangeMakesOneCopyTheSameBytesAsTheFileWasASecondChangeInTheSameSittingMakesNoneAnotherSittingMakesOne()
    {
        var path = bench.CopyOf(PlainSong);
        var original = File.ReadAllBytes(path);
        var now = Time();
        var song = Open(path, () => now);
        Assert.Empty(Keeper.CopiesOf(Kept));

        song.SetKey("E");
        var kept = Keeper.CopiesOf(Kept);
        Assert.Single(kept);
        Assert.Equal(original, File.ReadAllBytes(kept[0].Path));
        Assert.Equal("2026-10-11 09.14.03.pro", kept[0].Id);
        Assert.Equal(Path.Combine(bench.Folder, "Own", "Backups", "Sunday", "Songs", "Plain Song"), Path.GetDirectoryName(kept[0].Path));
        Assert.Equal(Path.GetDirectoryName(kept[0].Path), Keeper.FolderOf(Kept));

        now = Time(seconds: 60);
        song.SetKey("D");
        song.Undo();
        Assert.Single(Keeper.CopiesOf(Kept));

        // Opened again and changed: a copy of the file as it is now, which is in E.
        var inE = File.ReadAllBytes(path);
        now = Time(seconds: 3600);
        Open(path, () => now).SetKey("A");
        kept = Keeper.CopiesOf(Kept);
        Assert.Equal(["2026-10-11 10.14.03.pro", "2026-10-11 09.14.03.pro"], Names(kept));
        Assert.Equal(inE, File.ReadAllBytes(kept[0].Path));
        Assert.Equal([false, true], kept.Select(copy => copy.IsTheFirst));
    }

    [Fact]
    public void WhatTheIndexSaysOfACopyWhyOfWhatTheSongsIdAndNameItsChordsItsKeyItsSize()
    {
        var path = bench.CopyOf(PlainSong);
        var song = Open(path, () => Time(seconds: 0.75));
        song.SetKey("E");
        var copy = Keeper.CopiesOf(Kept).Single();
        // (To the second, as its name is.)
        Assert.Equal(Time(), copy.Made);
        Assert.Equal(DateTimeKind.Utc, copy.Made.Kind);
        Assert.Equal(CopyReason.BeforeAChange, copy.Why);
        Assert.Equal(Paths.IdOf(path), copy.CopyOf);
        Assert.Equal(("Plain Song", song.Id), (copy.SongName, copy.SongId));
        Assert.NotEqual("", copy.SongId);
        Assert.Equal((11, "G"), (copy.Chords, copy.Key));
        Assert.Equal(TestData.InFiles("made-by-simple-presenter/" + PlainSong).Length, copy.Size);

        // The index is a file of its own beside the copies, and says the app's version.
        var home = Path.GetDirectoryName(copy.Path)!;
        var index = File.ReadAllText(Path.Combine(home, "index.json"));
        using var read = JsonDocument.Parse(index);
        var entry = read.RootElement.EnumerateArray().Single();
        Assert.Equal("before a change", entry.GetProperty("why").GetString());
        Assert.Equal("0.1", entry.GetProperty("appVersion").GetString());
        Assert.Equal("2026-10-11 09.14.03.pro", entry.GetProperty("file").GetString());
        Assert.Equal("2026-10-11T09:14:03Z", entry.GetProperty("made").GetString());
        Assert.Equal(song.Id, entry.GetProperty("songID").GetString());
        Assert.Equal(
            ["appVersion", "chords", "copyOf", "file", "key", "made", "size", "songID", "songName", "why"],
            entry.EnumerateObject().Select(one => one.Name).Order(StringComparer.Ordinal));
        // It has not a word of the song in it, nor a chord's name.
        Assert.DoesNotContain("One two three", index, StringComparison.Ordinal);
        Assert.DoesNotContain("D/F#", index, StringComparison.Ordinal);
        // And nothing is left beside it of its being written.
        Assert.Equal(["2026-10-11 09.14.03.pro", "index.json"], Directory.EnumerateFiles(home).Select(file => Path.GetFileName(file)).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void TheThreeReasonsAreWrittenInTheIndexInThePagesWords()
    {
        Assert.Equal("before a change", Copies.Said(CopyReason.BeforeAChange));
        Assert.Equal("before putting back a copy", Copies.Said(CopyReason.BeforePuttingBack));
        Assert.Equal("before mending", Copies.Said(CopyReason.BeforeMending));
    }

    [Fact]
    public void ASongWithNoKeyAndBytesThatAreNoSongAreKeptAllTheSame()
    {
        // A song that states no key: its copy says none.
        var holy = bench.CopyOf("Holy Holy Holy.pro");
        var copy = Keeper.Keep(holy, new KeptSong("Sunday", "Songs", "Holy Holy Holy"), CopyReason.BeforeMending, Time())!;
        Assert.Equal(("", 22, CopyReason.BeforeMending), (copy.Key, copy.Chords, copy.Why));

        // A file that is no song is copied as any file is: its bytes, and nothing said of
        // what is in it.
        var nonsense = bench.FileOf("Nonsense.pro", [1, 2, 3]);
        var kept = Keeper.Keep(nonsense, new KeptSong("Sunday", "Songs", "Nonsense"), CopyReason.BeforeAChange, Time())!;
        Assert.Equal([1, 2, 3], File.ReadAllBytes(kept.Path));
        Assert.Equal(("", "", 0, "", 3L), (kept.SongId, kept.SongName, kept.Chords, kept.Key, kept.Size));
    }

    [Fact]
    public void ACopysChordsAreThoseTheEditorsWouldBeShownAsTheMarkInTheListCountsThem()
    {
        // A song with a text box that shows something else's words (a ticker's): its
        // chords are none of the song's (Reading.cs), in a copy's count as in the mark.
        var path = bench.CopyOf(PlainSong);
        var presentation = Workbench.Read(path);
        var first = new OpenSong(path).TextBoxes[0];
        var element = (from cue in presentation.Cues
                       from action in cue.Actions
                       from one in action.Slide?.Presentation?.BaseSlide?.Elements ?? []
                       where one.Element_?.Uuid?.String == first.Element
                       select one).Single();
        element.DataLinks.Add(new Pro.SerializationInterop.RVProtoData.Slide.Types.Element.Types.DataLink
        {
            Ticker = new Pro.SerializationInterop.RVProtoData.Slide.Types.Element.Types.DataLink.Types.Ticker(),
        });
        var changed = bench.FileOf("A box that shows a ticker.pro", Google.Protobuf.MessageExtensions.ToByteArray(presentation));
        Assert.Equal(3, first.Chords.Count);

        var copy = Keeper.Keep(changed, Kept, CopyReason.BeforeAChange, Time())!;
        Assert.Equal(8, copy.Chords);
        Assert.Equal(Marks.Read(new ListedSong("x", changed, 0, DateTime.UnixEpoch)).Chords, copy.Chords);
        // And so with the index gone, read from the copy itself.
        File.Delete(Path.Combine(Keeper.FolderOf(Kept), "index.json"));
        Assert.Equal(8, Keeper.CopiesOf(Kept).Single().Chords);
    }

    [Fact]
    public void WithTheIndexGoneTheCopiesAreStillListedFromTheFolderAndFromThemselves()
    {
        var path = bench.CopyOf(PlainSong);
        Open(path, () => Time()).SetKey("E");
        Open(path, () => Time(days: 1)).SetKey("D");
        var home = Keeper.FolderOf(Kept);
        File.Delete(Path.Combine(home, "index.json"));

        var kept = Keeper.CopiesOf(Kept);
        Assert.Equal(["2026-10-12 09.14.03.pro", "2026-10-11 09.14.03.pro"], Names(kept));
        // When, from its name; what is in it, from itself; why, and of which file, not known.
        Assert.Equal([Time(days: 1), Time()], kept.Select(copy => copy.Made));
        Assert.Equal(["E", "G"], kept.Select(copy => copy.Key));
        Assert.All(kept, copy => Assert.True(copy is { Chords: 11, SongName: "Plain Song", Why: null, CopyOf: "" } && copy.Size == new FileInfo(copy.Path).Length && copy.SongId.Length > 0));
        Assert.Equal([false, true], kept.Select(copy => copy.IsTheFirst));

        // An index that is damaged is the same as none; and a file that is not named as
        // a copy is no copy.
        File.WriteAllText(Path.Combine(home, "index.json"), "{ not json");
        File.WriteAllText(Path.Combine(home, "Read me.txt"), "a note");
        File.WriteAllText(Path.Combine(home, "Plain Song.pro"), "a song");
        File.WriteAllText(Path.Combine(home, "2026-10-13 09.14.03.txt"), "not a copy's ending");
        File.WriteAllText(Path.Combine(home, "2026-13-45 09.14.03.pro"), "no such day");
        File.WriteAllText(Path.Combine(home, "2026-10-13 09.14.03 1.pro"), "a second copy is 2, never 1");
        File.WriteAllText(Path.Combine(home, "2026-10-13 09.14.03 two.pro"), "nor a word");
        File.WriteAllText(Path.Combine(home, "2026-10-13 09.14.pro"), "to the second, or not at all");
        Assert.Equal(2, Keeper.CopiesOf(Kept).Count);
        // The next copy is kept all the same, and the index begun again with it alone.
        Open(path, () => Time(days: 2)).SetKey("A");
        Assert.Equal(3, Keeper.CopiesOf(Kept).Count);
        Assert.Equal([CopyReason.BeforeAChange, null, null], Keeper.CopiesOf(Kept).Select(copy => copy.Why));
    }

    [Fact]
    public void NoSecondCopyIsMadeOfBytesThatTheNewestCopyAlreadyIsAndAnotherMadeInTheSameSecondIsNamedApart()
    {
        var path = bench.CopyOf(PlainSong);
        var keeper = Keeper;
        Assert.NotNull(keeper.Keep(path, Kept, CopyReason.BeforeAChange, Time()));
        Assert.Null(keeper.Keep(path, Kept, CopyReason.BeforeMending, Time(seconds: 5)));
        Assert.Single(keeper.CopiesOf(Kept));

        // The file changes, and is copied twice more in one second.
        new OpenSong(path, bench.Safeguards).SetKey("E");
        Assert.NotNull(keeper.Keep(path, Kept, CopyReason.BeforePuttingBack, Time()));
        new OpenSong(path, bench.Safeguards).SetKey("D");
        Assert.NotNull(keeper.Keep(path, Kept, CopyReason.BeforeMending, Time()));
        var kept = keeper.CopiesOf(Kept);
        Assert.Equal(["2026-10-11 09.14.03 3.pro", "2026-10-11 09.14.03 2.pro", "2026-10-11 09.14.03.pro"], Names(kept));
        Assert.Equal([CopyReason.BeforeMending, CopyReason.BeforePuttingBack, CopyReason.BeforeAChange], kept.Select(copy => copy.Why));
        Assert.Equal(["D", "E", "G"], kept.Select(copy => copy.Key));
        Assert.Equal([false, false, true], kept.Select(copy => copy.IsTheFirst));
        // With the index gone they are in the same order still: by their names.
        File.Delete(Path.Combine(keeper.FolderOf(Kept), "index.json"));
        Assert.Equal(["D", "E", "G"], keeper.CopiesOf(Kept).Select(copy => copy.Key));

        // Only the newest is held against what is to be copied: a copy the same as an
        // older one is made all the same, so that the copies read as what happened.
        File.WriteAllBytes(path, File.ReadAllBytes(kept[2].Path));
        Assert.NotNull(keeper.Keep(path, Kept, CopyReason.BeforeAChange, Time(seconds: 30)));
        Assert.Equal(4, keeper.CopiesOf(Kept).Count);
        // And that one is now the newest, so the same again makes none.
        Assert.Null(keeper.Keep(path, Kept, CopyReason.BeforeAChange, Time(seconds: 31)));
    }

    [Fact]
    public void ACopyThatCannotBeMadeRefusesTheChangeAndLeavesTheFileAlone()
    {
        var path = bench.CopyOf(PlainSong);
        var original = File.ReadAllBytes(path);
        // Where the folder of copies would be, there is a file: nothing can be kept there.
        Directory.CreateDirectory(Path.Combine(bench.Folder, "Own"));
        File.WriteAllText(Path.Combine(bench.Folder, "Own", "Backups"), "in the way");

        var song = Open(path, () => Time());
        Assert.Equal(Refusal.Why.CopyNotKept, Assert.Throws<Refusal>(() => song.SetKey("E")).Reason);
        var refusal = Assert.Throws<Refusal>(() => song.SetChords(0, song.TextBoxes[0].Element, []));
        Assert.Equal(Refusal.Why.CopyNotKept, refusal.Reason);
        Assert.StartsWith("A copy of the song's file could not be kept first, so the song was not changed: ", refusal.Sentence, StringComparison.Ordinal);
        Assert.Equal(original, File.ReadAllBytes(path));
        Assert.Equal("G", song.Key);
        Assert.False(song.CanUndo);

        // And a file that is not there cannot be copied, which is said so.
        Assert.Throws<Copies.CannotBeKept>(() => Keeper.Keep(Path.Combine(bench.Folder, "Gone.pro"), Kept, CopyReason.BeforeAChange));
        var elsewhere = new Copies(Path.Combine(bench.Folder, "Elsewhere", "Backups"));
        Assert.Throws<Copies.CannotBeKept>(() => elsewhere.Keep(Path.Combine(bench.Folder, "Gone.pro"), Kept, CopyReason.BeforeAChange));
        Assert.False(Directory.Exists(Path.Combine(bench.Folder, "Elsewhere")));
    }

    [Fact]
    public void ACopyIsNeverWrittenOverOneThatIsThere()
    {
        var path = bench.CopyOf(PlainSong);
        var keeper = Keeper;
        // A file already has the name the next copy would have, and is not in the index.
        Directory.CreateDirectory(keeper.FolderOf(Kept));
        var there = Path.Combine(keeper.FolderOf(Kept), "2026-10-11 09.14.03.pro");
        File.WriteAllBytes(there, [9, 9, 9]);

        var copy = keeper.Keep(path, Kept, CopyReason.BeforeAChange, Time())!;
        Assert.Equal("2026-10-11 09.14.03 2.pro", copy.Id);
        Assert.Equal([9, 9, 9], File.ReadAllBytes(there));
        Assert.Equal(File.ReadAllBytes(path), File.ReadAllBytes(copy.Path));
    }

    // ---- Where, and under what name

    [Fact]
    public void ANameIsMadeFitToBeAFoldersAndACopyIsNamedInTheComputersOwnTime()
    {
        Assert.Equal("Sunday - AM- one", Copies.FolderName("Sunday / AM: one"));
        Assert.Equal("a-b-c-d", Copies.FolderName("a" + Backslash + "b\nc\rd"));
        Assert.Equal("Plain Song", Copies.FolderName("Plain Song"));
        Assert.Equal("-", Copies.FolderName(""));
        // Windows' own: the marks it will not have in a name, and a full stop or a space
        // at the end of one, which it would drop.
        Assert.Equal("What- -Why- -a-b-", Copies.FolderName("What? <Why> \"a|b*"));
        Assert.Equal("Song-", Copies.FolderName("Song."));
        Assert.Equal("Song--", Copies.FolderName("Song. "));
        Assert.Equal("-", Copies.FolderName("."));
        Assert.Equal("--", Copies.FolderName(".."));
        Assert.Equal("Mr. Song", Copies.FolderName("Mr. Song"));
        Assert.Equal(Path.Combine(bench.Folder, "B", "a-b", "Songs", "It Is Well-"), new Copies(Path.Combine(bench.Folder, "B")).FolderOf(new KeptSong("a:b", "Songs", "It Is Well?")));

        // Where the clocks are eleven hours on from Greenwich, a copy made at 09:14:03
        // there is named for 20:14:03 here, and read back as the same moment.
        var path = bench.CopyOf(PlainSong);
        var sydney = TimeZoneInfo.FindSystemTimeZoneById("AUS Eastern Standard Time");
        var keeper = new Copies(Path.Combine(bench.Folder, "Sydney"), "", sydney);
        var copy = keeper.Keep(path, Kept, CopyReason.BeforeAChange, Time())!;
        Assert.Equal("2026-10-11 20.14.03.pro", copy.Id);
        Assert.Equal(Time(), copy.Made);
        File.Delete(Path.Combine(keeper.FolderOf(Kept), "index.json"));
        Assert.Equal(Time(), keeper.CopiesOf(Kept).Single().Made);
        Assert.Equal(Time(), keeper.TimeOfCopyNamed("2026-10-11 20.14.03 7.pro"));
        // An hour the clocks skip (there, two to three on the first Sunday of October)
        // has no copies made in it; a name that says one is taken as the hour after.
        Assert.Equal(new DateTime(2026, 10, 3, 16, 30, 0, DateTimeKind.Utc), keeper.TimeOfCopyNamed("2026-10-04 02.30.00.pro"));
        Assert.Equal(new DateTime(2026, 10, 3, 16, 30, 0, DateTimeKind.Utc), keeper.TimeOfCopyNamed("2026-10-04 03.30.00.pro"));
        Assert.Null(keeper.TimeOfCopyNamed("2026-10-04 03.30.00.PRO"));
        Assert.Null(keeper.TimeOfCopyNamed("index.json"));
    }

    // ---- Putting one back

    [Fact]
    public void PutBackTheFileIsByteForByteTheCopyAndTakenBackByteForByteWhatItWas()
    {
        var path = bench.CopyOf(PlainSong);
        var original = File.ReadAllBytes(path);
        var now = Time();
        var song = Open(path, () => now);
        var box = song.TextBoxes[0];
        song.SetChords(box.Slide, box.Element, [new Chord(0, "Em")]);
        song.SetKey("E");
        var changed = File.ReadAllBytes(path);
        // (And something done and undone, so that there is something to redo.)
        song.SetKey("D");
        song.Undo();
        Assert.Equal(changed, File.ReadAllBytes(path));
        Assert.True(song.CanUndo && song.CanRedo);

        // The one copy there is, is the file as it was. Put back:
        now = Time(seconds: 600);
        var first = Keeper.CopiesOf(Kept).Single();
        song.PutBack(File.ReadAllBytes(first.Path));
        Assert.Equal(original, File.ReadAllBytes(path));
        // The song starts afresh from it: what it shows is the file, and undo is empty.
        Assert.Equal("G", song.Key);
        Assert.Equal(box.Chords, song.TextBoxes[0].Chords);
        Assert.False(song.CanUndo || song.CanRedo);
        Assert.True(song.FileIsAsLastRead);
        Assert.Equal(original, song.Bytes.ToArray());

        // Before it was put back, the file as it was then was copied, and says why.
        var kept = Keeper.CopiesOf(Kept);
        Assert.Equal(2, kept.Count);
        Assert.Equal((CopyReason.BeforePuttingBack, now), (kept[0].Why, kept[0].Made));
        Assert.Equal(changed, File.ReadAllBytes(kept[0].Path));

        // Taking it back is putting that copy back. There is no second mechanism.
        now = Time(seconds: 660);
        song.PutBack(File.ReadAllBytes(kept[0].Path));
        Assert.Equal(changed, File.ReadAllBytes(path));
        Assert.Equal("E", song.Key);
        Assert.Equal([new Chord(0, "Em")], song.TextBoxes[0].Chords);
        // That was a putting back too, and so the file as it was before it was copied:
        // the same bytes as the first copy, but the first is not the newest, and it is
        // the newest that a new copy is held against.
        kept = Keeper.CopiesOf(Kept);
        Assert.Equal(3, kept.Count);
        Assert.Equal(CopyReason.BeforePuttingBack, kept[0].Why);
        Assert.Equal(original, File.ReadAllBytes(kept[0].Path));

        // The song goes on from there as from a new sitting: its next change keeps a copy.
        now = Time(seconds: 720);
        song.SetKey("D");
        kept = Keeper.CopiesOf(Kept);
        Assert.Equal(4, kept.Count);
        Assert.Equal(CopyReason.BeforeAChange, kept[0].Why);
        Assert.Equal(changed, File.ReadAllBytes(kept[0].Path));
        song.Undo();
        Assert.Equal(changed, File.ReadAllBytes(path));
        // A put back and its taking back, one after the other, left two copies more
        // than there were, each saying "before putting back a copy".
        Assert.Equal(
            [CopyReason.BeforeAChange, CopyReason.BeforePuttingBack, CopyReason.BeforePuttingBack, CopyReason.BeforeAChange],
            kept.Select(copy => copy.Why));
    }

    [Fact]
    public void PuttingBackIsASaveRefusedWhileProPresenterRunsOverAFileSomethingElseChangedAndWithoutItsOwnCopyFirst()
    {
        var path = bench.CopyOf(PlainSong);
        var original = File.ReadAllBytes(path);
        var song = Open(path, () => Time());
        song.SetKey("E");
        var changed = File.ReadAllBytes(path);

        bench.ProPresenterIsRunning = true;
        Assert.Equal(Refusal.Why.ProPresenterIsRunning, Assert.Throws<Refusal>(() => song.PutBack(original)).Reason);
        bench.ProPresenterIsRunning = false;

        // A copy that is no song is not put in a song's place.
        var noSong = Assert.Throws<Refusal>(() => song.PutBack("not a song"u8));
        Assert.Equal(Refusal.Why.CannotBeRead, noSong.Reason);
        Assert.Equal("That copy could not be read as a ProPresenter presentation: it is not a presentation, or it is damaged.", noSong.Sentence);
        // Bytes that are the file already are nothing to do.
        song.PutBack(changed);
        Assert.Equal(changed, File.ReadAllBytes(path));
        Assert.True(song.CanUndo);
        Assert.Single(Keeper.CopiesOf(Kept));

        // A song opened only to be looked at puts nothing back.
        Assert.Equal(Refusal.Why.OnlyForReading, Assert.Throws<Refusal>(() => new OpenSong(path).PutBack(original)).Reason);

        // The copy that is kept first cannot be: nothing is put back.
        var unkept = new OpenSong(path, new Safeguards(() => false, (_, _) => throw new IOException("the disk is full.")));
        var refusal = Assert.Throws<Refusal>(() => unkept.PutBack(original));
        Assert.Equal(Refusal.Why.CopyNotKept, refusal.Reason);
        Assert.EndsWith("the disk is full.", refusal.Sentence, StringComparison.Ordinal);
        Assert.Equal(changed, File.ReadAllBytes(path));

        // Something else changes the file.
        new OpenSong(path, bench.Safeguards).SetKey("A");
        var byAnother = File.ReadAllBytes(path);
        Assert.Equal(Refusal.Why.ChangedByAnother, Assert.Throws<Refusal>(() => song.PutBack(original)).Reason);
        Assert.Equal(byAnother, File.ReadAllBytes(path));
        Assert.Equal("E", song.Key);
        Assert.True(song.CanUndo);
    }

    [Fact]
    public void WhatIsPutBackIsTheSongFromThenOnItsSlidesAndItsBoxesWhateverTheSongWasBefore()
    {
        // Put in Plain Song's place, of four slides, the bytes of a song of seven: the
        // song is then that one, slide for slide, as if it had been opened afresh.
        var path = bench.CopyOf(PlainSong);
        var song = Open(path, () => Time());
        Assert.Equal(4, song.SlideCount);
        var grace = TestData.InFiles("made-by-simple-presenter/Amazing Grace.pro");

        song.PutBack(grace);

        var afresh = new OpenSong(path);
        Assert.Equal(7, song.SlideCount);
        Assert.Equal(afresh.SlideCount, song.SlideCount);
        Assert.Equal(afresh.TextBoxes.Select(box => (box.Slide, box.Group, box.Element, box.Words)), song.TextBoxes.Select(box => (box.Slide, box.Group, box.Element, box.Words)));
        Assert.Equal(afresh.TextBoxes.SelectMany(box => box.Chords), song.TextBoxes.SelectMany(box => box.Chords));
        Assert.Equal((afresh.Name, afresh.Id, afresh.Key), (song.Name, song.Id, song.Key));
        // And it can be changed on from there: its last slide's chords, which Plain Song
        // has no slide for.
        var last = song.TextBoxes[^1];
        song.SetChords(last.Slide, last.Element, []);
        Assert.Empty(new OpenSong(path).TextBoxes[^1].Chords);
    }

    [Fact]
    public void WhatWasMadeForABlankSlidesChordsIsForgottenWhenACopyIsPutBack()
    {
        // A blank slide given chords and then relieved of them in the same sitting is the
        // file it was (spec/01). Not across a putting back: the file is another by then.
        var path = bench.CopyOf("Blank Slides.pro");
        var song = Open(path, () => Time());
        var blank = song.TextBoxes.First(box => box.Blank && box.Element.Length > 0);
        song.SetChordsAlone(blank.Slide, blank.Element, 0, ["G"]);
        var withAChord = File.ReadAllBytes(path);
        song.SetKey("E");
        song.PutBack(withAChord);

        // Taken off now, the box keeps the stand-in it was given: it is not made empty
        // again, since nothing is remembered of how it was.
        song.SetChordsAlone(blank.Slide, blank.Element, 0, []);
        var after = song.TextBoxes.Single(box => box.Slide == blank.Slide);
        Assert.Empty(after.Chords);
        Assert.True(Words.HasAStandIn(after.Words));
    }

    // ---- How long

    [Fact]
    public void OfOldCopiesTheFirstIsNeverThrownAwayNorTheTwentyNewestNorAnyOfTheLastThirtyDays()
    {
        var path = bench.CopyOf(PlainSong);
        var keeper = Keeper;
        // Thirty copies, one every ten days back from now, each of other bytes.
        var keys = Chords.MajorKeys().Concat(Chords.MinorKeys()).Concat(["A#", "B#", "Cb", "C#"]).ToList();
        for (var n = 29; n >= 0; n--)
        {
            new OpenSong(path, bench.Safeguards).SetKey(keys[^1]);
            keys.RemoveAt(keys.Count - 1);
            Assert.NotNull(keeper.Keep(path, Kept, CopyReason.BeforeAChange, Time(days: -10.0 * n)));
        }
        var home = keeper.FolderOf(Kept);
        File.WriteAllText(Path.Combine(home, "Read me.txt"), "a note");
        Assert.Equal(30, keeper.CopiesOf(Kept).Count);

        // Now: copies 0 to 29, of ages 0, 10, 20 ... 290 days. The twenty newest stay
        // (0 to 190 days), and of the ten older only the very first (290 days).
        Assert.Equal(9, keeper.Thin(Time()));
        var left = keeper.CopiesOf(Kept);
        Assert.Equal(21, left.Count);
        Assert.Equal(Time(days: -290), left[^1].Made);
        Assert.True(left[^1].IsTheFirst);
        Assert.Equal(Enumerable.Range(0, 20).Select(n => Time(days: -10.0 * n)), left.SkipLast(1).Select(copy => copy.Made));
        // The index follows: it has an entry for each copy that is left and for no
        // other. And what is not a copy is not touched.
        using var index = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(home, "index.json")));
        Assert.Equal(
            left.Select(copy => copy.Id).Order(StringComparer.Ordinal),
            index.RootElement.EnumerateArray().Select(entry => entry.GetProperty("file").GetString()!).Order(StringComparer.Ordinal));
        Assert.True(File.Exists(Path.Combine(home, "Read me.txt")));
        Assert.All(left, copy => Assert.Equal(CopyReason.BeforeAChange, copy.Why));
        // Again, and nothing more goes.
        Assert.Equal(0, keeper.Thin(Time()));

        // A year on, all are old: the twenty newest and the first are there still.
        Assert.Equal(0, keeper.Thin(Time(days: 365)));
        Assert.Equal(21, keeper.CopiesOf(Kept).Count);
        Assert.Equal((20, 30), (Copies.NewestKept, Copies.DaysKept));
    }

    [Fact]
    public void EverythingOfTheLastThirtyDaysIsKeptHoweverMany()
    {
        var path = bench.CopyOf(PlainSong);
        var keeper = Keeper;
        var keys = Chords.MajorKeys().Concat(Chords.MinorKeys()).ToList();
        // Twenty-five copies in the last twenty-five days.
        for (var n = 24; n >= 0; n--)
        {
            new OpenSong(path, bench.Safeguards).SetKey(keys[^1]);
            keys.RemoveAt(keys.Count - 1);
            keeper.Keep(path, Kept, CopyReason.BeforeAChange, Time(days: -n));
        }
        Assert.Equal(0, keeper.Thin(Time()));
        Assert.Equal(25, keeper.CopiesOf(Kept).Count);
        // Thirty days on from the oldest but one, to the second, it is not yet older
        // than thirty days; a second later it is, and goes: the first never does.
        Assert.Equal(0, keeper.Thin(Time(days: 7)));
        Assert.Equal(1, keeper.Thin(Time(days: 7, seconds: 1)));
        // Sixty days on they are all old, and all but the twenty newest and the first go.
        Assert.Equal(3, keeper.Thin(Time(days: 60)));
        Assert.Equal(21, keeper.CopiesOf(Kept).Count);
    }

    // ---- All of them

    [Fact]
    public void EverySongThatHasCopiesIsFoundByPlaceAndLibraryAndHowMuchRoomTheyTake()
    {
        var keeper = Keeper;
        var plain = bench.CopyOf(PlainSong);
        var grace = bench.CopyOf("Amazing Grace.pro");
        keeper.Keep(plain, Kept, CopyReason.BeforeAChange, Time());
        keeper.Keep(grace, new KeptSong("Sunday", "Songs", "Amazing Grace"), CopyReason.BeforeAChange, Time());
        // A library of the same name in another place is another library.
        keeper.Keep(plain, new KeptSong("Documents", "Songs", "Plain Song"), CopyReason.BeforeAChange, Time());
        // A name that is not fit to be a folder's is made fit.
        keeper.Keep(plain, new KeptSong("Sunday / AM: one", "Songs", "Plain Song"), CopyReason.BeforeAChange, Time());
        // A song's folder with nothing in it that is a copy is no song with copies.
        Directory.CreateDirectory(Path.Combine(keeper.Folder, "Sunday", "Songs", "Nothing kept"));
        File.WriteAllText(Path.Combine(keeper.Folder, "Sunday", "Songs", "Nothing kept", "Read me.txt"), "a note");

        Assert.Equal(
            ["Documents/Songs/Plain Song", "Sunday/Songs/Amazing Grace", "Sunday/Songs/Plain Song", "Sunday - AM- one/Songs/Plain Song"],
            keeper.Songs().Select(song => song.Id));
        Assert.Equal(["Amazing Grace", "Plain Song"], keeper.NamesWithCopies("Sunday", "Songs").Order(StringComparer.Ordinal));
        Assert.Equal(["Plain Song"], keeper.NamesWithCopies("Documents", "Songs"));
        Assert.Equal(["Plain Song"], keeper.NamesWithCopies("Sunday / AM: one", "Songs"));
        Assert.Empty(keeper.NamesWithCopies("Sunday", "Kids"));
        Assert.Contains("plain song", keeper.NamesWithCopies("Sunday", "Songs"));
        Assert.Equal((new FileInfo(plain).Length * 3) + new FileInfo(grace).Length, keeper.Size);
        // Every copy of a song is counted, not its newest alone.
        var before = keeper.Size;
        new OpenSong(plain, bench.Safeguards).SetKey("E");
        keeper.Keep(plain, Kept, CopyReason.BeforeAChange, Time(seconds: 60));
        Assert.Equal(before + new FileInfo(plain).Length, keeper.Size);
        Assert.Equal(2, keeper.CopiesOf(Kept).Count);
        // Nothing kept yet is nothing found, and no folder made to look.
        var none = new Copies(Path.Combine(bench.Folder, "Nowhere", "Backups"));
        Assert.Empty(none.Songs());
        Assert.Equal((0L, 0), (none.Size, none.Thin()));
        Assert.False(Directory.Exists(Path.Combine(bench.Folder, "Nowhere")));
    }

    [Fact]
    public void WhenACopyWasMadeAsTheListSaysIt()
    {
        var keeper = new Copies(Path.Combine(bench.Folder, "Nowhere"), "", TimeZoneInfo.Utc);
        var british = CultureInfo.GetCultureInfo("en-GB");
        // Now is Sunday 11 October 2026, 09:14.
        Assert.Equal("Today 08:14", keeper.When(Time(seconds: -3600), Time(), british));
        Assert.Equal("Today 00:00", keeper.When(Time(seconds: -33243), Time(), british));
        Assert.Equal("Saturday 10 Oct", keeper.When(Time(seconds: -33244), Time(), british));
        Assert.Equal("Saturday 10 Oct", keeper.When(Time(days: -1), Time(), british));
        Assert.Equal("Monday 5 Oct", keeper.When(Time(days: -6), Time(), british));
        Assert.Equal("4 Oct", keeper.When(Time(days: -7), Time(), british));
        Assert.Equal("2 Aug", keeper.When(Time(days: -70), Time(), british));
        Assert.Equal("1 Jan", keeper.When(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Time(), british));
        Assert.Equal("31 Dec 2025", keeper.When(new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc), Time(), british));
        Assert.Equal("15 Dec 2025", keeper.When(Time(days: -300), Time(), british));
        // A copy that says it was made after now (the clock has been put back since)
        // is not of today: it is said by its date.
        Assert.Equal("12 Oct", keeper.When(Time(days: 1), Time(), british));
        Assert.Equal("1 Jan 2027", keeper.When(new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc), Time(), british));
        // The day is the computer's own: what is still Saturday at Greenwich is Sunday
        // already where the clocks are eleven hours on, and so is today there.
        var sydney = new Copies(Path.Combine(bench.Folder, "Nowhere"), "", TimeZoneInfo.FindSystemTimeZoneById("AUS Eastern Standard Time"));
        Assert.Equal("Today 01:14", sydney.When(new DateTime(2026, 10, 10, 14, 14, 3, DateTimeKind.Utc), Time(), british));
        // The names of days and months are the person's own language's.
        Assert.StartsWith("samedi 10 oct", keeper.When(Time(days: -1), Time(), CultureInfo.GetCultureInfo("fr-FR")), StringComparison.Ordinal);
    }
}
