// Finding places, the lists, the marks, the app's own folder, a song as lines of text,
// and whether ProPresenter is running: spec/06-library.md, rules 1 to 11, 17, and "What
// the app keeps of its own". All on folders and settings made up for the test, in a
// temporary folder: no test asks this PC's ProPresenter anything, or looks where it
// keeps its songs.
//
// NEW: nothing here is in Simple Presenter, so there is no vector for any of it. What a
// song's marks should be is held to as-read-by-simple-presenter.json all the same, which
// says how many chords each test file has. The rest is MINE: what each case expects is
// the page's rule, as the Mac's tests of the same hold it (LibraryTests.swift), with
// what is Windows' own beside: the file of paths, and what the system marks hidden.

using System.Diagnostics;
using System.Text.Json;
using Pro7ChordEditor.ChordRules;
using Pro7ChordEditor.Tests;
using Xunit;

namespace Pro7ChordEditor.Song.Tests;

public sealed class LibraryTests : IDisposable
{
    private static readonly string Backslash = ((char)0x5C).ToString();
    private static readonly string Ete = (char)0xC9 + "t" + (char)0xE9;
    private readonly Workbench bench = new();

    public void Dispose() => bench.Dispose();

    // A place laid out as ProPresenter lays one out, in the bench's folder: two
    // libraries of copies of the test files and one with nothing in it, and beside them
    // what is not a library and not a song.
    private string MadeUpPlace(string name = "ProPresenter")
    {
        var place = Path.Combine(bench.Folder, name);
        var songs = Path.Combine(place, "Libraries", "Songs");
        var copies = Path.Combine(place, "Libraries", "Old copies");
        foreach (var folder in new[] { songs, copies, Path.Combine(place, "Libraries", "Empty"), Path.Combine(place, "Media"), Path.Combine(place, "Libraries", ".hidden") })
        {
            Directory.CreateDirectory(folder);
        }
        new DirectoryInfo(Directory.CreateDirectory(Path.Combine(place, "Libraries", "Hidden by Windows")).FullName).Attributes |= FileAttributes.Hidden;
        foreach (var path in TestData.PresentationPaths())
        {
            File.WriteAllBytes(Path.Combine(path.StartsWith("as-the-old", StringComparison.Ordinal) ? copies : songs, Path.GetFileName(path)), TestData.InFiles(path));
        }
        // Things in `Libraries` and in a library that are not libraries and not songs.
        File.WriteAllText(Path.Combine(place, "Libraries", "LibraryData"), "not a folder");
        File.WriteAllText(Path.Combine(songs, "Doxology.pro6"), "an older kind of file");
        File.WriteAllText(Path.Combine(songs, "Doxology.proBundle"), "a bundle");
        File.WriteAllText(Path.Combine(songs, "Nonsense.pro"), "This is not a song.");
        File.WriteAllBytes(Path.Combine(songs, ".Hidden.pro"), []);
        File.WriteAllBytes(Path.Combine(songs, "Hidden by Windows.pro"), TestData.InFiles("made-by-simple-presenter/Plain Song.pro"));
        File.SetAttributes(Path.Combine(songs, "Hidden by Windows.pro"), FileAttributes.Hidden);
        Directory.CreateDirectory(Path.Combine(songs, "A folder.pro"));
        return place;
    }

    // A folder as ProPresenter's file of paths writes it: each backslash twice.
    private static string Twice(string path) => path.Replace(Backslash, Backslash + Backslash, StringComparison.Ordinal);

    private static readonly string[] SongsOfTheMadeUpPlace =
        ["A Song - In A Minor Key", "Amazing Grace", "Blank Slides", "Chords With No Words", "Holy Holy Holy", "It Is Well", "Nonsense", "Plain Song", "Rock Of Ages (Toplady)"];

    // ---- Places

    [Fact]
    public void TheFolderProPresentersFileNamesDocumentsAndFoldersChosenByHandArePlacesEachOnceAndOnlyIfThere()
    {
        var moved = MadeUpPlace("Moved");
        var documents = Path.Combine(bench.Folder, "Documents");
        var inDocuments = MadeUpPlace(Path.Combine("Documents", "ProPresenter"));
        var chosen = Directory.CreateDirectory(Path.Combine(bench.Folder, "Somewhere else")).FullName;
        // As ProPresenter writes the file: the folder with a backslash after it, each
        // backslash twice, and other lines beside.
        var file = "Media=" + Twice(Path.Combine(moved, "Media") + Backslash) + ";\r\nBase=" + Twice(moved + Backslash) + ";\r\n";

        var places = new PlaceFinder(() => file, documents, [chosen, inDocuments.ToUpperInvariant() + Backslash, Path.Combine(bench.Folder, "Not there")]).Places();

        Assert.Equal([PlaceKind.NamedInSettings, PlaceKind.Documents, PlaceKind.Chosen], places.Select(place => place.Kind));
        Assert.Equal(["Moved", "Documents", "Somewhere else"], places.Select(place => place.Name));
        Assert.Equal(["Moved", "ProPresenter", "Somewhere else"], places.Select(place => Path.GetFileName(place.Id)));
        Assert.Equal(moved, places[0].Id);

        // The file's folder and Documents may be one folder: it is shown once, as the
        // file's.
        var same = new PlaceFinder(() => "Base=" + Twice(inDocuments) + ";", documents).Places();
        Assert.Equal([PlaceKind.NamedInSettings], same.Select(place => place.Kind));
    }

    [Fact]
    public void WithNoFileOrAnEmptyOneOrOneThatNamesNoProPresenterFolderThereIsOnlyDocuments()
    {
        var documents = Path.Combine(bench.Folder, "Documents");
        MadeUpPlace(Path.Combine("Documents", "ProPresenter"));
        var bare = Directory.CreateDirectory(Path.Combine(bench.Folder, "No libraries here")).FullName;

        // No file; a file with nothing in it, as ProPresenter 22 leaves one; one that
        // names a folder with no `Libraries` in it; one that names a folder that is gone.
        foreach (var file in new[] { null, "", "Base=" + Twice(bare) + ";", "Base=" + Twice(Path.Combine(bench.Folder, "Gone")) + ";" })
        {
            var places = new PlaceFinder(() => file, documents).Places();
            Assert.Equal([PlaceKind.Documents], places.Select(place => place.Kind));
        }
        // And with nothing anywhere there is no place, which is not an error.
        Assert.Empty(new PlaceFinder(() => null, Path.Combine(bench.Folder, "No documents")).Places());
        Assert.Empty(new PlaceFinder(() => null, null).Places());
    }

    [Fact]
    public void TheFolderAFileOfPathsNamesIsWhatStandsBetweenBaseAndTheLastSemicolonOfItsLine()
    {
        var folder = "C:" + Backslash + "Some" + Backslash + "Folder";
        Assert.Null(PlaceFinder.BaseNamedIn(null));
        Assert.Null(PlaceFinder.BaseNamedIn(""));
        Assert.Null(PlaceFinder.BaseNamedIn("Media=" + Twice(folder) + ";"));
        Assert.Equal(folder, PlaceFinder.BaseNamedIn("Base=" + Twice(folder) + ";"));
        Assert.Equal(folder + Backslash, PlaceFinder.BaseNamedIn("Base=" + Twice(folder + Backslash) + ";"));
        Assert.Equal(folder, PlaceFinder.BaseNamedIn("First=1;\nBase=" + Twice(folder) + ";\nLast=2;"));
        Assert.Equal(folder, PlaceFinder.BaseNamedIn("First=1;\r\nBase=" + Twice(folder) + ";\r\nLast=2;\r\n"));
        // A folder on another computer, which begins with two backslashes, written as four.
        Assert.Equal(Backslash + Backslash + "server" + Backslash + "share", PlaceFinder.BaseNamedIn("Base=" + Twice(Backslash + Backslash + "server" + Backslash + "share") + ";"));
        // Up to the LAST semicolon of the line, and not into the next line.
        Assert.Equal("C:" + Backslash + "A;B", PlaceFinder.BaseNamedIn("Base=" + Twice("C:" + Backslash + "A;B") + ";\nOther=1;"));
        // Nothing named, or a line that does not end as such a line ends, names none.
        Assert.Null(PlaceFinder.BaseNamedIn("Base=;"));
        Assert.Null(PlaceFinder.BaseNamedIn("Base=   ;"));
        Assert.Null(PlaceFinder.BaseNamedIn("Base=" + Twice(folder)));
        Assert.Null(PlaceFinder.BaseNamedIn("Base=" + Twice(folder) + "\nOther=1;"));
        // The first line that names one is taken.
        Assert.Equal(folder, PlaceFinder.BaseNamedIn("Base=;\nBase=" + Twice(folder) + ";\nBase=D:" + Twice(Backslash) + "Other;"));
    }

    [Fact]
    public void AFolderIsKnownByWhereItIsWhateverItsCapitalsAndWhateverStandsAfterIt()
    {
        var folder = Path.Combine(bench.Folder, "Songs");
        Assert.Equal(folder, Paths.IdOf(folder + Backslash));
        Assert.Equal(folder, Paths.IdOf(Path.Combine(bench.Folder, "Other", "..", "Songs")));
        Assert.True(Paths.Same(Paths.IdOf(folder.ToUpperInvariant()), Paths.IdOf(folder)));
        Assert.False(Paths.Same(Paths.IdOf(folder), Paths.IdOf(folder + "2")));
        Assert.False(Paths.Same(null, folder));
        Assert.Equal("Songs", Paths.NameOf(folder + Backslash));
        Assert.Equal("C:" + Backslash, Paths.NameOf("C:" + Backslash));
    }

    // ---- The lists

    [Fact]
    public void APlacesLibrariesAreTheFoldersOfItsLibrariesByNameAndALibrarysSongsAreItsProFilesByName()
    {
        var place = new Place(PlaceKind.Documents, "Made up", MadeUpPlace());
        var libraries = Listing.Libraries(place);
        // Not the file beside them, nor either hidden folder; the empty one is a library still.
        Assert.Equal(["Empty", "Old copies", "Songs"], libraries.Select(library => library.Name));
        Assert.Equal(Path.Combine(place.Folder, "Libraries", "Songs"), libraries[2].Id);

        var songs = Listing.Songs(libraries[2]);
        // Not the older kind of file nor the bundle, not either hidden file, not the folder.
        Assert.Equal(SongsOfTheMadeUpPlace, songs.Select(song => song.Name));
        Assert.All(songs, song => Assert.True(song.Size > 0 && song.Modified > new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        Assert.All(songs, song => Assert.Equal(new FileInfo(song.Path).Length, song.Size));
        Assert.All(songs, song => Assert.Equal(File.GetLastWriteTimeUtc(song.Path), song.Modified));
        Assert.Equal(Path.Combine(libraries[2].Folder, "Plain Song.pro"), songs.Single(song => song.Name == "Plain Song").Id);
        Assert.Empty(Listing.Songs(libraries[0]));
        Assert.Equal(4, Listing.Songs(libraries[1]).Count);
        // A library that is not there has no songs, which is not an error.
        Assert.Empty(Listing.Songs(new Library("Gone", Path.Combine(bench.Folder, "Gone"))));
    }

    [Fact]
    public void ASongsEndingIsOneWhateverItsCapitalsAndItsNameIsWhatStandsBeforeIt()
    {
        var folder = Directory.CreateDirectory(Path.Combine(bench.Folder, "Endings")).FullName;
        foreach (var name in new[] { "Loud.PRO", "quiet.pro", "Mixed.Pro", "Two.endings.pro", "Not one.pro.bak", "Nor this.prox", "Nor.pro this" })
        {
            File.WriteAllBytes(Path.Combine(folder, name), [1]);
        }
        Assert.Equal(["Loud", "Mixed", "quiet", "Two.endings"], Listing.Songs(new Library("Endings", folder)).Select(song => song.Name));
        // And such a folder, chosen, is a library for having them.
        Assert.Equal(["Endings"], Listing.Libraries(new Place(PlaceKind.Chosen, "", folder)).Select(library => library.Name));
    }

    [Fact]
    public void NamesAreSortedAsTheSystemSortsThem()
    {
        var folder = Directory.CreateDirectory(Path.Combine(bench.Folder, "Numbers")).FullName;
        foreach (var name in new[] { "Song 10", "song 2", "Song 1", Ete, "Zion", "apple" })
        {
            File.WriteAllBytes(Path.Combine(folder, name + ".pro"), []);
        }
        // Numbers by how much they are, no regard to capitals, and an E with an accent
        // among the Es.
        Assert.Equal(["apple", Ete, "Song 1", "song 2", "Song 10", "Zion"], Listing.Songs(new Library("Numbers", folder)).Select(song => song.Name));
    }

    [Fact]
    public void AFolderChosenByHandIsTakenForWhatItHas()
    {
        var whole = MadeUpPlace();
        IEnumerable<string> LibrariesOf(params string[] parts) =>
            Listing.Libraries(new Place(PlaceKind.Chosen, "", Path.Combine([whole, .. parts]))).Select(library => library.Name);

        // The ProPresenter folder itself, its Libraries folder, and one library.
        Assert.Equal(["Empty", "Old copies", "Songs"], LibrariesOf());
        // (Chosen so, a folder with no songs in it is not offered as a library.)
        Assert.Equal(["Old copies", "Songs"], LibrariesOf("Libraries"));
        var one = Listing.Libraries(new Place(PlaceKind.Chosen, "", Path.Combine(whole, "Libraries", "Songs")));
        Assert.Equal(["Songs"], one.Select(library => library.Name));
        Assert.Equal(SongsOfTheMadeUpPlace.Length, Listing.Songs(one[0]).Count);
        // (One library, by its own folder's name, whatever the place was called.)
        Assert.Equal(["Old copies"], LibrariesOf("Libraries", "Old copies"));
        Assert.Equal(Path.Combine(whole, "Libraries", "Old copies"), Listing.Libraries(new Place(PlaceKind.Chosen, "Called otherwise", Path.Combine(whole, "Libraries", "Old copies") + Backslash)).Single().Id);
        Assert.Empty(LibrariesOf("Media"));
        Assert.Empty(LibrariesOf("Not there"));
    }

    [Fact]
    public void TheListIsNarrowedByWhatIsTypedCapitalsAndAccentsApart()
    {
        var songs = new[] { "Amazing Grace", "This Is Amazing Grace", Ete, "Holy Holy Holy" }
            .Select(name => new ListedSong(name, Path.Combine(bench.Folder, name + ".pro"), 1, DateTime.UnixEpoch)).ToList();
        IEnumerable<string> Narrowed(string typed) => Listing.Narrowed(songs, typed).Select(song => song.Name);

        Assert.Equal(4, Narrowed("").Count());
        Assert.Equal(4, Narrowed("  ").Count());
        Assert.Equal(["Amazing Grace", "This Is Amazing Grace"], Narrowed("grace"));
        Assert.Equal(["Amazing Grace", "This Is Amazing Grace"], Narrowed(" AMAZING g"));
        Assert.Equal([Ete], Narrowed("ete"));
        Assert.Equal([Ete], Narrowed("T" + (char)0xC9));
        Assert.Empty(Narrowed("zzz"));
    }

    [Fact]
    public void ASongsFileAskedAgainIsAsItIsNowOrNothingIfItIsGone()
    {
        var path = bench.CopyOf("Plain Song.pro");
        var listed = new ListedSong("Plain Song", path, 1, DateTime.UnixEpoch);
        var now = Listing.AsItIsNow(listed);
        Assert.Equal(listed with { Size = new FileInfo(path).Length, Modified = File.GetLastWriteTimeUtc(path) }, now);
        File.Delete(path);
        Assert.Null(Listing.AsItIsNow(listed));
    }

    // ---- The marks

    [Fact]
    public void ASongsMarksHowManyChordsItHasWhetherItIsOneOfTheOldEditorsCopiesWhetherItCanBeRead()
    {
        var place = new Place(PlaceKind.Documents, "Made up", MadeUpPlace());
        using var asRead = JsonDocument.Parse(TestData.InFiles("expected/as-read-by-simple-presenter.json"));
        var chordsOf = asRead.RootElement.GetProperty("files").EnumerateArray().ToDictionary(
            file => file.GetProperty("file").GetString()!,
            file => file.GetProperty("textBoxes").EnumerateArray().Sum(box => box.GetProperty("chords").GetArrayLength()),
            StringComparer.Ordinal);
        var held = 0;
        foreach (var library in Listing.Libraries(place))
        {
            foreach (var song in Listing.Songs(library))
            {
                var marks = Marks.Read(song);
                Assert.Equal(song.Name.EndsWith(" (Chords)", StringComparison.Ordinal), marks.IsOldEditorsCopy);
                if (song.Name == "Nonsense")
                {
                    Assert.True(marks.CannotBeRead);
                    Assert.False(marks.HasChords);
                    continue;
                }
                Assert.False(marks.CannotBeRead);
                // As many chords as the song hands the editors ...
                Assert.Equal(new OpenSong(song.Path).TextBoxes.Sum(box => box.Chords.Count), marks.Chords);
                Assert.Equal(marks.Chords > 0, marks.HasChords);
                // ... and as many as Simple Presenter's editor is handed for that file.
                if (chordsOf.TryGetValue(song.Name + ".pro", out var chords))
                {
                    held++;
                    Assert.Equal(chords, marks.Chords);
                }
            }
        }
        Assert.Equal(11, held);
        var songs = Listing.Songs(Listing.Libraries(place)[2]);
        // (The song ProPresenter wrote is the one with none.)
        Assert.Equal(new Marks(), Marks.Read(songs.Single(song => song.Name == "Rock Of Ages (Toplady)")));
        Assert.Equal(40, Marks.Read(songs.Single(song => song.Name == "Amazing Grace")).Chords);
        // A file that is not there cannot be read either.
        Assert.True(Marks.Read(new ListedSong("Gone (Chords)", Path.Combine(bench.Folder, "Gone (Chords).pro"), 0, DateTime.UnixEpoch)) is { CannotBeRead: true, IsOldEditorsCopy: true, Chords: 0 });
    }

    // spec/06, rule 9: "it is one of the old editor's "(Chords)" copies: its name ends
    // ` (Chords)`", a space and then the word in brackets with its capital, as that
    // editor wrote it (spec/08-repair.md, D).
    [Fact]
    public void TheOldEditorsCopyIsASongWhoseNameEndsWithASpaceAndChordsInBracketsAndNoOther()
    {
        var path = bench.CopyOf("Plain Song.pro");
        bool IsACopy(string name) => Marks.Read(new ListedSong(name, path, 0, DateTime.UnixEpoch)).IsOldEditorsCopy;

        Assert.True(IsACopy("Plain Song (Chords)"));
        Assert.True(IsACopy("Plain Song (Chords) (Chords)"));
        Assert.False(IsACopy("Plain Song"));
        Assert.False(IsACopy("Plain Song (Chords) 2"));
        Assert.False(IsACopy("Plain Song (chords)"));
        Assert.False(IsACopy("Plain Song(Chords)"));
        Assert.False(IsACopy("Plain Song (Chords) "));
        Assert.Equal(" (Chords)", Marks.OldEditorsEnding);
    }

    [Fact]
    public void ChordsOnABoxThatShowsSomethingElsesWordsAreNoneOfTheSongs()
    {
        // The editors are not shown a box that shows a timer or another box's words
        // (Reading.cs), and its chords are not counted.
        var path = bench.CopyOf("Plain Song.pro");
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
        var all = Marks.Read(new ListedSong("Plain Song", path, 0, DateTime.UnixEpoch)).Chords;

        Assert.Equal(all - first.Chords.Count, Marks.Read(new ListedSong("A box that shows a ticker", changed, 0, DateTime.UnixEpoch)).Chords);
        Assert.True(first.Chords.Count > 0);
    }

    [Fact]
    public void MarksWorkedOutBeforeAreKnownAgainOnlyWhileTheFileIsAsItWas()
    {
        var song = new ListedSong("A", Path.Combine(bench.Folder, "A.pro"), 100, DateTime.UnixEpoch.AddSeconds(1000));
        var kept = new KeptMarks();
        Assert.Null(kept.MarksFor(song));
        kept.Keep(new Marks(12), song);
        Assert.Equal(new Marks(12), kept.MarksFor(song));
        Assert.Equal(1, kept.Count);
        // (The same file, as Windows knows it, whatever its capitals.)
        Assert.Equal(new Marks(12), kept.MarksFor(song with { Path = song.Path.ToUpperInvariant() }));
        Assert.Null(kept.MarksFor(song with { Size = 101 }));
        Assert.Null(kept.MarksFor(song with { Modified = song.Modified.AddTicks(1) }));
        Assert.Null(kept.MarksFor(song with { Path = Path.Combine(bench.Folder, "Elsewhere", "A.pro") }));
        // Kept again, it is the newer that is known.
        kept.Keep(new Marks(13), song with { Size = 101 });
        Assert.Equal(new Marks(13), kept.MarksFor(song with { Size = 101 }));
        Assert.Equal(1, kept.Count);
    }

    [Fact]
    public void TheListCanBeNarrowedToSongsWithChordsOrWithout()
    {
        // (One chord is chords: it is the first chord that takes a song out of "without".)
        Assert.True(new Marks(1).HasChords);
        Assert.False(new Marks(0).HasChords);
        Assert.True(MarkFilter.WithChords.Lets(new Marks(1)));
        Assert.False(MarkFilter.WithoutChords.Lets(new Marks(1)));
        Marks with = new(3), without = new(), broken = new(CannotBeRead: true);
        Assert.True(MarkFilter.All.Lets(with) && MarkFilter.All.Lets(without) && MarkFilter.All.Lets(null) && MarkFilter.All.Lets(broken));
        Assert.True(MarkFilter.WithChords.Lets(with));
        Assert.False(MarkFilter.WithChords.Lets(without) || MarkFilter.WithChords.Lets(null) || MarkFilter.WithChords.Lets(broken));
        // A song that cannot be read is not said to be without chords: nobody knows.
        Assert.True(MarkFilter.WithoutChords.Lets(without));
        Assert.False(MarkFilter.WithoutChords.Lets(with) || MarkFilter.WithoutChords.Lets(null) || MarkFilter.WithoutChords.Lets(broken));
    }

    // ---- The app's own folder

    [Fact]
    public void TheAppsSettingsAndMarksAreKeptInItsOwnFolderAndReadBack()
    {
        var data = new AppData(Path.Combine(bench.Folder, "Own", "Pro7ChordEditor"));
        // Nothing kept yet is not an error, and makes no folder.
        Assert.Equal(new AppData.Settings(), data.ReadSettings());
        Assert.Equal(0, data.ReadMarks().Count);
        Assert.False(Directory.Exists(data.Folder));

        var settings = new AppData.Settings
        {
            ChosenFolders = [Path.Combine(bench.Folder, "Somewhere", "Songs"), Path.Combine(bench.Folder, "Elsewhere")],
            LastPlace = Path.Combine(bench.Folder, "Somewhere"),
            LastLibrary = Path.Combine(bench.Folder, "Somewhere", "Songs"),
            ListsSplit = 0.25,
        };
        data.Keep(settings);
        var song = new ListedSong("A", Path.Combine(bench.Folder, "A.pro"), 9, File.GetLastWriteTimeUtc(TestData.Repository));
        var kept = new KeptMarks();
        kept.Keep(new Marks(4, IsOldEditorsCopy: true), song);
        kept.Keep(new Marks(CannotBeRead: true), song with { Path = Path.Combine(bench.Folder, "B.pro") });
        data.Keep(kept);

        var again = new AppData(data.Folder);
        Assert.Equal(settings, again.ReadSettings());
        Assert.NotEqual(new AppData.Settings(), again.ReadSettings());
        var read = again.ReadMarks();
        Assert.Equal(2, read.Count);
        // (To the very tick of the file's time: a mark is only known again while that
        // is what the file still has.)
        Assert.Equal(new Marks(4, IsOldEditorsCopy: true), read.MarksFor(song));
        Assert.Equal(new Marks(CannotBeRead: true), read.MarksFor(song with { Path = Path.Combine(bench.Folder, "B.pro") }));
        // What is kept is in the app's own folder, under the names the page gives, and
        // nothing else is left there.
        Assert.Equal(["marks.json", "settings.json"], Directory.EnumerateFileSystemEntries(data.Folder).Select(entry => Path.GetFileName(entry)).Order(StringComparer.Ordinal));

        // Kept again, it is the newer that is read: what is kept takes the place of what was.
        data.Keep(settings with { LastPlace = Path.Combine(bench.Folder, "Elsewhere"), ListsSplit = null });
        Assert.Equal(settings with { LastPlace = Path.Combine(bench.Folder, "Elsewhere"), ListsSplit = null }, again.ReadSettings());
        kept.Keep(new Marks(7), song);
        data.Keep(kept);
        Assert.Equal(new Marks(7), again.ReadMarks().MarksFor(song));
        Assert.Equal(["marks.json", "settings.json"], Directory.EnumerateFileSystemEntries(data.Folder).Select(entry => Path.GetFileName(entry)).Order(StringComparer.Ordinal));

        // A file that cannot be read is taken as not there.
        File.WriteAllText(Path.Combine(data.Folder, "settings.json"), "{ not json");
        File.WriteAllText(Path.Combine(data.Folder, "marks.json"), "[1, 2");
        Assert.Equal(new AppData.Settings(), again.ReadSettings());
        Assert.Equal(0, again.ReadMarks().Count);
    }

    [Fact]
    public void SettingsAreTheSameWhenTheyHaveTheSameThingsInThem()
    {
        var one = new AppData.Settings { ChosenFolders = ["a", "b"], LastPlace = "p", LastLibrary = "l", ListsSplit = 0.5 };
        Assert.Equal(one, new AppData.Settings { ChosenFolders = ["a", "b"], LastPlace = "p", LastLibrary = "l", ListsSplit = 0.5 });
        Assert.NotEqual(one, one with { ChosenFolders = ["a"] });
        Assert.NotEqual(one, one with { ChosenFolders = ["b", "a"] });
        Assert.NotEqual(one, one with { LastPlace = null });
        Assert.NotEqual(one, one with { LastLibrary = "other" });
        Assert.NotEqual(one, one with { ListsSplit = null });
    }

    [Fact]
    public void TheLogHasALineForEachThingWithItsTimeAndIsBegunAgainWhenItHasGrownLong()
    {
        var data = new AppData(Path.Combine(bench.Folder, "Own"));
        data.Log("Started.", DateTime.UnixEpoch);
        data.Log("Opened Plain Song.pro\nin Songs.", DateTime.UnixEpoch.AddSeconds(61));
        var lines = File.ReadAllLines(Path.Combine(data.Folder, "log.txt"));
        Assert.Equal(["1970-01-01T00:00:00Z  Started.", "1970-01-01T00:01:01Z  Opened Plain Song.pro in Songs."], lines);
        // (A time given by the clock on the wall is written by the clock at Greenwich.)
        data.Log("Later.", new DateTime(2026, 10, 12, 12, 0, 0, DateTimeKind.Local));
        var later = File.ReadAllLines(Path.Combine(data.Folder, "log.txt"))[2];
        Assert.Equal(new DateTime(2026, 10, 12, 12, 0, 0, DateTimeKind.Local).ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture) + "  Later.", later);

        // Past a megabyte, the next line begins a new log and the old one is put aside,
        // in place of the one put aside before.
        File.WriteAllText(Path.Combine(data.Folder, "log-before.txt"), "the one before");
        File.WriteAllBytes(Path.Combine(data.Folder, "log.txt"), new byte[AppData.LongLog + 1]);
        data.Log("After.");
        Assert.EndsWith("  After." + Environment.NewLine, File.ReadAllText(Path.Combine(data.Folder, "log.txt")), StringComparison.Ordinal);
        Assert.Single(File.ReadAllLines(Path.Combine(data.Folder, "log.txt")));
        Assert.Equal(AppData.LongLog + 1, new FileInfo(Path.Combine(data.Folder, "log-before.txt")).Length);
        // At a megabyte and no more, it is not yet long.
        File.WriteAllBytes(Path.Combine(data.Folder, "log.txt"), new byte[AppData.LongLog]);
        data.Log("Still the same log.");
        Assert.True(new FileInfo(Path.Combine(data.Folder, "log.txt")).Length > AppData.LongLog);
        Assert.Equal(AppData.LongLog + 1, new FileInfo(Path.Combine(data.Folder, "log-before.txt")).Length);
    }

    [Fact]
    public void WhatCannotBeKeptIsNotAnErrorToTroubleAnyoneWith()
    {
        // The app's own folder cannot be made: a file stands where it would be.
        var inTheWay = bench.FileOf("In the way", [1]);
        var data = new AppData(Path.Combine(inTheWay, "Pro7ChordEditor"));
        data.Keep(new AppData.Settings { LastPlace = "x" });
        data.Keep(new KeptMarks());
        data.Log("Nothing comes of this.");
        Assert.Equal(new AppData.Settings(), data.ReadSettings());
        Assert.Equal([1], File.ReadAllBytes(inTheWay));
    }

    // ---- A song as lines of text

    public static TheoryData<string> EveryTestFile() => [.. TestData.PresentationPaths()];

    [Theory]
    [MemberData(nameof(EveryTestFile))]
    public void ASongAsLinesAGapBeforeEachSlideButTheFirstAGroupsNameBeforeItsFirstSlideThenEachLineOfEachBox(string path)
    {
        var boxes = new OpenSong(Path.Combine(TestData.Root, "files", path.Replace('/', Path.DirectorySeparatorChar))).TextBoxes;
        var lines = SongText.Lines(boxes);

        // The gaps: one fewer than the slides that have a box.
        Assert.Equal(boxes.Select(box => box.Slide).Distinct().Count() - 1, lines.Count(line => line.Kind == SongLineKind.Gap));
        Assert.NotEqual(SongLineKind.Gap, lines[0].Kind);
        Assert.NotEqual(SongLineKind.Gap, lines[^1].Kind);
        // A gap is before the slide that follows it, and after the one before.
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].Kind == SongLineKind.Gap)
            {
                Assert.Equal(lines[i + 1].Slide, lines[i].Slide);
                Assert.True(lines[i - 1].Slide < lines[i].Slide);
                Assert.Equal("", lines[i].Text);
                Assert.Equal(-1, lines[i].Box);
            }
        }
        // The groups: one line for each slide that starts a group with a name, before
        // that slide's first line.
        var starts = boxes.Where(box => box.StartsItsGroup && box.Group.Length > 0).GroupBy(box => box.Slide).Select(slide => slide.First()).ToList();
        var groups = lines.Where(line => line.Kind == SongLineKind.Group).ToList();
        Assert.Equal(starts.Select(box => "{c: " + box.Group + "}"), groups.Select(line => line.Text));
        Assert.Equal(starts.Select(box => (box.Group, box.GroupColour, box.Slide)), groups.Select(line => (line.Group, line.GroupColour, line.Slide)));
        Assert.All(groups, group => Assert.Equal(group, lines.First(line => line.Slide == group.Slide && line.Kind != SongLineKind.Gap)));
        // Each box's lines, put together again, are the box as `ToChordPro` writes it
        // (which chordpro.json holds), and each line knows where it is in its box.
        for (var index = 0; index < boxes.Count; index++)
        {
            var box = boxes[index];
            var own = lines.Where(line => line.Box == index).ToList();
            Assert.All(own, line => Assert.True(line.Slide == box.Slide && line.Element == box.Element));
            // A blank slide is one line, and that line is empty.
            if (box.Blank)
            {
                Assert.Equal([SongLineKind.BlankSlide], own.Select(line => line.Kind));
                Assert.Equal("", own[0].Text);
                continue;
            }
            Assert.Equal(Chords.ToChordPro(box.Words, box.Chords), string.Join('\n', own.Select(line => line.Text)));
            Assert.Equal(Words.Lines(box.Words), own.Select(line => line.Words));
            Assert.Equal(Enumerable.Range(0, own.Count), own.Select(line => line.Line));
            Assert.All(own, line => Assert.Equal(line.Words, box.Words.Substring(line.Start, line.Words.Length)));
            Assert.All(own, line => Assert.Equal(Chords.IsPlaceholders(line.Words) ? SongLineKind.ChordsAlone : SongLineKind.Words, line.Kind));
        }
        Assert.Equal(string.Join('\n', lines.Select(line => line.Text)), SongText.Text(boxes));
        Assert.Equal(boxes.Count, lines.Where(line => line.Box >= 0).Select(line => line.Box).Distinct().Count());
    }

    [Fact]
    public void PlainSongAsItsLinesIsTheChordProFileItWasMadeFrom()
    {
        var song = new OpenSong(Path.Combine(TestData.Root, "files", "made-by-simple-presenter", "Plain Song.pro"));
        // testdata/songs/plain-song.cho, without what says what the song is, and with its
        // verse as the two slides it was dealt into, two lines to a slide.
        string[] expected =
        [
            "{c: Verse 1}",
            "[G]One two three [D]four",
            "Five six [Em]seven eight",
            "",
            "[C]Nine ten e[G/B]leven twelve",
            "Thirteen",
            "",
            "{c: Chorus}",
            "[C]Sing it [G]out",
            "[D]Sing it loud",
            "",
            "{c: Outro}",
            "[G] [D/F#] [Em]",
        ];
        Assert.Equal(string.Join('\n', expected), SongText.Text(song.TextBoxes));
        Assert.Equal(
            [SongLineKind.Group, SongLineKind.Words, SongLineKind.Words, SongLineKind.Gap, SongLineKind.Words, SongLineKind.Words, SongLineKind.Gap,
             SongLineKind.Group, SongLineKind.Words, SongLineKind.Words, SongLineKind.Gap, SongLineKind.Group, SongLineKind.ChordsAlone],
            SongText.Lines(song.TextBoxes).Select(line => line.Kind));
    }

    [Fact]
    public void AChordStandingOnAnEmptyLineIsNotLostAndABlankSlideIsOneEmptyLine()
    {
        TextBox Box(int slide, string words, bool blank = false, params Chord[] chords) => new(slide, slide == 0 ? "Verse" : "", null, slide == 0, "id" + slide, words, chords, blank);
        var lines = SongText.Lines(
        [
            Box(0, "One\n\nThree", false, new Chord(0, "G"), new Chord(4, "D"), new Chord(5, "Em")),
            Box(1, "", true),
            Box(2, "Last"),
        ]);
        Assert.Equal(["{c: Verse}", "[G]One", "[D]", "[Em]Three", "", "", "", "Last"], lines.Select(line => line.Text));
        Assert.Equal(
            [SongLineKind.Group, SongLineKind.Words, SongLineKind.Words, SongLineKind.Words, SongLineKind.Gap, SongLineKind.BlankSlide, SongLineKind.Gap, SongLineKind.Words],
            lines.Select(line => line.Kind));
        Assert.Equal([0, 0, 4, 5, 0, 0, 0, 0], lines.Select(line => line.Start));
        // (A slide that starts a group with no name has no line for it.)
        Assert.DoesNotContain(lines, line => line.Kind == SongLineKind.Group && line.Slide > 0);
        Assert.Equal("id1", lines[5].Element);
        Assert.Equal(1, lines[5].Box);
    }

    // ---- Whether ProPresenter is running

    [Fact]
    public void ProPresenterIsRunningIfAProcessOfItsNameIs()
    {
        Assert.Equal("ProPresenter", ProPresenterApp.ProcessName);
        // (No test starts ProPresenter. A process there is, is this one.)
        using var me = Process.GetCurrentProcess();
        Assert.True(ProPresenterApp.IsRunning(me.ProcessName));
        Assert.False(ProPresenterApp.IsRunning("Pro7ChordEditor no such program " + Guid.NewGuid().ToString("N")));
        // The name is asked for whole: a helper program whose name only begins with it
        // is another program.
        Assert.False(ProPresenterApp.IsRunning(me.ProcessName[..^1]));
    }
}
