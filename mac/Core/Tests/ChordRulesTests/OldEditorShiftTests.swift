// Where the old Windows editor meant a chord to be, held to
// testdata/vectors/line-break-shift.json.

import ChordRules
import Testing

@Suite("The old editor's shift")
struct OldEditorShiftTests {
    struct File: Decodable {
        struct Unshifted: Decodable {
            let words: String
            let savedAt: Int
            let meantFor: Int
        }
        struct Song: Decodable {
            struct Box: Decodable {
                struct Chord: Decodable {
                    let start: Int
                }
                let words: String
                let chords: [Chord]
            }
            struct Out: Decodable {
                let chordsBelowTheFirstLine: Int
                let atTheStartOfAWord: Int
                let ifMovedBack: Int
                let likely: Bool
            }
            let about: String
            let textBoxes: [Box]
            let out: Out
        }
        let unshifted: [Unshifted]
        let songs: [Song]
    }

    @Test("line-break-shift.json: every case")
    func everyCase() throws {
        let file: File = try vector("line-break-shift.json")
        var tally = Tally("line-break-shift.json")
        for c in file.unshifted {
            tally.check(spell(OldEditor.unshifted(Words(c.words), savedAt: c.savedAt)), spell(c.meantFor),
                        "unshifted(\(spell(c.words)), \(c.savedAt))")
        }
        for song in file.songs {
            let shift = OldEditor.lineBreakShift(song.textBoxes.map { (words: Words($0.words), starts: $0.chords.map(\.start)) })
            let which = spell(song.about)
            tally.check(spell(shift.chordsBelowTheFirstLine), spell(song.out.chordsBelowTheFirstLine), "\(which): chordsBelowTheFirstLine")
            tally.check(spell(shift.atTheStartOfAWord), spell(song.out.atTheStartOfAWord), "\(which): atTheStartOfAWord")
            tally.check(spell(shift.ifMovedBack), spell(song.out.ifMovedBack), "\(which): ifMovedBack")
            tally.check(spell(shift.likely), spell(song.out.likely), "\(which): likely")
        }
        tally.settle()
    }

    // Not a vector. The reference for this rule is Python, which counts a string in
    // whole characters; the file and the old editor count UTF-16 units, and so does this
    // (OldEditorShift.swift says so). Worked out by hand, the party popper being two:
    //
    //   🎉 🎉 _ a \n b _ c        without the line break:  🎉 🎉 _ a b _ c
    //   0  1  2 3 4  5 6 7                                 0  1  2 3 4 5 6
    @Test("Mine, not a vector: an emoji is two places")
    func anEmojiIsTwoPlaces() {
        let words = Words("\u{1F389} a\nb c")
        #expect(OldEditor.unshifted(words, savedAt: 3) == 3)
        #expect(OldEditor.unshifted(words, savedAt: 4) == 5)
        #expect(OldEditor.unshifted(words, savedAt: 6) == 7)
    }
}
