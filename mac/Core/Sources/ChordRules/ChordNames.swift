// A chord's name: taking it apart, tidying one that was typed, and saying whether what
// is being typed could be one. Held to testdata/vectors/chord-names.json.
//
// A chord that is in a file already is never held to `isChord`. "C /// | Am ///" is not
// a chord, and is kept, shown, moved and copied as it is (spec/02-chord-rules.md).

/// A chord's name taken apart, in units: what the public `parts` is made from.
struct UnitParts {
    var root: Units = []
    var quality: Units = []
    var bass: Units = []
    var valid = false
}

func parts(_ chord: Units) -> UnitParts {
    var result = UnitParts()
    let (root, length) = readNote(chord)
    guard root.valid else { return result }
    result.root = root.written
    var rest = chord.mid(length)
    let slash = rest.lastIndexOf(u("/"))
    if slash >= 0 {
        let after = rest.mid(slash + 1)
        let (bass, bassLength) = readNote(after)
        // "C6/9" has a slash and no bass note.
        if bass.valid && bassLength == after.count {
            result.bass = bass.written
            rest = rest.left(slash)
        }
    }
    result.quality = rest
    result.valid = true
    return result
}

func couldBecome(_ typed: Units) -> Bool {
    if typed.isEmpty { return true }
    let (note, length) = readNote(typed)
    guard note.valid else { return false }
    return typed[length...].allSatisfy(chordCharacter)
}

func tidied(_ typed: Units) -> Units {
    let chord = typed.trimmed()
    let (root, length) = readNote(chord)
    guard root.valid else { return chord }
    var rest = chord.mid(length)
    let slash = rest.lastIndexOf(u("/"))
    if slash >= 0 {
        let after = rest.mid(slash + 1)
        let (bass, bassLength) = readNote(after)
        if bass.valid && bassLength == after.count {
            rest = rest.left(slash + 1) + bass.written
        }
    }
    return root.written + rest
}

extension Chords {
    /// A chord's name taken apart.
    public struct Parts: Hashable, Sendable {
        /// "F#"
        public var root: String
        /// "m7", or "" for a plain major chord
        public var quality: String
        /// "A#", or "" for none
        public var bass: String
        /// Whether it starts with a note. If not, the rest is empty.
        public var valid: Bool
    }

    /// Takes a chord's name apart. Not valid if it does not start with a note. The bass
    /// is what follows the LAST slash, if that is a note and nothing more.
    public static func parts(_ chord: String) -> Parts {
        let found = ChordRules.parts(units(chord))
        return Parts(root: string(found.root), quality: string(found.quality), bass: string(found.bass), valid: found.valid)
    }

    /// Whether this could be the start of a chord that is being typed: empty, or a note,
    /// or a note and anything after it that a chord's name is written with. It is what
    /// the editors let through as a chord is typed.
    public static func couldBecome(_ typed: String) -> Bool {
        ChordRules.couldBecome(units(typed))
    }

    /// A typed chord tidied: spaces off its ends, the note letters as capitals, and "b"
    /// and "#" for a flat and a sharp ("f#M7/a" is "F#M7/A": what is between the notes
    /// is the writer's business).
    public static func tidied(_ typed: String) -> String {
        string(ChordRules.tidied(units(typed)))
    }

    /// Whether a NEWLY TYPED name is to be put on as a chord: it starts with a note, and
    /// is written with nothing a chord's name is not written with.
    public static func isChord(_ typed: String) -> Bool {
        let typed = units(typed)
        return ChordRules.parts(typed).valid && ChordRules.couldBecome(typed)
    }
}
