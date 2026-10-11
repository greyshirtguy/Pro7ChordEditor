// Rules of the song that no test file holds, each held here by a song made for it.
//
// How they were found. With every other test of the song passing, each rule of
// Reading.cs, Writing.cs and OpenSong.cs was broken in one small way in turn (ninety
// ways, in a clone made for it) and the tests run again. Those here were not caught:
// the rule could be got wrong and no test would say. Each song is made from a test file
// with the reader of the format, in the workbench, to have the one thing the test files
// have not.
//
// MINE. What each expects is the page's rule (spec/01-file-format.md and
// spec/05-song-and-saving.md, named at each), or the reference's code where the page is
// silent, and not what this app happens to do.

using Google.Protobuf;
using Pro.SerializationInterop.RVProtoData;
using Pro7ChordEditor.ChordRules;
using Xunit;
using CustomAttribute = Pro.SerializationInterop.RVProtoData.Graphics.Types.Text.Types.Attributes.Types.CustomAttribute;

namespace Pro7ChordEditor.Song.Tests;

public sealed class NotHeldByTheTestFilesTests : IDisposable
{
    private const string Shaped = "Chords With No Words.pro";
    private readonly Workbench bench = new();

    public void Dispose() => bench.Dispose();

    // ---- Reading

    // Two cues with one id (a slide made by duplicating another can keep more than its
    // boxes' ids): the LATER is the one a group's naming of that id finds, as in the
    // reference, which keeps cues in a table by id that the later overwrites. The
    // earlier is then named by no group, and comes after the rest.
    [Fact]
    public void OfTwoCuesWithOneIdAGroupsNamingFindsTheLater()
    {
        var path = bench.CopyOf("Plain Song.pro");
        var asItWas = new OpenSong(path).TextBoxes.ToList();
        var presentation = Workbench.Read(path);
        var firstId = presentation.CueGroups[0].CueIdentifiers[0].String;
        var first = presentation.Cues.Single(cue => cue.Uuid.String == firstId);
        // The last slide's cue, again, under the first slide's id, at the end of the cues.
        var lastId = presentation.CueGroups[^1].CueIdentifiers[^1].String;
        var twin = presentation.Cues.Single(cue => cue.Uuid.String == lastId).Clone();
        twin.Uuid.String = firstId;
        presentation.Cues.Add(twin);
        Assert.True(presentation.Cues.IndexOf(first) < presentation.Cues.Count - 1);

        var boxes = new OpenSong(bench.FileOf("one-id-twice.pro", presentation.ToByteArray())).TextBoxes;

        // Where the first slide was, the twin is; and the first slide's own cue is last,
        // in no group.
        Assert.Equal(asItWas[^1].Words, boxes[0].Words);
        Assert.Equal(asItWas[0].Group, boxes[0].Group);
        Assert.True(boxes[0].StartsItsGroup);
        Assert.Equal(asItWas[0].Words, boxes[^1].Words);
        Assert.Equal("", boxes[^1].Group);
        Assert.Equal(asItWas.Count + 1, boxes.Count);
    }

    // spec/01: "a box whose text is blank is still shown if it has chords, or has a
    // stand-in character in it (U+200B or U+2001)". An em quad is white space, so a box
    // of plain spaces with an em quad between is blank to look at, and has a stand-in.
    // With no chords on it, it is still a line of chords alone, and not a blank slide.
    [Fact]
    public void ABoxOfSpacesWithAnEmQuadAndNoChordsIsALineOfChordsAlone()
    {
        var path = bench.CopyOf("Plain Song.pro");
        var first = new OpenSong(path).TextBoxes[0];
        var presentation = Workbench.Read(path);
        var text = Workbench.TextOf(presentation, first.Element);
        // A space, an em quad, a space.
        text.RtfData = ByteString.CopyFrom(Rtf("{", "rtf1  ", "uc0", "u8193  }"));
        RemoveChords(text);

        var box = new OpenSong(bench.FileOf("spaces-and-a-quad.pro", presentation.ToByteArray())).TextBoxes[0];

        Assert.Equal([' ', Words.EmQuad, ' '], box.Words.ToCharArray());
        Assert.False(box.Blank);
        Assert.Equal(first.Element, box.Element);
        Assert.Empty(box.Chords);
    }

    // ---- Writing a box's chords

    // "A request that would change nothing saves nothing." A text box that has no
    // settings written at all (ProPresenter writes them for every text box seen so far),
    // given no chords, is left as it is: there is nothing to write, and nowhere to
    // write it. Given a chord, it is given settings to hold it.
    [Fact]
    public void ATextBoxWithNoSettingsWrittenIsNotChangedByBeingGivenNoChords()
    {
        var path = bench.CopyOf("Plain Song.pro");
        var first = new OpenSong(path).TextBoxes[0];
        var presentation = Workbench.Read(path);
        Workbench.TextOf(presentation, first.Element).Attributes = null;
        var song = bench.OpenAt(bench.FileOf("no-settings.pro", presentation.ToByteArray()));
        var asItWas = File.ReadAllBytes(song.Path);
        Assert.Empty(song.TextBoxes[0].Chords);

        song.SetChords(first.Slide, first.Element, []);
        Assert.False(song.CanUndo);
        Assert.Equal(asItWas, File.ReadAllBytes(song.Path));
        Assert.Empty(bench.Copies);

        song.SetChords(first.Slide, first.Element, [new Chord(0, "G")]);
        Assert.Equal([new Chord(0, "G")], new OpenSong(song.Path).TextBoxes[0].Chords);
        song.Undo();
        Assert.Equal(asItWas, File.ReadAllBytes(song.Path));
    }

    // spec/05: "If the text box named ... is on the slide and has no text at all:
    // chords are not put on a shape."
    [Fact]
    public void ChordsAreNotPutOnAShape()
    {
        var path = bench.CopyOf("Plain Song.pro");
        var first = new OpenSong(path).TextBoxes[0];
        var presentation = Workbench.Read(path);
        ElementOf(presentation, first.Element).Element_.Text = null;
        var song = bench.OpenAt(bench.FileOf("a-shape.pro", presentation.ToByteArray()));
        // (A slide with only a shape on it is a blank slide with no text box.)
        Assert.True(song.TextBoxes[0].Blank);
        Assert.Equal("", song.TextBoxes[0].Element);

        var refusal = Assert.Throws<Refusal>(() => song.SetChords(first.Slide, first.Element, [new Chord(0, "G")]));
        Assert.Equal(Refusal.Why.TextBoxGone, refusal.Reason);
        refusal = Assert.Throws<Refusal>(() => song.SetChordsAlone(first.Slide, first.Element, 0, ["G"]));
        Assert.Equal(Refusal.Why.TextBoxGone, refusal.Reason);
        Assert.False(song.CanUndo);
    }

    // ---- Chords with no words

    // spec/01: the stand-in is "a plain space if it had spaces and no U+200B, else
    // U+200B". A line that has both hangs its chords on zero-width spaces from then on.
    [Fact]
    public void ALineWithBothKindsOfStandInIsGivenZeroWidthSpaces()
    {
        var path = bench.CopyOf(Shaped);
        var box = new OpenSong(path).TextBoxes.First(one => !one.Blank && Chords.IsPlaceholders(one.Words) && !one.Words.Contains(Words.ZeroWidthSpace, StringComparison.Ordinal));
        var presentation = Workbench.Read(path);
        var text = Workbench.TextOf(presentation, box.Element);
        // A zero-width space, an em quad, and a plain space: two chords, the second on
        // the plain space.
        text.RtfData = ByteString.CopyFrom(Rtf("{", "rtf1 ", "uc0", "u8203 ", "uc0", "u8193  }"));
        RemoveChords(text);
        text.Attributes.CustomAttributes.Add(new CustomAttribute { Range = new IntRange { Start = 0, End = 1 }, Chord = "G" });
        text.Attributes.CustomAttributes.Add(new CustomAttribute { Range = new IntRange { Start = 2, End = 3 }, Chord = "D" });
        var song = bench.OpenAt(bench.FileOf("both-kinds.pro", presentation.ToByteArray()));
        Assert.Equal([Words.ZeroWidthSpace, Words.EmQuad, ' '], song.TextBoxes.Single(one => one.Slide == box.Slide).Words.ToCharArray());

        song.SetChordsAlone(box.Slide, box.Element, 0, ["G", "D", "Em"]);

        var after = song.TextBoxes.Single(one => one.Slide == box.Slide);
        Assert.Equal(Chords.Placeholders(3), after.Words);
        Assert.Equal([new Chord(0, "G"), new Chord(2, "D"), new Chord(4, "Em")], after.Chords);
    }

    // spec/05, request 2: "A chord that stands on the line, or on the line break that
    // ends it, is one of the line's, and is replaced by the names. One before the line
    // stays; one after it moves by as much as the line grew or shrank."
    [Fact]
    public void AChordOnTheLineBreakThatEndsALineOfChordsAloneIsTheLines()
    {
        var path = bench.CopyOf(Shaped);
        // The slide whose three chords on plain spaces are over its two lines of words.
        var box = new OpenSong(path).TextBoxes.First(one => !one.Blank && Words.Lines(one.Words).Length == 3 && Chords.IsPlaceholders(Words.Lines(one.Words)[0]));
        var lineBreak = Words.Lines(box.Words)[0].Length;
        var below = box.Chords.Where(chord => chord.At > lineBreak).ToList();
        Assert.NotEmpty(below);
        var presentation = Workbench.Read(path);
        Workbench.TextOf(presentation, box.Element).Attributes.CustomAttributes.Add(
            new CustomAttribute { Range = new IntRange { Start = lineBreak, End = lineBreak + 1 }, Chord = "OnTheBreak" });
        var song = bench.OpenAt(bench.FileOf("on-the-break.pro", presentation.ToByteArray()));
        Assert.Contains(new Chord(lineBreak, "OnTheBreak"), song.TextBoxes.Single(one => one.Slide == box.Slide).Chords);

        // Every chord taken off the line. (With a chord put on it instead, a chord that
        // had wrongly been kept from the break would land on the same stand-in and give
        // way to the new one, and nothing would show: so none is put on.)
        song.SetChordsAlone(box.Slide, box.Element, 0, []);

        // The line is one stand-in now with no chord on it, the chord on its break gone
        // with its others, and those below have moved with their words.
        var after = song.TextBoxes.Single(one => one.Slide == box.Slide);
        var moved = 1 - lineBreak;
        Assert.Equal(Words.ZeroWidthSpace.ToString(), Words.Lines(after.Words)[0]);
        Assert.Equal(below.Select(chord => chord with { At = chord.At + moved }), after.Chords);
    }

    // spec/01, "A change of mind": the file is then "byte for byte what it was". A slide
    // may have nothing written of itself at all, not even that it is empty. Given a text
    // box and then relieved of it, it is that again.
    [Fact]
    public void ASlideWithNothingWrittenOfItselfIsThatAgainAfterAChangeOfMind()
    {
        var path = bench.CopyOf("Blank Slides.pro");
        var bare = new OpenSong(path).TextBoxes.Single(box => box.Blank && box.Element.Length == 0);
        var presentation = Workbench.Read(path);
        var rows = presentation.Cues.SelectMany(cue => cue.Actions).Where(action => action.Slide?.Presentation is not null).ToList();
        var slide = rows.Single(action => action.Slide.Presentation.BaseSlide is not null && action.Slide.Presentation.BaseSlide.Elements.Count == 0);
        slide.Slide.Presentation.BaseSlide = null;
        var song = bench.OpenAt(bench.FileOf("nothing-of-itself.pro", presentation.ToByteArray()));
        var asItWas = File.ReadAllBytes(song.Path);
        Assert.Equal("", song.TextBoxes.Single(box => box.Slide == bare.Slide).Element);

        song.SetChordsAlone(bare.Slide, "", 0, ["E"]);
        var given = song.TextBoxes.Single(box => box.Slide == bare.Slide);
        Assert.Equal([new Chord(0, "E")], given.Chords);
        song.SetChordsAlone(bare.Slide, given.Element, 0, []);

        Assert.Equal(asItWas, File.ReadAllBytes(song.Path));
    }

    // spec/01, "A slide with no text box": the box is a copy of the song's own; "its
    // name is the copied name, or that with ` 2`, ` 3` ... after it until no other
    // element of the slide has it; its build_in, build_out, child_builds and data_links
    // are cleared"; and "it is added after the slide's other elements, so that it is
    // drawn over them".
    [Fact]
    public void ABoxGivenToASlideThatHasAShapeIsNamedApartClearedOfItsBuildsAndGoesOnTop()
    {
        var path = bench.CopyOf(Shaped);
        var reader = new OpenSong(path);
        var bare = reader.TextBoxes.Single(box => box.Blank && box.Element.Length == 0);
        var modelId = reader.TextBoxes.Last(box => !box.Blank && box.Words.Length > 0 && box.Slide < bare.Slide).Element;
        var presentation = Workbench.Read(path);
        // The box it will be modelled on is given ways of coming on and going off, and
        // the bare slide a shape that has the model's name.
        var model = ElementOf(presentation, modelId);
        model.BuildIn = new Slide.Types.Element.Types.Build { DelayTime = 1.5 };
        model.BuildOut = new Slide.Types.Element.Types.Build { DelayTime = 2.5 };
        model.ChildBuilds.Add(new Slide.Types.Element.Types.ChildBuild { Index = 1 });
        var shape = model.Clone();
        shape.Element_.Text = null;
        shape.Element_.Uuid.String = "11111111-2222-3333-4444-555555555555";
        var bareSlide = presentation.Cues.SelectMany(cue => cue.Actions)
            .Single(action => action.Slide?.Presentation?.BaseSlide is not null && action.Slide.Presentation.BaseSlide.Elements.Count == 0)
            .Slide.Presentation.BaseSlide;
        bareSlide.Elements.Add(shape);
        var song = bench.OpenAt(bench.FileOf("a-shape-and-builds.pro", presentation.ToByteArray()));
        Assert.Equal("", song.TextBoxes.Single(box => box.Slide == bare.Slide).Element);

        song.SetChordsAlone(bare.Slide, "", 0, ["E"]);

        var given = song.TextBoxes.Single(box => box.Slide == bare.Slide);
        var elements = Workbench.Read(song.Path).Cues.SelectMany(cue => cue.Actions)
            .Select(action => action.Slide?.Presentation?.BaseSlide?.Elements)
            .Single(all => all is not null && all.Any(element => element.Element_?.Uuid?.String == given.Element))!;
        Assert.Equal(2, elements.Count);
        Assert.Equal(shape.Element_.Uuid.String, elements[0].Element_.Uuid.String);
        var added = elements[1];
        Assert.Equal(given.Element, added.Element_.Uuid.String);
        Assert.Equal(model.Element_.Name + " 2", added.Element_.Name);
        Assert.Null(added.BuildIn);
        Assert.Null(added.BuildOut);
        Assert.Empty(added.ChildBuilds);
        Assert.Empty(added.DataLinks);
    }

    // spec/01, "Chords onto a blank slide": "Take the RTF of the nearest box of the song
    // that is shown and is not blank: on a slide before this one if there is one, else
    // on one after." The test files' boxes all open alike, so here three are given
    // openings that can be told apart.
    [Fact]
    public void ABlankSlidesBoxIsModelledOnTheNearestBeforeItElseTheNearestAfter()
    {
        var path = bench.CopyOf("Blank Slides.pro");
        var boxes = new OpenSong(path).TextBoxes;
        // Its slides: 0 blank, 1 and 2 words, 3 blank, 4 and 5 words, 6 nothing at all.
        Assert.Equal([true, false, false, true, false, false, true], boxes.Select(box => box.Blank));
        var presentation = Workbench.Read(path);
        byte[] Opening(string font, string words) => Rtf("{", "rtf1{", "fonttbl", "f0 " + font + ";}", "f0 " + words + "}");
        Workbench.TextOf(presentation, boxes[1].Element).RtfData = ByteString.CopyFrom(Opening("Farther", "One"));
        Workbench.TextOf(presentation, boxes[2].Element).RtfData = ByteString.CopyFrom(Opening("Nearer", "Two"));
        Workbench.TextOf(presentation, boxes[4].Element).RtfData = ByteString.CopyFrom(Opening("After", "Three"));
        var song = bench.OpenAt(bench.FileOf("told-apart.pro", presentation.ToByteArray()));
        byte[] RtfOf(string element) => Workbench.TextOf(Workbench.Read(song.Path), element).RtfData.ToByteArray();

        // The blank box between: from the slide just before it, not the one before that,
        // and not the one after.
        song.SetChordsAlone(3, boxes[3].Element, 0, ["G"]);
        Assert.Equal(RtfWords.WithTheOpeningOf(Opening("Nearer", "Two"), Chords.Placeholders(1)), RtfOf(boxes[3].Element));

        // The blank box at the start, with nothing before it: from the first after.
        song.SetChordsAlone(0, boxes[0].Element, 0, ["G"]);
        Assert.Equal(RtfWords.WithTheOpeningOf(Opening("Farther", "One"), Chords.Placeholders(1)), RtfOf(boxes[0].Element));

        // The slide with no box, at the end: a copy of the box just before it, which is
        // none of the three.
        song.SetChordsAlone(6, "", 0, ["G"]);
        var given = song.TextBoxes.Single(box => box.Slide == 6);
        Assert.Equal(RtfWords.WithTheOpeningOf(RtfOf(boxes[5].Element), Chords.Placeholders(1)), RtfOf(given.Element));
    }

    // An RTF from its pieces, with a backslash between each two: Rtf("{", "rtf1 x}") is
    // an opening brace, a backslash, and "rtf1 x}". (So that no backslash need be typed
    // into this file before a letter.)
    private static byte[] Rtf(params string[] pieces) => System.Text.Encoding.ASCII.GetBytes(string.Join((char)0x5C, pieces));

    private static void RemoveChords(Graphics.Types.Text text)
    {
        var others = text.Attributes.CustomAttributes.Where(entry => entry.AttributeCase != CustomAttribute.AttributeOneofCase.Chord).ToList();
        text.Attributes.CustomAttributes.Clear();
        text.Attributes.CustomAttributes.AddRange(others);
    }

    private static Slide.Types.Element ElementOf(Presentation presentation, string id) =>
        (from cue in presentation.Cues
         from action in cue.Actions
         from element in action.Slide?.Presentation?.BaseSlide?.Elements ?? []
         where element.Element_?.Uuid?.String == id
         select element).Single();
}
