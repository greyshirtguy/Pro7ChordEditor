// Sharps and flats drawn as signs, held to testdata/vectors/signs.json.

import ChordRules
import Testing

@Suite("Sharps and flats drawn as signs")
struct SignsTests {
    struct File: Decodable {
        struct Case: Decodable {
            let about: String
            let `in`: String
            let out: String
        }
        let withSigns: [Case]
        let withoutSigns: [Case]
    }

    @Test("signs.json: every case of withSigns")
    func withSigns() throws {
        let file: File = try vector("signs.json")
        var tally = Tally("signs.json")
        for c in file.withSigns {
            tally.check(spell(Chords.withSigns(c.in)), spell(c.out), "withSigns(\(spell(c.in))), \(c.about)")
        }
        tally.settle()
    }

    @Test("signs.json: every case of withoutSigns")
    func withoutSigns() throws {
        let file: File = try vector("signs.json")
        var tally = Tally("signs.json")
        for c in file.withoutSigns {
            tally.check(spell(Chords.withoutSigns(c.in)), spell(c.out), "withoutSigns(\(spell(c.in))), \(c.about)")
        }
        tally.settle()
    }

    @Test("Mine, not a vector. A name drawn is as long as the name, in units, and reads back as it was")
    func asLongAsTheName() throws {
        let file: File = try vector("signs.json")
        for c in file.withSigns {
            let shown = Chords.withSigns(c.in)
            #expect(shown.utf16.count == c.in.utf16.count, "\(spell(c.in))")
            #expect(Array(Chords.withoutSigns(shown).utf16) == Array(Chords.withoutSigns(c.in).utf16), "\(spell(c.in))")
        }
    }
}
