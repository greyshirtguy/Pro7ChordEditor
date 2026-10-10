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

    private func modelWithASongOpen(_ mac: MadeUpMac, song: String = "Amazing Grace") async -> LibraryModel {
        let model = mac.model()
        await model.start()
        model.choose(library: mac.library("Songs"))
        await model.marksAreWorkedOut()
        model.open(song: mac.song(song))
        return model
    }

    @Test("A song open, to be looked at")
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
}
