// The key, as the file has it and as a musician writes it.
//
// ProPresenter keeps a presentation's key as one of twenty-one names (every letter flat,
// plain and sharp, as its file format lists them: A flat is 0 and G sharp is 20) and
// whether it is major or minor. Everywhere else a key is written the way a musician
// writes one, "E" or "Bb" or "F#m", and KeyNumber and KeyName go between the two.
// spec/02-chord-rules.md, "Keys". Held to testdata/vectors/keys.json.
//
// Port of keyName, keyNumber, keyIsMinor, majorKeys and minorKeys of
// reference/simple-presenter/src/chords.cpp. The Mac's is Keys.swift.

namespace Pro7ChordEditor.ChordRules;

public static partial class Chords
{
    // The keys as the file numbers them (MusicKeyScale.MusicKey).
    private static readonly string[] KeyNames =
        ["Ab", "A", "A#", "Bb", "B", "B#", "Cb", "C", "C#", "Db", "D", "D#", "Eb", "E", "E#", "Fb", "F", "F#", "Gb", "G", "G#"];

    /// <summary>
    /// A key by the number the file has for it, as its name: "Ab" for 0 and so on, with
    /// "m" after it for a minor one. Empty outside 0 to 20.
    /// </summary>
    public static string KeyName(int number, bool minor) =>
        number < 0 || number >= KeyNames.Length ? "" : KeyNames[number] + (minor ? "m" : "");

    /// <summary>The number the file has for a key; -1 for a name that is no key.</summary>
    public static int KeyNumber(string key)
    {
        var name = KeyIsMinor(key) ? key[..^1] : key;
        return Array.FindIndex(KeyNames, one => string.Equals(one, name, StringComparison.Ordinal));
    }

    /// <summary>Whether a key's name is a minor key's: it ends in "m", and is longer than one letter.</summary>
    public static bool KeyIsMinor(string key) => key.Length > 1 && key[^1] == 'm';

    /// <summary>
    /// The major keys a picker offers, in the order of the notes from C: thirteen, by
    /// their usual names, with both F# and Gb, which are equally usual.
    /// </summary>
    public static IReadOnlyList<string> MajorKeys() => ["C", "Db", "D", "Eb", "E", "F", "F#", "Gb", "G", "Ab", "A", "Bb", "B"];

    /// <summary>The same for minor: thirteen, with both D#m and Ebm.</summary>
    public static IReadOnlyList<string> MinorKeys() => ["Cm", "C#m", "Dm", "D#m", "Ebm", "Em", "Fm", "F#m", "Gm", "G#m", "Am", "Bbm", "Bm"];
}
