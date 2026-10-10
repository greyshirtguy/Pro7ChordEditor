// What the tests of the rules share: reading a file of testdata/vectors, comparing an
// answer with the one the file has, and saying what went wrong.
//
// How these tests are written (spec/02-chord-rules.md, "The vectors"). A test runs
// EVERY case of its file and says, for a failure, which file, which case and what was
// got. No case is copied into a test's code: the files are read, so that when they are
// made again both apps are held to the new ones without a line changing. And an expected
// answer is never changed to make a test pass (AGENTS.md).
//
// Strings are compared unit by unit, never as Swift compares them (which calls "é" equal
// to "e" with an accent after it) and never by how they print (some hold U+200B and
// U+2001, which cannot be seen). That is done by `spell`: every answer is spelt out in
// plain ASCII with each other unit written as its number, and it is the spellings that
// are compared. A failure then also shows exactly what was got.

import ChordRules
import Foundation
import Testing
import TestData

/// Reads a vector file as a type that describes its shape.
func vector<T: Decodable>(_ name: String, as type: T.Type = T.self) throws -> T {
    try JSONDecoder().decode(T.self, from: TestData.vector(name))
}

/// A chord as the vectors write one.
struct VectorChord: Decodable {
    let at: Int
    let name: String

    var chord: Chord { Chord(at: at, name: name) }
}

/// A string unit by unit: printable ASCII as it is, anything else as `\u{XXXX}`.
func spell(_ string: String) -> String {
    spell(Array(string.utf16))
}

func spell(_ units: [UInt16]) -> String {
    var out = "\""
    for unit in units {
        switch unit {
        case 0x22: out += "\\\""
        case 0x5C: out += "\\\\"
        case 0x20...0x7E: out += String(UnicodeScalar(UInt8(unit)))
        default: out += "\\u{" + String(unit, radix: 16, uppercase: true) + "}"
        }
    }
    return out + "\""
}

func spell(_ words: Words) -> String { spell(words.units) }
func spell(_ strings: [String]) -> String { "[" + strings.map { spell($0) }.joined(separator: ", ") + "]" }
func spell(_ chord: Chord) -> String { "\(spell(chord.name))@\(chord.at)" }
func spell(_ chords: [Chord]) -> String { "[" + chords.map { spell($0) }.joined(separator: ", ") + "]" }
func spell(_ chords: [VectorChord]) -> String { spell(chords.map(\.chord)) }
func spell(_ ranges: [ChordRange]) -> String {
    "[" + ranges.map { "\(spell($0.name)) \($0.start)-\($0.end)" }.joined(separator: ", ") + "]"
}
func spell(_ flag: Bool) -> String { flag ? "true" : "false" }
func spell(_ number: Int) -> String { String(number) }

/// The cases of one vector file as they are gone through: how many were checked, and
/// what was wrong with those that were.
struct Tally {
    let file: String
    private(set) var checked = 0
    private(set) var wrong: [String] = []

    init(_ file: String) {
        self.file = file
    }

    /// One answer against the one expected, both as `spell` spells them.
    mutating func check(_ got: String, _ expected: String, _ which: @autoclosure () -> String) {
        checked += 1
        if got != expected {
            wrong.append("\(file): \(which()): got \(got), expected \(expected)")
        }
    }

    /// Says what was found: every failure up to twenty, and how many there were.
    func settle(sourceLocation: SourceLocation = #_sourceLocation) {
        #expect(checked > 0, "\(file): no case was checked", sourceLocation: sourceLocation)
        for failure in wrong.prefix(20) {
            Issue.record(Comment(rawValue: failure), sourceLocation: sourceLocation)
        }
        if wrong.count > 20 {
            Issue.record("\(file): and \(wrong.count - 20) more, of \(checked) checked", sourceLocation: sourceLocation)
        }
    }
}
