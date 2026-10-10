// All Copies, in the app's settings: every song that has copies kept, by place and
// library, those whose song is no longer where it was included. From here a copy can be
// looked at, or saved somewhere as a file. spec/07-backups.md, rules 13 and 18.
//
// Nothing here changes a song. Putting a copy back is done from the song itself
// (EarlierVersionsPanel), where the song is open and its safeguards are asked.
//
// AllCopiesModel is what the view shows, with no drawing in it, and is tested so.

import AppKit
import Observation
import Song
import SwiftUI

@MainActor
@Observable
public final class AllCopiesModel {
    /// A song that has copies, and what is known of whether it is still where it was.
    public struct Row: Identifiable, Equatable, Sendable {
        public var song: KeptSong
        public var copies: [KeptCopy]
        /// Whether the file the newest copy says it is a copy of is there still. Not
        /// known if the copies' index is lost.
        public var songIsStillThere: Bool?

        public var id: String { song.id }
    }

    private let copies: Copies

    public private(set) var rows: [Row] = []
    public private(set) var pickedSong: KeptSong.ID?
    public private(set) var pickedCopy: KeptCopy.ID?
    /// The copy picked, as its lines, to look at.
    public private(set) var looked: LibraryModel.ShownSong?
    /// How much room all the copies take, in words: "1.2 MB".
    public private(set) var size = ""

    public init(copies: Copies) {
        self.copies = copies
    }

    public static func onThisMac() -> AllCopiesModel {
        AllCopiesModel(copies: AppData.onThisMac().copies())
    }

    /// The folder the copies are in.
    public var folder: URL { copies.folder }

    public var row: Row? { rows.first { $0.id == pickedSong } }
    public var copy: KeptCopy? { row?.copies.first { $0.id == pickedCopy } }

    /// Reads what is kept. Done when the settings are opened.
    public func read() {
        rows = copies.songs().map { song in
            let kept = copies.copies(of: song)
            let whereItWas = kept.first?.copyOf ?? ""
            return Row(song: song, copies: kept, songIsStillThere: whereItWas.isEmpty ? nil : FileManager.default.fileExists(atPath: whereItWas))
        }
        size = ByteCountFormatter.string(fromByteCount: Int64(rows.reduce(0) { $0 + $1.copies.reduce(0) { $0 + $1.size } }), countStyle: .file)
        if row == nil {
            pick(song: rows.first?.id)
        }
    }

    public func pick(song id: KeptSong.ID?) {
        pickedSong = id
        pick(copy: row?.copies.first?.id)
    }

    public func pick(copy id: KeptCopy.ID?) {
        pickedCopy = id
        guard let copy else {
            looked = nil
            return
        }
        if let then = try? OpenSong(opening: copy.url) {
            let boxes = then.textBoxes
            looked = LibraryModel.ShownSong(name: copy.songName, key: then.key, shownKey: then.shownKey, lines: SongText.lines(of: boxes),
                                           slides: then.slideCount, chords: boxes.reduce(0) { $0 + $1.chords.count })
        } else {
            looked = LibraryModel.ShownSong(name: copy.songName, problem: "This copy could not be read as a ProPresenter presentation.")
        }
    }

    public func when(_ copy: KeptCopy) -> String {
        copies.when(copy.made)
    }

    /// Saves the copy picked somewhere as a file: its bytes, unchanged. Answers with
    /// what went wrong, or nothing.
    public func savePicked(to url: URL) -> String? {
        guard let copy else { return nil }
        do {
            try Data(contentsOf: copy.url).write(to: url, options: .atomic)
            return nil
        } catch {
            return "The copy could not be saved there: \(error.localizedDescription)"
        }
    }
}

public struct AllCopiesView: View {
    @Bindable private var model: AllCopiesModel
    @State private var problem: String?

    public init(model: AllCopiesModel) {
        self.model = model
    }

    private func saveAs() {
        guard let copy = model.copy, let row = model.row else { return }
        let panel = NSSavePanel()
        panel.nameFieldStringValue = "\(row.song.name) (\(model.when(copy))).pro"
        panel.message = "Save this copy as a file of its own. The song in the library is not changed."
        if panel.runModal() == .OK, let url = panel.url {
            problem = model.savePicked(to: url)
        }
    }

    public var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            Text("Before this app changes a song\u{2019}s file, it keeps a copy of the file as it was. These are the copies.")
                .fixedSize(horizontal: false, vertical: true)
                .padding(12)
            Divider()
            if model.rows.isEmpty {
                Text("No copies are kept yet.")
                    .foregroundStyle(.secondary)
                    .frame(maxWidth: .infinity, maxHeight: .infinity)
            } else {
                HSplitView {
                    List(selection: Binding(get: { model.pickedSong }, set: { model.pick(song: $0) })) {
                        ForEach(model.rows) { row in
                            VStack(alignment: .leading, spacing: 2) {
                                Text(row.song.name)
                                Text("\(row.song.library), in \(row.song.place)" + (row.songIsStillThere == false ? ". The song is no longer there." : ""))
                                    .font(.caption)
                                    .foregroundStyle(.secondary)
                            }
                            .tag(Optional(row.id))
                        }
                    }
                    .listStyle(.inset)
                    .frame(minWidth: 220, idealWidth: 260, maxWidth: 340)

                    List(selection: Binding(get: { model.pickedCopy }, set: { model.pick(copy: $0) })) {
                        ForEach(model.row?.copies ?? []) { copy in
                            HStack {
                                Text(model.when(copy))
                                Spacer(minLength: 8)
                                Text((copy.isTheFirst ? "(the first) " : "") + (copy.why?.rawValue ?? ""))
                                    .foregroundStyle(.secondary)
                            }
                            .tag(Optional(copy.id))
                        }
                    }
                    .listStyle(.inset)
                    .frame(minWidth: 240, idealWidth: 280, maxWidth: 360)

                    Group {
                        if let problem = model.looked?.problem {
                            Text(problem)
                                .padding(16)
                                .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .topLeading)
                        } else {
                            GeometryReader { pane in
                                ScrollView([.vertical, .horizontal]) {
                                    SongLines(lines: model.looked?.lines ?? [])
                                        .padding(16)
                                        .frame(minWidth: pane.size.width, minHeight: pane.size.height, alignment: .topLeading)
                                }
                            }
                        }
                    }
                    .frame(minWidth: 300, maxWidth: .infinity, maxHeight: .infinity)
                    .background(Color(nsColor: .textBackgroundColor))
                }
            }
            Divider()
            HStack(spacing: 10) {
                Text(problem ?? (model.rows.isEmpty ? "" : "The copies take \(model.size)."))
                    .foregroundStyle(.secondary)
                Spacer(minLength: 8)
                Button("Show Their Folder") { NSWorkspace.shared.activateFileViewerSelecting([model.folder]) }
                    .disabled(model.rows.isEmpty)
                    .help("Show in the Finder the folder the copies are kept in.")
                Button("Save This Copy As\u{2026}", action: saveAs)
                    .disabled(model.copy == nil)
                    .help("Save the copy picked as a file of its own. The song in the library is not changed.")
            }
            .padding(12)
        }
        .frame(minWidth: 860, idealWidth: 940, minHeight: 440, idealHeight: 520)
        .onAppear { model.read() }
    }
}
