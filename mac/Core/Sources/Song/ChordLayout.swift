// Where the chords of one line stand, worked out from the widths of its words
// (spec/03-chord-sheet.md, rule 11; reference: qml/chordlayout.js). Held to
// testdata/vectors/layout.json.
//
// A chord stands over the character it belongs to: its left edge where that character
// starts. Two chords close together would run into each other (over a short word, or on
// a line of chords alone, where the characters they hang on are no wider than a space),
// so each is pushed right as far as it needs to clear the one before it by `gap`. That
// is all there is to it, and it is done here, in one place, so that everything that
// draws chords, and everything that asks what is under the pointer, goes by the same
// numbers.
//
// Nothing here measures anything. It is handed two ways of measuring: `advance`, how
// wide the first so-many UTF-16 units of the line's words are, and `bubble`, how wide a
// chord's bubble is with its margins. The window hands it Core Text's; the tests hand
// it letters of a fixed width, as the vectors were made with.

import ChordRules

public enum ChordLayout {
    public struct Placed: Equatable, Sendable {
        /// Where each chord's left edge is, from the line's.
        public var xs: [Double] = []
        /// How wide each chord is.
        public var widths: [Double] = []
        /// How wide the line is with its chords.
        public var width: Double = 0
        /// How wide the words are alone.
        public var wordsWidth: Double = 0

        public init() {}
    }

    /// Where the chords of a line stand. `length` is how many units the line's words
    /// have; each chord's `at` is counted along the line.
    public static func place(length: Int, chords: [Chord], advance: (Int) -> Double, bubble: (String) -> Double, gap: Double) -> Placed {
        var placed = Placed()
        var right = -1e9
        for chord in chords {
            let width = bubble(chord.name)
            let x = max(advance(min(max(chord.at, 0), length)), right + gap)
            placed.xs.append(x)
            placed.widths.append(width)
            right = x + width
        }
        placed.wordsWidth = advance(length)
        placed.width = max(placed.wordsWidth, right)
        return placed
    }

    /// Which character of the line is at `x`: the one whose left edge is nearest on the
    /// left. Found by halving, a dozen measurements for the longest line.
    public static func characterAt(length: Int, x: Double, advance: (Int) -> Double) -> Int {
        var low = 0
        var high = length
        while low < high {
            let middle = (low + high + 1) >> 1
            if advance(middle) <= x {
                low = middle
            } else {
                high = middle - 1
            }
        }
        return min(low, max(0, length - 1))
    }
}
