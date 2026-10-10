// Keys, held to testdata/vectors/keys.json.

import ChordRules
import Testing

@Suite("Keys")
struct KeysTests {
    struct File: Decodable {
        struct Name: Decodable {
            let number: Int
            let major: String
            let minor: String
        }
        struct Number: Decodable {
            let key: String
            let number: Int
            let minor: Bool
        }
        let names: [Name]
        let outOfRange: [Name]
        let numbers: [Number]
        let majorKeys: [String]
        let minorKeys: [String]
    }

    @Test("keys.json: every case")
    func everyCase() throws {
        let file: File = try vector("keys.json")
        var tally = Tally("keys.json")
        for name in file.names + file.outOfRange {
            tally.check(spell(Chords.keyName(name.number, minor: false)), spell(name.major), "keyName(\(name.number), major)")
            tally.check(spell(Chords.keyName(name.number, minor: true)), spell(name.minor), "keyName(\(name.number), minor)")
        }
        for number in file.numbers {
            tally.check(spell(Chords.keyNumber(number.key)), spell(number.number), "keyNumber(\(spell(number.key)))")
            tally.check(spell(Chords.keyIsMinor(number.key)), spell(number.minor), "keyIsMinor(\(spell(number.key)))")
        }
        tally.check(spell(Chords.majorKeys()), spell(file.majorKeys), "majorKeys")
        tally.check(spell(Chords.minorKeys()), spell(file.minorKeys), "minorKeys")
        tally.settle()
    }
}
