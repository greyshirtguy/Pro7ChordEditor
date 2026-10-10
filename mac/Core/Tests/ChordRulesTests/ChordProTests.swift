// Chords as ChordPro text, and the stand-ins, held to testdata/vectors/chordpro.json.

import ChordRules
import Testing

@Suite("ChordPro text")
struct ChordProTests {
    struct File: Decodable {
        struct ToLine: Decodable {
            let text: String
            let chords: [VectorChord]
            let out: String
        }
        struct FromLine: Decodable {
            let `in`: String
            let text: String
            let chords: [VectorChord]
        }
        struct ChordsOf: Decodable {
            let `in`: String
            let out: [VectorChord]
        }
        struct Without: Decodable {
            let `in`: String
            let out: String
        }
        struct Placeholders: Decodable {
            let count: Int
            let standIn: String
            let out: String
        }
        struct IsPlaceholders: Decodable {
            let `in`: String
            let out: Bool
        }
        let toLine: [ToLine]
        let fromLine: [FromLine]
        let toText: [ToLine]
        let chordsOf: [ChordsOf]
        let withoutChords: [Without]
        let placeholders: [Placeholders]
        let isPlaceholders: [IsPlaceholders]
    }

    @Test("chordpro.json: every case of every kind")
    func everyCase() throws {
        let file: File = try vector("chordpro.json")
        var tally = Tally("chordpro.json")
        for (n, c) in file.toLine.enumerated() {
            let line = Chords.Line(text: Words(c.text), chords: c.chords.map(\.chord))
            tally.check(spell(Chords.toChordPro(line)), spell(c.out), "toLine \(n): \(spell(c.text)) with \(spell(c.chords))")
        }
        for (n, c) in file.fromLine.enumerated() {
            let line = Chords.fromChordPro(c.in)
            tally.check(spell(line.text), spell(c.text), "fromLine \(n): text of \(spell(c.in))")
            tally.check(spell(line.chords), spell(c.chords), "fromLine \(n): chords of \(spell(c.in))")
        }
        for (n, c) in file.toText.enumerated() {
            tally.check(spell(Chords.toChordPro(Words(c.text), c.chords.map(\.chord))), spell(c.out),
                        "toText \(n): \(spell(c.text)) with \(spell(c.chords))")
        }
        for (n, c) in file.chordsOf.enumerated() {
            tally.check(spell(Chords.chordsOf(c.in)), spell(c.out), "chordsOf \(n): \(spell(c.in))")
        }
        for (n, c) in file.withoutChords.enumerated() {
            tally.check(spell(Chords.withoutChords(c.in)), spell(c.out), "withoutChords \(n): \(spell(c.in))")
        }
        for (n, c) in file.placeholders.enumerated() {
            let standIn = try #require(Array(c.standIn.utf16).first)
            try #require(c.standIn.utf16.count == 1)
            tally.check(spell(Chords.placeholders(c.count, standIn: standIn)), spell(c.out), "placeholders \(n): \(c.count) of \(spell(c.standIn))")
        }
        for (n, c) in file.isPlaceholders.enumerated() {
            tally.check(spell(Chords.isPlaceholders(Words(c.in))), spell(c.out), "isPlaceholders \(n): \(spell(c.in))")
        }
        tally.settle()
    }

    // Not a vector: the file has no character from beyond the first 65,536, so it does
    // not hold the places to UTF-16 units. Worked out by hand, the party popper being two:
    //
    //   J o y _ 🎉 🎉 _ t o \n t  h  e ...
    //   0 1 2 3 4  5  6 7 8 9  10 11 12
    @Test("Mine, not a vector: an emoji is two places")
    func anEmojiIsTwoPlaces() {
        let typed = "Joy \u{1F389} [G]to\n[D]the world"
        #expect(Chords.chordsOf(typed) == [Chord(at: 7, name: "G"), Chord(at: 10, name: "D")])
        #expect(spell(Chords.withoutChords(typed)) == spell("Joy \u{1F389} to\nthe world"))
        #expect(spell(Chords.toChordPro("Joy \u{1F389} to\nthe world", [Chord(at: 7, name: "G"), Chord(at: 10, name: "D")])) == spell(typed))

        let line = Chords.fromChordPro("Joy \u{1F389} [G]to")
        #expect(line == Chords.Line(text: "Joy \u{1F389} to", chords: [Chord(at: 7, name: "G")]))
    }

    // Not a vector. Foundation calls U+200B white space, and its trimming would empty a
    // line of stand-ins; Qt, which made the vectors, does not. This is what would break
    // first if anything here were ever done with Foundation's sets.
    @Test("Mine, not a vector: a zero-width space is not white space")
    func aZeroWidthSpaceIsNotWhiteSpace() {
        #expect(Chords.tidied("\u{200B}G") == "\u{200B}G")
        #expect(Chords.tidy("\u{200B}", [Chord(at: 0, name: "\u{200B}")]) == [Chord(at: 0, name: "\u{200B}")])
    }
}
