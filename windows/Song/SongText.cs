// A song as lines of text, each line knowing what it is: the form the ChordPro editor
// works on (spec/04-chordpro-editor.md, "The text", rules 1 to 3), and what the app
// shows of a song until there is an editor.
//
// It is made from the song's text boxes in order: an empty line before each new slide
// but the first; then, if that slide starts a group that has a name, the group's line;
// then each line of each box as `ToChordPro` writes it. A blank slide is one line, and
// that line is empty.
//
// Beside the text of each line is kept what the line is: which text box and which line
// of it, where that line starts in the box, and its words without chords. Nothing is
// changed here. Reading the text back into chords is the ChordPro editor's, at M5.
//
// The Mac's is SongText.swift.

using Pro7ChordEditor.ChordRules;

namespace Pro7ChordEditor.Song;

public enum SongLineKind
{
    /// <summary>The name of a group, before its first slide. Its text is <c>{c: Verse 1}</c>.</summary>
    Group,

    /// <summary>The gap between one slide and the next. Its text is empty.</summary>
    Gap,

    /// <summary>A line of a slide's words, with its chords.</summary>
    Words,

    /// <summary>A line of chords alone.</summary>
    ChordsAlone,

    /// <summary>A blank slide: a line of chords alone with none yet. Its text is empty.</summary>
    BlankSlide,
}

/// <param name="Text">The line as ChordPro writes it.</param>
/// <param name="Slide">Which slide, counted from 0. For a gap, the slide that follows it.</param>
public sealed record SongLine(SongLineKind Kind, string Text, int Slide)
{
    /// <summary>The group's name, for a group's line; its colour, if the file gives it one.</summary>
    public string Group { get; init; } = "";

    public Colour? GroupColour { get; init; }

    /// <summary>For a line of a box: which of the boxes handed over it is. -1 for a line that is no box's.</summary>
    public int Box { get; init; } = -1;

    /// <summary>The box's id.</summary>
    public string Element { get; init; } = "";

    /// <summary>Which line of the box this is.</summary>
    public int Line { get; init; }

    /// <summary>Where that line starts in the box.</summary>
    public int Start { get; init; }

    /// <summary>The line's words, without chords.</summary>
    public string Words { get; init; } = "";
}

public static class SongText
{
    /// <summary>The lines of a song, from what the editors are handed.</summary>
    public static IReadOnlyList<SongLine> Lines(IReadOnlyList<TextBox> boxes)
    {
        var lines = new List<SongLine>();
        int? slide = null;
        for (var index = 0; index < boxes.Count; index++)
        {
            var box = boxes[index];
            if (box.Slide != slide)
            {
                if (slide is not null)
                {
                    lines.Add(new SongLine(SongLineKind.Gap, "", box.Slide));
                }
                if (box.StartsItsGroup && box.Group.Length > 0)
                {
                    lines.Add(new SongLine(SongLineKind.Group, "{c: " + box.Group + "}", box.Slide) { Group = box.Group, GroupColour = box.GroupColour });
                }
                slide = box.Slide;
            }
            if (box.Blank)
            {
                lines.Add(new SongLine(SongLineKind.BlankSlide, "", box.Slide) { Box = index, Element = box.Element });
                continue;
            }
            var start = 0;
            var number = 0;
            foreach (var words in ChordRules.Words.Lines(box.Words))
            {
                // A chord belongs to the line its place is in; an empty line is taken as
                // one place wide, so that a chord standing on it is not lost.
                var from = start;
                var own = box.Chords
                    .Where(chord => chord.At >= from && chord.At < from + Math.Max(1, words.Length))
                    .Select(chord => new Chord(chord.At - from, chord.Name))
                    .ToList();
                lines.Add(new SongLine(
                    Chords.IsPlaceholders(words) ? SongLineKind.ChordsAlone : SongLineKind.Words,
                    Chords.ToChordPro(new Line(words, own)),
                    box.Slide)
                {
                    Box = index,
                    Element = box.Element,
                    Line = number,
                    Start = start,
                    Words = words,
                });
                start += words.Length + 1;
                number++;
            }
        }
        return lines;
    }

    /// <summary>The whole song as one piece of text, its lines parted by line breaks.</summary>
    public static string Text(IReadOnlyList<TextBox> boxes) => string.Join('\n', Lines(boxes).Select(line => line.Text));
}
