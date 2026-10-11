// A chord in another key, and a chord written in each of ProPresenter's four notations.
// spec/02-chord-rules.md, "Moving a chord to another key" and "The four notations". Held
// to testdata/vectors/transposed.json, notated.json and shown.json.
//
// In the file a chord is written in the song's ORIGINAL key, whatever key it is shown
// in: ProPresenter moves each chord from the one to the other as it draws. So nothing
// here is ever used to rewrite a file's chords for a change of key. The editors always
// show letters, in the key the chords are written in. Transposed is what tells the keys
// 1 to 7 and the offers apart from key to key; Notated and Shown are here because the
// same rules draw Simple Presenter's stage, and because a "show as numbers" switch in
// the editor would be a cheap thing to add later.
//
// A GUESS, in the three notations that are not letters: ProPresenter does not say what
// exactly it writes and no file to hand shows it, so these are the forms musicians use.
// They are ported as they are; nothing the editor saves goes through them.
//
// Port of transposed, notated and shown of reference/simple-presenter/src/chords.cpp.
// The Mac's is Transposing.swift.

using System.Globalization;

namespace Pro7ChordEditor.ChordRules;

public static partial class Chords
{
    private static readonly string[] Latin = ["Do", "Re", "Mi", "Fa", "Sol", "La", "Si"];
    private static readonly string[] Numerals = ["I", "II", "III", "IV", "V", "VI", "VII"];
    private static readonly string[] SmallNumerals = ["i", "ii", "iii", "iv", "v", "vi", "vii"];

    /// <summary>
    /// The chord as it is in another key. Each of its two notes is moved by as many
    /// letters and as many semitones as the keys' notes are apart, so that it is spelt
    /// as the new key spells it (C sharp minor in E is B flat minor in D flat, not A
    /// sharp minor); a spelling that would need a double sharp or flat gives way to the
    /// plain name of the same note. What kind of chord it is, is put back unchanged. A
    /// chord that cannot be read, a key that is no key, or the same key twice, gives the
    /// chord back exactly as it was.
    /// </summary>
    public static string Transposed(string chord, string fromKey, string toKey)
    {
        var from = Notes.OfKey(fromKey);
        var to = Notes.OfKey(toKey);
        var parts = Parts(chord);
        if (!from.Valid || !to.Valid || !parts.Valid || string.Equals(fromKey, toKey, StringComparison.Ordinal))
        {
            return chord;
        }
        var result = Notes.Written(Notes.Moved(Notes.Read(parts.Root), from, to)) + parts.Quality;
        if (parts.Bass.Length > 0)
        {
            result += "/" + Notes.Written(Notes.Moved(Notes.Read(parts.Bass), from, to));
        }
        return result;
    }

    /// <summary>
    /// The chord, which is in <paramref name="key"/>, in a notation (numbered as
    /// <see cref="Notation"/> is):
    ///   0 letters   as it is: "C#m7/E"
    ///   1 numbers   by the note's place in the key's scale: "6m7/1" in E
    ///   2 numerals  the same in Roman numbers, small for a minor chord: "vi7/I"
    ///   3 do re mi  by the notes' names in the Latin countries, where C is always Do:
    ///               "Do#m7/Mi"
    /// For a minor key the places are counted from its relative major (A minor's are
    /// C's), which is how number charts are usually written.
    /// </summary>
    public static string Notated(string chord, string key, int notation)
    {
        var parts = Parts(chord);
        if (notation == (int)Notation.Letters || !parts.Valid)
        {
            return chord;
        }
        var root = Notes.Read(parts.Root);
        var bass = parts.Bass.Length == 0 ? Note.None : Notes.Read(parts.Bass);

        if (notation == (int)Notation.DoReMi)
        {
            static string Name(Note note) => Latin[note.Letter] + Notes.Marks(note.Accidental);
            return Name(root) + parts.Quality + (bass.Valid ? "/" + Name(bass) : "");
        }

        var scale = Notes.ScaleOf(key);
        if (!scale.Valid)
        {
            return chord;
        }
        string Place(Note note, bool roman, bool small)
        {
            var (degree, accidental) = Notes.PlaceIn(note, scale);
            var number = !roman ? (degree + 1).ToString(CultureInfo.InvariantCulture) : small ? SmallNumerals[degree] : Numerals[degree];
            return Notes.Marks(accidental) + number;
        }
        if (notation == (int)Notation.Numbers)
        {
            return Place(root, false, false) + parts.Quality + (bass.Valid ? "/" + Place(bass, false, false) : "");
        }

        // Numerals: a minor chord is a small numeral and loses its "m"; a diminished one
        // is small too, with the little circle. The bass is always a capital.
        var quality = parts.Quality;
        var isSmall = false;
        if (quality.StartsWith("dim", StringComparison.Ordinal))
        {
            isSmall = true;
            quality = (char)0x00B0 + Text.Mid(quality, 3);
        }
        else if (IsMinorQuality(quality))
        {
            isSmall = true;
            quality = Text.Mid(quality, quality.StartsWith("min", StringComparison.Ordinal) ? 3 : 1);
        }
        return Place(root, true, isSmall) + quality + (bass.Valid ? "/" + Place(bass, true, false) : "");
    }

    /// <summary>
    /// What is drawn for a chord of the file: moved from the key the file's chords are
    /// written in to the key asked for (if both are given and they differ), and then
    /// written in the notation.
    /// </summary>
    public static string Shown(string chord, string originalKey, string key, int notation)
    {
        var move = key.Length > 0 && originalKey.Length > 0 && !string.Equals(key, originalKey, StringComparison.Ordinal);
        return Notated(move ? Transposed(chord, originalKey, key) : chord, move ? key : originalKey, notation);
    }

    /// <summary>Whether what says a chord's kind says it is minor: it starts "m", and not "maj".</summary>
    private static bool IsMinorQuality(string quality) =>
        quality.StartsWith('m') && !quality.StartsWith("maj", StringComparison.Ordinal);
}
