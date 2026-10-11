// The window's model, the part of it that is about mending (LibraryModel.Mending.cs):
// what is found in the song that is open and what is offered for it, the mark and the
// choice over the list, the song shown both ways, and Mend All. spec/08-repair.md, and
// spec/06-library.md, rules 9 and 11.
//
// MINE. Neither the reference nor the Mac app has any of this yet, so nothing but the
// page holds it: a sentence the page gives is expected here as the page gives it, and
// what a mending leaves in a file is held to the clean file the damaged one was made
// from (testdata/files/expected/repairs.json, and Song.Tests/MendingTests.cs, where the
// checks and the mendings themselves are proved).
//
// The made-up PC's library "Old copies" has the four files as the old editor left them:
//
//   A Song - In A Minor Key (Chords)   1 chord shown twice; its key reset to A flat
//   Amazing Grace (Chords)             6 shown twice; every chord put on by hand
//   It Is Well (Chords)                4 shown twice
//   Plain Song (Chords)                3 shown twice

using System.IO;
using Pro7ChordEditor.Song;
using Pro7ChordEditor.Tests;
using Xunit;
using OfferAction = Pro7ChordEditor.AppViews.LibraryModel.OfferAction;

namespace Pro7ChordEditor.AppViews.Tests;

public class MendingModelTests
{
    private const string Old = "Old copies";
    private const string MinorKey = "A Song - In A Minor Key (Chords)";
    private const string Grace = "Amazing Grace (Chords)";
    private const string Well = "It Is Well (Chords)";
    private const string Plain = "Plain Song (Chords)";

    private const string MovedLeft =
        "look as if they were put on in the old Windows chord editor, which counted them a little to the left on every line after the first.";

    private static void OnAMadeUpPC(Func<MadeUpPC, Task> what) => Pictures.OnAThreadOfItsOwn(async () =>
    {
        using var pc = new MadeUpPC();
        await what(pc);
    });

    private static async Task<LibraryModel> AtTheOldCopies(MadeUpPC pc)
    {
        var model = pc.Model();
        await model.StartAsync();
        model.ChooseLibrary(pc.Library(Old));
        await model.MarksAreWorkedOutAsync();
        return model;
    }

    private static IEnumerable<string> Names(IEnumerable<ListedSong> songs) => songs.Select(song => song.Name);

    // An offer as "sentence [Button] [Button]".
    private static string Said(LibraryModel.Offer offer) => offer.Sentence + string.Concat(offer.Buttons.Select(button => " [" + button.Label + "]"));

    // Clicks the one button that says this, of what is offered for the open song: so
    // that what a button does is held with what it says.
    private static void Press(LibraryModel model, string label) =>
        model.Take(model.Offers.SelectMany(offer => offer.Buttons).Single(button => button.Label == label).Action);

    private static byte[] Clean(string name) => TestData.InFiles("made-by-simple-presenter/" + name + ".pro");

    private static bool Same(byte[] one, byte[] other) => one.AsSpan().SequenceEqual(other);

    // ---- The marks, and the choice over the list (spec/06, rules 9 and 11)

    [Fact]
    public void ASongThatWantsMendingIsMarkedAndSaysWhatInWordsAndTheListCanBeNarrowedToThose() => OnAMadeUpPC(async pc =>
    {
        var model = await AtTheOldCopies(pc);
        var marks = model.Marks;
        Assert.Equal((3, "", ""), (marks[pc.Song(Plain, Old)].ShownTwice, marks[pc.Song(Plain, Old)].DoubtedKey, marks[pc.Song(Plain, Old)].LikelierKey));
        Assert.Equal(4, marks[pc.Song(Well, Old)].ShownTwice);
        Assert.Equal((6, true), (marks[pc.Song(Grace, Old)].ShownTwice, marks[pc.Song(Grace, Old)].LooksMovedLeft));
        Assert.Equal((1, "Ab", "F#m"), (marks[pc.Song(MinorKey, Old)].ShownTwice, marks[pc.Song(MinorKey, Old)].DoubtedKey, marks[pc.Song(MinorKey, Old)].LikelierKey));
        Assert.False(marks[pc.Song(Plain, Old)].LooksMovedLeft || marks[pc.Song(Well, Old)].LooksMovedLeft || marks[pc.Song(MinorKey, Old)].LooksMovedLeft);

        var rows = model.Rows.ToDictionary(row => row.Name);
        Assert.All(rows.Values, row => Assert.True(row.WantsMending && row.IsOldEditorsCopy));
        Assert.Equal("3 chords are written so that ProPresenter shows them twice.", rows[Plain].MendingTip);
        Assert.Equal("6 chords are written so that ProPresenter shows them twice.", rows[Grace].MendingTip);
        Assert.Equal(
            "1 chord is written so that ProPresenter shows it twice. It says it is in A flat, but its chords look like F sharp minor.",
            rows[MinorKey].MendingTip);
        // A screen reader is told it with the rest of the row.
        Assert.Equal(
            Plain + ". Has 11 chords. 3 chords are written so that ProPresenter shows them twice. " + LibraryModel.SongRow.OldEditorsCopyTip,
            rows[Plain].Said);

        model.Filter = MarkFilter.WantingMending;
        Assert.Equal([MinorKey, Grace, Well, Plain], Names(model.ListedSongs));
        Assert.Equal("4 SONGS", model.SongsCount);
        model.Typed = "grace";
        Assert.Equal([Grace], Names(model.ListedSongs));
        model.Typed = "";

        // In the library of clean songs nothing wants mending: not the song that states
        // no key, though the same sum says what its chords look like, and not the file
        // that cannot be read, of which nobody knows.
        model.ChooseLibrary(pc.Library("Songs"));
        await model.MarksAreWorkedOutAsync();
        Assert.Equal(MarkFilter.WantingMending, model.Filter);
        Assert.Empty(model.ListedSongs);
        Assert.Equal("0 OF 9 SONGS", model.SongsCount);
        Assert.All(model.Rows, row => Assert.False(row.WantsMending));
        model.Filter = MarkFilter.All;
        Assert.All(model.Rows, row => Assert.Equal((false, ""), (row.WantsMending, row.MendingTip)));
        Assert.False(model.OffersMendAll);
    });

    // Marks worked out by a build that knew less are not believed: a song that wants
    // mending would go unmarked until its file changed.
    [Fact]
    public void MarksKeptByAnEarlierReadingOfTheFilesAreWorkedOutAgain() => OnAMadeUpPC(async pc =>
    {
        var model = await AtTheOldCopies(pc);
        Assert.Equal(4, pc.AppData.ReadMarks().Count);
        // As a build before this one wrote them: with no number, and with none of what
        // the checks find.
        var file = Path.Combine(pc.AppData.Folder, "marks.json");
        var written = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(file))!.AsObject();
        Assert.Equal(Marks.Version, written["version"]!.GetValue<int>());
        written.Remove("version");
        foreach (var entry in written["entries"]!.AsObject())
        {
            var kept = entry.Value!["marks"]!.AsObject();
            kept.Remove("shownTwice");
            kept.Remove("doubtedKey");
            kept.Remove("likelierKey");
            kept.Remove("looksMovedLeft");
        }
        File.WriteAllText(file, written.ToJsonString());
        Assert.Equal(0, pc.AppData.ReadMarks().Count);

        var again = pc.Model();
        var started = again.StartAsync();
        Assert.Empty(again.Marks);
        await started;
        Assert.Equal(3, again.Marks[pc.Song(Plain, Old)].ShownTwice);
        Assert.Equal(4, pc.AppData.ReadMarks().Count);
    });

    // ---- Check 1, for the song that is open

    [Fact]
    public void ChordsShownTwiceAreOfferedToBeMendedAndMendedTheSongIsTheOneItWasMadeFrom() => OnAMadeUpPC(async pc =>
    {
        var model = await AtTheOldCopies(pc);
        Assert.Empty(model.Offers);
        model.Open(pc.Song(Plain, Old));
        // The page's sentence, and its button.
        Assert.Equal(["3 chords in this song are written so that ProPresenter shows them twice. [Mend]"], model.Offers.Select(Said));
        // Nothing is mended because a song was opened.
        Assert.True(Same(TestData.InFiles("as-the-old-windows-app-left-them/" + Plain + ".pro"), pc.BytesOf(Plain, Old)));
        Assert.False(model.HasEarlierVersions);

        Press(model, "Mend");
        Assert.True(Same(Clean("Plain Song"), pc.BytesOf(Plain, Old)));
        Assert.Empty(model.Offers);
        Assert.Null(model.Refused);
        Assert.Equal("Mended. 3 chords are now written so that ProPresenter shows each once.", model.Notice);
        // Its mark has gone; it stays in the list, being the song that is open.
        Assert.Equal(0, model.Marks[pc.Song(Plain, Old)].ShownTwice);
        Assert.False(model.Rows.Single(row => row.Name == Plain).WantsMending);
        model.Filter = MarkFilter.WantingMending;
        Assert.Contains(Plain, Names(model.ListedSongs));
        // A copy was kept first, and says why.
        Assert.True(model.HasEarlierVersions);
        model.ShowEarlierVersions();
        Assert.Equal(["(the first) before mending"], model.Earlier!.Copies.Select(LibraryModel.Why));
        model.CloseEarlierVersions();
        Assert.Contains("Saved " + Plain + ".pro.", pc.Log, StringComparison.Ordinal);

        // It is one step of undo, which brings the offer back with the damage.
        Assert.True(model.CanUndo);
        model.Undo();
        Assert.True(Same(TestData.InFiles("as-the-old-windows-app-left-them/" + Plain + ".pro"), pc.BytesOf(Plain, Old)));
        Assert.Single(model.Offers);
        Assert.False(model.CanUndo);
        Assert.Equal(3, model.Marks[pc.Song(Plain, Old)].ShownTwice);
    });

    [Fact]
    public void OneChordShownTwiceIsSaidAsOneAndAMendingWithTheTextShownMakesTheTextAgain() => OnAMadeUpPC(async pc =>
    {
        var model = await AtTheOldCopies(pc);
        model.Open(pc.Song(MinorKey, Old));
        model.Show(LibraryModel.Editor.ChordPro);
        var text = model.ChordPro!;
        Assert.StartsWith("1 chord in this song is written so that ProPresenter shows it twice. [Mend]", Said(model.Offers[0]), StringComparison.Ordinal);
        model.Take(OfferAction.Mend);
        Assert.Equal("Mended. 1 chord is now written so that ProPresenter shows it once.", model.Notice);
        // The text says what it said, the chords being where they were; it is made again
        // from the song all the same, so that what is typed next is held against the
        // song as it now is.
        Assert.Equal(text.Text, model.ChordPro!.Text);
        Assert.True(model.ChordPro.Version > text.Version);
        Assert.Equal(text.Sitting, model.ChordPro.Sitting);
    });

    [Fact]
    public void AMendingIsRefusedWhileProPresenterIsRunningAndTheOfferStays() => OnAMadeUpPC(async pc =>
    {
        var model = await AtTheOldCopies(pc);
        model.Open(pc.Song(Well, Old));
        var asItWas = pc.BytesOf(Well, Old);
        pc.ProPresenterIsRunning = true;
        model.Take(OfferAction.Mend);
        Assert.Equal("ProPresenter is running. Quit it to change chords here. Nothing was changed.", model.Refused);
        Assert.True(model.ProPresenterIsRunning);
        Assert.True(Same(asItWas, pc.BytesOf(Well, Old)));
        Assert.Equal(["4 chords in this song are written so that ProPresenter shows them twice. [Mend]"], model.Offers.Select(Said));
        Assert.False(model.HasEarlierVersions);
        // It is quit, and the mending is done.
        pc.ProPresenterIsRunning = false;
        model.ProPresenterMayHaveChanged();
        model.Take(OfferAction.Mend);
        Assert.True(Same(Clean("It Is Well"), pc.BytesOf(Well, Old)));
    });

    // ---- Check 3: a key that does not fit the chords

    [Fact]
    public void AKeyThatDoesNotFitTheChordsIsOfferedToBeSetAndTheOldEditorsMistakeIsSaidToBeIts() => OnAMadeUpPC(async pc =>
    {
        var model = await AtTheOldCopies(pc);
        model.Open(pc.Song(MinorKey, Old));
        Assert.Equal("Ab", model.Shown!.Key);
        // A flat, doubtful, in a song that has the old editor's mark: said to be its
        // doing.
        Assert.Equal(
            [
                "1 chord in this song is written so that ProPresenter shows it twice. [Mend]",
                "This song says it is in A flat, but its chords look like F sharp minor. The old Windows chord editor, which saved this song, wrote A flat by mistake. [Set Key to F#m]",
            ],
            model.Offers.Select(Said));
        // Mended, the song has that mark no longer, and the key is only said to look
        // unlikely: the page's sentence.
        model.Take(OfferAction.Mend);
        Assert.Equal(["This song says it is in A flat, but its chords look like F sharp minor. [Set Key to F#m]"], model.Offers.Select(Said));
        Assert.True(model.Rows.Single(row => row.Name == MinorKey).WantsMending);
        Assert.Equal("It says it is in A flat, but its chords look like F sharp minor.", model.Rows.Single(row => row.Name == MinorKey).MendingTip);

        Press(model, "Set Key to F#m");
        Assert.Equal(("F#m", "F#m"), (model.Shown!.Key, model.Shown.ShownKey));
        Assert.Empty(model.Offers);
        Assert.False(model.Rows.Single(row => row.Name == MinorKey).WantsMending);
        // And the file is then the one it was made from, to the byte.
        Assert.True(Same(Clean("A Song - In A Minor Key"), pc.BytesOf(MinorKey, Old)));
        // Each was a step of undo.
        model.Undo();
        Assert.Equal("Ab", model.Shown!.Key);
        Assert.Single(model.Offers);
    });

    [Fact]
    public void AKeyIsSaidInWordsInASentence()
    {
        Assert.Equal("A flat", LibraryModel.KeyInWords("Ab"));
        Assert.Equal("F sharp minor", LibraryModel.KeyInWords("F#m"));
        Assert.Equal("G", LibraryModel.KeyInWords("G"));
        Assert.Equal("E minor", LibraryModel.KeyInWords("Em"));
        Assert.Equal("B flat minor", LibraryModel.KeyInWords("Bbm"));
        Assert.Equal("C sharp", LibraryModel.KeyInWords("C#"));
        Assert.Equal("", LibraryModel.KeyInWords(""));
        // Every key a list of keys has reads as a note, a sign in words, and its scale.
        foreach (var key in LibraryModel.Keys)
        {
            var words = LibraryModel.KeyInWords(key);
            Assert.DoesNotContain('#', words);
            Assert.Equal(key.EndsWith('m'), words.EndsWith(" minor", StringComparison.Ordinal));
            Assert.Equal(key[0], words[0]);
        }
    }

    // A song that states no key: the same sum says which its chords look like, where
    // the sheet says that it names none.
    [Fact]
    public void OfASongThatNamesNoKeyTheSheetSaysWhichItsChordsLookLike() => OnAMadeUpPC(async pc =>
    {
        var model = pc.Model();
        await model.StartAsync();
        model.ChooseLibrary(pc.Library("Songs"));
        await model.MarksAreWorkedOutAsync();
        model.Open(pc.Song("Holy Holy Holy"));
        Assert.Equal("", model.Shown!.Key);
        Assert.Equal("The song names no key: pick the one its chords are in. They look like D.", model.NoKeySentence);
        // It is nothing to mend, and nothing is offered.
        Assert.Empty(model.Offers);
        // A song with no chords: the sum has nothing to go by, and the page's words stand
        // alone.
        model.Open(pc.Song("Rock Of Ages (Toplady)"));
        Assert.Equal("", model.Shown!.Key);
        Assert.Equal("The song names no key: pick the one its chords are in", model.NoKeySentence);
        // Given its key, the first says so no more.
        model.Open(pc.Song("Holy Holy Holy"));
        model.SetKey("D");
        Assert.Equal("The song names no key: pick the one its chords are in", model.NoKeySentence);
    });

    // ---- Check 2: chords put on by hand in the old editor

    [Fact]
    public void ChordsThatLookMovedLeftAreNotOfferedUnlessTheSettingSaysSoWhichIsOffAsTheAppComes() => OnAMadeUpPC(async pc =>
    {
        var model = await AtTheOldCopies(pc);
        Assert.False(model.OffersToMoveBack);
        model.Open(pc.Song(Grace, Old));
        Assert.True(model.Shown!.Found!.Shift.Likely);
        Assert.Equal(["6 chords in this song are written so that ProPresenter shows them twice. [Mend]"], model.Offers.Select(Said));
        // Asked for all the same, nothing is shown otherwise and nothing is moved.
        var asItWas = pc.BytesOf(Grace, Old);
        model.Take(OfferAction.ShowMoved);
        Assert.Null(model.Judging);
        // Mended, it wants nothing more that is offered, and is not marked.
        model.Take(OfferAction.Mend);
        Assert.Empty(model.Offers);
        Assert.False(model.Rows.Single(row => row.Name == Grace).WantsMending);
        model.Undo();
        Assert.True(Same(asItWas, pc.BytesOf(Grace, Old)));

        // Switched on: it is offered, after the mending, and it is remembered.
        model.SetOffersToMoveBack(true);
        Assert.Equal(
            [
                "6 chords in this song are written so that ProPresenter shows them twice. [Mend]",
                "The chords of this song " + MovedLeft + " [Show Me]",
            ],
            model.Offers.Select(Said));
        Assert.True(pc.AppData.ReadSettings().OffersToMoveBack);
        Assert.Equal(
            "6 chords are written so that ProPresenter shows them twice. Its chords " + MovedLeft,
            model.Rows.Single(row => row.Name == Grace).MendingTip);
        // A song that has nothing else to mend is then marked for this alone.
        model.Take(OfferAction.Mend);
        Assert.Equal(["The chords of this song " + MovedLeft + " [Show Me]"], model.Offers.Select(Said));
        Assert.True(model.Rows.Single(row => row.Name == Grace).WantsMending);
        model.Filter = MarkFilter.WantingMending;
        model.Open(pc.Song(Plain, Old));
        Assert.Contains(Grace, Names(model.ListedSongs));
        var again = pc.Model();
        await again.StartAsync();
        Assert.True(again.OffersToMoveBack);
        // Switched off again, the mark and the offer go, and nothing is kept of it.
        model.SetOffersToMoveBack(false);
        Assert.DoesNotContain(Grace, Names(model.ListedSongs));
        Assert.Null(pc.AppData.ReadSettings().OffersToMoveBack);
    });

    [Fact]
    public void TheSongIsShownBothWaysBeforeItsChordsAreMovedAndNothingIsChangedUntilThatIsAskedFor() => OnAMadeUpPC(async pc =>
    {
        var model = await AtTheOldCopies(pc);
        model.SetOffersToMoveBack(true);
        model.Open(pc.Song(Grace, Old));
        model.Show(LibraryModel.Editor.ChordPro);
        var asItWas = pc.BytesOf(Grace, Old);
        // "That saved a wretch like me": its chord is over "me" in the song this was
        // made from, at 25, and here one to the left, on the space before it.
        static string SecondLine(LibraryModel model) =>
            string.Join(" ", model.Sheet!.Rows.First(row => row.Text == "That saved a wretch like me").Chords.Select(chord => chord.At + ":" + chord.Name));
        Assert.Equal("24:D", SecondLine(model));

        // Show Me: on the sheet, whichever editor was shown, as it would be.
        Press(model, "Show Me");
        Assert.Equal((LibraryModel.Way.AsItWouldBe, LibraryModel.Editor.Sheet), (model.Judging, model.EditorShown));
        Assert.Equal("25:D", SecondLine(model));
        Assert.Equal(
            [
                "6 chords in this song are written so that ProPresenter shows them twice. [Mend]",
                "This is the song with those chords moved to where they look to have been meant. Nothing has been changed. [Show As It Is] [Move the Chords] [Leave Them]",
            ],
            model.Offers.Select(Said));
        Assert.True(Same(asItWas, pc.BytesOf(Grace, Old)));
        Assert.False(model.HasEarlierVersions);

        // What is shown so is only shown: a chord put on it is refused, and says why.
        var line = Enumerable.Range(0, model.Sheet!.Rows.Count).First(row => model.Sheet.Rows[row].Text == "That saved a wretch like me");
        model.Sheet.SetSpot(line, 5);
        model.PutOfKey(1);
        Assert.Equal("This is the song as it would be with its chords moved, shown to be looked at. Nothing was changed.", model.Refused);
        Assert.Equal("25:D", SecondLine(model));
        Assert.True(Same(asItWas, pc.BytesOf(Grace, Old)));
        // Nor is a chord that is being typed on it kept when the song is left.
        model.Sheet.Pick(line, 5);
        model.Sheet.Type("Am");
        Assert.True(model.Settle());
        Assert.False(model.Sheet.Typing);
        Assert.True(Same(asItWas, pc.BytesOf(Grace, Old)));

        // As it is, and as it would be again.
        Press(model, "Show As It Is");
        Assert.Equal(LibraryModel.Way.AsItIs, model.Judging);
        Assert.Equal("24:D", SecondLine(model));
        Assert.Equal("This is the song as it is. [Show Moved] [Move the Chords] [Leave Them]", Said(model.Offers[1]));
        Press(model, "Show Moved");
        Assert.Equal("25:D", SecondLine(model));

        // Leave Them: the song is shown as it is, and is as it was.
        Press(model, "Leave Them");
        Assert.Null(model.Judging);
        Assert.Equal("24:D", SecondLine(model));
        Assert.True(Same(asItWas, pc.BytesOf(Grace, Old)));
        Assert.EndsWith("[Show Me]", Said(model.Offers[1]), StringComparison.Ordinal);

        // Move the Chords: every chord below a first line is moved, and the file is then
        // the one it was made from, to the byte.
        Press(model, "Show Me");
        Press(model, "Move the Chords");
        Assert.Null(model.Judging);
        Assert.Equal("25:D", SecondLine(model));
        Assert.True(Same(Clean("Amazing Grace"), pc.BytesOf(Grace, Old)));
        Assert.Empty(model.Offers);
        Assert.Equal("12 chords were moved to where they look to have been meant. Ctrl+Z puts them back.", model.Notice);
        model.ShowEarlierVersions();
        Assert.Equal(["(the first) before mending"], model.Earlier!.Copies.Select(LibraryModel.Why));
        model.CloseEarlierVersions();
        // One step of undo.
        model.Undo();
        Assert.True(Same(asItWas, pc.BytesOf(Grace, Old)));
        Assert.Equal("24:D", SecondLine(model));
        Assert.False(model.CanUndo);
        // And from the song shown as it is, the same button moves them all the same.
        Press(model, "Show Me");
        Press(model, "Show As It Is");
        Press(model, "Move the Chords");
        Assert.True(Same(Clean("Amazing Grace"), pc.BytesOf(Grace, Old)));
        Assert.Null(model.Judging);
        model.Undo();
        // Leave Them, from the song as it is.
        Press(model, "Show Me");
        Press(model, "Show As It Is");
        Press(model, "Leave Them");
        Assert.Null(model.Judging);
        Assert.True(Same(asItWas, pc.BytesOf(Grace, Old)));
    });

    [Fact]
    public void WhateverChangesTheSongOrLeavesTheSheetShowsTheSongAsItIsAgain() => OnAMadeUpPC(async pc =>
    {
        var model = await AtTheOldCopies(pc);
        model.SetOffersToMoveBack(true);
        model.Open(pc.Song(Grace, Old));
        void ShownMoved()
        {
            model.Take(OfferAction.ShowMoved);
            Assert.Equal(LibraryModel.Way.AsItWouldBe, model.Judging);
        }

        // The other editor is shown.
        ShownMoved();
        model.Show(LibraryModel.Editor.ChordPro);
        Assert.Null(model.Judging);
        model.Show(LibraryModel.Editor.Sheet);
        Assert.Equal("24:D", string.Join(" ", model.Sheet!.Rows.First(row => row.Text == "That saved a wretch like me").Chords.Select(chord => chord.At + ":" + chord.Name)));
        // The song's key is set.
        ShownMoved();
        model.SetKey("D");
        Assert.Null(model.Judging);
        // It is mended.
        ShownMoved();
        model.Take(OfferAction.Mend);
        Assert.Null(model.Judging);
        // A change is taken back.
        ShownMoved();
        model.Undo();
        Assert.Null(model.Judging);
        // The offer is switched off.
        ShownMoved();
        model.SetOffersToMoveBack(false);
        Assert.Null(model.Judging);
        model.SetOffersToMoveBack(true);
        // Another song is opened.
        ShownMoved();
        model.Open(pc.Song(Plain, Old));
        Assert.Null(model.Judging);
        // The song is put away and none is opened: another library is chosen.
        model.Open(pc.Song(Grace, Old));
        ShownMoved();
        model.ChooseLibrary(pc.Library("Songs"));
        Assert.Null(model.Shown);
        Assert.Null(model.Judging);
        model.ChooseLibrary(pc.Library(Old));
        // And so it is where the song opened next cannot be read: there is no song.
        model.Open(pc.Song(Grace, Old));
        ShownMoved();
        File.WriteAllBytes(pc.Song(Plain, Old), [0x0A, 0xFF, 0xFF]);
        model.Open(pc.Song(Plain, Old));
        Assert.NotNull(model.Shown!.Problem);
        Assert.Null(model.Judging);
        // And in all of it the chords were never moved.
        Assert.Equal(12, new OpenSong(pc.Song(Grace, Old)).Findings.Shift.ChordsBelowTheFirstLine);
        Assert.True(new OpenSong(pc.Song(Grace, Old)).Findings.Shift.Likely);
    });

    // ---- Mend All (spec/08-repair.md, "The whole library")

    [Fact]
    public void MendAllSaysFirstHowManySongsAndChordsAndAfterwardsWhatItDidByName() => OnAMadeUpPC(async pc =>
    {
        var model = await AtTheOldCopies(pc);
        // It is offered over the list narrowed to the songs that want mending.
        Assert.False(model.OffersMendAll);
        model.AskToMendAll();
        Assert.Null(model.MendAllQuestion);
        model.Filter = MarkFilter.WantingMending;
        Assert.True(model.OffersMendAll && model.CanMendAll);
        Assert.Equal([MinorKey, Grace, Well, Plain], Names(model.SongsToMend));

        model.AskToMendAll();
        Assert.Equal(
            new LibraryModel.Question(
                "Mend 4 songs?",
                "14 chords in them are written so that ProPresenter shows them twice. Each is written again where it stands: no chord is moved, and no name is changed. A copy of each song is kept first.",
                "Mend 4 Songs"),
            model.MendAllQuestion);
        // No: nothing is done.
        model.DontMendAll();
        Assert.Null(model.MendAllQuestion);
        Assert.Null(model.MendingAll);
        Assert.Equal(3, new OpenSong(pc.Song(Plain, Old)).Findings.ShownTwice);

        // Yes.
        model.AskToMendAll();
        await model.MendAllAsync();
        Assert.Null(model.MendAllQuestion);
        var run = model.MendingAll!;
        Assert.True(run.Finished);
        Assert.Equal(
            [new(MinorKey, 1, null), new(Grace, 6, null), new(Well, 4, null), new(Plain, 3, null)],
            run.Outcomes);
        Assert.Equal("4 songs were mended: 14 chords that ProPresenter showed twice.", run.Sentence);
        // Check 1 and no other: the songs whose chords it could mend unseen are now the
        // songs they were made from, to the byte; the key of the one is as it was, and
        // the chords of the other are where they were.
        Assert.True(Same(Clean("Plain Song"), pc.BytesOf(Plain, Old)));
        Assert.True(Same(Clean("It Is Well"), pc.BytesOf(Well, Old)));
        Assert.Equal("Ab", new OpenSong(pc.Song(MinorKey, Old)).Key);
        Assert.True(new OpenSong(pc.Song(Grace, Old)).Findings.Shift.Likely);
        Assert.All(new[] { MinorKey, Grace, Well, Plain }, name => Assert.Equal(0, new OpenSong(pc.Song(name, Old)).Findings.ShownTwice));
        // The marks are worked out again, and kept: the list has only the song whose
        // key is still doubted, and there is nothing more for Mend All to do.
        Assert.Equal([MinorKey], Names(model.ListedSongs));
        Assert.False(model.OffersMendAll);
        Assert.Equal(0, pc.AppData.ReadMarks().Entries.Values.Sum(entry => entry.Marks.ShownTwice));
        // A copy of every file it changed was kept first, and says why.
        var copies = pc.AppData.Copies("test");
        foreach (var name in new[] { MinorKey, Grace, Well, Plain })
        {
            var kept = copies.CopiesOf(new KeptSong(MadeUpPC.PlaceName, Old, name));
            Assert.Equal([CopyReason.BeforeMending], kept.Select(copy => copy.Why));
            Assert.True(Same(TestData.InFiles("as-the-old-windows-app-left-them/" + name + ".pro"), File.ReadAllBytes(kept[0].Path)));
        }
        // And the list shows each of them as a song with earlier versions.
        model.Filter = MarkFilter.All;
        Assert.All(model.Rows, row => Assert.True(row.HasCopies));
        model.Filter = MarkFilter.WantingMending;
        Assert.Contains("Mend All: 4 songs.", pc.Log, StringComparison.Ordinal);
        Assert.Contains("Mended " + Grace + ".pro: 6 chords.", pc.Log, StringComparison.Ordinal);
        // (FOUND in the log of the real window's first Mend All: "1 chords".)
        Assert.Contains("Mended " + MinorKey + ".pro: 1 chord.", pc.Log, StringComparison.Ordinal);

        // What it did is read, and put away.
        model.CloseMendAll();
        Assert.Null(model.MendingAll);
    });

    [Fact]
    public void MendAllOfOneSongSaysSoAndTheSongThatIsOpenIsMendedAsTheOneThatIsOpen() => OnAMadeUpPC(async pc =>
    {
        var model = await AtTheOldCopies(pc);
        model.Filter = MarkFilter.WantingMending;
        model.Typed = "well";
        model.Open(pc.Song(Well, Old));
        model.AskToMendAll();
        Assert.Equal(
            new LibraryModel.Question(
                "Mend 1 song?",
                "4 chords in it are written so that ProPresenter shows them twice. Each is written again where it stands: no chord is moved, and no name is changed. A copy of the song is kept first.",
                "Mend 1 Song"),
            model.MendAllQuestion);
        await model.MendAllAsync();
        Assert.Equal("1 song was mended: 4 chords that ProPresenter showed twice.", model.MendingAll!.Sentence);
        // What is shown of it is the song as it now is, and its mending is a step of
        // undo there, as if its own button had been clicked.
        Assert.Empty(model.Offers);
        Assert.Equal(0, model.Shown!.Found!.ShownTwice);
        Assert.True(model.CanUndo);
        model.CloseMendAll();
        model.Undo();
        Assert.Equal(4, model.Shown!.Found!.ShownTwice);
        // The others were not in the narrowed list, and are as they were.
        Assert.Equal(3, new OpenSong(pc.Song(Plain, Old)).Findings.ShownTwice);
    });

    [Fact]
    public void MendAllIsNotBegunWhileProPresenterIsRunningAndStopsIfItIsStartedMeanwhile() => OnAMadeUpPC(async pc =>
    {
        var model = await AtTheOldCopies(pc);
        model.Filter = MarkFilter.WantingMending;
        pc.ProPresenterIsRunning = true;
        model.ProPresenterMayHaveChanged();
        Assert.True(model.OffersMendAll);
        Assert.False(model.CanMendAll);
        model.AskToMendAll();
        Assert.Null(model.MendAllQuestion);
        pc.ProPresenterIsRunning = false;
        model.ProPresenterMayHaveChanged();

        // Started when two songs have been mended: the third is refused, and says why;
        // the fourth is not tried.
        model.AskToMendAll();
        pc.IsStartedOnceAsked = pc.TimesAsked + 2;
        await model.MendAllAsync();
        var run = model.MendingAll!;
        Assert.True(run.Finished);
        Assert.Equal(
            [
                new(MinorKey, 1, null),
                new(Grace, 6, null),
                new(Well, 0, "ProPresenter is running. Quit it to change chords here. Nothing was changed."),
            ],
            run.Outcomes);
        Assert.Equal(
            "2 songs were mended: 7 chords that ProPresenter showed twice. 1 song was not mended, and is as it was. 1 more was not tried.",
            run.Sentence);
        Assert.True(model.ProPresenterIsRunning);
        Assert.Equal(4, new OpenSong(pc.Song(Well, Old)).Findings.ShownTwice);
        Assert.Equal(3, new OpenSong(pc.Song(Plain, Old)).Findings.ShownTwice);
        Assert.Contains("Did not mend " + Well + ".pro.", pc.Log, StringComparison.Ordinal);
    });

    [Fact]
    public void TheSongThatIsOpenRefusedByMendAllIsListedAsNotMendedAndSaysWhy() => OnAMadeUpPC(async pc =>
    {
        var model = await AtTheOldCopies(pc);
        model.Filter = MarkFilter.WantingMending;
        // The first of the list is the song that is open; ProPresenter is started
        // before it is mended.
        model.Open(pc.Song(MinorKey, Old));
        model.AskToMendAll();
        pc.IsStartedOnceAsked = pc.TimesAsked;
        await model.MendAllAsync();
        var run = model.MendingAll!;
        Assert.Equal([new(MinorKey, 0, "ProPresenter is running. Quit it to change chords here. Nothing was changed.")], run.Outcomes);
        Assert.Equal("No song was mended. 1 song was not mended, and is as it was. 3 more were not tried.", run.Sentence);
        Assert.Equal(1, model.Shown!.Found!.ShownTwice);
        Assert.False(model.CanUndo);
    });

    [Fact]
    public void MendAllCanBeStoppedAndNothingElseIsBegunWhileItIsAtWork() => OnAMadeUpPC(async pc =>
    {
        var model = await AtTheOldCopies(pc);
        model.Filter = MarkFilter.WantingMending;
        model.AskToMendAll();
        var run = model.MendAllAsync();
        // It is at work on the first song.
        Assert.False(run.IsCompleted);
        Assert.Equal("Mending 1 of 4" + (char)0x2026, model.MendingAll!.Sentence);
        Assert.False(model.OffersMendAll);
        // Nothing else is begun meanwhile: no song opened, no other library chosen.
        model.Open(pc.Song(Plain, Old));
        Assert.Null(model.Shown);
        model.ChooseLibrary(pc.Library("Songs"));
        Assert.Equal(Old, model.Library?.Name);
        // What it did cannot be put away before it is done.
        model.CloseMendAll();
        Assert.NotNull(model.MendingAll);

        // Stop: the song it is at is finished, and no more are begun.
        model.StopMendingAll();
        await run;
        Assert.True(model.MendingAll!.Finished);
        Assert.Equal([new(MinorKey, 1, null)], model.MendingAll.Outcomes);
        Assert.Equal("1 song was mended: 1 chord that ProPresenter showed twice. 3 more were not tried.", model.MendingAll.Sentence);
        Assert.Equal(6, new OpenSong(pc.Song(Grace, Old)).Findings.ShownTwice);
        // And then the rest can be.
        model.CloseMendAll();
        model.Open(pc.Song(Plain, Old));
        Assert.NotNull(model.Shown);
        model.AskToMendAll();
        Assert.Equal("Mend 3 songs?", model.MendAllQuestion!.Title);
    });
}
