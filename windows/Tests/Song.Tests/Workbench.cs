// Where the tests of the song work: a folder of its own under the system's temporary
// folder, with copies of the test files in it, and safeguards that a test can set.
//
// Nothing is ever saved into testdata/, and no test goes near a real library (AGENTS.md,
// "The person's files"). A test that changes a song asks the workbench for a copy of a
// test file and changes the copy. The folder is taken away when the test is done.
//
// The safeguards are made-up ones: whether "ProPresenter is running" is whatever the
// test says, and "keeping a copy" only notes that one was asked for, with the file's
// bytes as they were then, or fails if the test says it is to. The real ones are
// milestones M3 and M4.
//
// The Mac's is Workbench.swift.

using Pro.SerializationInterop.RVProtoData;
using Pro7ChordEditor.Tests;

namespace Pro7ChordEditor.Song.Tests;

internal sealed class Workbench : IDisposable
{
    private const string Prefix = "Pro7ChordEditorTests-";
    private readonly DirectoryInfo folder = Directory.CreateTempSubdirectory(Prefix);

    /// <summary>The copy that was asked for before a change: of which file, why, and its bytes then.</summary>
    internal sealed record Copy(string Path, CopyReason Why, byte[] Bytes);

    /// <summary>What a save is told when it asks whether ProPresenter is running.</summary>
    internal bool ProPresenterIsRunning { get; set; }

    /// <summary>If not null, keeping a copy fails, saying this.</summary>
    internal string? KeepingACopyFails { get; set; }

    /// <summary>Every copy that was asked for, in order.</summary>
    internal List<Copy> Copies { get; } = [];

    internal string Folder => folder.FullName;

    internal Safeguards Safeguards => new(
        () => ProPresenterIsRunning,
        (path, why) =>
        {
            if (KeepingACopyFails is not null)
            {
                throw new IOException(KeepingACopyFails);
            }
            Copies.Add(new Copy(path, why, File.ReadAllBytes(path)));
        });

    /// <summary>
    /// A copy of a test file in the workbench, by its name alone ("Amazing Grace.pro") or
    /// its path from testdata/files, and where the copy is.
    /// </summary>
    internal string CopyOf(string testFile)
    {
        var from = TestData.PresentationPaths().Single(path =>
            string.Equals(path, testFile, StringComparison.Ordinal) || path.EndsWith("/" + testFile, StringComparison.Ordinal));
        var to = Path.Combine(Folder, Path.GetFileName(from));
        File.WriteAllBytes(to, TestData.InFiles(from));
        return to;
    }

    /// <summary>A song made by a test: these bytes as a file in the workbench.</summary>
    internal string FileOf(string name, byte[] bytes)
    {
        var to = Path.Combine(Folder, name);
        File.WriteAllBytes(to, bytes);
        return to;
    }

    /// <summary>A copy of a test file, opened so that it can be changed.</summary>
    internal OpenSong Open(string testFile) => new(CopyOf(testFile), Safeguards);

    /// <summary>A file of the workbench, opened so that it can be changed.</summary>
    internal OpenSong OpenAt(string path) => new(path, Safeguards);

    /// <summary>A file as the reader of the format reads it.</summary>
    internal static Presentation Read(string path) => Presentation.Parser.ParseFrom(File.ReadAllBytes(path));

    /// <summary>
    /// The text boxes of a presentation that have this id, wherever they are. (In the
    /// test files an id is one box's. In a real song it may not be: TextBox.cs.)
    /// </summary>
    internal static List<Graphics.Types.Text> TextsOf(Presentation presentation, string element) =>
        (from cue in presentation.Cues
         from action in cue.Actions
         from one in action.Slide?.Presentation?.BaseSlide?.Elements ?? []
         where one.Element_?.Text is not null && string.Equals(one.Element_.Uuid?.String, element, StringComparison.Ordinal)
         select one.Element_.Text).ToList();

    internal static Graphics.Types.Text TextOf(Presentation presentation, string element) => TextsOf(presentation, element).Single();

    /// <summary>Takes the workbench's folder away, and everything in it.</summary>
    public void Dispose()
    {
        // (It is a folder this made, under the system's temporary folder, with this
        // name: looked at before it is deleted, as AGENTS.md asks of anything deleted.)
        if (folder.Exists && folder.Name.StartsWith(Prefix, StringComparison.Ordinal)
            && folder.FullName.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase))
        {
            foreach (var file in folder.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                // (A test may have left a file that may not be changed.)
                file.Attributes = FileAttributes.Normal;
            }
            folder.Delete(recursive: true);
        }
    }
}
