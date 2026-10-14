// Reading a ChordPro file, held to testdata/vectors/parse-song.json, which has what
// each file of testdata/songs is read as.

import ChordRules
import Foundation
import Testing
import TestData

@Suite("A ChordPro file")
struct ChordProSongTests {
    struct File: Decodable {
        struct Line: Decodable {
            let text: String
            let chords: [VectorChord]
        }
        struct Section: Decodable {
            let name: String
            let lines: [Line]
            /// slidesOf(section, n), as how many lines land on each slide, for n "1" to "4".
            let linesOnEachSlide: [String: [Int]]
        }
        struct Case: Decodable {
            let file: String
            let title: String
            let artist: String
            let key: String
            let copyright: String
            let ccli: String
            let sections: [Section]
        }
        let cases: [Case]
    }

    @Test("parse-song.json: every song")
    func everySong() throws {
        let file: File = try vector("parse-song.json")
        var tally = Tally("parse-song.json")
        for c in file.cases {
            // (The files are UTF-8, and one has Windows line ends on purpose.)
            let song = Chords.parseSong(String(decoding: try TestData.song(c.file), as: UTF8.self))
            tally.check(spell(song.title), spell(c.title), "\(c.file): title")
            tally.check(spell(song.artist), spell(c.artist), "\(c.file): artist")
            tally.check(spell(song.key), spell(c.key), "\(c.file): key")
            tally.check(spell(song.copyright), spell(c.copyright), "\(c.file): copyright")
            tally.check(spell(song.ccli), spell(c.ccli), "\(c.file): ccli")
            tally.check(spell(song.sections.map(\.name)), spell(c.sections.map(\.name)), "\(c.file): the names of its sections")
            for (section, expected) in zip(song.sections, c.sections) {
                let which = "\(c.file): \(spell(expected.name))"
                tally.check(spell(section.lines.count), spell(expected.lines.count), "\(which): how many lines")
                for (n, (line, expectedLine)) in zip(section.lines, expected.lines).enumerated() {
                    tally.check(spell(line.text), spell(expectedLine.text), "\(which), line \(n): text")
                    tally.check(spell(line.chords), spell(expectedLine.chords), "\(which), line \(n): chords")
                }
                for (each, expectedCounts) in expected.linesOnEachSlide.sorted(by: { $0.key < $1.key }) {
                    let linesPerSlide = try #require(Int(each))
                    let slides = Chords.slidesOf(section, linesPerSlide: linesPerSlide)
                    tally.check("\(slides.map(\.count))", "\(expectedCounts)", "\(which): slidesOf, \(each) lines to a slide")
                    // (And the slides are the lines, in order, none lost.)
                    tally.check(spell(slides.flatMap { $0 }.map(\.text.string)), spell(section.lines.map(\.text.string)), "\(which): slidesOf \(each), the lines")
                }
            }
        }
        tally.settle()
    }

    @Test("Mine, not a vector. A directive's name is asked of its units: what it starts with is found whatever unit comes next, an accent laid over the last of them too")
    func aDirectivesNameIsAskedOfItsUnits() {
        // An accent after a letter or an underline is one character with it, to Swift,
        // whose own "starts with" then does not find "start_of_" at the front of the
        // name. The reference asks units (Qt's `startsWith`), and so must this:
        // spec/02-chord-rules.md, "To the letter". Nothing writes such a name. (Seen by
        // the Windows app's session, reading this port.)
        let accent = "\u{301}"
        let leftOut = Chords.parseSong("{start_of_\(accent)tab}\nleft out\n{end_of_\(accent)tab}\nkept\n")
        #expect(leftOut.sections.map { $0.lines.map(\.text.string) } == [["kept"]])
        // "eo" and one unit more ends what is left out, whatever the unit.
        let ended = Chords.parseSong("{sot}\nleft out\n{eo\(accent)}\nkept\n")
        #expect(ended.sections.map { $0.lines.map(\.text.string) } == [["kept"]])
        // (And four units are not three.)
        let notEnded = Chords.parseSong("{sot}\nleft out\n{eo\(accent)x}\nleft out too\n")
        #expect(notEnded.sections.isEmpty)
    }
}
