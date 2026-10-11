// The window's model, the part of it that is about the old editor's "(Chords)" copies
// (LibraryModel.Folding.cs): what a copy says of itself when the song it was saved
// beside is in the library too, the question that says what putting its chords into
// that song will do, the doing, and what then becomes of the copy. spec/08-repair.md, D.
//
// MINE. The page has the rule and no words for it; the sentences here are this app's,
// and what they say is held to what is then true of the two files. What folding leaves
// in a file is held, as in Song.Tests/FoldingTests.cs, to the song the copy was made
// from.
//
// The made-up PC's library "Old copies" has the four copies. Each test gives it the
// songs they would have been saved beside: the same songs, with no chords on them.

using System.IO;
using Pro7ChordEditor.Song;
using Pro7ChordEditor.Tests;
using Xunit;
using OfferAction = Pro7ChordEditor.AppViews.LibraryModel.OfferAction;

namespace Pro7ChordEditor.AppViews.Tests;

public class FoldingModelTests
{
    private const string Old = "Old copies";
    private const string Plain = "Plain Song";
    private const string PlainCopy = "Plain Song (Chords)";
    private static readonly string[] Originals = MadeUpPC.OriginalsOfTheOldCopies;

    private static readonly string OpenQ = ((char)0x201C).ToString();
    private static readonly string CloseQ = ((char)0x201D).ToString();
    private static readonly string Dots = ((char)0x2026).ToString();
    private static readonly string Apostrophe = ((char)0x2019).ToString();

    private static string Q(string name) => OpenQ + name + CloseQ;

    private static void OnAMadeUpPC(Func<MadeUpPC, Task> what) => Pictures.OnAThreadOfItsOwn(async () =>
    {
        using var pc = new MadeUpPC();
        await what(pc);
    });

    private static string InOld(MadeUpPC pc, string name) => Path.Combine(pc.Place, "Libraries", Old, name + ".pro");

    private static byte[] Clean(string name) => TestData.InFiles("made-by-simple-presenter/" + name + ".pro");

    private static byte[] AsTheOldEditorLeftIt(string name) => TestData.InFiles("as-the-old-windows-app-left-them/" + name + ".pro");

    private static bool Same(byte[] one, byte[] other) => one.AsSpan().SequenceEqual(other);

    // Puts beside each copy the song it would have been saved beside (MadeUpPC.cs).
    private static void GiveTheOriginals(MadeUpPC pc, bool withTheirChords = false)
    {
        pc.GiveTheOldCopiesTheirOriginals(withTheirChords);
        Assert.All(Originals, name => Assert.Equal(withTheirChords, new OpenSong(InOld(pc, name)).UsedChords.Count > 0));
    }

    private static async Task<LibraryModel> AtTheOldCopies(MadeUpPC pc)
    {
        var model = pc.Model();
        await model.StartAsync();
        model.ChooseLibrary(pc.Library(Old));
        await model.MarksAreWorkedOutAsync();
        return model;
    }

    private static string Said(LibraryModel.Offer offer) => offer.Sentence + string.Concat(offer.Buttons.Select(button => " [" + button.Label + "]"));

    private static IEnumerable<string> Names(IEnumerable<ListedSong> songs) => songs.Select(song => song.Name);

    // ---- What a copy says of itself

    [Fact]
    public void ACopyWhoseOriginalIsBesideItSaysSoAndOffersToPutItsChordsIntoIt() => OnAMadeUpPC(async pc =>
    {
        GiveTheOriginals(pc);
        var model = await AtTheOldCopies(pc);
        Assert.Equal(8, model.Songs.Count);
        model.Open(pc.Song(PlainCopy, Old));
        Assert.Equal(
            [
                "3 chords in this song are written so that ProPresenter shows them twice. [Mend]",
                "This is a copy that the old Windows chord editor saved of " + Q(Plain) + ", which has no chords. [Put Its Chords into " + Q(Plain) + Dots + "]",
            ],
            model.Offers.Select(Said));
        // The original says nothing of the kind: it is no copy.
        model.Open(pc.Song(Plain, Old));
        Assert.Empty(model.Offers);
        // Nor is anything asked by a song being opened.
        Assert.Null(model.Asked);
    });

    [Fact]
    public void ACopyWithNoOriginalBesideItOffersNothingOfTheKind() => OnAMadeUpPC(async pc =>
    {
        var model = await AtTheOldCopies(pc);
        model.Open(pc.Song(PlainCopy, Old));
        Assert.Equal(["3 chords in this song are written so that ProPresenter shows them twice. [Mend]"], model.Offers.Select(Said));
        model.Take(OfferAction.Fold);
        Assert.Null(model.Asked);
    });

    [Fact]
    public void AnOriginalWithChordsOfItsOwnIsSaidToHaveThemAndWhereTheWordsDifferOnlyOpeningItIsOffered() => OnAMadeUpPC(async pc =>
    {
        GiveTheOriginals(pc, withTheirChords: true);
        // (Plain Song's own are not quite the copy's: it has one fewer.)
        var own = new OpenSong(InOld(pc, Plain), new Safeguards(() => false, (_, _) => { }));
        own.SetChords(own.TextBoxes[0].Slide, own.TextBoxes[0].Element, [.. own.TextBoxes[0].Chords.Skip(1)]);
        // And one whose words are another song's.
        File.WriteAllBytes(InOld(pc, "It Is Well"), Clean("Holy Holy Holy"));
        var model = await AtTheOldCopies(pc);

        model.Open(pc.Song(PlainCopy, Old));
        Assert.Equal(
            "This is a copy that the old Windows chord editor saved of " + Q(Plain) + ", which has 10 chords of its own. [Put Its Chords into " + Q(Plain) + Dots + "]",
            Said(model.Offers[^1]));
        model.Take(OfferAction.Fold);
        Assert.Equal(
            Q(Plain) + " is given the 11 chords of " + Q(PlainCopy) + ". Its own 10 chords are replaced by them. The words of the two are the same, and are not changed. A copy of each file is kept first.",
            model.FoldQuestion!.Message);
        model.Answer(false);
        // (An original that has the copy's chords already, chord for chord, is one into
        // which the copy was put before: Amazing Grace's is not that, its copy's chords
        // being a character to the left; It Is Well's would be, had it its own words.)
        model.Open(pc.Song("Amazing Grace (Chords)", Old));
        Assert.EndsWith(", which has 40 chords of its own. [Put Its Chords into " + Q("Amazing Grace") + Dots + "]", Said(model.Offers[^1]), StringComparison.Ordinal);

        model.Open(pc.Song("It Is Well (Chords)", Old));
        Assert.Equal(
            "This is a copy that the old Windows chord editor saved of " + Q("It Is Well")
                + ". The words of the two are not the same, so its chords cannot be put into that song from here. [Open " + Q("It Is Well") + "]",
            Said(model.Offers[^1]));
        // Asked for all the same, nothing is asked and nothing is done.
        model.Take(OfferAction.Fold);
        Assert.Null(model.Asked);
        model.Fold();
        Assert.True(Same(Clean("Holy Holy Holy"), File.ReadAllBytes(InOld(pc, "It Is Well"))));
        // Open: the other is opened.
        model.Take(OfferAction.OpenOriginal);
        Assert.Equal(pc.Song("It Is Well", Old), model.SongId);
        Assert.Equal("It Is Well", model.Shown!.Name);
    });

    // (Two things that nothing else held, found by breaking the rules to see; and a
    // third while writing them down.)
    [Fact]
    public void AnOriginalsOneChordIsSaidAsOneAndAYesThatCanDoNothingByThenPutsTheQuestionAway() => OnAMadeUpPC(async pc =>
    {
        GiveTheOriginals(pc);
        var own = new OpenSong(InOld(pc, Plain), new Safeguards(() => false, (_, _) => { }));
        var box = own.TextBoxes.First(one => one.Words.Any(char.IsLetter));
        own.SetChords(box.Slide, box.Element, [new Pro7ChordEditor.ChordRules.Chord(0, "Am")]);
        var withOne = File.ReadAllBytes(InOld(pc, Plain));
        var model = await AtTheOldCopies(pc);
        model.Open(pc.Song(PlainCopy, Old));
        Assert.Equal(
            "This is a copy that the old Windows chord editor saved of " + Q(Plain) + ", which has 1 chord of its own. [Put Its Chords into " + Q(Plain) + Dots + "]",
            Said(model.Offers[^1]));
        model.Take(OfferAction.Fold);
        Assert.Equal(
            Q(Plain) + " is given the 11 chords of " + Q(PlainCopy) + ". Its own chord is replaced by them. The words of the two are the same, and are not changed. A copy of each file is kept first.",
            model.FoldQuestion!.Message);
        model.Answer(false);

        // Something is being typed in the copy as the question is asked, and ProPresenter
        // is started before the answer. Yes can do nothing now, since what was typed
        // cannot be saved: the question is put away all the same, the copy is still the
        // song that is open, and why is said.
        model.Show(LibraryModel.Editor.ChordPro);
        var text = model.ChordPro!.Text;
        Assert.True(model.TextMayBecome(ChordProModelTests.With("One two", "One [A]two", model)));
        model.Take(OfferAction.Fold);
        Assert.NotNull(model.FoldQuestion);
        pc.ProPresenterIsRunning = true;
        model.Answer(true);
        Assert.Null(model.FoldQuestion);
        Assert.Null(model.Asked);
        Assert.Equal(pc.Song(PlainCopy, Old), model.SongId);
        Assert.Equal("ProPresenter is running. Quit it to change chords here. Nothing was changed.", model.Refused);
        Assert.True(Same(withOne, File.ReadAllBytes(InOld(pc, Plain))));
        Assert.True(Same(AsTheOldEditorLeftIt(PlainCopy), File.ReadAllBytes(InOld(pc, PlainCopy))));
        // (What was typed and could not be saved is not left in the text.)
        Assert.Equal(text, model.ChordPro!.Text);
        pc.ProPresenterIsRunning = false;

        // While the question stands, something else makes the other song another
        // song's words. Yes opens it, and the song itself refuses, and says why.
        // Nothing is changed, and no copy of either file was kept for it.
        model.Take(OfferAction.Fold);
        File.WriteAllBytes(InOld(pc, Plain), Clean("Holy Holy Holy"));
        model.Answer(true);
        Assert.Null(model.Asked);
        Assert.Equal(pc.Song(Plain, Old), model.SongId);
        Assert.Equal("The two songs have not the same words, so the chords of the one cannot be put into the other. Nothing was changed.", model.Refused);
        Assert.True(Same(Clean("Holy Holy Holy"), File.ReadAllBytes(InOld(pc, Plain))));
        Assert.True(Same(AsTheOldEditorLeftIt(PlainCopy), File.ReadAllBytes(InOld(pc, PlainCopy))));
        Assert.Empty(pc.AppData.Copies().Songs());
    });

    // ---- Putting the chords in

    [Fact]
    public void WhatIsAboutToBeDoneIsSaidFirstAndThenTheOriginalHasTheChordsAndIsTheSongThatIsOpen() => OnAMadeUpPC(async pc =>
    {
        GiveTheOriginals(pc);
        var bare = File.ReadAllBytes(InOld(pc, Plain));
        var model = await AtTheOldCopies(pc);
        model.Open(pc.Song(PlainCopy, Old));

        model.Take(OfferAction.Fold);
        Assert.Equal(
            new LibraryModel.Question(
                "Put this copy" + Apostrophe + "s chords into " + Q(Plain) + "?",
                Q(Plain) + " is given the 11 chords of " + Q(PlainCopy) + ". The words of the two are the same, and are not changed. A copy of each file is kept first.",
                "Put the Chords In"),
            model.FoldQuestion);
        Assert.Equal("Cancel", model.Asked!.No);
        // Cancel: nothing is done, and the copy is still the song that is open.
        model.Answer(false);
        Assert.Null(model.Asked);
        Assert.Equal(pc.Song(PlainCopy, Old), model.SongId);
        Assert.True(Same(bare, File.ReadAllBytes(InOld(pc, Plain))));
        Assert.Empty(pc.AppData.Copies().Songs());

        // Yes.
        model.Take(OfferAction.Fold);
        model.Answer(true);
        // The original is the song that is open, with the copy's chords in it, mended
        // on the way: it is the song the copy was made from, to the byte.
        Assert.Equal(pc.Song(Plain, Old), model.SongId);
        Assert.Equal((Plain, 11, "G"), (model.Shown!.Name, model.Shown.Chords, model.Shown.Key));
        Assert.True(Same(Clean(Plain), File.ReadAllBytes(InOld(pc, Plain))));
        Assert.Null(model.Refused);
        Assert.Equal(Q(Plain) + " has the chords of its copy now. Ctrl+Z takes them out again.", model.Notice);
        // The copy's own file is as it was.
        Assert.True(Same(AsTheOldEditorLeftIt(PlainCopy), File.ReadAllBytes(InOld(pc, PlainCopy))));
        // A copy of each of the two files was kept first, and says why.
        var copies = pc.AppData.Copies();
        var ofTheOriginal = copies.CopiesOf(new KeptSong(MadeUpPC.PlaceName, Old, Plain));
        var ofTheCopy = copies.CopiesOf(new KeptSong(MadeUpPC.PlaceName, Old, PlainCopy));
        Assert.Equal([CopyReason.BeforeMending], ofTheOriginal.Select(copy => copy.Why));
        Assert.Equal([CopyReason.BeforeMending], ofTheCopy.Select(copy => copy.Why));
        Assert.True(Same(bare, File.ReadAllBytes(ofTheOriginal[0].Path)));
        Assert.True(Same(AsTheOldEditorLeftIt(PlainCopy), File.ReadAllBytes(ofTheCopy[0].Path)));
        // Its marks are worked out again: it has chords now, and wants no mending.
        Assert.Equal((11, 0), (model.Marks[pc.Song(Plain, Old)].Chords, model.Marks[pc.Song(Plain, Old)].ShownTwice));
        Assert.Contains("Put the chords of " + PlainCopy + ".pro into " + Plain + ".pro.", pc.Log, StringComparison.Ordinal);

        // And what is to become of the copy is asked: to leave it is what Enter gives.
        Assert.Equal(
            new LibraryModel.Question(
                "What of the copy, " + Q(PlainCopy) + "?",
                "It can be left in the library as it is, or moved out of it. Moved out, it is kept with this app" + Apostrophe
                    + "s copies of songs, which Settings lists under Backups, and ProPresenter no longer has it.",
                "Move It Out",
                "Leave It"),
            model.CopyQuestion);
        Assert.Same(model.CopyQuestion, model.Asked);
        model.Answer(false);
        Assert.Null(model.Asked);
        Assert.Contains(PlainCopy, Names(model.Songs));
        Assert.True(File.Exists(InOld(pc, PlainCopy)));

        // It was one step of undo in the original, which takes the chords out again.
        Assert.True(model.CanUndo);
        model.Undo();
        Assert.True(Same(bare, File.ReadAllBytes(InOld(pc, Plain))));
        Assert.False(model.CanUndo);
        Assert.Equal(0, model.Shown!.Chords);
    });

    [Fact]
    public void TheCopysKeyBecomesTheSongsAndTheQuestionSaysSo() => OnAMadeUpPC(async pc =>
    {
        GiveTheOriginals(pc);
        var model = await AtTheOldCopies(pc);
        // The copy whose key the old editor reset to A flat, beside a song in F sharp minor.
        model.Open(pc.Song("A Song - In A Minor Key (Chords)", Old));
        model.Take(OfferAction.Fold);
        Assert.Equal(
            Q("A Song - In A Minor Key") + " is given the 9 chords of " + Q("A Song - In A Minor Key (Chords)")
                + ", and its key, A flat, in place of F sharp minor. A flat does not look like the key of those chords, which look like F sharp minor: once they are in, the song offers to set it."
                + " The words of the two are the same, and are not changed. A copy of each file is kept first.",
            model.FoldQuestion!.Message);
        model.Answer(true);
        model.Answer(false);
        // What was wrong with the copy's key is now wrong with the song's, and is
        // offered to be put right there, as it would have been in the copy.
        Assert.Equal("Ab", model.Shown!.Key);
        Assert.Equal(["This song says it is in A flat, but its chords look like F sharp minor. [Set Key to F#m]"], model.Offers.Select(Said));
        model.Take(OfferAction.SetKey);
        Assert.True(Same(Clean("A Song - In A Minor Key"), File.ReadAllBytes(InOld(pc, "A Song - In A Minor Key"))));

        // A copy that states no key, beside a song that states one: the song would be
        // left with none, and the question says so. (Holy Holy Holy states no key, and
        // is the copy here; the song beside it is the same with no chords, in G.)
        static OpenSong Plainly(string path) => new(path, new Safeguards(() => false, (_, _) => { }));
        const string Holy = "Holy Holy Holy";
        File.WriteAllBytes(InOld(pc, Holy + " (Chords)"), Clean(Holy));
        File.WriteAllBytes(InOld(pc, Holy), Clean(Holy));
        var beside = Plainly(InOld(pc, Holy));
        foreach (var box in beside.TextBoxes.Where(box => box.Chords.Count > 0).ToList())
        {
            beside.SetChords(box.Slide, box.Element, []);
        }
        var keyless = File.ReadAllBytes(InOld(pc, Holy));
        beside.SetKey("G");
        await model.RefreshAsync();
        model.Open(pc.Song(Holy + " (Chords)", Old));
        model.Take(OfferAction.Fold);
        Assert.Equal(
            Q(Holy) + " is given the 22 chords of " + Q(Holy + " (Chords)")
                + ", and is left with no key, as the copy has none. The words of the two are the same, and are not changed. A copy of each file is kept first.",
            model.FoldQuestion!.Message);
        model.Answer(false);
        // And a song that states none is given the copy's, which is all that is said.
        Plainly(InOld(pc, Holy + " (Chords)")).SetKey("D");
        File.WriteAllBytes(InOld(pc, Holy), keyless);
        await model.RefreshAsync();
        model.Open(pc.Song(Holy + " (Chords)", Old));
        model.Take(OfferAction.Fold);
        Assert.Equal(
            Q(Holy) + " is given the 22 chords of " + Q(Holy + " (Chords)")
                + ", and its key, D. The words of the two are the same, and are not changed. A copy of each file is kept first.",
            model.FoldQuestion!.Message);
        model.Answer(true);
        Assert.Equal((Holy, "D", 22), (model.Shown!.Name, model.Shown.Key, model.Shown.Chords));
    });

    // ---- What becomes of the copy

    [Fact]
    public void TheCopyMovedOutIsKeptAmongTheAppsCopiesAndIsInTheLibraryNoLonger() => OnAMadeUpPC(async pc =>
    {
        GiveTheOriginals(pc);
        var model = await AtTheOldCopies(pc);
        model.Open(pc.Song(PlainCopy, Old));
        model.Take(OfferAction.Fold);
        model.Answer(true);
        Assert.NotNull(model.CopyQuestion);

        // Move It Out.
        model.Answer(true);
        Assert.Null(model.Asked);
        Assert.Null(model.Refused);
        Assert.False(File.Exists(InOld(pc, PlainCopy)));
        Assert.DoesNotContain(PlainCopy, Names(model.Songs));
        Assert.Equal(7, model.Songs.Count);
        // It is kept, to the byte, where the app keeps copies of songs, under its own
        // name; and it is the only file that went.
        var kept = pc.AppData.Copies().CopiesOf(new KeptSong(MadeUpPC.PlaceName, Old, PlainCopy));
        Assert.Single(kept);
        Assert.True(Same(AsTheOldEditorLeftIt(PlainCopy), File.ReadAllBytes(kept[0].Path)));
        Assert.Equal(7, Directory.GetFiles(Path.Combine(pc.Place, "Libraries", Old), "*.pro").Length);
        Assert.Equal(Q(PlainCopy) + " was moved out of the library. It is kept with this app" + Apostrophe + "s copies of songs.", model.Notice);
        Assert.Contains("Moved " + PlainCopy + ".pro out of the library, into the copies kept.", pc.Log, StringComparison.Ordinal);
        // The song that is open is the original still, with the chords, and what was
        // done to it can still be undone.
        Assert.Equal(pc.Song(Plain, Old), model.SongId);
        Assert.Equal(11, model.Shown!.Chords);
        Assert.True(model.CanUndo);
        Assert.True(Same(Clean(Plain), File.ReadAllBytes(InOld(pc, Plain))));
    });

    [Fact]
    public void ACopyLeftSaysWhenItIsOpenedAgainThatItsOriginalHasItsChordsAndCanBeMovedOutThen() => OnAMadeUpPC(async pc =>
    {
        GiveTheOriginals(pc);
        var model = await AtTheOldCopies(pc);
        model.Open(pc.Song(PlainCopy, Old));
        model.Take(OfferAction.Fold);
        model.Answer(true);
        model.Answer(false);

        model.Open(pc.Song(PlainCopy, Old));
        Assert.Equal(
            "This is a copy that the old Windows chord editor saved of " + Q(Plain) + ", which has its chords and its key already. [Move This Copy Out of the Library" + Dots + "]",
            Said(model.Offers[^1]));
        // There is nothing left to put in: asked for, nothing is asked.
        model.Take(OfferAction.Fold);
        Assert.Null(model.Asked);
        // The moving out asks, as it is asked after a folding.
        model.Take(OfferAction.MoveCopyOut);
        Assert.Equal(("What of the copy, " + Q(PlainCopy) + "?", "Move It Out", "Leave It"), (model.CopyQuestion!.Title, model.CopyQuestion.Yes, model.CopyQuestion.No));
        model.Answer(false);
        Assert.True(File.Exists(InOld(pc, PlainCopy)));
        model.Take(OfferAction.MoveCopyOut);
        model.Answer(true);
        // The copy was the song that is open: with its file gone, no song is.
        Assert.False(File.Exists(InOld(pc, PlainCopy)));
        Assert.Null(model.SongId);
        Assert.Null(model.Shown);
        Assert.DoesNotContain(PlainCopy, Names(model.Songs));
        Assert.Single(pc.AppData.Copies().CopiesOf(new KeptSong(MadeUpPC.PlaceName, Old, PlainCopy)));
    });

    [Fact]
    public void NothingIsPutInAndNothingMovedOutWhileProPresenterIsRunning() => OnAMadeUpPC(async pc =>
    {
        GiveTheOriginals(pc);
        var bare = File.ReadAllBytes(InOld(pc, Plain));
        var model = await AtTheOldCopies(pc);
        model.Open(pc.Song(PlainCopy, Old));
        model.Take(OfferAction.Fold);
        // It is started while the question is being read.
        pc.ProPresenterIsRunning = true;
        model.Answer(true);
        Assert.Equal("ProPresenter is running. Quit it to change chords here. Nothing was changed.", model.Refused);
        Assert.True(Same(bare, File.ReadAllBytes(InOld(pc, Plain))));
        Assert.Null(model.CopyQuestion);
        Assert.Empty(pc.AppData.Copies().Songs());

        // Quit, and done; and started again before the copy is moved out.
        pc.ProPresenterIsRunning = false;
        model.ProPresenterMayHaveChanged();
        model.Open(pc.Song(PlainCopy, Old));
        model.Take(OfferAction.Fold);
        model.Answer(true);
        Assert.NotNull(model.CopyQuestion);
        pc.ProPresenterIsRunning = true;
        model.Answer(true);
        Assert.Equal("ProPresenter is running. Quit it, and the copy can be moved out. Nothing was changed.", model.Refused);
        Assert.True(File.Exists(InOld(pc, PlainCopy)));
        Assert.Contains(PlainCopy, Names(model.Songs));
        Assert.True(model.ProPresenterIsRunning);
    });

    [Fact]
    public void EveryCopyOfTheLibraryCanBeFoldedIntoItsOriginalOneAfterAnother() => OnAMadeUpPC(async pc =>
    {
        GiveTheOriginals(pc);
        var model = await AtTheOldCopies(pc);
        foreach (var name in Originals)
        {
            model.Open(pc.Song(name + " (Chords)", Old));
            model.Take(OfferAction.Fold);
            model.Answer(true);
            Assert.Null(model.Refused);
            model.Answer(true);
            Assert.Null(model.Refused);
        }
        Assert.Equal(Originals, Names(model.Songs));
        // Each is the song its copy was made from, where the old editor had left nothing
        // in the copy but its ranges; and has what else it left, where it had.
        Assert.True(Same(Clean("Plain Song"), File.ReadAllBytes(InOld(pc, "Plain Song"))));
        Assert.True(Same(Clean("It Is Well"), File.ReadAllBytes(InOld(pc, "It Is Well"))));
        Assert.Equal("Ab", new OpenSong(InOld(pc, "A Song - In A Minor Key")).Key);
        Assert.True(new OpenSong(InOld(pc, "Amazing Grace")).Findings.Shift.Likely);
        Assert.All(Originals, name => Assert.Equal(0, new OpenSong(InOld(pc, name)).Findings.ShownTwice));
        // Four copies moved out, each kept.
        Assert.All(Originals, name => Assert.Single(pc.AppData.Copies().CopiesOf(new KeptSong(MadeUpPC.PlaceName, Old, name + " (Chords)"))));
    });
}
