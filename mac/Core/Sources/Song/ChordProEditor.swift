// The ChordPro editor with no window in it: the song as one piece of text, what may be
// typed in it, the help it gives with brackets, and what a pause in the typing saves.
// spec/04-chordpro-editor.md is this file's specification, rule for rule; the reference
// is reference/simple-presenter/qml/ChordProEditor.qml (`composed`, `check`, `apply`,
// `chordAround`, and the key handler at its foot).
//
// The window's part (AppViews/ChordProView.swift) is a text view that asks here before
// it lets a change through, and tells here when the typing has paused. Nothing there is
// a rule.
//
// The text is made by SongText (rules 1 to 3), which also says of every line what it
// is. Everything below goes by that, and counts in UTF-16 units: as the file counts
// along a song's words, and as the Mac's text view counts along its text. What is typed
// is held against the song's words unit for unit, never as two Strings, which Swift
// calls equal when they only look alike.
//
// Where this parts from the reference, each said at the place:
//
//   - A name the song already had when it was opened is let through whatever it is
//     (rule 22). There, a song with one name that is no chord cannot be changed at all.
//   - A box is saved when what the text says of it is no longer what the text said when
//     it was made from the song (rule 14). There, it is saved when what the text says
//     is not what the FILE has, and the two can differ before anything is typed: a name
//     that is not tidy, a chord past the end of its words. Typing in one box would then
//     write every such box of the song.
//   - The rounds of one saving follow each other at once and are one step of undo
//     (rules 17 and 18). There the second round waits for the next pause.

import ChordRules
import Foundation

public final class ChordProEditor {
    /// What became of something typed.
    public enum Verdict: Equatable, Sendable {
        /// It is let through, and the text is now that.
        case taken
        /// It is not the song's words with only chords changed. The text stays as it
        /// was, and nothing is said: the caret not moving says it.
        case notTheWords
    }

    /// A change to the text: these units are to be replaced with this, and the caret
    /// left there.
    public struct Edit: Equatable, Sendable {
        public var range: Range<Int>
        public var with: String
        public var caret: Int

        public init(range: Range<Int>, with: String, caret: Int) {
            self.range = range
            self.with = with
            self.caret = caret
        }
    }

    /// Where the text is to be coloured (rule 4): the chords, brackets and all, and the
    /// lines in curly brackets.
    public struct Colouring: Equatable, Sendable {
        public var chords: [Range<Int>] = []
        public var greyed: [Range<Int>] = []
    }

    public static let help = "Type chords in square brackets among the words: [G]  [D/F#]  [Am7].  Only the chords can be changed here; the words are the slides'."
    /// (The reference ends "The chord editor can still be used." The page has "The chord
    /// sheet", and until there is one, M6, this says neither.)
    static let wordsHaveBrackets = "This song's words have square brackets of their own in them, so it cannot be changed as ChordPro text. The chord sheet can still be used."
    static let cannotBeSaidExactly = "This song cannot be changed as ChordPro text: one of its chords has a square bracket or a line break in its name, or its words have a broken character in them."

    public let song: OpenSong
    /// The text as it last passed rule 6: what the window shows.
    public private(set) var text: String
    /// What each line of the text is.
    public private(set) var lines: [SongLine]
    /// Why the song can be read here and not changed, as a sentence; nil if it can be
    /// changed (rule 20).
    public private(set) var barred: String?

    private var boxes: [TextBox]
    /// The names the song's chords had when it was opened here, each as its units.
    private let namesAtOpening: Set<[UInt16]>

    public init(song: OpenSong) {
        self.song = song
        self.boxes = song.textBoxes
        self.lines = SongText.lines(of: boxes)
        self.text = lines.map(\.text).joined(separator: "\n")
        self.namesAtOpening = Set(boxes.flatMap { $0.chords.map { Array($0.name.utf16) } })
        self.barred = Self.whyBarred(boxes: boxes, lines: lines, text: text, names: namesAtOpening)
    }

    // ---- What may be typed

    /// Asks whether the text may become this, and if it may, takes it (rules 6 and 7).
    public func take(_ typed: String) -> Verdict {
        guard barred == nil else { return .notTheWords }
        let units = Array(typed.utf16)
        guard Self.allows(units, lines: lines, names: namesAtOpening) else { return .notTheWords }
        text = typed
        return .taken
    }

    /// THE ONE RULE (rule 6). A change to the text is let through only if, with the
    /// chords taken out, the text is still the words it started as: it has the same
    /// number of lines, and for each line
    ///
    ///   - a group's line or a gap is exactly what it was;
    ///   - a line of words, with every "[...]" taken out, is exactly its words, and has
    ///     no stray bracket left in it;
    ///   - a line of chords alone (a blank slide's among them), with every "[...]" taken
    ///     out, is nothing but spaces;
    ///   - whatever is in any pair of brackets could become a chord, or is, exactly, a
    ///     name the song had when it was opened (rule 22).
    ///
    /// It covers typing in the wrong place, pasting, cutting, dropping and undoing
    /// alike. There is no rule for each.
    static func allows(_ typed: [UInt16], lines: [SongLine], names: Set<[UInt16]>) -> Bool {
        let typedLines = split(typed)
        guard typedLines.count == lines.count else { return false }
        for (line, want) in zip(typedLines, lines) {
            switch want.kind {
            case .group, .gap:
                if line != Array(want.text.utf16) { return false }
            case .words, .chordsAlone, .blankSlide:
                let bare = Chords.withoutChords(Words(units: line))
                // (Kept as the reference has it. The last test below says as much by
                // itself, a song whose own words have brackets being barred.)
                if bare.contains(openBracket) || bare.contains(closeBracket) { return false }
                for pair in pairs(in: line) {
                    let inside = Array(line[(pair.open + 1)..<pair.close])
                    if names.contains(inside) { continue }
                    if !Chords.couldBecome(Words(units: inside).trimmed.string) { return false }
                }
                if want.kind == .words ? bare != want.words : !bare.isBlank { return false }
            }
        }
        return true
    }

    // ---- Saving

    /// Saves the chords the text has, wherever they are not what the song has (rules 13
    /// to 18): each text box whose chords are no longer what the text says, as one
    /// change each, and all of them one step of undo. Answers whether anything was
    /// saved. A refusal is thrown on; what was saved before it stays saved.
    ///
    /// Empty brackets, and ones that hold what is not yet a chord, are left out of what
    /// is saved and left in the text (rule 15): someone half way through typing "[F#"
    /// must not have it saved as a chord nor taken away from them.
    @discardableResult
    public func save() throws -> Bool {
        guard barred == nil else { return false }
        var saved = false
        defer {
            if saved { reload(keepingWhatIsTyped: true) }
        }
        // (A round for each line of chords alone at the most, and one more: should the
        // song ever not take what it was asked for a line, the asking stops.)
        var roundsLeft = lines.count + 1
        while true {
            let typedLines = Self.split(Array(text.utf16))
            let madeLines = lines.map { Array($0.text.utf16) }
            guard typedLines.count == lines.count, roundsLeft > 0 else { return saved }
            roundsLeft -= 1

            // A line of chords alone is saved as its chords' names in order (rule 17).
            // Since that can change its text box's words (the stand-ins), the song is
            // read again after it, and the other lines go in the next round, which
            // follows at once.
            var another = false
            for (n, want) in lines.enumerated() where want.kind == .chordsAlone || want.kind == .blankSlide {
                let names = chordsSaid(by: typedLines[n]).map(\.name)
                if names == chordsSaid(by: madeLines[n]).map(\.name) { continue }
                try song.setChordsAlone(slide: want.slide, element: want.element, line: want.line, names: names, joined: saved)
                saved = true
                reload(keepingWhatIsTyped: true)
                another = true
                break
            }
            if another { continue }

            for (index, box) in boxes.enumerated() where !box.blank {
                let said = chordsSaid(of: index, by: typedLines)
                // (Held against what the text said when it was made from the song, not
                // against the file's own entries: see the head of this file.)
                if said == chordsSaid(of: index, by: madeLines) { continue }
                // Its lines of chords alone keep theirs: they were dealt with above.
                let alone = box.chords.filter { chord in
                    lines.contains { $0.box == index && $0.kind == .chordsAlone && chord.at >= $0.start && chord.at < $0.start + max(1, $0.words.length) }
                }
                let wanted = (said + alone).enumerated().sorted { ($0.element.at, $0.offset) < ($1.element.at, $1.offset) }.map(\.element)
                try song.setChords(slide: box.slide, element: box.element, wanted, joined: saved)
                saved = true
            }
            return saved
        }
    }

    /// Reads the song again (rule 19). When the song has changed because of what was
    /// just typed here, the text is left exactly as typed, if it still says what the
    /// song's words are. When it has changed from elsewhere, the text is made again
    /// from the song.
    public func reload(keepingWhatIsTyped keep: Bool) {
        boxes = song.textBoxes
        lines = SongText.lines(of: boxes)
        let made = lines.map(\.text).joined(separator: "\n")
        barred = Self.whyBarred(boxes: boxes, lines: lines, text: made, names: namesAtOpening)
        if keep && Self.allows(Array(text.utf16), lines: lines, names: namesAtOpening) { return }
        text = made
    }

    // ---- Help with the brackets (rules 9 to 11). Rule 12, that none of it happens
    // while Command, Control or Option is down, is the window's to know.

    /// What typing a character does beyond being put in, if anything: "[" with nothing
    /// selected puts "[]" and leaves the caret between them; "]" where the next
    /// character is already "]" steps over it.
    public static func helpWithTyping(_ typed: String, in text: String, selection: Range<Int>) -> Edit? {
        guard selection.isEmpty else { return nil }
        let units = Array(text.utf16)
        let caret = selection.lowerBound
        if typed == "[" {
            return Edit(range: caret..<caret, with: "[]", caret: caret + 1)
        }
        if typed == "]", caret < units.count, units[caret] == closeBracket {
            return Edit(range: caret..<caret, with: "", caret: caret + 1)
        }
        return nil
    }

    /// Backspace just after a "[" or a "]" takes the whole chord, brackets and all.
    /// Nil where the key is to do what it always does.
    public static func helpWithBackspace(in text: String, selection: Range<Int>) -> Edit? {
        let units = Array(text.utf16)
        let caret = selection.lowerBound
        guard selection.isEmpty, caret > 0, caret <= units.count else { return nil }
        let before = units[caret - 1]
        guard before == openBracket || before == closeBracket,
              let chord = chordAround(before == openBracket ? caret : caret - 1, in: units) else { return nil }
        return Edit(range: chord, with: "", caret: chord.lowerBound)
    }

    /// Delete just before a "[" or a "]" does the same.
    public static func helpWithDelete(in text: String, selection: Range<Int>) -> Edit? {
        let units = Array(text.utf16)
        let caret = selection.lowerBound
        guard selection.isEmpty, caret >= 0, caret < units.count else { return nil }
        let after = units[caret]
        guard after == openBracket || after == closeBracket,
              let chord = chordAround(after == openBracket ? caret + 1 : caret, in: units) else { return nil }
        return Edit(range: chord, with: "", caret: chord.lowerBound)
    }

    /// The chord a place is in or beside, from its "[" to past its "]"; nil if the
    /// nearest "[" before the place is not on its line, or has no "]" on that line, or
    /// closes before the place.
    static func chordAround(_ position: Int, in units: [UInt16]) -> Range<Int>? {
        // (The last such unit at or before a place, as the reference's `lastIndexOf`
        // finds it: a place before the start is taken as the start.)
        func lastIndex(of unit: UInt16, from: Int) -> Int {
            var i = min(max(0, from), units.count - 1)
            while i >= 0 {
                if units[i] == unit { return i }
                i -= 1
            }
            return -1
        }
        let lineStart = lastIndex(of: lineBreak, from: position - 1) + 1
        let open = lastIndex(of: openBracket, from: position - 1)
        guard open >= lineStart, open >= 0 else { return nil }
        guard let close = units[open...].firstIndex(of: closeBracket) else { return nil }
        if let lineEnd = units[open...].firstIndex(of: lineBreak), close > lineEnd { return nil }
        if close < position - 1 { return nil }
        return open..<(close + 1)
    }

    // ---- Which slide is picked (rule 21), and the colours (rule 4)

    /// The slide whose line a place in the text is on; nil on a gap or a group's name.
    public func slide(at position: Int) -> Int? {
        let line = Array(text.utf16).prefix(max(0, position)).filter { $0 == Self.lineBreak }.count
        guard lines.indices.contains(line) else { return nil }
        switch lines[line].kind {
        case .group, .gap: return nil
        case .words, .chordsAlone, .blankSlide: return lines[line].slide
        }
    }

    /// Where a slide's first line starts in the text: where the caret goes when the
    /// slide is picked from elsewhere.
    public func start(ofSlide slide: Int) -> Int? {
        let typedLines = Self.split(Array(text.utf16))
        guard typedLines.count == lines.count,
              let index = lines.firstIndex(where: { $0.slide == slide && $0.kind != .group && $0.kind != .gap }) else { return nil }
        return typedLines[..<index].reduce(0) { $0 + $1.count + 1 }
    }

    /// Chords are coloured, brackets and all; the lines in curly brackets are greyed.
    /// The colouring never changes the text.
    public static func colouring(of text: String) -> Colouring {
        var colouring = Colouring()
        var start = 0
        for line in split(Array(text.utf16)) {
            if line.first == openBrace {
                colouring.greyed.append(start..<(start + line.count))
            } else {
                for pair in pairs(in: line) {
                    colouring.chords.append((start + pair.open)..<(start + pair.close + 1))
                }
            }
            start += line.count + 1
        }
        return colouring
    }

    // ---- How

    private static let lineBreak: UInt16 = 0x000A
    private static let openBracket: UInt16 = 0x005B
    private static let closeBracket: UInt16 = 0x005D
    private static let openBrace: UInt16 = 0x007B

    /// A pair of brackets in a line: where its "[" and its "]" are, and the place it
    /// has among the line's words.
    struct Pair: Equatable {
        var open: Int
        var close: Int
        var at: Int
    }

    /// The text's lines. There is always one at least; text that ends with a line break
    /// ends with an empty line.
    static func split(_ units: [UInt16]) -> [[UInt16]] {
        var lines: [[UInt16]] = [[]]
        for unit in units {
            if unit == lineBreak {
                lines.append([])
            } else {
                lines[lines.count - 1].append(unit)
            }
        }
        return lines
    }

    /// Every "[...]" of a line that has no bracket inside it, in order: what the
    /// reference finds with the pattern `\[[^\[\]]*\]`. In a line that passes rule 6
    /// these are all its brackets, and they are the chords `Chords.chordsOf` reads
    /// (bar the empty ones, which that leaves out), each at the same place.
    static func pairs(in line: [UInt16]) -> [Pair] {
        var found: [Pair] = []
        var i = 0
        var at = 0
        while i < line.count {
            if line[i] == openBracket {
                var j = i + 1
                while j < line.count && line[j] != openBracket && line[j] != closeBracket {
                    j += 1
                }
                if j < line.count && line[j] == closeBracket {
                    found.append(Pair(open: i, close: j, at: at))
                    i = j + 1
                    continue
                }
            }
            at += 1
            i += 1
        }
        return found
    }

    /// The chords a line of the text says, each at the place it has among the line's
    /// words, its name tidied: every bracket that holds a chord (`isChord`), or a name
    /// the song had when it was opened (rule 22). Empty brackets and half-typed ones
    /// are left out (rule 15).
    private func chordsSaid(by line: [UInt16]) -> [Chord] {
        Self.pairs(in: line).compactMap { pair in
            let inside = Array(line[(pair.open + 1)..<pair.close])
            let name = Chords.tidied(Words(units: inside).string)
            guard !name.utf16.isEmpty, namesAtOpening.contains(inside) || Chords.isChord(name) else { return nil }
            return Chord(at: pair.at, name: name)
        }
    }

    /// The chords the text says a box's lines of words have, each at its place along
    /// the whole box. One typed after the last word of a line hangs on the line's last
    /// character (rule 16; `Chords.tidy` would put it there too when the box is
    /// written); a line with no characters has none to hang one on.
    private func chordsSaid(of box: Int, by typedLines: [[UInt16]]) -> [Chord] {
        var chords: [Chord] = []
        for (line, want) in zip(typedLines, lines) where want.box == box && want.kind == .words && want.words.length > 0 {
            for chord in chordsSaid(by: line) {
                chords.append(Chord(at: want.start + min(chord.at, want.words.length - 1), name: chord.name))
            }
        }
        return chords
    }

    /// Rule 20, and what it does not think of. If any text box's words have a "[" or a
    /// "]" in them, ChordPro text cannot tell them from chords. And if the text as it is
    /// made from the song does not itself pass rule 6, nothing typed ever could: a
    /// chord's name has a bracket or a line break in it, or the words have half of a
    /// character in them that a String cannot hold. (NOT in the reference, where such a
    /// song takes no change and says nothing.)
    private static func whyBarred(boxes: [TextBox], lines: [SongLine], text: String, names: Set<[UInt16]>) -> String? {
        if boxes.contains(where: { $0.words.contains(openBracket) || $0.words.contains(closeBracket) }) {
            return wordsHaveBrackets
        }
        return allows(Array(text.utf16), lines: lines, names: names) ? nil : cannotBeSaidExactly
    }
}
