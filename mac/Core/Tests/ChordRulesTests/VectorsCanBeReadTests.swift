// That the tests can find testdata/vectors and read what is in it. Nothing here says a
// rule is right: each vector file has tests of its own for that, beside this one. This
// says the ground they stand on is there: every vector file is in the checkout, is
// JSON, and says of itself what it holds; and one of them, keys.json, has the shape its
// `about` says. It is the first thing to fail if a checkout is broken, and what tells
// a broken checkout from a broken rule.
//
// sheet-model.json and layout.json are read here and nowhere else yet: the chord sheet
// they hold is milestone M6.

import Foundation
import Testing
import TestData

@Suite("The vectors can be read")
struct VectorsCanBeReadTests {
    /// The eighteen files testdata/README.md lists. Written out here, so that a file
    /// that goes missing is noticed, and one that is added is given tests.
    static let expected = [
        "carried.json", "chord-names.json", "chordpro.json", "completions.json", "diatonic.json",
        "key-fit.json", "keys.json", "layout.json", "line-break-shift.json", "notated.json",
        "parse-song.json", "ranges.json", "rtf-text.json", "sheet-model.json", "shown.json",
        "signs.json", "stand-ins-in-rtf.json", "tidy.json", "transposed.json",
    ]

    @Test("Every vector file is there")
    func everyFileIsThere() throws {
        #expect(try TestData.vectorNames() == Self.expected)
    }

    @Test("Each says what it holds and what made it", arguments: expected)
    func eachSaysWhatItHolds(name: String) throws {
        let object = try #require(try JSONSerialization.jsonObject(with: TestData.vector(name)) as? [String: Any])
        let about = try #require(object["about"] as? String)
        let madeBy = try #require(object["madeBy"] as? String)
        #expect(!about.isEmpty)
        #expect(!madeBy.isEmpty)
        // Something besides those two: the cases themselves.
        #expect(object.count > 2)
    }

    /// keys.json as its `about` describes it. Only the shape is looked at: that the
    /// names are right for the numbers is a rule, and M1's to prove.
    struct Keys: Decodable {
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
        let numbers: [Number]
        let outOfRange: [Name]
        let majorKeys: [String]
        let minorKeys: [String]
    }

    @Test("keys.json has the shape it says it has")
    func keysHasItsShape() throws {
        let keys = try JSONDecoder().decode(Keys.self, from: TestData.vector("keys.json"))

        // The file numbers a key 0 to 20, and the vector has every one, in order.
        #expect(keys.names.map(\.number) == Array(0...20))
        #expect(keys.names.allSatisfy { !$0.major.isEmpty && !$0.minor.isEmpty })

        // What is no key has no name.
        #expect(!keys.outOfRange.isEmpty)
        #expect(keys.outOfRange.allSatisfy { $0.major.isEmpty && $0.minor.isEmpty && !(0...20).contains($0.number) })

        // A key's number is one the file can hold, or -1 for what is no key.
        #expect(!keys.numbers.isEmpty)
        #expect(keys.numbers.allSatisfy { (-1...20).contains($0.number) })

        #expect(keys.majorKeys.count == 13)
        #expect(keys.minorKeys.count == 13)
    }
}
