// The song that is open: what it hands the editors, what they may ask of it, and undo.
// spec/05-song-and-saving.md is this file's specification, rule for rule; the reference
// is `PresentationEditor` in reference/simple-presenter/src/presentationeditor.cpp
// (`song`, `usedChords`, `setChords`, `setChordsAlone`, `key`, `setKey`, `change`,
// `undo`, `redo`), which also edits slides, props and stage layouts, none of which is
// wanted here. It was ported from the Mac's OpenSong.swift, which is this file in
// Swift, so that the two apps do the same thing to a file.
//
// How a change goes. Everything that may be asked comes down to `Save`: a new state of
// the whole message is made beside the one in memory, the safeguards are asked, the
// file is written, and only when it has read back as written does the new state take
// the old one's place. So a refusal, or a failure at any point, leaves the song in
// memory exactly as it was, with nothing to put back. Keep it so: nothing but `Save`
// puts a message in the song's place.
//
// Where this parts from the reference, each said at the place:
//
//   - The key is a step of undo like any other (rule 8 and after). There it is not.
//   - Setting the key leaves the key ProPresenter shows the song in alone, if someone
//     had chosen another (rule 6). There the two are always set together.
//   - There is a second request, for the key ProPresenter shows it in (rule 7).
//   - A save is refused while ProPresenter is running, and when the file on disk is no
//     longer the one that was read (rule 10). There neither is asked.
//   - A line of chords alone is given more or fewer stand-ins by changing the bytes of
//     that line in the box's RTF and no others (spec/01, "Chords with no words").
//     There the whole RTF is read into a model and written again.
//   - A blank slide's box is given its stand-ins after the opening of a neighbour's
//     RTF, and a slide with no box is given a copy of a neighbour's and never one of
//     this app's devising (spec/01, "Chords onto a blank slide").
//   - Only a line of chords alone is ever made stand-ins. There, any line asked for
//     is, its words with it; nothing there asks for one.
//   - A line of chords alone with its last chord taken off keeps one stand-in, a
//     zero-width space, and takes chords again (the maintainer's, 2026-10-12). There
//     it is left with none.
//   - What the first Windows editor left in a song can be put right (`Mend`,
//     `MoveBack`: spec/08-repair.md). There is nothing of the kind there.
//
// Not safe to use from two threads at once, and not meant to be: the app keeps a song
// where its window is.

using Google.Protobuf;
using Pro7ChordEditor.ChordRules;
using RV = Pro.SerializationInterop.RVProtoData;

namespace Pro7ChordEditor.Song;

public sealed class OpenSong : ISheetSong
{
    /// <summary>How many steps of undo are kept, as the reference keeps.</summary>
    internal const int UndoLimit = 200;

    private readonly Safeguards? safeguards;
    private readonly List<Step> undoSteps = [];
    private readonly List<Step> redoSteps = [];

    // What was made for the chords of blank slides while the song has been open here,
    // so that taking the chords off again leaves no trace (spec/01, "A change of mind").
    // By the text box: its cue, and its id on that cue's slide. It is forgotten when the
    // song is closed.
    private readonly Dictionary<(int Cue, string Element), Made> madeForChords = new();

    private RV.Presentation presentation;

    // The file's bytes as they were last read from it or written to it, and what the
    // file system said of it then. A save is refused if the disk no longer agrees.
    private byte[] bytesOnDisk;
    private SongFile.Stamp stamp;

    // The slides. They do not change while the song is open: nothing here adds a slide,
    // takes one away or moves one.
    private List<SlideRow> rows;

    // Whether the copy that is kept before a first change has been kept.
    private bool copyKept;

    /// <summary>
    /// Opens a song's file. With no safeguards it can be read and nothing else: every
    /// request to change it is refused.
    /// </summary>
    /// <exception cref="Refusal">The file cannot be read, or is no presentation.</exception>
    public OpenSong(string path, Safeguards? safeguards = null)
    {
        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(path);
            stamp = SongFile.StampOf(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SongFile.Failure or NotSupportedException or ArgumentException)
        {
            // (The system says why in its own words: there is no such file, or it may
            // not be read.)
            throw Refusal.CannotBeRead(System.IO.Path.GetFileName(path), exception.Message);
        }
        try
        {
            presentation = RV.Presentation.Parser.ParseFrom(bytes);
        }
        catch (InvalidProtocolBufferException)
        {
            throw Refusal.CannotBeRead(System.IO.Path.GetFileName(path), Refusal.NoPresentation);
        }
        Path = path;
        bytesOnDisk = bytes;
        rows = Reading.SlideRows(presentation);
        this.safeguards = safeguards;
    }

    // The part of the file a change changed, as it was before and as it was after (rule
    // 13). A change to chords is within one cue; a change of key is `music`, which a
    // song may not have at all. (Neither message is ever changed once it is here.)
    private abstract record Unit;

    private sealed record CueUnit(int Index, RV.Cue Before, RV.Cue After) : Unit;

    private sealed record MusicUnit(RV.Presentation.Types.Music? Before, RV.Presentation.Types.Music? After) : Unit;

    // A mending changes the chords of many cues at once, and is one thing to undo
    // whatever their number: so it is one unit, and not a run of steps joined, of which
    // the oldest would be forgotten in a song of more slides than steps are kept.
    private sealed record CuesUnit(IReadOnlyList<CueUnit> Cues) : Unit;

    /// <param name="Joined">One step of undo with the change before it.</param>
    private sealed record Step(Unit Unit, bool Joined);

    // What a blank slide's first chords had made for them.
    private abstract record Made;

    // The slide's own box was given words: this is its text, whole, as the file had it
    // before.
    private sealed record MadeWords(RV.Graphics.Types.Text Was) : Made;

    // The slide had no box and was given one: the cue as the file had it before, and as
    // it was just after, less the box. (While the second is what the cue still is
    // without the box, nothing else in it has changed, and the first can be put back
    // whole: the file is then what it was to the last byte, down to which of its empty
    // parts were written and which were left out.)
    private sealed record MadeBox(RV.Cue CueBefore, RV.Cue CueWithoutIt) : Made;

    private delegate void CueChange(ref RV.Cue cue, int action);

    // ---- What the editors are handed

    /// <summary>Where the song's file is.</summary>
    public string Path { get; }

    /// <summary>The song's name, as it is inside the file.</summary>
    public string Name => presentation.Name;

    /// <summary>The song's id, as it is inside the file.</summary>
    public string Id => presentation.Uuid?.String ?? "";

    /// <summary>
    /// One entry for each text box that holds the song's words, in the order of the
    /// slides and, on a slide, of its boxes. Read from the song each time it is asked.
    /// </summary>
    public IReadOnlyList<TextBox> TextBoxes => Reading.TextBoxes(presentation, rows);

    /// <summary>
    /// Every chord's name in song order, one for each use. It is what the typing
    /// bubble's offers go by.
    /// </summary>
    public IReadOnlyList<string> UsedChords => TextBoxes.SelectMany(box => box.Chords.Select(chord => chord.Name)).ToList();

    /// <summary>How many slides the song has, in the order they are shown.</summary>
    public int SlideCount => rows.Count;

    /// <summary>
    /// The key the chords in the file are written in, as a musician writes it, or "" if
    /// the song states none.
    /// </summary>
    public string Key => presentation.Music is null ? "" : Reading.KeyName(presentation.Music.Original);

    /// <summary>
    /// The key ProPresenter is to show the chords in, or "" if the song states no key.
    /// The editors never draw chords in it: they show what is in the file.
    /// </summary>
    public string ShownKey => presentation.Music is null ? "" : Reading.KeyName(presentation.Music.User);

    /// <summary>
    /// What the checks of spec/08-repair.md find in the song as it is now, in every
    /// text box of its file (Checks.cs).
    /// </summary>
    public Findings Findings => Checks.Of(presentation);

    public bool CanUndo => undoSteps.Count > 0;

    public bool CanRedo => redoSteps.Count > 0;

    /// <summary>
    /// The file's bytes as they were last read from it or written to it: what the song
    /// is, as a file.
    /// </summary>
    public ReadOnlyMemory<byte> Bytes => bytesOnDisk;

    /// <summary>
    /// Whether the file on disk is still the one that was last read here or written from
    /// here: in its size, its time of last change and its bytes. If it is not, something
    /// else has changed it, and nothing will be saved over it.
    /// </summary>
    public bool FileIsAsLastRead
    {
        get
        {
            try
            {
                return SongFile.StampOf(Path) == stamp && File.ReadAllBytes(Path).AsSpan().SequenceEqual(bytesOnDisk);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SongFile.Failure)
            {
                return false;
            }
        }
    }

    // ---- What the editors may ask
    //
    // Each changes the song and saves it at once, or throws a Refusal and changes
    // nothing. `joined` makes it one step of undo with the change before it. A request
    // that would change nothing saves nothing and is no step of undo.

    /// <summary>
    /// "This text box now has these chords": the whole list, each place counted along
    /// the whole box. Every name is tidied, those of chords that were there before among
    /// them, since an editor hands back the whole box.
    /// </summary>
    /// <exception cref="Refusal">Nothing was changed, and its sentence says why.</exception>
    public void SetChords(int slide, string element, IReadOnlyList<Chord> chords, bool joined = false)
    {
        ChangeCue(slide, joined, (ref RV.Cue cue, int action) =>
        {
            var elements = SlideOf(cue, action).Elements;
            var text = elements[IndexOfBox(element, elements)].Element_.Text;
            Writing.WriteChords(chords.Select(chord => chord with { Name = Chords.Tidied(chord.Name) }), text);
        });
    }

    /// <summary>
    /// "This line of chords alone now has these": the names in order. The line's text
    /// becomes that many stand-ins and its chords go on them, every other place; the
    /// chords of the lines after it move with their words.
    ///
    ///   - WITH NO NAMES THE LINE IS ONE ZERO-WIDTH SPACE, whatever it hung its chords
    ///     on, and is a line of chords alone still, so that a chord can be put on it
    ///     again. (Decided by the maintainer on 2026-10-12. And not the plain space the
    ///     line may have had: a text box left holding one plain space and no chord is,
    ///     as a song is read, a stray space and nothing more, and the line would be
    ///     lost.)
    ///   - Where the line has those stand-ins already (its chords renamed, or put in
    ///     another order), only the chord entries change.
    ///   - Otherwise the line's characters are changed in the box's RTF, in place, and
    ///     not another byte of it (RtfWords). This is the one case in which the app
    ///     writes words.
    ///   - A blank slide's line: its first chords give its box words, or give the slide
    ///     a box if it has none (an empty <paramref name="element"/>). No chords for a
    ///     blank slide is nothing to do, and nothing is made for it.
    ///   - The last chord taken off a blank slide that was given its chords while the
    ///     song has been open here unmakes what was made for them: the file is then
    ///     byte for byte what it was before the first.
    ///
    /// Only a line of chords alone is ever made stand-ins: a line that has words, or is
    /// empty, is refused, whatever asks. (The words of a song are not this app's to
    /// change.) A name that is nothing, or nothing but spaces, is left out, as it would
    /// be when the box's chords are written.
    /// </summary>
    /// <exception cref="Refusal">Nothing was changed, and its sentence says why.</exception>
    public void SetChordsAlone(int slide, string element, int line, IReadOnlyList<string> names, bool joined = false)
    {
        if (slide < 0 || slide >= rows.Count)
        {
            throw Refusal.SlideGone;
        }
        var tidied = names.Select(Chords.Tidied).Where(name => name.Length > 0).ToList();
        // A blank slide is handed over as one box with no words: its own, or one with no
        // id if it has none.
        var handed = Reading.TextBoxes(slide, presentation, rows);
        var blank = handed.Count == 1 && handed[0].Blank ? handed[0] : null;
        if (element.Length == 0)
        {
            if (tidied.Count == 0)
            {
                return;
            }
            if (blank is null || blank.Element.Length != 0)
            {
                throw Refusal.NoLongerBlank;
            }
            GiveABox(slide, tidied, joined);
        }
        else if (blank is not null && string.Equals(blank.Element, element, StringComparison.Ordinal))
        {
            if (tidied.Count == 0)
            {
                return;
            }
            GiveWords(element, slide, tidied, joined);
        }
        else
        {
            ChangeLine(line, element, slide, tidied, joined);
        }
    }

    /// <summary>
    /// Sets the key the chords are written in. It changes no chord: it says what key the
    /// chords that are there are in.
    ///
    /// The key ProPresenter shows the song in is set to the same, IF the song had no key
    /// or its two keys were the same. If they differed, someone chose to show the song
    /// in another key, and that is left alone. (A song whose `music` has no `user` part
    /// is taken as one whose two keys are the same: nobody chose another. That is a
    /// GUESS at what ProPresenter means by leaving it out; no file to hand does.)
    /// </summary>
    /// <exception cref="Refusal">Nothing was changed, and its sentence says why.</exception>
    public void SetKey(string key, bool joined = false)
    {
        var number = Chords.KeyNumber(key);
        if (number < 0)
        {
            throw Refusal.NotAKey(key);
        }
        if (string.Equals(key, Key, StringComparison.Ordinal))
        {
            return;
        }
        var minor = Chords.KeyIsMinor(key);
        ChangeMusic(joined, (music, hadMusic) =>
        {
            var together = !hadMusic || music.User is null || music.User.Equals(music.Original ?? new RV.MusicKeyScale());
            music.Original = Set(music.Original, number, minor);
            if (together)
            {
                music.User = Set(music.User, number, minor);
            }
        });
    }

    /// <summary>
    /// NEW (rule 7): sets the key ProPresenter is to show the song in, and that alone. A
    /// song that states no key cannot be shown in another, and that is refused: a
    /// `music` made with only this part in it would read as a song in A flat.
    /// </summary>
    /// <exception cref="Refusal">Nothing was changed, and its sentence says why.</exception>
    public void SetShownKey(string key, bool joined = false)
    {
        var number = Chords.KeyNumber(key);
        if (number < 0)
        {
            throw Refusal.NotAKey(key);
        }
        if (presentation.Music is null)
        {
            throw Refusal.NoKeyToShowAnother;
        }
        if (string.Equals(key, ShownKey, StringComparison.Ordinal))
        {
            return;
        }
        var minor = Chords.KeyIsMinor(key);
        ChangeMusic(joined, (music, _) => music.User = Set(music.User, number, minor));
    }

    /// <summary>
    /// NEW (spec/08-repair.md, check 1): puts right the chords that are written so that
    /// ProPresenter shows them twice. Every text box of the song that has chords has
    /// them written again, exactly where they are; everything written has the right
    /// ranges (Writing.cs), so a mending is nothing but that. It moves no chord and
    /// changes no name, but for what any writing of a box does: a chord that is on no
    /// character of its words is brought onto one (check 0), and a name loses the
    /// spaces off its ends.
    ///
    /// EVERY box, and not only those in which a range runs over a line break. The page
    /// says "the box's chords"; but that editor wrote every range of a song its own
    /// way, and on a line of chords alone its way runs a chord on to the next without
    /// crossing any line break. `repairs.json` has those put right too (the last box
    /// of Plain Song), and a file mended is to have exactly the chord entries of the
    /// one it was made from.
    ///
    /// A song in which nothing is found is left alone, whatever its ranges are: nothing
    /// is saved, and no copy is kept. (So are the lines of chords alone that Simple
    /// Presenter 0.8 to 0.82 wrote that same way: they are no one's damage, and are
    /// written right when their box is next changed.)
    ///
    /// One save and one step of undo, and before it a copy of the file is kept, whatever
    /// was kept before (spec/07-backups.md, rule 3). Answers how many chords were
    /// shown twice.
    /// </summary>
    /// <exception cref="Refusal">Nothing was changed, and its sentence says why.</exception>
    public int Mend()
    {
        var found = Findings.ShownTwice;
        if (found > 0)
        {
            ChangeEveryBox((_, chords) => chords);
        }
        return found;
    }

    /// <summary>
    /// NEW (spec/08-repair.md, check 2), for when it has been offered and taken: moves
    /// the chords that were put on by hand in the first Windows editor to where they
    /// were meant. Every chord goes to `OldEditor.Unshifted` of its place, which moves
    /// those below the first line of their text box, by one character for each line
    /// break above, and leaves the others where they are; and every box is written, as
    /// by `Mend`, which puts the ranges right by the way.
    ///
    /// It is never done unasked: the check that offers it can only say that a song
    /// LOOKS as if it was chorded there (OldEditor.cs). One save and one step of undo,
    /// with a copy kept first.
    /// </summary>
    /// <exception cref="Refusal">Nothing was changed, and its sentence says why.</exception>
    public void MoveBack() =>
        ChangeEveryBox((words, chords) => chords.Select(chord => chord with { At = OldEditor.Unshifted(words, chord.At) }));

    /// <summary>
    /// Puts the last change back as it was before, and saves. A step that was joined to
    /// the one before it is undone with it, in one go.
    /// </summary>
    /// <exception cref="Refusal">Nothing was changed, and its sentence says why.</exception>
    public void Undo() => Restore(forwards: false);

    /// <summary>Does again what was last undone.</summary>
    /// <exception cref="Refusal">Nothing was changed, and its sentence says why.</exception>
    public void Redo() => Restore(forwards: true);

    /// <summary>
    /// Makes the song's file these bytes again: a copy kept earlier, or the song as it
    /// was when it was opened (spec/07-backups.md, rules 19 to 22a). It is a save like
    /// any other, asked the same things first, and the file as it is now is kept as a
    /// copy before it is written over: so that this can itself be taken back, by
    /// putting that copy back.
    ///
    /// The song then starts afresh from the file: its history of undo is empty, and its
    /// next change keeps a copy as the first of a sitting does. Putting back what the
    /// file already is, is nothing to do.
    /// </summary>
    /// <exception cref="Refusal">Nothing was changed, and its sentence says why.</exception>
    public void PutBack(ReadOnlySpan<byte> bytes)
    {
        if (bytes.SequenceEqual(bytesOnDisk))
        {
            return;
        }
        var asked = MaySave();
        // What is put back must be a song: a copy that cannot be read is not swapped in.
        var put = bytes.ToArray();
        RV.Presentation copy;
        try
        {
            copy = RV.Presentation.Parser.ParseFrom(put);
        }
        catch (InvalidProtocolBufferException)
        {
            throw Refusal.CannotBeRead("That copy", Refusal.NoPresentation);
        }
        try
        {
            asked.KeepCopy(Path, CopyReason.BeforePuttingBack);
        }
        catch (Exception exception)
        {
            throw Refusal.CopyNotKept(exception.Message);
        }
        try
        {
            SongFile.Write(put, Path);
        }
        catch (SongFile.Failure failure)
        {
            throw Refusal.NotSaved(failure.Message);
        }
        presentation = copy;
        bytesOnDisk = put;
        try
        {
            stamp = SongFile.StampOf(Path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SongFile.Failure)
        {
        }
        rows = Reading.SlideRows(copy);
        undoSteps.Clear();
        redoSteps.Clear();
        // (And nothing is remembered of what was made for a blank slide's chords: that
        // was of the file as it was, and this is another.)
        madeForChords.Clear();
        // (A new sitting with the song: its next change asks for a copy again, and none
        // is made if the newest kept is already what the file is.)
        copyKept = false;
    }

    // ---- Chords with no words (spec/01-file-format.md)

    // A line of chords alone that has words already, of a kind: its stand-ins.
    private void ChangeLine(int line, string element, int slide, List<string> names, bool joined)
    {
        var made = (rows[slide].Cue, element);
        ChangeCue(slide, joined, (ref RV.Cue cue, int action) =>
        {
            var elements = SlideOf(cue, action).Elements;
            var index = IndexOfBox(element, elements);
            var text = elements[index].Element_.Text;
            var lines = Words.Lines(Reading.WordsOf(text));
            if (line < 0 || line >= lines.Length)
            {
                throw Refusal.LineGone;
            }
            var old = lines[line];
            if (!Chords.IsPlaceholders(old))
            {
                throw Refusal.NotChordsAlone;
            }
            // In the character the line hangs its chords on now: a plain space if it has
            // spaces and no zero-width space, else a zero-width space. One for each
            // chord; and where there is no chord, one zero-width space, which is what
            // keeps the line a line of chords alone.
            var spaces = !old.Contains(Words.ZeroWidthSpace, StringComparison.Ordinal) && old.Contains(' ', StringComparison.Ordinal);
            var now = names.Count == 0 ? Chords.Placeholders(1) : Chords.Placeholders(names.Count, spaces ? ' ' : Words.ZeroWidthSpace);
            // The chords of the other lines keep their characters; those after this line
            // move by as much as it grows or shrinks. This line's are replaced: a chord
            // that stands on the line, or on the line break that ends it, is one of the
            // line's.
            var start = lines.Take(line).Sum(before => before.Length + 1);
            var kept = new List<Chord>();
            foreach (var chord in Reading.ChordsOf(text))
            {
                if (chord.At < start)
                {
                    kept.Add(chord);
                }
                else if (chord.At > start + old.Length)
                {
                    kept.Add(chord with { At = chord.At + now.Length - old.Length });
                }
            }

            // A change of mind: the last chord taken off a blank slide that was given
            // its chords here, whose box has nothing in it but this line's stand-ins.
            // What was made for them is unmade. (This is looked for BEFORE the rule that
            // a line with no chord is one zero-width space: what was made here is unmade
            // whole, and keeps nothing.)
            if (names.Count == 0 && kept.Count == 0 && madeForChords.TryGetValue(made, out var was))
            {
                var nothingElse = lines.Where((_, n) => n != line).All(other => Words.IsBlank(other) && !Words.HasAStandIn(other));
                if (nothingElse)
                {
                    switch (was)
                    {
                        case MadeWords words:
                            elements[index].Element_.Text = words.Was.Clone();
                            break;
                        case MadeBox box:
                            elements.RemoveAt(index);
                            if (cue.Equals(box.CueWithoutIt))
                            {
                                cue = box.CueBefore.Clone();
                            }
                            break;
                    }
                    return;
                }
            }

            if (!string.Equals(now, old, StringComparison.Ordinal))
            {
                var rtf = RtfWords.WithLine(text.RtfData.Span, line, now) ?? throw Refusal.CannotBeWritten;
                text.RtfData = ByteString.CopyFrom(rtf);
            }
            kept.AddRange(names.Select((name, n) => new Chord(start + n * 2, name)));
            Writing.WriteChords(kept, text);
        });
    }

    // The first chords of a blank slide that has a text box: the box is given words, one
    // stand-in for each chord. Only its RTF and its chord entries change.
    //
    // The RTF is made, not changed in place: as Apple's text system writes a text box
    // with no text, there is no font in it, no size and no colour for a stand-in to be
    // in. So it is the opening of the nearest text box of the song that has words, and
    // the stand-ins after it. Zero-width spaces, whatever stray space the box held.
    private void GiveWords(string element, int slide, List<string> names, bool joined)
    {
        var made = (rows[slide].Cue, element);
        RV.Graphics.Types.Text? was = null;
        var changed = ChangeCue(slide, joined, (ref RV.Cue cue, int action) =>
        {
            var elements = SlideOf(cue, action).Elements;
            var text = elements[IndexOfBox(element, elements)].Element_.Text;
            was = text.Clone();
            var model = BoxToModelOn(slide) ?? throw Refusal.NoBoxToModelOn;
            Put(names, text, model.Element_.Text);
        });
        // (Remembered once it has been saved, and only the first time: what the file had
        // before its first chord here is what is to be gone back to.)
        if (changed && was is not null && !madeForChords.ContainsKey(made))
        {
            madeForChords[made] = new MadeWords(was);
        }
    }

    // The first chords of a blank slide that has no text box: it is given one. NEW to
    // these apps as decided on 2026-10-11, and as the reference does it but for one
    // thing: the box is always a copy of the song's own, the nearest that has words,
    // placed, sized and set as that is. If the song has none, the change is refused; the
    // apps do not devise a text box.
    //
    // Of the copy: its id is a new one; its name is the copied name, or that with a
    // number after it until no other element of the slide has it; what tied the other
    // box to things on its own slide is cleared (how it comes on and goes off, and its
    // links); its chords are taken off; and its words are the stand-ins. It goes after
    // the slide's other elements, so that it is drawn over them.
    //
    // A GUESS, until it has been seen in ProPresenter for each kind of slide: that the
    // settings the copied box keeps for stretches of its words (how they are
    // capitalised, a first font), whose stretches were over words that are not in the
    // copy, do no harm left as they are (spec/01, "A change of mind", the last
    // paragraph).
    private void GiveABox(int slide, List<string> names, bool joined)
    {
        var id = "";
        RV.Cue? cueBefore = null;
        RV.Cue? cueWithoutIt = null;
        var changed = ChangeCue(slide, joined, (ref RV.Cue cue, int action) =>
        {
            var model = BoxToModelOn(slide) ?? throw Refusal.NoBoxToModelOn;
            cueBefore = cue.Clone();
            var elements = SlideOf(cue, action).Elements;
            var added = model.Clone();
            id = NewId(Reading.IdOf(model));
            added.Element_.Uuid ??= new RV.UUID();
            added.Element_.Uuid.String = id;
            added.Element_.Name = NameNotAmong(model.Element_.Name, elements.Select(other => other.Element_?.Name ?? "").ToList());
            added.BuildIn = null;
            added.BuildOut = null;
            added.ChildBuilds.Clear();
            added.DataLinks.Clear();
            Put(names, added.Element_.Text, model.Element_.Text);
            // (As the cue will be with the box taken off again: made the way it will be
            // made then, so that the two can be held against each other.)
            cueWithoutIt = cue.Clone();
            elements.Add(added);
        });
        if (changed && cueBefore is not null && cueWithoutIt is not null)
        {
            madeForChords[(rows[slide].Cue, id)] = new MadeBox(cueBefore, cueWithoutIt);
        }
    }

    // Makes a text's words one stand-in for each of these chords, after the opening of
    // another text's RTF, and puts the chords on them.
    private static void Put(List<string> names, RV.Graphics.Types.Text text, RV.Graphics.Types.Text model)
    {
        var rtf = RtfWords.WithTheOpeningOf(model.RtfData.Span, Chords.Placeholders(names.Count)) ?? throw Refusal.CannotBeWritten;
        text.RtfData = ByteString.CopyFrom(rtf);
        Writing.WriteChords(names.Select((name, n) => new Chord(n * 2, name)), text);
    }

    // The text box a blank slide's is modelled on: the nearest of the song that is shown
    // and is not blank, on a slide before this one if there is one (the nearest first),
    // else on one after. Its first such box, if a slide has two.
    //
    // (A box that is shown for its chords and has no character at all is passed over:
    // it has no words to say where words begin, and so no opening to give.)
    private RV.Slide.Types.Element? BoxToModelOn(int slide)
    {
        var others = Enumerable.Range(0, slide).Reverse().Concat(Enumerable.Range(slide + 1, Math.Max(0, rows.Count - slide - 1)));
        foreach (var other in others)
        {
            var elements = Reading.ElementsOf(rows[other], presentation);
            foreach (var box in Reading.TextBoxes(other, presentation, rows))
            {
                if (box.Blank || box.Words.Length == 0)
                {
                    continue;
                }
                var found = elements.FirstOrDefault(element => Reading.HasText(element) && string.Equals(Reading.IdOf(element), box.Element, StringComparison.Ordinal));
                if (found is not null)
                {
                    return found;
                }
            }
        }
        return null;
    }

    // (Only a box that has text: chords are not put on a shape.)
    private static int IndexOfBox(string element, IList<RV.Slide.Types.Element> elements)
    {
        for (var i = 0; i < elements.Count; i++)
        {
            if (Reading.HasText(elements[i]) && string.Equals(Reading.IdOf(elements[i]), element, StringComparison.Ordinal))
            {
                return i;
            }
        }
        throw Refusal.TextBoxGone;
    }

    /// <summary>
    /// A new id, written as the file's others are: in capitals, as ProPresenter on the
    /// Mac writes them, unless the id it is modelled on has a small letter in it.
    /// </summary>
    internal static string NewId(string writtenAs)
    {
        var id = Guid.NewGuid().ToString("D");
        return writtenAs.Any(c => c >= 'a' && c <= 'f') ? id : id.ToUpperInvariant();
    }

    /// <summary>
    /// A name for an element that no other element of the slide has: the name, or the
    /// name with " 2", " 3" and so on after it.
    /// </summary>
    internal static string NameNotAmong(string name, IReadOnlyList<string> taken)
    {
        var candidate = name;
        var n = 2;
        while (taken.Contains(candidate, StringComparer.Ordinal))
        {
            candidate = name + " " + n.ToString(System.Globalization.CultureInfo.InvariantCulture);
            n++;
        }
        return candidate;
    }

    // ---- How a change is made

    // The slide of a cue, to be changed. (A slide with nothing written of itself is
    // given an empty self to put elements in, as assigning to one does on the Mac.
    // Nothing is saved for that alone: with no elements there is no text box, and what
    // asked is refused.)
    private static RV.Slide SlideOf(RV.Cue cue, int action)
    {
        var slide = cue.Actions[action].Slide.Presentation;
        return slide.BaseSlide ??= new RV.Slide();
    }

    // A key's note and scale, set in the part that holds them: the part that is there,
    // with whatever else it may hold, or a new one.
    private static RV.MusicKeyScale Set(RV.MusicKeyScale? key, int number, bool minor)
    {
        key ??= new RV.MusicKeyScale();
        key.MusicKey = (RV.MusicKeyScale.Types.MusicKey)number;
        key.MusicScale = minor ? RV.MusicKeyScale.Types.MusicScale.Minor : RV.MusicKeyScale.Types.MusicScale.Major;
        return key;
    }

    // Changes a slide's cue: what is handed over is a copy of the cue, and which of its
    // actions is the slide. Answers whether anything was changed, and so saved.
    private bool ChangeCue(int slide, bool joined, CueChange apply)
    {
        if (slide < 0 || slide >= rows.Count)
        {
            throw Refusal.SlideGone;
        }
        var row = rows[slide];
        var before = presentation.Cues[row.Cue];
        var after = before.Clone();
        apply(ref after, row.Action);
        if (after.Equals(before))
        {
            return false;
        }
        var changed = presentation.Clone();
        changed.Cues[row.Cue] = after;
        Save(changed);
        Record(new CueUnit(row.Cue, before, after), joined);
        return true;
    }

    // Gives every text box of the file's slides that has chords the chords that
    // `chordsFor` makes of its own (handed the box's words, and its chords as the file
    // has them, in order of place). Which boxes those are is Checks.cs's to say: every
    // one, shown by the editors or not. One save, one step of undo, and before the
    // save a copy of the file, kept as before a mending. Answers whether anything
    // changed.
    private bool ChangeEveryBox(Func<string, IReadOnlyList<Chord>, IEnumerable<Chord>> chordsFor)
    {
        var changed = presentation.Clone();
        var units = new List<CueUnit>();
        for (var index = 0; index < presentation.Cues.Count; index++)
        {
            var before = presentation.Cues[index];
            var after = before.Clone();
            foreach (var text in Checks.Texts(after))
            {
                var chords = Reading.ChordsOf(text);
                if (chords.Count > 0)
                {
                    Writing.WriteChords(chordsFor(Reading.WordsOf(text), chords), text);
                }
            }
            if (!after.Equals(before))
            {
                changed.Cues[index] = after;
                units.Add(new CueUnit(index, before, after));
            }
        }
        if (units.Count == 0)
        {
            return false;
        }
        Save(changed, CopyReason.BeforeMending);
        Record(new CuesUnit(units), joined: false);
        return true;
    }

    private void ChangeMusic(bool joined, Action<RV.Presentation.Types.Music, bool> apply)
    {
        var before = presentation.Music;
        var after = before?.Clone() ?? new RV.Presentation.Types.Music();
        apply(after, before is not null);
        if (before is not null && after.Equals(before))
        {
            return;
        }
        var changed = presentation.Clone();
        changed.Music = after;
        Save(changed);
        Record(new MusicUnit(before, after), joined);
    }

    // A new change is a step of undo, and throws away what could have been redone.
    private void Record(Unit unit, bool joined)
    {
        undoSteps.Add(new Step(unit, joined && undoSteps.Count > 0));
        if (undoSteps.Count > UndoLimit)
        {
            undoSteps.RemoveAt(0);
        }
        redoSteps.Clear();
    }

    // Takes the last step off one list, puts its part of the file as it was at the other
    // end of that step, saves, and puts the step on the other list.
    private void Restore(bool forwards)
    {
        var from = forwards ? redoSteps : undoSteps;
        var to = forwards ? undoSteps : redoSteps;
        if (from.Count == 0)
        {
            return;
        }
        var step = from[^1];
        var changed = presentation.Clone();
        switch (step.Unit)
        {
            case CueUnit cue:
                changed.Cues[cue.Index] = (forwards ? cue.After : cue.Before).Clone();
                break;
            case MusicUnit music:
                changed.Music = (forwards ? music.After : music.Before)?.Clone();
                break;
            case CuesUnit cues:
                foreach (var one in cues.Cues)
                {
                    changed.Cues[one.Index] = (forwards ? one.After : one.Before).Clone();
                }
                break;
        }
        Save(changed);
        from.RemoveAt(from.Count - 1);
        to.Add(step);
        // A step joined to the one before it goes with it: back, the one before follows;
        // forwards, the one that was joined to this follows.
        if (from.Count > 0 && (forwards ? from[^1].Joined : step.Joined))
        {
            Restore(forwards);
        }
    }

    // The first three things asked before anything is saved, in the order below (1 to
    // 3). Answers with the safeguards, or throws the refusal.
    private Safeguards MaySave()
    {
        if (safeguards is null)
        {
            throw Refusal.OnlyForReading;
        }
        if (safeguards.ProPresenterIsRunning())
        {
            throw Refusal.ProPresenterIsRunning;
        }
        if (!FileIsAsLastRead)
        {
            throw Refusal.ChangedByAnother;
        }
        return safeguards;
    }

    // Saves a new state of the song, and only then makes it the song. In order:
    //
    //   1. A song with no safeguards saves nothing.
    //   2. Nothing is saved while ProPresenter is running.
    //   3. Nothing is saved over a file that something else has changed: the file must
    //      be, in its size, its time of last change AND its bytes, what was last read
    //      from it or written to it here. (A file brought down by a syncing service can
    //      come with its old time on it; and a song is small.)
    //   4. Before the first save, a copy of the file is kept; and before a mending,
    //      whatever was kept before (`always`: spec/07-backups.md, rule 3). If it
    //      cannot be, nothing is saved.
    //   5. The whole message is written beside the file and put in its place, and read
    //      back.
    private void Save(RV.Presentation changed, CopyReason? always = null)
    {
        var asked = MaySave();
        if (!copyKept || always is not null)
        {
            try
            {
                asked.KeepCopy(Path, always ?? CopyReason.BeforeAChange);
            }
            catch (Exception exception)
            {
                throw Refusal.CopyNotKept(exception.Message);
            }
            copyKept = true;
        }
        byte[] bytes;
        try
        {
            bytes = changed.ToByteArray();
            SongFile.Write(bytes, Path);
        }
        catch (SongFile.Failure failure)
        {
            throw Refusal.NotSaved(failure.Message);
        }
        presentation = changed;
        bytesOnDisk = bytes;
        // (If the file's stamp cannot be read now, the next save will be refused, which
        // is right: something is wrong with the file.)
        try
        {
            stamp = SongFile.StampOf(Path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SongFile.Failure)
        {
        }
    }
}
