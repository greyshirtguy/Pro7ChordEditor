// The ChordPro editor with no window in it: the song as one piece of text, what may be
// typed in it, the help it gives with brackets, and what a pause in the typing saves.
// spec/04-chordpro-editor.md is this file's specification, rule for rule; the reference
// is reference/simple-presenter/qml/ChordProEditor.qml (`composed`, `check`, `apply`,
// `chordAround`, and the key handler at its foot); and the Mac's is
// ChordProEditor.swift, from which this was ported.
//
// The window's part is a text box that asks here whether what it now holds may stand,
// and tells here when the typing has paused. Nothing there is a rule.
//
// The text is made by SongText (rules 1 to 3), which also says of every line what it
// is. Everything below goes by that, and counts in UTF-16 units: as the file counts
// along a song's words, and as a .NET string and a WPF text box count along theirs. What
// is typed is held against the song's words unit for unit (`Ordinal`), never by the
// rules of a language, which call equal what only looks alike.
//
// Where this parts from the reference, each said at the place:
//
//   - A name the song already had when it was opened is let through whatever it is
//     (rule 22). There, a song with one name that is no chord cannot be changed at all.
//   - A box is saved when what the text says of it is no longer what the text said when
//     it was made from the song (rules 14 and 24). There, it is saved when what the text
//     says is not what the FILE has, and the two can differ before anything is typed: a
//     name that is not tidy, a chord past the end of its words. Typing in one box would
//     then write every such box of the song.
//   - The rounds of one saving follow each other at once and are one step of undo
//     (rules 17 and 18). There the second round waits for the next pause.

using Pro7ChordEditor.ChordRules;

namespace Pro7ChordEditor.Song;

public sealed class ChordProEditor
{
    /// <summary>What became of something typed.</summary>
    public enum Verdict
    {
        /// <summary>It is let through, and the text is now that.</summary>
        Taken,

        /// <summary>
        /// It is not the song's words with only chords changed. The text stays as it was,
        /// and nothing is said: the caret not moving says it.
        /// </summary>
        NotTheWords,
    }

    /// <summary>
    /// A change to the text: the units from <c>Start</c> to <c>End</c> are to be replaced
    /// with this, and the caret left there.
    /// </summary>
    public readonly record struct Edit(int Start, int End, string With, int Caret);

    /// <summary>A stretch of the text, from its first unit to one past its last.</summary>
    public readonly record struct Stretch(int Start, int End);

    /// <summary>
    /// Where the text is to be coloured (rule 4): the chords, brackets and all, and the
    /// lines in curly brackets.
    /// </summary>
    public sealed record Colours(IReadOnlyList<Stretch> Chords, IReadOnlyList<Stretch> Greyed);

    public const string Help =
        "Type chords in square brackets among the words: [G]  [D/F#]  [Am7].  Only the chords can be changed here; the words are the slides'.";

    // (The page goes on: "The chord sheet can still be used.", but only once there is a
    // chord sheet, M6. Until then this ends where it does.)
    internal const string WordsHaveBrackets =
        "This song's words have square brackets of their own in them, so it cannot be changed as ChordPro text.";

    internal const string CannotBeSaidExactly =
        "This song cannot be changed as ChordPro text: one of its chords has a square bracket or a line break in its name, or its words have a broken character in them.";

    private const char LineBreak = Words.LineBreak;
    private const char OpenBracket = '[';
    private const char CloseBracket = ']';
    private const char OpenBrace = '{';

    private readonly HashSet<string> namesAtOpening;
    private IReadOnlyList<TextBox> boxes;

    public ChordProEditor(OpenSong song)
    {
        Song = song;
        boxes = song.TextBoxes;
        Lines = SongText.Lines(boxes);
        Text = Made(Lines);
        // The names the song's chords had when it was opened here.
        namesAtOpening = boxes.SelectMany(box => box.Chords).Select(chord => chord.Name).ToHashSet(StringComparer.Ordinal);
        Barred = WhyBarred(boxes, Lines, Text, namesAtOpening);
    }

    public OpenSong Song { get; }

    /// <summary>The text as it last passed rule 6: what the window shows.</summary>
    public string Text { get; private set; }

    /// <summary>What each line of the text is.</summary>
    public IReadOnlyList<SongLine> Lines { get; private set; }

    /// <summary>
    /// Why the song can be read here and not changed, as a sentence; null if it can be
    /// changed (rules 20 and 20a).
    /// </summary>
    public string? Barred { get; private set; }

    private static string Made(IReadOnlyList<SongLine> lines) => string.Join(LineBreak, lines.Select(line => line.Text));

    // ---- What may be typed

    /// <summary>Asks whether the text may become this, and if it may, takes it (rules 6 and 7).</summary>
    public Verdict Take(string typed)
    {
        if (Barred is not null || !Allows(typed, Lines, namesAtOpening))
        {
            return Verdict.NotTheWords;
        }
        Text = typed;
        return Verdict.Taken;
    }

    /// <summary>
    /// THE ONE RULE (rule 6). A change to the text is let through only if, with the
    /// chords taken out, the text is still the words it started as: it has the same
    /// number of lines, and for each line
    ///
    ///   - a group's line or a gap is exactly what it was;
    ///   - a line of words, with every "[...]" taken out, is exactly its words, and has
    ///     no stray bracket left in it;
    ///   - a line of chords alone (a blank slide's among them), with every "[...]" taken
    ///     out, is nothing but spaces;
    ///   - whatever is in any pair of brackets could become a chord, or is, exactly, a
    ///     name the song had when it was opened (rule 22).
    ///
    /// It covers typing in the wrong place, pasting, cutting, dropping and undoing
    /// alike. There is no rule for each.
    /// </summary>
    internal static bool Allows(string typed, IReadOnlyList<SongLine> lines, IReadOnlySet<string> names)
    {
        var typedLines = Split(typed);
        if (typedLines.Length != lines.Count)
        {
            return false;
        }
        for (var n = 0; n < lines.Count; n++)
        {
            var line = typedLines[n];
            var want = lines[n];
            if (want.Kind is SongLineKind.Group or SongLineKind.Gap)
            {
                if (!string.Equals(line, want.Text, StringComparison.Ordinal))
                {
                    return false;
                }
                continue;
            }
            var bare = Chords.WithoutChords(line);
            // (Kept as the reference has it. The last test below says as much by itself,
            // a song whose own words have brackets being barred.)
            if (bare.Contains(OpenBracket, StringComparison.Ordinal) || bare.Contains(CloseBracket, StringComparison.Ordinal))
            {
                return false;
            }
            foreach (var pair in Pairs(line))
            {
                var inside = line[(pair.Open + 1)..pair.Close];
                if (!names.Contains(inside) && !Chords.CouldBecome(Words.Trimmed(inside)))
                {
                    return false;
                }
            }
            if (want.Kind == SongLineKind.Words ? !string.Equals(bare, want.Words, StringComparison.Ordinal) : !Words.IsBlank(bare))
            {
                return false;
            }
        }
        return true;
    }

    // ---- Saving

    /// <summary>
    /// Saves the chords the text has, wherever they are not what the song has (rules 13
    /// to 18): each text box whose chords are no longer what the text says, as one
    /// change each, and all of them one step of undo. Answers whether anything was
    /// saved.
    ///
    /// Empty brackets, and ones that hold what is not yet a chord, are left out of what
    /// is saved and left in the text (rule 15): someone half way through typing "[F#"
    /// must not have it saved as a chord nor taken away from them.
    /// </summary>
    /// <exception cref="Refusal">A change was refused. What was saved before it stays saved.</exception>
    public bool Save()
    {
        if (Barred is not null)
        {
            return false;
        }
        var saved = false;
        try
        {
            // (A round for each line of chords alone at the most, and one more: should
            // the song ever not take what it was asked for a line, the asking stops.)
            for (var roundsLeft = Lines.Count + 1; roundsLeft > 0; roundsLeft--)
            {
                var typedLines = Split(Text);
                if (typedLines.Length != Lines.Count)
                {
                    return saved;
                }
                // A line of chords alone is saved as its chords' names in order (rule
                // 17). Since that can change its text box's words (the stand-ins), the
                // song is read again after it, and the other lines go in the next
                // round, which follows at once.
                var another = false;
                for (var n = 0; n < Lines.Count; n++)
                {
                    var want = Lines[n];
                    if (want.Kind is not (SongLineKind.ChordsAlone or SongLineKind.BlankSlide))
                    {
                        continue;
                    }
                    var names = ChordsSaid(typedLines[n]).Select(chord => chord.Name).ToList();
                    if (names.SequenceEqual(ChordsSaid(want.Text).Select(chord => chord.Name), StringComparer.Ordinal))
                    {
                        continue;
                    }
                    Song.SetChordsAlone(want.Slide, want.Element, want.Line, names, joined: saved);
                    saved = true;
                    Reload(keepingWhatIsTyped: true);
                    another = true;
                    break;
                }
                if (another)
                {
                    continue;
                }

                var madeLines = Lines.Select(line => line.Text).ToArray();
                for (var index = 0; index < boxes.Count; index++)
                {
                    var box = boxes[index];
                    if (box.Blank)
                    {
                        continue;
                    }
                    var said = ChordsSaid(index, typedLines);
                    // (Held against what the text said when it was made from the song,
                    // not against the file's own entries: see the head of this file.)
                    if (said.SequenceEqual(ChordsSaid(index, madeLines)))
                    {
                        continue;
                    }
                    // Its lines of chords alone keep theirs: they were dealt with above.
                    var alone = box.Chords.Where(chord => Lines.Any(line =>
                        line.Box == index && line.Kind == SongLineKind.ChordsAlone && chord.At >= line.Start && chord.At < line.Start + Math.Max(1, line.Words.Length)));
                    // (In order of place, those the text says before those of a line of
                    // chords alone where two stand at one place: a sort that keeps what
                    // is equal in the order it came.)
                    var wanted = said.Concat(alone).OrderBy(chord => chord.At).ToList();
                    Song.SetChords(box.Slide, box.Element, wanted, joined: saved);
                    saved = true;
                }
                return saved;
            }
            return saved;
        }
        finally
        {
            if (saved)
            {
                Reload(keepingWhatIsTyped: true);
            }
        }
    }

    /// <summary>
    /// Reads the song again (rule 19). When the song has changed because of what was
    /// just typed here, the text is left exactly as typed, if it still says what the
    /// song's words are. When it has changed from elsewhere, the text is made again
    /// from the song.
    /// </summary>
    public void Reload(bool keepingWhatIsTyped)
    {
        boxes = Song.TextBoxes;
        Lines = SongText.Lines(boxes);
        var made = Made(Lines);
        Barred = WhyBarred(boxes, Lines, made, namesAtOpening);
        if (keepingWhatIsTyped && Allows(Text, Lines, namesAtOpening))
        {
            return;
        }
        Text = made;
    }

    // ---- Help with the brackets (rules 9 to 11). Rule 12, that none of it happens
    // while Ctrl, Alt or the Windows key is down, is the window's to know.

    /// <summary>
    /// What typing a character does beyond being put in, if anything: "[" with nothing
    /// selected puts "[]" and leaves the caret between them; "]" where the next
    /// character is already "]" steps over it.
    /// </summary>
    /// <param name="caret">Where the caret is.</param>
    /// <param name="selected">How many units are selected from there.</param>
    public static Edit? HelpWithTyping(string typed, string text, int caret, int selected)
    {
        if (selected != 0)
        {
            return null;
        }
        if (typed == "[")
        {
            return new Edit(caret, caret, "[]", caret + 1);
        }
        if (typed == "]" && caret >= 0 && caret < text.Length && text[caret] == CloseBracket)
        {
            return new Edit(caret, caret, "", caret + 1);
        }
        return null;
    }

    /// <summary>
    /// Backspace just after a "[" or a "]" takes the whole chord, brackets and all.
    /// Null where the key is to do what it always does.
    /// </summary>
    public static Edit? HelpWithBackspace(string text, int caret, int selected)
    {
        if (selected != 0 || caret <= 0 || caret > text.Length)
        {
            return null;
        }
        var before = text[caret - 1];
        if (before is not (OpenBracket or CloseBracket) || ChordAround(before == OpenBracket ? caret : caret - 1, text) is not { } chord)
        {
            return null;
        }
        return new Edit(chord.Start, chord.End, "", chord.Start);
    }

    /// <summary>Delete just before a "[" or a "]" does the same.</summary>
    public static Edit? HelpWithDelete(string text, int caret, int selected)
    {
        if (selected != 0 || caret < 0 || caret >= text.Length)
        {
            return null;
        }
        var after = text[caret];
        if (after is not (OpenBracket or CloseBracket) || ChordAround(after == OpenBracket ? caret + 1 : caret, text) is not { } chord)
        {
            return null;
        }
        return new Edit(chord.Start, chord.End, "", chord.Start);
    }

    /// <summary>
    /// The chord a place is in or beside, from its "[" to past its "]"; null if the
    /// nearest "[" before the place is not on its line, or has no "]" on that line, or
    /// closes before the place.
    /// </summary>
    internal static Stretch? ChordAround(int position, string text)
    {
        // (The last such unit at or before a place, as the reference's `lastIndexOf`
        // finds it: a place before the start is taken as the start.)
        int LastIndexOf(char unit, int from)
        {
            for (var i = Math.Min(Math.Max(0, from), text.Length - 1); i >= 0; i--)
            {
                if (text[i] == unit)
                {
                    return i;
                }
            }
            return -1;
        }

        var lineStart = LastIndexOf(LineBreak, position - 1) + 1;
        var open = LastIndexOf(OpenBracket, position - 1);
        if (open < lineStart || open < 0)
        {
            return null;
        }
        var close = text.IndexOf(CloseBracket, open);
        if (close < 0)
        {
            return null;
        }
        var lineEnd = text.IndexOf(LineBreak, open);
        if ((lineEnd >= 0 && close > lineEnd) || close < position - 1)
        {
            return null;
        }
        return new Stretch(open, close + 1);
    }

    // ---- Which slide is picked (rule 21), and the colours (rule 4)

    /// <summary>The slide whose line a place in the text is on; null on a gap or a group's name.</summary>
    public int? SlideAt(int position)
    {
        var line = 0;
        for (var i = 0; i < Math.Min(Math.Max(0, position), Text.Length); i++)
        {
            if (Text[i] == LineBreak)
            {
                line++;
            }
        }
        if (line >= Lines.Count)
        {
            return null;
        }
        return Lines[line].Kind is SongLineKind.Group or SongLineKind.Gap ? null : Lines[line].Slide;
    }

    /// <summary>
    /// Where a slide's first line starts in the text: where the caret goes when the slide
    /// is picked from elsewhere.
    /// </summary>
    public int? StartOfSlide(int slide)
    {
        var typedLines = Split(Text);
        if (typedLines.Length != Lines.Count)
        {
            return null;
        }
        for (int n = 0, start = 0; n < Lines.Count; start += typedLines[n].Length + 1, n++)
        {
            if (Lines[n].Slide == slide && Lines[n].Kind is not (SongLineKind.Group or SongLineKind.Gap))
            {
                return start;
            }
        }
        return null;
    }

    /// <summary>
    /// Chords are coloured, brackets and all; the lines in curly brackets are greyed. The
    /// colouring never changes the text.
    /// </summary>
    public static Colours Colouring(string text)
    {
        var chords = new List<Stretch>();
        var greyed = new List<Stretch>();
        var start = 0;
        foreach (var line in Split(text))
        {
            if (line.Length > 0 && line[0] == OpenBrace)
            {
                greyed.Add(new Stretch(start, start + line.Length));
            }
            else
            {
                foreach (var pair in Pairs(line))
                {
                    chords.Add(new Stretch(start + pair.Open, start + pair.Close + 1));
                }
            }
            start += line.Length + 1;
        }
        return new Colours(chords, greyed);
    }

    // ---- How

    // A pair of brackets in a line: where its "[" and its "]" are, and the place it has
    // among the line's words.
    internal readonly record struct Pair(int Open, int Close, int At);

    // The text's lines. There is always one at least; text that ends with a line break
    // ends with an empty line.
    internal static string[] Split(string text) => text.Split(LineBreak);

    /// <summary>
    /// Every "[...]" of a line that has no bracket inside it, in order: what the
    /// reference finds with a pattern of that shape. In a line that passes rule 6 these
    /// are all its brackets, and they are the chords `Chords.ChordsOf` reads (bar the
    /// empty ones, which that leaves out), each at the same place.
    /// </summary>
    internal static List<Pair> Pairs(string line)
    {
        var found = new List<Pair>();
        var at = 0;
        for (var i = 0; i < line.Length;)
        {
            if (line[i] == OpenBracket)
            {
                var j = i + 1;
                while (j < line.Length && line[j] != OpenBracket && line[j] != CloseBracket)
                {
                    j++;
                }
                if (j < line.Length && line[j] == CloseBracket)
                {
                    found.Add(new Pair(i, j, at));
                    i = j + 1;
                    continue;
                }
            }
            at++;
            i++;
        }
        return found;
    }

    // The chords a line of the text says, each at the place it has among the line's
    // words, its name tidied: every bracket that holds a chord (`IsChord`), or a name
    // the song had when it was opened (rule 22). Empty brackets and half-typed ones are
    // left out (rule 15).
    private List<Chord> ChordsSaid(string line)
    {
        var said = new List<Chord>();
        foreach (var pair in Pairs(line))
        {
            var inside = line[(pair.Open + 1)..pair.Close];
            var name = Chords.Tidied(inside);
            if (name.Length > 0 && (namesAtOpening.Contains(inside) || Chords.IsChord(name)))
            {
                said.Add(new Chord(pair.At, name));
            }
        }
        return said;
    }

    // The chords the text says a box's lines of words have, each at its place along the
    // whole box. One typed after the last word of a line hangs on the line's last
    // character (rule 16; `Chords.Tidy` would put it there too when the box is written);
    // a line with no characters has none to hang one on.
    private List<Chord> ChordsSaid(int box, string[] typedLines)
    {
        var said = new List<Chord>();
        for (var n = 0; n < Lines.Count && n < typedLines.Length; n++)
        {
            var want = Lines[n];
            if (want.Box != box || want.Kind != SongLineKind.Words || want.Words.Length == 0)
            {
                continue;
            }
            foreach (var chord in ChordsSaid(typedLines[n]))
            {
                said.Add(new Chord(want.Start + Math.Min(chord.At, want.Words.Length - 1), chord.Name));
            }
        }
        return said;
    }

    // Rule 20, and what it does not think of (20a). If any text box's words have a "["
    // or a "]" in them, ChordPro text cannot tell them from chords. And if the text as
    // it is made from the song does not itself pass rule 6, nothing typed ever could: a
    // chord's name has a bracket or a line break in it. (NOT in the reference, where
    // such a song takes no change and says nothing.)
    //
    // (The page's second reason, words with half a character in them, is the Mac's: a
    // .NET string holds such words as they are, and the text made from them passes.
    // Whether a WPF text box keeps them as they are is for the window's milestone.)
    private static string? WhyBarred(IReadOnlyList<TextBox> boxes, IReadOnlyList<SongLine> lines, string text, IReadOnlySet<string> names)
    {
        if (boxes.Any(box => box.Words.Contains(OpenBracket, StringComparison.Ordinal) || box.Words.Contains(CloseBracket, StringComparison.Ordinal)))
        {
            return WordsHaveBrackets;
        }
        return Allows(text, lines, names) ? null : CannotBeSaidExactly;
    }
}
