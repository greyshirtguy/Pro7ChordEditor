// What the window shows and what a click does, without drawing anything: LibraryModel
// on a made-up PC. spec/06-library.md, rules 6 to 15, 18 and 19.
//
// MINE, and held to the page: nothing here is in Simple Presenter, so no vector holds
// any of it. Where the Mac's tests of its own model hold the same thing
// (LibraryModelTests.swift), these expect what those expect, so that the two apps say
// the same sentences and list the same songs. How many chords a test file has is the
// reference's count (testdata/files/expected/as-read-by-simple-presenter.json).
//
// Each test is on a thread of the kind a window has (Pictures.cs), as the model is in
// the app.

using System.IO;
using Pro7ChordEditor.Song;
using Pro7ChordEditor.Tests;
using Xunit;

namespace Pro7ChordEditor.AppViews.Tests;

public class LibraryModelTests
{
    private static void OnAMadeUpPC(Func<MadeUpPC, Task> what) => Pictures.OnAThreadOfItsOwn(async () =>
    {
        using var pc = new MadeUpPC();
        await what(pc);
    });

    private static IEnumerable<string> Names(IEnumerable<ListedSong> songs) => songs.Select(song => song.Name);

    [Fact]
    public void AtTheStartThePlaceIsFoundItsFirstLibraryIsOpenAndTheMarksAreWorkedOutAndKept() => OnAMadeUpPC(async pc =>
    {
        var model = pc.Model();
        // Before it has started there is nothing, and nothing is said.
        Assert.Empty(model.Places);
        Assert.Equal("", model.Notice + model.NoSongSentence + model.NoPlaceSentence + model.NoLibrariesSentence + model.SongsCount);
        await model.StartAsync();

        Assert.Equal([PlaceKind.Documents], model.Places.Select(place => place.Kind));
        Assert.Equal(pc.Place, model.PlaceId);
        Assert.Equal("", model.NoPlaceSentence);
        Assert.Equal(["Kids", "Old copies", "Songs"], model.Libraries.Select(library => library.Name));
        Assert.Equal("Kids", model.Library?.Name);
        Assert.Empty(model.Songs);
        Assert.Null(model.Shown);
        Assert.Equal("0 songs, 0 with chords.", model.Notice);
        Assert.Equal("There are no songs in this library.", model.NoSongSentence);
        Assert.Equal("0 SONGS", model.SongsCount);
        Assert.False(model.ProPresenterIsRunning);

        model.ChooseLibrary(pc.Library("Songs"));
        // The names are listed at once; the marks come after.
        Assert.Equal(MadeUpPC.Songs, Names(model.Songs));
        Assert.Equal("Choose a song to look at it.", model.NoSongSentence);
        await model.MarksAreWorkedOutAsync();
        Assert.Equal(MadeUpPC.Songs.Length, model.Marks.Count);
        Assert.Equal(40, model.Marks[pc.Song("Amazing Grace")].Chords);
        Assert.False(model.Marks[pc.Song("Rock Of Ages (Toplady)")].HasChords);
        Assert.True(model.Marks[pc.Song("Nonsense")].CannotBeRead);
        Assert.Equal(MadeUpPC.SongsWithChords, Names(model.Songs.Where(song => model.Marks[song.Id].HasChords)));
        Assert.Equal("9 songs, 7 with chords.", model.Notice);
        Assert.Equal("9 SONGS", model.SongsCount);

        // The library is remembered, and the marks kept: another start has them at once,
        // with no file read again.
        var again = pc.Model();
        var started = again.StartAsync();
        Assert.Equal("Songs", again.Library?.Name);
        Assert.Equal(MadeUpPC.Songs.Length, again.Marks.Count);
        Assert.True(again.MarksAreWorkedOutAsync().IsCompleted);
        await started;
        Assert.Equal(MadeUpPC.Songs.Length, pc.AppData.ReadMarks().Count);
        Assert.Equal(pc.Library("Songs"), pc.AppData.ReadSettings().LastLibrary);
        Assert.Equal(pc.Place, pc.AppData.ReadSettings().LastPlace);
        // Started a second time, it does nothing: what is open stays open, and nothing
        // is asked or looked for again.
        again.ChooseLibrary(pc.Library("Kids"));
        var asked = pc.TimesAsked;
        var logged = pc.Log;
        await again.StartAsync();
        Assert.Equal("Kids", again.Library?.Name);
        Assert.Equal(asked, pc.TimesAsked);
        Assert.Equal(logged, pc.Log);
    });

    [Fact]
    public void ALibraryOfOneSongSaysOneSong() => OnAMadeUpPC(async pc =>
    {
        File.WriteAllBytes(Path.Combine(pc.Place, "Libraries", "Kids", "Only.pro"), TestData.InFiles("made-by-simple-presenter/Plain Song.pro"));
        var model = pc.Model();
        await model.StartAsync();
        Assert.Equal("1 song, 1 with chords.", model.Notice);
        Assert.Equal("1 SONG", model.SongsCount);
    });

    [Fact]
    public void AClickOnASongOpensItToBeLookedAtAndOneThatCannotBeReadSaysSo() => OnAMadeUpPC(async pc =>
    {
        var model = pc.Model();
        await model.StartAsync();
        model.ChooseLibrary(pc.Library("Songs"));

        model.Open(pc.Song("Plain Song"));
        var shown = model.Shown!;
        Assert.Equal(("Plain Song", "G", "G"), (shown.Name, shown.Key, shown.ShownKey));
        Assert.Null(shown.Problem);
        Assert.Equal((4, 11), (shown.Slides, shown.Chords));
        Assert.Equal("{c: Verse 1}", shown.Lines[0].Text);
        Assert.Equal("[G] [D/F#] [Em]", shown.Lines[^1].Text);
        // A song opens on the chord sheet, where the quiet line says how much there is of it;
        // over the ChordPro text it says what is there to be seen.
        Assert.Equal(LibraryModel.Editor.Sheet, model.EditorShown);
        Assert.Matches("^[0-9]+ slides?, [0-9]+ chords?[.]$", model.Notice);
        model.Show(LibraryModel.Editor.ChordPro);
        Assert.Equal("4 slides, 11 chords. A change is saved a moment after it is typed.", model.Notice);
        Assert.Equal("", model.NoSongSentence);
        Assert.Equal(pc.Song("Plain Song"), model.SongId);

        // A song that states no key, and one whose key ProPresenter is set to show otherwise.
        model.Open(pc.Song("Holy Holy Holy"));
        Assert.Equal(("", ""), (model.Shown!.Key, model.Shown.ShownKey));
        model.Open(pc.Song("A Song - In A Minor Key (Chords)", "Old copies"));
        Assert.Null(model.Shown);

        model.Open(pc.Song("Nonsense"));
        var broken = model.Shown!;
        Assert.Empty(broken.Lines);
        Assert.Equal("Nonsense.pro could not be read as a ProPresenter presentation: it is not a presentation, or it is damaged.", broken.Problem);
        Assert.Equal("Nothing was done to this file.", model.Notice);
        // Nothing was: it is the bytes it was.
        Assert.Equal("This is not a song.", File.ReadAllText(Path.Combine(pc.Place, "Libraries", "Songs", "Nonsense.pro")));

        // Another library, and the song that was open is open no more.
        model.ChooseLibrary(pc.Library("Old copies"));
        Assert.Null(model.Shown);
        Assert.Null(model.SongId);
        await model.MarksAreWorkedOutAsync();
        Assert.Equal(4, model.Songs.Count);
        Assert.All(model.Songs, song => Assert.True(model.Marks[song.Id].IsOldEditorsCopy));
        // The same library chosen again changes nothing.
        model.Open(model.Songs[0].Id);
        model.ChooseLibrary(pc.Library("Old copies"));
        Assert.NotNull(model.Shown);
    });

    [Fact]
    public void ASongOfOneSlideAndOneChordIsSaidSo() => OnAMadeUpPC(async pc =>
    {
        // Made from Plain Song: its last slide alone, with one chord left on it.
        var path = Path.Combine(pc.Place, "Libraries", "Kids", "One.pro");
        File.WriteAllBytes(path, TestData.InFiles("made-by-simple-presenter/Plain Song.pro"));
        var presentation = Pro.SerializationInterop.RVProtoData.Presentation.Parser.ParseFrom(File.ReadAllBytes(path));
        var last = presentation.CueGroups[^1].CueIdentifiers[^1].String;
        foreach (var cue in presentation.Cues.Where(cue => cue.Uuid.String != last))
        {
            cue.IsEnabled = false;
        }
        var text = presentation.Cues.Single(cue => cue.Uuid.String == last).Actions.Single(action => action.Slide?.Presentation is not null)
            .Slide.Presentation.BaseSlide.Elements.Single(element => element.Element_?.Text is not null).Element_.Text;
        var chords = text.Attributes.CustomAttributes.Where(entry => entry.Chord.Length > 0).Skip(1).ToList();
        foreach (var chord in chords)
        {
            text.Attributes.CustomAttributes.Remove(chord);
        }
        File.WriteAllBytes(path, Google.Protobuf.MessageExtensions.ToByteArray(presentation));

        var model = pc.Model();
        await model.StartAsync();
        model.Open(model.Songs.Single().Id);
        Assert.Equal((1, 1), (model.Shown!.Slides, model.Shown.Chords));
        model.Show(LibraryModel.Editor.ChordPro);
        Assert.Equal("1 slide, 1 chord. A change is saved a moment after it is typed.", model.Notice);
        Assert.Equal("Has 1 chord.", model.Rows.Single().ChordsTip);
    });

    [Fact]
    public void WhatIsShownOfASongIsItsOwnKeyTheKeyItIsShownInItsSlidesAndItsChords() => OnAMadeUpPC(async pc =>
    {
        // Two songs made from Plain Song, each with what no test file has: one that
        // ProPresenter is set to show in another key than its own, and one with a slide
        // that has two text boxes, so that it has more boxes than slides.
        var kids = Path.Combine(pc.Place, "Libraries", "Kids");
        var shown = Path.Combine(kids, "Shown Otherwise.pro");
        File.WriteAllBytes(shown, TestData.InFiles("made-by-simple-presenter/Plain Song.pro"));
        new OpenSong(shown, new Safeguards(() => false, (_, _) => { })).SetShownKey("A");

        var presentation = Pro.SerializationInterop.RVProtoData.Presentation.Parser.ParseFrom(TestData.InFiles("made-by-simple-presenter/Plain Song.pro"));
        var first = presentation.CueGroups[0].CueIdentifiers[0].String;
        var elements = presentation.Cues.Single(cue => cue.Uuid.String == first).Actions.Single(action => action.Slide?.Presentation is not null).Slide.Presentation.BaseSlide.Elements;
        var second = elements.Single(element => element.Element_?.Text is not null).Clone();
        second.Element_.Uuid.String = "11111111-2222-3333-4444-555555555555";
        elements.Add(second);
        File.WriteAllBytes(Path.Combine(kids, "Two Boxes.pro"), Google.Protobuf.MessageExtensions.ToByteArray(presentation));

        var model = pc.Model();
        await model.StartAsync();
        model.Open(pc.Song("Shown Otherwise", "Kids"));
        Assert.Equal(("G", "A"), (model.Shown!.Key, model.Shown.ShownKey));
        model.Open(pc.Song("Two Boxes", "Kids"));
        // Four slides, five boxes; and the first box's three chords twice over.
        Assert.Equal((4, 14), (model.Shown!.Slides, model.Shown.Chords));
        Assert.Equal(14, model.Marks[pc.Song("Two Boxes", "Kids")].Chords);
        model.Show(LibraryModel.Editor.ChordPro);
        Assert.Equal("4 slides, 14 chords. A change is saved a moment after it is typed.", model.Notice);
    });

    [Fact]
    public void TheListIsNarrowedAsTheSearchIsTypedAndByMark() => OnAMadeUpPC(async pc =>
    {
        var model = pc.Model();
        await model.StartAsync();
        model.ChooseLibrary(pc.Library("Songs"));
        await model.MarksAreWorkedOutAsync();

        model.Typed = "ho";
        Assert.Equal(["Chords With No Words", "Holy Holy Holy"], Names(model.ListedSongs));
        Assert.Equal("2 OF 9 SONGS", model.SongsCount);
        model.Typed = "a";
        model.Filter = MarkFilter.WithoutChords;
        Assert.Equal(["Rock Of Ages (Toplady)"], Names(model.ListedSongs));
        model.Typed = "";
        model.Filter = MarkFilter.WithChords;
        Assert.Equal(MadeUpPC.SongsWithChords, Names(model.ListedSongs));
        model.Filter = MarkFilter.All;
        Assert.Equal(MadeUpPC.Songs, Names(model.ListedSongs));
        Assert.Equal("9 SONGS", model.SongsCount);
        // The whole library is said of on the quiet line, however the list is narrowed.
        model.Typed = "zzz";
        Assert.Empty(model.ListedSongs);
        Assert.Equal("0 OF 9 SONGS", model.SongsCount);
        Assert.Equal("9 songs, 7 with chords.", model.Notice);
    });

    [Fact]
    public void TheSongThatIsOpenStaysInTheListWhateverItIsNarrowedTo() => OnAMadeUpPC(async pc =>
    {
        var model = pc.Model();
        await model.StartAsync();
        model.ChooseLibrary(pc.Library("Songs"));
        await model.MarksAreWorkedOutAsync();
        model.Open(pc.Song("Plain Song"));

        model.Filter = MarkFilter.WithoutChords;
        Assert.Equal(["Plain Song", "Rock Of Ages (Toplady)"], Names(model.ListedSongs));
        model.Typed = "grace";
        model.Filter = MarkFilter.All;
        Assert.Equal(["Amazing Grace", "Plain Song"], Names(model.ListedSongs));
        Assert.Equal("2 OF 9 SONGS", model.SongsCount);
        // It is in its own place in the order, and listed once where the list has it anyway.
        model.Typed = "plain";
        Assert.Equal(["Plain Song"], Names(model.ListedSongs));
    });

    [Fact]
    public void ASongWhoseMarksAreNotWorkedOutYetIsListedOnlyUnderAll() => OnAMadeUpPC(async pc =>
    {
        var model = pc.Model();
        await model.StartAsync();
        model.ChooseLibrary(pc.Library("Songs"));
        // At once, before any file has been read: every name, no marks.
        Assert.Empty(model.Marks);
        Assert.Equal(MadeUpPC.Songs, Names(model.ListedSongs));
        Assert.All(model.Rows, row => Assert.True(row.Marks is null && !row.HasChords && !row.CannotBeRead && row.ChordsTip.Length == 0));
        model.Filter = MarkFilter.WithChords;
        Assert.Empty(model.ListedSongs);
        model.Filter = MarkFilter.WithoutChords;
        Assert.Empty(model.ListedSongs);
        Assert.Equal("9 songs, 0 with chords.", model.Notice);

        await model.MarksAreWorkedOutAsync();
        Assert.Equal(["Rock Of Ages (Toplady)"], Names(model.ListedSongs));
        model.Filter = MarkFilter.WithChords;
        Assert.Equal(MadeUpPC.SongsWithChords, Names(model.ListedSongs));
    });

    [Fact]
    public void WhatARowSaysOfASongInWords() => OnAMadeUpPC(async pc =>
    {
        var model = pc.Model();
        await model.StartAsync();
        model.ChooseLibrary(pc.Library("Songs"));
        await model.MarksAreWorkedOutAsync();
        LibraryModel.SongRow Row(string name) => model.Rows.Single(row => row.Name == name);

        Assert.Equal(MadeUpPC.Songs, model.Rows.Select(row => row.Name));
        Assert.Equal(model.ListedSongs.Select(song => song.Id), model.Rows.Select(row => row.Id));
        Assert.Equal("Has 40 chords.", Row("Amazing Grace").ChordsTip);
        Assert.True(Row("Amazing Grace") is { HasChords: true, CannotBeRead: false, IsOldEditorsCopy: false });
        Assert.True(Row("Rock Of Ages (Toplady)") is { HasChords: false, CannotBeRead: false, IsOldEditorsCopy: false });
        Assert.True(Row("Nonsense") is { HasChords: false, CannotBeRead: true });
        Assert.Equal("This file could not be read as a ProPresenter presentation.", LibraryModel.SongRow.CannotBeReadTip);
        Assert.Equal(
            "A copy saved by the first Windows chord editor, which named its copies " + (char)0x201C + (char)0x2026 + " (Chords)" + (char)0x201D + ".",
            LibraryModel.SongRow.OldEditorsCopyTip);

        model.ChooseLibrary(pc.Library("Old copies"));
        await model.MarksAreWorkedOutAsync();
        Assert.All(model.Rows, row => Assert.True(row.IsOldEditorsCopy && row.HasChords));
    });

    [Fact]
    public void WhenTheAppComesToTheFrontTheListsAreReadAgainAndProPresenterIsAskedAfter() => OnAMadeUpPC(async pc =>
    {
        var model = pc.Model();
        // (Before it has started, coming to the front does nothing.)
        await model.RefreshAsync();
        Assert.Equal(0, pc.TimesAsked);
        await model.StartAsync();
        model.ChooseLibrary(pc.Library("Songs"));
        model.Open(pc.Song("Plain Song"));
        await model.MarksAreWorkedOutAsync();
        Assert.False(model.ProPresenterIsRunning);
        var shown = model.Shown;

        // Meanwhile ProPresenter was started, and a song added.
        pc.ProPresenterIsRunning = true;
        File.WriteAllBytes(Path.Combine(pc.Place, "Libraries", "Songs", "Another Song.pro"), TestData.InFiles("made-by-simple-presenter/It Is Well.pro"));
        await model.RefreshAsync();
        Assert.True(model.ProPresenterIsRunning);
        Assert.Equal(10, model.Songs.Count);
        Assert.Contains("Another Song", Names(model.Songs));
        Assert.True(model.Marks[pc.Song("Another Song")].HasChords);
        // What was open is open still, and is the very song that was read: its file is
        // as it was, so it was not read again.
        Assert.Equal("Songs", model.Library?.Name);
        Assert.Same(shown, model.Shown);
        Assert.Equal(pc.Song("Plain Song"), model.SongId);

        pc.ProPresenterIsRunning = false;
        await model.RefreshAsync();
        Assert.False(model.ProPresenterIsRunning);

        // Something else changes the file of the song that is open: it is read again.
        File.WriteAllBytes(Path.Combine(pc.Place, "Libraries", "Songs", "Plain Song.pro"), TestData.InFiles("made-by-simple-presenter/Amazing Grace.pro"));
        await model.RefreshAsync();
        Assert.NotSame(shown, model.Shown);
        Assert.Equal(("Plain Song", 40), (model.Shown!.Name, model.Shown.Chords));
        Assert.Equal(40, model.Marks[pc.Song("Plain Song")].Chords);

        // And one that was open is taken away: it is open no more.
        File.Delete(Path.Combine(pc.Place, "Libraries", "Songs", "Plain Song.pro"));
        await model.RefreshAsync();
        Assert.Null(model.Shown);
        Assert.Null(model.SongId);
        Assert.Equal(9, model.Songs.Count);
    });

    [Fact]
    public void WhetherProPresenterIsRunningIsAskedAgainAsTimeGoesByAndTheModelSaysWhenItHasChanged() => OnAMadeUpPC(async pc =>
    {
        var model = pc.Model();
        await model.StartAsync();
        var changes = 0;
        model.PropertyChanged += (_, _) => changes++;

        // Asked, and nothing has changed: nothing is said.
        model.ProPresenterMayHaveChanged();
        await model.LookForProPresenterAsync();
        Assert.Equal(0, changes);
        Assert.Equal(3, pc.TimesAsked);

        pc.ProPresenterIsRunning = true;
        model.ProPresenterMayHaveChanged();
        Assert.True(model.ProPresenterIsRunning);
        Assert.Equal(1, changes);

        pc.ProPresenterIsRunning = false;
        await model.LookForProPresenterAsync();
        Assert.False(model.ProPresenterIsRunning);
        Assert.Equal(2, changes);
    });

    [Fact]
    public void WithNoPlaceFoundThereIsNothingListedAndAFolderChosenByHandBecomesOneAndIsRemembered() => OnAMadeUpPC(async pc =>
    {
        pc.HasAProPresenterFolder = false;
        var model = pc.Model();
        await model.StartAsync();
        Assert.Empty(model.Places);
        Assert.Empty(model.Libraries);
        Assert.Empty(model.Songs);
        Assert.Null(model.PlaceId);
        Assert.Null(model.LibraryId);
        Assert.Equal("", model.Notice);
        Assert.Equal("No ProPresenter library was found on this PC.", model.NoPlaceSentence);
        Assert.Equal("", model.NoLibrariesSentence + model.NoSongSentence + model.SongsCount);

        // One library of songs, chosen by its own folder.
        model.Add(Path.Combine(pc.Place, "Libraries", "Songs"));
        await model.MarksAreWorkedOutAsync();
        Assert.Equal([PlaceKind.Chosen], model.Places.Select(place => place.Kind));
        Assert.Equal(["Songs"], model.Libraries.Select(library => library.Name));
        Assert.Equal(9, model.Songs.Count);
        Assert.Equal("", model.NoPlaceSentence);
        Assert.Equal("Songs (chosen folder)", LibraryModel.Describe(model.Place!));

        // Chosen again, it is one place still, and is kept once. Another is another, and
        // is the one open.
        model.Add(Path.Combine(pc.Place, "Libraries", "Songs") + Path.DirectorySeparatorChar);
        Assert.Single(model.Places);
        Assert.Single(pc.AppData.ReadSettings().ChosenFolders);
        var empty = Directory.CreateDirectory(Path.Combine(pc.Folder, "Nothing in it")).FullName;
        model.Add(empty);
        Assert.Equal(["Songs", "Nothing in it"], model.Places.Select(place => place.Name));
        Assert.Equal("Nothing in it", model.Place?.Name);
        Assert.Equal("This folder has no libraries in it.", model.NoLibrariesSentence);
        Assert.Equal("", model.Notice + model.NoSongSentence);

        var again = pc.Model();
        await again.StartAsync();
        Assert.Equal(["Songs", "Nothing in it"], again.Places.Select(place => place.Name));
        Assert.Equal("Nothing in it", again.Place?.Name);
        again.ChoosePlace(again.Places[0].Id);
        Assert.Equal(9, again.Songs.Count);
        // And the place last open is the one open at the next start.
        var third = pc.Model();
        await third.StartAsync();
        Assert.Equal("Songs", third.Place?.Name);
    });

    [Fact]
    public void APlaceIsCalledByItsNameAndHowItWasFound()
    {
        Assert.Equal("ProPresenter in Documents", LibraryModel.Describe(new Place(PlaceKind.Documents, "Documents", "C:/Users/someone/Documents/ProPresenter")));
        Assert.Equal("Sunday (ProPresenter" + (char)0x2019 + "s folder)", LibraryModel.Describe(new Place(PlaceKind.NamedInSettings, "Sunday", "D:/Sunday")));
        Assert.Equal("Songs (chosen folder)", LibraryModel.Describe(new Place(PlaceKind.Chosen, "Songs", "D:/Songs")));
    }

    [Fact]
    public void WhereTheBarBetweenTheListsWasLeftIsRememberedWithinBounds() => OnAMadeUpPC(async pc =>
    {
        var model = pc.Model();
        Assert.Equal(LibraryModel.UsualListsSplit, model.ListsSplit);
        // (Before the start nothing is kept: there is nothing yet to keep it with.)
        model.SetListsSplit(0.5);
        Assert.False(Directory.Exists(pc.AppData.Folder));
        await model.StartAsync();
        Assert.Equal(LibraryModel.UsualListsSplit, model.ListsSplit);

        model.SetListsSplit(0.5);
        Assert.Equal(0.5, model.ListsSplit);
        Assert.Equal(0.5, pc.AppData.ReadSettings().ListsSplit);
        var again = pc.Model();
        await again.StartAsync();
        Assert.Equal(0.5, again.ListsSplit);

        // Never so far that either list is dragged away altogether.
        model.SetListsSplit(0.01);
        Assert.Equal(0.1, model.ListsSplit);
        model.SetListsSplit(7);
        Assert.Equal(0.9, model.ListsSplit);
        model.SetListsSplit(double.NaN);
        Assert.Equal(LibraryModel.UsualListsSplit, model.ListsSplit);
        // Put back where it usually is, nothing is kept of it.
        model.SetListsSplit(0.7);
        model.SetListsSplit(null);
        Assert.Equal(LibraryModel.UsualListsSplit, model.ListsSplit);
        Assert.Null(pc.AppData.ReadSettings().ListsSplit);
        // And what is kept beside it is kept still.
        Assert.Equal(pc.Place, pc.AppData.ReadSettings().LastPlace);
    });

    [Fact]
    public void TheModelSaysWhenWhatItShowsHasChanged() => OnAMadeUpPC(async pc =>
    {
        var model = pc.Model();
        var changes = 0;
        model.PropertyChanged += (_, _) => changes++;
        void Changes(Action what)
        {
            var before = changes;
            what();
            Assert.True(changes > before);
        }

        await model.StartAsync();
        Assert.True(changes > 0);
        Changes(() => model.ChooseLibrary(pc.Library("Songs")));
        var before = changes;
        await model.MarksAreWorkedOutAsync();
        Assert.True(changes > before);
        Changes(() => model.Open(pc.Song("Plain Song")));
        Changes(() => model.Typed = "a");
        Changes(() => model.Filter = MarkFilter.WithChords);
        Changes(() => model.SetListsSplit(0.4));
        Changes(() => model.Add(Path.Combine(pc.Place, "Libraries", "Old copies")));
        Changes(() => model.ChoosePlace(model.Places[0].Id));
        before = changes;
        await model.RefreshAsync();
        Assert.True(changes > before);
        // What is set to what it already is, is no change.
        before = changes;
        model.Typed = "a";
        model.Filter = MarkFilter.WithChords;
        model.ChoosePlace(model.PlaceId);
        model.ChooseLibrary(model.LibraryId);
        Assert.Equal(before, changes);
    });

    [Fact]
    public void TheLogNamesFilesAndNeverHasAWordOfASongInIt() => OnAMadeUpPC(async pc =>
    {
        var model = pc.Model();
        await model.StartAsync();
        model.ChooseLibrary(pc.Library("Songs"));
        model.Open(pc.Song("Amazing Grace"));
        model.Open(pc.Song("Nonsense"));
        model.Add(Path.Combine(pc.Place, "Libraries", "Old copies"));
        pc.ProPresenterIsRunning = true;
        model.ProPresenterMayHaveChanged();
        pc.ProPresenterIsRunning = false;
        model.ProPresenterMayHaveChanged();
        await model.MarksAreWorkedOutAsync();

        var log = pc.Log;
        Assert.Contains("  Started. 1 places found, 3 libraries in the one open.", log, StringComparison.Ordinal);
        Assert.Contains("  Opened Amazing Grace.pro.", log, StringComparison.Ordinal);
        Assert.Contains("  Could not read Nonsense.pro.", log, StringComparison.Ordinal);
        Assert.Contains("  A folder was chosen: Old copies.", log, StringComparison.Ordinal);
        Assert.Contains("  ProPresenter is running.", log, StringComparison.Ordinal);
        Assert.Contains("  ProPresenter is no longer running.", log, StringComparison.Ordinal);
        // Not a line of the song, and not a chord of it.
        model.ChoosePlace(model.Places[0].Id);
        model.ChooseLibrary(pc.Library("Songs"));
        model.Open(pc.Song("Amazing Grace"));
        Assert.All(model.Shown!.Lines.Where(line => line.Words.Length > 6), line => Assert.DoesNotContain(line.Words, pc.Log, StringComparison.Ordinal));
        Assert.DoesNotContain("[G", pc.Log, StringComparison.Ordinal);
    });
}
