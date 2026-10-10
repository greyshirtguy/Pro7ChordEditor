// A text box's chords in order, as ranges, and carried along, held to
// testdata/vectors/tidy.json, ranges.json and carried.json.

import ChordRules
import Testing

@Suite("A box's chords")
struct ChordsInABoxTests {
    struct Tidy: Decodable {
        struct Case: Decodable {
            let text: String
            let chords: [VectorChord]
            let out: [VectorChord]
        }
        let cases: [Case]
    }

    @Test("tidy.json: every case")
    func tidy() throws {
        let file: Tidy = try vector("tidy.json")
        var tally = Tally("tidy.json")
        for (n, c) in file.cases.enumerated() {
            tally.check(spell(Chords.tidy(Words(c.text), c.chords.map(\.chord))), spell(c.out),
                        "case \(n): tidy(\(spell(c.text)), \(spell(c.chords)))")
        }
        tally.settle()
    }

    struct Ranges: Decodable {
        struct Range: Decodable {
            let start: Int
            let end: Int
            let name: String
        }
        struct Case: Decodable {
            let text: String
            let chords: [VectorChord]
            let out: [Range]
        }
        let cases: [Case]
    }

    @Test("ranges.json: every case")
    func ranges() throws {
        let file: Ranges = try vector("ranges.json")
        var tally = Tally("ranges.json")
        for (n, c) in file.cases.enumerated() {
            let expected = c.out.map { ChordRange(start: $0.start, end: $0.end, name: $0.name) }
            tally.check(spell(Chords.ranges(Words(c.text), c.chords.map(\.chord))), spell(expected),
                        "case \(n): ranges(\(spell(c.text)), \(spell(c.chords)))")
        }
        tally.settle()
    }

    struct Carried: Decodable {
        struct Case: Decodable {
            let before: String
            let after: String
            let chords: [VectorChord]
            let out: [VectorChord]
        }
        let cases: [Case]
    }

    @Test("carried.json: every case")
    func carried() throws {
        let file: Carried = try vector("carried.json")
        var tally = Tally("carried.json")
        for (n, c) in file.cases.enumerated() {
            tally.check(spell(Chords.carried(before: Words(c.before), after: Words(c.after), c.chords.map(\.chord))),
                        spell(c.out),
                        "case \(n): carried(\(spell(c.before)) to \(spell(c.after)), \(spell(c.chords)))")
        }
        tally.settle()
    }

    // ---- Not vectors.
    //
    // None of the three files above has a character from beyond the first 65,536 in it,
    // so they would pass if places were counted in Swift's characters. These hold the
    // counting to UTF-16 units (AGENTS.md; spec/01-file-format.md). What is expected was
    // worked out by hand from the rules, with the party popper counting as two:
    //
    //   J o y _ 🎉 🎉 _ t o \n t  h  e  _  w  o  r  l  d
    //   0 1 2 3 4  5  6 7 8 9  10 11 12 13 14 15 16 17 18
    static let joy = Words("Joy \u{1F389} to\nthe world")

    @Test("Mine, not a vector: an emoji is two places")
    func anEmojiIsTwoPlaces() {
        #expect(Self.joy.length == 19)
        // On the line break: to the unit before it. Past the end: to the last.
        let tidied = Chords.tidy(Self.joy, [Chord(at: 9, name: "D"), Chord(at: 500, name: "A"), Chord(at: 7, name: "G")])
        #expect(tidied == [Chord(at: 7, name: "G"), Chord(at: 8, name: "D"), Chord(at: 18, name: "A")])

        // To the next chord, or to the end of the line: which is at 9, not at 8.
        #expect(Chords.ranges(Self.joy, [Chord(at: 0, name: "G"), Chord(at: 7, name: "C"), Chord(at: 10, name: "D")]) == [
            ChordRange(start: 0, end: 7, name: "G"), ChordRange(start: 7, end: 9, name: "C"), ChordRange(start: 10, end: 19, name: "D"),
        ])

        // A second emoji put in before a chord moves it two places.
        let carried = Chords.carried(before: "Joy \u{1F389} to the world", after: "Joy \u{1F389}\u{1F389} to the world", [Chord(at: 7, name: "G")])
        #expect(carried == [Chord(at: 9, name: "G")])
    }
}
