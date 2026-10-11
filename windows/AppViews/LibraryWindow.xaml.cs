// The code beside LibraryWindow.xaml, which says what the window is and why it holds
// only a view. Here is what only a window can know, and the model is told of it:
//
//   - that it has first been shown: the model starts (finds the places, lists them);
//   - that the app has come to the front: the lists are read again, since ProPresenter
//     may have added songs meanwhile (spec/06-library.md, rule 15);
//   - that a moment has gone by: whether ProPresenter is running is asked again, every
//     two seconds while the window is open, so that the band comes and goes by itself
//     (rule 18). Windows has no news of a program starting or quitting that an app can
//     have for the asking, as a Mac has; the page gives this as the other way.
//
// None of that can be tried by a test, which opens no window: it is for the maintainer
// to try. What the model does when it is told is tried in LibraryModelTests.

using System.Windows;
using System.Windows.Threading;

namespace Pro7ChordEditor.AppViews;

/// <summary>The app's one window.</summary>
public partial class LibraryWindow : Window
{
    private readonly DispatcherTimer looking = new() { Interval = TimeSpan.FromSeconds(2) };

    /// <summary>The window on this PC: its songs, its ProPresenter.</summary>
    public LibraryWindow()
        : this(LibraryModel.OnThisPC())
    {
    }

    public LibraryWindow(LibraryModel model)
    {
        InitializeComponent();
        Content = new LibraryView(model);
        Loaded += async (_, _) =>
        {
            looking.Start();
            await model.StartAsync();
        };
        // (Asked before the model has started, as when the window is first shown, this
        // does nothing.)
        Activated += async (_, _) => await model.RefreshAsync();
        // What was typed and is waiting for its pause is saved when the app is left for
        // another program, and when the window is closed (spec/04, rule 13).
        Deactivated += (_, _) => model.Settle();
        Closing += (_, _) => model.Settle();
        looking.Tick += async (_, _) => await model.LookForProPresenterAsync();
        Closed += (_, _) => looking.Stop();
    }
}
