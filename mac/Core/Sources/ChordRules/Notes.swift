// A note, and the arithmetic of notes that the rules of keys and chords share.
//
// A note is a letter and how far it is sharpened. Its pitch is one of twelve, but the
// letter is kept, because moving a chord to another key is done by letters as well as
// by semitones, so that the chord is spelt as the new key spells it (Transposing.swift).
//
// Ported from the unnamed namespace at the top of reference/simple-presenter/src/
// chords.cpp, function for function.

/// The seven letters from C, and how many semitones above C each is.
let noteLetters: Units = units("CDEFGAB")
let semitones = [0, 2, 4, 5, 7, 9, 11]
/// The plain name of each of the twelve notes, written with sharps and with flats.
let sharpNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"]
let flatNames = ["C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B"]

private let sharp = u("#")
private let flat = u("b")
private let musicSharp = u("\u{266F}")
private let musicFlat = u("\u{266D}")

struct Note {
    /// 0 for C to 6 for B; -1 for what is no note.
    var letter = -1
    /// How far it is sharpened; flattened, below nothing.
    var accidental = 0

    var valid: Bool { letter >= 0 }
    var pitch: Int { ((semitones[letter] + accidental) % 12 + 12) % 12 }

    /// "F#", "Bb", "C": the letter as a capital, with "#" or "b" for each step.
    var written: Units {
        var name = [noteLetters[letter]]
        if accidental > 0 { name += Units(repeating: sharp, count: accidental) }
        if accidental < 0 { name += Units(repeating: flat, count: -accidental) }
        return name
    }
}

/// Reads a note from the start of `text` and says how many units it took: a letter A to
/// G of either case, then sharps ("#" or the music sign, any number) or else flats ("b"
/// or the music sign, any number).
///
/// (The reference raises the first character with Qt's `toUpper` and looks for it among
/// the seven. No character but the small letters a to g is raised to one of those, so
/// asking for the fourteen letters outright is the same thing.)
func readNote(_ text: Units) -> (note: Note, length: Int) {
    var note = Note()
    guard let first = text.first, let letter = noteLetters.firstIndex(of: asciiRaised(first)) else {
        return (note, 0)
    }
    note.letter = letter
    var i = 1
    while i < text.count && (text[i] == sharp || text[i] == musicSharp) {
        note.accidental += 1
        i += 1
    }
    if i == 1 {
        while i < text.count && (text[i] == flat || text[i] == musicFlat) {
            note.accidental -= 1
            i += 1
        }
    }
    return (note, i)
}

/// The note a key is named for: read from the start of its name, major or minor.
func noteOfKey(_ key: Units) -> Note {
    readNote(key).note
}

/// The major key whose scale a key's places are counted on: itself, or for a minor key
/// the major key a minor third above it.
func scaleOf(_ key: Units) -> Note {
    let note = noteOfKey(key)
    guard note.valid, keyIsMinor(key) else { return note }
    var major = Note()
    major.letter = (note.letter + 2) % 7
    var rise = semitones[major.letter] - semitones[note.letter]
    if rise < 0 { rise += 12 }
    major.accidental = note.accidental + 3 - rise
    return major
}

/// The nearest way round the twelve: B sharp is one above B, not eleven below it.
func nearest(_ accidental: Int) -> Int {
    var accidental = accidental
    while accidental > 6 { accidental -= 12 }
    while accidental < -6 { accidental += 12 }
    return accidental
}

/// A note moved by as many letters and semitones as `to` is above `from`.
func moved(_ note: Note, from: Note, to: Note) -> Note {
    let steps = ((to.letter - from.letter) % 7 + 7) % 7
    let rise = ((to.pitch - from.pitch) % 12 + 12) % 12
    var result = Note()
    result.letter = (note.letter + steps) % 7
    let pitch = (note.pitch + rise) % 12
    result.accidental = nearest(pitch - semitones[result.letter])
    if abs(result.accidental) > 1 {
        // No double sharps or flats: the note's plain name, in sharps for a key that is
        // written with them and in flats otherwise.
        let sharps = to.accidental > 0 || (to.accidental == 0 && units("GDAEB").contains(noteLetters[to.letter]))
        result = readNote(units(sharps ? sharpNames[pitch] : flatNames[pitch])).note
    }
    return result
}

/// A note's place in a major key's scale (0 to 6) and how far it is sharpened from the
/// note the scale has there.
func placeIn(_ note: Note, key: Note) -> (degree: Int, accidental: Int) {
    let degree = ((note.letter - key.letter) % 7 + 7) % 7
    let inScale = (key.pitch + semitones[degree]) % 12
    return (degree, nearest(note.pitch - inScale))
}

/// "##" for 2, "b" for -1.
func marks(_ accidental: Int) -> Units {
    accidental > 0 ? Units(repeating: sharp, count: accidental) : Units(repeating: flat, count: -accidental)
}

/// Whether what follows a chord's root says it is minor: it starts with "m", and not
/// with "maj".
func minorQuality(_ quality: Units) -> Bool {
    quality.startsWith("m") && !quality.startsWith("maj")
}

/// What a chord's name is written with after its root.
func chordCharacter(_ unit: UInt16) -> Bool {
    isLetterOrNumber(unit) || units("#/+-()\u{00B0}\u{00F8}^.,\u{266F}\u{266D}\u{0394}").contains(unit)
}
