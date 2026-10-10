// Rules that no vector holds, each held here by a case of its own.
//
// How they were found. With every vector passing, each rule was broken in one small way
// in turn (seventy-nine ways in all) and the tests run again. Most breakages were caught
// by a vector. These were not: the rule could be got wrong and every file of
// testdata/vectors would still pass.
//
// What these are NOT. They are not vectors. A vector is what Simple Presenter's own code
// answered. What is expected here was worked out by reading that code
// (reference/simple-presenter/src/chords.cpp and rtf.cpp, tools/keyfit.py), by the agent
// that ported it, on a machine that cannot run it. Each case says which lines it was
// read from. If one of these ever disagrees with a vector made later, the vector is right.
//
// The cure is for these inputs to go into the tools that make the vectors
// (tools/make-vectors, tools/keyfit.py), so that both apps are held to the same answers
// from the reference itself. mac/NOTES.md lists them for that. Until then the Windows
// app should carry the same cases.

import ChordRules
import Testing

@Suite("Mine, not vectors: rules the vectors do not hold")
struct NotHeldByVectorsTests {
    // ---- A chord's name

    // readNote: flats are read only where no sharp was.
    @Test("A flat after a sharp is not part of the note")
    func aFlatAfterASharp() {
        let parts = Chords.parts("G#b5")
        #expect(parts.root == "G#")
        #expect(parts.quality == "b5")
        #expect(parts.bass == "")
    }

    // parts and tidied: `bass.valid && bassLength == after.size()`.
    @Test("What follows the last slash is a bass only if it is a note and nothing more")
    func aBassIsANoteAndNothingMore() {
        let parts = Chords.parts("C/Em")
        #expect(parts.valid)
        #expect(parts.root == "C")
        #expect(parts.quality == "/Em")
        #expect(parts.bass == "")
        // So it is not tidied as a bass, nor moved as one.
        #expect(Chords.tidied("c/em") == "C/em")
        #expect(Chords.transposed("C/Em", from: "C", to: "D") == "D/Em")
    }

    // transposed: `fromKey == toKey` gives the chord back, however it is written.
    @Test("The same key twice gives a chord back exactly as it was")
    func theSameKeyTwice() {
        #expect(spell(Chords.transposed("g", from: "C", to: "C")) == spell("g"))
        #expect(spell(Chords.transposed("A\u{266D}/c", from: "E", to: "E")) == spell("A\u{266D}/c"))
    }

    // ---- A box's chords

    // carried: `if (before == after || chords.isEmpty()) return tidy(after, chords)`, and
    // tidy lets the LATER of two in one place stand, where carried itself lets the first.
    @Test("Chords carried over unchanged words are only tidied")
    func carriedOverUnchangedWords() {
        #expect(Chords.carried(before: "Amen", after: "Amen", [Chord(at: 0, name: "G"), Chord(at: 0, name: "D")]) == [Chord(at: 0, name: "D")])
    }

    // carried: `tail < shorter - same`. What the words share at their end is not looked
    // for in what they share at their start.
    @Test("The shared end of changed words does not reach into their shared start")
    func theSharedEndStopsAtTheSharedStart() {
        // "ab" is all of the start; the chord on the second "a" was in what went.
        #expect(Chords.carried(before: "abab", after: "ab", [Chord(at: 2, name: "G")]) == [Chord(at: 1, name: "G")])
    }

    // toChordPro(text, chords): `chord.at < start + qMax(1, textLine.size())`. A chord on
    // an empty line stands on that line's own line break, and belongs to that line.
    @Test("A chord on an empty line is written on that line")
    func aChordOnAnEmptyLine() {
        //   A \n \n B
        //   0 1  2  3
        #expect(Chords.tidy("A\n\nB", [Chord(at: 2, name: "G")]) == [Chord(at: 2, name: "G")])
        #expect(spell(Chords.toChordPro("A\n\nB", [Chord(at: 2, name: "G")])) == spell("A\n[G]\nB"))
    }

    // withoutChords: the expression is \[[^\[\]\n]*\]. A bracket is not closed on
    // another line.
    @Test("A bracket left open at the end of a line is words")
    func aBracketLeftOpen() {
        #expect(spell(Chords.withoutChords("[G\nA]men [C]again")) == spell("[G\nA]men again"))
        #expect(spell(Chords.withoutChords("[[G]]")) == spell("[]"))
    }

    // ---- A ChordPro file

    private func sections(_ text: String) -> [String] {
        Chords.parseSong(text).sections.map { section in
            section.name + ": " + section.lines.map { Chords.toChordPro($0) }.joined(separator: " / ")
        }
    }

    // parseSong: `if (song.artist.isEmpty()) song.artist = value`.
    @Test("The first artist given stands")
    func theFirstArtistStands() {
        let song = Chords.parseSong("{artist: John Newton}\n{subtitle: 1779}\n{st: a hymn}")
        #expect(song.artist == "John Newton")
    }

    // parseSong: `if (!marked || section.name.isEmpty()) close()`.
    @Test("An empty line inside a part that a directive marked is only a gap")
    func anEmptyLineInAMarkedPart() {
        #expect(sections("{start_of_chorus}\n[G]One\n\n[C]Two\n{end_of_chorus}") == ["Chorus: [G]One / [C]Two"])
    }

    // parseSong: `lead`, and `chord.at - lead`. A line is trimmed, and its chords go
    // with its words.
    @Test("Spaces after a line's first chord go, and the chords move with the words")
    func chordsMoveWithTrimmedWords() {
        let line = Chords.parseSong("[G]  Amazing [C]grace").sections.first?.lines.first
        #expect(line == Chords.Line(text: "Amazing grace", chords: [Chord(at: 0, name: "G"), Chord(at: 8, name: "C")]))
    }

    // headingOf: the colon, `( ?\d+[a-z]?)?`, and a capital after a space or a hyphen.
    @Test("Headings: a colon, a letter after the number, a capital after a hyphen")
    func headings() {
        #expect(sections("CHORUS:\n[G]One") == ["Chorus: [G]One"])
        #expect(sections("verse 2b\n[G]One") == ["Verse 2b: [G]One"])
        #expect(sections("pre-chorus 2:\n[G]One") == ["Pre-Chorus 2: [G]One"])
        #expect(sections("[PRE CHORUS]\n[G]One") == ["Pre Chorus: [G]One"])
        // Two letters after the number, or words after the name, and it is a line of the song.
        #expect(sections("Verse 2bb\n[G]One") == [": Verse 2bb / [G]One"])
        #expect(sections("Chorus of angels\n[G]One") == [": Chorus of angels / [G]One"])
    }

    // ---- The words of an RTF

    // rtf.cpp: `\uc` is part of a group's state, saved at "{" and put back at "}".
    @Test("How many characters to pass over after \\u is a group's own")
    func theSkipCountIsAGroupsOwn() {
        // Inside the group nothing is passed over, so "x" is words. After it one is again,
        // so "?" is not.
        // (The numbers are 8216 and 8217, the two curly quotes. The sample is put together
        // from pieces so that this file nowhere has a backslash, a "u" and four digits in a
        // row: written so, they have been turned into one Chinese character on the way into
        // a file more than once in this repository.)
        let u = #"\u"#
        let rtf = Array((#"{\rtf1\ansi {\uc0"# + u + "8216 x}" + u + "8217 ?y}").utf8)
        #expect(spell(RTFWords.words(ofRTF: rtf)) == spell("\u{2018}x\u{2019}y"))
    }

    // ---- Which key is likely (tools/keyfit.py)

    // `doubtful`: the stated key's score is MORE than 0.25 below the best.
    @Test("A stated key is doubted only when it is more than a quarter below the best")
    func aQuarterBelow() {
        // G fits every chord. D fits all but C.
        let fourFifths = KeyFit.likely(chords: ["G", "D", "Em", "G", "C"], stated: "D")
        #expect(fourFifths.scores["G"] == 1.0)
        #expect(fourFifths.scores["D"] == 0.8)
        #expect(!fourFifths.doubtful)
        #expect(fourFifths.suggested == "")

        // Exactly a quarter below is not more than a quarter below.
        let threeQuarters = KeyFit.likely(chords: ["G", "D", "Em", "C"], stated: "D")
        #expect(threeQuarters.scores["D"] == 0.75)
        #expect(!threeQuarters.doubtful)

        let sevenTenths = KeyFit.likely(chords: ["G", "D", "Em", "G", "D", "Em", "G", "C", "C", "C"], stated: "D")
        #expect(sevenTenths.scores["D"] == 0.7)
        #expect(sevenTenths.doubtful)
        #expect(sevenTenths.suggested == "G")
    }

    // `doubtful` and `suggested`: only when the best score is at least 0.75.
    @Test("No key is doubted or suggested when none fits three chords in four")
    func theBestMustFitWell() {
        // G fits three of the five, and nothing fits more.
        let answer = KeyFit.likely(chords: ["G", "C", "D", "F#", "Bb"], stated: "Ab")
        #expect(answer.scores["G"] == 0.6)
        #expect(answer.scores.values.max() == 0.6)
        #expect(!answer.doubtful)
        #expect(answer.suggested == "")
    }

    // `order`: (-uses, key.endswith("m"), against, ...). The major key comes before its
    // relative minor even when it is the one spelt against the song's chords.
    @Test("Of a major key and its relative minor used equally, the major comes first")
    func theMajorFirst() {
        // E flat's chords, written in sharps. E flat is "against" them; C minor is not.
        let answer = KeyFit.likely(chords: ["D#", "G#", "A#", "Cm", "D#", "Cm"], stated: "")
        #expect(answer.likeliest == ["Eb", "Cm"])
    }

    // `note_and_kind`: rest.startswith("dim") or rest.startswith("°").
    @Test("The little circle is diminished")
    func theLittleCircle() {
        let answer = KeyFit.likely(chords: ["C", "Dm", "G", "B\u{00B0}", "C", "F", "G", "C"], stated: "C")
        #expect(answer.scores["C"] == 1.0)
    }
}
