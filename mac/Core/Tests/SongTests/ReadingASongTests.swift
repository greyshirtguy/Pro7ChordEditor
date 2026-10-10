// The first two proofs of spec/05-song-and-saving.md: that every test file read gives
// what as-read-by-simple-presenter.json says, and that a file read and written with
// nothing changed is the same file, byte for byte.

import ChordRules
import Foundation
import ProFormat
import Song
import Testing
import TestData

@Suite("Reading a song")
struct ReadingASongTests {
    /// testdata/files/expected/as-read-by-simple-presenter.json: each test file as
    /// Simple Presenter's own editor reads it.
    struct AsRead: Decodable {
        struct File: Decodable {
            struct Box: Decodable {
                struct Chord: Decodable {
                    let at: Int
                    let name: String
                }
                let slide: Int
                let group: String
                let startsItsGroup: Bool
                let element: String
                let words: String
                let chords: [Chord]
                let blank: Bool
            }
            let file: String
            let key: String
            let textBoxes: [Box]
        }
        let files: [File]
    }

    static let asRead = try? JSONDecoder().decode(AsRead.self, from: TestData.file("expected/as-read-by-simple-presenter.json"))
    static let fileNames = asRead?.files.map(\.file) ?? []

    /// A box as one line of text, every unit of its words and names written out.
    private func spell(slide: Int, group: String, starts: Bool, element: String, words: [UInt16], chords: [(Int, String)], blank: Bool) -> String {
        let units = words.map { String($0, radix: 16) }.joined(separator: " ")
        let named = chords.map { "\($0.0):" + $0.1.utf16.map { String($0, radix: 16) }.joined(separator: " ") }.joined(separator: ", ")
        return "slide \(slide) | group \(group) | starts \(starts) | \(element) | \(blank ? "blank" : "words") | \(units) | \(named)"
    }

    @Test("It says what all eleven test files read as")
    func theFileIsWhole() {
        #expect(Self.fileNames.count == 11)
    }

    @Test("Each test file reads as Simple Presenter reads it", arguments: fileNames)
    func readsAsTheReferenceReads(name: String) throws {
        let expected = try #require(Self.asRead?.files.first { $0.file == name })
        let path = try #require(try TestData.presentationPath(named: name))
        // (Opened where it lies, with no safeguards: it can be read and nothing else.)
        let song = try OpenSong(opening: TestData.root.appendingPathComponent("files").appendingPathComponent(path))

        #expect(song.key == expected.key)
        let got = song.textBoxes.map {
            spell(slide: $0.slide, group: $0.group, starts: $0.startsItsGroup, element: $0.element, words: $0.words.units,
                  chords: $0.chords.map { ($0.at, $0.name) }, blank: $0.blank)
        }
        // (A slide with no text box at all is there too, with an empty `element`: the
        // last slide of Blank Slides is one.)
        let want = expected.textBoxes.map {
            spell(slide: $0.slide, group: $0.group, starts: $0.startsItsGroup, element: $0.element, words: Array($0.words.utf16),
                  chords: $0.chords.map { ($0.at, $0.name) }, blank: $0.blank)
        }
        #expect(got == want)
        #expect(song.slideCount == (expected.textBoxes.map(\.slide).max() ?? -1) + 1)
        #expect(song.usedChords == expected.textBoxes.flatMap { $0.chords.map(\.name) })
    }

    @Test("Read and written with nothing changed, each file is the same file, byte for byte",
          arguments: (try? TestData.presentationPaths()) ?? [])
    func unchangedIsTheSameBytes(path: String) throws {
        let original = try TestData.file(path)
        let written: Data = try RVData_Presentation(serializedBytes: original).serializedData()
        #expect(written == original)
    }
}
