// The reader of a .pro file (ProFormat), before anything is built on it.
//
// These are milestone M0's. The song's own proofs (spec/05-song-and-saving.md, "Proofs
// to write") are M2's, and will stand on what is shown here:
//
//   - the reader was made from the descriptions that are in this clone, and from the
//     files a presentation needs and no others;
//   - it reads a real file all the way down: a presentation, its cues, a cue's slide,
//     the slide's text boxes, a box's chords;
//   - a file read and written back with nothing changed is the same file, byte for
//     byte. The page says to try this first. It is not what the rule asks (that is
//     said of the message, not of the bytes: ProFormat/MadeFrom.cs), but while it
//     holds, a change of chords changes the bytes of the chords and no others, and a
//     file that one day comes back otherwise is to be looked into, not shrugged at;
//   - what the descriptions have no name for is kept through a read and a write. That
//     is the first thing that must stay true (AGENTS.md): it is how a file written by
//     a newer ProPresenter than the descriptions comes through with everything as it
//     came.
//
// The files are those of testdata/files, read where they lie and never written.

using System.Text.Json;
using Google.Protobuf;
using Pro.SerializationInterop.RVProtoData;
using Pro7ChordEditor.ProFormat;
using Pro7ChordEditor.Tests;
using Xunit;
using CustomAttribute = Pro.SerializationInterop.RVProtoData.Graphics.Types.Text.Types.Attributes.Types.CustomAttribute;

namespace Pro7ChordEditor.Song.Tests;

public class TheReaderTests
{
    private const string AmazingGrace = "made-by-simple-presenter/Amazing Grace.pro";

    // Every presentation there is, found by looking. If none were found the tests
    // that take this would fail for want of anything to try.
    public static TheoryData<string> EveryPresentation => new(TestData.PresentationPaths());

    [Fact]
    public void TheReaderIsMadeFromTheDescriptionsInThisClone()
    {
        var versionFile = Path.Combine(TestData.Repository, "proto", "autogen-proto", "version.txt");

        Assert.Equal(File.ReadAllText(versionFile).Trim(), MadeFrom.ProPresenterVersion);
        Assert.NotEqual("", MadeFrom.ProPresenterVersion);
    }

    // spec/01-file-format.md, "What to generate code from": presentation.proto and
    // every file it imports, directly or through another, and not the hundred and more
    // others. The build already fails if a needed one is missing from the list in
    // ProFormat.csproj. This is the other half: nothing is made that is not needed.
    [Fact]
    public void TheReaderIsMadeOfTheFilesAPresentationNeedsAndNoOthers()
    {
        // Each made file leaves one class whose name ends "Reflection", which knows
        // the name of the file it was made from.
        var made = typeof(Presentation).Assembly.GetTypes()
            .Where(type => type.IsClass && type.IsAbstract && type.IsSealed && type.Name.EndsWith("Reflection", StringComparison.Ordinal))
            .Select(type => (Google.Protobuf.Reflection.FileDescriptor)type.GetProperty("Descriptor")!.GetValue(null)!)
            .Select(file => file.Name)
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Equal(MadeFrom.Files, made);
        Assert.Contains("presentation.proto", made);
        // Thirty-two at ProPresenter 21.4, as the page says. A newer set of
        // descriptions may rightly have another number: then change it here, and
        // there.
        Assert.Equal(32, made.Count);
    }

    [Fact]
    public void AmazingGraceIsReadDownToItsChords()
    {
        var presentation = Presentation.Parser.ParseFrom(TestData.InFiles(AmazingGrace));
        using var expected = JsonDocument.Parse(TestData.InFiles("expected/Amazing Grace.json"));
        var said = expected.RootElement;

        Assert.Equal(said.GetProperty("nameInside").GetString(), presentation.Name);
        Assert.Equal(said.GetProperty("uuid").GetString(), presentation.Uuid.String);
        Assert.Equal(said.GetProperty("hasMusic").GetBoolean(), presentation.Music is not null);
        Assert.Equal(said.GetProperty("multitracks").GetBoolean(), presentation.MultiTracksLicensing is not null);

        // Every chord of the file, each as where it starts, where its range ends and
        // its name. The expected file has them box by box; in what order the boxes
        // come is M2's to hold, so both are put in one order here and set side by side.
        var expectedChords = InOneOrder(said.GetProperty("textBoxes").EnumerateArray()
            .SelectMany(box => box.GetProperty("chords").EnumerateArray())
            .Select(chord => (chord.GetProperty("start").GetInt32(), chord.GetProperty("end").GetInt32(), chord.GetProperty("name").GetString()!)));
        var chords = InOneOrder(ChordsOf(presentation)
            .Select(chord => (chord.Range?.Start ?? 0, chord.Range?.End ?? 0, chord.Chord)));

        Assert.NotEmpty(expectedChords);
        Assert.Equal(expectedChords, chords);
    }

    // By where they start, then where they end, then by name, unit by unit.
    private static List<(int Start, int End, string Name)> InOneOrder(IEnumerable<(int Start, int End, string Name)> chords) =>
        chords.OrderBy(chord => chord.Start).ThenBy(chord => chord.End).ThenBy(chord => chord.Name, StringComparer.Ordinal).ToList();

    [Theory]
    [MemberData(nameof(EveryPresentation))]
    public void AFileReadAndWrittenBackIsTheSameBytes(string path)
    {
        var bytes = TestData.InFiles(path);

        var written = Presentation.Parser.ParseFrom(bytes).ToByteArray();

        Assert.True(bytes.AsSpan().SequenceEqual(written), WhereTheyDiffer(path, bytes, written));
    }

    // The test above would pass for any file if writing a message gave back the bytes
    // it was read from whatever had been done to it. It does not: one chord renamed,
    // and the bytes are others.
    [Fact]
    public void AFileWithOneChordRenamedIsNotTheSameBytes()
    {
        var bytes = TestData.InFiles(AmazingGrace);
        var presentation = Presentation.Parser.ParseFrom(bytes);

        var chord = ChordsOf(presentation).First();
        chord.Chord += "sus4";
        var written = presentation.ToByteArray();

        Assert.False(bytes.AsSpan().SequenceEqual(written));
        // Longer by the four letters at least. (And by a byte more for each thing the
        // chord is inside whose length, which the file writes before it, came to need
        // another byte to be written in.)
        Assert.True(written.Length >= bytes.Length + "sus4".Length, $"{bytes.Length} bytes read, {written.Length} written");
    }

    // A field with a number the descriptions do not have, as a newer ProPresenter
    // might write one, is put at the end of each file's bytes: number 2047, holding the
    // number 1, which is the three bytes F8 7F 01. The message is read and written, and
    // the three bytes must still be there. (They come out at the end, where what is not
    // understood is written, which is also where they were put: so the whole file is
    // the same.)
    [Theory]
    [MemberData(nameof(EveryPresentation))]
    public void WhatTheDescriptionsHaveNoNameForIsKept(string path)
    {
        byte[] unknown = [0xF8, 0x7F, 0x01];
        Assert.DoesNotContain(Presentation.Descriptor.Fields.InDeclarationOrder(), field => field.FieldNumber == 2047);
        byte[] bytes = [.. TestData.InFiles(path), .. unknown];

        var presentation = Presentation.Parser.ParseFrom(bytes);
        var written = presentation.ToByteArray();

        Assert.True(bytes.AsSpan().SequenceEqual(written), WhereTheyDiffer(path, bytes, written));

        // And it is kept through a change made elsewhere in the message, which is what
        // the app does to a file.
        presentation.Name += " (changed)";
        var changed = presentation.ToByteArray();
        Assert.True(changed.AsSpan().EndsWith(unknown), $"{path}: the field that is not in the descriptions was lost when the name was changed");
    }

    // Every chord of a presentation, wherever it is: the entries of a text box's
    // custom attributes whose `chord` is the field that is set
    // (spec/01-file-format.md, "Chords"). Which boxes hold a song's words, and in what
    // order its slides come, is not asked here.
    private static IEnumerable<CustomAttribute> ChordsOf(Presentation presentation) =>
        from cue in presentation.Cues
        from action in cue.Actions
        from element in action.Slide?.Presentation?.BaseSlide?.Elements ?? []
        from attribute in element.Element_?.Text?.Attributes?.CustomAttributes ?? []
        where attribute.AttributeCase == CustomAttribute.AttributeOneofCase.Chord
        select attribute;

    private static string WhereTheyDiffer(string path, byte[] was, byte[] written)
    {
        var same = was.AsSpan().CommonPrefixLength(written);
        return $"{path}: {was.Length} bytes read, {written.Length} written, the same up to byte {same}";
    }
}
