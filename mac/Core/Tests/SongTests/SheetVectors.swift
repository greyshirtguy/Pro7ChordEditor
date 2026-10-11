// What the tests of the chord sheet's model share: the shape of
// testdata/vectors/sheet-model.json, and a stand-in for the song.
//
// The vectors were made by running the reference's own functions against a stand-in
// document (tools/make-vectors/sheet.js): one that keeps a song as the list of text
// boxes the real one hands over, notes every change it is asked for, and makes the
// change the way the real one does, so that the next thing done starts from the song as
// it then is. `StandInSong` is that stand-in again, written from that script. What a
// test compares is what the song was ASKED, which is the part a port has to get the
// same; and the song afterwards, which says the stand-in here is the one there.
//
// Strings are compared unit by unit, spelt out (`spelt`), never as Swift compares them:
// some of these hold U+200B and U+2001, which cannot be seen.

import ChordRules
import Foundation
import Song
import Testing
import TestData

func spelt(_ string: String) -> String {
    var out = "\""
    for unit in string.utf16 {
        switch unit {
        case 0x22: out += "\\\""
        case 0x5C: out += "\\\\"
        case 0x20...0x7E: out += String(UnicodeScalar(UInt8(unit)))
        default: out += "\\u{" + String(unit, radix: 16, uppercase: true) + "}"
        }
    }
    return out + "\""
}

func spelt(_ chords: [Chord]) -> String {
    "[" + chords.map { "\(spelt($0.name))@\($0.at)" }.joined(separator: ", ") + "]"
}

func spelt(_ spot: Spot) -> String { "row \(spot.row) at \(spot.at)" }

struct SheetVectors: Decodable {
    struct AChord: Decodable {
        let at: Int
        let name: String
        var chord: Chord { Chord(at: at, name: name) }
    }

    struct ASpot: Decodable {
        let row: Int
        let at: Int
        var spot: Spot { Spot(row: row, at: at) }
    }

    struct Box: Decodable {
        let row: Int
        let group: String
        let groupStart: Bool
        let element: String
        let text: String
        let chords: [AChord]
        let blank: Bool

        var textBox: TextBox {
            TextBox(slide: row, group: group, startsItsGroup: groupStart, element: element, words: Words(text),
                    chords: chords.map(\.chord), blank: blank)
        }
    }

    struct ASong: Decodable {
        let key: String
        let textBoxes: [Box]
    }

    struct ARow: Decodable {
        let kind: String
        let row: Int
        let name: String?
        let block: Int?
        let line: Int?
        let element: String?
        let text: String?
        let start: Int?
        let chords: [AChord]?
        let alone: Bool?
        let blank: Bool?
        let first: Bool?
        let count: Int?
        let last: Bool?
    }

    struct Rows: Decodable {
        let song: String
        let mostLines: Int
        let used: [String]
        let rows: [ARow]
    }

    struct WordStarts: Decodable {
        let text: String
        let out: [Int]
    }

    struct Stops: Decodable {
        let song: String
        let row: Int
        let usable: Bool
        let out: [Int]
    }

    struct StopAt: Decodable {
        let song: String
        let row: Int
        let at: Int
        let out: Int
    }

    struct Walk: Decodable {
        let song: String
        let start: ASpot?
        let keys: [String]
        let spots: [ASpot]
    }

    /// What the song was asked.
    struct Asked: Decodable {
        let ask: String
        let row: Int
        let element: String
        let chords: [AChord]?
        let line: Int?
        let names: [String]?
        let joined: Bool

        var spelt: String {
            if ask == "setChords" {
                return "setChords slide \(row) box \(SongTests.spelt(element)) \(SongTests.spelt((chords ?? []).map(\.chord))) joined \(joined)"
            }
            return "setChordsAlone slide \(row) box \(SongTests.spelt(element)) line \(line ?? -1) "
                + "[\((names ?? []).map { SongTests.spelt($0) }.joined(separator: ", "))] joined \(joined)"
        }
    }

    struct After: Decodable {
        let row: Int
        let element: String
        let text: String
        let chords: [AChord]

        var spelt: String { "slide \(row) box \(SongTests.spelt(element)) \(SongTests.spelt(text)) \(SongTests.spelt(chords.map(\.chord)))" }
    }

    struct Edit: Decodable {
        struct What: Decodable {
            struct Then: Decodable {
                let put: String?
                let at: Int?
                let take: Bool?
            }

            let put: String?
            let take: Bool?
            let key: Int?
            let move: ASpot?
            let copy: ASpot?
            let onRow: Int?
            let then: Then?
            let note: String?
        }

        let song: String
        let spot: ASpot
        let what: What
        let asked: [Asked]
        let spotAfter: ASpot
        let songAfter: [After]
        let answer: Bool?

        enum CodingKeys: String, CodingKey {
            case song, spot, what = "do", asked, spotAfter, songAfter, answer
        }
    }

    struct Typing: Decodable {
        struct Step: Decodable {
            struct What: Decodable {
                let spot: ASpot?
                let press: String?
                let typed: String?
                let finish: String?
                let click: ASpot?
                let settle: Bool?
            }

            struct Holds: Decodable {
                let text: String
                let selected: Bool
            }

            let step: What
            let asked: [Asked]
            let typing: Bool
            let typingAt: ASpot?
            let boxHolds: Holds?
            let spot: ASpot
        }

        let song: String
        let title: String
        let steps: [Step]
        let songAfter: [After]
    }

    struct CopyPaste: Decodable {
        struct OnAWord: Decodable {
            let word: Int
            let offset: Int
            let name: String
        }

        /// A copied line: its chords by word, or the names of a line of chords alone.
        enum Line: Decodable {
            case words([OnAWord])
            case alone([String])

            private struct Alone: Decodable {
                let alone: [String]
            }

            init(from decoder: Decoder) throws {
                let single = try decoder.singleValueContainer()
                if let words = try? single.decode([OnAWord].self) {
                    self = .words(words)
                } else {
                    self = .alone(try single.decode(Alone.self).alone)
                }
            }

            var spelt: String {
                switch self {
                case .words(let chords): "words [" + chords.map { "\(SongTests.spelt($0.name)) word \($0.word)+\($0.offset)" }.joined(separator: ", ") + "]"
                case .alone(let names): "alone [" + names.map { SongTests.spelt($0) }.joined(separator: ", ") + "]"
                }
            }
        }

        struct Copied: Decodable {
            let lines: [Line]
        }

        let song: String
        let title: String
        let copyAt: ASpot
        let wholeGroup: Bool
        let copied: Copied
        let said: [String]
        let pasteAt: ASpot
        let asked: [Asked]
        let songAfter: [After]
    }

    let songs: [String: ASong]
    let rows: [Rows]
    let wordStarts: [WordStarts]
    let stops: [Stops]
    let stopAt: [StopAt]
    let walks: [Walk]
    let edits: [Edit]
    let typing: [Typing]
    let copyPaste: [CopyPaste]

    static func read() throws -> SheetVectors {
        try JSONDecoder().decode(SheetVectors.self, from: TestData.vector("sheet-model.json"))
    }

    /// A sheet on one of the vectors' songs, and the stand-in it asks.
    func sheet(_ name: String) throws -> (sheet: ChordSheet, song: StandInSong) {
        let made = try #require(songs[name], "the vectors have no song \(name)")
        let song = StandInSong(made.textBoxes.map(\.textBox), key: made.key)
        return (ChordSheet(song: song), song)
    }
}

/// The song's stand-in, as tools/make-vectors/sheet.js has it.
final class StandInSong: SheetSong {
    static let zeroWidth: UInt16 = 0x200B
    static let quad: UInt16 = 0x2001

    var textBoxes: [TextBox]
    var key: String
    /// What it was asked, spelt out as `SheetVectors.Asked` spells what is expected.
    var asked: [String] = []
    /// The text boxes that were added for a slide's chords: they go again when the
    /// chords do.
    private var added: [String] = []
    /// What to refuse everything with, if anything.
    var refusing: (any Error)?

    init(_ textBoxes: [TextBox], key: String) {
        self.textBoxes = textBoxes
        self.key = key
    }

    var usedChords: [String] {
        textBoxes.flatMap { $0.chords.map(\.name) }
    }

    var after: [String] {
        textBoxes.map { "slide \($0.slide) box \(spelt($0.element)) \(spelt($0.words.string)) \(spelt($0.chords))" }
    }

    private func find(_ slide: Int, _ element: String) -> Int? {
        textBoxes.firstIndex { $0.slide == slide && Array($0.element.utf16) == Array(element.utf16) }
    }

    /// A text box's words are nothing to a song: none, or only white space, with none
    /// of the characters that chords with no words hang on.
    private func noWords(_ text: Words) -> Bool {
        text.units.allSatisfy { unit in
            unit != Self.zeroWidth && unit != Self.quad
                && ([0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x20, 0xA0, 0x1680, 0x2028, 0x2029, 0x202F, 0x205F, 0x3000, 0xFEFF].contains(unit)
                    || (0x2000...0x200A).contains(unit))
        }
    }

    func setChords(slide: Int, element: String, _ chords: [Chord], joined: Bool) throws {
        asked.append("setChords slide \(slide) box \(spelt(element)) \(spelt(chords)) joined \(joined)")
        if let refusing { throw refusing }
        let index = try #require(find(slide, element))
        textBoxes[index].chords = Chords.tidy(textBoxes[index].words, chords.map { Chord(at: $0.at, name: Chords.tidied($0.name)) })
    }

    func setChordsAlone(slide: Int, element: String, line: Int, names: [String], joined: Bool) throws {
        asked.append("setChordsAlone slide \(slide) box \(spelt(element)) line \(line) [\(names.map { spelt($0) }.joined(separator: ", "))] joined \(joined)")
        if let refusing { throw refusing }
        let index = try #require(find(slide, element))
        var block = textBoxes[index]
        defer { textBoxes[index] = block }
        // A blank slide's box: nothing there and nothing to put changes nothing. Its
        // first chords become all it holds, on zero-width spaces; and a slide with no
        // text box (no id) is given one for them.
        if block.chords.isEmpty && noWords(block.words) {
            if names.isEmpty { return }
            if block.element.isEmpty {
                block.element = "(a text box added for the chords)"
                added.append(block.element)
            }
            block.words = Chords.placeholders(names.count, standIn: Self.zeroWidth)
            block.chords = names.enumerated().map { Chord(at: $0.offset * 2, name: Chords.tidied($0.element)) }
            block.blank = false
            return
        }
        var lines = block.words.lines
        let old = lines[line]
        let standIn: UInt16 = !old.contains(Self.zeroWidth) && old.contains(0x20) ? 0x20 : Self.zeroWidth
        let made = Chords.placeholders(names.count, standIn: standIn)
        let start = lines[..<line].reduce(0) { $0 + $1.length + 1 }
        let shift = made.length - old.length
        var kept: [Chord] = []
        for chord in block.chords {
            if chord.at < start {
                kept.append(chord)
            } else if chord.at > start + old.length {
                kept.append(Chord(at: chord.at + shift, name: chord.name))
            }
        }
        lines[line] = made
        block.words = Words(units: Array(lines.map(\.units).joined(separator: [Words.lineBreak])))
        for (n, name) in names.enumerated() {
            kept.append(Chord(at: start + n * 2, name: Chords.tidied(name)))
        }
        block.chords = Chords.tidy(block.words, kept)
        // With its last chord gone a slide that has nothing else on it is blank again:
        // handed over with no words, and without the text box if that was only added
        // for the chords.
        if block.chords.isEmpty && noWords(block.words) && textBoxes.filter({ $0.slide == block.slide }).count == 1 {
            block.words = Words()
            block.blank = true
            if added.contains(block.element) {
                block.element = ""
            }
        }
    }
}

/// Letters of a fixed width, as testdata/vectors/layout.json was made with: every
/// letter of the words ten wide, every letter of a chord eight, and a bubble seven
/// either side of its name.
struct FixedWidths: SheetMeasure {
    func advance(row: Int, units: Int) -> Double { 10 * Double(units) }
    func bubble(_ name: String) -> Double { 8 * Double(name.utf16.count) + 14 }
}
