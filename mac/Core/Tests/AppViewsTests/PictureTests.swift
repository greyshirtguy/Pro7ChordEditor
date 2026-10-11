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

@MainActor
@Suite("Pictures of the window")
struct PictureTests {
    static let folder: URL = {
        let named = ProcessInfo.processInfo.environment["PRO7_PICTURES"].map { URL(fileURLWithPath: $0, isDirectory: true) }
        let folder = named ?? FileManager.default.temporaryDirectory.appendingPathComponent("Pro7ChordEditorPictures", isDirectory: true)
        try? FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
        return folder
    }()

    /// Draws a view off the screen and writes it as a PNG. Says where.
    @discardableResult
    private func draw(_ view: some View, named name: String, size: NSSize = NSSize(width: 1000, height: 560), dark: Bool = false) throws -> URL {
        let appearance = NSAppearance(named: dark ? .darkAqua : .aqua)
        let host = NSHostingView(rootView: view.frame(width: size.width, height: size.height).environment(\.colorScheme, dark ? .dark : .light))
        let window = NSWindow(contentRect: NSRect(origin: .zero, size: size), styleMask: [.borderless], backing: .buffered, defer: false)
        window.appearance = appearance
        window.contentView = host
        host.frame = NSRect(origin: .zero, size: size)
        host.layoutSubtreeIfNeeded()
        // (The lists fill themselves in on the next turns of the run loop.)
        RunLoop.main.run(until: Date(timeIntervalSinceNow: 0.4))
        host.layoutSubtreeIfNeeded()
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
        try draw(LibraryWindow(model: model), named: "23 the sheet, the window at its narrowest", size: NSSize(width: 860, height: 520))
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
}
