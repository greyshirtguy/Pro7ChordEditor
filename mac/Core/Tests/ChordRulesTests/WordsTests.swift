// Words, the type a text box's words are counted in. There is no vector file for it:
// these are the things it is there to get right, each of which Swift's String gets
// otherwise (Words.swift says why that matters).

import ChordRules
import Testing

@Suite("Words")
struct WordsTests {
    @Test("An emoji is two places, and a letter with an accent after it is two")
    func countsInUnits() {
        #expect(Words("\u{1F389}").length == 2)
        #expect("\u{1F389}".count == 1)
        #expect(Words("e\u{301}").length == 2)
        #expect(Words("Amazing grace").length == 13)
        #expect(Words("").length == 0)
    }

    @Test("Two spellings of one letter are not the same words")
    func comparesByUnits() {
        #expect("\u{E9}" == "e\u{301}")
        #expect(Words("\u{E9}") != Words("e\u{301}"))
    }

    @Test("A line break is one place, and parts the lines")
    func lines() {
        let words = Words("One\n\nTwo\n")
        #expect(words.length == 9)
        #expect(words[3] == Words.lineBreak)
        #expect(words.lines == ["One", "", "Two", ""])
        #expect(Words("").lines == [""])
    }

    @Test("Half of a pair is kept as one place")
    func keepsHalfAPair() {
        let half = Words(units: [0x41, 0xD83C, 0x42])
        #expect(half.length == 3)
        #expect(half.units == [0x41, 0xD83C, 0x42])
        // (Only the String made of it for showing loses it.)
        #expect(half.string == "A\u{FFFD}B")
    }
}
