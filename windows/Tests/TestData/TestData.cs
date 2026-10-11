// Where the tests find testdata/, the folder at the top of the repository that both
// apps' tests are held to (testdata/README.md).
//
// It is found by walking up from where the tests were built, not copied beside them:
// a copy would be a second thing to keep true. So the tests must be run from the
// repository they were built in, which is how `dotnet test` and Visual Studio both
// run them.
//
// The files are found by looking, never by a list of names kept here: when the test
// data is made again with a file more, the tests take it in without a line changing
// (spec/02-chord-rules.md, "The vectors").
//
// Nothing here writes. A test that changes a file works on a copy in a temporary
// folder, never on testdata/ itself and never on anyone's library (AGENTS.md, "The
// person's files").

namespace Pro7ChordEditor.Tests;

public static class TestData
{
    /// <summary>The repository's own folder: the one that has <c>testdata</c> in it.</summary>
    public static string Repository { get; } = FindRepository();

    /// <summary>The repository's <c>testdata</c> folder.</summary>
    public static string Root { get; } = Path.Combine(Repository, "testdata");

    /// <summary>A file of <c>testdata/vectors</c>, by its name: "keys.json".</summary>
    public static byte[] Vector(string name) => File.ReadAllBytes(Path.Combine(Root, "vectors", name));

    /// <summary>The names of every file of <c>testdata/vectors</c>, in order.</summary>
    public static IReadOnlyList<string> VectorNames() =>
        InOrder(Directory.EnumerateFiles(Path.Combine(Root, "vectors"), "*.json").Select(path => Path.GetFileName(path)));

    /// <summary>
    /// Every presentation of <c>testdata/files</c>, as its path from there with
    /// forward slashes ("made-by-simple-presenter/Amazing Grace.pro"), in order.
    /// </summary>
    public static IReadOnlyList<string> PresentationPaths()
    {
        var files = Path.Combine(Root, "files");
        return InOrder(Directory.EnumerateFiles(files, "*.pro", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(files, path).Replace('\\', '/')));
    }

    /// <summary>
    /// A file of <c>testdata/files</c>, by its path from there with forward slashes:
    /// "expected/Amazing Grace.json".
    /// </summary>
    public static byte[] InFiles(string path) => File.ReadAllBytes(Path.Combine(Root, "files", path.Replace('/', Path.DirectorySeparatorChar)));

    // Unit by unit, so that the order is the same on every computer.
    private static List<string> InOrder(IEnumerable<string> names)
    {
        var list = names.ToList();
        list.Sort(StringComparer.Ordinal);
        return list;
    }

    private static string FindRepository()
    {
        var started = AppContext.BaseDirectory;
        for (var folder = new DirectoryInfo(started); folder is not null; folder = folder.Parent)
        {
            if (Directory.Exists(Path.Combine(folder.FullName, "testdata", "vectors")))
            {
                return folder.FullName;
            }
        }
        throw new DirectoryNotFoundException(
            $"testdata/vectors was not found in any folder above {started}: run the tests from a clone of the repository.");
    }
}
