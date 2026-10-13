// How long the window keeps the main thread as it is used: as the pointer moves over
// the sheet, as a chord is dragged, and as the edge between the lists and the song is
// moved. mac/NOTES.md, "What made the window slow", has what was found with these.
//
// The maintainer, of 1.0.1 (2026-10-13): "the insert cursor is pretty laggy now", and
// of every build, that dragging the edge was slow with a song open. No test had looked
// at either, and no picture could: so the window is put through the same things here,
// off the screen, and the work the main thread does for each is timed (its own time at
// work, not time on the clock, so that a busy Mac says much the same).
//
// TWO KINDS OF TEST.
//
//   - One that is always run, and holds the one thing that was badly wrong: a change in
//     what the line of hints says is not to stop the window. It stopped it for a fifth
//     of a second, as often as the pointer crossed a chord. It is held to a fifth of
//     that, which is ten times what it takes now on the Mac this was written on, so
//     that a slower machine keeps it with room to spare; the fault did not come near.
//   - One that only measures, on a long song, and says what it found. It is run when it
//     is asked for:
//
//         PRO7_SPEED=1 swift test --package-path mac/Core --filter SpeedTests
//
//     (and with PRO7_SPEED=2, as a screen of two dots to the point would have it).
//     Its numbers are for whoever changes how the window is laid out or drawn, to be
//     set beside the ones in the notes. It fails for nothing: how long is too long on
//     another Mac is not a thing a test can know.
//
// What neither tells: what the screen itself costs. A window that is never shown is
// drawn into a picture, which is not how a shown one reaches the glass.

import AppKit
@testable import AppViews
import ProFormat
import Song
import SwiftUI
import Testing
import TestData

/// A window that says it is on a screen of two dots to the point.
private final class TwoDotsToThePoint: NSWindow {
    override var backingScaleFactor: CGFloat { 2 }
}

@MainActor
@Suite("How long the window takes")
struct SpeedTests {
    /// How long the main thread has been at work, in thousandths of a second.
    static func atWork() -> Double {
        var time = timespec()
        clock_gettime(CLOCK_THREAD_CPUTIME_ID, &time)
        return Double(time.tv_sec) * 1000 + Double(time.tv_nsec) / 1_000_000
    }

    private func find<Wanted: NSView>(_ type: Wanted.Type, in view: NSView) -> Wanted? {
        if let found = view as? Wanted { return found }
        for inner in view.subviews {
            if let found = find(type, in: inner) { return found }
        }
        return nil
    }

    /// Lets what was asked for be done: the run loop a few turns, and the window laid
    /// out.
    private func settle(_ host: NSView) {
        for _ in 0..<3 { CFRunLoopRunInMode(.defaultMode, 0.0004, false) }
        host.layoutSubtreeIfNeeded()
    }

    private func wait(_ seconds: Double) {
        RunLoop.main.run(until: Date(timeIntervalSinceNow: seconds))
    }

    /// The window on a model, never shown, at a size.
    private func window(on model: LibraryModel, size: NSSize, twoDots: Bool = false) -> (NSHostingView<some View>, NSWindow) {
        let host = NSHostingView(rootView: LibraryWindow(model: model).frame(width: size.width, height: size.height))
        let rect = NSRect(origin: .zero, size: size)
        let window: NSWindow = twoDots ? TwoDotsToThePoint(contentRect: rect, styleMask: [.borderless], backing: .buffered, defer: false)
                                       : NSWindow(contentRect: rect, styleMask: [.borderless], backing: .buffered, defer: false)
        window.isReleasedWhenClosed = false
        window.contentView = host
        host.frame = rect
        settle(host)
        return (host, window)
    }

    private static func median(_ times: [Double]) -> Double {
        times.sorted()[times.count / 2]
    }

    // ---- What is always held

    @Test("A change in what the line of hints says does not stop the window: what the window then has to do is done in a few thousandths of a second")
    func aChangeOfHints() async throws {
        let mac = try MadeUpMac()
        let model = mac.model()
        await model.start()
        model.choose(library: mac.library("Songs"))
        await model.marksAreWorkedOut()
        model.open(song: mac.song("Amazing Grace"))
        model.show(editor: .sheet)
        let (host, window) = window(on: model, size: NSSize(width: 1280, height: 800))
        defer {
            window.contentView = nil
            window.close()
        }
        wait(0.3)
        settle(host)
        var times: [Double] = []
        for turn in 0..<40 {
            let before = Self.atWork()
            // (As the sheet says it when the pointer goes from a word onto a chord
            // and off it again.)
            model.sheetIs(turn % 2 == 0 ? .onChord : .onPlace)
            settle(host)
            times.append(Self.atWork() - before)
        }
        let usual = Self.median(times)
        // (It was 100 to 250, on the Mac this was written on, until the song's header
        // was no longer built three times over each time: SongHeader.swift. It is 4
        // there now; and 118 with the header put back as it was, to see this fail.)
        #expect(usual < 40, "a change of hints kept the window \(usual) thousandths of a second: \(times.map { Int($0.rounded()) })")
        // And what it says did change, each time.
        #expect(model.sheetHints == SheetHints.hints(.onPlace))
    }

    @Test("The line of hints lighting up, and fading over a second or two, keeps the main thread at work for a small part of that time")
    func theLineLightingUp() async throws {
        let mac = try MadeUpMac()
        let model = mac.model()
        await model.start()
        model.choose(library: mac.library("Songs"))
        await model.marksAreWorkedOut()
        model.open(song: mac.song("Amazing Grace"))
        model.show(editor: .sheet)
        let (host, window) = window(on: model, size: NSSize(width: 1280, height: 800))
        defer {
            window.contentView = nil
            window.close()
        }
        wait(0.3)
        settle(host)
        // Two seconds with nothing going on, and then the two seconds after what
        // the line says has changed, in which it lights up and fades.
        var before = Self.atWork()
        wait(2)
        let idle = Self.atWork() - before
        model.sheetIs(.onChord)
        before = Self.atWork()
        wait(2)
        let lit = Self.atWork() - before
        // (It is 130 on the Mac this was written on: a little over a thousandth of a
        // second for each time the screen is drawn while it fades. Were anything in
        // the window measured again each time, it would be many times that.)
        #expect(lit < 800, "the line lighting up kept the main thread \(lit) thousandths of a second at work in two seconds (\(idle) with nothing going on)")
    }

    // ---- What is only measured

    /// A long song: the test file Amazing Grace eight times over, 56 slides, put among
    /// the made-up Mac's songs.
    private func longSong(on mac: MadeUpMac) throws -> ListedSong.ID {
        var song = try RVData_Presentation(serializedBytes: TestData.file("made-by-simple-presenter/Amazing Grace.pro"))
        let groups = song.cueGroups
        let cues = Dictionary(uniqueKeysWithValues: song.cues.map { ($0.uuid.string, $0) })
        for copy in 2...8 {
            for group in groups {
                var again = group
                again.group.uuid.string = UUID().uuidString
                again.group.name = group.group.name + " (\(copy))"
                again.cueIdentifiers = []
                for id in group.cueIdentifiers {
                    var cue = try #require(cues[id.string])
                    cue.uuid.string = UUID().uuidString
                    for action in cue.actions.indices {
                        cue.actions[action].uuid.string = UUID().uuidString
                        for element in cue.actions[action].slide.presentation.baseSlide.elements.indices {
                            cue.actions[action].slide.presentation.baseSlide.elements[element].element.uuid.string = UUID().uuidString
                        }
                    }
                    song.cues.append(cue)
                    var named = id
                    named.string = cue.uuid.string
                    again.cueIdentifiers.append(named)
                }
                song.cueGroups.append(again)
            }
        }
        song.name = "Long Song"
        try song.serializedData().write(to: mac.place.appendingPathComponent("Libraries/Songs/Long Song.pro"))
        return mac.song("Long Song")
    }

    private func say(_ name: String, _ times: [Double]) {
        guard !times.isEmpty else { return }
        let sorted = times.sorted()
        let mean = times.reduce(0, +) / Double(times.count)
        let padded = name.padding(toLength: 52, withPad: " ", startingAt: 0)
        print(String(format: "SPEED %@ usual %6.1f   mean %6.1f   nine in ten under %6.1f   worst %6.1f", padded, sorted[sorted.count / 2], mean,
                     sorted[Int(Double(sorted.count) * 0.9)], sorted.last ?? 0))
    }

    @Test("Measured, when asked for: a long song in a large window, the pointer moved across it, a chord dragged, and the edge moved with no song, with the sheet and with the ChordPro text")
    func measured() async throws {
        guard let asked = ProcessInfo.processInfo.environment["PRO7_SPEED"] else { return }
        let mac = try MadeUpMac()
        let long = try longSong(on: mac)
        let model = mac.model()
        await model.start()
        model.choose(library: mac.library("Songs"))
        await model.marksAreWorkedOut()
        let (host, window) = window(on: model, size: NSSize(width: 1920, height: 1050), twoDots: asked == "2")
        defer {
            window.contentView = nil
            window.close()
        }
        print("SPEED thousandths of a second of the main thread's work, in a window of 1920 by 1050 at \(window.backingScaleFactor) dots to the point")
        try moveTheEdge("the edge moved a step, no song open", host: host, model: model)

        model.open(song: long)
        model.show(editor: .sheet)
        settle(host)
        wait(0.3)
        settle(host)
        let canvas = try #require(find(SheetCanvas.self, in: host))
        let sheet = try #require(model.sheet)
        print("SPEED the song: \(canvas.cells.count) slides, \(sheet.rows.count) rows, \(canvas.columns) slides to a row, \(Int(canvas.visibleRect.width)) by \(Int(canvas.visibleRect.height)) of it seen")
        let seen = try #require(canvas.bitmapImageRepForCachingDisplay(in: canvas.visibleRect))

        // The pointer, across every line that is seen.
        var after: [Double] = [], drawn: [Double] = []
        let visible = canvas.visibleRect
        var y = visible.minY + 60
        while y < visible.maxY - 20 {
            var x = visible.minX + 10
            while x < visible.maxX - 10 {
                let start = Self.atWork()
                canvas.pointerMoved(to: NSPoint(x: x, y: y), shift: false)
                settle(host)
                let settled = Self.atWork()
                canvas.cacheDisplay(in: canvas.visibleRect, to: seen)
                after.append(settled - start)
                drawn.append(Self.atWork() - settled)
                x += 9
            }
            y += 37
        }
        say("the pointer moved: the window's work", after)
        say("the pointer moved: what is seen of the sheet drawn", drawn)

        // A chord, dragged along its line and over others.
        let row = try #require(sheet.rows.firstIndex { $0.kind == .line && !$0.chords.isEmpty })
        let from = try #require(canvas.point(over: Spot(row: row, at: sheet.rows[row].chords[0].at), onItsChord: true))
        canvas.pressed(at: from, shift: false)
        after = []
        for step in 1...150 {
            let start = Self.atWork()
            canvas.dragged(to: NSPoint(x: from.x + Double(step) * 5, y: from.y + Double(step % 30) * 6), shift: false)
            settle(host)
            after.append(Self.atWork() - start)
        }
        sheet.dropNowhere()
        canvas.released(at: from, shift: false, copy: false)
        if sheet.typing { sheet.press(.escape) }
        canvas.sync()
        say("a chord dragged: the window's work", after)

        try moveTheEdge("the edge moved a step, the sheet shown", host: host, model: model)
        model.show(editor: .chordPro)
        settle(host)
        wait(0.3)
        settle(host)
        try moveTheEdge("the edge moved a step, the ChordPro text shown", host: host, model: model)
    }

    private func moveTheEdge(_ name: String, host: NSView, model: LibraryModel) throws {
        var times: [Double] = []
        for width in Array(stride(from: 345.0, through: 445, by: 4)) + Array(stride(from: 445.0, through: 345, by: -4)) {
            let start = Self.atWork()
            model.setListsWidth(width)
            settle(host)
            times.append(Self.atWork() - start)
        }
        model.setListsWidth(nil)
        settle(host)
        say(name, times)
    }
}
