// A key's own chords and what a chord being typed might be, held to
// testdata/vectors/diatonic.json and completions.json.

import ChordRules
import Testing

@Suite("The key's chords, and completions")
struct SuggestionsTests {
    struct Diatonic: Decodable {
        struct Case: Decodable {
            let key: String
            let out: [String]
        }
        let cases: [Case]
    }

    @Test("diatonic.json: every case")
    func diatonic() throws {
        let file: Diatonic = try vector("diatonic.json")
        var tally = Tally("diatonic.json")
        for c in file.cases {
            tally.check(spell(Chords.diatonic(c.key)), spell(c.out), "diatonic(\(spell(c.key)))")
        }
        tally.settle()
    }

    struct Completions: Decodable {
        struct Case: Decodable {
            let typed: String
            let key: String
            let used: [String]
            let out: [String]
        }
        let cases: [Case]
    }

    @Test("completions.json: every case")
    func completions() throws {
        let file: Completions = try vector("completions.json")
        var tally = Tally("completions.json")
        for (n, c) in file.cases.enumerated() {
            tally.check(spell(Chords.completions(typed: c.typed, key: c.key, used: c.used)), spell(c.out),
                        "case \(n): completions(typed \(spell(c.typed)), key \(spell(c.key)), used \(spell(c.used)))")
        }
        tally.settle()
    }

    // Not a vector. The vectors have no two names that differ only in how an accented
    // letter is written; Swift calls such two equal, and the rules go by units.
    @Test("Mine, not a vector: two names that differ only in their units are two names")
    func namesAreComparedByUnits() {
        let offered = Chords.completions(typed: "", key: "", used: ["\u{E9}", "e\u{301}"])
        #expect(spell(offered) == spell(["\u{E9}", "e\u{301}"]))
    }
}
