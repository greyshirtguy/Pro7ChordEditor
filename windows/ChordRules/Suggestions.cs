// Helping a chord to be entered quickly: the seven chords that belong to a key, which
// the keys 1 to 7 put on, and what a chord being typed might be going to be, which the
// typing bubble offers. spec/02-chord-rules.md, "A key's own chords" and "What a chord
// being typed might be". Held to testdata/vectors/diatonic.json and completions.json.
//
// Port of diatonic and completions of reference/simple-presenter/src/chords.cpp. The
// Mac's is Suggestions.swift.

namespace Pro7ChordEditor.ChordRules;

public static partial class Chords
{
    // What kind of chord stands on each note of a major scale.
    private static readonly string[] KindsInAKey = ["", "m", "m", "", "", "m", "dim"];

    // The usual kinds of chord, in the order they are offered on a note that was typed.
    private static readonly string[] UsualKinds =
        ["", "m", "7", "m7", "maj7", "sus4", "sus2", "2", "add9", "6", "9", "5", "dim", "aug", "m7b5", "11", "13"];

    /// <summary>
    /// The seven chords that belong to a key, on each note of its scale in turn: for C,
    /// "C", "Dm", "Em", "F", "G", "Am", "Bdim". A minor key's are its relative major's,
    /// starting from the key's own note: for Am, "Am", "Bdim", "C", "Dm", "Em", "F", "G".
    /// Empty for what is no key.
    /// </summary>
    public static IReadOnlyList<string> Diatonic(string key)
    {
        var scale = Notes.ScaleOf(key);
        if (!scale.Valid)
        {
            return [];
        }
        var result = new List<string>();
        for (var degree = 0; degree < 7; degree++)
        {
            var letter = (scale.Letter + degree) % 7;
            var accidental = Notes.Nearest((scale.Pitch + Notes.Semitones[degree]) % 12 - Notes.Semitones[letter]);
            result.Add(Notes.Written(new Note(letter, accidental)) + KindsInAKey[degree]);
        }
        // A minor key's own chords start from its own note: the sixth of the major scale.
        if (KeyIsMinor(key))
        {
            result = [.. result.Skip(5), .. result.Take(5)];
        }
        return result;
    }

    /// <summary>
    /// What a chord being typed might be going to be, likeliest first, for a list to
    /// pick from. <paramref name="used"/> is every chord of the song, one entry for each
    /// time it is used.
    ///
    /// What is typed is tidied first. Only names that START WITH it are ever offered,
    /// never the very thing typed, and nothing twice. In this order: the song's own
    /// chords, the most used first (among equals, in the order they first come in the
    /// song); the key's own chords; and then, if something is typed and it reads as a
    /// chord, either the usual bass notes (after a slash) or the usual kinds of chord on
    /// the note typed. With nothing typed it is the song's chords and the key's.
    /// </summary>
    public static IReadOnlyList<string> Completions(string typed, string key, IReadOnlyList<string> used)
    {
        var start = Tidied(typed);
        var result = new List<string>();
        void Offer(string chord)
        {
            if (chord.Length > 0
                && !string.Equals(chord, start, StringComparison.Ordinal)
                && chord.StartsWith(start, StringComparison.Ordinal)
                && !result.Contains(chord, StringComparer.Ordinal))
            {
                result.Add(chord);
            }
        }

        // The song's own chords, the most used first; then the key's. (OrderBy keeps
        // those used equally often in the order they first came in.)
        var own = used.Distinct(StringComparer.Ordinal)
            .OrderByDescending(chord => used.Count(other => string.Equals(other, chord, StringComparison.Ordinal)))
            .ToList();
        foreach (var chord in own)
        {
            Offer(chord);
        }
        var inKey = Diatonic(key);
        foreach (var chord in inKey)
        {
            Offer(chord);
        }
        if (start.Length == 0)
        {
            return result;
        }

        var typedParts = Parts(start);
        if (!typedParts.Valid)
        {
            return result;
        }
        var slash = start.LastIndexOf('/');
        if (slash >= 0 && (typedParts.Bass.Length == 0) == start.EndsWith('/'))
        {
            // After a slash (the name ends with one, or has a bass note that may yet be
            // gone on with, as "D/F" may to "D/F#"): the chord over its third and its
            // fifth, which are what most slash chords stand on, then over the notes of
            // the key. The third and the fifth are written in flats if the chord's root
            // is flat or is F, in sharps otherwise.
            var before = Text.Left(start, slash + 1);
            var root = Notes.Read(typedParts.Root);
            var minor = IsMinorQuality(typedParts.Quality);
            var names = root.Accidental < 0 || Notes.Letters[root.Letter] == 'F' ? Notes.FlatNames : Notes.SharpNames;
            Offer(before + names[(root.Pitch + (minor ? 3 : 4)) % 12]);
            Offer(before + names[(root.Pitch + 7) % 12]);
            foreach (var chord in inKey)
            {
                // (A chord over its own root is only the chord.)
                var note = Parts(chord).Root;
                if (!string.Equals(note, typedParts.Root, StringComparison.Ordinal))
                {
                    Offer(before + note);
                }
            }
            return result;
        }
        // The usual kinds of chord on the note typed.
        foreach (var kind in UsualKinds)
        {
            Offer(typedParts.Root + kind);
        }
        return result;
    }
}
