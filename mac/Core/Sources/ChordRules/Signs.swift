// Sharps and flats drawn as the music signs, ♯ and ♭, where they are clearly a sharp
// or a flat: how the chord sheet draws a chord's name. Held to
// testdata/vectors/signs.json.
//
// Not from chords.cpp: new here, the maintainer's (2026-10-14), "just a visual skin -
// not changing the underlying data". Its reference is tools/signs.py, whose head says
// the rule and why each part of it is there; spec/02-chord-rules.md, "Sharps and flats
// drawn as signs", says it too.
//
// What `withSigns` gives is only ever drawn. Nothing that is written, copied or carried
// is made from it, and the ChordPro text never has it (spec/03-chord-sheet.md, rule 10a).
// A "#" and its sign are one unit each, and so are a "b" and its, so a name drawn is as
// long as the name, and a place in one is the same place in the other: the caret in the
// box a chord is typed in never moves for a sign (rule 30a).

private let sharp = u("#")
private let flat = u("b")
private let musicSharp = u("\u{266F}")
private let musicFlat = u("\u{266D}")

/// Whether a unit is one of the capitals A to G.
private func isCapitalNote(_ unit: UInt16) -> Bool {
    unit >= u("A") && unit <= u("G")
}

/// The `#` and `b` of the units from `from` up to `to` as their signs.
private func signed(_ name: inout Units, from: Int, to: Int) {
    for i in from..<to {
        if name[i] == sharp { name[i] = musicSharp }
        if name[i] == flat { name[i] = musicFlat }
    }
}

func withSigns(_ name: Units) -> Units {
    // Only a chord, as a newly typed one would have to be, whose root is a capital: a
    // small letter may be no note at all ("b3/b5", a chord in numbers).
    guard let first = name.first, isCapitalNote(first), parts(name).valid, couldBecome(name) else {
        return name
    }
    var shown = name
    let (_, root) = readNote(name)
    signed(&shown, from: 1, to: root)
    // The bass, as `parts` finds one: after the last slash, a note and nothing more.
    var end = name.count
    let slash = name.mid(root).lastIndexOf(u("/"))
    if slash >= 0 {
        let at = root + slash + 1
        let (bass, length) = readNote(name.mid(at))
        if bass.valid && at + length == name.count {
            end = root + slash
            if isCapitalNote(name[at]) {
                signed(&shown, from: at + 1, to: name.count)
            }
        }
    }
    // Between the notes, a "#" or "b" with a digit 0 to 9 straight after it.
    for i in root..<end where (name[i] == sharp || name[i] == flat) && i + 1 < name.count && isASCIIDigit(name[i + 1]) {
        shown[i] = name[i] == sharp ? musicSharp : musicFlat
    }
    return shown
}

func withoutSigns(_ text: Units) -> Units {
    text.map { $0 == musicSharp ? sharp : $0 == musicFlat ? flat : $0 }
}

extension Chords {
    /// A chord's name as the chord sheet draws it: "F♯m7", "B♭", "C7♭9", "E♭/G". The
    /// root's sharps and flats, the bass's, and between them a "#" or "b" that has a
    /// digit after it, drawn as the music signs; only for a name that is a chord whose
    /// root is a capital. Anything else is given back as it is.
    public static func withSigns(_ name: String) -> String {
        string(ChordRules.withSigns(units(name)))
    }

    /// The other way: every ♯ as "#" and every ♭ as "b". What the box a chord is typed in
    /// holds, while it shows the signs, is read so before anything is done with it.
    public static func withoutSigns(_ text: String) -> String {
        string(ChordRules.withoutSigns(units(text)))
    }
}
