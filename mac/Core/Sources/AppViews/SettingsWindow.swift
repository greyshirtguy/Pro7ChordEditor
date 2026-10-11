// The settings (Command+comma), in sections: how chords are coloured in the two
// editors, and the copies that are kept (spec/06-library.md, "Settings";
// spec/07-backups.md, rule 18, for the second).
//
// Each section is a tab of the system's settings window, which is as big as the tab
// that is open. The copies were all there was here at first, and are AllCopies.swift
// as they were.
//
// A colour is kept by the window's model as it is written, "#rrggbb", and nothing is
// kept where the usual colour is: `ChordColour` turns one into the other. Nothing here
// touches a song: a colour is only how this app draws a chord.

import AppKit
import SwiftUI

/// A colour as it is kept, "#rrggbb", and as it is drawn.
enum ChordColour {
    /// The colour so written; nothing if it is not written so.
    static func colour(_ written: String?) -> NSColor? {
        guard let written, written.count == 7, written.first == "#", let number = UInt32(written.dropFirst(), radix: 16) else { return nil }
        return SheetLook.colour(number)
    }

    /// How a colour is written: its red, green and blue as they are on an ordinary
    /// screen, each as two of 0 to 9 and a to f.
    static func written(_ colour: NSColor) -> String? {
        guard let plain = colour.usingColorSpace(.sRGB) else { return nil }
        func part(_ share: CGFloat) -> Int { Int((min(max(share, 0), 1) * 255).rounded()) }
        return String(format: "#%02x%02x%02x", part(plain.redComponent), part(plain.greenComponent), part(plain.blueComponent))
    }
}

/// The settings window's sections.
public struct SettingsWindow: View {
    @Bindable private var model: LibraryModel
    private var copies: AllCopiesModel

    public init(model: LibraryModel, copies: AllCopiesModel) {
        self.model = model
        self.copies = copies
    }

    public var body: some View {
        TabView {
            ChordColourSettings(model: model)
                .tabItem { Label("Chords", systemImage: "paintpalette") }
            AllCopiesView(model: copies)
                .tabItem { Label("Backups", systemImage: "clock.arrow.circlepath") }
        }
    }
}

/// The colour of a chord in each of the two editors, each with what it looks like and
/// a way back to the usual colour.
struct ChordColourSettings: View {
    @Bindable var model: LibraryModel

    private var onTheSheet: NSColor { ChordColour.colour(model.sheetChordColour) ?? SheetLook.chordName }
    private var inTheText: NSColor { ChordColour.colour(model.textChordColour) ?? .systemOrange }

    var body: some View {
        VStack(alignment: .leading, spacing: 18) {
            row("On the chord sheet", colour: onTheSheet, isUsual: model.sheetChordColour == nil,
                choose: { model.setSheetChordColour($0) }) {
                // A chord as the sheet draws one: its name in a bubble, over a word.
                VStack(alignment: .leading, spacing: 3) {
                    Text("Em7")
                        .font(.system(size: 13, weight: .bold))
                        .foregroundStyle(Color(nsColor: onTheSheet))
                        .padding(.horizontal, 7)
                        .padding(.vertical, 3)
                        .background(RoundedRectangle(cornerRadius: 6).fill(Color(nsColor: SheetLook.bubble)))
                        .overlay(RoundedRectangle(cornerRadius: 6).stroke(Color(nsColor: SheetLook.bubbleEdge)))
                    Text("Amazing grace")
                        .font(.system(size: 17))
                        .foregroundStyle(Color(nsColor: SheetLook.words))
                }
                .padding(10)
                .frame(width: 190, alignment: .leading)
                .background(RoundedRectangle(cornerRadius: 4).fill(Color(nsColor: SheetLook.card)))
            }
            Divider()
            row("In ChordPro text", colour: inTheText, isUsual: model.textChordColour == nil,
                choose: { model.setTextChordColour($0) }) {
                // A line as the text shows one: the chords in their colour, bold, among
                // the words.
                (Text("[Em7]").bold().foregroundColor(Color(nsColor: inTheText)) + Text("Amazing ")
                    + Text("[D]").bold().foregroundColor(Color(nsColor: inTheText)) + Text("grace"))
                    .font(.system(size: 13, design: .monospaced))
                    .padding(10)
                    .frame(width: 190, alignment: .leading)
                    .background(RoundedRectangle(cornerRadius: 4).fill(Color(nsColor: .textBackgroundColor)))
                    .overlay(RoundedRectangle(cornerRadius: 4).stroke(Color(nsColor: .separatorColor)))
            }
            Text("These are only how chords look in this app. Nothing in a song\u{2019}s file says a colour, and ProPresenter shows chords in its own.")
                .font(.callout)
                .foregroundStyle(.secondary)
                .fixedSize(horizontal: false, vertical: true)
        }
        .padding(22)
        .frame(width: 560)
    }

    /// One editor's colour: its name, the well to choose it from, the way back to the
    /// usual one, and what a chord then looks like.
    private func row<Sample: View>(_ name: String, colour: NSColor, isUsual: Bool, choose: @escaping (String?) -> Void,
                                   @ViewBuilder sample: () -> Sample) -> some View {
        HStack(alignment: .center, spacing: 14) {
            VStack(alignment: .leading, spacing: 8) {
                Text(name)
                    .font(.headline)
                HStack(spacing: 10) {
                    ColorPicker("The colour of a chord: \(name.lowercased())",
                                selection: Binding(get: { Color(nsColor: colour) }, set: { choose(ChordColour.written(NSColor($0))) }),
                                supportsOpacity: false)
                        .labelsHidden()
                    Button("Usual Colour") { choose(nil) }
                        .disabled(isUsual)
                        .help("Put this colour back to the one the app comes with.")
                }
            }
            Spacer(minLength: 12)
            sample()
        }
    }
}
