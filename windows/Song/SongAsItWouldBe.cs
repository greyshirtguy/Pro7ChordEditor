// A song as it would be with its chords moved to where the first Windows editor meant
// them, for the sheet to show beside the song as it is. spec/08-repair.md, check 2: a
// song that looks as if it was chorded in that editor "is only ever offered, with the
// song shown both ways on the sheet, as it is and as it would be, for the person to
// judge with their own eyes".
//
// It is to be looked at and nothing else: whatever the sheet asks of it is refused, and
// says why. What it shows is worked out by OpenSong (`TextBoxesMovedBack`) on a copy of
// the song in memory; the song itself and its file are as they were.

using Pro7ChordEditor.ChordRules;

namespace Pro7ChordEditor.Song;

internal sealed class SongAsItWouldBe(IReadOnlyList<TextBox> boxes, string key) : ISheetSong
{
    public IReadOnlyList<TextBox> TextBoxes => boxes;

    public IReadOnlyList<string> UsedChords { get; } = [.. boxes.SelectMany(box => box.Chords.Select(chord => chord.Name))];

    public string Key => key;

    public void SetChords(int slide, string element, IReadOnlyList<Chord> chords, bool joined) => throw Refusal.ShownAsItWouldBe;

    public void SetChordsAlone(int slide, string element, int line, IReadOnlyList<string> names, bool joined) => throw Refusal.ShownAsItWouldBe;
}
