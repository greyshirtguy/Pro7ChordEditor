// The window: one, in two parts. spec/06-library.md, "The window".
//
//   at the left    two lists, one over the other, as ProPresenter has them: where the
//                  songs are (a place: a workspace, ProPresenter's folder, a folder
//                  chosen by hand) and that place's libraries; and under them the
//                  songs of the library chosen, with a search box over them and a
//                  choice that narrows them by mark. The bar between the two is
//                  dragged up and down, and the two have grounds of different greys,
//                  the libraries' the lighter;
//   at the right   the song that is open.
//
// The edge between the left and the right is dragged too, sideways. It is this file's
// own, as the bar between the lists is, and not the system's split view: that one gives
// each side half the window, up to the most the lists may have, whatever width it is
// asked to start at (asked for 270, the lists started 480 wide in a window of the usual
// size), and it changes the lists' width as the window is made wider or narrower. Here
// the lists start half way between their least and their most, keep that width while
// the window changes, and are left where they are dragged to, which is remembered.
// What is dragged is a narrow bar (`EdgeGrip`).
//
// Nothing here is a rule. What is shown comes from LibraryModel, and what the person
// does is passed on to it.
//
// The parts are plain panes, each with a ground of its own, and not the system's
// sidebar with its see-through material. One reason is the tests that draw this into
// a picture file to be looked at (AppViewsTests): drawn off the screen, that material
// comes out blank. The other is the two greys. The lists, fields and buttons are the
// system's own.
//
// The song is SongView.swift: its header, and its two editors. What is being typed is
// saved when the app is left, when it quits and when the window goes, as well as a
// moment after the typing stops.
//
// The guide (GuideWindow.swift) is another window, which this one opens.

import AppKit
import Song
import SwiftUI

/// What the window's title bar says: the app's name and, after it in square brackets,
/// its version, so that whoever uses a build, or shows one, sees which it is
/// (spec/06-library.md, rule 6d; the maintainer's, 2026-10-12).
public enum AppTitle {
    public static let name = "Pro7 Chord Editor"

    /// A build that knows of no version says the name alone.
    public static func of(version: String) -> String {
        version.isEmpty ? name : "\(name) [\(version)]"
    }

    /// The version of the app that is running, as it says it of itself to macOS
    /// (MARKETING_VERSION in the Xcode project), or "" where it says none: under the
    /// tests, which are no app.
    public static var versionOfThisApp: String {
        Bundle.main.object(forInfoDictionaryKey: "CFBundleShortVersionString") as? String ?? ""
    }

    /// The title of the app that is running.
    public static var ofThisApp: String {
        of(version: versionOfThisApp)
    }
}

public struct LibraryWindow: View {
    @Bindable private var model: LibraryModel
    @Environment(\.openWindow) private var openWindow
    /// What is done when the window is closed. The app quits: there is one window,
    /// and nothing to do without it (spec/10-platforms.md). Nothing, where none is
    /// given: under the tests, which close a good many windows.
    private var whenClosed: () -> Void
    /// While the edge between the lists and the song is being dragged: how wide the
    /// drag has made the lists. (It is remembered when the edge is let go.)
    @State private var dragged: Double?

    public init(model: LibraryModel, whenClosed: @escaping () -> Void = {}) {
        self.model = model
        self.whenClosed = whenClosed
    }

    public var body: some View {
        GeometryReader { given in
            let whole = given.size.width
            let width = dragged ?? ListsLook.width(asked: model.listsWidth, of: whole)
            HStack(spacing: 0) {
                ListsPane(model: model)
                    .frame(width: width)
                // The edge, which is dragged: a narrow bar of its own between the
                // two parts, lying over neither.
                EdgeGrip(width: width,
                         moved: { dragged = ListsLook.width(asked: $0, of: whole) },
                         ended: {
                             if let dragged {
                                 model.setListsWidth(dragged)
                             }
                             dragged = nil
                         },
                         // (Twice clicked, it goes back to where it usually is.)
                         reset: {
                             dragged = nil
                             model.setListsWidth(nil)
                         })
                    .frame(width: ListsLook.edgeWidth)
                SongPane(model: model)
                    .frame(maxWidth: .infinity)
                    // (How wide it is: what the line of hints fits itself to.)
                    .environment(\.songWidth, max(0, whole - width - ListsLook.edgeWidth))
            }
        }
        .frame(minWidth: 620, minHeight: 440)
        .background(WindowKeeper(save: { model.saveNow() }, closed: whenClosed).frame(width: 0, height: 0))
        .sheet(isPresented: Binding(get: { model.earlier != nil }, set: { if !$0 { model.closeEarlierVersions() } })) {
            EarlierVersionsPanel(model: model)
        }
        // Revert asks first, in words that say it is every change and not the last.
        .alert(model.revertQuestion?.title ?? "", isPresented: Binding(get: { model.revertQuestion != nil }, set: { if !$0 { model.dontRevert() } })) {
            Button(model.revertQuestion?.yes ?? "Revert All Changes", role: .destructive) { model.revert() }
            // (Return is not to be what throws an hour's work away: it cancels.)
            Button("Cancel", role: .cancel) { model.dontRevert() }
                .keyboardShortcut(.defaultAction)
        } message: {
            Text(model.revertQuestion?.message ?? "")
        }
        .task { await model.start() }
        // The guide is a window of its own. It is opened whenever the model says it
        // is to be shown: asked for with the ? in the strip, or by the app itself,
        // once, at its first start (spec/13-guide.md). Asked for while it is open, it
        // comes to the front: there is one of it.
        .onChange(of: model.guideAsked) { openWindow(id: GuideWindow.id) }
        .onReceive(NotificationCenter.default.publisher(for: NSApplication.willResignActiveNotification)) { _ in
            model.settle()
            // (A key held down as the app is left is let go where this app cannot
            // see it.)
            model.holds(false)
        }
        .onReceive(NotificationCenter.default.publisher(for: NSApplication.willTerminateNotification)) { _ in
            model.settle()
        }
        .onDisappear { model.settle() }
        // When the app comes to the front the lists are read again: ProPresenter may
        // have added songs meanwhile, or been started or quit.
        .onReceive(NotificationCenter.default.publisher(for: NSApplication.didBecomeActiveNotification)) { _ in
            Task { await model.refresh() }
        }
        // And whenever the system says ProPresenter has started or quit, whichever app
        // is in front: the band comes and goes by itself (spec/06, rules 18 and 19).
        .onReceive(NSWorkspace.shared.notificationCenter.publisher(for: NSWorkspace.didLaunchApplicationNotification)) { note in
            proPresenterMayHaveChanged(note)
        }
        .onReceive(NSWorkspace.shared.notificationCenter.publisher(for: NSWorkspace.didTerminateApplicationNotification)) { note in
            proPresenterMayHaveChanged(note)
        }
    }

    /// An application has started or quit. If it was ProPresenter, the model asks
    /// again whether it is running: now, and once more a moment on, in case the
    /// system's list of what is running is a beat behind its own news.
    private func proPresenterMayHaveChanged(_ note: Notification) {
        let app = note.userInfo?[NSWorkspace.applicationUserInfoKey] as? NSRunningApplication
        guard app?.bundleIdentifier == PlaceFinder.proPresenter else { return }
        model.proPresenterMayHaveChanged()
        Task {
            try? await Task.sleep(for: .seconds(1))
            model.proPresenterMayHaveChanged()
        }
    }
}

/// What the window needs of AppKit that SwiftUI has no word for: to be told when it
/// closes, and to answer Command+S, which is in no menu (spec/06-library.md, rule 12a:
/// the key is for the hand that presses it out of habit, and a Save in the menu would
/// say there is saving to do).
struct WindowKeeper: NSViewRepresentable {
    var save: () -> Void
    var closed: () -> Void

    final class Keeper: NSView {
        var save: () -> Void = {}
        var closed: () -> Void = {}
        private var watch: (any NSObjectProtocol)?

        override func viewDidMoveToWindow() {
            super.viewDidMoveToWindow()
            if let watch {
                NotificationCenter.default.removeObserver(watch)
                self.watch = nil
            }
            guard let window else { return }
            watch = NotificationCenter.default.addObserver(forName: NSWindow.willCloseNotification, object: window, queue: .main) { [weak self] _ in
                MainActor.assumeIsolated { self?.closed() }
            }
        }

        /// Whether a key pressed is Command+S and nothing more.
        static func isSave(_ event: NSEvent) -> Bool {
            event.type == .keyDown
                && event.modifierFlags.intersection([.command, .control, .option, .shift]) == .command
                && event.charactersIgnoringModifiers?.lowercased() == "s"
        }

        override func performKeyEquivalent(with event: NSEvent) -> Bool {
            guard Self.isSave(event) else { return super.performKeyEquivalent(with: event) }
            save()
            return true
        }
    }

    func makeNSView(context: Context) -> Keeper {
        let keeper = Keeper()
        keeper.save = save
        keeper.closed = closed
        return keeper
    }

    func updateNSView(_ keeper: Keeper, context: Context) {
        keeper.save = save
        keeper.closed = closed
    }
}

/// The edge between the lists and the song, which is dragged sideways: a narrow bar
/// with a small grip in its middle, as the bar between the two lists has.
///
/// It is AppKit's and not SwiftUI's, for the two things the maintainer found wrong
/// with the one before it (2026-10-13: "a bit hard to grab ... it might be too
/// narrow? or take too long to show the mouse cursor change?"). That one was five
/// points of nothing to be seen, and SwiftUI said the pointer was over it a moment
/// late. This one is a bar that is seen, every point of it takes the mouse, and the
/// pointer's shape over it is the system's own to keep (a cursor rectangle), which
/// changes as the pointer crosses the bar's edge. It lies over neither part of the
/// window: the sheet says what shape the pointer is as it moves over the sheet, and
/// the lists have their scroll bars at that side.
struct EdgeGrip: NSViewRepresentable {
    /// How wide the lists are now.
    var width: Double
    /// It is being dragged: the lists are asked to be this wide.
    var moved: (Double) -> Void
    /// It was let go.
    var ended: () -> Void
    /// It was clicked twice.
    var reset: () -> Void

    final class Grip: NSView {
        var width = 0.0
        var moved: (Double) -> Void = { _ in }
        var ended: () -> Void = {}
        var reset: () -> Void = {}
        /// The button is down: where the pointer was across the window when it went
        /// down, and how wide the lists were then.
        private(set) var down: (x: Double, width: Double)?
        private var over = false
        private var tracking: NSTrackingArea?

        override var isFlipped: Bool { true }
        // (A click on it in a window that is not in front drags it all the same.)
        override func acceptsFirstMouse(for event: NSEvent?) -> Bool { true }
        override var mouseDownCanMoveWindow: Bool { false }

        override func resetCursorRects() {
            addCursorRect(bounds, cursor: .resizeLeftRight)
        }

        override func updateTrackingAreas() {
            super.updateTrackingAreas()
            if let tracking {
                removeTrackingArea(tracking)
            }
            let area = NSTrackingArea(rect: .zero, options: [.mouseEnteredAndExited, .activeInKeyWindow, .inVisibleRect], owner: self)
            addTrackingArea(area)
            tracking = area
        }

        override func mouseEntered(with event: NSEvent) {
            over = true
            needsDisplay = true
        }

        override func mouseExited(with event: NSEvent) {
            over = false
            needsDisplay = true
        }

        /// The button went down at this place across the window; `clicks`: how
        /// many times in a row.
        func pressed(at x: Double, clicks: Int) {
            if clicks >= 2 {
                down = nil
                reset()
                return
            }
            down = (x, width)
            needsDisplay = true
        }

        /// The pointer is at this place across the window, with the button down.
        func pulled(to x: Double) {
            guard let down else { return }
            moved(down.width + x - down.x)
        }

        func released() {
            guard down != nil else { return }
            down = nil
            needsDisplay = true
            ended()
        }

        override func mouseDown(with event: NSEvent) {
            pressed(at: event.locationInWindow.x, clicks: event.clickCount)
        }

        override func mouseDragged(with event: NSEvent) {
            pulled(to: event.locationInWindow.x)
        }

        override func mouseUp(with event: NSEvent) {
            released()
        }

        override func draw(_ dirtyRect: NSRect) {
            ListsLook.barGrey.setFill()
            bounds.fill()
            // The grip: a little bar in the middle, darker while the pointer is on
            // the edge or has hold of it.
            let grip = NSRect(x: (bounds.width - 4) / 2, y: (bounds.height - 30) / 2, width: 4, height: 30)
            NSColor.labelColor.withAlphaComponent(over || down != nil ? 0.55 : 0.28).setFill()
            NSBezierPath(roundedRect: grip, xRadius: 2, yRadius: 2).fill()
        }
    }

    func makeNSView(context: Context) -> Grip {
        let grip = Grip()
        grip.toolTip = "Drag sideways to give the lists or the song more room. Clicked twice, it goes back to where it starts."
        grip.setAccessibilityElement(true)
        grip.setAccessibilityRole(.splitter)
        grip.setAccessibilityLabel("The edge between the lists and the song")
        updateNSView(grip, context: context)
        return grip
    }

    func updateNSView(_ grip: Grip, context: Context) {
        grip.width = width
        grip.moved = moved
        grip.ended = ended
        grip.reset = reset
    }
}

/// The greys of the two lists at the left, and of the bar between them: ProPresenter's
/// own in the dark (read off a picture of its window), and their like in the light.
/// The libraries' ground is the lighter of the two, as there.
enum ListsLook {
    /// A grey that is one thing in the light and another in the dark.
    static func systemGrey(light: Double, dark: Double) -> NSColor {
        NSColor(name: nil) { appearance in
            let isDark = appearance.bestMatch(from: [.darkAqua, .aqua]) == .darkAqua
            return NSColor(white: isDark ? dark : light, alpha: 1)
        }
    }

    static func grey(light: Double, dark: Double) -> Color {
        Color(nsColor: systemGrey(light: light, dark: dark))
    }

    static let libraries = grey(light: 0.925, dark: 0.345)
    static let songs = grey(light: 0.835, dark: 0.243)
    /// The bars that are dragged: the one between the two lists, and the edge
    /// between the lists and the song.
    nonisolated(unsafe) static let barGrey = systemGrey(light: 0.74, dark: 0.184)
    static let bar = Color(nsColor: barGrey)
    /// What the bar says of the list under it: how many songs the library has, and
    /// how many of them are listed where the list is narrowed.
    static func count(shown: Int, of all: Int) -> String {
        if shown != all { return "\(shown) OF \(all) SONGS" }
        return all == 1 ? "1 SONG" : "\(all) SONGS"
    }

    /// The edge between the lists and the song: a bar of the colour of the bar
    /// between the two lists, this wide, all of which can be dragged.
    static let edgeWidth = 8.0
    /// The least width the song is left with, whatever the lists are asked to be.
    static let leastSong = 380.0

    /// How wide the lists are, asked to be this wide in a window this wide: never
    /// less than their least nor more than their most (LibraryModel has both), and
    /// never so wide that the song is left with less than its own least. (In a
    /// window too narrow for both, the lists have their least.)
    static func width(asked: Double, of whole: Double) -> Double {
        let most = max(LibraryModel.leastListsWidth, min(LibraryModel.mostListsWidth, whole - edgeWidth - leastSong))
        return min(max(asked, LibraryModel.leastListsWidth), most)
    }

    /// How tall the bar is, and the least height each list is left with: for the
    /// libraries, room for what is over them and three of them; for the songs, for
    /// the search box and the choice over them and a few of them.
    static let barHeight = 22.0
    static let leastLibraries = 190.0
    /// What stands over the choice of place.
    static let placesTitle = "WORKSPACE OR PROPRESENTER FOLDER"
    static let leastSongs = 150.0

    /// How tall the libraries' part is, given this share of a height that also holds
    /// the bar and the songs' part: the share of it, but never so much or so little
    /// that either is left with less than its least. (In a window too short for both,
    /// the libraries have theirs.)
    static func top(share: Double, of height: Double) -> Double {
        let most = max(leastLibraries, height - barHeight - leastSongs)
        return min(max(share * height, leastLibraries), most)
    }
}

/// At the left: the libraries over the songs, with a bar between them that is dragged
/// up and down to give one more room and the other less.
struct ListsPane: View {
    @Bindable var model: LibraryModel
    /// While the bar is being dragged: how tall the libraries' list was when the drag
    /// began, and how tall the drag has made it.
    @State private var began: Double?
    @State private var dragged: Double?
    @State private var over = false

    private var count: String {
        model.library == nil ? "" : ListsLook.count(shown: model.listedSongs.count, of: model.songs.count)
    }

    var body: some View {
        GeometryReader { given in
            let height = given.size.height
            let top = dragged ?? ListsLook.top(share: model.listsSplit, of: height)
            VStack(spacing: 0) {
                PlacesPane(model: model)
                    .frame(height: top)
                bar
                    .gesture(DragGesture(minimumDistance: 1, coordinateSpace: .global)
                        .onChanged { drag in
                            let start = began ?? top
                            began = start
                            dragged = ListsLook.top(share: (start + drag.translation.height) / max(height, 1), of: height)
                        }
                        .onEnded { _ in
                            if let dragged {
                                model.setListsSplit(dragged / max(height, 1))
                            }
                            began = nil
                            dragged = nil
                        })
                    // (Twice clicked, it goes back to where it usually is.)
                    .onTapGesture(count: 2) { model.setListsSplit(nil) }
                SongsPane(model: model)
            }
        }
    }

    /// The bar between the two lists: how many songs the list under it has, and in its
    /// middle a small grip to say that it can be dragged.
    private var bar: some View {
        ZStack {
            ListsLook.bar
            Capsule()
                .fill(Color.primary.opacity(over || dragged != nil ? 0.55 : 0.28))
                .frame(width: 30, height: 4)
            HStack {
                Text(count)
                    .font(.caption2.weight(.semibold))
                    .foregroundStyle(.secondary)
                    .lineLimit(1)
                Spacer(minLength: 0)
            }
            .padding(.horizontal, 10)
        }
        .frame(height: ListsLook.barHeight)
        .contentShape(Rectangle())
        .onHover { inside in
            over = inside
            if inside {
                NSCursor.resizeUpDown.push()
            } else {
                NSCursor.pop()
            }
        }
        .help("Drag up or down to give the libraries or the songs more room.")
        .accessibilityLabel("The bar between the libraries and the songs")
    }
}

/// At the top left: the place, and its libraries.
struct PlacesPane: View {
    @Bindable var model: LibraryModel

    private func describe(_ place: Place) -> String {
        switch place.kind {
        case .workspace: return place.isInUse ? "\(place.name) (workspace in use)" : "\(place.name) (workspace)"
        case .namedInSettings: return "\(place.name) (ProPresenter's folder)"
        case .documents: return "ProPresenter in Documents"
        case .chosen: return "\(place.name) (chosen folder)"
        }
    }

    private func chooseFolder() {
        let panel = NSOpenPanel()
        panel.canChooseDirectories = true
        panel.canChooseFiles = false
        panel.allowsMultipleSelection = false
        panel.message = "Choose a ProPresenter folder, its Libraries folder, or any folder of .pro files."
        panel.prompt = "Choose"
        if panel.runModal() == .OK, let folder = panel.url {
            model.add(folder: folder)
        }
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            VStack(alignment: .leading, spacing: 6) {
                // What this top part is: where the songs are looked for. (The
                // maintainer's, 2026-10-13; the libraries under it had a title and
                // this had none.)
                Text(ListsLook.placesTitle)
                    .font(.caption)
                    .foregroundStyle(.secondary)
                    .lineLimit(1)
                if model.places.isEmpty {
                    // It is not an error: someone may keep a copy of a library elsewhere,
                    // or have no ProPresenter on this Mac at all.
                    Text("No ProPresenter library was found on this Mac.")
                        .fixedSize(horizontal: false, vertical: true)
                } else {
                    Picker("Songs are in", selection: Binding(get: { model.placeID }, set: { model.choose(place: $0) })) {
                        ForEach(model.places) { place in
                            Text(describe(place)).tag(Optional(place.id))
                        }
                    }
                    .labelsHidden()
                    .help(model.place.map { $0.folder.path } ?? "")
                }
                Button("Choose Folder\u{2026}", action: chooseFolder)
                    .help("Open any folder of .pro files: a ProPresenter folder, its Libraries folder, or one library.")
            }
            .padding(10)

            Divider()
            Text("LIBRARIES")
                .font(.caption)
                .foregroundStyle(.secondary)
                .padding(.horizontal, 10)
                .padding(.top, 8)
            if model.libraries.isEmpty && !model.places.isEmpty {
                Text("This folder has no libraries in it.")
                    .foregroundStyle(.secondary)
                    .padding(10)
                Spacer(minLength: 0)
            } else {
                List(selection: Binding(get: { model.libraryID }, set: { model.choose(library: $0) })) {
                    ForEach(model.libraries) { library in
                        HStack(spacing: 7) {
                            LibraryIcon()
                                .frame(width: 16, height: 16)
                            Text(library.name)
                                .lineLimit(1)
                                .truncationMode(.middle)
                        }
                        .tag(Optional(library.id))
                    }
                }
                .listStyle(.inset)
                .scrollContentBackground(.hidden)
            }
        }
        .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .top)
        .background(ListsLook.libraries)
        .clipped()
    }
}

/// Under the libraries: the songs of the library, a search box over them, and the marks.
struct SongsPane: View {
    @Bindable var model: LibraryModel

    var body: some View {
        VStack(spacing: 0) {
            VStack(spacing: 8) {
                HStack(spacing: 6) {
                    Image(systemName: "magnifyingglass")
                        .foregroundStyle(.secondary)
                    TextField("Search", text: $model.typed)
                        .textFieldStyle(.plain)
                    if !model.typed.isEmpty {
                        Button { model.typed = "" } label: { Image(systemName: "xmark.circle.fill") }
                            .buttonStyle(.plain)
                            .foregroundStyle(.secondary)
                            .help("Clear the search")
                    }
                }
                .padding(6)
                .background(RoundedRectangle(cornerRadius: 6).fill(Color(nsColor: .textBackgroundColor)))
                .overlay(RoundedRectangle(cornerRadius: 6).stroke(Color(nsColor: .separatorColor)))

                Picker("Show", selection: $model.filter) {
                    Text("All").tag(MarkFilter.all)
                    Text("With chords").tag(MarkFilter.withChords)
                    Text("Without").tag(MarkFilter.withoutChords)
                }
                .pickerStyle(.segmented)
                .labelsHidden()
                .help("Show every song, only those that have chords, or only those that have none.")
            }
            .padding(10)

            Divider()
            List(selection: Binding(get: { model.songID }, set: { model.open(song: $0) })) {
                ForEach(model.listedSongs) { song in
                    SongRow(name: song.name, marks: model.marks[song.id], hasCopies: model.songsWithCopies.contains(song.name))
                        .tag(Optional(song.id))
                }
            }
            .listStyle(.inset)
            .scrollContentBackground(.hidden)
        }
        .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .top)
        .background(ListsLook.songs)
        .clipped()
    }
}

/// A song's name, with the grey mark of a presentation before it, and after it its
/// marks, each with a tip that says what it means.
struct SongRow: View {
    var name: String
    var marks: Marks?
    var hasCopies = false

    var body: some View {
        HStack(spacing: 6) {
            PresentationIcon()
                .frame(width: 15, height: 15)
            Text(name)
                .lineLimit(1)
                .truncationMode(.middle)
            Spacer(minLength: 4)
            if hasCopies {
                Image(systemName: "clock.arrow.circlepath")
                    .help("Earlier versions of this song are kept.")
            }
            if let marks {
                if marks.cannotBeRead {
                    Image(systemName: "exclamationmark.triangle")
                        .help("This file could not be read as a ProPresenter presentation.")
                }
                if marks.isOldEditorsCopy {
                    Image(systemName: "doc.on.doc")
                        .help("A copy saved by the first Windows chord editor, which named its copies \u{201C}\u{2026} (Chords)\u{201D}.")
                }
                if marks.hasChords {
                    Image(systemName: "music.note")
                        .help(marks.chords == 1 ? "Has 1 chord." : "Has \(marks.chords) chords.")
                }
            }
        }
        .foregroundStyle(marks?.cannotBeRead == true ? .secondary : .primary)
    }
}
