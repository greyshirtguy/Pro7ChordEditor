// From one key to another, and from one notation to another. Held to
// testdata/vectors/transposed.json, notated.json and shown.json.
//
// Moving a chord: each of its two notes is moved by as many LETTERS and as many
// SEMITONES as the keys' notes are apart, so that it is spelt as the new key spells it.
// C sharp minor in E becomes B flat minor in D flat, not A sharp minor. A spelling that
// would need a double sharp or a double flat gives way to the plain name of the same
// note. What says what kind of chord it is, is put back unchanged.

extension Chords {
    /// The chord as it is in another key. A chord that cannot be read, a key that is no
    /// key, or the same key twice, gives the chord back exactly as it was.
    public static func transposed(_ chord: String, from fromKey: String, to toKey: String) -> String {
        string(ChordRules.transposed(units(chord), from: units(fromKey), to: units(toKey)))
    }

    /// ProPresenter's four ways of writing a chord, numbered as its file numbers them.
    /// That is a setting of a stage layout's text box, not of the song, and nothing here
    /// writes it. The editors always show letters.
    public enum Notation: Int, Sendable, CaseIterable {
        /// As it is: "C#m7/E"
        case letters = 0
        /// By the note's place in the key's scale: "6m7/1" in E
        case numbers = 1
        /// The same in Roman numbers, small for a minor chord: "vi7/I"
        case numerals = 2
        /// By the notes' names in the Latin countries, where C is always Do: "Do#m7/Mi"
        case doReMi = 3
    }

    /// The chord, which is in `key`, in a notation.
    ///
    /// A GUESS, in the last three: ProPresenter does not say what exactly it writes and
    /// none of the files to hand shows it, so these are the forms musicians use. For a
    /// minor key the places are counted from its relative major (A minor's are C's),
    /// which is how number charts are usually written. They are not on the path of
    /// anything the editor saves.
    public static func notated(_ chord: String, key: String, notation: Notation) -> String {
        string(ChordRules.notated(units(chord), key: units(key), notation: notation))
    }

    /// What is drawn for a chord of the file: moved from the original key to the key
    /// asked for (if they differ and both are given), and then written in the notation.
    public static func shown(_ chord: String, originalKey: String, key: String, notation: Notation) -> String {
        let chord = units(chord), originalKey = units(originalKey), key = units(key)
        let move = !key.isEmpty && !originalKey.isEmpty && key != originalKey
        return string(ChordRules.notated(move ? ChordRules.transposed(chord, from: originalKey, to: key) : chord,
                                         key: move ? key : originalKey, notation: notation))
    }
}

func transposed(_ chord: Units, from fromKey: Units, to toKey: Units) -> Units {
    let from = noteOfKey(fromKey)
    let to = noteOfKey(toKey)
    let chordParts = parts(chord)
    guard from.valid, to.valid, chordParts.valid, fromKey != toKey else { return chord }
    var result = moved(readNote(chordParts.root).note, from: from, to: to).written + chordParts.quality
    if !chordParts.bass.isEmpty {
        result += [u("/")] + moved(readNote(chordParts.bass).note, from: from, to: to).written
    }
    return result
}

private let latin = ["Do", "Re", "Mi", "Fa", "Sol", "La", "Si"]
private let romanNumerals = ["I", "II", "III", "IV", "V", "VI", "VII"]

func notated(_ chord: Units, key: Units, notation: Chords.Notation) -> Units {
    let chordParts = parts(chord)
    guard notation != .letters, chordParts.valid else { return chord }
    let root = readNote(chordParts.root).note
    let bass = chordParts.bass.isEmpty ? Note() : readNote(chordParts.bass).note
    let slash = [u("/")]

    if notation == .doReMi {
        func name(_ note: Note) -> Units { units(latin[note.letter]) + marks(note.accidental) }
        return name(root) + chordParts.quality + (bass.valid ? slash + name(bass) : [])
    }

    let scale = scaleOf(key)
    guard scale.valid else { return chord }
    func place(_ note: Note, roman: Bool, small: Bool) -> Units {
        let (degree, accidental) = placeIn(note, key: scale)
        let number = roman ? romanNumerals[degree] : String(degree + 1)
        return marks(accidental) + units(small ? number.lowercased() : number)
    }
    if notation == .numbers {
        return place(root, roman: false, small: false) + chordParts.quality
            + (bass.valid ? slash + place(bass, roman: false, small: false) : [])
    }

    // Numerals: a minor chord is a small numeral and loses its "m"; a diminished one is
    // small too, with the little circle.
    var quality = chordParts.quality
    var small = false
    if quality.startsWith("dim") {
        small = true
        quality = [u("\u{00B0}")] + quality.mid(3)
    } else if minorQuality(quality) {
        small = true
        quality = quality.mid(quality.startsWith("min") ? 3 : 1)
    }
    return place(root, roman: true, small: small) + quality + (bass.valid ? slash + place(bass, roman: true, small: false) : [])
}
