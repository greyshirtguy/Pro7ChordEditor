// The window, drawn into picture files to be looked at. spec/10-platforms.md, "Seeing
// what you have drawn".
//
// An agent cannot look at a window. It can look at a picture file. Each test here shows
// the window in one state on a made-up Mac, draws it off the screen at a fixed size, and
// writes a PNG. What the tests can tell by themselves is little (that a picture was
// drawn, and is not one flat colour); the point is that whoever changes how something is
// drawn runs these, OPENS THE PICTURES, and says what was seen.
//
// The pictures go to the folder named by PRO7_PICTURES if that is set, and otherwise to
// "Pro7ChordEditorPictures" in the system's temporary folder. They do not go in the
// repository. The songs in them are the test files: hymns long out of copyright, and
// made-up words.
//
// What a picture cannot tell (does the list keep up with the typing, does a click land)
// only a person can.

import AppKit
@testable import AppViews
import Song
import SwiftUI
import Testing
import TestData

@MainActor
@Suite("Pictures of the window")
struct PictureTests {
    static let folder: URL = {
        let named = ProcessInfo.processInfo.environment["PRO7_PICTURES"].map { URL(fileURLWithPath: $0, isDirectory: true) }
        let folder = named ?? FileManager.default.temporaryDirectory.appendingPathComponent("Pro7ChordEditorPictures", isDirectory: true)
        try? FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
        return folder
    }()

    /// Draws a view off the screen and writes it as a PNG. Says where. `first` is
    /// done to the view once it is laid out, before it is drawn.
    @discardableResult
    private func draw(_ view: some View, named name: String, size: NSSize = NSSize(width: 1000, height: 560), dark: Bool = false,
                      first: @MainActor (NSView) -> Void = { _ in }) throws -> URL {
        let appearance = NSAppearance(named: dark ? .darkAqua : .aqua)
        let host = NSHostingView(rootView: view.frame(width: size.width, height: size.height).environment(\.colorScheme, dark ? .dark : .light))
        let window = NSWindow(contentRect: NSRect(origin: .zero, size: size), styleMask: [.borderless], backing: .buffered, defer: false)
        window.isReleasedWhenClosed = false
        // (Done with, the window goes, and what is in it with it. One that stayed
        // would go on being kept up to date with its model; and two sheets on one
        // model, the pointer over one of them and not the other, each tell the model
        // what is going on, and never agree.)
        defer {
            window.contentView = nil
            window.close()
        }
        window.appearance = appearance
        window.contentView = host
        host.frame = NSRect(origin: .zero, size: size)
        host.layoutSubtreeIfNeeded()
        // (The lists fill themselves in on the next turns of the run loop.)
        RunLoop.main.run(until: Date(timeIntervalSinceNow: 0.4))
        host.layoutSubtreeIfNeeded()
        first(host)
        let picture = try #require(host.bitmapImageRepForCachingDisplay(in: host.bounds))
        host.cacheDisplay(in: host.bounds, to: picture)
        let png = try #require(picture.representation(using: .png, properties: [:]))
        let file = Self.folder.appendingPathComponent(name + ".png")
        try png.write(to: file)

        // Not nothing, and not one flat colour: a few points across it differ.
        var seen: Set<String> = []
        for x in stride(from: 10, to: picture.pixelsWide, by: max(1, picture.pixelsWide / 40)) {
            for y in stride(from: 10, to: picture.pixelsHigh, by: max(1, picture.pixelsHigh / 20)) {
                if let colour = picture.colorAt(x: x, y: y) { seen.insert("\(colour.redComponent),\(colour.greenComponent),\(colour.blueComponent)") }
            }
        }
        #expect(seen.count > 3, "\(name) is one flat colour")
        print("PICTURE \(file.path)")
        return file
    }

    private func modelWithASongOpen(_ mac: MadeUpMac, song: String = "Amazing Grace", editor: LibraryModel.Editor = .chordPro) async -> LibraryModel {
        let model = mac.model()
        await model.start()
        model.choose(library: mac.library("Songs"))
        await model.marksAreWorkedOut()
        model.open(song: mac.song(song))
        model.show(editor: editor)
        return model
    }

    @Test("A song open, as ChordPro text")
    func aSongOpen() async throws {
        let mac = try MadeUpMac()
        try draw(LibraryWindow(model: await modelWithASongOpen(mac)), named: "1 a song open")
    }

    @Test("The same in the dark, with ProPresenter running")
    func inTheDarkWithProPresenterRunning() async throws {
        let mac = try MadeUpMac()
        mac.proPresenterIsRunning = true
        try draw(LibraryWindow(model: await modelWithASongOpen(mac, song: "Plain Song")), named: "2 dark, ProPresenter running", dark: true)
    }

    @Test("A song ProPresenter wrote: no key, groups with their colours")
    func aFileProPresenterWrote() async throws {
        let mac = try MadeUpMac()
        try draw(LibraryWindow(model: await modelWithASongOpen(mac, song: "Rock Of Ages (Toplady)")), named: "3 a song ProPresenter wrote")
    }

    @Test("The list narrowed by search, and a file that cannot be read")
    func narrowedAndUnreadable() async throws {
        let mac = try MadeUpMac()
        let model = await modelWithASongOpen(mac, song: "Nonsense")
        model.typed = "n"
        try draw(LibraryWindow(model: model), named: "4 searched, and a file that cannot be read")
    }

    @Test("The old editor's copies, and only those with chords")
    func theOldEditorsCopies() async throws {
        let mac = try MadeUpMac()
        let model = mac.model()
        await model.start()
        model.choose(library: mac.library("Old copies"))
        await model.marksAreWorkedOut()
        model.filter = .withChords
        try draw(LibraryWindow(model: model), named: "5 the old editor's copies")
    }

    @Test("No ProPresenter library found")
    func nothingFound() async throws {
        let mac = try MadeUpMac()
        mac.hasAWorkspace = false
        let model = mac.model()
        await model.start()
        try draw(LibraryWindow(model: model), named: "6 nothing found")
    }

    // ---- Earlier versions (spec/07-backups.md)

    /// A made-up Mac on which Plain Song was changed nine days ago and two days ago.
    private func macWithHistory() throws -> MadeUpMac {
        let mac = try MadeUpMac()
        try mac.change("Plain Song", daysAgo: 9) { try $0.setKey("E") }
        try mac.change("Plain Song", daysAgo: 2) { song in
            let box = song.textBoxes[0]
            try song.setChords(slide: box.slide, element: box.element, [])
        }
        try mac.change("Holy Holy Holy", daysAgo: 30) { try $0.setKey("D") }
        return mac
    }

    @Test("Earlier versions of a song, the first picked")
    func earlierVersions() async throws {
        let mac = try macWithHistory()
        let model = await modelWithASongOpen(mac, song: "Plain Song")
        model.showEarlierVersions()
        model.pick(copy: model.earlier?.copies.last?.id)
        try draw(EarlierVersionsPanel(model: model), named: "7 earlier versions", size: NSSize(width: 860, height: 520))
    }

    @Test("A song put back as it was: the mark, the button, and the offer to undo")
    func justPutBack() async throws {
        let mac = try macWithHistory()
        let model = await modelWithASongOpen(mac, song: "Plain Song")
        model.showEarlierVersions()
        model.pick(copy: model.earlier?.copies.last?.id)
        model.putBackPicked()
        try draw(LibraryWindow(model: model), named: "8 just put back")
    }

    @Test("All Copies, in the settings")
    func allCopies() async throws {
        let mac = try macWithHistory()
        try FileManager.default.removeItem(at: mac.place.appendingPathComponent("Libraries/Songs/Holy Holy Holy.pro"))
        let model = AllCopiesModel(copies: mac.appData.copies())
        model.read()
        model.pick(song: "Sunday/Songs/Plain Song")
        try draw(AllCopiesView(model: model), named: "9 all copies", size: NSSize(width: 940, height: 520))
    }

    @Test("A chord being typed: its brackets empty still on one line, and one typed on another")
    func aChordBeingTyped() async throws {
        let mac = try MadeUpMac()
        let model = await modelWithASongOpen(mac)
        let text = try #require(model.chordPro?.text)
        #expect(model.textMayBecome(text.replacingOccurrences(of: "That saved a wretch", with: "That [Em7]saved a []wretch")))
        try draw(LibraryWindow(model: model), named: "10 a chord being typed")
        model.settle()
    }

    @Test("A song whose words have brackets of their own: shown, and why it cannot be typed in")
    func wordsWithBrackets() async throws {
        let mac = try MadeUpMac()
        let url = mac.place.appendingPathComponent("Libraries/Songs/Plain Song.pro")
        var bytes = try Data(contentsOf: url)
        let word = try #require(bytes.range(of: Data("Thirteen".utf8)))
        bytes.replaceSubrange(word, with: Data("Thir[een".utf8))
        try bytes.write(to: url)
        try draw(LibraryWindow(model: await modelWithASongOpen(mac, song: "Plain Song")), named: "11 words with brackets")
    }

    @Test("A save refused because the file was changed by something else, and the offer to open it again")
    func refused() async throws {
        let mac = try MadeUpMac()
        let model = await modelWithASongOpen(mac, song: "Plain Song")
        let text = try #require(model.chordPro?.text)
        #expect(model.textMayBecome(text.replacingOccurrences(of: "One two", with: "One [A]two")))
        try mac.change("Plain Song", daysAgo: 0) { try $0.setKey("E") }
        model.settle()
        #expect(model.offersOpeningAgain)
        try draw(LibraryWindow(model: model), named: "12 a save refused")
    }

    @Test("A song with a blank slide and a line of chords alone, in the dark")
    func blankSlidesInTheDark() async throws {
        let mac = try MadeUpMac()
        try draw(LibraryWindow(model: await modelWithASongOpen(mac, song: "Blank Slides")), named: "13 blank slides, dark", dark: true)
    }

    // ---- The chord sheet (spec/03-chord-sheet.md; to be set beside spec/pictures)

    private static let sheetSize = NSSize(width: 1200, height: 760)

    /// The row of the sheet that has these words.
    private func row(_ words: String, of sheet: ChordSheet) throws -> Int {
        try #require(sheet.rows.firstIndex { $0.kind == .line && $0.text.string == words }, "no line \(words)")
    }

    @Test("The chord sheet of Amazing Grace, as opened")
    func theSheet() async throws {
        let mac = try MadeUpMac()
        try draw(LibraryWindow(model: await modelWithASongOpen(mac, editor: .sheet)), named: "14 the chord sheet", size: Self.sheetSize)
    }

    @Test("The sheet with the spot on a word, and on a chord")
    func theSpot() async throws {
        let mac = try MadeUpMac()
        let model = await modelWithASongOpen(mac, editor: .sheet)
        let sheet = try #require(model.sheet)
        sheet.setSpot(try row("That saved a wretch like me", of: sheet), 5)
        sheet.picked = 1
        try draw(LibraryWindow(model: model), named: "15 the spot on a word", size: Self.sheetSize)
        let chorded = try #require(sheet.rows.firstIndex { $0.kind == .line && !$0.alone && $0.chords.count > 1 })
        sheet.setSpot(chorded, sheet.rows[chorded].chords[1].at)
        try draw(LibraryWindow(model: model), named: "16 the spot on a chord", size: Self.sheetSize)
    }

    @Test("The bubble a chord is typed in, with D/ typed")
    func typingAChord() async throws {
        let mac = try MadeUpMac()
        let model = await modelWithASongOpen(mac, editor: .sheet)
        let sheet = try #require(model.sheet)
        sheet.setSpot(try row("Amazing grace how sweet the sound", of: sheet), 8)
        sheet.startTyping(seed: "D")
        sheet.type("D/")
        sheet.pointerOver(try row("That saved a wretch like me", of: sheet), 13)
        try draw(LibraryWindow(model: model), named: "17 typing a chord", size: Self.sheetSize)
        sheet.press(.down)
        sheet.press(.down)
        try draw(LibraryWindow(model: model), named: "18 typing, the second offered picked", size: Self.sheetSize)
        sheet.press(.escape)
    }

    @Test("A chord being dragged to another word")
    func dragging() async throws {
        let mac = try MadeUpMac()
        let model = await modelWithASongOpen(mac, editor: .sheet)
        let sheet = try #require(model.sheet)
        let from = try row("That saved a wretch like me", of: sheet)
        let chord = try #require(sheet.rows[from].chords.first)
        sheet.lift(from, chord.at)
        sheet.carry(over: from, 13)
        try draw(LibraryWindow(model: model), named: "19 dragging a chord", size: Self.sheetSize)
        sheet.dropNowhere()
    }

    @Test("One of the key's chords carried from the strip, over a word: its button faint in the strip, and another held down")
    func carriedFromTheStrip() async throws {
        let mac = try MadeUpMac()
        let model = await modelWithASongOpen(mac, editor: .sheet)
        let sheet = try #require(model.sheet)
        let fourth = try #require(sheet.inKey.dropFirst(3).first)
        sheet.carry(fourth, over: try row("That saved a wretch like me", of: sheet), 13)
        var found = 0
        try draw(LibraryWindow(model: model), named: "37 a chord carried from the strip", size: Self.sheetSize) { window in
            // The fourth button's chord is out being carried, and the sixth is held down.
            @MainActor func look(in view: NSView) {
                if let grip = view as? KeyChordGrip {
                    found += 1
                    if grip.name == fourth {
                        grip.pressed(at: NSPoint(x: 4, y: 4))
                        _ = grip.pulled(to: NSPoint(x: 4, y: 14))
                    } else if grip.name == sheet.inKey.dropFirst(5).first {
                        grip.pressed(at: NSPoint(x: 4, y: 4))
                    }
                }
                view.subviews.forEach(look)
            }
            look(in: window)
        }
        #expect(found == 7)
        sheet.carryOff()
    }

    @Test("The sheet of a song with blank slides and a line of chords alone")
    func theSheetWithBlankSlides() async throws {
        let mac = try MadeUpMac()
        let model = await modelWithASongOpen(mac, song: "Blank Slides", editor: .sheet)
        let sheet = try #require(model.sheet)
        let blank = try #require(sheet.rows.firstIndex { $0.blank })
        sheet.setSpot(blank, 0)
        try draw(LibraryWindow(model: model), named: "20 the sheet, blank slides", size: Self.sheetSize)
    }

    @Test("The sheet of a song ProPresenter wrote: no key stated, groups in their colours; and with ProPresenter running")
    func theSheetOfAFileProPresenterWrote() async throws {
        let mac = try MadeUpMac()
        mac.proPresenterIsRunning = true
        try draw(LibraryWindow(model: await modelWithASongOpen(mac, song: "Rock Of Ages (Toplady)", editor: .sheet)),
                 named: "21 the sheet, a song ProPresenter wrote", size: Self.sheetSize)
    }

    @Test("The sheet in the window at its usual size, and what it said after a copy")
    func theSheetAfterACopy() async throws {
        let mac = try MadeUpMac()
        let model = await modelWithASongOpen(mac, song: "It Is Well", editor: .sheet)
        let sheet = try #require(model.sheet)
        sheet.setSpot(try #require(sheet.rows.indices.first(where: sheet.usable)), 0)
        model.copyChords(wholeGroup: true)
        try draw(LibraryWindow(model: model), named: "22 the sheet, after a copy")
        // And as narrow as the window goes.
        try draw(LibraryWindow(model: model), named: "23 the sheet, the window at its narrowest", size: NSSize(width: 620, height: 520))
    }

    @Test("The sheet as a grid in a wide window: at its usual size, with the words small, and with them large")
    func theGrid() async throws {
        let mac = try MadeUpMac()
        let model = await modelWithASongOpen(mac, editor: .sheet)
        let sheet = try #require(model.sheet)
        sheet.setSpot(try row("That saved a wretch like me", of: sheet), 5)
        let wide = NSSize(width: 1600, height: 900)
        try draw(LibraryWindow(model: model), named: "24 the grid, wide", size: wide)
        model.zoomSheet(-1)
        model.zoomSheet(-1)
        try draw(LibraryWindow(model: model), named: "25 the grid, the words small", size: wide)
        model.zoomSheet(0)
        model.zoomSheet(1)
        model.zoomSheet(1)
        model.zoomSheet(1)
        try draw(LibraryWindow(model: model), named: "26 the grid, the words large", size: wide)
        model.zoomSheet(0)
    }

    @Test("The grid of a song ProPresenter wrote, its slides framed in their groups' colours")
    func theGridInColours() async throws {
        let mac = try MadeUpMac()
        let model = await modelWithASongOpen(mac, song: "Rock Of Ages (Toplady)", editor: .sheet)
        try draw(LibraryWindow(model: model), named: "27 the grid, groups in their colours", size: NSSize(width: 1600, height: 900))
    }

    @Test("The settings: the colours of chords, as they come and with two chosen")
    func theSettings() async throws {
        let mac = try MadeUpMac()
        let model = await modelWithASongOpen(mac, editor: .sheet)
        // (On the window's own ground, which a settings window has under it.)
        let settings = ChordColourSettings(model: model).background(Color(nsColor: .windowBackgroundColor))
        try draw(settings, named: "28 settings, the colours of chords", size: NSSize(width: 560, height: 440))
        model.setSheetChordColour("#7fe3ff")
        model.setTextChordColour("#c95fff")
        model.setSheetLineColour("#8cf58c")
        try draw(settings, named: "29 settings, three colours chosen", size: NSSize(width: 560, height: 440), dark: true)
        model.setSheetLineColour(nil)
        try draw(LibraryWindow(model: model), named: "30 the sheet, another colour for chords", size: Self.sheetSize)
    }

    @Test("Under the song: Revert, with something to revert, and the words that say changes are saved as they are made")
    func revertAndSave() async throws {
        let mac = try MadeUpMac()
        let model = await modelWithASongOpen(mac, editor: .sheet)
        model.setKey("A")
        try draw(LibraryWindow(model: model), named: "31 Revert, something changed", size: Self.sheetSize)
        model.revert()
        try draw(LibraryWindow(model: model), named: "32 just reverted", size: NSSize(width: 1000, height: 560), dark: true)
    }

    // ---- The guide (spec/13-guide.md)

    @Test("The guide: its window as it opens, in the light and in the dark; in a narrow window; and the whole of its page")
    func theGuide() async throws {
        let mac = try MadeUpMac()
        let model = await modelWithASongOpen(mac, editor: .sheet)
        let opens = NSSize(width: 780, height: 820)
        try draw(GuideWindow(model: model), named: "38 the guide, as it opens", size: opens)
        try draw(GuideWindow(model: model), named: "39 the guide, in the dark", size: opens, dark: true)
        try draw(GuideWindow(model: model), named: "40 the guide, in a narrow window", size: NSSize(width: 460, height: 700))
        // All of the page, as tall as it is.
        let guide = try #require(GuideFiles.guide)
        let page = GuidePage(guide: guide, chordColour: .systemOrange)
            .frame(width: GuideLook.width)
            .padding(GuideLook.margin)
            .background(Color(nsColor: .textBackgroundColor))
        let whole = NSHostingView(rootView: page).fittingSize
        try draw(page, named: "41 the guide, the whole page", size: whole)
        try draw(page, named: "42 the guide, the whole page in the dark", size: whole, dark: true)
        // The ? that opens it, at the right of the strip.
        try draw(LibraryWindow(model: model), named: "43 the strip, with the way to the guide", size: Self.sheetSize)
    }

    /// The sheet's own view, in a window that has been laid out.
    private func canvas(in view: NSView) -> SheetCanvas? {
        if let found = view as? SheetCanvas { return found }
        for inner in view.subviews {
            if let found = canvas(in: inner) { return found }
        }
        return nil
    }

    /// Puts the pointer on a place of the sheet in a window that has been laid out,
    /// as the mouse would, with Command held or not; and gives the window a moment to
    /// say what it then has to say.
    private func point(at spot: Spot, onItsChord: Bool = false, commandHeld: Bool = false, in window: NSView) {
        guard let canvas = canvas(in: window), let point = canvas.point(over: spot, onItsChord: onItsChord) else {
            Issue.record("the window has no sheet, or the sheet no such place")
            return
        }
        canvas.pointerMoved(to: point, shift: false)
        canvas.modifiers(commandHeld ? [.command] : [])
        RunLoop.main.run(until: Date(timeIntervalSinceNow: 0.2))
        window.layoutSubtreeIfNeeded()
    }

    @Test("The line of hints under the sheet: what it says for each thing that can be going on, in the light and the dark, lit, and where there is little room; and the window with the pointer on a word, its line highlighted, on a chord, with Command held, narrow, over the key's chords, and on a line of chords alone")
    func theLineOfHints() async throws {
        let mac = try macWithHistory()
        let model = await modelWithASongOpen(mac, editor: .sheet)
        typealias Paste = SheetHints.Paste
        let cases: [(String, [SheetHints.Hint])] = [
            ("the pointer on a chord", SheetHints.hints(.onChord)),
            ("on a place with no chord", SheetHints.hints(.onPlace)),
            ("Command held", SheetHints.hints(.onPlace, command: true)),
            ("held, the line has chords", SheetHints.hints(.onPlace, command: true, paste: Paste(lines: 1, replaces: true))),
            ("held, a verse copied", SheetHints.hints(.onPlace, command: true, paste: Paste(lines: 4, replaces: true))),
            ("a chord being typed", SheetHints.hints(.typing(onChord: false, empty: false))),
            ("the box cleared, on a chord", SheetHints.hints(.typing(onChord: true, empty: true))),
            ("a chord being carried", SheetHints.hints(.carrying(fromStrip: false))),
            ("carried, with Option held", SheetHints.hints(.carrying(fromStrip: false), option: true)),
            ("carried from the strip", SheetHints.hints(.carrying(fromStrip: true))),
            ("over the key's chords", SheetHints.strip),
            ("ProPresenter running", SheetHints.cannotChange),
            ("nobody at the sheet", SheetHints.idle),
        ]
        /// Every case, each fitted into this much room as the line under the sheet
        /// fits it and on the line's own ground, with what it is a case of before
        /// it. `lit`: how much the ground is lit, as it is for a moment when what is
        /// said has just changed.
        func all(room: Double, lit: Double = 0) -> some View {
            VStack(alignment: .leading, spacing: 0) {
                ForEach(Array(cases.enumerated()), id: \.offset) { _, one in
                    let fit = HintFit.fit(one.1, in: room)
                    HStack(spacing: 0) {
                        Text(one.0)
                            .font(.caption)
                            .foregroundStyle(.tertiary)
                            .frame(width: 190, alignment: .leading)
                        HintRow(hints: one.1, fit: fit)
                            .padding(.horizontal, HintFit.sides)
                            .frame(width: room + 2 * HintFit.sides, height: HintFit.height(of: fit), alignment: .leading)
                            .clipped()
                            .background(HintGround(said: [], shown: lit))
                    }
                    .padding(.leading, 12)
                    Color(nsColor: .windowBackgroundColor).frame(height: 3)
                }
                Spacer(minLength: 0)
            }
            .background(Color(nsColor: .windowBackgroundColor))
        }
        // (Tall enough for three of the rows to be two lines.)
        let tall = Double(cases.count) * 33 + 80
        func wide(_ room: Double) -> NSSize { NSSize(width: 190 + 12 + room + 2 * HintFit.sides, height: tall) }
        try draw(all(room: 946), named: "45 the line of hints, all it says", size: wide(946))
        try draw(all(room: 946), named: "46 the line of hints, in the dark", size: wide(946), dark: true)
        // As it is for a moment when what it says has just changed: lit.
        try draw(all(room: 946, lit: 1), named: "60 the line of hints, lit", size: wide(946))
        try draw(all(room: 946, lit: 1), named: "61 the line of hints, lit, in the dark", size: wide(946), dark: true)
        // With less room: the last hints are left off; the two sentences on copying
        // go one under the other, and then are drawn smaller.
        try draw(all(room: 720), named: "53 the line of hints, with 720 of room", size: wide(720))
        try draw(all(room: 560), named: "54 the line of hints, with 560 of room", size: wide(560))
        try draw(all(room: 340), named: "59 the line of hints, with 340 of room", size: wide(340))

        // In the window: under the sheet, over the quiet line.
        let sheet = try #require(model.sheet)
        let first = try row("Amazing grace how sweet the sound", of: sheet)
        let second = try row("That saved a wretch like me", of: sheet)
        try draw(LibraryWindow(model: model), named: "47 the sheet, nobody at it", size: Self.sheetSize)
        // The pointer on a word: its line is the highlighted one.
        try draw(LibraryWindow(model: model), named: "48 the sheet, the pointer on a word", size: Self.sheetSize) { window in
            point(at: Spot(row: second, at: 13), in: window)
        }
        // The pointer on a chord: that it can be dragged, and a copy of it.
        let chord = try #require(sheet.rows[first].chords.dropFirst().first)
        try draw(LibraryWindow(model: model), named: "62 the sheet, the pointer on a chord", size: NSSize(width: 1280, height: 800)) { window in
            point(at: Spot(row: first, at: chord.at), onItsChord: true, in: window)
        }
        // And with Command held: how that line's chords are copied, and pasted onto it.
        try draw(LibraryWindow(model: model), named: "49 the sheet, Command held", size: Self.sheetSize, dark: true) { window in
            point(at: Spot(row: first, at: 8), commandHeld: true, in: window)
        }
        try draw(LibraryWindow(model: model), named: "55 the sheet, Command held, at the size the app opens at", size: NSSize(width: 1280, height: 800)) { window in
            point(at: Spot(row: first, at: 8), commandHeld: true, in: window)
        }
        try draw(LibraryWindow(model: model), named: "56 the sheet, Command held, in a narrow window", size: NSSize(width: 900, height: 560)) { window in
            point(at: Spot(row: first, at: 8), commandHeld: true, in: window)
        }
        model.holds(command: false, option: false)
        // The pointer over the key's chords at the top of the sheet.
        model.sheetIs(.nothing)
        model.pointerIsOverTheKeyChords(true)
        try draw(LibraryWindow(model: model), named: "63 the sheet, the pointer over the key's chords", size: NSSize(width: 1280, height: 800))
        // (A window that goes takes the pointer off its strip with it: so it is put
        // there again for the next.)
        #expect(!model.overKeyChords)
        model.pointerIsOverTheKeyChords(true)
        try draw(LibraryWindow(model: model), named: "64 the same, in the dark", size: NSSize(width: 1280, height: 800), dark: true)
        model.pointerIsOverTheKeyChords(false)
        // The highlighted line in another colour, chosen in the settings.
        model.setSheetLineColour("#5fd0ff")
        try draw(LibraryWindow(model: model), named: "57 the sheet, another colour for the highlighted line", size: Self.sheetSize) { window in
            point(at: Spot(row: second, at: 13), in: window)
        }
        model.setSheetLineColour(nil)
        // A song that has earlier versions: the button to them, in its colour.
        model.open(song: mac.song("Holy Holy Holy"))
        try draw(LibraryWindow(model: model), named: "50 a song with earlier versions", size: Self.sheetSize)
        try draw(LibraryWindow(model: model), named: "51 a song with earlier versions, in the dark", size: Self.sheetSize, dark: true)

        // A line of chords alone has no words to colour: its rule takes the colour.
        // (The song that has such lines is not among those every made-up Mac has.)
        try TestData.file("shaped-as-real-ones/Chords With No Words.pro")
            .write(to: mac.place.appendingPathComponent("Libraries/Songs/Chords With No Words.pro"))
        await model.refresh()
        model.open(song: mac.song("Chords With No Words"))
        let shaped = try #require(model.sheet)
        let alone = try #require(shaped.rows.firstIndex { $0.alone && !$0.blank && !$0.chords.isEmpty }, "the song was to have a line of chords alone")
        try draw(LibraryWindow(model: model), named: "58 a line of chords alone, highlighted", size: Self.sheetSize) { window in
            point(at: Spot(row: alone, at: 0), onItsChord: true, commandHeld: true, in: window)
        }
        model.holds(command: false, option: false)
    }

    @Test("Chords with no words: the song in the shapes real ones have, as it is; with chords put on its three blank slides and on its lines of chords alone; and that as ChordPro text")
    func chordsWithNoWords() async throws {
        let mac = try MadeUpMac()
        // (The song is not among those every made-up Mac has: TestData says why.)
        try TestData.file("shaped-as-real-ones/Chords With No Words.pro")
            .write(to: mac.place.appendingPathComponent("Libraries/Songs/Chords With No Words.pro"))
        let model = await modelWithASongOpen(mac, song: "Chords With No Words", editor: .sheet)
        let sheet = try #require(model.sheet)
        let wide = NSSize(width: 1500, height: 900)
        sheet.setSpot(try #require(sheet.rows.firstIndex { $0.blank }), 0)
        try draw(LibraryWindow(model: model), named: "33 chords with no words, as the song is", size: wide)

        // A chord on each blank slide, two on the last; and one more on every line
        // of chords alone that had some.
        for row in sheet.rows.indices where sheet.usable(row) && sheet.rows[row].alone {
            let has = sheet.rows[row].chords.count
            #expect(sheet.put(row, has * 2, has == 0 ? "E" : "F#m"))
        }
        let last = try #require(sheet.rows.indices.last { sheet.rows[$0].slide == 11 })
        #expect(sheet.put(last, 2, "B7"))
        sheet.setSpot(last, 4)
        #expect(model.refusal == nil)
        try draw(LibraryWindow(model: model), named: "34 chords with no words, chords put on", size: wide)
        model.show(editor: .chordPro)
        try draw(LibraryWindow(model: model), named: "35 chords with no words, as ChordPro text", size: NSSize(width: 1100, height: 900))
    }
}
