// Keys: as the file has them and as a musician writes them.
//
// ProPresenter keeps a song's key as one of twenty-one names (every letter plain, sharp
// and flat, as its file format lists them: A flat is 0 and G sharp is 20) and whether it
// is major or minor. Everywhere else a key is written the way a musician writes one, "E"
// or "Bb" or "F#m", and these go between the two. Held to testdata/vectors/keys.json.

/// The keys as the file numbers them (MusicKeyScale.MusicKey).
private let keyNames = ["Ab", "A", "A#", "Bb", "B", "B#", "Cb", "C", "C#", "Db", "D",
                        "D#", "Eb", "E", "E#", "Fb", "F", "F#", "Gb", "G", "G#"]

/// A key's name ends in "m", and is more than that letter, if it is minor.
func keyIsMinor(_ key: Units) -> Bool {
    key.count > 1 && key.endsWith(u("m"))
}

extension Chords {
    /// A key by the number the file has for it, as its name: "Ab" for 0 and so on, with
    /// "m" after it for a minor one. Empty outside 0 to 20.
    public static func keyName(_ number: Int, minor: Bool) -> String {
        guard keyNames.indices.contains(number) else { return "" }
        return keyNames[number] + (minor ? "m" : "")
    }

    /// The number the file has for a key; -1 for a name that is no key.
    public static func keyNumber(_ key: String) -> Int {
        var name = units(key)
        if ChordRules.keyIsMinor(name) { name.removeLast() }
        return keyNames.firstIndex { units($0) == name } ?? -1
    }

    public static func keyIsMinor(_ key: String) -> Bool {
        ChordRules.keyIsMinor(units(key))
    }

    /// The keys a picker offers, in the order of the notes from C: the twelve major keys
    /// by their usual names (with both F# and Gb, which are equally usual).
    public static func majorKeys() -> [String] {
        ["C", "Db", "D", "Eb", "E", "F", "F#", "Gb", "G", "Ab", "A", "Bb", "B"]
    }

    /// The same for minor (with both D#m and Ebm).
    public static func minorKeys() -> [String] {
        ["Cm", "C#m", "Dm", "D#m", "Ebm", "Em", "Fm", "F#m", "Gm", "G#m", "Am", "Bbm", "Bm"]
    }
}
