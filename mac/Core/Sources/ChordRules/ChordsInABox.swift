// A text box's chords, in the file: put in order and made fit to be kept, written as the
// stretches of text the file keeps them as, and carried along when the words under them
// change. Held to testdata/vectors/tidy.json, ranges.json and carried.json.
//
// Every place here is a place in the WHOLE text box's words, a line break being one.
//
// None of the three vector files has a character from beyond the first 65,536 in it, so
// the vectors do not hold these to counting in UTF-16 units. ChordsInABoxTests has cases
// of its own for that, which say they are not vectors.

/// The stretch of the text a chord is written over in the file. ProPresenter does not
/// keep where a chord stands but a stretch of characters it belongs to, which in every
/// file to hand starts at the chord and runs to the next chord or to the end of its line,
/// whichever comes first, and is never empty. (`Range` in the reference; Swift has a
/// Range of its own.)
public struct ChordRange: Hashable, Sendable {
    public var start: Int
    public var end: Int
    public var name: String

    public init(start: Int, end: Int, name: String) {
        self.start = start
        self.end = end
        self.name = name
    }
}

extension Chords {
    /// The chords of a text box put in order and made fit to be kept: names trimmed, and
    /// a chord with no name dropped; each on a unit the text has (one past the end goes
    /// to the last); one on a line break moved to the unit before it, if that is not a
    /// line break too; no two in one place (the later one given stands, in the earlier
    /// one's position); then sorted by place. An empty text has no chords.
    public static func tidy(_ text: Words, _ chords: [Chord]) -> [Chord] {
        guard !text.isEmpty else { return [] }
        var result: [Chord] = []
        for var chord in chords {
            chord.name = string(units(chord.name).trimmed())
            if chord.name.utf16.isEmpty { continue }
            chord.at = min(max(0, chord.at), text.length - 1)
            // A chord cannot stand on the end of a line, which has no character of its
            // own that shows: it goes to the last character before it.
            if text[chord.at] == Words.lineBreak && chord.at > 0 && text[chord.at - 1] != Words.lineBreak {
                chord.at -= 1
            }
            if let same = result.firstIndex(where: { $0.at == chord.at }) {
                result[same] = chord
            } else {
                result.append(chord)
            }
        }
        return result.stablySorted { $0.at < $1.at }
    }

    /// How each chord is written in the file (tidied first). On a line of chords alone,
    /// its own place and no more: one long, the stand-in it hangs on. On any other line,
    /// from its place to the next chord's place or the end of its line, whichever comes
    /// first, and never empty. This is what is WRITTEN. On reading, only `start` is
    /// believed.
    ///
    /// The first half is NOT the reference's, which has only the second for every line
    /// and so runs a chord alone on over the em quad to the next. It is how Multitracks
    /// writes such a line, in every one of 290 chords looked at, and was decided by the
    /// maintainer on 2026-10-10 (spec/01-file-format.md, "Chords"). With it, a box given
    /// the chords it already has is written exactly as it was.
    public static func ranges(_ text: Words, _ chords: [Chord]) -> [ChordRange] {
        let ordered = tidy(text, chords)
        var result: [ChordRange] = []
        for (i, chord) in ordered.enumerated() {
            var lineEnd = text.units.indexOf(Words.lineBreak, from: chord.at)
            if lineEnd < 0 { lineEnd = text.length }
            var lineStart = chord.at
            while lineStart > 0 && text[lineStart - 1] != Words.lineBreak { lineStart -= 1 }
            var end = lineEnd
            if ChordRules.isPlaceholders(Array(text.units[lineStart..<lineEnd])) {
                end = chord.at + 1
            } else if i + 1 < ordered.count {
                end = min(end, ordered[i + 1].at)
            }
            result.append(ChordRange(start: chord.at, end: max(end, chord.at + 1), name: chord.name))
        }
        return result
    }

    /// The chords of a text box whose words have been changed, where they stand in the
    /// new words. What the old and new words have in common at their start and at their
    /// end is taken to be the same words: a chord there stays on its character. A chord
    /// in the part that changed goes to the start of what replaced it.
    ///
    /// Neither app changes words, so nothing calls this yet. It is here for the day one
    /// does, and it is what a repair would use on a song whose words were changed
    /// elsewhere (spec/08-repair.md).
    public static func carried(before: Words, after: Words, _ chords: [Chord]) -> [Chord] {
        if before == after || chords.isEmpty { return tidy(after, chords) }
        let shorter = min(before.length, after.length)
        var same = 0
        while same < shorter && before[same] == after[same] { same += 1 }
        var tail = 0
        while tail < shorter - same && before[before.length - 1 - tail] == after[after.length - 1 - tail] { tail += 1 }
        // Two that end up in one place: the first stands, the way they read.
        var apart: [Chord] = []
        for var chord in chords {
            if chord.at >= before.length - tail {
                chord.at += after.length - before.length
            } else if chord.at >= same {
                chord.at = same
            }
            if !apart.contains(where: { $0.at == chord.at }) {
                apart.append(chord)
            }
        }
        return tidy(after, apart)
    }
}
