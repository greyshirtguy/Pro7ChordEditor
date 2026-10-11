// The code beside LibraryView.xaml, which says what the view is. Here it is handed its
// model, told to follow the look it is under (Look.cs), keeps the lists as wide as the
// edge was last dragged to, as far as the window's width lets it (ListsLook.Width), and
// passes on what is chosen in the menu.
//
// Two things in the menu open something of the system's or a window of their own, and
// so are here and not in the model: choosing a folder, and the settings. And two keys
// are the whole view's, whatever in it has the keyboard: Esc and Ctrl+S.

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
        if (e.Key == Key.Escape && Model.RevertQuestion is not null)
        {
            Model.DontRevert();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && Model.Earlier is not null)
        {
            Model.CloseEarlierVersions();
            e.Handled = true;
        }
        else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
        {
            Model.SaveNow();
            e.Handled = true;
        }
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

    private void ShowEarlierVersions(object sender, RoutedEventArgs e) => Model.ShowEarlierVersions();
}
