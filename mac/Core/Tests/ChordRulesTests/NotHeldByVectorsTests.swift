// Rules that no vector holds, each held here by a case of its own.
//
// How they were found. With every vector passing, each rule was broken in one small way
// in turn (seventy-nine ways in all) and the tests run again. Most breakages were caught
// by a vector. Nineteen were not: the rule could be got wrong and every file of
// testdata/vectors would still pass. Each was given a case here, whose answer was read
// from the reference's code and not got from it.
//
// What became of them. The inputs were listed for the tools that make the vectors, and
// on 2026-10-10 the vectors were made again with nearly all of them in. The Mac app's
// rules passed every one of the new cases with not a line changed: the reference
// answered as its code had been read. So those cases are gone from here, being held now
// where they belong, for both apps.
//
// What is left is the one the vectors still do not have (testdata/README.md lists it
// as still wanted). It is not a vector: what is expected was read from
// reference/simple-presenter/src/chords.cpp. If it ever disagrees with a vector made
// later, the vector is right.

import ChordRules
import Testing

@Suite("Mine, not a vector: the rule the vectors do not hold")
struct NotHeldByVectorsTests {
    // parseSong: `lead`, and `chord.at - lead`. A line is trimmed, and its chords go with
    // its words. (The vectors have a line with two spaces after its first chord, and no
    // chord after the spaces to show whether it moved.)
    @Test("Spaces after a line's first chord go, and the chords after them move with the words")
    func chordsMoveWithTrimmedWords() {
        let line = Chords.parseSong("[G]  Amazing [C]grace").sections.first?.lines.first
        #expect(line == Chords.Line(text: "Amazing grace", chords: [Chord(at: 0, name: "G"), Chord(at: 8, name: "C")]))
    }
}
