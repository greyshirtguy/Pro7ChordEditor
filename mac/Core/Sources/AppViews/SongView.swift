// The song that is open, at the right of the window: its name, its two keys, a switch
// between its two editors, and the one that is shown: the chord sheet
// (ChordSheetView.swift) or the song as ChordPro text to be typed in
// (ChordProView.swift). spec/06-library.md, rules 12, 14 and 19;
// spec/03-chord-sheet.md; spec/04-chordpro-editor.md; spec/05-song-and-saving.md, rules
// 5 to 7.
//
// `SongLines` is a song's lines only to be looked at, each line of words with its chords
// before the characters they stand over: what an earlier version of a song is shown as
// (EarlierVersionsPanel.swift).

import Song
import SwiftUI

struct SongPane: View {
    @Bindable var model: LibraryModel

    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            if model.proPresenterIsRunning {
                // While it is running, everything can be looked at and nothing changed.
                // The band goes by itself when ProPresenter does.
                Text("ProPresenter is running. Quit it to change chords here.")
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 6)
                    .padding(.horizontal, 10)
                    .background(Color.yellow.opacity(0.35))
            }
            if let shown = model.shown {
                header(shown)
                Divider()
                if let problem = shown.problem {
                    Text(problem)
                        .textSelection(.enabled)
                        .padding(16)
                        .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .topLeading)
                } else if model.editorShown == .sheet, let sheet = model.sheet {
                    SheetStrip(model: model, sheet: sheet, key: shown.key)
                    ChordSheetCanvasView(model: model, version: model.sheetVersion)
                        .frame(maxWidth: .infinity, maxHeight: .infinity)
                } else if let chordPro = model.chordPro {
                    // One line of help over the text; or, in a warmer colour, why this
                    // song can be read here and not changed.
                    Text(chordPro.barred ?? ChordProEditor.help)
                        .font(.callout)
                        .foregroundStyle(chordPro.barred == nil ? AnyShapeStyle(.secondary) : AnyShapeStyle(Color.orange))
                        .lineLimit(4)
                        .fixedSize(horizontal: false, vertical: true)
                        .frame(maxWidth: .infinity, alignment: .leading)
                        .padding(.horizontal, 16)
                        .padding(.vertical, 6)
                        .background(Color(nsColor: .windowBackgroundColor))
                    Divider()
                    ChordProView(model: model, shown: chordPro, canType: model.canType)
                        .frame(maxWidth: .infinity, maxHeight: .infinity)
                }
            } else {
                Text(model.library == nil ? "" : model.songs.isEmpty ? "There are no songs in this library." : "Choose a song to look at it.")
                    .foregroundStyle(.secondary)
                    .frame(maxWidth: .infinity, maxHeight: .infinity)
            }
            Divider()
            HStack(spacing: 10) {
                Text(model.notice.isEmpty ? " " : model.notice)
                    .font(.callout)
                    .foregroundStyle(model.refusal == nil ? .secondary : .primary)
                    .lineLimit(4)
                    .fixedSize(horizontal: false, vertical: true)
                if model.offersOpeningAgain {
                    Button("Open Again") { model.openAgain() }
                        .controlSize(.small)
                        .help("Read the song again from its file, as it is now.")
                }
                if model.justPutBack != nil && model.refusal == nil {
                    // Undoing a putting-back is only putting back the copy that was
                    // made just before it. The offer goes after a few seconds; the copy
                    // stays at the top of the earlier versions.
                    Button("Undo") { model.takeBack() }
                        .controlSize(.small)
                        .help("Put the song back as it was a moment ago.")
                }
                Spacer(minLength: 0)
            }
            .padding(.horizontal, 12)
            .padding(.vertical, 6)
            .task(id: model.justPutBack) {
                guard model.justPutBack != nil else { return }
                try? await Task.sleep(for: .seconds(8))
                if !Task.isCancelled { model.forgetWhatWasPutBack() }
            }
        }
        .background(Color(nsColor: .textBackgroundColor))
    }

    /// The song's name, its keys, and the switch between its two editors: on one row
    /// where the pane is wide enough for the name to be read; otherwise the keys on a
    /// row of their own under it; and where even that is too wide, the switch on a
    /// third. Nothing is ever cut short to fit but the name.
    private func header(_ shown: LibraryModel.ShownSong) -> some View {
        ViewThatFits(in: .horizontal) {
            HStack(alignment: .firstTextBaseline, spacing: 12) {
                name(shown, whole: true)
                Spacer(minLength: 8)
                earlierVersions
                if shown.problem == nil {
                    keys(shown)
                    editorSwitch
                }
            }
            VStack(alignment: .leading, spacing: 6) {
                HStack(alignment: .firstTextBaseline, spacing: 12) {
                    name(shown, whole: false)
                    Spacer(minLength: 8)
                    earlierVersions
                    if shown.problem == nil {
                        editorSwitch
                    }
                }
                if shown.problem == nil {
                    HStack(alignment: .firstTextBaseline, spacing: 12) {
                        keys(shown)
                        Spacer(minLength: 0)
                    }
                }
            }
            VStack(alignment: .leading, spacing: 6) {
                HStack(alignment: .firstTextBaseline, spacing: 12) {
                    name(shown, whole: false)
                    Spacer(minLength: 8)
                    earlierVersions
                }
                if shown.problem == nil {
                    HStack(alignment: .firstTextBaseline, spacing: 12) {
                        keys(shown)
                        Spacer(minLength: 0)
                    }
                    editorSwitch
                }
            }
        }
        .padding(.horizontal, 16)
        .padding(.vertical, 10)
    }

    private func name(_ shown: LibraryModel.ShownSong, whole: Bool) -> some View {
        Text(shown.name)
            .font(.title2)
            .lineLimit(1)
            .truncationMode(.middle)
            // (On the one row the name is not to be cut short to make room: if it
            // would be, the row does not fit, and the keys go under it.)
            .fixedSize(horizontal: whole, vertical: false)
    }

    @ViewBuilder
    private var earlierVersions: some View {
        if model.hasEarlierVersions {
            Button("Earlier Versions") { model.showEarlierVersions() }
                .help("See this song as it was before it was changed here, and put a version back.")
        }
    }

    /// The song's two keys, each to be set: the key its chords are written in, and,
    /// quieter beside it, the key ProPresenter is to show them in. Neither changes any
    /// chord in the file.
    @ViewBuilder
    private func keys(_ shown: LibraryModel.ShownSong) -> some View {
        Picker("Key", selection: Binding(get: { shown.key }, set: { model.setKey($0) })) {
            if shown.key.isEmpty {
                Text("None").tag("")
            }
            ForEach(model.keys, id: \.self) { Text($0).tag($0) }
        }
        .fixedSize()
        .disabled(!model.canChange)
        .help(shown.key.isEmpty ? "This song does not say what key it is in. Pick the key its chords are written in."
                                : "The key the song's chords are written in. Changing it changes no chord.")
        Picker("Shown in", selection: Binding(get: { shown.shownKey }, set: { model.setShownKey($0) })) {
            if shown.shownKey.isEmpty {
                Text("None").tag("")
            }
            ForEach(model.keys, id: \.self) { Text($0).tag($0) }
        }
        .fixedSize()
        .foregroundStyle(.secondary)
        .disabled(!model.canChange || shown.key.isEmpty)
        .help(shown.key.isEmpty ? "Set the song's key first, and then the key ProPresenter is to show it in."
                                : "The key ProPresenter shows this song's chords in. It moves them there itself; the chords here stay as they are written.")
    }

    /// The switch between the song's two editors.
    private var editorSwitch: some View {
        Picker("Editor", selection: Binding(get: { model.editorShown }, set: { model.show(editor: $0) })) {
            Text("Chords").tag(LibraryModel.Editor.sheet)
            Text("ChordPro").tag(LibraryModel.Editor.chordPro)
        }
        .pickerStyle(.segmented)
        .labelsHidden()
        .fixedSize()
        .help("The chord sheet, for the mouse and the keys; or the song as ChordPro text, to be typed in.")
    }
}

/// The lines of a song, one under another, never wrapped.
struct SongLines: View {
    var lines: [SongLine]

    /// A line of ChordPro with its chords coloured: everything from a "[" to its "]".
    static func coloured(_ text: String) -> AttributedString {
        var result = AttributedString()
        var rest = Substring(text)
        while let open = rest.firstIndex(of: "["), let close = rest[open...].firstIndex(of: "]") {
            result.append(AttributedString(rest[..<open]))
            var chord = AttributedString(rest[open...close])
            chord.foregroundColor = .orange
            result.append(chord)
            rest = rest[rest.index(after: close)...]
        }
        result.append(AttributedString(rest))
        return result
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 3) {
            ForEach(Array(lines.enumerated()), id: \.offset) { _, line in
                switch line.kind {
                case .group:
                    HStack(spacing: 6) {
                        if let colour = line.groupColour {
                            Circle()
                                .fill(Color(red: colour.red, green: colour.green, blue: colour.blue))
                                .frame(width: 9, height: 9)
                        }
                        Text(line.group)
                            .font(.headline)
                            .foregroundStyle(.secondary)
                    }
                    .padding(.top, 6)
                case .gap:
                    Color.clear.frame(height: 10)
                case .blankSlide:
                    Text("(a blank slide)")
                        .font(.system(.body, design: .monospaced))
                        .foregroundStyle(.tertiary)
                        .help("This slide has no words.")
                case .words, .chordsAlone:
                    // (A line with nothing in it still takes a line's height.)
                    Text(line.text.isEmpty ? AttributedString(" ") : Self.coloured(line.text))
                        .font(.system(.body, design: .monospaced))
                        .fixedSize()
                        .textSelection(.enabled)
                }
            }
        }
    }
}
