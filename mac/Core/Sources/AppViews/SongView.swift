// The song that is open, at the right of the window: its header (SongHeader.swift: its
// name, the switch between its two editors, its two keys), the editor that is shown,
// which is the chord sheet (ChordSheetView.swift) or the song as ChordPro text to be
// typed in (ChordProView.swift), under the sheet the line of hints, and at the foot the
// quiet line. spec/06-library.md, rules 12, 14 and 19; spec/03-chord-sheet.md;
// spec/04-chordpro-editor.md; spec/05-song-and-saving.md, rules 5 to 7.
//
// The parts that change often are views of their own (the line of hints, the quiet
// line), and nothing here is left to SwiftUI to arrange by trying arrangements: what
// that cost is in SongHeader.swift, and in mac/NOTES.md, "What made the window slow".
//
// `SongLines` is a song's lines only to be looked at, each line of words with its chords
// before the characters they stand over: what an earlier version of a song is shown as
// (EarlierVersionsPanel.swift).

import AppKit
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
                SongHeader(model: model, shown: shown)
                Divider()
                if let problem = shown.problem {
                    Text(problem)
                        .textSelection(.enabled)
                        .padding(16)
                        .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .topLeading)
                } else if model.editorShown == .sheet, let sheet = model.sheet {
                    SheetStrip(model: model, sheet: sheet, key: shown.key)
                    ChordSheetCanvasView(model: model, version: model.sheetVersion, scale: model.sheetZoom, chordColour: model.sheetChordColour,
                                         lineColour: model.sheetLineColour)
                        .frame(maxWidth: .infinity, maxHeight: .infinity)
                    Divider()
                    // What the keys and the mouse do just now, where the spot is.
                    SheetHintBar(model: model)
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
            QuietLine(model: model)
        }
        .background(Color(nsColor: .textBackgroundColor))
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
}

/// The quiet line at the foot of the song's part of the window: what is there to be
/// seen, or for a few seconds what just happened; and at its right Revert, and why
/// there is no Save. A view of its own, so that what it says can change (it changes at
/// every chord put on) without the rest of the song's part being worked out again.
struct QuietLine: View {
    @Bindable var model: LibraryModel

    var body: some View {
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

    /// At the right of the quiet line: Revert, which asks before it does anything, and
    /// where a Save button would be, the words that say why there is none. Nothing is
    /// saved by hand here: every change is saved as it is made (spec/06, rule 12a).
    @ViewBuilder
    private var saving: some View {
        Button("Revert\u{2026}") { model.askToRevert() }
            .controlSize(.small)
            .disabled(!model.canRevert)
            .help("Take back every change made to this song since it was opened here, after asking. What the song is now is kept as an earlier version first.")
        Text(model.savingCaption)
            .font(.caption)
            .foregroundStyle(.secondary)
            .fixedSize()
            .help(model.canChange
                  ? "There is nothing to save by hand: every change is saved as it is made, with a copy of the song as it was kept first."
                  : "Nothing is saved while ProPresenter is running.")
    }
}

/// The colours of the song's header that are this app's own.
enum SongLook {
    /// The Earlier Versions button: a blue, the colour furthest from the sheet's
    /// orange, which in this header says which editor is shown.
    static let earlierVersions = Color(red: 0.16, green: 0.47, blue: 0.86)
}

/// A button filled with a colour of the app's choosing, its words white: drawn here
/// and not left to the system's coloured button, which is that colour only in the
/// window that has the keyboard and grey in any other (and in the pictures the tests
/// draw, where it could not be seen at all).
struct FilledButton: ButtonStyle {
    var colour: Color

    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .font(.system(size: 12.5, weight: .medium))
            .foregroundStyle(.white)
            .padding(.horizontal, 10)
            .frame(height: 24)
            .background(RoundedRectangle(cornerRadius: 6, style: .continuous).fill(colour))
            .opacity(configuration.isPressed ? 0.7 : 1)
            .contentShape(Rectangle())
    }
}

/// How the hints are fitted into the width there is: how many of them, on one line or
/// one under another, and how large. Arithmetic, on widths measured with the font the
/// line is drawn in; no window in it.
///
/// Where there is not room for all the hints the last are left off; but not one that
/// is needed (the two sentences on copying and pasting are one thing, and the second
/// is the point of it). Those go one under the other where they do not fit side by
/// side; where one of them is still too long they are drawn smaller, a step at a time;
/// and at their smallest they are cut off at the right.
///
/// WHY IT IS ARITHMETIC, and SwiftUI is not asked to choose the first arrangement
/// that fits (`ViewThatFits`), as it was until 2026-10-13: to choose, SwiftUI builds
/// every arrangement it is offered and measures it, each time the line is laid out,
/// and the line is laid out whenever what it says changes: as often as the pointer
/// crosses a chord. Measured then, that was most of what a move of the pointer cost
/// once the song's header had been put right (SongHeader.swift has the greater part).
@MainActor
enum HintFit {
    struct Fit: Equatable, Sendable {
        /// How many of the hints are said, from the first.
        var count: Int
        /// Whether they are one under another, and not side by side.
        var stacked: Bool
        /// How many times their usual size they are drawn.
        var size: Double
    }

    /// The sizes the needed hints are tried at, one under the other: as they come,
    /// and two steps smaller.
    static let sizes = [1.0, 0.9, 0.8]
    /// The size of the words, and of the sign on a key; the room between two hints,
    /// between the parts of one, and between two lines of them; how tall the line is,
    /// and the room it keeps at its sides and over and under what it says.
    static let wordSize = 12.0
    static let keySize = 11.5
    static let between = 18.0
    static let within = 5.0
    static let lines = 4.0
    static let height = 30.0
    static let sides = 12.0
    static let overAndUnder = 5.0

    private static var known: [String: Double] = [:]

    /// How wide some words are, in the system's font of a size and a weight. (A
    /// point is added: SwiftUI gives a line of text a whole number of points.)
    static func width(of words: String, size: Double, weight: NSFont.Weight = .regular) -> Double {
        let key = "\(size) \(weight.rawValue) \(words)"
        if let width = known[key] { return width }
        let width = ceil((words as NSString).size(withAttributes: [.font: NSFont.systemFont(ofSize: size, weight: weight)]).width) + 1
        known[key] = width
        return width
    }

    /// How wide a key is drawn: its sign and the room either side of it, and never
    /// less than a key is.
    static func width(ofKey key: String, size: Double) -> Double {
        max(20 * size, width(of: key, size: keySize * size, weight: .medium) + 10 * size)
    }

    /// How wide one hint is: its words before its keys, its keys, and what it says.
    static func width(of hint: SheetHints.Hint, size: Double) -> Double {
        var parts: [Double] = []
        if !hint.lead.isEmpty {
            parts.append(width(of: hint.lead, size: wordSize * size))
        }
        parts += hint.keys.map { width(ofKey: $0, size: size) }
        parts.append(width(of: hint.says, size: wordSize * size))
        return parts.reduce(0, +) + within * size * Double(parts.count - 1)
    }

    /// How wide these hints are side by side.
    static func width(of hints: [SheetHints.Hint], size: Double) -> Double {
        hints.map { width(of: $0, size: size) }.reduce(0, +) + between * size * Double(max(0, hints.count - 1))
    }

    /// How the hints are fitted into a width (the width they have to themselves, the
    /// room at the line's sides taken off).
    static func fit(_ hints: [SheetHints.Hint], in room: Double) -> Fit {
        guard !hints.isEmpty else { return Fit(count: 0, stacked: false, size: 1) }
        // (How many of them, from the first, may not be left off.)
        let needed = hints.prefix { $0.needed }.count
        for count in (max(1, needed)...hints.count).reversed() where width(of: Array(hints.prefix(count)), size: 1) <= room {
            return Fit(count: count, stacked: false, size: 1)
        }
        guard needed > 1 else {
            // (One hint, and no room for it: it is cut off.)
            return Fit(count: 1, stacked: false, size: 1)
        }
        for size in sizes where hints.prefix(needed).allSatisfy({ width(of: $0, size: size) <= room }) {
            return Fit(count: needed, stacked: true, size: size)
        }
        return Fit(count: needed, stacked: true, size: sizes.last ?? 1)
    }

    /// How tall the line is for a fit: its usual height, or what the hints need one
    /// under another, if that is more.
    static func height(of fit: Fit) -> Double {
        guard fit.stacked else { return height }
        let line = 19 * fit.size
        return max(height, Double(fit.count) * line + Double(fit.count - 1) * lines * fit.size + 2 * overAndUnder)
    }
}

/// How wide the song's part of the window is: what the line of hints fits itself to.
/// (The window says it, which is what divides itself.)
private struct SongWidthKey: EnvironmentKey {
    static let defaultValue = 900.0
}

extension EnvironmentValues {
    var songWidth: Double {
        get { self[SongWidthKey.self] }
        set { self[SongWidthKey.self] = newValue }
    }
}

/// The line of hints under the sheet: what SheetHints says just now, its keys drawn
/// as keys. As many of the hints as there is room for, the first ones first. It is one
/// line, but for the two sentences on copying and pasting where there is not room for
/// them side by side: then it is two, and the sheet over it that much shorter while
/// they are said.
///
/// How tall it is, is worked out (HintFit) and not asked of what is in it: so a change
/// in what it says, which is as often as the pointer crosses a chord, has nothing
/// else in the window measured again unless the line itself is to be taller.
struct SheetHintBar: View {
    var model: LibraryModel
    @Environment(\.songWidth) private var width

    var body: some View {
        let hints = model.sheetHints
        let fit = HintFit.fit(hints, in: width - 2 * HintFit.sides)
        HintRow(hints: hints, fit: fit)
            .padding(.horizontal, HintFit.sides)
            // (As wide as the song's part of the window and no wider, and as tall
            // as was worked out, whatever is in it: what does not fit is cut off at
            // the right, and pushes nothing about.)
            .frame(minWidth: 0, maxWidth: .infinity, minHeight: HintFit.height(of: fit), maxHeight: HintFit.height(of: fit), alignment: .leading)
            .clipped()
            .background(Color(nsColor: .windowBackgroundColor))
    }
}

/// Hints as they were fitted: so many of them side by side, or one under another.
struct HintRow: View {
    var hints: [SheetHints.Hint]
    var fit: HintFit.Fit

    var body: some View {
        let said = Array(hints.prefix(fit.count))
        if fit.stacked {
            VStack(alignment: .leading, spacing: HintFit.lines * fit.size) {
                ForEach(Array(said.enumerated()), id: \.offset) { _, hint in
                    one(hint, size: fit.size)
                }
            }
        } else {
            HStack(spacing: HintFit.between * fit.size) {
                ForEach(Array(said.enumerated()), id: \.offset) { _, hint in
                    one(hint, size: fit.size)
                }
            }
        }
    }

    private func one(_ hint: SheetHints.Hint, size: Double) -> some View {
        HStack(spacing: HintFit.within * size) {
            if !hint.lead.isEmpty {
                words(hint.lead, size: size)
            }
            ForEach(Array(hint.keys.enumerated()), id: \.offset) { _, key in
                KeyCap(key: key, size: size)
            }
            words(hint.says, size: size)
        }
        .accessibilityElement(children: .ignore)
        .accessibilityLabel(hint.read)
    }

    private func words(_ words: String, size: Double) -> some View {
        Text(words)
            .font(.system(size: HintFit.wordSize * size))
            .foregroundStyle(.secondary)
            .lineLimit(1)
            .fixedSize()
    }
}

/// A key, drawn as one: its sign in a small box with rounded corners.
struct KeyCap: View {
    var key: String
    var size = 1.0

    var body: some View {
        Text(key)
            .font(.system(size: HintFit.keySize * size, weight: .medium))
            .foregroundStyle(.primary.opacity(0.85))
            .lineLimit(1)
            .fixedSize()
            .padding(.horizontal, 5 * size)
            .frame(minWidth: 20 * size, minHeight: 19 * size)
            .background(RoundedRectangle(cornerRadius: 4.5 * size, style: .continuous).fill(Color.primary.opacity(0.07)))
            .overlay(RoundedRectangle(cornerRadius: 4.5 * size, style: .continuous).stroke(Color.primary.opacity(0.28)))
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
