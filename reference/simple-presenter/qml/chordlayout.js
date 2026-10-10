.pragma library

// Where the chords of one line of words stand, worked out from the widths of the words.
//
// A chord stands over the character it belongs to: its left edge where that character
// starts. Two chords close together would run into each other (over a short word, or
// on a line of chords alone, where the characters they hang on are no wider than a
// space), so each is pushed right as far as it needs to clear the one before it by
// `gap`. That is all there is to it, and it is done here, in one place, so that the
// stage (ChordedText.qml) and the chord editor (ChordSheet.qml) put a chord in the same
// spot.
//
// `words` and `chords` are FontMetrics of the two fonts. The answer is
// { xs: where each chord's left edge is, widths: how wide each is, width: how wide the
// line is with its chords, wordsWidth: how wide the words are alone }, all in the
// pixels of those fonts.
function place(text, chordList, words, chords, gap) {
    const xs = []
    const widths = []
    let right = -1e9
    for (const chord of chordList) {
        const width = chords.advanceWidth(chord.name)
        const x = Math.max(words.advanceWidth(text.substring(0, chord.at)), right + gap)
        xs.push(x)
        widths.push(width)
        right = x + width
    }
    const wordsWidth = words.advanceWidth(text)
    return { xs: xs, widths: widths, width: Math.max(wordsWidth, right), wordsWidth: wordsWidth }
}

// Which character of the line is at `x`: the one whose left edge is nearest on the
// left. Found by halving, a dozen measurements for the longest line.
function characterAt(text, x, words) {
    let low = 0
    let high = text.length
    while (low < high) {
        const middle = (low + high + 1) >> 1
        if (words.advanceWidth(text.substring(0, middle)) <= x)
            low = middle
        else
            high = middle - 1
    }
    return Math.min(low, Math.max(0, text.length - 1))
}
