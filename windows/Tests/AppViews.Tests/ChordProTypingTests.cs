// The ChordPro editor's text box itself, typed into a key at a time
// (spec/04-chordpro-editor.md, "What to hold your build to": "a character over a word
// (nothing happens), [ (its other comes), a name, ] (stepped over), Backspace on a
// bracket, a paste, the box's own undo; and after each, what the file has").
//
// The box is the real one, wired to a model as the song's pane wires it, on a made-up PC
// (MadeUpPC.cs). No window is opened, and nobody's keyboard is listened to: what a key
// would bring is handed to the box by the same two doors a key comes in by. A character
// typed is raised as WPF raises one, first to be looked at and then to be taken. A key
// pressed is told to the box's own answer to keys, and if that does not take it, the
// command the key stands for is given to the box. One test opens a window that is never
// shown, and sends real key presses through it, to hold that the doors are the ones the
// keyboard uses.
//
// The person's clipboard is not touched: what would be pasted is put in place of what
// is selected, which is what a paste does with it, and the one thing done to what is
// pasted on its way in (its line breaks) is asked of the box by itself.
//
// MINE, NOT A VECTOR, each of them. Where the Mac's tests of its own text view hold the
// same thing (ChordProTypingTests.swift, "Typed into"), these expect what those expect.
// At the foot, with no box at all: the one change between two texts, line breaks as the
// song counts them, and the box's undo (ChordProTyping.cs).

using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Pro7ChordEditor.Song;
using Xunit;
using TextBox = System.Windows.Controls.TextBox;

namespace Pro7ChordEditor.AppViews.Tests;

public class ChordProTypingTests
{
    private static void OnAMadeUpPC(Func<MadeUpPC, Task> what) => Pictures.OnAThreadOfItsOwn(async () =>
    {
        using var pc = new MadeUpPC();
        await what(pc);
    });

    // The box with a look on it, wired to a model as the pane wires it, and laid out.
    private static Typist At(LibraryModel model)
    {
        var editor = new ChordProText { Asked = new ChordProText.Asks(model.TextMayBecome, direction => model.ZoomText(direction)) };
        var typist = new Typist(editor, model);
        // (No sound in a test.)
        editor.SaysNo = () => typist.SaidNo++;
        // (The app's own colours, the chords' among them, and the system's dark look.)
        Look.Follow(editor);
        Look.Wear(editor, dark: true);
        editor.SetScale(model.TextZoom);
        typist.Show();
        Pictures.LayOut(editor, 600, 400);
        return typist;
    }

    // The same with no model: a text that takes whatever a test says it may.
    private static Typist Alone(string text, Func<string, bool>? mayBecome = null)
    {
        var editor = new ChordProText { Asked = new ChordProText.Asks(mayBecome ?? (_ => true)) };
        var typist = new Typist(editor, null);
        editor.SaysNo = () => typist.SaidNo++;
        // (The app's own colours, the chords' among them, and the system's dark look.)
        Look.Follow(editor);
        Look.Wear(editor, dark: true);
        editor.Show(new LibraryModel.ChordProShown(text, 1, 1, null), canType: true);
        Pictures.LayOut(editor, 600, 400);
        return typist;
    }

    private static Task<LibraryModel> Open(MadeUpPC pc, string name) => ChordProModelTests.Open(pc, name);

    private static IEnumerable<string> ChordsOf(int box, string name, MadeUpPC pc) => ChordProModelTests.ChordsOf(box, name, pc);

    // ---- Typed into

    [Fact]
    public void TheWordsCannotBeTypedOverABracketBringsItsOtherAndAChordIsTypedBetweenThemAndSaved() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        var typist = At(model);
        var before = typist.Text;
        Assert.Equal(model.ChordPro!.Text, before);
        Assert.False(typist.Box.IsReadOnly);

        // In the middle of a word of the song: nothing happens, the caret stays, and
        // the sound for "no" is made.
        var inWord = typist.PlaceOf("Thirteen") + 4;
        typist.CaretAt(inWord);
        typist.Type("x");
        Assert.Equal(before, typist.Text);
        Assert.Equal((inWord, 0), typist.Selected);
        Assert.Equal(1, typist.SaidNo);

        // A bracket brings its other with it, and the caret is between them.
        typist.Type("[");
        Assert.Equal("Thir[]teen", typist.LineWith("Thir"));
        Assert.Equal((inWord + 1, 0), typist.Selected);
        // A chord is typed between them, a character at a time.
        typist.Type("G/B");
        Assert.Equal("Thir[G/B]teen", typist.LineWith("Thir"));
        Assert.Equal((inWord + 4, 0), typist.Selected);
        // What could not become a chord is not let in, and the caret is where it was.
        typist.Type("*");
        Assert.Equal("Thir[G/B]teen", typist.LineWith("Thir"));
        Assert.Equal((inWord + 4, 0), typist.Selected);
        // "]" typed at the "]" steps over it.
        typist.Type("]");
        Assert.Equal("Thir[G/B]teen", typist.LineWith("Thir"));
        Assert.Equal((inWord + 5, 0), typist.Selected);
        // And the model has every character of it. Only the "x" and the "*" were not taken.
        Assert.Equal(typist.Text, model.ChordPro!.Text);
        Assert.Equal(2, typist.SaidNo);

        await model.WhatWasTypedIsSavedAsync();
        Assert.Equal(["0:C", "10:G/B", "27:G/B"], ChordsOf(1, "Plain Song", pc));
        // The saving did not set the text afresh from under the person typing: the
        // text, the caret and the box's undo are as they were.
        typist.Show();
        Assert.Equal("Thir[G/B]teen", typist.LineWith("Thir"));
        Assert.Equal((inWord + 5, 0), typist.Selected);
        Assert.Equal(1, model.ChordPro!.Version);
        Assert.True(typist.Editor.CanUndo);
    });

    [Fact]
    public void WhatIsTypedOverWhatIsSelectedIsHeldToTheRuleTooAndWhatWasSelectedIsSelectedAgain() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        var typist = At(model);
        var before = typist.Text;
        // A word of the song selected, and a letter typed over it.
        var word = typist.PlaceOf("Thirteen");
        typist.CaretAt(word, 8);
        typist.Type("x");
        Assert.Equal(before, typist.Text);
        Assert.Equal((word, 8), typist.Selected);
        // A bracket typed over a selection is not given its other (rule 9), and by
        // itself is a stray one.
        typist.Type("[");
        Assert.Equal(before, typist.Text);
        Assert.Equal((word, 8), typist.Selected);
        Assert.Equal(2, typist.SaidNo);
        // A chord selected, brackets and all, and a key pressed that would take it:
        // that is let through.
        var chord = typist.PlaceOf("[G/B]");
        typist.CaretAt(chord, 5);
        typist.Press(Key.Delete);
        Assert.Equal("[C]Nine ten eleven twelve", typist.LineWith("Nine"));
        Assert.Equal((chord, 0), typist.Selected);
        // A line break typed would make a line more: it is not let in, whichever way a
        // line break is written.
        var now = typist.Text;
        typist.Paste("\r\n", typist.PlaceOf("Thirteen"));
        Assert.Equal(now, typist.Text);
        typist.Paste("\n", typist.PlaceOf("Thirteen"));
        Assert.Equal(now, typist.Text);
        Assert.Equal(4, typist.SaidNo);
        Assert.Equal(typist.Text, model.ChordPro!.Text);
    });

    [Fact]
    public void BackspaceJustAfterABracketOrDeleteJustBeforeOneTakesTheWholeChordAndElsewhereTheyAreTheKeysTheyAlwaysAre() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        var typist = At(model);

        // Backspace just after the "]" of "[G/B]" in "e[G/B]leven".
        var chord = typist.PlaceOf("[G/B]");
        typist.CaretAt(chord + 5);
        typist.Press(Key.Back);
        Assert.Equal("[C]Nine ten eleven twelve", typist.LineWith("Nine"));
        Assert.Equal((chord, 0), typist.Selected);
        // Delete just before the "[" of "[C]".
        var first = typist.PlaceOf("[C]Nine");
        typist.CaretAt(first);
        typist.Press(Key.Delete);
        Assert.Equal("Nine ten eleven twelve", typist.LineWith("Nine"));
        Assert.Equal((first, 0), typist.Selected);
        // Backspace among the words would take a letter, and is not let through.
        var before = typist.Text;
        var among = typist.PlaceOf("Nine") + 2;
        typist.CaretAt(among);
        typist.Press(Key.Back);
        Assert.Equal(before, typist.Text);
        Assert.Equal((among, 0), typist.Selected);
        typist.Press(Key.Delete);
        Assert.Equal(before, typist.Text);
        Assert.Equal((among, 0), typist.Selected);
        Assert.Equal(2, typist.SaidNo);
        // Inside a chord's name it takes one character of the name.
        var em = typist.PlaceOf("[Em]seven");
        typist.CaretAt(em + 3);
        typist.Press(Key.Back);
        Assert.Equal("Five six [E]seven eight", typist.LineWith("seven"));
        // With Ctrl, Alt or the Windows key down there is no help (rule 12), nor for
        // Delete with Shift, which is Cut: the key is left to the box.
        // (Each asked where it WOULD be helped with nothing held: Backspace just after
        // the chord's "[", Delete just before it.)
        var bracket = typist.PlaceOf("[D]four");
        foreach (var held in new[] { ModifierKeys.Control, ModifierKeys.Alt, ModifierKeys.Windows, ModifierKeys.Control | ModifierKeys.Alt })
        {
            typist.CaretAt(bracket + 1);
            Assert.False(typist.Editor.KeyPressed(Key.Back, held));
            typist.CaretAt(bracket);
            Assert.False(typist.Editor.KeyPressed(Key.Delete, held));
        }
        Assert.False(typist.Editor.KeyPressed(Key.Delete, ModifierKeys.Shift));
        Assert.Contains("[D]four", typist.Text, StringComparison.Ordinal);
        typist.CaretAt(bracket + 1);
        // Shift with Backspace is Backspace.
        Assert.True(typist.Editor.KeyPressed(Key.Back, ModifierKeys.Shift));
        Assert.Equal("[G]One two three four", typist.LineWith("One"));
        // And no other key is taken.
        Assert.False(typist.Editor.KeyPressed(Key.Left, ModifierKeys.None));
        Assert.False(typist.Editor.KeyPressed(Key.A, ModifierKeys.None));

        await model.WhatWasTypedIsSavedAsync();
        Assert.Empty(ChordsOf(1, "Plain Song", pc));
        Assert.Equal(["0:G", "28:E"], ChordsOf(0, "Plain Song", pc));
        Assert.Equal(typist.Text, model.ChordPro!.Text);
    });

    [Fact]
    public void TheBoxsOwnUndoTakesBackWhatWasTypedAndWhatItLeavesIsSavedLikeAnythingTyped() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        var typist = At(model);
        var before = typist.Text;
        Assert.False(typist.Editor.CanUndo || typist.Editor.CanRedo);
        Assert.False(ApplicationCommands.Undo.CanExecute(null, typist.Box));

        var here = typist.PlaceOf("Thirteen");
        typist.CaretAt(here);
        typist.Type("[A");
        Assert.Equal("[A]Thirteen", typist.LineWith("Thirteen"));
        await model.WhatWasTypedIsSavedAsync();
        Assert.Equal(["0:C", "10:G/B", "23:A"], ChordsOf(1, "Plain Song", pc));

        // A run of typing is one step: the chord goes whole, and the caret is where it
        // was before it was typed.
        Assert.True(typist.Editor.CanUndo);
        Assert.True(typist.Editor.Undo());
        Assert.Equal(before, typist.Text);
        Assert.Equal(before, model.ChordPro!.Text);
        Assert.Equal((here, 0), typist.Selected);
        Assert.False(typist.Editor.CanUndo);
        Assert.False(typist.Editor.Undo());
        await model.WhatWasTypedIsSavedAsync();
        Assert.Equal(["0:C", "10:G/B"], ChordsOf(1, "Plain Song", pc));
        // And made again, it is saved again.
        Assert.True(typist.Editor.Redo());
        Assert.Equal("[A]Thirteen", typist.LineWith("Thirteen"));
        Assert.Equal(typist.Text, model.ChordPro!.Text);
        Assert.Equal((here + 2, 0), typist.Selected);
        Assert.False(typist.Editor.CanRedo);
        await model.WhatWasTypedIsSavedAsync();
        Assert.Equal(["0:C", "10:G/B", "23:A"], ChordsOf(1, "Plain Song", pc));

        // Ctrl+Z and Ctrl+Y, and Undo and Redo in the Edit menu, reach whatever has the
        // keyboard as these two commands, and the box answers them with this same undo
        // of its own. (WPF's own for a text box is switched off, and says it has none.)
        Assert.False(typist.Box.IsUndoEnabled);
        Assert.True(ApplicationCommands.Undo.CanExecute(null, typist.Box));
        Assert.False(ApplicationCommands.Redo.CanExecute(null, typist.Box));
        ApplicationCommands.Undo.Execute(null, typist.Box);
        Assert.Equal(before, typist.Text);
        Assert.Equal(before, model.ChordPro!.Text);
        Assert.True(ApplicationCommands.Redo.CanExecute(null, typist.Box));
        ApplicationCommands.Redo.Execute(null, typist.Box);
        Assert.Equal("[A]Thirteen", typist.LineWith("Thirteen"));
        // Ctrl+Shift+Z is Redo too (spec/10).
        Assert.Contains(
            typist.Box.InputBindings.OfType<KeyBinding>(),
            binding => binding.Command == ApplicationCommands.Redo && binding.Key == Key.Z && binding.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift));

        // The caret moved and something typed elsewhere is a step of its own; and
        // something typed after an undo forgets what was undone.
        typist.CaretAt(typist.PlaceOf("Sing it loud"));
        typist.Type("[E");
        Assert.True(typist.Editor.Undo());
        Assert.Equal("[D]Sing it loud", typist.LineWith("loud"));
        Assert.Equal("[A]Thirteen", typist.LineWith("Thirteen"));
        Assert.True(typist.Editor.CanRedo);
        typist.CaretAt(typist.PlaceOf("Five six"));
        typist.Type("[");
        Assert.False(typist.Editor.CanRedo);
        // What was not let in is no step: there is nothing of it to take back.
        typist.CaretAt(typist.PlaceOf("seven") + 2);
        typist.Type("x");
        Assert.True(typist.Editor.Undo());
        Assert.Equal("Five six [Em]seven eight", typist.LineWith("seven"));
        Assert.True(typist.Editor.Undo());
        Assert.Equal(before, typist.Text);
        Assert.False(typist.Editor.CanUndo);
        await model.WhatWasTypedIsSavedAsync();
        Assert.Equal(["0:C", "10:G/B"], ChordsOf(1, "Plain Song", pc));
        Assert.Equal(["0:G", "14:D", "28:Em"], ChordsOf(0, "Plain Song", pc));
    });

    [Fact]
    public void WhatIsPastedIsHeldToTheSameRuleAndAPastedBracketIsNotGivenItsOther() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        var typist = At(model);
        // (Something is typed first, so that a paste is seen to follow typing and still
        // not be taken for it: a bracket typed in a group's name, which is not let in.)
        typist.CaretAt(3);
        typist.Type("[");
        Assert.StartsWith("{c: Verse 1}\n", typist.Text, StringComparison.Ordinal);
        Assert.Equal(1, typist.SaidNo);
        var before = typist.Text;
        var here = typist.PlaceOf("Thirteen");
        // Words pasted among the words, and a bracket by itself, are not let in.
        typist.Paste("more words ", here);
        Assert.Equal(before, typist.Text);
        typist.Paste("[", here);
        Assert.Equal(before, typist.Text);
        Assert.Equal(3, typist.SaidNo);
        Assert.Equal((here, 0), typist.Selected);
        // A chord is, brackets and all; and two with the song's own words between.
        typist.Paste("[Am7]", here);
        Assert.Equal("[Am7]Thirteen", typist.LineWith("Thirteen"));
        typist.Paste("Sing [E]it [A]loud", typist.PlaceOf("Sing it loud"), over: 12);
        Assert.Equal("[D]Sing [E]it [A]loud", typist.LineWith("loud"));
        Assert.Equal(typist.Text, model.ChordPro!.Text);
        await model.WhatWasTypedIsSavedAsync();
        Assert.Equal(["0:C", "10:G/B", "23:Am7"], ChordsOf(1, "Plain Song", pc));
        Assert.Equal(["0:C", "8:G", "12:D", "17:E", "20:A"], ChordsOf(2, "Plain Song", pc));
        // A paste is a step of the box's undo by itself.
        Assert.True(typist.Editor.Undo());
        Assert.Equal("[D]Sing it loud", typist.LineWith("loud"));
        Assert.Equal("[Am7]Thirteen", typist.LineWith("Thirteen"));
    });

    [Fact]
    public void TheWholeTextPastedBackWithLineBreaksAsWindowsWritesThemIsTakenAndTheBoxHoldsThemAsTheSongCountsThem() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        var typist = At(model);
        var before = typist.Text;
        Assert.DoesNotContain("\r", before, StringComparison.Ordinal);
        // The text as it might come back from Notepad: a chord more, and every line
        // break two characters.
        var chorded = before.Replace("Thirteen", "Thir[A]teen", StringComparison.Ordinal);
        typist.Paste(chorded.Replace("\n", "\r\n", StringComparison.Ordinal), 0, over: before.Length);
        Assert.Equal(chorded, typist.Text);
        Assert.Equal(chorded, model.ChordPro!.Text);
        Assert.Equal(0, typist.SaidNo);
        // What was pasted is selected, as it would be: the whole of it, counted as the
        // song counts.
        Assert.Equal((0, chorded.Length), typist.Selected);
        await model.WhatWasTypedIsSavedAsync();
        Assert.Equal(["0:C", "10:G/B", "27:A"], ChordsOf(1, "Plain Song", pc));
        // One step back, and it is as it was.
        Assert.True(typist.Editor.Undo());
        Assert.Equal(before, typist.Text);
        // Two lines pasted over themselves, a chord more in the second and the line
        // break between them two characters: what is selected after is what was
        // pasted, counted as the song counts it.
        const string two = "[G]One two three [D]four\nFive six [Em]seven eight";
        const string pasted = "[G]One two three [D]four\r\nFive [A]six [Em]seven eight";
        var at = typist.PlaceOf(two);
        typist.Paste(pasted, at, over: two.Length);
        Assert.Equal("Five [A]six [Em]seven eight", typist.LineWith("seven"));
        Assert.DoesNotContain("\r", typist.Text, StringComparison.Ordinal);
        Assert.Equal((at, pasted.Length - 1), typist.Selected);
        Assert.Equal(typist.Text, model.ChordPro!.Text);

        // On its way in, what is pasted or dropped has its line breaks made one unit
        // each: asked of the box as WPF asks it.
        var carried = new DataObject();
        carried.SetData(DataFormats.UnicodeText, "one\r\ntwo\rthree\nfour");
        var pasting = new DataObjectPastingEventArgs(carried, isDragDrop: false, DataFormats.UnicodeText);
        typist.Box.RaiseEvent(pasting);
        Assert.Equal("one\ntwo\nthree\nfour", pasting.DataObject.GetData(DataFormats.UnicodeText));
        Assert.False(pasting.CommandCancelled);
        // What has none is let by as it came.
        var plain = new DataObject();
        plain.SetData(DataFormats.UnicodeText, "[Am7]");
        var untouched = new DataObjectPastingEventArgs(plain, isDragDrop: true, DataFormats.UnicodeText);
        typist.Box.RaiseEvent(untouched);
        Assert.Same(plain, untouched.DataObject);
    });

    [Fact]
    public void WhenTheTextIsSetFromTheSongItTakesThePlaceOfWhatWasTypedAndTheUndoOfThatIsForgotten() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        var typist = At(model);
        var thirteen = typist.PlaceOf("Thirteen");
        typist.CaretAt(thirteen);
        typist.Type("[A");
        Assert.True(typist.Editor.CanUndo);
        // ProPresenter is found to be running when the save comes: refused, and the
        // text is the song's again.
        pc.ProPresenterIsRunning = true;
        await model.WhatWasTypedIsSavedAsync();
        typist.Show();
        Assert.Equal("Thirteen", typist.LineWith("Thirteen"));
        Assert.False(typist.Editor.CanUndo);
        Assert.False(ApplicationCommands.Undo.CanExecute(null, typist.Box));
        Assert.True(typist.Box.IsReadOnly);
        // Nothing can be typed while it runs, by either door or by a paste; and no
        // sound is made, as none is by any box that is only to be read.
        typist.Type("[");
        typist.Press(Key.Back);
        Assert.False(typist.Editor.KeyPressed(Key.Back, ModifierKeys.None));
        Assert.Equal("Thirteen", typist.LineWith("Thirteen"));
        Assert.Equal(0, typist.SaidNo);
        // The same song's text made again leaves the caret at the place it was (which
        // was after the "[A" that is gone, two along); another song opened starts at
        // its top.
        Assert.Equal((thirteen + 2, 0), typist.Selected);
        // (Nor is there help at a chord's bracket, where with the box to be typed in
        // Backspace and Delete would each take the chord.)
        var chord = typist.PlaceOf("[G/B]");
        typist.CaretAt(chord + 5);
        Assert.False(typist.Editor.KeyPressed(Key.Back, ModifierKeys.None));
        typist.CaretAt(chord);
        Assert.False(typist.Editor.KeyPressed(Key.Delete, ModifierKeys.None));
        Assert.Contains("[G/B]", typist.Text, StringComparison.Ordinal);
        Assert.Equal(0, typist.SaidNo);
        pc.ProPresenterIsRunning = false;
        await model.RefreshAsync();
        model.Open(pc.Song("Amazing Grace"));
        typist.Show();
        Assert.StartsWith("{c: Verse 1}\nA[G]mazing", typist.Text, StringComparison.Ordinal);
        Assert.Equal((0, 0), typist.Selected);
        Assert.False(typist.Box.IsReadOnly);
        Assert.False(typist.Editor.CanUndo);
    });

    [Fact]
    public void ASongWhoseWordsHaveBracketsOfTheirOwnIsShownAndTakesNothing() => OnAMadeUpPC(async pc =>
    {
        var bytes = pc.BytesOf("Plain Song");
        var word = bytes.AsSpan().IndexOf("Thirteen"u8);
        "Thir[een"u8.CopyTo(bytes.AsSpan(word));
        File.WriteAllBytes(pc.Song("Plain Song"), bytes);
        var model = await Open(pc, "Plain Song");
        var typist = At(model);
        Assert.True(typist.Box.IsReadOnly);
        Assert.Contains("Thir[een", typist.Text, StringComparison.Ordinal);
        var before = typist.Text;
        typist.CaretAt(typist.PlaceOf("Five six"));
        typist.Type("[");
        typist.Paste("[A]", typist.PlaceOf("Five six"));
        // (A text box that is only to be read still takes what a program puts in it: it
        // is put back, as anything is that the model does not take.)
        Assert.Equal(before, typist.Text);
        Assert.Equal(before, model.ChordPro!.Text);
    });

    [Fact]
    public void TheTextCanBeMadeLargerAndSmallerWithCtrlAndThePlusMinusAndNoughtKeysAndOnlyItsSizeChanges() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        var typist = At(model);
        var file = pc.BytesOf("Plain Song");
        var before = typist.Text;
        var word = typist.PlaceOf("One");
        typist.CaretAt(word + 2);
        Assert.Equal(1.0, model.TextZoom);
        Assert.Equal(14.0, typist.Box.FontSize);

        Assert.True(typist.Editor.KeyPressed(Key.OemMinus, ModifierKeys.Control));
        Assert.Equal(0.9, model.TextZoom);
        Assert.Equal(0.9, typist.Editor.Scale);
        Assert.Equal(12.6, typist.Box.FontSize, 6);
        // Not a character of the text, nor where the caret is, nor what can be undone,
        // nor the song.
        Assert.Equal(before, typist.Text);
        Assert.Equal((word + 2, 0), typist.Selected);
        Assert.False(typist.Editor.CanUndo);
        Assert.Equal(before, model.ChordPro!.Text);
        Assert.Equal(1, model.ChordPro.Version);

        // Plus is on the equals key, with Shift and without.
        Assert.True(typist.Editor.KeyPressed(Key.OemPlus, ModifierKeys.Control));
        Assert.True(typist.Editor.KeyPressed(Key.OemPlus, ModifierKeys.Control | ModifierKeys.Shift));
        Assert.Equal(1.1, model.TextZoom);
        Assert.Equal(15.4, typist.Box.FontSize, 6);
        Assert.True(typist.Editor.KeyPressed(Key.D0, ModifierKeys.Control));
        Assert.Equal(1.0, model.TextZoom);
        Assert.Equal(14.0, typist.Box.FontSize);
        // The keys at the keyboard's right do the same.
        Assert.True(typist.Editor.KeyPressed(Key.Add, ModifierKeys.Control));
        Assert.True(typist.Editor.KeyPressed(Key.Subtract, ModifierKeys.Control));
        Assert.True(typist.Editor.KeyPressed(Key.Subtract, ModifierKeys.Control));
        Assert.Equal(0.9, model.TextZoom);
        Assert.True(typist.Editor.KeyPressed(Key.NumPad0, ModifierKeys.Control));
        Assert.Equal(1.0, model.TextZoom);
        // A key that is not one of these is not taken: Ctrl with 1 is to be the
        // header's, Ctrl with Shift and minus is another key, and without Ctrl they
        // are only characters.
        Assert.False(typist.Editor.KeyPressed(Key.D1, ModifierKeys.Control));
        Assert.False(typist.Editor.KeyPressed(Key.OemMinus, ModifierKeys.Control | ModifierKeys.Shift));
        Assert.False(typist.Editor.KeyPressed(Key.D0, ModifierKeys.Control | ModifierKeys.Shift));
        Assert.False(typist.Editor.KeyPressed(Key.OemPlus, ModifierKeys.None));
        Assert.False(typist.Editor.KeyPressed(Key.OemMinus, ModifierKeys.Control | ModifierKeys.Alt));
        Assert.Equal(1.0, model.TextZoom);

        // While nothing can be typed, the size can still be changed.
        pc.ProPresenterIsRunning = true;
        model.ProPresenterMayHaveChanged();
        typist.Show();
        Assert.True(typist.Box.IsReadOnly);
        Assert.True(typist.Editor.KeyPressed(Key.OemPlus, ModifierKeys.Control));
        Assert.Equal(1.1, model.TextZoom);
        Assert.Equal(file, pc.BytesOf("Plain Song"));
    });

    // ---- The letters, drawn behind the box

    [Fact]
    public void WhichStretchesOfALineAreDrawnAsChordsAndWhichLinesAreGreyed()
    {
        var group = ChordProUnderlay.ColoursOf("{c: Verse 1}");
        Assert.True(group.Greyed);
        Assert.Empty(group.Chords);
        // (A chord in a line that begins with a curly bracket is of the greyed line.)
        Assert.True(ChordProUnderlay.ColoursOf("{c: Not a [G]chord}").Greyed);
        var words = ChordProUnderlay.ColoursOf("[G]One two three [D/F#]four");
        Assert.False(words.Greyed);
        Assert.Equal([new ChordProEditor.Stretch(0, 3), new ChordProEditor.Stretch(17, 23)], words.Chords);
        // Brackets still empty are drawn as a chord; one with no other is not.
        Assert.Equal([new ChordProEditor.Stretch(5, 7)], ChordProUnderlay.ColoursOf("Five []six [G").Chords);
        var empty = ChordProUnderlay.ColoursOf("");
        Assert.False(empty.Greyed);
        Assert.Empty(empty.Chords);
    }

    // How many of a picture's dots are the chords' orange.
    private static int Orange(int[] dots) => dots.Count(dot =>
    {
        var (red, green, blue) = ((dot >> 16) & 0xFF, (dot >> 8) & 0xFF, dot & 0xFF);
        return red > 225 && green is > 110 and < 185 && blue < 70;
    });

    [Fact]
    public void ChordsAreDrawnInTheirColourAsTheyAreTypedAndTheTextIsNeverChangedByIt() => Pictures.OnAThreadOfItsOwn(() =>
    {
        var typist = Alone("Words with no chord\n{c: Not a [G]chord}\nmore words");
        var ground = new Border { Background = Brushes.Black, Child = typist.Editor };
        var before = typist.Text;
        // No chord, and no orange: the "[G]" in the greyed line is of the greyed line.
        Assert.Equal(0, Orange(Pictures.Drawn(ground, 600, 400).Dots));
        typist.CaretAt(typist.PlaceOf("with"));
        typist.Type("[D");
        Assert.Equal("Words [D]with no chord", typist.LineWith("Words"));
        var chorded = Orange(Pictures.Drawn(ground, 600, 400).Dots);
        Assert.True(chorded > 40, "only " + chorded + " dots of a typed chord are orange");
        // A second chord, about as much orange again.
        typist.CaretAt(typist.PlaceOf("more"));
        typist.Type("[D");
        var twice = Orange(Pictures.Drawn(ground, 600, 400).Dots);
        Assert.InRange(twice, chorded * 3 / 2, chorded * 5 / 2);
        // Taken out again, none.
        Assert.True(typist.Editor.Undo());
        Assert.True(typist.Editor.Undo());
        Assert.Equal(before, typist.Text);
        Assert.Equal(0, Orange(Pictures.Drawn(ground, 600, 400).Dots));
    });

    [Fact]
    public void ChordsAreDrawnBoldAndAGroupsNameInItsOwnColour() => Pictures.OnAThreadOfItsOwn(() =>
    {
        static int Blue(int[] dots) => dots.Count(dot => (dot & 0xFF) > 200 && ((dot >> 16) & 0xFF) < 60 && ((dot >> 8) & 0xFF) < 60);

        // A group's name, in a colour of the test's own to tell it by.
        var grouped = Alone("{c: Verse 1}\nwords");
        grouped.Editor.Underlay.QuietBrush = Brushes.Blue;
        var named = Pictures.Drawn(new Border { Background = Brushes.Black, Child = grouped.Editor }, 600, 400).Dots;
        Assert.True(Blue(named) > 60, "only " + Blue(named) + " dots of a group's name are in its colour");
        Assert.Equal(0, Orange(named));

        // Chords, and the same letters as the box itself would draw them in the same
        // colour: bold has a good deal more of its colour in it.
        var chords = Alone("[G/B] [D/F#] [Am7] [Em]");
        var ground = new Border { Background = Brushes.Black, Child = chords.Editor };
        var bold = Orange(Pictures.Drawn(ground, 600, 400).Dots);
        chords.Box.Foreground = chords.Editor.Underlay.ChordBrush;
        chords.Editor.Underlay.Visibility = Visibility.Hidden;
        var plain = Orange(Pictures.Drawn(ground, 600, 400).Dots);
        Assert.True(plain > 40, "the box drew only " + plain + " dots of its own letters in the chords' colour");
        Assert.True(bold > plain * 5 / 4, bold + " dots of the chords against " + plain + " of the same letters drawn plain: are they bold?");
    });

    [Fact]
    public void TheLettersDrawnBehindTheBoxAreDotForDotWhereTheBoxHasItsOwn() => Pictures.OnAThreadOfItsOwn(() =>
    {
        // Lines of words only, long and short, one of them past the box's right edge,
        // and enough of them to run past its foot.
        var lines = Enumerable.Range(1, 40).Select(n => n % 7 == 0 ? "" : "Line " + n + " of plain words, " + new string('w', n * 3) + " to the end").ToList();
        var typist = Alone(string.Join('\n', lines));
        var ground = new Border { Background = Brushes.Black, Child = typist.Editor };
        var box = typist.Box;
        var under = typist.Editor.Underlay;

        (int Width, int Height, int[] Dots) OfTheBoxAlone()
        {
            box.Foreground = Brushes.White;
            under.Visibility = Visibility.Hidden;
            var drawn = Pictures.Drawn(ground, 600, 400);
            box.Foreground = Brushes.Transparent;
            under.Visibility = Visibility.Visible;
            return drawn;
        }

        void TheSame(string when)
        {
            under.WordsBrush = Brushes.White;
            var behind = Pictures.Drawn(ground, 600, 400);
            var own = OfTheBoxAlone();
            var lit = own.Dots.Count(dot => (dot & 0xFFFFFF) != 0);
            Assert.True(lit > 2000, when + ": the box drew only " + lit + " dots of its own letters");
            var apart = 0;
            for (var dot = 0; dot < own.Dots.Length; dot++)
            {
                if (own.Dots[dot] != behind.Dots[dot])
                {
                    apart++;
                }
            }
            Assert.True(apart == 0, when + ": " + apart + " dots of " + lit + " are not where the box has them");
        }

        TheSame("as it opens");
        // Scrolled down and across.
        box.ScrollToVerticalOffset(137);
        box.ScrollToHorizontalOffset(53);
        TheSame("scrolled");
        // Larger and smaller.
        typist.Editor.SetScale(1.5);
        TheSame("larger");
        typist.Editor.SetScale(0.5);
        box.ScrollToHome();
        TheSame("smaller");
        // Words with a character in them at which a text box starts a new line, though
        // it is no line break of the song's: the letters behind start a new line there
        // too, and stand where the box has its own. Each such character in turn.
        typist.Editor.SetScale(1);
        foreach (var unit in new[] { (char)0x0B, (char)0x0C, (char)0x85, (char)0x2028, (char)0x2029 })
        {
            var named = "U+" + ((int)unit).ToString("X4", CultureInfo.InvariantCulture);
            typist.Editor.Show(new LibraryModel.ChordProShown("one line" + unit + "of the song's, in two\nand its next", 2, 2, null), canType: true);
            Assert.True(box.LineCount == 3, named + ": the box has " + box.LineCount + " lines");
            TheSame("a line the box breaks in two at " + named);
        }
        // And a line with a chord in it is as wide drawn bold as the box has it plain,
        // so that the letters after a chord are under the caret that is at them.
        const string line = "[G]One two three [D/F#]four [Am7]";
        var alone = Alone(line);
        var across = alone.Box.GetRectFromCharacterIndex(line.Length).X - alone.Box.GetRectFromCharacterIndex(0).X;
        Assert.True(across > 100);
        Assert.Equal(across, alone.Editor.Underlay.WidthOf(line), 3);
    });

    // ---- Through a window

    [Fact]
    public void KeysPressedInAWindowComeInByTheSameDoors() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        var typist = At(model);
        // A window of the system's that is never shown, with the box in it: a key press
        // is something that happens in a window.
        using var window = new HwndSource(new HwndSourceParameters("Pro7ChordEditor tests", 600, 400) { WindowStyle = 0 }) { RootVisual = typist.Editor };
        Pictures.Settle();
        var box = typist.Box;

        bool Press(Key key)
        {
            var looked = new KeyEventArgs(Keyboard.PrimaryDevice, window, 0, key) { RoutedEvent = Keyboard.PreviewKeyDownEvent };
            box.RaiseEvent(looked);
            if (looked.Handled)
            {
                return true;
            }
            var pressed = new KeyEventArgs(Keyboard.PrimaryDevice, window, 0, key) { RoutedEvent = Keyboard.KeyDownEvent };
            box.RaiseEvent(pressed);
            return pressed.Handled;
        }

        // Backspace just after a chord's "]": the help takes the whole chord.
        var chord = typist.PlaceOf("[G/B]");
        typist.CaretAt(chord + 5);
        Assert.True(Press(Key.Back));
        Assert.Equal("[C]Nine ten eleven twelve", typist.LineWith("Nine"));
        // Delete among the words: the box's own Delete takes a letter, which is not let
        // through, and it is put back with the caret where it was.
        var among = typist.PlaceOf("Nine") + 1;
        typist.CaretAt(among);
        var before = typist.Text;
        Press(Key.Delete);
        Assert.Equal(before, typist.Text);
        Assert.Equal((among, 0), typist.Selected);
        Assert.Equal(1, typist.SaidNo);
        // Backspace inside a chord's name: the box's own, and let through.
        typist.CaretAt(typist.PlaceOf("[Em]") + 3);
        Press(Key.Back);
        Assert.Equal("Five six [E]seven eight", typist.LineWith("seven"));
        // And a character typed.
        typist.Type("7");
        Assert.Equal("Five six [E7]seven eight", typist.LineWith("seven"));
        Assert.Equal(typist.Text, model.ChordPro!.Text);
        await model.WhatWasTypedIsSavedAsync();
        Assert.Equal(["0:G", "14:D", "28:E7"], ChordsOf(0, "Plain Song", pc));
    });

    // ---- In the song's pane

    private static T Named<T>(FrameworkElement view, string name)
        where T : class => Assert.IsAssignableFrom<T>(view.FindName(name));

    // The first thing of a kind that a view has in it.
    private static T Found<T>(DependencyObject under)
        where T : DependencyObject
    {
        T? Look(DependencyObject at)
        {
            for (var child = 0; child < VisualTreeHelper.GetChildrenCount(at); child++)
            {
                var one = VisualTreeHelper.GetChild(at, child);
                if (one is T found)
                {
                    return found;
                }
                if (Look(one) is { } deeper)
                {
                    return deeper;
                }
            }
            return null;
        }

        return Assert.IsAssignableFrom<T>(Look(under));
    }

    private static void Click(ButtonBase button) => button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

    [Fact]
    public void InTheSongsPaneTheTextIsTypedInTheKeysArePickedTheTextIsSizedAndRevertAsksFirst() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        var view = new LibraryView(model);
        Look.Wear(view, dark: true);
        Pictures.LayOut(view, 1280, 800);
        var pane = view.SongPane;
        var typist = new Typist(pane.ChordPro, model);
        pane.ChordPro.SaysNo = () => typist.SaidNo++;
        var original = pc.BytesOf("Plain Song");

        // The text is the model's, and what is typed in it goes to the model and is saved.
        Assert.Equal(model.ChordPro!.Text, typist.Text);
        Assert.Equal(ChordProEditor.Help, Named<TextBlock>(pane, "Help").Text);
        Assert.Equal("Autosave on", Named<TextBlock>(pane, "Autosave").Text);
        var revert = Named<Button>(pane, "Revert");
        Assert.False(revert.IsEnabled);
        typist.CaretAt(typist.PlaceOf("Thirteen"));
        typist.Type("[A");
        await model.WhatWasTypedIsSavedAsync();
        Assert.Equal(["0:C", "10:G/B", "23:A"], ChordsOf(1, "Plain Song", pc));
        Assert.Equal("4 slides, 12 chords. A change is saved a moment after it is typed.", Named<TextBlock>(pane, "Quiet").Text);
        Assert.Equal("[A]Thirteen", typist.LineWith("Thirteen"));
        Assert.True(revert.IsEnabled);
        Assert.Equal(Visibility.Visible, Named<Button>(pane, "Earlier").Visibility);

        // The two keys: each a picker of the twenty-six, set to the song's.
        var key = Named<ComboBox>(pane, "Key");
        var shownIn = Named<ComboBox>(pane, "ShownKey");
        Assert.Equal((26, "G", "G"), (key.Items.Count, key.SelectedValue, shownIn.SelectedValue));
        Assert.True(key.IsEnabled && shownIn.IsEnabled);
        key.SelectedValue = "E";
        Assert.Equal(("E", "E"), (model.Shown!.Key, model.Shown.ShownKey));
        shownIn.SelectedValue = "A";
        Assert.Equal(("E", "A"), (model.Shown!.Key, model.Shown.ShownKey));
        Assert.Equal(("E", "A"), (key.SelectedValue, shownIn.SelectedValue));
        Assert.Equal("[A]Thirteen", typist.LineWith("Thirteen"));
        // One that is refused is not left looking as if it had been set.
        pc.ProPresenterIsRunning = true;
        key.SelectedValue = "D";
        Assert.Equal("E", model.Shown!.Key);
        Assert.Equal("E", key.SelectedValue);
        Assert.False(key.IsEnabled || shownIn.IsEnabled);
        Assert.Equal("Autosave paused", Named<TextBlock>(pane, "Autosave").Text);
        Assert.True(typist.Box.IsReadOnly);
        Assert.False(revert.IsEnabled);
        pc.ProPresenterIsRunning = false;
        model.ProPresenterMayHaveChanged();
        Assert.True(key.IsEnabled && !typist.Box.IsReadOnly && revert.IsEnabled);

        // The two letters at the right of the line of help.
        var smaller = Named<Button>(pane, "Smaller");
        var larger = Named<Button>(pane, "Larger");
        Click(larger);
        Assert.Equal(1.1, model.TextZoom);
        Assert.Equal(15.4, typist.Box.FontSize, 6);
        Click(smaller);
        Click(smaller);
        Assert.Equal(0.9, model.TextZoom);
        Assert.Equal(12.6, typist.Box.FontSize, 6);
        for (var times = 0; times < 9; times++)
        {
            Click(smaller);
        }
        Assert.False(smaller.IsEnabled);
        Assert.True(larger.IsEnabled);
        // And at the largest, the large letter is the one that is greyed.
        for (var times = 0; times < 9; times++)
        {
            Click(larger);
        }
        Assert.Equal(1.5, model.TextZoom);
        Assert.False(larger.IsEnabled);
        Assert.True(smaller.IsEnabled);
        // And the keys, from the box in the pane.
        Assert.True(pane.ChordPro.KeyPressed(Key.D0, ModifierKeys.Control));
        Assert.Equal(1.0, model.TextZoom);
        Assert.True(smaller.IsEnabled);

        // Revert asks first, over the window; Cancel leaves everything; yes makes the
        // song what it was, and the line under it offers to undo that.
        var question = Found<QuestionPanel>(view);
        Assert.Equal(Visibility.Collapsed, question.Visibility);
        Click(revert);
        Assert.Equal(Visibility.Visible, question.Visibility);
        Assert.Equal("Revert " + (char)0x201C + "Plain Song" + (char)0x201D + " to how it was when it was opened?", Named<TextBlock>(question, "Heading").Text);
        Assert.Equal("This takes back EVERY change made to this song since it was opened in this editor.", Named<TextBlock>(question, "Message").Text);
        Assert.Equal("Revert All Changes", Named<Button>(question, "Yes").Content);
        var cancel = Named<Button>(question, "Cancel");
        Assert.True(cancel.IsDefault && cancel.IsCancel);
        Click(cancel);
        Assert.Equal(Visibility.Collapsed, question.Visibility);
        Assert.NotEqual(original, pc.BytesOf("Plain Song"));
        // A click on the dimmed window behind the question is Cancel too, and so is Esc.
        Click(revert);
        var dimmed = Assert.IsType<Border>(Assert.IsType<Grid>(question.Content).Children[0]);
        dimmed.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = UIElement.MouseLeftButtonDownEvent });
        Assert.Equal(Visibility.Collapsed, question.Visibility);
        Click(revert);
        Assert.False(view.KeyPressed(Key.Escape, ModifierKeys.Shift));
        Assert.Equal(Visibility.Visible, question.Visibility);
        Assert.True(view.KeyPressed(Key.Escape, ModifierKeys.None));
        Assert.Equal(Visibility.Collapsed, question.Visibility);
        Assert.NotEqual(original, pc.BytesOf("Plain Song"));
        Assert.Null(model.JustPutBack);
        // (With nothing over the window, Esc is not the view's.)
        Assert.False(view.KeyPressed(Key.Escape, ModifierKeys.None));
        Click(revert);
        Click(Named<Button>(question, "Yes"));
        Assert.Equal(Visibility.Collapsed, question.Visibility);
        Assert.Equal(original, pc.BytesOf("Plain Song"));
        Assert.Equal("Thirteen", typist.LineWith("Thirteen"));
        Assert.Equal(("G", "G"), (key.SelectedValue, shownIn.SelectedValue));
        Assert.Equal("The song is as it was when it was opened here.", Named<TextBlock>(pane, "Quiet").Text);
        Assert.Equal(Visibility.Visible, Named<Button>(pane, "Undo").Visibility);
        Assert.False(revert.IsEnabled);
        Click(Named<Button>(pane, "Undo"));
        Assert.Equal("[A]Thirteen", typist.LineWith("Thirteen"));
        Assert.Equal(("E", "A"), (key.SelectedValue, shownIn.SelectedValue));
    });

    [Fact]
    public void TheKeyThatWouldSaveIsTheWholeViewsAndEscPutsTheEarlierVersionsAway() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Plain Song");
        var view = new LibraryView(model);
        Look.Wear(view, dark: true);
        Pictures.LayOut(view, 1280, 800);
        var typist = new Typist(view.SongPane.ChordPro, model);
        model.PauseBeforeSaving = TimeSpan.FromSeconds(60);
        typist.CaretAt(typist.PlaceOf("Thirteen"));
        typist.Type("[A");
        Assert.DoesNotContain(ChordsOf(1, "Plain Song", pc), chord => chord.EndsWith(":A", StringComparison.Ordinal));
        // Ctrl+S, and nothing else with S.
        Assert.False(view.KeyPressed(Key.S, ModifierKeys.None));
        Assert.False(view.KeyPressed(Key.S, ModifierKeys.Control | ModifierKeys.Shift));
        Assert.False(view.KeyPressed(Key.A, ModifierKeys.Control));
        Assert.Null(model.Said);
        Assert.True(view.KeyPressed(Key.S, ModifierKeys.Control));
        Assert.Equal(["0:C", "10:G/B", "23:A"], ChordsOf(1, "Plain Song", pc));
        Assert.Equal("Saved. Every change here is saved as it is made: there is nothing to save by hand.", Named<TextBlock>(view.SongPane, "Quiet").Text);
        // Esc puts the earlier versions away.
        model.ShowEarlierVersions();
        Assert.NotNull(model.Earlier);
        Assert.True(view.KeyPressed(Key.Escape, ModifierKeys.None));
        Assert.Null(model.Earlier);
    });

    [Fact]
    public void InTheSongsPaneASongThatCannotBeChangedHereSaysWhyOverItsText() => OnAMadeUpPC(async pc =>
    {
        var bytes = pc.BytesOf("Plain Song");
        "Thir[een"u8.CopyTo(bytes.AsSpan(bytes.AsSpan().IndexOf("Thirteen"u8)));
        File.WriteAllBytes(pc.Song("Plain Song"), bytes);
        var model = await Open(pc, "Plain Song");
        var view = new LibraryView(model);
        Look.Wear(view, dark: true);
        Pictures.LayOut(view, 1280, 800);
        var pane = view.SongPane;
        Assert.Equal("This song's words have square brackets of their own in them, so it cannot be changed as ChordPro text.", Named<TextBlock>(pane, "Help").Text);
        Assert.True(pane.ChordPro.TextBox.IsReadOnly);
        Assert.Contains("Thir[een", pane.ChordPro.TextBox.Text, StringComparison.Ordinal);
        // Its keys can still be set, and the line under it does not say that what is
        // typed is saved.
        Assert.True(Named<ComboBox>(pane, "Key").IsEnabled);
        Assert.Equal("4 slides, 11 chords.", Named<TextBlock>(pane, "Quiet").Text);
        // Another song opened, and the line of help is the usual one again.
        model.Open(pc.Song("Amazing Grace"));
        Assert.Equal(ChordProEditor.Help, Named<TextBlock>(pane, "Help").Text);
        Assert.False(pane.ChordPro.TextBox.IsReadOnly);
    });

    [Fact]
    public void TheHeaderIsLaidOutInTheRoomThePaneHas() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "A Song - In A Minor Key");
        // (With an earlier version, so that its button is there too.)
        model.SetShownKey("Bm");
        var view = new LibraryView(model);
        Look.Wear(view, dark: true);
        var pane = view.SongPane;
        var header = Named<FrameworkElement>(pane, "Header");
        var name = Named<TextBlock>(pane, "SongName");
        var keys = Named<FrameworkElement>(pane, "Keys");
        var earlier = Named<Button>(pane, "Earlier");
        var editors = pane.Switch;

        // Where a thing of the header is, in the pane.
        Rect Of(FrameworkElement thing) => new(thing.TranslatePoint(default, pane), new Size(thing.ActualWidth, thing.ActualHeight));

        HeaderRows? At(double width)
        {
            Pictures.LayOut(view, width, 600);
            Pictures.LayOut(view, width, 600);
            // Whatever the way, nothing but the name is cut short, and nothing stands
            // on anything else. (Of what is there: a song that could not be read has
            // no switch and no keys.)
            var there = new FrameworkElement[] { name, editors, earlier, keys }.Where(thing => thing.Visibility == Visibility.Visible).ToList();
            foreach (var thing in there.Skip(1))
            {
                Assert.True(Of(thing).Left >= Of(header).Left - 0.5 && Of(thing).Right <= Of(header).Right + 0.5, thing.Name + " is not within the header at " + width);
                Assert.True(thing.ActualWidth >= thing.DesiredSize.Width - thing.Margin.Left - 0.5, thing.Name + " is cut short at " + width);
            }
            var all = there.Select(Of).ToList();
            for (var one = 0; one < all.Count; one++)
            {
                for (var other = one + 1; other < all.Count; other++)
                {
                    var both = Rect.Intersect(all[one], all[other]);
                    Assert.True(both.IsEmpty || both.Width < 0.5 || both.Height < 0.5, "two things of the header stand on each other at " + width);
                }
            }
            return pane.HeaderLaidOut;
        }

        // Wide: one row, the name, the switch, Earlier Versions, the keys; and the switch
        // in the middle of the pane.
        Assert.Equal(HeaderRows.One, At(1600));
        Assert.Equal(Of(header).Left + (Of(header).Width / 2), Of(editors).Left + (Of(editors).Width / 2), 1.0);
        Assert.True(Of(name).Right <= Of(editors).Left && Of(editors).Right <= Of(earlier).Left && Of(earlier).Right <= Of(keys).Left);
        Assert.Equal(Of(header).Right, Of(keys).Right, 0.5);
        // (How wide the name is when none of it is cut short: what it asks for, where it
        // is given more.)
        var nameWhole = name.DesiredSize.Width;
        Assert.InRange(nameWhole, 100, name.ActualWidth);

        // Narrower and narrower: one row, then the keys under, then a row each, and
        // never back to a wider way.
        var seen = new List<HeaderRows>();
        var offTheMiddle = 0;
        for (var width = 1580.0; width >= 620; width -= 20)
        {
            var way = At(width)!.Value;
            Assert.True(seen.Count == 0 || way >= seen[^1], "at " + width + " the header went back to " + way);
            seen.Add(way);
            var middle = Of(header).Left + (Of(header).Width / 2);
            switch (way)
            {
                case HeaderRows.One:
                    // The switch stands where the name's side ends, which SongLook says:
                    // in the middle while each side can be read in its half, and after
                    // that where the name and the keys leave it.
                    var side = SongLook.NameSide(nameWhole, editors.ActualWidth, SongLook.AtTheRight(earlier.ActualWidth, keys.ActualWidth), Of(header).Width);
                    Assert.True(
                        Math.Abs(Of(header).Left + side + SongLook.Gap - Of(editors).Left) <= 1.5,
                        string.Create(CultureInfo.InvariantCulture, $"at {width}: the header is {Of(header).Width} wide from {Of(header).Left}; the name {nameWhole} (now {name.ActualWidth}), the switch {editors.ActualWidth} at {Of(editors).Left}, Earlier Versions {earlier.ActualWidth} at {Of(earlier).Left}, the keys {keys.ActualWidth} at {Of(keys).Left}; the name's side should be {side} and its column is {((Grid)header).ColumnDefinitions[0].ActualWidth}"));
                    Assert.Equal(Of(header).Right, Of(keys).Right, 0.5);
                    Assert.True(Of(name).Right <= Of(editors).Left && Of(editors).Right <= Of(earlier).Left && Of(earlier).Right <= Of(keys).Left);
                    Assert.True(name.ActualWidth >= nameWhole - 0.5, "the name is cut short on the one row at " + width);
                    if (Math.Abs(Of(editors).Left + (Of(editors).Width / 2) - middle) > 5)
                    {
                        offTheMiddle++;
                    }
                    break;
                case HeaderRows.KeysUnder:
                    // The name and the switch over the keys, with Earlier Versions at the
                    // right of the keys' row.
                    Assert.Equal(Of(header).Right, Of(editors).Right, 0.5);
                    Assert.True(Of(name).Right <= Of(editors).Left);
                    Assert.True(name.ActualWidth >= nameWhole - 0.5, "the name is cut short beside the switch at " + width);
                    Assert.True(Of(keys).Top >= Of(editors).Bottom && Of(earlier).Top >= Of(editors).Bottom);
                    Assert.Equal(Of(header).Left, Of(keys).Left, 0.5);
                    Assert.Equal(Of(header).Right, Of(earlier).Right, 0.5);
                    break;
                default:
                    // The name with Earlier Versions, the switch in the middle, and the
                    // keys, a row each.
                    Assert.Equal(Of(header).Right, Of(earlier).Right, 0.5);
                    Assert.True(Of(name).Right <= Of(earlier).Left);
                    Assert.True(Of(editors).Top >= Of(earlier).Bottom && Of(keys).Top >= Of(editors).Bottom);
                    Assert.Equal(middle, Of(editors).Left + (Of(editors).Width / 2), 1.0);
                    Assert.Equal(Of(header).Left, Of(keys).Left, 0.5);
                    break;
            }
        }
        Assert.Equal([HeaderRows.One, HeaderRows.KeysUnder, HeaderRows.EachARow], seen.Distinct());
        Assert.True(offTheMiddle > 0, "the switch was in the middle at every width at which the header was one row");
        // And wide again, it is one row again.
        Assert.Equal(HeaderRows.One, At(1600));
        Assert.Equal(Of(header).Left + (Of(header).Width / 2), Of(editors).Left + (Of(editors).Width / 2), 1.0);
        // A song that could not be read has a name and nothing else.
        model.Open(pc.Song("Nonsense"));
        At(1280);
        Assert.Equal(Visibility.Collapsed, editors.Visibility);
        Assert.Equal(Visibility.Collapsed, keys.Visibility);
    });

    [Fact]
    public void ASongThatStatesNoKeyShowsNoneAndItsSecondKeyCannotBeSetUntilItsFirstIs() => OnAMadeUpPC(async pc =>
    {
        var model = await Open(pc, "Holy Holy Holy");
        var view = new LibraryView(model);
        Look.Wear(view, dark: true);
        Pictures.LayOut(view, 1280, 800);
        var key = Named<ComboBox>(view.SongPane, "Key");
        var shownIn = Named<ComboBox>(view.SongPane, "ShownKey");
        Assert.Equal((27, ""), (key.Items.Count, key.SelectedValue));
        Assert.Equal("None", ((SongPane.KeyRow)key.SelectedItem).Name);
        Assert.True(key.IsEnabled);
        Assert.False(shownIn.IsEnabled);
        Assert.Equal("This song does not say what key it is in. Pick the key its chords are written in.", key.ToolTip);
        // Picked, the first key is the song's, and "None" is no longer listed.
        key.SelectedValue = "D";
        Assert.Equal(("D", "D"), (model.Shown!.Key, model.Shown.ShownKey));
        Assert.Equal((26, "D"), (key.Items.Count, key.SelectedValue));
        Assert.Equal((26, "D"), (shownIn.Items.Count, shownIn.SelectedValue));
        Assert.True(shownIn.IsEnabled);
        // A song that could not be read has no keys to show, and no text.
        model.Open(pc.Song("Nonsense"));
        Assert.Equal(Visibility.Collapsed, Named<FrameworkElement>(view.SongPane, "Keys").Visibility);
        Assert.Equal(Visibility.Collapsed, Named<FrameworkElement>(view.SongPane, "Editor").Visibility);
        Assert.Equal(Visibility.Collapsed, Named<FrameworkElement>(view.SongPane, "Saving").Visibility);
    });

    [Fact]
    public void AKeyThatIsNotOneOfTheTwentySixIsListedFirstSoThatItCanBeSeenAndChosen()
    {
        Assert.Equal(26, LibraryModel.KeysListedFor("G").Count);
        Assert.Same(LibraryModel.Keys, LibraryModel.KeysListedFor("Ebm"));
        var none = LibraryModel.KeysListedFor("");
        Assert.Equal((27, "", "C"), (none.Count, none[0], none[1]));
        var other = LibraryModel.KeysListedFor("C#");
        Assert.Equal((27, "C#", "C", "Bm"), (other.Count, other[0], other[1], other[26]));
    }

    [Fact]
    public void TheHeaderIsOneRowWhereTheNameCanBeReadWholeBesideTheRestAndOtherwiseTheKeysGoUnderIt()
    {
        // A name of 200, a switch of 230, Earlier Versions of 110 and keys of 260, each
        // parted from what is at its left by 14: one row wants 842.
        Assert.Equal(14, SongLook.Gap);
        Assert.Equal(HeaderRows.One, SongLook.Rows(name: 200, editors: 230, earlier: 110, keys: 260, room: 842));
        // A little less, and the name would be cut short: the keys go under it and the
        // switch, and Earlier Versions with them, at their row's right. The name's row
        // wants 444 and the keys' 384.
        Assert.Equal(HeaderRows.KeysUnder, SongLook.Rows(name: 200, editors: 230, earlier: 110, keys: 260, room: 841));
        Assert.Equal(HeaderRows.KeysUnder, SongLook.Rows(name: 200, editors: 230, earlier: 110, keys: 260, room: 444));
        // Less than that, and each has a row.
        Assert.Equal(HeaderRows.EachARow, SongLook.Rows(name: 200, editors: 230, earlier: 110, keys: 260, room: 443));
        // With a shorter name it is the keys' row that says: it wants 384.
        Assert.Equal(HeaderRows.KeysUnder, SongLook.Rows(name: 100, editors: 230, earlier: 110, keys: 260, room: 384));
        Assert.Equal(HeaderRows.EachARow, SongLook.Rows(name: 100, editors: 230, earlier: 110, keys: 260, room: 383));
        // What is not there takes no room, nor a gap: a song with no earlier versions,
        // and one that could not be read, which has no switch and no keys.
        Assert.Equal(HeaderRows.One, SongLook.Rows(name: 200, editors: 230, earlier: 0, keys: 260, room: 718));
        Assert.Equal(HeaderRows.KeysUnder, SongLook.Rows(name: 200, editors: 230, earlier: 0, keys: 260, room: 717));
        Assert.Equal(HeaderRows.One, SongLook.Rows(name: 200, editors: 0, earlier: 110, keys: 0, room: 324));
        Assert.Equal(HeaderRows.KeysUnder, SongLook.Rows(name: 200, editors: 0, earlier: 110, keys: 0, room: 323));
        Assert.Equal(HeaderRows.One, SongLook.Rows(name: 900, editors: 0, earlier: 0, keys: 0, room: 900));
        // A name too long for any row is cut short, on a row with Earlier Versions.
        Assert.Equal(HeaderRows.EachARow, SongLook.Rows(name: 900, editors: 0, earlier: 110, keys: 0, room: 899));
        Assert.Equal(HeaderRows.EachARow, SongLook.Rows(name: 900, editors: 230, earlier: 110, keys: 260, room: 500));

        // What stands at the right, side by side.
        Assert.Equal(384, SongLook.AtTheRight(earlier: 110, keys: 260));
        Assert.Equal(260, SongLook.AtTheRight(earlier: 0, keys: 260));
        Assert.Equal(110, SongLook.AtTheRight(earlier: 110, keys: 0));
        Assert.Equal(0, SongLook.AtTheRight(earlier: 0, keys: 0));

        // On the one row the switch is in the middle of the pane where each side can be
        // read in half of what the switch leaves: of 1100, a switch of 230 and its two
        // gaps leave 842, and the name's side has 421. The middle of the switch is then
        // at 421 + 14 + 115, which is half of 1100.
        Assert.Equal(421, SongLook.NameSide(name: 200, editors: 230, atTheRight: 384, room: 1100));
        // Where what is at the right needs more than its half, it has it, and the name
        // what is left: of 900, 642 are left, and 384 of them are the right's.
        Assert.Equal(258, SongLook.NameSide(name: 200, editors: 230, atTheRight: 384, room: 900));
        // And where the name needs more than its half, it has it.
        Assert.Equal(500, SongLook.NameSide(name: 500, editors: 230, atTheRight: 100, room: 900));
    }

    // ---- With no box: ChordProTyping.cs

    [Fact]
    public void TheOneChangeBetweenTwoTextsIsSaidAsSmallAsItCanBe()
    {
        Assert.Null(TextChange.Between("same", "same"));
        Assert.Equal(new TextChange(4, 0, "[G]"), TextChange.Between("One two", "One [G]two"));
        Assert.Equal(new TextChange(4, 3, ""), TextChange.Between("One [G]two", "One two"));
        Assert.Equal(new TextChange(5, 1, "D"), TextChange.Between("One [G]two", "One [D]two"));
        Assert.Equal(new TextChange(0, 0, "x"), TextChange.Between("", "x"));
        Assert.Equal(new TextChange(0, 3, ""), TextChange.Between("abc", ""));
        // Where the same character stands on both sides, either place says the same change.
        Assert.Equal("aaa", TextChange.Between("aa", "aaa")!.Value.AppliedTo("aa"));
        foreach (var (before, after) in new[]
        {
            ("One two", "One [G]two"), ("abcabc", "abc"), ("abc", "abcabc"), ("a\nb\nc", "a\n[G]b\nc"), ("", ""), ("xyz", "abc"), ("ab", "ba"),
        })
        {
            Assert.Equal(after, TextChange.Between(before, after)?.AppliedTo(before) ?? before);
        }
        // Never from the middle of a character that is two units: two faces that begin
        // with the same unit and end with another.
        var grin = char.ConvertFromUtf32(0x1F600);
        var wink = char.ConvertFromUtf32(0x1F609);
        Assert.Equal(grin[0], wink[0]);
        Assert.Equal(new TextChange(1, 2, wink), TextChange.Between("a" + grin + "b", "a" + wink + "b"));
        Assert.Equal(new TextChange(1, 2, grin), TextChange.Between("a" + wink, "a" + grin));
        Assert.Equal(new TextChange(3, 2, ""), TextChange.Between("a" + grin + grin, "a" + grin));
        Assert.Equal(new TextChange(1, 0, wink), TextChange.Between("a" + grin, "a" + wink + grin));
        // Nor of two that END with the same unit and begin with another.
        var other = char.ConvertFromUtf32(0x1F200);
        Assert.Equal(grin[1], other[1]);
        Assert.NotEqual(grin[0], other[0]);
        Assert.Equal(new TextChange(1, 2, other), TextChange.Between("a" + grin + "b", "a" + other + "b"));
    }

    [Fact]
    public void LineBreaksAsWindowsWritesThemAreMadeTheOneUnitTheSongCounts()
    {
        const string asTheSong = "one\ntwo\n\nthree";
        Assert.Same(asTheSong, ChordProTyping.WithLineBreaksOfOneUnit(asTheSong));
        Assert.Equal(asTheSong, ChordProTyping.WithLineBreaksOfOneUnit("one\r\ntwo\r\n\r\nthree"));
        Assert.Equal(asTheSong, ChordProTyping.WithLineBreaksOfOneUnit("one\rtwo\r\rthree"));
        Assert.Equal("\n\n", ChordProTyping.WithLineBreaksOfOneUnit("\r\n\r"));
        // A place in the text as it came is so many units less far in, one for each
        // line break of two that is wholly before it.
        const string came = "one\r\ntwo\r\nthree";
        Assert.Equal([0, 3, 3, 4, 7, 7, 8, 13], new[] { 0, 3, 4, 5, 8, 9, 10, 15 }.Select(place => ChordProTyping.PlaceWithLineBreaksOfOneUnit(came, place)));
        Assert.Equal(13, ChordProTyping.PlaceWithLineBreaksOfOneUnit(came, 99));
        Assert.Equal(0, ChordProTyping.PlaceWithLineBreaksOfOneUnit(came, -1));
        Assert.Equal(2, ChordProTyping.PlaceWithLineBreaksOfOneUnit("a\rb", 2));
    }

    private static TypingHistory.State S(string text, int caret, int length = 0) => new(text, caret, length);

    [Fact]
    public void ARunOfTypingIsOneStepOfTheBoxsUndoAndSoIsARunOfDeleting()
    {
        var history = new TypingHistory();
        Assert.False(history.CanUndo || history.CanRedo);
        Assert.Null(history.Before);
        Assert.Null(history.After);
        // "[" brings its other, then a name a character at a time, each where the last
        // left the caret: one step.
        history.NoteTyped(S("ab", 1), S("a[]b", 2));
        history.Note(S("a[]b", 2), S("a[G]b", 3));
        history.Note(S("a[G]b", 3), S("a[Gm]b", 4));
        Assert.Equal(1, history.Steps);
        Assert.Equal(S("ab", 1), history.Before);
        // The caret moved between: another step.
        history.Note(S("a[Gm]b", 6), S("a[Gm]b[", 7));
        Assert.Equal(2, history.Steps);
        // A change that is no change is no step.
        history.Note(S("x", 0), S("x", 1));
        history.NoteTyped(S("x", 0), S("x", 1));
        Assert.Equal(2, history.Steps);
        // More than one character at once is not typing, and is a step of its own,
        // though it follows on; and typing after it does not join it.
        history.Note(S("a[Gm]b[", 7), S("a[Gm]b[Am7]", 11));
        history.Note(S("a[Gm]b[Am7]", 11), S("a[Gm]b[Am7]c", 12));
        Assert.Equal(4, history.Steps);
        // Deleting, one character at a time where the caret is: one step. It does not
        // join the typing before it.
        history.Note(S("a[Gm]b[Am7]c", 12), S("a[Gm]b[Am7]", 11));
        history.Note(S("a[Gm]b[Am7]", 11), S("a[Gm]b[Am7", 10));
        Assert.Equal(5, history.Steps);
        Assert.Equal(S("a[Gm]b[Am7]c", 12), history.Before);
        // A character that is two units is one character typed.
        var grin = char.ConvertFromUtf32(0x1F600);
        history.Note(S("q", 1), S("q" + grin, 3));
        history.Note(S("q" + grin, 3), S("q" + grin + grin, 5));
        Assert.Equal(6, history.Steps);
    }

    [Fact]
    public void TheBoxsUndoIsWalkedBackAndForwardAndForgetsWhatWasUndoneWhenSomethingNewIsDone()
    {
        var history = new TypingHistory();
        history.Note(S("a", 1), S("ab", 2));
        history.Note(S("ab", 0), S("xab", 1));
        Assert.Equal(S("ab", 0), history.Before);
        history.SteppedBack();
        Assert.Equal(S("a", 1), history.Before);
        Assert.Equal(S("xab", 1), history.After);
        history.SteppedBack();
        Assert.False(history.CanUndo);
        Assert.Null(history.Before);
        Assert.Equal(S("ab", 2), history.After);
        // (Asked to go back with nothing to take back, nothing happens.)
        history.SteppedBack();
        Assert.Equal(S("ab", 2), history.After);
        history.SteppedForward();
        Assert.Equal(S("a", 1), history.Before);
        Assert.Equal(S("xab", 1), history.After);
        // Something new is done: what was undone is gone. And it is a step of its own,
        // though it is typed where the step made again left the caret: a run of typing
        // ends with an undo or a redo.
        history.Note(S("ab", 2), S("ab!", 3));
        Assert.False(history.CanRedo);
        Assert.Null(history.After);
        Assert.Equal(2, history.Steps);
        Assert.Equal(S("ab", 2), history.Before);
        // (Asked to go forward with nothing to make again, nothing happens.)
        history.SteppedForward();
        Assert.Equal(2, history.Steps);
        // What goes on from THAT joins it, as typing does.
        history.Note(S("ab!", 3), S("ab!?", 4));
        Assert.Equal(2, history.Steps);
        history.Forget();
        Assert.False(history.CanUndo || history.CanRedo);

        // An undo ends the run that is then the last, too: what is typed where that
        // run left the caret is a step of its own.
        var run = new TypingHistory();
        run.Note(S("a", 1), S("ab", 2));
        run.Note(S("ab", 0), S("xab", 1));
        run.SteppedBack();
        run.Note(S("ab", 2), S("abc", 3));
        Assert.Equal(2, run.Steps);
        Assert.Equal(S("ab", 2), run.Before);
    }

    [Fact]
    public void TheOldestStepsAreForgottenWhenThereAreMoreThanAreKept()
    {
        var history = new TypingHistory();
        for (var step = 0; step < TypingHistory.Most + 25; step++)
        {
            // (Each with the caret somewhere else than the last left it: a step of its own.)
            history.Note(S(new string('a', step), 0), S(new string('a', step + 1), 0, 1));
        }
        Assert.Equal(TypingHistory.Most, history.Steps);
        Assert.Equal(300, TypingHistory.Most);
        Assert.Equal(S(new string('a', TypingHistory.Most + 24), 0), history.Before);
    }
}
