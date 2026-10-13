// The line of hints under the chord sheet: what the keys and the mouse do just now,
// where the spot is. spec/03-chord-sheet.md, "The line of hints", rules 53a to 53h.
//
// It took the place of one sentence on the quiet line that never changed and said
// everything at once. (The maintainer, 2026-10-13, after two days with the Mac's sheet:
// "I did not realise I could hover and press delete".) So it says less, and says what
// goes with what is under the pointer:
//
//   on a chord            Delete takes it off; 1 to 7 change it; it can be dragged,
//                         and with Ctrl a copy of it; a click opens it to be typed over;
//   on a place with none  1 to 7 put one on; a letter or a click types one;
//   while one is typed    what Enter, Tab and Esc do with it. With the box empty on a
//                         chord, that Enter takes the chord off;
//   while one is carried  that letting go moves it, and Ctrl leaves a copy; or, for one
//                         of the key's chords from the strip, puts it there;
//   with Ctrl held        the copying of a line's chords onto another: Ctrl+C and
//                         Ctrl+V, which no picture on the sheet shows;
//   over the key's chords that they can be dragged down onto a slide;
//   the pointer elsewhere where to point to be told.
//
// It says them when they are of use, and at no other time: NOTHING IS SAID BY THE
// CLOCK (rule 53d).
//
// Each begins with a capital, as a sentence does, whether or not it is a whole
// sentence (rule 53e). The keys are named in words, as Windows names them
// (spec/10-platforms.md): Delete, Enter, Tab, Ctrl+C. The Mac's has their signs.
//
// ONE KEY IS TWO THINGS ON WINDOWS. On the Mac the key for shortcuts is Command and the
// key that copies a chord as it is dragged is Option. Here both are Ctrl. So Ctrl held
// at a line brings up the copying of the line's chords, as the page says it is to; and
// Ctrl held while a chord is carried says that letting go leaves a copy. Carrying is
// asked first, so the two never meet.
//
// THE HIGHLIGHTED LINE. The copying is of a line and onto a line, and the hint for it
// has to say which. So the line the person is at is drawn in a colour of its own on
// the sheet (`Line`, below, says which line that is; the sheet's element draws it), and
// the hint calls it "the highlighted line".
//
// No window in this file, and no rule of the song: what a key DOES is ChordSheet's.
// This only says it, and is held to what ChordSheet does by reading the same state.
// The Mac's is SheetHints.swift.

using Pro7ChordEditor.Song;

namespace Pro7ChordEditor.AppViews;

public static class SheetHints
{
    /// <summary>What is going on in the sheet, as far as what is worth saying goes.</summary>
    public enum Doing
    {
        /// <summary>Nothing in particular: the person is not at the sheet, and nothing is being typed or carried.</summary>
        Nothing,

        /// <summary>A chord is being typed.</summary>
        Typing,

        /// <summary>A chord is being carried.</summary>
        Carrying,

        /// <summary>The spot is on a chord.</summary>
        OnChord,

        /// <summary>The spot is on a place that has none.</summary>
        OnPlace,
    }

    /// <summary>What is going on, and what of it changes what is said.</summary>
    /// <param name="What">Which of the five.</param>
    /// <param name="OnChord">While a chord is typed: whether there is a chord at that place already.</param>
    /// <param name="Empty">While a chord is typed: whether the box has nothing in it.</param>
    /// <param name="FromStrip">While a chord is carried: whether it is one of the key's, out of the strip, and not one of the sheet's own.</param>
    public readonly record struct Situation(Doing What, bool OnChord = false, bool Empty = false, bool FromStrip = false)
    {
        public static Situation Nothing => new(Doing.Nothing);

        public static Situation OnAChord => new(Doing.OnChord);

        public static Situation OnAPlace => new(Doing.OnPlace);

        public static Situation Typing(bool onChord, bool empty) => new(Doing.Typing, OnChord: onChord, Empty: empty);

        public static Situation Carrying(bool fromStrip) => new(Doing.Carrying, FromStrip: fromStrip);
    }

    /// <summary>One hint: the keys it is about, drawn as keys, and what they do.</summary>
    /// <param name="Lead">Words that stand before the keys, where the hint is a sentence: "Press".</param>
    /// <param name="Keys">The keys, each as it is named: "Delete", "Ctrl+C".</param>
    /// <param name="Says">What they do.</param>
    /// <param name="Needed">
    /// Whether it is to be said whatever the room: where the line has not room for all
    /// its hints the last are left off, but not one of these, for which the line is
    /// drawn smaller first.
    /// </param>
    public sealed record Hint(string Lead, IReadOnlyList<string> Keys, string Says, bool Needed = false)
    {
        /// <summary>A hint that is words alone.</summary>
        public Hint(string says)
            : this("", [], says)
        {
        }

        /// <summary>A hint that is keys and what they do.</summary>
        public Hint(IReadOnlyList<string> keys, string says)
            : this("", keys, says)
        {
        }

        /// <summary>The hint as it would be read out.</summary>
        public string Read => string.Join(" ", new[] { Lead }.Concat(Keys).Append(Says).Where(part => part.Length > 0));

        public bool Equals(Hint? other) =>
            other is not null && Needed == other.Needed
            && string.Equals(Lead, other.Lead, StringComparison.Ordinal)
            && string.Equals(Says, other.Says, StringComparison.Ordinal)
            && Keys.SequenceEqual(other.Keys, StringComparer.Ordinal);

        public override int GetHashCode() => HashCode.Combine(Lead, Says, Needed, Keys.Count);
    }

    /// <summary>What a paste would do just now, which the hint for it says before the key is pressed.</summary>
    /// <param name="Lines">How many lines have been copied, if any have.</param>
    /// <param name="Replaces">
    /// Whether it would take chords off to put the copied ones on: whether the lines it
    /// would go onto have chords of their own. (With nothing copied yet, whether the
    /// spot's line has: that is what a paste there will do.)
    /// </param>
    public readonly record struct Paste(int? Lines = null, bool Replaces = false);

    // The keys, as Windows names them.
    public const string Delete = "Delete";
    public const string Enter = "Enter";
    public const string Tab = "Tab";
    public const string Escape = "Esc";
    public const string Ctrl = "Ctrl";
    public const string Up = "Up";
    public const string Down = "Down";

    private static readonly string Dash = ((char)0x2013).ToString();

    /// <summary>The keys 1 to 7, and the letters A to G, each written as a run with a dash.</summary>
    public static readonly string Numbers = "1 " + Dash + " 7";

    public static readonly string Letters = "A " + Dash + " G";

    /// <summary>
    /// What is going on in a sheet. <paramref name="attending"/>: the pointer is over a
    /// place of a line, or the keys have just moved the spot (rule 53c); without that
    /// the spot is only where it was left, and says nothing of what the person is about.
    /// </summary>
    public static Situation Of(ChordSheet sheet, bool attending)
    {
        if (sheet.Typing)
        {
            var at = sheet.TypingAt;
            return Situation.Typing(onChord: sheet.ChordAt(at.Row, at.At).Length > 0, empty: sheet.Entry.Text.Length == 0);
        }
        if (sheet.Dragged is { } carried)
        {
            return Situation.Carrying(fromStrip: carried.From.Row < 0);
        }
        if (!attending || !sheet.Usable(sheet.Spot.Row))
        {
            return Situation.Nothing;
        }
        return sheet.ChordAt(sheet.Spot.Row, sheet.Spot.At).Length == 0 ? Situation.OnAPlace : Situation.OnAChord;
    }

    /// <summary>
    /// The highlighted line (rule 53f): which row of the sheet is the line the person is
    /// at, if they are at one. The line a chord is being typed on, wherever the pointer
    /// has gone on to since; else the spot's line, while the person is at the sheet or
    /// carries a chord over it. It is where Ctrl+C copies from and Ctrl+V pastes to,
    /// which is why it is shown.
    /// </summary>
    public static int? Line(ChordSheet sheet, bool attending)
    {
        var doing = Of(sheet, attending).What;
        if (doing == Doing.Nothing)
        {
            return null;
        }
        var row = doing == Doing.Typing ? sheet.TypingAt.Row : sheet.Spot.Row;
        return sheet.Usable(row) ? row : null;
    }

    /// <summary>What a paste would do in a sheet just now (rule 53g).</summary>
    public static Paste PasteIn(ChordSheet sheet)
    {
        if (sheet.Copied is not { } copied)
        {
            var row = sheet.Spot.Row;
            return new Paste(null, sheet.Usable(row) && sheet.Rows[row].Chords.Count > 0);
        }
        return new Paste(copied.Count, sheet.PasteWouldReplace);
    }

    /// <summary>
    /// What to say: the first is the one most worth reading, and where there is not
    /// room for all of them the last are left off.
    /// </summary>
    /// <param name="situation">What is going on in the sheet.</param>
    /// <param name="ctrl">
    /// Whether Ctrl is down. At a line it brings up the copying of the line's chords,
    /// and nothing else does. While a chord is carried it says what letting go will do.
    /// </param>
    /// <param name="paste">What a paste would do just now.</param>
    /// <param name="overStrip">The pointer is over the key's chords in the strip.</param>
    public static IReadOnlyList<Hint> Hints(Situation situation, bool ctrl = false, Paste paste = default, bool overStrip = false)
    {
        switch (situation.What)
        {
            case Doing.Typing when situation is { OnChord: true, Empty: true }:
                return
                [
                    new Hint([Enter], "Takes this chord off"),
                    new Hint([Escape], "Leaves it as it was"),
                    new Hint("Or type the chord that is to be here"),
                ];
            case Doing.Typing:
                return
                [
                    new Hint([Enter], "Keeps it"),
                    new Hint([Tab], "Keeps it and goes on to the next word"),
                    new Hint([Up, Down], "Choose from what is offered"),
                    new Hint([Escape], "Throws it away"),
                    new Hint("A click on another word keeps it too"),
                ];
            case Doing.Carrying when situation.FromStrip:
                return [new Hint("Let go on a word to put this chord there")];
            case Doing.Carrying:
                return ctrl
                    ? [new Hint("Let go on a word to put a copy there")]
                    : [new Hint("Let go on a word to move it there"), new Hint([Ctrl], "Held as you let go, leaves a copy behind")];
            default:
                // (The pointer over the strip is not over the sheet: what is said of the
                // spot, which is only where it was left, gives way.)
                if (overStrip)
                {
                    return Strip;
                }
                if (situation.What == Doing.Nothing)
                {
                    // (Whatever is held: there is no highlighted line to speak of.)
                    return Idle;
                }
                if (ctrl)
                {
                    return Copying(paste);
                }
                if (situation.What == Doing.OnChord)
                {
                    return
                    [
                        new Hint([Delete], "Takes this chord off"),
                        new Hint([Numbers], "Change it"),
                        new Hint(Dragging),
                        new Hint("Click to type over it"),
                    ];
                }
                return
                [
                    new Hint([Numbers], "Put one of the key" + (char)0x2019 + "s chords here"),
                    new Hint([Letters], "Or a click: type one"),
                ];
        }
    }

    /// <summary>
    /// What is said of dragging a chord, while the pointer is on one: the maintainer's
    /// words (2026-10-14), with Windows' own key that copies named in them.
    /// </summary>
    public const string Dragging = "You can drag this chord to move it anywhere. (Hold Ctrl to drag a copy)";

    /// <summary>
    /// The copying of a line's chords onto another, which is done with keys alone and so
    /// is seen nowhere on the sheet. The words are the maintainer's (2026-10-13): "Press
    /// ... to copy all chords on the highlighted line. Press ... to paste previously
    /// copied chords to the highlighted line", and after it, where a line it would go
    /// onto has chords, " (Replaces existing chords)".
    ///
    /// One thing is added to them (the Mac session's). A whole verse or chorus that was
    /// copied (Ctrl+Shift+C) goes onto as many lines as it has, from the highlighted
    /// one on; so for that, and only then, the hint says "and the lines after it".
    /// </summary>
    public static IReadOnlyList<Hint> Copying(Paste paste)
    {
        var onto = (paste.Lines ?? 1) > 1 ? "to the highlighted line and the lines after it" : "to the highlighted line";
        return
        [
            new Hint("Press", [Ctrl + "+C"], "to copy all chords on the highlighted line.", Needed: true),
            new Hint("Press", [Ctrl + "+V"], "to paste previously copied chords " + onto + (paste.Replaces ? " (Replaces existing chords)" : ""), Needed: true),
        ];
    }

    /// <summary>What is said while the pointer is over the key's chords in the strip at the top of the sheet.</summary>
    public static IReadOnlyList<Hint> Strip { get; } = [new Hint("You can drag these down to the text on any slide")];

    /// <summary>What is said while the person is not at the sheet: where to point to be told.</summary>
    public static IReadOnlyList<Hint> Idle { get; } = [new Hint("Point at a word or a chord, and this line says what the keys do there.")];

    /// <summary>What is said while ProPresenter is running and nothing can be put on.</summary>
    public static IReadOnlyList<Hint> CannotChange { get; } = [new Hint("Nothing can be changed here while ProPresenter is running.")];
}
