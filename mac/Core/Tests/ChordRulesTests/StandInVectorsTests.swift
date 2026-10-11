// Stand-ins written into an RTF, held to testdata/vectors/stand-ins-in-rtf.json: for
// each case, the bytes the RTF becomes, or that it cannot be done.
//
// The vectors were made by tools/standins.py, which is the reference for this rule
// (it is new: Simple Presenter does not do it this way). They are what holds the two
// apps to writing the same bytes.

import ChordRules
import Foundation
import Testing

@Suite("Stand-ins written into an RTF: the vectors")
struct StandInVectorsTests {
    struct File: Decodable {
        struct InPlace: Decodable {
            let name: String
            let rtfBase64: String
            let line: Int
            let standIn: UInt16
            let count: Int
            let outBase64: String?
        }
        struct Opening: Decodable {
            let name: String
            let modelBase64: String
            let count: Int
            let outBase64: String?
        }
        let inPlace: [InPlace]
        let fromAnOpening: [Opening]
    }

    /// Bytes as they read, a line feed shown as one, or "nothing".
    static func shown(_ bytes: [UInt8]?) -> String {
        guard let bytes else { return "nothing" }
        return spell(String(decoding: bytes, as: UTF8.self))
    }

    @Test("stand-ins-in-rtf.json, inPlace: every case")
    func inPlace() throws {
        let file: File = try vector("stand-ins-in-rtf.json")
        var tally = Tally("stand-ins-in-rtf.json, inPlace")
        for c in file.inPlace {
            let rtf = try #require(Data(base64Encoded: c.rtfBase64), "\(c.name): its RTF is not base64")
            let want = try c.outBase64.map { Array(try #require(Data(base64Encoded: $0), "\(c.name): its answer is not base64")) }
            let got = RTFWords.rtf(rtf, withLine: c.line, become: Chords.placeholders(c.count, standIn: c.standIn))
            tally.check(Self.shown(got), Self.shown(want), spell(c.name))
        }
        tally.settle()
        // (Both kinds of answer are there, or the half that was missing proves nothing.)
        #expect(file.inPlace.contains { $0.outBase64 == nil } && file.inPlace.filter { $0.outBase64 != nil }.count > 200)
    }

    @Test("stand-ins-in-rtf.json, fromAnOpening: every case")
    func fromAnOpening() throws {
        let file: File = try vector("stand-ins-in-rtf.json")
        var tally = Tally("stand-ins-in-rtf.json, fromAnOpening")
        for c in file.fromAnOpening {
            let model = try #require(Data(base64Encoded: c.modelBase64), "\(c.name): its RTF is not base64")
            let want = try c.outBase64.map { Array(try #require(Data(base64Encoded: $0), "\(c.name): its answer is not base64")) }
            let got = RTFWords.rtf(withTheOpeningOf: model, then: Chords.placeholders(c.count))
            tally.check(Self.shown(got), Self.shown(want), spell(c.name))
        }
        tally.settle()
        #expect(file.fromAnOpening.contains { $0.outBase64 == nil } && file.fromAnOpening.filter { $0.outBase64 != nil }.count > 50)
    }
}
