// The window's model, the part that keeps what the line of hints under the chord sheet
// goes by (spec/03-chord-sheet.md, "The line of hints"; SheetHints.cs has the words).
//
// None of it is anything of the song's: what is going on in the sheet, as the sheet's
// own element says it; what a paste there would do; whether Ctrl is held down; and
// whether the pointer is over the key's chords in the strip.
//
// IT HAS A SIGNAL OF ITS OWN, `HintsChanged`, and does not use the model's one signal
// that something has changed. That one has every part of the window read the model
// again: the lists, the song's header, its pickers of keys. What the hints say changes
// as often as the pointer crosses a chord, and nothing but the line of hints is to be
// done again for it (spec/06, rule 6e: "Nothing in the window keeps the pointer
// waiting"). So only what has really changed is put down, and only the line that shows
// it is told.

namespace Pro7ChordEditor.AppViews;

public sealed partial class LibraryModel
{
    /// <summary>What is going on in the sheet, as its element last said it.</summary>
    public SheetHints.Situation SheetSituation { get; private set; } = SheetHints.Situation.Nothing;

    /// <summary>What a paste in the sheet would do just now.</summary>
    public SheetHints.Paste SheetPaste { get; private set; }

    /// <summary>Whether Ctrl is held down, as the sheet's element last said.</summary>
    public bool CtrlHeld { get; private set; }

    /// <summary>Whether the pointer is over the key's chords in the strip.</summary>
    public bool OverKeyChords { get; private set; }

    /// <summary>What the line of hints says has changed. (Not said by <see cref="PropertyChanged"/>: the head of this file has why.)</summary>
    public event Action? HintsChanged;

    /// <summary>How many times <see cref="HintsChanged"/> has been said: for a test to count.</summary>
    internal int HintsTold { get; private set; }

    /// <summary>What the line of hints under the sheet says just now.</summary>
    public IReadOnlyList<SheetHints.Hint> SheetHintsNow =>
        CanChange ? SheetHints.Hints(SheetSituation, CtrlHeld, SheetPaste, OverKeyChords) : SheetHints.CannotChange;

    /// <summary>
    /// The sheet's element says what is going on in it, and what a paste there would do.
    /// (Only what has changed is put down: the pointer says this at every move.)
    /// </summary>
    public void SheetIs(SheetHints.Situation situation, SheetHints.Paste paste = default)
    {
        if (situation == SheetSituation && paste == SheetPaste)
        {
            return;
        }
        SheetSituation = situation;
        SheetPaste = paste;
        TellTheHints();
    }

    /// <summary>Ctrl went down, or came up.</summary>
    public void Holds(bool ctrl)
    {
        if (ctrl == CtrlHeld)
        {
            return;
        }
        CtrlHeld = ctrl;
        TellTheHints();
    }

    /// <summary>The pointer has come over the key's chords in the strip, or has left them.</summary>
    public void PointerIsOverTheKeyChords(bool over)
    {
        if (over == OverKeyChords)
        {
            return;
        }
        OverKeyChords = over;
        TellTheHints();
    }

    private void TellTheHints()
    {
        HintsTold++;
        HintsChanged?.Invoke();
    }
}
