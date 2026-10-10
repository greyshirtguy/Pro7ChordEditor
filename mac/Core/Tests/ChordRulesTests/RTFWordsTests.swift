// The words of an RTF, held to testdata/vectors/rtf-text.json: for each sample of RTF,
// the plain text it gives, unit by unit.

import ChordRules
import Foundation
import Testing

@Suite("The words of an RTF")
struct RTFWordsTests {
    struct File: Decodable {
        struct Case: Decodable {
            let name: String
            /// In base64, since some samples hold bytes that are not text.
            let rtfBase64: String
            let text: String
            let length: Int
            let units: [UInt16]
        }
        let cases: [Case]
    }

    @Test("rtf-text.json: every sample")
    func everySample() throws {
        let file: File = try vector("rtf-text.json")
        var tally = Tally("rtf-text.json")
        for c in file.cases {
            let rtf = try #require(Data(base64Encoded: c.rtfBase64), "\(c.name): its RTF is not base64")
            let words = RTFWords.words(ofRTF: rtf)
            tally.check(spell(words.length), spell(c.length), "\(spell(c.name)): length")
            tally.check(spell(words), spell(c.units), "\(spell(c.name)): units")
            // (The file says the same thing twice, as a string and as its units. If the
            // two ever disagreed, the JSON was read wrongly, and nothing above is proved.)
            tally.check(spell(c.text), spell(c.units), "\(spell(c.name)): the vector's own text against its own units")
        }
        tally.settle()
    }
}
