// Helping a chord to be entered quickly: the key's own chords, and what a chord being
// typed might be going to be. Held to testdata/vectors/diatonic.json and
// completions.json.

extension Chords {
    /// The seven chords that belong to a key, on each note of its scale in turn: for C,
    /// "C", "Dm", "Em", "F", "G", "Am", "Bdim". For a minor key they are its relative
    /// major's, starting from the key's own note. Empty for what is no key. They are
    /// what the keys 1 to 7 put on in the chord sheet.
    public static func diatonic(_ key: String) -> [String] {
        ChordRules.diatonic(units(key)).map(string)
    }

    /// What a chord being typed might be going to be, likeliest first, for a list to
    /// pick from. `used` is every chord of the song, one entry for each time it is used.
    ///
    /// What is typed is tidied first. Only names that START WITH it are ever offered,
    /// never the very thing typed, and nothing twice. In this order: the song's own
    /// chords, the most used first; the key's own; and then, if something is typed and
    /// it reads as a chord, after a slash the usual bass notes, otherwise the usual
    /// kinds of chord on the note typed. With nothing typed it is the song's chords and
    /// the key's.
    public static func completions(typed: String, key: String, used: [String]) -> [String] {
        ChordRules.completions(typed: units(typed), key: units(key), used: used.map(units)).map(string)
    }
}

func diatonic(_ key: Units) -> [Units] {
    let scale = scaleOf(key)
    guard scale.valid else { return [] }
    let kinds = ["", "m", "m", "", "", "m", "dim"]
    var result: [Units] = []
    for degree in 0..<7 {
        var note = Note()
        note.letter = (scale.letter + degree) % 7
        note.accidental = nearest((scale.pitch + semitones[degree]) % 12 - semitones[note.letter])
        result.append(note.written + units(kinds[degree]))
    }
    // A minor key's own chords start from its own note: the sixth of the major scale.
    if keyIsMinor(key) {
        result = Array(result[5...] + result[..<5])
    }
    return result
}

/// The usual kinds of chord on a note, in the order they are offered.
private let usualKinds = ["", "m", "7", "m7", "maj7", "sus4", "sus2", "2", "add9", "6", "9", "5", "dim", "aug", "m7b5", "11", "13"]

func completions(typed: Units, key: Units, used: [Units]) -> [Units] {
    let start = tidied(typed)
    var result: [Units] = []
    func offer(_ chord: Units) {
        if !chord.isEmpty && chord != start && chord.startsWith(start) && !result.contains(chord) {
            result.append(chord)
        }
    }
    // The song's own chords, the most used first (among equals, in the order they first
    // come in the song); then the key's.
    var uses: [Units: Int] = [:]
    var own: [Units] = []
    for chord in used {
        if uses[chord] == nil { own.append(chord) }
        uses[chord, default: 0] += 1
    }
    for chord in own.stablySorted(by: { uses[$0]! > uses[$1]! }) {
        offer(chord)
    }
    let inKey = diatonic(key)
    for chord in inKey {
        offer(chord)
    }
    if start.isEmpty { return result }

    let typedParts = parts(start)
    guard typedParts.valid else { return result }
    let slash = start.lastIndexOf(u("/"))
    if slash >= 0 && typedParts.bass.isEmpty == start.endsWith(u("/")) {
        // After a slash (the name ends with one, or has a bass note that may yet be gone
        // on with, as "D/F" may to "D/F#"): the chord's third and fifth, which are what
        // most slash chords stand on, then the notes of the key.
        let before = start.left(slash + 1)
        let root = readNote(typedParts.root).note
        let minor = minorQuality(typedParts.quality)
        let names = (root.accidental < 0 || noteLetters[root.letter] == u("F")) ? flatNames : sharpNames
        offer(before + units(names[(root.pitch + (minor ? 3 : 4)) % 12]))
        offer(before + units(names[(root.pitch + 7) % 12]))
        for chord in inKey {
            // (A chord over its own root is only the chord.)
            let note = parts(chord).root
            if note != typedParts.root {
                offer(before + note)
            }
        }
        return result
    }
    for kind in usualKinds {
        offer(typedParts.root + units(kind))
    }
    return result
}
