// Milestone M0's test of the generated reader: that it reads the presentations of
// testdata/files, finds in them what testdata/files/expected says is there, and writes
// them back saying what they said.
//
// There is no Song yet (that is M2), so these go to the generated code directly, and
// read a file the plainest way there is: every cue as it comes, every element that has
// text. Which boxes the editors show, and in what order, is M2's to build and to hold
// to expected/as-read-by-simple-presenter.json. Here the chords of a file are compared
// as a heap, sorted, with no regard to which box or slide each is in, so that nothing
// of M2's rules is assumed.
//
// The last tests are of the first thing that must stay true (AGENTS.md): that a file
// written back holds everything it came with, what the descriptions of the format have
// no name for included.

import Foundation
import ProFormat
import Song
import SwiftProtobuf
import Testing
import TestData

/// What `tools/look.py --json` says is in a presentation: the files of testdata/files/expected.
private struct Expected: Decodable {
    struct TextBox: Decodable {
        struct Chord: Decodable {
            let start: Int
            let end: Int
            let name: String
        }
        let chords: [Chord]
    }
    let nameInside: String
    let uuid: String
    let hasMusic: Bool
    let textBoxes: [TextBox]
}

/// A chord as the file has it, to compare and to sort.
private struct Found: Equatable, Comparable, CustomStringConvertible {
    let start: Int
    let end: Int
    let name: String

    static func < (a: Found, b: Found) -> Bool {
        (a.start, a.end, a.name) < (b.start, b.end, b.name)
    }
    var description: String { "\(name) \(start)-\(end)" }
}

/// Every chord in a presentation, sorted. A custom attribute is a chord if `chord` is
/// the one of its kinds that is set (spec/01-file-format.md, "Chords").
private func chords(of presentation: RVData_Presentation) -> [Found] {
    var found: [Found] = []
    for cue in presentation.cues {
        for action in cue.actions {
            for element in action.slide.presentation.baseSlide.elements where element.element.hasText {
                for attribute in element.element.text.attributes.customAttributes {
                    if case .chord(let name)? = attribute.attribute {
                        found.append(Found(start: Int(attribute.range.start), end: Int(attribute.range.end), name: name))
                    }
                }
            }
        }
    }
    return found.sorted()
}

@Suite("The presentations can be read")
struct PresentationsCanBeReadTests {
    static let paths = (try? TestData.presentationPaths()) ?? []

    @Test("The nine test files are there")
    func theFilesAreThere() {
        #expect(Self.paths.count == 9)
    }

    @Test("Amazing Grace is read, and is what it is said to be")
    func amazingGrace() throws {
        let presentation = try RVData_Presentation(serializedBytes: TestData.file("made-by-simple-presenter/Amazing Grace.pro"))
        #expect(presentation.name == "Amazing Grace")
        #expect(presentation.cues.count == 7)
        #expect(presentation.hasMusic)
        // Forty chords in all, of which the second, sorted by place, is the G over
        // "Amazing" on the first slide.
        let found = chords(of: presentation)
        #expect(found.count == 40)
        #expect(found.dropFirst().first == Found(start: 1, end: 8, name: "G"))
    }

    @Test("Each file holds the chords expected/ says it holds", arguments: paths)
    func chordsAreAsExpected(path: String) throws {
        let presentation = try RVData_Presentation(serializedBytes: TestData.file(path))
        let name = String(path.split(separator: "/").last!.dropLast(".pro".count))
        let expected = try JSONDecoder().decode(Expected.self, from: TestData.file("expected/\(name).json"))

        #expect(presentation.name == expected.nameInside)
        #expect(presentation.uuid.string == expected.uuid)
        #expect(presentation.hasMusic == expected.hasMusic)

        let expectedChords = expected.textBoxes
            .flatMap(\.chords)
            .map { Found(start: $0.start, end: $0.end, name: $0.name) }
            .sorted()
        #expect(!expectedChords.isEmpty)
        #expect(chords(of: presentation) == expectedChords)
    }

    @Test("Each file written back says what it said", arguments: paths)
    func writtenBackSaysTheSame(path: String) throws {
        let original = try TestData.file(path)
        let presentation = try RVData_Presentation(serializedBytes: original)
        let written: Data = try presentation.serializedData()

        // Read again, it is the same message: every field, known or not.
        #expect(try RVData_Presentation(serializedBytes: written) == presentation)
        // And writing is steady: the same message always gives the same bytes.
        #expect(try RVData_Presentation(serializedBytes: written).serializedData() == written)
        // Not asked: that `written == original`. The bytes may be in another order and
        // say the same (ProFormat.swift). mac/NOTES.md has what was found of that.
    }

    // The two below build a small presentation by hand, as bytes, with something in it
    // that the descriptions of the format have no name for, as a file from a newer
    // ProPresenter might have. Field 9999 is far past anything ProPresenter uses.
    //
    //   0x1A 0x04 "Song"         field 3 of the presentation, its name
    //   0xFA 0xF0 0x04 0x04 ...  field 9999, four bytes this app knows nothing of
    private static let unknown: [UInt8] = [0xFA, 0xF0, 0x04, 0x04, 0x6B, 0x65, 0x70, 0x74]

    @Test("What the reader has no name for is kept: in the presentation itself")
    func keepsWhatItDoesNotKnow() throws {
        let bytes: [UInt8] = [0x1A, 0x04, 0x53, 0x6F, 0x6E, 0x67] + Self.unknown
        var presentation = try RVData_Presentation(serializedBytes: bytes)
        #expect(presentation.name == "Song")
        #expect(presentation.unknownFields.data == Data(Self.unknown))

        // Changed, as the app will change a file, and written: it is still there.
        presentation.name = "Another"
        let written: Data = try presentation.serializedData()
        let again = try RVData_Presentation(serializedBytes: written)
        #expect(again.name == "Another")
        #expect(again.unknownFields.data == Data(Self.unknown))
    }

    @Test("What the reader has no name for is kept: inside something it does know")
    func keepsWhatItDoesNotKnowFurtherIn() throws {
        //   0x12 len                 field 2 of the presentation, its uuid, which holds
        //     0x0A 0x02 "id"           field 1 of that, the id's text
        //     (the unknown field)      and something else
        let inner: [UInt8] = [0x0A, 0x02, 0x69, 0x64] + Self.unknown
        let bytes: [UInt8] = [0x12, UInt8(inner.count)] + inner + [0x1A, 0x04, 0x53, 0x6F, 0x6E, 0x67]
        var presentation = try RVData_Presentation(serializedBytes: bytes)
        #expect(presentation.uuid.string == "id")
        #expect(presentation.uuid.unknownFields.data == Data(Self.unknown))

        presentation.name = "Another"
        let written: Data = try presentation.serializedData()
        let again = try RVData_Presentation(serializedBytes: written)
        #expect(again.uuid.string == "id")
        #expect(again.uuid.unknownFields.data == Data(Self.unknown))
    }
}
