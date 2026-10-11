// Earlier versions in the window, without drawing anything: the mark, the panel's list,
// looking at a copy, putting one back and taking that back, and All Copies.
// spec/07-backups.md, rules 12 to 23.
//
// MINE, and held to the page; where the Mac's tests of its own model hold the same
// thing (EarlierVersionsTests.swift), these expect what those expect.

using System.IO;
using Pro7ChordEditor.ChordRules;
using Pro7ChordEditor.Song;
using Xunit;

namespace Pro7ChordEditor.AppViews.Tests;

public class EarlierVersionsTests
{
    private static readonly KeptSong PlainSong = new(MadeUpPC.PlaceName, "Songs", "Plain Song");

    // A made-up PC on which Plain Song has been changed twice: nine days ago its key was
    // set to E, and two days ago the three chords of its first slide were taken off,
    // leaving eight of its eleven.
    private sealed record History(MadeUpPC PC, byte[] Original, byte[] InE, byte[] Now);

    private static void WithHistory(Func<History, Task> what) => Pictures.OnAThreadOfItsOwn(async () =>
    {
        using var pc = new MadeUpPC();
        var original = pc.BytesOf("Plain Song");
        pc.GivePlainSongAHistory();
        var inE = File.ReadAllBytes(pc.AppData.Copies().CopiesOf(PlainSong)[0].Path);
        await what(new History(pc, original, inE, pc.BytesOf("Plain Song")));
    });

    private static async Task<LibraryModel> ModelOn(MadeUpPC pc, string song = "Plain Song")
    {
        var model = pc.Model();
        await model.StartAsync();
        model.ChooseLibrary(pc.Library("Songs"));
        await model.MarksAreWorkedOutAsync();
        model.Open(pc.Song(song));
        return model;
    }

    [Fact]
    public void ASongThatHasCopiesHasAMarkAndItsEarlierVersionsAreListedTheNewestFirstAndPicked() => WithHistory(async history =>
    {
        var pc = history.PC;
        var model = await ModelOn(pc);
        Assert.Equal(["Plain Song"], model.Rows.Where(row => row.HasCopies).Select(row => row.Name));
        Assert.Equal("Earlier versions of this song are kept.", LibraryModel.SongRow.HasCopiesTip);
        Assert.True(model.HasEarlierVersions);
        Assert.Equal(("E", 8), (model.Shown!.Key, model.Shown.Chords));

        model.ShowEarlierVersions();
        var panel = model.Earlier!;
        Assert.Equal(2, panel.Copies.Count);
        Assert.Equal([CopyReason.BeforeAChange, CopyReason.BeforeAChange], panel.Copies.Select(copy => copy.Why));
        Assert.Equal([false, true], panel.Copies.Select(copy => copy.IsTheFirst));
        Assert.Equal(["before a change", "(the first) before a change"], panel.Copies.Select(LibraryModel.Why));
        // The newest is picked and shown as it was: in E, with all its chords still.
        Assert.Equal(panel.Copies[0].Id, panel.PickedId);
        Assert.Equal(("E", 11), (panel.Picked!.Key, panel.Picked.Chords));
        Assert.Equal("11 chords, in E.  Now: 8 chords, in E.", panel.Comparison);

        model.PickCopy(panel.Copies[1].Id);
        Assert.Equal("G", model.Earlier!.Picked!.Key);
        Assert.Equal("11 chords, in G.  Now: 8 chords, in E.", model.Earlier.Comparison);
        Assert.Equal("{c: Verse 1}", model.Earlier.Picked.Lines[0].Text);
        Assert.Equal("Plain Song", model.Earlier.Picked.Name);
        // A copy that is not among them is not picked.
        model.PickCopy("2001-01-01 00.00.00.pro");
        Assert.Equal(panel.Copies[1].Id, model.Earlier!.PickedId);

        model.CloseEarlierVersions();
        Assert.Null(model.Earlier);
        // A song with no copies has no earlier versions to show.
        model.Open(pc.Song("Amazing Grace"));
        Assert.False(model.HasEarlierVersions);
        model.ShowEarlierVersions();
        Assert.Null(model.Earlier);
        // Nor has no song at all.
        model.ChooseLibrary(pc.Library("Kids"));
        Assert.False(model.HasEarlierVersions);
        model.ShowEarlierVersions();
        Assert.Null(model.Earlier);
    });

    [Fact]
    public void ASongThatStatesNoKeyIsSaidToHaveNoneInTheLineThatSetsACopyAgainstIt() => Pictures.OnAThreadOfItsOwn(async () =>
    {
        using var pc = new MadeUpPC();
        // Holy Holy Holy states no key. Yesterday the chords of its first slide were
        // taken off.
        pc.Change("Holy Holy Holy", 1, song => song.SetChords(song.TextBoxes[0].Slide, song.TextBoxes[0].Element, []));
        var model = await ModelOn(pc, "Holy Holy Holy");
        var now = model.Shown!.Chords;
        Assert.True(now < 22);

        model.ShowEarlierVersions();
        Assert.Equal("22 chords, no key.  Now: " + now + " chords, no key.", model.Earlier!.Comparison);
        Assert.Equal("", model.Earlier.Picked!.Key);
    });

    [Fact]
    public void WhatTheListSaysOfWhyACopyWasMade()
    {
        static KeptCopy Copy(CopyReason? why, bool first = false) => new("x", DateTime.UnixEpoch, why, "", "", "", 0, "", 0) { IsTheFirst = first };

        Assert.Equal("before a change", LibraryModel.Why(Copy(CopyReason.BeforeAChange)));
        Assert.Equal("before putting back a copy", LibraryModel.Why(Copy(CopyReason.BeforePuttingBack)));
        Assert.Equal("before mending", LibraryModel.Why(Copy(CopyReason.BeforeMending)));
        // Why is not known of a copy the index does not know.
        Assert.Equal("", LibraryModel.Why(Copy(null)));
        Assert.Equal("(the first) before mending", LibraryModel.Why(Copy(CopyReason.BeforeMending, first: true)));
        Assert.Equal("(the first) ", LibraryModel.Why(Copy(null, first: true)));
    }

    [Fact]
    public void PutBackTheSongsFileIsTheCopyAndTakenBackItIsWhatItWas() => WithHistory(async history =>
    {
        var pc = history.PC;
        var model = await ModelOn(pc);
        model.ShowEarlierVersions();
        var first = model.Earlier!.Copies[^1];
        model.PickCopy(first.Id);

        model.PutBackPicked();
        Assert.Equal(history.Original, pc.BytesOf("Plain Song"));
        Assert.Null(model.Earlier);
        Assert.Null(model.Refused);
        Assert.Equal(("G", 11), (model.Shown!.Key, model.Shown.Chords));
        Assert.Equal("Put back the version of " + model.When(first) + ".", model.JustPutBack!.Sentence);
        Assert.Equal(model.JustPutBack.Sentence, model.Notice);
        Assert.Contains("  Put back an earlier version of Plain Song.pro, the one of " + model.When(first) + ".", pc.Log, StringComparison.Ordinal);
        // Its mark in the list is the file's as it now is.
        Assert.Equal(11, model.Marks[pc.Song("Plain Song")].Chords);
        Assert.Equal("Has 11 chords.", model.Rows.Single(row => row.Name == "Plain Song").ChordsTip);
        // And is kept so for the next start, by the file as it now is.
        Assert.Equal(11, pc.AppData.ReadMarks().MarksFor(model.Songs.Single(song => song.Name == "Plain Song"))?.Chords);

        // The file as it was just before is the newest copy now, and says why.
        model.ShowEarlierVersions();
        var newest = model.Earlier!.Copies[0];
        Assert.Equal(3, model.Earlier.Copies.Count);
        Assert.Equal(CopyReason.BeforePuttingBack, newest.Why);
        Assert.Equal(history.Now, File.ReadAllBytes(newest.Path));
        // It is the one the offer to undo will put back.
        Assert.Equal(newest.Path, model.JustPutBack.Before.Path);
        model.CloseEarlierVersions();

        // Undo: only putting that copy back.
        model.TakeBack();
        Assert.Equal(history.Now, pc.BytesOf("Plain Song"));
        Assert.Equal(("E", 8), (model.Shown!.Key, model.Shown.Chords));
        Assert.Null(model.JustPutBack);
        // (Over the ChordPro text: on the sheet the line says what the keys do.)
        model.Show(LibraryModel.Editor.ChordPro);
        Assert.Equal("4 slides, 8 chords. A change is saved a moment after it is typed.", model.Notice);
        Assert.Contains("  Putting back was undone for Plain Song.pro.", pc.Log, StringComparison.Ordinal);
        Assert.Equal(8, model.Marks[pc.Song("Plain Song")].Chords);
        // A put back and its taking back left two copies more than there were.
        Assert.Equal(4, pc.AppData.Copies().CopiesOf(PlainSong).Count);
        // With nothing just put back, there is nothing to take back.
        model.TakeBack();
        Assert.Equal(history.Now, pc.BytesOf("Plain Song"));
        Assert.Equal(4, pc.AppData.Copies().CopiesOf(PlainSong).Count);
    });

    [Fact]
    public void TheOfferToUndoAndWhatWentWrongGoWithTheSongWhenItsLibraryIsLeft() => WithHistory(async history =>
    {
        var pc = history.PC;
        var model = await ModelOn(pc);
        model.ShowEarlierVersions();
        model.PutBackPicked();
        Assert.NotNull(model.JustPutBack);
        model.ChooseLibrary(pc.Library("Old copies"));
        Assert.Null(model.Shown);
        Assert.Null(model.JustPutBack);

        // And so does what went wrong: a putting back that was refused.
        model.ChooseLibrary(pc.Library("Songs"));
        model.Open(pc.Song("Plain Song"));
        model.ShowEarlierVersions();
        pc.ProPresenterIsRunning = true;
        model.PutBackPicked();
        Assert.NotNull(model.Refused);
        Assert.NotNull(model.Earlier);
        model.ChooseLibrary(pc.Library("Old copies"));
        Assert.Null(model.Refused);
        Assert.Null(model.Earlier);
        Assert.False(model.OffersOpeningAgain);
    });

    [Fact]
    public void TheOfferToUndoGoesAndTheCopyIsInTheListAllTheSame() => WithHistory(async history =>
    {
        var model = await ModelOn(history.PC);
        model.ShowEarlierVersions();
        model.PickCopy(model.Earlier!.Copies[^1].Id);
        model.PutBackPicked();
        Assert.NotNull(model.JustPutBack);
        // (Over the ChordPro text: on the sheet the line says what the keys do.)
        model.Show(LibraryModel.Editor.ChordPro);
        var changes = 0;
        model.PropertyChanged += (_, _) => changes++;

        model.ForgetWhatWasPutBack();
        Assert.Null(model.JustPutBack);
        Assert.Equal(1, changes);
        Assert.Equal("4 slides, 11 chords. A change is saved a moment after it is typed.", model.Notice);
        model.ForgetWhatWasPutBack();
        Assert.Equal(1, changes);
        model.ShowEarlierVersions();
        Assert.Equal(CopyReason.BeforePuttingBack, model.Earlier!.Copies[0].Why);
        // And another song opened forgets it too.
        model.CloseEarlierVersions();
        model.ShowEarlierVersions();
        model.PickCopy(model.Earlier!.Copies[0].Id);
        model.PutBackPicked();
        Assert.NotNull(model.JustPutBack);
        model.Open(history.PC.Song("Amazing Grace"));
        Assert.Null(model.JustPutBack);
    });

    [Fact]
    public void NothingIsPutBackWhileProPresenterIsRunningAndThePanelSaysWhy() => WithHistory(async history =>
    {
        var pc = history.PC;
        var model = await ModelOn(pc);
        model.ShowEarlierVersions();
        model.PickCopy(model.Earlier!.Copies[^1].Id);
        // It is started with this app in front all the while: nobody has told the model.
        pc.ProPresenterIsRunning = true;
        Assert.False(model.ProPresenterIsRunning);

        model.PutBackPicked();
        Assert.Equal("ProPresenter is running. Quit it to change chords here. Nothing was changed.", model.Refused);
        Assert.Equal(model.Refused, model.Notice);
        Assert.False(model.OffersOpeningAgain);
        Assert.Equal(history.Now, pc.BytesOf("Plain Song"));
        Assert.NotNull(model.Earlier);
        Assert.Null(model.JustPutBack);
        Assert.Equal(2, model.Earlier!.Copies.Count);
        Assert.Equal(2, pc.AppData.Copies().CopiesOf(PlainSong).Count);
        // The band is there from then (spec/06, rule 19).
        Assert.True(model.ProPresenterIsRunning);
        Assert.Contains("  An earlier version of Plain Song.pro was not put back.", pc.Log, StringComparison.Ordinal);
        Assert.Contains("  A change to Plain Song.pro was refused.", pc.Log, StringComparison.Ordinal);

        pc.ProPresenterIsRunning = false;
        model.PutBackPicked();
        Assert.Null(model.Refused);
        Assert.Null(model.Earlier);
        Assert.Equal("G", model.Shown!.Key);
    });

    [Fact]
    public void NothingIsPutBackOverAFileSomethingElseHasChangedAndTheSongIsOfferedReadAgain() => WithHistory(async history =>
    {
        var pc = history.PC;
        var model = await ModelOn(pc);
        model.ShowEarlierVersions();
        model.PickCopy(model.Earlier!.Copies[^1].Id);
        // Something else changes the song's file while the panel is open.
        File.WriteAllBytes(pc.Song("Plain Song"), history.InE);

        model.PutBackPicked();
        Assert.Equal(
            "This song's file has been changed by something else since it was opened here. Nothing was changed. Open the song again to see it as it is now.",
            model.Refused);
        Assert.True(model.OffersOpeningAgain);
        Assert.Equal(history.InE, pc.BytesOf("Plain Song"));

        // Opened again, it is the file as it now is, and what went wrong is said no more.
        model.OpenAgain();
        Assert.Null(model.Refused);
        Assert.False(model.OffersOpeningAgain);
        Assert.Null(model.Earlier);
        Assert.Equal(("E", 11), (model.Shown!.Key, model.Shown.Chords));
    });

    [Fact]
    public void WhenTheAppComesToTheFrontWhileThePanelIsOpenTheSongIsNotReadAgainUnderIt() => WithHistory(async history =>
    {
        var pc = history.PC;
        var model = await ModelOn(pc);
        model.ShowEarlierVersions();
        var shown = model.Shown;
        File.WriteAllBytes(pc.Song("Plain Song"), history.InE);

        await model.RefreshAsync();
        Assert.NotNull(model.Earlier);
        Assert.Same(shown, model.Shown);
        // With the panel put away, it is.
        model.CloseEarlierVersions();
        await model.RefreshAsync();
        Assert.NotSame(shown, model.Shown);
        Assert.Equal(11, model.Shown!.Chords);
    });

    [Fact]
    public void ACopyOfAnotherSongThanTheOneNowThereAsksFirstWithBothNames() => WithHistory(async history =>
    {
        var pc = history.PC;
        // Among Plain Song's copies is one of a file that was Amazing Grace: another
        // song once stood at that place under that name.
        var grace = pc.BytesOf("Amazing Grace");
        var elsewhere = Path.Combine(pc.Folder, "Was there once.pro");
        File.WriteAllBytes(elsewhere, grace);
        pc.AppData.Copies().Keep(elsewhere, PlainSong, CopyReason.BeforeAChange, DateTime.UtcNow.AddDays(-1));
        var model = await ModelOn(pc);
        model.ShowEarlierVersions();
        var stranger = model.Earlier!.Copies.Single(copy => copy.SongName == "Amazing Grace");
        model.PickCopy(stranger.Id);

        model.PutBackPicked();
        Assert.Equal(
            "This copy is of " + (char)0x201C + "Amazing Grace" + (char)0x201D + ", and the song here now is " + (char)0x201C + "Plain Song" + (char)0x201D
            + ": another song under the same name. Put the copy in its place?",
            model.Earlier!.Question);
        Assert.Equal(history.Now, pc.BytesOf("Plain Song"));
        // Picking again is leaving it.
        model.PickCopy(stranger.Id);
        Assert.Null(model.Earlier!.Question);
        Assert.Equal(history.Now, pc.BytesOf("Plain Song"));

        model.PutBackPicked();
        model.PutBackPicked(sure: true);
        Assert.Equal(grace, pc.BytesOf("Plain Song"));
        Assert.Equal(40, model.Shown!.Chords);
        Assert.Null(model.Earlier);
        // The song there now is Amazing Grace, inside. So a copy of Plain Song is now
        // another song's, and asks, with the names the other way round.
        model.ShowEarlierVersions();
        model.PickCopy(model.Earlier!.Copies.First(copy => copy.SongName == "Plain Song").Id);
        model.PutBackPicked();
        Assert.Equal(
            "This copy is of " + (char)0x201C + "Plain Song" + (char)0x201D + ", and the song here now is " + (char)0x201C + "Amazing Grace" + (char)0x201D
            + ": another song under the same name. Put the copy in its place?",
            model.Earlier!.Question);
        // (And nothing was put back for the asking.)
        Assert.Equal(grace, pc.BytesOf("Plain Song"));
    });

    [Fact]
    public void AQuestionAnsweredYesAndThenRefusedIsAskedNoMoreAndWhatWentWrongIsSaid() => WithHistory(async history =>
    {
        var pc = history.PC;
        var elsewhere = Path.Combine(pc.Folder, "Was there once.pro");
        File.WriteAllBytes(elsewhere, pc.BytesOf("Amazing Grace"));
        pc.AppData.Copies().Keep(elsewhere, PlainSong, CopyReason.BeforeAChange, DateTime.UtcNow.AddDays(-1));
        var model = await ModelOn(pc);
        model.ShowEarlierVersions();
        model.PickCopy(model.Earlier!.Copies.Single(copy => copy.SongName == "Amazing Grace").Id);
        model.PutBackPicked();
        Assert.NotNull(model.Earlier!.Question);

        // Yes, it says; and ProPresenter has been started meanwhile.
        pc.ProPresenterIsRunning = true;
        model.PutBackPicked(sure: true);
        Assert.Equal("ProPresenter is running. Quit it to change chords here. Nothing was changed.", model.Refused);
        Assert.NotNull(model.Earlier);
        Assert.Null(model.Earlier!.Question);
        Assert.Equal(history.Now, pc.BytesOf("Plain Song"));
    });

    [Fact]
    public void PuttingBackWhatTheFileAlreadyIsIsNothingToDoAndOffersNothingToUndo() => WithHistory(async history =>
    {
        var pc = history.PC;
        var model = await ModelOn(pc);
        model.ShowEarlierVersions();
        model.PickCopy(model.Earlier!.Copies[^1].Id);
        model.PutBackPicked();
        model.ForgetWhatWasPutBack();
        Assert.Equal(3, pc.AppData.Copies().CopiesOf(PlainSong).Count);

        // The first copy is what the file now is. Put back again:
        model.ShowEarlierVersions();
        model.PickCopy(model.Earlier!.Copies[^1].Id);
        model.PutBackPicked();
        Assert.Null(model.Earlier);
        Assert.Null(model.Refused);
        // No copy was made first, and nothing is offered to be undone: "Undo" would
        // otherwise put back whatever copy happened to be the newest.
        Assert.Null(model.JustPutBack);
        Assert.Equal(3, pc.AppData.Copies().CopiesOf(PlainSong).Count);
        Assert.Equal(history.Original, pc.BytesOf("Plain Song"));
    });

    [Fact]
    public void ACopyThatCannotBeReadIsShownAsSuchAndIsNotPutBack() => WithHistory(async history =>
    {
        var pc = history.PC;
        // A copy whose file is no song: named as a copy, and not in the index.
        var home = pc.AppData.Copies().FolderOf(PlainSong);
        File.WriteAllText(Path.Combine(home, "2001-01-01 00.00.00.pro"), "not a song");
        var model = await ModelOn(pc);
        model.ShowEarlierVersions();
        var broken = model.Earlier!.Copies[^1];
        model.PickCopy(broken.Id);
        Assert.Equal("This copy could not be read as a ProPresenter presentation.", model.Earlier!.Picked!.Problem);
        Assert.Empty(model.Earlier.Picked.Lines);

        model.PutBackPicked();
        Assert.Equal("That copy could not be read as a ProPresenter presentation: it is not a presentation, or it is damaged.", model.Refused);
        Assert.Equal(history.Now, pc.BytesOf("Plain Song"));
        Assert.NotNull(model.Earlier);
        // And one whose file has gone since the panel was opened.
        model.PickCopy(model.Earlier!.Copies[0].Id);
        File.Delete(model.Earlier.Copies[0].Path);
        model.PutBackPicked();
        Assert.Equal("The copy could not be read.", model.Refused);
        Assert.Equal(history.Now, pc.BytesOf("Plain Song"));
    });

    [Fact]
    public void WhenTheAppStartsCopiesThatAreKeptNoLongerAreThrownAwayAndNeverTheFirst() => Pictures.OnAThreadOfItsOwn(async () =>
    {
        using var pc = new MadeUpPC();
        // Twenty-five changes of Plain Song, each a copy, from 300 days ago to 60.
        var keys = Chords.MajorKeys().Concat(Chords.MinorKeys()).ToList();
        for (var n = 0; n < 25; n++)
        {
            var key = keys[n];
            pc.Change("Plain Song", 300 - (10.0 * n), song => song.SetKey(key));
        }
        var copies = pc.AppData.Copies();
        var first = copies.CopiesOf(PlainSong)[^1];
        Assert.Equal(25, copies.CopiesOf(PlainSong).Count);

        var model = pc.Model();
        await model.StartAsync();
        var left = copies.CopiesOf(PlainSong);
        Assert.Equal(21, left.Count);
        Assert.Equal(first.Id, left[^1].Id);
        Assert.Contains("  4 old copies were thrown away.", pc.Log, StringComparison.Ordinal);
        // Another start, and nothing is said of what did not happen.
        File.Delete(Path.Combine(pc.AppData.Folder, "log.txt"));
        await pc.Model().StartAsync();
        Assert.DoesNotContain("thrown away", pc.Log, StringComparison.Ordinal);
    });

    [Fact]
    public void TheModelsCopiesSayWhichVersionOfTheAppMadeThem() => Pictures.OnAThreadOfItsOwn(async () =>
    {
        using var pc = new MadeUpPC();
        pc.Change("Plain Song", 1, song => song.SetKey("E"));
        var model = new LibraryModel(pc.AppData, chosen => new PlaceFinder(() => null, pc.Documents, chosen), () => false, "9.8.7");
        await model.StartAsync();
        model.ChooseLibrary(pc.Library("Songs"));
        model.Open(pc.Song("Plain Song"));
        model.ShowEarlierVersions();
        model.PutBackPicked();
        var index = File.ReadAllText(Path.Combine(pc.AppData.Copies().FolderOf(PlainSong), "index.json"));
        Assert.Contains("\"appVersion\": \"9.8.7\"", index, StringComparison.Ordinal);
        Assert.Contains("\"appVersion\": \"test\"", index, StringComparison.Ordinal);
    });

    // ---- All Copies

    [Fact]
    public void AllCopiesEverySongThatHasCopiesWhetherItsSongIsStillThereACopyLookedAtAndSavedAsAFile() => WithHistory(history =>
    {
        var pc = history.PC;
        // Five days ago Holy Holy Holy was a file somewhere else, which is there still;
        // yesterday it was in the library, where it no longer is.
        var elsewhere = Path.Combine(pc.Folder, "Elsewhere.pro");
        File.Copy(pc.Song("Holy Holy Holy"), elsewhere);
        new OpenSong(elsewhere, new Safeguards(() => false, (_, _) => { })).SetKey("C");
        Assert.NotNull(pc.AppData.Copies().Keep(elsewhere, new KeptSong(MadeUpPC.PlaceName, "Songs", "Holy Holy Holy"), CopyReason.BeforeAChange, DateTime.UtcNow.AddDays(-5)));
        pc.Change("Holy Holy Holy", 1, song => song.SetKey("D"));
        pc.Change("Amazing Grace (Chords)", 3, song => song.SetKey("A"), "Old copies");
        File.Delete(pc.Song("Holy Holy Holy"));

        var model = new AllCopiesModel(pc.AppData.Copies());
        Assert.Empty(model.Rows);
        Assert.Equal("", model.Notice);
        model.Read();
        Assert.Equal(["Documents/Old copies/Amazing Grace (Chords)", "Documents/Songs/Holy Holy Holy", "Documents/Songs/Plain Song"], model.Rows.Select(row => row.Id));
        // (Where a song was is what its NEWEST copy says.)
        Assert.Equal([true, false, true], model.Rows.Select(row => row.SongIsStillThere));
        Assert.Equal([1, 2, 2], model.Rows.Select(row => row.Copies.Count));
        Assert.Equal(["Old copies, in Documents", "Songs, in Documents. The song is no longer there.", "Songs, in Documents"], model.Rows.Select(row => row.Where));
        Assert.Equal("The copies take " + model.Size + ".", model.Notice);
        Assert.Equal(AllCopiesModel.InWords(model.Rows.Sum(row => row.Copies.Sum(copy => copy.Size))), model.Size);
        Assert.Equal(pc.AppData.Copies().Folder, model.Folder);
        // The first song is picked, and its newest copy shown.
        Assert.Equal("Amazing Grace (Chords)", model.PickedRow?.Song.Name);
        Assert.Equal("G", model.Looked?.Key);

        // A song picked has its newest copy picked with it.
        model.PickSong("Documents/Songs/Plain Song");
        Assert.Equal(model.PickedRow!.Copies[0].Id, model.PickedCopy);
        Assert.Equal(("E", 11), (model.Looked!.Key, model.Looked.Chords));
        model.PickCopy(model.PickedRow!.Copies[^1].Id);
        Assert.Equal(("G", 11), (model.Looked!.Key, model.Looked.Chords));
        Assert.Equal("Plain Song (" + model.When(model.Copy!) + ").pro", model.NameToSaveAs(model.Copy!));
        var saved = Path.Combine(pc.Folder, "Saved.pro");
        Assert.Null(model.SavePicked(saved));
        Assert.Equal(history.Original, File.ReadAllBytes(saved));
        Assert.Equal(["Documents", "Elsewhere.pro", "Own", "Saved.pro"], Directory.EnumerateFileSystemEntries(pc.Folder).Select(entry => Path.GetFileName(entry)).Order(StringComparer.Ordinal));
        // Saved again over what is there, it is that copy again.
        model.PickCopy(model.PickedRow.Copies[0].Id);
        Assert.Null(model.SavePicked(saved));
        Assert.Equal(history.InE, File.ReadAllBytes(saved));
        // Where it cannot be saved is said, at the foot too, until the next thing is done.
        var problem = model.SavePicked(Path.Combine(pc.Folder, "No such folder", "Saved.pro"));
        Assert.StartsWith("The copy could not be saved there: ", problem, StringComparison.Ordinal);
        Assert.Equal(problem, model.Notice);
        Assert.Null(model.SavePicked(saved));
        Assert.Equal("The copies take " + model.Size + ".", model.Notice);
        // Where a folder is in the way, nothing is left beside it of what was being
        // written; and what went wrong is said no longer once the copies are read again.
        var inTheWay = Path.Combine(pc.Folder, "Own");
        Assert.StartsWith("The copy could not be saved there: ", model.SavePicked(inTheWay), StringComparison.Ordinal);
        Assert.Equal(["Documents", "Elsewhere.pro", "Own", "Saved.pro"], Directory.EnumerateFileSystemEntries(pc.Folder).Select(entry => Path.GetFileName(entry)).Order(StringComparer.Ordinal));
        Assert.True(Directory.Exists(inTheWay));
        model.Read();
        Assert.Equal("The copies take " + model.Size + ".", model.Notice);
        // Nothing here changed a song, or a copy.
        Assert.Equal(history.Now, pc.BytesOf("Plain Song"));
        Assert.Equal(2, pc.AppData.Copies().CopiesOf(PlainSong).Count);

        // Read again, what was picked is picked still.
        model.Read();
        Assert.Equal("Plain Song", model.PickedRow?.Song.Name);
        // A copy with no index says nothing of where its song was: whether the song is
        // still there is then not known.
        File.Delete(Path.Combine(pc.AppData.Copies().FolderOf(PlainSong), "index.json"));
        model.Read();
        Assert.Null(model.Rows.Single(row => row.Song.Name == "Plain Song").SongIsStillThere);
        Assert.Equal("Songs, in Documents", model.Rows.Single(row => row.Song.Name == "Plain Song").Where);
        return Task.CompletedTask;
    });

    [Fact]
    public void WithNoCopiesKeptAllCopiesSaysSo() => Pictures.OnAThreadOfItsOwn(() =>
    {
        using var pc = new MadeUpPC();
        var model = new AllCopiesModel(pc.AppData.Copies());
        model.Read();
        Assert.Empty(model.Rows);
        Assert.Null(model.PickedRow);
        Assert.Null(model.Copy);
        Assert.Null(model.Looked);
        Assert.Equal("", model.Notice);
        Assert.Null(model.SavePicked(Path.Combine(pc.Folder, "Saved.pro")));
        Assert.False(File.Exists(Path.Combine(pc.Folder, "Saved.pro")));
        Assert.Equal("No copies are kept yet.", AllCopiesModel.NoneYet);
        Assert.Equal(
            "Before this app changes a song" + (char)0x2019 + "s file, it keeps a copy of the file as it was. These are the copies.",
            AllCopiesModel.WhatTheseAre);
    });

    [Fact]
    public void HowMuchRoomInWordsAsWindowsCountsIt()
    {
        Assert.Equal("0 bytes", AllCopiesModel.InWords(0));
        Assert.Equal("1 byte", AllCopiesModel.InWords(1));
        Assert.Equal("1023 bytes", AllCopiesModel.InWords(1023));
        Assert.Equal("1 KB", AllCopiesModel.InWords(1024));
        Assert.Equal("81 KB", AllCopiesModel.InWords(83_000));
        Assert.Equal("1024 KB", AllCopiesModel.InWords((1024 * 1024) - 1));
        Assert.Equal("1.0 MB", AllCopiesModel.InWords(1024 * 1024));
        Assert.Equal("1.2 MB", AllCopiesModel.InWords(1_258_291));
        Assert.Equal("1023.9 MB", AllCopiesModel.InWords((1024L * 1024 * 1024) - 100_000));
        Assert.Equal("1.00 GB", AllCopiesModel.InWords(1024L * 1024 * 1024));
        Assert.Equal("2.50 GB", AllCopiesModel.InWords(5L * 1024 * 1024 * 512));
    }
}
