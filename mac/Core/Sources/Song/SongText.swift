// A song as lines of text, each line knowing what it is: the form the ChordPro editor
// works on (spec/04-chordpro-editor.md, "The text", rules 1 to 3), and what the app
// shows of a song until there is an editor.
//
// It is made from the song's text boxes in order: an empty line before each new slide
// but the first; then, if that slide starts a group that has a name, the group's line;
// then each line of each box as `toChordPro` writes it. A blank slide is one line, and
// that line is empty.
//
// Beside the text of each line is kept what the line is: which text box and which line
// of it, where that line starts in the box, and its words without chords. Nothing is
// changed here. Reading the text back into chords is the ChordPro editor's, at M5.

import ChordRules

public struct SongLine: Hashable, Sendable {
    public enum Kind: Hashable, Sendable {
        /// The name of a group, before its first slide. Its text is `{c: Verse 1}`.
        case group
        /// The gap between one slide and the next. Its text is empty.
        case gap
        /// A line of a slide's words, with its chords.
        case words
        /// A line of chords alone.
        case chordsAlone
        /// A blank slide: a line of chords alone with none yet. Its text is empty.
        case blankSlide
    }

    public var kind: Kind
    /// The line as ChordPro writes it.
    public var text: String
    /// Which slide, counted from 0. For a gap, the slide that follows it.
    public var slide: Int
    /// The group's name, for a group's line; its colour, if the file gives it one.
    public var group: String = ""
    public var groupColour: Colour?
    /// For a line of a box: which of the boxes handed over it is (-1 for a line that is
    /// no box's), the box's id, which line of the box this is, where that line starts in
    /// the box, and its words without chords.
    public var box: Int = -1
    public var element: String = ""
    public var line: Int = 0
    public var start: Int = 0
    public var words: Words = Words()
}

public enum SongText {
    /// The lines of a song, from what the editors are handed.
    public static func lines(of boxes: [TextBox]) -> [SongLine] {
        var lines: [SongLine] = []
        var slide: Int?
        for (index, box) in boxes.enumerated() {
            if box.slide != slide {
                if slide != nil {
                    lines.append(SongLine(kind: .gap, text: "", slide: box.slide))
                }
                if box.startsItsGroup && !box.group.utf16.isEmpty {
                    lines.append(SongLine(kind: .group, text: "{c: \(box.group)}", slide: box.slide, group: box.group, groupColour: box.groupColour))
                }
                slide = box.slide
            }
            if box.blank {
                lines.append(SongLine(kind: .blankSlide, text: "", slide: box.slide, box: index, element: box.element))
                continue
            }
            var start = 0
            for (number, words) in box.words.lines.enumerated() {
                // A chord belongs to the line its place is in; an empty line is taken as
                // one place wide, so that a chord standing on it is not lost.
                let own = box.chords
                    .filter { $0.at >= start && $0.at < start + max(1, words.length) }
                    .map { Chord(at: $0.at - start, name: $0.name) }
                lines.append(SongLine(kind: Chords.isPlaceholders(words) ? .chordsAlone : .words,
                                      text: Chords.toChordPro(Chords.Line(text: words, chords: own)),
                                      slide: box.slide, box: index, element: box.element, line: number, start: start, words: words))
                start += words.length + 1
            }
        }
        return lines
    }

    /// The whole song as one piece of text, its lines parted by line breaks.
    public static func text(of boxes: [TextBox]) -> String {
        lines(of: boxes).map(\.text).joined(separator: "\n")
    }
}
