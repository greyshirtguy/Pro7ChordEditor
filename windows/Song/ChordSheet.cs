// The chord sheet, with no window in it: the whole song as rows, the one spot the next
// chord goes to, and what each thing done at the spot asks of the song
// (spec/03-chord-sheet.md; reference: qml/ChordSheet.qml, whose functions these are,
// under the names they have there). Held to testdata/vectors/sheet-model.json.
//
// One sheet, where the file has slides. Every line of every text box is a row, one
// under another, with a row for a group's name over its first slide. Nothing is joined
// underneath: a chord put on a word is asked for in that word's own text box.
//
// The sheet changes nothing itself. Everything comes down to one of two requests to the
// song, each of which the song saves at once or refuses:
//
//   "this text box now has these chords"       SetChords
//   "this line of chords alone now has these"  SetChordsAlone
//
// and after each the rows are made again from the song, as the reference does when its
// document says it has changed. So nothing here remembers what the song has; it is
// asked.
//
// Where this departs from the reference, each time on purpose:
//
//   - A name the song ALREADY has is moved, copied and pasted as it is, whether or not
//     it is a chord ("N.C.", "C /// | Am ///"). Only what is newly typed is held to
//     being one (the page, "Chords that are not chords"). The reference holds a moved
//     name too, and so cannot move such a one.
//   - The changes of one paste are one step of undo whichever of its lines was the
//     first to be changed (rule 52). The reference joins every line but the first
//     COPIED to the change before, so that where the first is passed over (the kinds
//     differ) the paste is joined to whatever was done before it.
//   - A place is never the second half of a pair of units (a character from beyond the
//     first 65,536): the pointer and the keys pass over it. The page was silent.
//   - The empty lines a text box's words END with get no row, unless a chord stands on
//     one (the maintainer's, 2026-10-11). The reference gives each a row.
//
// And one thing is here that the reference has not: one of the key's chords carried in
// from the strip and let go on a place (the page, "A chord dragged out of the strip";
// the maintainer's, 2026-10-12). It is `LiftOfKey`, `Carry(name, row, at)`, `CarryOff`
// and `DropOfKey`, at the foot, and what it asks of the song is what a key's `Put` asks.
//
// What is drawn where is not here. The two things that go by where the words are on
// the screen (which place is under the pointer, and which stop is nearest above or
// below) are handed an `ISheetMeasure`, so that the window's answers and the tests' come
// by the same road.
//
// The Mac's is ChordSheet.swift, and this was ported from it with the reference beside.

using System.Globalization;
using Pro7ChordEditor.ChordRules;

namespace Pro7ChordEditor.Song;

/// <summary>
/// What the sheet asks of the song. An <see cref="OpenSong"/> is one; the tests have
/// another, which writes down what it was asked.
/// </summary>
public interface ISheetSong
{
    IReadOnlyList<TextBox> TextBoxes { get; }

    /// <summary>Every chord of the song, one entry for each time it is used.</summary>
    IReadOnlyList<string> UsedChords { get; }

    /// <summary>The key the chords are written in, or "" for none stated.</summary>
    string Key { get; }

    /// <exception cref="Refusal">The song does not make the change, and says why.</exception>
    void SetChords(int slide, string element, IReadOnlyList<Chord> chords, bool joined);

    /// <exception cref="Refusal">The song does not make the change, and says why.</exception>
    void SetChordsAlone(int slide, string element, int line, IReadOnlyList<string> names, bool joined);
}

public enum SheetRowKind
{
    /// <summary>The name of a group, over its first slide.</summary>
    Group,

    /// <summary>One line of one text box.</summary>
    Line,
}

/// <summary>One row of the sheet: the name of a group, or one line of one text box.</summary>
/// <param name="Slide">The slide, counted from 0.</param>
public sealed record SheetRow(SheetRowKind Kind, int Slide)
{
    /// <summary>A group's name and colour (a group row).</summary>
    public string Name { get; init; } = "";

    public Colour? Colour { get; init; }

    /// <summary>Which of the text boxes handed over this line is of, and which line of it.</summary>
    public int Block { get; init; }

    public int Line { get; init; }

    /// <summary>The text box's id. Empty for a slide that has no text box.</summary>
    public string Element { get; init; } = "";

    /// <summary>The line's words.</summary>
    public string Text { get; init; } = "";

    /// <summary>Where the line starts in its text box.</summary>
    public int Start { get; init; }

    /// <summary>The line's chords, each place counted ALONG THE LINE.</summary>
    public IReadOnlyList<Chord> Chords { get; init; } = [];

    /// <summary>A line of chords with no words: its chords hang on stand-ins, every other place.</summary>
    public bool Alone { get; init; }

    /// <summary>A blank slide's one line: alone, with no chords yet and no stand-ins.</summary>
    public bool Blank { get; init; }

    /// <summary>The first line of its slide, the last, and how many lines the slide has.</summary>
    public bool First { get; init; }

    public bool Last { get; init; }

    public int Count { get; init; }
}

/// <summary>A place on the sheet: a row and a place along its line. The row is -1 for nowhere.</summary>
public readonly record struct Spot(int Row, int At);

/// <summary>How wide things are, for the two questions that go by the screen.</summary>
public interface ISheetMeasure
{
    /// <summary>How wide the first so-many units of a row's words are.</summary>
    double Advance(int row, int units);

    /// <summary>How wide a chord's bubble is, margins and all.</summary>
    double Bubble(string name);

    /// <summary>
    /// How many times its usual size everything is drawn (the sheet can be zoomed): the
    /// few gaps the sheet itself keeps along a line are that many times theirs.
    /// </summary>
    double Scale => 1;
}

public sealed class ChordSheet
{
    /// <summary>
    /// The gap two bubbles are kept apart by. (How far past a bubble on a line of chords
    /// alone the next one is put, and from how far past one it is pointed at, are ten
    /// and five, where they are used.)
    /// </summary>
    public const double Gap = 4.0;

    /// <summary>How many of what is offered are shown, and how long what is typed may be.</summary>
    internal const int OfferedAtMost = 14;

    internal const int LongestTyped = 16;

    private Spot spot = new(-1, 0);

    public ChordSheet(ISheetSong song)
    {
        Song = song;
        Reload();
    }

    /// <summary>A chord by which word of its line it is on, and how far into that word.</summary>
    public readonly record struct OnAWord(int Word, int Offset, string Name);

    /// <summary>
    /// What was copied of one line: its chords by which word they are on, or the names of
    /// a line of chords alone.
    /// </summary>
    public abstract record CopiedLine;

    public sealed record CopiedWords(IReadOnlyList<OnAWord> Chords) : CopiedLine;

    public sealed record CopiedAlone(IReadOnlyList<string> Names) : CopiedLine;

    /// <summary>The box a chord is typed in.</summary>
    public sealed record TypingBox
    {
        /// <summary>What it holds.</summary>
        public string Text { get; init; } = "";

        /// <summary>
        /// What it last held that could become a chord: what a key press that would make
        /// it otherwise is undone to.
        /// </summary>
        internal string Last { get; init; } = "";

        /// <summary>Which of what is offered is picked, or -1.</summary>
        public int Pick { get; init; } = -1;

        /// <summary>What it was opened with, and whether all of that was selected to be typed over.</summary>
        public string Begun { get; init; } = "";

        public bool Selected { get; init; }
    }

    /// <summary>A key pressed in the box a chord is typed in, of those that are the sheet's to answer.</summary>
    public enum EntryKey
    {
        Escape,
        Enter,
        Tab,
        Down,
        Up,
        Right,
        Delete,
    }

    /// <summary>
    /// What a paste did, for saying so: a paste that changes nothing says nothing of
    /// itself otherwise, and there are three plain reasons it may not.
    /// </summary>
    /// <param name="Lines">The lines whose chords were replaced.</param>
    /// <param name="Chords">The chords that were put on them.</param>
    /// <param name="LeftOut">
    /// The chords that had no word to go on in the line they were for (or the place
    /// another of them had taken), and were left out.
    /// </param>
    /// <param name="PassedOver">
    /// The lines passed over, being of the other kind than what was copied for them:
    /// words, or chords alone.
    /// </param>
    /// <param name="Refused">The lines the song refused.</param>
    public readonly record struct Pasted(int Lines = 0, int Chords = 0, int LeftOut = 0, int PassedOver = 0, int Refused = 0);

    public ISheetSong Song { get; }

    public IReadOnlyList<TextBox> Blocks { get; private set; } = [];

    public IReadOnlyList<SheetRow> Rows { get; private set; } = [];

    public IReadOnlyList<string> Used { get; private set; } = [];

    /// <summary>How many lines the slide with the most has.</summary>
    public int MostLines { get; private set; } = 1;

    /// <summary>
    /// The spot: which row, and which character of its line. On a line of chords alone
    /// it is a chord's stand-in (every other character) or, past the last, a new one.
    /// </summary>
    public Spot Spot => spot;

    /// <summary>
    /// The bubble for typing a chord is open, and the place the chord being typed belongs
    /// to. That place is not the spot: the spot goes on following the pointer while a
    /// chord is typed.
    /// </summary>
    public bool Typing { get; private set; }

    public Spot TypingAt { get; private set; } = new(-1, 0);

    public TypingBox Entry { get; private set; } = new();

    /// <summary>
    /// Goes up each time the bubble is opened: what tells a chord's typing from the next
    /// one's at the same place.
    /// </summary>
    public int TypingsBegun { get; private set; }

    /// <summary>
    /// A chord being dragged: where from, and its name. One of the key's chords carried
    /// in from the strip is from nowhere on the sheet (its row is -1), and is held here
    /// only while it is over a place that can take it.
    /// </summary>
    public (Spot From, string Name)? Dragged { get; private set; }

    public IReadOnlyList<CopiedLine>? Copied { get; private set; }

    /// <summary>The slide that is picked: the one the spot was last moved to by the keys or a click.</summary>
    public int Picked { get; set; }

    /// <summary>How the key that pastes is named in what is said after a copy.</summary>
    public string PasteKeys { get; set; } = "Ctrl+V";

    /// <summary>Told when the song refused something, with why.</summary>
    public Action<Refusal> Refused { get; set; } = _ => { };

    /// <summary>Told when the song has taken a change.</summary>
    public Action Changed { get; set; } = () => { };

    /// <summary>Told when the spot was moved by the keys, and is to be brought into view.</summary>
    public Action Reveal { get; set; } = () => { };

    /// <summary>
    /// Whether a chord's sharps and flats are drawn as the music signs (spec/03, rule
    /// 10a): the setting, which the window's model gives the sheet. Off as a sheet is
    /// made, so that a sheet no window has made is as it always was. Only drawing: all
    /// that the sheet works out, and all it asks of the song, is the same either way.
    /// </summary>
    public bool Signs { get; set; }

    /// <summary>
    /// A chord's name, or a key's, as the sheet draws it: with signs, if they are drawn
    /// (<see cref="Chords.WithSigns"/>), or as it is.
    /// </summary>
    public string Drawn(string name) => Signs ? Chords.WithSigns(name) : name;

    /// <summary>
    /// What the box a chord is typed in shows: what it holds, drawn (spec/03, rule 30a).
    /// A sign is one unit, as the character it stands for is, so a place in what is
    /// shown is the same place in what is held.
    /// </summary>
    public string InTheBox => Drawn(Entry.Text);

    /// <summary>
    /// The key the chords are in. A song that names none is taken to be in C, which is
    /// only what the keys 1 to 7 give until the key is set.
    /// </summary>
    public string Key => Song.Key.Length == 0 ? "C" : Song.Key;

    /// <summary>The key's seven chords: what 1 to 7 put on.</summary>
    public IReadOnlyList<string> InKey => Chords.Diatonic(Key);

    // ---- The rows

    /// <summary>Makes the rows again from the song.</summary>
    public void Reload()
    {
        var read = Song.TextBoxes;
        var flat = new List<SheetRow>();
        var last = -1;
        for (var b = 0; b < read.Count; b++)
        {
            var block = read[b];
            var newSlide = block.Slide != last;
            if (newSlide && block.StartsItsGroup && block.Group.Length > 0)
            {
                flat.Add(new SheetRow(SheetRowKind.Group, block.Slide) { Name = block.Group, Colour = block.GroupColour });
            }
            last = block.Slide;
            var start = 0;
            var lines = WithoutEmptyEnd(Words.Lines(block.Words), block);
            for (var l = 0; l < lines.Count; l++)
            {
                var text = lines[l];
                // An empty line is taken as one place wide, so that a chord standing on
                // it is not lost.
                var from = start;
                var end = start + Math.Max(1, text.Length);
                flat.Add(new SheetRow(SheetRowKind.Line, block.Slide)
                {
                    Block = b,
                    Line = l,
                    Element = block.Element,
                    Text = text,
                    Start = start,
                    Chords = [.. block.Chords.Where(chord => chord.At >= from && chord.At < end).Select(chord => new Chord(chord.At - from, chord.Name))],
                    Alone = Chords.IsPlaceholders(text) || block.Blank,
                    Blank = block.Blank,
                    First = newSlide && l == 0,
                });
                start += text.Length + 1;
            }
        }
        // Each slide's lines know how many they are and which is the last, and the
        // sheet how many the longest slide has: what the cards are sized by.
        var most = 1;
        for (var first = 0; first < flat.Count;)
        {
            if (flat[first].Kind != SheetRowKind.Line)
            {
                first++;
                continue;
            }
            var to = first;
            while (to + 1 < flat.Count && flat[to + 1].Kind == SheetRowKind.Line && flat[to + 1].Slide == flat[first].Slide)
            {
                to++;
            }
            for (var index = first; index <= to; index++)
            {
                flat[index] = flat[index] with { Count = to - first + 1, Last = index == to };
            }
            most = Math.Max(most, to - first + 1);
            first = to + 1;
        }
        Blocks = read;
        MostLines = most;
        Rows = flat;
        Used = Song.UsedChords;
        if (spot.Row >= Rows.Count || (spot.Row >= 0 && !Usable(spot.Row)))
        {
            spot = spot with { Row = -1 };
        }
        // The line a chord was being typed on may have gone (an undo, a copy put back).
        if (Typing && !Usable(TypingAt.Row))
        {
            Typing = false;
        }
        if (Dragged is { } lifted)
        {
            // The chord that was lifted has gone from where it was; or the place one
            // from the strip was over has.
            var gone = lifted.From.Row >= 0
                ? !string.Equals(ChordAt(lifted.From.Row, lifted.From.At), lifted.Name, StringComparison.Ordinal)
                : !Usable(spot.Row);
            if (gone)
            {
                Dragged = null;
            }
        }
    }

    /// <summary>
    /// A text box's lines without the empty ones its words end with: those get no row. A
    /// slide whose words end with a line break or several would otherwise count as that
    /// many lines taller. An empty line that a chord stands on is kept, and so are those
    /// before it, so that the chord is not lost from sight; and a box always has one
    /// line.
    /// </summary>
    internal static IReadOnlyList<string> WithoutEmptyEnd(IReadOnlyList<string> lines, TextBox block)
    {
        var kept = lines.ToList();
        var start = lines.Sum(line => line.Length + 1);
        while (kept.Count > 1 && kept[^1].Length == 0)
        {
            // (Where the line starts: the whole length so far, less its own place.)
            start--;
            var from = start;
            if (block.Chords.Any(chord => chord.At >= from))
            {
                break;
            }
            kept.RemoveAt(kept.Count - 1);
        }
        return kept;
    }

    // ---- Where a chord can go

    /// <summary>
    /// A line with words on it, or a blank slide's, which has none and is there to be
    /// given chords. An empty line among a slide's words is neither.
    /// </summary>
    public bool Usable(int index) =>
        index >= 0 && index < Rows.Count && Rows[index].Kind == SheetRowKind.Line && (Rows[index].Text.Length > 0 || Rows[index].Blank);

    /// <summary>Where the words of a line start.</summary>
    public static IReadOnlyList<int> WordStarts(string text)
    {
        var starts = new List<int>();
        var inWord = false;
        for (var place = 0; place < text.Length; place++)
        {
            var space = IsWordSpace(text[place]);
            if (!space && !inWord)
            {
                starts.Add(place);
            }
            inWord = !space;
        }
        return starts;
    }

    /// <summary>
    /// The places on a line the spot stops at: the starts of its words, or on a line of
    /// chords alone each chord and one more for a new one (which on a blank slide's line
    /// is the only one).
    /// </summary>
    public IReadOnlyList<int> Stops(int index)
    {
        var line = Rows[index];
        return line.Alone ? [.. Enumerable.Range(0, line.Chords.Count + 1).Select(slot => slot * 2)] : WordStarts(line.Text);
    }

    /// <summary>The stop at or before a character, which is the word it is in.</summary>
    public int StopAt(int index, int at)
    {
        var all = Stops(index);
        var best = all.Count > 0 ? all[0] : 0;
        foreach (var stop in all)
        {
            if (stop <= at)
            {
                best = stop;
            }
        }
        return best;
    }

    /// <summary>The name of the chord at a place, or "" for none.</summary>
    public string ChordAt(int index, int at)
    {
        if (!Usable(index))
        {
            return "";
        }
        foreach (var chord in Rows[index].Chords)
        {
            if (chord.At == at)
            {
                return chord.Name;
            }
        }
        return "";
    }

    public void SetSpot(int index, int at)
    {
        if (Usable(index))
        {
            spot = new Spot(index, at);
        }
    }

    /// <summary>
    /// Moves the spot along the line and on to the next, by stops or, <paramref name="exact"/>,
    /// by characters (the keys Right and Left, and with Shift).
    /// </summary>
    public void Step(int delta, bool exact)
    {
        if (!Usable(spot.Row))
        {
            for (var index = 0; index < Rows.Count; index++)
            {
                if (Usable(index))
                {
                    var stops = Stops(index);
                    SetSpot(index, stops.Count > 0 ? stops[0] : 0);
                    break;
                }
            }
            return;
        }
        var line = Rows[spot.Row];
        List<int> all = exact && !line.Alone
            ? [.. Enumerable.Range(0, line.Text.Length).Where(place => line.Text[place] != ' ' && !IsSecondOfAPair(line.Text[place]))]
            : [.. Stops(spot.Row)];
        var at = spot.At;
        var place = all.IndexOf(at);
        if (place < 0)
        {
            place = all.FindIndex(stop => stop > at) - (delta > 0 ? 1 : 0);
        }
        place += delta;
        if (place >= 0 && place < all.Count)
        {
            spot = spot with { At = all[place] };
        }
        else
        {
            // Off the end of the line: the first stop of the next one, or the last of
            // the one before.
            var next = spot.Row + delta;
            while (next >= 0 && next < Rows.Count && !Usable(next))
            {
                next += delta;
            }
            if (Usable(next))
            {
                var there = Stops(next);
                SetSpot(next, there.Count == 0 ? 0 : delta > 0 ? there[0] : there[^1]);
            }
        }
        RevealSpot();
    }

    /// <summary>
    /// Moves the spot to the line above or below, to the stop nearest under or over it on
    /// the screen (the keys Down and Up).
    /// </summary>
    public void StepLine(int delta, ISheetMeasure measure)
    {
        if (!Usable(spot.Row))
        {
            Step(1, exact: false);
            return;
        }
        var next = spot.Row + delta;
        while (next >= 0 && next < Rows.Count && !Usable(next))
        {
            next += delta;
        }
        if (!Usable(next))
        {
            return;
        }
        var x = XOf(spot.Row, spot.At, measure);
        var best = 0;
        var distance = 1e9;
        foreach (var stop in Stops(next))
        {
            var off = Math.Abs(XOf(next, stop, measure) - x);
            if (off < distance)
            {
                distance = off;
                best = stop;
            }
        }
        SetSpot(next, best);
        RevealSpot();
    }

    /// <summary>
    /// The keys have moved the spot: its slide is the one picked, and it is to be brought
    /// into view. (Asked from outside too, by a view that works out for itself which line
    /// is above or below on the screen.)
    /// </summary>
    public void RevealSpot()
    {
        if (Usable(spot.Row))
        {
            Picked = Rows[spot.Row].Slide;
        }
        Reveal();
    }

    // ---- Where things are along a line

    /// <summary>Where the chords of a row stand.</summary>
    public ChordLayout.Placed Placed(int index, ISheetMeasure measure)
    {
        var line = Rows[index];
        return ChordLayout.Place(line.Text.Length, line.Chords, units => measure.Advance(index, units), measure.Bubble, Gap * measure.Scale);
    }

    /// <summary>
    /// Where a place on a line is, from the line's left edge: a chord's bubble if one is
    /// there, the character if not, and on a line of chords alone the next free place for
    /// a new one.
    /// </summary>
    public double XOf(int index, int at, ISheetMeasure measure)
    {
        var line = Rows[index];
        var placed = Placed(index, measure);
        for (var found = 0; found < line.Chords.Count; found++)
        {
            if (line.Chords[found].At == at)
            {
                return placed.Xs[found];
            }
        }
        if (line.Alone)
        {
            var last = line.Chords.Count - 1;
            return last < 0 ? 0 : placed.Xs[last] + placed.Widths[last] + (10 * measure.Scale);
        }
        return measure.Advance(index, Math.Min(Math.Max(at, 0), line.Text.Length));
    }

    /// <summary>
    /// The place at a distance along a line: <paramref name="exact"/> the character
    /// there, or else the word it is in. Over the space between two words it is the word
    /// after that is meant.
    /// </summary>
    public int PlaceAt(int index, double along, bool exact, ISheetMeasure measure)
    {
        var line = Rows[index];
        if (line.Alone)
        {
            var placed = Placed(index, measure);
            for (var slot = 0; slot < line.Chords.Count; slot++)
            {
                if (along < placed.Xs[slot] + placed.Widths[slot] + (5 * measure.Scale))
                {
                    return slot * 2;
                }
            }
            return line.Chords.Count * 2;
        }
        var length = line.Text.Length;
        var character = ChordLayout.CharacterAt(length, Math.Max(0, along), units => measure.Advance(index, units));
        if (character >= length)
        {
            return 0;
        }
        if (IsSecondOfAPair(line.Text[character]) && character > 0)
        {
            character--;
        }
        if (exact && line.Text[character] == ' ' && character + 1 < length && line.Text[character + 1] != ' ')
        {
            character++;
        }
        return exact ? character : StopAt(index, character);
    }

    // ---- Changing chords

    // Asks the song, says so if it refuses, and reads the song again either way.
    private bool Ask(Action request)
    {
        var done = true;
        try
        {
            request();
        }
        catch (Refusal refusal)
        {
            done = false;
            Refused(refusal);
        }
        Reload();
        if (done)
        {
            Changed();
        }
        return done;
    }

    // Gives a line of words these chords in place of its own.
    //
    // (An empty line is one place wide here, as it is where the rows are made, and as
    // the reference has it in both. Nothing gets here with one as things are: the spot
    // cannot be on an empty line, so its chords are never set. It is for the day one
    // can be.)
    private bool SetLine(int index, IEnumerable<Chord> chords, bool joined)
    {
        var line = Rows[index];
        var block = Blocks[line.Block];
        var end = line.Start + Math.Max(1, line.Text.Length);
        List<Chord> all =
        [
            .. block.Chords.Where(chord => chord.At < line.Start || chord.At >= end),
            .. chords.Select(chord => new Chord(chord.At + line.Start, chord.Name)),
        ];
        return Ask(() => Song.SetChords(line.Slide, line.Element, all, joined));
    }

    /// <summary>
    /// Puts a chord on a place, in place of the one there if there is one.
    /// <paramref name="typed"/>: the name is newly typed, and so must be a chord; a name
    /// the song already had is put as it is.
    /// </summary>
    public bool Put(int index, int at, string name, bool joined = false, bool typed = true)
    {
        if (typed)
        {
            // A slash typed on the way to a bass note that never came is not part of the
            // chord.
            name = name.TrimEnd('/');
            if (!Chords.IsChord(name))
            {
                return false;
            }
        }
        if (!Usable(index) || name.Length == 0)
        {
            return false;
        }
        var line = Rows[index];
        if (line.Alone)
        {
            var names = line.Chords.Select(chord => chord.Name).ToList();
            var slot = Math.Min(names.Count, Slot(at));
            if (slot < names.Count)
            {
                names[slot] = name;
            }
            else
            {
                names.Add(name);
            }
            return Ask(() => Song.SetChordsAlone(line.Slide, line.Element, line.Line, names, joined));
        }
        return SetLine(index, [.. line.Chords.Where(chord => chord.At != at), new Chord(at, name)], joined);
    }

    /// <summary>Takes off the chord at a place.</summary>
    public bool Take(int index, int at, bool joined = false)
    {
        if (ChordAt(index, at).Length == 0)
        {
            return false;
        }
        var line = Rows[index];
        if (line.Alone)
        {
            var names = line.Chords.Select(chord => chord.Name).ToList();
            if (Slot(at) >= 0 && Slot(at) < names.Count)
            {
                names.RemoveAt(Slot(at));
            }
            return Ask(() => Song.SetChordsAlone(line.Slide, line.Element, line.Line, names, joined));
        }
        return SetLine(index, line.Chords.Where(chord => chord.At != at), joined);
    }

    /// <summary>Moves a chord from one place to another, or with <paramref name="copy"/> puts another there.</summary>
    public void Move(Spot from, Spot to, bool copy)
    {
        var name = ChordAt(from.Row, from.At);
        if (name.Length == 0 || from == to)
        {
            return;
        }
        var line = Rows[from.Row];
        if (!copy && from.Row == to.Row && !line.Alone)
        {
            // Along its own line: one change.
            SetLine(from.Row, [.. line.Chords.Where(chord => chord.At != from.At && chord.At != to.At), new Chord(to.At, name)], joined: false);
        }
        else if (!copy && from.Row == to.Row)
        {
            // Along a line of chords alone: it changes places.
            var names = line.Chords.Select(chord => chord.Name).ToList();
            if (Slot(from.At) >= 0 && Slot(from.At) < names.Count)
            {
                names.RemoveAt(Slot(from.At));
            }
            names.Insert(Math.Min(names.Count, Slot(to.At)), name);
            Ask(() => Song.SetChordsAlone(line.Slide, line.Element, line.Line, names, joined: false));
        }
        else if (Put(to.Row, to.At, name, joined: false, typed: false) && !copy)
        {
            // To another line: put there, then taken from here, as one thing to undo.
            Take(from.Row, from.At, joined: true);
        }
        SetSpot(to.Row, to.At);
    }

    // Which chord of a line of chords alone a place is: places there are 0, 2, 4 ...
    private static int Slot(int at) => (at + 1) / 2;

    // ---- Copying a line's chords, or a group's, to other lines

    /// <summary>
    /// Copies the chords of the spot's line, or <paramref name="whole"/> of its whole
    /// group: every line from the group's name down to the next group's. Answers with
    /// what to say; with nothing, if there is no spot.
    /// </summary>
    public string? Copy(bool whole)
    {
        if (!Usable(spot.Row))
        {
            return null;
        }
        var from = spot.Row;
        var to = spot.Row;
        if (whole)
        {
            while (from > 0 && Rows[from - 1].Kind != SheetRowKind.Group)
            {
                from--;
            }
            while (to + 1 < Rows.Count && Rows[to + 1].Kind != SheetRowKind.Group)
            {
                to++;
            }
        }
        var lines = new List<CopiedLine>();
        var count = 0;
        for (var index = from; index <= to; index++)
        {
            if (!Usable(index))
            {
                continue;
            }
            var line = Rows[index];
            count += line.Chords.Count;
            if (line.Alone)
            {
                lines.Add(new CopiedAlone([.. line.Chords.Select(chord => chord.Name)]));
                continue;
            }
            var starts = WordStarts(line.Text);
            lines.Add(new CopiedWords([.. line.Chords.Select(chord =>
            {
                // A chord standing on the space before a word counts as on the word
                // before.
                var word = 0;
                for (var w = 0; w < starts.Count; w++)
                {
                    if (starts[w] <= chord.At)
                    {
                        word = w;
                    }
                }
                return new OnAWord(word, chord.At - (word < starts.Count ? starts[word] : 0), chord.Name);
            })]));
        }
        Copied = lines;
        return Number(count) + (count == 1 ? " chord" : " chords") + " copied from " + (lines.Count == 1 ? "the line" : Number(lines.Count) + " lines")
            + ": " + PasteKeys + " puts them on the lines from the spot's on, word for word";
    }

    /// <summary>
    /// Puts what was copied on the lines from the spot's on, one copied line to each line
    /// that can take the spot. Answers with what it did; with nothing, if nothing has
    /// been copied or there is no spot.
    /// </summary>
    public Pasted? Paste()
    {
        if (Copied is not { } copied || !Usable(spot.Row))
        {
            return null;
        }
        var pasted = default(Pasted);
        var index = spot.Row;
        // However many lines it changes, a paste undoes as one: each change after the
        // first that was made is joined to it.
        var made = false;
        foreach (var line in copied)
        {
            while (index < Rows.Count && !Usable(index))
            {
                index++;
            }
            if (index >= Rows.Count)
            {
                break;
            }
            var target = Rows[index];
            switch (line)
            {
                // A copied line of chords alone goes only onto a line of chords alone.
                case CopiedAlone alone when target.Alone:
                    var joined = made;
                    if (Ask(() => Song.SetChordsAlone(target.Slide, target.Element, target.Line, alone.Names, joined)))
                    {
                        made = true;
                        pasted = pasted with { Lines = pasted.Lines + 1, Chords = pasted.Chords + alone.Names.Count };
                    }
                    else
                    {
                        pasted = pasted with { Refused = pasted.Refused + 1 };
                    }
                    break;
                case CopiedWords words when !target.Alone:
                    var starts = WordStarts(target.Text);
                    var chords = new List<Chord>();
                    foreach (var chord in words.Chords)
                    {
                        if (chord.Word >= starts.Count)
                        {
                            continue;
                        }
                        // On the same numbered word, as far into it as it was but not
                        // past the word's last letter.
                        var start = starts[chord.Word];
                        var end = chord.Word + 1 < starts.Count ? starts[chord.Word + 1] - 1 : target.Text.Length;
                        var at = Math.Min(start + chord.Offset, Math.Max(start, end - 1));
                        if (!chords.Any(one => one.At == at))
                        {
                            chords.Add(new Chord(at, chord.Name));
                        }
                    }
                    if (SetLine(index, chords, made))
                    {
                        made = true;
                        pasted = pasted with
                        {
                            Lines = pasted.Lines + 1,
                            Chords = pasted.Chords + chords.Count,
                            LeftOut = pasted.LeftOut + words.Chords.Count - chords.Count,
                        };
                    }
                    else
                    {
                        pasted = pasted with { Refused = pasted.Refused + 1 };
                    }
                    break;
                default:
                    pasted = pasted with { PassedOver = pasted.PassedOver + 1 };
                    break;
            }
            index++;
        }
        return pasted;
    }

    /// <summary>
    /// Whether a paste at the spot would take chords off to put the copied ones on:
    /// whether any line that what was copied would go onto has chords of its own ("The
    /// line's own chords are replaced by these, all of them"). The lines are found as
    /// <see cref="Paste"/> finds them, and one that would be passed over, being of the
    /// other kind, loses nothing. Nothing is asked of the song. (For the line of
    /// hints, which says so before the key is pressed: spec/03, rule 53g.)
    /// </summary>
    public bool PasteWouldReplace
    {
        get
        {
            if (Copied is not { } copied || !Usable(spot.Row))
            {
                return false;
            }
            var index = spot.Row;
            foreach (var line in copied)
            {
                while (index < Rows.Count && !Usable(index))
                {
                    index++;
                }
                if (index >= Rows.Count)
                {
                    break;
                }
                var target = Rows[index];
                var ofOneKind = line is CopiedAlone ? target.Alone : !target.Alone;
                if (ofOneKind && target.Chords.Count > 0)
                {
                    return true;
                }
                index++;
            }
            return false;
        }
    }

    // ---- Typing a chord

    /// <summary>What the chord being typed might be going to be: the first fourteen.</summary>
    public IReadOnlyList<string> Offered =>
        Typing ? [.. Chords.Completions(Entry.Text, Key, Used).Take(OfferedAtMost)] : [];

    /// <summary>
    /// Opens the bubble on the spot: with <paramref name="seed"/> to go on from (the
    /// letter that was pressed), or without, on the chord that is there, all of it picked
    /// to be typed over.
    /// </summary>
    public void StartTyping(string? seed = null)
    {
        if (!Usable(spot.Row))
        {
            return;
        }
        TypingAt = spot;
        Typing = true;
        var text = seed ?? ChordAt(spot.Row, spot.At);
        Entry = new TypingBox { Text = text, Last = text, Pick = -1, Begun = text, Selected = seed is null };
        TypingsBegun++;
    }

    /// <summary>
    /// What was typed in the box, as the box would have it now. Only what could be a
    /// chord gets in, and its notes are made capitals as they are typed. Answers with
    /// what the box is to show: what was typed tidied, or what it held before, drawn.
    ///
    /// While the signs are drawn, the box shows them, and what it shows is read with
    /// each sign as "#" or "b" (<see cref="Chords.WithoutSigns"/>) before anything else:
    /// so the box holds what it always held, and a sign typed by hand is taken as "#" or
    /// "b" (spec/03, rule 30a). Off, it is as it was before there were signs.
    /// </summary>
    public string Type(string text)
    {
        var tidy = Chords.Tidied(Signs ? Chords.WithoutSigns(text) : text);
        if (!Chords.CouldBecome(tidy) || tidy.Length > LongestTyped)
        {
            Entry = Entry with { Text = Entry.Last };
            return InTheBox;
        }
        Entry = Entry with { Text = tidy, Last = tidy, Pick = -1 };
        return InTheBox;
    }

    /// <summary>What Enter takes: the one picked from what is offered, or what is typed.</summary>
    public string Taken()
    {
        var offered = Offered;
        return Entry.Pick >= 0 && Entry.Pick < offered.Count ? offered[Entry.Pick] : Chords.Tidied(Entry.Text);
    }

    /// <summary>
    /// Ends the typing. With a name, that chord goes on the place it was being typed for;
    /// with none, nothing is changed (Esc). <paramref name="onward"/> then moves the spot
    /// on from that place to the next word (Tab), wherever the pointer has been
    /// meanwhile.
    ///
    /// With a name that is empty, the box was kept with nothing in it, and that is
    /// taken at its word: no chord is to be there, and the one that is goes. (spec/03,
    /// rule 35a, the maintainer's, 2026-10-13: until then an empty box that was kept
    /// changed nothing, so a chord whose name had been cleared came back.) Where there
    /// is no chord, there is nothing to take and the song is asked nothing.
    /// </summary>
    public void FinishTyping(string? name, bool onward)
    {
        var at = TypingAt;
        Typing = false;
        if (name is not null)
        {
            if (name.Length == 0)
            {
                Take(at.Row, at.At);
            }
            else
            {
                Put(at.Row, at.At, name);
            }
        }
        if (onward)
        {
            SetSpot(at.Row, at.At);
            Step(1, exact: false);
        }
    }

    /// <summary>
    /// Keeps the chord that is being typed, if one is: what a click anywhere else does,
    /// and what leaving the sheet does, so that going on to the next thing never loses
    /// it.
    /// </summary>
    public void Settle()
    {
        if (Typing)
        {
            FinishTyping(Taken(), onward: false);
        }
    }

    /// <summary>
    /// A click on a place of a line: the chord being typed elsewhere is kept, and the
    /// bubble opens here, on the chord that is here or for a new one.
    /// </summary>
    /// <remarks>
    /// It is done by row and place because keeping a chord makes every row again, the one
    /// that was clicked with it.
    /// </remarks>
    public void Pick(int index, int at)
    {
        // A new chord at the end of a line of chords alone is one place further along
        // once the chord that was being typed on that same line has gone in before it.
        var fresh = Usable(index) && Rows[index].Alone && at >= Rows[index].Chords.Count * 2;
        Settle();
        if (!Usable(index))
        {
            return;
        }
        SetSpot(index, fresh ? Rows[index].Chords.Count * 2 : at);
        Picked = Rows[index].Slide;
        StartTyping();
    }

    /// <summary>
    /// A key pressed in the box, with whether the caret is at the box's end. Answers
    /// whether it was the sheet's to answer; if not, the box does with it what a box
    /// does.
    /// </summary>
    public bool Press(EntryKey key, bool caretAtEnd = true)
    {
        if (!Typing)
        {
            return false;
        }
        var offered = Offered;
        switch (key)
        {
            case EntryKey.Escape:
                FinishTyping(null, onward: false);
                break;
            case EntryKey.Enter:
                FinishTyping(Taken(), onward: false);
                break;
            case EntryKey.Tab:
                // The first offered, if nothing is picked and nothing typed is a chord yet.
                if (Entry.Pick < 0 && !Chords.IsChord(Entry.Text) && offered.Count > 0)
                {
                    Entry = Entry with { Pick = 0 };
                }
                FinishTyping(Taken(), onward: true);
                break;
            case EntryKey.Down:
                Entry = Entry with { Pick = offered.Count == 0 ? -1 : (Entry.Pick + 1) % offered.Count };
                break;
            case EntryKey.Up:
                Entry = Entry with { Pick = offered.Count == 0 ? -1 : (Entry.Pick + offered.Count - 1 + (Entry.Pick < 0 ? 1 : 0)) % offered.Count };
                break;
            case EntryKey.Right:
                // The one picked goes into the box to be gone on with (for a bass note, say).
                if (Entry.Pick < 0 || Entry.Pick >= offered.Count || !caretAtEnd)
                {
                    return false;
                }
                Entry = Entry with { Text = offered[Entry.Pick], Last = offered[Entry.Pick], Pick = -1 };
                break;
            case EntryKey.Delete:
                // Nothing left of it: the chord goes.
                if (Entry.Text.Length > 0)
                {
                    return false;
                }
                var at = TypingAt;
                Typing = false;
                Take(at.Row, at.At);
                break;
            default:
                return false;
        }
        return true;
    }

    // ---- The keys, with no chord being typed

    /// <summary>
    /// 1 to 7: the key's own chord for that note of its scale, on the spot. (A click on
    /// one in the strip does the same; while a chord is being typed it goes where the
    /// typing was, in place of what was typed.)
    /// </summary>
    public void PutOfKey(int number)
    {
        var chords = InKey;
        if (chords.Count != 7 || number < 1 || number > 7)
        {
            return;
        }
        if (Typing)
        {
            FinishTyping(chords[number - 1], onward: false);
        }
        else
        {
            Put(spot.Row, spot.At, chords[number - 1]);
        }
    }

    /// <summary>Delete: the chord at the spot goes.</summary>
    public void TakeAtSpot() => Take(spot.Row, spot.At);

    // ---- The pointer

    /// <summary>
    /// The pointer has moved over a place. The spot follows it, unless a chord is being
    /// carried (which moves the spot itself).
    /// </summary>
    public void PointerOver(int index, int at)
    {
        if (Dragged is null)
        {
            SetSpot(index, at);
        }
    }

    /// <summary>A chord is picked up to be carried. Not while another is being typed.</summary>
    public void Lift(int index, int at)
    {
        var name = ChordAt(index, at);
        if (!Typing && name.Length > 0)
        {
            Dragged = (new Spot(index, at), name);
        }
    }

    /// <summary>The chord being carried is over a place.</summary>
    public void Carry(int index, int at)
    {
        if (Dragged is not null)
        {
            SetSpot(index, at);
        }
    }

    /// <summary>
    /// It is let go: it moves to the spot, or with <paramref name="copy"/> another is put
    /// there. (One that came from the strip is put there, whatever <paramref name="copy"/>
    /// is: rule 46g.)
    /// </summary>
    public void Drop(bool copy)
    {
        if (Dragged is not { } lifted)
        {
            return;
        }
        if (lifted.From.Row < 0)
        {
            DropOfKey();
            return;
        }
        Dragged = null;
        Move(lifted.From, spot, copy);
    }

    public void DropNowhere() => Dragged = null;

    // ---- A chord carried in from the strip (rules 46a to 46g)

    /// <summary>
    /// One of the key's chords is picked up from the strip, to be carried to a place. A
    /// chord that is being typed is kept first, as it is when the sheet is left for
    /// anything else: so one of these can be carried while another is typed, which a
    /// chord of the sheet's own cannot.
    /// </summary>
    public void LiftOfKey() => Settle();

    /// <summary>
    /// It is over a place: the spot goes there and shows its name, as for a chord lifted
    /// from the sheet. Only a chord is carried so, and never while one is being typed
    /// (picking it up ended that).
    /// </summary>
    public void Carry(string name, int index, int at)
    {
        if (Typing || !Usable(index) || !Chords.IsChord(name))
        {
            CarryOff();
            return;
        }
        Dragged = (new Spot(-1, 0), name);
        SetSpot(index, at);
    }

    /// <summary>
    /// It is over a place no longer: bare sheet, a frame, the strip, anything else. No
    /// place shows it, and let go now it is put nowhere. (A chord lifted from the sheet
    /// is not let go of by this.)
    /// </summary>
    public void CarryOff()
    {
        if (Dragged is { From.Row: < 0 })
        {
            Dragged = null;
        }
    }

    /// <summary>
    /// It is let go. Over a place it is put there, as its key would have put it with the
    /// spot there, in place of whatever chord is there; anywhere else nothing is done.
    /// Answers whether the song took it.
    /// </summary>
    public bool DropOfKey()
    {
        if (Dragged is not { From.Row: < 0 } lifted)
        {
            return false;
        }
        Dragged = null;
        return Put(spot.Row, spot.At, lifted.Name);
    }

    // ---- How

    // A number as it is written in a sentence, the same on every PC.
    private static string Number(int number) => number.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// White space, as the reference's sheet parts a line into words by it (JavaScript's
    /// own notion: its <c>\S+</c>). A zero-width space is not white space, so a stand-in
    /// is a "word"; an em quad is.
    /// </summary>
    internal static bool IsWordSpace(char unit) =>
        unit is (>= (char)0x0009 and <= (char)0x000D) or (char)0x0020 or (char)0x00A0 or (char)0x1680
            or (>= (char)0x2000 and <= (char)0x200A) or (char)0x2028 or (char)0x2029 or (char)0x202F or (char)0x205F or (char)0x3000 or (char)0xFEFF;

    /// <summary>The second unit of a character that takes two.</summary>
    internal static bool IsSecondOfAPair(char unit) => unit is >= (char)0xDC00 and <= (char)0xDFFF;
}
