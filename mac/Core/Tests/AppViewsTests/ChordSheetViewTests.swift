// The chord sheet in the window: the real view, pointed at, clicked and typed into, in a
// window that is never shown, on a made-up Mac; and then the song's FILE read again.
//
// What these can tell: that a pointer over a place puts the spot there, that each key
// does to the file what the page says, that the box a chord is typed in takes the
// keyboard and gives it back, and that the two editors keep to one song. What they
// cannot: whether any of it is quick, or feels right under a hand. That is the
// maintainer's to say (spec/03, "What to hold your build to").

import AppKit
@testable import AppViews
import ChordRules
import CoreText
import Song
import Testing

@MainActor
@Suite("The chord sheet in the window")
struct ChordSheetViewTests {
    private func open(_ name: String, on mac: MadeUpMac) async -> LibraryModel {
        let model = mac.model()
        model.pauseBeforeSaving = .milliseconds(1)
        await model.start()
        model.choose(library: mac.library("Songs"))
        await model.marksAreWorkedOut()
        model.open(song: mac.song(name))
        return model
    }

    /// The real view, wired to a model as the window wires it, in a window that is
    /// never shown.
    private func view(for model: LibraryModel, width: Double = 900) -> (SheetCanvas, NSWindow) {
        let scroll = SheetScroll(frame: NSRect(x: 0, y: 0, width: width, height: 600))
        let window = NSWindow(contentRect: NSRect(x: 0, y: 0, width: width, height: 600), styleMask: [.borderless], backing: .buffered, defer: false)
        window.isReleasedWhenClosed = false
        window.contentView = scroll
        scroll.canvas.asks = .of(model)
        scroll.canvas.show(model.sheet, scale: model.sheetZoom)
        window.makeFirstResponder(scroll.canvas)
        return (scroll.canvas, window)
    }

    /// Makes the window another width, as dragging its edge would.
    private func widen(_ window: NSWindow, to width: Double, _ canvas: SheetCanvas) {
        window.setContentSize(NSSize(width: width, height: 600))
        window.contentView?.layoutSubtreeIfNeeded()
        canvas.fit()
    }

    private func row(_ words: String, of model: LibraryModel) throws -> Int {
        try #require(model.sheet?.rows.firstIndex { $0.kind == .line && $0.text.string == words }, "no line \(words)")
    }

    /// A key, as the window would hand it over.
    private func press(_ characters: String, _ flags: NSEvent.ModifierFlags = [], in window: NSWindow) throws -> NSEvent {
        try #require(NSEvent.keyEvent(with: .keyDown, location: .zero, modifierFlags: flags, timestamp: 0, windowNumber: window.windowNumber,
                                      context: nil, characters: characters, charactersIgnoringModifiers: characters, isARepeat: false, keyCode: 0))
    }

    private func special(_ key: Int) -> String {
        String(UnicodeScalar(UInt16(key)).map(Character.init) ?? " ")
    }

    /// The chords a line has in the file now.
    private func chords(of words: String, in mac: MadeUpMac, song: String) throws -> [Chord] {
        let read = ChordSheet(song: try OpenSong(opening: mac.place.appendingPathComponent("Libraries/Songs/\(song).pro")))
        let row = try #require(read.rows.first { $0.kind == .line && $0.text.string == words })
        return row.chords
    }

    private func typeInBox(_ text: String, of canvas: SheetCanvas) throws {
        let editor = try #require(canvas.entry.field.currentEditor() as? NSTextView, "the box is not being typed in")
        editor.insertText(text, replacementRange: editor.selectedRange())
    }

    private func pressInBox(_ command: Selector, of canvas: SheetCanvas) throws {
        let editor = try #require(canvas.entry.field.currentEditor() as? NSTextView, "the box is not being typed in")
        editor.doCommand(by: command)
    }

    static let first = "Amazing grace how sweet the sound"
    static let second = "That saved a wretch like me"

    @Test("The spot follows the pointer: to the character under it, with Shift to the word, over a chord's bubble to that chord")
    func followsThePointer() async throws {
        let mac = try MadeUpMac()
        let model = await open("Amazing Grace", on: mac)
        let (canvas, window) = view(for: model)
        defer { window.close() }
        let sheet = try #require(model.sheet)
        let line = try row(Self.second, of: model)
        canvas.pointerMoved(to: try #require(canvas.point(over: Spot(row: line, at: 7))), shift: false)
        #expect(sheet.spot == Spot(row: line, at: 7))
        canvas.pointerMoved(to: try #require(canvas.point(over: Spot(row: line, at: 7))), shift: true)
        #expect(sheet.spot == Spot(row: line, at: 5))
        // The space after "That" means the word after.
        canvas.pointerMoved(to: try #require(canvas.point(over: Spot(row: line, at: 4))), shift: false)
        #expect(sheet.spot == Spot(row: line, at: 5))
        let chord = try #require(sheet.rows[line].chords.first)
        canvas.pointerMoved(to: try #require(canvas.point(over: Spot(row: line, at: chord.at), onItsChord: true)), shift: false)
        #expect(sheet.spot == Spot(row: line, at: chord.at))
        // Beside the cards is bare sheet: the spot stays where it was.
        canvas.pointerMoved(to: NSPoint(x: canvas.bounds.maxX - 3, y: 200), shift: false)
        #expect(sheet.spot == Spot(row: line, at: chord.at))
    }

    @Test("A place along a line never goes backwards: a zero-width space that ends a line is where the line ends, whatever Core Text says of it")
    func neverBackwards() {
        // (Made-up words. Real songs have such lines: a zero-width space after the
        // last word, for a chord to hang on.)
        let units = Array("sing forever".utf16) + [0x200B]
        let font = SheetMetrics(scale: 1).wordFont
        let line = SheetMetrics.line(String(decoding: units, as: UTF16.self), font: font)
        let edges = SheetMetrics.edges(of: line)
        #expect(edges.count == units.count + 1)
        #expect(zip(edges, edges.dropFirst()).allSatisfy { $0 <= $1 })
        #expect(edges[units.count - 1] == edges[units.count] && edges[units.count - 1] > edges[units.count - 2])
        // So a pointer over the last letter is over the last letter.
        let overTheLastLetter = (edges[units.count - 2] + edges[units.count - 1]) / 2
        #expect(ChordLayout.characterAt(length: units.count, x: overTheLastLetter) { edges[$0] } == units.count - 2)
        // And two of them with a space between, as some lines end.
        let more = SheetMetrics.edges(of: SheetMetrics.line(String(decoding: units + [0x20, 0x200B], as: UTF16.self), font: font))
        #expect(zip(more, more.dropFirst()).allSatisfy { $0 <= $1 })
    }

    @Test("The slides are a grid of cards of one size: as many to a row as fit across, each as wide as the widest slide of the song needs and the shape of a screen, with its lines in its middle; a narrower window has fewer to a row, and smaller words more")
    func aGridOfSlides() async throws {
        let mac = try MadeUpMac()
        let model = await open("It Is Well", on: mac)
        let (canvas, window) = view(for: model, width: 1500)
        defer { window.close() }
        let sheet = try #require(model.sheet)
        let slides = sheet.rows.filter { $0.kind == .line && $0.first }.count
        #expect(slides >= 4)

        /// What a slide's lines need across: the widest of them with its chords (a
        /// line of chords alone, and room for one more), and the room inside the card.
        func needed(by cell: SheetCanvas.Cell) -> Double {
            (cell.rows.map { canvas.laid[$0].placed.width + (sheet.rows[$0].alone ? 44 * canvas.metrics.scale : 0) }.max() ?? 0) + 2 * canvas.metrics.cardPad
        }

        func held(_ what: String) {
            #expect(canvas.cells.count == slides, "\(what)")
            #expect(canvas.cells.map(\.slide) == Array(0..<slides), "\(what)")
            let first = canvas.cells[0]
            let line = canvas.metrics.lineHeight
            // The shape of a screen, 16 by 9, though never taller for that than four
            // lines need.
            let shaped = min(first.card.width * 9 / 16, 4 * line + 2 * canvas.metrics.cardPad)
            for (n, cell) in canvas.cells.enumerated() {
                // In the order of the song: along a row, and then the next row.
                #expect(cell.gridRow == n / canvas.columns && cell.column == n % canvas.columns, "\(what)")
                // Every card is one width, which is enough for each of them.
                #expect(abs(cell.frame.width - first.frame.width) < 0.01, "\(what)")
                #expect(cell.card.width >= needed(by: cell) - 0.01, "\(what)")
                // The card is inside its frame, with the frame's foot under it.
                #expect(cell.frame.contains(cell.card) && abs(cell.frame.maxY - cell.card.maxY - SheetLook.label) < 0.01, "\(what)")
                // Its lines are in it, one under another, as much room over the first
                // as under the last.
                for (down, row) in cell.rows.enumerated() {
                    #expect(canvas.laid[row].cell == n, "\(what)")
                    #expect(canvas.laid[row].left > cell.card.minX, "\(what)")
                    #expect(abs(canvas.laid[row].chordTop - (cell.linesTop + Double(down) * line + 2 * canvas.metrics.scale)) < 0.01, "\(what)")
                }
                let over = cell.linesTop - cell.card.minY
                let under = cell.card.maxY - (cell.linesTop + Double(cell.rows.count) * line)
                #expect(abs(over - under) < 0.01 && over >= canvas.metrics.cardPad - 0.01, "\(what)")
            }
            // No card is wider than it need be: the widest slide's lines fill it.
            #expect((canvas.cells.map { $0.card.width - needed(by: $0) }.min() ?? 1) < 0.01, "\(what)")
            for gridRow in 0...(canvas.cells.last?.gridRow ?? 0) {
                let inRow = canvas.cells.filter { $0.gridRow == gridRow }
                let most = inRow.map(\.rows.count).max() ?? 0
                // Those of a row stand level, the shape of a screen, or as tall as the
                // one of them with the most lines needs if that is more; no two
                // overlap, and the next row starts under this one.
                for cell in inRow {
                    #expect(abs(cell.frame.minY - inRow[0].frame.minY) < 0.01, "\(what)")
                    #expect(abs(cell.card.height - max(Double(most) * line + 2 * canvas.metrics.cardPad, shaped)) < 0.01, "\(what)")
                }
                for (one, other) in zip(inRow, inRow.dropFirst()) {
                    #expect(abs(other.frame.minX - one.frame.maxX - 2 * SheetLook.around) < 0.01, "\(what)")
                }
                if let below = canvas.cells.first(where: { $0.gridRow == gridRow + 1 }) {
                    #expect(abs(below.frame.minY - inRow[0].frame.maxY - 2 * SheetLook.around) < 0.01, "\(what)")
                }
            }
            // Nothing is wider than what it is seen through, where one slide fits it;
            // and one more to a row would not have fitted.
            let right = canvas.cells.map(\.frame.maxX).max() ?? 0
            let seen = Double(window.contentView?.bounds.width ?? 0)
            let cell = first.frame.width + 2 * SheetLook.around
            if cell + 2 * SheetLook.margin <= seen {
                #expect(right + SheetLook.around + SheetLook.margin <= seen + 0.01, "\(what)")
                #expect(canvas.columns == slides || Double(canvas.columns + 1) * cell + 2 * SheetLook.margin > seen, "\(what)")
            }
        }

        held("wide")
        let wide = canvas.columns
        #expect(wide >= 2)
        // (This song's slides have three lines at most, which the shape of a screen
        // holds at this size: every card of it is the same height.)
        #expect(Set(canvas.cells.map { ($0.card.height * 100).rounded() }).count == 1)
        #expect(abs(canvas.cells[0].card.height - canvas.cells[0].card.width * 9 / 16) < 0.01)
        widen(window, to: 620, canvas)
        held("narrow")
        let narrow = canvas.columns
        #expect(narrow < wide && narrow >= 1)

        // A chord that sticks out past the end of the widest line of the song makes
        // every card wider by as much; taken off, they are as they were.
        widen(window, to: 1500, canvas)
        let before = canvas.cells.map(\.frame.width)
        let widest = try #require(canvas.cells.max { needed(by: $0) < needed(by: $1) })
        let line = try #require(widest.rows.max { canvas.laid[$0].placed.width < canvas.laid[$1].placed.width })
        #expect(sheet.put(line, sheet.rows[line].text.length - 1, "F#m7b5/A"))
        canvas.sync()
        let after = canvas.cells.map(\.frame.width)
        #expect(after.allSatisfy { $0 > before[0] + 20 } && Set(after.map { ($0 * 100).rounded() }).count == 1)
        held("with a long chord at a line's end")
        model.undo()
        canvas.sync()
        #expect(canvas.cells.map(\.frame.width) == before)

        // Smaller words: more to a row. Larger: fewer. (In a window where not all of
        // the song's slides go to one row to begin with.)
        widen(window, to: 760, canvas)
        let middling = canvas.columns
        #expect(middling < slides)
        canvas.setScale(0.5)
        held("small")
        #expect(canvas.columns > middling)
        canvas.setScale(1.5)
        held("large")
        #expect(canvas.columns <= middling && (middling == 1 || canvas.columns < middling))
    }

    /// A song of the test's own making, that can be looked at and not changed.
    private final class MadeUpSong: SheetSong {
        let textBoxes: [TextBox]
        let key = "C"
        init(_ textBoxes: [TextBox]) { self.textBoxes = textBoxes }
        var usedChords: [String] { textBoxes.flatMap { $0.chords.map(\.name) } }
        func setChords(slide: Int, element: String, _ chords: [Chord], joined: Bool) throws {}
        func setChordsAlone(slide: Int, element: String, line: Int, names: [String], joined: Bool) throws {}
    }

    /// The sheet of a made-up song, in a window wide enough for all of it in one row.
    private func canvas(of boxes: [TextBox], scale: Double = 1) -> (SheetCanvas, ChordSheet, NSWindow) {
        let sheet = ChordSheet(song: MadeUpSong(boxes))
        let scroll = SheetScroll(frame: NSRect(x: 0, y: 0, width: 3000, height: 600))
        let window = NSWindow(contentRect: NSRect(x: 0, y: 0, width: 3000, height: 600), styleMask: [.borderless], backing: .buffered, defer: false)
        window.isReleasedWhenClosed = false
        window.contentView = scroll
        scroll.canvas.show(sheet, scale: scale)
        return (scroll.canvas, sheet, window)
    }

    @Test("A card is as wide as the widest slide needs and no wider: a line of chords alone, with room past its last chord for the next to be pointed at; a blank slide, for its one place; never too narrow for the words on its foot; and never less than the least")
    func asWideAsItNeeds() throws {
        // Each of these is a song of one slide, so that its card is as wide as that
        // one slide needs.
        // Two chords alone: their bubbles, and 44 more for the place past the last.
        do {
            let (canvas, sheet, window) = canvas(of: [TextBox(slide: 0, element: "A", words: Chords.placeholders(2),
                                                              chords: [Chord(at: 0, name: "Dsus4"), Chord(at: 2, name: "G/B")])])
            defer { window.close() }
            let pad = canvas.metrics.cardPad
            #expect(abs(canvas.cells[0].card.width - (canvas.laid[0].placed.width + 44 + 2 * pad)) < 0.01)
            canvas.pointerMoved(to: NSPoint(x: canvas.cells[0].card.maxX - pad - 3, y: canvas.cells[0].card.midY), shift: false)
            #expect(sheet.spot == Spot(row: 0, at: 4))
        }
        // A blank slide: its one place, and no more.
        do {
            let (canvas, sheet, window) = canvas(of: [TextBox(slide: 0, element: "A", words: Words(), blank: true)])
            defer { window.close() }
            #expect(abs(canvas.cells[0].card.width - (44 + 2 * canvas.metrics.cardPad)) < 0.01)
            canvas.pointerMoved(to: NSPoint(x: canvas.cells[0].card.midX, y: canvas.cells[0].card.midY), shift: false)
            #expect(sheet.spot == Spot(row: 0, at: 0))
        }
        // Two letters under a long name: the frame is as wide as the foot's words need.
        do {
            let (canvas, _, window) = canvas(of: [TextBox(slide: 0, group: "A very long name for a group of one small slide", startsItsGroup: true,
                                                          element: "A", words: "Oh")])
            defer { window.close() }
            #expect(canvas.cells[0].label == "1  A very long name for a group of one small slide")
            let foot = SheetMetrics.width(of: SheetMetrics.line(canvas.cells[0].label, font: SheetLook.labelFont))
            #expect(abs(canvas.cells[0].frame.width - (foot + 16)) < 0.01)
        }
        // A chord that sticks out past the end of the words: the card is as wide as
        // the line WITH it.
        do {
            let (canvas, _, window) = canvas(of: [TextBox(slide: 0, element: "A", words: "One two three four five six",
                                                          chords: [Chord(at: 24, name: "F#m7b5/A")])])
            defer { window.close() }
            let words = canvas.laid[0].placed
            #expect(words.width > words.wordsWidth + 20)
            #expect(abs(canvas.cells[0].card.width - (words.width + 2 * canvas.metrics.cardPad)) < 0.01)
        }
        // The least a card is, for two letters: 40 and the room inside it.
        do {
            let (canvas, _, window) = canvas(of: [TextBox(slide: 0, element: "A", words: "Oh")])
            defer { window.close() }
            #expect(canvas.cells[0].label == "1")
            #expect(abs(canvas.cells[0].card.width - (40 + 2 * canvas.metrics.cardPad)) < 0.01)
        }
        // And together in one song, every card is as wide as the one that needs most.
        do {
            let (canvas, _, window) = canvas(of: [
                TextBox(slide: 0, element: "A", words: "Oh"),
                TextBox(slide: 1, element: "B", words: "One two three four five six"),
                TextBox(slide: 2, element: "C", words: Words(), blank: true),
            ])
            defer { window.close() }
            let widest = canvas.laid[1].placed.width + 2 * canvas.metrics.cardPad
            #expect(canvas.columns == 3 && canvas.cells.allSatisfy { abs($0.card.width - widest) < 0.01 })
        }
    }

    @Test("The shape of a screen has its limits: a song of long lines has cards no taller for it than four lines need, and a slide of more lines than the shape holds makes its row as tall as it needs, and no other row")
    func theShapeHasItsLimits() throws {
        let long = String(repeating: "word ", count: 30) + "end"
        let many = (1...7).map { "Line \($0)" }.joined(separator: "\n")
        let (canvas, _, window) = canvas(of: [
            TextBox(slide: 0, element: "A", words: Words(long)),
            TextBox(slide: 1, element: "B", words: Words(many)),
            TextBox(slide: 2, element: "C", words: "Short"),
        ], scale: 0.5)
        defer { window.close() }
        let line = canvas.metrics.lineHeight
        let pad = canvas.metrics.cardPad
        // (At half size the three are side by side in the test's wide window, or one
        // under another: either way each row is as tall as its tallest needs.)
        let four = 4 * line + 2 * pad
        #expect(canvas.cells[0].card.width * 9 / 16 > four)
        for cell in canvas.cells {
            let most = canvas.cells.filter { $0.gridRow == cell.gridRow }.map(\.rows.count).max() ?? 0
            #expect(abs(cell.card.height - max(Double(most) * line + 2 * pad, four)) < 0.01)
        }
        #expect(canvas.cells.contains { abs($0.card.height - (7 * line + 2 * pad)) < 0.01 })
    }

    @Test("A slide's frame says its number, and on the first slide of each group the group's name; a group with a colour frames its slides in it")
    func framedInItsGroupsColour() async throws {
        let mac = try MadeUpMac()
        let model = await open("Rock Of Ages (Toplady)", on: mac)
        let (canvas, window) = view(for: model)
        defer { window.close() }
        let sheet = try #require(model.sheet)
        let labels = canvas.cells.map(\.label)
        #expect(labels.first == "1  Verse 1")
        #expect(labels.dropFirst().first == "2")
        // One name for each group of the song that has one, in order.
        let named = labels.filter { $0.contains("  ") }.map { String($0.split(separator: " ", maxSplits: 1).last ?? "").trimmingCharacters(in: .whitespaces) }
        #expect(named == sheet.rows.filter { $0.kind == .group }.map(\.name))
        // The file gives its groups colours: the frames are theirs, and not the plain one.
        #expect(canvas.cells.allSatisfy { $0.colour != SheetLook.group })
        #expect(Set(canvas.cells.map(\.colour)).count > 1)
        // A song that gives none is framed in the plain colour.
        let plain = await open("Amazing Grace", on: mac)
        canvas.asks = .of(plain)
        canvas.show(plain.sheet, scale: plain.sheetZoom)
        #expect(canvas.cells.allSatisfy { $0.colour == SheetLook.group })
    }

    @Test("Down and Up go to the line under or over on the screen: within a slide, and from its last line to the slide under it in the grid, not to the next slide of the song")
    func downAndUpInTheGrid() async throws {
        let mac = try MadeUpMac()
        let model = await open("Amazing Grace", on: mac)
        let (canvas, window) = view(for: model, width: 1500)
        defer { window.close() }
        let sheet = try #require(model.sheet)
        let columns = canvas.columns
        #expect(columns >= 2 && canvas.cells.count > columns)
        let first = canvas.cells[0]
        let under = canvas.cells[columns]
        let lastLine = first.rows.upperBound - 1
        // Down the first slide's own lines, and then to the slide under it.
        sheet.setSpot(first.rows.lowerBound, 0)
        for _ in 1..<first.rows.count {
            canvas.keyDown(with: try press(special(NSDownArrowFunctionKey), in: window))
        }
        #expect(sheet.spot.row == lastLine)
        canvas.keyDown(with: try press(special(NSDownArrowFunctionKey), in: window))
        #expect(sheet.spot.row == under.rows.lowerBound)
        #expect(sheet.picked == under.slide)
        // (The next slide of the song is the one beside it, and is not where Down goes.)
        #expect(canvas.cells[1].rows.lowerBound != under.rows.lowerBound)
        canvas.keyDown(with: try press(special(NSUpArrowFunctionKey), in: window))
        #expect(sheet.spot.row == lastLine)
        // Right still goes on through the song: from the end of a slide's last line to
        // the first word of the next slide, which is beside it.
        let stops = sheet.stops(lastLine)
        sheet.setSpot(lastLine, try #require(stops.last))
        canvas.keyDown(with: try press(special(NSRightArrowFunctionKey), in: window))
        #expect(sheet.spot.row == canvas.cells[1].rows.lowerBound)
        // From the second slide of a row, Down goes to the second of the row under it.
        if canvas.cells.count > columns + 1 {
            sheet.setSpot(canvas.cells[1].rows.upperBound - 1, 0)
            canvas.keyDown(with: try press(special(NSDownArrowFunctionKey), in: window))
            #expect(sheet.spot.row == canvas.cells[columns + 1].rows.lowerBound)
        }
        // From the last row of slides, Down goes nowhere; and where the row under is a
        // short one, to its last slide.
        let last = try #require(canvas.cells.last)
        sheet.setSpot(last.rows.upperBound - 1, 0)
        canvas.keyDown(with: try press(special(NSDownArrowFunctionKey), in: window))
        #expect(sheet.spot.row == last.rows.upperBound - 1)
        if last.column < columns - 1, last.gridRow > 0 {
            let above = try #require(canvas.cells.first { $0.gridRow == last.gridRow - 1 && $0.column == columns - 1 })
            sheet.setSpot(above.rows.upperBound - 1, 0)
            canvas.keyDown(with: try press(special(NSDownArrowFunctionKey), in: window))
            #expect(sheet.spot.row == last.rows.lowerBound)
        }
    }

    @Test("Laid out again, for another size of words, the slide that was at the top of what is seen is still there")
    func staysWhereItWas() async throws {
        let mac = try MadeUpMac()
        let model = await open("Amazing Grace", on: mac)
        let (canvas, window) = view(for: model, width: 620)
        defer { window.close() }
        #expect(canvas.columns == 1 && canvas.cells.count > 5)
        #expect(canvas.bounds.height > 900)
        let fourth = canvas.cells[3]
        canvas.scroll(NSPoint(x: 0, y: fourth.frame.minY - 10))
        #expect(abs(canvas.visibleRect.minY - (fourth.frame.minY - 10)) < 1)
        canvas.setScale(0.9)
        #expect(canvas.metrics.scale == 0.9 && canvas.columns == 1)
        #expect(canvas.cells[3].frame.minY != fourth.frame.minY)
        #expect(abs(canvas.visibleRect.minY - (canvas.cells[3].frame.minY - 10)) < 1)
    }

    @Test("Every part of a card is one of its lines: the room over its first line is that line's, and the room under its last, the last's; a frame and its foot are bare sheet")
    func overAndUnderItsLines() async throws {
        let mac = try MadeUpMac()
        let model = await open("It Is Well", on: mac)
        let (canvas, window) = view(for: model, width: 1500)
        defer { window.close() }
        let sheet = try #require(model.sheet)
        // A slide of two lines or more, both of which can take the spot.
        let cell = try #require(canvas.cells.first { $0.rows.count >= 2 && sheet.usable($0.rows.lowerBound) && sheet.usable($0.rows.upperBound - 1) })
        #expect(cell.linesTop - cell.card.minY > 6)
        canvas.pointerMoved(to: NSPoint(x: cell.card.minX + 30, y: cell.card.minY + 2), shift: true)
        #expect(sheet.spot.row == cell.rows.lowerBound)
        canvas.pointerMoved(to: NSPoint(x: cell.card.minX + 30, y: cell.card.maxY - 2), shift: true)
        #expect(sheet.spot.row == cell.rows.upperBound - 1)
        // And between the two, each line's own height is its own: from the top of its
        // row of chords to the foot of its words.
        for row in cell.rows where sheet.usable(row) {
            let top = canvas.laid[row].chordTop
            canvas.pointerMoved(to: NSPoint(x: cell.card.minX + 30, y: top + 1), shift: true)
            #expect(sheet.spot.row == row)
            canvas.pointerMoved(to: NSPoint(x: cell.card.minX + 30, y: top + canvas.metrics.lineHeight - 4), shift: true)
            #expect(sheet.spot.row == row)
        }
        canvas.pointerMoved(to: NSPoint(x: cell.card.minX + 30, y: cell.card.maxY - 2), shift: true)
        // Its frame's foot, and the strip of frame over the card, are bare sheet: the
        // spot stays where it was, though a place of the line would be another.
        let was = sheet.spot
        let length = sheet.rows[was.row].text.length
        #expect(length > 3)
        let along = canvas.laid[was.row].left + canvas.metrics.advance(row: was.row, units: length - 1) + 1
        canvas.pointerMoved(to: NSPoint(x: along, y: cell.frame.maxY - 3), shift: false)
        #expect(sheet.spot == was)
        canvas.pointerMoved(to: NSPoint(x: along, y: cell.frame.minY + 1), shift: false)
        #expect(sheet.spot == was)
        // (The same place along, down on the card, is the line's last letter.)
        canvas.pointerMoved(to: NSPoint(x: along, y: cell.card.maxY - 3), shift: false)
        #expect(sheet.spot == Spot(row: was.row, at: length - 1))
    }

    @Test("Command with minus and plus make the words a step smaller and larger, and Command with 0 their usual size; it stops at its ends, and is remembered")
    func zoomed() async throws {
        let mac = try MadeUpMac()
        let model = await open("Amazing Grace", on: mac)
        let (canvas, window) = view(for: model, width: 1500)
        defer { window.close() }
        #expect(model.sheetZoom == LibraryModel.usualSheetZoom && canvas.metrics.scale == LibraryModel.usualSheetZoom)
        let usual = canvas.columns
        let lineHeight = canvas.metrics.lineHeight
        #expect(canvas.performKeyEquivalent(with: try press("-", .command, in: window)))
        #expect(model.sheetZoom == 0.7 && canvas.metrics.scale == 0.7)
        #expect(canvas.metrics.lineHeight < lineHeight && canvas.columns >= usual)
        for _ in 0..<12 {
            #expect(canvas.performKeyEquivalent(with: try press("-", .command, in: window)))
        }
        #expect(model.sheetZoom == 0.5 && !model.canZoomSheetOut && model.canZoomSheetIn)
        #expect(canvas.columns > usual)
        #expect(canvas.performKeyEquivalent(with: try press("0", .command, in: window)))
        #expect(model.sheetZoom == LibraryModel.usualSheetZoom && canvas.columns == usual)
        // (Plus is Shift with the equals key: either is taken.)
        #expect(canvas.performKeyEquivalent(with: try press("=", .command, in: window)))
        #expect(canvas.performKeyEquivalent(with: try press("+", [.command, .shift], in: window)))
        #expect(model.sheetZoom == 1.0)
        for _ in 0..<12 {
            model.zoomSheet(1)
        }
        #expect(model.sheetZoom == 1.5 && !model.canZoomSheetIn)
        // Nothing of the song is changed by it.
        #expect(!model.canUndo)

        // Another start of the app has the size that was left.
        model.zoomSheet(-1)
        let again = mac.model()
        await again.start()
        #expect(again.sheetZoom == 1.25)
        again.zoomSheet(0)
        let third = mac.model()
        await third.start()
        #expect(third.sheetZoom == LibraryModel.usualSheetZoom)
    }

    @Test("Point and press: 4 puts the key's fourth chord on the spot, in the file; Delete takes it off; Command+Z and Shift+Command+Z walk the song's history")
    func pointAndPress() async throws {
        let mac = try MadeUpMac()
        let model = await open("Amazing Grace", on: mac)
        let (canvas, window) = view(for: model)
        defer { window.close() }
        let before = try mac.bytes(of: "Amazing Grace")
        let had = try chords(of: Self.second, in: mac, song: "Amazing Grace")
        let line = try row(Self.second, of: model)
        canvas.pointerMoved(to: try #require(canvas.point(over: Spot(row: line, at: 5))), shift: false)
        canvas.keyDown(with: try press("4", in: window))
        #expect(try chords(of: Self.second, in: mac, song: "Amazing Grace") == (had + [Chord(at: 5, name: "C")]).sorted { $0.at < $1.at })
        #expect(model.shown?.chords == 21 || model.shown?.chords == (model.sheet?.used.count))

        canvas.keyDown(with: try press(special(NSDeleteCharacter), in: window))
        #expect(try chords(of: Self.second, in: mac, song: "Amazing Grace") == had)

        // The two changes, taken back one at a time, and put again.
        #expect(canvas.performKeyEquivalent(with: try press("z", .command, in: window)))
        #expect(try chords(of: Self.second, in: mac, song: "Amazing Grace").contains(Chord(at: 5, name: "C")))
        #expect(canvas.performKeyEquivalent(with: try press("z", .command, in: window)))
        #expect(try mac.bytes(of: "Amazing Grace") == before)
        #expect(!model.canUndo && model.canRedo)
        #expect(canvas.performKeyEquivalent(with: try press("z", [.command, .shift], in: window)))
        #expect(try chords(of: Self.second, in: mac, song: "Amazing Grace").contains(Chord(at: 5, name: "C")))
        // The same from the Edit menu.
        #expect(canvas.validateUserInterfaceItem(NSMenuItem(title: "Undo", action: #selector(SheetCanvas.undo(_:)), keyEquivalent: "z")))
        canvas.undo(nil)
        #expect(try mac.bytes(of: "Amazing Grace") == before)
        #expect(!canvas.validateUserInterfaceItem(NSMenuItem(title: "Undo", action: #selector(SheetCanvas.undo(_:)), keyEquivalent: "z")))
    }

    @Test("The arrow keys: Right a word, Shift+Right a letter, Down to the line below; with no spot yet, the first word of the song")
    func theArrowKeys() async throws {
        let mac = try MadeUpMac()
        let model = await open("Amazing Grace", on: mac)
        let (canvas, window) = view(for: model)
        defer { window.close() }
        let before = try mac.bytes(of: "Amazing Grace")
        let sheet = try #require(model.sheet)
        let line = try row(Self.first, of: model)
        canvas.keyDown(with: try press(special(NSRightArrowFunctionKey), in: window))
        #expect(sheet.spot == Spot(row: line, at: 0))
        canvas.keyDown(with: try press(special(NSRightArrowFunctionKey), in: window))
        #expect(sheet.spot == Spot(row: line, at: 8))
        canvas.keyDown(with: try press(special(NSRightArrowFunctionKey), .shift, in: window))
        #expect(sheet.spot == Spot(row: line, at: 9))
        canvas.keyDown(with: try press(special(NSLeftArrowFunctionKey), in: window))
        #expect(sheet.spot == Spot(row: line, at: 8))
        canvas.keyDown(with: try press(special(NSDownArrowFunctionKey), in: window))
        #expect(sheet.spot.row == line + 1)
        canvas.keyDown(with: try press(special(NSUpArrowFunctionKey), in: window))
        #expect(sheet.spot == Spot(row: line, at: 8))
        // A key that is nothing here does nothing, and a key with Option is not the sheet's.
        canvas.keyDown(with: try press("x", in: window))
        canvas.keyDown(with: try press("4", .option, in: window))
        #expect(try mac.bytes(of: "Amazing Grace") == before)
    }

    @Test("A letter opens the bubble with the keyboard in its box; what could not be a chord is undone; Return keeps the chord and gives the keyboard back")
    func typedInTheBubble() async throws {
        let mac = try MadeUpMac()
        let model = await open("Amazing Grace", on: mac)
        let (canvas, window) = view(for: model)
        defer { window.close() }
        let sheet = try #require(model.sheet)
        let line = try row(Self.second, of: model)
        canvas.pointerMoved(to: try #require(canvas.point(over: Spot(row: line, at: 5))), shift: false)
        canvas.keyDown(with: try press("e", in: window))
        #expect(sheet.typing && !canvas.entry.isHidden)
        #expect(canvas.entry.field.stringValue == "E")
        #expect(window.firstResponder === canvas.entry.field.currentEditor())
        // The caret is after the letter, so that what is typed next goes on from it.
        try typeInBox("m", of: canvas)
        #expect(canvas.entry.field.stringValue == "Em")
        try typeInBox("!", of: canvas)
        #expect(canvas.entry.field.stringValue == "Em")
        try typeInBox("7/g", of: canvas)
        #expect(canvas.entry.field.stringValue == "Em7/G")
        try pressInBox(#selector(NSResponder.insertNewline(_:)), of: canvas)
        #expect(!sheet.typing && canvas.entry.isHidden)
        #expect(window.firstResponder === canvas)
        #expect(try chords(of: Self.second, in: mac, song: "Amazing Grace").contains(Chord(at: 5, name: "Em7/G")))

        // Return on a chord opens the bubble on it, all of it selected to be typed
        // over; Esc throws the typing away.
        canvas.keyDown(with: try press("\r", in: window))
        #expect(sheet.typing && canvas.entry.field.stringValue == "Em7/G")
        try typeInBox("D", of: canvas)
        #expect(canvas.entry.field.stringValue == "D")
        try pressInBox(#selector(NSResponder.cancelOperation(_:)), of: canvas)
        #expect(!sheet.typing && window.firstResponder === canvas)
        #expect(try chords(of: Self.second, in: mac, song: "Amazing Grace").contains(Chord(at: 5, name: "Em7/G")))

        // Emptied, Backspace takes the chord off.
        canvas.keyDown(with: try press("\r", in: window))
        try typeInBox("D", of: canvas)
        try pressInBox(#selector(NSResponder.deleteBackward(_:)), of: canvas)
        #expect(sheet.typing && canvas.entry.field.stringValue.isEmpty)
        try pressInBox(#selector(NSResponder.deleteBackward(_:)), of: canvas)
        #expect(!sheet.typing)
        #expect(!(try chords(of: Self.second, in: mac, song: "Amazing Grace")).contains { $0.at == 5 })
    }

    @Test("Click, type, click, type: a click on another place keeps what was typed and opens the bubble there; Tab keeps it and moves on; a click on the bare sheet keeps it too")
    func clickTypeClickType() async throws {
        let mac = try MadeUpMac()
        let model = await open("Amazing Grace", on: mac)
        let (canvas, window) = view(for: model)
        defer { window.close() }
        let sheet = try #require(model.sheet)
        let line = try row(Self.second, of: model)
        func click(_ at: Int, on row: Int = line) throws {
            let point = try #require(canvas.point(over: Spot(row: row, at: at)))
            canvas.pressed(at: point, shift: false)
            canvas.released(at: point, shift: false, copy: false)
        }
        try click(0)
        #expect(sheet.typing && sheet.typingAt == Spot(row: line, at: 0) && canvas.entry.field.stringValue.isEmpty)
        try typeInBox("d", of: canvas)
        // (A click on the sheet is not to take the keyboard from the box meanwhile.)
        #expect(!canvas.acceptsFirstResponder)
        // While it is typed the spot goes on following the pointer.
        canvas.pointerMoved(to: try #require(canvas.point(over: Spot(row: line, at: 5))), shift: false)
        #expect(sheet.spot == Spot(row: line, at: 5) && sheet.typingAt == Spot(row: line, at: 0))
        try click(5)
        #expect(try chords(of: Self.second, in: mac, song: "Amazing Grace").contains(Chord(at: 0, name: "D")))
        #expect(sheet.typing && sheet.typingAt == Spot(row: line, at: 5))
        #expect(window.firstResponder === canvas.entry.field.currentEditor())
        try typeInBox("a", of: canvas)
        try typeInBox("m", of: canvas)
        try pressInBox(#selector(NSResponder.insertTab(_:)), of: canvas)
        #expect(try chords(of: Self.second, in: mac, song: "Amazing Grace").contains(Chord(at: 5, name: "Am")))
        #expect(!sheet.typing && sheet.spot == Spot(row: line, at: 11))

        canvas.keyDown(with: try press("b", in: window))
        try typeInBox("m", of: canvas)
        let bare = NSPoint(x: canvas.bounds.maxX - 3, y: 30)
        canvas.pressed(at: bare, shift: false)
        canvas.released(at: bare, shift: false, copy: false)
        #expect(!sheet.typing)
        #expect(try chords(of: Self.second, in: mac, song: "Amazing Grace").contains(Chord(at: 11, name: "Bm")))

        // A click is where the button is let go: pressed on one word and let go on
        // another of the line, the bubble opens on the second.
        let down = try #require(canvas.point(over: Spot(row: line, at: 20)))
        let up = try #require(canvas.point(over: Spot(row: line, at: 25)))
        canvas.pressed(at: down, shift: false)
        canvas.released(at: up, shift: false, copy: false)
        #expect(sheet.typing && sheet.typingAt == Spot(row: line, at: 25))
        try pressInBox(#selector(NSResponder.cancelOperation(_:)), of: canvas)

        // A click on what is offered keeps that one.
        try click(13)
        try typeInBox("e", of: canvas)
        let offered = try #require(sheet.offered.first)
        canvas.entry.chosen(offered)
        #expect(!sheet.typing && canvas.entry.isHidden)
        #expect(try chords(of: Self.second, in: mac, song: "Amazing Grace").contains(Chord(at: 13, name: offered)))
    }

    @Test("Down and Up pick from what is offered, and Right puts the one picked in the box to be gone on with")
    func pickedFromWhatIsOffered() async throws {
        let mac = try MadeUpMac()
        let model = await open("Amazing Grace", on: mac)
        let (canvas, window) = view(for: model)
        defer { window.close() }
        let sheet = try #require(model.sheet)
        let line = try row(Self.second, of: model)
        canvas.pointerMoved(to: try #require(canvas.point(over: Spot(row: line, at: 5))), shift: false)
        canvas.keyDown(with: try press("d", in: window))
        let offered = sheet.offered
        try pressInBox(#selector(NSResponder.moveDown(_:)), of: canvas)
        try pressInBox(#selector(NSResponder.moveDown(_:)), of: canvas)
        #expect(sheet.entry.pick == 1)
        try pressInBox(#selector(NSResponder.moveRight(_:)), of: canvas)
        #expect(canvas.entry.field.stringValue == offered[1] && sheet.entry.pick == -1)
        // Up from none picked is the last that is offered; Return keeps the one picked.
        try typeInBox("/", of: canvas)
        try pressInBox(#selector(NSResponder.moveUp(_:)), of: canvas)
        let last = try #require(sheet.offered.last)
        #expect(sheet.entry.pick == sheet.offered.count - 1)
        try pressInBox(#selector(NSResponder.insertNewline(_:)), of: canvas)
        #expect(try chords(of: Self.second, in: mac, song: "Amazing Grace").contains(Chord(at: 5, name: last)))
    }

    @Test("A chord is dragged to another word once the pointer has gone 6; with Option it is copied; while another is typed it is not carried, and letting go is a click on it")
    func draggedAndCopied() async throws {
        let mac = try MadeUpMac()
        let model = await open("Amazing Grace", on: mac)
        let (canvas, window) = view(for: model)
        defer { window.close() }
        let sheet = try #require(model.sheet)
        let line = try row(Self.second, of: model)
        let chord = try #require(sheet.rows[line].chords.first)
        let from = try #require(canvas.point(over: Spot(row: line, at: chord.at), onItsChord: true))
        canvas.pressed(at: from, shift: false)
        canvas.dragged(to: NSPoint(x: from.x + 3, y: from.y), shift: false)
        #expect(sheet.dragged == nil)
        let to = try #require(canvas.point(over: Spot(row: line, at: 5)))
        canvas.dragged(to: to, shift: false)
        #expect(sheet.dragged?.name == chord.name && sheet.spot == Spot(row: line, at: 5))
        canvas.released(at: to, shift: false, copy: false)
        #expect(try chords(of: Self.second, in: mac, song: "Amazing Grace") == [Chord(at: 5, name: chord.name)])
        #expect(sheet.dragged == nil && !sheet.typing)

        // To a line of another slide, with Option: it is copied, and the first stays.
        let other = try row(Self.first, of: model)
        let again = try #require(canvas.point(over: Spot(row: line, at: 5), onItsChord: true))
        let there = try #require(canvas.point(over: Spot(row: other + 3, at: 2)))
        canvas.pressed(at: again, shift: false)
        canvas.dragged(to: there, shift: false)
        canvas.released(at: there, shift: false, copy: true)
        #expect(try chords(of: Self.second, in: mac, song: "Amazing Grace") == [Chord(at: 5, name: chord.name)])
        #expect(sheet.chordAt(other + 3, 2) == chord.name)

        // While another chord is being typed, a press and a pull on a bubble carries
        // nothing: letting go is a click on it.
        canvas.keyDown(with: try press("e", in: window))
        canvas.pressed(at: again, shift: false)
        canvas.dragged(to: there, shift: false)
        #expect(sheet.dragged == nil)
        canvas.released(at: there, shift: false, copy: false)
        #expect(sheet.typing && sheet.typingAt == Spot(row: line, at: 5) && canvas.entry.field.stringValue == chord.name)
        try pressInBox(#selector(NSResponder.cancelOperation(_:)), of: canvas)
    }

    @Test("Command+C copies the spot's line, Shift+Command+C its group, and Command+V puts them on the lines from the spot's on; what was copied is said for a while")
    func copiedOntoOtherLines() async throws {
        let mac = try MadeUpMac()
        let model = await open("Amazing Grace", on: mac)
        model.sayingLasts = .milliseconds(30)
        let (canvas, window) = view(for: model)
        defer { window.close() }
        let untouched = try mac.bytes(of: "Amazing Grace")
        let sheet = try #require(model.sheet)
        let line = try row(Self.first, of: model)
        let secondGroup = try #require(sheet.rows.indices.first { $0 > line && sheet.rows[$0].kind == .group })
        let names = sheet.rows[line].chords.map(\.name)
        sheet.setSpot(line, 0)
        #expect(canvas.performKeyEquivalent(with: try press("c", .command, in: window)))
        #expect(model.notice == "\(names.count) chords copied from the line: Command+V puts them on the lines from the spot's on, word for word")
        #expect(canvas.performKeyEquivalent(with: try press("C", [.command, .shift], in: window)))
        #expect(model.notice.contains("copied from 4 lines"))
        await model.whatWasSaidIsGone()
        #expect(model.notice == LibraryModel.sheetHelp)

        // The first line's chords onto the second, word for word: each on the same
        // numbered word, as far into it as it was.
        sheet.setSpot(line, 0)
        #expect(canvas.performKeyEquivalent(with: try press("c", .command, in: window)))
        sheet.setSpot(line + 1, 0)
        #expect(canvas.performKeyEquivalent(with: try press("v", .command, in: window)))
        #expect(try chords(of: Self.second, in: mac, song: "Amazing Grace")
            == [Chord(at: 1, name: "G"), Chord(at: 5, name: "G7"), Chord(at: 13, name: "C"), Chord(at: 25, name: "G")])
        #expect(canvas.performKeyEquivalent(with: try press("z", .command, in: window)))
        #expect(try mac.bytes(of: "Amazing Grace") == untouched)

        // A whole group onto the next: each line's own text box is changed.
        sheet.setSpot(line, 0)
        #expect(canvas.performKeyEquivalent(with: try press("c", [.command, .shift], in: window)))
        sheet.setSpot(secondGroup + 1, 0)
        #expect(canvas.performKeyEquivalent(with: try press("v", .command, in: window)))
        #expect(try mac.bytes(of: "Amazing Grace") != untouched)
        #expect(sheet.rows[secondGroup + 1].chords == [Chord(at: 1, name: "G"), Chord(at: 5, name: "G7"), Chord(at: 16, name: "C"), Chord(at: 26, name: "G")])
        // (The second line's one chord is on its sixth word, and the line it goes to
        // has five: a chord for a word the line has not got is left out.)
        #expect(sheet.rows[secondGroup + 2].chords.isEmpty)
        // However many lines it changed, one Command+Z takes all of it back.
        #expect(canvas.performKeyEquivalent(with: try press("z", .command, in: window)))
        #expect(try mac.bytes(of: "Amazing Grace") == untouched)
    }

    @Test("A paste says what came of it; and a click on a line and then Command+V pastes there, the box the click opened having nothing typed in it")
    func pastedAfterAClick() async throws {
        let mac = try MadeUpMac()
        let model = await open("Amazing Grace", on: mac)
        let (canvas, window) = view(for: model)
        defer { window.close() }
        // (The system's count of what has been copied, kept still for the test.)
        var copiedElsewhere = 7
        canvas.whatWasCopiedLast = { copiedElsewhere }
        let sheet = try #require(model.sheet)
        let first = try row(Self.first, of: model)
        let second = try row(Self.second, of: model)

        // Nothing copied yet: Command+V says so.
        sheet.setSpot(second, 0)
        #expect(canvas.performKeyEquivalent(with: try press("v", .command, in: window)))
        #expect(model.notice.hasPrefix("Nothing has been copied yet."))

        sheet.setSpot(first, 0)
        #expect(canvas.performKeyEquivalent(with: try press("c", .command, in: window)))
        // A click on the second line: the box opens there, empty.
        let point = try #require(canvas.point(over: Spot(row: second, at: 13)))
        canvas.pressed(at: point, shift: false)
        canvas.released(at: point, shift: false, copy: false)
        #expect(sheet.typing && sheet.entry.text.isEmpty)
        #expect(window.firstResponder === canvas.entry.field.currentEditor())
        // (The pointer wanders off to a line of another slide meanwhile, and the spot
        // with it: it is the line that was CLICKED that is pasted onto.)
        let elsewhere = second + 2
        let untouched = sheet.rows[elsewhere].chords
        canvas.pointerMoved(to: try #require(canvas.point(over: Spot(row: elsewhere, at: 0))), shift: false)
        #expect(sheet.spot.row == elsewhere)
        // Command+V is the sheet's: the box closes, changing nothing of its own, and
        // the first line's chords are on the second, word for word.
        #expect(canvas.performKeyEquivalent(with: try press("v", .command, in: window)))
        #expect(!sheet.typing && canvas.entry.isHidden && window.firstResponder === canvas)
        #expect(try chords(of: Self.second, in: mac, song: "Amazing Grace")
            == [Chord(at: 1, name: "G"), Chord(at: 5, name: "G7"), Chord(at: 13, name: "C"), Chord(at: 25, name: "G")])
        #expect(sheet.rows[elsewhere].chords == untouched)
        #expect(model.notice == "4 chords put on the line.")

        // With something typed in the box, Command+V is the box's own.
        canvas.pressed(at: point, shift: false)
        canvas.released(at: point, shift: false, copy: false)
        try typeInBox("D", of: canvas)
        #expect(!canvas.performKeyEquivalent(with: try press("v", .command, in: window)) || sheet.typing)
        #expect(sheet.typing && canvas.entry.field.stringValue.hasPrefix("D"))
        try pressInBox(#selector(NSResponder.cancelOperation(_:)), of: canvas)

        // And if something has been copied elsewhere since, it is the box's own too:
        // what would be pasted is no longer the line's chords.
        copiedElsewhere += 1
        let other = try #require(canvas.point(over: Spot(row: second, at: 20)))
        canvas.pressed(at: other, shift: false)
        canvas.released(at: other, shift: false, copy: false)
        #expect(sheet.typing && sheet.entry.text.isEmpty)
        _ = canvas.performKeyEquivalent(with: try press("v", .command, in: window))
        #expect(sheet.typing)
        // Command+C in an empty box has nothing of the box's to copy: it copies the
        // chords of the box's line (wherever the pointer has gone), and the box stays
        // open.
        let fewer = try #require(sheet.rows.indices.first { sheet.usable($0) && !sheet.rows[$0].alone && sheet.rows[$0].chords.count != 4 && sheet.rows[$0].chords.count > 0 })
        canvas.pointerMoved(to: try #require(canvas.point(over: Spot(row: fewer, at: 0))), shift: false)
        #expect(canvas.performKeyEquivalent(with: try press("c", .command, in: window)))
        #expect(sheet.typing && model.notice == "4 chords copied from the line: Command+V puts them on the lines from the spot's on, word for word")
        #expect(canvas.performKeyEquivalent(with: try press("v", .command, in: window)))
        #expect(!sheet.typing)
        // The keys that zoom are the sheet's whoever has the keyboard.
        canvas.pressed(at: other, shift: false)
        canvas.released(at: other, shift: false, copy: false)
        #expect(canvas.performKeyEquivalent(with: try press("-", .command, in: window)))
        #expect(model.sheetZoom == 0.7 && sheet.typing)
        try pressInBox(#selector(NSResponder.cancelOperation(_:)), of: canvas)
    }

    @Test("What is said of a paste: how many chords went on how many lines, how many had no word to go on, and why a line was passed over")
    func whatIsSaidOfAPaste() async throws {
        #expect(LibraryModel.said(of: .init(lines: 1, chords: 4)) == "4 chords put on the line.")
        #expect(LibraryModel.said(of: .init(lines: 1, chords: 1)) == "1 chord put on the line.")
        #expect(LibraryModel.said(of: .init(lines: 3, chords: 9)) == "9 chords put on 3 lines.")
        #expect(LibraryModel.said(of: .init(lines: 1, chords: 3, leftOut: 1)) == "3 chords put on the line. 1 had no word to go on, and was left out.")
        #expect(LibraryModel.said(of: .init(lines: 2, chords: 2, leftOut: 3)) == "2 chords put on 2 lines. 3 had no word to go on, and were left out.")
        #expect(LibraryModel.said(of: .init(lines: 1, leftOut: 2)) == "None of the copied chords had a word to go on here.")
        let other = "chords copied from a line of words go only onto a line of words, and chords with no words only onto a line with none"
        #expect(LibraryModel.said(of: .init(passedOver: 1)) == "Nothing was put here: \(other).")
        #expect(LibraryModel.said(of: .init(lines: 1, chords: 2, passedOver: 1)) == "2 chords put on the line. 1 line was passed over: \(other).")
        #expect(LibraryModel.said(of: .init()) == nil)

        // On a blank slide, where a line of words' chords cannot go: said, and nothing
        // changed.
        let mac = try MadeUpMac()
        let model = await open("Blank Slides", on: mac)
        let before = try mac.bytes(of: "Blank Slides")
        let sheet = try #require(model.sheet)
        let words = try #require(sheet.rows.indices.first { sheet.usable($0) && !sheet.rows[$0].alone && !sheet.rows[$0].chords.isEmpty })
        let blank = try #require(sheet.rows.firstIndex { $0.blank })
        sheet.setSpot(words, 0)
        model.copyChords(wholeGroup: false)
        sheet.setSpot(blank, 0)
        model.pasteChords()
        #expect(model.notice == "Nothing was put here: \(other).")
        #expect(try mac.bytes(of: "Blank Slides") == before)
    }

    @Test("Leaving the sheet keeps the chord that was being typed: for another song, for the other editor, and when the keyboard goes elsewhere")
    func leavingKeepsIt() async throws {
        let mac = try MadeUpMac()
        let model = await open("Amazing Grace", on: mac)
        let (canvas, window) = view(for: model)
        defer { window.close() }
        let sheet = try #require(model.sheet)
        let line = try row(Self.second, of: model)
        sheet.setSpot(line, 5)
        canvas.keyDown(with: try press("a", in: window))
        try typeInBox("m", of: canvas)
        model.open(song: mac.song("Plain Song"))
        #expect(try chords(of: Self.second, in: mac, song: "Amazing Grace").contains(Chord(at: 5, name: "Am")))

        model.open(song: mac.song("Amazing Grace"))
        canvas.show(model.sheet)
        let again = try #require(model.sheet)
        again.setSpot(line, 13)
        canvas.keyDown(with: try press("e", in: window))
        model.show(editor: .chordPro)
        #expect(try chords(of: Self.second, in: mac, song: "Amazing Grace").contains(Chord(at: 13, name: "E")))
        #expect(model.chordPro?.text.contains("That [Am]saved a [E]wretch") == true)

        // The keyboard taken by something else in the window: the chord is kept.
        model.show(editor: .sheet)
        canvas.show(model.sheet)
        again.setSpot(line, 0)
        canvas.keyDown(with: try press("g", in: window))
        #expect(again.typing)
        window.makeFirstResponder(nil)
        #expect(!again.typing && canvas.entry.isHidden)
        #expect(try chords(of: Self.second, in: mac, song: "Amazing Grace").contains(Chord(at: 0, name: "G")))
    }

    @Test("The two editors keep to one song: a chord put on the sheet is in the text, one typed in the text is on the sheet, and the sheet's undo walks back through both")
    func twoEditorsOneSong() async throws {
        let mac = try MadeUpMac()
        let model = await open("Plain Song", on: mac)
        let before = try mac.bytes(of: "Plain Song")
        let (canvas, window) = view(for: model)
        defer { window.close() }
        let sheet = try #require(model.sheet)
        let line = try #require(sheet.rows.indices.first { sheet.usable($0) && !sheet.rows[$0].alone })
        let words = sheet.rows[line].text.string
        sheet.setSpot(line, 4)
        canvas.keyDown(with: try press("6", in: window))
        let put = try #require(sheet.inKey.dropFirst(5).first)
        model.show(editor: .chordPro)
        let text = try #require(model.chordPro?.text)
        #expect(text.contains("One [\(put)]two"))
        #expect(words == "One two three four")
        #expect(model.textMayBecome(text.replacingOccurrences(of: "One [\(put)]two", with: "One [\(put)]tw[Bm7]o")))
        model.show(editor: .sheet)
        canvas.show(model.sheet)
        #expect(sheet.rows[line].chords.contains { $0.name == "Bm7" })
        #expect(sheet.rows[line].chords.contains(Chord(at: 4, name: put)))
        #expect(canvas.performKeyEquivalent(with: try press("z", .command, in: window)))
        #expect(!sheet.rows[line].chords.contains { $0.name == "Bm7" })
        #expect(canvas.performKeyEquivalent(with: try press("z", .command, in: window)))
        #expect(try mac.bytes(of: "Plain Song") == before)
    }

    @Test("A chord is put on a blank slide by pointing and pressing, like any other, and Delete takes it off again leaving the file as it was. While ProPresenter is running nothing is changed, and the quiet line says why")
    func aBlankSlideAndARefusal() async throws {
        let mac = try MadeUpMac()
        let model = await open("Blank Slides", on: mac)
        let (canvas, window) = view(for: model)
        defer { window.close() }
        let before = try mac.bytes(of: "Blank Slides")
        let sheet = try #require(model.sheet)
        let blank = try #require(sheet.rows.firstIndex { $0.blank })
        canvas.pointerMoved(to: try #require(canvas.point(over: Spot(row: blank, at: 0))), shift: false)
        #expect(sheet.spot == Spot(row: blank, at: 0))
        canvas.keyDown(with: try press("1", in: window))
        canvas.show(model.sheet)
        #expect(model.refusal == nil)
        #expect(sheet.rows[blank].alone && !sheet.rows[blank].blank)
        #expect(sheet.rows[blank].chords == [Chord(at: 0, name: try #require(sheet.inKey.first))])
        #expect(try mac.bytes(of: "Blank Slides") != before)
        // The spot has gone on to where the next would go. Back on the chord, Delete
        // takes it off, and the slide is blank again: in the file, to the byte.
        sheet.setSpot(blank, 0)
        canvas.keyDown(with: try press(special(NSDeleteCharacter), in: window))
        canvas.show(model.sheet)
        #expect(sheet.rows[blank].blank && sheet.rows[blank].chords.isEmpty)
        #expect(try mac.bytes(of: "Blank Slides") == before)

        let words = try #require(sheet.rows.indices.first { sheet.usable($0) && !sheet.rows[$0].alone })
        mac.proPresenterIsRunning = true
        sheet.setSpot(words, 0)
        canvas.keyDown(with: try press("5", in: window))
        #expect(model.notice == "ProPresenter is running. Quit it to change chords here. Nothing was changed.")
        #expect(model.proPresenterIsRunning)
        #expect(try mac.bytes(of: "Blank Slides") == before)
        // A chord typed and refused is not swept away with the song: the song stays.
        canvas.keyDown(with: try press("d", in: window))
        model.open(song: mac.song("Plain Song"))
        #expect(model.shown?.name == "Blank Slides")
        mac.proPresenterIsRunning = false
        model.open(song: mac.song("Plain Song"))
        #expect(model.shown?.name == "Plain Song")
    }

    @Test("A click on one of the key's chords in the strip puts it on the spot; while a chord is typed, where the typing was; and setting the key changes what 1 to 7 give")
    func theStrip() async throws {
        let mac = try MadeUpMac()
        let model = await open("Amazing Grace", on: mac)
        let (canvas, window) = view(for: model)
        defer { window.close() }
        let sheet = try #require(model.sheet)
        let line = try row(Self.second, of: model)
        sheet.setSpot(line, 5)
        model.putOfKey(2)
        #expect(try chords(of: Self.second, in: mac, song: "Amazing Grace").contains(Chord(at: 5, name: "Am")))
        sheet.setSpot(line, 13)
        canvas.keyDown(with: try press("b", in: window))
        try typeInBox("m7", of: canvas)
        model.putOfKey(5)
        canvas.show(model.sheet)
        #expect(!sheet.typing && canvas.entry.isHidden)
        #expect(try chords(of: Self.second, in: mac, song: "Amazing Grace").contains(Chord(at: 13, name: "D")))
        model.setKey("A")
        #expect(sheet.key == "A" && sheet.inKey.first == "A")
        // Changing the key changes no chord.
        #expect(try chords(of: Self.second, in: mac, song: "Amazing Grace").contains(Chord(at: 13, name: "D")))
    }
}
