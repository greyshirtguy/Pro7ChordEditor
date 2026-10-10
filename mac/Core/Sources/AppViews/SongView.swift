// The song that is open, at the right of the window: its name, its key, and its lines
// with their chords in brackets. So far (milestone M3) it is only shown. spec/06-library.md,
// rules 12, 14 and 19; the ChordPro editor (M5) and the chord sheet (M6) take the place
// of the lines.
//
// The lines are the song's text as the ChordPro editor will have it (Song/SongText.swift):
// a group's name before its first slide, a gap between slides, each line of words with
// its chords before the characters they stand over. Chords are orange, brackets and all,
// as spec/04-chordpro-editor.md has them; the colouring never changes the text.

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
                } else {
                    // A line never wraps: the song scrolls both ways. One that is
                    // shorter or narrower than the pane sits at its top left, which a
                    // view that scrolls both ways does not do by itself.
                    GeometryReader { pane in
                        ScrollView([.vertical, .horizontal]) {
                            SongLines(lines: shown.lines)
                                .padding(16)
                                .frame(minWidth: pane.size.width, minHeight: pane.size.height, alignment: .topLeading)
                        }
                    }
                    .frame(maxWidth: .infinity, maxHeight: .infinity)
                }
            } else {
                Text(model.library == nil ? "" : model.songs.isEmpty ? "There are no songs in this library." : "Choose a song to look at it.")
                    .foregroundStyle(.secondary)
                    .frame(maxWidth: .infinity, maxHeight: .infinity)
            }
            Divider()
            Text(model.notice.isEmpty ? " " : model.notice)
                .font(.callout)
                .foregroundStyle(.secondary)
                .lineLimit(1)
                .padding(.horizontal, 12)
                .padding(.vertical, 6)
        }
        .background(Color(nsColor: .textBackgroundColor))
    }

    private func header(_ shown: LibraryModel.ShownSong) -> some View {
        HStack(alignment: .firstTextBaseline, spacing: 12) {
            Text(shown.name)
                .font(.title2)
                .lineLimit(1)
                .truncationMode(.middle)
            Spacer(minLength: 8)
            if shown.problem == nil {
                if shown.key.isEmpty {
                    Text("No key")
                        .foregroundStyle(.secondary)
                        .help("This song does not say what key it is in.")
                } else {
                    Text("Key \(shown.key)")
                        .help("The key the song's chords are written in.")
                    if shown.shownKey != shown.key && !shown.shownKey.isEmpty {
                        Text("Shown in \(shown.shownKey)")
                            .foregroundStyle(.secondary)
                            .help("The key ProPresenter is set to show this song's chords in.")
                    }
                }
            }
        }
        .padding(.horizontal, 16)
        .padding(.vertical, 10)
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
                        .help("This slide has no words. Chords cannot be put on it yet.")
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
