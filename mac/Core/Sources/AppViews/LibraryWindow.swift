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
// The edge between the left and the right is dragged too, sideways.
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

    public init(model: LibraryModel) {
        self.model = model
    }

    public var body: some View {
        HSplitView {
            ListsPane(model: model)
                .frame(minWidth: 210, idealWidth: 270, maxWidth: 480)
            SongPane(model: model)
                .frame(minWidth: 380, maxWidth: .infinity)
        }
        .frame(minWidth: 620, minHeight: 440)
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
        .onReceive(NotificationCenter.default.publisher(for: NSApplication.willResignActiveNotification)) { _ in
            model.settle()
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

/// The greys of the two lists at the left, and of the bar between them: ProPresenter's
/// own in the dark (read off a picture of its window), and their like in the light.
/// The libraries' ground is the lighter of the two, as there.
enum ListsLook {
    static func grey(light: Double, dark: Double) -> Color {
        Color(nsColor: NSColor(name: nil) { appearance in
            let isDark = appearance.bestMatch(from: [.darkAqua, .aqua]) == .darkAqua
            return NSColor(white: isDark ? dark : light, alpha: 1)
        })
    }

    static let libraries = grey(light: 0.925, dark: 0.345)
    static let songs = grey(light: 0.835, dark: 0.243)
    static let bar = grey(light: 0.74, dark: 0.184)
    /// What the bar says of the list under it: how many songs the library has, and
    /// how many of them are listed where the list is narrowed.
    static func count(shown: Int, of all: Int) -> String {
        if shown != all { return "\(shown) OF \(all) SONGS" }
        return all == 1 ? "1 SONG" : "\(all) SONGS"
    }

    /// How tall the bar is, and the least height each list is left with: for the
    /// libraries, room for what is over them and three of them; for the songs, for
    /// the search box and the choice over them and a few of them.
    static let barHeight = 22.0
    static let leastLibraries = 170.0
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
