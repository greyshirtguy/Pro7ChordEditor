// A text box's chords in order, as ranges, and carried along, held to
// testdata/vectors/tidy.json, ranges.json and carried.json.

import ChordRules
import Testing

@Suite("A box's chords")
struct ChordsInABoxTests {
    struct Tidy: Decodable {
        struct Case: Decodable {
            let text: String
            let chords: [VectorChord]
            let out: [VectorChord]
        }
        let cases: [Case]
    }

    @Test("tidy.json: every case")
    func tidy() throws {
        let file: Tidy = try vector("tidy.json")
        var tally = Tally("tidy.json")
        for (n, c) in file.cases.enumerated() {
            tally.check(spell(Chords.tidy(Words(c.text), c.chords.map(\.chord))), spell(c.out),
                        "case \(n): tidy(\(spell(c.text)), \(spell(c.chords)))")
        }
        tally.settle()
    }

    struct Ranges: Decodable {
        struct Range: Decodable {
            let start: Int
            let end: Int
            let name: String
        }
        struct Case: Decodable {
            let text: String
            let chords: [VectorChord]
            let out: [Range]
        }
        let cases: [Case]
    }

    /// Whether a place is on a line of chords alone.
    private func isOnALineOfChordsAlone(_ place: Int, in text: Words) -> Bool {
        var start = 0
        for line in text.lines {
            if place >= start && place < start + max(1, line.length) { return Chords.isPlaceholders(line) }
            start += line.length + 1
        }
        return false
    }

    /// The rule as it was before 2026-10-10, and as Simple Presenter still has it: for
    /// every line, to the next chord or the end of the line. Here only to say what a
    /// vector made before that day must hold. It is not a rule of the app any more.
    private func rangesAsTheyWere(_ text: Words, _ chords: [Chord]) -> [ChordRange] {
        let ordered = Chords.tidy(text, chords)
        return ordered.enumerated().map { i, chord in
            var end = text.units[chord.at...].firstIndex(of: Words.lineBreak) ?? text.length
            if i + 1 < ordered.count { end = min(end, ordered[i + 1].at) }
            return ChordRange(start: chord.at, end: max(end, chord.at + 1), name: chord.name)
        }
    }

    // ONE ALLOWANCE, and how it ends.
    //
    // On 2026-10-10 the maintainer decided that a chord on a line of chords alone is
    // written one long, as Multitracks writes it, and not run on to the next chord as
    // Simple Presenter writes it (spec/01-file-format.md, "Chords"). `Chords.ranges`
    // does that. ranges.json was made from Simple Presenter before that day, and one of
    // its cases is such a line. A vector is never edited: it is to be made again, from
    // a changed Simple Presenter, on another machine.
    //
    // Until it is, this test does not pretend the old answer is right, nor leave the
    // case out. For a case where the rule and the vector differ it asks two things: that
    // the vector is EXACTLY what the rule was before the decision (so the difference is
    // the decided one and no other), and that the rule's answer is the vector's with
    // only the chords on a line of chords alone made one long. And it counts those
    // cases: there is one. When ranges.json has been made again there will be none, this
    // test will fail on that count, and everything from this comment down to `settle`
    // that is about the allowance is to be taken out, with `rangesAsTheyWere`.
    @Test("ranges.json: every case (one of them made before the rule for a chord alone was decided)")
    func ranges() throws {
        let file: Ranges = try vector("ranges.json")
        var tally = Tally("ranges.json")
        var madeBeforeTheDecision = 0
        for (n, c) in file.cases.enumerated() {
            let text = Words(c.text)
            let chords = c.chords.map(\.chord)
            let got = Chords.ranges(text, chords)
            let vector = c.out.map { ChordRange(start: $0.start, end: $0.end, name: $0.name) }
            let which = "case \(n): ranges(\(spell(c.text)), \(spell(c.chords)))"
            if spell(got) == spell(vector) {
                tally.check(spell(got), spell(vector), which)
                continue
            }
            madeBeforeTheDecision += 1
            tally.check(spell(vector), spell(rangesAsTheyWere(text, chords)), which + ": the vector is not what the rule was before the decision")
            let decided = vector.map { range in
                isOnALineOfChordsAlone(range.start, in: text) ? ChordRange(start: range.start, end: range.start + 1, name: range.name) : range
            }
            tally.check(spell(got), spell(decided), which + ": differs from the vector by more than what was decided")
        }
        tally.settle()
        #expect(madeBeforeTheDecision == 1,
                "ranges.json is not as it was: if it has been made again, take the allowance out of this test (its comment says how)")
    }

    // Not a vector: the rule decided on 2026-10-10, in the shapes Multitracks writes
    // (seen in real songs: a stand-in for each chord, a zero-width space or a plain
    // space, with an em quad between; a single chord on one space). Until ranges.json
    // is made again this is what holds it.
    @Test("Mine, not a vector: a chord on a line of chords alone is written one long")
    func aChordAloneIsOneLong() {
        func ranges(_ text: Words, _ places: [Int]) -> [String] {
            Chords.ranges(text, places.map { Chord(at: $0, name: "G") }).map { "\($0.start)-\($0.end)" }
        }
        #expect(ranges("\u{200B}\u{2001}\u{200B}\u{2001}\u{200B}", [0, 2, 4]) == ["0-1", "2-3", "4-5"])
        #expect(ranges(" \u{2001} \u{2001} \u{2001} ", [0, 2, 4, 6]) == ["0-1", "2-3", "4-5", "6-7"])
        #expect(ranges(" ", [0]) == ["0-1"])
        #expect(ranges("\u{200B}", [0]) == ["0-1"])
        // Such a line among lines of words: only its own chords are one long.
        //   A m e n \n Z Q Z \n A m e n
        //   0 1 2 3 4  5 6 7 8  9 ...
        #expect(ranges("Amen\n\u{200B}\u{2001}\u{200B}\nAmen", [0, 2, 5, 7, 9]) == ["0-2", "2-4", "5-6", "7-8", "9-13"])
        // A line of words is as it ever was, spaces in it and all.
        #expect(ranges("One two", [0, 4]) == ["0-4", "4-7"])
    }

    struct Carried: Decodable {
        struct Case: Decodable {
            let before: String
            let after: String
            let chords: [VectorChord]
            let out: [VectorChord]
        }
        let cases: [Case]
    }

    @Test("carried.json: every case")
    func carried() throws {
        let file: Carried = try vector("carried.json")
        var tally = Tally("carried.json")
        for (n, c) in file.cases.enumerated() {
            tally.check(spell(Chords.carried(before: Words(c.before), after: Words(c.after), c.chords.map(\.chord))),
                        spell(c.out),
                        "case \(n): carried(\(spell(c.before)) to \(spell(c.after)), \(spell(c.chords)))")
        }
        tally.settle()
    }

    // ---- Not vectors.
    //
    // None of the three files above has a character from beyond the first 65,536 in it,
    // so they would pass if places were counted in Swift's characters. These hold the
    // counting to UTF-16 units (AGENTS.md; spec/01-file-format.md). What is expected was
    // worked out by hand from the rules, with the party popper counting as two:
    //
    //   J o y _ 🎉 🎉 _ t o \n t  h  e  _  w  o  r  l  d
    //   0 1 2 3 4  5  6 7 8 9  10 11 12 13 14 15 16 17 18
    static let joy = Words("Joy \u{1F389} to\nthe world")

    @Test("Mine, not a vector: an emoji is two places")
    func anEmojiIsTwoPlaces() {
        #expect(Self.joy.length == 19)
        // On the line break: to the unit before it. Past the end: to the last.
        let tidied = Chords.tidy(Self.joy, [Chord(at: 9, name: "D"), Chord(at: 500, name: "A"), Chord(at: 7, name: "G")])
        #expect(tidied == [Chord(at: 7, name: "G"), Chord(at: 8, name: "D"), Chord(at: 18, name: "A")])

        // To the next chord, or to the end of the line: which is at 9, not at 8.
        #expect(Chords.ranges(Self.joy, [Chord(at: 0, name: "G"), Chord(at: 7, name: "C"), Chord(at: 10, name: "D")]) == [
            ChordRange(start: 0, end: 7, name: "G"), ChordRange(start: 7, end: 9, name: "C"), ChordRange(start: 10, end: 19, name: "D"),
        ])

        // A second emoji put in before a chord moves it two places.
        let carried = Chords.carried(before: "Joy \u{1F389} to the world", after: "Joy \u{1F389}\u{1F389} to the world", [Chord(at: 7, name: "G")])
        #expect(carried == [Chord(at: 9, name: "G")])
    }
}
