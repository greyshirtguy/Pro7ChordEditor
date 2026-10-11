// The switch between a song's two editors, in the middle of the song's header: the
// chord sheet, for the mouse and the keys, and the song as ChordPro text, to be typed in
// (spec/06-library.md, rule 12).
//
// It is two large buttons side by side and not the system's small segmented control,
// which it was at first: that the song can be worked on in two ways is the first thing
// to know about this window, and the maintainer found it too easy to miss. The one
// that is shown is filled with the sheet's own orange, as the tab of the editor that
// is open is in Simple Presenter. Command with 1 and with 2 choose them from the keys.
//
// Each has a small picture, drawn here and nowhere else:
//
//   the sheet     a chord's bubble in outline with a plus in it, its tail coming down
//                 to a line of words: the sheet's own spot, which is what one looks at
//                 and points with there;
//   the text      a chord as it is typed there: a C in square brackets.

import SwiftUI

/// The sheet's picture: the spot over a line of words, and a shorter line under it.
struct ChordsModeIcon: View {
    var body: some View {
        Canvas { context, size in
            // (Drawn in a box of 22 by 18, and fitted to what it is given.)
            let scale = min(size.width / 22, size.height / 18)
            context.translateBy(x: (size.width - 22 * scale) / 2, y: (size.height - 18 * scale) / 2)
            context.scaleBy(x: scale, y: scale)
            let ink = GraphicsContext.Shading.foreground
            let line = StrokeStyle(lineWidth: 1.5, lineCap: .round, lineJoin: .round)

            // The bubble, in outline, and the plus in it.
            context.stroke(Path(roundedRect: CGRect(x: 2.2, y: 0.9, width: 10.6, height: 7.6), cornerRadius: 2.6), with: ink, style: line)
            var plus = Path()
            plus.move(to: CGPoint(x: 5.5, y: 4.7))
            plus.addLine(to: CGPoint(x: 9.5, y: 4.7))
            plus.move(to: CGPoint(x: 7.5, y: 2.7))
            plus.addLine(to: CGPoint(x: 7.5, y: 6.7))
            context.stroke(plus, with: ink, style: StrokeStyle(lineWidth: 1.3, lineCap: .round))
            // Its tail, down to the words.
            var tail = Path()
            tail.move(to: CGPoint(x: 4.8, y: 8.5))
            tail.addLine(to: CGPoint(x: 4.8, y: 11.4))
            context.stroke(tail, with: ink, style: line)
            // A line of words, and a shorter one, fainter, under it.
            context.fill(Path(roundedRect: CGRect(x: 1.6, y: 11.9, width: 18.8, height: 2.3), cornerRadius: 1.15), with: ink)
            context.opacity = 0.5
            context.fill(Path(roundedRect: CGRect(x: 1.6, y: 15.6, width: 12.4, height: 2.3), cornerRadius: 1.15), with: ink)
        }
        .accessibilityHidden(true)
    }
}

/// The text's picture: a chord as it is typed there.
struct ChordProModeIcon: View {
    var body: some View {
        HStack(spacing: 0.5) {
            Text("[").fontWeight(.medium)
            Text("C").fontWeight(.heavy)
            Text("]").fontWeight(.medium)
        }
        .font(.system(size: 13.5, design: .monospaced))
        .fixedSize()
        .accessibilityHidden(true)
    }
}

/// The two buttons.
struct EditorSwitch: View {
    @Bindable var model: LibraryModel
    @Namespace private var shown

    var body: some View {
        HStack(spacing: 2) {
            choice(.sheet, "Chords", key: "1", says: "The chord sheet: point at a word and press a key, or click and type. Command+1") {
                ChordsModeIcon()
                    .frame(width: 22, height: 18)
            }
            choice(.chordPro, "ChordPro", key: "2", says: "The song as ChordPro text: type chords in square brackets among the words. Command+2") {
                ChordProModeIcon()
            }
        }
        .padding(3)
        .background(RoundedRectangle(cornerRadius: 11, style: .continuous).fill(Color.primary.opacity(0.09)))
        .animation(.snappy(duration: 0.2), value: model.editorShown)
        .fixedSize()
        .accessibilityElement(children: .contain)
        .accessibilityLabel("How the song is shown")
    }

    private func choice<Picture: View>(_ editor: LibraryModel.Editor, _ name: String, key: KeyEquivalent, says: String,
                                       @ViewBuilder picture: () -> Picture) -> some View {
        let isShown = model.editorShown == editor
        return Button {
            model.show(editor: editor)
        } label: {
            HStack(spacing: 7) {
                picture()
                Text(name)
                    .font(.system(size: 13, weight: .semibold))
            }
            // (Dark on the orange, whatever the system is: it is the sheet's orange,
            // and reads the same in the light and in the dark.)
            .foregroundStyle(isShown ? Color.black.opacity(0.86) : Color.primary.opacity(0.78))
            .padding(.horizontal, 14)
            .frame(height: 30)
            .background {
                if isShown {
                    RoundedRectangle(cornerRadius: 8, style: .continuous)
                        .fill(Color(nsColor: SheetLook.accent))
                        .shadow(color: .black.opacity(0.22), radius: 1.5, y: 0.5)
                        .matchedGeometryEffect(id: "shown", in: shown)
                }
            }
            .contentShape(Rectangle())
        }
        .buttonStyle(.plain)
        .keyboardShortcut(key, modifiers: .command)
        .help(says)
        .accessibilityLabel(name)
        .accessibilityAddTraits(isShown ? .isSelected : [])
    }
}
