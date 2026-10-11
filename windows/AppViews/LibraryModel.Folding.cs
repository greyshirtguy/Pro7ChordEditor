// The window's model, the part of it that is about the "(Chords)" copies the first
// Windows editor saved: putting such a copy's chords and key into the song it was saved
// beside, and what then becomes of the copy. spec/08-repair.md, D; the rules of it are
// in the layer below (Song: Folding.cs, and OpenSong.FoldIn).
//
// NEW: neither the reference nor, yet, the Mac app has any of this. The page has the
// rule and no sentence; every sentence here is this app's own, and is listed in
// NOTES.md for the maintainer to read and for the Mac to have the same.
//
// "This is the largest thing the app does to a library in one go. Build it last, show
// what it is about to do before it does it" (the page). So:
//
//   1. A copy that is open, whose original is beside it in the library with the same
//      words, says so under its header, with a button.
//   2. The button asks, over the window, in words that say what will be given to which
//      song, what of that song's own will be replaced, and that a copy of each of the
//      two files is kept first. Cancel is what Enter gives.
//   3. Yes: a copy of the copy's file is kept; the original is opened in its place, and
//      is given the chords and the key, as one step of undo there, with a copy of its
//      own file kept first. What is then seen is the original, with the chords in it.
//   4. And the person is asked what is to become of the copy: left where it is, which is
//      what Enter gives, or moved out of the library. Moved out, its file is kept among
//      the app's copies of songs (it is there already, from step 3, and that is made
//      sure of to the byte before anything is taken away) and is then taken from the
//      library's folder. It is never simply deleted.
//   5. A copy that was left, opened again, says that its original has its chords
//      already, and offers the moving out, which asks as in step 4. So "leave it" can
//      be thought better of on another day.
//
// Where the words of the two are not the same, nothing is offered but to open the other.

using System.IO;
using Pro7ChordEditor.Song;

namespace Pro7ChordEditor.AppViews;

public sealed partial class LibraryModel
{
    // The copy whose chords were just put into its original, while what is to become of
    // it is being asked.
    private ListedSong? foldedCopy;

    // What folding the open song into its original would do, and for which two files, as
    // they were when it was worked out.
    private (string For, ListedSong Copy, ListedSong Original, FoldPlan Plan)? planned;

    /// <summary>What is asked before a copy's chords are put into its original: what is about to be done, in words.</summary>
    public Question? FoldQuestion { get; private set; }

    /// <summary>What is asked afterwards, of the copy: whether it is to be moved out of the library.</summary>
    public Question? CopyQuestion { get; private set; }

    /// <summary>The question that is over the window now, whichever it is: Revert's, or one of these two.</summary>
    public Question? Asked => RevertQuestion ?? FoldQuestion ?? CopyQuestion;

    /// <summary>The question was answered: with its own button, or with the other, which Esc and a click outside also give.</summary>
    public void Answer(bool yes)
    {
        if (RevertQuestion is not null)
        {
            if (yes)
            {
                Revert();
            }
            else
            {
                DontRevert();
            }
        }
        else if (FoldQuestion is not null)
        {
            if (yes)
            {
                Fold();
            }
            else
            {
                FoldQuestion = null;
                Changed();
            }
        }
        else if (CopyQuestion is not null)
        {
            if (yes)
            {
                MoveTheCopyOut();
            }
            else
            {
                LeaveTheCopy();
            }
        }
    }

    // The song that is open, if it is one of the old editor's copies and the song it was
    // saved beside is in the library too: the two, and what folding would do. Read from
    // the two files once, and again only when either has changed.
    private (ListedSong Copy, ListedSong Original, FoldPlan Plan)? Foldable
    {
        get
        {
            if (openSong is null
                || Songs.FirstOrDefault(song => Paths.Same(song.Id, SongId)) is not { } copy
                || Folding.OriginalOf(copy, Songs) is not { } original)
            {
                return null;
            }
            var now = string.Join('|', copy.Id, copy.Size, copy.Modified.Ticks, original.Id, original.Size, original.Modified.Ticks);
            if (planned is not { } known || !string.Equals(known.For, now, StringComparison.Ordinal))
            {
                try
                {
                    planned = (now, copy, original, Folding.Plan(copy.Path, original.Path));
                }
                catch (Refusal)
                {
                    // (One of the two cannot be read just now: nothing is offered.)
                    planned = null;
                    return null;
                }
            }
            return planned is { } plan ? (plan.Copy, plan.Original, plan.Plan) : null;
        }
    }

    // What is said of a copy under its header, and offered.
    private Offer? FoldOffer
    {
        get
        {
            if (Foldable is not { } pair || pair.Plan.CopysChords == 0)
            {
                return null;
            }
            var original = Quoted(pair.Original.Name);
            if (!pair.Plan.SameWords)
            {
                return new Offer(
                    "This is a copy that the old Windows chord editor saved of " + original
                        + ". The words of the two are not the same, so its chords cannot be put into that song from here.",
                    [new OfferButton("Open " + original, OfferAction.OpenOriginal)]);
            }
            if (pair.Plan.NothingToDo)
            {
                return new Offer(
                    "This is a copy that the old Windows chord editor saved of " + original + ", which has its chords and its key already.",
                    [new OfferButton("Move This Copy Out of the Library" + (char)0x2026, OfferAction.MoveCopyOut)]);
            }
            var has = pair.Plan.OriginalsChords switch
            {
                0 => "which has no chords",
                1 => "which has 1 chord of its own",
                _ => "which has " + Number(pair.Plan.OriginalsChords) + " chords of its own",
            };
            return new Offer(
                "This is a copy that the old Windows chord editor saved of " + original + ", " + has + ".",
                [new OfferButton("Put Its Chords into " + original + (char)0x2026, OfferAction.Fold)]);
        }
    }

    /// <summary>"Put Its Chords into ...": asks first, saying what is about to be done.</summary>
    public void AskToFold()
    {
        if (Foldable is not { Plan: { SameWords: true, CopysChords: > 0, NothingToDo: false } plan } pair)
        {
            return;
        }
        var original = Quoted(pair.Original.Name);
        var message = original + " is given the " + (plan.CopysChords == 1 ? "1 chord" : Number(plan.CopysChords) + " chords") + " of " + Quoted(pair.Copy.Name);
        if (!string.Equals(plan.CopysKey, plan.OriginalsKey, StringComparison.Ordinal))
        {
            message += plan.CopysKey.Length == 0
                ? ", and is left with no key, as the copy has none"
                : ", and its key, " + KeyInWords(plan.CopysKey) + (plan.OriginalsKey.Length == 0 ? "" : ", in place of " + KeyInWords(plan.OriginalsKey));
        }
        message += ".";
        if (plan.OriginalsChords > 0)
        {
            message += " Its own " + (plan.OriginalsChords == 1 ? "chord is" : Number(plan.OriginalsChords) + " chords are") + " replaced by them.";
        }
        // (The page has the song given the copy's key, whatever it is. That editor wrote
        // a wrong one into some of its copies, and the song beside such a copy may well
        // have the right one still: so where the copy's key does not look like its
        // chords' it is said here, before it is made the song's. NOTES.md has it for the
        // maintainer: whether such a song should not rather keep its own.)
        if (plan.CopysLikelierKey.Length > 0 && plan.CopysKey.Length > 0)
        {
            message += " " + KeyInWords(plan.CopysKey) + " does not look like the key of those chords, which look like " + KeyInWords(plan.CopysLikelierKey)
                + ": once they are in, the song offers to set it.";
        }
        message += " The words of the two are the same, and are not changed. A copy of each file is kept first.";
        FoldQuestion = new Question("Put this copy" + (char)0x2019 + "s chords into " + original + "?", message, "Put the Chords In");
        Changed();
    }

    /// <summary>
    /// The answer was yes: the original is opened, and is given the copy's chords and
    /// key as one step of undo, with a copy of each of the two files kept first. Then
    /// what is to become of the copy is asked.
    /// </summary>
    public void Fold()
    {
        FoldQuestion = null;
        if (Foldable is not { Plan.SameWords: true } pair || KeptSongOf(pair.Copy) is not { } keptAs || !Settle())
        {
            Changed();
            return;
        }
        byte[] chorded;
        try
        {
            chorded = File.ReadAllBytes(pair.Copy.Path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Refused = Quoted(pair.Copy.Name) + " could not be read: " + exception.Message;
            Changed();
            return;
        }
        Open(pair.Original.Id);
        if (openSong is null)
        {
            // (The original could not be read after all. What is shown says so.)
            return;
        }
        var was = openSong.Bytes;
        Change(song => song.FoldIn(chorded, () => copies.Keep(pair.Copy.Path, keptAs, CopyReason.BeforeMending)));
        if (Refused is not null || was.Span.SequenceEqual(openSong.Bytes.Span))
        {
            return;
        }
        TheSongChangedUnderTheText();
        appData.Log("Put the chords of " + Path.GetFileName(pair.Copy.Path) + " into " + Path.GetFileName(pair.Original.Path) + ".");
        Say(Quoted(pair.Original.Name) + " has the chords of its copy now. Ctrl+Z takes them out again.");
        AskAboutTheCopy(pair.Copy);
    }

    /// <summary>
    /// "Move This Copy Out of the Library...", of a copy whose original has its chords
    /// already: asks first, as is asked after a folding.
    /// </summary>
    public void AskToMoveTheCopyOut()
    {
        if (Foldable is { Plan.NothingToDo: true } pair)
        {
            AskAboutTheCopy(pair.Copy);
        }
    }

    private void AskAboutTheCopy(ListedSong copy)
    {
        foldedCopy = copy;
        CopyQuestion = new Question(
            "What of the copy, " + Quoted(copy.Name) + "?",
            "It can be left in the library as it is, or moved out of it. Moved out, it is kept with this app" + (char)0x2019
                + "s copies of songs, which Settings lists under Backups, and ProPresenter no longer has it.",
            "Move It Out",
            "Leave It");
        Changed();
    }

    /// <summary>The copy is left in the library, as it is.</summary>
    public void LeaveTheCopy()
    {
        foldedCopy = null;
        if (CopyQuestion is not null)
        {
            CopyQuestion = null;
            Changed();
        }
    }

    /// <summary>
    /// The copy is moved out of the library, into the app's copies of songs. Its file is
    /// taken from the library's folder only once a copy of it is known to be kept that
    /// is the file to the byte; if that cannot be made sure of, or ProPresenter is
    /// running, it is left where it is and the quiet line says why.
    /// </summary>
    public void MoveTheCopyOut()
    {
        CopyQuestion = null;
        var copy = foldedCopy;
        foldedCopy = null;
        // Looked at before it is taken away: it is a file of this library, listed as one
        // of its songs, and is named as that editor named its copies.
        if (copy is null
            || KeptSongOf(copy) is not { } keptAs
            || !Songs.Any(song => Paths.Same(song.Id, copy.Id))
            || Folding.OriginalsName(copy.Name) is null
            || !File.Exists(copy.Path)
            || !Settle())
        {
            Changed();
            return;
        }
        Refused = null;
        if (askWhetherProPresenterIsRunning())
        {
            ProPresenterIs(true);
            Refused = "ProPresenter is running. Quit it, and the copy can be moved out. Nothing was changed.";
            refusedForProPresenter = true;
            Changed();
            return;
        }
        try
        {
            // (No second copy is made if the newest kept is already the file.)
            copies.Keep(copy.Path, keptAs, CopyReason.BeforeMending);
            var file = File.ReadAllBytes(copy.Path);
            if (copies.CopiesOf(keptAs).FirstOrDefault() is not { } newest || !File.ReadAllBytes(newest.Path).AsSpan().SequenceEqual(file))
            {
                throw new IOException("no copy of it could be found that is the same as the file.");
            }
            File.Delete(copy.Path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or Copies.CannotBeKept)
        {
            Refused = Quoted(copy.Name) + " was not moved out, and is where it was: " + exception.Message;
            appData.Log("Could not move " + Path.GetFileName(copy.Path) + " out of the library.");
            Changed();
            return;
        }
        appData.Log("Moved " + Path.GetFileName(copy.Path) + " out of the library, into the copies kept.");
        ListSongs();
        Say(Quoted(copy.Name) + " was moved out of the library. It is kept with this app" + (char)0x2019 + "s copies of songs.");
    }

    // A name between the quotation marks that curl.
    private static string Quoted(string name) => (char)0x201C + name + (char)0x201D;
}
