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
// How it is laid out (spec/03, "How it looks"): the slides are a grid, as the slides
// of a presentation are in Simple Presenter's main window and in ProPresenter's, but
// each is its words at a size to be read, with room over every line for its chords. A
// slide is a card in a frame of its group's colour, with the slide's number on the
// frame's foot and, on the first slide of each group, the group's name. As many go to
// a row as fit across, so a wider window, or smaller words (the sheet zooms), shows
// more of the song at once. All the slides are one width, and no wider than the widest
// of them needs: no room is kept for what might be put on a line later. That is the maintainer's, on trying the reference's layout
// with real songs: one column of slides left most of a window empty and showed few
// words. How a LINE is drawn (words, bubbles, tails, the spot) is the reference's
// (qml/ChordSheet.qml), number for number, times the zoom. But for one thing the
// reference has not: the line the person is at, the highlighted line, has its words in
// a colour of its own (spec/03, rule 53f; SheetHints.swift says which line that is).
//
// The same numbers, for drawing and for pointing. Each line's words are one Core Text
// line (`SheetMetrics`). That line is what is drawn, and how far along it a character
// is, is asked of that same line; so a tail stands on the character the pointer chose.
// A place is a UTF-16 unit, which is what Core Text counts in too.
//
// What the Mac does its own way (spec/10-platforms.md): Command where the page says
// Ctrl; Option copies a chord as it is dragged; either delete key; Return opens the
// bubble on what is at the spot; and one of the key's chords is carried from the strip
// to a word by the system's own dragging, which this view is the far end of ("A chord
// carried in from the strip", below; the near end is KeyChordGrip.swift).
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

/// The sheet's colours, and the sizes that do not change with the zoom: Simple
/// Presenter's (spec/03, "How it looks"). What grows and shrinks with the words is in
/// `SheetMetrics`.
enum SheetLook {
    static func colour(_ hex: UInt32, alpha: CGFloat = 1) -> NSColor {
        NSColor(srgbRed: CGFloat((hex >> 16) & 0xFF) / 255, green: CGFloat((hex >> 8) & 0xFF) / 255, blue: CGFloat(hex & 0xFF) / 255, alpha: alpha)
    }

    static let ground = colour(0x1b1c1f)
    static let strip = colour(0x202226)
    static let card = colour(0x222428)
    static let words = colour(0xe6e6e6)
    static let accent = colour(0xff8a1f)
    static let chordName = colour(0xffd9a8)
    static let bubble = colour(0x34363c)
    static let bubbleEdge = colour(0x5c5f66)
    static let lit = colour(0x4a3a26)
    static let rule = colour(0x3a3c42)
    /// The frame of a slide whose group has no colour.
    static let group = colour(0x2b2d31)
    static let panel = colour(0x2b2d31)
    static let chip = colour(0x3a3c42)
    static let caption = colour(0x8d9097)
    static let quiet = colour(0x9a9da3)

    // (A font is never changed once it is made, which the compiler cannot know.)
    nonisolated(unsafe) static let labelFont = NSFont.systemFont(ofSize: 12)
    nonisolated(unsafe) static let chipFont = NSFont.boldSystemFont(ofSize: 13)
    nonisolated(unsafe) static let fieldFont = NSFont.boldSystemFont(ofSize: 16)

    /// The sizes of the words and of a chord's name where the sheet is not zoomed.
    static let wordSize = 21.0
    static let chordSize = 14.0
    /// How thick a slide's frame is at its sides and its top, and how tall its foot,
    /// where the slide's number is and its group's name.
    static let frame = 4.0
    static let label = 22.0
    /// The room each frame keeps clear all round it (so two frames are twice this
    /// apart), and the sheet's own margin outside that.
    static let around = 6.0
    static let margin = 6.0
    /// The sheet's margin at its top, which is more: the bubble a chord is typed in
    /// grows upwards from the line it is for, and over the first row of slides it
    /// would otherwise have nowhere to grow to but down over the words.
    static let top = 30.0
    /// The shape of a card: as tall for its width as a screen is, 9 to 16. But a card
    /// is not made taller for its shape's sake than this many lines need, or a song
    /// of long lines would have cards mostly empty.
    static let shape = 9.0 / 16.0
    static let shapedUpTo = 4.0
    static let entryWidth = 330.0

    static func height(of font: NSFont) -> Double {
        Double(ceil(font.ascender - font.descender + font.leading))
    }

    /// Whether words on a colour are to be black: it is a light one.
    static func isLight(_ colour: NSColor) -> Bool {
        guard let plain = colour.usingColorSpace(.sRGB) else { return false }
        return 0.299 * plain.redComponent + 0.587 * plain.greenComponent + 0.114 * plain.blueComponent > 0.6
    }
}

/// How big things are at one zoom: the two fonts, the sizes that go with them, each
/// line's words as one Core Text line, and the widths of chords' names.
final class SheetMetrics: SheetMeasure {
    /// How many times its usual size everything is.
    let scale: Double
    let wordFont: NSFont
    let chordFont: NSFont
    let wordHeight: Double
    let chordHeight: Double

    init(scale: Double) {
        self.scale = scale
        wordFont = NSFont.systemFont(ofSize: SheetLook.wordSize * scale)
        chordFont = NSFont.boldSystemFont(ofSize: SheetLook.chordSize * scale)
        wordHeight = SheetLook.height(of: wordFont)
        chordHeight = SheetLook.height(of: chordFont)
    }

    /// Either side of a chord's name in its bubble.
    var bubblePad: Double { 7 * scale }
    var tail: Double { 6 * scale }
    /// The room inside a card round its lines.
    var cardPad: Double { 10 * scale }
    /// Each row's words as a line of text; nil for a row that is a group's name.
    var lines: [CTLine?] = [] {
        didSet { edges = Array(repeating: nil, count: lines.count) }
    }
    /// Where each unit of a row's words starts along its line, and where the line
    /// ends: worked out the first time the row is asked about.
    private var edges: [[Double]?] = []
    private var names: [String: (line: CTLine, width: Double)] = [:]

    var bubbleHeight: Double { chordHeight + 6 * scale }
    /// A line of words with its row of chords over it.
    var lineHeight: Double { bubbleHeight + tail + wordHeight + 6 * scale }

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
        let line = Self.line(name, font: chordFont)
        let made = (line, Self.width(of: line))
        names[name] = made
        return made
    }

    func bubble(_ name: String) -> Double {
        self.name(name).width + 2 * bubblePad
    }
}

/// A key the sheet answers while no chord is being typed.
enum SheetKey: Equatable {
    case left, right, up, down, delete, open
    case number(Int)
    case letter(String)
    case copy(wholeGroup: Bool), paste, undo, redo
    /// The words a step larger (1) or smaller (-1), or their usual size (0).
    case zoom(Int)
}

@MainActor
final class SheetCanvas: NSView, NSTextFieldDelegate, NSUserInterfaceValidations {
    /// What the view asks of the window's model, which has the song.
    struct Asks {
        var copy: (_ wholeGroup: Bool) -> Void = { _ in }
        var paste: () -> Void = {}
        var undo: () -> Void = {}
        var redo: () -> Void = {}
        var canUndo: () -> Bool = { false }
        var canRedo: () -> Bool = { false }
        /// The words a step larger or smaller, or their usual size: answers with how
        /// many times their usual size they then are.
        var zoom: (_ direction: Int) -> Double? = { _ in nil }
        /// What is going on in the sheet, and what a paste there would do, for the
        /// line of hints under it; and whether Option or Command is held down, which
        /// that line answers to.
        var situation: (SheetHints.Situation, SheetHints.Paste) -> Void = { _, _ in }
        var held: (Bool) -> Void = { _ in }

        /// As the window wires them.
        @MainActor
        static func of(_ model: LibraryModel) -> Asks {
            Asks(copy: { [model] in model.copyChords(wholeGroup: $0) },
                 paste: { [model] in model.pasteChords() },
                 undo: { [model] in model.undo() },
                 redo: { [model] in model.redo() },
                 canUndo: { [model] in model.canUndo },
                 canRedo: { [model] in model.canRedo },
                 zoom: { [model] in model.zoomSheet($0) },
                 situation: { [model] in model.sheetIs($0, paste: $1) },
                 held: { [model] in model.holds($0) })
        }
    }

    /// Whether the person is at the sheet just now: the pointer is over it, or the
    /// keys have moved the spot since it left. The line of hints says what goes with
    /// the spot only then (SheetHints).
    private(set) var attending = false
    private var flagsWatch: Any?

    /// Says what is going on to whoever keeps the line of hints. (There is one sheet
    /// to a model. Two would each say what is going on in itself, the pointer over
    /// one and not the other, and the model would be told one thing and the other
    /// for ever: the tests, which draw many windows, close each when it is done with.)
    private func tell() {
        guard let sheet else {
            asks.situation(.nothing, SheetHints.Paste())
            return
        }
        asks.situation(SheetHints.situation(of: sheet, attending: attending), SheetHints.paste(in: sheet))
    }

    /// The highlighted line: the row whose words are drawn in a colour of their own,
    /// being the line the person is at (SheetHints.line has which that is, and why
    /// it is shown).
    var highlighted: Int? {
        sheet.flatMap { SheetHints.line(of: $0, attending: attending) }
    }

    /// Option or Command went down or came up, anywhere in the app.
    func modifiers(_ flags: NSEvent.ModifierFlags) {
        asks.held(flags.contains(.option) || flags.contains(.command))
    }

    // The keys that are held are watched for the whole app while the sheet is in a
    // window, and not only while the sheet has the keyboard: the box a chord is typed
    // in has it as often.
    override func viewDidMoveToWindow() {
        super.viewDidMoveToWindow()
        if let flagsWatch {
            NSEvent.removeMonitor(flagsWatch)
            self.flagsWatch = nil
        }
        guard window != nil else {
            asks.held(false)
            return
        }
        flagsWatch = NSEvent.addLocalMonitorForEvents(matching: .flagsChanged) { [weak self] event in
            self?.modifiers(event.modifierFlags)
            return event
        }
    }

    private(set) var sheet: ChordSheet?
    var asks = Asks()
    /// The colour of a chord's name: the usual one, or the one chosen in the settings.
    var chordColour = SheetLook.chordName {
        didSet {
            guard chordColour != oldValue else { return }
            entry.ink = chordColour
            needsDisplay = true
        }
    }
    /// The colour of the words of the highlighted line: the usual one, the sheet's
    /// own orange, or the one chosen in the settings.
    var lineColour = SheetLook.accent {
        didSet {
            guard lineColour != oldValue else { return }
            needsDisplay = true
        }
    }
    private(set) var metrics = SheetMetrics(scale: 1)
    let entry = SheetEntry()

    /// One slide as it is laid out: its frame in its group's colour, the card inside
    /// that where its lines are, what its foot says, and where it is in the grid.
    struct Cell {
        var slide: Int
        /// The sheet's rows that are this slide's lines.
        var rows: Range<Int>
        var frame: NSRect
        var card: NSRect
        /// Where the first of its lines starts, down the sheet.
        var linesTop: Double
        var colour: NSColor
        var label: String
        var gridRow: Int
        var column: Int
    }

    /// One line as it is laid out: which slide's card it is in, where its words start,
    /// where its row of chords is, and where its chords stand along it.
    struct Laid {
        var cell = -1
        var left = 0.0
        var chordTop = 0.0
        var placed = ChordLayout.Placed()
    }

    private(set) var cells: [Cell] = []
    private(set) var laid: [Laid] = []
    /// How many slides there are to a row of the grid.
    private(set) var columns = 1
    private var laidRows: [SheetRow] = []
    /// How wide the view was seen to be when the slides were last laid out.
    private var laidWidth = -1.0
    private var layingOut = false
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
    /// How far two fingers have spread or closed since the zoom last took a step.
    private var pinched = 0.0
    /// A number that changes whenever anything is copied, anywhere on this Mac: the
    /// system's count of changes to what is on its pasteboard. And what it was when a
    /// line's chords were last copied here, which are not put on that pasteboard.
    /// While the two are the same, a line's chords are the last thing that was copied.
    var whatWasCopiedLast: () -> Int = { NSPasteboard.general.changeCount }
    private var copiedWhen: Int?
    /// While one of the key's chords is carried over the sheet and held near its top
    /// or bottom edge: when the view was last asked about it there, and how far the
    /// sheet is still owed (it goes by whole points; the rest waits). And whether the
    /// chord has been clear of the top of what is seen since it came over.
    private var carriedAskedAt: TimeInterval?
    private var carriedOwed = 0.0
    private var carriedClearOfTheTop = false
    /// What the time is, in seconds, and whether Shift is held: asked of the system,
    /// which is not told either while a thing is dragged. (The tests answer for it.)
    var now: () -> TimeInterval = { ProcessInfo.processInfo.systemUptime }
    var shiftIsHeld: () -> Bool = { NSEvent.modifierFlags.contains(.shift) }

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
        // One of the key's chords, carried from the strip, may be let go here; and
        // nothing else that is dragged may.
        registerForDraggedTypes([.chordOfTheKey])
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
    /// With a `scale`, at that zoom.
    func show(_ shown: ChordSheet?, scale: Double? = nil) {
        if let scale, scale != metrics.scale {
            setScale(scale)
        }
        if shown !== sheet {
            sheet = shown
            shown?.reveal = { [weak self] in self?.revealSpot() }
            laidRows = []
            laid = []
            cells = []
            typingShown = nil
            pressed = nil
            scroll(.zero)
        }
        sync()
    }

    // ---- Laying out

    /// How wide the sheet is seen to be: what the slides are fitted across. (Where the
    /// system draws its scroll bars beside what they scroll, and not over it, room is
    /// kept for one always: a bar that came and went with the layout would change the
    /// width the layout was made for.)
    private var seenWidth: Double {
        guard let scroll = enclosingScrollView else { return bounds.width }
        let bar = scroll.scrollerStyle == .legacy ? NSScroller.scrollerWidth(for: .regular, scrollerStyle: .legacy) : 0
        return scroll.bounds.width - bar
    }

    /// What one slide needs across, frame and all: the widest of its lines with their
    /// chords and the room inside the card, or what the words on its foot need if that
    /// is more. Nothing is added for what might be put there later: a line that comes
    /// to need more, for a chord at its end, is given it then.
    private func widthNeeded(byLines lines: Double, label: String) -> Double {
        let foot = SheetMetrics.width(of: SheetMetrics.line(label, font: SheetLook.labelFont)) + 16
        return max(max(lines, 40 * metrics.scale) + 2 * metrics.cardPad + 2 * SheetLook.frame, foot)
    }

    /// Lays the slides out as a grid: in the order of the song, as many to a row as fit
    /// across. Every card is one width, what the widest slide of the song needs, and
    /// the shape of a screen, 16 by 9 (though never taller for that than four lines
    /// need); a row with a slide of more lines than that holds is as tall as that
    /// slide needs. A slide's lines stand in the middle of its card, up and down.
    private func layOut() {
        laidWidth = seenWidth
        guard let sheet else {
            laid = []
            cells = []
            content = .zero
            fit()
            return
        }
        let rows = sheet.rows
        metrics.lines = rows.map { $0.kind == .line ? SheetMetrics.line($0.text.string, font: metrics.wordFont) : nil }
        var lines = Array(repeating: Laid(), count: rows.count)
        // Each slide: its first line's row, how many lines it has, what its foot says,
        // and how wide a cell of the grid it needs.
        var slides: [(first: Int, count: Int, label: String, needs: Double)] = []
        for (index, row) in rows.enumerated() where row.kind == .line && row.first {
            var widest = 0.0
            for line in index..<(index + row.count) {
                lines[line].placed = sheet.placed(line, metrics)
                // (A line of chords alone has a place past its last chord, for the
                // next: that place needs room to be pointed at.)
                widest = max(widest, lines[line].placed.width + (rows[line].alone ? 44 * metrics.scale : 0))
            }
            let box = sheet.blocks[row.block]
            // The slide's number, and on the first slide of each group the group's name.
            let label = String(row.slide + 1) + (box.startsItsGroup && !box.group.isEmpty ? "  " + box.group : "")
            slides.append((index, row.count, label, widthNeeded(byLines: widest, label: label) + 2 * SheetLook.around))
        }
        let pad = metrics.cardPad
        let around = SheetLook.around

        // Every slide is as wide as the widest of them needs, and as many go to a row
        // as fit across at that width. What is left of the sheet is at its right.
        let across = laidWidth - 2 * SheetLook.margin
        let width = slides.map(\.needs).max() ?? 0
        columns = max(1, min(slides.count, Int(across / max(width, 1))))
        let cardWidth = width - 2 * around - 2 * SheetLook.frame
        let shaped = min(cardWidth * SheetLook.shape, SheetLook.shapedUpTo * metrics.lineHeight + 2 * pad)

        var made: [Cell] = []
        var top = SheetLook.top
        var gridRow = 0
        var from = 0
        while from < slides.count {
            let these = slides[from..<min(from + columns, slides.count)]
            // The shape of a screen; or taller, if the row's lines need more than that.
            let needed = Double(these.map(\.count).max() ?? 1) * metrics.lineHeight + 2 * pad
            let cardHeight = max(needed, shaped)
            for (column, slide) in these.enumerated() {
                let frame = NSRect(x: SheetLook.margin + Double(column) * width + around, y: top + around,
                                   width: width - 2 * around, height: SheetLook.frame + cardHeight + SheetLook.label)
                let card = NSRect(x: frame.minX + SheetLook.frame, y: frame.minY + SheetLook.frame, width: cardWidth, height: cardHeight)
                let box = sheet.blocks[rows[slide.first].block]
                let colour = box.groupColour.map { NSColor(srgbRed: $0.red, green: $0.green, blue: $0.blue, alpha: 1) } ?? SheetLook.group
                // The slide's lines, in the middle of its card up and down.
                let linesTop = card.minY + (cardHeight - Double(slide.count) * metrics.lineHeight) / 2
                for line in 0..<slide.count {
                    lines[slide.first + line].cell = made.count
                    lines[slide.first + line].left = card.minX + pad
                    lines[slide.first + line].chordTop = linesTop + Double(line) * metrics.lineHeight + 2 * metrics.scale
                }
                made.append(Cell(slide: rows[slide.first].slide, rows: slide.first..<(slide.first + slide.count), frame: frame, card: card,
                                 linesTop: linesTop, colour: colour, label: slide.label, gridRow: gridRow, column: column))
            }
            top += 2 * around + SheetLook.frame + cardHeight + SheetLook.label
            gridRow += 1
            from += columns
        }
        let left = SheetLook.margin + Double(columns) * width
        cells = made
        laid = lines
        laidRows = rows
        content = NSSize(width: left + SheetLook.margin, height: top + SheetLook.margin + 40)
        fit()
    }

    /// Lays out again, for another width or another zoom, with the slide that was at
    /// the top of what is seen still there.
    private func layOutWhereItWas() {
        guard !layingOut else { return }
        layingOut = true
        defer { layingOut = false }
        let seen = visibleRect
        let atTop = cells.first { $0.frame.maxY > seen.minY + 1 }
        let within = atTop.map { $0.frame.minY - seen.minY } ?? 0
        layOut()
        if let atTop, let now = cells.first(where: { $0.rows.lowerBound == atTop.rows.lowerBound }) {
            scroll(NSPoint(x: 0, y: max(0, min(now.frame.minY - within, bounds.height - seen.height))))
        }
        placeEntry()
        needsDisplay = true
    }

    /// The view is as big as the song, and never smaller than what it is seen through.
    /// If it is seen through something of another width than the slides were laid out
    /// for, they are laid out again: fewer or more to a row.
    func fit() {
        if !layingOut, sheet != nil, laidWidth >= 0, abs(seenWidth - laidWidth) > 0.5 {
            layOutWhereItWas()
            return
        }
        let seen = enclosingScrollView?.contentView.bounds.size ?? .zero
        let wanted = NSSize(width: max(content.width, seen.width), height: max(content.height, seen.height))
        if frame.size != wanted {
            setFrameSize(wanted)
        }
        placeEntry()
    }

    /// Draws everything so many times its usual size.
    func setScale(_ scale: Double) {
        guard scale != metrics.scale else { return }
        metrics = SheetMetrics(scale: scale)
        if sheet != nil {
            layOutWhereItWas()
            if let sheet, sheet.typing {
                entry.show(offered: sheet.offered, picked: sheet.entry.pick, metrics: metrics)
                placeEntry()
            }
        }
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
        tell()
    }

    /// Puts the bubble a chord is typed in at the place the chord is for. It grows
    /// upwards from where the chord will stand, so that the word being given the chord,
    /// and the rest of its line, stay in sight; and it is kept within what is seen.
    private func placeEntry() {
        guard let sheet, sheet.typing, laid.indices.contains(sheet.typingAt.row) else { return }
        let item = laid[sheet.typingAt.row]
        var x = item.left + sheet.xOf(sheet.typingAt.row, sheet.typingAt.at, metrics)
        var y = item.chordTop + metrics.bubbleHeight + 3 - entry.frame.height
        let seen = visibleRect
        // (Before the view has been given its size there is nothing to keep it within.)
        if seen.width > entry.frame.width {
            x = max(seen.minX + 4, min(x, seen.maxX - entry.frame.width - 12))
            y = max(seen.minY + 4, y)
        }
        entry.setFrameOrigin(NSPoint(x: x, y: max(4, y)))
    }

    /// Brings the spot into view, when the keys have moved it: its slide's whole frame
    /// if that can be seen whole, and the place itself whatever else.
    private func revealSpot() {
        guard let sheet, laid.indices.contains(sheet.spot.row) else { return }
        if sheet.rows != laidRows {
            layOut()
        }
        let item = laid[sheet.spot.row]
        guard cells.indices.contains(item.cell) else { return }
        let frame = cells[item.cell].frame.insetBy(dx: -SheetLook.around, dy: -SheetLook.around)
        let seen = visibleRect
        if frame.height <= seen.height, frame.width <= seen.width {
            scrollToVisible(frame)
        }
        let x = item.left + sheet.xOf(sheet.spot.row, sheet.spot.at, metrics)
        scrollToVisible(NSRect(x: x - 40, y: item.chordTop - 10, width: min(160, max(80, seen.width - 20)), height: metrics.lineHeight + 20))
    }

    // ---- What is where

    /// The line of a card that a point is on. Every part of a card is one of its lines:
    /// over the first of them, the first, and under the last, the last.
    private func line(at point: NSPoint) -> Int? {
        guard let cell = cells.first(where: { $0.card.contains(point) }) else { return nil }
        let down = Int(((point.y - cell.linesTop) / metrics.lineHeight).rounded(.down))
        return cell.rows.lowerBound + min(max(down, 0), cell.rows.count - 1)
    }

    /// The place of a line under a point: a chord's, if the point is on its bubble;
    /// otherwise the character there, or with Shift the word. Nothing off the cards,
    /// which is bare sheet (a frame and its foot are too), or on a line that can take
    /// no chord.
    func place(at point: NSPoint, shift: Bool) -> (row: Int, at: Int, onChord: Bool)? {
        guard let sheet, let index = line(at: point), sheet.usable(index) else { return nil }
        let item = laid[index]
        let along = point.x - item.left
        if point.y >= item.chordTop, point.y < item.chordTop + metrics.bubbleHeight + metrics.tail {
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
        if onItsChord {
            guard let n = row.chords.firstIndex(where: { $0.at == spot.at }) else { return nil }
            return NSPoint(x: item.left + item.placed.xs[n] + item.placed.widths[n] / 2, y: item.chordTop + metrics.bubbleHeight / 2)
        }
        let wordsMiddle = item.chordTop + metrics.bubbleHeight + metrics.tail + metrics.wordHeight / 2
        if row.alone {
            return NSPoint(x: item.left + sheet.xOf(spot.row, spot.at, metrics) + 2, y: wordsMiddle)
        }
        let from = metrics.advance(row: spot.row, units: spot.at)
        let to = metrics.advance(row: spot.row, units: min(spot.at + 1, row.text.length))
        return NSPoint(x: item.left + (from + to) / 2, y: wordsMiddle)
    }

    /// The same for a chord that is being carried: the line under the pointer, and
    /// never another chord's bubble for itself.
    private func placeCarriedTo(_ point: NSPoint, shift: Bool) -> (row: Int, at: Int)? {
        guard let sheet, let index = line(at: point), sheet.usable(index) else { return nil }
        return (index, sheet.placeAt(index, along: point.x - laid[index].left, exact: !shift, metrics))
    }

    /// The line over or under a line ON THE SCREEN, that can take the spot: the next of
    /// its own slide; or from a slide's last line the first line of the slide under it
    /// in the grid, and from its first the last line of the slide over it. The slide
    /// under or over is the one in the same column of the next row of slides, or that
    /// row's last where it is a short one.
    func lineBeside(_ row: Int, _ delta: Int) -> Int? {
        guard let sheet, laid.indices.contains(row), cells.indices.contains(laid[row].cell) else { return nil }
        let here = cells[laid[row].cell]
        var next = row + delta
        while here.rows.contains(next) {
            if sheet.usable(next) { return next }
            next += delta
        }
        var gridRow = here.gridRow + delta
        while true {
            let inRow = cells.filter { $0.gridRow == gridRow }
            guard let last = inRow.last else { return nil }
            let there = inRow.first { $0.column == here.column } ?? last
            let lines = delta > 0 ? Array(there.rows) : Array(there.rows.reversed())
            if let found = lines.first(where: sheet.usable) { return found }
            gridRow += delta
        }
    }

    /// Down and Up: the spot goes to the line under or over it on the screen, to the
    /// stop of it that is nearest under or over where it was.
    private func stepLine(_ delta: Int) {
        guard let sheet else { return }
        guard sheet.usable(sheet.spot.row) else {
            // With no spot yet, the first arrow key puts it on the first word of the song.
            sheet.step(1, exact: false)
            return
        }
        guard let next = lineBeside(sheet.spot.row, delta) else { return }
        let x = laid[sheet.spot.row].left + sheet.xOf(sheet.spot.row, sheet.spot.at, metrics)
        var best = 0
        var distance = Double.infinity
        for stop in sheet.stops(next) {
            let off = abs(laid[next].left + sheet.xOf(next, stop, metrics) - x)
            if off < distance {
                distance = off
                best = stop
            }
        }
        sheet.setSpot(next, best)
        sheet.revealSpot()
    }

    // ---- The pointer

    /// The pointer has moved, with no button down.
    func pointerMoved(to point: NSPoint, shift: Bool) {
        guard let sheet else { return }
        takeTheKeysIfNobodyIsTyping()
        if let place = place(at: point, shift: shift) {
            sheet.pointerOver(place.row, place.at)
            (place.onChord ? NSCursor.pointingHand : NSCursor.arrow).set()
            attending = true
        } else {
            NSCursor.arrow.set()
            // (Over the bare sheet, between the slides, there is nothing to say of
            // the spot: it is where it was left.)
            attending = false
        }
        needsDisplay = true
        tell()
    }

    /// The pointer has left the sheet.
    func pointerLeft() {
        attending = false
        // (No line is the highlighted one now.)
        needsDisplay = true
        tell()
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
        tell()
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
        tell()
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
        let area = NSTrackingArea(rect: .zero, options: [.mouseMoved, .mouseEnteredAndExited, .activeInKeyWindow, .inVisibleRect], owner: self)
        addTrackingArea(area)
        tracking = area
    }

    override func mouseExited(with event: NSEvent) {
        pointerLeft()
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

    // ---- A chord carried in from the strip (spec/03, rules 46a to 46g)
    //
    // The Mac's own dragging carries it. A button of the strip begins it
    // (KeyChordGrip.swift), the system draws the button's picture under the pointer,
    // and this view, which has said it takes such a chord and nothing else, is told
    // as the chord comes over it, moves, leaves and is let go. What each of those does
    // to the song is ChordSheet's.

    /// One of the key's chords, carried from the strip, is over a point of the view;
    /// or, with no point, is over the view no longer. Answers whether letting go would
    /// put it on: whether it is over a place. Only then is it shown, in a lit bubble
    /// at that place.
    @discardableResult
    func carriedOfKey(_ name: String, to point: NSPoint?, shift: Bool) -> Bool {
        guard let sheet else { return false }
        if let point, let place = placeCarriedTo(point, shift: shift) {
            sheet.carry(name, over: place.row, place.at)
        } else {
            sheet.carryOff()
        }
        needsDisplay = true
        return sheet.dragged.map { $0.from.row < 0 } ?? false
    }

    /// It is let go at a point: over a place it is put there, and anywhere else
    /// nothing is done. Answers whether the song took it.
    @discardableResult
    func droppedOfKey(_ name: String, at point: NSPoint, shift: Bool) -> Bool {
        guard let sheet else { return false }
        let put = carriedOfKey(name, to: point, shift: shift) && sheet.dropOfKey()
        // (The line may have a chord more, or a blank slide its first: laid out again.)
        sync()
        return put
    }

    /// How near the top or the bottom of what is seen a carried chord is held for the
    /// sheet to scroll, and how fast it then goes at the very edge, in a second.
    static let carriedNear = 36.0
    static let carriedFastest = 520.0

    /// A chord carried in from the strip is held near the top or the bottom of what is
    /// seen of the sheet: the sheet goes that way, faster the nearer the edge, so that
    /// a slide out of sight can be reached (rule 46f). It goes by the time that has
    /// passed and not by how often it is asked: the system tells the view of the chord
    /// at every move of the pointer, and now and again while it lies still.
    ///
    /// Not upwards until the chord has once been clear of the top: it comes from the
    /// strip, over the top of the sheet, and is not to scroll the sheet on its way in.
    func scrollIfCarriedNearAnEdge(_ point: NSPoint) {
        let seen = visibleRect
        let near = Self.carriedNear
        if point.y >= seen.minY + near {
            carriedClearOfTheTop = true
        }
        var depth = 0.0
        if point.y > seen.maxY - near {
            depth = min(1, (point.y - (seen.maxY - near)) / near)
        } else if point.y < seen.minY + near, carriedClearOfTheTop {
            depth = -min(1, (seen.minY + near - point.y) / near)
        }
        guard depth != 0 else {
            carriedAskedAt = nil
            carriedOwed = 0
            return
        }
        // The first time it is found near an edge the clock is only started. After
        // that the sheet is owed what the time since the view was last asked comes
        // to (a long wait counting as a fifth of a second, so that it never jumps),
        // and goes the nearest whole number of points to what it is owed. The rest
        // is owed still (or was lent): so being asked very often, each time too soon
        // for a point, is not being asked in vain. (What a view is seen through scrolls by whole points
        // anyway, and would round each small step up or away.)
        let time = now()
        let passed = carriedAskedAt.map { min(0.2, max(0, time - $0)) } ?? 0
        carriedAskedAt = time
        carriedOwed += depth * Self.carriedFastest * passed
        let whole = carriedOwed.rounded()
        if whole != 0 {
            carriedOwed -= whole
            // (What it is seen through stops it at the sheet's top and its bottom.)
            scroll(NSPoint(x: seen.minX, y: seen.minY + whole))
        }
    }

    /// The chord a pasteboard carries from the strip, if it carries one.
    static func chordOfKey(on pasteboard: NSPasteboard) -> String? {
        guard pasteboard.availableType(from: [.chordOfTheKey]) != nil, let name = pasteboard.string(forType: .chordOfTheKey), !name.isEmpty else { return nil }
        return name
    }

    /// What is dragged is over the view: where, and whether it may be let go there. (The
    /// system's word for putting a thing somewhere is "copy", and its sign for it a
    /// green plus beside the pointer.)
    private func carried(by dragging: any NSDraggingInfo) -> NSDragOperation {
        guard let name = Self.chordOfKey(on: dragging.draggingPasteboard) else { return [] }
        scrollIfCarriedNearAnEdge(convert(dragging.draggingLocation, from: nil))
        // (Where it is, is asked again after any scrolling: another place is under
        // the pointer then.)
        return carriedOfKey(name, to: convert(dragging.draggingLocation, from: nil), shift: shiftIsHeld()) ? .copy : []
    }

    override func draggingEntered(_ sender: any NSDraggingInfo) -> NSDragOperation {
        // (Picking the chord up kept a chord that was being typed: its bubble goes.)
        sync()
        carriedAskedAt = nil
        carriedOwed = 0
        carriedClearOfTheTop = false
        return carried(by: sender)
    }

    override func draggingUpdated(_ sender: any NSDraggingInfo) -> NSDragOperation {
        carried(by: sender)
    }

    override func draggingExited(_ sender: (any NSDraggingInfo)?) {
        carriedOfKey("", to: nil, shift: false)
    }

    override func draggingEnded(_ sender: any NSDraggingInfo) {
        carriedOfKey("", to: nil, shift: false)
    }

    override func prepareForDragOperation(_ sender: any NSDraggingInfo) -> Bool {
        Self.chordOfKey(on: sender.draggingPasteboard) != nil
    }

    override func performDragOperation(_ sender: any NSDraggingInfo) -> Bool {
        guard let name = Self.chordOfKey(on: sender.draggingPasteboard) else { return false }
        return droppedOfKey(name, at: convert(sender.draggingLocation, from: nil), shift: shiftIsHeld())
    }

    // ---- The keys

    /// A key pressed with no chord being typed.
    func key(_ key: SheetKey, shift: Bool = false) {
        guard let sheet else { return }
        switch key {
        case .left: sheet.step(-1, exact: shift)
        case .right: sheet.step(1, exact: shift)
        case .up: stepLine(-1)
        case .down: stepLine(1)
        case .delete: sheet.takeAtSpot()
        case .open: sheet.startTyping()
        case .number(let number): sheet.putOfKey(number)
        case .letter(let letter): sheet.startTyping(seed: letter.uppercased())
        case .copy(let wholeGroup):
            asks.copy(wholeGroup)
            copiedWhen = whatWasCopiedLast()
        case .paste: asks.paste()
        case .undo: asks.undo()
        case .redo: asks.redo()
        case .zoom(let direction):
            if let scale = asks.zoom(direction) {
                setScale(scale)
            }
        }
        // (The keys that move the spot or act on it are the person at the sheet, as
        // the pointer over it is.)
        switch key {
        case .zoom, .undo, .redo: break
        default: attending = true
        }
        sync()
    }

    /// Which key of the sheet's an event is, if any.
    static func key(of event: NSEvent) -> (key: SheetKey, shift: Bool)? {
        let flags = event.modifierFlags.intersection([.command, .control, .option, .shift])
        let shift = flags.contains(.shift)
        let typed = event.charactersIgnoringModifiers?.lowercased() ?? ""
        if flags.contains(.command) {
            // (Option held shows the hint for copying and pasting, and may well be
            // down still when the keys it names are pressed: they do the same with
            // it. For the rest, Option makes it another key, which is not the
            // sheet's.)
            if flags.contains(.option) {
                guard flags.isSubset(of: [.command, .shift, .option]) else { return nil }
                switch typed {
                case "c": return (.copy(wholeGroup: shift), shift)
                case "v" where !shift: return (.paste, false)
                default: return nil
                }
            }
            guard flags.isSubset(of: [.command, .shift]) else { return nil }
            switch typed {
            case "c": return (.copy(wholeGroup: shift), shift)
            case "v" where !shift: return (.paste, false)
            case "z": return (shift ? .redo : .undo, shift)
            // (Plus is Shift with the equals key on most keyboards: both are taken.)
            case "=", "+": return (.zoom(1), false)
            case "-": return (.zoom(-1), false)
            case "0" where !shift: return (.zoom(0), false)
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

    // Two fingers spread or closed on a trackpad: the words a step larger or smaller
    // for each fair stretch of it.
    override func magnify(with event: NSEvent) {
        if event.phase == .began {
            pinched = 0
        }
        pinched += event.magnification
        if abs(pinched) > 0.2 {
            key(.zoom(pinched > 0 ? 1 : -1))
            pinched = 0
        }
    }

    // Command with C, V, Z and the keys that zoom is asked of the views of a window
    // before its menus are. The sheet answers them itself while it has the keyboard, so that they work
    // whatever the menus make of them.
    override func performKeyEquivalent(with event: NSEvent) -> Bool {
        guard event.type == .keyDown, event.modifierFlags.contains(.command), let pressed = Self.key(of: event) else {
            return super.performKeyEquivalent(with: event)
        }
        if window?.firstResponder === self {
            key(pressed.key, shift: pressed.shift)
            return true
        }
        if answersInTheBox(pressed.key) {
            return true
        }
        return super.performKeyEquivalent(with: event)
    }

    /// A Command key pressed while the box a chord is typed in has the keyboard. Most
    /// are the box's own: it is a box of text, and Command+V pastes text into it. But a
    /// click on a line opens the box, and the one who clicks a line and presses
    /// Command+V means the chords just copied to go on that line. So, while NOTHING
    /// HAS BEEN TYPED in the box:
    ///
    ///   - Command+V, if the last thing copied anywhere was a line's chords here,
    ///     closes the box, changing nothing, and pastes them from the box's line on;
    ///   - Command+C and Shift+Command+C, if the box is empty and so has nothing of
    ///     its own to copy, copy the chords of the box's line, or of its group.
    ///
    /// The keys that zoom are the sheet's whoever has the keyboard. Answers whether
    /// the key was answered here.
    private func answersInTheBox(_ pressed: SheetKey) -> Bool {
        guard let sheet, sheet.typing, let editor = entry.field.currentEditor(), window?.firstResponder === editor else { return false }
        if case .zoom = pressed {
            key(pressed)
            return true
        }
        guard sheet.entry.text == sheet.entry.begun else { return false }
        let line = sheet.typingAt
        switch pressed {
        case .paste where sheet.copied != nil && copiedWhen == whatWasCopiedLast():
            sheet.finishTyping(nil, onward: false)
            sheet.setSpot(line.row, line.at)
            sync()
            key(.paste)
            return true
        case .copy where sheet.entry.text.isEmpty:
            sheet.setSpot(line.row, line.at)
            key(pressed)
            return true
        default:
            return false
        }
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

    private func rounded(_ rect: NSRect, radius: Double, fill: NSColor?, edge: NSColor?, alpha: Double = 1, width: Double = 1) {
        // (An edge is drawn down the middle of its own width.)
        let path = NSBezierPath(roundedRect: rect.insetBy(dx: width / 2, dy: width / 2), xRadius: radius, yRadius: radius)
        if let fill {
            fill.withAlphaComponent(fill.alphaComponent * alpha).setFill()
            path.fill()
        }
        if let edge {
            edge.withAlphaComponent(edge.alphaComponent * alpha).setStroke()
            path.lineWidth = width
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
        rounded(NSRect(x: x, y: top, width: width, height: metrics.bubbleHeight), radius: 6 * metrics.scale, fill: lit ? SheetLook.lit : SheetLook.bubble, edge: edge, alpha: alpha)
        let text = metrics.name(name)
        Self.draw(text.line, at: NSPoint(x: x + (width - text.width) / 2, y: top + (metrics.bubbleHeight - metrics.chordHeight) / 2),
                  font: metrics.chordFont, colour: chordColour.withAlphaComponent(alpha), in: context)
    }

    override func draw(_ dirtyRect: NSRect) {
        SheetLook.ground.setFill()
        dirtyRect.fill()
        guard let sheet, let context = NSGraphicsContext.current?.cgContext, laid.count == sheet.rows.count else { return }
        let rows = sheet.rows
        let tailWidth = max(1.5, 2 * metrics.scale)
        let highlighted = highlighted

        for cell in cells where cell.frame.insetBy(dx: -SheetLook.around, dy: -SheetLook.around).intersects(dirtyRect) {
            // The slide that is picked has an orange ring, outside its frame and set
            // off from it by a little of the sheet, so that it reads against a frame
            // of any colour.
            if cell.slide == sheet.picked {
                rounded(cell.frame.insetBy(dx: -4.5, dy: -4.5), radius: 7, fill: nil, edge: SheetLook.accent, width: 2)
            }
            // The frame is the colour of the slide's group; the card is laid on it,
            // and under the card is its foot, with the slide's number and, on the
            // first slide of each group, the group's name.
            rounded(cell.frame, radius: 4, fill: cell.colour, edge: nil)
            rounded(cell.card, radius: 2, fill: SheetLook.card, edge: nil)
            var label = SheetMetrics.line(cell.label, font: SheetLook.labelFont)
            let room = cell.frame.width - 16
            if SheetMetrics.width(of: label) > room, room > 20 {
                let dots = SheetMetrics.line("\u{2026}", font: SheetLook.labelFont)
                label = CTLineCreateTruncatedLine(label, room, .end, dots) ?? label
            }
            Self.draw(label, at: NSPoint(x: cell.frame.minX + 8, y: cell.card.maxY + (SheetLook.label - SheetLook.height(of: SheetLook.labelFont)) / 2),
                      font: SheetLook.labelFont, colour: SheetLook.isLight(cell.colour) ? .black : .white, in: context)

            for index in cell.rows {
                let row = rows[index]
                let item = laid[index]
                let wordsTop = item.chordTop + metrics.bubbleHeight + metrics.tail
                // The highlighted line, the one the person is at, has its words in a
                // colour of its own.
                let lit = index == highlighted
                if row.alone {
                    // A line of chords alone has nothing to read: a rule stands where
                    // its words would be. (Highlighted, it is the rule that takes the
                    // colour, and is a little thicker, or there would be nothing to
                    // show which line is meant.)
                    (lit ? lineColour : SheetLook.rule).setFill()
                    let thick = lit ? max(2, (2 * metrics.scale).rounded()) : 1
                    NSRect(x: item.left, y: wordsTop + metrics.wordHeight / 2 - (thick - 1) / 2, width: max(1, cell.card.maxX - metrics.cardPad - item.left), height: thick).fill()
                } else if let line = metrics.lines[index] {
                    Self.draw(line, at: NSPoint(x: item.left, y: wordsTop), font: metrics.wordFont, colour: lit ? lineColour : SheetLook.words, in: context)
                }
                // The chords, each a bubble with its tail on its character
                for (n, chord) in row.chords.enumerated() {
                    let x = item.placed.xs[n]
                    let width = item.placed.widths[n]
                    let atSpot = sheet.spot.row == index && sheet.spot.at == chord.at
                    let lifted = sheet.dragged.map { $0.from == Spot(row: index, at: chord.at) } ?? false
                    let alpha = lifted ? 0.35 : 1
                    let edge = atSpot ? SheetLook.accent : SheetLook.bubbleEdge
                    // Where its character is, which is where the tail points even when
                    // the bubble has had to move right of it
                    let character = row.alone ? x + width / 2 : metrics.advance(row: index, units: min(chord.at, row.text.length))
                    edge.withAlphaComponent(alpha).setFill()
                    NSRect(x: item.left + x + max(3 * metrics.scale, min(width - 5 * metrics.scale, character - x + 1)), y: item.chordTop + metrics.bubbleHeight - 1,
                           width: tailWidth, height: metrics.tail + 3 * metrics.scale).fill()
                    bubble(name: chord.name, x: item.left + x, top: item.chordTop, width: width, lit: atSpot, edge: edge, alpha: alpha, in: context)
                }
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
        let x = item.left + sheet.xOf(spot.row, spot.at, metrics)
        let carried = sheet.dragged?.name
        let width = max(26 * metrics.scale, carried.map { metrics.bubble($0) } ?? 0)
        SheetLook.accent.withAlphaComponent(0.75).setFill()
        NSRect(x: x + 3 * metrics.scale, y: item.chordTop + metrics.bubbleHeight - 1, width: tailWidth, height: metrics.tail + metrics.wordHeight + 3 * metrics.scale).fill()
        if let carried {
            bubble(name: carried, x: x, top: item.chordTop, width: width, lit: true, edge: SheetLook.accent, alpha: 1, in: context)
        } else {
            rounded(NSRect(x: x, y: item.chordTop, width: width, height: metrics.bubbleHeight), radius: 6 * metrics.scale, fill: nil, edge: SheetLook.accent, alpha: 0.75)
            let plus = metrics.name("+")
            Self.draw(plus.line, at: NSPoint(x: x + (width - plus.width) / 2, y: item.chordTop + (metrics.bubbleHeight - metrics.chordHeight) / 2),
                      font: metrics.chordFont, colour: SheetLook.accent.withAlphaComponent(0.75), in: context)
        }
    }
}

/// The bubble a chord is typed in: a one-line box, and over it what the chord might be
/// going to be, as a row of buttons that wraps (spec/03, rule 29).
@MainActor
final class SheetEntry: NSView {
    let field = NSTextField()
    /// The colour of what is typed: the colour of a chord's name on the sheet.
    var ink = SheetLook.chordName {
        didSet {
            field.textColor = ink
            (field.currentEditor() as? NSTextView)?.insertionPointColor = ink
        }
    }
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
        field.textColor = ink
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
        editor?.insertionPointColor = ink
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
    /// How many times its usual size the sheet is drawn.
    var scale: Double
    /// The colour of a chord's name, if another than the usual was chosen: "#rrggbb".
    var chordColour: String?
    /// And the colour of the highlighted line's words, the same way.
    var lineColour: String?

    func makeNSView(context: Context) -> SheetScroll {
        let scroll = SheetScroll(frame: NSRect(x: 0, y: 0, width: 600, height: 400))
        scroll.canvas.asks = .of(model)
        scroll.canvas.chordColour = ChordColour.colour(chordColour) ?? SheetLook.chordName
        scroll.canvas.lineColour = ChordColour.colour(lineColour) ?? SheetLook.accent
        scroll.canvas.show(model.sheet, scale: scale)
        return scroll
    }

    func updateNSView(_ scroll: SheetScroll, context: Context) {
        scroll.canvas.asks = .of(model)
        scroll.canvas.chordColour = ChordColour.colour(chordColour) ?? SheetLook.chordName
        scroll.canvas.lineColour = ChordColour.colour(lineColour) ?? SheetLook.accent
        scroll.canvas.show(model.sheet, scale: scale)
    }
}

/// The strip across the top of the sheet: the key's own chords on the keys 1 to 7,
/// which can be clicked there too, or dragged from there onto a word (spec/03, rules
/// 14 and 46a; KeyChordGrip.swift is what takes the mouse over each). The key itself is set in the
/// song's header, over this, and nowhere else: the reference has its picker here, and
/// these apps had it in both places until the maintainer said one was enough.
struct SheetStrip: View {
    var model: LibraryModel
    var sheet: ChordSheet
    /// The key the song states, or "" for none.
    var key: String

    var body: some View {
        // Nothing in the strip is ever squeezed or wrapped. Where the pane is too narrow
        // for the chords and what is said of the key, that is left out; and where it is
        // too narrow for the chords, they scroll sideways.
        ViewThatFits(in: .horizontal) {
            HStack(spacing: 8) {
                chords
                Spacer(minLength: 12)
                if key.isEmpty {
                    Text("The song names no key: pick the one its chords are in")
                        .font(.system(size: 12))
                        .foregroundStyle(Color(nsColor: SheetLook.caption))
                        .lineLimit(1)
                        .fixedSize()
                }
                zoom
            }
            HStack(spacing: 8) {
                chords
                Spacer(minLength: 12)
                zoom
            }
            HStack(spacing: 8) {
                ScrollView(.horizontal, showsIndicators: false) {
                    chords
                }
                zoom
            }
        }
        .padding(.horizontal, 14)
        .frame(height: 44)
        .frame(maxWidth: .infinity)
        .background(Color(nsColor: SheetLook.strip))
        .environment(\.colorScheme, .dark)
    }

    /// At the strip's right: the size of the words, and so how many slides there are to
    /// a row; and, a little apart from those two, the guide (spec/13-guide.md, rule 3).
    private var zoom: some View {
        HStack(spacing: 6) {
            round("minus", named: "Smaller words", can: model.canZoomSheetOut,
                  says: "Smaller words, and more slides in view. Command with the minus key does the same.") { model.zoomSheet(-1) }
            round("plus", named: "Larger words", can: model.canZoomSheetIn,
                  says: "Larger words. Command with the plus key does the same; Command with 0 is the usual size.") { model.zoomSheet(1) }
            round("questionmark", named: "Guide", can: true, says: GuideWindow.tip) { model.showGuide() }
                .padding(.leading, 6)
        }
        .fixedSize()
    }

    /// One of the small round buttons at the strip's right.
    private func round(_ picture: String, named name: String, can: Bool, says: String, does: @escaping () -> Void) -> some View {
        Button(action: does) {
            Image(systemName: picture)
                .font(.system(size: 11, weight: .bold))
                .foregroundStyle(Color(nsColor: SheetLook.words).opacity(can ? 1 : 0.35))
                .frame(width: 24, height: 24)
                .background(Circle().fill(Color(nsColor: SheetLook.chip)))
        }
        .buttonStyle(.plain)
        .disabled(!can)
        .help(says)
        .accessibilityLabel(name)
    }

    private var chords: some View {
        HStack(spacing: 8) {
            ForEach(Array(sheet.inKey.enumerated()), id: \.offset) { index, name in
                let says = Self.says(of: name, number: index + 1)
                // The button is there for the keyboard and for what reads the screen
                // aloud. The mouse is taken by what lies over it, which makes a press
                // let go the button's click and a press pulled away the chord picked
                // up to be carried to a word.
                Button {
                    model.putOfKey(index + 1)
                } label: {
                    Self.chip(index + 1, name)
                }
                .buttonStyle(.plain)
                .help(says)
                .overlay {
                    KeyChordHandle(name: name, says: says, picture: { Self.picture(of: index + 1, name) },
                                   clicked: { model.putOfKey(index + 1) }, lifted: { model.liftOfKey() }, ended: { model.carriedOfKeyEnded() })
                }
            }
        }
        .fixedSize()
    }

    /// What one of the key's chords says of itself when it is pointed at (spec/03,
    /// rule 46h).
    static func says(of name: String, number: Int) -> String {
        "Puts \(name) on the spot. The key \(number) does the same. Or drag it onto a word."
    }

    /// One of the key's chords as the strip shows it: its number in grey, and its name.
    static func chip(_ number: Int, _ name: String) -> some View {
        HStack(spacing: 5) {
            Text(String(number))
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

    /// The same as a picture, to go with the pointer while the chord is carried.
    @MainActor
    static func picture(of number: Int, _ name: String) -> NSImage? {
        let renderer = ImageRenderer(content: chip(number, name).environment(\.colorScheme, .dark))
        renderer.scale = NSScreen.main?.backingScaleFactor ?? 2
        return renderer.nsImage
    }
}
