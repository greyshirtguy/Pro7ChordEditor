// The code beside ChordProText.xaml: what stands between the text box and the model
// (spec/04-chordpro-editor.md, rules 6 to 12, 23 and 25).
//
// Nothing here is a rule. What may be typed, and the help with brackets, are
// Song/ChordProEditor.cs's, asked by way of the model (`Asks`). This is how a WPF text
// box is kept to them.
//
// HOW A CHANGE IS HELD TO THE RULE. WPF's text box does not ask before it changes. So,
// as in the reference: whatever has just happened to the text (a key, a paste, a cut, a
// drop) is asked about as the whole text it has left, and if that is not the song's
// words the text is put back as it was, at once and before it is drawn, with the caret
// where it was (rule 7), and the system's sound for "no" is made. One road for every
// kind of change (rule 8).
//
// The help with brackets comes before that, at the two doors typing has: a character
// typed (`TextTyped`) and a key pressed (`KeyPressed`). What the help makes of it is
// then asked about like anything else.
//
// What Windows has its own way (spec/10-platforms.md):
//
//   - Undo is this box's own (rule 23), and is kept here and not by WPF: ChordProTyping.cs
//     says why. Ctrl+Z, and Ctrl+Y or Ctrl+Shift+Z, while the box has the keyboard.
//   - The help with a bracket TYPED is given whatever keys typed it. The page has the
//     help withheld while Ctrl or Alt is down; but on many keyboards "[" and "]" are
//     typed with AltGr, which Windows reports as Ctrl and Alt together, and a "[" that
//     did not bring its other would be a stray bracket and never be let in at all. So,
//     as on the Mac, what arrives as typed text is helped. Backspace and Delete are
//     helped only with none of Ctrl, Alt and the Windows key down; and Delete not with
//     Shift, which on Windows is Cut.
//   - A line break pasted is two characters on Windows, a carriage return and a line
//     feed. What is pasted or dropped has them made one before it goes in, and what
//     the box holds is always the text as the song counts it.
//   - Ctrl with the mouse's wheel makes the text larger and smaller, as it does in
//     Notepad, beside the keys the page gives.

using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ChordProEditor = Pro7ChordEditor.Song.ChordProEditor;

namespace Pro7ChordEditor.AppViews;

/// <summary>The song as ChordPro text, to be typed in.</summary>
public partial class ChordProText : UserControl
{
    /// <summary>What the text box asks of the model.</summary>
    /// <param name="Typed">May the text become this? If it may, it has been taken.</param>
    /// <param name="Zoom">
    /// The text a step larger (1) or smaller (-1), or its usual size (0): answers with
    /// how many times its usual size it then is.
    /// </param>
    public sealed record Asks(Func<string, bool> Typed, Func<int, double?>? Zoom = null);

    /// <summary>The size of the letters, where the text is its usual size.</summary>
    public const double UsualSize = 14;

    private readonly TypingHistory history = new();

    // The text as it last was when it might be, and what was selected in it then: what
    // a change that is not let through is put back to.
    private string good = "";
    private (int Start, int Length) selected;

    // The change in hand is one made from here, already asked about.
    private bool applying;
    private int? shownVersion;
    private int? shownSitting;
    private Color? chordColour;

    public ChordProText()
    {
        InitializeComponent();
        Under.Follow(Box);
        Box.FontSize = UsualSize;
        Box.TextChanged += (_, _) =>
        {
            if (!applying)
            {
                TheTextChanged();
            }
        };
        Box.SelectionChanged += (_, _) =>
        {
            if (!applying)
            {
                selected = (Box.SelectionStart, Box.SelectionLength);
            }
        };
        Box.PreviewTextInput += (_, e) =>
        {
            if (TextTyped(e.Text))
            {
                e.Handled = true;
            }
        };
        Box.PreviewKeyDown += (_, e) =>
        {
            if (KeyPressed(e.Key == Key.System ? e.SystemKey : e.Key, Keyboard.Modifiers))
            {
                e.Handled = true;
            }
        };
        Box.PreviewMouseWheel += (_, e) =>
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                Zoom(e.Delta > 0 ? 1 : -1);
                e.Handled = true;
            }
        };
        DataObject.AddPastingHandler(Box, WhatIsPasted);

        // Undo and Redo, from their keys and from a menu, arrive at whatever has the
        // keyboard as these two commands. This box answers them itself, before WPF's
        // own answer for a text box, which is switched off.
        Box.CommandBindings.Add(new CommandBinding(
            ApplicationCommands.Undo,
            (_, e) =>
            {
                Undo();
                e.Handled = true;
            },
            (_, e) =>
            {
                e.CanExecute = CanUndo;
                e.Handled = true;
            }));
        Box.CommandBindings.Add(new CommandBinding(
            ApplicationCommands.Redo,
            (_, e) =>
            {
                Redo();
                e.Handled = true;
            },
            (_, e) =>
            {
                e.CanExecute = CanRedo;
                e.Handled = true;
            }));
        Box.InputBindings.Add(new KeyBinding(ApplicationCommands.Redo, Key.Z, ModifierKeys.Control | ModifierKeys.Shift));
    }

    /// <summary>What is asked of the model. Until it is given, nothing can be typed.</summary>
    public Asks? Asked { get; set; }

    /// <summary>What is done when a key is not taken: the system's sound for "no".</summary>
    public Action SaysNo { get; set; } = () => SystemSounds.Beep.Play();

    /// <summary>The text box itself: for the window to give the keyboard to, and for a test to type into.</summary>
    public TextBox TextBox => Box;

    /// <summary>The letters drawn behind the box: for a test to hold against the box's own.</summary>
    internal ChordProUnderlay Underlay => Under;

    /// <summary>How many times its usual size the text is.</summary>
    public double Scale { get; private set; } = 1;

    public bool CanUndo => !Box.IsReadOnly && history.CanUndo;

    public bool CanRedo => !Box.IsReadOnly && history.CanRedo;

    /// <summary>
    /// Shows what the model has. The text is put in place of what is there only when it
    /// was set from the song (a new <c>Version</c>), never because of what was typed
    /// here.
    /// </summary>
    public void Show(LibraryModel.ChordProShown shown, bool canType)
    {
        if (shown.Version != shownVersion)
        {
            shownVersion = shown.Version;
            // A song just opened starts at its top; the same song's text made again
            // keeps the caret where it was, and the scrolling.
            var again = shown.Sitting == shownSitting;
            shownSitting = shown.Sitting;
            var caret = again ? Math.Min(Box.SelectionStart, shown.Text.Length) : 0;
            applying = true;
            try
            {
                Box.Text = shown.Text;
                Box.Select(caret, 0);
            }
            finally
            {
                applying = false;
            }
            good = shown.Text;
            selected = (Box.SelectionStart, Box.SelectionLength);
            Under.Show(good);
            // (Rules 19 and 19a: the box's undo is forgotten whenever the text is made
            // again from the song.)
            history.Forget();
            if (!again)
            {
                Box.ScrollToHome();
                Box.ScrollToHorizontalOffset(0);
            }
        }
        if (Box.IsReadOnly == canType)
        {
            Box.IsReadOnly = !canType;
        }
    }

    /// <summary>
    /// Shows the text so many times its usual size. Only its size changes: not a
    /// character of it, nor where the caret is, nor what can be undone.
    /// </summary>
    public void SetScale(double wanted)
    {
        if (wanted == Scale)
        {
            return;
        }
        Scale = wanted;
        Box.FontSize = UsualSize * wanted;
        Under.Remake();
    }

    /// <summary>
    /// Draws chords in a colour chosen in the settings; or, given none, in the usual one,
    /// which is the look's (spec/06, "Settings").
    /// </summary>
    public void SetChordColour(Color? chosen)
    {
        if (chosen == chordColour)
        {
            return;
        }
        chordColour = chosen;
        if (chosen is { } colour)
        {
            var brush = new SolidColorBrush(colour);
            brush.Freeze();
            Under.ChordBrush = brush;
        }
        else
        {
            Under.SetResourceReference(ChordProUnderlay.ChordBrushProperty, Look.Chord);
        }
    }

    /// <summary>Takes back the last step of what was typed. Answers whether it did.</summary>
    public bool Undo() => Step(history.Before, history.SteppedBack);

    /// <summary>Makes again the step last taken back. Answers whether it did.</summary>
    public bool Redo() => Step(history.After, history.SteppedForward);

    // Whatever undo and redo leave is held to the rule, and saved, like anything typed
    // (rule 23).
    private bool Step(TypingHistory.State? to, Action stepped)
    {
        if (Box.IsReadOnly || to is not { } state)
        {
            return false;
        }
        if (Asked is not { } asks || !asks.Typed(state.Text))
        {
            SaysNo();
            return false;
        }
        stepped();
        Put(state.Text, state.Start, state.Length);
        return true;
    }

    // ---- The two doors typing has

    /// <summary>
    /// A character was typed, and is not in the text yet. Answers whether it was dealt
    /// with here, by the help with brackets (rules 9 and 10); if not, the box puts it
    /// in, and is then asked about what it has.
    /// </summary>
    internal bool TextTyped(string typed)
    {
        if (Box.IsReadOnly || ChordProEditor.HelpWithTyping(typed, good, Box.SelectionStart, Box.SelectionLength) is not { } edit)
        {
            return false;
        }
        Make(edit, typed: true);
        return true;
    }

    /// <summary>
    /// A key was pressed, with these others held. Answers whether it was dealt with
    /// here: the keys that change the size of the text (rule 25), and Backspace and
    /// Delete at a chord's bracket (rule 11).
    /// </summary>
    internal bool KeyPressed(Key key, ModifierKeys modifiers)
    {
        var shift = modifiers.HasFlag(ModifierKeys.Shift);
        // Ctrl with plus (or equals, which is plus without Shift), minus and 0.
        if ((modifiers & ~ModifierKeys.Shift) == ModifierKeys.Control)
        {
            switch (key)
            {
                case Key.OemPlus or Key.Add:
                    Zoom(1);
                    return true;
                case Key.OemMinus or Key.Subtract when !shift:
                    Zoom(-1);
                    return true;
                case Key.D0 or Key.NumPad0 when !shift:
                    Zoom(0);
                    return true;
                default:
                    return false;
            }
        }
        // (Rule 12.)
        if ((modifiers & (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows)) != ModifierKeys.None || Box.IsReadOnly)
        {
            return false;
        }
        var edit = key switch
        {
            Key.Back => ChordProEditor.HelpWithBackspace(good, Box.SelectionStart, Box.SelectionLength),
            Key.Delete when !shift => ChordProEditor.HelpWithDelete(good, Box.SelectionStart, Box.SelectionLength),
            _ => null,
        };
        if (edit is null)
        {
            return false;
        }
        Make(edit.Value, typed: false);
        return true;
    }

    // Makes a change from here, as if it had been typed: asked about, and a step of
    // the box's undo.
    private void Make(ChordProEditor.Edit edit, bool typed)
    {
        if (edit.End == edit.Start && edit.With.Length == 0)
        {
            // Only the caret moves: a closing bracket stepped over.
            Box.Select(edit.Caret, 0);
            return;
        }
        var proposed = string.Concat(good.AsSpan(0, edit.Start), edit.With, good.AsSpan(edit.End));
        if (Asked is not { } asks || !asks.Typed(proposed))
        {
            SaysNo();
            return;
        }
        var before = new TypingHistory.State(good, selected.Start, selected.Length);
        Put(proposed, edit.Caret, 0);
        var after = new TypingHistory.State(proposed, edit.Caret);
        if (typed)
        {
            history.NoteTyped(before, after);
        }
        else
        {
            history.Note(before, after);
        }
    }

    private void Zoom(int direction)
    {
        if (Asked?.Zoom?.Invoke(direction) is { } now)
        {
            SetScale(now);
        }
    }

    // ---- Every other road

    // Something has changed the box's text that was not asked about: the box put a
    // character in, or took one out, or something was pasted, cut or dropped. It is
    // asked about now, as the whole text it has left.
    private void TheTextChanged()
    {
        var raw = Box.Text;
        // (Line breaks as the song counts them; unless the song's own words have a
        // carriage return in them, which is then not this box's to make away with.)
        var now = good.Contains('\r', StringComparison.Ordinal) ? raw : ChordProTyping.WithLineBreaksOfOneUnit(raw);
        if (string.Equals(now, good, StringComparison.Ordinal))
        {
            if (!string.Equals(raw, now, StringComparison.Ordinal))
            {
                Put(good, selected.Start, selected.Length, raw);
            }
            return;
        }
        var before = new TypingHistory.State(good, selected.Start, selected.Length);
        if (!Box.IsReadOnly && Asked is { } asks && asks.Typed(now))
        {
            var start = Box.SelectionStart;
            var end = start + Box.SelectionLength;
            if (!string.Equals(raw, now, StringComparison.Ordinal))
            {
                start = ChordProTyping.PlaceWithLineBreaksOfOneUnit(raw, start);
                end = ChordProTyping.PlaceWithLineBreaksOfOneUnit(raw, end);
            }
            Put(now, start, end - start, raw);
            history.Note(before, new TypingHistory.State(now, start, end - start));
        }
        else
        {
            // Not the song's words. Put back as it was, the caret where the eye expects
            // it: where it was (rule 7).
            Put(good, before.Start, before.Length, raw);
            SaysNo();
        }
    }

    // What is pasted or dropped is plain text, its line breaks one unit each.
    private static void WhatIsPasted(object sender, DataObjectPastingEventArgs e)
    {
        if (e.SourceDataObject.GetData(DataFormats.UnicodeText, autoConvert: true) is not string pasted)
        {
            return;
        }
        var plain = ChordProTyping.WithLineBreaksOfOneUnit(pasted);
        if (!string.Equals(plain, pasted, StringComparison.Ordinal))
        {
            var made = new DataObject();
            made.SetData(DataFormats.UnicodeText, plain);
            e.DataObject = made;
        }
    }

    // Makes the box's text this, with this selected in it. By the smallest change that
    // does it, so that the box keeps its scrolling.
    private void Put(string text, int start, int length, string? raw = null)
    {
        applying = true;
        try
        {
            raw ??= Box.Text;
            if (!string.Equals(raw, text, StringComparison.Ordinal))
            {
                if (TextChange.Between(raw, text) is { } change)
                {
                    Box.Select(change.Start, change.Removed);
                    Box.SelectedText = change.Inserted;
                }
                // (A text box will not put its caret in the middle of what it takes for
                // one character, a letter and its accent, the two units of a line
                // break. A change said there is made somewhere else, and is seen here
                // not to have done it: then the whole text is set, which is sure.)
                if (!string.Equals(Box.Text, text, StringComparison.Ordinal))
                {
                    Box.Text = text;
                }
            }
            start = Math.Clamp(start, 0, text.Length);
            length = Math.Clamp(length, 0, text.Length - start);
            if (Box.SelectionStart != start || Box.SelectionLength != length)
            {
                Box.Select(start, length);
            }
        }
        finally
        {
            applying = false;
        }
        good = text;
        selected = (Box.SelectionStart, Box.SelectionLength);
        Under.Show(text);
    }
}
