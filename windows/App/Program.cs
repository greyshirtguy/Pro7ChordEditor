// The app itself: it starts, shows the window, and ends when the window is closed.
//
// That is all, and it is to stay all. What the window shows and what a click does is
// in AppViews, a library, so that tests can draw it into pictures and call what a
// click calls; what a click does to a song is in Song, with no window in it. Anything
// written here can be tried only by a person starting the app
// (spec/10-platforms.md, "The Windows app").

using System.Windows;
using Pro7ChordEditor.AppViews;

namespace Pro7ChordEditor.App;

public static class Program
{
    // A window and everything in it must be made and used on one thread, of the kind
    // Windows calls a single-threaded apartment. STAThread makes the app's first
    // thread one.
    [STAThread]
    public static void Main()
    {
        var app = new Application();
        app.Run(new LibraryWindow());
    }
}
