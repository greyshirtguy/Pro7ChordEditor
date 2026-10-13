// How the window is divided: where the edge between the lists at the left and the song
// at the right starts, where it can be dragged to, and what is remembered of it.
// spec/06-library.md, rule 6a.
//
// The edge is the window's own (LibraryWindow.swift says why it is not the system's
// split view, which starts the lists at their widest whatever it is asked). So where it
// is, is plain arithmetic, and is tried as that; and then the window itself is laid out
// off the screen, drawn, and the edge looked for in the picture. What a hand on the
// mouse does to it is tried by telling the edge what the mouse would: that the button
// went down, where the pointer went, and that it was let go.

import AppKit
@testable import AppViews
import Song
import SwiftUI
import Testing

@MainActor
@Suite("How the window is divided")
struct WindowLayoutTests {
    @Test("The lists start half way between the least width they can be dragged to and the most")
    func whereTheEdgeStarts() async throws {
        #expect(LibraryModel.leastListsWidth == 210 && LibraryModel.mostListsWidth == 480)
        #expect(LibraryModel.usualListsWidth == 345)
        let mac = try MadeUpMac()
        let model = mac.model()
        #expect(model.listsWidth == 345)
        await model.start()
        #expect(model.listsWidth == 345)
    }

    @Test("The lists are as wide as they are asked to be, between their least and their most, and never so wide that the song has less than its own least; in a window too narrow for both they have their least")
    func widths() {
        // The size the app opens at, and any wider: what is asked, within the two.
        for whole in [1280.0, 1000, 2400] {
            #expect(ListsLook.width(asked: 345, of: whole) == 345)
            #expect(ListsLook.width(asked: 100, of: whole) == 210)
            #expect(ListsLook.width(asked: 210, of: whole) == 210)
            #expect(ListsLook.width(asked: 480, of: whole) == 480)
            #expect(ListsLook.width(asked: 9000, of: whole) == 480)
        }
        // Narrower: the song keeps 380, and the edge between them its eight.
        #expect(ListsLook.edgeWidth == 8)
        #expect(ListsLook.width(asked: 480, of: 800) == 412)
        #expect(ListsLook.width(asked: 345, of: 800) == 345)
        #expect(ListsLook.width(asked: 345, of: 700) == 312)
        // The narrowest the window can be made, and narrower than that.
        #expect(ListsLook.width(asked: 345, of: 620) == 232)
        #expect(ListsLook.width(asked: 345, of: 500) == 210)
        #expect(ListsLook.width(asked: 345, of: 0) == 210)
    }

    @Test("Dragged, the edge is left where it was put, between the least and the most, and that is remembered; put back where it usually is, nothing is kept of it")
    func dragged() async throws {
        let mac = try MadeUpMac()
        let model = mac.model()
        await model.start()
        model.setListsWidth(260)
        #expect(model.listsWidth == 260)
        #expect(mac.appData.settings.listsWidth == 260)
        model.setListsWidth(50)
        #expect(model.listsWidth == 210)
        model.setListsWidth(4000)
        #expect(model.listsWidth == 480)
        #expect(mac.appData.settings.listsWidth == 480)

        // The next time the app is started.
        let again = mac.model()
        #expect(again.listsWidth == 345)
        await again.start()
        #expect(again.listsWidth == 480)
        again.setListsWidth(nil)
        #expect(again.listsWidth == 345)
        #expect(mac.appData.settings.listsWidth == nil)
        // And nothing else that is kept was lost by it.
        again.setListsSplit(0.5)
        again.setListsWidth(300)
        #expect(mac.appData.settings.listsSplit == 0.5 && mac.appData.settings.listsWidth == 300)

        // A width that was kept by something else and is out of bounds is brought within.
        var kept = mac.appData.settings
        kept.listsWidth = 12
        mac.appData.settings = kept
        let third = mac.model()
        await third.start()
        #expect(third.listsWidth == 210)
    }

    // ---- The edge itself, which is dragged

    @Test("The edge, dragged: the lists are asked to be as much wider or narrower as the pointer has gone since the button went down; let go, that is said once; clicked twice, it is put back and nothing is dragged")
    func theEdgeDragged() {
        let grip = EdgeGrip.Grip(frame: NSRect(x: 0, y: 0, width: ListsLook.edgeWidth, height: 400))
        var asked: [Double] = []
        var ended = 0
        var reset = 0
        grip.width = 345
        grip.moved = { asked.append($0) }
        grip.ended = { ended += 1 }
        grip.reset = { reset += 1 }

        // Not down: the pointer passing over it drags nothing.
        grip.pulled(to: 600)
        grip.released()
        #expect(asked.isEmpty && ended == 0)

        grip.pressed(at: 349, clicks: 1)
        grip.pulled(to: 379)
        grip.pulled(to: 300)
        // (Whatever width the lists are meanwhile, as the dragging makes them: it
        // goes by where the button went down.)
        grip.width = 296
        grip.pulled(to: 1000)
        #expect(asked == [375, 296, 996])
        grip.released()
        grip.released()
        #expect(ended == 1 && reset == 0)

        // Twice clicked: put back, and the second click drags nothing.
        grip.pressed(at: 349, clicks: 2)
        grip.pulled(to: 500)
        grip.released()
        #expect(reset == 1 && asked.count == 3 && ended == 1)
        // The pointer's shape over it is the system's to keep, by a rectangle that is
        // all of the bar; and a click on it in a window that is not in front drags it.
        #expect(grip.acceptsFirstMouse(for: nil) && !grip.mouseDownCanMoveWindow)
    }

    @Test("In the window: the edge dragged makes the lists as wide as it is dragged to, within their least and their most; let go, that is remembered; clicked twice, they are back where they start")
    func theEdgeInTheWindow() async throws {
        let mac = try MadeUpMac()
        let model = mac.model()
        await model.start()
        model.choose(library: mac.library("Songs"))
        let size = NSSize(width: 1280, height: 800)
        let host = NSHostingView(rootView: LibraryWindow(model: model).frame(width: size.width, height: size.height))
        let window = NSWindow(contentRect: NSRect(origin: .zero, size: size), styleMask: [.borderless], backing: .buffered, defer: false)
        window.isReleasedWhenClosed = false
        defer {
            window.contentView = nil
            window.close()
        }
        window.contentView = host
        host.frame = NSRect(origin: .zero, size: size)
        func settle() {
            for _ in 0..<4 { CFRunLoopRunInMode(.defaultMode, 0.01, false) }
            host.layoutSubtreeIfNeeded()
        }
        func grip() throws -> EdgeGrip.Grip {
            func find(in view: NSView) -> EdgeGrip.Grip? {
                if let found = view as? EdgeGrip.Grip { return found }
                for inner in view.subviews {
                    if let found = find(in: inner) { return found }
                }
                return nil
            }
            return try #require(find(in: host), "the window has no edge to drag")
        }
        settle()
        // It stands just right of the lists, and is as wide as it is said to be.
        var frame = try grip().convert(try grip().bounds, to: host)
        #expect(frame.minX == 345 && frame.width == ListsLook.edgeWidth && frame.height == 800)

        try grip().pressed(at: 349, clicks: 1)
        try grip().pulled(to: 409)
        settle()
        frame = try grip().convert(try grip().bounds, to: host)
        #expect(frame.minX == 405)
        // (Not remembered until it is let go.)
        #expect(model.listsWidth == 345)
        // Past the most the lists may have: they stop there.
        try grip().pulled(to: 2000)
        settle()
        #expect(try grip().convert(try grip().bounds, to: host).minX == 480)
        try grip().pulled(to: 440)
        try grip().released()
        settle()
        #expect(model.listsWidth == 436 && mac.appData.settings.listsWidth == 436)
        #expect(try grip().convert(try grip().bounds, to: host).minX == 436)

        try grip().pressed(at: 440, clicks: 2)
        settle()
        #expect(model.listsWidth == 345 && mac.appData.settings.listsWidth == nil)
        #expect(try grip().convert(try grip().bounds, to: host).minX == 345)
    }

    /// The window laid out off the screen at a size and drawn; and where, along a row
    /// near its top, the lists' ground ends: how wide the lists are, in points.
    private func listsWidth(of model: LibraryModel, at size: NSSize) throws -> Double {
        let host = NSHostingView(rootView: LibraryWindow(model: model).frame(width: size.width, height: size.height).environment(\.colorScheme, .light))
        let window = NSWindow(contentRect: NSRect(origin: .zero, size: size), styleMask: [.borderless], backing: .buffered, defer: false)
        window.isReleasedWhenClosed = false
        defer {
            window.contentView = nil
            window.close()
        }
        window.appearance = NSAppearance(named: .aqua)
        window.contentView = host
        host.frame = NSRect(origin: .zero, size: size)
        host.layoutSubtreeIfNeeded()
        RunLoop.main.run(until: Date(timeIntervalSinceNow: 0.3))
        host.layoutSubtreeIfNeeded()
        let picture = try #require(host.bitmapImageRepForCachingDisplay(in: host.bounds))
        host.cacheDisplay(in: host.bounds, to: picture)
        if let folder = ProcessInfo.processInfo.environment["PRO7_PICTURES"], let png = picture.representation(using: .png, properties: [:]) {
            try? png.write(to: URL(fileURLWithPath: folder).appendingPathComponent("the window \(Int(size.width)) wide, the lists asked \(Int(model.listsWidth)).png"))
        }
        let scale = Double(picture.pixelsWide) / size.width
        // (Just under the window's top, over the title of the places: the lists'
        // ground and nothing on it, from the left side to the edge.)
        let y = Int(3 * scale)
        func grey(_ x: Int) throws -> Double {
            let colour = try #require(picture.colorAt(x: x, y: y)?.usingColorSpace(.deviceRGB))
            return Double(colour.redComponent + colour.greenComponent + colour.blueComponent) / 3
        }
        let ground = try grey(Int(20 * scale))
        for x in Int(20 * scale)..<picture.pixelsWide where abs(try grey(x) - ground) > 0.03 {
            return Double(x) / scale
        }
        Issue.record("the lists' ground runs the whole width of the window")
        return size.width
    }

    @Test("The window as it is drawn: the lists are 345 wide at the size the app opens at, and at any other; narrower where the window is, so that the song keeps its least; and where they were dragged to")
    func asDrawn() async throws {
        let mac = try MadeUpMac()
        let model = mac.model()
        await model.start()
        model.choose(library: mac.library("Songs"))
        for (size, wide) in [(NSSize(width: 1280, height: 800), 345.0), (NSSize(width: 1000, height: 560), 345), (NSSize(width: 1900, height: 1000), 345), (NSSize(width: 620, height: 440), 232)] {
            let drawn = try listsWidth(of: model, at: size)
            #expect(drawn == wide, "in a window \(size.width) wide the lists are drawn \(drawn) wide")
        }
        model.setListsWidth(430)
        for (size, wide) in [(NSSize(width: 1280, height: 800), 430.0), (NSSize(width: 700, height: 500), 312)] {
            let drawn = try listsWidth(of: model, at: size)
            #expect(drawn == wide, "dragged to 430, in a window \(size.width) wide the lists are drawn \(drawn) wide")
        }
    }
}
