// The code beside EditorSwitch.xaml, which says what the switch is. Here: the one of the
// two that the model shows is the one filled, and a choice of the other is passed on.
//
// Nothing here is a rule. Whether the other editor can be gone to (what was being typed
// in this one is kept first, and may be refused) is LibraryModel's; so after a choice
// the switch shows what the model then has, which may be what it had.

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Pro7ChordEditor.AppViews;

/// <summary>The two buttons that choose between the chord sheet and the ChordPro text.</summary>
public partial class EditorSwitch : UserControl
{
    private LibraryModel? model;

    // What is being put into the buttons is from the model, and not a choice made.
    private bool showing;

    public EditorSwitch()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Follow(DataContext as LibraryModel);
    }

    /// <summary>
    /// One of the two was clicked, or chosen with Space: whoever holds the switch may
    /// want to give the keyboard to the editor that is now shown.
    /// </summary>
    public event EventHandler? Clicked;

    /// <summary>Which of the two is filled.</summary>
    internal LibraryModel.Editor Filled => ChordPro.IsChecked == true ? LibraryModel.Editor.ChordPro : LibraryModel.Editor.Sheet;

    /// <summary>Chooses one, as a click on it does.</summary>
    internal void Choose(LibraryModel.Editor editor)
    {
        (editor == LibraryModel.Editor.Sheet ? Chords : ChordPro).IsChecked = true;
        Clicked?.Invoke(this, EventArgs.Empty);
    }

    private void Follow(LibraryModel? now)
    {
        if (model is not null)
        {
            model.PropertyChanged -= TheModelChanged;
        }
        model = now;
        if (model is not null)
        {
            model.PropertyChanged += TheModelChanged;
        }
        Show();
    }

    private void TheModelChanged(object? sender, PropertyChangedEventArgs e) => Show();

    private void Show()
    {
        if (model is null)
        {
            return;
        }
        showing = true;
        try
        {
            var sheet = model.EditorShown == LibraryModel.Editor.Sheet;
            Chords.IsChecked = sheet;
            ChordPro.IsChecked = !sheet;
        }
        finally
        {
            showing = false;
        }
    }

    private void OneWasChosen(object sender, RoutedEventArgs e)
    {
        if (showing || model is null)
        {
            return;
        }
        model.Show(ReferenceEquals(sender, Chords) ? LibraryModel.Editor.Sheet : LibraryModel.Editor.ChordPro);
        // (Whatever came of it, the one filled is the one that is shown.)
        Show();
    }

    private void OneWasClicked(object sender, RoutedEventArgs e) => Clicked?.Invoke(this, EventArgs.Empty);
}
