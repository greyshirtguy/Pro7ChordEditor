// Reading a song: what the editors are handed, held to what Simple Presenter's own
// editor is handed for every test file
// (testdata/files/expected/as-read-by-simple-presenter.json, "the reference for reading
// a file"). spec/05-song-and-saving.md, "Proofs to write", the first.
//
// And, mine and not from the test data, the rules of reading that no test file would
// notice being left out (the Mac's session found the same of its own port): in every
// test file the cues are already in the order their groups name them, and a box's
// chords already in order of place. So songs are made here, from Plain Song, in the
// workbench, that have them otherwise; and a cue named twice, a cue in no group, one
// switched off, and a text box that shows something else's words.

using System.Text.Json;
using Google.Protobuf;
using Pro.SerializationInterop.RVProtoData;
using Pro7ChordEditor.ChordRules;
using Pro7ChordEditor.Tests;
using Xunit;

namespace Pro7ChordEditor.Song.Tests;

public sealed class ReadingASongTests : IDisposable
{
    private readonly Workbench bench = new();

    public void Dispose() => bench.Dispose();

    [Fact]
    public void EveryTestFileIsReadAsSimplePresenterReadsIt()
    {
        using var expected = JsonDocument.Parse(TestData.InFiles("expected/as-read-by-simple-presenter.json"));
        var wrong = new List<string>();
        var files = 0;
        foreach (var file in expected.RootElement.GetProperty("files").EnumerateArray())
        {
            var name = file.GetProperty("file").GetString()!;
            var song = new OpenSong(Path.Combine(TestData.Root, "files", TestData.PresentationPaths().Single(path => path.EndsWith("/" + name, StringComparison.Ordinal))));
            files++;

            if (!string.Equals(song.Key, file.GetProperty("key").GetString(), StringComparison.Ordinal))
            {
                wrong.Add($"{name}: its key is read as \"{song.Key}\"");
            }
            var boxes = file.GetProperty("textBoxes").EnumerateArray().Select(box => Written(box)).ToList();
            var got = song.TextBoxes.Select(box => Show(box.Slide, box.Group, box.StartsItsGroup, box.Element, box.Words, box.Chords, box.Blank)).ToList();
            if (got.Count != boxes.Count)
            {
                wrong.Add($"{name}: {got.Count} text boxes are handed over, and {boxes.Count} are expected");
            }
            for (var n = 0; n < Math.Min(got.Count, boxes.Count); n++)
            {
                if (!string.Equals(got[n], boxes[n], StringComparison.Ordinal))
                {
                    wrong.Add($"{name}, text box {n}:\n      got      {got[n]}\n      expected {boxes[n]}");
                }
            }
        }
        Assert.True(files >= 11, "fewer test files than there were are described");
        Assert.True(wrong.Count == 0, $"{wrong.Count} things are not read as expected. The first:\n  " + string.Join("\n  ", wrong.Take(5)));

        static string Written(JsonElement box) => Show(
            box.GetProperty("slide").GetInt32(),
            box.GetProperty("group").GetString()!,
            box.GetProperty("startsItsGroup").GetBoolean(),
            box.GetProperty("element").GetString()!,
            box.GetProperty("words").GetString()!,
            box.GetProperty("chords").EnumerateArray().Select(chord => new Chord(chord.GetProperty("at").GetInt32(), chord.GetProperty("name").GetString()!)),
            box.GetProperty("blank").GetBoolean());
    }

    // The one test file that Simple Presenter has not read (it was made after, on the
    // Mac): held to what tools/look.py says is in it, which is every text box that has
    // RTF, in the file's own order. So the two are set side by side without regard to
    // order: every box this app shows for its words or its chords is one of those, with
    // those words and those chords, and none of those that has anything to show is
    // missed.
    [Fact]
    public void TheSongInTheShapesOfRealOnesIsReadAsItsDescriptionSays()
    {
        using var expected = JsonDocument.Parse(TestData.InFiles("expected/Chords With No Words.json"));
        var described = expected.RootElement.GetProperty("textBoxes").EnumerateArray()
            .Select(box => (
                Words: box.GetProperty("words").GetString()!,
                Chords: box.GetProperty("chords").EnumerateArray().Select(chord => new Chord(chord.GetProperty("start").GetInt32(), chord.GetProperty("name").GetString()!)).ToList()))
            .Where(box => !Words.IsBlank(box.Words) || box.Chords.Count > 0 || Words.HasAStandIn(box.Words))
            .Select(box => Show(0, "", false, "", box.Words, box.Chords, false))
            .Order(StringComparer.Ordinal)
            .ToList();

        var song = bench.Open("Chords With No Words.pro");
        var shown = song.TextBoxes.Where(box => !box.Blank)
            .Select(box => Show(0, "", false, "", box.Words, box.Chords, false))
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Equal(described, shown);
        // Twelve slides, and three of them blank: a text box with no text, one that
        // holds one plain space, and a slide with nothing on it, which has no id to give.
        Assert.Equal(12, song.SlideCount);
        var blank = song.TextBoxes.Where(box => box.Blank).ToList();
        Assert.Equal([0, 10, 11], blank.Select(box => box.Slide));
        Assert.Equal("", blank[2].Element);
        Assert.NotEqual("", blank[0].Element);
        Assert.All(blank, box => Assert.Equal("", box.Words));
        Assert.Equal("", song.Key);
    }

    [Fact]
    public void WhatASongIsCalledAndItsChordsInSongOrder()
    {
        var song = bench.Open("Plain Song.pro");

        Assert.Equal("Plain Song", song.Name);
        Assert.Equal(Workbench.Read(song.Path).Uuid.String, song.Id);
        Assert.Equal(song.TextBoxes.SelectMany(box => box.Chords).Select(chord => chord.Name), song.UsedChords);
        Assert.NotEmpty(song.UsedChords);
        Assert.Equal(song.TextBoxes.Select(box => box.Slide).Distinct().Count(), song.SlideCount);
    }

    // ---- Mine: what no test file would notice being left out

    // The order of the slides is the order of the groups, and inside each group the
    // order of its cue identifiers, whatever order the cues themselves are in.
    [Fact]
    public void TheSlidesAreInTheOrderTheirGroupsNameThem()
    {
        var path = bench.CopyOf("Plain Song.pro");
        var asItWas = new OpenSong(path).TextBoxes.Select(box => box.Words).ToList();
        var presentation = Workbench.Read(path);
        Assert.True(presentation.Cues.Count >= 3);

        // The cues turned end for end in the file. The groups still name them as before.
        var turned = presentation.Cues.Reverse().ToList();
        presentation.Cues.Clear();
        presentation.Cues.AddRange(turned);
        var song = new OpenSong(bench.FileOf("turned.pro", presentation.ToByteArray()));

        Assert.Equal(asItWas, song.TextBoxes.Select(box => box.Words));
    }

    [Fact]
    public void ACueNamedTwiceIsTakenOnceAndOneInNoGroupComesAfterTheRest()
    {
        var path = bench.CopyOf("Plain Song.pro");
        var asItWas = new OpenSong(path).TextBoxes.Select(box => box.Words).ToList();
        var presentation = Workbench.Read(path);
        var first = presentation.CueGroups[0];
        var last = presentation.CueGroups[^1];
        var firstCue = first.CueIdentifiers[0].Clone();

        // The first group's first cue is named again by the last group, and an id that
        // is no cue's is named too: the cue is taken the first time, and the stranger
        // passed over.
        last.CueIdentifiers.Add(firstCue);
        last.CueIdentifiers.Add(new UUID { String = "00000000-0000-0000-0000-000000000000" });
        Assert.Equal(asItWas, new OpenSong(bench.FileOf("twice.pro", presentation.ToByteArray())).TextBoxes.Select(box => box.Words));

        // And named by no group at all, it comes after every slide that a group names,
        // in no group.
        last.CueIdentifiers.RemoveAt(last.CueIdentifiers.Count - 1);
        last.CueIdentifiers.RemoveAt(last.CueIdentifiers.Count - 1);
        first.CueIdentifiers.RemoveAt(0);
        var song = new OpenSong(bench.FileOf("in-no-group.pro", presentation.ToByteArray()));
        var boxes = song.TextBoxes;

        Assert.Equal(asItWas[0], boxes[^1].Words);
        Assert.Equal("", boxes[^1].Group);
        Assert.False(boxes[^1].StartsItsGroup);
        Assert.Equal(song.SlideCount - 1, boxes[^1].Slide);
    }

    [Fact]
    public void ACueThatIsSwitchedOffOrHasNoSlideSwitchedOnIsLeftOut()
    {
        var path = bench.CopyOf("Plain Song.pro");
        var asItWas = new OpenSong(path).TextBoxes.ToList();
        var whole = Workbench.Read(path);
        var firstId = asItWas[0].Element;

        // The cue of the first slide, switched off.
        var off = whole.Clone();
        off.Cues.Single(cue => HasElement(cue, firstId)).IsEnabled = false;
        var song = new OpenSong(bench.FileOf("cue-off.pro", off.ToByteArray()));
        Assert.Equal(asItWas.Skip(1).Select(box => box.Words), song.TextBoxes.Select(box => box.Words));
        Assert.Equal(0, song.TextBoxes[0].Slide);

        // The same cue, with its slide's action switched off instead.
        var actionOff = whole.Clone();
        foreach (var action in actionOff.Cues.Single(cue => HasElement(cue, firstId)).Actions)
        {
            action.IsEnabled = false;
        }
        Assert.Equal(
            asItWas.Skip(1).Select(box => box.Words),
            new OpenSong(bench.FileOf("action-off.pro", actionOff.ToByteArray())).TextBoxes.Select(box => box.Words));

        static bool HasElement(Cue cue, string id) =>
            cue.Actions.Any(action => (action.Slide?.Presentation?.BaseSlide?.Elements ?? []).Any(element => element.Element_?.Uuid?.String == id));
    }

    // A box's chords are read in order of place, the file's order kept among those in
    // one place; and an entry among them that is not a chord is not one.
    [Fact]
    public void AChordsOrderIsItsPlaceWhateverOrderTheFileHasThemIn()
    {
        var path = bench.CopyOf("Plain Song.pro");
        var box = new OpenSong(path).TextBoxes.First(one => one.Chords.Count >= 2);
        var presentation = Workbench.Read(path);
        var entries = Workbench.TextOf(presentation, box.Element).Attributes.CustomAttributes;

        // The chord entries turned end for end, an entry that is not a chord put among
        // them, and a second chord put in the first one's place, after it in the file.
        var chords = entries.Where(entry => entry.AttributeCase == Graphics.Types.Text.Types.Attributes.Types.CustomAttribute.AttributeOneofCase.Chord).Reverse().ToList();
        var others = entries.Where(entry => !chords.Contains(entry)).ToList();
        entries.Clear();
        entries.AddRange(others);
        entries.Add(chords[0]);
        entries.Add(new Graphics.Types.Text.Types.Attributes.Types.CustomAttribute { Range = new IntRange { Start = 0, End = 3 }, OriginalFontSize = 60 });
        entries.AddRange(chords.Skip(1));
        entries.Add(new Graphics.Types.Text.Types.Attributes.Types.CustomAttribute { Range = new IntRange { Start = box.Chords[0].At, End = box.Chords[0].At + 1 }, Chord = "Second" });
        var read = new OpenSong(bench.FileOf("out-of-order.pro", presentation.ToByteArray())).TextBoxes.Single(one => one.Element == box.Element);

        var expected = box.Chords.ToList();
        expected.Insert(1, new Chord(box.Chords[0].At, "Second"));
        Assert.Equal(expected, read.Chords);
    }

    // A box that shows something else's words (a clock, another slide's text) is not
    // the song's words. One whose only link says when it is to be seen keeps its own.
    [Fact]
    public void ABoxWithALinkShowsSomethingElseUnlessTheLinkOnlySaysWhenItIsSeen()
    {
        var path = bench.CopyOf("Plain Song.pro");
        var asItWas = new OpenSong(path).TextBoxes.ToList();
        var first = asItWas[0];

        var seen = Workbench.Read(path);
        ElementOf(seen, first.Element).DataLinks.Add(new Slide.Types.Element.Types.DataLink { VisibilityLink = new Slide.Types.Element.Types.DataLink.Types.VisibilityLink() });
        Assert.Equal(asItWas.Select(box => box.Words), new OpenSong(bench.FileOf("seen-when.pro", seen.ToByteArray())).TextBoxes.Select(box => box.Words));

        // With a link that puts something else in the box, the slide has no words of its
        // own and no text box to put chords in: it is a blank slide with no box.
        var linked = Workbench.Read(path);
        ElementOf(linked, first.Element).DataLinks.Add(new Slide.Types.Element.Types.DataLink { Ticker = new Slide.Types.Element.Types.DataLink.Types.Ticker() });
        var boxes = new OpenSong(bench.FileOf("linked.pro", linked.ToByteArray())).TextBoxes;
        Assert.True(boxes[0].Blank);
        Assert.Equal("", boxes[0].Element);
        Assert.Equal(asItWas.Skip(1).Select(box => box.Words), boxes.Skip(1).Select(box => box.Words));

        static Slide.Types.Element ElementOf(Presentation presentation, string id) =>
            (from cue in presentation.Cues
             from action in cue.Actions
             from element in action.Slide?.Presentation?.BaseSlide?.Elements ?? []
             where element.Element_?.Uuid?.String == id
             select element).Single();
    }

    // A group's colour is handed over if the file gives it one that can be seen.
    [Fact]
    public void AGroupsOwnColourIsHandedOver()
    {
        var withColours = new OpenSong(bench.CopyOf("Rock Of Ages (Toplady).pro")).TextBoxes;
        Assert.All(withColours, box => Assert.NotNull(box.GroupColour));

        var path = bench.CopyOf("Plain Song.pro");
        var presentation = Workbench.Read(path);
        presentation.CueGroups[0].Group.Color = new Color { Red = 0.25f, Green = 0.5f, Blue = 0.75f, Alpha = 1 };
        var boxes = new OpenSong(bench.FileOf("coloured.pro", presentation.ToByteArray())).TextBoxes;
        Assert.Equal(new Colour(0.25, 0.5, 0.75, 1), boxes[0].GroupColour);

        // One that cannot be seen is none.
        presentation.CueGroups[0].Group.Color.Alpha = 0;
        Assert.Null(new OpenSong(bench.FileOf("see-through.pro", presentation.ToByteArray())).TextBoxes[0].GroupColour);
    }

    // One text box as a line of text, such that two are written alike only if every
    // part of them is the same; with what cannot be seen written as its number.
    private static string Show(int slide, string group, bool starts, string element, string words, IEnumerable<Chord> chords, bool blank)
    {
        static string Plain(string text) => string.Concat(text.Select(c => c == '\n' ? "\\n" : c < ' ' || c > '~' ? "{" + ((int)c).ToString("X4", System.Globalization.CultureInfo.InvariantCulture) + "}" : c.ToString()));
        return $"slide {slide} [{Plain(group)}]{(starts ? " starts" : "")} {element}{(blank ? " BLANK" : "")} \"{Plain(words)}\" " +
            string.Join(" ", chords.Select(chord => $"{chord.At}={Plain(chord.Name)}"));
    }
}
