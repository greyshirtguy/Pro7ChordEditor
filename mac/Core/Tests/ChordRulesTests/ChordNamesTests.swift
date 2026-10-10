// A chord's name, held to testdata/vectors/chord-names.json.

import ChordRules
import Testing

@Suite("Chord names")
struct ChordNamesTests {
    struct File: Decodable {
        struct Case: Decodable {
            let `in`: String
            let valid: Bool
            let root: String
            let quality: String
            let bass: String
            let tidied: String
            let couldBecome: Bool
            let isChord: Bool
        }
        let cases: [Case]
    }

    @Test("chord-names.json: every case")
    func everyCase() throws {
        let file: File = try vector("chord-names.json")
        var tally = Tally("chord-names.json")
        for c in file.cases {
            let name = spell(c.in)
            let parts = Chords.parts(c.in)
            tally.check(spell(parts.valid), spell(c.valid), "parts(\(name)).valid")
            tally.check(spell(parts.root), spell(c.root), "parts(\(name)).root")
            tally.check(spell(parts.quality), spell(c.quality), "parts(\(name)).quality")
            tally.check(spell(parts.bass), spell(c.bass), "parts(\(name)).bass")
            tally.check(spell(Chords.tidied(c.in)), spell(c.tidied), "tidied(\(name))")
            tally.check(spell(Chords.couldBecome(c.in)), spell(c.couldBecome), "couldBecome(\(name))")
            tally.check(spell(Chords.isChord(c.in)), spell(c.isChord), "isChord(\(name))")
        }
        tally.settle()
    }
}
