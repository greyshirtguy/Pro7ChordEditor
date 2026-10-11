// What the tests of the window stand on: a made-up PC, in a folder of its own under the
// system's temporary folder. It has a Documents folder with a ProPresenter folder in
// it, laid out as ProPresenter lays one out, with copies of the test files for songs; a
// folder for the app's own things; and a ProPresenter that is running or not as the
// test says.
//
// No test of the window asks this PC's ProPresenter anything, reads a real library, or
// writes outside its own folder (AGENTS.md, "The person's files").
//
// The Mac's is MadeUpMac.swift.

using System.IO;
using Pro7ChordEditor.Song;
using Pro7ChordEditor.Tests;

namespace Pro7ChordEditor.AppViews.Tests;

internal sealed class MadeUpPC : IDisposable
{
    private const string Prefix = "Pro7ChordEditorViewTests-";
    private readonly DirectoryInfo folder = Directory.CreateTempSubdirectory(Prefix);

    internal MadeUpPC()
    {
        Documents = Path.Combine(folder.FullName, "Documents");
        Place = Path.Combine(Documents, "ProPresenter");
        var songs = Directory.CreateDirectory(Path.Combine(Place, "Libraries", "Songs")).FullName;
        var copies = Directory.CreateDirectory(Path.Combine(Place, "Libraries", "Old copies")).FullName;
        Directory.CreateDirectory(Path.Combine(Place, "Libraries", "Kids"));
        foreach (var path in TestData.PresentationPaths())
        {
            File.WriteAllBytes(Path.Combine(path.StartsWith("as-the-old", StringComparison.Ordinal) ? copies : songs, Path.GetFileName(path)), TestData.InFiles(path));
        }
        File.WriteAllText(Path.Combine(songs, "Nonsense.pro"), "This is not a song.");
    }

    /// <summary>The names of the songs of the library "Songs", as they are listed.</summary>
    internal static readonly string[] Songs =
        ["A Song - In A Minor Key", "Amazing Grace", "Blank Slides", "Chords With No Words", "Holy Holy Holy", "It Is Well", "Nonsense", "Plain Song", "Rock Of Ages (Toplady)"];

    /// <summary>Those of them that have chords.</summary>
    internal static readonly string[] SongsWithChords =
        ["A Song - In A Minor Key", "Amazing Grace", "Blank Slides", "Chords With No Words", "Holy Holy Holy", "It Is Well", "Plain Song"];

    internal string Folder => folder.FullName;

    /// <summary>The PC's Documents folder.</summary>
    internal string Documents { get; }

    /// <summary>ProPresenter's folder, in Documents.</summary>
    internal string Place { get; }

    internal bool ProPresenterIsRunning { get; set; }

    /// <summary>How many times ProPresenter was asked after.</summary>
    internal int TimesAsked { get; private set; }

    /// <summary>
    /// If set: ProPresenter is started by someone once it has been asked after this many
    /// times in all, and is found running from the next asking on. For a ProPresenter
    /// that is started while the app is in the middle of something.
    /// </summary>
    internal int? IsStartedOnceAsked { get; set; }

    /// <summary>
    /// Whether the PC has ProPresenter's folder where one is looked for. If not, there
    /// is no place to be found at all, though the folder is still on the disk for a
    /// test to choose by hand.
    /// </summary>
    internal bool HasAProPresenterFolder { get; set; } = true;

    internal AppData AppData => new(Path.Combine(Folder, "Own"));

    /// <summary>A model of the window on this PC. Each is a new start of the app.</summary>
    internal LibraryModel Model() => new(
        AppData,
        chosen => new PlaceFinder(() => null, HasAProPresenterFolder ? Documents : null, chosen),
        () =>
        {
            if (IsStartedOnceAsked is { } times && TimesAsked >= times)
            {
                ProPresenterIsRunning = true;
            }
            TimesAsked++;
            return ProPresenterIsRunning;
        });

    internal string Library(string name) => Paths.IdOf(Path.Combine(Place, "Libraries", name));

    internal string Song(string name, string library = "Songs") => Paths.IdOf(Path.Combine(Place, "Libraries", library, name + ".pro"));

    internal string Log => File.ReadAllText(Path.Combine(AppData.Folder, "log.txt"));

    /// <summary>
    /// Gives Plain Song a history: nine days ago its key was set to E, and two days ago
    /// the three chords of its first slide were taken off, leaving eight of its eleven.
    /// So it has two copies kept: the file as it first was, and as it was in E.
    /// </summary>
    internal void GivePlainSongAHistory()
    {
        Change("Plain Song", 9, song => song.SetKey("E"));
        Change("Plain Song", 2, song => song.SetChords(song.TextBoxes[0].Slide, song.TextBoxes[0].Element, []));
    }

    /// <summary>The songs the old editor's copies, in the library "Old copies", are copies of.</summary>
    internal static readonly string[] OriginalsOfTheOldCopies = ["A Song - In A Minor Key", "Amazing Grace", "It Is Well", "Plain Song"];

    /// <summary>
    /// Puts beside each of the old editor's copies the song it would have been saved
    /// beside: the song the copy was made from, with every chord taken off it (as the
    /// app itself takes one off), or with its chords left on.
    /// </summary>
    internal void GiveTheOldCopiesTheirOriginals(bool withTheirChords = false)
    {
        foreach (var name in OriginalsOfTheOldCopies)
        {
            var path = Path.Combine(Place, "Libraries", "Old copies", name + ".pro");
            File.WriteAllBytes(path, TestData.InFiles("made-by-simple-presenter/" + name + ".pro"));
            if (!withTheirChords)
            {
                var song = new OpenSong(path, new Safeguards(() => false, (_, _) => { }));
                foreach (var box in song.TextBoxes.Where(box => box.Chords.Count > 0).ToList())
                {
                    song.SetChords(box.Slide, box.Element, []);
                }
            }
        }
    }

    /// <summary>The name the app's copies know this PC's one place by: it is the ProPresenter folder in Documents.</summary>
    internal const string PlaceName = "Documents";

    /// <summary>A song's file, as it is now.</summary>
    internal byte[] BytesOf(string name, string library = "Songs") => File.ReadAllBytes(Song(name, library));

    /// <summary>
    /// Changes a song of the library as the app would, some days ago: with a copy kept
    /// first, among the app's own things, dated then.
    /// </summary>
    internal void Change(string name, double daysAgo, Action<OpenSong> what, string library = "Songs")
    {
        var copies = AppData.Copies("test");
        var kept = new KeptSong(PlaceName, library, name);
        var then = DateTime.UtcNow.AddDays(-daysAgo);
        what(new OpenSong(Song(name, library), new Safeguards(() => false, (file, why) => copies.Keep(file, kept, why, then))));
    }

    /// <summary>Takes the PC's folder away, and everything in it.</summary>
    public void Dispose()
    {
        // (Only ever the folder made above, under the system's temporary folder, and
        // only if it is named as one of these: looked at before it is deleted.)
        if (folder.Exists && folder.Name.StartsWith(Prefix, StringComparison.Ordinal)
            && folder.FullName.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase))
        {
            foreach (var file in folder.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
            }
            // A test may end while the marks of the library it last chose are still
            // being worked out, off its thread: a song's file is then being read at the
            // moment the folder is to go, and Windows will not delete a file that is
            // open. (FOUND when the marks came to read more of each file, at M8, with
            // the PC busy: two tests in three hundred failed so, not the same two each
            // time.) The reading is over in a moment, so it is waited for.
            for (var tries = 0; ; tries++)
            {
                try
                {
                    folder.Delete(recursive: true);
                    break;
                }
                catch (IOException) when (tries < 100)
                {
                    Thread.Sleep(50);
                }
            }
        }
    }
}
