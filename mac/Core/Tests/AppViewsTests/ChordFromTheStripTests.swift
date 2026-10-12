// One of the key's chords dragged out of the strip onto a word (spec/03, "A chord
// dragged out of the strip"), in the window: the real strip and the real sheet, in
// windows that are never shown, on a made-up Mac; and then the song's FILE read again.
//
// The carrying itself is the system's: it follows the pointer, and tells the sheet
// what is over it. There is no pointer here for it to follow. So these tests stand at
// the system's two ends and do what it would: at the strip's they press a button and
// pull, and take what the button hands the system to carry; at the sheet's they tell
// the sheet, as the system tells it, that this is over it at such a point and is now
// let go. What that leaves untried is the system between the two, and the feel of it
// under a hand: the maintainer's to say.

import AppKit
@testable import AppViews
import ChordRules
import Song
import SwiftUI
import Testing
import TestData

/// What the system hands a view that a thing is dragged over: here, made by the test.
@MainActor
private final class Dragged: NSObject, @preconcurrency NSDraggingInfo {
    let draggingPasteboard: NSPasteboard
    let draggingDestinationWindow: NSWindow?
    /// Where the pointer is, as the window counts.
    var draggingLocation = NSPoint.zero

    init(_ pasteboard: NSPasteboard, over window: NSWindow) {
        draggingPasteboard = pasteboard
        draggingDestinationWindow = window
    }

    var draggingSourceOperationMask: NSDragOperation { .copy }
    var draggedImageLocation: NSPoint { draggingLocation }
    var draggedImage: NSImage? { nil }
    var draggingSource: Any? { nil }
    var draggingSequenceNumber: Int { 1 }
    var draggingFormation: NSDraggingFormation = .none
    var animatesToDestination = false
    var numberOfValidItemsForDrop = 1
    var springLoadingHighlight: NSSpringLoadingHighlight { .none }
    func slideDraggedImage(to screenPoint: NSPoint) {}
    override func namesOfPromisedFilesDropped(atDestination dropDestination: URL) -> [String]? { nil }
    func enumerateDraggingItems(options enumOpts: NSDraggingItemEnumerationOptions = [], for view: NSView?, classes classArray: [AnyClass],
                                searchOptions: [NSPasteboard.ReadingOptionKey: Any] = [:],
                                using block: @escaping (NSDraggingItem, Int, UnsafeMutablePointer<ObjCBool>) -> Void) {}
    func resetSpringLoading() {}
}

@MainActor
@Suite("A chord carried from the strip, in the window")
struct ChordFromTheStripTests {
    static let first = "Amazing grace how sweet the sound"
    static let second = "That saved a wretch like me"
    static let running = "ProPresenter is running. Quit it to change chords here. Nothing was changed."

    private func open(_ name: String, on mac: MadeUpMac) async -> LibraryModel {
        let model = mac.model()
        model.pauseBeforeSaving = .milliseconds(1)
        await model.start()
        model.choose(library: mac.library("Songs"))
        await model.marksAreWorkedOut()
        model.open(song: mac.song(name))
        return model
    }

    /// The real sheet, wired to a model as the window wires it, in a window that is
    /// never shown.
    private func view(for model: LibraryModel, width: Double = 900, height: Double = 600) -> (SheetCanvas, NSWindow) {
        let scroll = SheetScroll(frame: NSRect(x: 0, y: 0, width: width, height: height))
        let window = NSWindow(contentRect: NSRect(x: 0, y: 0, width: width, height: height), styleMask: [.borderless], backing: .buffered, defer: false)
        window.isReleasedWhenClosed = false
        window.contentView = scroll
        scroll.canvas.asks = .of(model)
        scroll.canvas.show(model.sheet, scale: model.sheetZoom)
        window.makeFirstResponder(scroll.canvas)
        return (scroll.canvas, window)
    }

    /// The real strip, in a window that is never shown, and the seven grips that lie
    /// over its buttons, from the left.
    private func strip(for model: LibraryModel, width: Double = 900) throws -> (grips: [KeyChordGrip], window: NSWindow) {
        let sheet = try #require(model.sheet)
        let host = NSHostingView(rootView: SheetStrip(model: model, sheet: sheet, key: model.shown?.key ?? "").frame(width: width, height: 44))
        let window = NSWindow(contentRect: NSRect(x: 0, y: 0, width: width, height: 44), styleMask: [.borderless], backing: .buffered, defer: false)
        window.isReleasedWhenClosed = false
        window.contentView = host
        host.frame = NSRect(x: 0, y: 0, width: width, height: 44)
        host.layoutSubtreeIfNeeded()
        RunLoop.main.run(until: Date(timeIntervalSinceNow: 0.2))
        host.layoutSubtreeIfNeeded()
        var found: [KeyChordGrip] = []
        func look(in view: NSView) {
            if let grip = view as? KeyChordGrip {
                found.append(grip)
            }
            view.subviews.forEach(look)
        }
        look(in: host)
        return (found.sorted { $0.convert($0.bounds, to: nil).minX < $1.convert($1.bounds, to: nil).minX }, window)
    }

    private func row(_ words: String, of model: LibraryModel) throws -> Int {
        try #require(model.sheet?.rows.firstIndex { $0.kind == .line && $0.text.string == words }, "no line \(words)")
    }

    /// The chords a line has in the file now.
    private func chords(of words: String, in mac: MadeUpMac, song: String) throws -> [Chord] {
        let read = ChordSheet(song: try OpenSong(opening: mac.place.appendingPathComponent("Libraries/Songs/\(song).pro")))
        let row = try #require(read.rows.first { $0.kind == .line && $0.text.string == words })
        return row.chords
    }

    private func press(_ characters: String, _ flags: NSEvent.ModifierFlags = [], in window: NSWindow) throws -> NSEvent {
        try #require(NSEvent.keyEvent(with: .keyDown, location: .zero, modifierFlags: flags, timestamp: 0, windowNumber: window.windowNumber,
                                      context: nil, characters: characters, charactersIgnoringModifiers: characters, isARepeat: false, keyCode: 0))
    }

    private func mouse(_ type: NSEvent.EventType, at point: NSPoint, in window: NSWindow) throws -> NSEvent {
        try #require(NSEvent.mouseEvent(with: type, location: point, modifierFlags: [], timestamp: 0, windowNumber: window.windowNumber,
                                        context: nil, eventNumber: 0, clickCount: 1, pressure: 1))
    }

    /// A pasteboard of the test's own, with what a grip hands the system to carry.
    private func pasteboard(carrying grip: KeyChordGrip) throws -> NSPasteboard {
        let pasteboard = NSPasteboard.withUniqueName()
        pasteboard.clearContents()
        let written = try #require(grip.carried().item as? any NSPasteboardWriting)
        #expect(pasteboard.writeObjects([written]))
        return pasteboard
    }

    // ---- The sheet's end

    @Test("Over a place the chord shows there, and let go it is in the file; over the bare sheet, a frame's foot or off the sheet it shows nowhere, and let go changes nothing. With Shift it goes to the start of the word. It is one step of undo")
    func overAPlaceAndOffThePlaces() async throws {
        let mac = try MadeUpMac()
        let model = await open("Amazing Grace", on: mac)
        let (canvas, window) = view(for: model)
        defer { window.close() }
        let sheet = try #require(model.sheet)
        let before = try mac.bytes(of: "Amazing Grace")
        let line = try row(Self.second, of: model)
        let name = try #require(sheet.inKey.dropFirst(3).first)
        let over = try #require(canvas.point(over: Spot(row: line, at: 7)))

        #expect(canvas.carriedOfKey(name, to: over, shift: false))
        #expect(sheet.dragged?.name == name && sheet.dragged?.from.row == -1 && sheet.spot == Spot(row: line, at: 7))
        #expect(canvas.carriedOfKey(name, to: over, shift: true))
        #expect(sheet.spot == Spot(row: line, at: 5))

        // Beside the cards is bare sheet, and so is a frame's foot: it shows nowhere,
        // and let go there nothing is asked of the song.
        let cell = try #require(canvas.cells.first)
        let foot = NSPoint(x: cell.frame.midX, y: cell.card.maxY + 8)
        for bare in [NSPoint(x: canvas.bounds.maxX - 3, y: 200), foot] {
            #expect(canvas.carriedOfKey(name, to: over, shift: false))
            #expect(!canvas.carriedOfKey(name, to: bare, shift: false))
            #expect(sheet.dragged == nil)
            #expect(!canvas.droppedOfKey(name, at: bare, shift: false))
        }
        // Off the sheet altogether.
        #expect(canvas.carriedOfKey(name, to: over, shift: false))
        #expect(!canvas.carriedOfKey(name, to: nil, shift: false) && sheet.dragged == nil)
        #expect(try mac.bytes(of: "Amazing Grace") == before && !model.canUndo)

        // Let go over the place.
        #expect(canvas.droppedOfKey(name, at: over, shift: false))
        #expect(try chords(of: Self.second, in: mac, song: "Amazing Grace").contains(Chord(at: 7, name: name)))
        #expect(sheet.dragged == nil && !sheet.typing && sheet.spot == Spot(row: line, at: 7))
        #expect(model.refusal == nil)
        // In place of the chord that is there.
        let other = try #require(sheet.inKey.dropFirst(5).first)
        #expect(canvas.droppedOfKey(other, at: try #require(canvas.point(over: Spot(row: line, at: 7))), shift: false))
        #expect(try chords(of: Self.second, in: mac, song: "Amazing Grace").filter { $0.at == 7 } == [Chord(at: 7, name: other)])
        // Each was one step.
        #expect(canvas.performKeyEquivalent(with: try press("z", .command, in: window)))
        #expect(try chords(of: Self.second, in: mac, song: "Amazing Grace").filter { $0.at == 7 } == [Chord(at: 7, name: name)])
        #expect(canvas.performKeyEquivalent(with: try press("z", .command, in: window)))
        #expect(try mac.bytes(of: "Amazing Grace") == before && !model.canUndo)
    }

    @Test("On a line of chords alone it takes the place of the chord it is let go on, or past the last is one more; on a blank slide it is the first, and Delete then leaves the file as it was")
    func withNoWords() async throws {
        let mac = try MadeUpMac()
        // (The song is not among those every made-up Mac has: TestData says why.)
        let song = "Chords With No Words"
        try TestData.file("shaped-as-real-ones/\(song).pro").write(to: mac.place.appendingPathComponent("Libraries/Songs/\(song).pro"))
        let model = await open(song, on: mac)
        let (canvas, window) = view(for: model, width: 1500, height: 900)
        defer { window.close() }
        let sheet = try #require(model.sheet)
        let before = try mac.bytes(of: song)
        let name = try #require(sheet.inKey.dropFirst(4).first)

        let blank = try #require(sheet.rows.firstIndex { $0.blank })
        #expect(canvas.droppedOfKey(name, at: try #require(canvas.point(over: Spot(row: blank, at: 0))), shift: false))
        #expect(sheet.rows[blank].chords == [Chord(at: 0, name: name)] && !sheet.rows[blank].blank)
        #expect(try mac.bytes(of: song) != before && model.refusal == nil)
        sheet.setSpot(blank, 0)
        canvas.keyDown(with: try press(String(UnicodeScalar(UInt16(NSDeleteCharacter)).map(Character.init) ?? " "), in: window))
        #expect(sheet.rows[blank].blank)
        #expect(try mac.bytes(of: song) == before)

        let alone = try #require(sheet.rows.firstIndex { $0.alone && !$0.blank && $0.chords.count >= 2 })
        let names = sheet.rows[alone].chords.map(\.name)
        #expect(canvas.droppedOfKey(name, at: try #require(canvas.point(over: Spot(row: alone, at: 2))), shift: false))
        var wanted = names
        wanted[1] = name
        #expect(sheet.rows[alone].chords.map(\.name) == wanted)
        #expect(canvas.droppedOfKey(name, at: try #require(canvas.point(over: Spot(row: alone, at: names.count * 2))), shift: false))
        #expect(sheet.rows[alone].chords.map(\.name) == wanted + [name])
        // The sheet is laid out again for the line as it now is: a bubble for each of
        // its chords, the new one among them.
        #expect(canvas.laid[alone].placed.xs.count == names.count + 1)
        let read = ChordSheet(song: try OpenSong(opening: mac.place.appendingPathComponent("Libraries/Songs/\(song).pro")))
        #expect(read.rows[alone].chords == sheet.rows[alone].chords && model.refusal == nil)
    }

    @Test("While a chord is being typed, picking one up from the strip keeps what was typed and closes the bubble, and the one carried then goes where it is let go. While ProPresenter is running it is refused, and the quiet line says why")
    func whileAChordIsTyped() async throws {
        let mac = try MadeUpMac()
        let model = await open("Amazing Grace", on: mac)
        let (canvas, window) = view(for: model)
        defer { window.close() }
        let (grips, stripWindow) = try strip(for: model)
        defer { stripWindow.close() }
        let sheet = try #require(model.sheet)
        let line = try row(Self.second, of: model)
        let fourth = try #require(grips.dropFirst(3).first)
        fourth.begin = { _, _, _ in }

        canvas.pointerMoved(to: try #require(canvas.point(over: Spot(row: line, at: 13))), shift: false)
        canvas.keyDown(with: try press("b", in: window))
        let editor = try #require(canvas.entry.field.currentEditor() as? NSTextView, "the box is not being typed in")
        editor.insertText("m7", replacementRange: editor.selectedRange())
        #expect(sheet.typing && sheet.entry.text == "Bm7")

        // The button is pressed and pulled: the chord is picked up.
        fourth.pressed(at: NSPoint(x: 5, y: 5))
        #expect(fourth.pulled(to: NSPoint(x: 5, y: 12)))
        #expect(!sheet.typing)
        #expect(try chords(of: Self.second, in: mac, song: "Amazing Grace").contains(Chord(at: 13, name: "Bm7")))
        // The sheet is told that it has come over it, as the system tells it.
        let carried = try pasteboard(carrying: fourth)
        defer { carried.releaseGlobally() }
        let dragged = Dragged(carried, over: window)
        dragged.draggingLocation = canvas.convert(try #require(canvas.point(over: Spot(row: line, at: 5))), to: nil)
        canvas.shiftIsHeld = { false }
        #expect(canvas.draggingEntered(dragged) == .copy)
        #expect(canvas.entry.isHidden && window.firstResponder === canvas)
        #expect(sheet.dragged?.name == fourth.name && sheet.spot == Spot(row: line, at: 5))
        #expect(canvas.performDragOperation(dragged))
        fourth.carryingEnded()
        let now = try chords(of: Self.second, in: mac, song: "Amazing Grace")
        #expect(now.contains(Chord(at: 5, name: fourth.name)) && now.contains(Chord(at: 13, name: "Bm7")))
        #expect(!fourth.carrying && sheet.dragged == nil)

        // The system ends it while the chord is still shown over a place, and tells
        // only the strip (at Esc, say): nothing is left looking as if it were carried,
        // and the window is told to draw the sheet again.
        fourth.pressed(at: NSPoint(x: 5, y: 5))
        #expect(fourth.pulled(to: NSPoint(x: 5, y: 12)))
        #expect(canvas.draggingUpdated(dragged) == .copy && sheet.dragged != nil)
        let drawn = model.sheetVersion
        fourth.carryingEnded()
        #expect(sheet.dragged == nil && model.sheetVersion > drawn && !fourth.carrying)
        #expect(try chords(of: Self.second, in: mac, song: "Amazing Grace") == now)

        // While ProPresenter is running: refused in the song's own sentence, and the
        // system is told that nothing was done (the picture then goes back).
        let before = try mac.bytes(of: "Amazing Grace")
        mac.proPresenterIsRunning = true
        dragged.draggingLocation = canvas.convert(try #require(canvas.point(over: Spot(row: line, at: 0))), to: nil)
        #expect(canvas.draggingUpdated(dragged) == .copy)
        #expect(!canvas.performDragOperation(dragged))
        #expect(model.notice == Self.running)
        #expect(try mac.bytes(of: "Amazing Grace") == before && sheet.dragged == nil)
    }

    @Test("What the system asks of the sheet as a thing is dragged over it: the sheet takes one of the key's chords from the strip and nothing else; says it may be let go over a place and not off one; and is left with nothing carried when it leaves, or ends")
    func whatTheSystemAsks() async throws {
        let mac = try MadeUpMac()
        let model = await open("Amazing Grace", on: mac)
        let (canvas, window) = view(for: model)
        defer { window.close() }
        let sheet = try #require(model.sheet)
        let before = try mac.bytes(of: "Amazing Grace")
        let line = try row(Self.second, of: model)
        #expect(canvas.registeredDraggedTypes == [.chordOfTheKey])
        canvas.shiftIsHeld = { false }

        let grip = KeyChordGrip(frame: NSRect(x: 0, y: 0, width: 40, height: 26))
        grip.name = try #require(sheet.inKey.dropFirst(1).first)
        let carried = try pasteboard(carrying: grip)
        defer { carried.releaseGlobally() }
        #expect(SheetCanvas.chordOfKey(on: carried) == grip.name)
        let dragged = Dragged(carried, over: window)
        let place = canvas.convert(try #require(canvas.point(over: Spot(row: line, at: 7))), to: nil)
        let bare = canvas.convert(NSPoint(x: canvas.bounds.maxX - 3, y: 200), to: nil)

        dragged.draggingLocation = bare
        #expect(canvas.draggingEntered(dragged) == [] && sheet.dragged == nil)
        dragged.draggingLocation = place
        #expect(canvas.draggingUpdated(dragged) == .copy && sheet.spot == Spot(row: line, at: 7))
        canvas.shiftIsHeld = { true }
        #expect(canvas.draggingUpdated(dragged) == .copy && sheet.spot == Spot(row: line, at: 5))
        canvas.shiftIsHeld = { false }
        dragged.draggingLocation = bare
        #expect(canvas.draggingUpdated(dragged) == [] && sheet.dragged == nil)
        // Let go off the places, should the system ask all the same: nothing.
        #expect(canvas.prepareForDragOperation(dragged) && !canvas.performDragOperation(dragged))
        dragged.draggingLocation = place
        #expect(canvas.draggingUpdated(dragged) == .copy)
        canvas.draggingExited(dragged)
        #expect(sheet.dragged == nil)
        #expect(canvas.draggingUpdated(dragged) == .copy)
        canvas.draggingEnded(dragged)
        #expect(sheet.dragged == nil)
        #expect(try mac.bytes(of: "Amazing Grace") == before)

        // Anything else that is dragged over the sheet is not taken: text from another
        // app, or this app's type with nothing under it.
        for other in [NSPasteboard.withUniqueName(), NSPasteboard.withUniqueName()] {
            defer { other.releaseGlobally() }
            other.clearContents()
            if other.name.rawValue.isEmpty { continue }
            let foreign = Dragged(other, over: window)
            foreign.draggingLocation = place
            other.setString("C", forType: .string)
            #expect(SheetCanvas.chordOfKey(on: other) == nil)
            #expect(canvas.draggingEntered(foreign) == [] && canvas.draggingUpdated(foreign) == [])
            #expect(!canvas.prepareForDragOperation(foreign) && !canvas.performDragOperation(foreign))
            other.clearContents()
            other.setString("", forType: .chordOfTheKey)
            #expect(SheetCanvas.chordOfKey(on: other) == nil && canvas.draggingEntered(foreign) == [])
        }
        // And a name that is no chord, under this app's own type, is put nowhere.
        let nonsense = NSPasteboard.withUniqueName()
        defer { nonsense.releaseGlobally() }
        nonsense.clearContents()
        nonsense.setString("Hello", forType: .chordOfTheKey)
        let odd = Dragged(nonsense, over: window)
        odd.draggingLocation = place
        #expect(canvas.draggingEntered(odd) == [] && !canvas.performDragOperation(odd))
        #expect(try mac.bytes(of: "Amazing Grace") == before && !model.canUndo)
    }

    @Test("Let go, as the system tells the sheet of it: the chord goes to the character under the pointer, or with Shift held to the start of its word")
    func letGoWithShift() async throws {
        let mac = try MadeUpMac()
        let model = await open("Amazing Grace", on: mac)
        let (canvas, window) = view(for: model)
        defer { window.close() }
        let sheet = try #require(model.sheet)
        let line = try row(Self.second, of: model)
        let grip = KeyChordGrip(frame: NSRect(x: 0, y: 0, width: 40, height: 26))
        grip.name = try #require(sheet.inKey.dropFirst(2).first)
        let carried = try pasteboard(carrying: grip)
        defer { carried.releaseGlobally() }
        let dragged = Dragged(carried, over: window)
        dragged.draggingLocation = canvas.convert(try #require(canvas.point(over: Spot(row: line, at: 7))), to: nil)
        canvas.shiftIsHeld = { false }
        #expect(canvas.performDragOperation(dragged))
        #expect(try chords(of: Self.second, in: mac, song: "Amazing Grace").filter { $0.name == grip.name }.map(\.at) == [7])
        canvas.shiftIsHeld = { true }
        dragged.draggingLocation = canvas.convert(try #require(canvas.point(over: Spot(row: line, at: 16))), to: nil)
        #expect(canvas.performDragOperation(dragged))
        #expect(try chords(of: Self.second, in: mac, song: "Amazing Grace").filter { $0.name == grip.name }.map(\.at) == [7, 13])
    }

    @Test("Held near the bottom of what is seen, the sheet scrolls down, and near the top, up: by the time that has passed, faster the nearer the edge, and never past its ends. Not upwards on the chord's way in from the strip")
    func scrolledAtItsEdges() async throws {
        let mac = try MadeUpMac()
        let model = await open("Amazing Grace", on: mac)
        let (canvas, window) = view(for: model, width: 520, height: 300)
        defer { window.close() }
        var clock = 100.0
        canvas.now = { clock }
        canvas.shiftIsHeld = { false }
        let room = canvas.bounds.height - canvas.visibleRect.height
        try #require(room > 400, "the song is not tall enough here to scroll: \(canvas.bounds.height)")
        canvas.scroll(NSPoint(x: 0, y: 200))
        #expect(canvas.visibleRect.minY == 200)

        let grip = KeyChordGrip(frame: NSRect(x: 0, y: 0, width: 40, height: 26))
        grip.name = try #require(model.sheet?.inKey.first)
        let carried = try pasteboard(carrying: grip)
        defer { carried.releaseGlobally() }
        let dragged = Dragged(carried, over: window)
        /// The system tells the sheet that the chord is so far down what is seen of it.
        func held(_ down: Double, entering: Bool = false) {
            let seen = canvas.visibleRect
            dragged.draggingLocation = canvas.convert(NSPoint(x: seen.midX, y: seen.minY + down), to: nil)
            _ = entering ? canvas.draggingEntered(dragged) : canvas.draggingUpdated(dragged)
        }

        // In from the strip, over the top: the sheet does not scroll up under it,
        // however long it is held there.
        held(4, entering: true)
        clock += 0.1
        held(4)
        clock += 0.1
        held(10)
        #expect(canvas.visibleRect.minY == 200)
        // In the middle: nothing.
        held(150)
        clock += 0.1
        held(150)
        #expect(canvas.visibleRect.minY == 200)

        // At the very bottom: the clock is started the first time, and then the sheet
        // goes its fastest for the time that has passed.
        held(300)
        #expect(canvas.visibleRect.minY == 200)
        clock += 0.1
        held(300)
        #expect(abs(canvas.visibleRect.minY - (200 + SheetCanvas.carriedFastest * 0.1)) < 1)
        // Half way into the edge: half as fast. And a long wait counts as a fifth of
        // a second, so that the sheet never jumps.
        let from = canvas.visibleRect.minY
        clock += 0.1
        held(300 - SheetCanvas.carriedNear / 2)
        #expect(abs(canvas.visibleRect.minY - (from + SheetCanvas.carriedFastest * 0.05)) < 1)
        let then = canvas.visibleRect.minY
        clock += 30
        held(300)
        #expect(abs(canvas.visibleRect.minY - min(room, then + SheetCanvas.carriedFastest * 0.2)) < 1)
        // Asked very often, each time too soon for the sheet to go a whole point, it
        // goes all the same as the time adds up: 40 askings a thousandth of a second
        // apart, barely into the edge, are 40 thousandths at a tenth of its fastest,
        // which is two points and a little owed.
        canvas.scroll(NSPoint(x: 0, y: 200))
        held(150)
        held(300 - SheetCanvas.carriedNear * 0.9)
        for _ in 0..<40 {
            clock += 0.001
            held(300 - SheetCanvas.carriedNear * 0.9)
        }
        #expect(abs(canvas.visibleRect.minY - (200 + SheetCanvas.carriedFastest * 0.1 * 0.04)) < 0.6, "\(canvas.visibleRect.minY)")

        // Never past the end.
        for _ in 0..<40 {
            clock += 0.2
            held(300)
        }
        #expect(abs(canvas.visibleRect.minY - room) < 1)

        // Now that it has been clear of the top, held at the top the sheet goes up;
        // and never past its start.
        held(150)
        held(0)
        clock += 0.1
        held(0)
        #expect(abs(canvas.visibleRect.minY - (room - SheetCanvas.carriedFastest * 0.1)) < 1)
        for _ in 0..<60 {
            clock += 0.2
            held(0)
        }
        #expect(canvas.visibleRect.minY == 0)
        // Leaving the edge stops the clock: back at it, the time away is not counted.
        held(150)
        clock += 0.1
        held(300)
        clock += 0.1
        #expect(canvas.visibleRect.minY == 0)

        // Taken off the sheet and brought over it again, it comes in over the top
        // afresh: the sheet does not scroll up under it this time either.
        canvas.draggingExited(dragged)
        canvas.scroll(NSPoint(x: 0, y: 200))
        held(4, entering: true)
        clock += 0.1
        held(4)
        clock += 0.1
        held(4)
        #expect(canvas.visibleRect.minY == 200)
    }

    // ---- The strip's end

    @Test("What lies over a button: a press let go on it is the button's click; pulled 6 away the chord is picked up, the once, and the system is handed its name under this app's own type, to be put somewhere in this app and nowhere else")
    func theGrip() throws {
        let grip = KeyChordGrip(frame: NSRect(x: 0, y: 0, width: 40, height: 26))
        grip.name = "F#m"
        var clicks = 0
        var lifts = 0
        var ends = 0
        var begun: [NSDraggingItem] = []
        grip.clicked = { clicks += 1 }
        grip.lifted = { lifts += 1 }
        grip.ended = { ends += 1 }
        grip.begin = { _, item, _ in begun.append(item) }

        // A click: the pointer may stray a little.
        grip.pressed(at: NSPoint(x: 10, y: 10))
        #expect(grip.pressedAt != nil)
        #expect(!grip.pulled(to: NSPoint(x: 13, y: 12)))
        grip.released(at: NSPoint(x: 13, y: 12))
        #expect(clicks == 1 && lifts == 0 && grip.pressedAt == nil)
        // Let go off the button, nothing is clicked; and nothing without a press.
        grip.pressed(at: NSPoint(x: 10, y: 10))
        grip.released(at: NSPoint(x: 10, y: 40))
        grip.released(at: NSPoint(x: 10, y: 10))
        #expect(!grip.pulled(to: NSPoint(x: 30, y: 30)))
        #expect(clicks == 1 && lifts == 0)

        // Pulled 6: picked up, the once. Let go then, it is no click.
        grip.pressed(at: NSPoint(x: 10, y: 10))
        #expect(!grip.pulled(to: NSPoint(x: 13, y: 12)))
        #expect(grip.pulled(to: NSPoint(x: 14, y: 12)))
        #expect(lifts == 1 && grip.carrying)
        #expect(!grip.pulled(to: NSPoint(x: 60, y: 90)))
        grip.released(at: NSPoint(x: 14, y: 12))
        #expect(clicks == 1 && lifts == 1 && grip.carrying)
        grip.carryingEnded()
        #expect(ends == 1 && !grip.carrying && grip.pressedAt == nil)
        // And the next press is a press like any other.
        grip.pressed(at: NSPoint(x: 10, y: 10))
        grip.released(at: NSPoint(x: 10, y: 10))
        #expect(clicks == 2)
        // So it is even if the system never said that a carrying had ended: a button
        // is not left dead.
        grip.pressed(at: NSPoint(x: 10, y: 10))
        #expect(grip.pulled(to: NSPoint(x: 10, y: 20)) && grip.carrying)
        grip.pressed(at: NSPoint(x: 10, y: 10))
        #expect(!grip.carrying)
        grip.released(at: NSPoint(x: 10, y: 10))
        #expect(clicks == 3)
        clicks = 2
        lifts = 1

        // What the system is handed.
        let item = grip.carried()
        let written = try #require(item.item as? NSPasteboardItem)
        #expect(written.types == [.chordOfTheKey] && written.string(forType: .chordOfTheKey) == "F#m")
        #expect(item.draggingFrame == grip.bounds)
        #expect(KeyChordGrip.mayBeDone(.withinApplication) == .copy)
        #expect(KeyChordGrip.mayBeDone(.outsideApplication) == [])

        // The same by the mouse's own events, in a window.
        let window = NSWindow(contentRect: NSRect(x: 0, y: 0, width: 200, height: 100), styleMask: [.borderless], backing: .buffered, defer: false)
        window.isReleasedWhenClosed = false
        defer { window.close() }
        window.contentView?.addSubview(grip)
        grip.frame = NSRect(x: 50, y: 30, width: 40, height: 26)
        grip.mouseDown(with: try mouse(.leftMouseDown, at: NSPoint(x: 60, y: 40), in: window))
        grip.mouseDragged(with: try mouse(.leftMouseDragged, at: NSPoint(x: 62, y: 41), in: window))
        #expect(begun.isEmpty)
        grip.mouseUp(with: try mouse(.leftMouseUp, at: NSPoint(x: 62, y: 41), in: window))
        #expect(clicks == 3 && begun.isEmpty)
        grip.mouseDown(with: try mouse(.leftMouseDown, at: NSPoint(x: 60, y: 40), in: window))
        grip.mouseDragged(with: try mouse(.leftMouseDragged, at: NSPoint(x: 60, y: 20), in: window))
        grip.mouseDragged(with: try mouse(.leftMouseDragged, at: NSPoint(x: 60, y: 0), in: window))
        #expect(begun.count == 1 && lifts == 2 && clicks == 3)
        #expect((begun.first?.item as? NSPasteboardItem)?.string(forType: .chordOfTheKey) == "F#m")
    }

    @Test("What lies over a button is seen through; but it shades the button while it is held down, and makes it faint while its chord is out being carried")
    func whatTheGripDraws() throws {
        let grip = KeyChordGrip(frame: NSRect(x: 0, y: 0, width: 40, height: 26))
        /// The middle of the grip, and its corner, as it draws itself: red, green, blue
        /// and how much is there, each out of 255.
        func drawn() throws -> (middle: [Int], corner: [Int]) {
            let picture = try #require(grip.bitmapImageRepForCachingDisplay(in: grip.bounds))
            // (A picture got so is not always clear to begin with.)
            picture.bitmapData.map { $0.update(repeating: 0, count: picture.bytesPerRow * picture.pixelsHigh) }
            grip.cacheDisplay(in: grip.bounds, to: picture)
            func at(_ x: Int, _ y: Int) throws -> [Int] {
                let colour = try #require(picture.colorAt(x: x, y: y)?.usingColorSpace(.sRGB))
                return [colour.redComponent, colour.greenComponent, colour.blueComponent, colour.alphaComponent].map { Int(($0 * 255).rounded()) }
            }
            return (try at(picture.pixelsWide / 2, picture.pixelsHigh / 2), try at(0, 0))
        }
        #expect(try drawn().middle[3] == 0)
        // Held down: a shade of black, and the button's rounded corners left alone.
        grip.pressed(at: NSPoint(x: 10, y: 10))
        var seen = try drawn()
        #expect(seen.middle[3] > 40 && seen.middle[3] < 70 && seen.corner[3] == 0, "\(seen)")
        // Its chord out being carried: most of the strip's own colour over it.
        #expect(grip.pulled(to: NSPoint(x: 10, y: 20)))
        seen = try drawn()
        #expect(seen.middle[3] > 140 && seen.middle[3] < 165 && seen.corner[3] == 0, "\(seen)")
        // And seen through again when it is over.
        grip.carryingEnded()
        #expect(try drawn().middle[3] == 0)
    }

    @Test("Each of the strip's seven buttons has one over it, of its own chord and its own size, that says the button's three ways; a click through it puts the chord on the spot; and its picture is the button's. So in a strip too narrow for them all, where they scroll")
    func overEachButton() async throws {
        let mac = try MadeUpMac()
        let model = await open("Amazing Grace", on: mac)
        let sheet = try #require(model.sheet)
        let line = try row(Self.second, of: model)
        for width in [900.0, 260.0] {
            let (grips, window) = try strip(for: model, width: width)
            defer { window.close() }
            #expect(grips.map(\.name) == sheet.inKey, "at \(width)")
            for (index, grip) in grips.enumerated() {
                #expect(grip.bounds.width >= 34 && grip.bounds.height >= 26, "\(grip.name) is \(grip.bounds.size)")
                #expect(grip.toolTip == "Puts \(grip.name) on the spot. The key \(index + 1) does the same. Or drag it onto a word.")
            }
        }
        let (grips, window) = try strip(for: model)
        defer { window.close() }
        // No two lie over each other, and they are in the order of the keys.
        let frames = grips.map { $0.convert($0.bounds, to: nil) }
        for (one, next) in zip(frames, frames.dropFirst()) {
            #expect(one.maxX <= next.minX)
        }

        let second = try #require(grips.dropFirst(1).first)
        sheet.setSpot(line, 5)
        second.pressed(at: NSPoint(x: second.bounds.midX, y: second.bounds.midY))
        second.released(at: NSPoint(x: second.bounds.midX, y: second.bounds.midY))
        #expect(try chords(of: Self.second, in: mac, song: "Amazing Grace").contains(Chord(at: 5, name: second.name)))

        // The picture that goes with the pointer is the button's own, at its size.
        let picture = try #require(second.picture())
        #expect(abs(picture.size.width - second.bounds.width) <= 1 && abs(picture.size.height - second.bounds.height) <= 1,
                "the picture is \(picture.size), the button \(second.bounds.size)")
        // (Read by drawing it into a picture of known colours: asked for its own
        // bytes it hands them over in another colour space, a shade lighter.)
        let image = try #require(picture.cgImage(forProposedRect: nil, context: nil, hints: nil))
        let colours = try #require(CGColorSpace(name: CGColorSpace.sRGB))
        let context = try #require(CGContext(data: nil, width: image.width, height: image.height, bitsPerComponent: 8, bytesPerRow: 0,
                                             space: colours, bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue))
        context.draw(image, in: CGRect(x: 0, y: 0, width: image.width, height: image.height))
        let bytes = try #require(context.data).assumingMemoryBound(to: UInt8.self)
        func pixel(_ x: Int, _ y: Int) -> [Int] {
            let at = y * context.bytesPerRow + x * 4
            return (0..<4).map { Int(bytes[at + $0]) }
        }
        // A button's own colour at its left edge and over its name, and at its corners,
        // which are rounded, nothing.
        #expect(pixel(3, image.height / 2) == [0x3a, 0x3c, 0x42, 255])
        #expect(pixel(image.width / 2, 2) == [0x3a, 0x3c, 0x42, 255])
        #expect(pixel(0, 0) == [0, 0, 0, 0] && pixel(image.width - 1, image.height - 1) == [0, 0, 0, 0])
        // And its name is written on it: some of it is as light as the words are.
        var light = 0
        for y in 0..<image.height {
            for x in 0..<image.width where pixel(x, y)[0] > 200 {
                light += 1
            }
        }
        #expect(light > 10, "nothing is written on the picture")
        if let folder = ProcessInfo.processInfo.environment["PRO7_PICTURES"], let made = context.makeImage(),
           let png = NSBitmapImageRep(cgImage: made).representation(using: .png, properties: [:]) {
            try png.write(to: URL(fileURLWithPath: folder, isDirectory: true).appendingPathComponent("36 the picture that goes with the pointer.png"))
        }
    }
}
