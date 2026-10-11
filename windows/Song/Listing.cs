// The lists: a place's libraries, and a library's songs. spec/06-library.md, rules 5, 7
// and 8.
//
// Inside a place, `Libraries` has one folder for each library, named as ProPresenter
// shows it, and each of those has the library's presentations as `.pro` files directly
// in it. A folder chosen by hand need not be laid out so, and is taken for what it has:
// a ProPresenter folder, or a `Libraries` folder, or one library of songs.
//
// Only names are read here, and what the file system says of a file. No file is opened:
// that is Marks.cs, afterwards and in the background.
//
// WHAT IS HIDDEN. The page leaves out what is hidden. On Windows that is a file or
// folder the system marks as hidden; and here it is also one whose name begins with a
// full stop, which is how a Mac hides one. A library copied from a Mac, or kept where
// both read it, has such files in it (a Mac writes `._Name.pro` beside `Name.pro` on a
// disk that is not its own kind), and none of them is a song. So one folder lists the
// same in both apps. NEW, and this session's choice: the page does not say what hidden
// means on Windows.
//
// The Mac's is Listing.swift.

using System.Globalization;
using System.Runtime.InteropServices;

namespace Pro7ChordEditor.Song;

/// <summary>A library: a folder of songs.</summary>
public sealed record Library(string Name, string Folder)
{
    public string Id => Paths.IdOf(Folder);
}

/// <summary>
/// A song's file, as it is listed: its name without the ending, where it is, and its
/// size and time of last change, by which what was worked out of it before is known to
/// be still true.
/// </summary>
public sealed record ListedSong(string Name, string Path, long Size, DateTime Modified)
{
    public string Id => Paths.IdOf(Path);
}

public static class Listing
{
    private const string Ending = ".pro";

    /// <summary>
    /// Sorted as the system sorts names: "Song 2" before "Song 10", and no regard to
    /// capitals. On Windows that is the order File Explorer lists files in, and the
    /// system has a function for it, which is asked. (So no rule of this app's says
    /// what comes before what.)
    /// </summary>
    internal static List<T> Sorted<T>(IEnumerable<T> items, Func<T, string> name) =>
        [.. items.OrderBy(name, Comparer<string>.Create(SystemOrder.Compare))];

    private static bool IsHidden(FileSystemInfo entry) =>
        entry.Attributes.HasFlag(FileAttributes.Hidden) || entry.Name.StartsWith('.');

    // What is in a folder and is not hidden. A folder that is not there, or may not be
    // read, has nothing in it.
    private static List<FileSystemInfo> Contents(string folder)
    {
        try
        {
            return [.. new DirectoryInfo(folder).EnumerateFileSystemInfos().Where(entry => !IsHidden(entry))];
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Security.SecurityException)
        {
            return [];
        }
    }

    private static IEnumerable<DirectoryInfo> Folders(string folder) => Contents(folder).OfType<DirectoryInfo>();

    // (A folder named `Something.pro` is no song. And the ending is told by itself:
    // asking the system for `*.pro` also gives `.pro6` and `.proBundle`.)
    private static bool IsSong(FileSystemInfo entry) =>
        entry is FileInfo && string.Equals(entry.Extension, Ending, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The libraries of a place, by name.
    ///
    /// A place with <c>Libraries</c> in it has the folders of that, whether or not they
    /// have songs in them yet. One without (a folder chosen by hand) is itself a library
    /// if it has songs directly in it, and otherwise has for libraries those of its
    /// folders that have songs.
    /// </summary>
    public static IReadOnlyList<Library> Libraries(Place place)
    {
        var inside = Path.Combine(place.Folder, "Libraries");
        if (Paths.IsFolder(inside))
        {
            return Sorted(Folders(inside).Select(folder => new Library(folder.Name, folder.FullName)), library => library.Name);
        }
        if (Contents(place.Folder).Any(IsSong))
        {
            return [new Library(Paths.NameOf(place.Folder), place.Folder)];
        }
        return Sorted(
            Folders(place.Folder).Where(folder => Contents(folder.FullName).Any(IsSong)).Select(folder => new Library(folder.Name, folder.FullName)),
            library => library.Name);
    }

    /// <summary>The songs of a library: its <c>.pro</c> files, by name.</summary>
    public static IReadOnlyList<ListedSong> Songs(Library library) =>
        Sorted(
            Contents(library.Folder).Where(IsSong).Cast<FileInfo>().Select(file =>
                new ListedSong(System.IO.Path.GetFileNameWithoutExtension(file.Name), file.FullName, file.Length, file.LastWriteTimeUtc)),
            song => song.Name);

    /// <summary>
    /// A song's file as it is now: its size and time of last change asked again. Null if
    /// it is no longer there.
    /// </summary>
    public static ListedSong? AsItIsNow(ListedSong song)
    {
        try
        {
            var file = new FileInfo(song.Path);
            return file.Exists ? song with { Size = file.Length, Modified = file.LastWriteTimeUtc } : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>
    /// The songs whose names have what was typed in them, capitals and accents apart.
    /// Nothing typed is every song.
    /// </summary>
    public static IReadOnlyList<ListedSong> Narrowed(IReadOnlyList<ListedSong> songs, string typed)
    {
        var wanted = typed.Trim();
        if (wanted.Length == 0)
        {
            return songs;
        }
        // (By the rules of no one language: the same names are found on every PC.)
        var compare = CultureInfo.InvariantCulture.CompareInfo;
        return [.. songs.Where(song => compare.IndexOf(song.Name, wanted, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0)];
    }
}

/// <summary>The order the system itself lists names in.</summary>
internal static class SystemOrder
{
    // What File Explorer sorts by: numbers in a name by how much they are, and no
    // regard to capitals. It is Windows' own and is asked only of Windows' own copy.
    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int StrCmpLogicalW(string first, string second);

    internal static int Compare(string? first, string? second) => StrCmpLogicalW(first ?? "", second ?? "");
}
