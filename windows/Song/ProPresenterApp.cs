// Whether ProPresenter is running. spec/06-library.md, rules 16 to 20.
//
// On Windows it is running if a process is that is named `ProPresenter`: so the first
// editor asked (old/Pro7ChordEditor/MainWindow.xaml.cs), and so the page has it. Its
// helper programs have names of their own ("ProPresenter Helper - Network" and others,
// seen beside version 22) and do not count: the name is asked for whole.
//
// Asking is the system's own list of what is running, read. Nothing is done to
// ProPresenter, and no window of its is looked for.
//
// It is here, with the song, and not with the window as on the Mac (where asking needs
// the Mac's window toolkit): on Windows it needs nothing but .NET. The song is handed
// it as one of its safeguards, and asks it before every save.

using System.Diagnostics;

namespace Pro7ChordEditor.Song;

public static class ProPresenterApp
{
    /// <summary>What ProPresenter's process is named: its program's name, without the ending.</summary>
    public const string ProcessName = "ProPresenter";

    public static bool IsRunning() => IsRunning(ProcessName);

    /// <summary>Whether a process of this name is running. (With a name to give, so that a test can ask after itself.)</summary>
    internal static bool IsRunning(string named)
    {
        try
        {
            var found = Process.GetProcessesByName(named);
            foreach (var process in found)
            {
                process.Dispose();
            }
            return found.Length > 0;
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
        {
            // The system would not say. Nothing can be told from that, and a save is
            // not to be let through on it: it is taken as running.
            return true;
        }
    }
}
