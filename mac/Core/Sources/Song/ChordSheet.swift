// The chord sheet, with no window in it: the whole song as rows, the one spot the next
// chord goes to, and what each thing done at the spot asks of the song
// (spec/03-chord-sheet.md; reference: qml/ChordSheet.qml, whose functions these are,
// under the names they have there). Held to testdata/vectors/sheet-model.json.
//
// One sheet, where the file has slides. Every line of every text box is a row, one
// under another, with a row for a group's name over its first slide. Nothing is joined
// underneath: a chord put on a word is asked for in that word's own text box.
//
// The sheet changes nothing itself. Everything comes down to one of two requests to the
// song, each of which the song saves at once or refuses:
//
//   "this text box now has these chords"       setChords
//   "this line of chords alone now has these"  setChordsAlone
//
// and after each the rows are made again from the song, as the reference does when its
// document says it has changed. So nothing here remembers what the song has; it is
// asked.
//
// Where this departs from the reference, each time on purpose:
//
//   - A name the song ALREADY has is moved, copied and pasted as it is, whether or not
//     it is a chord ("N.C.", "C /// | Am ///"). Only what is newly typed is held to
//     being one (the page, "Chords that are not chords"). The reference holds a moved
//     name too, and so cannot move such a one.
//   - The changes of one paste are one step of undo whichever of its lines was the
//     first to be changed (rule 52). The reference joins every line but the first
//     COPIED to the change before, so that where the first is passed over (the kinds
//     differ) the paste is joined to whatever was done before it.
//   - A place is never the second half of a pair of units (a character from beyond the
//     first 65,536): the pointer and the keys pass over it. The page was silent.
//
// What is drawn where is not here. The two things that go by where the words are on
// the screen (which place is under the pointer, and which stop is nearest above or
// below) are handed a `SheetMeasure`, so that the window's answers and the tests' come
// by the same road.

import ChordRules
import Foundation

/// What the sheet asks of the song. An `OpenSong` is one; the tests have another, which
/// writes down what it was asked.
public protocol SheetSong: AnyObject {
    var textBoxes: [TextBox] { get }
    /// Every chord of the song, one entry for each time it is used.
    var usedChords: [String] { get }
    /// The key the chords are written in, or "" for none stated.
    var key: String { get }
    func setChords(slide: Int, element: String, _ chords: [Chord], joined: Bool) throws
    func setChordsAlone(slide: Int, element: String, line: Int, names: [String], joined: Bool) throws
}

extension OpenSong: SheetSong {}

/// One row of the sheet: the name of a group, or one line of one text box.
public struct SheetRow: Equatable, Sendable {
    public enum Kind: Equatable, Sendable { case group, line }

    public var kind: Kind
    /// The slide, counted from 0.
    public var slide: Int
    /// A group's name and colour (a group row).
    public var name = ""
    public var colour: Colour?
    /// Which of the text boxes handed over this line is of, and which line of it.
    public var block = 0
    public var line = 0
    /// The text box's id. Empty for a slide that has no text box.
    public var element = ""
    /// The line's words.
    public var text = Words()
    /// Where the line starts in its text box.
    public var start = 0
    /// The line's chords, each place counted ALONG THE LINE.
    public var chords: [Chord] = []
    /// A line of chords with no words: its chords hang on stand-ins, every other place.
    public var alone = false
    /// A blank slide's one line: alone, with no chords yet and no stand-ins.
    public var blank = false
    /// The first line of its slide, the last, and how many lines the slide has.
    public var first = false
    public var last = false
    public var count = 0
}

/// A place on the sheet: a row and a place along its line. `row` is -1 for nowhere.
public struct Spot: Equatable, Hashable, Sendable {
    public var row: Int
    public var at: Int

    public init(row: Int, at: Int) {
        self.row = row
        self.at = at
    }
}

/// How wide things are, for the two questions that go by the screen.
public protocol SheetMeasure {
    /// How wide the first `units` units of a row's words are.
    func advance(row: Int, units: Int) -> Double
    /// How wide a chord's bubble is, margins and all.
    func bubble(_ name: String) -> Double
}

public final class ChordSheet {
    /// What was copied: for each line, its chords by which word they are on, or the
    /// names of a line of chords alone.
    public enum CopiedLine: Equatable, Sendable {
        case words([OnAWord])
        case alone([String])
    }

    public struct OnAWord: Equatable, Sendable {
        public var word: Int
        public var offset: Int
        public var name: String

        public init(word: Int, offset: Int, name: String) {
            self.word = word
            self.offset = offset
            self.name = name
        }
    }

    /// The box a chord is typed in.
    public struct Entry: Equatable, Sendable {
        /// What it holds.
        public var text = ""
        /// What it last held that could become a chord: what a key press that would
        /// make it otherwise is undone to.
        var last = ""
        /// Which of what is offered is picked, or -1.
        public var pick = -1
        /// What it was opened with, and whether all of that was selected to be typed
        /// over.
        public var begun = ""
        public var selected = false
    }

    /// A key pressed in the box a chord is typed in, of those that are the sheet's to
    /// answer.
    public enum EntryKey: Sendable { case escape, enter, tab, down, up, right, delete }

    /// The gap two bubbles are kept apart by, how far past a bubble on a line of chords
    /// alone the next one is put, and how many of what is offered are shown.
    public static let gap = 4.0
    static let offeredAtMost = 14
    static let longestTyped = 16

    public let song: any SheetSong

    public private(set) var blocks: [TextBox] = []
    public private(set) var rows: [SheetRow] = []
    public private(set) var used: [String] = []
    /// How many lines the slide with the most has.
    public private(set) var mostLines = 1

    /// The spot: which row, and which character of its line. On a line of chords alone
    /// it is a chord's stand-in (every other character) or, past the last, a new one.
    public private(set) var spot = Spot(row: -1, at: 0)
    /// The bubble for typing a chord is open, and the place the chord being typed
    /// belongs to. That place is not the spot: the spot goes on following the pointer
    /// while a chord is typed.
    public private(set) var typing = false
    public private(set) var typingAt = Spot(row: -1, at: 0)
    public private(set) var entry = Entry()
    /// Goes up each time the bubble is opened: what tells a chord's typing from the
    /// next one's at the same place.
    public private(set) var typingsBegun = 0
    /// A chord being dragged: where from, and its name.
    public private(set) var dragged: (from: Spot, name: String)?
    public private(set) var copied: [CopiedLine]?
    /// The slide that is picked: the one the spot was last moved to by the keys or a
    /// click.
    public var picked = 0

    /// How the key that pastes is named in what is said after a copy.
    public var pasteKeys = "Ctrl+V"

    /// Told when the song refused something, with why.
    public var refused: (any Error) -> Void = { _ in }
    /// Told when the song has taken a change.
    public var changed: () -> Void = {}
    /// Told when the spot was moved by the keys, and is to be brought into view.
    public var reveal: () -> Void = {}

    public init(song: any SheetSong) {
        self.song = song
        reload()
    }

    /// The key the chords are in. A song that names none is taken to be in C, which is
    /// only what the keys 1 to 7 give until the key is set.
    public var key: String {
        song.key.isEmpty ? "C" : song.key
    }

    /// The key's seven chords: what 1 to 7 put on.
    public var inKey: [String] {
        Chords.diatonic(key)
    }

    // ---- The rows

    /// Makes the rows again from the song.
    public func reload() {
        let read = song.textBoxes
        var flat: [SheetRow] = []
        var last = -1
        for (b, block) in read.enumerated() {
            let newSlide = block.slide != last
            if newSlide && block.startsItsGroup && !block.group.isEmpty {
                flat.append(SheetRow(kind: .group, slide: block.slide, name: block.group, colour: block.groupColour))
            }
            last = block.slide
            var start = 0
            for (l, text) in block.words.lines.enumerated() {
                // An empty line is taken as one place wide, so that a chord standing on
                // it is not lost.
                let end = start + max(1, text.length)
                var row = SheetRow(kind: .line, slide: block.slide)
                row.block = b
                row.line = l
                row.element = block.element
                row.text = text
                row.start = start
                row.chords = block.chords.filter { $0.at >= start && $0.at < end }.map { Chord(at: $0.at - start, name: $0.name) }
                row.alone = Chords.isPlaceholders(text) || block.blank
                row.blank = block.blank
                row.first = newSlide && l == 0
                flat.append(row)
                start += text.length + 1
            }
        }
        // Each slide's lines know how many they are and which is the last, and the
        // sheet how many the longest slide has: what the cards are sized by.
        var most = 1
        var from = 0
        while from < flat.count {
            guard flat[from].kind == .line else {
                from += 1
                continue
            }
            var to = from
            while to + 1 < flat.count && flat[to + 1].kind == .line && flat[to + 1].slide == flat[from].slide {
                to += 1
            }
            for index in from...to {
                flat[index].count = to - from + 1
            }
            flat[to].last = true
            most = max(most, to - from + 1)
            from = to + 1
        }
        blocks = read
        mostLines = most
        rows = flat
        used = song.usedChords
        if spot.row >= rows.count || (spot.row >= 0 && !usable(spot.row)) {
            spot.row = -1
        }
        // The line a chord was being typed on may have gone (an undo, a copy put back).
        if typing && !usable(typingAt.row) {
            typing = false
        }
        if let lifted = dragged, chordAt(lifted.from.row, lifted.from.at) != lifted.name {
            dragged = nil
        }
    }

    // ---- Where a chord can go

    /// A line with words on it, or a blank slide's, which has none and is there to be
    /// given chords. An empty line among a slide's words is neither.
    public func usable(_ index: Int) -> Bool {
        rows.indices.contains(index) && rows[index].kind == .line && (rows[index].text.length > 0 || rows[index].blank)
    }

    /// Where the words of a line start.
    public static func wordStarts(_ text: Words) -> [Int] {
        var starts: [Int] = []
        var inWord = false
        for (place, unit) in text.units.enumerated() {
            let space = isWordSpace(unit)
            if !space && !inWord {
                starts.append(place)
            }
            inWord = !space
        }
        return starts
    }

    /// The places on a line the spot stops at: the starts of its words, or on a line of
    /// chords alone each chord and one more for a new one (which on a blank slide's
    /// line is the only one).
    public func stops(_ index: Int) -> [Int] {
        let line = rows[index]
        if !line.alone {
            return Self.wordStarts(line.text)
        }
        return (0...line.chords.count).map { $0 * 2 }
    }

    /// The stop at or before a character, which is the word it is in.
    public func stopAt(_ index: Int, _ at: Int) -> Int {
        let all = stops(index)
        var best = all.first ?? 0
        for stop in all where stop <= at {
            best = stop
        }
        return best
    }

    /// The name of the chord at a place, or "" for none.
    public func chordAt(_ index: Int, _ at: Int) -> String {
        guard usable(index) else { return "" }
        return rows[index].chords.first { $0.at == at }?.name ?? ""
    }

    public func setSpot(_ index: Int, _ at: Int) {
        guard usable(index) else { return }
        spot = Spot(row: index, at: at)
    }

    /// Moves the spot along the line and on to the next, by stops or, `exact`, by
    /// characters (the keys Right and Left, and with Shift).
    public func step(_ delta: Int, exact: Bool) {
        guard usable(spot.row) else {
            if let first = rows.indices.first(where: usable) {
                setSpot(first, stops(first).first ?? 0)
            }
            return
        }
        let line = rows[spot.row]
        let all = exact && !line.alone
            ? (0..<line.text.length).filter { line.text[$0] != 0x0020 && !isSecondOfAPair(line.text[$0]) }
            : stops(spot.row)
        var place = all.firstIndex(of: spot.at) ?? -1
        if place < 0 {
            place = (all.firstIndex { $0 > spot.at } ?? -1) - (delta > 0 ? 1 : 0)
        }
        place += delta
        if all.indices.contains(place) {
            spot.at = all[place]
        } else {
            // Off the end of the line: the first stop of the next one, or the last of
            // the one before.
            var next = spot.row + delta
            while rows.indices.contains(next) && !usable(next) {
                next += delta
            }
            if usable(next) {
                let there = stops(next)
                setSpot(next, (delta > 0 ? there.first : there.last) ?? 0)
            }
        }
        revealSpot()
    }

    /// Moves the spot to the line above or below, to the stop nearest under or over it
    /// on the screen (the keys Down and Up).
    public func stepLine(_ delta: Int, _ measure: any SheetMeasure) {
        guard usable(spot.row) else {
            step(1, exact: false)
            return
        }
        var next = spot.row + delta
        while rows.indices.contains(next) && !usable(next) {
            next += delta
        }
        guard usable(next) else { return }
        let x = xOf(spot.row, spot.at, measure)
        var best = 0
        var distance = 1e9
        for stop in stops(next) {
            let off = abs(xOf(next, stop, measure) - x)
            if off < distance {
                distance = off
                best = stop
            }
        }
        setSpot(next, best)
        revealSpot()
    }

    private func revealSpot() {
        if usable(spot.row) {
            picked = rows[spot.row].slide
        }
        reveal()
    }

    // ---- Where things are along a line

    /// Where the chords of a row stand.
    public func placed(_ index: Int, _ measure: any SheetMeasure) -> ChordLayout.Placed {
        let line = rows[index]
        return ChordLayout.place(length: line.text.length, chords: line.chords,
                                 advance: { measure.advance(row: index, units: $0) }, bubble: measure.bubble, gap: Self.gap)
    }

    /// Where a place on a line is, from the line's left edge: a chord's bubble if one
    /// is there, the character if not, and on a line of chords alone the next free
    /// place for a new one.
    public func xOf(_ index: Int, _ at: Int, _ measure: any SheetMeasure) -> Double {
        let line = rows[index]
        let placed = placed(index, measure)
        if let found = line.chords.firstIndex(where: { $0.at == at }) {
            return placed.xs[found]
        }
        if line.alone {
            guard let last = line.chords.indices.last else { return 0 }
            return placed.xs[last] + placed.widths[last] + 10
        }
        return measure.advance(row: index, units: min(max(at, 0), line.text.length))
    }

    /// The place at a distance along a line: `exact` the character there, or else the
    /// word it is in. Over the space between two words it is the word after that is
    /// meant.
    public func placeAt(_ index: Int, along: Double, exact: Bool, _ measure: any SheetMeasure) -> Int {
        let line = rows[index]
        if line.alone {
            let placed = placed(index, measure)
            for slot in line.chords.indices where along < placed.xs[slot] + placed.widths[slot] + 5 {
                return slot * 2
            }
            return line.chords.count * 2
        }
        let length = line.text.length
        var character = ChordLayout.characterAt(length: length, x: max(0, along)) { measure.advance(row: index, units: $0) }
        guard character < length else { return 0 }
        if isSecondOfAPair(line.text[character]) && character > 0 {
            character -= 1
        }
        if exact && line.text[character] == 0x0020 && character + 1 < length && line.text[character + 1] != 0x0020 {
            character += 1
        }
        return exact ? character : stopAt(index, character)
    }

    // ---- Changing chords

    /// Asks the song, says so if it refuses, and reads the song again either way.
    private func ask(_ request: () throws -> Void) -> Bool {
        var done = true
        do {
            try request()
        } catch {
            done = false
            refused(error)
        }
        reload()
        if done {
            changed()
        }
        return done
    }

    /// Gives a line of words these chords in place of its own.
    @discardableResult
    func setLine(_ index: Int, _ chords: [Chord], joined: Bool) -> Bool {
        let line = rows[index]
        let block = blocks[line.block]
        let end = line.start + max(1, line.text.length)
        let others = block.chords.filter { $0.at < line.start || $0.at >= end }
        let all = others + chords.map { Chord(at: $0.at + line.start, name: $0.name) }
        return ask { try song.setChords(slide: line.slide, element: line.element, all, joined: joined) }
    }

    /// Puts a chord on a place, in place of the one there if there is one. `typed`: the
    /// name is newly typed, and so must be a chord; a name the song already had is put
    /// as it is.
    @discardableResult
    public func put(_ index: Int, _ at: Int, _ name: String, joined: Bool = false, typed: Bool = true) -> Bool {
        var name = name
        if typed {
            // A slash typed on the way to a bass note that never came is not part of
            // the chord.
            while name.utf16.last == 0x002F {
                name = String(decoding: Array(name.utf16.dropLast()), as: UTF16.self)
            }
            guard Chords.isChord(name) else { return false }
        }
        guard usable(index), !name.isEmpty else { return false }
        let line = rows[index]
        if line.alone {
            var names = line.chords.map(\.name)
            let slot = min(names.count, Self.slot(at))
            if slot < names.count {
                names[slot] = name
            } else {
                names.append(name)
            }
            return ask { try song.setChordsAlone(slide: line.slide, element: line.element, line: line.line, names: names, joined: joined) }
        }
        return setLine(index, line.chords.filter { $0.at != at } + [Chord(at: at, name: name)], joined: joined)
    }

    /// Takes off the chord at a place.
    @discardableResult
    public func take(_ index: Int, _ at: Int, joined: Bool = false) -> Bool {
        guard !chordAt(index, at).isEmpty else { return false }
        let line = rows[index]
        if line.alone {
            var names = line.chords.map(\.name)
            if names.indices.contains(Self.slot(at)) {
                names.remove(at: Self.slot(at))
            }
            return ask { try song.setChordsAlone(slide: line.slide, element: line.element, line: line.line, names: names, joined: joined) }
        }
        return setLine(index, line.chords.filter { $0.at != at }, joined: joined)
    }

    /// Moves a chord from one place to another, or with `copy` puts another there.
    public func move(from: Spot, to: Spot, copy: Bool) {
        let name = chordAt(from.row, from.at)
        guard !name.isEmpty, from != to else { return }
        let line = rows[from.row]
        if !copy && from.row == to.row && !line.alone {
            // Along its own line: one change
            setLine(from.row, line.chords.filter { $0.at != from.at && $0.at != to.at } + [Chord(at: to.at, name: name)], joined: false)
        } else if !copy && from.row == to.row {
            // Along a line of chords alone: it changes places
            var names = line.chords.map(\.name)
            if names.indices.contains(Self.slot(from.at)) {
                names.remove(at: Self.slot(from.at))
            }
            names.insert(name, at: min(names.count, Self.slot(to.at)))
            _ = ask { try song.setChordsAlone(slide: line.slide, element: line.element, line: line.line, names: names, joined: false) }
        } else {
            // To another line: put there, then taken from here, as one thing to undo
            if put(to.row, to.at, name, joined: false, typed: false) && !copy {
                take(from.row, from.at, joined: true)
            }
        }
        setSpot(to.row, to.at)
    }

    /// Which chord of a line of chords alone a place is: places there are 0, 2, 4 ...
    private static func slot(_ at: Int) -> Int {
        (at + 1) / 2
    }

    // ---- Copying a line's chords, or a group's, to other lines

    /// Copies the chords of the spot's line, or `whole` of its whole group: every line
    /// from the group's name down to the next group's. Answers with what to say.
    @discardableResult
    public func copy(whole: Bool) -> String? {
        guard usable(spot.row) else { return nil }
        var from = spot.row
        var to = spot.row
        if whole {
            while from > 0 && rows[from - 1].kind != .group {
                from -= 1
            }
            while to + 1 < rows.count && rows[to + 1].kind != .group {
                to += 1
            }
        }
        var lines: [CopiedLine] = []
        for index in from...to where usable(index) {
            let line = rows[index]
            if line.alone {
                lines.append(.alone(line.chords.map(\.name)))
                continue
            }
            let starts = Self.wordStarts(line.text)
            lines.append(.words(line.chords.map { chord in
                // A chord standing on the space before a word counts as on the word
                // before.
                var word = 0
                for (w, start) in starts.enumerated() where start <= chord.at {
                    word = w
                }
                return OnAWord(word: word, offset: chord.at - (starts.indices.contains(word) ? starts[word] : 0), name: chord.name)
            }))
        }
        copied = lines
        let count = lines.reduce(0) { sum, line in
            switch line {
            case .words(let chords): sum + chords.count
            case .alone(let names): sum + names.count
            }
        }
        return "\(count)\(count == 1 ? " chord" : " chords") copied from \(lines.count == 1 ? "the line" : "\(lines.count) lines")"
            + ": \(pasteKeys) puts them on the lines from the spot's on, word for word"
    }

    /// Puts what was copied on the lines from the spot's on, one copied line to each
    /// line that can take the spot.
    public func paste() {
        guard let copied, usable(spot.row) else { return }
        var index = spot.row
        // However many lines it changes, a paste undoes as one: each change after the
        // first that was made is joined to it.
        var made = false
        for line in copied {
            while index < rows.count && !usable(index) {
                index += 1
            }
            if index >= rows.count {
                break
            }
            let target = rows[index]
            switch line {
            case .alone(let names):
                // A copied line of chords alone goes only onto a line of chords alone.
                if target.alone {
                    let joined = made
                    if ask({ try song.setChordsAlone(slide: target.slide, element: target.element, line: target.line, names: names, joined: joined) }) {
                        made = true
                    }
                }
            case .words(let copiedChords):
                if !target.alone {
                    let starts = Self.wordStarts(target.text)
                    var chords: [Chord] = []
                    for chord in copiedChords where chord.word < starts.count {
                        // On the same numbered word, as far into it as it was but not
                        // past the word's last letter.
                        let start = starts[chord.word]
                        let end = chord.word + 1 < starts.count ? starts[chord.word + 1] - 1 : target.text.length
                        let at = min(start + chord.offset, max(start, end - 1))
                        if !chords.contains(where: { $0.at == at }) {
                            chords.append(Chord(at: at, name: chord.name))
                        }
                    }
                    if setLine(index, chords, joined: made) {
                        made = true
                    }
                }
            }
            index += 1
        }
    }

    // ---- Typing a chord

    /// What the chord being typed might be going to be: the first fourteen.
    public var offered: [String] {
        guard typing else { return [] }
        return Array(Chords.completions(typed: entry.text, key: key, used: used).prefix(Self.offeredAtMost))
    }

    /// Opens the bubble on the spot: with `seed` to go on from (the letter that was
    /// pressed), or without, on the chord that is there, all of it picked to be typed
    /// over.
    public func startTyping(seed: String? = nil) {
        guard usable(spot.row) else { return }
        typingAt = spot
        typing = true
        let text = seed ?? chordAt(spot.row, spot.at)
        entry = Entry(text: text, last: text, pick: -1, begun: text, selected: seed == nil)
        typingsBegun += 1
    }

    /// What was typed in the box, as the box would have it now. Only what could be a
    /// chord gets in, and its notes are made capitals as they are typed. Answers with
    /// what the box is to hold: what was typed tidied, or what it held before.
    @discardableResult
    public func type(_ text: String) -> String {
        let tidy = Chords.tidied(text)
        guard Chords.couldBecome(tidy), tidy.utf16.count <= Self.longestTyped else {
            entry.text = entry.last
            return entry.text
        }
        entry.text = tidy
        entry.last = tidy
        entry.pick = -1
        return tidy
    }

    /// What Enter takes: the one picked from what is offered, or what is typed.
    public func taken() -> String {
        let offered = offered
        return offered.indices.contains(entry.pick) ? offered[entry.pick] : Chords.tidied(entry.text)
    }

    /// Ends the typing. With a name, that chord goes on the place it was being typed
    /// for; with none, nothing is changed (Esc). `onward` then moves the spot on from
    /// that place to the next word (Tab), wherever the pointer has been meanwhile.
    public func finishTyping(_ name: String?, onward: Bool) {
        let at = typingAt
        typing = false
        if let name, !name.isEmpty {
            put(at.row, at.at, name)
        }
        if onward {
            setSpot(at.row, at.at)
            step(1, exact: false)
        }
    }

    /// Keeps the chord that is being typed, if one is: what a click anywhere else does,
    /// and what leaving the sheet does, so that going on to the next thing never loses
    /// it.
    public func settle() {
        if typing {
            finishTyping(taken(), onward: false)
        }
    }

    /// A click on a place of a line: the chord being typed elsewhere is kept, and the
    /// bubble opens here, on the chord that is here or for a new one.
    ///
    /// (It is done by row and place because keeping a chord makes every row again, the
    /// one that was clicked with it.)
    public func pick(_ index: Int, _ at: Int) {
        // A new chord at the end of a line of chords alone is one place further along
        // once the chord that was being typed on that same line has gone in before it.
        let fresh = usable(index) && rows[index].alone && at >= rows[index].chords.count * 2
        settle()
        guard usable(index) else { return }
        setSpot(index, fresh ? rows[index].chords.count * 2 : at)
        picked = rows[index].slide
        startTyping()
    }

    /// A key pressed in the box, with whether the caret is at the box's end. Answers
    /// whether it was the sheet's to answer; if not, the box does with it what a box
    /// does.
    @discardableResult
    public func press(_ key: EntryKey, caretAtEnd: Bool = true) -> Bool {
        guard typing else { return false }
        let offered = offered
        switch key {
        case .escape:
            finishTyping(nil, onward: false)
        case .enter:
            finishTyping(taken(), onward: false)
        case .tab:
            // The first offered, if nothing is picked and nothing typed is a chord yet
            if entry.pick < 0 && !Chords.isChord(entry.text) && !offered.isEmpty {
                entry.pick = 0
            }
            finishTyping(taken(), onward: true)
        case .down:
            entry.pick = offered.isEmpty ? -1 : (entry.pick + 1) % offered.count
        case .up:
            entry.pick = offered.isEmpty ? -1 : (entry.pick + offered.count - 1 + (entry.pick < 0 ? 1 : 0)) % offered.count
        case .right:
            // The one picked goes into the box to be gone on with (for a bass note, say)
            guard offered.indices.contains(entry.pick), caretAtEnd else { return false }
            entry.text = offered[entry.pick]
            entry.last = entry.text
            entry.pick = -1
        case .delete:
            // Nothing left of it: the chord goes
            guard entry.text.isEmpty else { return false }
            let at = typingAt
            typing = false
            take(at.row, at.at)
        }
        return true
    }

    // ---- The keys, with no chord being typed

    /// 1 to 7: the key's own chord for that note of its scale, on the spot. (A click on
    /// one in the strip does the same; while a chord is being typed it goes where the
    /// typing was, in place of what was typed.)
    public func putOfKey(_ number: Int) {
        let chords = inKey
        guard chords.count == 7, (1...7).contains(number) else { return }
        if typing {
            finishTyping(chords[number - 1], onward: false)
        } else {
            put(spot.row, spot.at, chords[number - 1])
        }
    }

    /// Delete: the chord at the spot goes.
    public func takeAtSpot() {
        take(spot.row, spot.at)
    }

    // ---- The pointer

    /// The pointer has moved over a place. The spot follows it, unless a chord is
    /// being carried (which moves the spot itself).
    public func pointerOver(_ index: Int, _ at: Int) {
        if dragged == nil {
            setSpot(index, at)
        }
    }

    /// A chord is picked up to be carried. Not while another is being typed.
    public func lift(_ index: Int, _ at: Int) {
        let name = chordAt(index, at)
        guard !typing, !name.isEmpty else { return }
        dragged = (Spot(row: index, at: at), name)
    }

    /// The chord being carried is over a place.
    public func carry(over index: Int, _ at: Int) {
        guard dragged != nil else { return }
        setSpot(index, at)
    }

    /// It is let go: it moves to the spot, or with `copy` another is put there.
    public func drop(copy: Bool) {
        guard let lifted = dragged else { return }
        dragged = nil
        move(from: lifted.from, to: spot, copy: copy)
    }

    public func dropNowhere() {
        dragged = nil
    }
}

/// White space, as the reference's sheet parts a line into words by it (JavaScript's
/// own notion: its `\S+`). A zero-width space is not white space, so a stand-in is a
/// "word"; an em quad is.
func isWordSpace(_ unit: UInt16) -> Bool {
    switch unit {
    case 0x0009...0x000D, 0x0020, 0x00A0, 0x1680, 0x2000...0x200A, 0x2028, 0x2029, 0x202F, 0x205F, 0x3000, 0xFEFF:
        return true
    default:
        return false
    }
}

/// The second unit of a character that takes two.
func isSecondOfAPair(_ unit: UInt16) -> Bool {
    (0xDC00...0xDFFF).contains(unit)
}
