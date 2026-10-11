// The one place the apps write into the RTF of a song's words: the characters that
// chords with no words hang on (spec/01-file-format.md, "Chords with no words" and
// "Chords onto a blank slide").
//
// What is expected here was worked out by hand, byte for byte, from the rule as that
// page has it, and not got from the code. The RTF is written as Apple's text system
// writes it, in the shapes real songs were found to have (mac/NOTES.md, "At M7"): a
// song from Multitracks has every other stand-in in another font, so that control
// words lie between the characters of one line; some hang their chords on plain
// spaces; a single chord often hangs on one. None of the RTF below is anyone's song.
//
// testdata/vectors/stand-ins-in-rtf.json holds the same rule for both apps, with more
// cases, made by tools/make-fixtures.py (StandInVectorsTests.swift). These are here
// because what they expect can be read beside what they are given.

@testable import ChordRules
import Foundation
import Testing

/// An RTF as Apple's text system opens one, and then this.
private func apple(_ body: String) -> [UInt8] {
    Array((#"{\rtf1\ansi\ansicpg1252\cocoartf2761"# + "\n"
        + #"\cocoatextscaling0\cocoaplatform0{\fonttbl\f0\fswiss\fcharset0 Helvetica;\f1\fnil\fcharset0 LucidaGrande;}"# + "\n"
        + #"{\colortbl;\red255\green255\blue255;\red255\green255\blue255;}"# + "\n"
        + #"{\*\expandedcolortbl;;\csgenericrgb\c100000\c100000\c100000;}"# + "\n"
        + #"\pard\pardirnatural\qc\partightenfactor0"# + "\n\n" + body).utf8)
}

/// Bytes as they read, a line feed shown as one.
private func shown(_ rtf: [UInt8]?) -> String {
    guard let rtf else { return "nothing" }
    return String(decoding: rtf, as: UTF8.self).replacingOccurrences(of: "\n", with: "\u{23CE}")
}

private let zero: UInt16 = 0x200B
private let space: UInt16 = 0x0020

private func standIns(_ count: Int, _ standIn: UInt16 = zero) -> Words {
    Chords.placeholders(count, standIn: standIn)
}

@Suite("Stand-ins written into an RTF, in place")
struct StandInsInRTFTests {
    // As Multitracks writes two chords with no words: each stand-in in the song's font,
    // the em quad between them in another.
    static let two = #"\f0\fs160 \cf2 \uc0\u8203 "# + "\n" + #"\f1 \cf2 \uc0\u8193 "# + "\n" + #"\f0 \cf2 \uc0\u8203 "#

    @Test("One more chord: an em quad and a stand-in go after the line's last character, and not a byte before it changes")
    func oneMore() {
        let made = RTFWords.rtf(apple(Self.two + "}"), withLine: 0, become: standIns(3))
        #expect(shown(made) == shown(apple(Self.two + #"\uc0\u8193 \uc0\u8203 }"#)))
    }

    @Test("One fewer: the characters go, and the control words that lay between them stay, since they say how what follows is set")
    func oneFewer() {
        let made = RTFWords.rtf(apple(Self.two + "}"), withLine: 0, become: standIns(1))
        #expect(shown(made) == shown(apple(#"\f0\fs160 \cf2 \uc0\u8203 "# + "\n" + #"\f1 \cf2 \uc0"# + "\n" + #"\f0 \cf2 \uc0}"#)))
    }

    @Test("None: every character of the line goes, and the line is then empty")
    func none() {
        let made = RTFWords.rtf(apple(Self.two + "}"), withLine: 0, become: standIns(0))
        #expect(shown(made) == shown(apple(#"\f0\fs160 \cf2 \uc0"# + "\n" + #"\f1 \cf2 \uc0"# + "\n" + #"\f0 \cf2 \uc0}"#)))
        #expect(made.map { RTFWords.words(ofRTF: $0) } == Words())
    }

    @Test("As many as it has: nothing is written, and what comes back is what went in")
    func asMany() {
        let rtf = apple(Self.two + "}")
        #expect(RTFWords.rtf(rtf, withLine: 0, become: standIns(2)) == rtf)
    }

    @Test("A line that hangs its chords on plain spaces: a space is written as a space, after the em quad's own")
    func plainSpaces() {
        // Three chords: a space, an em quad, a space, an em quad, a space.
        let three = #"\f0\fs84 \cf2  \uc0\u8193  \uc0\u8193  }"#
        #expect(shown(RTFWords.rtf(apple(three), withLine: 0, become: standIns(4, space)))
            == shown(apple(#"\f0\fs84 \cf2  \uc0\u8193  \uc0\u8193  \uc0\u8193  }"#)))
        #expect(shown(RTFWords.rtf(apple(three), withLine: 0, become: standIns(2, space)))
            == shown(apple(#"\f0\fs84 \cf2  \uc0\u8193  \uc0}"#)))
        #expect(shown(RTFWords.rtf(apple(three), withLine: 0, become: standIns(1, space)))
            == shown(apple(#"\f0\fs84 \cf2  \uc0\uc0}"#)))
    }

    @Test("One chord hung on one plain space, as a slide of one chord often is: a second goes after it")
    func oneSpace() {
        #expect(shown(RTFWords.rtf(apple(#"\f0\fs84 \cf2  }"#), withLine: 0, become: standIns(2, space)))
            == shown(apple(#"\f0\fs84 \cf2  \uc0\u8193  }"#)))
    }

    @Test("A space put in after a control word that has no space of its own is given one, or it would end the word and not be text")
    func aSpaceThatWouldBeLost() {
        // The line is a space and an em quad, and the em quad's word runs up to the bracket.
        let after = RTFWords.rtf(apple(#"\cf2  \uc0\u8193}"#), withLine: 0, become: standIns(2, space))
        #expect(shown(after) == shown(apple(#"\cf2  \uc0\u8193  }"#)))
        #expect(after.map { RTFWords.words(ofRTF: $0) } == standIns(2, space))
        // The line is an em quad and a space, and is to be one space: the space goes
        // where the em quad was, straight after a word that has no space.
        let before = RTFWords.rtf(apple(#"\cf2\uc0\u8193  }"#), withLine: 0, become: standIns(1, space))
        #expect(shown(before) == shown(apple(#"\cf2\uc0  }"#)))
        #expect(before.map { RTFWords.words(ofRTF: $0) } == standIns(1, space))
        // The same where the em quad is written straight in, as its three bytes of
        // UTF-8: it is no control word, and the word before it still has no space.
        let straightIn = Array(#"{\rtf1\ansi\cf2"#.utf8) + [0xE2, 0x80, 0x81] + Array(" }".utf8)
        #expect(RTFWords.words(ofRTF: straightIn) == Words(units: [0x2001, 0x20]))
        #expect(RTFWords.rtf(straightIn, withLine: 0, become: standIns(1, space)) == Array(#"{\rtf1\ansi\cf2  }"#.utf8))
    }

    @Test("The same where the character whose place it takes is itself a word with no space after it, straight after another")
    func aSpaceThatWouldBeLostBeforeAWordWithNoSpace() {
        // Three stand-ins, not a space among their words: a zero-width space, an em
        // quad, a zero-width space. To be two chords on plain spaces: a space goes
        // where the first was, straight after `\uc0`, and must not end it.
        let made = RTFWords.rtf(apple(#"\f0 \uc0\u8203\uc0\u8193\uc0\u8203}"#), withLine: 0, become: standIns(2, space))
        #expect(shown(made) == shown(apple(#"\f0 \uc0  \uc0\u8193  \uc0\uc0}"#)))
        #expect(made.map { RTFWords.words(ofRTF: $0) } == standIns(2, space))
    }

    @Test("A line of chords alone under a line of words: the words' bytes are not touched")
    func underWords() {
        let rtf = apple(#"\f0\fs84 \cf2 Made up words\"# + "\n" + #" \uc0\u8193  }"#)
        let made = RTFWords.rtf(rtf, withLine: 1, become: standIns(3, space))
        #expect(shown(made) == shown(apple(#"\f0\fs84 \cf2 Made up words\"# + "\n" + #" \uc0\u8193  \uc0\u8193  }"#)))
        // And the first line cannot be told to become stand-ins by a slip of the number
        // without it showing: it is the caller that keeps to lines of chords alone.
        #expect(RTFWords.words(ofRTF: made ?? []).lines.first == Words("Made up words"))
    }

    @Test("A line of chords alone over a line of words: what comes after the line is byte for byte what it was")
    func overWords() {
        let rtf = apple(#"\f0\fs84 \cf2  \uc0\u8193  \"# + "\n" + #"Made up words}"#)
        #expect(shown(RTFWords.rtf(rtf, withLine: 0, become: standIns(3, space)))
            == shown(apple(#"\f0\fs84 \cf2  \uc0\u8193  \uc0\u8193  \"# + "\n" + #"Made up words}"#)))
        #expect(shown(RTFWords.rtf(rtf, withLine: 0, become: standIns(1, space)))
            == shown(apple(#"\f0\fs84 \cf2  \uc0\"# + "\n" + #"Made up words}"#)))
    }

    @Test("Two lines of chords alone in one box: only the one that is named changes")
    func twoLines() {
        let first = #"\f0\fs160 \cf2 \uc0\u8203 "# + "\n" + #"\f1 \cf2 \uc0\u8193 "# + "\n" + #"\f0 \cf2 \uc0\u8203 \"# + "\n"
        let second = #"\uc0\u8203 "# + "\n" + #"\f1 \cf2 \uc0\u8193 "# + "\n" + #"\f0 \cf2 \uc0\u8203 "#
        let rtf = apple(first + second + "}")
        #expect(shown(RTFWords.rtf(rtf, withLine: 1, become: standIns(3)))
            == shown(apple(first + second + #"\uc0\u8193 \uc0\u8203 }"#)))
        #expect(shown(RTFWords.rtf(rtf, withLine: 0, become: standIns(1)))
            == shown(apple(#"\f0\fs160 \cf2 \uc0\u8203 "# + "\n" + #"\f1 \cf2 \uc0"# + "\n" + #"\f0 \cf2 \uc0\"# + "\n" + second + "}")))
    }

    @Test("Stand-ins written straight in as UTF-8, as another tool may write them, are found by their bytes all the same")
    func writtenStraightIn() {
        let rtf = Array(#"{\rtf1\ansi "#.utf8) + [0xE2, 0x80, 0x8B, 0xE2, 0x80, 0x81, 0xE2, 0x80, 0x8B] + Array("}".utf8)
        #expect(RTFWords.words(ofRTF: rtf) == standIns(2))
        #expect(RTFWords.rtf(rtf, withLine: 0, become: standIns(1)) == Array(#"{\rtf1\ansi "#.utf8) + [0xE2, 0x80, 0x8B] + Array("}".utf8))
        #expect(RTFWords.rtf(rtf, withLine: 0, become: standIns(3))
            == Array(#"{\rtf1\ansi "#.utf8) + [0xE2, 0x80, 0x8B, 0xE2, 0x80, 0x81, 0xE2, 0x80, 0x8B] + Array(#"\uc0\u8193 \uc0\u8203 }"#.utf8))
    }

    @Test("What cannot be done cleanly is not done: no such line, an empty line, and an RTF whose characters have others standing in after them")
    func notDone() {
        let rtf = apple(Self.two + #"\"# + "\n" + #"\"# + "\n" + "Words}")
        #expect(RTFWords.rtf(rtf, withLine: 3, become: standIns(1)) == nil)
        #expect(RTFWords.rtf(rtf, withLine: -1, become: standIns(1)) == nil)
        // (The second line is empty: there is no character to say where it is.)
        #expect(RTFWords.words(ofRTF: rtf).lines.map(\.length) == [3, 0, 5])
        #expect(RTFWords.rtf(rtf, withLine: 1, become: standIns(1)) == nil)
        // An RTF that says one character follows each `\u` to stand in for it, for
        // readers that do not know `\u`. Taking a `\u` out would leave its stand-in to
        // be read as a word; putting `\uc0` in would do the same to those after it.
        // Neither is written: what was made would not read back as what was meant.
        let other = Array(#"{\rtf1\ansi \u8203?\u8193?\u8203?\par Words \u8217?}"#.utf8)
        #expect(RTFWords.words(ofRTF: other).lines.first == standIns(2))
        #expect(RTFWords.rtf(other, withLine: 0, become: standIns(1)) == nil)
        #expect(RTFWords.rtf(other, withLine: 0, become: standIns(3)) == nil)
        // Only stand-ins are ever written.
        #expect(RTFWords.rtf(apple(Self.two + "}"), withLine: 0, become: Words("G")) == nil)
    }

    @Test("rtf-text.json, every sample, every line that has a character, to none, one, two and three stand-ins of both kinds: outside the line not a byte changes, and the text reads back as meant")
    func everySampleEveryLine() throws {
        let file: RTFWordsTests.File = try vector("rtf-text.json")
        var tried = 0, made = 0
        for sample in file.cases {
            let rtf = Array(try #require(Data(base64Encoded: sample.rtfBase64)))
            let lines = RTFWords.words(ofRTF: rtf).lines
            for (number, line) in lines.enumerated() {
                guard let span = RTFWords.span(ofLine: number, of: rtf) else {
                    #expect(line.isEmpty, "\(sample.name): line \(number) has characters and no place")
                    continue
                }
                for standIn in [zero, space] {
                    for count in 0...3 {
                        tried += 1
                        let wanted = standIns(count, standIn)
                        guard let changed = RTFWords.rtf(rtf, withLine: number, become: wanted) else { continue }
                        made += 1
                        var meant = lines
                        meant[number] = wanted
                        #expect(RTFWords.words(ofRTF: changed).lines == meant, "\(sample.name): line \(number) to \(count)")
                        let after = rtf.count - span.upperBound
                        #expect(Array(changed.prefix(span.lowerBound)) == Array(rtf.prefix(span.lowerBound)), "\(sample.name): line \(number) to \(count): before the line")
                        #expect(Array(changed.suffix(after)) == Array(rtf.suffix(after)), "\(sample.name): line \(number) to \(count): after the line")
                    }
                }
            }
        }
        // (If nearly none could be made, the test above would be proving nothing.)
        #expect(tried > 100)
        #expect(made * 10 > tried * 8, "\(made) of \(tried) could be made")
    }
}

@Suite("A new RTF for a box with no words, from another's opening")
struct OpeningOfAnRTFTests {
    @Test("The opening is everything up to the first character of the words: the fonts, the colours and how the words are set; the stand-ins follow, and the bracket closes")
    func fromAppleRTF() {
        let model = apple(#"\f0\fs134 \cf2 \outl0\strokewidth-60 \strokec3 Made up words\"# + "\n" + "And more}")
        #expect(shown(RTFWords.rtf(withTheOpeningOf: model, then: standIns(2)))
            == shown(apple(#"\f0\fs134 \cf2 \outl0\strokewidth-60 \strokec3 \uc0\u8203 \uc0\u8193 \uc0\u8203 }"#)))
        #expect(shown(RTFWords.rtf(withTheOpeningOf: model, then: standIns(1)))
            == shown(apple(#"\f0\fs134 \cf2 \outl0\strokewidth-60 \strokec3 \uc0\u8203 }"#)))
    }

    @Test("Words that begin inside groups: as many brackets close as the opening left open")
    func insideGroups() {
        let model = Array(#"{\rtf1\ansi{\fonttbl{\f0 Arial;}}{\pard{\f0\fs40 Words}\par}}"#.utf8)
        let made = RTFWords.rtf(withTheOpeningOf: model, then: standIns(2))
        #expect(shown(made) == #"{\rtf1\ansi{\fonttbl{\f0 Arial;}}{\pard{\f0\fs40 \uc0\u8203 \uc0\u8193 \uc0\u8203 }}}"#)
        #expect(made.map { RTFWords.words(ofRTF: $0) } == standIns(2))
    }

    @Test("A model whose own words are stand-ins gives its opening like any other; one that begins with an empty line gives what is before that line's end")
    func otherModels() {
        // (The `\uc0` before the model's first stand-in is not that character's own
        // bytes, so it is part of the opening, and what is put after it begins with
        // another. That says one thing twice and harms nothing; a rule that left it
        // out would be one more thing for two apps to get the same.)
        #expect(shown(RTFWords.rtf(withTheOpeningOf: apple(StandInsInRTFTests.two + "}"), then: standIns(1)))
            == shown(apple(#"\f0\fs160 \cf2 \uc0\uc0\u8203 }"#)))
        #expect(shown(RTFWords.rtf(withTheOpeningOf: apple(#"\f0\fs84 \cf2 \"# + "\n" + "Words}"), then: standIns(1)))
            == shown(apple(#"\f0\fs84 \cf2 \uc0\u8203 }"#)))
    }

    @Test("A text box with no character at all, as ProPresenter leaves an empty one, has no opening to give")
    func noOpening() {
        let empty = Array((#"{\rtf1\ansi\ansicpg1252\cocoartf2761"# + "\n" + #"\cocoatextscaling0\cocoaplatform0{\fonttbl}"# + "\n"
            + #"{\colortbl;\red255\green255\blue255;}"# + "\n" + #"{\*\expandedcolortbl;;}"# + "\n}").utf8)
        #expect(RTFWords.words(ofRTF: empty).isEmpty)
        #expect(RTFWords.rtf(withTheOpeningOf: empty, then: standIns(1)) == nil)
        #expect(RTFWords.rtf(withTheOpeningOf: [], then: standIns(1)) == nil)
    }

    @Test("rtf-text.json, every sample that has a character: the opening is the sample's own bytes, and the new RTF reads as the stand-ins and nothing else")
    func everySample() throws {
        let file: RTFWordsTests.File = try vector("rtf-text.json")
        var made = 0
        for sample in file.cases where !sample.units.isEmpty {
            let rtf = Array(try #require(Data(base64Encoded: sample.rtfBase64)))
            guard let new = RTFWords.rtf(withTheOpeningOf: rtf, then: standIns(3)) else { continue }
            made += 1
            #expect(RTFWords.words(ofRTF: new) == standIns(3), "\(sample.name)")
            // What is before the stand-ins is the sample's own, byte for byte.
            let put = Array(#"\uc0\u8203 \uc0\u8193 \uc0\u8203 \uc0\u8193 \uc0\u8203 "#.utf8)
            let closing = new.reversed().prefix { $0 == UInt8(ascii: "}") }.count
            let opening = new.count - closing - put.count
            #expect(opening >= 0 && Array(new[opening..<(opening + put.count)]) == put, "\(sample.name): the stand-ins are not where they should be")
            #expect(opening >= 0 && Array(new.prefix(max(0, opening))) == Array(rtf.prefix(max(0, opening))), "\(sample.name): the opening")
        }
        #expect(made > 20)
    }
}
