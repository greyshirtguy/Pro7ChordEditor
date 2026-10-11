// Finding where ProPresenter keeps songs as files. spec/06-library.md, "Where
// ProPresenter keeps its songs".
//
// A place is a folder with a `Libraries` folder in it, in which each folder is a library
// and each `.pro` file directly in one is a song. On one PC there can be:
//
//   - the folder ProPresenter was told to keep its things in, if that was moved from
//     Documents. ProPresenter writes where in a text file of its own,
//     `PathSettings.proPaths`, in a line of the form `Base=C:\\Some\\Folder;` with each
//     backslash written twice. KNOWN: the first editor read it so
//     (old/Pro7ChordEditor/MainWindow.xaml.cs), and this reads it the same way;
//   - `ProPresenter` in the person's Documents folder, where it is unless moved;
//   - any folder the person has chosen by hand: a copy of a library kept elsewhere,
//     songs exported from ProPresenter, or the songs of someone with no ProPresenter on
//     this PC at all.
//
// WHAT IS NOT FOUND, AND WHY. The Mac app also finds workspaces, which ProPresenter 21
// keeps there as folders of the same kind. On Windows the only ProPresenter looked at
// that has workspaces is version 22, and it keeps a workspace's songs as rows of a
// database and not as files (NOTES.md, "ProPresenter 22 keeps no .pro files"). There is
// nothing in such a workspace this app can open, so it is no place. What an earlier
// version on Windows did about workspaces, if anything, is NOT KNOWN.
//
// ProPresenter's settings are only ever read. Nothing of this app's is put in
// ProPresenter's folders, and nothing here makes a folder or a file.
//
// The finder is handed what it needs (what ProPresenter's file of paths says, where
// Documents is, what was chosen), so that tests can hand it made-up ones. `OnThisPC`
// hands it the real ones. The Mac's is Places.swift.

namespace Pro7ChordEditor.Song;

/// <summary>What kind of place a place is: how it was found.</summary>
public enum PlaceKind
{
    /// <summary>The folder ProPresenter's own file of paths names as its own.</summary>
    NamedInSettings,

    /// <summary><c>ProPresenter</c> in Documents.</summary>
    Documents,

    /// <summary>A folder the person chose.</summary>
    Chosen,
}

/// <summary>
/// A place where songs are kept as files: a folder with <c>Libraries</c> in it, or a
/// folder chosen by hand.
/// </summary>
/// <param name="Name">What to call it: the folder's own name, or "Documents".</param>
public sealed record Place(PlaceKind Kind, string Name, string Folder)
{
    /// <summary>A place is known by where it is.</summary>
    public string Id => Paths.IdOf(Folder);
}

/// <summary>
/// Paths as they are told apart here. Windows takes <c>C:\Songs</c> and <c>c:\songs\</c>
/// for one folder, and so does this.
/// </summary>
public static class Paths
{
    /// <summary>A file or folder's whole path, with no separator after it.</summary>
    public static string IdOf(string path)
    {
        try
        {
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException or IOException)
        {
            // (What is no path at all is known by itself.)
            return path;
        }
    }

    /// <summary>Whether two ids are of one file or folder.</summary>
    public static bool Same(string? first, string? second) => string.Equals(first, second, StringComparison.OrdinalIgnoreCase);

    /// <summary>How ids are told apart, for a table kept by id.</summary>
    public static StringComparer ById => StringComparer.OrdinalIgnoreCase;

    /// <summary>The last part of a path: a folder's own name. The whole path, for a drive.</summary>
    public static string NameOf(string folder)
    {
        var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(folder));
        return name.Length > 0 ? name : folder;
    }

    internal static bool IsFolder(string path) => Directory.Exists(path);
}

public sealed class PlaceFinder
{
    private readonly Func<string?> pathSettings;
    private readonly string? documents;
    private readonly IReadOnlyList<string> chosen;

    /// <param name="pathSettings">Reads ProPresenter's own file of paths, whole; null if there is no such file. Nothing is ever written.</param>
    /// <param name="documents">The person's Documents folder, as the system says it is; null if it says none.</param>
    /// <param name="chosen">Folders the person has chosen by hand, in the order they chose them.</param>
    public PlaceFinder(Func<string?> pathSettings, string? documents, IReadOnlyList<string>? chosen = null)
    {
        this.pathSettings = pathSettings;
        this.documents = documents;
        this.chosen = chosen ?? [];
    }

    /// <summary>Where ProPresenter keeps its own settings on a PC, under the person's roaming data.</summary>
    public static string ProPresentersSettingsFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RenewedVision", "ProPresenter");

    /// <summary>The finder for this PC.</summary>
    public static PlaceFinder OnThisPC(IReadOnlyList<string>? chosen = null)
    {
        // (Asked of the system, not built: Documents is often inside OneDrive.)
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return new PlaceFinder(
            () =>
            {
                try
                {
                    // (In what encoding ProPresenter writes it is A GUESS: this takes
                    // UTF-8, or whatever the file's first bytes say it is.)
                    return File.ReadAllText(Path.Combine(ProPresentersSettingsFolder, "PathSettings.proPaths"));
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
                {
                    return null;
                }
            },
            documents.Length > 0 ? documents : null,
            chosen);
    }

    /// <summary>
    /// Every place there is, each once: the folder ProPresenter's file of paths names,
    /// Documents, then what was chosen. A folder that is not there is left out.
    /// </summary>
    public IReadOnlyList<Place> Places()
    {
        var found = new List<Place>();
        void Add(Place place)
        {
            if (Paths.IsFolder(place.Folder) && !found.Any(other => Paths.Same(other.Id, place.Id)))
            {
                found.Add(place);
            }
        }

        // (A folder the file names that has no `Libraries` in it is not ProPresenter's
        // folder, whatever the file says: it is no place.)
        if (BaseNamedIn(pathSettings()) is string named && Paths.IsFolder(Path.Combine(named, "Libraries")))
        {
            Add(new Place(PlaceKind.NamedInSettings, Paths.NameOf(named), named));
        }
        if (documents is not null)
        {
            Add(new Place(PlaceKind.Documents, "Documents", Path.Combine(documents, "ProPresenter")));
        }
        foreach (var folder in chosen)
        {
            Add(new Place(PlaceKind.Chosen, Paths.NameOf(folder), folder));
        }
        return found;
    }

    /// <summary>
    /// The folder a file of paths names as ProPresenter's: what stands between
    /// <c>Base=</c> and the last <c>;</c> of its line, with each two backslashes read as
    /// one. Null if the file is not there, is empty, or names none.
    ///
    /// (ProPresenter 22 leaves the file there with nothing in it: seen on the
    /// maintainer's PC, 2026-10-12.)
    /// </summary>
    internal static string? BaseNamedIn(string? pathSettings)
    {
        if (pathSettings is null)
        {
            return null;
        }
        const string key = "Base=";
        var from = 0;
        while (true)
        {
            var at = pathSettings.IndexOf(key, from, StringComparison.Ordinal);
            if (at < 0)
            {
                return null;
            }
            var start = at + key.Length;
            var end = pathSettings.IndexOfAny(['\r', '\n'], start);
            var line = end < 0 ? pathSettings[start..] : pathSettings[start..end];
            var last = line.LastIndexOf(';');
            if (last > 0)
            {
                var twice = new string((char)0x5C, 2);
                var once = new string((char)0x5C, 1);
                var folder = line[..last].Replace(twice, once, StringComparison.Ordinal).Trim();
                if (folder.Length > 0)
                {
                    return folder;
                }
            }
            from = start;
        }
    }
}
