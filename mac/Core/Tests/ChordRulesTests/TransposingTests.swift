// Moving a chord to another key and writing it in another notation, held to
// testdata/vectors/transposed.json, notated.json and shown.json.

import ChordRules
import Testing

@Suite("Transposing and notating")
struct TransposingTests {
    struct Transposed: Decodable {
        struct Row: Decodable {
            let from: String
            let to: String
            let out: [String]
        }
        struct Odd: Decodable {
            let chord: String
            let from: String
            let to: String
            let out: String
        }
        let chords: [String]
        let rows: [Row]
        let odd: [Odd]
    }

    @Test("transposed.json: every pair of keys for every chord, and the odd ones")
    func transposed() throws {
        let file: Transposed = try vector("transposed.json")
        var tally = Tally("transposed.json")
        for row in file.rows {
            try #require(row.out.count == file.chords.count)
            for (chord, expected) in zip(file.chords, row.out) {
                tally.check(spell(Chords.transposed(chord, from: row.from, to: row.to)), spell(expected),
                            "transposed(\(spell(chord)), from \(spell(row.from)), to \(spell(row.to)))")
            }
        }
        for odd in file.odd {
            tally.check(spell(Chords.transposed(odd.chord, from: odd.from, to: odd.to)), spell(odd.out),
                        "odd: transposed(\(spell(odd.chord)), from \(spell(odd.from)), to \(spell(odd.to)))")
        }
        tally.settle()
    }

    struct Notated: Decodable {
        struct Row: Decodable {
            let key: String
            let notation: Int
            let out: [String]
        }
        struct Kind: Decodable {
            let chord: String
            let key: String
            let notation: Int
            let out: String
        }
        let chords: [String]
        let rows: [Row]
        let kinds: [Kind]
    }

    @Test("notated.json: every key and notation for every chord, and the kinds")
    func notated() throws {
        let file: Notated = try vector("notated.json")
        var tally = Tally("notated.json")
        for row in file.rows {
            try #require(row.out.count == file.chords.count)
            let notation = try #require(Chords.Notation(rawValue: row.notation))
            for (chord, expected) in zip(file.chords, row.out) {
                tally.check(spell(Chords.notated(chord, key: row.key, notation: notation)), spell(expected),
                            "notated(\(spell(chord)), key \(spell(row.key)), \(notation))")
            }
        }
        for kind in file.kinds {
            let notation = try #require(Chords.Notation(rawValue: kind.notation))
            tally.check(spell(Chords.notated(kind.chord, key: kind.key, notation: notation)), spell(kind.out),
                        "kinds: notated(\(spell(kind.chord)), key \(spell(kind.key)), \(notation))")
        }
        tally.settle()
    }

    struct Shown: Decodable {
        struct Case: Decodable {
            let chord: String
            let originalKey: String
            let key: String
            let notation: Int
            let out: String
        }
        let cases: [Case]
    }

    @Test("shown.json: every case")
    func shown() throws {
        let file: Shown = try vector("shown.json")
        var tally = Tally("shown.json")
        for c in file.cases {
            let notation = try #require(Chords.Notation(rawValue: c.notation))
            tally.check(spell(Chords.shown(c.chord, originalKey: c.originalKey, key: c.key, notation: notation)), spell(c.out),
                        "shown(\(spell(c.chord)), original \(spell(c.originalKey)), key \(spell(c.key)), \(notation))")
        }
        tally.settle()
    }
}
