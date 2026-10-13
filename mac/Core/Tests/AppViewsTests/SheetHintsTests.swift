// The line of hints under the sheet (SheetHints.swift), the menu bar kept to what the
// app has a use for and the About window's icon (AppMenus.swift), and what the window
// needs of AppKit: to quit the app when it closes, and to answer Command+S though it
// is in no menu (WindowKeeper, in LibraryWindow.swift).
//
// What these can tell: what is said for each thing that can be going on in the sheet,
// that the sheet's view says what is going on as the pointer and the keys move, and
// that the menus' tidying hides what it is to hide and nothing else. What they cannot:
// how the line reads to someone using the sheet, and what the real menu bar of the
// real app has in it. Those are the maintainer's to see.

import AppKit
@testable import AppViews
import Song
import Testing

@MainActor
@Suite("The line of hints, the menus, and the window's close")
struct SheetHintsTests {
    private func open(_ name: String, on mac: MadeUpMac) async -> LibraryModel {
        let model = mac.model()
        model.pauseBeforeSaving = .milliseconds(1)
        await model.start()
        model.choose(library: mac.library("Songs"))
        await model.marksAreWorkedOut()
        model.open(song: mac.song(name))
        return model
    }

    /// The real view of the sheet, wired to the model as the window wires it, in a
    /// window that is never shown.
    private func view(for model: LibraryModel) -> (SheetCanvas, NSWindow) {
        let scroll = SheetScroll(frame: NSRect(x: 0, y: 0, width: 900, height: 600))
        let window = NSWindow(contentRect: NSRect(x: 0, y: 0, width: 900, height: 600), styleMask: [.borderless], backing: .buffered, defer: false)
        window.isReleasedWhenClosed = false
        window.contentView = scroll
        scroll.canvas.asks = .of(model)
        scroll.canvas.show(model.sheet, scale: model.sheetZoom)
        window.makeFirstResponder(scroll.canvas)
        return (scroll.canvas, window)
    }

    private func line(_ words: String, of sheet: ChordSheet) throws -> Int {
        try #require(sheet.rows.firstIndex { $0.kind == .line && $0.text.string == words }, "no line \(words)")
    }

    /// A line of words with no chord on it. (Amazing Grace has a chord on every
    /// line: those of "I once was lost but now am found" are taken off.)
    private func lineWithNoChord(in sheet: ChordSheet) throws -> Int {
        let row = try line("I once was lost but now am found", of: sheet)
        for chord in sheet.rows[row].chords {
            #expect(sheet.take(row, chord.at))
        }
        #expect(sheet.rows[row].chords.isEmpty)
        return row
    }

    typealias Hint = SheetHints.Hint

    // ---- What is said

    @Test("On a chord, the first thing said is that Delete takes it off; on a place with none, that 1 to 7 put one on")
    func whereTheSpotIs() {
        let onChord = SheetHints.hints(.onChord)
        #expect(onChord.first == Hint([SheetHints.delete], "takes this chord off"))
        #expect(onChord.map(\.says) == ["takes this chord off", "change it", "drag it to another word", "click to type over it"])
        let onPlace = SheetHints.hints(.onPlace)
        #expect(onPlace.first == Hint(["1 \u{2013} 7"], "put one of the key\u{2019}s chords here"))
        #expect(onPlace.count == 3 && onPlace[2].keys == [SheetHints.option])
        // Not the same line for the two, nor for either and nothing.
        #expect(onChord != onPlace && SheetHints.hints(.nothing) != onPlace && SheetHints.hints(.nothing) != onChord)
        // None of these is needed whatever the room: the last are left off first.
        #expect((onChord + onPlace).allSatisfy { !$0.needed && $0.lead.isEmpty })
    }

    @Test("While a chord is typed: what Return, Tab and Esc do with it; and with the box empty on a chord, that Return takes the chord off")
    func whileTyping() {
        let typing = SheetHints.hints(.typing(onChord: false, empty: false))
        #expect(typing.prefix(2) == [Hint([SheetHints.enter], "keeps it"), Hint([SheetHints.tab], "keeps it and goes on to the next word")])
        #expect(typing.contains(Hint([SheetHints.escape], "throws it away")))
        // The same whatever is in the box, but for the one case: nothing in it, on a
        // chord.
        #expect(SheetHints.hints(.typing(onChord: true, empty: false)) == typing)
        #expect(SheetHints.hints(.typing(onChord: false, empty: true)) == typing)
        let cleared = SheetHints.hints(.typing(onChord: true, empty: true))
        #expect(cleared.prefix(2) == [Hint([SheetHints.enter], "takes this chord off"), Hint([SheetHints.escape], "leaves it as it was")])
        // What is held makes no difference while a chord is typed: Command+V there is
        // the box's own.
        #expect(SheetHints.hints(.typing(onChord: true, empty: true), held: true) == cleared)
    }

    @Test("While a chord is carried: that letting go moves it and Option leaves a copy; with Option held, that it will be a copy")
    func whileCarrying() {
        #expect(SheetHints.hints(.carrying) == [Hint("Let go on a word to move it there"), Hint([SheetHints.option], "held as you let go, leaves a copy behind")])
        #expect(SheetHints.hints(.carrying, held: true) == [Hint("Let go on a word to put a copy there")])
    }

    @Test("With Option or Command held, at a line: how its chords are copied and pasted, in the maintainer's words, the keys by their signs; and that a paste replaces, where the line it would go onto has chords")
    func copying() {
        typealias Paste = SheetHints.Paste
        for situation in [SheetHints.Situation.onChord, .onPlace] {
            let held = SheetHints.hints(situation, held: true)
            #expect(held == [Hint(lead: "Press", ["\u{2318}C"], "to copy all chords on the highlighted line.", needed: true),
                             Hint(lead: "Press", ["\u{2318}V"], "to paste previously copied chords to the highlighted line", needed: true)])
            // As it is read: the two sentences that were asked for.
            #expect(held.map(\.read).joined(separator: " ")
                == "Press \u{2318}C to copy all chords on the highlighted line. Press \u{2318}V to paste previously copied chords to the highlighted line")
            // The line it would go onto has chords: it says they will be replaced.
            for paste in [Paste(lines: nil, replaces: true), Paste(lines: 1, replaces: true)] {
                let replacing = SheetHints.hints(situation, held: true, paste: paste)
                #expect(replacing[0] == held[0])
                #expect(replacing[1].says == "to paste previously copied chords to the highlighted line (Replaces existing chords)")
            }
            #expect(SheetHints.hints(situation, held: true, paste: Paste(lines: 1, replaces: false)) == held)
            // A whole verse was copied: it goes onto as many lines as it has.
            #expect(SheetHints.hints(situation, held: true, paste: Paste(lines: 4, replaces: false))[1].says
                == "to paste previously copied chords to the highlighted line and the lines after it")
            #expect(SheetHints.hints(situation, held: true, paste: Paste(lines: 4, replaces: true))[1].says
                == "to paste previously copied chords to the highlighted line and the lines after it (Replaces existing chords)")
            // Not held, what a paste would do changes nothing that is said.
            #expect(SheetHints.hints(situation, paste: Paste(lines: 4, replaces: true)) == SheetHints.hints(situation))
        }
    }

    @Test("With nobody at the sheet there is no highlighted line, and one thing is said, whatever is held: where to point to be told. No tip comes and goes by the clock")
    func nobodyAtTheSheet() {
        #expect(SheetHints.idle == [Hint("Point at a word or a chord, and this line says what the keys do there.")])
        #expect(SheetHints.hints(.nothing) == SheetHints.idle)
        #expect(SheetHints.hints(.nothing, held: true) == SheetHints.idle)
        #expect(SheetHints.hints(.nothing, held: true, paste: SheetHints.Paste(lines: 2, replaces: true)) == SheetHints.idle)
    }

    // ---- What is going on, read off the sheet

    @Test("What is going on is read off the sheet: on a chord, on a place, typing (and whether on a chord, and whether the box is empty), carrying; and nothing, with nobody at the sheet")
    func readOffTheSheet() async throws {
        let mac = try MadeUpMac()
        let model = await open("Amazing Grace", on: mac)
        let sheet = try #require(model.sheet)
        let row = try line("Amazing grace how sweet the sound", of: sheet)
        let chord = try #require(sheet.rows[row].chords.first)
        let bare = try #require(sheet.stops(row).first { sheet.chordAt(row, $0).isEmpty })

        sheet.setSpot(row, chord.at)
        #expect(SheetHints.situation(of: sheet, attending: true) == .onChord)
        #expect(SheetHints.situation(of: sheet, attending: false) == .nothing)
        sheet.setSpot(row, bare)
        #expect(SheetHints.situation(of: sheet, attending: true) == .onPlace)

        sheet.pick(row, chord.at)
        #expect(SheetHints.situation(of: sheet, attending: false) == .typing(onChord: true, empty: false))
        sheet.type("")
        #expect(SheetHints.situation(of: sheet, attending: false) == .typing(onChord: true, empty: true))
        #expect(sheet.press(.escape))
        sheet.pick(row, bare)
        #expect(SheetHints.situation(of: sheet, attending: true) == .typing(onChord: false, empty: true))
        #expect(sheet.press(.escape))

        sheet.lift(row, chord.at)
        #expect(SheetHints.situation(of: sheet, attending: false) == .carrying)
        sheet.dropNowhere()
        #expect(SheetHints.situation(of: sheet, attending: true) != .carrying)

        // What a paste would do. Nothing copied yet: it would replace where the
        // spot's line has chords, which is all there is to go by.
        let none = try line("That saved a wretch like me", of: sheet)
        let noChords = try lineWithNoChord(in: sheet)
        sheet.setSpot(row, 0)
        #expect(SheetHints.paste(in: sheet) == SheetHints.Paste(lines: nil, replaces: true))
        sheet.setSpot(noChords, 0)
        #expect(SheetHints.paste(in: sheet) == SheetHints.Paste(lines: nil, replaces: false))
        // One line copied: as the sheet itself says a paste would do.
        sheet.setSpot(row, 0)
        #expect(sheet.copy(whole: false) != nil)
        #expect(SheetHints.paste(in: sheet) == SheetHints.Paste(lines: 1, replaces: true))
        sheet.setSpot(noChords, 0)
        #expect(SheetHints.paste(in: sheet) == SheetHints.Paste(lines: 1, replaces: false))
        // A whole verse: as many lines as it has.
        sheet.setSpot(none, 0)
        #expect(sheet.copy(whole: true) != nil)
        #expect(SheetHints.paste(in: sheet).lines == sheet.copied?.count && (sheet.copied?.count ?? 0) > 1)
    }

    @Test("The highlighted line is the line the person is at: the spot's, while they are at the sheet or carry a chord over it; the line a chord is being typed on, wherever the pointer has gone since; and none, with nobody at the sheet")
    func theHighlightedLine() async throws {
        let mac = try MadeUpMac()
        let model = await open("Amazing Grace", on: mac)
        let sheet = try #require(model.sheet)
        let row = try line("Amazing grace how sweet the sound", of: sheet)
        let other = try line("That saved a wretch like me", of: sheet)
        let chord = try #require(sheet.rows[row].chords.first)

        // No spot yet.
        #expect(SheetHints.line(of: sheet, attending: true) == nil)
        sheet.setSpot(row, 0)
        #expect(SheetHints.line(of: sheet, attending: true) == row)
        #expect(SheetHints.line(of: sheet, attending: false) == nil)
        sheet.setSpot(other, 5)
        #expect(SheetHints.line(of: sheet, attending: true) == other)

        // Typing on one line, the pointer gone on to another: the line of the typing.
        sheet.pick(row, chord.at)
        sheet.pointerOver(other, 0)
        #expect(sheet.spot.row == other)
        #expect(SheetHints.line(of: sheet, attending: true) == row)
        #expect(SheetHints.line(of: sheet, attending: false) == row)
        #expect(sheet.press(.escape))

        // Carrying: where it would land.
        sheet.lift(row, chord.at)
        sheet.carry(over: other, 5)
        #expect(SheetHints.line(of: sheet, attending: false) == other)
        sheet.dropNowhere()
        #expect(SheetHints.line(of: sheet, attending: false) == nil)
    }

    @Test("The sheet's view says what is going on as the pointer moves over a chord, over a place, off the slides and out of the sheet; as a key moves the spot; and as Option and Command go down and come up")
    func theViewSaysIt() async throws {
        let mac = try MadeUpMac()
        let model = await open("Amazing Grace", on: mac)
        let (canvas, window) = view(for: model)
        defer { window.close() }
        let sheet = try #require(model.sheet)
        let row = try line("Amazing grace how sweet the sound", of: sheet)
        let chord = try #require(sheet.rows[row].chords.first)
        let bare = try #require(sheet.stops(row).first { sheet.chordAt(row, $0).isEmpty })
        #expect(model.sheetSituation == .nothing)
        // (Nobody is at the sheet yet: where to point, and no line is highlighted.)
        #expect(model.sheetHints == SheetHints.idle && canvas.highlighted == nil)

        canvas.pointerMoved(to: try #require(canvas.point(over: Spot(row: row, at: chord.at), onItsChord: true)), shift: false)
        #expect(model.sheetSituation == .onChord && canvas.attending)
        #expect(model.sheetHints.first == Hint([SheetHints.delete], "takes this chord off"))
        #expect(canvas.highlighted == row)
        canvas.pointerMoved(to: try #require(canvas.point(over: Spot(row: row, at: bare))), shift: false)
        #expect(model.sheetSituation == .onPlace && canvas.highlighted == row)
        // Off the slides, on the bare sheet: the spot is where it was left, and
        // nothing is said of it, nor is its line highlighted.
        canvas.pointerMoved(to: NSPoint(x: 2, y: 2), shift: false)
        #expect(model.sheetSituation == .nothing && !canvas.attending && canvas.highlighted == nil)
        // A key that moves the spot is someone at the sheet again.
        canvas.key(.right)
        #expect(model.sheetSituation != .nothing && canvas.attending && canvas.highlighted == row)
        canvas.pointerLeft()
        #expect(model.sheetSituation == .nothing && canvas.highlighted == nil)
        // With nobody at the sheet, a key held says nothing of copying: there is no
        // highlighted line for it to be about.
        canvas.modifiers([.command])
        #expect(model.modifierHeld && model.sheetHints == SheetHints.idle)
        canvas.modifiers([])

        // Option, and Command, held and let go, at a line that has chords.
        canvas.pointerMoved(to: try #require(canvas.point(over: Spot(row: row, at: bare))), shift: false)
        canvas.modifiers([.option])
        #expect(model.modifierHeld && model.sheetHints.map(\.keys) == [["\u{2318}C"], ["\u{2318}V"]])
        #expect(model.sheetHints.map(\.read).joined(separator: " ")
            == "Press \u{2318}C to copy all chords on the highlighted line. Press \u{2318}V to paste previously copied chords to the highlighted line (Replaces existing chords)")
        canvas.modifiers([])
        #expect(!model.modifierHeld && model.sheetHints.first?.keys == ["1 \u{2013} 7"])
        canvas.modifiers([.command, .shift])
        #expect(model.modifierHeld)
        canvas.modifiers([.shift])
        #expect(!model.modifierHeld)

        // At a line that has no chord, a paste replaces nothing, and the hint does
        // not say it would: before anything is copied, and after.
        let noChords = try lineWithNoChord(in: sheet)
        canvas.sync()
        let onIt = try #require(canvas.point(over: Spot(row: noChords, at: 0)))
        let onTheFirst = try #require(canvas.point(over: Spot(row: row, at: bare)))
        canvas.modifiers([.command])
        canvas.pointerMoved(to: onIt, shift: false)
        #expect(canvas.highlighted == noChords)
        #expect(model.sheetHints.last?.says == "to paste previously copied chords to the highlighted line")
        canvas.pointerMoved(to: onTheFirst, shift: false)
        canvas.key(.copy(wholeGroup: false))
        #expect(model.sheetPaste == SheetHints.Paste(lines: 1, replaces: true))
        #expect(model.sheetHints.last?.says == "to paste previously copied chords to the highlighted line (Replaces existing chords)")
        // (The pointer moves from one line to another with nothing else changing:
        // what is said follows it.)
        canvas.pointerMoved(to: onIt, shift: false)
        #expect(model.sheetPaste == SheetHints.Paste(lines: 1, replaces: false))
        #expect(model.sheetHints.last?.says == "to paste previously copied chords to the highlighted line")
        // Pasted there, the line has chords, and the next paste would replace them.
        canvas.key(.paste)
        #expect(!sheet.rows[noChords].chords.isEmpty)
        #expect(model.sheetHints.last?.says == "to paste previously copied chords to the highlighted line (Replaces existing chords)")
        // A whole verse copied: the hint says it goes onto the lines after as well.
        canvas.key(.copy(wholeGroup: true))
        #expect(model.sheetHints.last?.says.hasPrefix("to paste previously copied chords to the highlighted line and the lines after it") == true)
        canvas.modifiers([])
        canvas.key(.undo)
        canvas.pointerMoved(to: onTheFirst, shift: false)

        // Typing: a click on the chord opens it; cleared, the line says Return takes
        // it off, and Return does (rule 35a).
        let on = try #require(canvas.point(over: Spot(row: row, at: chord.at), onItsChord: true))
        canvas.pressed(at: on, shift: false)
        canvas.released(at: on, shift: false, copy: false)
        #expect(model.sheetSituation == .typing(onChord: true, empty: false))
        sheet.type("")
        canvas.sync()
        #expect(model.sheetSituation == .typing(onChord: true, empty: true))
        #expect(model.sheetHints.first == Hint([SheetHints.enter], "takes this chord off"))
        // (The line of the typing is the highlighted one.)
        #expect(canvas.highlighted == row)
        #expect(sheet.press(.enter))
        canvas.sync()
        #expect(sheet.chordAt(row, chord.at).isEmpty && !sheet.typing)

        // The app is left with a key held: it is taken as let go.
        model.holds(true)
        model.holds(false)
        #expect(!model.modifierHeld)
    }

    @Test("While ProPresenter is running the line says that nothing can be changed, whatever is under the pointer")
    func whileProPresenterRuns() async throws {
        let mac = try MadeUpMac()
        let model = await open("Amazing Grace", on: mac)
        model.sheetIs(.onChord)
        #expect(model.sheetHints == SheetHints.hints(.onChord))
        mac.proPresenterIsRunning = true
        model.proPresenterMayHaveChanged()
        #expect(model.sheetHints == SheetHints.cannotChange)
        model.holds(true)
        #expect(model.sheetHints == SheetHints.cannotChange)
    }

    // ---- The menus

    private func item(_ title: String, _ action: String?) -> NSMenuItem {
        NSMenuItem(title: title, action: action.map(NSSelectorFromString), keyEquivalent: "")
    }

    private func shown(_ menu: NSMenu) -> [String] {
        menu.items.filter { !$0.isHidden }.map { $0.isSeparatorItem ? "-" : $0.title }
    }

    @Test("Edit is kept to Undo, Redo, Cut, Copy, Paste and Select All: Delete and what macOS adds are hidden, and no line is left with nothing beside it")
    func editIsTidied() {
        let edit = NSMenu(title: "Edit")
        let tools = item("Writing Tools", nil)
        tools.submenu = NSMenu(title: "Writing Tools")
        tools.submenu?.addItem(item("Proofread", "anythingAtAll:"))
        let fill = item("AutoFill", nil)
        fill.submenu = NSMenu(title: "AutoFill")
        for made in [item("Undo", "undo:"), item("Redo", "redo:"), .separator(), item("Cut", "cut:"), item("Copy", "copy:"), item("Paste", "paste:"),
                     item("Paste and Match Style", "pasteAsPlainText:"), item("Delete", "delete:"), item("Select All", "selectAll:"),
                     .separator(), tools, fill, .separator(), item("Start Dictation\u{2026}", "startDictation:"),
                     item("Emoji & Symbols", "orderFrontCharacterPalette:")] {
            edit.addItem(made)
        }
        #expect(AppMenus.isEdit(edit))
        AppMenus.tidy(edit: edit)
        #expect(shown(edit) == ["Undo", "Redo", "-", "Cut", "Copy", "Paste", "Select All"])
        // Nothing was taken out: what is not wanted is hidden where it was.
        #expect(edit.items.count == 15)
        // Done again, it is the same; and what was put back is hidden again.
        edit.items[7].isHidden = false
        edit.addItem(item("Something new of the system's", "somethingNew:"))
        AppMenus.tidy(edit: edit)
        #expect(shown(edit) == ["Undo", "Redo", "-", "Cut", "Copy", "Paste", "Select All"])

        // In the menu bar, only Edit is touched.
        let bar = NSMenu(title: "Main")
        let file = NSMenu(title: "File")
        file.addItem(item("Close", "performClose:"))
        file.addItem(.separator())
        file.addItem(item("Delete", "delete:"))
        #expect(!AppMenus.isEdit(file))
        for (title, menu) in [("File", file), ("Edit", edit)] {
            let top = NSMenuItem(title: title, action: nil, keyEquivalent: "")
            top.submenu = menu
            bar.addItem(top)
        }
        edit.items[7].isHidden = false
        AppMenus.tidy(bar)
        #expect(shown(file) == ["Close", "-", "Delete"])
        #expect(shown(edit) == ["Undo", "Redo", "-", "Cut", "Copy", "Paste", "Select All"])
        AppMenus.tidy(nil)
        // Written out, it is what can be seen of each menu, and nothing that is hidden.
        #expect(AppMenus.described(bar) == "File: Close, -, Delete\nEdit: Undo, Redo, -, Cut, Copy, Paste, Select All")
        #expect(AppMenus.described(nil).isEmpty)

        // A menu with nothing in it to choose is not shown; given something, it is.
        let view = NSMenu(title: "View")
        view.addItem(.separator())
        let top = NSMenuItem(title: "View", action: nil, keyEquivalent: "")
        top.submenu = view
        bar.addItem(top)
        AppMenus.tidy(bar)
        #expect(top.isHidden && !bar.items[0].isHidden && !bar.items[1].isHidden)
        #expect(AppMenus.described(bar) == "File: Close, -, Delete\nEdit: Undo, Redo, -, Cut, Copy, Paste, Select All")
        let full = item("Enter Full Screen", "toggleFullScreen:")
        full.keyEquivalent = "f"
        full.keyEquivalentModifierMask = [.control, .command]
        view.addItem(full)
        AppMenus.tidy(bar)
        #expect(!top.isHidden)
        // (Only Edit's lines are tidied: the line this test put at the top of View stays.)
        #expect(AppMenus.described(bar).hasSuffix("\nView: -, Enter Full Screen [ctrl+cmd+f]"))
    }

    @Test("A line at the top of Edit, two lines together, and a line at its foot are all hidden; one between two things is kept")
    func linesInEdit() {
        let edit = NSMenu(title: "Edit")
        for made in [.separator(), item("Undo", "undo:"), .separator(), .separator(), item("Paste", "paste:"), .separator(), item("Delete", "delete:"), .separator()] {
            edit.addItem(made)
        }
        AppMenus.tidy(edit: edit)
        #expect(shown(edit) == ["Undo", "-", "Paste"])
    }

    @Test("The About window is given the icon macOS has for the app's own file")
    func aboutIcon() {
        // (Of this test's own program here: any file has an icon, and it has sizes.)
        let icon = AppMenus.icon(ofAppAt: Bundle.main.bundlePath)
        #expect(icon.isValid && !icon.representations.isEmpty)
    }

    // ---- The window's keeper

    private func key(_ characters: String, _ flags: NSEvent.ModifierFlags, type: NSEvent.EventType = .keyDown) throws -> NSEvent {
        try #require(NSEvent.keyEvent(with: type, location: .zero, modifierFlags: flags, timestamp: 0, windowNumber: 0, context: nil,
                                      characters: characters, charactersIgnoringModifiers: characters, isARepeat: false, keyCode: 0))
    }

    @Test("Command+S is answered by the window, though it is in no menu; Command with anything else, or S with anything more, is not")
    func commandS() throws {
        let keeper = WindowKeeper.Keeper()
        var saved = 0
        keeper.save = { saved += 1 }
        #expect(keeper.performKeyEquivalent(with: try key("s", [.command])))
        #expect(keeper.performKeyEquivalent(with: try key("S", [.command])))
        #expect(saved == 2)
        for (characters, flags) in [("s", NSEvent.ModifierFlags([.command, .shift])), ("s", [.command, .option]), ("s", []), ("a", [.command]), ("s", [.control])] {
            #expect(!keeper.performKeyEquivalent(with: try key(characters, flags)), "\(characters) with \(flags.rawValue)")
        }
        #expect(!WindowKeeper.Keeper.isSave(try key("s", [.command], type: .keyUp)))
        #expect(saved == 2)
    }

    @Test("When the window closes, the window's keeper says so, once; and Command+S there says that nothing is saved by hand")
    func theWindowCloses() async throws {
        let mac = try MadeUpMac()
        let model = await open("Amazing Grace", on: mac)
        var closed = 0
        let keeper = WindowKeeper.Keeper()
        keeper.closed = { closed += 1 }
        keeper.save = { model.saveNow() }
        let window = NSWindow(contentRect: NSRect(x: 0, y: 0, width: 300, height: 200), styleMask: [.titled, .closable], backing: .buffered, defer: false)
        window.isReleasedWhenClosed = false
        window.contentView?.addSubview(keeper)
        #expect(closed == 0)
        #expect(keeper.performKeyEquivalent(with: try key("s", [.command])))
        #expect(model.notice == "Saved. Every change here is saved as it is made: there is nothing to save by hand.")
        // Another window closing is nothing to this one.
        let other = NSWindow(contentRect: NSRect(x: 0, y: 0, width: 100, height: 100), styleMask: [.titled], backing: .buffered, defer: false)
        other.isReleasedWhenClosed = false
        other.close()
        #expect(closed == 0)
        window.close()
        #expect(closed == 1)
        // Taken out of its window, it watches that window no longer.
        keeper.removeFromSuperview()
        window.close()
        #expect(closed == 1)
    }

    @Test("The window says what the top left part is, over the choice of place")
    func thePlacesTitle() {
        #expect(ListsLook.placesTitle == "WORKSPACE OR PROPRESENTER FOLDER")
        // (Room for it: the libraries' part is never left shorter than it was plus a
        // line of small letters.)
        #expect(ListsLook.leastLibraries >= 188)
    }
}
