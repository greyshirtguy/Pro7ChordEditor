// The window's model, the part of it that is about mending what the first Windows
// editor left in songs: what is found in the song that is open and what is offered for
// it, the song shown both ways before its chords are moved, and "Mend All" for the songs
// of the list. spec/08-repair.md is the page; the checks themselves, and what a mending
// does to a file, are in the layer below (Song: Checks.cs, OpenSong.cs, Marks.cs).
//
// NEW: neither the reference nor, yet, the Mac app has any of this. Every sentence here
// is this app's own unless the page gives it, and each that is not the page's is listed
// in NOTES.md for the maintainer to read and for the Mac to have the same.
//
// What is offered, and how freely:
//
//   - Check 1, chords that ProPresenter shows twice: offered with a button that does it,
//     for the song that is open, and for every song of the narrowed list at once. It is
//     the one mending that needs nobody's eyes: it moves no chord and changes no name.
//   - Check 3, a key that does not fit the chords: offered with a button, for the song
//     that is open and no other. The sum cannot know; the person does.
//   - Check 2, chords that look as if they were put on by hand in that editor: offered
//     only where a setting says so, which is off as the app comes (the page: that
//     editor's fault has not been seen in a real file yet), and then only by showing the
//     song both ways first, as it is and as it would be.
//
// Nothing is ever mended because a song was opened.

using System.IO;
using Pro7ChordEditor.Song;

namespace Pro7ChordEditor.AppViews;

public sealed partial class LibraryModel
{
    /// <summary>What a button of an offer does.</summary>
    public enum OfferAction
    {
        /// <summary>Check 1: the song's chords are written again where they are.</summary>
        Mend,

        /// <summary>Check 3: the song's key is set to the one its chords look like.</summary>
        SetKey,

        /// <summary>Check 2: the song is shown as it would be with its chords moved.</summary>
        ShowMoved,

        /// <summary>Check 2: the song is shown as it is again, the offer still open.</summary>
        ShowAsItIs,

        /// <summary>Check 2, taken: the chords are moved.</summary>
        MoveBack,

        /// <summary>Check 2, not taken: the song is left as it is.</summary>
        Leave,

        /// <summary>A "(Chords)" copy: its chords are put into the song it was saved beside, once what that will do has been read (LibraryModel.Folding.cs).</summary>
        Fold,

        /// <summary>A "(Chords)" copy: the song it was saved beside is opened.</summary>
        OpenOriginal,

        /// <summary>A "(Chords)" copy whose original has its chords already: it is moved out of the library, once that has been asked.</summary>
        MoveCopyOut,
    }

    public sealed record OfferButton(string Label, OfferAction Action);

    /// <summary>One thing found in the song that is open: a sentence, and what can be done about it.</summary>
    public sealed record Offer(string Sentence, IReadOnlyList<OfferButton> Buttons);

    /// <summary>The two ways a song is shown while check 2's offer is being judged.</summary>
    public enum Way
    {
        AsItIs,
        AsItWouldBe,
    }

    /// <summary>What "Mend All" did to one song.</summary>
    /// <param name="Chords">How many of its chords were shown twice, and are mended.</param>
    /// <param name="NotMended">If it was not mended, the sentence that says why.</param>
    public sealed record MendOutcome(string Name, int Chords, string? NotMended);

    /// <summary>"Mend All" as it goes, and when it is done: how far it has got, and what it did, by name.</summary>
    public sealed record MendAllRun(int Of, IReadOnlyList<MendOutcome> Outcomes, bool Finished)
    {
        public int Mended => Outcomes.Count(outcome => outcome.NotMended is null);

        public int Chords => Outcomes.Where(outcome => outcome.NotMended is null).Sum(outcome => outcome.Chords);

        /// <summary>What is said over the list of what was done.</summary>
        public string Sentence
        {
            get
            {
                if (!Finished)
                {
                    return "Mending " + Number(Math.Min(Of, Outcomes.Count + 1)) + " of " + Number(Of) + (char)0x2026;
                }
                var said = (Mended == 1 ? "1 song was mended: " : Number(Mended) + " songs were mended: ")
                    + (Chords == 1 ? "1 chord that ProPresenter showed twice." : Number(Chords) + " chords that ProPresenter showed twice.");
                var not = Outcomes.Count - Mended;
                if (not > 0)
                {
                    said += not == 1 ? " 1 song was not mended, and is as it was." : " " + Number(not) + " songs were not mended, and are as they were.";
                }
                var left = Of - Outcomes.Count;
                if (left > 0)
                {
                    said += left == 1 ? " 1 more was not tried." : " " + Number(left) + " more were not tried.";
                }
                return said;
            }
        }
    }

    private bool stopMendingAll;

    // Whether "Mend All" is at work: nothing else is begun until it is done.
    private bool MendingAllNow => MendingAll is { Finished: false };

    // ---- What is offered for the song that is open

    /// <summary>
    /// Whether a song whose chords look as if they were put on by hand in the first
    /// Windows editor is marked for it and offered to have them moved (check 2). Off as
    /// the app comes, and remembered.
    /// </summary>
    public bool OffersToMoveBack { get; private set; }

    /// <summary>While check 2's offer is being judged: which way the song is shown. Nothing when it is not.</summary>
    public Way? Judging { get; private set; }

    /// <summary>
    /// What is found in the song that is open, each with what can be done about it, in
    /// the order of the page's checks. None while an earlier version is looked at, and
    /// none for a song that cannot be read.
    /// </summary>
    public IReadOnlyList<Offer> Offers
    {
        get
        {
            if (openSong is null || Earlier is not null || Shown?.Found is not { } found)
            {
                return [];
            }
            var offers = new List<Offer>();
            if (found.ShownTwice > 0)
            {
                offers.Add(new Offer(ShownTwiceSentence(found.ShownTwice, "in this song "), [new OfferButton("Mend", OfferAction.Mend)]));
            }
            if (Judging is { } way)
            {
                offers.Add(way == Way.AsItWouldBe
                    ? new Offer(
                        "This is the song with those chords moved to where they look to have been meant. Nothing has been changed.",
                        [new("Show As It Is", OfferAction.ShowAsItIs), new("Move the Chords", OfferAction.MoveBack), new("Leave Them", OfferAction.Leave)])
                    : new Offer(
                        "This is the song as it is.",
                        [new("Show Moved", OfferAction.ShowMoved), new("Move the Chords", OfferAction.MoveBack), new("Leave Them", OfferAction.Leave)]));
            }
            else if (OffersToMoveBack && found.Shift.Likely)
            {
                offers.Add(new Offer(MovedLeftSentence("The chords of this song"), [new OfferButton("Show Me", OfferAction.ShowMoved)]));
            }
            if (found.Key.Doubtful && found.Key.Suggested.Length > 0)
            {
                offers.Add(new Offer(
                    DoubtedKeySentence("This song says", found.StatedKey, found.Key.Suggested)
                        + (found.KeyIsTheOldEditors ? " The old Windows chord editor, which saved this song, wrote " + KeyInWords(found.StatedKey) + " by mistake." : ""),
                    [new OfferButton("Set Key to " + found.Key.Suggested, OfferAction.SetKey)]));
            }
            if (FoldOffer is { } fold)
            {
                offers.Add(fold);
            }
            return offers;
        }
    }

    /// <summary>
    /// What the sheet's strip says of a song that states no key, where the keys' chords
    /// would be: that, and which key its chords look like, if the sum can say
    /// (spec/08-repair.md, check 3).
    /// </summary>
    public string NoKeySentence => Shown?.Found is { StatedKey.Length: 0, Key.Suggested: { Length: > 0 } likely }
        ? NoKey + ". They look like " + KeyInWords(likely) + "."
        : NoKey;

    internal const string NoKey = "The song names no key: pick the one its chords are in";

    /// <summary>A button of an offer was clicked.</summary>
    public void Take(OfferAction action)
    {
        switch (action)
        {
            case OfferAction.Mend:
                Mend();
                break;
            case OfferAction.SetKey:
                if (Shown?.Found is { Key: { Doubtful: true, Suggested: { Length: > 0 } key } })
                {
                    SetKey(key);
                }
                break;
            case OfferAction.ShowMoved:
                ShowTheSong(Way.AsItWouldBe);
                break;
            case OfferAction.ShowAsItIs:
                ShowTheSong(Way.AsItIs);
                break;
            case OfferAction.MoveBack:
                MoveBack();
                break;
            case OfferAction.Leave:
                LeaveThem();
                break;
            case OfferAction.Fold:
                AskToFold();
                break;
            case OfferAction.OpenOriginal:
                if (Foldable is { } pair)
                {
                    Open(pair.Original.Id);
                }
                break;
            case OfferAction.MoveCopyOut:
                AskToMoveTheCopyOut();
                break;
        }
    }

    /// <summary>
    /// Check 1's mending of the song that is open: its chords are written again where
    /// they are, so that ProPresenter shows each once. One step of undo; a copy is kept
    /// first.
    /// </summary>
    public void Mend()
    {
        StopJudging();
        var mended = 0;
        Change(song => mended = song.Mend());
        if (mended > 0 && Refused is null)
        {
            TheSongChangedUnderTheText();
            Say(mended == 1
                ? "Mended. 1 chord is now written so that ProPresenter shows it once."
                : "Mended. " + Number(mended) + " chords are now written so that ProPresenter shows each once.");
        }
    }

    /// <summary>
    /// Check 2's offer taken: every chord below a first line is moved to where the first
    /// Windows editor meant it. One step of undo; a copy is kept first.
    /// </summary>
    public void MoveBack()
    {
        if (openSong is null || Shown?.Found is not { } found)
        {
            return;
        }
        StopJudging();
        var moved = found.Shift.ChordsBelowTheFirstLine;
        var was = openSong.Bytes;
        Change(song => song.MoveBack());
        if (Refused is null && !was.Span.SequenceEqual(openSong.Bytes.Span))
        {
            TheSongChangedUnderTheText();
            Say((moved == 1 ? "1 chord was" : Number(moved) + " chords were") + " moved to where " + (moved == 1 ? "it looks" : "they look") + " to have been meant. Ctrl+Z puts "
                + (moved == 1 ? "it" : "them") + " back.");
        }
    }

    /// <summary>Check 2's offer not taken: the song is shown as it is, and is as it was.</summary>
    public void LeaveThem()
    {
        if (Judging is not null)
        {
            StopJudging();
            Changed();
        }
    }

    /// <summary>
    /// Switches check 2's offer on or off, and remembers it. The list's marks and its
    /// "wanting mending" go by it at once: what the check finds in each song is kept
    /// whether or not it is offered.
    /// </summary>
    public void SetOffersToMoveBack(bool offered)
    {
        if (OffersToMoveBack == offered)
        {
            return;
        }
        OffersToMoveBack = offered;
        if (!offered)
        {
            StopJudging();
        }
        Remember();
        Changed();
    }

    // Shows the song one of the two ways, on the sheet, for check 2's offer to be judged.
    private void ShowTheSong(Way way)
    {
        if (openSong is not { } song || !OffersToMoveBack || (Judging is null && Shown?.Found?.Shift.Likely != true))
        {
            return;
        }
        if (EditorShown != Editor.Sheet)
        {
            Show(Editor.Sheet);
            if (EditorShown != Editor.Sheet)
            {
                return;
            }
        }
        else if (!Settle())
        {
            return;
        }
        Judging = way;
        Sheet = way == Way.AsItWouldBe ? MakeSheet(song.AsItWouldBeMovedBack()) : MakeSheet(song);
        SheetVersion++;
        Changed();
    }

    // The song is shown as it is, on a sheet that changes it, and nothing is being judged.
    private void StopJudging()
    {
        if (Judging is null)
        {
            return;
        }
        var shownAsItWouldBe = Judging == Way.AsItWouldBe;
        Judging = null;
        if (shownAsItWouldBe && openSong is { } song)
        {
            Sheet = MakeSheet(song);
            SheetVersion++;
        }
    }

    // The song was changed otherwise than by typing in the text: the text is made again
    // from it now, if it is what is shown, or before it next is.
    private void TheSongChangedUnderTheText()
    {
        if (EditorShown == Editor.ChordPro && editor is not null)
        {
            editor.Reload(keepingWhatIsTyped: false);
            ShowText(editor);
            Changed();
        }
        else
        {
            textIsStale = true;
        }
    }

    // ---- The sentences

    /// <summary>"12 chords in this song are written so that ProPresenter shows them twice."</summary>
    internal static string ShownTwiceSentence(int chords, string where = "") => chords == 1
        ? "1 chord " + where + "is written so that ProPresenter shows it twice."
        : Number(chords) + " chords " + where + "are written so that ProPresenter shows them twice.";

    /// <summary>"This song says it is in A flat, but its chords look like G."</summary>
    internal static string DoubtedKeySentence(string itSays, string stated, string likelier) =>
        itSays + " it is in " + KeyInWords(stated) + ", but its chords look like " + KeyInWords(likelier) + ".";

    internal static string MovedLeftSentence(string theChords) =>
        theChords + " look as if they were put on in the old Windows chord editor, which counted them a little to the left on every line after the first.";

    /// <summary>What wants mending in a song, by its marks: a sentence for each thing found.</summary>
    internal static IReadOnlyList<string> WhatWantsMending(Marks marks, bool offeringToMoveBack)
    {
        var said = new List<string>();
        if (marks.ShownTwice > 0)
        {
            said.Add(ShownTwiceSentence(marks.ShownTwice));
        }
        if (offeringToMoveBack && marks.LooksMovedLeft)
        {
            said.Add(MovedLeftSentence("Its chords"));
        }
        if (marks.DoubtedKey.Length > 0)
        {
            said.Add(DoubtedKeySentence("It says", marks.DoubtedKey, marks.LikelierKey));
        }
        return said;
    }

    /// <summary>
    /// A key as a sentence says it: "A flat", "F sharp minor", "G". (The lists of keys
    /// have it as a musician writes it over a song, "Ab", "F#m", and so has the button
    /// that sets it. In a sentence that is read, the page spells it out.)
    /// </summary>
    internal static string KeyInWords(string key)
    {
        if (key.Length == 0)
        {
            return key;
        }
        var minor = key.Length > 1 && key[^1] == 'm';
        var note = minor ? key[..^1] : key;
        var words = note[..1];
        foreach (var sign in note[1..])
        {
            words += sign switch
            {
                'b' => " flat",
                '#' => " sharp",
                _ => sign.ToString(),
            };
        }
        return words + (minor ? " minor" : "");
    }

    // ---- Mend All (spec/08-repair.md, "The whole library")

    /// <summary>
    /// The songs "Mend All" would mend: those of the list as it is narrowed that have
    /// chords ProPresenter shows twice. Check 1 and no other.
    /// </summary>
    public IReadOnlyList<ListedSong> SongsToMend =>
        [.. ListedSongs.Where(song => marks.GetValueOrDefault(song.Id) is { ShownTwice: > 0, CannotBeRead: false })];

    /// <summary>
    /// Whether the list's fourth choice, "wanting mending", is offered: the library has
    /// a song that wants it. And while it is the one chosen, whatever the library has,
    /// so that it can be seen to be chosen. (Most libraries have no such song, and need
    /// not be asked about one for ever.)
    /// </summary>
    public bool OffersWantingMending =>
        filter == MarkFilter.WantingMending || Songs.Any(song => marks.GetValueOrDefault(song.Id)?.WantsMending(OffersToMoveBack) == true);

    /// <summary>
    /// Whether "Mend All" is offered: the list is narrowed to the songs that want
    /// mending, and some of them have what it mends.
    /// </summary>
    public bool OffersMendAll => filter == MarkFilter.WantingMending && MendingAll is null && SongsToMend.Count > 0;

    /// <summary>Whether it can be done now: not while ProPresenter is running.</summary>
    public bool CanMendAll => OffersMendAll && !ProPresenterIsRunning;

    /// <summary>"Mend All" asks first, saying how many songs and how many chords.</summary>
    public Question? MendAllQuestion { get; private set; }

    /// <summary>"Mend All" as it goes, and afterwards what it did, until that is closed.</summary>
    public MendAllRun? MendingAll { get; private set; }

    public void AskToMendAll()
    {
        if (!CanMendAll)
        {
            return;
        }
        var songs = SongsToMend;
        var chords = songs.Sum(song => marks[song.Id].ShownTwice);
        var one = songs.Count == 1;
        MendAllQuestion = new Question(
            one ? "Mend 1 song?" : "Mend " + Number(songs.Count) + " songs?",
            ShownTwiceSentence(chords, one ? "in it " : "in them ")
                + " Each is written again where it stands: no chord is moved, and no name is changed. A copy of " + (one ? "the song" : "each song") + " is kept first.",
            one ? "Mend 1 Song" : "Mend " + Number(songs.Count) + " Songs");
        Changed();
    }

    /// <summary>The answer was no.</summary>
    public void DontMendAll()
    {
        if (MendAllQuestion is not null)
        {
            MendAllQuestion = null;
            Changed();
        }
    }

    /// <summary>"Stop": the song being mended is finished, and no more are begun.</summary>
    public void StopMendingAll() => stopMendingAll = true;

    /// <summary>What "Mend All" did has been read: its list is put away.</summary>
    public void CloseMendAll()
    {
        if (MendingAll is { Finished: true })
        {
            MendingAll = null;
            Changed();
        }
    }

    /// <summary>
    /// The answer was yes. Each song is mended in turn as the one that is open would be:
    /// asked the same things first, a copy kept, one save. It stops at the first song
    /// that is refused because ProPresenter is running, since every other would be too.
    /// What it did is then said, by name.
    /// </summary>
    public async Task MendAllAsync()
    {
        MendAllQuestion = null;
        if (MendingAll is not null || !Settle())
        {
            Changed();
            return;
        }
        var songs = SongsToMend;
        var outcomes = new List<MendOutcome>();
        stopMendingAll = false;
        MendingAll = new MendAllRun(songs.Count, [], Finished: false);
        Changed();
        appData.Log("Mend All: " + Number(songs.Count) + " songs.");
        foreach (var listed in songs)
        {
            if (stopMendingAll)
            {
                break;
            }
            MendOutcome outcome;
            if (Paths.Same(listed.Id, SongId) && openSong is not null)
            {
                // The song that is open is mended as the one that is open: it is its
                // own step of undo there, and what is shown of it is brought up to date.
                var mended = 0;
                StopJudging();
                Change(song => mended = song.Mend());
                outcome = new MendOutcome(listed.Name, mended, Refused);
                if (Refused is null)
                {
                    TheSongChangedUnderTheText();
                }
            }
            else if (KeptSongOf(listed) is { } kept)
            {
                var safeguards = new Safeguards(askWhetherProPresenterIsRunning, (path, why) => copies.Keep(path, kept, why));
                (outcome, var forProPresenter) = await Task.Run(() => MendOne(listed, safeguards));
                if (forProPresenter)
                {
                    ProPresenterIs(true);
                }
                NoteAMending(listed);
            }
            else
            {
                continue;
            }
            outcomes.Add(outcome);
            appData.Log(outcome.NotMended is null
                ? "Mended " + Path.GetFileName(listed.Path) + ": " + Number(outcome.Chords) + " chords."
                : "Did not mend " + Path.GetFileName(listed.Path) + ".");
            MendingAll = new MendAllRun(songs.Count, [.. outcomes], Finished: false);
            Changed();
            if (ProPresenterIsRunning)
            {
                break;
            }
        }
        appData.Keep(keptMarks);
        NoteCopies();
        MendingAll = new MendAllRun(songs.Count, [.. outcomes], Finished: true);
        Changed();
    }

    // Mends one song that is not the one open, and says whether it was refused because
    // ProPresenter is running. (Off the window's thread: it touches nothing of the
    // model's.)
    private static (MendOutcome Outcome, bool ForProPresenter) MendOne(ListedSong listed, Safeguards safeguards)
    {
        try
        {
            return (new MendOutcome(listed.Name, new OpenSong(listed.Path, safeguards).Mend(), null), false);
        }
        catch (Refusal refusal)
        {
            return (new MendOutcome(listed.Name, 0, refusal.Sentence), refusal.Reason == Song.Refusal.Why.ProPresenterIsRunning);
        }
    }

    // A song of the list was mended, or was to be: its marks are worked out again from
    // its file as it now is.
    private void NoteAMending(ListedSong listed)
    {
        if (Listing.AsItIsNow(listed) is { } now)
        {
            Songs = [.. Songs.Select(one => Paths.Same(one.Id, now.Id) ? now : one)];
            var found = Song.Marks.Read(now);
            marks[now.Id] = found;
            keptMarks.Keep(found, now);
        }
    }
}
