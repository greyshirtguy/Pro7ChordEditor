// The ChordPro editor in the window, as far as it is the model's
// (spec/04-chordpro-editor.md): what the model does with what is typed, and when it
// saves; the two keys in the song's header (spec/05-song-and-saving.md, rules 5 to 7);
// and that nothing is saved by hand, with Revert in the place of it (spec/06-library.md,
// rule 12a; spec/07-backups.md, rule 22a).
//
// All on a made-up PC, in a folder of its own (MadeUpPC.cs). Its songs are copies of the
// test files. The rules of what may be typed are proved without a window in
// Song.Tests/ChordProEditorTests; here it is that the window's model keeps to them. The
// text box itself, typed into a key at a time, is ChordProTypingTests.
//
// MINE, NOT A VECTOR, each of them. They expect what the Mac's tests of the same expect
// (ChordProTypingTests.swift, "The model"; EarlierVersionsTests.swift, "Revert" and
// "Command+S"), less what waits for the chord sheet.

using System.IO;
using Pro7ChordEditor.ChordRules;
using Pro7ChordEditor.Song;
using Xunit;

namespace Pro7ChordEditor.AppViews.Tests;

public class ChordProModelTests
{
    private const string RunningSentence = "ProPresenter is running. Quit it to change chords here. Nothing was changed.";
    private static readonly KeptSong PlainSong = new(MadeUpPC.PlaceName, "Songs", "Plain Song");

    private static void OnAMadeUpPC(Func<MadeUpPC, Task> what) => Pictures.OnAThreadOfItsOwn(async () =>
    {
        using var pc = new MadeUpPC();
        await what(pc);
    });

    internal static async Task<LibraryModel> Open(MadeUpPC pc, string name)
    {
        var model = pc.Model();
        // (No waiting about in a test: a save follows the typing at once when asked.)
        model.PauseBeforeSaving = TimeSpan.FromMilliseconds(1);
        await model.StartAsync();
        model.ChooseLibrary(pc.Library("Songs"));
        await model.MarksAreWorkedOutAsync();
        model.Open(pc.Song(name));
        return model;
    }

    // The text with the first of something in it replaced: what typing there leaves.
    internal static string With(string old, string made, LibraryModel model)
    {
        var text = model.ChordPro!.Text;
        var at = text.IndexOf(old, StringComparison.Ordinal);
        Assert.True(at >= 0, "the text has no \"" + old + "\" in it");
        return text[..at] + made + text[(at + old.Length)..];
    }

    // A box of a song's file as "place:name" for each chord.
    internal static IEnumerable<string> ChordsOf(int box, string name, MadeUpPC pc) =>
        new OpenSong(pc.Song(name)).TextBoxes[box].Chords.Select(chord => chord.At + ":" + chord.Name);

    // ---- What is typed

    [Fact]
    public void ASongOpenedIsShownAsChordProTextToBeTypedIn() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        Assert.StartsWith("{c: Verse 1}\n[G]One two three [D]four\n", model.ChordPro!.Text, StringComparison.Ordinal);
        Assert.Null(model.ChordPro.Barred);
        Assert.True(model.CanType && model.CanChange);
        Assert.Equal("4 slides, 11 chords. A change is saved a moment after it is typed.", model.Notice);
        // One that cannot be read has no text, and nothing can be done to it.
        model.Open(pc.Song("Nonsense"));
        Assert.Null(model.ChordPro);
        Assert.False(model.CanType || model.CanChange);
        Assert.False(model.TextMayBecome("anything"));
        Assert.Equal("Nothing was done to this file.", model.Notice);
        // And with no song open there is no text either.
        model.Open(null);
        Assert.Null(model.ChordPro);
        Assert.False(model.TextMayBecome("anything"));
    });

    [Fact]
    public void AChordTypedIsSavedAMomentAfterTheTypingStopsWithACopyOfTheFileKeptFirst() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        var original = pc.BytesOf("Plain Song");
        Assert.False(model.HasEarlierVersions);
        Assert.DoesNotContain(model.Rows, row => row.HasCopies);

        Assert.True(model.TextMayBecome(With("One two", "One []two", model)));
        Assert.True(model.TextMayBecome(With("One []two", "One [A]two", model)));
        // Not yet: the typing has only just stopped.
        Assert.Equal(original, pc.BytesOf("Plain Song"));
        await model.WhatWasTypedIsSavedAsync();
        Assert.Equal(["0:G", "4:A", "14:D", "28:Em"], ChordsOf(0, "Plain Song", pc));

        // What is shown of the song follows: its count, its mark in the list, and that
        // it has an earlier version now, which is the file as it was.
        Assert.Equal(12, model.Shown!.Chords);
        Assert.Equal(12, model.Marks[pc.Song("Plain Song")].Chords);
        Assert.Equal("4 slides, 12 chords. A change is saved a moment after it is typed.", model.Notice);
        Assert.True(model.HasEarlierVersions);
        Assert.Equal(["Plain Song"], model.Rows.Where(row => row.HasCopies).Select(row => row.Name));
        var copy = Assert.Single(pc.AppData.Copies().CopiesOf(PlainSong));
        Assert.Equal(CopyReason.BeforeAChange, copy.Why);
        Assert.Equal(original, File.ReadAllBytes(copy.Path));
        // The marks are kept for the next start, by the file as it now is.
        Assert.Equal(12, pc.AppData.ReadMarks().MarksFor(model.Songs.Single(song => song.Name == "Plain Song"))?.Chords);
        // The text is left exactly as typed, and is not set afresh.
        Assert.Contains("[G]One [A]two three [D]four", model.ChordPro!.Text, StringComparison.Ordinal);
        Assert.Equal(1, model.ChordPro.Version);
        Assert.Null(model.Refused);
    });

    [Fact]
    public void TheSaveWaitsForTheTypingToStopAndEachKeyPutsItOff() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        var original = pc.BytesOf("Plain Song");
        // (A pause far longer than the test: if the first wait were not given up, the
        // test would be seen to wait for it.)
        model.PauseBeforeSaving = TimeSpan.FromSeconds(60);
        Assert.True(model.TextMayBecome(With("One two", "One [A]two", model)));
        var first = model.WhatWasTypedIsSavedAsync();
        Assert.False(first.IsCompleted);
        // Typed again before the pause was over: the first wait is given up, and saves
        // nothing; the save comes after the second.
        model.PauseBeforeSaving = TimeSpan.FromMilliseconds(1);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        Assert.True(model.TextMayBecome(With("One [A]two", "One [Am]two", model)));
        await first;
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(30));
        Assert.Equal(original, pc.BytesOf("Plain Song"));
        await model.WhatWasTypedIsSavedAsync();
        Assert.Equal(["0:G", "4:Am", "14:D", "28:Em"], ChordsOf(0, "Plain Song", pc));
        // One save, one copy, and the pause is what the page says unless a test says otherwise.
        Assert.Single(pc.AppData.Copies().CopiesOf(PlainSong));
        Assert.Equal(TimeSpan.FromMilliseconds(700), pc.Model().PauseBeforeSaving);
    });

    [Fact]
    public void WhatIsNotTheSongsWordsIsNotLetThroughAndNothingIsSaved() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        var original = pc.BytesOf("Plain Song");
        var before = model.ChordPro;
        Assert.False(model.TextMayBecome(With("One two", "One xtwo", model)));
        Assert.False(model.TextMayBecome(With("{c: Chorus}", "{c: Chorus 2}", model)));
        await model.WhatWasTypedIsSavedAsync();
        Assert.True(model.Settle());
        Assert.Equal(before, model.ChordPro);
        Assert.Null(model.Refused);
        Assert.Equal(original, pc.BytesOf("Plain Song"));
        Assert.False(model.HasEarlierVersions);
    });

    [Fact]
    public void OneMoreChordOnALineWithNoWordsIsSavedAndOneFewerAndAChordOnABlankSlidesLineGivesTheSlideItsWords() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        // A bracket still empty is let through, and is not saved as a chord.
        Assert.True(model.TextMayBecome(With("[G] [D/F#] [Em]", "[G] [D/F#] [Em] []", model)));
        await model.WhatWasTypedIsSavedAsync();
        Assert.Equal(["0:G", "2:D/F#", "4:Em"], ChordsOf(3, "Plain Song", pc));
        Assert.True(model.TextMayBecome(With("[G] [D/F#] [Em] []", "[G] [D/F#] [Em] [C]", model)));
        await model.WhatWasTypedIsSavedAsync();
        Assert.Equal(["0:G", "2:D/F#", "4:Em", "6:C"], ChordsOf(3, "Plain Song", pc));
        Assert.True(model.TextMayBecome(With("[G] [D/F#] [Em] [C]", "[D/F#] [C]", model)));
        await model.WhatWasTypedIsSavedAsync();
        Assert.Equal(["0:D/F#", "2:C"], ChordsOf(3, "Plain Song", pc));
        Assert.Null(model.Refused);

        // Blank Slides begins with a blank slide: the second line of the text, after
        // its group's name.
        var blank = await Open(pc, "Blank Slides");
        var lines = blank.ChordPro!.Text.Split('\n');
        Assert.Equal("", lines[1]);
        lines[1] = "[G] [C]";
        Assert.True(blank.TextMayBecome(string.Join('\n', lines)));
        await blank.WhatWasTypedIsSavedAsync();
        Assert.Equal(["0:G", "2:C"], ChordsOf(0, "Blank Slides", pc));
        Assert.Null(blank.Refused);
        // The text is left as typed; made again from the song it is the same.
        Assert.Equal("[G] [C]", blank.ChordPro!.Text.Split('\n')[1]);
        blank.OpenAgain();
        Assert.Equal("[G] [C]", blank.ChordPro!.Text.Split('\n')[1]);
    });

    [Fact]
    public void OpeningAnotherSongSavesWhatWasBeingTypedInTheOneThatIsLeft() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        model.PauseBeforeSaving = TimeSpan.FromSeconds(60);
        Assert.True(model.TextMayBecome(With("Thirteen", "Thir[A]teen", model)));
        model.Open(pc.Song("Amazing Grace"));
        Assert.Equal(["0:C", "10:G/B", "27:A"], ChordsOf(1, "Plain Song", pc));
        Assert.Equal("Amazing Grace", model.Shown!.Name);
        Assert.Contains("A[G]mazing [G7]grace", model.ChordPro!.Text, StringComparison.Ordinal);
        // (A new sitting, and the text set afresh.)
        Assert.Equal((2, 2), (model.ChordPro.Version, model.ChordPro.Sitting));
        // And so does leaving the app, or closing it: `Settle` is what those call.
        Assert.True(model.TextMayBecome(With("grace", "[F#m7]grace", model)));
        Assert.True(model.Settle());
        Assert.Contains("F#m7", new OpenSong(pc.Song("Amazing Grace")).UsedChords);
        // So does another library chosen, and a folder chosen by hand.
        Assert.True(model.TextMayBecome(With("[F#m7]grace", "[Bm]grace", model)));
        model.ChooseLibrary(pc.Library("Kids"));
        Assert.Contains("Bm", new OpenSong(pc.Song("Amazing Grace")).UsedChords);
        Assert.Null(model.Shown);
        Assert.Null(model.ChordPro);
    });

    [Fact]
    public void WhileProPresenterIsRunningNothingCanBeTypedAndIfItWasStartedUnseenTheSaveIsRefusedAndTheTextIsTheSongAgain() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        var original = pc.BytesOf("Plain Song");
        Assert.True(model.TextMayBecome(With("One two", "One [A]two", model)));
        // ProPresenter is started before the pause is over, and the app has not been
        // told: only the save itself asks.
        pc.ProPresenterIsRunning = true;
        await model.WhatWasTypedIsSavedAsync();
        Assert.Equal(original, pc.BytesOf("Plain Song"));
        Assert.False(model.HasEarlierVersions);
        Assert.Equal(RunningSentence, model.Refused);
        Assert.Equal(RunningSentence, model.Notice);
        Assert.True(model.ProPresenterIsRunning);
        Assert.False(model.CanType || model.CanChange);
        Assert.DoesNotContain("[A]", model.ChordPro!.Text, StringComparison.Ordinal);
        Assert.Equal((2, 1), (model.ChordPro.Version, model.ChordPro.Sitting));
        Assert.False(model.TextMayBecome(With("One two", "One [A]two", model)));
        // (The log says a change was refused, and does not say the file was saved.)
        Assert.Contains("  A change to Plain Song.pro was refused.", pc.Log, StringComparison.Ordinal);
        Assert.DoesNotContain("Saved", pc.Log, StringComparison.Ordinal);
        // Quit, and the app come to the front again: it can be typed in, and the line
        // no longer says what is no longer so.
        pc.ProPresenterIsRunning = false;
        await model.RefreshAsync();
        Assert.True(model.CanType);
        Assert.Null(model.Refused);
        Assert.Equal(2, model.ChordPro!.Version);
        Assert.True(model.TextMayBecome(With("One two", "One [A]two", model)));
        await model.WhatWasTypedIsSavedAsync();
        Assert.Equal(["0:G", "4:A", "14:D", "28:Em"], ChordsOf(0, "Plain Song", pc));
    });

    [Fact]
    public void WhenProPresenterIsQuitWithTheAppInFrontWhatWasRefusedBecauseOfItIsSaidNoLonger() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        Assert.True(model.TextMayBecome(With("One two", "One [A]two", model)));
        pc.ProPresenterIsRunning = true;
        await model.WhatWasTypedIsSavedAsync();
        Assert.Equal(RunningSentence, model.Notice);
        // It is quit, and the window's look every second or two finds it gone.
        pc.ProPresenterIsRunning = false;
        model.ProPresenterMayHaveChanged();
        Assert.Null(model.Refused);
        Assert.True(model.CanType);
        Assert.Equal("4 slides, 11 chords. A change is saved a moment after it is typed.", model.Notice);
        // What went wrong otherwise stays said, whatever ProPresenter does.
        Assert.True(model.TextMayBecome(With("One two", "One [A]two", model)));
        pc.Change("Plain Song", 0, song => song.SetKey("E"));
        await model.WhatWasTypedIsSavedAsync();
        Assert.True(model.OffersOpeningAgain);
        pc.ProPresenterIsRunning = true;
        model.ProPresenterMayHaveChanged();
        pc.ProPresenterIsRunning = false;
        await model.LookForProPresenterAsync();
        Assert.StartsWith("This song's file has been changed by something else", model.Refused, StringComparison.Ordinal);
        Assert.True(model.OffersOpeningAgain);
    });

    [Fact]
    public void IfWhatWasTypedCannotBeSavedWhenTheSongIsLeftTheSongIsNotLeftTheSentenceIsSeenFirst() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        model.PauseBeforeSaving = TimeSpan.FromSeconds(60);
        Assert.True(model.TextMayBecome(With("One two", "One [A]two", model)));
        // ProPresenter is started unseen, and another song is clicked.
        pc.ProPresenterIsRunning = true;
        model.Open(pc.Song("Amazing Grace"));
        Assert.Equal(pc.Song("Plain Song"), model.SongId);
        Assert.Equal("Plain Song", model.Shown!.Name);
        Assert.Equal(RunningSentence, model.Refused);
        Assert.DoesNotContain("[A]", model.ChordPro!.Text, StringComparison.Ordinal);
        // Nor is another library opened, nor another place, nor a folder chosen, nor the
        // earlier versions shown, nor a key set.
        foreach (var leave in new Action<LibraryModel>[]
        {
            left => left.ChooseLibrary(pc.Library("Kids")),
            left => left.ChoosePlace(null),
            left => left.Add(Path.Combine(pc.Folder, "Documents")),
            left => left.ShowEarlierVersions(),
            left => left.SetKey("E"),
            left => left.SetShownKey("E"),
        })
        {
            pc.ProPresenterIsRunning = false;
            await model.RefreshAsync();
            Assert.True(model.TextMayBecome(With("One two", "One [A]two", model)));
            pc.ProPresenterIsRunning = true;
            leave(model);
            Assert.Equal("Songs", model.Library?.Name);
            Assert.Equal(pc.Place, model.PlaceId);
            Assert.Equal(("Plain Song", "G", "G"), (model.Shown!.Name, model.Shown.Key, model.Shown.ShownKey));
            Assert.Equal(RunningSentence, model.Refused);
            Assert.Null(model.Earlier);
            Assert.DoesNotContain("[A]", model.ChordPro!.Text, StringComparison.Ordinal);
        }
        // Clicked again, with nothing left unsaved, the other song opens.
        model.Open(pc.Song("Amazing Grace"));
        Assert.Equal("Amazing Grace", model.Shown!.Name);
        Assert.Null(model.Refused);
    });

    [Fact]
    public void AFileChangedBySomethingElseIsNotSavedOverTheLineSaysSoAndOffersTheSongReadAgain() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        Assert.True(model.TextMayBecome(With("One two", "One [A]two", model)));
        // Something else changes the file meanwhile: here, its key.
        pc.Change("Plain Song", 0, song => song.SetKey("E"));
        var theirs = pc.BytesOf("Plain Song");
        await model.WhatWasTypedIsSavedAsync();
        Assert.Equal(theirs, pc.BytesOf("Plain Song"));
        Assert.StartsWith("This song's file has been changed by something else", model.Refused, StringComparison.Ordinal);
        Assert.True(model.OffersOpeningAgain);
        // (The band is for ProPresenter, and this was not ProPresenter.)
        Assert.False(model.ProPresenterIsRunning);
        model.OpenAgain();
        Assert.Equal("E", model.Shown!.Key);
        Assert.Null(model.Refused);
        Assert.False(model.OffersOpeningAgain);
        Assert.True(model.TextMayBecome(With("One two", "One [A]two", model)));
        await model.WhatWasTypedIsSavedAsync();
        Assert.Equal(["0:G", "4:A", "14:D", "28:Em"], ChordsOf(0, "Plain Song", pc));
    });

    [Fact]
    public void SomethingTypedAfterARefusalTakesTheSentenceAway() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        Assert.True(model.TextMayBecome(With("One two", "One [A]two", model)));
        pc.Change("Plain Song", 0, song => song.SetKey("E"));
        await model.WhatWasTypedIsSavedAsync();
        Assert.NotNull(model.Refused);
        // What went wrong stays on the line until the next thing is done; a key that is
        // not taken is not a thing done.
        Assert.False(model.TextMayBecome(With("One two", "One xtwo", model)));
        Assert.NotNull(model.Refused);
        Assert.True(model.OffersOpeningAgain);
        model.PauseBeforeSaving = TimeSpan.FromSeconds(60);
        Assert.True(model.TextMayBecome(With("One two", "One []two", model)));
        Assert.Null(model.Refused);
        Assert.False(model.OffersOpeningAgain);
    });

    [Fact]
    public void WhenTheAppComesToTheFrontTheSongStaysAsItIsBeingTypedUnlessItsFileHasChangedUnderneath() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        Assert.True(model.TextMayBecome(With("Five six", "Five []six", model)));
        await model.RefreshAsync();
        // Still the same sitting: the empty brackets are there, and the text was not set afresh.
        Assert.Equal(1, model.ChordPro!.Version);
        Assert.Contains("Five []six", model.ChordPro.Text, StringComparison.Ordinal);
        pc.Change("Plain Song", 0, song => song.SetKey("E"));
        await model.RefreshAsync();
        Assert.Equal(2, model.ChordPro!.Version);
        Assert.DoesNotContain("Five []six", model.ChordPro.Text, StringComparison.Ordinal);
        Assert.Equal("E", model.Shown!.Key);
    });

    [Fact]
    public void TheSongThatIsOpenStaysInTheListWhenItsFirstChordTakesItOutOfWhatTheListIsNarrowedTo() => OnAMadeUpPC(async pc =>
    {
        // A song with no chords: Plain Song with every chord taken off, through the song itself.
        pc.Change("Plain Song", 0, song =>
        {
            foreach (var box in song.TextBoxes.Where(box => !Chords.IsPlaceholders(box.Words)))
            {
                song.SetChords(box.Slide, box.Element, [], joined: true);
            }
        });
        var model = await Open(pc, "Plain Song");
        model.Filter = MarkFilter.WithoutChords;
        Assert.Contains("Plain Song", model.ListedSongs.Select(song => song.Name));
        Assert.True(model.TextMayBecome(With("One two", "One [A]two", model)));
        await model.WhatWasTypedIsSavedAsync();
        Assert.True(model.Marks[pc.Song("Plain Song")].HasChords);
        Assert.Contains("Plain Song", model.ListedSongs.Select(song => song.Name));
        // Left for another song, it is no longer one without chords.
        model.Open(null);
        Assert.DoesNotContain("Plain Song", model.ListedSongs.Select(song => song.Name));
    });

    [Fact]
    public void AnEarlierVersionPutBackIsWhatTheTextThenShowsAsANewSitting() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        var original = pc.BytesOf("Plain Song");
        model.PauseBeforeSaving = TimeSpan.FromSeconds(60);
        Assert.True(model.TextMayBecome(With("One two", "One [A]two", model)));
        // The panel is opened with the typing still unsaved: it is saved first, so the
        // copy listed is of the file as it was before it.
        model.ShowEarlierVersions();
        Assert.Single(model.Earlier!.Copies);
        model.PutBackPicked();
        Assert.Equal(original, pc.BytesOf("Plain Song"));
        Assert.DoesNotContain("[A]", model.ChordPro!.Text, StringComparison.Ordinal);
        Assert.Equal((2, 2), (model.ChordPro.Version, model.ChordPro.Sitting));
        Assert.Equal(11, model.Shown!.Chords);
        Assert.Equal(11, model.Marks[pc.Song("Plain Song")].Chords);
        model.TakeBack();
        Assert.Contains("[G]One [A]two", model.ChordPro!.Text, StringComparison.Ordinal);
        Assert.Equal((3, 3), (model.ChordPro.Version, model.ChordPro.Sitting));
    });

    [Fact]
    public void ASongWhoseWordsHaveBracketsOfTheirOwnIsShownAndCannotBeTypedInAndItsKeysCanStillBeSet() => OnAMadeUpPC(async pc =>
    {
        // Plain Song with a bracket put among its words, in the file's RTF.
        var bytes = pc.BytesOf("Plain Song");
        var word = bytes.AsSpan().IndexOf("Thirteen"u8);
        Assert.True(word > 0);
        "Thir[een"u8.CopyTo(bytes.AsSpan(word));
        File.WriteAllBytes(pc.Song("Plain Song"), bytes);

        var model = await Open(pc, "Plain Song");
        Assert.Contains("Thir[een", model.ChordPro!.Text, StringComparison.Ordinal);
        Assert.Equal("This song's words have square brackets of their own in them, so it cannot be changed as ChordPro text.", model.ChordPro.Barred);
        Assert.False(model.CanType);
        Assert.True(model.CanChange);
        Assert.False(model.TextMayBecome(With("One two", "One [A]two", model)));
        // (Where nothing can be typed, the line does not say that what is typed is saved.)
        Assert.Equal("4 slides, 11 chords.", model.Notice);
        model.SetKey("E");
        Assert.Equal("E", model.Shown!.Key);
        Assert.Null(model.Refused);
    });

    [Fact]
    public void TheLogNamesFilesAndNeverHasAChordOrAWordOfASongInIt() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        Assert.True(model.TextMayBecome(With("Thirteen", "Thir[F#m7]teen", model)));
        await model.WhatWasTypedIsSavedAsync();
        pc.ProPresenterIsRunning = true;
        model.SetKey("E");
        Assert.Contains("  Opened Plain Song.pro.", pc.Log, StringComparison.Ordinal);
        Assert.Contains("  Saved Plain Song.pro.", pc.Log, StringComparison.Ordinal);
        Assert.Contains("  A change to Plain Song.pro was refused.", pc.Log, StringComparison.Ordinal);
        Assert.DoesNotContain("F#m7", pc.Log, StringComparison.Ordinal);
        Assert.DoesNotContain("Thirteen", pc.Log, StringComparison.Ordinal);
    });

    // ---- The two keys (spec/05, rules 5 to 7)

    [Fact]
    public void TheTwoKeysCanBeSetFromTheSongsHeaderAndNeitherChangesAChord() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        Assert.Equal(("G", "G"), (model.Shown!.Key, model.Shown.ShownKey));
        // The twenty-six keys, the thirteen major first.
        Assert.Equal(26, LibraryModel.Keys.Count);
        Assert.Equal(("C", "B", "Cm", "Bm"), (LibraryModel.Keys[0], LibraryModel.Keys[12], LibraryModel.Keys[13], LibraryModel.Keys[25]));
        var chordsBefore = new OpenSong(pc.Song("Plain Song")).TextBoxes.Select(box => box.Chords.ToList()).ToList();

        model.SetShownKey("A");
        Assert.Equal(("G", "A"), (model.Shown!.Key, model.Shown.ShownKey));
        Assert.Null(model.Refused);
        // Someone chose to show it in another key, so setting the key leaves that alone.
        model.SetKey("E");
        Assert.Equal(("E", "A"), (model.Shown!.Key, model.Shown.ShownKey));
        var now = new OpenSong(pc.Song("Plain Song"));
        Assert.Equal(("E", "A"), (now.Key, now.ShownKey));
        Assert.Equal(chordsBefore, now.TextBoxes.Select(box => box.Chords.ToList()).ToList());
        Assert.True(model.HasEarlierVersions);
        // (The text is the same text: a key is no part of it, and it is not set afresh.)
        Assert.Equal(1, model.ChordPro!.Version);
        // What was being typed is saved before the key is.
        model.PauseBeforeSaving = TimeSpan.FromSeconds(60);
        Assert.True(model.TextMayBecome(With("One two", "One [A]two", model)));
        model.SetKey("D");
        Assert.Equal(["0:G", "4:A", "14:D", "28:Em"], ChordsOf(0, "Plain Song", pc));
        Assert.Equal("D", new OpenSong(pc.Song("Plain Song")).Key);
        // While ProPresenter runs, a key is refused like anything else.
        pc.ProPresenterIsRunning = true;
        model.SetKey("C");
        Assert.Equal("D", model.Shown!.Key);
        Assert.Equal(RunningSentence, model.Refused);
        model.SetShownKey("C");
        Assert.Equal("A", model.Shown!.ShownKey);
        Assert.Equal("D", new OpenSong(pc.Song("Plain Song")).Key);
    });

    [Fact]
    public void ASongThatStatesNoKeyIsGivenOneFromItsHeader() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Holy Holy Holy");
        Assert.Equal(("", ""), (model.Shown!.Key, model.Shown.ShownKey));
        model.SetKey("D");
        Assert.Null(model.Refused);
        // The key it is shown in comes with its first key, since nobody chose another.
        Assert.Equal(("D", "D"), (model.Shown!.Key, model.Shown.ShownKey));
        var now = new OpenSong(pc.Song("Holy Holy Holy"));
        Assert.Equal(("D", "D"), (now.Key, now.ShownKey));
        // With no song open, or one that cannot be read, there is no key to set.
        model.Open(pc.Song("Nonsense"));
        model.SetKey("E");
        Assert.Null(model.Refused);
        Assert.Equal("This is not a song.", File.ReadAllText(pc.Song("Nonsense")));
    });

    // ---- Nothing is saved by hand (spec/06, rule 12a; spec/07, rule 22a)

    [Fact]
    public void RevertMakesTheSongWhatItWasWhenItWasOpenedHereOfferedOnlyOnceSomethingHasChangedUndoneLikeAnyPuttingBackAndRefusedLikeAnySave() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        var opened = pc.BytesOf("Plain Song");
        Assert.False(model.CanRevert || model.ChangedSinceOpened);
        Assert.Equal("Autosave on", model.SavingCaption);
        Assert.Equal("There is nothing to save by hand: every change is saved as it is made, with a copy of the song as it was kept first.", model.SavingTip);
        // With nothing changed there is nothing to revert: nothing is asked, and
        // nothing is done.
        model.AskToRevert();
        Assert.Null(model.RevertQuestion);
        model.Revert();
        Assert.Null(model.JustPutBack);
        Assert.False(model.HasEarlierVersions);

        // Changes of the key and of the text.
        model.SetKey("E");
        Assert.True(model.CanRevert);
        var text = model.ChordPro!.Text;
        Assert.True(model.TextMayBecome(text.Replace("three", "thr[F#m]ee", StringComparison.Ordinal)));
        Assert.True(model.Settle());
        var changed = pc.BytesOf("Plain Song");
        Assert.NotEqual(opened, changed);
        Assert.True(model.CanRevert);

        // It asks first, in words that say it is every change and not the last one.
        model.AskToRevert();
        var question = model.RevertQuestion!;
        Assert.Equal("Revert " + (char)0x201C + "Plain Song" + (char)0x201D + " to how it was when it was opened?", question.Title);
        Assert.Equal("This takes back EVERY change made to this song since it was opened in this editor.", question.Message);
        Assert.Equal("Revert All Changes", question.Yes);
        Assert.Equal(changed, pc.BytesOf("Plain Song"));
        // No: nothing is done, and it can be asked again.
        model.DontRevert();
        Assert.Null(model.RevertQuestion);
        Assert.True(model.CanRevert);
        Assert.Null(model.JustPutBack);
        Assert.Equal(changed, pc.BytesOf("Plain Song"));
        // Yes.
        model.AskToRevert();
        model.Revert();
        Assert.Null(model.RevertQuestion);
        Assert.Equal(opened, pc.BytesOf("Plain Song"));
        Assert.False(model.CanRevert);
        Assert.Equal("G", model.Shown!.Key);
        Assert.Equal("The song is as it was when it was opened here.", model.Notice);
        Assert.Equal(text, model.ChordPro!.Text);
        Assert.Equal(11, model.Marks[pc.Song("Plain Song")].Chords);
        // What it was just before is kept, and the line offers to take the revert back.
        Assert.NotNull(model.JustPutBack);
        Assert.True(model.HasEarlierVersions);
        Assert.Equal(changed, File.ReadAllBytes(model.JustPutBack!.Before.Path));
        Assert.Equal(CopyReason.BeforePuttingBack, model.JustPutBack.Before.Why);
        model.TakeBack();
        Assert.Equal(changed, pc.BytesOf("Plain Song"));
        Assert.True(model.CanRevert);
        Assert.Contains("thr[F#m]ee", model.ChordPro!.Text, StringComparison.Ordinal);
        // And reverted once more, it is as opened once more.
        model.Revert();
        Assert.Equal(opened, pc.BytesOf("Plain Song"));
        Assert.False(model.CanRevert);
        // Asked again with nothing to revert, nothing is done: no offer to undo, which
        // would be an offer to put back a copy that has nothing to do with it.
        model.ForgetWhatWasPutBack();
        model.Revert();
        Assert.Null(model.JustPutBack);
        Assert.NotEqual("The song is as it was when it was opened here.", model.Notice);

        // While ProPresenter is running it is refused like any save, in the same words.
        model.SetKey("A");
        var inA = pc.BytesOf("Plain Song");
        pc.ProPresenterIsRunning = true;
        model.ProPresenterMayHaveChanged();
        Assert.Equal("Autosave paused", model.SavingCaption);
        Assert.Equal("Nothing is saved while ProPresenter is running.", model.SavingTip);
        Assert.False(model.CanRevert);
        model.AskToRevert();
        Assert.Null(model.RevertQuestion);
        pc.ProPresenterIsRunning = false;
        model.ProPresenterMayHaveChanged();
        Assert.True(model.CanRevert);
        Assert.Equal("Autosave on", model.SavingCaption);
        pc.ProPresenterIsRunning = true;
        model.Revert();
        Assert.Equal(RunningSentence, model.Notice);
        Assert.Equal(inA, pc.BytesOf("Plain Song"));
        Assert.Contains("  Plain Song.pro was not reverted.", pc.Log, StringComparison.Ordinal);
        pc.ProPresenterIsRunning = false;
        model.ProPresenterMayHaveChanged();

        // The song left with the question still up: the question goes with it. And
        // come back to, as opened is what the song is now.
        model.AskToRevert();
        Assert.NotNull(model.RevertQuestion);
        model.Open(pc.Song("Amazing Grace"));
        Assert.False(model.CanRevert);
        Assert.Null(model.RevertQuestion);
        model.Open(pc.Song("Plain Song"));
        Assert.False(model.CanRevert);
        Assert.Equal("A", model.Shown!.Key);
        Assert.Contains("  Reverted Plain Song.pro to as it was opened.", pc.Log, StringComparison.Ordinal);
    });

    [Fact]
    public void WhatWasTypedAndNotYetSavedIsSavedBeforeARevertSoThatItCanBeHadBack() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        var opened = pc.BytesOf("Plain Song");
        model.SetKey("E");
        model.PauseBeforeSaving = TimeSpan.FromSeconds(60);
        Assert.True(model.TextMayBecome(With("One two", "One [A]two", model)));
        model.AskToRevert();
        model.Revert();
        Assert.Equal(opened, pc.BytesOf("Plain Song"));
        // The copy kept just before has the chord that was typed, and the key.
        var before = new OpenSong(model.JustPutBack!.Before.Path);
        Assert.Equal("E", before.Key);
        Assert.Contains("A", before.UsedChords);
    });

    [Fact]
    public void TheKeyThatWouldSaveSavesNothingByHandWhatIsWaitingForThePauseIsSavedAtOnceAndTheQuietLineSaysThereWasNothingToDo() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        // (A long pause, so that what is typed stays unsaved until something saves it.)
        model.PauseBeforeSaving = TimeSpan.FromSeconds(60);
        model.SayingLasts = TimeSpan.FromMilliseconds(30);
        var before = pc.BytesOf("Plain Song");
        Assert.True(model.TextMayBecome(model.ChordPro!.Text.Replace("three", "thr[F#m]ee", StringComparison.Ordinal)));
        Assert.Equal(before, pc.BytesOf("Plain Song"));
        model.SaveNow();
        Assert.NotEqual(before, pc.BytesOf("Plain Song"));
        Assert.Equal("Saved. Every change here is saved as it is made: there is nothing to save by hand.", model.Notice);
        // It is said for a few seconds, and then the line is what it was.
        await model.WhatWasSaidIsGoneAsync();
        Assert.Null(model.Said);
        Assert.Equal("4 slides, 12 chords. A change is saved a moment after it is typed.", model.Notice);
        // While ProPresenter is running it says what the band says, and no more.
        pc.ProPresenterIsRunning = true;
        model.ProPresenterMayHaveChanged();
        model.SayingLasts = TimeSpan.FromSeconds(60);
        model.SaveNow();
        Assert.Equal("ProPresenter is running. Quit it to change chords here.", model.Notice);
        // What went wrong is said before what was only said.
        pc.ProPresenterIsRunning = false;
        model.ProPresenterMayHaveChanged();
        model.SaveNow();
        Assert.StartsWith("Saved.", model.Notice, StringComparison.Ordinal);
        pc.ProPresenterIsRunning = true;
        model.SetKey("E");
        Assert.Equal(RunningSentence, model.Notice);
        // A song left takes what was said with it; and with no song open there is
        // nothing to say.
        pc.ProPresenterIsRunning = false;
        model.ProPresenterMayHaveChanged();
        model.SaveNow();
        model.Open(pc.Song("Amazing Grace"));
        Assert.Null(model.Said);
        model.Open(null);
        model.SaveNow();
        Assert.Null(model.Shown);
        Assert.Null(model.Said);
        Assert.DoesNotContain("Saved", model.Notice, StringComparison.Ordinal);
    });

    // ---- The size of the text (spec/04, rule 25)

    [Fact]
    public void TheTextCanBeMadeLargerAndSmallerInTheSheetsStepsToTheirEndsAndTheSizeIsRemembered() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        var before = pc.BytesOf("Plain Song");
        Assert.Equal([0.5, 0.6, 0.7, 0.8, 0.9, 1.0, 1.1, 1.25, 1.5], LibraryModel.Zooms);
        Assert.Equal(1.0, model.TextZoom);
        Assert.True(model.CanZoomTextIn && model.CanZoomTextOut);
        Assert.Equal(0.9, model.ZoomText(-1));
        Assert.Equal(1.0, model.ZoomText(1));
        Assert.Equal(1.1, model.ZoomText(1));
        Assert.Equal(1.25, model.ZoomText(1));
        Assert.Equal(1.0, model.ZoomText(0));
        for (var times = 0; times < 12; times++)
        {
            model.ZoomText(1);
        }
        Assert.Equal(1.5, model.TextZoom);
        Assert.True(!model.CanZoomTextIn && model.CanZoomTextOut);
        for (var times = 0; times < 12; times++)
        {
            model.ZoomText(-1);
        }
        Assert.Equal(0.5, model.TextZoom);
        Assert.True(model.CanZoomTextIn && !model.CanZoomTextOut);
        // Only the size: not a character of the text, nor the song.
        Assert.Equal(1, model.ChordPro!.Version);
        Assert.Equal(before, pc.BytesOf("Plain Song"));
        // It is remembered; and the usual size is kept as nothing at all.
        var again = pc.Model();
        await again.StartAsync();
        Assert.Equal(0.5, again.TextZoom);
        Assert.Equal(0.5, pc.AppData.ReadSettings().TextZoom);
        again.ZoomText(0);
        Assert.Null(pc.AppData.ReadSettings().TextZoom);
        // A size that is none of the steps, as a hand in the settings might leave it, is
        // taken as the nearest of them.
        pc.AppData.Keep(pc.AppData.ReadSettings() with { TextZoom = 1.3 });
        var third = pc.Model();
        await third.StartAsync();
        Assert.Equal(1.25, third.TextZoom);
    });
}
