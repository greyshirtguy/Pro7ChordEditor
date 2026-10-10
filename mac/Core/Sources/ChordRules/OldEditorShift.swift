// Where the first Windows editor meant a chord to be, and whether a song's chords look
// as if they were put on in it. Held to testdata/vectors/line-break-shift.json.
//
// This is check 2 of spec/08-repair.md, and what it is about is told there in full. In
// short: that editor read a text box's words with their line breaks lost, so a chord put
// on by hand in it was saved at a place counted along the words WITHOUT their line
// breaks. ProPresenter counts each line break as one. So such a chord sits to the left
// of where it was meant, by one character for each line break above it.
//
// NEW, and a GUESS about that editor until the maintainer has seen the fault in a real
// file: the plan has this check built and left switched off until then (spec/11-plan.md,
// M8). Only the two plain functions are here, because their vectors are among those M1
// is held to. Finding such songs and mending them is M8.
//
// The reference is `unshifted` and `line_break_shift` in tools/make-fixtures.py. That is
// Python, which counts a string in whole characters where the file, and that editor,
// count UTF-16 units. For words from beyond the first 65,536 characters the two would
// differ, and here it is units, as everywhere (AGENTS.md). The vectors have no such
// words in them. White space, for where a word starts, is Python's: Qt's list and
// U+001C to U+001F.

public enum OldEditor {
    /// The place a chord was meant for, if it was saved by the old editor at `savedAt`:
    /// `savedAt` counted along the words with their line breaks left out, turned back
    /// into a place in the words as they are.
    public static func unshifted(_ words: Words, savedAt: Int) -> Int {
        var left = savedAt
        var at = 0
        let lines = words.units.split(at: Words.lineBreak)
        for (n, line) in lines.enumerated() {
            if left < line.count || n == lines.count - 1 {
                return at + min(left, max(0, line.count - 1))
            }
            left -= line.count
            at += line.count + 1
        }
        return at
    }

    /// What `lineBreakShift` finds in a song.
    public struct Shift: Hashable, Sendable {
        /// How many chords stand below the first line of their text box (once moved to
        /// where `unshifted` puts them): the only ones that can show the fault.
        public var chordsBelowTheFirstLine: Int
        /// How many of those are at the start of a word as they are.
        public var atTheStartOfAWord: Int
        /// How many would be if they were moved.
        public var ifMovedBack: Int
        /// Whether moving them puts at least four more of them, and at least a quarter
        /// more of all of them, on the starts of words. It is never certain, so it is
        /// only ever offered, with the song shown both ways.
        public var likely: Bool
    }

    /// Whether a song's chords look as if they were put on by hand in the old editor:
    /// sitting a little to the left of the starts of words on every line but the first.
    /// Each text box is its words and the places its chords start at.
    public static func lineBreakShift(_ boxes: [(words: Words, starts: [Int])]) -> Shift {
        var below = 0, asIs = 0, moved = 0
        for box in boxes {
            let firstBreak = box.words.units.indexOf(Words.lineBreak)
            if firstBreak < 0 { continue }
            for start in box.starts {
                let there = unshifted(box.words, savedAt: start)
                if there <= firstBreak { continue }
                below += 1
                if startsAWord(box.words, start) { asIs += 1 }
                if startsAWord(box.words, there) { moved += 1 }
            }
        }
        let likely = below > 0 && moved - asIs >= 4 && (moved - asIs) * 4 >= below
        return Shift(chordsBelowTheFirstLine: below, atTheStartOfAWord: asIs, ifMovedBack: moved, likely: likely)
    }

    /// Whether a place is the first character of a word: not white space, with white
    /// space or nothing before it.
    static func startsAWord(_ words: Words, _ at: Int) -> Bool {
        func space(_ unit: UInt16) -> Bool { isSpace(unit) || (0x001C...0x001F).contains(unit) }
        return at >= 0 && at < words.length && !space(words[at]) && (at == 0 || space(words[at - 1]))
    }
}
