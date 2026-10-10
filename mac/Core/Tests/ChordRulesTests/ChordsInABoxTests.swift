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

    // (For a day this test made an allowance for one case, made before the rule for a
    // chord on a line of chords alone was decided. The file has been made again since,
    // from a Simple Presenter that has the rule, and holds it: three chords on such a
    // line are 0 to 1, 2 to 3 and 4 to 5.)
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


}
