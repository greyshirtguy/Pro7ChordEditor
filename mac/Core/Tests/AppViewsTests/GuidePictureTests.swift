// The guide's pictures: of this app's own sheet, drawn here from a test song, and kept in
// the repository beside the guide's words (Sources/AppViews/Guide), from where they go
// into the app. spec/13-guide.md, "The pictures".
//
// Each is the strip and the first row of slides of Amazing Grace, as the window shows
// them, in a pane as wide as the guide's page, with the sheet in the state the picture
// is of: the spot on a word, a chord being typed, a chord being dragged. A window that
// is drawn has no pointer in it, so the pointer is drawn in afterwards, where it would
// be. They are drawn at twice the size, whatever screen this Mac has, so that they are
// sharp on a screen with dots to spare.
//
// TO MAKE THEM AGAIN, when how the sheet or its strip is drawn has changed:
//
//     PRO7_GUIDE_PICTURES=write swift test --package-path mac/Core --filter GuidePictureTests
//
// and then LOOK AT THEM (they are written to the pictures' folder too, as "guide
// <name>.png": PictureTests says where that is), and commit them.
//
// What holds them true. `asTheAppDrawsThem` draws each afresh and holds the kept one to
// it, dot for dot: a picture in the guide of a sheet the app no longer draws is a
// comment that has become untrue. It is not asked on GitHub's machine, whose Mac draws
// letters a hair otherwise than the one the pictures were made on; there the pictures
// are only seen to be there, of the size they should be, and not blank. Should it fail
// on a Mac where nothing of the sheet was changed (another macOS draws another hair
// otherwise), look at the fresh pictures, and if they are right write them again.

import AppKit
@testable import AppViews
import Song
import SwiftUI
import Testing
import TestData

@MainActor
@Suite("The guide's pictures")
struct GuidePictureTests {
    /// Where the kept pictures are, in the source: beside the guide's words.
    static let kept = URL(fileURLWithPath: #filePath).deletingLastPathComponent().deletingLastPathComponent().deletingLastPathComponent()
        .appendingPathComponent("Sources/AppViews/Guide", isDirectory: true)
    static let writing = ProcessInfo.processInfo.environment["PRO7_GUIDE_PICTURES"] == "write"
    static let onGitHub = ProcessInfo.processInfo.environment["GITHUB_ACTIONS"] != nil || ProcessInfo.processInfo.environment["CI"] != nil

    /// As wide as the guide's page, in points; and how many dots to a point.
    static let width = GuideLook.width
    static let dots = 2

    /// The strip and the sheet under it, as the window has them, of a song on a
    /// made-up Mac.
    final class Scene {
        let mac: MadeUpMac
        let model: LibraryModel
        let sheet: ChordSheet
        var window: NSWindow?
        var host: NSView?
        var canvas: SheetCanvas?

        init(mac: MadeUpMac, model: LibraryModel, sheet: ChordSheet) {
            self.mac = mac
            self.model = model
            self.sheet = sheet
        }

        func row(_ words: String) throws -> Int {
            try #require(sheet.rows.firstIndex { $0.kind == .line && $0.text.string == words }, "no line \(words)")
        }
    }

    /// A window that says it is on a screen of two dots to the point, whatever screen
    /// this Mac has. It is never shown; but what is in it asks it how finely to draw,
    /// and the strip's letters, which SwiftUI draws once and keeps, would otherwise be
    /// drawn for this Mac's first screen and then stretched.
    final class TwoDotsWindow: NSWindow {
        override var backingScaleFactor: CGFloat { CGFloat(GuidePictureTests.dots) }
    }

    private func scene() async throws -> Scene {
        let mac = try MadeUpMac()
        let model = mac.model()
        await model.start()
        model.choose(library: mac.library("Songs"))
        await model.marksAreWorkedOut()
        model.open(song: mac.song("Amazing Grace"))
        model.show(editor: .sheet)
        return Scene(mac: mac, model: model, sheet: try #require(model.sheet))
    }

    private func find<Wanted: NSView>(_ type: Wanted.Type, in view: NSView) -> Wanted? {
        if let found = view as? Wanted { return found }
        for inner in view.subviews {
            if let found = find(type, in: inner) { return found }
        }
        return nil
    }

    /// Puts the scene in a window that is never shown, as tall as the strip and the
    /// first row of slides need.
    private func lay(_ scene: Scene) throws {
        let model = scene.model
        let shown = try #require(model.shown)
        let view = VStack(spacing: 0) {
            SheetStrip(model: model, sheet: scene.sheet, key: shown.key)
            ChordSheetCanvasView(model: model, version: model.sheetVersion, scale: model.sheetZoom, chordColour: model.sheetChordColour)
                .frame(maxWidth: .infinity, maxHeight: .infinity)
        }
        // (Drawn for a screen of two dots to the point, whatever this Mac's is.)
        .environment(\.displayScale, CGFloat(Self.dots))
        .environment(\.colorScheme, .dark)
        var size = NSSize(width: Self.width, height: 420)
        let host = NSHostingView(rootView: view)
        let window = TwoDotsWindow(contentRect: NSRect(origin: .zero, size: size), styleMask: [.borderless], backing: .buffered, defer: false)
        window.appearance = NSAppearance(named: .darkAqua)
        window.contentView = host
        func settle() {
            window.setContentSize(size)
            host.frame = NSRect(origin: .zero, size: size)
            host.layoutSubtreeIfNeeded()
            RunLoop.main.run(until: Date(timeIntervalSinceNow: 0.15))
            host.layoutSubtreeIfNeeded()
        }
        settle()
        let scroll = try #require(find(SheetScroll.self, in: host))
        // The scroll bars are not to be in the picture, nor to take room from the
        // sheet, whatever this Mac shows them as.
        scroll.scrollerStyle = .overlay
        scroll.hasVerticalScroller = false
        scroll.hasHorizontalScroller = false
        settle()
        // As tall as the strip, the first row of slides, and the room that row keeps
        // clear under it.
        let canvas = scroll.canvas
        let foot = try #require(canvas.cells.filter { $0.gridRow == 0 }.map(\.frame.maxY).max())
        let top = host.convert(NSPoint(x: 0, y: 0), from: canvas).y
        size.height = (host.isFlipped ? top : host.bounds.height - top) + foot + SheetLook.around + SheetLook.margin
        size.height = size.height.rounded(.up)
        settle()
        scene.window = window
        scene.host = host
        scene.canvas = canvas
    }

    /// Draws the scene, at two dots to the point, with the pointer on a place of the
    /// sheet if one is given.
    private func draw(_ scene: Scene, pointerOn spot: Spot? = nil) throws -> NSBitmapImageRep {
        let host = try #require(scene.host)
        let canvas = try #require(scene.canvas)
        let size = host.bounds.size
        let picture = try #require(NSBitmapImageRep(
            bitmapDataPlanes: nil, pixelsWide: Int(size.width) * Self.dots, pixelsHigh: Int(size.height) * Self.dots,
            bitsPerSample: 8, samplesPerPixel: 4, hasAlpha: true, isPlanar: false, colorSpaceName: .deviceRGB, bytesPerRow: 0, bitsPerPixel: 0))
        // (Its size in points is half its dots each way: so it is drawn twice as
        // large, and says of itself what size it is to be shown at.)
        picture.size = size
        host.cacheDisplay(in: host.bounds, to: picture)

        if let spot {
            let place = try #require(canvas.point(over: spot))
            // (At the foot of the letter, so that the arrow hangs under the words and
            // hides little of the one it points at.)
            var at = host.convert(NSPoint(x: place.x, y: place.y + canvas.metrics.wordHeight * 0.3), from: canvas)
            if !host.isFlipped { at.y = size.height - at.y }
            let context = try #require(NSGraphicsContext(bitmapImageRep: picture))
            NSGraphicsContext.saveGraphicsState()
            NSGraphicsContext.current = context
            // (Counted from the top left, as the view counts.)
            let turned = NSAffineTransform()
            turned.translateX(by: 0, yBy: size.height)
            turned.scaleX(by: 1, yBy: -1)
            turned.concat()
            Self.drawPointer(at: at)
            NSGraphicsContext.restoreGraphicsState()
        }
        return picture
    }

    /// The Mac's arrow, with its tip at a point: black, edged with white, as the
    /// system draws it. Drawn here and not taken from the system, whose own picture
    /// of it is whatever size and colour the person has set theirs to.
    private static func drawPointer(at tip: NSPoint) {
        let shape: [(Double, Double)] = [(0, 0), (0, 16.4), (3.75, 12.8), (6.3, 18.9), (8.8, 17.8), (6.3, 11.9), (11.4, 11.9)]
        let arrow = NSBezierPath()
        for (index, corner) in shape.enumerated() {
            let point = NSPoint(x: tip.x + corner.0, y: tip.y + corner.1)
            if index == 0 { arrow.move(to: point) } else { arrow.line(to: point) }
        }
        arrow.close()
        arrow.lineJoinStyle = .round
        let shadow = NSShadow()
        shadow.shadowColor = NSColor(white: 0, alpha: 0.45)
        shadow.shadowBlurRadius = 2.5
        shadow.shadowOffset = NSSize(width: 0, height: -1)
        NSGraphicsContext.saveGraphicsState()
        shadow.set()
        NSColor.white.setStroke()
        arrow.lineWidth = 2.4
        arrow.stroke()
        NSGraphicsContext.restoreGraphicsState()
        NSColor.black.setFill()
        arrow.fill()
    }

    /// The picture's dots as plain numbers: red, green, blue and how solid, for each,
    /// row by row.
    private static func numbers(of picture: NSBitmapImageRep) throws -> [UInt8] {
        let image = try #require(picture.cgImage)
        var numbers = [UInt8](repeating: 0, count: image.width * image.height * 4)
        let space = try #require(CGColorSpace(name: CGColorSpace.sRGB))
        try numbers.withUnsafeMutableBytes { bytes in
            let context = try #require(CGContext(data: bytes.baseAddress, width: image.width, height: image.height, bitsPerComponent: 8,
                                                 bytesPerRow: image.width * 4, space: space, bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue))
            context.draw(image, in: CGRect(x: 0, y: 0, width: image.width, height: image.height))
        }
        return numbers
    }

    /// What is done with a picture just drawn: it is written to the pictures' folder
    /// to be looked at; and either written over the kept one, when that was asked
    /// for, or held against it.
    private func keep(_ picture: NSBitmapImageRep, named name: String) throws {
        let png = try #require(picture.representation(using: .png, properties: [:]))
        let seen = PictureTests.folder.appendingPathComponent("guide \(name).png")
        try png.write(to: seen)
        print("PICTURE \(seen.path)")
        let file = Self.kept.appendingPathComponent(name + ".png")
        if Self.writing {
            // (All at once, so that the test beside this one never reads half of it.)
            try png.write(to: file, options: .atomic)
            print("WRITTEN \(file.path)")
            return
        }
        guard !Self.onGitHub else { return }
        let was = try #require(NSBitmapImageRep(data: try Data(contentsOf: file)), "there is no picture \(name).png kept for the guide")
        #expect(was.pixelsWide == picture.pixelsWide && was.pixelsHigh == picture.pixelsHigh,
                "the guide's picture \(name) is kept as \(was.pixelsWide) by \(was.pixelsHigh) dots, and is now drawn as \(picture.pixelsWide) by \(picture.pixelsHigh)")
        guard was.pixelsWide == picture.pixelsWide, was.pixelsHigh == picture.pixelsHigh else { return }
        let before = try Self.numbers(of: was)
        let now = try Self.numbers(of: picture)
        var differing = 0
        for dot in stride(from: 0, to: now.count, by: 4) where now[dot..<dot + 4] != before[dot..<dot + 4] {
            differing += 1
        }
        #expect(differing == 0, """
            The guide's picture \(name) is no longer what the app draws: \(differing) of its \(now.count / 4) dots are otherwise. \
            Look at \(seen.path). If that is right, write the guide's pictures again \
            (PRO7_GUIDE_PICTURES=write swift test --filter GuidePictureTests), look at them, and commit them.
            """)
    }

    // ---- The pictures

    @Test("The guide's pictures are what the app draws now: the spot on a word, a chord being typed, a chord being dragged")
    func asTheAppDrawsThem() async throws {
        // The spot on a word that has no chord, and the pointer on that word.
        var scene = try await scene()
        var line = try scene.row("That saved a wretch like me")
        scene.sheet.pointerOver(line, 13)
        try lay(scene)
        try keep(try draw(scene, pointerOn: Spot(row: line, at: 13)), named: "point-and-press")

        // A chord being typed over "wretch", with D typed so far. (On a slide's second
        // line, so that the bubble has room to grow upwards and the word it is for,
        // and the rest of its line, stay in sight: spec/03, rule 29.)
        scene = try await self.scene()
        line = try scene.row("That saved a wretch like me")
        scene.sheet.setSpot(line, 13)
        scene.sheet.startTyping(seed: "D")
        try lay(scene)
        try keep(try draw(scene), named: "typing")
        scene.sheet.press(.escape)

        // The chord over "like" being dragged to "saved": the bubble it left faint,
        // and its name lit where it would land.
        scene = try await self.scene()
        line = try scene.row("That saved a wretch like me")
        let chord = try #require(scene.sheet.rows[line].chords.first)
        scene.sheet.lift(line, chord.at)
        scene.sheet.carry(over: line, 5)
        try lay(scene)
        try keep(try draw(scene, pointerOn: Spot(row: line, at: 5)), named: "dragging")
        scene.sheet.dropNowhere()
    }

    @Test("Every picture the guide's words ask for is kept, twice as many dots across as the page is wide, and not blank")
    func keptForEveryName() throws {
        let guide = try #require(Guide.read(try Data(contentsOf: Self.kept.appendingPathComponent(GuideFiles.wordsName))))
        #expect(!guide.picturesNamed.isEmpty)
        for name in guide.picturesNamed {
            let file = Self.kept.appendingPathComponent(name + ".png")
            let picture = try #require(NSBitmapImageRep(data: try Data(contentsOf: file)), "no picture \(name).png is kept for the guide")
            #expect(picture.pixelsWide == Int(Self.width) * Self.dots, "\(name)")
            // (It says its own size, in points: half its dots each way. To within a
            // hair: a PNG says how many dots it has to the metre, in whole numbers.)
            #expect(abs(picture.size.width - Self.width) < 0.5, "\(name) says it is \(picture.size.width) wide")
            #expect(abs(picture.size.height * Double(Self.dots) - Double(picture.pixelsHigh)) < 1, "\(name) says it is \(picture.size.height) tall")
            var seen: Set<String> = []
            for x in stride(from: 10, to: picture.pixelsWide, by: max(1, picture.pixelsWide / 40)) {
                for y in stride(from: 10, to: picture.pixelsHigh, by: max(1, picture.pixelsHigh / 20)) {
                    if let colour = picture.colorAt(x: x, y: y) { seen.insert("\(colour.redComponent),\(colour.greenComponent),\(colour.blueComponent)") }
                }
            }
            #expect(seen.count > 3, "\(name) is one flat colour")
            // And the app finds it where the guide looks for it.
            #expect(GuideFiles.picture(name) != nil, "the guide does not find \(name)")
        }
        // Nothing is kept there that the words do not ask for.
        let there = try FileManager.default.contentsOfDirectory(atPath: Self.kept.path).filter { $0.hasSuffix(".png") }.map { String($0.dropLast(4)) }
        #expect(Set(there) == Set(guide.picturesNamed))
    }
}
