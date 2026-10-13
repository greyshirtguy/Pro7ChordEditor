// The code beside LibraryView.xaml, which says what the view is. Here it is handed its
// model, told to follow the look it is under (Look.cs), keeps the lists as wide as the
// edge was last dragged to, as far as the window's width lets it (ListsLook.Width), and
// passes on what is chosen in the menu.
//
// A few things in the menu open something of the system's or a window of their own, and
// so are here and not in the model: choosing a folder, the settings, the guide. And a
// few keys are the whole view's, whatever in it has the keyboard: Esc; Ctrl+S, which is
// in no menu and is answered here all the same (spec/06, rule 12a); F1, which shows the
// guide; and Ctrl with 1 and with 2, which show the song's two editors (rule 12).

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Win32;

namespace Pro7ChordEditor.AppViews;

/// <summary>What the window holds: its menu, the lists at the left, the song at the right.</summary>
public partial class LibraryView : UserControl
{
    // How wide the lists were when the edge was last let go: the model's, which
    // remembers it from one start of the app to the next.
    private double leftAt = ListsLook.UsualWidth;

    public LibraryView(LibraryModel model)
    {
        InitializeComponent();
        Look.Follow(this);
        // (The panes have it from here: what a view is handed, those in it are handed
        // too.)
        DataContext = model;
        leftAt = model.ListsWidth;
        SizeChanged += (_, _) => LayOut();
        // The model says how wide the lists were left: when the app starts, what was
        // remembered; and after the edge is let go or clicked twice. (Not while the
        // edge is being dragged: where it is then is the hand's to say.)
        model.PropertyChanged += (_, _) =>
        {
            if (leftAt != Model.ListsWidth && !Edge.IsDragging)
            {
                leftAt = Model.ListsWidth;
                LayOut();
            }
        };
    }

    /// <summary>
    /// Shows the guide, in a window of its own beside this one (spec/13-guide.md). The
    /// window sets it; a view in no window has none to show.
    /// </summary>
    public Action ShowGuide { get; set; } = () => { };

    /// <summary>The edge between the lists and the song: for a test to drag.</summary>
    internal GridSplitter TheEdge => Edge;

    /// <summary>The menus across the top, as their names: for a test to read.</summary>
    internal IReadOnlyList<MenuItem> Menus => [.. ((Menu)((MenuItem)Revert.Parent).Parent).Items.OfType<MenuItem>()];

    public LibraryModel Model => (LibraryModel)DataContext;

    /// <summary>The song's pane: for a test to type into its text.</summary>
    internal SongPane SongPane => Song;

    /// <summary>The lists at the left: for a test to click in.</summary>
    internal ListsPane ListsPane => Listed;

    /// <summary>Mend All's panel, over the window: for a test to read and to answer.</summary>
    internal MendAllPanel MendAllPanel => MendAll;

    /// <summary>The panel a question is asked in, over the window: for a test to read and to answer.</summary>
    internal QuestionPanel QuestionPanel => Asking;

    /// <summary>The panel of a new song from a ChordPro file: for a test to read, fill in and click.</summary>
    internal NewSongPanel NewSongPanel => NewSong;

    /// <summary>The two commands of the File menu that are about ChordPro files, as they are when the menu is opened. For a test.</summary>
    internal (MenuItem BringIn, MenuItem WriteOut) ChordProCommands
    {
        get
        {
            TheFileMenuWasOpened(this, new RoutedEventArgs());
            return (BringIn, WriteOut);
        }
    }

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

    /// <summary>
    /// What a ChordPro file is called by. There is no one ending: these are the ones in
    /// use, and a plain text file may be one too. (Not ".pro", which some use for
    /// ChordPro: here that is a ProPresenter presentation.)
    /// </summary>
    internal const string ChordProFiles = "ChordPro files|*.cho;*.chordpro;*.chopro;*.crd;*.txt|All files|*.*";

    /// <summary>
    /// Asks the person for a ChordPro file, with the system's own chooser. Nothing, if
    /// they chose none.
    /// </summary>
    internal static string? AskForAChordProFile(DependencyObject from)
    {
        var asking = new OpenFileDialog
        {
            Title = "Choose a ChordPro file to make a song from",
            Filter = ChordProFiles,
            CheckFileExists = true,
            Multiselect = false,
        };
        return asking.ShowDialog(Window.GetWindow(from)) == true && asking.FileName.Length > 0 ? asking.FileName : null;
    }

    /// <summary>
    /// Asks the person where a ChordPro file is to be written, with the system's own
    /// chooser, which asks before a file that is there is written over. Nothing, if they
    /// chose nowhere.
    /// </summary>
    internal static string? AskWhereToWrite(DependencyObject from, string name)
    {
        var asking = new SaveFileDialog
        {
            Title = "Save the song as a ChordPro file",
            Filter = "ChordPro files|*.cho|All files|*.*",
            DefaultExt = Pro7ChordEditor.Song.ChordProFile.Ending,
            AddExtension = true,
            FileName = name,
            OverwritePrompt = true,
        };
        return asking.ShowDialog(Window.GetWindow(from)) == true && asking.FileName.Length > 0 ? asking.FileName : null;
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

    private void TheEdgeWasDragged(object sender, DragCompletedEventArgs e) => EdgeLeftAt(Lists.ActualWidth);

    // Clicked twice, the edge goes back to where it starts.
    private void TheEdgeWasClickedTwice(object sender, MouseButtonEventArgs e)
    {
        EdgeLeftAt(null);
        e.Handled = true;
    }

    /// <summary>
    /// The edge was let go where the lists are this wide; or, with nothing, was clicked
    /// twice, and goes back to where it starts. It is the model's to remember.
    /// </summary>
    internal void EdgeLeftAt(double? width)
    {
        Model.SetListsWidth(width);
        leftAt = Model.ListsWidth;
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
        // While Mend All is asking, at work, or saying what it did, it is over the whole
        // window: Esc is Cancel at its question and Done once it has finished, and the
        // window's other keys do nothing behind it.
        if (Model.MendAllQuestion is not null || Model.MendingAll is not null)
        {
            if (key == Key.Escape && held == ModifierKeys.None)
            {
                if (Model.MendAllQuestion is not null)
                {
                    Model.DontMendAll();
                }
                else
                {
                    Model.CloseMendAll();
                }
                return true;
            }
            return held == ModifierKeys.Control && key is Key.D1 or Key.NumPad1 or Key.D2 or Key.NumPad2 or Key.S;
        }
        // And so while a new song is being asked about: Esc is its Cancel (or its
        // Close), and the window's other keys do nothing behind it.
        if (Model.NewSong is not null)
        {
            if (key == Key.Escape && held == ModifierKeys.None)
            {
                Model.DontMakeTheSong();
                return true;
            }
            return held == ModifierKeys.Control && key is Key.D1 or Key.NumPad1 or Key.D2 or Key.NumPad2 or Key.S;
        }
        // Ctrl with 1 shows the chord sheet and with 2 the ChordPro text, and the
        // keyboard goes to the one shown. Not while something is asked over the window.
        if (held == ModifierKeys.Control && key is Key.D1 or Key.NumPad1 or Key.D2 or Key.NumPad2)
        {
            if (Model.Asked is null && Model.Earlier is null && Model.Shown is { Problem: null })
            {
                Show(key is Key.D1 or Key.NumPad1 ? LibraryModel.Editor.Sheet : LibraryModel.Editor.ChordPro);
            }
            return true;
        }
        if (key == Key.Escape && held == ModifierKeys.None && Model.Asked is not null)
        {
            Model.Answer(false);
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
        // F1 is Windows' key for help: the guide (spec/10-platforms.md).
        if (key == Key.F1 && held == ModifierKeys.None)
        {
            ShowGuide();
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

    // "New Song from ChordPro File...": the system is asked for the file, and the model
    // reads it and shows what it found. Nothing is made until that has been seen.
    private void BringInAFile(object sender, RoutedEventArgs e)
    {
        if (Model.CanBringInAFile && AskForAChordProFile(this) is { } file)
        {
            Model.BringIn(file);
        }
    }

    // "Save Song as ChordPro File...": the system is asked where, and the model writes it.
    private void WriteAChordProFile(object sender, RoutedEventArgs e)
    {
        if (Model.CanWriteChordPro && AskWhereToWrite(this, Model.NameForAChordProFile) is { } path)
        {
            Model.WriteAsChordPro(path);
        }
    }

    private void OpenTheSettings(object sender, RoutedEventArgs e) => OpenSettings(null);

    // The settings, in a window of their own over this one: at the section they were
    // last at, or at one that is named.
    private void OpenSettings(string? section)
    {
        var settings = new SettingsWindow(Model) { Owner = Window.GetWindow(this) };
        if (section is not null)
        {
            settings.Show(section);
        }
        settings.ShowDialog();
    }

    private void CloseTheWindow(object sender, RoutedEventArgs e) => Window.GetWindow(this)?.Close();

    private void AskToRevert(object sender, RoutedEventArgs e) => Model.AskToRevert();

    private void ShowTheGuide(object sender, RoutedEventArgs e) => ShowGuide();

    // What the app is, and which version: the settings' own section of that name.
    private void ShowAbout(object sender, RoutedEventArgs e) => OpenSettings("About");

    // (A command that has nothing to do just now is greyed, and is still there to be
    // found.)
    private void TheFileMenuWasOpened(object sender, RoutedEventArgs e)
    {
        Revert.IsEnabled = Model.CanRevert;
        BringIn.IsEnabled = Model.CanBringInAFile;
        WriteOut.IsEnabled = Model.CanWriteChordPro;
    }

    // Shows the song in one of its two editors, and gives that one the keyboard.
    private void Show(LibraryModel.Editor editor)
    {
        Model.Show(editor);
        Song.GiveTheEditorTheKeyboard();
    }
}
