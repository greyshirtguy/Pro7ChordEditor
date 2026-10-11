// The chord sheet, drawn: one view that draws every row of the song itself, follows the
// pointer, and takes the keys (spec/03-chord-sheet.md; spec/10-platforms.md: "the sheet
// is one custom-drawn view ... measuring text with Core Text so that the place under
// the pointer and the place a bubble is drawn come from the same numbers").
//
// Nothing here is a rule. What a key or a click does to the song is
// Song/ChordSheet.swift's, with no window in it: this view tells it what was done (the
// pointer is over this place, this key was pressed) and draws what it then has. After
// each thing done it calls `sync`, which lays the rows out again if they have changed,
// opens or closes the bubble a chord is typed in, and draws.
//
// How it is laid out is the reference's (qml/ChordSheet.qml), number for number: a
// column of rows, and a card behind each slide's lines. The sizes are in `SheetLook`.
// One thing is not the reference's: there all cards are one size, each as tall as the
// slide with the most lines. Here they are one WIDTH, and each as tall as its own
// slide (spec/03, rule 8: the maintainer's, on seeing a real library, where a slide of
// one line or none had a card with room for the longest).
//
// The same numbers, for drawing and for pointing. Each line's words are one Core Text
// line (`SheetMetrics`). That line is what is drawn, and how far along it a character
// is, is asked of that same line; so a tail stands on the character the pointer chose.
// A place is a UTF-16 unit, which is what Core Text counts in too.
//
// What the Mac does its own way (spec/10-platforms.md): Command where the page says
// Ctrl; Option copies a chord as it is dragged; either delete key; Return opens the
// bubble on what is at the spot.
//
// The keyboard. The view is the window's first responder while no chord is being
// typed, and the box in the bubble is while one is. It takes the keyboard when it is
// clicked, and also when the pointer moves over it (unless a box of text elsewhere is
// being typed in): the spot follows the pointer, and the keys act on the spot, so the
// keys are to go where the pointer is, without a click to say so first.

import AppKit
import ChordRules
import CoreText
import Song
import SwiftUI

/// The sheet's colours and sizes: Simple Presenter's (spec/03, rules 6 to 14).
enum SheetLook {
    static func colour(_ hex: UInt32, alpha: CGFloat = 1) -> NSColor {
        NSColor(srgbRed: CGFloat((hex >> 16) & 0xFF) / 255, green: CGFloat((hex >> 8) & 0xFF) / 255, blue: CGFloat(hex & 0xFF) / 255, alpha: alpha)
    }

    static let ground = colour(0x1b1c1f)
    static let strip = colour(0x202226)
    static let card = colour(0x222428)
    static let cardEdge = colour(0x33363c)
    static let pickedEdge = colour(0x8a5a22)
    static let words = colour(0xe6e6e6)
    static let accent = colour(0xff8a1f)
    static let chordName = colour(0xffd9a8)
    static let bubble = colour(0x34363c)
    static let bubbleEdge = colour(0x5c5f66)
    static let lit = colour(0x4a3a26)
    static let rule = colour(0x3a3c42)
    static let number = colour(0x5c5f66)
    static let group = colour(0x2b2d31)
    static let panel = colour(0x2b2d31)
    static let chip = colour(0x3a3c42)
    static let caption = colour(0x8d9097)
    static let quiet = colour(0x9a9da3)

    // (A font is never changed once it is made, which the compiler cannot know.)
    nonisolated(unsafe) static let wordFont = NSFont.systemFont(ofSize: 21)
    nonisolated(unsafe) static let chordFont = NSFont.boldSystemFont(ofSize: 14)
    nonisolated(unsafe) static let groupFont = NSFont.boldSystemFont(ofSize: 12)
    nonisolated(unsafe) static let numberFont = NSFont.systemFont(ofSize: 12)
    nonisolated(unsafe) static let chipFont = NSFont.boldSystemFont(ofSize: 13)
    nonisolated(unsafe) static let fieldFont = NSFont.boldSystemFont(ofSize: 16)

    /// The room at the left of a line for its slide's number.
    static let gutter = 54.0
    /// Either side of a chord's name in its bubble.
    static let bubblePad = 7.0
    static let tail = 6.0
    /// The room inside a card round its lines, and the gap between two cards.
    static let cardPad = 10.0
    static let cardGap = 10.0
    /// Where the column of rows starts on the sheet.
    static let left = 16.0
    static let top = 14.0
    static let groupRow = 34.0
    static let entryWidth = 330.0

    static func height(of font: NSFont) -> Double {
        Double(ceil(font.ascender - font.descender + font.leading))
    }
}

/// How wide things are: each line's words as one Core Text line, and the widths of
/// chords' names.
final class SheetMetrics: SheetMeasure {
    /// Each row's words as a line of text; nil for a row that is a group's name.
    var lines: [CTLine?] = [] {
        didSet { edges = Array(repeating: nil, count: lines.count) }
    }
    /// Where each unit of a row's words starts along its line, and where the line
    /// ends: worked out the first time the row is asked about.
    private var edges: [[Double]?] = []
    private var names: [String: (line: CTLine, width: Double)] = [:]
    let wordHeight = SheetLook.height(of: SheetLook.wordFont)
    let chordHeight = SheetLook.height(of: SheetLook.chordFont)

    var bubbleHeight: Double { chordHeight + 6 }
    /// A line of words with its row of chords over it.
    var lineHeight: Double { bubbleHeight + SheetLook.tail + wordHeight + 6 }

    /// Text as one line, in a colour that is set when it is drawn.
    static func line(_ text: String, font: NSFont) -> CTLine {
        CTLineCreateWithAttributedString(NSAttributedString(string: text, attributes: [
            .font: font,
            NSAttributedString.Key(kCTForegroundColorFromContextAttributeName as String): true,
        ]))
    }

    static func width(of line: CTLine) -> Double {
        CTLineGetTypographicBounds(line, nil, nil, nil)
    }

    func advance(row: Int, units: Int) -> Double {
        guard lines.indices.contains(row), let line = lines[row] else { return 0 }
        let known = edges[row] ?? Self.edges(of: line)
        edges[row] = known
        return known[min(max(units, 0), known.count - 1)]
    }

    /// Where each unit of a line starts, and at the end where the line ends: never
    /// less than the one before.
    ///
    /// Core Text is asked for each, and is not believed where what it says is left of
    /// something earlier in the line. SEEN: for a zero-width space that ends a line
    /// (which real songs have, after their last word, for a chord to hang on) it says
    /// nought, the line's left edge. Such a unit is put where the next one that is
    /// believed starts, which for that one is the end of the line. The halving that
    /// finds the character under the pointer (ChordLayout.characterAt) counts on this
    /// never going backwards.
    static func edges(of line: CTLine) -> [Double] {
        let length = CTLineGetStringRange(line).length
        var edges = (0...length).map { Double(CTLineGetOffsetForStringIndex(line, $0, nil)) }
        var furthest = 0.0
        var believed = edges.map { _ in true }
        for index in edges.indices {
            if edges[index] < furthest {
                believed[index] = false
            } else {
                furthest = edges[index]
            }
        }
        var next = max(furthest, CTLineGetTypographicBounds(line, nil, nil, nil))
        for index in edges.indices.reversed() {
            if believed[index] {
                next = edges[index]
            } else {
                edges[index] = next
            }
        }
        return edges
    }

    func name(_ name: String) -> (line: CTLine, width: Double) {
        if let known = names[name] { return known }
        let line = Self.line(name, font: SheetLook.chordFont)
        let made = (line, Self.width(of: line))
        names[name] = made
        return made
    }

    func bubble(_ name: String) -> Double {
        self.name(name).width + 2 * SheetLook.bubblePad
    }
}

/// A key the sheet answers while no chord is being typed.
enum SheetKey: Equatable {
    case left, right, up, down, delete, open
    case number(Int)
    case letter(String)
    case copy(wholeGroup: Bool), paste, undo, redo
}

@MainActor
final class SheetCanvas: NSView, NSTextFieldDelegate, NSUserInterfaceValidations {
    /// What the view asks of the window's model, which has the song.
    struct Asks {
        var copy: (_ wholeGroup: Bool) -> Void = { _ in }
        var undo: () -> Void = {}
        var redo: () -> Void = {}
        var canUndo: () -> Bool = { false }
        var canRedo: () -> Bool = { false }

        /// As the window wires them.
        @MainActor
        static func of(_ model: LibraryModel) -> Asks {
            Asks(copy: { [model] in model.copyChords(wholeGroup: $0) },
                 undo: { [model] in model.undo() },
                 redo: { [model] in model.redo() },
                 canUndo: { [model] in model.canUndo },
                 canRedo: { [model] in model.canRedo })
        }
    }

    private(set) var sheet: ChordSheet?
    var asks = Asks()
    let metrics = SheetMetrics()
    let entry = SheetEntry()

    /// One row as it is laid out: where it is down the sheet, the room over its line,
    /// and where its chords stand along it.
    struct Laid {
        var top = 0.0
        var height = 0.0
        var lead = 0.0
        var placed = ChordLayout.Placed()
        var chordTop: Double { top + lead + 2 }
    }

    private(set) var laid: [Laid] = []
    private var laidRows: [SheetRow] = []
    private(set) var cardWidth = 0.0
    private var content = NSSize.zero
    /// The typing the bubble is open for: `ChordSheet.typingsBegun` as it was.
    private var typingShown: Int?
    /// The button is down: on which row, on which chord's place if on a bubble, where,
    /// and whether the pointer has since moved far enough to be carrying it.
    private var pressed: (row: Int, chordAt: Int?, point: NSPoint, moved: Bool)?
    /// The keyboard is being moved by this view itself, to the box or back from it:
    /// that is not the person leaving the box for something else.
    private var movingTheKeys = false
    private var tracking: NSTrackingArea?

    override init(frame: NSRect) {
        super.init(frame: frame)
        entry.isHidden = true
        entry.field.delegate = self
        entry.chosen = { [weak self] name in
            self?.sheet?.finishTyping(name, onward: false)
            self?.sync()
        }
        addSubview(entry)
        setAccessibilityLabel("The song's chord sheet")
    }

    @available(*, unavailable)
    required init?(coder: NSCoder) {
        fatalError("not made from a file")
    }

    override var isFlipped: Bool { true }
    override var isOpaque: Bool { true }
    // While a chord is being typed a click on the sheet is not to take the keyboard
    // from the box: what the click does (keep the chord, open the bubble elsewhere) is
    // worked out with the song as it was when it was made (ChordSheet.pick).
    override var acceptsFirstResponder: Bool { sheet?.typing != true }

    /// Shows a song's sheet: another song's from the top, the same one's where it was.
    func show(_ shown: ChordSheet?) {
        if shown !== sheet {
            sheet = shown
            shown?.reveal = { [weak self] in self?.revealSpot() }
            laidRows = []
            laid = []
            typingShown = nil
            pressed = nil
            scroll(.zero)
        }
        sync()
    }

    // ---- Laying out

    /// The card of the slide whose first line is this row: as wide as every other,
    /// and as tall as the slide's own lines.
    func card(ofSlideStartingAt index: Int) -> NSRect {
        guard let sheet, laid.indices.contains(index), sheet.rows[index].kind == .line else { return .zero }
        return NSRect(x: SheetLook.left + SheetLook.gutter - SheetLook.cardPad, y: laid[index].top + SheetLook.cardGap, width: cardWidth,
                      height: Double(sheet.rows[index].count) * metrics.lineHeight + 2 * SheetLook.cardPad)
    }

    private func layOut() {
        guard let sheet else {
            laid = []
            content = .zero
            return
        }
        let rows = sheet.rows
        metrics.lines = rows.map { $0.kind == .line ? SheetMetrics.line($0.text.string, font: SheetLook.wordFont) : nil }
        var made: [Laid] = []
        var top = SheetLook.top
        var widest = 240.0
        for (index, row) in rows.enumerated() {
            var item = Laid(top: top)
            if row.kind == .line {
                // Room over the first line of a slide, for the gap between two cards
                // and the top of its own; and under its last, for the foot of the card.
                item.lead = row.first ? SheetLook.cardGap + SheetLook.cardPad : 0
                let foot = row.last ? SheetLook.cardPad : 0
                item.height = item.lead + metrics.lineHeight + foot
                item.placed = sheet.placed(index, metrics)
                widest = max(widest, item.placed.width + (row.alone ? 50 : 0))
            } else {
                item.height = SheetLook.groupRow
            }
            made.append(item)
            top += item.height
        }
        laid = made
        laidRows = rows
        // All cards are one width: as wide as the widest line of the song with its
        // chords. (Each is as tall as its own slide: `card(ofSlideStartingAt:)`.)
        cardWidth = widest + 2 * SheetLook.cardPad + 8
        content = NSSize(width: SheetLook.left + SheetLook.gutter + cardWidth + 50, height: top + 60)
        fit()
    }

    /// The view is as big as the song, and never smaller than what it is seen through.
    func fit() {
        let seen = enclosingScrollView?.contentView.bounds.size ?? .zero
        let wanted = NSSize(width: max(content.width, seen.width), height: max(content.height, seen.height))
        if frame.size != wanted {
            setFrameSize(wanted)
        }
        placeEntry()
    }

    /// After anything was done: the rows laid out again if they changed, the bubble for
    /// typing opened, moved or closed, and everything drawn.
    func sync() {
        guard let sheet else {
            layOut()
            entry.isHidden = true
            needsDisplay = true
            return
        }
        if sheet.rows != laidRows {
            layOut()
        }
        if sheet.typing, laid.indices.contains(sheet.typingAt.row) {
            entry.show(offered: sheet.offered, picked: sheet.entry.pick, metrics: metrics)
            placeEntry()
            if typingShown != sheet.typingsBegun {
                typingShown = sheet.typingsBegun
                entry.isHidden = false
                movingTheKeys = true
                entry.begin(sheet.entry.text, selected: sheet.entry.selected, in: window)
                movingTheKeys = false
            } else if entry.field.stringValue != sheet.entry.text {
                entry.set(sheet.entry.text)
            }
        } else if typingShown != nil {
            typingShown = nil
            // The keyboard comes back to the sheet, if the box had it. (Asked before the
            // bubble is hidden: a box that is hidden gives the keyboard up to the window.)
            let hadTheKeys = window.map { $0.firstResponder === entry.field.currentEditor() || $0.firstResponder === entry.field } ?? false
            movingTheKeys = true
            entry.isHidden = true
            if hadTheKeys {
                window?.makeFirstResponder(self)
            }
            movingTheKeys = false
        }
        needsDisplay = true
    }

    /// Puts the bubble a chord is typed in at the place the chord is for. It grows
    /// upwards from where the chord will stand, so that the word being given the chord,
    /// and the rest of its line, stay in sight; and it is kept within what is seen.
    private func placeEntry() {
        guard let sheet, sheet.typing, laid.indices.contains(sheet.typingAt.row) else { return }
        let item = laid[sheet.typingAt.row]
        var x = SheetLook.left + SheetLook.gutter + sheet.xOf(sheet.typingAt.row, sheet.typingAt.at, metrics)
        var y = item.chordTop + metrics.bubbleHeight + 3 - entry.frame.height
        let seen = visibleRect
        // (Before the view has been given its size there is nothing to keep it within.)
        if seen.width > entry.frame.width {
            x = max(seen.minX + 4, min(x, seen.maxX - entry.frame.width - 12))
            y = max(seen.minY + 4, y)
        }
        entry.setFrameOrigin(NSPoint(x: x, y: max(4, y)))
    }

    /// Brings the spot into view, when the keys have moved it.
    private func revealSpot() {
        guard let sheet, laid.indices.contains(sheet.spot.row) else { return }
        if sheet.rows != laidRows {
            layOut()
        }
        let item = laid[sheet.spot.row]
        let seen = visibleRect
        var to = seen.origin
        if item.top < seen.minY + 10 {
            to.y = max(0, item.top - 30)
        } else if item.top + item.height > seen.maxY - 10 {
            to.y = min(max(0, bounds.height - seen.height), item.top + item.height - seen.height + 30)
        }
        let x = SheetLook.left + SheetLook.gutter + sheet.xOf(sheet.spot.row, sheet.spot.at, metrics)
        if x < seen.minX + SheetLook.gutter {
            to.x = max(0, x - SheetLook.gutter - SheetLook.left)
        } else if x + 60 > seen.maxX {
            to.x = min(max(0, bounds.width - seen.width), x + 120 - seen.width)
        }
        if to != seen.origin {
            scroll(to)
        }
    }

    // ---- What is where

    private func row(atY y: Double) -> Int? {
        laid.firstIndex { y >= $0.top && y < $0.top + $0.height }
    }

    /// The place of a line under a point: a chord's, if the point is on its bubble;
    /// otherwise the character there, or with Shift the word. Nothing beside the cards,
    /// which is bare sheet, or on a row that can take no chord.
    func place(at point: NSPoint, shift: Bool) -> (row: Int, at: Int, onChord: Bool)? {
        guard let sheet, let index = row(atY: point.y), sheet.usable(index) else { return nil }
        let x = point.x - SheetLook.left
        guard x >= 0, x < SheetLook.gutter + cardWidth - SheetLook.cardPad else { return nil }
        let item = laid[index]
        let along = x - SheetLook.gutter
        if point.y >= item.chordTop, point.y < item.chordTop + metrics.bubbleHeight + SheetLook.tail {
            for (n, chord) in sheet.rows[index].chords.enumerated() where along >= item.placed.xs[n] && along < item.placed.xs[n] + item.placed.widths[n] {
                return (index, chord.at, true)
            }
        }
        return (index, sheet.placeAt(index, along: along, exact: !shift, metrics), false)
    }

    /// A point of the view that is over a place of a line: in the middle of its
    /// character, down among the words; or, `onItsChord`, in the bubble of the chord
    /// that stands there. (What the tests point at.)
    func point(over spot: Spot, onItsChord: Bool = false) -> NSPoint? {
        guard let sheet, laid.indices.contains(spot.row), sheet.usable(spot.row) else { return nil }
        let item = laid[spot.row]
        let row = sheet.rows[spot.row]
        let lineLeft = SheetLook.left + SheetLook.gutter
        if onItsChord {
            guard let n = row.chords.firstIndex(where: { $0.at == spot.at }) else { return nil }
            return NSPoint(x: lineLeft + item.placed.xs[n] + item.placed.widths[n] / 2, y: item.chordTop + metrics.bubbleHeight / 2)
        }
        let wordsMiddle = item.chordTop + metrics.bubbleHeight + SheetLook.tail + metrics.wordHeight / 2
        if row.alone {
            return NSPoint(x: lineLeft + sheet.xOf(spot.row, spot.at, metrics) + 2, y: wordsMiddle)
        }
        let from = metrics.advance(row: spot.row, units: spot.at)
        let to = metrics.advance(row: spot.row, units: min(spot.at + 1, row.text.length))
        return NSPoint(x: lineLeft + (from + to) / 2, y: wordsMiddle)
    }

    /// The same for a chord that is being carried: the line under the pointer wherever
    /// across the sheet that is, and never another chord's bubble for itself.
    private func placeCarriedTo(_ point: NSPoint, shift: Bool) -> (row: Int, at: Int)? {
        guard let sheet, let index = row(atY: point.y), sheet.usable(index) else { return nil }
        return (index, sheet.placeAt(index, along: point.x - SheetLook.left - SheetLook.gutter, exact: !shift, metrics))
    }

    // ---- The pointer

    /// The pointer has moved, with no button down.
    func pointerMoved(to point: NSPoint, shift: Bool) {
        guard let sheet else { return }
        takeTheKeysIfNobodyIsTyping()
        if let place = place(at: point, shift: shift) {
            sheet.pointerOver(place.row, place.at)
            (place.onChord ? NSCursor.pointingHand : NSCursor.arrow).set()
        } else {
            NSCursor.arrow.set()
        }
        needsDisplay = true
    }

    func pressed(at point: NSPoint, shift: Bool) {
        guard let sheet else { return }
        if !sheet.typing {
            window?.makeFirstResponder(self)
        }
        if let place = place(at: point, shift: shift) {
            sheet.setSpot(place.row, place.at)
            pressed = (place.row, place.onChord ? place.at : nil, point, false)
        } else {
            pressed = (-1, nil, point, false)
        }
        needsDisplay = true
    }

    func dragged(to point: NSPoint, shift: Bool) {
        guard let sheet, var down = pressed, let chordAt = down.chordAt else { return }
        // A chord is not carried off while another is being typed: letting go of it
        // then is a click on it, which keeps the one and opens this one.
        if sheet.typing { return }
        // It is carried once the pointer has gone 6 with the button down.
        if !down.moved && abs(point.x - down.point.x) + abs(point.y - down.point.y) < 6 { return }
        down.moved = true
        pressed = down
        if sheet.dragged == nil {
            sheet.lift(down.row, chordAt)
        }
        if let place = placeCarriedTo(point, shift: shift) {
            sheet.carry(over: place.row, place.at)
        }
        NSCursor.closedHand.set()
        needsDisplay = true
    }

    /// The button is let go. `copy`: the key that copies as it drags is held.
    func released(at point: NSPoint, shift: Bool, copy: Bool) {
        guard let sheet, let down = pressed else { return }
        pressed = nil
        if sheet.dragged != nil {
            sheet.drop(copy: copy)
            NSCursor.arrow.set()
        } else if let chordAt = down.chordAt {
            if !down.moved {
                sheet.pick(down.row, chordAt)
            }
        } else if down.row >= 0 {
            // A click on a place of a line: the bubble opens there.
            if let place = place(at: point, shift: shift), place.row == down.row {
                sheet.pick(place.row, place.at)
            }
        } else if place(at: point, shift: shift) == nil {
            // A click on the bare sheet keeps the chord that was being typed.
            sheet.settle()
        }
        sync()
    }

    private func takeTheKeysIfNobodyIsTyping() {
        guard let window, window.isKeyWindow, sheet?.typing != true, window.firstResponder !== self else { return }
        // (A box of text that is being typed in has the window's field editor as the
        // first responder, which is a text view.)
        if window.firstResponder is NSText { return }
        window.makeFirstResponder(self)
    }

    override func updateTrackingAreas() {
        super.updateTrackingAreas()
        if let tracking {
            removeTrackingArea(tracking)
        }
        let area = NSTrackingArea(rect: .zero, options: [.mouseMoved, .activeInKeyWindow, .inVisibleRect], owner: self)
        addTrackingArea(area)
        tracking = area
    }

    override func mouseMoved(with event: NSEvent) {
        pointerMoved(to: convert(event.locationInWindow, from: nil), shift: event.modifierFlags.contains(.shift))
    }

    override func mouseDown(with event: NSEvent) {
        pressed(at: convert(event.locationInWindow, from: nil), shift: event.modifierFlags.contains(.shift))
    }

    override func mouseDragged(with event: NSEvent) {
        dragged(to: convert(event.locationInWindow, from: nil), shift: event.modifierFlags.contains(.shift))
        if sheet?.dragged != nil {
            autoscroll(with: event)
        }
    }

    override func mouseUp(with event: NSEvent) {
        released(at: convert(event.locationInWindow, from: nil), shift: event.modifierFlags.contains(.shift),
                 copy: event.modifierFlags.contains(.option))
    }

    // ---- The keys

    /// A key pressed with no chord being typed.
    func key(_ key: SheetKey, shift: Bool = false) {
        guard let sheet else { return }
        switch key {
        case .left: sheet.step(-1, exact: shift)
        case .right: sheet.step(1, exact: shift)
        case .up: sheet.stepLine(-1, metrics)
        case .down: sheet.stepLine(1, metrics)
        case .delete: sheet.takeAtSpot()
        case .open: sheet.startTyping()
        case .number(let number): sheet.putOfKey(number)
        case .letter(let letter): sheet.startTyping(seed: letter.uppercased())
        case .copy(let wholeGroup): asks.copy(wholeGroup)
        case .paste: sheet.paste()
        case .undo: asks.undo()
        case .redo: asks.redo()
        }
        sync()
    }

    /// Which key of the sheet's an event is, if any.
    static func key(of event: NSEvent) -> (key: SheetKey, shift: Bool)? {
        let flags = event.modifierFlags.intersection([.command, .control, .option, .shift])
        let shift = flags.contains(.shift)
        let typed = event.charactersIgnoringModifiers?.lowercased() ?? ""
        if flags.contains(.command) {
            guard flags.isSubset(of: [.command, .shift]) else { return nil }
            switch typed {
            case "c": return (.copy(wholeGroup: shift), shift)
            case "v" where !shift: return (.paste, false)
            case "z": return (shift ? .redo : .undo, shift)
            default: return nil
            }
        }
        if flags.contains(.control) || flags.contains(.option) { return nil }
        switch event.specialKey {
        case .leftArrow: return (.left, shift)
        case .rightArrow: return (.right, shift)
        case .upArrow: return (.up, shift)
        case .downArrow: return (.down, shift)
        case .delete, .deleteForward, .backspace: return (.delete, shift)
        case .carriageReturn, .enter, .newline: return (.open, shift)
        default: break
        }
        if typed.count == 1, let number = Int(typed), (1...7).contains(number) { return (.number(number), shift) }
        if typed.count == 1, "abcdefg".contains(typed) { return (.letter(typed), shift) }
        return nil
    }

    override func keyDown(with event: NSEvent) {
        if let pressed = Self.key(of: event) {
            key(pressed.key, shift: pressed.shift)
        } else if !event.modifierFlags.intersection([.command, .control]).isEmpty {
            super.keyDown(with: event)
        }
        // (Any other key is nothing here, and is let go without a sound.)
    }

    // Command with C, V and Z is asked of the views of a window before its menus are.
    // The sheet answers them itself while it has the keyboard, so that they work
    // whatever the menus make of them.
    override func performKeyEquivalent(with event: NSEvent) -> Bool {
        guard window?.firstResponder === self, event.type == .keyDown, event.modifierFlags.contains(.command),
              let pressed = Self.key(of: event) else {
            return super.performKeyEquivalent(with: event)
        }
        key(pressed.key, shift: pressed.shift)
        return true
    }

    // And the same four from the Edit menu, when they are chosen there with the mouse.
    @objc func copy(_ sender: Any?) { key(.copy(wholeGroup: false)) }
    @objc func paste(_ sender: Any?) { key(.paste) }
    @objc func undo(_ sender: Any?) { key(.undo) }
    @objc func redo(_ sender: Any?) { key(.redo) }

    func validateUserInterfaceItem(_ item: any NSValidatedUserInterfaceItem) -> Bool {
        switch item.action {
        case #selector(undo(_:)): asks.canUndo()
        case #selector(redo(_:)): asks.canRedo()
        case #selector(copy(_:)): sheet.map { $0.usable($0.spot.row) } ?? false
        case #selector(paste(_:)): sheet.map { $0.copied != nil && $0.usable($0.spot.row) } ?? false
        default: true
        }
    }

    // ---- The box a chord is typed in

    func controlTextDidChange(_ notification: Notification) {
        guard let sheet, sheet.typing else { return }
        let editor = entry.field.currentEditor() as? NSTextView
        // (A character that is still being put together, an accent waiting for its
        // letter, is not yet what was typed.)
        if editor?.hasMarkedText() == true { return }
        let typed = entry.field.stringValue
        let caret = editor?.selectedRange().location ?? (typed as NSString).length
        let held = sheet.type(typed)
        if held != typed {
            // Tidied, the caret stays where it was; undone, it goes back the one place
            // the key had moved it.
            let tidied = Array(held.utf16).count == Array(typed.utf16).count
            entry.set(held, caret: tidied ? caret : max(0, caret - 1))
        }
        sync()
    }

    func control(_ control: NSControl, textView: NSTextView, doCommandBy selector: Selector) -> Bool {
        guard let sheet, sheet.typing else { return false }
        let atEnd = textView.selectedRange().length == 0 && textView.selectedRange().location == (textView.string as NSString).length
        let key: ChordSheet.EntryKey
        switch selector {
        case #selector(NSResponder.cancelOperation(_:)): key = .escape
        case #selector(NSResponder.insertNewline(_:)): key = .enter
        case #selector(NSResponder.insertTab(_:)): key = .tab
        case #selector(NSResponder.moveDown(_:)): key = .down
        case #selector(NSResponder.moveUp(_:)): key = .up
        case #selector(NSResponder.moveRight(_:)): key = .right
        case #selector(NSResponder.deleteBackward(_:)), #selector(NSResponder.deleteForward(_:)): key = .delete
        // (Shift with Tab would take the keyboard out of the box, and so end the
        // typing by the back door.)
        case #selector(NSResponder.insertBacktab(_:)): return true
        default: return false
        }
        let answered = sheet.press(key, caretAtEnd: atEnd)
        if answered {
            sync()
        }
        return answered
    }

    func controlTextDidEndEditing(_ notification: Notification) {
        // The keyboard has gone from the box to something else than the sheet: leaving
        // the sheet for anything else keeps the chord that was being typed.
        guard let sheet, sheet.typing, !movingTheKeys else { return }
        sheet.settle()
        sync()
    }

    // ---- Drawing

    private func rounded(_ rect: NSRect, radius: Double, fill: NSColor?, edge: NSColor?, alpha: Double = 1) {
        // (An edge one wide is drawn down the middle of its pixels.)
        let path = NSBezierPath(roundedRect: rect.insetBy(dx: 0.5, dy: 0.5), xRadius: radius, yRadius: radius)
        if let fill {
            fill.withAlphaComponent(fill.alphaComponent * alpha).setFill()
            path.fill()
        }
        if let edge {
            edge.withAlphaComponent(edge.alphaComponent * alpha).setStroke()
            path.lineWidth = 1
            path.stroke()
        }
    }

    /// Draws a line of text with its top left corner at a point.
    static func draw(_ line: CTLine, at point: NSPoint, font: NSFont, colour: NSColor, in context: CGContext) {
        context.saveGState()
        context.setFillColor(colour.cgColor)
        // (The view counts down from its top; Core Text draws upwards.)
        context.textMatrix = CGAffineTransform(scaleX: 1, y: -1)
        context.textPosition = CGPoint(x: point.x, y: point.y + ceil(font.ascender))
        CTLineDraw(line, context)
        context.restoreGState()
    }

    private func bubble(name: String, x: Double, top: Double, width: Double, lit: Bool, edge: NSColor, alpha: Double, in context: CGContext) {
        rounded(NSRect(x: x, y: top, width: width, height: metrics.bubbleHeight), radius: 6, fill: lit ? SheetLook.lit : SheetLook.bubble, edge: edge, alpha: alpha)
        let text = metrics.name(name)
        Self.draw(text.line, at: NSPoint(x: x + (width - text.width) / 2, y: top + (metrics.bubbleHeight - metrics.chordHeight) / 2),
                  font: SheetLook.chordFont, colour: SheetLook.chordName.withAlphaComponent(alpha), in: context)
    }

    override func draw(_ dirtyRect: NSRect) {
        SheetLook.ground.setFill()
        dirtyRect.fill()
        guard let sheet, let context = NSGraphicsContext.current?.cgContext, laid.count == sheet.rows.count else { return }
        let rows = sheet.rows
        let left = SheetLook.left
        let lineLeft = left + SheetLook.gutter

        // The cards first: each is drawn for its slide's first line and reaches down
        // behind the rest of them.
        for (index, row) in rows.enumerated() where row.kind == .line && row.first {
            let card = card(ofSlideStartingAt: index)
            guard card.intersects(dirtyRect) else { continue }
            rounded(card, radius: 8, fill: SheetLook.card, edge: row.slide == sheet.picked ? SheetLook.pickedEdge : SheetLook.cardEdge)
        }

        for (index, row) in rows.enumerated() {
            let item = laid[index]
            guard NSRect(x: 0, y: item.top - 4, width: bounds.width, height: item.height + 8).intersects(dirtyRect) else { continue }
            if row.kind == .group {
                // The name of a group, in its colour
                let name = SheetMetrics.line(row.name, font: SheetLook.groupFont)
                let width = SheetMetrics.width(of: name)
                let colour = row.colour.map { NSColor(srgbRed: $0.red, green: $0.green, blue: $0.blue, alpha: $0.alpha) } ?? SheetLook.group
                rounded(NSRect(x: left, y: item.top + 10, width: width + 20, height: 22), radius: 4, fill: colour, edge: nil)
                Self.draw(name, at: NSPoint(x: left + 10, y: item.top + 10 + (22 - SheetLook.height(of: SheetLook.groupFont)) / 2),
                          font: SheetLook.groupFont, colour: .white, in: context)
                continue
            }
            let wordsTop = item.chordTop + metrics.bubbleHeight + SheetLook.tail
            if row.first {
                // The slide's number, beside its card
                let number = SheetMetrics.line(String(row.slide + 1), font: SheetLook.numberFont)
                Self.draw(number, at: NSPoint(x: left + SheetLook.gutter - 20 - SheetMetrics.width(of: number), y: wordsTop + 4),
                          font: SheetLook.numberFont, colour: row.slide == sheet.picked ? SheetLook.accent : SheetLook.number, in: context)
            }
            if row.alone {
                // A line of chords alone has nothing to read: a rule stands where its
                // words would be.
                SheetLook.rule.setFill()
                NSRect(x: lineLeft, y: wordsTop + metrics.wordHeight / 2, width: max(60, item.placed.width + 40), height: 1).fill()
            } else if let line = metrics.lines[index] {
                Self.draw(line, at: NSPoint(x: lineLeft, y: wordsTop), font: SheetLook.wordFont, colour: SheetLook.words, in: context)
            }
            // The chords, each a bubble with its tail on its character
            for (n, chord) in row.chords.enumerated() {
                let x = item.placed.xs[n]
                let width = item.placed.widths[n]
                let atSpot = sheet.spot.row == index && sheet.spot.at == chord.at
                let lifted = sheet.dragged.map { $0.from == Spot(row: index, at: chord.at) } ?? false
                let alpha = lifted ? 0.35 : 1
                let edge = atSpot ? SheetLook.accent : SheetLook.bubbleEdge
                // Where its character is, which is where the tail points even when the
                // bubble has had to move right of it
                let character = row.alone ? x + width / 2 : metrics.advance(row: index, units: min(chord.at, row.text.length))
                edge.withAlphaComponent(alpha).setFill()
                NSRect(x: lineLeft + x + max(3, min(width - 5, character - x + 1)), y: item.chordTop + metrics.bubbleHeight - 1,
                       width: 2, height: SheetLook.tail + 3).fill()
                bubble(name: chord.name, x: lineLeft + x, top: item.chordTop, width: width, lit: atSpot, edge: edge, alpha: alpha, in: context)
            }
        }

        // The spot, where there is no chord yet: a bubble in outline with its tail on
        // the character. Where there is one, that chord's own bubble is lit instead.
        // It is shown while a chord is being typed as well, wherever the pointer has
        // gone on to, and not where the typing is, which has its own bubble.
        let spot = sheet.spot
        guard laid.indices.contains(spot.row), sheet.usable(spot.row) else { return }
        let free = sheet.chordAt(spot.row, spot.at).isEmpty
        let underTyping = sheet.typing && spot == sheet.typingAt
        guard !underTyping, free || sheet.dragged != nil else { return }
        let item = laid[spot.row]
        let x = lineLeft + sheet.xOf(spot.row, spot.at, metrics)
        let carried = sheet.dragged?.name
        let width = max(26, carried.map { metrics.bubble($0) } ?? 0)
        SheetLook.accent.withAlphaComponent(0.75).setFill()
        NSRect(x: x + 3, y: item.chordTop + metrics.bubbleHeight - 1, width: 2, height: SheetLook.tail + metrics.wordHeight + 3).fill()
        if let carried {
            bubble(name: carried, x: x, top: item.chordTop, width: width, lit: true, edge: SheetLook.accent, alpha: 1, in: context)
        } else {
            rounded(NSRect(x: x, y: item.chordTop, width: width, height: metrics.bubbleHeight), radius: 6, fill: nil, edge: SheetLook.accent, alpha: 0.75)
            let plus = metrics.name("+")
            Self.draw(plus.line, at: NSPoint(x: x + (width - plus.width) / 2, y: item.chordTop + (metrics.bubbleHeight - metrics.chordHeight) / 2),
                      font: SheetLook.chordFont, colour: SheetLook.accent.withAlphaComponent(0.75), in: context)
        }
    }
}

/// The bubble a chord is typed in: a one-line box, and over it what the chord might be
/// going to be, as a row of buttons that wraps (spec/03, rule 29).
@MainActor
final class SheetEntry: NSView {
    let field = NSTextField()
    /// One that is offered was clicked.
    var chosen: (String) -> Void = { _ in }
    private var chips: [(name: String, line: CTLine, width: Double, rect: NSRect)] = []
    private var picked = -1

    override init(frame: NSRect) {
        super.init(frame: frame)
        field.isBordered = false
        field.drawsBackground = false
        field.focusRingType = .none
        field.font = SheetLook.fieldFont
        field.textColor = SheetLook.chordName
        field.cell?.usesSingleLineMode = true
        field.cell?.isScrollable = true
        field.cell?.wraps = false
        field.setAccessibilityLabel("The chord being typed")
        addSubview(field)
    }

    @available(*, unavailable)
    required init?(coder: NSCoder) {
        fatalError("not made from a file")
    }

    override var isFlipped: Bool { true }

    /// Lays out what is offered, and sizes the bubble to hold it.
    func show(offered: [String], picked: Int, metrics: SheetMetrics) {
        self.picked = picked
        var made: [(name: String, line: CTLine, width: Double, rect: NSRect)] = []
        var x = 8.0
        var y = 8.0
        for name in offered {
            let line = SheetMetrics.line(name, font: SheetLook.chipFont)
            let text = SheetMetrics.width(of: line)
            let width = max(34, text + 16)
            if x > 8, x + width > SheetLook.entryWidth - 8 {
                x = 8
                y += 26 + 5
            }
            made.append((name, line, text, NSRect(x: x, y: y, width: width, height: 26)))
            x += width + 5
        }
        chips = made
        let offers = made.isEmpty ? 0 : y + 26 - 8
        let height = 24 + 14 + (made.isEmpty ? 0 : offers + 8)
        setFrameSize(NSSize(width: SheetLook.entryWidth, height: height))
        field.frame = NSRect(x: 10, y: height - 24 - 7, width: SheetLook.entryWidth - 20, height: 24)
        needsDisplay = true
    }

    /// Opens the box on some text: all of it selected to be typed over, or the caret
    /// at its end.
    func begin(_ text: String, selected: Bool, in window: NSWindow?) {
        field.stringValue = text
        guard let window else { return }
        // (If the box is being typed in already, it stays so: this is the next chord.)
        if field.currentEditor() == nil {
            window.makeFirstResponder(field)
        }
        let editor = field.currentEditor() as? NSTextView
        editor?.insertionPointColor = SheetLook.chordName
        let length = (text as NSString).length
        editor?.setSelectedRange(selected ? NSRange(location: 0, length: length) : NSRange(location: length, length: 0))
    }

    /// Puts other text in the box, with the caret at a place or at its end.
    func set(_ text: String, caret: Int? = nil) {
        field.stringValue = text
        let length = (text as NSString).length
        field.currentEditor()?.selectedRange = NSRange(location: min(caret ?? length, length), length: 0)
    }

    override func mouseDown(with event: NSEvent) {
        // (A click on the bubble itself goes no further than the bubble.)
        let point = convert(event.locationInWindow, from: nil)
        if let chip = chips.first(where: { $0.rect.contains(point) }) {
            chosen(chip.name)
        }
    }

    override func draw(_ dirtyRect: NSRect) {
        guard let context = NSGraphicsContext.current?.cgContext else { return }
        let panel = NSBezierPath(roundedRect: bounds.insetBy(dx: 0.5, dy: 0.5), xRadius: 8, yRadius: 8)
        SheetLook.panel.setFill()
        panel.fill()
        SheetLook.accent.setStroke()
        panel.lineWidth = 1
        panel.stroke()
        for (n, chip) in chips.enumerated() {
            (n == picked ? SheetLook.accent : SheetLook.chip).setFill()
            NSBezierPath(roundedRect: chip.rect, xRadius: 6, yRadius: 6).fill()
            SheetCanvas.draw(chip.line, at: NSPoint(x: chip.rect.minX + (chip.rect.width - chip.width) / 2,
                                                    y: chip.rect.minY + (26 - SheetLook.height(of: SheetLook.chipFont)) / 2),
                             font: SheetLook.chipFont, colour: n == picked ? SheetLook.ground : SheetLook.words, in: context)
        }
    }
}

/// The scroll view the sheet is in: it scrolls both ways, and keeps the sheet at least
/// as big as itself.
final class SheetScroll: NSScrollView {
    let canvas = SheetCanvas(frame: NSRect(x: 0, y: 0, width: 600, height: 400))

    override init(frame: NSRect) {
        super.init(frame: frame)
        documentView = canvas
        hasVerticalScroller = true
        hasHorizontalScroller = true
        autohidesScrollers = true
        borderType = .noBorder
        drawsBackground = true
        backgroundColor = SheetLook.ground
        // (The sheet is dark whatever the system is: spec/10-platforms.md.)
        appearance = NSAppearance(named: .darkAqua)
    }

    @available(*, unavailable)
    required init?(coder: NSCoder) {
        fatalError("not made from a file")
    }

    override func tile() {
        super.tile()
        canvas.fit()
    }
}

/// The sheet in the window.
struct ChordSheetCanvasView: NSViewRepresentable {
    var model: LibraryModel
    /// Read so that the view is brought up to date whenever it goes up.
    var version: Int

    func makeNSView(context: Context) -> SheetScroll {
        let scroll = SheetScroll(frame: NSRect(x: 0, y: 0, width: 600, height: 400))
        scroll.canvas.asks = .of(model)
        scroll.canvas.show(model.sheet)
        return scroll
    }

    func updateNSView(_ scroll: SheetScroll, context: Context) {
        scroll.canvas.asks = .of(model)
        scroll.canvas.show(model.sheet)
    }
}

/// The strip across the top of the sheet: the key's own chords on the keys 1 to 7,
/// which can be clicked there too (spec/03, rule 14). The key itself is set in the
/// song's header, over this, and nowhere else: the reference has its picker here, and
/// these apps had it in both places until the maintainer said one was enough.
struct SheetStrip: View {
    var model: LibraryModel
    var sheet: ChordSheet
    /// The key the song states, or "" for none.
    var key: String

    var body: some View {
        HStack(spacing: 8) {
            // (Nothing in the strip is ever squeezed or wrapped. Where the pane is too
            // narrow for all of it, it scrolls sideways.)
            ScrollView(.horizontal, showsIndicators: false) {
                chords
            }
            if key.isEmpty {
                Text("The song names no key: pick the one its chords are in")
                    .font(.system(size: 12))
                    .foregroundStyle(Color(nsColor: SheetLook.caption))
                    .lineLimit(1)
                    .truncationMode(.tail)
                    .layoutPriority(-1)
            }
        }
        .padding(.horizontal, 14)
        .frame(height: 44)
        .frame(maxWidth: .infinity)
        .background(Color(nsColor: SheetLook.strip))
        .environment(\.colorScheme, .dark)
    }

    private var chords: some View {
        HStack(spacing: 8) {
            ForEach(Array(sheet.inKey.enumerated()), id: \.offset) { index, name in
                Button {
                    model.putOfKey(index + 1)
                } label: {
                    HStack(spacing: 5) {
                        Text(String(index + 1))
                            .font(.system(size: 11))
                            .foregroundStyle(Color(nsColor: SheetLook.caption))
                        Text(name)
                            .font(.system(size: 13, weight: .bold))
                            .foregroundStyle(Color(nsColor: SheetLook.words))
                    }
                    .lineLimit(1)
                    .fixedSize()
                    .padding(.horizontal, 8)
                    .frame(minWidth: 34, minHeight: 26)
                    .background(RoundedRectangle(cornerRadius: 6).fill(Color(nsColor: SheetLook.chip)))
                }
                .buttonStyle(.plain)
                .help("Puts \(name) on the spot. The key \(index + 1) does the same.")
            }
        }
        .fixedSize()
    }
}
