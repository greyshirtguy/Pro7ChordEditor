// Someone at the ChordPro text box: what they do to it, and what they then see. For the
// tests that type into the box (ChordProTypingTests.cs says how a key is handed to it
// with no keyboard), and for the pictures of a song being typed in.

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Xunit;

namespace Pro7ChordEditor.AppViews.Tests;

internal sealed class Typist(ChordProText editor, LibraryModel? model)
{
    public ChordProText Editor { get; } = editor;

    public TextBox Box => Editor.TextBox;

    public string Text => Box.Text;

    public (int Start, int Length) Selected => (Box.SelectionStart, Box.SelectionLength);

    /// <summary>How many times the sound for "no" would have been made.</summary>
    public int SaidNo { get; set; }

    /// <summary>What the pane does whenever the model says it has changed.</summary>
    public void Show() => Editor.Show(model!.ChordPro!, model.CanType);

    public int PlaceOf(string what)
    {
        var at = Text.IndexOf(what, StringComparison.Ordinal);
        Assert.True(at >= 0, "the box has no \"" + what + "\" in it");
        return at;
    }

    public string LineWith(string what) => Text.Split('\n').FirstOrDefault(line => line.Contains(what, StringComparison.Ordinal)) ?? "";

    public void CaretAt(int place, int length = 0) => Box.Select(place, length);

    /// <summary>Types, a character at a time, as a keyboard does.</summary>
    public void Type(string typed)
    {
        foreach (var character in typed)
        {
            var composition = new TextComposition(InputManager.Current, Box, character.ToString());
            var looked = new TextCompositionEventArgs(Keyboard.PrimaryDevice, composition) { RoutedEvent = TextCompositionManager.PreviewTextInputEvent };
            Box.RaiseEvent(looked);
            if (!looked.Handled)
            {
                Box.RaiseEvent(new TextCompositionEventArgs(Keyboard.PrimaryDevice, composition) { RoutedEvent = TextCompositionManager.TextInputEvent });
            }
        }
    }

    /// <summary>Presses Backspace or Delete, with these other keys held.</summary>
    public void Press(Key key, ModifierKeys held = ModifierKeys.None)
    {
        if (Editor.KeyPressed(key, held))
        {
            return;
        }
        // Not taken by the help: the key is the one it always is.
        (key == Key.Back ? EditingCommands.Backspace : EditingCommands.Delete).Execute(null, Box);
    }

    /// <summary>Pastes at a place: what is selected there has this put in its place.</summary>
    public void Paste(string pasted, int at, int over = 0)
    {
        Box.Select(at, over);
        Box.SelectedText = pasted;
    }
}
