// That the words of every text box in the test files, read from its RTF by the rules'
// own reader (ChordRules.RTFWords), are the words testdata/files/expected says the box
// has.
//
// This joins two things that are proved apart elsewhere: the reader of RTF, which
// testdata/vectors/rtf-text.json holds to samples of RTF, and the files, whose RTF is
// what a writer of presentations really wrote (Simple Presenter, for these nine). What
// `expected/` has was read by tools/look.py, which has a reader of RTF of its own, in
// Python; so this is two readers written apart agreeing on every box.
//
// It is of the two readers, below the song, so, as in PresentationsCanBeReadTests, it
// takes every element that has text and compares the boxes of a file as a heap, sorted.
// Which boxes the editors are handed, and in what order, is ReadingASongTests'.

import ChordRules
import Foundation
import ProFormat
import Testing
import TestData

@Suite("The words of the test files")
struct WordsOfTheTestFilesTests {
    private struct Expected: Decodable {
        struct TextBox: Decodable {
            let words: String
            let length: Int
        }
        let textBoxes: [TextBox]
    }

    static let paths = (try? TestData.presentationPaths()) ?? []

    /// Words, unit by unit, as text that sorts and compares: each unit as its number.
    private func spell(_ units: [UInt16]) -> String {
        units.map { String($0, radix: 16) }.joined(separator: " ")
    }

    @Test("Each text box's RTF gives the words expected/ has", arguments: paths)
    func wordsAreAsExpected(path: String) throws {
        let presentation = try RVData_Presentation(serializedBytes: TestData.file(path))
        let name = String(path.split(separator: "/").last!.dropLast(".pro".count))
        let expected = try JSONDecoder().decode(Expected.self, from: TestData.file("expected/\(name).json"))

        var found: [String] = []
        for cue in presentation.cues {
            for action in cue.actions {
                for element in action.slide.presentation.baseSlide.elements where element.element.hasText {
                    found.append(spell(RTFWords.words(ofRTF: element.element.text.rtfData).units))
                }
            }
        }
        #expect(!expected.textBoxes.isEmpty)
        #expect(found.sorted() == expected.textBoxes.map { spell(Array($0.words.utf16)) }.sorted())
        // (What expected/ calls a box's length is its length in UTF-16 units.)
        #expect(expected.textBoxes.allSatisfy { $0.words.utf16.count == $0.length })
    }
}
