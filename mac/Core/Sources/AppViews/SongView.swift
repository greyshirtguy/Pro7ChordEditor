// The song that is open, at the right of the window: its name, the switch between its
// two editors (EditorSwitch.swift), its two keys, and the editor that is shown: the
// chord sheet (ChordSheetView.swift) or the song as ChordPro text to be typed in
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
                    ChordSheetCanvasView(model: model, version: model.sheetVersion, scale: model.sheetZoom, chordColour: model.sheetChordColour)
                        .frame(maxWidth: .infinity, maxHeight: .infinity)
                } else if let chordPro = model.chordPro {
                    // One line of help over the text; or, in a warmer colour, why this
                    // song can be read here and not changed.
                    // At its right, the size of the text: where the sheet has the two
                    // buttons that zoom it.
                    HStack(alignment: .center, spacing: 10) {
                        Text(chordPro.barred ?? ChordProEditor.help)
                            .font(.callout)
                            .foregroundStyle(chordPro.barred == nil ? AnyShapeStyle(.secondary) : AnyShapeStyle(Color.orange))
                            .lineLimit(4)
                            .fixedSize(horizontal: false, vertical: true)
                            .frame(maxWidth: .infinity, alignment: .leading)
                        textSize("textformat.size.smaller", by: -1, can: model.canZoomTextOut,
                                 says: "Smaller text. Command with the minus key does the same.")
                        textSize("textformat.size.larger", by: 1, can: model.canZoomTextIn,
                                 says: "Larger text. Command with the plus key does the same; Command with 0 is the usual size.")
                    }
                    .padding(.horizontal, 16)
                    .padding(.vertical, 6)
                    .background(Color(nsColor: .windowBackgroundColor))
                    Divider()
                    ChordProView(model: model, shown: chordPro, canType: model.canType, scale: model.textZoom, chordColour: model.textChordColour)
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
                if let shown = model.shown, shown.problem == nil {
                    saving
                }
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

    /// At the right of the quiet line: Revert, and where a Save button would be, one
    /// that is always greyed, with why beside it. Nothing is saved by hand here: every
    /// change is saved as it is made (spec/06, rule 12a).
    @ViewBuilder
    private var saving: some View {
        Button("Revert") { model.revert() }
            .controlSize(.small)
            .disabled(!model.canRevert)
            .help("Put the song back as it was when it was opened here. What it is now is kept as an earlier version first, so this can be undone.")
        Button("Save") {}
            .controlSize(.small)
            .disabled(true)
            .help("There is nothing to save by hand: every change is saved as it is made, with a copy of the song as it was kept first.")
        Text(model.savingCaption)
            .font(.caption)
            .foregroundStyle(.secondary)
            .fixedSize()
            .help(model.canChange ? "Every change is saved as it is made." : "Nothing is saved while ProPresenter is running.")
    }

    /// One of the two buttons that make the ChordPro text smaller and larger: a small
    /// letter A and a large one.
    private func textSize(_ picture: String, by direction: Int, can: Bool, says: String) -> some View {
        Button {
            model.zoomText(direction)
        } label: {
            Image(systemName: picture)
                .font(.system(size: 13, weight: .medium))
                .frame(width: 26, height: 24)
                .background(RoundedRectangle(cornerRadius: 6, style: .continuous).fill(Color.primary.opacity(0.08)))
                .contentShape(Rectangle())
        }
        .buttonStyle(.plain)
        .foregroundStyle(can ? .primary : .tertiary)
        .disabled(!can)
        .help(says)
        .accessibilityLabel(direction < 0 ? "Smaller text" : "Larger text")
    }

    /// The song's name at the left, the switch between its two editors in the middle,
    /// and its keys at the right: on one row where the pane is wide enough for all
    /// three with the name whole. Otherwise the keys go on a row of their own under
    /// the other two; and where even that is too wide, each has a row. Nothing is
    /// ever cut short to fit but the name.
    private func header(_ shown: LibraryModel.ShownSong) -> some View {
        ViewThatFits(in: .horizontal) {
            // (The two sides are given the same width, so that the switch is in the
            // middle of the pane and not of what is left over.)
            HStack(alignment: .center, spacing: 12) {
                name(shown, whole: true)
                    .frame(maxWidth: .infinity, alignment: .leading)
                if shown.problem == nil {
                    EditorSwitch(model: model)
                }
                HStack(alignment: .firstTextBaseline, spacing: 12) {
                    earlierVersions
                    if shown.problem == nil {
                        keys(shown)
                    }
                }
                .frame(maxWidth: .infinity, alignment: .trailing)
            }
            // (Narrower: the name has what the switch leaves, and is not cut short
            // for the sake of the switch standing in the middle.)
            VStack(alignment: .leading, spacing: 8) {
                HStack(alignment: .center, spacing: 12) {
                    name(shown, whole: false)
                    Spacer(minLength: 8)
                    if shown.problem == nil {
                        EditorSwitch(model: model)
                    }
                }
                HStack(alignment: .firstTextBaseline, spacing: 12) {
                    if shown.problem == nil {
                        keys(shown)
                    }
                    Spacer(minLength: 0)
                    earlierVersions
                }
            }
            VStack(alignment: .leading, spacing: 8) {
                HStack(alignment: .firstTextBaseline, spacing: 12) {
                    name(shown, whole: false)
                    Spacer(minLength: 8)
                    earlierVersions
                }
                if shown.problem == nil {
                    EditorSwitch(model: model)
                        .frame(maxWidth: .infinity)
                    HStack(alignment: .firstTextBaseline, spacing: 12) {
                        keys(shown)
                        Spacer(minLength: 0)
                    }
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
