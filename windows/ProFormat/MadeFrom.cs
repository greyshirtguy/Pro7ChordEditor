// ProFormat: the reader and writer of ProPresenter's .pro files.
//
// This is the only file of the project written by hand. Everything else is made at
// each build from the descriptions of the format in the proto/ submodule
// (ProFormat.csproj says how, and which files), and is in the namespace those
// descriptions name for C#: Pro.SerializationInterop.RVProtoData. Do not write a
// reader by hand (spec/10-platforms.md).
//
// A presentation is `Presentation`. To read one and write it back:
//
//     Presentation presentation = Presentation.Parser.ParseFrom(bytes);
//     byte[] bytes = presentation.ToByteArray();
//
// What makes writing a file back safe is that Google.Protobuf keeps what it does not
// understand: a field that is not in the descriptions is held in the message and
// written out again. That is on unless it is asked for otherwise
// (`Parser.WithDiscardUnknownFields(true)`), and it must never be asked for
// otherwise: it is how a file from a ProPresenter newer than these descriptions comes
// through a change of chords with everything else as it came (AGENTS.md, the first
// thing that must stay true). Tests/Song.Tests holds it.
//
// "As it came" is said of the message, not of the bytes. A file read and written back
// with nothing changed says exactly what it said, to ProPresenter or to anything else
// that reads protocol buffers; its bytes could be in another order, or leave out a
// number that was written as nought, which reads as nought. The maintainer's word on
// 2026-10-10 was that this is what the rule means: ProPresenter's reading of the file
// is what is not to be disturbed (mac/NOTES.md, decision 2). In fact every test file
// comes back the same to the byte, and Tests/Song.Tests holds that too, so that a file
// that one day does not is noticed and looked into.
//
// One made file is not in that namespace. customOptions.proto names none for C#, so
// its one empty class is in Rv.Data, after the format's own name. That was the
// namespace of the whole of the first editor's reader (windows/old), which this
// project must not be taken for; nothing here uses the class.
//
// The rules of chords do not belong here, nor does anything about what this app does
// with a file: that is Song, which is built on this.

using System.Reflection;
using Google.Protobuf.Reflection;
using Pro.SerializationInterop.RVProtoData;

namespace Pro7ChordEditor.ProFormat;

/// <summary>What the reader of ProPresenter's files was made from, at this build.</summary>
public static class MadeFrom
{
    /// <summary>
    /// The version of ProPresenter the descriptions were taken from, as
    /// proto/autogen-proto/version.txt had it when this was built: "21.4,352583705",
    /// the version and ProPresenter's own number for that build of it. Empty if the
    /// build found no such file.
    /// </summary>
    public static string ProPresenterVersion { get; } =
        typeof(MadeFrom).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(said => string.Equals(said.Key, "ProPresenterVersion", StringComparison.Ordinal))
            ?.Value ?? "";

    /// <summary>
    /// The files of the descriptions that a presentation takes in, by name
    /// ("presentation.proto"), in order: presentation.proto and every file it imports,
    /// directly or through another. Google's own are left out: the library has those
    /// already. This is protobuf's own account of them, read from the made code, and
    /// not the list in ProFormat.csproj, which a test holds to it.
    /// </summary>
    public static IReadOnlyList<string> Files { get; } = FilesOf(PresentationReflection.Descriptor);

    private static List<string> FilesOf(FileDescriptor root)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var still = new Stack<FileDescriptor>();
        still.Push(root);
        while (still.Count > 0)
        {
            var file = still.Pop();
            if (!seen.Add(file.Name))
            {
                continue;
            }
            foreach (var imported in file.Dependencies)
            {
                still.Push(imported);
            }
        }
        var names = seen.Where(name => !name.StartsWith("google/protobuf/", StringComparison.Ordinal)).ToList();
        names.Sort(StringComparer.Ordinal);
        return names;
    }
}
