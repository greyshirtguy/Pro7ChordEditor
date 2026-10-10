// Which key a song's chords are most likely in, and whether the key it states is to be
// doubted. Held to testdata/vectors/key-fit.json.
//
// NEW: Simple Presenter has nothing like it. The reference is tools/keyfit.py, which has
// the rule and its reasons at its top, and this is a port of its `likely`. What it is
// for is spec/08-repair.md: the old Windows editor could save a song with its key set to
// A flat major when the key was really something else, and a song typed by hand often
// names no key at all. Neither app can know the right key, but both can say "these
// chords look like G" and let the person decide.
//
// The rule, in short. A chord is a note and a kind (minor, diminished, or else major;
// sevenths, suspensions and a bass note are not looked at). A key has seven chords of its
// own, and a chord fits a key if it is one of them. A key's score is the share of the
// song's chords that fit it. The twenty-six keys a picker offers are scored. The stated
// key is doubtful only when the song has at least four chords, the best score is at
// least 0.75, and the stated key's score is more than 0.25 below it.
//
// Three things are as the Python has them and not as the rest of this module does, so
// that the two give the same answer for every input and not only for the vectors:
//
//   - A chord's name is read by the Python's own expression, which is not `readNote`:
//     its sharps or flats are a run of ONE sign ("#" or "b" or either music sign), and a
//     name with a line break in it is no chord.
//   - White space, for trimming a name, is Python's: Qt's list and U+001C to U+001F.
//   - A score is rounded to four places as Python's `round` does it, which goes by the
//     exact value of the number as the machine holds it. Scaling by ten thousand and
//     rounding does not always give the same, so it is done here by printing the number
//     to four places, which C does exactly, and reading it back. The comparisons are then
//     made on the rounded scores, as the Python makes them.

import Foundation

public enum KeyFit {
    public struct Answer: Sendable {
        /// Every offered key's share of the chords that fit it, to four places.
        public var scores: [String: Double]
        /// The keys with the best score, the likeliest first. Empty if no chord fits any.
        public var likeliest: [String]
        /// Whether the stated key is far enough from the likeliest to say so.
        public var doubtful: Bool
        /// The key to offer (when the stated one is doubtful, or none is stated), or "".
        public var suggested: String
    }

    /// `chords` is every chord of the song, one entry for each time it is used. `stated`
    /// is the key the song says it is in, or "" if it says none.
    public static func likely(chords: [String], stated: String) -> Answer {
        let offered = Chords.majorKeys() + Chords.minorKeys()
        let read = chords.compactMap { noteAndKind(units($0)) }

        var scores: [String: Double] = [:]
        var tonics: [String: NoteAndKind] = [:]
        for key in offered {
            let own = diatonic(units(key)).compactMap(noteAndKind)
            tonics[key] = own.first
            let fits = read.filter { own.contains($0) }.count
            scores[key] = read.isEmpty ? 0.0 : pythonRound4(Double(fits) / Double(read.count))
        }
        let best = scores.values.max() ?? 0.0

        // Which way the song's own chords are written, to tell F# from Gb.
        func written(with mark: UInt16) -> Int {
            chords.filter {
                let name = pythonStripped(units($0))
                return name.count >= 2 && noteLetters.contains(asciiRaised(name[0])) && name[1] == mark
            }.count
        }
        let flatsWritten = written(with: u("b"))
        let sharpsWritten = written(with: u("#"))

        // Among the best, the one whose own first chord (G in G, Em in E minor) is used
        // most comes first; then the major one; then the one spelt the way the song's
        // chords are spelt; then as a picker lists them.
        struct Order {
            var uses: Int, minor: Bool, against: Bool, index: Int
        }
        func order(_ index: Int) -> Order {
            let key = offered[index]
            let name = units(key)
            let uses = read.filter { $0 == tonics[key] }.count
            let second = name.count > 1 ? name[1] : 0
            let against = (second == u("b") && sharpsWritten > flatsWritten) || (second == u("#") && flatsWritten > sharpsWritten)
            return Order(uses: uses, minor: name.last == u("m"), against: against, index: index)
        }
        let likeliest = offered.indices
            .filter { scores[offered[$0]] == best && best > 0 }
            .map(order)
            .stablySorted { a, b in
                if a.uses != b.uses { return a.uses > b.uses }
                if a.minor != b.minor { return !a.minor }
                if a.against != b.against { return !a.against }
                return a.index < b.index
            }
            .map { offered[$0.index] }

        let isStated = !stated.utf16.isEmpty
        let statedScore = isStated ? (scores.first { units($0.key) == units(stated) }?.value ?? 0.0) : 0.0
        let enough = read.count >= 4 && best >= 0.75
        let doubtful = isStated && enough && statedScore < best - 0.25
        let suggested = (doubtful || !isStated) && enough ? (likeliest.first ?? "") : ""
        return Answer(scores: scores, likeliest: likeliest, doubtful: doubtful, suggested: suggested)
    }
}

/// A chord as this rule sees it: one of the twelve notes, and "m", "dim" or "" for major.
private struct NoteAndKind: Hashable {
    var pitch: Int
    var kind: String
}

/// Python's `str.strip`: Qt's white space, and the four separators U+001C to U+001F.
private func pythonStripped(_ text: Units) -> Units {
    func space(_ unit: UInt16) -> Bool { isSpace(unit) || (0x001C...0x001F).contains(unit) }
    var start = 0
    var end = text.count
    while start < end && space(text[start]) { start += 1 }
    while end > start && space(text[end - 1]) { end -= 1 }
    return Array(text[start..<end])
}

/// A chord's note and kind, or nothing for what does not start with a note. The
/// Python's expression, on the name with the white space off its ends:
///
///     ^([A-Ga-g])(#+|b+|♯+|♭+)?(.*)$
private func noteAndKind(_ chord: Units) -> NoteAndKind? {
    let name = pythonStripped(chord)
    guard let first = name.first, let letter = noteLetters.firstIndex(of: asciiRaised(first)) else { return nil }
    // (`.` is anything but a line break, so a name with one inside is no match.)
    guard !name.contains(Words.lineBreak) else { return nil }
    var i = 1
    var accidental = 0
    if i < name.count, [u("#"), u("b"), u("\u{266F}"), u("\u{266D}")].contains(name[i]) {
        let mark = name[i]
        while i < name.count && name[i] == mark {
            accidental += (mark == u("#") || mark == u("\u{266F}")) ? 1 : -1
            i += 1
        }
    }
    let pitch = ((semitones[letter] + accidental) % 12 + 12) % 12
    var rest = name.mid(i)
    // A bass note is not looked at: a letter, then sharps or flats, after the last slash.
    let slash = rest.lastIndexOf(u("/"))
    if slash >= 0 {
        let after = rest.mid(slash + 1)
        if let bass = after.first, noteLetters.contains(asciiRaised(bass)) {
            let marks = after.mid(1)
            if marks.isEmpty || marks.allSatisfy({ $0 == u("#") }) || marks.allSatisfy({ $0 == u("b") }) {
                rest = rest.left(slash)
            }
        }
    }
    let kind: String
    if rest.startsWith("dim") || rest.startsWith("\u{00B0}") {
        kind = "dim"
    } else if rest.startsWith("m") && !rest.startsWith("maj") {
        kind = "m"
    } else {
        kind = ""
    }
    return NoteAndKind(pitch: pitch, kind: kind)
}

/// Python's `round(x, 4)`.
private func pythonRound4(_ x: Double) -> Double {
    Double(String(format: "%.4f", x)) ?? x
}
