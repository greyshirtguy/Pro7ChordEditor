// What the app writes into a song's RTF, read by Apple's own reader of RTF.
//
// ProPresenter on the Mac reads a text box's RTF with Apple's text system. The rules
// here read it with a reader of their own (ChordRules/RTFWords.swift says why), and
// the one thing the app writes into an RTF, the stand-ins of chords with no words, is
// checked by that same reader before it is saved. So everything so far is the app
// agreeing with itself. This asks the other party: `NSAttributedString`, which is
// that text system, is handed what the app made and must read
//
//   - the same words the app meant;
//   - every other line of the box set exactly as it was: its font, its size, its
//     colour, its outline, how its paragraph is set (spec/01-file-format.md, "every
//     other line of the box, and all its format, stays as it was");
//   - and a box that was given words after another's opening, set as that other's
//     first word is.
//
// It is the nearest a test can come to the plan's third proof, that ProPresenter shows
// what was written. It is NOT that proof: ProPresenter does more with a text box than
// read its RTF, and only looking at it there tells.
//
// AppKit is used here, in a test, and nowhere in the rules.

#if canImport(AppKit)
import AppKit
import ChordRules
import Foundation
import ProFormat
import Song
import Testing
import TestData

@Suite("What is written into an RTF, as Apple's text system reads it")
struct AsAppleReadsItTests {
    /// An RTF as Apple's text system reads it, or nothing if it will not.
    static func read(_ rtf: some Sequence<UInt8>) -> NSAttributedString? {
        NSAttributedString(rtf: Data(rtf), documentAttributes: nil)
    }

    /// Its words as the rules count them: a break inside a paragraph, which Apple's
    /// reader gives as U+2028, is a line break like any other.
    static func words(_ text: NSAttributedString) -> [UInt16] {
        Array(text.string.utf16).map { $0 == 0x2028 ? Words.lineBreak : $0 }
    }

    /// Where each line of a text is, as ranges of its units.
    static func lines(_ units: [UInt16]) -> [NSRange] {
        var ranges: [NSRange] = []
        var start = 0
        for (n, unit) in units.enumerated() where unit == Words.lineBreak {
            ranges.append(NSRange(location: start, length: n - start))
            start = n + 1
        }
        ranges.append(NSRange(location: start, length: units.count - start))
        return ranges
    }

    struct Vectors: Decodable {
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

    static func vectors() throws -> Vectors {
        try JSONDecoder().decode(Vectors.self, from: TestData.vector("stand-ins-in-rtf.json"))
    }

    @Test("stand-ins-in-rtf.json, inPlace: wherever Apple's reader and the rules' own read the RTF alike before the change, they read it alike after; and every line but the one changed is set as it was")
    func inPlace() throws {
        var compared = 0, lines = 0
        for c in try Self.vectors().inPlace {
            guard let out = c.outBase64, let rtf = Data(base64Encoded: c.rtfBase64), let made = Data(base64Encoded: out) else { continue }
            // (Some samples are of ways of writing RTF that Apple's reader takes
            // otherwise than the rules do, or not at all: mac/NOTES.md, "At M1". Those
            // are not this test's to settle.)
            guard let before = Self.read(rtf), Self.words(before) == RTFWords.words(ofRTF: rtf).units else { continue }
            let after = try #require(Self.read(made), "\(c.name): Apple's reader will not read what was made")
            compared += 1
            #expect(Self.words(after) == RTFWords.words(ofRTF: made).units, "\(c.name): the words")
            let was = Self.lines(Self.words(before)), now = Self.lines(Self.words(after))
            #expect(was.count == now.count, "\(c.name): how many lines")
            for (n, (old, new)) in zip(was, now).enumerated() where n != c.line {
                lines += 1
                #expect(before.attributedSubstring(from: old).isEqual(to: after.attributedSubstring(from: new)), "\(c.name): line \(n) is not set as it was")
            }
            // What the line had already of what it became is set as it was, too.
            if c.line < was.count, c.line < now.count {
                let same = zip(Self.words(before)[Range(was[c.line])!], Self.words(after)[Range(now[c.line])!]).prefix { $0 == $1 }.count
                let kept = NSRange(location: was[c.line].location, length: same)
                #expect(before.attributedSubstring(from: kept).isEqual(to: after.attributedSubstring(from: NSRange(location: now[c.line].location, length: same))),
                        "\(c.name): what was kept of the line is not set as it was")
                // And every stand-in of it is in a font: none is left with nothing to be drawn in.
                for place in 0..<now[c.line].length {
                    #expect(after.attribute(.font, at: now[c.line].location + place, effectiveRange: nil) != nil, "\(c.name): a stand-in with no font")
                }
            }
        }
        #expect(compared > 200 && lines > 60, "\(compared) changes and \(lines) other lines were compared")
    }

    @Test("stand-ins-in-rtf.json, fromAnOpening: Apple's reader reads the stand-ins and nothing else, set as the model's first character is")
    func fromAnOpening() throws {
        var compared = 0
        for c in try Self.vectors().fromAnOpening {
            guard let out = c.outBase64, let model = Data(base64Encoded: c.modelBase64), let made = Data(base64Encoded: out) else { continue }
            guard let before = Self.read(model), Self.words(before) == RTFWords.words(ofRTF: model).units, before.length > 0 else { continue }
            let after = try #require(Self.read(made), "\(c.name): Apple's reader will not read what was made")
            compared += 1
            #expect(Self.words(after) == Chords.placeholders(c.count).units, "\(c.name): the words")
            // Its font, size and colour, and how its paragraph is set. (Not held
            // character by character against the model's first: a model that begins
            // with a line break has that break's own setting.)
            let first = before.attributes(at: 0, effectiveRange: nil)
            let given = after.attributes(at: 0, effectiveRange: nil)
            #expect(given[.font] as? NSFont == first[.font] as? NSFont, "\(c.name): the font")
            #expect(given[.foregroundColor] as? NSColor == first[.foregroundColor] as? NSColor, "\(c.name): the colour")
            #expect(given[.paragraphStyle] as? NSParagraphStyle == first[.paragraphStyle] as? NSParagraphStyle, "\(c.name): the paragraph")
        }
        #expect(compared > 40, "\(compared) openings were compared")
    }

    @Test("Chords With No Words, each of its lines of chords alone and blank slides changed by the song itself: the box's RTF in the file reads to Apple's reader as the words the song says, and its other lines are set as they were")
    func theSongItself() throws {
        let bench = try Workbench()
        let path = ChordsWithNoWordsTests.shaped
        let untouched = try bench.open(path).textBoxes
        func rtf(_ slide: Int, at url: URL) throws -> Data {
            let made = try message(at: url)
            let box = try OpenSong(opening: url).textBoxes[slide]
            let here = try #require(place(of: box.element, in: made))
            return made.cues[here.cue].actions[here.action].slide.presentation.baseSlide.elements[here.element].element.text.rtfData
        }
        var tried = 0
        for box in untouched {
            let alone = box.blank ? [0] : box.words.lines.indices.filter { Chords.isPlaceholders(box.words.lines[$0]) }
            for line in alone {
                let has = box.blank ? 0 : (box.words.lines[line].length + 1) / 2
                for count in Set([has + 1, has + 2, max(1, has - 1)]) {
                    tried += 1
                    let song = try bench.open(path)
                    try song.setChordsAlone(slide: box.slide, element: box.element, line: line, names: Array(repeating: "G", count: count))
                    let now = song.textBoxes[box.slide]
                    let after = try #require(Self.read(try rtf(box.slide, at: song.url)), "slide \(box.slide): Apple's reader will not read what was saved")
                    #expect(Self.words(after) == now.words.units, "slide \(box.slide), line \(line), to \(count)")
                    #expect(after.length > 0 && after.attribute(.font, at: 0, effectiveRange: nil) != nil)
                    guard !box.blank, let before = Self.read(try rtf(box.slide, at: TestData.root.appendingPathComponent("files").appendingPathComponent(path))) else { continue }
                    let was = Self.lines(Self.words(before)), then = Self.lines(Self.words(after))
                    for (n, (old, new)) in zip(was, then).enumerated() where n != line {
                        #expect(before.attributedSubstring(from: old).isEqual(to: after.attributedSubstring(from: new)), "slide \(box.slide): line \(n) is not set as it was")
                    }
                }
            }
        }
        #expect(tried >= 24)
    }
}
#endif
