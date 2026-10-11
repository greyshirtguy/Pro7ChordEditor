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
    private func view(for model: LibraryModel) -> (SheetCanvas, NSWindow) {
        let scroll = SheetScroll(frame: NSRect(x: 0, y: 0, width: 900, height: 600))
        let window = NSWindow(contentRect: NSRect(x: 0, y: 0, width: 900, height: 600), styleMask: [.borderless], backing: .buffered, defer: false)
        window.isReleasedWhenClosed = false
        window.contentView = scroll
        scroll.canvas.asks = .of(model)
        scroll.canvas.show(model.sheet)
        window.makeFirstResponder(scroll.canvas)
        return (scroll.canvas, window)
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

    @Test("While ProPresenter is running nothing is changed, and the quiet line says why. UNTIL M7: nor is a chord put on a blank slide, in its own sentence")
    func refusedInASentence() async throws {
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
        #expect(model.notice.contains("cannot yet be added to a line that has no words"))
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
