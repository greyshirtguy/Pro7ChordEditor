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
}
