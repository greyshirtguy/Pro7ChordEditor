// A note: one of the seven letters and how far it is sharpened or flattened. Reading one
// from the start of a name, writing one, and moving one from a key to another.
//
// Only the notes of a chord are understood by the rules. A chord's name is a root note,
// then whatever says what kind of chord it is, then perhaps a slash and a bass note; the
// two notes are read with what is here and what stands between them is carried as it is
// (ChordNames.cs).
//
// Port of the first part of reference/simple-presenter/src/chords.cpp (the unnamed
// namespace at its top). The Mac's is mac/Core/Sources/ChordRules/Notes.swift.

namespace Pro7ChordEditor.ChordRules;

/// <summary>
/// A note as its letter (0 for C to 6 for B, or -1 for none) and how far it is sharpened
/// (flattened, below nothing).
/// </summary>
internal readonly record struct Note(int Letter, int Accidental)
{
    internal static readonly Note None = new(-1, 0);

    internal bool Valid => Letter >= 0;

    /// <summary>Which of the twelve notes it is, counted in semitones from C.</summary>
    internal int Pitch => ((Notes.Semitones[Letter] + Accidental) % 12 + 12) % 12;
}

internal static class Notes
{
    /// <summary>The seven letters from C, and how many semitones above C each is.</summary>
    internal const string Letters = "CDEFGAB";
    internal static readonly int[] Semitones = [0, 2, 4, 5, 7, 9, 11];

    /// <summary>The plain name of each of the twelve notes, written with sharps and with flats.</summary>
    internal static readonly string[] SharpNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
    internal static readonly string[] FlatNames = ["C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B"];

    /// <summary>
    /// Reads a note from the start of a piece of text, and says how many units it took.
    /// A letter A to G of either case (and no other character: spec/02, "To the
    /// letter"), then sharps, any number of them, written "#" or with the music sign; or
    /// else flats, written "b" or with the music sign.
    /// </summary>
    internal static Note Read(string text, out int length)
    {
        length = 0;
        if (text.Length == 0)
        {
            return Note.None;
        }
        var letter = Letters.IndexOf(Text.AsciiRaised(text[0]), StringComparison.Ordinal);
        if (letter < 0)
        {
            return Note.None;
        }
        var accidental = 0;
        var i = 1;
        while (i < text.Length && (text[i] == '#' || text[i] == Text.MusicSharp))
        {
            accidental++;
            i++;
        }
        if (i == 1)
        {
            while (i < text.Length && (text[i] == 'b' || text[i] == Text.MusicFlat))
            {
                accidental--;
                i++;
            }
        }
        length = i;
        return new Note(letter, accidental);
    }

    internal static Note Read(string text) => Read(text, out _);

    /// <summary>A note as it is written: its letter as a capital, then "#" or "b" for each step.</summary>
    internal static string Written(Note note) =>
        Letters[note.Letter] + (note.Accidental > 0 ? new string('#', note.Accidental) : new string('b', -note.Accidental));

    /// <summary>The marks alone: "#" or "b" for each step.</summary>
    internal static string Marks(int accidental) => accidental > 0 ? new string('#', accidental) : new string('b', -accidental);

    /// <summary>The note a key is named for, read from the start of its name: major or minor does not matter.</summary>
    internal static Note OfKey(string key) => Read(key);

    /// <summary>
    /// The major key whose scale a key's places are counted on: itself, or for a minor
    /// key the major key a minor third above it.
    /// </summary>
    internal static Note ScaleOf(string key)
    {
        var note = OfKey(key);
        if (!note.Valid || !Chords.KeyIsMinor(key))
        {
            return note;
        }
        var letter = (note.Letter + 2) % 7;
        var rise = Semitones[letter] - Semitones[note.Letter];
        if (rise < 0)
        {
            rise += 12;
        }
        return new Note(letter, note.Accidental + 3 - rise);
    }

    /// <summary>
    /// A note moved by as many letters and as many semitones as <paramref name="to"/> is
    /// above <paramref name="from"/>, so that it is spelt as the new key spells it.
    /// </summary>
    internal static Note Moved(Note note, Note from, Note to)
    {
        var steps = ((to.Letter - from.Letter) % 7 + 7) % 7;
        var rise = ((to.Pitch - from.Pitch) % 12 + 12) % 12;
        var letter = (note.Letter + steps) % 7;
        var pitch = (note.Pitch + rise) % 12;
        var accidental = Nearest(pitch - Semitones[letter]);
        if (Math.Abs(accidental) > 1)
        {
            // No double sharps or flats: the note's plain name, in sharps for a key that
            // is written with them and in flats otherwise.
            var sharps = to.Accidental > 0 || (to.Accidental == 0 && "GDAEB".Contains(Letters[to.Letter], StringComparison.Ordinal));
            return Read(sharps ? SharpNames[pitch] : FlatNames[pitch]);
        }
        return new Note(letter, accidental);
    }

    /// <summary>
    /// A note's place in a major key's scale (0 to 6), and how far it is sharpened from
    /// the note the scale has there.
    /// </summary>
    internal static (int Degree, int Accidental) PlaceIn(Note note, Note key)
    {
        var degree = ((note.Letter - key.Letter) % 7 + 7) % 7;
        var inScale = (key.Pitch + Semitones[degree]) % 12;
        return (degree, Nearest(note.Pitch - inScale));
    }

    /// <summary>The nearest way round the twelve: B sharp is one above B, not eleven below it.</summary>
    internal static int Nearest(int accidental)
    {
        while (accidental > 6)
        {
            accidental -= 12;
        }
        while (accidental < -6)
        {
            accidental += 12;
        }
        return accidental;
    }
}
