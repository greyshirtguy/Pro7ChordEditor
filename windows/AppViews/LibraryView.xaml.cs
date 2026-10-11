// The code beside LibraryView.xaml, which says what the view is. Here it is handed its
// model, told to follow the look it is under (Look.cs), keeps the lists as wide as the
// edge was last dragged to, as far as the window's width lets it (ListsLook.Width), and
// passes on what is chosen in the menu.
//
// Two things in the menu open something of the system's or a window of their own, and
// so are here and not in the model: choosing a folder, and the settings. And a few keys
// are the whole view's, whatever in it has the keyboard: Esc, Ctrl+S, and Ctrl with 1
// and with 2, which show the song's two editors (spec/06, rule 12).

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Win32;

namespace Pro7ChordEditor.AppViews;

/// <summary>What the window holds: its menu, the lists at the left, the song at the right.</summary>
public partial class LibraryView : UserControl
{
    // How wide the lists were when the edge was last let go.
    private double leftAt = ListsLook.UsualWidth;

    public LibraryView(LibraryModel model)
    {
        InitializeComponent();
        Look.Follow(this);
        // (The panes have it from here: what a view is handed, those in it are handed
        // too.)
        DataContext = model;
        SizeChanged += (_, _) => LayOut();
    }

    public LibraryModel Model => (LibraryModel)DataContext;

    /// <summary>The song's pane: for a test to type into its text.</summary>
    internal SongPane SongPane => Song;

    /// <summary>
    /// Asks the person for a folder, with the system's own chooser, over the window
    /// this is in. Nothing, if they chose none.
    /// </summary>
    internal static string? AskForAFolder(DependencyObject from)
    {
        var asking = new OpenFolderDialog
        {
            Title = "Choose a ProPresenter folder, its Libraries folder, or any folder of .pro files",
            Multiselect = false,
        };
        return asking.ShowDialog(Window.GetWindow(from)) == true && asking.FolderName.Length > 0 ? asking.FolderName : null;
    }

    // Gives the lists their width for the width the view now has.
    private void LayOut()
    {
        if (ActualWidth <= 0)
        {
            return;
        }
        var width = ListsLook.Width(leftAt, ActualWidth);
        if (Lists.Width.Value != width)
        {
            Lists.Width = new GridLength(width);
        }
    }

    private void TheEdgeWasDragged(object sender, DragCompletedEventArgs e)
    {
        leftAt = Lists.ActualWidth;
        LayOut();
    }

    // Esc puts the earlier versions away (spec/07, rule 17), and is Cancel to what
    // Revert asks. Ctrl+S, pressed out of habit, saves at once what was typed and is
    // waiting for its pause, and the quiet line says there was nothing to do by hand
    // (spec/06, rule 12a). The view is asked before whatever in it has the keyboard.
    private void AKeyWasPressed(object sender, KeyEventArgs e)
    {
        if (KeyPressed(e.Key, Keyboard.Modifiers))
        {
            e.Handled = true;
        }
    }

    /// <summary>A key was pressed, with these others held. Answers whether it was one of the view's own.</summary>
    internal bool KeyPressed(Key key, ModifierKeys held)
    {
        // Ctrl with 1 shows the chord sheet and with 2 the ChordPro text, and the
        // keyboard goes to the one shown. Not while something is asked over the window.
        if (held == ModifierKeys.Control && key is Key.D1 or Key.NumPad1 or Key.D2 or Key.NumPad2)
        {
            if (Model.RevertQuestion is null && Model.Earlier is null && Model.Shown is { Problem: null })
            {
                Show(key is Key.D1 or Key.NumPad1 ? LibraryModel.Editor.Sheet : LibraryModel.Editor.ChordPro);
            }
            return true;
        }
        if (key == Key.Escape && held == ModifierKeys.None && Model.RevertQuestion is not null)
        {
            Model.DontRevert();
            return true;
        }
        if (key == Key.Escape && held == ModifierKeys.None && Model.Earlier is not null)
        {
            Model.CloseEarlierVersions();
            return true;
        }
        if (key == Key.S && held == ModifierKeys.Control)
        {
            Model.SaveNow();
            return true;
        }
        return false;
    }

    // ---- The menu

    private void ChooseAFolder(object sender, RoutedEventArgs e)
    {
        if (AskForAFolder(this) is { } folder)
        {
            Model.Add(folder);
        }
    }

    private void OpenTheSettings(object sender, RoutedEventArgs e) =>
        new SettingsWindow { Owner = Window.GetWindow(this) }.ShowDialog();

    private void CloseTheWindow(object sender, RoutedEventArgs e) => Window.GetWindow(this)?.Close();

    private void SaveNow(object sender, RoutedEventArgs e) => Model.SaveNow();

    private void AskToRevert(object sender, RoutedEventArgs e) => Model.AskToRevert();

    // (A command that has nothing to do just now is greyed, and is still there to be
    // found.)
    private void TheFileMenuWasOpened(object sender, RoutedEventArgs e)
    {
        Save.IsEnabled = Model.Shown is { Problem: null };
        Revert.IsEnabled = Model.CanRevert;
    }

    private void TheSongMenuWasOpened(object sender, RoutedEventArgs e) => EarlierVersions.IsEnabled = Model.HasEarlierVersions;

    private void TheViewMenuWasOpened(object sender, RoutedEventArgs e)
    {
        var read = Model.Shown is { Problem: null };
        var sheet = Model.EditorShown == LibraryModel.Editor.Sheet;
        ShowChords.IsEnabled = ShowChordPro.IsEnabled = UsualWords.IsEnabled = read;
        ShowChords.IsChecked = read && sheet;
        ShowChordPro.IsChecked = read && !sheet;
        LargerWords.IsEnabled = read && (sheet ? Model.CanZoomSheetIn : Model.CanZoomTextIn);
        SmallerWords.IsEnabled = read && (sheet ? Model.CanZoomSheetOut : Model.CanZoomTextOut);
    }

    // Shows the song in one of its two editors, and gives that one the keyboard.
    private void Show(LibraryModel.Editor editor)
    {
        Model.Show(editor);
        Song.GiveTheEditorTheKeyboard();
    }

    private void ShowTheSheet(object sender, RoutedEventArgs e) => Show(LibraryModel.Editor.Sheet);

    private void ShowTheText(object sender, RoutedEventArgs e) => Show(LibraryModel.Editor.ChordPro);

    // The words of the editor that is shown, a step larger or smaller, or their usual
    // size: the sheet's and the text's are each remembered by themselves.
    private void Zoom(int direction)
    {
        if (Model.EditorShown == LibraryModel.Editor.Sheet)
        {
            Model.ZoomSheet(direction);
        }
        else
        {
            Model.ZoomText(direction);
        }
    }

    private void MakeTheWordsLarger(object sender, RoutedEventArgs e) => Zoom(1);

    private void MakeTheWordsSmaller(object sender, RoutedEventArgs e) => Zoom(-1);

    private void MakeTheWordsTheirUsualSize(object sender, RoutedEventArgs e) => Zoom(0);

    private void ShowEarlierVersions(object sender, RoutedEventArgs e) => Model.ShowEarlierVersions();
}
