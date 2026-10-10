// Chords as ChordPro text: each chord in square brackets before the character it is
// played on, "[G]Amazing [C]grace". And the stand-ins a chord with no words under it
// hangs on. Held to testdata/vectors/chordpro.json.
//
// Two readers, for two jobs, and they are not the same on purpose:
//
//   fromChordPro   reads a line of a ChordPro FILE. A bracket holding what could not be
//                  a chord is dropped ("[*Coda]"), a chord after the last word hangs on
//                  the last character, and a line of chords alone becomes stand-ins.
//   chordsOf       reads what was typed in the ChordPro EDITOR. Every bracket that is not
//                  empty is a chord, at the place it has among the words as typed, and no
//                  stand-ins are made. `withoutChords` is its other half: the words alone,
//                  which the editor compares with the words it began with.
//
// A line of chords alone. Multitracks writes one (an intro, a turnaround) as one
// character for each chord with a wide space between them, and hangs each chord on its
// character. In the files to hand the character is a zero-width space (U+200B) in some
// songs and a plain space in others, and what is between is always an em quad (U+2001);
// a single chord on a slide of its own hangs on one space. A song made here from a
// ChordPro file gets zero-width spaces, and a line that is changed keeps whichever it
// had (`standIn`).
//
// White space here is Qt's (Units.swift). A zero-width space is not white space there,
// which is why `isPlaceholders` names it separately. It IS white space to Foundation, so
// nothing here may trim or test with Foundation's sets.

private let openBracket = u("[")
private let closeBracket = u("]")
private let space = u(" ")
/// U+200B, the zero-width space: what a chord with no words hangs on.
let zeroWidthSpace: UInt16 = 0x200B
/// U+2001, the em quad: what stands between two such.
let emQuad: UInt16 = 0x2001

extension Chords {
    /// A line of words and its chords (each `at` counted along the line).
    public struct Line: Hashable, Sendable {
        public var text: Words
        public var chords: [Chord]

        public init(text: Words, chords: [Chord] = []) {
            self.text = text
            self.chords = chords
        }
    }

    /// A line as ChordPro writes it. A line of chords alone is written as its chords
    /// with one space between, "[C] [G] [Am]", which is how such a line is written by
    /// hand; the stand-ins they hang on are the file's business.
    public static func toChordPro(_ line: Line) -> String {
        string(ChordRules.toChordPro(line.text.units, line.chords))
    }

    /// A whole text box: each line so, joined by line breaks. A chord belongs to the
    /// line its place is in.
    public static func toChordPro(_ text: Words, _ chords: [Chord]) -> String {
        var lines: [Units] = []
        var start = 0
        for textLine in text.units.split(at: Words.lineBreak) {
            let own = chords
                .filter { $0.at >= start && $0.at < start + max(1, textLine.count) }
                .map { Chord(at: $0.at - start, name: $0.name) }
            lines.append(ChordRules.toChordPro(textLine, own))
            start += textLine.count + 1
        }
        return string(Units(lines.joined(separator: [Words.lineBreak])))
    }

    /// A line of a ChordPro FILE read.
    public static func fromChordPro(_ line: String) -> Line {
        ChordRules.fromChordPro(units(line))
    }

    /// The chords in ChordPro text typed in the editor, each at the place it has among
    /// the words as typed.
    public static func chordsOf(_ chordPro: String) -> [Chord] {
        chordsOf(Words(units: units(chordPro)))
    }

    /// The same, of text held as its units: what the editor asks, which counts along
    /// what is typed as the file counts along a song's words.
    public static func chordsOf(_ chordPro: Words) -> [Chord] {
        let chordPro = chordPro.units
        var result: [Chord] = []
        var at = 0
        var i = 0
        while i < chordPro.count {
            let close = chordPro[i] == openBracket ? chordPro.indexOf(closeBracket, from: i) : -1
            if close > i {
                let inside = ChordRules.tidied(chordPro.mid(i + 1, close - i - 1))
                if !inside.isEmpty {
                    result.append(Chord(at: at, name: string(inside)))
                }
                i = close + 1
                continue
            }
            at += 1
            i += 1
        }
        return result
    }

    /// ChordPro text with its chords taken out: the words alone. Every "[...]" that has
    /// no bracket and no line break inside it goes.
    public static func withoutChords(_ chordPro: String) -> String {
        withoutChords(Words(units: units(chordPro))).string
    }

    /// The same, of text held as its units. The editor holds what is typed against a
    /// song's words unit for unit, which two Strings that look alike need not be.
    public static func withoutChords(_ chordPro: Words) -> Words {
        let chordPro = chordPro.units
        var result: Units = []
        var i = 0
        while i < chordPro.count {
            if chordPro[i] == openBracket {
                var j = i + 1
                while j < chordPro.count && chordPro[j] != openBracket && chordPro[j] != closeBracket && chordPro[j] != Words.lineBreak {
                    j += 1
                }
                if j < chordPro.count && chordPro[j] == closeBracket {
                    i = j + 1
                    continue
                }
            }
            result.append(chordPro[i])
            i += 1
        }
        return Words(units: result)
    }

    /// That many stand-ins with an em quad between them.
    public static func placeholders(_ count: Int, standIn: UInt16 = 0x200B) -> Words {
        Words(units: ChordRules.placeholders(count, standIn: standIn))
    }

    /// Whether a line is one of chords alone: not empty, and nothing in it but
    /// zero-width spaces and white space. (A line of plain spaces counts. Whether it is
    /// a line of chords or only a stray space in a text box is for whoever asks to tell,
    /// by whether it has chords.)
    public static func isPlaceholders(_ line: Words) -> Bool {
        ChordRules.isPlaceholders(line.units)
    }
}

func placeholders(_ count: Int, standIn: UInt16 = zeroWidthSpace) -> Units {
    var result: Units = []
    for i in 0..<max(0, count) {
        if i > 0 { result.append(emQuad) }
        result.append(standIn)
    }
    return result
}

func isPlaceholders(_ line: Units) -> Bool {
    !line.isEmpty && line.allSatisfy { $0 == zeroWidthSpace || isSpace($0) }
}

func toChordPro(_ text: Units, _ chords: [Chord]) -> Units {
    var result: Units = []
    let ordered = chords.stablySorted { $0.at < $1.at }
    var next = 0
    let alone = isPlaceholders(text)
    for i in 0...text.count {
        while next < ordered.count && ordered[next].at <= i {
            if alone && !result.isEmpty { result.append(space) }
            result += [openBracket] + units(ordered[next].name) + [closeBracket]
            next += 1
        }
        if i < text.count && !alone {
            result.append(text[i])
        }
    }
    return result
}

func fromChordPro(_ line: Units) -> Chords.Line {
    var text: Units = []
    var chords: [Chord] = []
    var i = 0
    while i < line.count {
        let close = line[i] == openBracket ? line.indexOf(closeBracket, from: i) : -1
        if close > i {
            let inside = tidied(line.mid(i + 1, close - i - 1))
            if parts(inside).valid && couldBecome(inside) {
                chords.append(Chord(at: text.count, name: string(inside)))
            }
            i = close + 1
            continue
        }
        text.append(line[i])
        i += 1
    }
    // Chords with no words: one stand-in each.
    if text.trimmed().isEmpty && !chords.isEmpty {
        for n in chords.indices {
            chords[n].at = n * 2
        }
        return Chords.Line(text: Words(units: placeholders(chords.count)), chords: chords)
    }
    // A chord after the last word hangs on the last character, there being none after it.
    for n in chords.indices {
        chords[n].at = min(chords[n].at, max(0, text.count - 1))
    }
    return Chords.Line(text: Words(units: text), chords: chords)
}
