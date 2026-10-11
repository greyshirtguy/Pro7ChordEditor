// Reading a presentation: which of its cues are slides and in what order, and which of
// a slide's boxes hold the song's words. spec/01-file-format.md, "From the presentation
// down to a text box", is the rule; reference/simple-presenter/src/presentationeditor.cpp
// (`open`, `song`) and proconvert.cpp (`readChords`) are what it was taken from; and
// testdata/files/expected/as-read-by-simple-presenter.json has what every test file
// reads as. The Mac's is Reading.swift.
//
// Nothing here changes anything.
//
// A part of a message that is not there is nothing here (null), where in Swift it reads
// as an empty one. So each step down from the presentation to a text box asks, and a
// part that is not there is read as the Mac reads an empty one: a slide with no
// elements, an element with no text, a text with no words and no chords.

using Pro7ChordEditor.ChordRules;
using RV = Pro.SerializationInterop.RVProtoData;

namespace Pro7ChordEditor.Song;

/// <summary>One slide of the song: where it is in the message, and what its group is.</summary>
/// <param name="Cue">Which of the presentation's cues it is.</param>
/// <param name="Action">Which of that cue's actions is its slide.</param>
internal readonly record struct SlideRow(int Cue, int Action, string Group, Colour? GroupColour, bool StartsItsGroup);

internal static class Reading
{
    /// <summary>
    /// The first of a cue's actions that is switched on and is a presentation's slide,
    /// or -1 if it has none. (The page says "one of its actions is a slide"; that the
    /// action must itself be switched on is from the reference, `slideAction`.)
    /// </summary>
    internal static int SlideAction(RV.Cue cue)
    {
        for (var i = 0; i < cue.Actions.Count; i++)
        {
            var action = cue.Actions[i];
            if (action.IsEnabled && action.Slide?.SlideCase == RV.Action.Types.SlideType.SlideOneofCase.Presentation)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// The slides, in the order ProPresenter shows them: the order of the groups, and
    /// inside each group the order of its cue identifiers; after those, any cue that no
    /// group names, in the order of the cues. A cue is left out if it is switched off or
    /// has no slide, and a cue named twice is taken the first time.
    ///
    /// (A presentation also has arrangements, which play the groups in another order.
    /// They are not gone by here: the sheet is the song as it is laid down, each slide
    /// once.)
    /// </summary>
    internal static List<SlideRow> SlideRows(RV.Presentation presentation)
    {
        // (Two cues with one id: the later is the one a group's naming of that id finds,
        // as in the reference, which keeps them in a table that the later overwrites.)
        var cueOfId = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < presentation.Cues.Count; i++)
        {
            cueOfId[presentation.Cues[i].Uuid?.String ?? ""] = i;
        }
        var rows = new List<SlideRow>();
        var placed = new HashSet<int>();
        bool Place(int cue, string group, Colour? colour, bool starts)
        {
            var candidate = presentation.Cues[cue];
            if (placed.Contains(cue) || !candidate.IsEnabled)
            {
                return false;
            }
            var action = SlideAction(candidate);
            if (action < 0)
            {
                return false;
            }
            placed.Add(cue);
            rows.Add(new SlideRow(cue, action, group, colour, starts));
            return true;
        }

        foreach (var group in presentation.CueGroups)
        {
            // A group has a colour of its own if someone set it in ProPresenter. Most
            // have none (or one that cannot be seen), and ProPresenter then colours the
            // group by its name, from a list it keeps for the whole library.
            var colour = group.Group?.Color;
            Colour? own = colour is not null && colour.Alpha > 0 ? new Colour(colour.Red, colour.Green, colour.Blue, colour.Alpha) : null;
            var first = true;
            foreach (var id in group.CueIdentifiers)
            {
                if (cueOfId.TryGetValue(id.String, out var cue) && Place(cue, group.Group?.Name ?? "", own, first))
                {
                    first = false;
                }
            }
        }
        for (var cue = 0; cue < presentation.Cues.Count; cue++)
        {
            Place(cue, "", null, false);
        }
        return rows;
    }

    /// <summary>The elements of a row's slide. None, if the slide has nothing written of itself.</summary>
    internal static IReadOnlyList<RV.Slide.Types.Element> ElementsOf(SlideRow row, RV.Presentation presentation) =>
        presentation.Cues[row.Cue].Actions[row.Action].Slide?.Presentation?.BaseSlide?.Elements ?? [];

    /// <summary>
    /// Whether a box shows words of its own. A box with a link shows something else:
    /// another element's words, a timer, a clock, the next slide's words. The one kind of
    /// link that leaves a box its own words is one that only says when the box is to be
    /// seen.
    ///
    /// (The page says "no data_links". The reference is finer, and is followed: of the
    /// links it knows it counts every kind but that one. A link of a kind the
    /// descriptions of the format have no name for reads here as no kind at all, and so
    /// as none. The one file ProPresenter wrote that the test data has, has such a link
    /// on every one of its text boxes.)
    /// </summary>
    internal static bool ShowsItsOwnWords(RV.Slide.Types.Element element) =>
        element.DataLinks.All(link => link.PropertyTypeCase
            is RV.Slide.Types.Element.Types.DataLink.PropertyTypeOneofCase.None
            or RV.Slide.Types.Element.Types.DataLink.PropertyTypeOneofCase.VisibilityLink);

    /// <summary>Whether an element is a text box: it has text, whatever is in it.</summary>
    internal static bool HasText(RV.Slide.Types.Element element) => element.Element_?.Text is not null;

    /// <summary>An element's id, or "" if it has none written.</summary>
    internal static string IdOf(RV.Slide.Types.Element element) => element.Element_?.Uuid?.String ?? "";

    /// <summary>
    /// A box's chords as the file has them: every custom attribute that is a chord
    /// (`chord` is the one of its kinds that is set), as its range's start and its name,
    /// sorted by start with the file's order kept among equals. The range's end is not
    /// believed (spec/01-file-format.md, "Chords").
    /// </summary>
    internal static List<Chord> ChordsOf(RV.Graphics.Types.Text? text)
    {
        var found = new List<Chord>();
        foreach (var attribute in text?.Attributes?.CustomAttributes ?? [])
        {
            if (IsAChord(attribute))
            {
                found.Add(new Chord(attribute.Range?.Start ?? 0, attribute.Chord));
            }
        }
        // (OrderBy keeps those that start in one place in the order the file has them.)
        return found.OrderBy(chord => chord.At).ToList();
    }

    /// <summary>
    /// A box's chords as the editors are handed them: as the file has them, but each on
    /// a character of the box's words.
    ///
    /// NEW (spec/08-repair.md, check 0). A chord the file has past the end of its words,
    /// or on the line break that ends a line, stands on no line of the box. An editor
    /// goes by lines, and would not show it at all: it could not be seen, and the next
    /// change to its box would write it where `Chords.Tidy` puts it, as if from
    /// nowhere. So it is handed over at that place already (`Chords.OnACharacter`: the
    /// last character; the character before the break), is shown there, and is written
    /// there when its box's chords next are. Nothing is saved for it until then.
    ///
    /// (The page asks this of the sheet. It is done here, for both editors, so that a
    /// song has the same chords in the one as in the other: NOTES.md has it, for the
    /// maintainer to say and for the Mac.)
    /// </summary>
    internal static List<Chord> ChordsHanded(string words, RV.Graphics.Types.Text? text) =>
        ChordsOf(text).Select(chord => chord with { At = Chords.OnACharacter(words, chord.At) }).OrderBy(chord => chord.At).ToList();

    internal static bool IsAChord(RV.Graphics.Types.Text.Types.Attributes.Types.CustomAttribute attribute) =>
        attribute.AttributeCase == RV.Graphics.Types.Text.Types.Attributes.Types.CustomAttribute.AttributeOneofCase.Chord;

    /// <summary>The words of a box: the plain text of its RTF.</summary>
    internal static string WordsOf(RV.Graphics.Types.Text? text) => text is null ? "" : RtfWords.Read(text.RtfData.Span);

    /// <summary>What the editors are handed.</summary>
    internal static List<TextBox> TextBoxes(RV.Presentation presentation, IReadOnlyList<SlideRow> rows)
    {
        var boxes = new List<TextBox>();
        for (var slide = 0; slide < rows.Count; slide++)
        {
            boxes.AddRange(TextBoxes(slide, presentation, rows));
        }
        return boxes;
    }

    /// <summary>What the editors are handed of one slide: one entry at least.</summary>
    internal static List<TextBox> TextBoxes(int slide, RV.Presentation presentation, IReadOnlyList<SlideRow> rows)
    {
        var row = rows[slide];
        var elements = ElementsOf(row, presentation);
        TextBox Box(string element, string words, IReadOnlyList<Chord> chords, bool blank) =>
            new(slide, row.Group, row.GroupColour, row.StartsItsGroup, element, words, chords, blank);

        var shown = new List<TextBox>();
        foreach (var element in elements)
        {
            if (!ShowsItsOwnWords(element))
            {
                continue;
            }
            var words = WordsOf(element.Element_?.Text);
            var chords = ChordsHanded(words, element.Element_?.Text);
            // A box with no words is left out, unless it is a line of chords alone: it
            // has chords, or the characters Multitracks hangs them on. (A box with only
            // a stray space in it, which files are full of, is neither.)
            if (Words.IsBlank(words) && chords.Count == 0 && !Words.HasAStandIn(words))
            {
                continue;
            }
            shown.Add(Box(IdOf(element), words, chords, false));
        }
        // NEW (spec/01, "A blank slide"): a slide none of whose boxes is shown is handed
        // over all the same, as one box with no words, so that chords can be put on an
        // intro that someone made as an empty slide.
        //   - If it has a text box to put chords in, that is the box: its first that
        //     shows its own words, whatever stray space that may hold.
        //   - If it has none, the box has no id yet. Nothing is made for it here: a text
        //     box is added to the slide only when a chord is put on it
        //     (OpenSong.SetChordsAlone).
        if (shown.Count == 0)
        {
            var empty = elements.FirstOrDefault(element => HasText(element) && ShowsItsOwnWords(element));
            shown.Add(Box(empty is null ? "" : IdOf(empty), "", [], true));
        }
        return shown;
    }

    /// <summary>
    /// A key as a musician writes it, from the file's number for its note and its scale.
    /// One that is not written at all reads as its nought does: A flat major.
    /// </summary>
    internal static string KeyName(RV.MusicKeyScale? key) =>
        key is null ? Chords.KeyName(0, false) : Chords.KeyName((int)key.MusicKey, key.MusicScale == RV.MusicKeyScale.Types.MusicScale.Minor);
}
