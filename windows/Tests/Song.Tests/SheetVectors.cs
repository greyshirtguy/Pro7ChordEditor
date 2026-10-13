// What the tests of the chord sheet's model share: the shape of
// testdata/vectors/sheet-model.json, and a stand-in for the song.
//
// The vectors were made by running the reference's own functions against a stand-in
// document (tools/make-vectors/sheet.js): one that keeps a song as the list of text
// boxes the real one hands over, notes every change it is asked for, and makes the
// change the way the real one does, so that the next thing done starts from the song as
// it then is. `StandInSong` is that stand-in again, written from that script by way of
// the Mac's (SheetVectors.swift). What a test compares is what the song was ASKED, which
// is the part a port has to get the same; and the song afterwards, which says the
// stand-in here is the one there.
//
// Strings are compared unit by unit, spelt out (`Spelt.Of`): some of these hold U+200B
// and U+2001, which cannot be seen.

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Pro7ChordEditor.ChordRules;
using Pro7ChordEditor.Tests;

namespace Pro7ChordEditor.Song.Tests;

internal static class Spelt
{
    internal static string Of(string text)
    {
        var spelt = new StringBuilder("\"");
        foreach (var unit in text)
        {
            if (unit == '"')
            {
                spelt.Append((char)0x5C).Append('"');
            }
            else if (unit == (char)0x5C)
            {
                spelt.Append((char)0x5C).Append((char)0x5C);
            }
            else if (unit is >= ' ' and <= '~')
            {
                spelt.Append(unit);
            }
            else
            {
                spelt.Append((char)0x5C).Append("u{").Append(((int)unit).ToString("X", CultureInfo.InvariantCulture)).Append('}');
            }
        }
        return spelt.Append('"').ToString();
    }

    internal static string Of(IEnumerable<Chord> chords) => "[" + string.Join(", ", chords.Select(chord => Of(chord.Name) + "@" + N(chord.At))) + "]";

    internal static string Of(Spot spot) => "row " + N(spot.Row) + " at " + N(spot.At);

    internal static string Of(IEnumerable<string> names) => "[" + string.Join(", ", names.Select(Of)) + "]";

    internal static string N(int number) => number.ToString(CultureInfo.InvariantCulture);

    internal static string B(bool flag) => flag ? "true" : "false";
}

internal sealed record SheetVectors(
    Dictionary<string, SheetVectors.ASong> Songs,
    List<SheetVectors.RowsOf> Rows,
    List<SheetVectors.WordStartsCase> WordStarts,
    List<SheetVectors.StopsCase> Stops,
    List<SheetVectors.StopAtCase> StopAt,
    List<SheetVectors.Walk> Walks,
    List<SheetVectors.Edit> Edits,
    List<SheetVectors.TypingCase> Typing,
    List<SheetVectors.CopyPaste> CopyPasted)
{
    private static readonly JsonSerializerOptions AsWritten = new() { PropertyNameCaseInsensitive = true };

    internal sealed record AChord(int At, string Name)
    {
        internal Chord Chord => new(At, Name);
    }

    internal sealed record ASpot(int Row, int At)
    {
        internal Spot Spot => new(Row, At);
    }

    internal sealed record Box(int Row, string Group, bool GroupStart, string Element, string Text, List<AChord> Chords, bool Blank)
    {
        internal TextBox TextBox => new(Row, Group, null, GroupStart, Element, Text, [.. Chords.Select(chord => chord.Chord)], Blank);
    }

    internal sealed record ASong(string Key, List<Box> TextBoxes);

    internal sealed record ARow(
        string Kind, int Row, string? Name, int? Block, int? Line, string? Element, string? Text, int? Start, List<AChord>? Chords, bool? Alone, bool? Blank, bool? First, int? Count, bool? Last);

    internal sealed record RowsOf(string Song, int MostLines, List<string> Used, List<ARow> Rows);

    internal sealed record WordStartsCase(string Text, List<int> Out);

    internal sealed record StopsCase(string Song, int Row, bool Usable, List<int> Out);

    internal sealed record StopAtCase(string Song, int Row, int At, int Out);

    internal sealed record Walk(string Song, ASpot? Start, List<string> Keys, List<ASpot> Spots);

    /// <summary>What the song was asked.</summary>
    internal sealed record Asked(string Ask, int Row, string Element, List<AChord>? Chords, int? Line, List<string>? Names, bool Joined)
    {
        internal string Spelt => Ask == "setChords"
            ? StandInSong.AskedForChords(Row, Element, (Chords ?? []).Select(chord => chord.Chord), Joined)
            : StandInSong.AskedForChordsAlone(Row, Element, Line ?? -1, Names ?? [], Joined);
    }

    internal sealed record After(int Row, string Element, string Text, List<AChord> Chords)
    {
        internal string Spelt => StandInSong.Said(Row, Element, Text, Chords.Select(chord => chord.Chord));
    }

    /// <summary>
    /// What was done next, at the case's own spot or, if it names a place, at that place
    /// of the spot's row; and what was done after that.
    /// </summary>
    internal sealed record Then(string? Put, int? At, bool? Take, [property: JsonPropertyName("then")] Then? Next);

    internal sealed record What(string? Put, bool? Take, int? Key, ASpot? Move, ASpot? Copy, int? OnRow, Then? Then, string? Note);

    internal sealed record Edit(
        string Song, ASpot Spot, [property: JsonPropertyName("do")] What What, List<Asked> Asked, ASpot SpotAfter, List<After> SongAfter, bool? Answer);

    internal sealed record Holds(string Text, bool Selected);

    internal sealed record StepWhat(ASpot? Spot, string? Press, string? Typed, string? Finish, ASpot? Click, bool? Settle);

    internal sealed record TypingStep(StepWhat Step, List<Asked> Asked, bool Typing, ASpot? TypingAt, Holds? BoxHolds, ASpot Spot);

    internal sealed record TypingCase(string Song, string Title, List<TypingStep> Steps, List<After> SongAfter);

    /// <summary>What was copied: each line its chords by word, or the names of a line of chords alone.</summary>
    internal sealed record CopiedOf(List<JsonElement> Lines)
    {
        internal IEnumerable<string> Spelt => Lines.Select(line => line.ValueKind == JsonValueKind.Array
            ? "words [" + string.Join(", ", line.EnumerateArray().Select(chord =>
                Tests.Spelt.Of(chord.GetProperty("name").GetString()!) + " word " + Tests.Spelt.N(chord.GetProperty("word").GetInt32()) + "+" + Tests.Spelt.N(chord.GetProperty("offset").GetInt32()))) + "]"
            : "alone " + Tests.Spelt.Of(line.GetProperty("alone").EnumerateArray().Select(name => name.GetString()!)));
    }

    /// <summary>How many of each a paste answers that it did (spec/03, rule 52a).</summary>
    internal sealed record Did(int Lines, int Chords, int LeftOut, int PassedOver, int Refused)
    {
        internal ChordSheet.Pasted Pasted => new(Lines, Chords, LeftOut, PassedOver, Refused);
    }

    /// <summary>What a paste answers, and the sentence that is said of it (in the vectors since 2026-10-12).</summary>
    internal sealed record PastedOf(Did Did, List<string> Said);

    internal sealed record CopyPaste(
        string Song, string Title, ASpot CopyAt, bool WholeGroup, CopiedOf Copied, List<string> Said, ASpot PasteAt, List<Asked> Asked, List<After> SongAfter, PastedOf Pasted);

    internal static SheetVectors Read()
    {
        using var read = JsonDocument.Parse(TestData.Vector("sheet-model.json"));
        var root = read.RootElement;
        T Of<T>(string name) => root.GetProperty(name).Deserialize<T>(AsWritten) ?? throw new InvalidDataException("sheet-model.json has no " + name);
        return new SheetVectors(
            Of<Dictionary<string, ASong>>("songs"),
            Of<List<RowsOf>>("rows"),
            Of<List<WordStartsCase>>("wordStarts"),
            Of<List<StopsCase>>("stops"),
            Of<List<StopAtCase>>("stopAt"),
            Of<List<Walk>>("walks"),
            Of<List<Edit>>("edits"),
            Of<List<TypingCase>>("typing"),
            Of<List<CopyPaste>>("copyPaste"));
    }

    /// <summary>A sheet on one of the vectors' songs, and the stand-in it asks.</summary>
    internal (ChordSheet Sheet, StandInSong Song) Sheet(string name)
    {
        var made = Songs.TryGetValue(name, out var found) ? found : throw new InvalidDataException("the vectors have no song " + name);
        var song = new StandInSong(made.TextBoxes.Select(box => box.TextBox), made.Key);
        return (new ChordSheet(song), song);
    }
}

/// <summary>The song's stand-in, as tools/make-vectors/sheet.js has it.</summary>
internal sealed class StandInSong(IEnumerable<TextBox> textBoxes, string key) : ISheetSong
{
    private const char ZeroWidth = (char)0x200B;
    private const char Quad = (char)0x2001;
    private const string AddedBox = "(a text box added for the chords)";

    private readonly List<TextBox> boxes = [.. textBoxes];

    // What was made for the chords of blank slides, as the real song remembers it while
    // it is open: the boxes that were given words, by their slide and id, and those of
    // them that were added. What was made goes again when the chords do.
    private readonly List<string> made = [];
    private readonly List<string> added = [];

    public IReadOnlyList<TextBox> TextBoxes => [.. boxes];

    public string Key { get; set; } = key;

    /// <summary>What it was asked, spelt out as <see cref="SheetVectors.Asked"/> spells what is expected.</summary>
    internal List<string> Asked { get; } = [];

    /// <summary>What to refuse everything with, if anything.</summary>
    internal Refusal? Refusing { get; set; }

    /// <summary>The song's last text box goes, as if something else had changed the song.</summary>
    internal void LoseItsLastBox() => boxes.RemoveAt(boxes.Count - 1);

    public IReadOnlyList<string> UsedChords => [.. boxes.SelectMany(box => box.Chords.Select(chord => chord.Name))];

    internal IReadOnlyList<string> After => [.. boxes.Select(box => Said(box.Slide, box.Element, box.Words, box.Chords))];

    internal static string Said(int slide, string element, string words, IEnumerable<Chord> chords) =>
        "slide " + Spelt.N(slide) + " box " + Spelt.Of(element) + " " + Spelt.Of(words) + " " + Spelt.Of(chords);

    internal static string AskedForChords(int slide, string element, IEnumerable<Chord> chords, bool joined) =>
        "setChords slide " + Spelt.N(slide) + " box " + Spelt.Of(element) + " " + Spelt.Of(chords) + " joined " + Spelt.B(joined);

    internal static string AskedForChordsAlone(int slide, string element, int line, IEnumerable<string> names, bool joined) =>
        "setChordsAlone slide " + Spelt.N(slide) + " box " + Spelt.Of(element) + " line " + Spelt.N(line) + " " + Spelt.Of(names) + " joined " + Spelt.B(joined);

    private int Find(int slide, string element)
    {
        var index = boxes.FindIndex(box => box.Slide == slide && string.Equals(box.Element, element, StringComparison.Ordinal));
        return index >= 0 ? index : throw new InvalidOperationException("the stand-in has no box " + Spelt.Of(element) + " on slide " + Spelt.N(slide));
    }

    // A text box's words are nothing to a song: none, or only white space, with none of
    // the characters that chords with no words hang on.
    private static bool NoWords(string text) => text.All(unit =>
        unit != ZeroWidth && unit != Quad
        && (unit is (char)0x09 or (char)0x0A or (char)0x0B or (char)0x0C or (char)0x0D or (char)0x20 or (char)0xA0 or (char)0x1680
            or (char)0x2028 or (char)0x2029 or (char)0x202F or (char)0x205F or (char)0x3000 or (char)0xFEFF
            || (unit >= (char)0x2000 && unit <= (char)0x200A)));

    public void SetChords(int slide, string element, IReadOnlyList<Chord> chords, bool joined)
    {
        Asked.Add(AskedForChords(slide, element, chords, joined));
        if (Refusing is not null)
        {
            throw Refusing;
        }
        var index = Find(slide, element);
        boxes[index] = boxes[index] with { Chords = Chords.Tidy(boxes[index].Words, chords.Select(chord => new Chord(chord.At, Chords.Tidied(chord.Name)))) };
    }

    public void SetChordsAlone(int slide, string element, int line, IReadOnlyList<string> names, bool joined)
    {
        Asked.Add(AskedForChordsAlone(slide, element, line, names, joined));
        if (Refusing is not null)
        {
            throw Refusing;
        }
        var index = Find(slide, element);
        var block = boxes[index];
        // A blank slide's box: nothing there and nothing to put changes nothing. Its
        // first chords become all it holds, on zero-width spaces; and a slide with no
        // text box (no id) is given one for them.
        if (block.Chords.Count == 0 && NoWords(block.Words))
        {
            if (names.Count == 0)
            {
                return;
            }
            var id = block.Element;
            if (id.Length == 0)
            {
                id = AddedBox;
                added.Add(id);
            }
            made.Add(Spelt.N(block.Slide) + "/" + id);
            boxes[index] = block with
            {
                Element = id,
                Words = Chords.Placeholders(names.Count, ZeroWidth),
                Chords = [.. names.Select((name, n) => new Chord(n * 2, Chords.Tidied(name)))],
                Blank = false,
            };
            return;
        }
        var lines = Words.Lines(block.Words).ToList();
        var old = lines[line];
        var standIn = !old.Contains(ZeroWidth, StringComparison.Ordinal) && old.Contains(' ', StringComparison.Ordinal) ? ' ' : ZeroWidth;
        // (A line with no chord is one zero-width space: decided on 2026-10-12, and in
        // tools/make-vectors/sheet.js since.)
        var standIns = names.Count == 0 ? Chords.Placeholders(1, ZeroWidth) : Chords.Placeholders(names.Count, standIn);
        var start = lines.Take(line).Sum(before => before.Length + 1);
        var shift = standIns.Length - old.Length;
        var kept = new List<Chord>();
        foreach (var chord in block.Chords)
        {
            if (chord.At < start)
            {
                kept.Add(chord);
            }
            else if (chord.At > start + old.Length)
            {
                kept.Add(new Chord(chord.At + shift, chord.Name));
            }
        }
        lines[line] = standIns;
        var words = string.Join(Words.LineBreak, lines);
        for (var n = 0; n < names.Count; n++)
        {
            kept.Add(new Chord(start + (n * 2), Chords.Tidied(names[n])));
        }
        var chords = Chords.Tidy(words, kept);
        block = block with { Words = words, Chords = chords };
        lines[line] = "";
        var without = string.Join(Words.LineBreak, lines);
        if (names.Count == 0 && chords.Count == 0 && NoWords(without) && made.Contains(Spelt.N(block.Slide) + "/" + block.Element))
        {
            // The last chord taken off a blank slide that was given its chords while the
            // song has been open: what was made for them is unmade. The slide is blank
            // again, handed over with no words, and without the text box if that was
            // only added for the chords.
            block = block with { Words = "", Blank = true, Element = added.Contains(block.Element) ? "" : block.Element };
        }
        boxes[index] = block;
    }
}

/// <summary>
/// Letters of a fixed width, as testdata/vectors/layout.json was made with: every letter
/// of the words ten wide, every letter of a chord eight, and a bubble seven either side
/// of its name.
/// </summary>
internal sealed class FixedWidths : ISheetMeasure
{
    public double Advance(int row, int units) => 10.0 * units;

    public double Bubble(string name) => (8.0 * name.Length) + 14;
}
