// The window: one, in three parts. spec/06-library.md, "The window".
//
//   at the left    where the songs are (a place: a workspace, ProPresenter's folder, a
//                  folder chosen by hand) and that place's libraries;
//   in the middle  the songs of the library chosen, with a search box over them and a
//                  choice that narrows them by mark;
//   at the right   the song that is open.
//
// Nothing here is a rule. What is shown comes from LibraryModel, and what the person
// does is passed on to it.
//
// The three parts are plain panes side by side, each with a ground of its own, and not
// the system's sidebar with its see-through material. The reason is the tests that draw
// this into a picture file to be looked at (AppViewsTests): drawn off the screen, that
// material comes out blank. The lists, fields and buttons are the system's own.
//
// So far (milestone M3) a song is only shown: SongView.swift. The editors take its
// place at M5 and M6.

import AppKit
import Song
import SwiftUI

public struct LibraryWindow: View {
    @Bindable private var model: LibraryModel

    public init(model: LibraryModel) {
        self.model = model
    }

    public var body: some View {
        HSplitView {
            PlacesPane(model: model)
                .frame(minWidth: 170, idealWidth: 200, maxWidth: 300)
            SongsPane(model: model)
                .frame(minWidth: 220, idealWidth: 270, maxWidth: 420)
            SongPane(model: model)
                .frame(minWidth: 380, maxWidth: .infinity)
        }
        .frame(minWidth: 860, minHeight: 440)
        .sheet(isPresented: Binding(get: { model.earlier != nil }, set: { if !$0 { model.closeEarlierVersions() } })) {
            EarlierVersionsPanel(model: model)
        }
        .task { await model.start() }
        // When the app comes to the front the lists are read again: ProPresenter may
        // have added songs meanwhile, or been started or quit.
        .onReceive(NotificationCenter.default.publisher(for: NSApplication.didBecomeActiveNotification)) { _ in
            Task { await model.refresh() }
        }
    }
}

/// At the left: the place, and its libraries.
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
                        Text(library.name).tag(Optional(library.id))
                    }
                }
                .listStyle(.inset)
                .scrollContentBackground(.hidden)
            }
        }
        .frame(maxHeight: .infinity, alignment: .top)
        .background(Color(nsColor: .controlBackgroundColor))
    }
}

/// In the middle: the songs of the library, a search box over them, and the marks.
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
        .background(Color(nsColor: .controlBackgroundColor))
    }
}

/// A song's name, and beside it its marks, each with a tip that says what it means.
struct SongRow: View {
    var name: String
    var marks: Marks?
    var hasCopies = false

    var body: some View {
        HStack(spacing: 6) {
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
